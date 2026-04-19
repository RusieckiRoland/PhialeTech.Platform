using System;
using System.Windows.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace PhialeTech.WebHost.Wpf.Controls
{
    /// <summary>
    /// Hosts browser-backed content inside a clipped viewport with a non-focusable custom scroll container.
    /// </summary>
    public sealed class PhialeWebComponentScrollHost : UserControl
    {
        public static readonly DependencyProperty HostedContentProperty =
            DependencyProperty.Register(
                nameof(HostedContent),
                typeof(UIElement),
                typeof(PhialeWebComponentScrollHost),
                new PropertyMetadata(null, OnHostedContentChanged));

        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(
                nameof(CornerRadius),
                typeof(CornerRadius),
                typeof(PhialeWebComponentScrollHost),
                new PropertyMetadata(new CornerRadius()));

        private readonly Border _chromeBorder;
        private readonly Grid _root;
        private readonly ScrollViewportPanel _viewport;
        private readonly ScrollBar _verticalScrollBar;

        public PhialeWebComponentScrollHost()
        {
            Focusable = false;
            IsTabStop = false;
            ClipToBounds = true;
            UseLayoutRounding = true;
            SnapsToDevicePixels = true;
            PreviewMouseWheel += OnPreviewMouseWheel;
            SizeChanged += OnHostSizeChanged;

            _chromeBorder = new Border
            {
                ClipToBounds = true,
                SnapsToDevicePixels = true
            };
            _chromeBorder.SetBinding(Border.BackgroundProperty, CreateSelfBinding("Background"));
            _chromeBorder.SetBinding(Border.BorderBrushProperty, CreateSelfBinding("BorderBrush"));
            _chromeBorder.SetBinding(Border.BorderThicknessProperty, CreateSelfBinding("BorderThickness"));
            _chromeBorder.SetBinding(Border.CornerRadiusProperty, CreateSelfBinding(nameof(CornerRadius)));

            _root = new Grid
            {
                ClipToBounds = true,
                Focusable = false,
                UseLayoutRounding = true,
                SnapsToDevicePixels = true
            };
            _root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1d, GridUnitType.Star) });
            _root.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _viewport = new ScrollViewportPanel
            {
                ClipToBounds = true,
                Focusable = false,
                IsHitTestVisible = true
            };
            _viewport.ExtentHeightChanged += OnViewportExtentHeightChanged;
            Grid.SetColumn(_viewport, 0);

            _verticalScrollBar = new ScrollBar
            {
                Orientation = Orientation.Vertical,
                Visibility = Visibility.Collapsed,
                Focusable = false,
                IsTabStop = false,
                SmallChange = ResolveMouseWheelStep(),
                LargeChange = 0d
            };
            _verticalScrollBar.ValueChanged += OnVerticalScrollBarValueChanged;
            Grid.SetColumn(_verticalScrollBar, 1);

            _root.Children.Add(_viewport);
            _root.Children.Add(_verticalScrollBar);

            _chromeBorder.Child = _root;
            Content = _chromeBorder;
        }

        public UIElement HostedContent
        {
            get => (UIElement)GetValue(HostedContentProperty);
            set => SetValue(HostedContentProperty, value);
        }

        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        public double VerticalOffset => _viewport.VerticalOffset;

        private static void OnHostedContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((PhialeWebComponentScrollHost)d).ApplyHostedContent((UIElement)e.NewValue);
        }

        private void ApplyHostedContent(UIElement content)
        {
            _viewport.Children.Clear();
            if (content != null)
            {
                _viewport.Children.Add(content);
            }

            UpdateScrollMetrics();
        }

        private void OnHostSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateScrollMetrics();
        }

        private void OnViewportExtentHeightChanged(object sender, EventArgs e)
        {
            UpdateScrollMetrics();
        }

        private void OnVerticalScrollBarValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (Math.Abs(_viewport.VerticalOffset - e.NewValue) < 0.01d)
            {
                return;
            }

            _viewport.VerticalOffset = e.NewValue;
            _viewport.InvalidateArrange();
        }

        private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (_verticalScrollBar.Visibility != Visibility.Visible)
            {
                return;
            }

            var delta = -Math.Sign(e.Delta) * ResolveMouseWheelStep();
            ScrollToOffset(_viewport.VerticalOffset + delta);
            e.Handled = true;
        }

        private void ScrollToOffset(double offset)
        {
            var clamped = ClampOffset(offset);
            if (Math.Abs(_viewport.VerticalOffset - clamped) < 0.01d)
            {
                return;
            }

            _viewport.VerticalOffset = clamped;
            _viewport.InvalidateArrange();
            if (Math.Abs(_verticalScrollBar.Value - clamped) >= 0.01d)
            {
                _verticalScrollBar.Value = clamped;
            }
        }

        private void UpdateScrollMetrics()
        {
            var viewportHeight = Math.Max(0d, _viewport.ActualHeight);
            var extentHeight = Math.Max(viewportHeight, _viewport.ExtentHeight);
            var maximumOffset = Math.Max(0d, extentHeight - viewportHeight);
            var nextOffset = ClampOffset(_viewport.VerticalOffset, maximumOffset);

            _verticalScrollBar.ViewportSize = viewportHeight;
            _verticalScrollBar.Minimum = 0d;
            _verticalScrollBar.Maximum = maximumOffset;
            _verticalScrollBar.LargeChange = viewportHeight;
            _verticalScrollBar.Visibility = maximumOffset > 0d ? Visibility.Visible : Visibility.Collapsed;

            if (Math.Abs(_verticalScrollBar.Value - nextOffset) >= 0.01d)
            {
                _verticalScrollBar.Value = nextOffset;
            }

            if (Math.Abs(_viewport.VerticalOffset - nextOffset) >= 0.01d)
            {
                _viewport.VerticalOffset = nextOffset;
                _viewport.InvalidateArrange();
            }
        }

        private double ClampOffset(double offset)
        {
            return ClampOffset(offset, Math.Max(0d, _verticalScrollBar.Maximum));
        }

        private static double ClampOffset(double offset, double maximumOffset)
        {
            if (offset < 0d)
            {
                return 0d;
            }

            if (offset > maximumOffset)
            {
                return maximumOffset;
            }

            return offset;
        }

        private static double ResolveMouseWheelStep()
        {
            var wheelLines = SystemParameters.WheelScrollLines;
            if (wheelLines <= 0)
            {
                return 48d;
            }

            return wheelLines * 16d;
        }

        private Binding CreateSelfBinding(string path)
        {
            return new Binding(path)
            {
                Source = this,
                Mode = BindingMode.OneWay
            };
        }

        private sealed class ScrollViewportPanel : Panel
        {
            private double _extentHeight;
            private double _verticalOffset;

            public event EventHandler ExtentHeightChanged;

            public double ExtentHeight
            {
                get => _extentHeight;
                private set
                {
                    if (Math.Abs(_extentHeight - value) < 0.01d)
                    {
                        return;
                    }

                    _extentHeight = value;
                    ExtentHeightChanged?.Invoke(this, EventArgs.Empty);
                }
            }

            public double VerticalOffset
            {
                get => _verticalOffset;
                set
                {
                    if (Math.Abs(_verticalOffset - value) < 0.01d)
                    {
                        return;
                    }

                    _verticalOffset = value;
                }
            }

            protected override Size MeasureOverride(Size availableSize)
            {
                if (InternalChildren.Count == 0)
                {
                    ExtentHeight = 0d;
                    return availableSize;
                }

                var child = InternalChildren[0];
                var width = double.IsInfinity(availableSize.Width) ? double.PositiveInfinity : availableSize.Width;
                child.Measure(new Size(width, double.PositiveInfinity));
                ExtentHeight = child.DesiredSize.Height;

                return new Size(availableSize.Width, availableSize.Height);
            }

            protected override Size ArrangeOverride(Size finalSize)
            {
                if (InternalChildren.Count == 0)
                {
                    ExtentHeight = 0d;
                    return finalSize;
                }

                var child = InternalChildren[0];
                var childHeight = Math.Max(child.DesiredSize.Height, finalSize.Height);
                ExtentHeight = childHeight;
                child.Arrange(new Rect(0d, -VerticalOffset, finalSize.Width, childHeight));

                return finalSize;
            }
        }
    }
}
