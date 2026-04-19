using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using PhialeTech.WebHost.Abstractions.Ui.Web;
using PhialeTech.WebHost;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xilium.CefGlue;
using Xilium.CefGlue.Avalonia;
using Xilium.CefGlue.Common.Events;

namespace PhialeTech.WebHost.Avalonia.Controls
{
    /// <summary>
    /// Reusable Avalonia host for browser-based UI components.
    /// </summary>
    public sealed class PhialeWebComponentHost : UserControl, IWebComponentHost
    {
        private readonly Grid _root;
        private readonly AvaloniaWebComponentPlatformBridge _bridge;
        private readonly WebComponentHostRuntime _runtime;
        private bool _disposed;

        public PhialeWebComponentHost()
            : this(new WebComponentHostOptions())
        {
        }

        public PhialeWebComponentHost(WebComponentHostOptions options)
        {
            _root = new Grid();
            Content = _root;
            Focusable = true;

            _bridge = new AvaloniaWebComponentPlatformBridge(_root);
            _runtime = new WebComponentHostRuntime(_bridge, options ?? new WebComponentHostOptions());

            AttachedToVisualTree += OnAttachedToVisualTree;
        }

        public WebComponentHostOptions Options => _runtime.Options;

        public new bool IsInitialized => _runtime.IsInitialized;

        public bool IsReady => _runtime.IsReady;

        public event EventHandler<WebComponentMessageEventArgs> MessageReceived
        {
            add { _runtime.MessageReceived += value; }
            remove { _runtime.MessageReceived -= value; }
        }

        public event EventHandler<WebComponentReadyStateChangedEventArgs> ReadyStateChanged
        {
            add { _runtime.ReadyStateChanged += value; }
            remove { _runtime.ReadyStateChanged -= value; }
        }

        public Task InitializeAsync() => _runtime.InitializeAsync();

        public Task LoadEntryPageAsync(string entryPageRelativePath) => _runtime.LoadEntryPageAsync(entryPageRelativePath);

        public Task NavigateAsync(Uri uri) => _runtime.NavigateAsync(uri);

        public Task LoadHtmlAsync(string html, string? baseUrl = null) => _runtime.LoadHtmlAsync(html, baseUrl);

        public Task PostMessageAsync(object message) => _runtime.PostMessageAsync(message);

        public Task PostRawMessageAsync(string rawMessage) => _runtime.PostRawMessageAsync(rawMessage);

        public Task<string> ExecuteScriptAsync(string script) => _runtime.ExecuteScriptAsync(script);

        public void FocusHost() => _runtime.FocusHost();

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            AttachedToVisualTree -= OnAttachedToVisualTree;
            _runtime.Dispose();
        }

        private void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
        {
            _bridge.NotifyLoaded();
            _ = _runtime.InitializeAsync();
        }

        private sealed class AvaloniaWebComponentPlatformBridge : IWebComponentPlatformBridge
        {
            private readonly Grid _host;
            private readonly TaskCompletionSource<bool> _loadedTcs =
                new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            private readonly JsBridge _jsBridge;

            private AvaloniaCefBrowser? _browser;
            private bool _disposed;
            private bool _jsBridgeAvailable;
            private bool _navigationFailed;

            public AvaloniaWebComponentPlatformBridge(Grid host)
            {
                _host = host ?? throw new ArgumentNullException(nameof(host));
                _jsBridge = new JsBridge(this);
            }

            public bool IsInitialized { get; private set; }

            public event EventHandler<string>? MessageReceived;

            public event EventHandler<WebComponentPlatformNavigationEventArgs>? NavigationCompleted;

            public void NotifyLoaded()
            {
                _loadedTcs.TrySetResult(true);
            }

