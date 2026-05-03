using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace PhialeTech.WebHost.Wpf.Controls
{
    public abstract class OverlayableWebComponentControlBase : UserControl
    {
        private readonly Dictionary<FrameworkElement, Visibility> _suspendedAirspaceElements = new Dictionary<FrameworkElement, Visibility>();
        private Panel _overlayScopePanel;
        private Window _overlayWindow;
        private Window _overlayHostWindow;
        private bool _isOverlaySurfaceInitialized;

        protected OverlayableWebComponentControlBase()
        {
            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;
            UseLayoutRounding = true;
            SnapsToDevicePixels = true;
            Loaded += HandleLoaded;
            Unloaded += HandleUnloaded;
        }

        protected Grid HostRoot { get; private set; }

        protected Grid HostPresenter { get; private set; }

        protected Border HostViewport { get; private set; }

        protected Grid OverlayRoot { get; private set; }

        protected Border OverlayBackdrop { get; private set; }

        protected Border OverlayViewport { get; private set; }

        protected Grid OverlayPresenter { get; private set; }

        protected bool IsOverlayOpen { get; private set; }

        protected void InitializeOverlaySurface(UIElement hostElement, Thickness hostInset, Thickness overlayInset, CornerRadius overlayCornerRadius)
        {
            if (hostElement == null)
            {
                throw new ArgumentNullException(nameof(hostElement));
            }

            if (_isOverlaySurfaceInitialized)
            {
                throw new InvalidOperationException("Overlay surface has already been initialized.");
            }

            if (hostElement is FrameworkElement hostFrameworkElement)
            {
                hostFrameworkElement.HorizontalAlignment = HorizontalAlignment.Stretch;
                hostFrameworkElement.VerticalAlignment = VerticalAlignment.Stretch;
                hostFrameworkElement.MinWidth = 0d;
                hostFrameworkElement.MinHeight = 0d;
                hostFrameworkElement.Margin = new Thickness(0d);
            }

            HostRoot = new Grid
            {
                SnapsToDevicePixels = true,
                UseLayoutRounding = true,
                ClipToBounds = true,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            HostPresenter = new Grid
            {
                ClipToBounds = true,
                SnapsToDevicePixels = true,
                UseLayoutRounding = true,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            HostPresenter.Children.Add(hostElement);

            HostViewport = new Border
            {
                Padding = hostInset,
                Background = Brushes.Transparent,
                ClipToBounds = true,
                SnapsToDevicePixels = true,
                UseLayoutRounding = true,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Child = HostPresenter,
            };
            HostRoot.Children.Add(HostViewport);

            OverlayBackdrop = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(132, 15, 23, 42)),
                SnapsToDevicePixels = true,
                UseLayoutRounding = true,
            };
            OverlayBackdrop.MouseLeftButtonDown += HandleOverlayBackdropMouseLeftButtonDown;

            OverlayPresenter = new Grid
            {
                ClipToBounds = true,
                SnapsToDevicePixels = true,
                UseLayoutRounding = true,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
            };

            OverlayViewport = new Border
            {
                Margin = overlayInset,
                Background = Brushes.Transparent,
                CornerRadius = overlayCornerRadius,
                ClipToBounds = true,
                SnapsToDevicePixels = true,
                UseLayoutRounding = true,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Child = OverlayPresenter,
            };

            OverlayRoot = new Grid
            {
                Background = Brushes.Transparent,
                SnapsToDevicePixels = true,
                UseLayoutRounding = true,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
            };
            OverlayRoot.Children.Add(OverlayBackdrop);
            OverlayRoot.Children.Add(OverlayViewport);

            Content = HostRoot;
            _isOverlaySurfaceInitialized = true;
        }

        protected bool OpenOverlayInNearestScope()
        {
            EnsureOverlaySurfaceInitialized();
            UpdateOverlayPlacement();

            if (_overlayScopePanel == null)
            {
                throw new InvalidOperationException("Overlay scope is required but no ancestor panel is marked with OverlayHost.IsScope.");
            }

            RemoveOverlayWindow();
            ApplyPanelOverlay(_overlayScopePanel);
            MoveViewport(OverlayPresenter);
            SuspendUnderlyingAirspaceElements();
            IsOverlayOpen = true;
            return true;
        }

        protected bool OpenOverlayWindow()
        {
            EnsureOverlaySurfaceInitialized();
            var owner = Window.GetWindow(this);
            if (owner == null)
            {
                return false;
            }

            RemovePanelOverlay();

            if (_overlayHostWindow == null)
            {
                _overlayHostWindow = new Window
                {
                    Owner = owner,
                    WindowStyle = WindowStyle.None,
                    ResizeMode = ResizeMode.NoResize,
                    ShowInTaskbar = false,
                    AllowsTransparency = false,
                    Background = new SolidColorBrush(Color.FromRgb(15, 23, 42)),
                    SizeToContent = SizeToContent.Manual,
                    Topmost = false,
                    Content = OverlayRoot
                };
                _overlayHostWindow.Closed += HandleOverlayHostWindowClosed;
            }
            else if (!ReferenceEquals(_overlayHostWindow.Owner, owner))
            {
                _overlayHostWindow.Owner = owner;
            }

            UpdateOverlayWindowBounds();
            if (!_overlayHostWindow.IsVisible)
            {
                _overlayHostWindow.Show();
            }

            _overlayHostWindow.Activate();
            MoveViewport(OverlayPresenter);
            SuspendUnderlyingAirspaceElements();
            IsOverlayOpen = true;
            return true;
        }

        protected void CloseOverlaySurface()
        {
            if (!_isOverlaySurfaceInitialized)
            {
                return;
            }

            RemoveOverlayWindow();
            RemovePanelOverlay();
            RestoreSuspendedAirspaceElements();
            MoveViewport(HostRoot);
            IsOverlayOpen = false;
        }

        protected virtual void OnOverlayBackdropMouseLeftButtonDown(MouseButtonEventArgs e)
        {
        }

        protected virtual bool ShouldSuspendForOverlay(FrameworkElement element)
        {
            var fullName = element.GetType().FullName ?? string.Empty;
            if (string.Equals(fullName, "PhialeTech.MonacoEditor.Wpf.Controls.PhialeMonacoEditor", StringComparison.Ordinal) ||
                string.Equals(fullName, "PhialeTech.WebHost.Wpf.Controls.PhialeWebComponentHost", StringComparison.Ordinal) ||
                string.Equals(fullName, "Microsoft.Web.WebView2.Wpf.WebView2", StringComparison.Ordinal))
            {
                return true;
            }

            return fullName.IndexOf("WebView2", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        protected override void OnVisualParentChanged(DependencyObject oldParent)
        {
            base.OnVisualParentChanged(oldParent);
            if (IsOverlayOpen)
            {
                UpdateOverlayPlacement();
            }
        }

        private void HandleLoaded(object sender, RoutedEventArgs e)
        {
            if (IsOverlayOpen)
            {
                UpdateOverlayPlacement();
            }
        }

        private void HandleUnloaded(object sender, RoutedEventArgs e)
        {
            if (IsOverlayOpen)
            {
                CloseOverlaySurface();
            }
        }

        private void HandleOverlayBackdropMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            OnOverlayBackdropMouseLeftButtonDown(e);
        }

        private void UpdateOverlayPlacement()
        {
            var window = Window.GetWindow(this);
            if (window == null)
            {
                return;
            }

            if (!ReferenceEquals(_overlayWindow, window))
            {
                DetachOverlayWindow();
                _overlayWindow = window;
                _overlayWindow.SizeChanged += HandleOverlayWindowLayoutChanged;
                _overlayWindow.LocationChanged += HandleOverlayWindowLayoutChanged;
                _overlayWindow.StateChanged += HandleOverlayWindowLayoutChanged;
            }

            _overlayScopePanel = OverlayHost.FindNearestScopePanel(this);
            if (_overlayScopePanel != null)
            {
                PrepareOverlayRootForHostPanel(_overlayScopePanel);
            }

            if (IsOverlayOpen && _overlayHostWindow != null)
            {
                UpdateOverlayWindowBounds();
            }
        }

        private void ApplyPanelOverlay(Panel scopePanel)
        {
            if (!ReferenceEquals(OverlayRoot.Parent, scopePanel))
            {
                if (OverlayRoot.Parent is Panel currentOverlayParent)
                {
                    currentOverlayParent.Children.Remove(OverlayRoot);
                }

                PrepareOverlayRootForHostPanel(scopePanel);
                Panel.SetZIndex(OverlayRoot, short.MaxValue);
                scopePanel.Children.Add(OverlayRoot);
            }
            else
            {
                PrepareOverlayRootForHostPanel(scopePanel);
            }
        }

        private void MoveViewport(Panel target)
        {
            if (target == null)
            {
                return;
            }

            if (ReferenceEquals(HostViewport.Parent, target))
            {
                return;
            }

            if (HostViewport.Parent is Panel currentPanel)
            {
                currentPanel.Children.Remove(HostViewport);
            }
            else if (HostViewport.Parent is Decorator currentDecorator)
            {
                currentDecorator.Child = null;
            }

            target.Children.Add(HostViewport);
        }

        private void RemovePanelOverlay()
        {
            if (OverlayRoot != null && OverlayRoot.Parent is Panel overlayParent)
            {
                overlayParent.Children.Remove(OverlayRoot);
            }
        }

        private void RemoveOverlayWindow()
        {
            if (_overlayHostWindow == null)
            {
                return;
            }

            var overlayHostWindow = _overlayHostWindow;
            _overlayHostWindow = null;
            overlayHostWindow.Closed -= HandleOverlayHostWindowClosed;
            overlayHostWindow.Content = null;
            overlayHostWindow.Close();
        }

        private void PrepareOverlayRootForHostPanel(Panel hostPanel)
        {
            if (hostPanel == null)
            {
                return;
            }

            OverlayRoot.Width = Math.Max(hostPanel.RenderSize.Width, 0d);
            OverlayRoot.Height = Math.Max(hostPanel.RenderSize.Height, 0d);
            OverlayRoot.HorizontalAlignment = HorizontalAlignment.Stretch;
            OverlayRoot.VerticalAlignment = VerticalAlignment.Stretch;

            if (hostPanel is Grid grid)
            {
                Grid.SetRow(OverlayRoot, 0);
                Grid.SetColumn(OverlayRoot, 0);
                Grid.SetRowSpan(OverlayRoot, Math.Max(grid.RowDefinitions.Count, 1));
                Grid.SetColumnSpan(OverlayRoot, Math.Max(grid.ColumnDefinitions.Count, 1));
            }
        }

        private void HandleOverlayWindowLayoutChanged(object sender, EventArgs e)
        {
            UpdateOverlayPlacement();
        }

        private void DetachOverlayWindow()
        {
            if (_overlayWindow == null)
            {
                return;
            }

            _overlayWindow.SizeChanged -= HandleOverlayWindowLayoutChanged;
            _overlayWindow.LocationChanged -= HandleOverlayWindowLayoutChanged;
            _overlayWindow.StateChanged -= HandleOverlayWindowLayoutChanged;
            _overlayWindow = null;
            _overlayScopePanel = null;
        }

        private void UpdateOverlayWindowBounds()
        {
            if (_overlayHostWindow == null)
            {
                return;
            }

            var owner = _overlayHostWindow.Owner ?? Window.GetWindow(this);
            if (owner == null)
            {
                return;
            }

            var contentElement = owner.Content as FrameworkElement;
            if (contentElement == null || contentElement.ActualWidth <= 0d || contentElement.ActualHeight <= 0d)
            {
                _overlayHostWindow.Left = owner.Left;
                _overlayHostWindow.Top = owner.Top;
                _overlayHostWindow.Width = Math.Max(owner.ActualWidth, owner.Width);
                _overlayHostWindow.Height = Math.Max(owner.ActualHeight, owner.Height);
                return;
            }

            var topLeft = contentElement.PointToScreen(new Point(0d, 0d));
            var presentationSource = PresentationSource.FromVisual(contentElement);
            if (presentationSource != null && presentationSource.CompositionTarget != null)
            {
                topLeft = presentationSource.CompositionTarget.TransformFromDevice.Transform(topLeft);
            }

            _overlayHostWindow.Left = topLeft.X;
            _overlayHostWindow.Top = topLeft.Y;
            _overlayHostWindow.Width = contentElement.ActualWidth;
            _overlayHostWindow.Height = contentElement.ActualHeight;
            OverlayRoot.Width = Math.Max(contentElement.ActualWidth, 0d);
            OverlayRoot.Height = Math.Max(contentElement.ActualHeight, 0d);
            OverlayRoot.HorizontalAlignment = HorizontalAlignment.Stretch;
            OverlayRoot.VerticalAlignment = VerticalAlignment.Stretch;
        }

        private void HandleOverlayHostWindowClosed(object sender, EventArgs e)
        {
            if (ReferenceEquals(sender, _overlayHostWindow))
            {
                _overlayHostWindow = null;
            }

            if (IsOverlayOpen)
            {
                CloseOverlaySurface();
            }
        }

        private void SuspendUnderlyingAirspaceElements()
        {
            var window = Window.GetWindow(this);
            if (window == null)
            {
                return;
            }

            foreach (var element in EnumerateVisualDescendants(window))
            {
                if (element == null ||
                    ReferenceEquals(element, this) ||
                    OverlayHost.IsDescendantOf(element, OverlayRoot) ||
                    OverlayHost.IsDescendantOf(element, this) ||
                    _suspendedAirspaceElements.ContainsKey(element) ||
                    !ShouldSuspendForOverlay(element))
                {
                    continue;
                }

                _suspendedAirspaceElements[element] = element.Visibility;
                element.Visibility = Visibility.Hidden;
            }
        }

        private void RestoreSuspendedAirspaceElements()
        {
            if (_suspendedAirspaceElements.Count == 0)
            {
                return;
            }

            foreach (var entry in _suspendedAirspaceElements)
            {
                if (entry.Key != null)
                {
                    entry.Key.Visibility = entry.Value;
                }
            }

            _suspendedAirspaceElements.Clear();
        }

        private static IEnumerable<FrameworkElement> EnumerateVisualDescendants(DependencyObject root)
        {
            if (root == null)
            {
                yield break;
            }

            var childCount = VisualTreeHelper.GetChildrenCount(root);
            for (var index = 0; index < childCount; index++)
            {
                var child = VisualTreeHelper.GetChild(root, index);
                if (child is FrameworkElement frameworkElement)
                {
                    yield return frameworkElement;
                }

                foreach (var nested in EnumerateVisualDescendants(child))
                {
                    yield return nested;
                }
            }
        }

        private void EnsureOverlaySurfaceInitialized()
        {
            if (!_isOverlaySurfaceInitialized)
            {
                throw new InvalidOperationException("Overlay surface must be initialized before overlay operations.");
            }
        }
    }
}
