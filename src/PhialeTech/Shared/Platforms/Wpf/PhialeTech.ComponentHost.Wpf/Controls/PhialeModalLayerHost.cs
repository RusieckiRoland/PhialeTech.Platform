using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using PhialeTech.ComponentHost.Abstractions.Presentation;
using PhialeTech.ComponentHost.Wpf.Bridges;
using PhialeTech.ComponentHost.Wpf.Services;

namespace PhialeTech.ComponentHost.Wpf.Controls
{
    [TemplatePart(Name = PartSurfaceHost, Type = typeof(FrameworkElement))]
    public class PhialeModalLayerHost : Control
    {
        private const string PartSurfaceHost = "PART_SurfaceHost";

        private FrameworkElement _surfaceHost;
        private WpfHostedSurfaceService _subscribedService;
        private readonly Dictionary<FrameworkElement, Visibility> _suspendedAirspaceElements = new Dictionary<FrameworkElement, Visibility>();

        public static readonly DependencyProperty ServiceProperty =
            DependencyProperty.Register(nameof(Service), typeof(WpfHostedSurfaceService), typeof(PhialeModalLayerHost), new FrameworkPropertyMetadata(null, OnServiceChanged));

        public static readonly DependencyProperty HostedContentProperty =
            DependencyProperty.Register(nameof(HostedContent), typeof(object), typeof(PhialeModalLayerHost), new FrameworkPropertyMetadata(null));

        public static readonly DependencyProperty IsSessionOpenProperty =
            DependencyProperty.Register(nameof(IsSessionOpen), typeof(bool), typeof(PhialeModalLayerHost), new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty IsAnimatedOpenProperty =
            DependencyProperty.Register(nameof(IsAnimatedOpen), typeof(bool), typeof(PhialeModalLayerHost), new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty PresentationModeProperty =
            DependencyProperty.Register(nameof(PresentationMode), typeof(HostedPresentationMode), typeof(PhialeModalLayerHost), new FrameworkPropertyMetadata(HostedPresentationMode.Inline));

        public static readonly DependencyProperty EntranceStyleProperty =
            DependencyProperty.Register(nameof(EntranceStyle), typeof(HostedEntranceStyle), typeof(PhialeModalLayerHost), new FrameworkPropertyMetadata(HostedEntranceStyle.Directional));

        public static readonly DependencyProperty PresentationSizeProperty =
            DependencyProperty.Register(nameof(PresentationSize), typeof(HostedPresentationSize), typeof(PhialeModalLayerHost), new FrameworkPropertyMetadata(HostedPresentationSize.Medium));

        public static readonly DependencyProperty SheetPlacementProperty =
            DependencyProperty.Register(nameof(SheetPlacement), typeof(HostedSheetPlacement), typeof(PhialeModalLayerHost), new FrameworkPropertyMetadata(HostedSheetPlacement.Center));

        public static readonly DependencyProperty CanDismissProperty =
            DependencyProperty.Register(nameof(CanDismiss), typeof(bool), typeof(PhialeModalLayerHost), new FrameworkPropertyMetadata(true));

        public static readonly DependencyProperty SurfaceHorizontalAlignmentProperty =
            DependencyProperty.Register(nameof(SurfaceHorizontalAlignment), typeof(HorizontalAlignment), typeof(PhialeModalLayerHost), new FrameworkPropertyMetadata(HorizontalAlignment.Center));

        public static readonly DependencyProperty SurfaceVerticalAlignmentProperty =
            DependencyProperty.Register(nameof(SurfaceVerticalAlignment), typeof(VerticalAlignment), typeof(PhialeModalLayerHost), new FrameworkPropertyMetadata(VerticalAlignment.Center));

        public static readonly DependencyProperty SurfaceWidthProperty =
            DependencyProperty.Register(nameof(SurfaceWidth), typeof(double), typeof(PhialeModalLayerHost), new FrameworkPropertyMetadata(double.NaN));

        public static readonly DependencyProperty SurfaceMaxWidthProperty =
            DependencyProperty.Register(nameof(SurfaceMaxWidth), typeof(double), typeof(PhialeModalLayerHost), new FrameworkPropertyMetadata(double.PositiveInfinity));

        public static readonly DependencyProperty SurfaceMarginProperty =
            DependencyProperty.Register(nameof(SurfaceMargin), typeof(Thickness), typeof(PhialeModalLayerHost), new FrameworkPropertyMetadata(new Thickness(24)));

        static PhialeModalLayerHost()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(PhialeModalLayerHost), new FrameworkPropertyMetadata(typeof(PhialeModalLayerHost)));
        }