            public async Task InitializeAsync(WebComponentHostOptions options)
            {
                if (IsInitialized)
                    return;

                await _loadedTcs.Task.ConfigureAwait(false);

                await RunOnUiAsync(() =>
                {
                    if (IsInitialized || _disposed)
                        return;

                    _browser = CreateBrowserOrThrow();
                    _browser.Focusable = true;
                    _browser.IsHitTestVisible = true;
                    _browser.PointerPressed += OnBrowserPointerPressed;
                    _browser.LoadStart += OnLoadStart;
                    _browser.LoadEnd += OnLoadEnd;
                    _browser.LoadError += OnLoadError;
                    _browser.TitleChanged += OnTitleChanged;
                    _browser.AddressChanged += OnAddressChanged;
                    _browser.ConsoleMessage += OnConsoleMessage;

                    TryRegisterJsBridge(_browser);

                    _host.Children.Clear();
                    _host.Children.Add(_browser);
                    IsInitialized = true;
                }).ConfigureAwait(false);
            }

            public Task LoadEntryPageAsync(string contentRootPath, string entryPageRelativePath, string virtualHostName)
            {
                string absoluteRoot = Path.GetFullPath(contentRootPath ?? string.Empty);
                string absoluteEntry = Path.Combine(absoluteRoot, NormalizeRelativePath(entryPageRelativePath));
                if (!File.Exists(absoluteEntry))
                    throw new FileNotFoundException("Web host entry page was not found.", absoluteEntry);

                return NavigateCoreAsync(new Uri(absoluteEntry, UriKind.Absolute));
            }

            public Task NavigateAsync(Uri uri)
            {
                if (uri == null) throw new ArgumentNullException(nameof(uri));
                return NavigateCoreAsync(uri);
            }

            public Task LoadHtmlAsync(string html, string? baseUrl)
            {
                string content = WebComponentHtmlContent.ApplyBaseUrl(html, baseUrl);
                string dataUri = WebComponentHtmlContent.ToDataUri(content);
                return NavigateCoreAsync(new Uri(dataUri, UriKind.Absolute));
            }

            public Task<string> ExecuteScriptAsync(string script)
            {
                return RunOnUiAsync(() =>
                {
                    EnsureBrowser();
                    var browser = _browser!;
                    try
                    {
                        browser.ExecuteJavaScript(script ?? string.Empty, "about:blank", 0);
                    }
                    catch
                    {
                        // Best-effort bridge.
                    }

                    return string.Empty;
                });
            }

            public void Focus()
            {
                if (_disposed)
                    return;

                _ = RunOnUiAsync(() =>
                {
                    _browser?.Focus(NavigationMethod.Tab, KeyModifiers.None);
                });
            }

            public void Dispose()
            {
                if (_disposed)
                    return;

                _disposed = true;
                _loadedTcs.TrySetResult(false);

                _ = RunOnUiAsync(() =>
                {
                    if (_browser != null)
                    {
                        _browser.PointerPressed -= OnBrowserPointerPressed;
                        _browser.LoadStart -= OnLoadStart;
                        _browser.LoadEnd -= OnLoadEnd;
                        _browser.LoadError -= OnLoadError;
                        _browser.TitleChanged -= OnTitleChanged;
                        _browser.AddressChanged -= OnAddressChanged;
                        _browser.ConsoleMessage -= OnConsoleMessage;
                        if (_browser is IDisposable disposable)
                            disposable.Dispose();
                    }

                    _host.Children.Clear();
                    _browser = null;
                    _jsBridgeAvailable = false;
                    _navigationFailed = false;
                    IsInitialized = false;
                });
            }

            private Task NavigateCoreAsync(Uri uri)
            {
                if (uri == null) throw new ArgumentNullException(nameof(uri));

                return RunOnUiAsync(() =>
                {
                    EnsureBrowser();
                    _navigationFailed = false;
                    _browser!.Address = uri.AbsoluteUri;
                });
            }

            private void OnBrowserPointerPressed(object? sender, PointerPressedEventArgs e)
            {
                _browser?.Focus();
            }

            private void OnLoadStart(object? sender, LoadStartEventArgs args)
            {
                _navigationFailed = false;
            }

            private void OnLoadEnd(object? sender, LoadEndEventArgs args)
            {
                if (!IsMainFrame(args?.Frame))
                    return;

                if (_navigationFailed)
                    return;

                NavigationCompleted?.Invoke(this, new WebComponentPlatformNavigationEventArgs(true, string.Empty));
            }

