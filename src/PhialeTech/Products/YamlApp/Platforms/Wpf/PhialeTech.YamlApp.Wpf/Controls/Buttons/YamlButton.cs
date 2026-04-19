using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using PhialeTech.YamlApp.Abstractions.Enums;
using PhialeTech.YamlApp.Core.Controls.Button;
using PhialeTech.YamlApp.Wpf.Controls;

namespace PhialeTech.YamlApp.Wpf.Controls.Buttons
{
    [ToolboxItem(true)]
    [TemplatePart(Name = PartChromePresenter, Type = typeof(YamlButtonSkiaChromePresenter))]
    public class YamlButton : Button
    {
        private const string PartChromePresenter = "PART_ChromePresenter";

        private readonly YamlButtonController _controller = new YamlButtonController();
        private YamlButtonSkiaChromePresenter _chromePresenter;
        private string _lastAutoAutomationName = string.Empty;
        private string _lastAutoAutomationHelpText = string.Empty;

        public static readonly RoutedEvent InvokedEvent =
            EventManager.RegisterRoutedEvent(
                nameof(Invoked),
                RoutingStrategy.Bubble,
                typeof(EventHandler<YamlButtonInvokedEventArgs>),
                typeof(YamlButton));

        public static readonly DependencyProperty CommandIdProperty =
            DependencyProperty.Register(nameof(CommandId), typeof(string), typeof(YamlButton), new FrameworkPropertyMetadata(string.Empty, OnVisualPropertyChanged));

        public static readonly DependencyProperty IconKeyProperty =
            DependencyProperty.Register(nameof(IconKey), typeof(string), typeof(YamlButton), new FrameworkPropertyMetadata(string.Empty, OnVisualPropertyChanged));

        public static readonly DependencyProperty ToneProperty =
            DependencyProperty.Register(nameof(Tone), typeof(ButtonTone), typeof(YamlButton), new FrameworkPropertyMetadata(ButtonTone.Secondary, OnVisualPropertyChanged));

        public static readonly DependencyProperty VariantProperty =
            DependencyProperty.Register(nameof(Variant), typeof(ButtonVariant), typeof(YamlButton), new FrameworkPropertyMetadata(ButtonVariant.Standard, OnVisualPropertyChanged));

        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(nameof(Size), typeof(ButtonSize), typeof(YamlButton), new FrameworkPropertyMetadata(ButtonSize.Regular, OnVisualPropertyChanged));

        public static readonly DependencyProperty IconPlacementProperty =
            DependencyProperty.Register(nameof(IconPlacement), typeof(IconPlacement), typeof(YamlButton), new FrameworkPropertyMetadata(IconPlacement.Leading, OnVisualPropertyChanged));

        public static readonly DependencyProperty IconGlyphProperty =
            DependencyProperty.Register(nameof(IconGlyph), typeof(string), typeof(YamlButton), new FrameworkPropertyMetadata(string.Empty));

        public static readonly DependencyProperty ChromePaddingProperty =
            DependencyProperty.Register(nameof(ChromePadding), typeof(Thickness), typeof(YamlButton), new FrameworkPropertyMetadata(new Thickness(16, 9, 16, 9)));

        public static readonly DependencyProperty ChromeMinWidthProperty =
            DependencyProperty.Register(nameof(ChromeMinWidth), typeof(double), typeof(YamlButton), new FrameworkPropertyMetadata(96d));

        public static readonly DependencyProperty ChromeMinHeightProperty =
            DependencyProperty.Register(nameof(ChromeMinHeight), typeof(double), typeof(YamlButton), new FrameworkPropertyMetadata(36d));

        public static readonly DependencyProperty ChromeCornerRadiusProperty =
            DependencyProperty.Register(nameof(ChromeCornerRadius), typeof(double), typeof(YamlButton), new FrameworkPropertyMetadata(8d));

        public static readonly DependencyProperty ChromeFontSizeProperty =
            DependencyProperty.Register(nameof(ChromeFontSize), typeof(double), typeof(YamlButton), new FrameworkPropertyMetadata(13d));

        public static readonly DependencyProperty ChromeIconSizeProperty =
            DependencyProperty.Register(nameof(ChromeIconSize), typeof(double), typeof(YamlButton), new FrameworkPropertyMetadata(12d));

        public static readonly DependencyProperty ChromeIconMarginProperty =
            DependencyProperty.Register(nameof(ChromeIconMargin), typeof(Thickness), typeof(YamlButton), new FrameworkPropertyMetadata(new Thickness(0, 0, 8, 0)));

