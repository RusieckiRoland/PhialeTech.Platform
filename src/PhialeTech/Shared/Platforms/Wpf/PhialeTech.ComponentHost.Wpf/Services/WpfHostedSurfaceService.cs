using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using PhialeTech.ComponentHost.Abstractions.Presentation;
using PhialeTech.ComponentHost.Wpf.Hosting;
using UniversalInput.Contracts;

namespace PhialeTech.ComponentHost.Wpf.Services
{
    public sealed class WpfHostedSurfaceService : IDisposable
    {
        private readonly IHostedSurfaceManager _manager;
        private readonly WpfHostedSurfaceFactoryRegistry _factoryRegistry;
        private FrameworkElement _currentContent;

        public WpfHostedSurfaceService(IHostedSurfaceManager manager, WpfHostedSurfaceFactoryRegistry factoryRegistry)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
            _factoryRegistry = factoryRegistry ?? throw new ArgumentNullException(nameof(factoryRegistry));
            _manager.CurrentSessionChanged += HandleManagerCurrentSessionChanged;
        }

        public event EventHandler SessionChanged;

        public IHostedSurfaceManager Manager => _manager;

        public IHostedSurfaceSessionState CurrentSession => _manager.CurrentSession;

        public FrameworkElement CurrentContent => _currentContent;

        public Task<IHostedSurfaceResult> ShowAsync(IHostedSurfaceRequest request, CancellationToken cancellationToken = default)
        {
            return _manager.ShowAsync(request, cancellationToken);
        }

        public void HandleCommand(UniversalCommandEventArgs e)
        {
            _manager.HandleCommand(e);
        }

        public void HandleKey(UniversalKeyEventArgs e)
        {
            _manager.HandleKey(e);
        }

        public void HandleFocus(UniversalFocusChangedEventArgs e)
        {
            _manager.HandleFocus(e);
        }

        public void Dispose()
        {
            _manager.CurrentSessionChanged -= HandleManagerCurrentSessionChanged;
            DisposeCurrentContent();
        }

        private void HandleManagerCurrentSessionChanged(object sender, EventArgs e)
        {
            var session = _manager.CurrentSession;

            DisposeCurrentContent();

            if (session != null && session.Request != null)
            {
                try
                {
                    _currentContent = _factoryRegistry.CreateContent(session.Request, _manager);
                }
                catch (Exception ex)
                {
                    _currentContent = BuildFailureContent(ex, _manager);
                }
            }

            SessionChanged?.Invoke(this, EventArgs.Empty);
        }

        private void DisposeCurrentContent()
        {
            if (_currentContent is IDisposable disposable)
            {
                disposable.Dispose();
            }

            _currentContent = null;
        }

        private static FrameworkElement BuildFailureContent(Exception ex, IHostedSurfaceManager manager)
        {
            var message = new TextBlock
            {
                Text = ex == null ? "Unknown hosted surface failure." : ex.Message,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center
            };
            message.SetResourceReference(TextBlock.ForegroundProperty, "Brush.Danger.Text");

            var closeButton = new Button
            {
                Name = "HostedSurfaceFailureCloseButton",
                Content = "Close",
                HorizontalAlignment = HorizontalAlignment.Right,
                MinWidth = 88,
                Margin = new Thickness(16, 0, 0, 0)
            };
            closeButton.Click += (sender, args) => manager?.TryDismissCurrent(HostedSurfaceCommandIds.Dismiss);
            closeButton.SetResourceReference(FrameworkElement.StyleProperty, "Button.Secondary");

            var layout = new Grid();
            layout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            layout.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            layout.Children.Add(message);
            Grid.SetColumn(closeButton, 1);
            layout.Children.Add(closeButton);

            var border = new Border
            {
                Padding = new Thickness(18),
                Child = layout
            };
            border.SetResourceReference(Border.BackgroundProperty, "Brush.ComponentHost.Sheet.Background");
            border.SetResourceReference(Border.BorderBrushProperty, "Brush.ComponentHost.Sheet.Border");
            border.BorderThickness = new Thickness(1);

            return border;
        }
    }
}
