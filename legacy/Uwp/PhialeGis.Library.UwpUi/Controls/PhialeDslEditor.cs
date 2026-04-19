//PhialeDslEditor.cs
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using PhialeGis.Library.Abstractions.Interactions;
using PhialeGis.Library.Abstractions.Interactions.Dsl; // Dla WebView2 w UWP z WinUI
using PhialeGis.Library.Abstractions.Interactions.Editors;
using PhialeGis.Library.Abstractions.Modes;
using PhialeGis.Library.Abstractions.Ui.Rendering;
using PhialeGis.Library.DslEditor.Interop;
using PhialeGis.Library.UwpUi.Interactions.Bridges;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace PhialeGis.Library.UwpUi.Controls
{
    [TemplatePart(Name = "WebHost", Type = typeof(Grid))]
    [TemplatePart(Name = "PART_WebView2", Type = typeof(WebView2))]
    public sealed partial class PhialeDslEditor : Control
    {
        private Grid _webHost;
        private WebView2 _webView;

        private const string HtmlResSuffix = ".Assets.DslEditor.TextArea.PhialeDslEditor.html";
        private string _htmlResName;

        private bool useMonaco = true;

        private int _caretOffset;
        internal int CaretOffsetInternal => _caretOffset;

        // Editor adapter exposed to Core for registration/binding.
        internal EditorAdapter Adapter { get; }
        public object CompositionAdapter => Adapter;

        private readonly List<string> _history = new List<string>();
        private int _histIndex = 0;

        // Connector lives only in this partial
        private DslEditorConnector _connector;

        public PhialeDslEditor()
        {
            DefaultStyleKey = typeof(PhialeDslEditor);
            Adapter = new EditorAdapter(this);

            Unloaded += (s, e) =>
            {
                try
                {
                    _connector?.Dispose();
                }
                catch { }
                
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

            _connector.SemanticLegendAvailable += async (s, legend) =>
            {
                var json = ToJson(legend);
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    JsEval($"window.__ph_setSemanticLegend && window.__ph_setSemanticLegend({json});");
                });
            };

            _connector.SemanticTokensAvailable += async (s, toks) =>
            {
                var json = ToJson(toks);
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    JsEval($"window.__ph_applySemanticTokens && window.__ph_applySemanticTokens({json});");
                });
            };


            _connector.ValidationAvailable += async (s, validationResult) =>
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
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
            { _ = AttachWebView(web); }

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

            // Inicjalizuj CoreWebView2 asynchronicznie
            await _webView.EnsureCoreWebView2Async();

            // Podłącz zdarzenia do CoreWebView2 (użyj += i -=)

            _webView.WebMessageReceived += OnWebMessageRecived;

            _webView.NavigationStarting += WebView_NavigationStarting;

            _webView.NavigationCompleted += OnNavCompleted;

            // Ustawienia JavaScript i komunikacji (ważne dla window.external.notify)
            _webView.CoreWebView2.Settings.IsScriptEnabled = true;
            _webView.CoreWebView2.Settings.AreHostObjectsAllowed = true; // Włącz host objects

            // Nawigacja do HTML
            if (string.IsNullOrEmpty(_htmlResName))
            {
                var asm = typeof(PhialeDslEditor).GetTypeInfo().Assembly;
                _htmlResName = asm.GetManifestResourceNames()
                                  .FirstOrDefault(n => n.EndsWith(HtmlResSuffix, StringComparison.Ordinal));
            }
            var html = LoadEmbeddedHtml(_htmlResName) ??
                       "<!doctype html><html><body style='font-family:Segoe UI;padding:8px;color:#a00'>Phiale DSL host not found (embedded).</body></html>";
            _webView.NavigateToString(html);
        }



        internal async Task AttachMonaco(WebView2 view)
        {
            if (view == null) return;

            var mappedFolder = ResolveMonacoAssetsFolder();
            if (string.IsNullOrEmpty(mappedFolder))
            {
                Diag("[AttachMonaco] Monaco assets not found. Falling back to regular editor host.");
                await AttachRegular(view);
                return;
            }

            await view.EnsureCoreWebView2Async();

            // Podstawowe ustawienia
            view.CoreWebView2.Settings.IsScriptEnabled = true;
            view.CoreWebView2.Settings.AreHostObjectsAllowed = true;

            _webView = view;

            _webView.WebMessageReceived += OnWebMessageRecived;

            _webView.NavigationStarting += WebView_NavigationStarting;

            _webView.NavigationCompleted += OnNavCompleted;

            System.Diagnostics.Debug.WriteLine($"[AttachMonaco] Mapping folder: {mappedFolder}");

            _webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                "monaco.appset",
                mappedFolder,
                CoreWebView2HostResourceAccessKind.Allow);

            // Otwieramy stronę z Assets
            _webView.Source = new Uri("https://monaco.appset/index.html");

            // Debug log
            _webView.CoreWebView2.NavigationCompleted += (s, e) =>
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[AttachMonaco] NavigationCompleted: {e.IsSuccess}, {e.WebErrorStatus}, {_webView.Source}");

                // Otwórz DevTools, żeby zobaczyć zawartość
                _webView.CoreWebView2.OpenDevToolsWindow();
            };
        }

        private static string ResolveMonacoAssetsFolder()
        {
            var installed = Windows.ApplicationModel.Package.Current.InstalledLocation.Path;
            var candidates = new[]
            {
                Path.Combine(installed, "Assets", "Monaco"),
                Path.Combine(installed, "PhialeGis.Library.UwpUi", "Assets", "Monaco"),
                Path.Combine(installed, "Files", "PhialeGis.Library.UwpUi", "Assets", "Monaco")
            };

            foreach (var candidate in candidates)
            {
                try
                {
                    if (!Directory.Exists(candidate)) continue;
                    if (!File.Exists(Path.Combine(candidate, "index.html"))) continue;
                    if (!File.Exists(Path.Combine(candidate, "phiale-dsl-host.js"))) continue;
                    return candidate;
                }
                catch
                {
                    // Ignore path probing failures and continue with next candidate.
                }
            }

            return null;
        }




        private void WebView_NavigationStarting(WebView2 sender, CoreWebView2NavigationStartingEventArgs args)
        {
            var uri = args.Uri.ToString();
            Diag($"NAV-START: uri={uri} cancel={args.Cancel}");
        }

        private void OnWebMessageRecived(object o, CoreWebView2WebMessageReceivedEventArgs e)
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
                _webView.NavigateToString(
                    $"<html><body style='font-family:Segoe UI;color:#a00'>NAV FAILED: {a.WebErrorStatus}<br/><small></small></body></html>");
                return;
            }



            // Twoja dotychczasowa inicjalizacja:
            JsEval($"window.__ph_setText({ToJsString(Text)});");
            JsEval($"window.__ph_setReadOnly({(IsReadOnly ? "true" : "false")});");
            var jsMode = IsScriptMode ? "script" : "command";
            JsEval($"if(window.__ph_setMode) window.__ph_setMode('{jsMode}');");
            JsEval("window.__ph_focus();");
            _histIndex = _history.Count;

            _connector?.RequestSemanticRefreshNow();
        }


        private static string LoadEmbeddedHtml(string resourceName)
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

        private void OnWebViewNavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
        {
            JsEval($"window.__ph_setText({ToJsString(Text)});");
            JsEval($"window.__ph_setReadOnly({(IsReadOnly ? "true" : "false")});");
            var jsMode = IsScriptMode ? "script" : "command";
            JsEval($"if(window.__ph_setMode) window.__ph_setMode('{jsMode}');");
            JsEval("window.__ph_focus();");
            _histIndex = _history.Count;

            // Push legend + tokens if connector already exists.
            _connector?.RequestSemanticRefreshNow();
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

                    Adapter?.RaiseTextChanged(text);
                    _connector?.OnTextChanged();
                }
                else if (type == "esc")
                {
                    // zamknij ewentualne podpowiedzi
                    JsEval("if(window.__ph_hideCompletions) window.__ph_hideCompletions();");
                    // anuluj akcję (FSM.Cancel)
                    _core?.CancelInteractiveAction(Adapter);
                    // fokus z powrotem
                    JsEval("if(window.__ph_focus) window.__ph_focus();");
                }
                else if (type == "enter")
                {
                    if (IsScriptMode) return;

                    var cmd = jo.GetNamedString("value", "");
                    // NAJPIERW spróbuj skierować do FSM (akcje interaktywne)
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
                        // FSM skonsumował – nie echo’uj, wyczyść input i wróć
                        JsEval("if(window.__ph_clearInput) window.__ph_clearInput();");
                        JsEval("if(window.__ph_focus) window.__ph_focus();");
                        return;
                    }

                    // --- stary path: DSL command line ---
                    if (!string.Equals(Text, cmd, StringComparison.Ordinal))
                        SetValue(TextProperty, cmd);
                    Adapter?.RaiseTextChanged(cmd);

                    HistoryPush(cmd);
                    JsEval($"if(window.__ph_echo) window.__ph_echo({ToJsString(cmd)});");

                    Adapter?.RaiseCommand("enter", false, false, false);
                    _ = _connector?.ExecuteNowAsync(cmd, false);

                    JsEval("if(window.__ph_clearInput) window.__ph_clearInput();");
                    JsEval("if(window.__ph_focus) window.__ph_focus();");
                }
                else if (type == "runScript")
                {
                    if (!IsScriptMode) return;
                    var script = jo.GetNamedString("value", "");
                    Adapter?.RaiseTextChanged(script);
                    Adapter?.RaiseCommand("enter", true, false, false);
                    _ = _connector?.ExecuteNowAsync(script, true);
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

                    _webView.Focus(Windows.UI.Xaml.FocusState.Programmatic);

                    JsEval("window.__ph_focus();");

                }
            }
            catch
            {
                // ignore malformed payloads
            }
        }

        // ===== completions from Core -> show popup in JS =====
        // ======= show completions payload from core (filtered by current prefix) =======
        private void OnCompletionsAvailableFromCore(object sender, object payload)
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

            var prefix = text.Substring(start, caret - start); // what user has already typed

            // Normalize, de-duplicate, and filter by prefix (starts-with, case-insensitive)
            // If you prefer "contains", change the predicate below accordingly.
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
                    return (x.Insert ?? x.Label).StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ||
                           (x.Label ?? x.Insert).StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
                })
                .OrderBy(x => x.Label, StringComparer.OrdinalIgnoreCase)
                .ToList();

            // Nothing matches the typed prefix → hide popup
            if (filtered.Count == 0)
            {
                JsEval("if(window.__ph_hideCompletions) window.__ph_hideCompletions();");
                return;
            }

            // Build JSON for JS host
            var itemsJsParts = new System.Collections.Generic.List<string>(filtered.Count);
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



        private async void JsEval(string js)
        {
            if (string.IsNullOrWhiteSpace(js) || _webView == null)
                return;

            // UWP/WinUI 3: Dispatcher na kontrolce
            if (Dispatcher.HasThreadAccess)
            {
                var core = _webView.CoreWebView2;
                if (core == null) return;
                try
                {
                    await core.ExecuteScriptAsync(js);
                }
                catch { /* swallow/log */ }
            }
            else
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                {
                    var core = _webView?.CoreWebView2;
                    if (core == null) return;
                    try
                    {
                        await core.ExecuteScriptAsync(js);
                    }
                    catch { /* swallow/log */ }
                });
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
            try
            {
                System.Diagnostics.Debug.WriteLine("[PhialeDslEditor] " + msg);
            }
            catch { }
        }

        private async System.Threading.Tasks.Task<bool> FileExistsAsync(Uri appxUri)
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




        // ============== Adapter ==============
        internal sealed partial class EditorAdapter :
            IEditorInteractive,
            IEditorViewportLink,
            IEditorTextSource,
            IEditorTargetAware
        {
            private readonly PhialeDslEditor _owner;
            public EditorAdapter(PhialeDslEditor owner) => _owner = owner;

            private WeakReference<IRenderingComposition> _targetDrawWeak;

            public string Text => _owner.Text ?? string.Empty;
            int IEditorTextSource.CaretOffset => _owner.CaretOffsetInternal;
            public int CaretOffset => _owner.CaretOffsetInternal;

            internal void InternalSetTargetDraw(IRenderingComposition target)
                => _targetDrawWeak = target != null ? new WeakReference<IRenderingComposition>(target) : null;

            internal IRenderingComposition TryGetTargetDraw()
                => _targetDrawWeak != null && _targetDrawWeak.TryGetTarget(out var rc) ? rc : null;

            object IEditorViewportLink.GetAttachedViewportAdapterOrNull() => (object)TryGetTargetDraw();

            // IEditorTargetAware for Core.RegisterControl → GraphicsFacade wiring
            public object TargetDraw => (object)TryGetTargetDraw();

            public void RunOnUI(Action action)
            {
                var _ = _owner.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => action());
            }
        }





    }
}
