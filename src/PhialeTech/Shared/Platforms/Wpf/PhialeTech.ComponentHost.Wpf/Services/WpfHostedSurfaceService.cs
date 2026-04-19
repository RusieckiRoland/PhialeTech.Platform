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
                    _currentContent = BuildFailureContent(ex);
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

        private static FrameworkElement BuildFailureContent(Exception ex)
        {
            return new Border
            {
                Padding = new Thickness(18),
                Child = new TextBlock
                {
                    Text = ex == null ? "Unknown hosted surface failure." : ex.Message,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = System.Windows.Media.Brushes.IndianRed,
                    FontSize = 13,
                }
            };
        }
    }
}