        public PhialeModalLayerHost()
        {
            PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
            PreviewKeyDown += OnPreviewKeyDown;
            GotKeyboardFocus += OnGotKeyboardFocus;
            LostKeyboardFocus += OnLostKeyboardFocus;
            Unloaded += OnUnloaded;
        }

        public WpfHostedSurfaceService Service
        {
            get => (WpfHostedSurfaceService)GetValue(ServiceProperty);
            set => SetValue(ServiceProperty, value);
        }

        public object HostedContent
        {
            get => GetValue(HostedContentProperty);
            set => SetValue(HostedContentProperty, value);
        }

        public bool IsSessionOpen
        {
            get => (bool)GetValue(IsSessionOpenProperty);
            set => SetValue(IsSessionOpenProperty, value);
        }

        public bool IsAnimatedOpen
        {
            get => (bool)GetValue(IsAnimatedOpenProperty);
            set => SetValue(IsAnimatedOpenProperty, value);
        }

        public HostedPresentationMode PresentationMode
        {
            get => (HostedPresentationMode)GetValue(PresentationModeProperty);
            set => SetValue(PresentationModeProperty, value);
        }

        public HostedEntranceStyle EntranceStyle
        {
            get => (HostedEntranceStyle)GetValue(EntranceStyleProperty);
            set => SetValue(EntranceStyleProperty, value);
        }

        public HostedPresentationSize PresentationSize
        {
            get => (HostedPresentationSize)GetValue(PresentationSizeProperty);
            set => SetValue(PresentationSizeProperty, value);
        }

        public HostedSheetPlacement SheetPlacement
        {
            get => (HostedSheetPlacement)GetValue(SheetPlacementProperty);
            set => SetValue(SheetPlacementProperty, value);
        }

        public bool CanDismiss
        {
            get => (bool)GetValue(CanDismissProperty);
            set => SetValue(CanDismissProperty, value);
        }

        public HorizontalAlignment SurfaceHorizontalAlignment
        {
            get => (HorizontalAlignment)GetValue(SurfaceHorizontalAlignmentProperty);
            set => SetValue(SurfaceHorizontalAlignmentProperty, value);
        }

        public VerticalAlignment SurfaceVerticalAlignment
        {
            get => (VerticalAlignment)GetValue(SurfaceVerticalAlignmentProperty);
            set => SetValue(SurfaceVerticalAlignmentProperty, value);
        }

        public double SurfaceWidth
        {
            get => (double)GetValue(SurfaceWidthProperty);
            set => SetValue(SurfaceWidthProperty, value);
        }

        public double SurfaceMaxWidth
        {
            get => (double)GetValue(SurfaceMaxWidthProperty);
            set => SetValue(SurfaceMaxWidthProperty, value);
        }

        public Thickness SurfaceMargin
        {
            get => (Thickness)GetValue(SurfaceMarginProperty);
            set => SetValue(SurfaceMarginProperty, value);
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _surfaceHost = GetTemplateChild(PartSurfaceHost) as FrameworkElement;
        }

        private static void OnServiceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((PhialeModalLayerHost)d).HandleServiceChanged(e.OldValue as WpfHostedSurfaceService, e.NewValue as WpfHostedSurfaceService);
        }

