using PhialeTech.WebHost.Abstractions.Ui.Web;
using System;
using System.Collections.Generic;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PhialeTech.WebHost
{
    /// <summary>
    /// Shared runtime that centralizes browser-host behavior
    /// independently from the native browser engine.
    /// </summary>
    public sealed class WebComponentHostRuntime : IWebComponentHost
    {
        private static readonly JsonSerializerOptions _json =
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        private readonly IWebComponentPlatformBridge _platformBridge;
        private readonly SynchronizationContext _synchronizationContext;
        private readonly object _gate = new object();
        private readonly Queue<string> _pendingMessages = new Queue<string>();

        private Task _initializeTask;
        private bool _disposed;

        public WebComponentHostRuntime(
            IWebComponentPlatformBridge platformBridge,
            WebComponentHostOptions options)
        {
            _platformBridge = platformBridge ?? throw new ArgumentNullException(nameof(platformBridge));
            _synchronizationContext = SynchronizationContext.Current;
            Options = (options ?? new WebComponentHostOptions()).Clone();

            _platformBridge.MessageReceived += OnPlatformMessageReceived;
            _platformBridge.NavigationCompleted += OnPlatformNavigationCompleted;
        }

        public WebComponentHostOptions Options { get; }

        public bool IsInitialized { get; private set; }

        public bool IsReady { get; private set; }

        public event EventHandler<WebComponentMessageEventArgs> MessageReceived;

        public event EventHandler<WebComponentReadyStateChangedEventArgs> ReadyStateChanged;

        public Task InitializeAsync()
        {
            ThrowIfDisposed();

            lock (_gate)
            {
                if (_initializeTask == null)
                    _initializeTask = InitializeCoreAsync();

                return _initializeTask;
            }
        }

        public async Task LoadEntryPageAsync(string entryPageRelativePath)
        {
            ThrowIfDisposed();
            await InitializeAsync().ConfigureAwait(false);

            var root = Options.LocalContentRootPath;
            if (string.IsNullOrWhiteSpace(root))
                throw new InvalidOperationException("LocalContentRootPath must be configured before loading a local entry page.");

            SetReadyState(false);
            await _platformBridge
                .LoadEntryPageAsync(root, entryPageRelativePath ?? string.Empty, Options.VirtualHostName ?? "phiale.webhost")
                .ConfigureAwait(false);
        }

        public async Task NavigateAsync(Uri uri)
        {
            ThrowIfDisposed();
            if (uri == null) throw new ArgumentNullException(nameof(uri));

            await InitializeAsync().ConfigureAwait(false);
            SetReadyState(false);
            await _platformBridge.NavigateAsync(uri).ConfigureAwait(false);
        }

        public async Task LoadHtmlAsync(string html, string baseUrl = null)
        {
            ThrowIfDisposed();
            await InitializeAsync().ConfigureAwait(false);

            SetReadyState(false);
            await _platformBridge.LoadHtmlAsync(html ?? string.Empty, baseUrl).ConfigureAwait(false);
        }

        public Task PostMessageAsync(object message)
        {
            ThrowIfDisposed();
            return PostRawMessageAsync(JsonSerializer.Serialize(message, _json));
        }

        public async Task PostRawMessageAsync(string rawMessage)
        {
            ThrowIfDisposed();
            await InitializeAsync().ConfigureAwait(false);

            string payload = NormalizeRawMessage(rawMessage);
            bool enqueue;

            lock (_gate)
            {
                enqueue = Options.QueueMessagesUntilReady && !IsReady;
                if (enqueue)
                    _pendingMessages.Enqueue(payload);
            }

            if (!enqueue)
                await SendRawMessageCoreAsync(payload).ConfigureAwait(false);
        }

        public async Task<string> ExecuteScriptAsync(string script)
        {
            ThrowIfDisposed();
            await InitializeAsync().ConfigureAwait(false);
            return await _platformBridge.ExecuteScriptAsync(script ?? string.Empty).ConfigureAwait(false);
        }

        public void FocusHost()
        {
            if (_disposed)
                return;

            _platformBridge.Focus();
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            _platformBridge.MessageReceived -= OnPlatformMessageReceived;
            _platformBridge.NavigationCompleted -= OnPlatformNavigationCompleted;
            _platformBridge.Dispose();

            lock (_gate)
            {
                _pendingMessages.Clear();
            }
        }

        private async Task InitializeCoreAsync()
        {
            await _platformBridge.InitializeAsync(Options).ConfigureAwait(false);

            if (!_platformBridge.IsInitialized)
                return;

            SetInitializedState(true);
        }

        private async void OnPlatformNavigationCompleted(object sender, WebComponentPlatformNavigationEventArgs e)
        {
            if (_disposed)
                return;

            if (!e.IsSuccess)
            {
                SetReadyState(false);
                return;
            }

            try
            {
                await InjectBridgeScriptAsync().ConfigureAwait(false);

                if (string.IsNullOrWhiteSpace(Options.JavaScriptReadyMessageType))
                    await MarkReadyAsync().ConfigureAwait(false);
            }
            catch
            {
                SetReadyState(false);
            }
        }

        private async void OnPlatformMessageReceived(object sender, string rawMessage)
        {
            if (_disposed)
                return;

            string normalized = rawMessage ?? string.Empty;
            string messageType = TryExtractMessageType(normalized);

            if (!IsReady &&
                !string.IsNullOrWhiteSpace(Options.JavaScriptReadyMessageType) &&
                string.Equals(messageType, Options.JavaScriptReadyMessageType, StringComparison.Ordinal))
            {
                await MarkReadyAsync().ConfigureAwait(false);
            }

            RaiseMessageReceived(new WebComponentMessageEventArgs(normalized, messageType));
        }

        private async Task MarkReadyAsync()
        {
            bool shouldFlush = false;

            lock (_gate)
            {
                if (_disposed || IsReady)
                    return;

                IsReady = true;
                shouldFlush = true;
            }

            RaiseReadyStateChanged();

            if (shouldFlush)
                await FlushPendingMessagesAsync().ConfigureAwait(false);
        }

        private void SetInitializedState(bool isInitialized)
        {
            bool changed;

            lock (_gate)
            {
                changed = IsInitialized != isInitialized;
                IsInitialized = isInitialized;
            }

            if (changed)
                RaiseReadyStateChanged();
        }

        private void SetReadyState(bool isReady)
        {
            bool changed;

            lock (_gate)
            {
                changed = IsReady != isReady;
                IsReady = isReady;
            }

            if (changed)
                RaiseReadyStateChanged();
        }

        private void RaiseReadyStateChanged()
        {
            var args = new WebComponentReadyStateChangedEventArgs(IsInitialized, IsReady);
            DispatchOnCapturedContext(() => ReadyStateChanged?.Invoke(this, args));
        }

        private void RaiseMessageReceived(WebComponentMessageEventArgs args)
        {
            DispatchOnCapturedContext(() => MessageReceived?.Invoke(this, args));
        }

        private async Task FlushPendingMessagesAsync()
        {
            while (true)
            {
                string pending = null;

                lock (_gate)
                {
                    if (_pendingMessages.Count == 0)
                        return;

                    pending = _pendingMessages.Dequeue();
                }

                await SendRawMessageCoreAsync(pending).ConfigureAwait(false);
            }
        }

        private Task SendRawMessageCoreAsync(string rawMessage)
        {
            string script = BuildHostReceiveScript(rawMessage);
            return _platformBridge.ExecuteScriptAsync(script);
        }

        private Task InjectBridgeScriptAsync()
        {
            return _platformBridge.ExecuteScriptAsync(BuildBridgeBootstrapScript(Options.JavaScriptReadyMessageType));
        }

        private static string BuildHostReceiveScript(string rawMessage)
        {
            if (LooksLikeJson(rawMessage))
                return $"window.__phialeWebHostReceive && window.__phialeWebHostReceive({rawMessage});";

            string escaped = JavaScriptEncoder.Default.Encode(rawMessage ?? string.Empty);
            return $"window.__phialeWebHostReceive && window.__phialeWebHostReceive(\"{escaped}\");";
        }

        private static string BuildBridgeBootstrapScript(string configuredReadyMessageType)
        {
            string readyMessageType = string.IsNullOrWhiteSpace(configuredReadyMessageType)
                ? "__phialeWebHostReady"
                : JavaScriptEncoder.Default.Encode(configuredReadyMessageType);

            return @"
(function () {
  function emit(name, detail) {
    try { window.dispatchEvent(new CustomEvent(name, { detail: detail })); } catch (_) {}
  }

  var readyMessageType = '" + readyMessageType + @"';

  if (window.__phialeWebHostBridgeInitialized) {
    emit('phiale-webhost-bridge-ready', { repeated: true, readyMessageType: readyMessageType });
    return;
  }

  window.__phialeWebHostBridgeInitialized = true;

  function serialize(message) {
    if (typeof message === 'string') {
      return message;
    }

    try {
      return JSON.stringify(message);
    } catch (_) {
      return '';
    }
  }

  function postToHost(message) {
    var raw = serialize(message);
    if (!raw) {
      return false;
    }

    try {
      if (window.chrome && window.chrome.webview && window.chrome.webview.postMessage) {
        window.chrome.webview.postMessage(raw);
        return true;
      }
    } catch (_) {}

    try {
      if (window.external && typeof window.external.notify === 'function') {
        window.external.notify(raw);
        return true;
      }
    } catch (_) {}

    try {
      console.log('phiale-webhost:' + encodeURIComponent(raw));
      return true;
    } catch (_) {}

    return false;
  }

  function receiveFromHost(message) {
    try {
      if (window.PhialeWebHost && typeof window.PhialeWebHost.onHostMessage === 'function') {
        window.PhialeWebHost.onHostMessage(message);
      }
    } catch (_) {}

    emit('phiale-webhost-message', message);
  }

  var api = window.PhialeWebHost || {};
  api.postMessage = function (message) { return postToHost(message); };
  api.notifyReady = function (payload) {
    var base = payload && typeof payload === 'object' ? payload : {};
    return postToHost(Object.assign({}, base, { type: readyMessageType }));
  };
  api.receive = receiveFromHost;
  api.onHostMessage = api.onHostMessage || null;

  window.PhialeWebHost = api;
  window.__phialeWebHostReceive = receiveFromHost;
  emit('phiale-webhost-bridge-ready', { readyMessageType: readyMessageType });
})();";
        }

        private static string NormalizeRawMessage(string rawMessage)
        {
            if (LooksLikeJson(rawMessage))
                return rawMessage;

            return JsonSerializer.Serialize(rawMessage ?? string.Empty);
        }

        private static bool LooksLikeJson(string rawMessage)
        {
            if (string.IsNullOrWhiteSpace(rawMessage))
                return false;

            try
            {
                using (JsonDocument.Parse(rawMessage))
                    return true;
            }
            catch
            {
                return false;
            }
        }

        private static string TryExtractMessageType(string rawMessage)
        {
            if (!LooksLikeJson(rawMessage))
                return string.Empty;

            try
            {
                using (var doc = JsonDocument.Parse(rawMessage))
                {
                    if (doc.RootElement.ValueKind != JsonValueKind.Object)
                        return string.Empty;

                    if (!doc.RootElement.TryGetProperty("type", out var typeProp))
                        return string.Empty;

                    return typeProp.ValueKind == JsonValueKind.String
                        ? typeProp.GetString() ?? string.Empty
                        : string.Empty;
                }
            }
            catch
            {
                return string.Empty;
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(WebComponentHostRuntime));
        }

        private void DispatchOnCapturedContext(Action action)
        {
            if (action == null)
                return;

            if (_synchronizationContext == null || SynchronizationContext.Current == _synchronizationContext)
            {
                action();
                return;
            }

            _synchronizationContext.Post(_ => action(), null);
        }
    }
}
