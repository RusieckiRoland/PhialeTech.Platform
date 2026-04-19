// PhialeDslEditor.cs (Avalonia + CEF Glue)
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Threading;
using PhialeGis.Library.Abstractions.Interactions;
using PhialeGis.Library.Abstractions.Interactions.Dsl;
using PhialeGis.Library.Abstractions.Interactions.Editors;
using PhialeGis.Library.Abstractions.Modes;
using PhialeGis.Library.Abstractions.Ui.Rendering;
using PhialeGis.Library.AvaloniaUi.Interactions.Bridges;
using PhialeGis.Library.DslEditor.Interop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using Xilium.CefGlue;
using Xilium.CefGlue.Avalonia;
using Xilium.CefGlue.Common.Events;

namespace PhialeGis.Library.AvaloniaUi.Controls
{
    public sealed partial class PhialeDslEditor : TemplatedControl
    {
        private Panel? _webHost;
        private AvaloniaCefBrowser? _browser;
        private bool _useMonaco = true;
        private bool _isApplyingTextFromJs;
        private bool _jsBridgeAvailable;

        private int _caretOffset;
        internal int CaretOffsetInternal => _caretOffset;

        internal EditorAdapter Adapter { get; }
        public object CompositionAdapter => Adapter;

        private readonly List<string> _history = new List<string>();
        private int _histIndex = 0;

        private readonly JsBridge _jsBridge;

        private DslEditorConnector? _connector;

        public PhialeDslEditor()
        {
            Adapter = new EditorAdapter(this);
            _jsBridge = new JsBridge(this);

            DetachedFromVisualTree += (_, __) =>
            {
                try
                {
                    _connector?.Dispose();
                }
                catch
                {
                    // best effort
                }

                _connector = null;
                DetachBrowser();
            };
        }

        private static readonly JsonSerializerOptions _json =
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        private static string ToJson(object obj) => JsonSerializer.Serialize(obj, _json);

