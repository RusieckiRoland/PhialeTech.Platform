using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using PhialeTech.WebHost.Abstractions.Ui.Web;
using PhialeTech.WebHost.Avalonia;
using PhialeTech.Components.Shared.Services;
using System;
using System.Threading.Tasks;

namespace PhialeTech.Components.Avalonia;

public sealed class WebHostShowcaseView : UserControl, IDisposable
{
    private readonly IWebComponentHost _host;
    private readonly TextBlock _stateText;
    private readonly TextBlock _messageText;
    private readonly StackPanel _titlePanel;
    private readonly Grid _topBar;
    private readonly StackPanel _detailsPanel;
    private readonly Border _hostSurface;
    private readonly Button _expandButton;
    private bool _started;
    private int _sceneVersion;
    private bool _isFocusMode;
    private bool _disposed;
    public WebHostShowcaseView()
    {
        DemoCefRuntime.EnsureInitialized();

        var factory = new AvaloniaWebComponentHostFactory();
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

        var shuffleButton = new Button
        {
            Content = "Shuffle from .NET",
            Padding = new Thickness(14, 8, 14, 8),
            Margin = new Thickness(0, 0, 10, 0)
        };
        shuffleButton.Click += async (_, _) => await PushSceneAsync(".NET shuffle button");

        var focusButton = new Button
        {
            Content = "Focus host",
            Padding = new Thickness(14, 8, 14, 8)
        };
        focusButton.Click += (_, _) => _host.FocusHost();

        _expandButton = new Button
        {
            Content = "Expand demo",
            Padding = new Thickness(14, 8, 14, 8),
            Background = new SolidColorBrush(Color.FromArgb(188, 15, 23, 42)),
            Foreground = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromArgb(96, 148, 163, 184)),
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
        buttonRow.Children.Add(shuffleButton);
        buttonRow.Children.Add(focusButton);
        buttonRow.Children.Add(_expandButton);

        _titlePanel = new StackPanel
        {
            Spacing = 6
        };
        _titlePanel.Children.Add(new TextBlock
        {
            FontSize = 18,
            FontWeight = FontWeight.SemiBold,
            Text = "WebHost demo with JS canvas scene"
        });
        _titlePanel.Children.Add(_stateText);

        _topBar = new Grid
        {
            Margin = new Thickness(0, 0, 0, 12),
            ColumnDefinitions = new ColumnDefinitions("*,Auto")
        };
        Grid.SetColumn(_titlePanel, 0);
        Grid.SetColumn(buttonRow, 1);
        _topBar.Children.Add(_titlePanel);
        _topBar.Children.Add(buttonRow);

        _detailsPanel = new StackPanel
        {
            Margin = new Thickness(0, 0, 0, 12),
            Spacing = 10
        };
        _detailsPanel.Children.Add(new TextBlock
        {
            Text = "This showcase keeps the reusable WebHost clean and loads a demo-side JavaScript canvas scene on top of it.",
            TextWrapping = TextWrapping.Wrap
        });
        _detailsPanel.Children.Add(_messageText);

        _hostSurface = new Border
        {
            Margin = new Thickness(0, 12, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            ClipToBounds = true,
            Child = (Control)_host
        };

        var layout = new Grid
        {
            ClipToBounds = true
        };
        layout.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        layout.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        layout.RowDefinitions.Add(new RowDefinition(new GridLength(1, GridUnitType.Star)));
        Grid.SetRow(_topBar, 0);
        Grid.SetRow(_detailsPanel, 1);
        Grid.SetRow(_hostSurface, 2);
        layout.Children.Add(_topBar);
        layout.Children.Add(_detailsPanel);
        layout.Children.Add(_hostSurface);

        Content = layout;

        AttachedToVisualTree += HandleAttachedToVisualTree;
        KeyDown += HandleKeyDown;
        _host.ReadyStateChanged += HandleReadyStateChanged;
        _host.MessageReceived += HandleMessageReceived;

        UpdateFocusMode();
    }

    private async void HandleAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (_started)
        {
            return;
        }

        _started = true;
        await _host.InitializeAsync();
        await PushSceneAsync(".NET queued before ready");
        await _host.LoadHtmlAsync(DemoWebHostShowcaseContent.BuildHtml("Avalonia"), "https://demo.webhost/");
    }

    private void HandleReadyStateChanged(object? sender, WebComponentReadyStateChangedEventArgs e)
    {
        _stateText.Text = $"State: initialized={e.IsInitialized}, ready={e.IsReady}";
    }

    private void HandleMessageReceived(object? sender, WebComponentMessageEventArgs e)
    {
        _messageText.Text = $"Last message: {e.MessageType} | {e.RawMessage}";

        if (string.Equals(e.MessageType, "demoPing", StringComparison.OrdinalIgnoreCase))
        {
            _ = PushSceneAsync("JS ping acknowledged by .NET");
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

    private void HandleExpandClick(object? sender, RoutedEventArgs e)
    {
        _isFocusMode = !_isFocusMode;
        UpdateFocusMode();
    }

    private void HandleKeyDown(object? sender, KeyEventArgs e)
    {
        if (!_isFocusMode || e.Key != Key.Escape)
        {
            return;
        }

        _isFocusMode = false;
        UpdateFocusMode();
        e.Handled = true;
    }

    private void UpdateFocusMode()
    {
        _titlePanel.IsVisible = !_isFocusMode;
        _detailsPanel.IsVisible = !_isFocusMode;
        _topBar.Margin = _isFocusMode ? new Thickness(0, 0, 0, 8) : new Thickness(0, 0, 0, 12);
        _hostSurface.Margin = _isFocusMode ? new Thickness(0) : new Thickness(0, 12, 0, 0);
        _expandButton.Content = _isFocusMode ? "Exit focus" : "Expand demo";
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        AttachedToVisualTree -= HandleAttachedToVisualTree;
        KeyDown -= HandleKeyDown;
        _host.ReadyStateChanged -= HandleReadyStateChanged;
        _host.MessageReceived -= HandleMessageReceived;
        _host.Dispose();
    }
}