        private void HandleServiceChanged(WpfHostedSurfaceService oldService, WpfHostedSurfaceService newService)
        {
            if (oldService != null)
            {
                oldService.SessionChanged -= HandleServiceSessionChanged;
            }

            _subscribedService = newService;
            if (newService != null)
            {
                newService.SessionChanged += HandleServiceSessionChanged;
            }

            SyncFromService();
        }

        private void HandleServiceSessionChanged(object sender, EventArgs e)
        {
            SyncFromService();
        }

        private void SyncFromService()
        {
            var session = _subscribedService == null ? null : _subscribedService.CurrentSession;
            HostedContent = _subscribedService == null ? null : _subscribedService.CurrentContent;

            if (session == null || session.Request == null)
            {
                IsAnimatedOpen = false;
                IsSessionOpen = false;
                PresentationMode = HostedPresentationMode.Inline;
                EntranceStyle = HostedEntranceStyle.Directional;
                PresentationSize = HostedPresentationSize.Medium;
                SheetPlacement = HostedSheetPlacement.Center;
                CanDismiss = true;
                RestoreSuspendedAirspaceElements();
                return;
            }

            IsAnimatedOpen = false;
            PresentationMode = session.Request.PresentationMode;
            EntranceStyle = session.Request.EntranceStyle;
            PresentationSize = session.Request.Size;
            SheetPlacement = session.Request.Placement;
            CanDismiss = session.Request.CanDismiss;

            ApplyLayoutMetrics();
            IsSessionOpen = true;
            SuspendUnderlyingAirspaceElements();
            Dispatcher.BeginInvoke(new Action(BeginOpenAnimation), System.Windows.Threading.DispatcherPriority.Loaded);
            Dispatcher.BeginInvoke(new Action(FocusHostedContent), System.Windows.Threading.DispatcherPriority.Input);
        }

        private void BeginOpenAnimation()
        {
            if (!IsSessionOpen)
            {
                return;
            }

            IsAnimatedOpen = true;
        }

        private void ApplyLayoutMetrics()
        {
            if (PresentationMode == HostedPresentationMode.OverlaySheet)
            {
                SurfaceVerticalAlignment = VerticalAlignment.Stretch;
                SurfaceMargin = SheetPlacement == HostedSheetPlacement.FullWorkspace ? new Thickness(14) : new Thickness(20);

                if (SheetPlacement == HostedSheetPlacement.FullWorkspace || PresentationSize == HostedPresentationSize.Full)
                {
                    SurfaceHorizontalAlignment = HorizontalAlignment.Stretch;
                    SurfaceWidth = double.NaN;
                    SurfaceMaxWidth = double.PositiveInfinity;
                    return;
                }

                SurfaceHorizontalAlignment = SheetPlacement == HostedSheetPlacement.Right
                    ? HorizontalAlignment.Right
                    : HorizontalAlignment.Center;
                SurfaceWidth = ResolveSheetWidth(PresentationSize);
                SurfaceMaxWidth = ResolveSheetWidth(HostedPresentationSize.Full);
                return;
            }

            SurfaceHorizontalAlignment = HorizontalAlignment.Center;
            SurfaceVerticalAlignment = VerticalAlignment.Center;
            SurfaceMargin = new Thickness(24);
            SurfaceWidth = ResolveCompactWidth(PresentationSize);
            SurfaceMaxWidth = ResolveCompactWidth(HostedPresentationSize.Full);
        }

