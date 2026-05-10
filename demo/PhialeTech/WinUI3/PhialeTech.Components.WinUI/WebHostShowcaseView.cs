using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using PhialeTech.Components.Shared.Services;
using PhialeTech.WebHost.Abstractions.Ui.Web;
using PhialeTech.WebHost.WinUI;
using System;
using System.Threading.Tasks;

namespace PhialeTech.Components.WinUI
{
    public sealed class WebHostShowcaseView : UserControl, IDisposable, IWebDemoFocusModeSource
    {
        private readonly IWebComponentHost _host;
        private readonly TextBlock _stateText;
        private readonly TextBlock _messageText;
        private readonly TextBox _timingText;
        private readonly Button _shuffleButton;
        private readonly Button _focusButton;
        private readonly Button _expandButton;
        private readonly StackPanel _titlePanel;
        private readonly Grid _topBar;
        private readonly StackPanel _detailsPanel;
        private readonly Border _hostSurface;
        private bool _started;
        private bool _initialScenePushed;
        private int _sceneVersion;
        private bool _disposed;
        private bool _isFocusMode;
        private WebHostLoadTrace _loadTrace;

        bool IWebDemoFocusModeSource.IsFocusMode => _isFocusMode;
        bool IWebDemoFocusModeSource.ShowPrimaryFocusAction => true;
        string IWebDemoFocusModeSource.PrimaryFocusActionText => "Shuffle from .NET";
        Task IWebDemoFocusModeSource.ExecutePrimaryFocusActionAsync() => TryPushSceneAsync(".NET shuffle button");
        void IWebDemoFocusModeSource.ExitFocusMode()
        {
            if (!_isFocusMode)
            {
                return;
            }

            _isFocusMode = false;
            UpdateFocusMode();
        }

        event EventHandler<WebDemoFocusModeChangedEventArgs> IWebDemoFocusModeSource.FocusModeChanged
        {
            add => _focusModeChanged += value;
            remove => _focusModeChanged -= value;
        }

        private event EventHandler<WebDemoFocusModeChangedEventArgs> _focusModeChanged;

