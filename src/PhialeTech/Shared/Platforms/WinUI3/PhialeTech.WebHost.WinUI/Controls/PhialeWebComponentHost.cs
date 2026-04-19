using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using PhialeTech.WebHost.Abstractions.Ui.Web;
using PhialeTech.WebHost;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace PhialeTech.WebHost.WinUI.Controls
{
    /// <summary>
    /// Reusable WinUI 3 host for browser-based UI components.
    /// </summary>
    public sealed class PhialeWebComponentHost : UserControl, IWebComponentHost
    {
        private readonly Grid _root;
        private readonly WinUiWebComponentPlatformBridge _bridge;
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
            IsTabStop = true;

            _bridge = new WinUiWebComponentPlatformBridge(_root, DispatcherQueue);
            _runtime = new WebComponentHostRuntime(_bridge, options ?? new WebComponentHostOptions());

            Loaded += OnLoaded;
        }

        public WebComponentHostOptions Options => _runtime.Options;

        public bool IsInitialized => _runtime.IsInitialized;

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
            Loaded -= OnLoaded;
            _runtime.Dispose();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _bridge.NotifyLoaded();
            _ = _runtime.InitializeAsync();
        }

        private sealed class WinUiWebComponentPlatformBridge : IWebComponentPlatformBridge
        {
            private readonly Grid _host;
            private readonly DispatcherQueue _dispatcherQueue;
            private readonly TaskCompletionSource<bool> _loadedTcs =
                new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            private WebView2? _webView;
            private bool _disposed;

            public WinUiWebComponentPlatformBridge(Grid host, DispatcherQueue dispatcherQueue)
            {
                _host = host ?? throw new ArgumentNullException(nameof(host));
                _dispatcherQueue = dispatcherQueue ?? throw new ArgumentNullException(nameof(dispatcherQueue));
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

                await RunOnUiAsync(async () =>
                {
                    if (IsInitialized || _disposed)
                        return;

                    _webView = new WebView2();
                    _host.Children.Clear();
                    _host.Children.Add(_webView);

                    await _webView.EnsureCoreWebView2Async();
                    _webView.CoreWebView2.Settings.IsScriptEnabled = true;
                    DisableBrowserZoom(_webView.CoreWebView2.Settings);
                    _webView.WebMessageReceived += OnWebMessageReceived;
                    _webView.NavigationCompleted += OnNavigationCompleted;

                    IsInitialized = true;
                }).ConfigureAwait(false);
            }

            public async Task LoadEntryPageAsync(string contentRootPath, string entryPageRelativePath, string virtualHostName)
            {
                string absoluteRoot = Path.GetFullPath(contentRootPath ?? string.Empty);
                string absoluteEntry = Path.Combine(absoluteRoot, NormalizeRelativePath(entryPageRelativePath));
                if (!File.Exists(absoluteEntry))
                    throw new FileNotFoundException("Web host entry page was not found.", absoluteEntry);

                string entry = NormalizeRelativeUri(entryPageRelativePath);
                string hostName = string.IsNullOrWhiteSpace(virtualHostName) ? "phiale.webhost" : virtualHostName;

                await RunOnUiAsync(() =>
                {
                    EnsureWebView();
                    var webView = _webView!;
                    var core = webView.CoreWebView2!;
                    core.SetVirtualHostNameToFolderMapping(
                        hostName,
                        absoluteRoot,
                        CoreWebView2HostResourceAccessKind.Allow);
                    webView.Source = new Uri($"https://{hostName}/{entry}");
                }).ConfigureAwait(false);
            }

            public Task NavigateAsync(Uri uri)
            {
                if (uri == null) throw new ArgumentNullException(nameof(uri));

                return RunOnUiAsync(() =>
                {
                    EnsureWebView();
                    _webView!.Source = uri;
                });
            }

            public Task LoadHtmlAsync(string html, string? baseUrl)
            {
                string content = WebComponentHtmlContent.ApplyBaseUrl(html, baseUrl);
                return RunOnUiAsync(() =>
                {
                    EnsureWebView();
                    _webView!.NavigateToString(content);
                });
            }

            public Task<string> ExecuteScriptAsync(string script)
            {
                return RunOnUiAsync(async () =>
                {
                    EnsureWebView();
                    var webView = _webView!;
                    var core = webView.CoreWebView2!;
                    return await core.ExecuteScriptAsync(script ?? string.Empty);
                });
            }

            public void Focus()
            {
                if (_disposed)
                    return;

                _ = RunOnUiAsync(() =>
                {
                    _webView?.Focus(FocusState.Programmatic);
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
                    if (_webView != null)
                    {
                        _webView.WebMessageReceived -= OnWebMessageReceived;
                        _webView.NavigationCompleted -= OnNavigationCompleted;
                    }

                    _host.Children.Clear();
                    _webView = null;
                    IsInitialized = false;
                });
            }

            private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
            {
                try
                {
                    string payload = e.TryGetWebMessageAsString();
                    if (string.IsNullOrWhiteSpace(payload))
                        return;

                    MessageReceived?.Invoke(this, payload);
                }
                catch
                {
                    // Best-effort bridge
                }
            }

            private void OnNavigationCompleted(WebView2 sender, CoreWebView2NavigationCompletedEventArgs e)
            {
                NavigationCompleted?.Invoke(
                    this,
                    new WebComponentPlatformNavigationEventArgs(
                        e.IsSuccess,
                        e.IsSuccess ? string.Empty : e.WebErrorStatus.ToString()));
            }

            private static string NormalizeRelativePath(string entryPageRelativePath)
            {
                return (entryPageRelativePath ?? string.Empty)
                    .Replace('/', Path.DirectorySeparatorChar)
                    .TrimStart(Path.DirectorySeparatorChar);
            }

            private static string NormalizeRelativeUri(string entryPageRelativePath)
            {
                return (entryPageRelativePath ?? string.Empty)
                    .Replace('\\', '/')
                    .TrimStart('/');
            }

            private void EnsureWebView()
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(WinUiWebComponentPlatformBridge));

                if (!IsInitialized || _webView == null || _webView.CoreWebView2 == null)
                    throw new InvalidOperationException("The WinUI web host has not been initialized yet.");
            }

            private static void DisableBrowserZoom(CoreWebView2Settings settings)
            {
                if (settings == null)
                    return;

                settings.IsZoomControlEnabled = false;

                PropertyInfo? pinchZoomProperty = settings.GetType().GetProperty("IsPinchZoomEnabled");
                if (pinchZoomProperty != null && pinchZoomProperty.CanWrite && pinchZoomProperty.PropertyType == typeof(bool))
                {
                    pinchZoomProperty.SetValue(settings, false);
                }
            }

            private Task RunOnUiAsync(Action action)
            {
                if (_dispatcherQueue.HasThreadAccess)
                {
                    action();
                    return Task.CompletedTask;
                }

                var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                _dispatcherQueue.TryEnqueue(() =>
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
                });

                return tcs.Task;
            }

            private Task RunOnUiAsync(Func<Task> action)
            {
                if (_dispatcherQueue.HasThreadAccess)
                    return action();

                var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                _dispatcherQueue.TryEnqueue(async () =>
                {
                    try
                    {
                        await action().ConfigureAwait(true);
                        tcs.SetResult(true);
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                });

                return tcs.Task;
            }

            private Task<T> RunOnUiAsync<T>(Func<Task<T>> action)
            {
                if (_dispatcherQueue.HasThreadAccess)
                    return action();

                var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
                _dispatcherQueue.TryEnqueue(async () =>
                {
                    try
                    {
                        T result = await action().ConfigureAwait(true);
                        tcs.SetResult(result);
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                });

                return tcs.Task;
            }
        }
    }
}