            private void OnLoadError(object? sender, LoadErrorEventArgs args)
            {
                if (!IsMainFrame(args?.Frame))
                    return;

                _navigationFailed = true;
                NavigationCompleted?.Invoke(
                    this,
                    new WebComponentPlatformNavigationEventArgs(false, args?.ErrorText ?? string.Empty));
            }

            private void OnTitleChanged(object? sender, string title)
            {
                if (_jsBridgeAvailable)
                    return;

                if (TryExtractPayload(title, out var payload))
                    MessageReceived?.Invoke(this, payload);
            }

            private void OnAddressChanged(object? sender, string address)
            {
                if (_jsBridgeAvailable)
                    return;

                if (TryExtractPayload(address, out var payload))
                    MessageReceived?.Invoke(this, payload);
            }

            private void OnConsoleMessage(object? sender, ConsoleMessageEventArgs e)
            {
                if (_jsBridgeAvailable)
                    return;

                if (TryExtractPayload(e?.Message, out var payload))
                    MessageReceived?.Invoke(this, payload);
            }

            private void TryRegisterJsBridge(AvaloniaCefBrowser browser)
            {
                try
                {
                    if (!browser.IsJavascriptObjectRegistered("external"))
                        browser.RegisterJavascriptObject(_jsBridge, "external", OnJavascriptMethodCall);

                    _jsBridgeAvailable = true;
                }
                catch
                {
                    _jsBridgeAvailable = false;
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

            private void DispatchFromJavaScript(string payload)
            {
                if (_disposed || string.IsNullOrWhiteSpace(payload))
                    return;

                MessageReceived?.Invoke(this, payload);
            }

            private void EnsureBrowser()
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(AvaloniaWebComponentPlatformBridge));

                if (!IsInitialized || _browser == null)
                    throw new InvalidOperationException("The Avalonia web host has not been initialized yet.");
            }

            private static bool IsMainFrame(CefFrame? frame)
            {
                try
                {
                    return frame == null || frame.IsMain;
                }
                catch
                {
                    return true;
                }
            }

            private static string NormalizeRelativePath(string entryPageRelativePath)
            {
                return (entryPageRelativePath ?? string.Empty)
                    .Replace('/', Path.DirectorySeparatorChar)
                    .TrimStart(Path.DirectorySeparatorChar);
            }

