using System;
using System.Globalization;
using System.Threading;
using System.Windows.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using PhialeTech.WebHost.Wpf;

namespace PhialeTech.WebHost.Wpf.Controls
{
    /// <summary>
    /// Hosts browser-backed content inside a clipped viewport with a non-focusable custom scroll container.
    /// </summary>
    public sealed class PhialeWebComponentScrollHost : ContentControl
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

        public static readonly DependencyProperty ViewportPaddingProperty =
            DependencyProperty.Register(
                nameof(ViewportPadding),
                typeof(Thickness),
                typeof(PhialeWebComponentScrollHost),
                new PropertyMetadata(new Thickness()));

        private readonly Border _chromeBorder;
        private readonly Grid _root;
        private readonly ScrollViewportPanel _viewport;
        private readonly ScrollBar _verticalScrollBar;
        private static int _nextDiagnosticsId;
        private readonly int _diagnosticsId;
        private FrameworkElement _observedHostedContent;
        private double _observedHostedContentHeight;

        public PhialeWebComponentScrollHost()
        {
            _diagnosticsId = Interlocked.Increment(ref _nextDiagnosticsId);
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
            _chromeBorder.SetBinding(Border.PaddingProperty, CreateSelfBinding(nameof(ViewportPadding)));

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
            LogScrollMetrics("constructed");
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

        public Thickness ViewportPadding
        {
            get => (Thickness)GetValue(ViewportPaddingProperty);
            set => SetValue(ViewportPaddingProperty, value);
        }

        public double VerticalOffset => _viewport.VerticalOffset;

        private static void OnHostedContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((PhialeWebComponentScrollHost)d).ApplyHostedContent((UIElement)e.NewValue);
        }

        private void ApplyHostedContent(UIElement content)
        {
            LogScrollMetrics("ApplyHostedContent before clear newContent=" + DescribeElement(content));
            DetachHostedContentLayoutObserver();
            _viewport.Children.Clear();
            if (content != null)
            {
                _viewport.Children.Add(content);
                AttachHostedContentLayoutObserver(content);
            }

            UpdateScrollMetrics();
            LogScrollMetrics("ApplyHostedContent after update");
        }

        private void AttachHostedContentLayoutObserver(UIElement content)
        {
            _observedHostedContent = content as FrameworkElement;
            if (_observedHostedContent == null)
            {
                LogScrollMetrics("AttachHostedContentLayoutObserver skipped non-framework content=" + DescribeElement(content));
                return;
            }

            _observedHostedContentHeight = CoerceFiniteDimension(_observedHostedContent.DesiredSize.Height);
            _observedHostedContent.LayoutUpdated += OnHostedContentLayoutUpdated;
            LogScrollMetrics("AttachHostedContentLayoutObserver content=" + DescribeElement(content));
        }

        private void DetachHostedContentLayoutObserver()
        {
            if (_observedHostedContent != null)
            {
                _observedHostedContent.LayoutUpdated -= OnHostedContentLayoutUpdated;
                _observedHostedContent = null;
            }

            _observedHostedContentHeight = 0d;
        }

        private void OnHostedContentLayoutUpdated(object sender, EventArgs e)
        {
            if (_observedHostedContent == null)
            {
                return;
            }

            var desiredHeight = CoerceFiniteDimension(_observedHostedContent.DesiredSize.Height);
            if (Math.Abs(_observedHostedContentHeight - desiredHeight) < 0.01d)
            {
                return;
            }

            _observedHostedContentHeight = desiredHeight;
            _viewport.RefreshExtentFromChild();
            LogScrollMetrics("HostedContent LayoutUpdated desiredHeightChanged=" + FormatDimension(desiredHeight));
        }

        private void OnHostSizeChanged(object sender, SizeChangedEventArgs e)
        {
            LogScrollMetrics(
                "Host SizeChanged previous=" + FormatSize(e.PreviousSize) +
                " new=" + FormatSize(e.NewSize));
            UpdateScrollMetrics();
        }

        private void OnViewportExtentHeightChanged(object sender, EventArgs e)
        {
            LogScrollMetrics("Viewport ExtentHeightChanged before UpdateScrollMetrics");
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
            var viewportHeight = ResolveViewportHeight();
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

            LogScrollMetrics(
                "UpdateScrollMetrics viewportHeight=" + FormatDimension(viewportHeight) +
                " extentHeight=" + FormatDimension(extentHeight) +
                " maximumOffset=" + FormatDimension(maximumOffset) +
                " nextOffset=" + FormatDimension(nextOffset));
        }

