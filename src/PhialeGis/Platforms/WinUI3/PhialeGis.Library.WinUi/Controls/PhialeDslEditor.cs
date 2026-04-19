// PhialeDslEditor.cs (WinUI 3)
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using PhialeGis.Library.Abstractions.Interactions;
using PhialeGis.Library.Abstractions.Interactions.Dsl; // For WebView2 host
using PhialeGis.Library.Abstractions.Interactions.Editors;
using PhialeGis.Library.Abstractions.Modes;
using PhialeGis.Library.Abstractions.Ui.Rendering;
using PhialeGis.Library.DslEditor.Interop;
using PhialeGis.Library.WinUi.Interactions.Bridges;
using Microsoft.UI.Xaml;
using Microsoft.UI.Dispatching;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;

namespace PhialeGis.Library.WinUi.Controls
{
    [TemplatePart(Name = "WebHost", Type = typeof(Grid))]
    [TemplatePart(Name = "PART_WebView2", Type = typeof(WebView2))]
    public sealed partial class PhialeDslEditor : Control
    {
        private Grid? _webHost;
        private WebView2? _webView;

        private const string HtmlResSuffix = ".Assets.DslEditor.TextArea.PhialeDslEditor.html";
        private string? _htmlResName;

        private bool useMonaco = true;

        private int _caretOffset;
        internal int CaretOffsetInternal => _caretOffset;

        private string? _lastPromptModeText;
        private string? _lastPromptHtml;
        private string? _lastPromptKind;
        private bool _hasCachedPrompt;

        // Editor adapter exposed to Core for registration/binding.
        internal EditorAdapter Adapter { get; }
        public object CompositionAdapter => Adapter;

        private readonly List<string> _history = new List<string>();
        private int _histIndex = 0;

        // Connector lives only in this partial
        private DslEditorConnector? _connector;

        public PhialeDslEditor()
        {
            DefaultStyleKey = typeof(PhialeDslEditor);
            Adapter = new EditorAdapter(this);

            Unloaded += (s, e) =>
            {
                try { _connector?.Dispose(); } catch { }
                _connector = null;

                DetachWebView();
            };
        }

        private static readonly JsonSerializerOptions _json =
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        private static string ToJson(object obj) => JsonSerializer.Serialize(obj, _json);

