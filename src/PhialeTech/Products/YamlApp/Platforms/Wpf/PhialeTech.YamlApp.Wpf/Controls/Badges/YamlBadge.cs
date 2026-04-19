using System.ComponentModel;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using PhialeTech.YamlApp.Abstractions.Enums;
using PhialeTech.YamlApp.Core.Controls.Badge;
using PhialeTech.YamlApp.Wpf.Controls;

namespace PhialeTech.YamlApp.Wpf.Controls.Badges
{
    [ToolboxItem(true)]
    [TemplatePart(Name = PartChromePresenter, Type = typeof(YamlBadgeSkiaChromePresenter))]
    public class YamlBadge : Control
    {
        private const string PartChromePresenter = "PART_ChromePresenter";

        private readonly YamlBadgeController _controller = new YamlBadgeController();
        private YamlBadgeSkiaChromePresenter _chromePresenter;
        private string _lastAutoAutomationName = string.Empty;
        private string _lastAutoAutomationHelpText = string.Empty;

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(YamlBadge), new FrameworkPropertyMetadata(string.Empty, OnVisualPropertyChanged));

        public static readonly DependencyProperty IconKeyProperty =
            DependencyProperty.Register(nameof(IconKey), typeof(string), typeof(YamlBadge), new FrameworkPropertyMetadata(string.Empty, OnVisualPropertyChanged));

        public static readonly DependencyProperty ToneProperty =
            DependencyProperty.Register(nameof(Tone), typeof(BadgeTone), typeof(YamlBadge), new FrameworkPropertyMetadata(BadgeTone.Neutral, OnVisualPropertyChanged));

        public static readonly DependencyProperty VariantProperty =
            DependencyProperty.Register(nameof(Variant), typeof(BadgeVariant), typeof(YamlBadge), new FrameworkPropertyMetadata(BadgeVariant.Soft, OnVisualPropertyChanged));

        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(nameof(Size), typeof(BadgeSize), typeof(YamlBadge), new FrameworkPropertyMetadata(BadgeSize.Regular, OnVisualPropertyChanged));

        public static readonly DependencyProperty IconPlacementProperty =
            DependencyProperty.Register(nameof(IconPlacement), typeof(IconPlacement), typeof(YamlBadge), new FrameworkPropertyMetadata(IconPlacement.Leading, OnVisualPropertyChanged));

        public static readonly DependencyProperty IconGlyphProperty =
            DependencyProperty.Register(nameof(IconGlyph), typeof(string), typeof(YamlBadge), new FrameworkPropertyMetadata(string.Empty));

        public static readonly DependencyProperty ChromePaddingProperty =
            DependencyProperty.Register(nameof(ChromePadding), typeof(Thickness), typeof(YamlBadge), new FrameworkPropertyMetadata(new Thickness(12, 5, 12, 5)));

        public static readonly DependencyProperty ChromeMinHeightProperty =
            DependencyProperty.Register(nameof(ChromeMinHeight), typeof(double), typeof(YamlBadge), new FrameworkPropertyMetadata(26d));

        public static readonly DependencyProperty ChromeCornerRadiusProperty =
            DependencyProperty.Register(nameof(ChromeCornerRadius), typeof(double), typeof(YamlBadge), new FrameworkPropertyMetadata(999d));

        public static readonly DependencyProperty ChromeFontSizeProperty =
            DependencyProperty.Register(nameof(ChromeFontSize), typeof(double), typeof(YamlBadge), new FrameworkPropertyMetadata(12d));

        public static readonly DependencyProperty ChromeIconSizeProperty =
            DependencyProperty.Register(nameof(ChromeIconSize), typeof(double), typeof(YamlBadge), new FrameworkPropertyMetadata(11d));

        public static readonly DependencyProperty ChromeIconMarginProperty =
            DependencyProperty.Register(nameof(ChromeIconMargin), typeof(Thickness), typeof(YamlBadge), new FrameworkPropertyMetadata(new Thickness(0, 0, 6, 0)));

        static YamlBadge()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(YamlBadge), new FrameworkPropertyMetadata(typeof(YamlBadge)));
        }

        public YamlBadge()
        {
            _controller.StateChanged += OnControllerStateChanged;
            Focusable = false;
            IsTabStop = false;
            Loaded += OnLoaded;
            IsEnabledChanged += OnIsEnabledChanged;
            SyncControllerFromProperties();
            ApplyChromeState(_controller.GetChromeState());
            UpdateAutomationName();
            UpdateAutomationHelpText();
        }

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public string IconKey
        {
            get => (string)GetValue(IconKeyProperty);
            set => SetValue(IconKeyProperty, value);
        }

        public BadgeTone Tone
        {
            get => (BadgeTone)GetValue(ToneProperty);
            set => SetValue(ToneProperty, value);
        }

        public BadgeVariant Variant
        {
            get => (BadgeVariant)GetValue(VariantProperty);
            set => SetValue(VariantProperty, value);
        }

        public BadgeSize Size
        {
            get => (BadgeSize)GetValue(SizeProperty);
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
            _chromePresenter = GetTemplateChild(PartChromePresenter) as YamlBadgeSkiaChromePresenter;
        }

        private static void OnVisualPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((YamlBadge)d).SyncControllerFromProperties();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _controller.HandleThemeChanged(YamlBadgeUniversalInputBridge.CreateThemeChangedEventArgs("wpf"));
            UpdateAutomationHelpText();
        }

        private void OnIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            _controller.SetEnabled(IsEnabled);
        }

        private void SyncControllerFromProperties()
        {
            _controller.SetText(Text);
            _controller.SetTone(Tone);
            _controller.SetVariant(Variant);
            _controller.SetSize(Size);
            _controller.SetEnabled(IsEnabled);
            IconGlyph = YamlIconGlyphResolver.ResolveGlyph(IconKey);
            ApplyChromeState(_controller.GetChromeState());
            UpdateAutomationName();
            UpdateAutomationHelpText();
        }

        private void OnControllerStateChanged(object sender, YamlBadgeChromeState state)
        {
            ApplyChromeState(state);
        }

        private void ApplyChromeState(YamlBadgeChromeState state)
        {
            if (state == null || state.LayoutMetrics == null)
            {
                return;
            }

            var metrics = state.LayoutMetrics;
            ChromePadding = new Thickness(metrics.HorizontalPadding, metrics.VerticalPadding, metrics.HorizontalPadding, metrics.VerticalPadding);
            ChromeMinHeight = metrics.MinimumHeight;
            ChromeCornerRadius = metrics.CornerRadius;
            ChromeFontSize = metrics.FontSize;
            ChromeIconSize = metrics.IconSize;
            ChromeIconMargin = string.IsNullOrEmpty(IconGlyph)
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
            var automaticName = !string.IsNullOrWhiteSpace(Text)
                ? Text
                : ResolveToolTipText();
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