        private void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!IsSessionOpen)
            {
                return;
            }

            if (IsWithinSurface(e.OriginalSource as DependencyObject))
            {
                return;
            }

            e.Handled = true;
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!IsSessionOpen || _subscribedService == null)
            {
                return;
            }

            var args = HostedSurfaceUniversalInputBridge.CreateKeyEventArgs(e, true);
            _subscribedService.HandleKey(args);
            if (args.Handled)
            {
                e.Handled = true;
            }
        }

        private void OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            _subscribedService?.HandleFocus(HostedSurfaceUniversalInputBridge.CreateFocusChangedEventArgs(true));
        }

        private void OnLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            _subscribedService?.HandleFocus(HostedSurfaceUniversalInputBridge.CreateFocusChangedEventArgs(false));
        }

        private bool IsWithinSurface(DependencyObject source)
        {
            if (_surfaceHost == null || source == null)
            {
                return false;
            }

            var current = source;
            while (current != null)
            {
                if (ReferenceEquals(current, _surfaceHost))
                {
                    return true;
                }

                current = VisualTreeHelper.GetParent(current);
            }

            return false;
        }

        private void FocusHostedContent()
        {
            if (!IsSessionOpen)
            {
                return;
            }

            var contentElement = HostedContent as DependencyObject;
            var focusTarget = FindFocusableDescendant(contentElement, preferInputs: true)
                              ?? FindFocusableDescendant(contentElement, preferInputs: false);
            if (focusTarget is IInputElement inputElement)
            {
                Keyboard.Focus(inputElement);
            }
        }

        private static DependencyObject FindFocusableDescendant(DependencyObject root, bool preferInputs)
        {
            if (root == null)
            {
                return null;
            }

            if (root is Control control &&
                control.Focusable &&
                control.IsEnabled &&
                control.IsVisible &&
                (!preferInputs || IsPreferredInitialFocusTarget(control)))
            {
                return control;
            }

            if (root is UIElement element &&
                element.Focusable &&
                element.IsEnabled &&
                element.IsVisible &&
                (!preferInputs || !(element is ButtonBase)))
            {
                return element;
            }

            var childCount = VisualTreeHelper.GetChildrenCount(root);
            for (var index = 0; index < childCount; index++)
            {
                var child = VisualTreeHelper.GetChild(root, index);
                var match = FindFocusableDescendant(child, preferInputs);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        private static bool IsPreferredInitialFocusTarget(Control control)
        {
            return !(control is ButtonBase);
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            RestoreSuspendedAirspaceElements();
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
                    IsDescendantOf(element, this) ||
                    _suspendedAirspaceElements.ContainsKey(element) ||
                    !ShouldSuspendForModal(element))
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

        private static bool IsDescendantOf(DependencyObject node, DependencyObject ancestor)
        {
            var current = node;
            while (current != null)
            {
                if (ReferenceEquals(current, ancestor))
                {
                    return true;
                }

                current = VisualTreeHelper.GetParent(current);
            }

            return false;
        }

        private static bool ShouldSuspendForModal(FrameworkElement element)
        {
            if (element == null)
            {
                return false;
            }

            var fullName = element.GetType().FullName ?? string.Empty;
            if (string.Equals(fullName, "PhialeTech.MonacoEditor.Wpf.Controls.PhialeMonacoEditor", StringComparison.Ordinal) ||
                string.Equals(fullName, "PhialeTech.WebHost.Wpf.Controls.PhialeWebComponentHost", StringComparison.Ordinal) ||
                string.Equals(fullName, "Microsoft.Web.WebView2.Wpf.WebView2", StringComparison.Ordinal))
            {
                return true;
            }

            return fullName.IndexOf("WebView2", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static double ResolveCompactWidth(HostedPresentationSize size)
        {
            switch (size)
            {
                case HostedPresentationSize.Small:
                    return 480d;
                case HostedPresentationSize.Large:
                    return 860d;
                case HostedPresentationSize.Full:
                    return 1120d;
                default:
                    return 640d;
            }
        }

        private static double ResolveSheetWidth(HostedPresentationSize size)
        {
            switch (size)
            {
                case HostedPresentationSize.Small:
                    return 420d;
                case HostedPresentationSize.Large:
                    return 820d;
                case HostedPresentationSize.Full:
                    return 1120d;
                default:
                    return 620d;
            }
        }
    }
}
