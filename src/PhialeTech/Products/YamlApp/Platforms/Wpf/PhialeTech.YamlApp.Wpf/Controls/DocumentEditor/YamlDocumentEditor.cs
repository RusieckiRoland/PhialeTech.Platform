using PhialeTech.DocumentEditor.Abstractions;
using PhialeTech.DocumentEditor.Wpf.Controls;
using PhialeTech.YamlApp.Abstractions.Enums;
using PhialeTech.YamlApp.Runtime.Controls.DocumentEditor;
using PhialeTech.YamlApp.Runtime.Model;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PhialeTech.YamlApp.Wpf.Controls.DocumentEditor
{
    public sealed class YamlDocumentEditor : UserControl
    {
        public static readonly DependencyProperty RuntimeFieldStateProperty =
            DependencyProperty.Register(nameof(RuntimeFieldState), typeof(RuntimeFieldState), typeof(YamlDocumentEditor), new PropertyMetadata(null, OnRuntimeFieldStateChanged));

        public static readonly DependencyProperty ThemeProperty =
            DependencyProperty.Register(nameof(Theme), typeof(string), typeof(YamlDocumentEditor), new PropertyMetadata("light", OnThemeChanged));

        public static readonly DependencyProperty LanguageCodeProperty =
            DependencyProperty.Register(nameof(LanguageCode), typeof(string), typeof(YamlDocumentEditor), new PropertyMetadata("en", OnLanguageCodeChanged));

        private readonly TextBlock _captionText;
        private readonly Border _fieldChromeBorder;
        private readonly PhialeDocumentEditor _editor;
        private readonly TextBlock _supportText;
        private readonly Grid _contentPanel;
        private readonly Grid _root;
        private YamlDocumentEditorFieldBinding _runtimeBinding;
        private bool _isSynchronizing;
        private bool _isSynchronizingTheme;

        public YamlDocumentEditor()
        {
            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;
            SnapsToDevicePixels = true;
            UseLayoutRounding = true;

            _captionText = new TextBlock
            {
                Margin = new Thickness(0, 0, 0, 8),
                FontWeight = FontWeights.SemiBold,
                SnapsToDevicePixels = true,
                UseLayoutRounding = true,
            };
            _captionText.SetResourceReference(ForegroundProperty, "Brush.Text.Primary");

            _editor = new PhialeDocumentEditor(new PhialeTech.WebHost.Wpf.WpfWebComponentHostFactory(), new DocumentEditorOptions
            {
                OverlayMode = DocumentEditorOverlayMode.Container
            });
            _editor.HorizontalAlignment = HorizontalAlignment.Stretch;
            _editor.VerticalAlignment = VerticalAlignment.Stretch;
            _editor.MinWidth = 0d;
            _editor.MinHeight = 220d;
            _editor.ContentChanged += HandleEditorContentChanged;
            _editor.ThemeChanged += HandleEditorThemeChanged;

            _supportText = new TextBlock
            {
                Margin = new Thickness(0, 8, 0, 0),
                Visibility = Visibility.Collapsed,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center,
                SnapsToDevicePixels = true,
                UseLayoutRounding = true,
            };
            _supportText.SetResourceReference(ForegroundProperty, "Brush.Text.Secondary");

            _contentPanel = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                SnapsToDevicePixels = true,
                UseLayoutRounding = true,
            };
            _contentPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            _contentPanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1d, GridUnitType.Star) });
            _contentPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            Grid.SetRow(_captionText, 0);
            Grid.SetRow(_editor, 1);
            Grid.SetRow(_supportText, 2);
            _contentPanel.Children.Add(_captionText);
            _contentPanel.Children.Add(_editor);
            _contentPanel.Children.Add(_supportText);

            _fieldChromeBorder = new Border
            {
                MinWidth = 0d,
                Padding = new Thickness(12),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                SnapsToDevicePixels = true,
                UseLayoutRounding = true,
                Visibility = Visibility.Collapsed,
            };

            _root = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                SnapsToDevicePixels = true,
                UseLayoutRounding = true,
            };
            _root.Children.Add(_fieldChromeBorder);
            _root.Children.Add(_contentPanel);

            Content = _root;
            Loaded += HandleLoaded;
        }

        public RuntimeFieldState RuntimeFieldState
        {
            get => (RuntimeFieldState)GetValue(RuntimeFieldStateProperty);
            set => SetValue(RuntimeFieldStateProperty, value);
        }

        public string Theme
        {
            get => (string)GetValue(ThemeProperty);
            set => SetValue(ThemeProperty, value);
        }

        public string LanguageCode
        {
            get => (string)GetValue(LanguageCodeProperty);
            set => SetValue(LanguageCodeProperty, value);
        }

        public Task ApplyExternalThemeAsync(string theme)
        {
            var normalizedTheme = NormalizeTheme(theme);
            SetCurrentValue(ThemeProperty, normalizedTheme);
            return ApplyThemeCoreAsync(normalizedTheme, true);
        }

        public Task ApplyExternalLanguageAsync(string languageCode)
        {
            var normalizedLanguageCode = NormalizeLanguageCode(languageCode);
            SetCurrentValue(LanguageCodeProperty, normalizedLanguageCode);
            return ApplyLanguageCoreAsync(normalizedLanguageCode);
        }

        private static void OnRuntimeFieldStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((YamlDocumentEditor)d).AttachRuntimeFieldState((RuntimeFieldState)e.NewValue);
        }

        private static void OnThemeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((YamlDocumentEditor)d).ApplyThemeAsync((string)e.NewValue);
        }

        private static void OnLanguageCodeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((YamlDocumentEditor)d).ApplyLanguageAsync((string)e.NewValue);
        }

        private async void HandleLoaded(object sender, RoutedEventArgs e)
        {
            await _editor.InitializeAsync().ConfigureAwait(true);
            await ApplyThemeCoreAsync(Theme, true).ConfigureAwait(true);
            await ApplyLanguageCoreAsync(LanguageCode).ConfigureAwait(true);
        }

        private void AttachRuntimeFieldState(RuntimeFieldState runtimeFieldState)
        {
            if (_runtimeBinding != null)
            {
                _runtimeBinding.StateChanged -= HandleRuntimeBindingStateChanged;
                _runtimeBinding.Dispose();
                _runtimeBinding = null;
            }

            if (runtimeFieldState == null)
            {
                return;
            }

            _runtimeBinding = new YamlDocumentEditorFieldBinding(runtimeFieldState);
            _runtimeBinding.StateChanged += HandleRuntimeBindingStateChanged;
            ApplyRuntimeState(_runtimeBinding.GetState());
        }

        private async void ApplyRuntimeState(YamlDocumentEditorFieldBindingState state)
        {
            if (state == null)
            {
                return;
            }

            _captionText.Text = state.Caption;
            _captionText.Visibility = string.IsNullOrWhiteSpace(state.Caption) ? Visibility.Collapsed : Visibility.Visible;
            _supportText.Text = state.ErrorMessage;
            var hasError = !string.IsNullOrWhiteSpace(state.ErrorMessage);
            _supportText.Visibility = hasError ? Visibility.Visible : Visibility.Collapsed;
            IsEnabled = state.IsEnabled;
            Visibility = state.IsVisible ? Visibility.Visible : Visibility.Collapsed;

            ApplyChrome(state.FieldChromeMode, hasError);

            try
            {
                _isSynchronizing = true;
                await _editor.SetOverlayModeAsync(state.OverlayMode).ConfigureAwait(true);
                await _editor.SetReadOnlyAsync(!state.IsEnabled).ConfigureAwait(true);
                if (!string.IsNullOrWhiteSpace(state.DocumentJson))
                {
                    await _editor.SetDocumentJsonAsync(state.DocumentJson).ConfigureAwait(true);
                }
                else
                {
                    await _editor.SetDocumentJsonAsync(string.Empty).ConfigureAwait(true);
                }
            }
            finally
            {
                _isSynchronizing = false;
            }
        }

        private void ApplyChrome(FieldChromeMode chromeMode, bool hasError)
        {
            _fieldChromeBorder.Visibility = chromeMode == FieldChromeMode.Framed ? Visibility.Visible : Visibility.Collapsed;

            if (chromeMode == FieldChromeMode.Framed)
            {
                if (_contentPanel.Parent == _root)
                {
                    _root.Children.Remove(_contentPanel);
                }

                if (!ReferenceEquals(_fieldChromeBorder.Child, _contentPanel))
                {
                    _fieldChromeBorder.Child = _contentPanel;
                }

                _fieldChromeBorder.SetResourceReference(Border.BackgroundProperty, "Brush.Surface1");
                _fieldChromeBorder.BorderThickness = new Thickness(1);
                _fieldChromeBorder.CornerRadius = new CornerRadius(12);
                _fieldChromeBorder.SetResourceReference(Border.BorderBrushProperty, hasError ? "Brush.Danger.Border" : "Brush.Border.Default");
                _supportText.SetResourceReference(ForegroundProperty, hasError ? "Brush.Danger.Text" : "Brush.Text.Secondary");
                return;
            }

            if (ReferenceEquals(_fieldChromeBorder.Child, _contentPanel))
            {
                _fieldChromeBorder.Child = null;
            }

            if (!_root.Children.Contains(_contentPanel))
            {
                _root.Children.Add(_contentPanel);
            }

            _supportText.SetResourceReference(ForegroundProperty, hasError ? "Brush.Danger.Text" : "Brush.Text.Secondary");
        }

        private void HandleRuntimeBindingStateChanged(object sender, YamlDocumentEditorFieldBindingState e)
        {
            ApplyRuntimeState(e);
        }

        private void HandleEditorContentChanged(object sender, DocumentEditorContentChangedEventArgs e)
        {
            if (_isSynchronizing || _runtimeBinding == null)
            {
                return;
            }

            _runtimeBinding.UpdateDocumentJson(e.DocumentJson);
        }

        private void HandleEditorThemeChanged(object sender, string theme)
        {
            var normalizedTheme = NormalizeTheme(theme);
            if (string.Equals(NormalizeTheme(Theme), normalizedTheme, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _isSynchronizingTheme = true;
            try
            {
                SetCurrentValue(ThemeProperty, normalizedTheme);
            }
            finally
            {
                _isSynchronizingTheme = false;
            }
        }

        private async void ApplyThemeAsync(string theme)
        {
            await ApplyThemeCoreAsync(theme, false).ConfigureAwait(true);
        }

        private async void ApplyLanguageAsync(string languageCode)
        {
            await ApplyLanguageCoreAsync(languageCode).ConfigureAwait(true);
        }

        private async Task ApplyThemeCoreAsync(string theme, bool forceRefresh)
        {
            if (!IsLoaded)
            {
                return;
            }

            var normalizedTheme = NormalizeTheme(theme);
            if (!forceRefresh && _isSynchronizingTheme && string.Equals(_editor.Theme, normalizedTheme, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            await _editor.SetThemeAsync(normalizedTheme).ConfigureAwait(true);
        }

        private async Task ApplyLanguageCoreAsync(string languageCode)
        {
            if (!IsLoaded)
            {
                return;
            }

            var normalizedLanguageCode = NormalizeLanguageCode(languageCode);
            await _editor.SetLanguageAsync(normalizedLanguageCode).ConfigureAwait(true);
        }

        private static string NormalizeTheme(string theme)
        {
            return string.Equals(theme, "dark", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(theme, "night", StringComparison.OrdinalIgnoreCase)
                ? "dark"
                : "light";
        }

        private static string NormalizeLanguageCode(string languageCode)
        {
            return string.Equals(languageCode, "pl", StringComparison.OrdinalIgnoreCase)
                ? "pl"
                : "en";
        }
    }
}