        private void HookSemanticEvents()
        {
            if (_connector == null)
                return;

            _connector.SemanticLegendAvailable += (_, legend) =>
            {
                var json = ToJson(legend);
                Dispatcher.UIThread.Post(() =>
                {
                    JsEval($"window.__ph_setSemanticLegend && window.__ph_setSemanticLegend({json});");
                }, DispatcherPriority.Background);
            };

            _connector.SemanticTokensAvailable += (_, toks) =>
            {
                var json = ToJson(toks);
                Dispatcher.UIThread.Post(() =>
                {
                    JsEval($"window.__ph_applySemanticTokens && window.__ph_applySemanticTokens({json});");
                }, DispatcherPriority.Background);
            };

            _connector.ValidationAvailable += (_, validationResult) =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    var json = ToJson(validationResult);
                    JsEval($"window.__ph_setDiagnostics && window.__ph_setDiagnostics({json});");
                }, DispatcherPriority.Background);
            };
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            _webHost = e.NameScope.Find<Panel>("WebHost");
            var browser = e.NameScope.Find<AvaloniaCefBrowser>("PART_CefBrowser");

            if (_webHost == null && browser == null)
            {
                // English: Fail fast - template is missing required parts.
                throw new System.InvalidOperationException(
                    "CEF template is missing both 'WebHost' Panel and 'PART_CefBrowser' control. " +
                    "Define at least one of them in the control template.");
            }

            if (browser == null)
            {
                if (_webHost == null)
                {
                    // English: Can't host dynamically created browser without a host panel.
                    throw new System.InvalidOperationException(
                        "CEF template is missing 'WebHost' Panel. Cannot create and attach AvaloniaCefBrowser dynamically.");
                }

                try
                {
                    browser = CreateBrowserOrThrow();
                }
                catch (System.Exception ex)
                {
                    // English: Most common causes: missing native CEF deps, wrong arch (x86 vs x64), or no suitable ctor.
                    throw new System.InvalidOperationException(
                        "Failed to create AvaloniaCefBrowser. Ensure: (1) x64 build, (2) native CEF dependencies are present, " +
                        "and (3) you use a valid CefApp factory (never null) if the control requires it.",
                        ex);
                }

                if (browser == null)
                {
                    // English: Activator returned null - treat as fatal.
                    throw new System.InvalidOperationException("Activator.CreateInstance returned null for AvaloniaCefBrowser.");
                }

                _webHost.Children.Add(browser);
            }

            browser.Focusable = true;
            browser.IsHitTestVisible = true;

            // English: OnApplyTemplate may run multiple times - avoid duplicate subscriptions.
            browser.PointerPressed -= OnBrowserPointerPressed;
            browser.PointerPressed += OnBrowserPointerPressed;

            _ = AttachBrowserAsync(browser);
        }

        private static AvaloniaCefBrowser CreateBrowserOrThrow()
        {
            var type = typeof(AvaloniaCefBrowser);
            Exception? lastCtorException = null;
            string? lastCtorSignature = null;

            // Prefer simple constructors, but fall back to any public ctor we can satisfy.
            var ctors = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .OrderBy(c => c.GetParameters().Length)
                .ToArray();

            foreach (var ctor in ctors)
            {
                var args = TryBuildCtorArgs(ctor.GetParameters());
                if (args == null)
                    continue;

                try
                {
                    return (AvaloniaCefBrowser)ctor.Invoke(args);
                }
                catch (Exception ex)
                {
                    lastCtorSignature = DescribeCtorSignature(ctor);
                    if (ex is TargetInvocationException tie && tie.InnerException != null)
                        lastCtorException = tie.InnerException;
                    else
                        lastCtorException = ex;
                    // Try the next candidate.
                }
            }

            // Last chance: allow non-public parameterless ctor if present.
            try
            {
                var instance = Activator.CreateInstance(type, nonPublic: true);
                if (instance is AvaloniaCefBrowser browser)
                    return browser;
            }
            catch (Exception ex)
            {
                if (lastCtorException == null)
                    lastCtorException = ex;
            }

            if (lastCtorException != null)
            {
                throw new InvalidOperationException(
                    "AvaloniaCefBrowser ctor was found but threw an exception. " +
                    (lastCtorSignature == null ? string.Empty : $"Last tried ctor: {lastCtorSignature}. ") +
                    "See inner exception for details.",
                    lastCtorException);
            }

            var ctorDump = DescribeCefBrowserCtors(type);
            throw new MissingMethodException(
                "AvaloniaCefBrowser has no supported constructor. Ensure your CefGlue.Avalonia version exposes a usable public ctor. " +
                "Available ctors: " + ctorDump);
        }

        private static string DescribeCefBrowserCtors(Type type)
        {
            try
            {
                var ctors = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (ctors.Length == 0)
                    return "(none)";

                var parts = new List<string>();
                foreach (var ctor in ctors)
                {
                    var ps = ctor.GetParameters();
                var sig = ps.Length == 0
                        ? "()"
                        : "(" + string.Join(", ", ps.Select(p =>
                            $"{p.ParameterType.Name}[{p.ParameterType.FullName}]")) + ")";
                    var access = ctor.IsPublic ? "public"
                        : ctor.IsFamily ? "protected"
                        : ctor.IsAssembly ? "internal"
                        : ctor.IsPrivate ? "private"
                        : "nonpublic";
                    parts.Add(access + " " + sig);
                }

                return string.Join(" | ", parts);
            }
            catch
            {
                return "(failed to inspect ctors)";
            }
        }

        private static string DescribeCtorSignature(ConstructorInfo ctor)
        {
            try
            {
                var ps = ctor.GetParameters();
                var sig = ps.Length == 0
                    ? "()"
                    : "(" + string.Join(", ", ps.Select(p =>
                        $"{p.ParameterType.Name}[{p.ParameterType.FullName}]")) + ")";
                var access = ctor.IsPublic ? "public"
                    : ctor.IsFamily ? "protected"
                    : ctor.IsAssembly ? "internal"
                    : ctor.IsPrivate ? "private"
                    : "nonpublic";
                return access + " " + sig;
            }
            catch
            {
                return "(failed to describe ctor)";
            }
        }

        private static object?[]? TryBuildCtorArgs(ParameterInfo[] parameters)
        {
            if (parameters.Length == 0)
                return Array.Empty<object?>();

            var args = new object?[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                var p = parameters[i];
                var pt = p.ParameterType;

                if (pt == typeof(Func<CefApp>))
                {
                    args[i] = (Func<CefApp>)(() => new DefaultCefApp());
                    continue;
                }

                if (pt == typeof(CefApp))
                {
                    args[i] = new DefaultCefApp();
                    continue;
                }

                if (pt == typeof(Func<CefBrowserSettings>))
                {
                    args[i] = (Func<CefBrowserSettings>)(() => new CefBrowserSettings());
                    continue;
                }

                if (pt == typeof(CefBrowserSettings))
                {
                    args[i] = new CefBrowserSettings();
                    continue;
                }

                if (pt == typeof(Func<CefSettings>))
                {
                    args[i] = (Func<CefSettings>)(() => new CefSettings());
                    continue;
                }

                if (pt == typeof(CefSettings))
                {
                    args[i] = new CefSettings();
                    continue;
                }

                if (pt == typeof(Func<Func<CefRequestContext>>))
                {
                    args[i] = (Func<Func<CefRequestContext>>)(() => () => CefRequestContext.GetGlobalContext());
                    continue;
                }

                if (pt == typeof(Func<CefRequestContext>))
                {
                    args[i] = (Func<CefRequestContext>)(() => CefRequestContext.GetGlobalContext());
                    continue;
                }

                if (p.HasDefaultValue)
                {
                    args[i] = p.DefaultValue;
                    continue;
                }

                if (!pt.IsValueType)
                {
                    args[i] = null;
                    continue;
                }

                args[i] = Activator.CreateInstance(pt);
            }

            return args;
        }

        private void OnBrowserPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            _browser?.Focus();
            JsEval("window.__ph_focus && window.__ph_focus();");
        }

        internal async Task AttachBrowserAsync(AvaloniaCefBrowser browser)
        {
            if (browser == null || ReferenceEquals(_browser, browser))
                return;

            DetachBrowser();
            _browser = browser;

            _browser.LoadStart += OnLoadStart;
            _browser.LoadEnd += OnLoadEnd;
            _browser.LoadError += OnLoadError;
            _browser.TitleChanged += OnTitleChanged;
            _browser.AddressChanged += OnAddressChanged;
            _browser.ConsoleMessage += OnConsoleMessage;

            TryRegisterJsBridge(_browser);

            if (_useMonaco)
            {
                await AttachMonacoAsync(_browser).ConfigureAwait(false);
                return;
            }

            await AttachRegularAsync(_browser).ConfigureAwait(false);
        }

        internal Task AttachRegularAsync(AvaloniaCefBrowser browser)
        {
            // Avalonia implementation prefers Monaco host.
            // Keep no-op fallback for API parity.
            return Task.CompletedTask;
        }

        internal Task AttachMonacoAsync(AvaloniaCefBrowser browser)
        {
            var baseDir = AppContext.BaseDirectory;
            var monacoRoot = Path.Combine(baseDir, "Assets", "Monaco");
            var htmlPath = Path.Combine(monacoRoot, "index.avalonia.html");

            if (!File.Exists(htmlPath))
                htmlPath = Path.Combine(monacoRoot, "index.html");

            if (!File.Exists(htmlPath))
            {
                Diag($"Monaco host file missing: {htmlPath}");
                return Task.CompletedTask;
            }

            var uri = new Uri(htmlPath, UriKind.Absolute);
            _browser!.Address = uri.AbsoluteUri;
            return Task.CompletedTask;
        }

        private void OnLoadStart(object? sender, LoadStartEventArgs args)
        {
            Diag("NAV-START");
        }

        private void OnLoadError(object? sender, LoadErrorEventArgs args)
        {
            Diag($"NAV-ERR: code={args.ErrorCode} text={args.ErrorText} url={args.FailedUrl}");
        }

        private void OnLoadEnd(object? sender, LoadEndEventArgs args)
        {
            try
            {
                if (args?.Frame != null && !args.Frame.IsMain)
                    return;
            }
            catch
            {
                // Some runtimes may not expose IsMain. Fallback: continue init.
            }

            Dispatcher.UIThread.Post(() =>
            {
                var statusCode = args?.HttpStatusCode ?? 0;
                Diag($"NAV-DONE: status={statusCode}");

                _browser?.Focus();
                JsEval($"window.__ph_setText({ToJsString(Text)});");
                JsEval($"window.__ph_setReadOnly({(IsReadOnly ? "true" : "false")});");
                var jsMode = IsScriptMode ? "script" : "command";
                JsEval($"if(window.__ph_setMode) window.__ph_setMode('{jsMode}');");
                var stamp = GetBuildStamp();
                if (!string.IsNullOrWhiteSpace(stamp))
                {
                    // Show build stamp in the status bar and placeholder.
                    SetFSMStateAsync($"Idle mode | build {stamp}", string.Empty, "idle");
                    JsEval($"if(window.__ph_setPlaceholder) window.__ph_setPlaceholder({ToJsString($"command (build {stamp})")});");
                }
                JsEval("window.__ph_focus && window.__ph_focus();");
                _histIndex = _history.Count;

                _connector?.RequestSemanticRefreshNow();
            }, DispatcherPriority.Background);
        }

        private static string GetBuildStamp()
        {
            try
            {
                var asm = typeof(PhialeDslEditor).Assembly;
                var loc = asm.Location;
                if (string.IsNullOrWhiteSpace(loc) || !File.Exists(loc))
                    return string.Empty;

                var dt = File.GetLastWriteTime(loc);
                return dt.ToString("yyyy-MM-dd HH:mm:ss");
            }
            catch
            {
                return string.Empty;
            }
        }

        private void OnTitleChanged(object? sender, string title)
        {
            if (_jsBridgeAvailable)
                return;

            if (string.IsNullOrWhiteSpace(title))
                return;

            if (!TryExtractPhialePayload(title, out var payload))
                return;

            try
            {
                Diag("OnTitleChanged -> payload");
                WebViewScriptNotify(payload);
            }
            catch
            {
                // best effort
            }
        }

        private void OnAddressChanged(object? sender, string address)
        {
            if (_jsBridgeAvailable)
                return;

            if (string.IsNullOrWhiteSpace(address))
                return;

            if (!TryExtractPhialePayload(address, out var payload))
                return;

            try
            {
                Diag("OnAddressChanged -> payload");
                WebViewScriptNotify(payload);
            }
            catch
            {
                // best effort
            }
        }

        private void OnConsoleMessage(object? sender, ConsoleMessageEventArgs e)
        {
            if (_jsBridgeAvailable)
                return;

            if (e == null || string.IsNullOrWhiteSpace(e.Message))
                return;

            if (!TryExtractPhialePayload(e.Message, out var payload))
                return;

            try
            {
                Diag("OnConsoleMessage -> payload");
                WebViewScriptNotify(payload);
            }
            catch
            {
                // best effort
            }
        }

        private void TryRegisterJsBridge(AvaloniaCefBrowser browser)
        {
            try
            {
                // "external.notify(...)" fallback in host JS.
                if (!browser.IsJavascriptObjectRegistered("external"))
                {
                    browser.RegisterJavascriptObject(_jsBridge, "external", OnJavascriptMethodCall);
                }
                _jsBridgeAvailable = true;
            }
            catch (Exception ex)
            {
                _jsBridgeAvailable = false;
                Diag("RegisterJavascriptObject failed: " + ex.Message);
            }
        }

        private object? OnJavascriptMethodCall(Func<object> originalFunction)
        {
            if (originalFunction == null)
                return null;

            try
            {
                return originalFunction();
            }
            catch
            {
                return null;
            }
        }

        private void DetachBrowser()
        {
            if (_browser == null)
                return;

            try
            {
                _browser.LoadStart -= OnLoadStart;
                _browser.LoadEnd -= OnLoadEnd;
                _browser.LoadError -= OnLoadError;
                _browser.TitleChanged -= OnTitleChanged;
                _browser.AddressChanged -= OnAddressChanged;
                _browser.ConsoleMessage -= OnConsoleMessage;
            }
            catch
            {
                // best effort
            }

            _browser = null;
            _jsBridgeAvailable = false;
        }

        private void WebViewScriptNotify(string payload)
        {
            var raw = payload ?? string.Empty;
            var show = raw.Length > 500 ? raw.Substring(0, 500) + "...(+)" : raw;
            Diag("SCRIPT-NOTIFY: " + show);

            try
            {
                using var doc = JsonDocument.Parse(raw);
                var root = doc.RootElement;

                string type = root.TryGetProperty("type", out var typeProp) && typeProp.ValueKind == JsonValueKind.String
                    ? typeProp.GetString() ?? string.Empty
                    : string.Empty;

                if (type == "text")
                {
                    Diag("SCRIPT type=text");
                    var text = root.TryGetProperty("value", out var valProp) && valProp.ValueKind == JsonValueKind.String
                        ? valProp.GetString() ?? string.Empty
                        : string.Empty;

                    if (!string.Equals(Text, text, StringComparison.Ordinal))
                        SetTextFromJs(text);

                    try
                    {
                        if (root.TryGetProperty("caret", out var caretProp) && caretProp.ValueKind == JsonValueKind.Number)
                        {
                            var co = caretProp.GetInt32();
                            if (co >= 0)
                            {
                                _caretOffset = Math.Max(0, Math.Min(co, text.Length));
                                Adapter.RaiseCaretMoved(0, 0, _caretOffset);
                            }
                        }
                    }
                    catch
                    {
                        // ignore malformed caret payload
                    }

                    Adapter.RaiseTextChanged(text);
                    _connector?.OnTextChanged();
                }
                else if (type == "esc")
                {
                    JsEval("if(window.__ph_hideCompletions) window.__ph_hideCompletions();");
                    _core?.CancelInteractiveAction(Adapter);
                    JsEval("if(window.__ph_focus) window.__ph_focus();");
                }
                else if (type == "enter")
                {
                    Diag("SCRIPT type=enter");

                    if (IsScriptMode)
                        return;

                    var cmd = root.TryGetProperty("value", out var valueProp) && valueProp.ValueKind == JsonValueKind.String
                        ? valueProp.GetString() ?? string.Empty
                        : string.Empty;

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

                    Diag("ENTER cmd='" + cmd + "' mode=" + dslMode);

                    // Route input to interactive FSM only while action mode is active.
                    // In normal mode, always execute as a regular DSL command.
                    if (dslMode == DslMode.Points && _core?.TryHandleInteractiveInput(cmd, Adapter) == true)
                    {
                        JsEval("if(window.__ph_clearInput) window.__ph_clearInput();");
                        JsEval("if(window.__ph_focus) window.__ph_focus();");
                        return;
                    }

                    if (!string.Equals(Text, cmd, StringComparison.Ordinal))
                        SetTextFromJs(cmd);

                    Adapter.RaiseTextChanged(cmd);

                    HistoryPush(cmd);
                    JsEval($"if(window.__ph_echo) window.__ph_echo({ToJsString(cmd)});");

                    Adapter.RaiseCommand("enter", false, false, false);
                    _ = _connector?.ExecuteNowAsync(cmd, false);

                    JsEval("if(window.__ph_clearInput) window.__ph_clearInput();");
                    JsEval("if(window.__ph_focus) window.__ph_focus();");
                }
                else if (type == "runScript")
                {
                    if (!IsScriptMode)
                        return;

                    var script = root.TryGetProperty("value", out var scriptProp) && scriptProp.ValueKind == JsonValueKind.String
                        ? scriptProp.GetString() ?? string.Empty
                        : string.Empty;

                    Adapter.RaiseTextChanged(script);
                    Adapter.RaiseCommand("enter", true, false, false);
                    _ = _connector?.ExecuteNowAsync(script, true);
                }
                else if (type == "caret")
                {
                    var off = root.TryGetProperty("offset", out var offProp) && offProp.ValueKind == JsonValueKind.Number ? offProp.GetInt32() : 0;
                    var line = root.TryGetProperty("line", out var lineProp) && lineProp.ValueKind == JsonValueKind.Number ? lineProp.GetInt32() : 0;
                    var col = root.TryGetProperty("column", out var colProp) && colProp.ValueKind == JsonValueKind.Number ? colProp.GetInt32() : 0;
                    _caretOffset = off;
                    Adapter.RaiseCaretMoved(line, col, off);
                }
                else if (type == "complete")
                {
                    Diag("SCRIPT type=complete");
                    var text = Text ?? string.Empty;
                    if (_caretOffset <= 0 || _caretOffset > text.Length)
                        _caretOffset = text.Length;

                    Adapter.RaiseCaretMoved(0, 0, _caretOffset);
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
                    var insert = root.TryGetProperty("insert", out var insProp) && insProp.ValueKind == JsonValueKind.String
                        ? insProp.GetString() ?? string.Empty
                        : string.Empty;
                    var start = root.TryGetProperty("start", out var startProp) && startProp.ValueKind == JsonValueKind.Number
                        ? startProp.GetInt32()
                        : CaretOffsetInternal;
                    var end = root.TryGetProperty("end", out var endProp) && endProp.ValueKind == JsonValueKind.Number
                        ? endProp.GetInt32()
                        : CaretOffsetInternal;

                    JsEval($"if(window.__ph_replaceRange) window.__ph_replaceRange({start},{end},{ToJsString(insert)});");
                    JsEval("if(window.__ph_hideCompletions) window.__ph_hideCompletions();");
                }
                else if (type == "completeCancel")
                {
                    JsEval("if(window.__ph_hideCompletions) window.__ph_hideCompletions();");
                }
                else if (type == "monacoReady")
                {
                    Focus();
                    JsEval("if(window.__ph_focus) window.__ph_focus();");
                }
            }
            catch
            {
                // ignore malformed payloads
            }
        }

        private void OnCompletionsAvailableFromCore(object? sender, object? payload)
        {
            var list = payload as DslCompletionListDto;
            if (list?.Items == null || list.Items.Length == 0)
            {
                JsEval("if(window.__ph_hideCompletions) window.__ph_hideCompletions();");
                return;
            }

            var text = Text ?? string.Empty;
            var caret = CaretOffsetInternal;
            if (caret < 0 || caret > text.Length)
                caret = text.Length;

            int start = caret;
            while (start > 0)
            {
                var ch = text[start - 1];
                if (!(char.IsLetterOrDigit(ch) || ch == '_'))
                    break;
                start--;
            }

            var prefix = text.Substring(start, caret - start);

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
                    if (string.IsNullOrEmpty(prefix))
                        return true;

                    var insertOrLabel = x.Insert ?? x.Label ?? string.Empty;
                    var labelOrInsert = x.Label ?? x.Insert ?? string.Empty;
                    return insertOrLabel.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ||
                           labelOrInsert.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
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
            JsEval($"if(window.__ph_showCompletions) window.__ph_showCompletions({itemsJs}, {start}, {caret});");
        }

        private void HistoryPush(string cmd)
        {
            if (string.IsNullOrWhiteSpace(cmd))
                return;

            if (_history.Count == 0 || !string.Equals(_history[_history.Count - 1], cmd, StringComparison.Ordinal))
                _history.Add(cmd);

            _histIndex = _history.Count;
        }

        private string HistoryPrev()
        {
            if (_history.Count == 0)
                return string.Empty;

            if (_histIndex > 0)
                _histIndex--;

            return _history[_histIndex];
        }

        private string HistoryNext()
        {
            if (_history.Count == 0)
                return string.Empty;

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

        private void SetTextFromJs(string text)
        {
            _isApplyingTextFromJs = true;
            try
            {
                SetValue(TextProperty, text ?? string.Empty);
            }
            finally
            {
                _isApplyingTextFromJs = false;
            }
        }

        internal bool IsApplyingTextFromJs => _isApplyingTextFromJs;

        private void JsEval(string js)
        {
            if (string.IsNullOrWhiteSpace(js))
                return;

            var browser = _browser;
            if (browser == null)
                return;

            void Run()
            {
                try
                {
                    browser.ExecuteJavaScript(js, "about:blank", 0);
                }
                catch
                {
                    // best effort
                }
            }

            if (Dispatcher.UIThread.CheckAccess())
            {
                Run();
                return;
            }

            Dispatcher.UIThread.Post(Run, DispatcherPriority.Background);
        }

        private static string ToJsString(string? s)
        {
            var value = s ?? string.Empty;
            var esc = value
                .Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("\"", "\\\"", StringComparison.Ordinal)
                .Replace("\r", "\\r", StringComparison.Ordinal)
                .Replace("\n", "\\n", StringComparison.Ordinal);
            return $"\"{esc}\"";
        }

        private static bool TryExtractPhialePayload(string? raw, out string payload)
        {
            payload = string.Empty;
            if (string.IsNullOrWhiteSpace(raw))
                return false;

            var text = raw.Trim();

            if (text.Length >= 2)
            {
                var first = text[0];
                var last = text[text.Length - 1];
                if ((first == '"' && last == '"') || (first == '\'' && last == '\''))
                    text = text.Substring(1, text.Length - 2);
            }

            const string prefix = "phiale:";
            var idx = text.IndexOf(prefix, StringComparison.Ordinal);
            if (idx < 0)
                return false;

            var encoded = text.Substring(idx + prefix.Length);
            if (string.IsNullOrWhiteSpace(encoded))
                return false;

            try
            {
                payload = Uri.UnescapeDataString(encoded);
            }
            catch
            {
                payload = encoded;
            }

            return true;
        }

        private static void Diag(string msg)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[PhialeDslEditor/Avalonia] " + msg);
            }
            catch
            {
                // best effort
            }
        }

        internal void SetFSMStateAsync(string modeText, string htmlText, string kind)
        {
            string escapedMode = JavaScriptEncoder.Default.Encode(modeText ?? string.Empty);
            string escapedHtml = JavaScriptEncoder.Default.Encode(htmlText ?? string.Empty);
            string escapedKind = JavaScriptEncoder.Default.Encode(kind ?? "idle");
            string script = $"__ph_setFSMState(\"{escapedMode}\", \"{escapedHtml}\", \"{escapedKind}\");";
            JsEval(script);
        }

        internal sealed partial class EditorAdapter :
            IEditorInteractive,
            IEditorViewportLink,
            IEditorTextSource,
            IEditorTargetAware
        {
            private readonly PhialeDslEditor _owner;
            private WeakReference<IRenderingComposition>? _targetDrawWeak;

            public EditorAdapter(PhialeDslEditor owner) => _owner = owner;

            public string Text => _owner.Text ?? string.Empty;
            int IEditorTextSource.CaretOffset => _owner.CaretOffsetInternal;
            public int CaretOffset => _owner.CaretOffsetInternal;

            internal void InternalSetTargetDraw(IRenderingComposition? target)
            {
                _targetDrawWeak = target != null ? new WeakReference<IRenderingComposition>(target) : null;
            }

            internal IRenderingComposition? TryGetTargetDraw()
            {
                return _targetDrawWeak != null && _targetDrawWeak.TryGetTarget(out var rc) ? rc : null;
            }

            object? IEditorViewportLink.GetAttachedViewportAdapterOrNull() => TryGetTargetDraw();

            public object? TargetDraw => TryGetTargetDraw();

            public void RunOnUI(Action action)
            {
                if (action == null)
                    return;

                if (Dispatcher.UIThread.CheckAccess())
                {
                    action();
                    return;
                }

                Dispatcher.UIThread.Post(action, DispatcherPriority.Background);
            }
        }

        private sealed class JsBridge
        {
            private readonly WeakReference<PhialeDslEditor> _owner;

            public JsBridge(PhialeDslEditor owner)
            {
                _owner = new WeakReference<PhialeDslEditor>(owner);
            }

            // Called from JS: window.external.notify(payload)
            public void notify(string payload) => Dispatch(payload);

            // Called from JS if someone uses postMessage style on external object.
            public void postMessage(string payload) => Dispatch(payload);

            private void Dispatch(string payload)
            {
                if (!_owner.TryGetTarget(out var owner))
                    return;

                Dispatcher.UIThread.Post(() =>
                {
                    owner.WebViewScriptNotify(payload);
                }, DispatcherPriority.Background);
            }
        }

        private sealed class DefaultCefApp : CefApp
        {
        }
    }
}