            private static bool TryExtractPayload(string? raw, out string payload)
            {
                payload = string.Empty;
                if (string.IsNullOrWhiteSpace(raw))
                    return false;

                string text = raw.Trim();
                if (text.Length >= 2)
                {
                    char first = text[0];
                    char last = text[text.Length - 1];
                    if ((first == '"' && last == '"') || (first == '\'' && last == '\''))
                        text = text.Substring(1, text.Length - 2);
                }

                const string prefix = "phiale-webhost:";
                int index = text.IndexOf(prefix, StringComparison.Ordinal);
                if (index < 0)
                    return false;

                string encoded = text.Substring(index + prefix.Length);
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

            private Task RunOnUiAsync(Action action)
            {
                if (Dispatcher.UIThread.CheckAccess())
                {
                    action();
                    return Task.CompletedTask;
                }

                var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                Dispatcher.UIThread.Post(() =>
                {
                    try
                    {
                        action();
                        tcs.SetResult(true);
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                }, DispatcherPriority.Normal);

                return tcs.Task;
            }

            private Task<T> RunOnUiAsync<T>(Func<T> action)
            {
                if (Dispatcher.UIThread.CheckAccess())
                    return Task.FromResult(action());

                var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
                Dispatcher.UIThread.Post(() =>
                {
                    try
                    {
                        tcs.SetResult(action());
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                }, DispatcherPriority.Normal);

                return tcs.Task;
            }

            private static AvaloniaCefBrowser CreateBrowserOrThrow()
            {
                Type type = typeof(AvaloniaCefBrowser);
                Exception? lastCtorException = null;

                ConstructorInfo[] constructors = type
                    .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .OrderBy(x => x.GetParameters().Length)
                    .ToArray();

                foreach (ConstructorInfo ctor in constructors)
                {
                    object?[]? args = TryBuildCtorArgs(ctor.GetParameters());
                    if (args == null)
                        continue;

                    try
                    {
                        return (AvaloniaCefBrowser)ctor.Invoke(args);
                    }
                    catch (Exception ex)
                    {
                        lastCtorException = ex is TargetInvocationException tie && tie.InnerException != null
                            ? tie.InnerException
                            : ex;
                    }
                }

                try
                {
                    object? instance = Activator.CreateInstance(type, nonPublic: true);
                    if (instance is AvaloniaCefBrowser browser)
                        return browser;
                }
                catch (Exception ex)
                {
                    if (lastCtorException == null)
                        lastCtorException = ex;
                }

                if (lastCtorException != null)
                    throw new InvalidOperationException("Failed to create AvaloniaCefBrowser.", lastCtorException);

                throw new MissingMethodException("AvaloniaCefBrowser has no supported constructor.");
            }

            private static object?[]? TryBuildCtorArgs(ParameterInfo[] parameters)
            {
                if (parameters.Length == 0)
                    return Array.Empty<object>();

                object?[] args = new object?[parameters.Length];

                for (int i = 0; i < parameters.Length; i++)
                {
                    ParameterInfo parameter = parameters[i];
                    Type parameterType = parameter.ParameterType;

                    if (parameterType == typeof(Func<CefApp>))
                    {
                        args[i] = (Func<CefApp>)(() => new DefaultCefApp());
                        continue;
                    }

                    if (parameterType == typeof(CefApp))
                    {
                        args[i] = new DefaultCefApp();
                        continue;
                    }

                    if (parameterType == typeof(Func<CefBrowserSettings>))
                    {
                        args[i] = (Func<CefBrowserSettings>)(() => new CefBrowserSettings());
                        continue;
                    }

                    if (parameterType == typeof(CefBrowserSettings))
                    {
                        args[i] = new CefBrowserSettings();
                        continue;
                    }

                    if (parameterType == typeof(Func<CefSettings>))
                    {
                        args[i] = (Func<CefSettings>)(() => new CefSettings());
                        continue;
                    }

                    if (parameterType == typeof(CefSettings))
                    {
                        args[i] = new CefSettings();
                        continue;
                    }

                    if (parameterType == typeof(Func<Func<CefRequestContext>>))
                    {
                        args[i] = (Func<Func<CefRequestContext>>)(() => () => CefRequestContext.GetGlobalContext());
                        continue;
                    }

                    if (parameterType == typeof(Func<CefRequestContext>))
                    {
                        args[i] = (Func<CefRequestContext>)(() => CefRequestContext.GetGlobalContext());
                        continue;
                    }

                    if (parameter.HasDefaultValue)
                    {
                        args[i] = parameter.DefaultValue;
                        continue;
                    }

                    if (!parameterType.IsValueType)
                    {
                        args[i] = null;
                        continue;
                    }

                    args[i] = Activator.CreateInstance(parameterType);
                }

                return args;
            }

            private sealed class JsBridge
            {
                private readonly WeakReference<AvaloniaWebComponentPlatformBridge> _owner;

                public JsBridge(AvaloniaWebComponentPlatformBridge owner)
                {
                    _owner = new WeakReference<AvaloniaWebComponentPlatformBridge>(owner);
                }

                public void notify(string payload)
                {
                    Dispatch(payload);
                }

                public void postMessage(string payload)
                {
                    Dispatch(payload);
                }

                private void Dispatch(string payload)
                {
                    if (!_owner.TryGetTarget(out var owner))
                        return;

                    Dispatcher.UIThread.Post(() =>
                    {
                        owner.DispatchFromJavaScript(payload);
                    }, DispatcherPriority.Background);
                }
            }

            private sealed class DefaultCefApp : CefApp
            {
            }
        }
    }
}