        /// <summary>
        /// Subscribes to semantic events coming from the connector and forwards them to JS host.
        /// Safe to call multiple times; re-subscribes idempotently.
        /// </summary>
        private void HookSemanticEvents()
        {
            if (_connector == null) return;

            _connector.SemanticLegendAvailable += (s, legend) =>
            {
                var json = ToJson(legend);
                DispatcherQueue.TryEnqueue(() =>
                {
                    JsEval($"window.__ph_setSemanticLegend && window.__ph_setSemanticLegend({json});");
                });
            };

            _connector.SemanticTokensAvailable += (s, toks) =>
            {
                var json = ToJson(toks);
                DispatcherQueue.TryEnqueue(() =>
                {
                    JsEval($"window.__ph_applySemanticTokens && window.__ph_applySemanticTokens({json});");
                });
            };

            _connector.ValidationAvailable += (s, validationResult) =>
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    var json = ToJson(validationResult);
                    JsEval($"window.__ph_setDiagnostics && window.__ph_setDiagnostics({json});");
                });
            };
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _webHost = GetTemplateChild("WebHost") as Grid;
            var web = GetTemplateChild("PART_WebView2") as WebView2;

            if (web == null && _webHost != null)
            {
                web = new WebView2();
                _webHost.Children.Add(web);
            }
            if (web != null)
            {
                _ = AttachWebView(web);
            }
        }

        internal async Task AttachWebView(WebView2 view)
        {
            if (useMonaco) { await AttachMonaco(view); return; }
            await AttachRegular(view);
        }

        internal async Task AttachRegular(WebView2 view)
        {
            if (view == null || ReferenceEquals(_webView, view)) return;

            DetachWebView();
            _webView = view;

            // Initialize CoreWebView2 asynchronously
            await view.EnsureCoreWebView2Async();

            // Hook events
            view.WebMessageReceived += OnWebMessageRecived;
            view.NavigationStarting += WebView_NavigationStarting;
            view.NavigationCompleted += OnNavCompleted;

            // JS & host objects
            view.CoreWebView2.Settings.IsScriptEnabled = true;
            view.CoreWebView2.Settings.AreHostObjectsAllowed = true;

            // Navigate to embedded HTML
            if (string.IsNullOrEmpty(_htmlResName))
            {
                var asm = typeof(PhialeDslEditor).GetTypeInfo().Assembly;
                _htmlResName = asm.GetManifestResourceNames()
                                  .FirstOrDefault(n => n.EndsWith(HtmlResSuffix, StringComparison.Ordinal));
            }
            var html = LoadEmbeddedHtml(_htmlResName) ??
                       "<!doctype html><html><body style='font-family:Segoe UI;padding:8px;color:#a00'>Phiale DSL host not found (embedded).</body></html>";
            view.NavigateToString(html);
        }

        internal async Task AttachMonaco(WebView2 view)
        {
            if (view == null) return;

            await view.EnsureCoreWebView2Async();

            // Basic settings
            view.CoreWebView2.Settings.IsScriptEnabled = true;
            view.CoreWebView2.Settings.AreHostObjectsAllowed = true;

            _webView = view;

            view.WebMessageReceived += OnWebMessageRecived;
            view.NavigationStarting += WebView_NavigationStarting;
            view.NavigationCompleted += OnNavCompleted;

            // Map Assets\Monaco to a virtual host
            string mappedFolder = Path.Combine(
                Windows.ApplicationModel.Package.Current.InstalledLocation.Path,
                "Assets");
            mappedFolder = Path.Combine(mappedFolder, "Monaco");

            System.Diagnostics.Debug.WriteLine($"[AttachMonaco] Mapping folder: {mappedFolder}");

            view.CoreWebView2.SetVirtualHostNameToFolderMapping(
                "monaco.appset",
                mappedFolder,
                CoreWebView2HostResourceAccessKind.Allow);

            // Navigate to the page in Assets
            view.Source = new Uri("https://monaco.appset/index.html");

            // Debug log
            view.CoreWebView2.NavigationCompleted += (s, e) =>
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[AttachMonaco] NavigationCompleted: {e.IsSuccess}, {e.WebErrorStatus}, {view.Source}");

                view.CoreWebView2.OpenDevToolsWindow();
            };
        }

        private void WebView_NavigationStarting(WebView2 sender, CoreWebView2NavigationStartingEventArgs args)
        {
            var uri = args.Uri.ToString();
            Diag($"NAV-START: uri={uri} cancel={args.Cancel}");
        }

        private void OnWebMessageRecived(object? o, CoreWebView2WebMessageReceivedEventArgs e)
        {
            var s = e.TryGetWebMessageAsString();
            if (string.IsNullOrWhiteSpace(s)) return;
            WebViewScriptNotify(s);
        }

        private void OnNavCompleted(WebView2 sender, CoreWebView2NavigationCompletedEventArgs a)
        {
            Diag($"NAV-DONE: ok={a.IsSuccess} status={a.WebErrorStatus}");
            if (!a.IsSuccess)
            {
                sender.NavigateToString(
                    $"<html><body style='font-family:Segoe UI;color:#a00'>NAV FAILED: {a.WebErrorStatus}<br/><small></small></body></html>");
                return;
            }

            // Init JS host
            JsEval($"window.__ph_setText({ToJsString(Text)});");
            JsEval($"window.__ph_setReadOnly({(IsReadOnly ? "true" : "false")});");
            var jsMode = IsScriptMode ? "script" : "command";
            JsEval($"if(window.__ph_setMode) window.__ph_setMode('{jsMode}');");
            JsEval("window.__ph_focus();");
            _histIndex = _history.Count;

            _connector?.RequestSemanticRefreshNow();
            ApplyCachedPromptIfAny();
        }

        private static string? LoadEmbeddedHtml(string? resourceName)
        {
            if (string.IsNullOrEmpty(resourceName)) return null;
            try
            {
                var asm = typeof(PhialeDslEditor).GetTypeInfo().Assembly;
                using (var s = asm.GetManifestResourceStream(resourceName))
                {
                    if (s == null) return null;
                    using (var r = new StreamReader(s))
                        return r.ReadToEnd();
                }
            }
            catch { return null; }
        }

        private void DetachWebView()
        {
            if (_webView != null)
            {
                _webView.WebMessageReceived -= OnWebMessageRecived;
                _webView.NavigationStarting -= WebView_NavigationStarting;
                _webView.NavigationCompleted -= OnNavCompleted;
                _webView = null;
            }
        }

        // Kept for parity with original (not wired in WebView2 pipeline)
        private void OnWebViewNavigationCompleted(WebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
        {
            JsEval($"window.__ph_setText({ToJsString(Text)});");
            JsEval($"window.__ph_setReadOnly({(IsReadOnly ? "true" : "false")});");
            var jsMode = IsScriptMode ? "script" : "command";
            JsEval($"if(window.__ph_setMode) window.__ph_setMode('{jsMode}');");
            JsEval("window.__ph_focus();");
            _histIndex = _history.Count;

            _connector?.RequestSemanticRefreshNow();
            ApplyCachedPromptIfAny();
        }

        // ===== JS -> C# bridge =====
        private void WebViewScriptNotify(string s)
        {
            var raw = s;
            var show = raw.Length > 500 ? raw.Substring(0, 500) + "...(+)" : raw;
            Diag("SCRIPT-NOTIFY: " + show);

            try
            {
                var jo = Windows.Data.Json.JsonObject.Parse(s);
                var type = jo.GetNamedString("type", "");

                if (type == "text")
                {
                    var text = jo.GetNamedString("value", "");
                    if (!string.Equals(Text, text, StringComparison.Ordinal))
                        SetValue(TextProperty, text);

                    try
                    {
                        if (jo.ContainsKey("caret"))
                        {
                            var co = (int)jo.GetNamedNumber("caret", -1);
                            if (co >= 0)
                            {
                                _caretOffset = Math.Max(0, Math.Min(co, text?.Length ?? 0));
                                Adapter?.RaiseCaretMoved(0, 0, _caretOffset);
                            }
                        }
                    }
                    catch { /* ignore caret payload errors */ }

                    Adapter?.RaiseTextChanged(text ?? string.Empty);
                    _connector?.OnTextChanged();
                }
                else if (type == "esc")
                {
                    // hide completions
                    JsEval("if(window.__ph_hideCompletions) window.__ph_hideCompletions();");
                    // cancel current interactive action (FSM.Cancel)
                    _core?.CancelInteractiveAction(Adapter);
                    // focus back
                    JsEval("if(window.__ph_focus) window.__ph_focus();");
                }
                else if (type == "enter")
                {
                    if (IsScriptMode) return;

                    var cmd = jo.GetNamedString("value", "");
                    // Try FSM first (interactive actions)
                    var dslMode = DslMode.Normal;
                    try
                    {
                        var ctx = _core?.DslContextProvider?.GetFor(Adapter);
                        if (ctx != null)
                            dslMode = ctx.Mode;
                    }
                    catch
                    {
                        // best effort
                    }

                    if (dslMode == DslMode.Points && _core?.TryHandleInteractiveInput(cmd ?? string.Empty, Adapter) == true)
                    {
                        JsEval("if(window.__ph_clearInput) window.__ph_clearInput();");
                        JsEval("if(window.__ph_focus) window.__ph_focus();");
                        return;
                    }

                    // Legacy path: DSL command line
                    if (!string.Equals(Text, cmd, StringComparison.Ordinal))
                        SetValue(TextProperty, cmd);
                    var safeCmd = cmd ?? string.Empty;
                    Adapter?.RaiseTextChanged(safeCmd);

                    HistoryPush(safeCmd);
                    JsEval($"if(window.__ph_echo) window.__ph_echo({ToJsString(safeCmd)});");

                    Adapter?.RaiseCommand("enter", false, false, false);
                    _ = _connector?.ExecuteNowAsync(safeCmd, false);

                    JsEval("if(window.__ph_clearInput) window.__ph_clearInput();");
                    JsEval("if(window.__ph_focus) window.__ph_focus();");
                }
                else if (type == "runScript")
                {
                    if (!IsScriptMode) return;
                    var script = jo.GetNamedString("value", "");
                    var safeScript = script ?? string.Empty;
                    Adapter?.RaiseTextChanged(safeScript);
                    Adapter?.RaiseCommand("enter", true, false, false);
                    _ = _connector?.ExecuteNowAsync(safeScript, true);
                }
                else if (type == "caret")
                {
                    var off = (int)jo.GetNamedNumber("offset", 0);
                    var line = (int)jo.GetNamedNumber("line", 0);
                    var col = (int)jo.GetNamedNumber("column", 0);
                    _caretOffset = off;
                    Adapter?.RaiseCaretMoved(line, col, off);
                }
                else if (type == "complete")
                {
                    var t = Text ?? string.Empty;
                    if (_caretOffset <= 0 || _caretOffset > t.Length)
                        _caretOffset = t.Length;

                    Adapter?.RaiseCaretMoved(0, 0, _caretOffset);
                    _connector?.RequestCompletions();
                }
                else if (type == "historyUp")
                {
                    var val = HistoryPrev();
                    SetEditorTextFromHistory(val);
                }
                else if (type == "historyDown")
                {
                    var val = HistoryNext();
                    SetEditorTextFromHistory(val);
                }
                else if (type == "completePick")
                {
                    var insert = jo.GetNamedString("insert", "");
                    var start = (int)jo.GetNamedNumber("start", CaretOffsetInternal);
                    var end = (int)jo.GetNamedNumber("end", CaretOffsetInternal);

                    JsEval($"if(window.__ph_replaceRange) window.__ph_replaceRange({start},{end},{ToJsString(insert)});");
                    JsEval("if(window.__ph_hideCompletions) window.__ph_hideCompletions();");
                }
                else if (type == "completeCancel")
                {
                    JsEval("if(window.__ph_hideCompletions) window.__ph_hideCompletions();");
                }
                else if (type == "monacoReady")
                {
                    if (_webView != null)
                        _webView.Focus(FocusState.Programmatic);
                    JsEval("if(window.__ph_focus) window.__ph_focus();");
                }
            }
            catch
            {
                // ignore malformed payloads
            }
        }

        // ===== completions from Core -> show popup in JS =====
        private void OnCompletionsAvailableFromCore(object? sender, object payload)
        {
            var list = payload as DslCompletionListDto;
            if (list?.Items == null || list.Items.Length == 0)
            {
                JsEval("if(window.__ph_hideCompletions) window.__ph_hideCompletions();");
                return;
            }

            // Current editor text and caret
            var text = Text ?? string.Empty;
            var caret = CaretOffsetInternal;
            if (caret < 0 || caret > text.Length) caret = text.Length;

            // Compute start of the "word" under caret (letters/digits/_)
            int start = caret;
            while (start > 0)
            {
                var ch = text[start - 1];
                if (!(char.IsLetterOrDigit(ch) || ch == '_')) break;
                start--;
            }

            var prefix = text.Substring(start, caret - start);

            // Normalize, de-duplicate, and filter by prefix (starts-with, case-insensitive)
            var filtered = list.Items
                .Select(it => new
                {
                    Label = it?.Label ?? string.Empty,
                    Insert = string.IsNullOrEmpty(it?.InsertText) ? (it?.Label ?? string.Empty) : it.InsertText
                })
                .Where(x => !string.IsNullOrEmpty(x.Label) || !string.IsNullOrEmpty(x.Insert))
                .GroupBy(x => (x.Insert ?? x.Label).ToUpperInvariant())
                .Select(g => g.First())
                .Where(x =>
                {
                    if (string.IsNullOrEmpty(prefix)) return true; // Ctrl+Space on empty: show all
                    var insert = x.Insert ?? string.Empty;
                    var label = x.Label ?? string.Empty;
                    return insert.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ||
                           label.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
                })
                .OrderBy(x => x.Label, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (filtered.Count == 0)
            {
                JsEval("if(window.__ph_hideCompletions) window.__ph_hideCompletions();");
                return;
            }

            var itemsJsParts = new List<string>(filtered.Count);
            foreach (var it in filtered)
            {
                var label = ToJsString(it.Label ?? it.Insert ?? string.Empty);
                var insert = ToJsString(it.Insert ?? it.Label ?? string.Empty);
                itemsJsParts.Add($"{{label:{label},insert:{insert}}}");
            }
            var itemsJs = "[" + string.Join(",", itemsJsParts) + "]";

            // Show filtered list; replace range [start, caret)
            JsEval($"if(window.__ph_showCompletions) window.__ph_showCompletions({itemsJs}, {start}, {caret});");
        }

        private void HistoryPush(string cmd)
        {
            if (string.IsNullOrWhiteSpace(cmd)) return;
            if (_history.Count == 0 || !string.Equals(_history[_history.Count - 1], cmd, StringComparison.Ordinal))
                _history.Add(cmd);
            _histIndex = _history.Count;
        }

        private string HistoryPrev()
        {
            if (_history.Count == 0) return string.Empty;
            if (_histIndex > 0) _histIndex--;
            return _history[_histIndex];
        }

        private string HistoryNext()
        {
            if (_history.Count == 0) return string.Empty;
            if (_histIndex < _history.Count - 1)
            {
                _histIndex++;
                return _history[_histIndex];
            }
            _histIndex = _history.Count;
            return string.Empty;
        }

        private void SetEditorTextFromHistory(string text)
        {
            JsEval($"window.__ph_setText({ToJsString(text)})");
        }

        private void JsEval(string js)
        {
            if (string.IsNullOrWhiteSpace(js))
                return;

            if (!DispatcherQueue.HasThreadAccess)
            {
                DispatcherQueue.TryEnqueue(async () =>
                {
                    var c = _webView?.CoreWebView2;
                    if (c == null) return;
                    try { await c.ExecuteScriptAsync(js); } catch { }
                });
                return;
            }

            var webView = _webView;
            if (webView == null) return;

            var core = webView.CoreWebView2;
            if (core != null)
            {
                try { _ = core.ExecuteScriptAsync(js); } catch { }
            }
        }

        private static string ToJsString(string s)
        {
            if (s == null) s = "";
            var esc = s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n");
            return $"\"{esc}\"";
        }

        // ===================== DIAG =====================
        private static void Diag(string msg)
        {
            try { System.Diagnostics.Debug.WriteLine("[PhialeDslEditor] " + msg); } catch { }
        }

        private async Task<bool> FileExistsAsync(Uri appxUri)
        {
            try
            {
                var f = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(appxUri);
                Diag($"EXISTS: {appxUri} -> OK ({f.Name})");
                return true;
            }
            catch (Exception ex)
            {
                Diag($"EXISTS: {appxUri} -> MISSING ({ex.GetType().Name}: {ex.Message})");
                return false;
            }
        }
        // ================== END DIAG ====================

        internal void SetFSMStateAsync(string modeText, string htmlText, string kind)
        {
            // Escape for safe JS string literal
            string escapedMode = JavaScriptEncoder.Default.Encode(modeText ?? string.Empty);
            string escapedHtml = JavaScriptEncoder.Default.Encode(htmlText ?? string.Empty);
            string escapedKind = JavaScriptEncoder.Default.Encode(kind ?? "idle");

            string script = $"__ph_setFSMState(\"{escapedMode}\", \"{escapedHtml}\", \"{escapedKind}\");";
            JsEval(script);
        }

        internal void CachePrompt(string modeText, string htmlText, string kind)
        {
            _lastPromptModeText = modeText ?? string.Empty;
            _lastPromptHtml = htmlText ?? string.Empty;
            _lastPromptKind = string.IsNullOrWhiteSpace(kind) ? "idle" : kind;
            _hasCachedPrompt = true;
        }

        private void ApplyCachedPromptIfAny()
        {
            if (!_hasCachedPrompt) return;
            SetFSMStateAsync(_lastPromptModeText ?? string.Empty,
                             _lastPromptHtml ?? string.Empty,
                             _lastPromptKind ?? "idle");
        }

        // ============== Adapter ==============
        internal sealed partial class EditorAdapter :
            IEditorInteractive,
            IEditorViewportLink,
            IEditorTextSource,
            IEditorTargetAware
        {
            private readonly PhialeDslEditor _owner;
            public EditorAdapter(PhialeDslEditor owner) => _owner = owner;

            private WeakReference<IRenderingComposition>? _targetDrawWeak;

            public string Text => _owner.Text ?? string.Empty;
            int IEditorTextSource.CaretOffset => _owner.CaretOffsetInternal;
            public int CaretOffset => _owner.CaretOffsetInternal;

            internal void InternalSetTargetDraw(IRenderingComposition? target)
                => _targetDrawWeak = target != null ? new WeakReference<IRenderingComposition>(target) : null;

            internal IRenderingComposition? TryGetTargetDraw()
                => _targetDrawWeak != null && _targetDrawWeak.TryGetTarget(out var rc) ? rc : null;

            object? IEditorViewportLink.GetAttachedViewportAdapterOrNull() => TryGetTargetDraw();

            // IEditorTargetAware for Core.RegisterControl → GraphicsFacade wiring
            public object? TargetDraw => TryGetTargetDraw();

            public void RunOnUI(Action action)
            {
                // WinUI 3: DispatcherQueue replaces CoreDispatcher
                _owner.DispatcherQueue.TryEnqueue(() => action());
            }
        }
    }
}