        static YamlButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(YamlButton), new FrameworkPropertyMetadata(typeof(YamlButton)));
        }

        public YamlButton()
        {
            _controller.StateChanged += OnControllerStateChanged;
            Loaded += OnLoaded;
            MouseEnter += OnMouseEnter;
            MouseLeave += OnMouseLeave;
            PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
            PreviewMouseLeftButtonUp += OnPreviewMouseLeftButtonUp;
            LostMouseCapture += OnPointerLeave;
            GotKeyboardFocus += OnGotKeyboardFocus;
            LostKeyboardFocus += OnLostKeyboardFocus;
            IsEnabledChanged += OnIsEnabledChanged;
            SyncControllerFromProperties();
            ApplyChromeState(_controller.GetChromeState());
            UpdateAutomationName();
            UpdateAutomationHelpText();
        }

        public event EventHandler<YamlButtonInvokedEventArgs> Invoked
        {
            add => AddHandler(InvokedEvent, value);
            remove => RemoveHandler(InvokedEvent, value);
        }

        public string CommandId
        {
            get => (string)GetValue(CommandIdProperty);
            set => SetValue(CommandIdProperty, value);
        }

        public string IconKey
        {
            get => (string)GetValue(IconKeyProperty);
            set => SetValue(IconKeyProperty, value);
        }

        public ButtonTone Tone
        {
            get => (ButtonTone)GetValue(ToneProperty);
            set => SetValue(ToneProperty, value);
        }

        public ButtonVariant Variant
        {
            get => (ButtonVariant)GetValue(VariantProperty);
            set => SetValue(VariantProperty, value);
        }

        public ButtonSize Size
        {
            get => (ButtonSize)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        public IconPlacement IconPlacement
        {
            get => (IconPlacement)GetValue(IconPlacementProperty);
            set => SetValue(IconPlacementProperty, value);
        }

        public string IconGlyph
        {
            get => (string)GetValue(IconGlyphProperty);
            private set => SetValue(IconGlyphProperty, value);
        }

        public Thickness ChromePadding
        {
            get => (Thickness)GetValue(ChromePaddingProperty);
            private set => SetValue(ChromePaddingProperty, value);
        }

        public double ChromeMinWidth
        {
            get => (double)GetValue(ChromeMinWidthProperty);
            private set => SetValue(ChromeMinWidthProperty, value);
        }

        public double ChromeMinHeight
        {
            get => (double)GetValue(ChromeMinHeightProperty);
            private set => SetValue(ChromeMinHeightProperty, value);
        }

        public double ChromeCornerRadius
        {
            get => (double)GetValue(ChromeCornerRadiusProperty);
            private set => SetValue(ChromeCornerRadiusProperty, value);
        }

        public double ChromeFontSize
        {
            get => (double)GetValue(ChromeFontSizeProperty);
            private set => SetValue(ChromeFontSizeProperty, value);
        }

        public double ChromeIconSize
        {
            get => (double)GetValue(ChromeIconSizeProperty);
            private set => SetValue(ChromeIconSizeProperty, value);
        }

        public Thickness ChromeIconMargin
        {
            get => (Thickness)GetValue(ChromeIconMarginProperty);
            private set => SetValue(ChromeIconMarginProperty, value);
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _chromePresenter = GetTemplateChild(PartChromePresenter) as YamlButtonSkiaChromePresenter;
        }

        protected override void OnContentChanged(object oldContent, object newContent)
        {
            base.OnContentChanged(oldContent, newContent);
            SyncControllerFromProperties();
            UpdateAutomationName();
        }

        protected override void OnClick()
        {
            base.OnClick();

            var command = _controller.HandleTapped(YamlButtonUniversalInputBridge.CreateTappedEventArgs());
            if (command != null)
            {
                RaiseEvent(new YamlButtonInvokedEventArgs(InvokedEvent, this, command));
            }
        }

        private static void OnVisualPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((YamlButton)d).SyncControllerFromProperties();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _controller.HandleThemeChanged(YamlButtonUniversalInputBridge.CreateThemeChangedEventArgs("wpf"));
            UpdateAutomationHelpText();
        }

        private void OnMouseEnter(object sender, MouseEventArgs e)
        {
            _controller.SetHover(true);
        }

        private void OnMouseLeave(object sender, MouseEventArgs e)
        {
            _controller.SetHover(false);
            _controller.SetPressed(false);
        }

        private void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _controller.SetPressed(true);
        }

        private void OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _controller.SetPressed(false);
        }

        private void OnPointerLeave(object sender, RoutedEventArgs e)
        {
            _controller.SetPressed(false);
        }

        private void OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            _controller.HandleFocusChanged(YamlButtonUniversalInputBridge.CreateFocusChangedEventArgs(true));
        }

        private void OnLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            _controller.HandleFocusChanged(YamlButtonUniversalInputBridge.CreateFocusChangedEventArgs(false));
        }

        private void OnIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            _controller.SetEnabled(IsEnabled);
        }

        private void SyncControllerFromProperties()
        {
            _controller.SetCommandId(CommandId);
            _controller.SetTone(Tone);
            _controller.SetVariant(Variant);
            _controller.SetSize(Size);
            _controller.SetHasTextContent(!string.IsNullOrWhiteSpace(ResolveContentText(Content)));
            _controller.SetEnabled(IsEnabled);
            IconGlyph = YamlIconGlyphResolver.ResolveGlyph(IconKey);
            ApplyChromeState(_controller.GetChromeState());
            UpdateAutomationName();
            UpdateAutomationHelpText();
        }

        private void OnControllerStateChanged(object sender, YamlButtonChromeState state)
        {
            ApplyChromeState(state);
        }

        private void ApplyChromeState(YamlButtonChromeState state)
        {
            if (state == null || state.LayoutMetrics == null)
            {
                return;
            }

            var metrics = state.LayoutMetrics;
            ChromePadding = new Thickness(metrics.HorizontalPadding, metrics.VerticalPadding, metrics.HorizontalPadding, metrics.VerticalPadding);
            ChromeMinWidth = metrics.MinimumWidth;
            ChromeMinHeight = metrics.MinimumHeight;
            ChromeCornerRadius = metrics.CornerRadius;
            ChromeFontSize = metrics.FontSize;
            ChromeIconSize = metrics.IconSize;
            ChromeIconMargin = string.IsNullOrEmpty(IconGlyph) || !state.HasTextContent
                ? new Thickness(0)
                : IconPlacement == IconPlacement.Trailing
                    ? new Thickness(metrics.ContentGap, 0, 0, 0)
                    : new Thickness(0, 0, metrics.ContentGap, 0);

            if (_chromePresenter != null)
            {
                _chromePresenter.CornerRadius = metrics.CornerRadius;
            }
        }

        private void UpdateAutomationName()
        {
            var automaticName = ResolveAutomaticAutomationName();
            var localValue = ReadLocalValue(AutomationProperties.NameProperty);
            if (localValue == DependencyProperty.UnsetValue || Equals(localValue, _lastAutoAutomationName))
            {
                _lastAutoAutomationName = automaticName;
                AutomationProperties.SetName(this, automaticName);
            }
        }

        private void UpdateAutomationHelpText()
        {
            var automaticHelpText = ResolveToolTipText();
            var localValue = ReadLocalValue(AutomationProperties.HelpTextProperty);
            if (localValue == DependencyProperty.UnsetValue || Equals(localValue, _lastAutoAutomationHelpText))
            {
                _lastAutoAutomationHelpText = automaticHelpText;
                AutomationProperties.SetHelpText(this, automaticHelpText);
            }
        }

        private string ResolveAutomaticAutomationName()
        {
            var contentText = ResolveContentText(Content);
            if (!string.IsNullOrWhiteSpace(contentText))
            {
                return contentText;
            }

            var toolTipText = ResolveToolTipText();
            if (!string.IsNullOrWhiteSpace(toolTipText))
            {
                return toolTipText;
            }

            return CommandId ?? string.Empty;
        }

        private static string ResolveContentText(object content)
        {
            if (content == null)
            {
                return string.Empty;
            }

            if (content is string text)
            {
                return text;
            }

            if (content is AccessText accessText)
            {
                return accessText.Text ?? string.Empty;
            }

            if (content is TextBlock textBlock)
            {
                return textBlock.Text ?? string.Empty;
            }

            return content.ToString() ?? string.Empty;
        }

        private string ResolveToolTipText()
        {
            if (ToolTip is string text)
            {
                return text ?? string.Empty;
            }

            if (ToolTip is TextBlock textBlock)
            {
                return textBlock.Text ?? string.Empty;
            }

            return ToolTip?.ToString() ?? string.Empty;
        }
    }
}