        private double ResolveViewportHeight()
        {
            var hostHeight = CoerceFiniteDimension(ActualHeight);
            if (hostHeight > 0d)
            {
                var border = BorderThickness;
                var padding = ViewportPadding;
                return Math.Max(
                    0d,
                    hostHeight -
                    border.Top -
                    border.Bottom -
                    padding.Top -
                    padding.Bottom);
            }

            return Math.Max(0d, _viewport.ViewportHeight);
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

        private static double CoerceFiniteDimension(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
            {
                return 0d;
            }

            return value;
        }

        private Binding CreateSelfBinding(string path)
        {
            return new Binding(path)
            {
                Source = this,
                Mode = BindingMode.OneWay
            };
        }

        private void LogScrollMetrics(string phase)
        {
            PhialeWebHostDiagnostics.Write(
                "PhialeWebComponentScrollHost",
                "id=" + _diagnosticsId.ToString(CultureInfo.InvariantCulture) +
                " name=" + DescribeHostName() +
                " phase=" + phase +
                " host(actual=" + FormatSize(ActualWidth, ActualHeight) +
                ", desired=" + FormatSize(DesiredSize) +
                ", visibility=" + Visibility +
                ", isVisible=" + IsVisible +
                ")" +
                " viewport(actual=" + FormatSize(_viewport.ActualWidth, _viewport.ActualHeight) +
                ", desired=" + FormatSize(_viewport.DesiredSize) +
                ", extent=" + FormatDimension(_viewport.ExtentHeight) +
                ", viewportHeight=" + FormatDimension(_viewport.ViewportHeight) +
                ", offset=" + FormatDimension(_viewport.VerticalOffset) +
                ")" +
                " scrollbar(visibility=" + _verticalScrollBar.Visibility +
                ", max=" + FormatDimension(_verticalScrollBar.Maximum) +
                ", viewportSize=" + FormatDimension(_verticalScrollBar.ViewportSize) +
                ", value=" + FormatDimension(_verticalScrollBar.Value) +
                ", actual=" + FormatSize(_verticalScrollBar.ActualWidth, _verticalScrollBar.ActualHeight) +
                ")" +
                " content=" + DescribeElement(_viewport.Children.Count == 0 ? null : _viewport.Children[0]));
        }

        private string DescribeHostName()
        {
            return string.IsNullOrWhiteSpace(Name) ? "<unnamed>" : Name;
        }

        private static string DescribeElement(UIElement element)
        {
            if (element == null)
            {
                return "<null>";
            }

            var frameworkElement = element as FrameworkElement;
            return element.GetType().Name +
                "(name=" + (string.IsNullOrWhiteSpace(frameworkElement?.Name) ? "<unnamed>" : frameworkElement.Name) +
                ", actual=" + FormatSize(frameworkElement?.ActualWidth ?? 0d, frameworkElement?.ActualHeight ?? 0d) +
                ", desired=" + FormatSize(element.DesiredSize) +
                ", render=" + FormatSize(element.RenderSize) +
                ", visibility=" + frameworkElement?.Visibility +
                ")";
        }

        private static string FormatSize(Size size)
        {
            return FormatSize(size.Width, size.Height);
        }

        private static string FormatSize(double width, double height)
        {
            return FormatDimension(width) + "x" + FormatDimension(height);
        }

        private static string FormatDimension(double value)
        {
            if (double.IsNaN(value))
            {
                return "NaN";
            }

            if (double.IsPositiveInfinity(value))
            {
                return "+Infinity";
            }

            if (double.IsNegativeInfinity(value))
            {
                return "-Infinity";
            }

            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private sealed class ScrollViewportPanel : Panel
        {
            private double _extentHeight;
            private double _verticalOffset;
            private double _viewportHeight;

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

            public double ViewportHeight => _viewportHeight;

            public void RefreshExtentFromChild()
            {
                if (InternalChildren.Count == 0)
                {
                    ExtentHeight = 0d;
                    return;
                }

                var child = InternalChildren[0];
                ExtentHeight = Math.Max(
                    CoerceFiniteDimension(child.DesiredSize.Height),
                    _viewportHeight);
                InvalidateArrange();
            }

            protected override Size MeasureOverride(Size availableSize)
            {
                if (InternalChildren.Count == 0)
                {
                    ExtentHeight = 0d;
                    return new Size(
                        CoerceFiniteDimension(availableSize.Width),
                        0d);
                }

                var child = InternalChildren[0];
                var width = double.IsInfinity(availableSize.Width) ? double.PositiveInfinity : availableSize.Width;
                child.Measure(new Size(width, double.PositiveInfinity));
                ExtentHeight = CoerceFiniteDimension(child.DesiredSize.Height);

                return new Size(
                    ResolveMeasuredDimension(availableSize.Width, child.DesiredSize.Width),
                    ExtentHeight);
            }

            protected override Size ArrangeOverride(Size finalSize)
            {
                if (InternalChildren.Count == 0)
                {
                    ExtentHeight = 0d;
                    _viewportHeight = CoerceFiniteDimension(finalSize.Height);
                    return new Size(
                        CoerceFiniteDimension(finalSize.Width),
                        _viewportHeight);
                }

                var child = InternalChildren[0];
                var finalWidth = CoerceFiniteDimension(finalSize.Width);
                var finalHeight = CoerceFiniteDimension(finalSize.Height);
                var childHeight = Math.Max(CoerceFiniteDimension(child.DesiredSize.Height), finalHeight);
                _viewportHeight = finalHeight;
                ExtentHeight = childHeight;
                child.Arrange(new Rect(0d, -VerticalOffset, finalWidth, childHeight));

                return new Size(finalWidth, finalHeight);
            }

            protected override void OnChildDesiredSizeChanged(UIElement child)
            {
                base.OnChildDesiredSizeChanged(child);

                if (InternalChildren.Count == 0 || !ReferenceEquals(child, InternalChildren[0]))
                {
                    return;
                }

                ExtentHeight = Math.Max(
                    CoerceFiniteDimension(child.DesiredSize.Height),
                    _viewportHeight);
                InvalidateArrange();
            }

            private static double ResolveMeasuredDimension(double available, double desired)
            {
                if (!double.IsInfinity(available) && !double.IsNaN(available))
                {
                    return Math.Max(0d, available);
                }

                return CoerceFiniteDimension(desired);
            }

            private static double CoerceFiniteDimension(double value)
            {
                if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
                {
                    return 0d;
                }

                return value;
            }
        }
    }
}
