using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using PhialeTech.WebHost;
using PhialeTech.WebHost.Abstractions.Ui.Web;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace PhialeTech.WebHost.Wpf.ComparisonLab
{
    public sealed class StandardWebComponentHostFactory : IWebComponentHostFactory
    {
        public IWebComponentHost CreateHost(WebComponentHostOptions options)
        {
            return new StandardWebComponentHost(options ?? new WebComponentHostOptions());
        }
    }

    public sealed class CompositionWebComponentHostFactory : IWebComponentHostFactory
    {
        public IWebComponentHost CreateHost(WebComponentHostOptions options)
        {
            return new CompositionWebComponentHost(options ?? new WebComponentHostOptions());
        }
    }

    public sealed class StandardWebComponentHost : ComparisonWebComponentHostBase
    {
        public StandardWebComponentHost(WebComponentHostOptions options)
            : base(options)
        {
        }

        protected override IComparisonWpfPlatformBridge CreateBridge(Grid host, Dispatcher dispatcher)
        {
            return new StandardWpfPlatformBridge(host, dispatcher);
        }
    }

    public sealed class CompositionWebComponentHost : ComparisonWebComponentHostBase
    {
        public CompositionWebComponentHost(WebComponentHostOptions options)
            : base(options)
        {
        }

        protected override IComparisonWpfPlatformBridge CreateBridge(Grid host, Dispatcher dispatcher)
        {
            return new CompositionWpfPlatformBridge(host, dispatcher);
        }
    }

    public abstract class ComparisonWebComponentHostBase : UserControl, IWebComponentHost
    {
        private readonly Grid _root;
        private readonly IComparisonWpfPlatformBridge _bridge;
        private readonly WebComponentHostRuntime _runtime;
        private bool _disposed;

        protected ComparisonWebComponentHostBase(WebComponentHostOptions options)
        {
            _root = new Grid();
            Content = _root;
            Focusable = false;

            _bridge = CreateBridge(_root, Dispatcher);
            _bridge.WarmUp();
            _runtime = new WebComponentHostRuntime(_bridge, options ?? new WebComponentHostOptions());

            Loaded += OnLoaded;
        }

        protected abstract IComparisonWpfPlatformBridge CreateBridge(Grid host, Dispatcher dispatcher);

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
    }

    public interface IComparisonWpfPlatformBridge : IWebComponentPlatformBridge
    {
        void WarmUp();

        void NotifyLoaded();
    }

    internal abstract class WpfPlatformBridgeBase<TWebView> : IComparisonWpfPlatformBridge
        where TWebView : FrameworkElement
    {
        private static readonly object EnvironmentGate = new object();
        private static Task<CoreWebView2Environment> _sharedEnvironmentTask;
        private readonly Grid _host;
        private readonly Dispatcher _dispatcher;
        private readonly TaskCompletionSource<bool> _loadedTcs =
            new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        private TWebView _webView;
        private bool _disposed;

        protected WpfPlatformBridgeBase(Grid host, Dispatcher dispatcher)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        }

        public bool IsInitialized { get; private set; }

        public event EventHandler<string> MessageReceived;

        public event EventHandler<WebComponentPlatformNavigationEventArgs> NavigationCompleted;

        public void WarmUp()
        {
            ComparisonLabTrace.Write("bridge", GetTraceName(), "warmup requested");
            _ = RunOnUiAsync(() => GetSharedEnvironmentAsync());
        }

        public void NotifyLoaded()
        {
            ComparisonLabTrace.Write("bridge", GetTraceName(), "host loaded");
            _loadedTcs.TrySetResult(true);
        }

        public async Task InitializeAsync(WebComponentHostOptions options)
        {
            if (IsInitialized)
            {
                ComparisonLabTrace.Write("bridge", GetTraceName(), "initialize skipped; already initialized");
                return;
            }

            ComparisonLabTrace.Write("bridge", GetTraceName(), "initialize start");
            await _loadedTcs.Task.ConfigureAwait(false);
            var environment = await RunOnUiAsync(() => GetSharedEnvironmentAsync()).ConfigureAwait(false);

            await RunOnUiAsync(async () =>
            {
                if (IsInitialized || _disposed)
                    return;

                _webView = CreateWebView();
                ComparisonLabTrace.Write("bridge", GetTraceName(), "webview created; type=" + _webView.GetType().Name);
                _host.Children.Clear();
                _host.Children.Add(_webView);

                await EnsureCoreWebView2Async(_webView, environment).ConfigureAwait(true);
                ComparisonLabTrace.Write("bridge", GetTraceName(), "EnsureCoreWebView2Async completed");
                var settings = GetSettings(_webView);
                settings.IsScriptEnabled = true;
                DisableBrowserZoom(settings);
                SubscribeToEvents(_webView);

                IsInitialized = true;
                ComparisonLabTrace.Write("bridge", GetTraceName(), "initialize finished");
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
            ComparisonLabTrace.Write("nav", GetTraceName(), "load entry page; host=" + hostName + "; entry=" + entry);

            await RunOnUiAsync(() =>
            {
                EnsureWebView();
                GetCore(_webView).SetVirtualHostNameToFolderMapping(hostName, absoluteRoot, CoreWebView2HostResourceAccessKind.Allow);
                SetSource(_webView, new Uri("https://" + hostName + "/" + entry));
            }).ConfigureAwait(false);
        }

        public Task NavigateAsync(Uri uri)
        {
            if (uri == null)
                throw new ArgumentNullException(nameof(uri));

            ComparisonLabTrace.Write("nav", GetTraceName(), "navigate; uri=" + uri);

            return RunOnUiAsync(() =>
            {
                EnsureWebView();
                SetSource(_webView, uri);
            });
        }

        public Task LoadHtmlAsync(string html, string baseUrl)
        {
            string content = WebComponentHtmlContent.ApplyBaseUrl(html, baseUrl);
            ComparisonLabTrace.Write("nav", GetTraceName(), "load html; baseUrl=" + baseUrl + "; length=" + (content ?? string.Empty).Length);
            return RunOnUiAsync(() =>
            {
                EnsureWebView();
                NavigateToString(_webView, content);
            });
        }

        public Task<string> ExecuteScriptAsync(string script)
        {
            ComparisonLabTrace.Write("script", GetTraceName(), "execute script; snippet=" + ComparisonLabTrace.SafeSnippet(script));
            return RunOnUiAsync(async () =>
            {
                EnsureWebView();
                return await GetCore(_webView).ExecuteScriptAsync(script ?? string.Empty).ConfigureAwait(true);
            });
        }

        public void Focus()
        {
            if (_disposed)
                return;

            _ = RunOnUiAsync(() =>
            {
                ComparisonLabTrace.Write("focus", GetTraceName(), "focus requested");
                _webView.Focus();
                ComparisonLabTrace.Write(
                    "focus",
                    GetTraceName(),
                    "focus applied; IsKeyboardFocused=" + _webView.IsKeyboardFocused + "; IsKeyboardFocusWithin=" + _webView.IsKeyboardFocusWithin);
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
                    ComparisonLabTrace.Write("bridge", GetTraceName(), "disposing webview");
                    UnsubscribeFromEvents(_webView);
                    var disposable = _webView as IDisposable;
                    if (disposable != null)
                        disposable.Dispose();
                }

                _host.Children.Clear();
                _webView = null;
                IsInitialized = false;
            });
        }

        protected abstract TWebView CreateWebView();

        protected abstract Task EnsureCoreWebView2Async(TWebView webView, CoreWebView2Environment environment);

        protected abstract CoreWebView2 GetCore(TWebView webView);

        protected abstract CoreWebView2Settings GetSettings(TWebView webView);

        protected abstract void SetSource(TWebView webView, Uri uri);

        protected abstract void NavigateToString(TWebView webView, string html);

        protected abstract void SubscribeToEvents(TWebView webView);

        protected abstract void UnsubscribeFromEvents(TWebView webView);

        protected virtual string GetTraceName()
        {
            return GetType().Name;
        }

        protected void RaiseMessageReceived(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
                return;

            ComparisonLabTrace.Write("message", GetTraceName(), "received " + ComparisonLabTrace.SafeSnippet(payload));
            MessageReceived?.Invoke(this, payload);
        }

        protected void RaiseNavigationCompleted(bool isSuccess, string error)
        {
            ComparisonLabTrace.Write("nav", GetTraceName(), "navigation completed; success=" + isSuccess + "; error=" + error);
            NavigationCompleted?.Invoke(this, new WebComponentPlatformNavigationEventArgs(isSuccess, error));
        }

        private void EnsureWebView()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);

            if (!IsInitialized || _webView == null || GetCore(_webView) == null)
                throw new InvalidOperationException("The comparison web host has not been initialized yet.");
        }

        private static Task<CoreWebView2Environment> GetSharedEnvironmentAsync()
        {
            lock (EnvironmentGate)
            {
                if (_sharedEnvironmentTask == null)
                    _sharedEnvironmentTask = CoreWebView2Environment.CreateAsync();

                return _sharedEnvironmentTask;
            }
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

        private static void DisableBrowserZoom(CoreWebView2Settings settings)
        {
            if (settings == null)
                return;

            settings.IsZoomControlEnabled = false;

            PropertyInfo pinchZoomProperty = settings.GetType().GetProperty("IsPinchZoomEnabled");
            if (pinchZoomProperty != null && pinchZoomProperty.CanWrite && pinchZoomProperty.PropertyType == typeof(bool))
            {
                pinchZoomProperty.SetValue(settings, false);
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

    internal sealed class StandardWpfPlatformBridge : WpfPlatformBridgeBase<WebView2>
    {
        public StandardWpfPlatformBridge(Grid host, Dispatcher dispatcher)
            : base(host, dispatcher)
        {
        }

        protected override WebView2 CreateWebView() => new WebView2();

        protected override Task EnsureCoreWebView2Async(WebView2 webView, CoreWebView2Environment environment) => webView.EnsureCoreWebView2Async(environment);

        protected override CoreWebView2 GetCore(WebView2 webView) => webView.CoreWebView2;

        protected override CoreWebView2Settings GetSettings(WebView2 webView) => webView.CoreWebView2.Settings;

        protected override void SetSource(WebView2 webView, Uri uri) => webView.Source = uri;

        protected override void NavigateToString(WebView2 webView, string html) => webView.NavigateToString(html);

        protected override void SubscribeToEvents(WebView2 webView)
        {
            webView.WebMessageReceived += HandleWebMessageReceived;
            webView.NavigationCompleted += HandleNavigationCompleted;
            webView.NavigationStarting += HandleNavigationStarting;
            webView.ContentLoading += HandleContentLoading;
            webView.SourceChanged += HandleSourceChanged;
        }

        protected override void UnsubscribeFromEvents(WebView2 webView)
        {
            webView.WebMessageReceived -= HandleWebMessageReceived;
            webView.NavigationCompleted -= HandleNavigationCompleted;
            webView.NavigationStarting -= HandleNavigationStarting;
            webView.ContentLoading -= HandleContentLoading;
            webView.SourceChanged -= HandleSourceChanged;
        }

        private void HandleWebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                RaiseMessageReceived(e.TryGetWebMessageAsString());
            }
            catch
            {
            }
        }

        private void HandleNavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            RaiseNavigationCompleted(e.IsSuccess, e.IsSuccess ? string.Empty : e.WebErrorStatus.ToString());
        }

        private void HandleNavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            ComparisonLabTrace.Write("nav", GetTraceName(), "navigation starting; uri=" + e.Uri);
        }

        private void HandleContentLoading(object sender, CoreWebView2ContentLoadingEventArgs e)
        {
            ComparisonLabTrace.Write("nav", GetTraceName(), "content loading; isErrorPage=" + e.IsErrorPage + "; navId=" + e.NavigationId);
        }

        private void HandleSourceChanged(object sender, CoreWebView2SourceChangedEventArgs e)
        {
            ComparisonLabTrace.Write("nav", GetTraceName(), "source changed; newDocument=" + e.IsNewDocument);
        }
    }

    internal sealed class CompositionWpfPlatformBridge : WpfPlatformBridgeBase<WebView2CompositionControl>
    {
        public CompositionWpfPlatformBridge(Grid host, Dispatcher dispatcher)
            : base(host, dispatcher)
        {
        }

        protected override WebView2CompositionControl CreateWebView()
        {
            return new WebView2CompositionControl
            {
                UseLayoutRounding = true,
                SnapsToDevicePixels = true,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
        }

        protected override Task EnsureCoreWebView2Async(WebView2CompositionControl webView, CoreWebView2Environment environment) => webView.EnsureCoreWebView2Async(environment);

        protected override CoreWebView2 GetCore(WebView2CompositionControl webView) => webView.CoreWebView2;

        protected override CoreWebView2Settings GetSettings(WebView2CompositionControl webView) => webView.CoreWebView2.Settings;

        protected override void SetSource(WebView2CompositionControl webView, Uri uri) => webView.Source = uri;

        protected override void NavigateToString(WebView2CompositionControl webView, string html) => webView.NavigateToString(html);

        protected override void SubscribeToEvents(WebView2CompositionControl webView)
        {
            webView.WebMessageReceived += HandleWebMessageReceived;
            webView.NavigationCompleted += HandleNavigationCompleted;
            webView.NavigationStarting += HandleNavigationStarting;
            webView.ContentLoading += HandleContentLoading;
            webView.SourceChanged += HandleSourceChanged;
        }

        protected override void UnsubscribeFromEvents(WebView2CompositionControl webView)
        {
            webView.WebMessageReceived -= HandleWebMessageReceived;
            webView.NavigationCompleted -= HandleNavigationCompleted;
            webView.NavigationStarting -= HandleNavigationStarting;
            webView.ContentLoading -= HandleContentLoading;
            webView.SourceChanged -= HandleSourceChanged;
        }

        private void HandleWebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                RaiseMessageReceived(e.TryGetWebMessageAsString());
            }
            catch
            {
            }
        }

        private void HandleNavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            RaiseNavigationCompleted(e.IsSuccess, e.IsSuccess ? string.Empty : e.WebErrorStatus.ToString());
        }

        private void HandleNavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            ComparisonLabTrace.Write("nav", GetTraceName(), "navigation starting; uri=" + e.Uri);
        }

        private void HandleContentLoading(object sender, CoreWebView2ContentLoadingEventArgs e)
        {
            ComparisonLabTrace.Write("nav", GetTraceName(), "content loading; isErrorPage=" + e.IsErrorPage + "; navId=" + e.NavigationId);
        }

        private void HandleSourceChanged(object sender, CoreWebView2SourceChangedEventArgs e)
        {
            ComparisonLabTrace.Write("nav", GetTraceName(), "source changed; newDocument=" + e.IsNewDocument);
        }
    }
}
