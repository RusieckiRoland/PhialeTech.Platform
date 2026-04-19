using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using PhialeGis.Library.Abstractions.Ui.Web;
using PhialeGis.Library.Core.Web;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace PhialeGis.Library.WpfUi.Controls
{
    /// <summary>
    /// Reusable WPF host for browser-based UI components.
    /// </summary>
    public sealed class PhialeWebComponentHost : UserControl, IWebComponentHost
    {
        private readonly Grid _root;
        private readonly WpfWebComponentPlatformBridge _bridge;
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

            _bridge = new WpfWebComponentPlatformBridge(_root, Dispatcher);
            _bridge.WarmUp();
            _runtime = new WebComponentHostRuntime(_bridge, options ?? new WebComponentHostOptions());

            Loaded += OnLoaded;
        }

        public static void WarmUpBrowserRuntime()
        {
            WpfWebComponentPlatformBridge.WarmUpSharedEnvironment();
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

        public Task LoadHtmlAsync(string html, string baseUrl = null) => _runtime.LoadHtmlAsync(html, baseUrl);

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

        private sealed class WpfWebComponentPlatformBridge : IWebComponentPlatformBridge
        {
            private static readonly object _environmentGate = new object();
            private readonly Grid _host;
            private readonly Dispatcher _dispatcher;
            private readonly TaskCompletionSource<bool> _loadedTcs =
                new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            private static Task<CoreWebView2Environment> _sharedEnvironmentTask;
            private WebView2 _webView;
            private bool _disposed;

            public WpfWebComponentPlatformBridge(Grid host, Dispatcher dispatcher)
            {
                _host = host ?? throw new ArgumentNullException(nameof(host));
                _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            }

            public bool IsInitialized { get; private set; }

            public event EventHandler<string> MessageReceived;

            public event EventHandler<WebComponentPlatformNavigationEventArgs> NavigationCompleted;

            public void WarmUp()
            {
                _ = GetSharedEnvironmentAsync();
            }

            public static void WarmUpSharedEnvironment()
            {
                _ = GetSharedEnvironmentAsync();
            }

            public void NotifyLoaded()
            {
                _loadedTcs.TrySetResult(true);
            }

            public async Task InitializeAsync(WebComponentHostOptions options)
            {
                if (IsInitialized)
                    return;

                await _loadedTcs.Task.ConfigureAwait(false);
                var environment = await GetSharedEnvironmentAsync().ConfigureAwait(false);

                await RunOnUiAsync(async () =>
                {
                    if (IsInitialized || _disposed)
                        return;

                    _webView = new WebView2();
                    _host.Children.Clear();
                    _host.Children.Add(_webView);

                    await _webView.EnsureCoreWebView2Async(environment).ConfigureAwait(true);
                    _webView.CoreWebView2.Settings.IsScriptEnabled = true;
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
                    _webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                        hostName,
                        absoluteRoot,
                        CoreWebView2HostResourceAccessKind.Allow);
                    _webView.Source = new Uri($"https://{hostName}/{entry}");
                }).ConfigureAwait(false);
            }

            public Task NavigateAsync(Uri uri)
            {
                if (uri == null) throw new ArgumentNullException(nameof(uri));

                return RunOnUiAsync(() =>
                {
                    EnsureWebView();
                    _webView.Source = uri;
                });
            }

            public Task LoadHtmlAsync(string html, string baseUrl)
            {
                string content = WebComponentHtmlContent.ApplyBaseUrl(html, baseUrl);
                return RunOnUiAsync(() =>
                {
                    EnsureWebView();
                    _webView.NavigateToString(content);
                });
            }

            public Task<string> ExecuteScriptAsync(string script)
            {
                return RunOnUiAsync(async () =>
                {
                    EnsureWebView();
                    return await _webView.CoreWebView2.ExecuteScriptAsync(script ?? string.Empty).ConfigureAwait(true);
                });
            }

            public void Focus()
            {
                if (_disposed)
                    return;

                _ = RunOnUiAsync(() =>
                {
                    _webView?.Focus();
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
                        if (_webView is IDisposable disposable)
                            disposable.Dispose();
                    }

                    _host.Children.Clear();
                    _webView = null;
                    IsInitialized = false;
                });
            }

            private void OnWebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
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

            private void OnNavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
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
                    throw new ObjectDisposedException(nameof(WpfWebComponentPlatformBridge));

                if (!IsInitialized || _webView == null || _webView.CoreWebView2 == null)
                    throw new InvalidOperationException("The WPF web host has not been initialized yet.");
            }

            private static Task<CoreWebView2Environment> GetSharedEnvironmentAsync()
            {
                lock (_environmentGate)
                {
                    if (_sharedEnvironmentTask == null)
                        _sharedEnvironmentTask = CoreWebView2Environment.CreateAsync();

                    return _sharedEnvironmentTask;
                }
            }

            private Task RunOnUiAsync(Action action)
            {
                if (_dispatcher.CheckAccess())
                {
                    action();
                    return Task.CompletedTask;
                }

                var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                _dispatcher.BeginInvoke(new Action(() =>
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
                }), DispatcherPriority.Normal);

                return tcs.Task;
            }

            private Task RunOnUiAsync(Func<Task> action)
            {
                if (_dispatcher.CheckAccess())
                    return action();

                var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                _dispatcher.BeginInvoke(new Action(async () =>
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
                }), DispatcherPriority.Normal);

                return tcs.Task;
            }

            private Task<T> RunOnUiAsync<T>(Func<Task<T>> action)
            {
                if (_dispatcher.CheckAccess())
                    return action();

                var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
                _dispatcher.BeginInvoke(new Action(async () =>
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
                }), DispatcherPriority.Normal);

                return tcs.Task;
            }
        }
    }
}
