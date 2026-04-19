using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;
using PhialeGis.Library.Abstractions.Ui.Web;
using PhialeGis.Library.AvaloniaUi.Web;
using System;
using System.IO;

namespace PhialeGis.Library.AvaloniaUi.Controls
{
    /// <summary>
    /// Small sample showing how a higher-level control can embed and use the reusable host.
    /// </summary>
    public sealed class PhialeWebComponentHostSampleView : UserControl
    {
        private readonly PhialeWebComponentHost _host;
        private readonly TextBlock _stateText;
        private readonly TextBlock _messageText;
        private bool _started;

        public PhialeWebComponentHostSampleView()
        {
            var factory = new AvaloniaWebComponentHostFactory();
            _host = (PhialeWebComponentHost)factory.CreateHost(new WebComponentHostOptions
            {
                LocalContentRootPath = Path.Combine(AppContext.BaseDirectory, "Assets"),
                JavaScriptReadyMessageType = "sampleReady"
            });

            _stateText = new TextBlock
            {
                Margin = new Thickness(0, 0, 0, 6),
                Text = "State: waiting for browser"
            };

            _messageText = new TextBlock
            {
                Text = "Last message: (none)",
                TextWrapping = TextWrapping.Wrap
            };

            var title = new TextBlock
            {
                FontSize = 15,
                FontWeight = FontWeight.SemiBold,
                Text = "Reusable Web Host Sample (Avalonia)"
            };

            var header = new StackPanel
            {
                Margin = new Thickness(0, 0, 0, 10)
            };
            header.Children.Add(title);
            header.Children.Add(_stateText);
            header.Children.Add(_messageText);

            var layout = new Grid();
            layout.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            layout.RowDefinitions.Add(new RowDefinition(new GridLength(1, GridUnitType.Star)));
            Grid.SetRow(header, 0);
            Grid.SetRow(_host, 1);
            layout.Children.Add(header);
            layout.Children.Add(_host);

            Content = layout;

            AttachedToVisualTree += OnAttachedToVisualTree;
            DetachedFromVisualTree += OnDetachedFromVisualTree;
            _host.ReadyStateChanged += OnReadyStateChanged;
            _host.MessageReceived += OnMessageReceived;
        }

        private async void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
        {
            if (_started)
                return;

            _started = true;

            await _host.InitializeAsync();
            await _host.PostMessageAsync(new
            {
                type = "hostGreeting",
                platform = "Avalonia",
                detail = "This message was queued before the sample page became ready."
            });
            if (HasLocalEntryPage())
                await _host.LoadEntryPageAsync("WebHostSample/index.html");
            else
                await _host.LoadHtmlAsync(BuildFallbackHtml("Avalonia"));
        }

        private void OnDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
        {
            _host.Dispose();
        }

        private void OnReadyStateChanged(object? sender, WebComponentReadyStateChangedEventArgs e)
        {
            _stateText.Text = $"State: initialized={e.IsInitialized}, ready={e.IsReady}";
        }

        private void OnMessageReceived(object? sender, WebComponentMessageEventArgs e)
        {
            _messageText.Text = $"Last message: {e.MessageType} | {e.RawMessage}";
        }

        private bool HasLocalEntryPage()
        {
            string root = _host.Options.LocalContentRootPath ?? string.Empty;
            string path = Path.Combine(root, "WebHostSample", "index.html");
            return File.Exists(path);
        }

        private static string BuildFallbackHtml(string platform)
        {
            return @"<!doctype html>
<html>
<head>
  <meta charset='utf-8' />
  <meta name='viewport' content='width=device-width, initial-scale=1' />
  <title>Reusable Web Host Sample</title>
  <style>
    :root { color-scheme: dark; }
    body { margin: 0; font-family: 'Segoe UI', sans-serif; background: linear-gradient(180deg, #101726, #19253a); color: #eef3ff; }
    .layout { padding: 20px; display: grid; gap: 14px; }
    .card { border: 1px solid rgba(255,255,255,.14); border-radius: 14px; background: rgba(9, 15, 29, .72); padding: 16px; }
    button { border: 0; border-radius: 999px; padding: 10px 16px; background: #7dd3fc; color: #082032; font-weight: 700; cursor: pointer; }
    pre { margin: 0; white-space: pre-wrap; word-break: break-word; color: #bfdbfe; }
  </style>
</head>
<body>
  <div class='layout'>
    <div class='card'>
      <h2 style='margin:0 0 8px'>Reusable Web Host Sample (" + platform + @")</h2>
      <div>This fallback page was loaded with <code>LoadHtmlAsync</code>.</div>
    </div>
    <div class='card'>
      <button id='ping'>Send JS Ping</button>
    </div>
    <div class='card'>
      <pre id='log'>Waiting for bridge...</pre>
    </div>
  </div>
  <script>
    (function () {
      var logEl = document.getElementById('log');
      function log(message) {
        logEl.textContent = message + '\n' + logEl.textContent;
      }
      window.addEventListener('phiale-webhost-bridge-ready', function () {
        log('bridge ready');
        if (window.PhialeWebHost) {
          window.PhialeWebHost.postMessage({ type: 'sampleReady', source: 'inline', platform: '" + platform + @"', detail: 'inline sample ready' });
        }
      });
      window.addEventListener('phiale-webhost-message', function (event) {
        log('host -> js: ' + JSON.stringify(event.detail));
      });
      document.getElementById('ping').addEventListener('click', function () {
        if (window.PhialeWebHost) {
          window.PhialeWebHost.postMessage({ type: 'samplePing', source: 'inline', platform: '" + platform + @"', detail: 'button click from JS' });
        }
      });
    })();
  </script>
</body>
</html>";
        }
    }
}