        public WebHostShowcaseView()
        {
            var factory = new WinUiWebComponentHostFactory();
            _host = factory.CreateHost(new WebComponentHostOptions
            {
                JavaScriptReadyMessageType = DemoWebHostShowcaseContent.ReadyMessageType
            });

            _stateText = new TextBlock
            {
                Margin = new Thickness(0, 6, 0, 0),
                Text = "State: waiting for browser"
            };

            _messageText = new TextBlock
            {
                Margin = new Thickness(0, 6, 0, 0),
                Text = "Last message: (none)",
                TextWrapping = TextWrapping.Wrap
            };

            _timingText = new TextBox
            {
                Margin = new Thickness(0, 10, 0, 0),
                MinHeight = 96,
                MaxHeight = 140,
                IsReadOnly = true,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 11
            };
            ScrollViewer.SetVerticalScrollBarVisibility(_timingText, ScrollBarVisibility.Auto);
            ScrollViewer.SetHorizontalScrollBarVisibility(_timingText, ScrollBarVisibility.Auto);

            _shuffleButton = new Button
            {
                Margin = new Thickness(0, 0, 10, 0),
                Padding = new Thickness(14, 8, 14, 8),
                Content = "Shuffle from .NET",
                IsEnabled = false
            };
            _shuffleButton.Click += HandleShuffleClick;

            _focusButton = new Button
            {
                Padding = new Thickness(14, 8, 14, 8),
                Content = "Focus host",
                IsEnabled = false
            };
            _focusButton.Click += HandleFocusClick;

            _expandButton = new Button
            {
                Padding = new Thickness(14, 8, 14, 8),
                Content = "Expand demo",
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(188, 15, 23, 42)),
                Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255)),
                BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(96, 148, 163, 184)),
                BorderThickness = new Thickness(1)
            };
            _expandButton.Click += HandleExpandClick;

            var buttonRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 10,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(18, 0, 0, 0)
            };
            buttonRow.Children.Add(_shuffleButton);
            buttonRow.Children.Add(_focusButton);
            buttonRow.Children.Add(_expandButton);

            _titlePanel = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center
            };
            _titlePanel.Children.Add(new TextBlock
            {
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                Text = "WebHost demo with JS canvas scene"
            });
            _titlePanel.Children.Add(_stateText);

            _topBar = new Grid
            {
                Margin = new Thickness(0, 0, 0, 12)
            };
            _topBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            _topBar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            Grid.SetColumn(_titlePanel, 0);
            Grid.SetColumn(buttonRow, 1);
            _topBar.Children.Add(_titlePanel);
            _topBar.Children.Add(buttonRow);

            _detailsPanel = new StackPanel
            {
                Margin = new Thickness(0, 0, 0, 12)
            };
            _detailsPanel.Children.Add(new TextBlock
            {
                Text = "This showcase keeps the reusable WebHost clean and loads a demo-side JavaScript canvas scene on top of it.",
                TextWrapping = TextWrapping.Wrap
            });
            _detailsPanel.Children.Add(_messageText);
            _detailsPanel.Children.Add(_timingText);

            _hostSurface = new Border
            {
                Margin = new Thickness(0, 12, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Child = (UIElement)_host
            };

            var layout = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;
            layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            Grid.SetRow(_topBar, 0);
            Grid.SetRow(_detailsPanel, 1);
            Grid.SetRow(_hostSurface, 2);
            layout.Children.Add(_topBar);
            layout.Children.Add(_detailsPanel);
            layout.Children.Add(_hostSurface);

            Content = layout;

            Loaded += HandleLoaded;
            KeyDown += HandleKeyDown;
            _host.ReadyStateChanged += HandleReadyStateChanged;
            _host.MessageReceived += HandleMessageReceived;

            UpdateFocusMode();
        }

        private void HandleLoaded(object sender, RoutedEventArgs e)
        {
            _loadTrace?.Mark("WebHostShowcaseView.Loaded");

            if (_started)
            {
                _loadTrace?.Mark("WebHostShowcaseView already started");
                return;
            }

            _started = true;
            _stateText.Text = "State: loading browser host";
            _messageText.Text = "Last message: initializing WebView2";
            _loadTrace?.Mark("Starting host immediately from Loaded");
            _ = StartHostAsync();
        }

        private void HandleReadyStateChanged(object sender, WebComponentReadyStateChangedEventArgs e)
        {
            if (_disposed)
            {
                return;
            }

            _stateText.Text = $"State: initialized={e.IsInitialized}, ready={e.IsReady}";
            _shuffleButton.IsEnabled = e.IsReady;
            _focusButton.IsEnabled = e.IsInitialized;
            _loadTrace?.Mark($"ReadyStateChanged initialized={e.IsInitialized}, ready={e.IsReady}");
        }

        private void HandleMessageReceived(object sender, WebComponentMessageEventArgs e)
        {
            if (_disposed)
            {
                return;
            }

            _messageText.Text = $"Last message: {e.MessageType} | {e.RawMessage}";
            _loadTrace?.Mark("MessageReceived type=" + (e.MessageType ?? string.Empty));

            if (string.Equals(e.MessageType, "demoPing", StringComparison.OrdinalIgnoreCase))
            {
                _ = TryPushSceneAsync("JS ping acknowledged by .NET");
            }
        }

        private Task PushSceneAsync(string source)
        {
            _sceneVersion++;
            return _host.PostMessageAsync(new
            {
                type = "scene.update",
                seed = _sceneVersion * 19,
                rings = 3 + (_sceneVersion % 4),
                hue = (196 + (_sceneVersion * 27)) % 360,
                label = "Host scene update #" + _sceneVersion,
                source
            });
        }

        private async Task StartHostAsync()
        {
            try
            {
                _loadTrace?.Mark("Host.InitializeAsync begin");
                await _host.InitializeAsync();
                _loadTrace?.Mark("Host.InitializeAsync completed");

                if (!_initialScenePushed)
                {
                    _initialScenePushed = true;
                    _loadTrace?.Mark("Prequeue initial scene begin");
                    await TryPushSceneAsync(".NET initial scene");
                    _loadTrace?.Mark("Prequeue initial scene completed");
                }

                _loadTrace?.Mark("Host.LoadHtmlAsync begin");
                await _host.LoadHtmlAsync(DemoWebHostShowcaseContent.BuildHtml("WinUI3"), "https://demo.webhost/");
                _loadTrace?.Mark("Host.LoadHtmlAsync completed");
            }
            catch (Exception ex)
            {
                _stateText.Text = "State: host failed";
                _messageText.Text = $"Last message: {ex.GetType().Name} | {ex.Message}";
                _loadTrace?.Mark("Host failed: " + ex.GetType().Name + " | " + ex.Message);
            }
        }

        private async void HandleShuffleClick(object sender, RoutedEventArgs e)
        {
            await TryPushSceneAsync(".NET shuffle button");
        }

        private void HandleExpandClick(object sender, RoutedEventArgs e)
        {
            _isFocusMode = !_isFocusMode;
            _loadTrace?.Mark(_isFocusMode ? "Focus mode enabled" : "Focus mode disabled");
            UpdateFocusMode();
        }

        private void HandleFocusClick(object sender, RoutedEventArgs e)
        {
            if (_disposed)
            {
                return;
            }

            try
            {
                _loadTrace?.Mark("Focus host clicked");
                _host.FocusHost();
            }
            catch (Exception ex)
            {
                _stateText.Text = "State: host focus failed";
                _messageText.Text = $"Last message: {ex.GetType().Name} | {ex.Message}";
            }
        }

        private void HandleKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (!_isFocusMode || e.Key != Windows.System.VirtualKey.Escape)
            {
                return;
            }

            _isFocusMode = false;
            _loadTrace?.Mark("Focus mode disabled with Escape");
            UpdateFocusMode();
            e.Handled = true;
        }

        private async Task TryPushSceneAsync(string source)
        {
            if (_disposed)
            {
                return;
            }

            try
            {
                _loadTrace?.Mark("Post scene update begin: " + source);
                await PushSceneAsync(source);
                _loadTrace?.Mark("Post scene update completed: " + source);
            }
            catch (Exception ex)
            {
                _stateText.Text = "State: message failed";
                _messageText.Text = $"Last message: {ex.GetType().Name} | {ex.Message}";
                _loadTrace?.Mark("Post scene update failed: " + ex.GetType().Name + " | " + ex.Message);
            }
        }

        public void AttachLoadTrace(WebHostLoadTrace loadTrace)
        {
            if (_disposed)
            {
                return;
            }

            if (ReferenceEquals(_loadTrace, loadTrace))
            {
                WriteTimingSnapshot();
                return;
            }

            if (_loadTrace != null)
            {
                _loadTrace.Updated -= HandleLoadTraceUpdated;
            }

            _loadTrace = loadTrace;

            if (_loadTrace != null)
            {
                _loadTrace.Updated += HandleLoadTraceUpdated;
                _loadTrace.Mark("Trace attached to WebHostShowcaseView");
            }

            WriteTimingSnapshot();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            Loaded -= HandleLoaded;
            KeyDown -= HandleKeyDown;
            _shuffleButton.Click -= HandleShuffleClick;
            _focusButton.Click -= HandleFocusClick;
            _expandButton.Click -= HandleExpandClick;
            if (_loadTrace != null)
            {
                _loadTrace.Updated -= HandleLoadTraceUpdated;
            }

            _host.ReadyStateChanged -= HandleReadyStateChanged;
            _host.MessageReceived -= HandleMessageReceived;
            _host.Dispose();
        }

        private void HandleLoadTraceUpdated(object sender, EventArgs e)
        {
            if (_disposed)
            {
                return;
            }

            if (DispatcherQueue.HasThreadAccess)
            {
                WriteTimingSnapshot();
                return;
            }

            DispatcherQueue.TryEnqueue(WriteTimingSnapshot);
        }

        private void WriteTimingSnapshot()
        {
            _timingText.Text = _loadTrace?.GetText() ?? string.Empty;
        }

        private void UpdateFocusMode()
        {
            _titlePanel.Visibility = _isFocusMode ? Visibility.Collapsed : Visibility.Visible;
            _topBar.Visibility = _isFocusMode ? Visibility.Collapsed : Visibility.Visible;
            _detailsPanel.Visibility = _isFocusMode ? Visibility.Collapsed : Visibility.Visible;
            _topBar.Margin = _isFocusMode ? new Thickness(0, 0, 0, 8) : new Thickness(0, 0, 0, 12);
            _hostSurface.Margin = _isFocusMode ? new Thickness(0) : new Thickness(0, 12, 0, 0);
            _expandButton.Content = _isFocusMode ? "Exit focus" : "Expand demo";
            ToolTipService.SetToolTip(
                _expandButton,
                _isFocusMode
                    ? "Restore the demo details"
                    : "Hide the diagnostics and let the host fill the demo card");
            _focusModeChanged?.Invoke(this, new WebDemoFocusModeChangedEventArgs(_isFocusMode));
        }
    }
}
