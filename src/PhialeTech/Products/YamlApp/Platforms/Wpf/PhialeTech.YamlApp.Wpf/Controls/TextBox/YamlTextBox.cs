using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using PhialeTech.YamlApp.Abstractions.Enums;
using PhialeTech.YamlApp.Core.Controls.TextBox;
using PhialeTech.YamlApp.Runtime.Controls.TextBox;
using PhialeTech.YamlApp.Runtime.Model;

namespace PhialeTech.YamlApp.Wpf.Controls.TextBox
{
    [ToolboxItem(true)]
    [TemplatePart(Name = PartEditor, Type = typeof(System.Windows.Controls.TextBox))]
    [TemplatePart(Name = PartChromePresenter, Type = typeof(YamlTextBoxSkiaChromePresenter))]
    [TemplatePart(Name = PartClearButton, Type = typeof(ButtonBase))]
    [TemplatePart(Name = PartRestoreButton, Type = typeof(ButtonBase))]
    public class YamlTextBox : Control
    {
        private const string PartEditor = "PART_Editor";
        private const string PartChromePresenter = "PART_ChromePresenter";
        private const string PartClearButton = "PART_ClearButton";
        private const string PartRestoreButton = "PART_RestoreButton";

        private readonly YamlTextBoxController _controller = new YamlTextBoxController();
        private System.Windows.Controls.TextBox _editor;
        private YamlTextBoxSkiaChromePresenter _chromePresenter;
        private ButtonBase _clearButton;
        private ButtonBase _restoreButton;
        private YamlTextBoxFieldBinding _runtimeBinding;
        private bool _isSynchronizing;

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(YamlTextBox), new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnVisualPropertyChanged));

        public static readonly DependencyProperty CaptionProperty =
            DependencyProperty.Register(nameof(Caption), typeof(string), typeof(YamlTextBox), new FrameworkPropertyMetadata(string.Empty, OnVisualPropertyChanged));

        public static readonly DependencyProperty PlaceholderProperty =
            DependencyProperty.Register(nameof(Placeholder), typeof(string), typeof(YamlTextBox), new FrameworkPropertyMetadata(string.Empty, OnVisualPropertyChanged));

        public static readonly DependencyProperty OldValueProperty =
            DependencyProperty.Register(nameof(OldValue), typeof(string), typeof(YamlTextBox), new FrameworkPropertyMetadata(string.Empty, OnVisualPropertyChanged));

        public static readonly DependencyProperty ErrorMessageProperty =
            DependencyProperty.Register(nameof(ErrorMessage), typeof(string), typeof(YamlTextBox), new FrameworkPropertyMetadata(string.Empty, OnVisualPropertyChanged));

        public static readonly DependencyProperty IsFieldRequiredProperty =
            DependencyProperty.Register(nameof(IsFieldRequired), typeof(bool), typeof(YamlTextBox), new FrameworkPropertyMetadata(false, OnVisualPropertyChanged));

        public static readonly DependencyProperty ShowOldValueRestoreButtonProperty =
            DependencyProperty.Register(nameof(ShowOldValueRestoreButton), typeof(bool), typeof(YamlTextBox), new FrameworkPropertyMetadata(false, OnVisualPropertyChanged));

        public static readonly DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register(nameof(IsReadOnly), typeof(bool), typeof(YamlTextBox), new FrameworkPropertyMetadata(false, OnVisualPropertyChanged));

        public static readonly DependencyProperty FieldChromeModeProperty =
            DependencyProperty.Register(nameof(FieldChromeMode), typeof(FieldChromeMode), typeof(YamlTextBox), new FrameworkPropertyMetadata(FieldChromeMode.Framed, OnVisualPropertyChanged));

        public static readonly DependencyProperty CaptionPlacementProperty =
            DependencyProperty.Register(nameof(CaptionPlacement), typeof(CaptionPlacement), typeof(YamlTextBox), new FrameworkPropertyMetadata(CaptionPlacement.Top, OnVisualPropertyChanged));

        public static readonly DependencyProperty DensityModeProperty =
            DependencyProperty.Register(nameof(DensityMode), typeof(DensityMode), typeof(YamlTextBox), new FrameworkPropertyMetadata(DensityMode.Normal, OnVisualPropertyChanged));

        public static readonly DependencyProperty InteractionModeProperty =
            DependencyProperty.Register(nameof(InteractionMode), typeof(InteractionMode), typeof(YamlTextBox), new FrameworkPropertyMetadata(InteractionMode.Classic, OnVisualPropertyChanged));

        public static readonly DependencyProperty MaxLengthProperty =
            DependencyProperty.Register(nameof(MaxLength), typeof(int?), typeof(YamlTextBox), new FrameworkPropertyMetadata(null, OnVisualPropertyChanged));

        public static readonly DependencyProperty RuntimeFieldStateProperty =
            DependencyProperty.Register(nameof(RuntimeFieldState), typeof(RuntimeFieldState), typeof(YamlTextBox), new FrameworkPropertyMetadata(null, OnRuntimeFieldStateChanged));

        public static readonly DependencyProperty EditorMarginProperty =
            DependencyProperty.Register(nameof(EditorMargin), typeof(Thickness), typeof(YamlTextBox), new FrameworkPropertyMetadata(new Thickness(14, 30, 14, 26)));

        public static readonly DependencyProperty EditorPaddingProperty =
            DependencyProperty.Register(nameof(EditorPadding), typeof(Thickness), typeof(YamlTextBox), new FrameworkPropertyMetadata(new Thickness(9, 0, 28, 1)));

        public static readonly DependencyProperty PlaceholderMarginProperty =
            DependencyProperty.Register(nameof(PlaceholderMargin), typeof(Thickness), typeof(YamlTextBox), new FrameworkPropertyMetadata(new Thickness(10, 0, 32, 0)));

        public static readonly DependencyProperty ClearButtonMarginProperty =
            DependencyProperty.Register(nameof(ClearButtonMargin), typeof(Thickness), typeof(YamlTextBox), new FrameworkPropertyMetadata(new Thickness(0)));

        public static readonly DependencyProperty ClearButtonVisibilityProperty =
            DependencyProperty.Register(nameof(ClearButtonVisibility), typeof(Visibility), typeof(YamlTextBox), new FrameworkPropertyMetadata(Visibility.Collapsed));

        public static readonly DependencyProperty RestoreButtonVisibilityProperty =
            DependencyProperty.Register(nameof(RestoreButtonVisibility), typeof(Visibility), typeof(YamlTextBox), new FrameworkPropertyMetadata(Visibility.Collapsed));

        public static readonly DependencyProperty ClearButtonWidthProperty =
            DependencyProperty.Register(nameof(ClearButtonWidth), typeof(double), typeof(YamlTextBox), new FrameworkPropertyMetadata(14d));

        public static readonly DependencyProperty ClearButtonHeightProperty =
            DependencyProperty.Register(nameof(ClearButtonHeight), typeof(double), typeof(YamlTextBox), new FrameworkPropertyMetadata(14d));

        public static readonly DependencyProperty TrailingActionKindProperty =
            DependencyProperty.Register(nameof(TrailingActionKind), typeof(YamlTextBoxTrailingActionKind), typeof(YamlTextBox), new FrameworkPropertyMetadata(YamlTextBoxTrailingActionKind.None));

        static YamlTextBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(YamlTextBox), new FrameworkPropertyMetadata(typeof(YamlTextBox)));
        }

        public YamlTextBox()
        {
            _controller.StateChanged += OnControllerStateChanged;
            Loaded += OnLoaded;
            MouseEnter += OnMouseEnter;
            MouseLeave += OnMouseLeave;
            PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
            PreviewMouseLeftButtonUp += OnPreviewPointerReleased;
            MouseLeave += OnPointerLeave;
            LostMouseCapture += OnPointerLeave;
            PreviewTouchDown += OnPreviewTouchDown;
            PreviewTouchUp += OnPreviewTouchUp;
            IsEnabledChanged += OnIsEnabledChanged;
            SyncControllerFromProperties();
            ApplyChromeState(_controller.GetChromeState());
        }

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public string Caption
        {
            get => (string)GetValue(CaptionProperty);
            set => SetValue(CaptionProperty, value);
        }

        public string Placeholder
        {
            get => (string)GetValue(PlaceholderProperty);
            set => SetValue(PlaceholderProperty, value);
        }

        public string OldValue
        {
            get => (string)GetValue(OldValueProperty);
            set => SetValue(OldValueProperty, value);
        }

        public string ErrorMessage
        {
            get => (string)GetValue(ErrorMessageProperty);
            set => SetValue(ErrorMessageProperty, value);
        }

        public bool IsFieldRequired
        {
            get => (bool)GetValue(IsFieldRequiredProperty);
            set => SetValue(IsFieldRequiredProperty, value);
        }

        public bool ShowOldValueRestoreButton
        {
            get => (bool)GetValue(ShowOldValueRestoreButtonProperty);
            set => SetValue(ShowOldValueRestoreButtonProperty, value);
        }

        public bool IsReadOnly
        {
            get => (bool)GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }

        public FieldChromeMode FieldChromeMode
        {
            get => (FieldChromeMode)GetValue(FieldChromeModeProperty);
            set => SetValue(FieldChromeModeProperty, value);
        }

        public CaptionPlacement CaptionPlacement
        {
            get => (CaptionPlacement)GetValue(CaptionPlacementProperty);
            set => SetValue(CaptionPlacementProperty, value);
        }

        public DensityMode DensityMode
        {
            get => (DensityMode)GetValue(DensityModeProperty);
            set => SetValue(DensityModeProperty, value);
        }

        public InteractionMode InteractionMode
        {
            get => (InteractionMode)GetValue(InteractionModeProperty);
            set => SetValue(InteractionModeProperty, value);
        }

        public int? MaxLength
        {
            get => (int?)GetValue(MaxLengthProperty);
            set => SetValue(MaxLengthProperty, value);
        }

        public RuntimeFieldState RuntimeFieldState
        {
            get => (RuntimeFieldState)GetValue(RuntimeFieldStateProperty);
            set => SetValue(RuntimeFieldStateProperty, value);
        }

        public Thickness EditorMargin
        {
            get => (Thickness)GetValue(EditorMarginProperty);
            private set => SetValue(EditorMarginProperty, value);
        }

        public Thickness EditorPadding
        {
            get => (Thickness)GetValue(EditorPaddingProperty);
            private set => SetValue(EditorPaddingProperty, value);
        }

        public Thickness PlaceholderMargin
        {
            get => (Thickness)GetValue(PlaceholderMarginProperty);
            private set => SetValue(PlaceholderMarginProperty, value);
        }

        public Thickness ClearButtonMargin
        {
            get => (Thickness)GetValue(ClearButtonMarginProperty);
            private set => SetValue(ClearButtonMarginProperty, value);
        }

        public Visibility ClearButtonVisibility
        {
            get => (Visibility)GetValue(ClearButtonVisibilityProperty);
            private set => SetValue(ClearButtonVisibilityProperty, value);
        }

        public Visibility RestoreButtonVisibility
        {
            get => (Visibility)GetValue(RestoreButtonVisibilityProperty);
            private set => SetValue(RestoreButtonVisibilityProperty, value);
        }

        public double ClearButtonWidth
        {
            get => (double)GetValue(ClearButtonWidthProperty);
            private set => SetValue(ClearButtonWidthProperty, value);
        }

        public double ClearButtonHeight
        {
            get => (double)GetValue(ClearButtonHeightProperty);
            private set => SetValue(ClearButtonHeightProperty, value);
        }

        public YamlTextBoxTrailingActionKind TrailingActionKind
        {
            get => (YamlTextBoxTrailingActionKind)GetValue(TrailingActionKindProperty);
            private set => SetValue(TrailingActionKindProperty, value);
        }

        public override void OnApplyTemplate()
        {
            DetachTemplateParts();
            base.OnApplyTemplate();

            _editor = GetTemplateChild(PartEditor) as System.Windows.Controls.TextBox;
            _chromePresenter = GetTemplateChild(PartChromePresenter) as YamlTextBoxSkiaChromePresenter;
            _clearButton = GetTemplateChild(PartClearButton) as ButtonBase;
            _restoreButton = GetTemplateChild(PartRestoreButton) as ButtonBase;

            if (_editor != null)
            {
                _editor.Text = Text ?? string.Empty;
                _editor.MaxLength = MaxLength.GetValueOrDefault(0);
                _editor.TextChanged += OnEditorTextChanged;
                _editor.GotKeyboardFocus += OnEditorGotKeyboardFocus;
                _editor.LostKeyboardFocus += OnEditorLostKeyboardFocus;
            }

            if (_clearButton != null)
            {
                _clearButton.IsTabStop = false;
                _clearButton.Click += OnClearButtonClick;
            }

            if (_restoreButton != null)
            {
                _restoreButton.IsTabStop = false;
                _restoreButton.Click += OnRestoreButtonClick;
            }

            ApplyChromeState(_controller.GetChromeState());
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            _chromePresenter?.InvalidateVisual();
        }

        protected override void OnGotKeyboardFocus(System.Windows.Input.KeyboardFocusChangedEventArgs e)
        {
            base.OnGotKeyboardFocus(e);

            if (!ReferenceEquals(e.OriginalSource, this))
            {
                return;
            }

            if (_editor == null || !_editor.IsEnabled || _editor.IsReadOnly)
            {
                return;
            }

            if (_editor.IsKeyboardFocusWithin)
            {
                return;
            }

            e.Handled = true;
            _editor.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (!_editor.IsKeyboardFocusWithin)
                {
                    _editor.Focus();
                    _editor.CaretIndex = _editor.Text == null ? 0 : _editor.Text.Length;
                }
            }), System.Windows.Threading.DispatcherPriority.Input);
        }

        private static void OnVisualPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (YamlTextBox)d;
            if (!control._isSynchronizing)
            {
                control.SyncControllerFromProperties();
            }
        }

        private static void OnRuntimeFieldStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((YamlTextBox)d).AttachRuntimeFieldState((RuntimeFieldState)e.NewValue);
        }

        private void SyncControllerFromProperties()
        {
            _controller.SetCaption(Caption);
            _controller.SetText(Text);
            _controller.SetPlaceholder(Placeholder);
            _controller.SetOldValue(OldValue);
            _controller.SetErrorMessage(ErrorMessage);
            _controller.SetRequired(IsFieldRequired);
            _controller.SetShowOldValueRestoreButton(ShowOldValueRestoreButton);
            _controller.SetReadOnly(IsReadOnly);
            _controller.SetEnabled(IsEnabled);
            _controller.SetChromeMode(FieldChromeMode);
            _controller.SetCaptionPlacement(CaptionPlacement);
            _controller.SetDensityMode(DensityMode);
            _controller.SetInteractionMode(InteractionMode);
            _controller.HandleThemeChanged(YamlTextBoxUniversalInputBridge.CreateThemeChangedEventArgs("wpf"));
        }

        private void AttachRuntimeFieldState(RuntimeFieldState runtimeFieldState)
        {
            if (_runtimeBinding != null)
            {
                _runtimeBinding.StateChanged -= OnRuntimeBindingStateChanged;
                _runtimeBinding.Dispose();
                _runtimeBinding = null;
            }

            if (runtimeFieldState == null)
            {
                return;
            }

            _runtimeBinding = new YamlTextBoxFieldBinding(runtimeFieldState);
            _runtimeBinding.StateChanged += OnRuntimeBindingStateChanged;
            ApplyRuntimeBindingState(_runtimeBinding.GetState());
        }

        private void ApplyRuntimeBindingState(YamlTextBoxFieldBindingState state)
        {
            if (state == null)
            {
                return;
            }

            try
            {
                _isSynchronizing = true;
                SetCurrentValue(CaptionProperty, state.Caption);
                SetCurrentValue(PlaceholderProperty, state.Placeholder);
                SetCurrentValue(TextProperty, state.Text);
                SetCurrentValue(OldValueProperty, state.OldValue);
                SetCurrentValue(ErrorMessageProperty, state.ErrorMessage);
                SetCurrentValue(IsFieldRequiredProperty, state.IsRequired);
                SetCurrentValue(ShowOldValueRestoreButtonProperty, state.ShowOldValueRestoreButton);
                SetCurrentValue(MaxLengthProperty, state.MaxLength);
                SetCurrentValue(FieldChromeModeProperty, state.FieldChromeMode);
                SetCurrentValue(CaptionPlacementProperty, state.CaptionPlacement);
                SetCurrentValue(InteractionModeProperty, state.InteractionMode);
                SetCurrentValue(DensityModeProperty, state.DensityMode ?? ResolveThemeDefaultDensity());
                if (state.Width.HasValue)
                {
                    ApplyExactWidth(state.Width.Value);
                }
                else if (state.WidthHint.HasValue)
                {
                    ApplyWidthHint(state.WidthHint.Value);
                }
                else
                {
                    ApplyWidthHint(FieldWidthHint.Medium);
                }
                SetCurrentValue(IsEnabledProperty, state.IsEnabled);
                SetCurrentValue(VisibilityProperty, state.IsVisible ? Visibility.Visible : Visibility.Collapsed);
            }
            finally
            {
                _isSynchronizing = false;
            }

            SyncControllerFromProperties();
        }

        private void ApplyExactWidth(double width)
        {
            SetCurrentValue(WidthProperty, width);
            ClearValue(MinWidthProperty);
            ClearValue(MaxWidthProperty);
            SetCurrentValue(HorizontalAlignmentProperty, HorizontalAlignment.Left);
        }

        private void ApplyWidthHint(FieldWidthHint widthHint)
        {
            var token = ResolveFieldWidthToken(widthHint);
            SetCurrentValue(MinWidthProperty, token.MinWidth);

            if (token.MaxWidth.HasValue)
            {
                SetCurrentValue(MaxWidthProperty, token.MaxWidth.Value);
            }
            else
            {
                ClearValue(MaxWidthProperty);
            }

            if (token.Stretch)
            {
                ClearValue(WidthProperty);
                SetCurrentValue(HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
                return;
            }

            if (token.PreferredWidth.HasValue)
            {
                SetCurrentValue(WidthProperty, token.PreferredWidth.Value);
            }
            else
            {
                ClearValue(WidthProperty);
            }

            SetCurrentValue(HorizontalAlignmentProperty, HorizontalAlignment.Left);
        }

        private void ClearWidthConstraints()
        {
            ClearValue(WidthProperty);
            ClearValue(MinWidthProperty);
            ClearValue(MaxWidthProperty);
            ClearValue(HorizontalAlignmentProperty);
        }

        private FieldWidthTokenDefinition ResolveFieldWidthToken(FieldWidthHint widthHint)
        {
            var key = string.Format("FieldWidth.{0}", widthHint);
            if (TryFindResource(key) is FieldWidthTokenDefinition token)
            {
                return token;
            }

            switch (widthHint)
            {
                case FieldWidthHint.Short:
                    return new FieldWidthTokenDefinition { MinWidth = 80d, PreferredWidth = 120d, MaxWidth = 160d, Stretch = false };
                case FieldWidthHint.Medium:
                    return new FieldWidthTokenDefinition { MinWidth = 140d, PreferredWidth = 220d, MaxWidth = 320d, Stretch = false };
                case FieldWidthHint.Long:
                    return new FieldWidthTokenDefinition { MinWidth = 220d, PreferredWidth = 360d, MaxWidth = 520d, Stretch = false };
                case FieldWidthHint.Fill:
                    return new FieldWidthTokenDefinition { MinWidth = 220d, PreferredWidth = 360d, MaxWidth = null, Stretch = true };
                default:
                    return new FieldWidthTokenDefinition { MinWidth = 140d, PreferredWidth = 220d, MaxWidth = 320d, Stretch = false };
            }
        }

        private void ApplyChromeState(YamlTextBoxChromeState chromeState)
        {
            if (chromeState == null)
            {
                return;
            }

            var effectiveChromeState = ApplyPlatformDensityMetrics(chromeState);
            effectiveChromeState = ApplyPlatformInteractionMetrics(effectiveChromeState);
            effectiveChromeState = AdjustChromeStateForCaption(effectiveChromeState);

            Tag = effectiveChromeState;
            SetCurrentValue(MinHeightProperty, effectiveChromeState.LayoutMetrics.MinimumHeight);
            EditorMargin = new Thickness(effectiveChromeState.LayoutMetrics.EditorLeft, effectiveChromeState.LayoutMetrics.EditorTop, effectiveChromeState.LayoutMetrics.EditorRight, effectiveChromeState.LayoutMetrics.EditorBottom);
            TrailingActionKind = effectiveChromeState.TrailingActionKind;
            ClearButtonVisibility = effectiveChromeState.ShowClearButton ? Visibility.Visible : Visibility.Collapsed;
            RestoreButtonVisibility = effectiveChromeState.ShowRestoreOldValueButton ? Visibility.Visible : Visibility.Collapsed;

            if (_chromePresenter != null)
            {
                _chromePresenter.ChromeState = effectiveChromeState;
                _chromePresenter.InvalidateVisual();
            }

            if (_editor != null)
            {
                _editor.FontSize = ResolveEditorFontSize(effectiveChromeState.UsesInlineChrome, 13d);
                _editor.MinHeight = 24d;
                _editor.MaxLength = MaxLength.GetValueOrDefault(0);
                if (_editor.Text != (Text ?? string.Empty))
                {
                    _editor.Text = Text ?? string.Empty;
                }
            }
        }

        private YamlTextBoxChromeState AdjustChromeStateForCaption(YamlTextBoxChromeState chromeState)
        {
            if (chromeState.CaptionPlacement != CaptionPlacement.Left || string.IsNullOrWhiteSpace(chromeState.Caption))
            {
                return chromeState;
            }

            var measuredCaptionWidth = MeasureCaptionWidth(chromeState.Caption);
            var gapWidth = MeasureCaptionWidth("a");
            var captionStart = chromeState.UsesFramedChrome ? 12d : 0d;
            var requiredEditorLeft = Math.Max(chromeState.LayoutMetrics.EditorLeft, captionStart + measuredCaptionWidth + gapWidth);

            if (requiredEditorLeft <= chromeState.LayoutMetrics.EditorLeft)
            {
                return chromeState;
            }

            return new YamlTextBoxChromeState
            {
                Caption = chromeState.Caption,
                Placeholder = chromeState.Placeholder,
                SupportText = chromeState.SupportText,
                Text = chromeState.Text,
                ThemeId = chromeState.ThemeId,
                IsEnabled = chromeState.IsEnabled,
                IsReadOnly = chromeState.IsReadOnly,
                IsRequired = chromeState.IsRequired,
                HasFocus = chromeState.HasFocus,
                HasHover = chromeState.HasHover,
                HasPressed = chromeState.HasPressed,
                HasError = chromeState.HasError,
                ShowClearButton = chromeState.ShowClearButton,
                ShowRestoreOldValueButton = chromeState.ShowRestoreOldValueButton,
                TrailingActionKind = chromeState.TrailingActionKind,
                FieldChromeMode = chromeState.FieldChromeMode,
                CaptionPlacement = chromeState.CaptionPlacement,
                DensityMode = chromeState.DensityMode,
                InteractionMode = chromeState.InteractionMode,
                LayoutMetrics = new YamlTextBoxLayoutMetrics(
                    chromeState.LayoutMetrics.MinimumHeight,
                    requiredEditorLeft,
                    chromeState.LayoutMetrics.EditorTop,
                    chromeState.LayoutMetrics.EditorRight,
                    chromeState.LayoutMetrics.EditorBottom,
                    requiredEditorLeft - gapWidth,
                    chromeState.LayoutMetrics.ClearButtonWidth,
                    chromeState.LayoutMetrics.ClearButtonHeight),
            };
        }

        private double MeasureCaptionWidth(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 0d;
            }

            var dpi = VisualTreeHelper.GetDpi(this);
            var fontFamily = TryFindResource("YamlTextBox.Caption.FontFamily") as FontFamily ?? SystemFonts.MessageFontFamily;
            var fontSize = TryFindResource("YamlTextBox.Caption.FontSize") is double size ? size : 14d;

            var formattedText = new FormattedText(
                text,
                System.Globalization.CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                new Typeface(fontFamily, FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal),
                fontSize,
                Brushes.Black,
                dpi.PixelsPerDip);

            return Math.Ceiling(formattedText.WidthIncludingTrailingWhitespace);
        }

        private double ResolveEditorFontSize(bool usesInlineChrome, double fallback)
        {
            return FindDoubleResource(
                usesInlineChrome ? "YamlTextBox.Inline.Editor.FontSize" : "YamlTextBox.Framed.Editor.FontSize",
                fallback);
        }

        private double FindDoubleResource(string key, double fallback)
        {
            return TryFindResource(key) is double value ? value : fallback;
        }

        private DensityMode ResolveThemeDefaultDensity()
        {
            return DensityMode.Normal;
        }

        private YamlTextBoxChromeState ApplyPlatformDensityMetrics(YamlTextBoxChromeState chromeState)
        {
            var profile = YamlTextBoxDensityMetricsResolver.Resolve(chromeState);
            profile = YamlTextBoxInteractionMetricsResolver.Resolve(chromeState, profile);
            EditorPadding = profile.EditorPadding;
            PlaceholderMargin = profile.PlaceholderMargin;
            ClearButtonMargin = profile.ClearButtonMargin;
            ClearButtonWidth = profile.LayoutMetrics.ClearButtonWidth;
            ClearButtonHeight = profile.LayoutMetrics.ClearButtonHeight;

            return new YamlTextBoxChromeState
            {
                Caption = chromeState.Caption,
                Placeholder = chromeState.Placeholder,
                SupportText = chromeState.SupportText,
                Text = chromeState.Text,
                ThemeId = chromeState.ThemeId,
                IsEnabled = chromeState.IsEnabled,
                IsReadOnly = chromeState.IsReadOnly,
                IsRequired = chromeState.IsRequired,
                HasFocus = chromeState.HasFocus,
                HasHover = chromeState.HasHover,
                HasPressed = chromeState.HasPressed,
                HasError = chromeState.HasError,
                ShowClearButton = chromeState.ShowClearButton,
                ShowRestoreOldValueButton = chromeState.ShowRestoreOldValueButton,
                TrailingActionKind = chromeState.TrailingActionKind,
                FieldChromeMode = chromeState.FieldChromeMode,
                CaptionPlacement = chromeState.CaptionPlacement,
                DensityMode = chromeState.DensityMode,
                InteractionMode = chromeState.InteractionMode,
                LayoutMetrics = profile.LayoutMetrics,
            };
        }

        private YamlTextBoxChromeState ApplyPlatformInteractionMetrics(YamlTextBoxChromeState chromeState)
        {
            return chromeState;
        }

        private void OnControllerStateChanged(object sender, YamlTextBoxChromeState e)
        {
            ApplyChromeState(e);
        }

        private void OnRuntimeBindingStateChanged(object sender, YamlTextBoxFieldBindingState e)
        {
            ApplyRuntimeBindingState(e);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            ApplyChromeState(_controller.GetChromeState());
        }

        private void OnIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            _controller.SetEnabled(IsEnabled);
        }

        private void OnMouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (InteractionMode == InteractionMode.Touch)
            {
                return;
            }

            _controller.SetHover(true);
        }

        private void OnMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            _controller.SetHover(false);
        }

        private void OnPreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (InteractionMode != InteractionMode.Touch)
            {
                return;
            }

            _controller.SetPressed(true);
            FocusEditorFromFieldTap(e.OriginalSource as DependencyObject);
        }

        private void OnPreviewPointerReleased(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _controller.SetPressed(false);
        }

        private void OnPreviewTouchDown(object sender, System.Windows.Input.TouchEventArgs e)
        {
            if (InteractionMode != InteractionMode.Touch)
            {
                return;
            }

            _controller.SetPressed(true);
            FocusEditorFromFieldTap(e.OriginalSource as DependencyObject);
        }

        private void OnPreviewTouchUp(object sender, System.Windows.Input.TouchEventArgs e)
        {
            _controller.SetPressed(false);
        }

        private void OnPointerLeave(object sender, RoutedEventArgs e)
        {
            _controller.SetPressed(false);
        }

        private void FocusEditorFromFieldTap(DependencyObject originalSource)
        {
            if (_editor == null || !_editor.IsEnabled || _editor.IsReadOnly)
            {
                return;
            }

            if (IsDescendantOf(_clearButton, originalSource) || IsDescendantOf(_restoreButton, originalSource))
            {
                return;
            }

            if (_editor.IsKeyboardFocusWithin)
            {
                return;
            }

            _editor.Focus();
            _editor.CaretIndex = _editor.Text == null ? 0 : _editor.Text.Length;
        }

        private static bool IsDescendantOf(DependencyObject ancestor, DependencyObject current)
        {
            while (ancestor != null && current != null)
            {
                if (ReferenceEquals(ancestor, current))
                {
                    return true;
                }

                current = VisualTreeHelper.GetParent(current);
            }

            return false;
        }

        private void OnEditorTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isSynchronizing || _editor == null)
            {
                return;
            }

            try
            {
                _isSynchronizing = true;
                SetCurrentValue(TextProperty, _editor.Text);
                _runtimeBinding?.UpdateText(_editor.Text);
                _controller.HandleTextChanged(YamlTextBoxUniversalInputBridge.CreateTextChangedEventArgs(_editor.Text));
            }
            finally
            {
                _isSynchronizing = false;
            }
        }

        private void OnEditorGotKeyboardFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
        {
            _controller.HandleFocusChanged(YamlTextBoxUniversalInputBridge.CreateFocusChangedEventArgs(true));
        }

        private void OnEditorLostKeyboardFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
        {
            _controller.HandleFocusChanged(YamlTextBoxUniversalInputBridge.CreateFocusChangedEventArgs(false));
        }

        private void OnClearButtonClick(object sender, RoutedEventArgs e)
        {
            var targetText = string.Empty;
            var textBeforeClear = Text ?? string.Empty;

            if (_runtimeBinding != null)
            {
                _runtimeBinding.UpdateText(targetText);
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(textBeforeClear))
                {
                    SetCurrentValue(OldValueProperty, textBeforeClear);
                    SetCurrentValue(ShowOldValueRestoreButtonProperty, true);
                }

                SetCurrentValue(TextProperty, targetText);
                _controller.ClearText();
            }

            if (_editor != null)
            {
                if (_editor.Text != targetText)
                {
                    _editor.Text = targetText;
                }

                _editor.Focus();
                _editor.CaretIndex = _editor.Text == null ? 0 : _editor.Text.Length;
            }
        }

        private void OnRestoreButtonClick(object sender, RoutedEventArgs e)
        {
            var targetText = OldValue ?? string.Empty;

            if (_runtimeBinding != null)
            {
                _runtimeBinding.RestoreOldValue();
            }
            else
            {
                SetCurrentValue(TextProperty, targetText);
                _controller.RestoreOldValue();
            }

            if (_editor != null)
            {
                if (_editor.Text != targetText)
                {
                    _editor.Text = targetText;
                }

                _editor.Focus();
                _editor.CaretIndex = _editor.Text == null ? 0 : _editor.Text.Length;
            }
        }

        private void DetachTemplateParts()
        {
            if (_editor != null)
            {
                _editor.TextChanged -= OnEditorTextChanged;
                _editor.GotKeyboardFocus -= OnEditorGotKeyboardFocus;
                _editor.LostKeyboardFocus -= OnEditorLostKeyboardFocus;
                _editor = null;
            }

            if (_clearButton != null)
            {
                _clearButton.Click -= OnClearButtonClick;
                _clearButton = null;
            }

            if (_restoreButton != null)
            {
                _restoreButton.Click -= OnRestoreButtonClick;
                _restoreButton = null;
            }

            _chromePresenter = null;
        }
    }
}


