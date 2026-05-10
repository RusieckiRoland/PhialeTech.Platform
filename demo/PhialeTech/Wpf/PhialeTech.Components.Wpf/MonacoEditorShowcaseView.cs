using PhialeTech.Components.Shared.Services;
using PhialeTech.MonacoEditor.Abstractions;
using PhialeTech.MonacoEditor.Wpf.Controls;
using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PhialeTech.Components.Wpf
{
    public sealed class MonacoEditorShowcaseView : UserControl, IDisposable, IWebDemoFocusModeSource
    {
        private readonly PhialeMonacoEditor _editor;
        private readonly TextBlock _headlineText;
        private readonly TextBlock _descriptionText;
        private readonly Button _loadYamlButton;
        private readonly Button _loadCSharpButton;
        private readonly Button _focusButton;
        private readonly Button _expandButton;
        private readonly StackPanel _titlePanel;
        private readonly Grid _topBar;
        private readonly StackPanel _detailsPanel;
        private readonly Border _surface;
        private string _languageCode;
        private string _theme;
        private bool _opened;
        private bool _disposed;
        private bool _isFocusMode;

        bool IWebDemoFocusModeSource.IsFocusMode => _isFocusMode;
        bool IWebDemoFocusModeSource.ShowPrimaryFocusAction => true;
        string IWebDemoFocusModeSource.PrimaryFocusActionText => MonacoEditorShowcaseTextCatalog.GetText(_languageCode, "LoadYaml");
        Task IWebDemoFocusModeSource.ExecutePrimaryFocusActionAsync() => LoadYamlSampleAsync();
        void IWebDemoFocusModeSource.ExitFocusMode()
        {
            if (!_isFocusMode)
            {
                return;
            }

            _isFocusMode = false;
            UpdateFocusMode();
        }

        event EventHandler<WebDemoFocusModeChangedEventArgs> IWebDemoFocusModeSource.FocusModeChanged
        {
            add => _focusModeChanged += value;
            remove => _focusModeChanged -= value;
        }

        private event EventHandler<WebDemoFocusModeChangedEventArgs> _focusModeChanged;

        public MonacoEditorShowcaseView(string languageCode = "en", string theme = "light")
        {
            _languageCode = MonacoEditorShowcaseTextCatalog.NormalizeLanguage(languageCode);
            _theme = NormalizeTheme(theme);

            _headlineText = new TextBlock
            {
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
            };

            _descriptionText = new TextBlock
            {
                Margin = new Thickness(0, 8, 0, 0),
                TextWrapping = TextWrapping.Wrap,
            };

            _loadYamlButton = CreateButton(string.Empty);
            _loadYamlButton.Click += async (_, __) => await LoadYamlSampleAsync().ConfigureAwait(true);

            _loadCSharpButton = CreateButton(string.Empty);
            _loadCSharpButton.Click += async (_, __) => await LoadCSharpSampleAsync().ConfigureAwait(true);

            _focusButton = CreateButton(string.Empty);
            _focusButton.Click += (_, __) => _editor.FocusEditor();

            _expandButton = CreateButton(string.Empty);
            _expandButton.Click += HandleExpandClick;

            _editor = new PhialeMonacoEditor(
                new PhialeTech.WebHost.Wpf.WpfWebComponentHostFactory(),
                new MonacoEditorOptions
                {
                    InitialTheme = _theme,
                    InitialLanguage = "yaml",
                    InitialValue = DemoMonacoEditorSampleBuilder.CreateYamlSample(),
                })
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            AttachDiagnostics();

            var buttonRow = new WrapPanel
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(18, 0, 0, 0),
                ItemHeight = 36,
                ItemWidth = double.NaN
            };
            buttonRow.Children.Add(_loadYamlButton);
            buttonRow.Children.Add(_loadCSharpButton);
            buttonRow.Children.Add(_focusButton);
            buttonRow.Children.Add(_expandButton);

            _titlePanel = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                Children =
                {
                    _headlineText
                }
            };

            _topBar = new Grid
            {
                Margin = new Thickness(0, 0, 0, 12)
            };
            _topBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            _topBar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            Grid.SetColumn(_titlePanel, 0);
            Grid.SetColumn(buttonRow, 1);
            _topBar.Children.Add(_titlePanel);
            _topBar.Children.Add(buttonRow);

            _detailsPanel = new StackPanel
            {
                Margin = new Thickness(0, 0, 0, 12),
                Children =
                {
                    _descriptionText
                }
            };

            _surface = new Border
            {
                Margin = new Thickness(0, 12, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                ClipToBounds = true,
                Child = _editor
            };

            var root = new Grid
            {
                ClipToBounds = true
            };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            Grid.SetRow(_topBar, 0);
            Grid.SetRow(_detailsPanel, 1);
            Grid.SetRow(_surface, 2);
            root.Children.Add(_topBar);
            root.Children.Add(_detailsPanel);
            root.Children.Add(_surface);

            Content = root;
            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;
            Loaded += HandleLoaded;
            PreviewTextInput += HandlePreviewTextInput;
            GotKeyboardFocus += HandleGotKeyboardFocus;
            LostKeyboardFocus += HandleLostKeyboardFocus;
            PreviewKeyDown += HandlePreviewKeyDown;
            IsKeyboardFocusWithinChanged += HandleIsKeyboardFocusWithinChanged;
            UpdateLocalizedTexts();
            UpdateFocusMode();
        }

        public async Task ApplyEnvironmentAsync(string languageCode, string theme)
        {
            _languageCode = MonacoEditorShowcaseTextCatalog.NormalizeLanguage(languageCode);
            _theme = NormalizeTheme(theme);
            UpdateLocalizedTexts();

            if (_disposed || !_opened)
            {
                return;
            }

            await _editor.SetThemeAsync(_theme).ConfigureAwait(true);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            Loaded -= HandleLoaded;
            PreviewTextInput -= HandlePreviewTextInput;
            GotKeyboardFocus -= HandleGotKeyboardFocus;
            LostKeyboardFocus -= HandleLostKeyboardFocus;
            PreviewKeyDown -= HandlePreviewKeyDown;
            IsKeyboardFocusWithinChanged -= HandleIsKeyboardFocusWithinChanged;
            _expandButton.Click -= HandleExpandClick;
            _editor.Dispose();
        }

        private void HandleLoaded(object sender, RoutedEventArgs e)
        {
            if (_opened || _disposed)
            {
                return;
            }

            _opened = true;
            Log("Loaded opened=true");
            _ = LoadYamlSampleAsync();
        }

        private async Task LoadYamlSampleAsync()
        {
            try
            {
                Log("LoadYamlSampleAsync start");
                await _editor.InitializeAsync().ConfigureAwait(true);
                Log("InitializeAsync completed");
                await _editor.SetThemeAsync(_theme).ConfigureAwait(true);
                Log("SetThemeAsync completed theme=" + _theme);
                await _editor.SetLanguageAsync("yaml").ConfigureAwait(true);
                Log("SetLanguageAsync completed language=yaml");
                await _editor.SetValueAsync(DemoMonacoEditorSampleBuilder.CreateYamlSample()).ConfigureAwait(true);
                Log("SetValueAsync completed");
                _editor.FocusEditor();
                Log("FocusEditor requested");
            }
            catch (Exception ex)
            {
                Log("LoadYamlSampleAsync failed " + ex.Message);
                _descriptionText.Text = MonacoEditorShowcaseTextCatalog.GetText(_languageCode, "LoadError") + " " + ex.Message;
            }
        }

        private async Task LoadCSharpSampleAsync()
        {
            try
            {
                Log("LoadCSharpSampleAsync start");
                await _editor.InitializeAsync().ConfigureAwait(true);
                await _editor.SetThemeAsync(_theme).ConfigureAwait(true);
                await _editor.SetLanguageAsync("csharp").ConfigureAwait(true);
                await _editor.SetValueAsync(DemoMonacoEditorSampleBuilder.CreateCSharpSample()).ConfigureAwait(true);
                _editor.FocusEditor();
                Log("LoadCSharpSampleAsync complete and focus requested");
            }
            catch (Exception ex)
            {
                Log("LoadCSharpSampleAsync failed " + ex.Message);
                _descriptionText.Text = MonacoEditorShowcaseTextCatalog.GetText(_languageCode, "LoadError") + " " + ex.Message;
            }
        }

        private void HandleExpandClick(object sender, RoutedEventArgs e)
        {
            _isFocusMode = !_isFocusMode;
            Log("Expand clicked focusMode=" + _isFocusMode);
            UpdateFocusMode();
        }

        private void HandlePreviewKeyDown(object sender, KeyEventArgs e)
        {
            Log("PreviewKeyDown key=" + e.Key + " handled=" + e.Handled + " focusMode=" + _isFocusMode);
            if (!_isFocusMode || e.Key != Key.Escape)
            {
                return;
            }

            _isFocusMode = false;
            UpdateFocusMode();
            e.Handled = true;
        }

        private void HandlePreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Log("PreviewTextInput text=" + MonacoInputTrace.SafeSnippet(e.Text) + " handled=" + e.Handled);
        }

        private void HandleGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            Log("GotKeyboardFocus original=" + DescribeElement(e.OriginalSource as DependencyObject));
        }

        private void HandleLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            Log("LostKeyboardFocus original=" + DescribeElement(e.OriginalSource as DependencyObject));
        }

        private void HandleIsKeyboardFocusWithinChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Log("IsKeyboardFocusWithin=" + IsKeyboardFocusWithin);
        }

        private void UpdateLocalizedTexts()
        {
            _headlineText.Text = MonacoEditorShowcaseTextCatalog.GetText(_languageCode, "Headline");
            _descriptionText.Text = MonacoEditorShowcaseTextCatalog.GetText(_languageCode, "Description");
            _loadYamlButton.Content = MonacoEditorShowcaseTextCatalog.GetText(_languageCode, "LoadYaml");
            _loadCSharpButton.Content = MonacoEditorShowcaseTextCatalog.GetText(_languageCode, "LoadCSharp");
            _focusButton.Content = MonacoEditorShowcaseTextCatalog.GetText(_languageCode, "Focus");
            _expandButton.Content = _isFocusMode
                ? MonacoEditorShowcaseTextCatalog.GetText(_languageCode, "ExitFocus")
                : MonacoEditorShowcaseTextCatalog.GetText(_languageCode, "Expand");
        }

        private void UpdateFocusMode()
        {
            _titlePanel.Visibility = _isFocusMode ? Visibility.Collapsed : Visibility.Visible;
            _topBar.Visibility = _isFocusMode ? Visibility.Collapsed : Visibility.Visible;
            _detailsPanel.Visibility = _isFocusMode ? Visibility.Collapsed : Visibility.Visible;
            _topBar.Margin = _isFocusMode ? new Thickness(0, 0, 0, 8) : new Thickness(0, 0, 0, 12);
            _surface.Margin = _isFocusMode ? new Thickness(0) : new Thickness(0, 12, 0, 0);
            UpdateLocalizedTexts();
            _focusModeChanged?.Invoke(this, new WebDemoFocusModeChangedEventArgs(_isFocusMode));
        }

        private void AttachDiagnostics()
        {
            _editor.ReadyStateChanged += (_, args) => Log("Editor ReadyStateChanged initialized=" + args.IsInitialized + " ready=" + args.IsReady);
            _editor.ContentChanged += (_, args) => Log("Editor ContentChanged length=" + (args.Value ?? string.Empty).Length + " snippet=" + MonacoInputTrace.SafeSnippet(args.Value));
            _editor.ErrorOccurred += (_, args) => Log("Editor ErrorOccurred message=" + args.Message + " detail=" + args.Detail);
            _editor.Loaded += (_, __) => Log("Editor Loaded");
            _editor.Unloaded += (_, __) => Log("Editor Unloaded");
            _editor.PreviewMouseDown += (_, args) => Log("Editor PreviewMouseDown button=" + args.ChangedButton);
            _editor.PreviewKeyDown += (_, args) => Log("Editor PreviewKeyDown key=" + args.Key + " handled=" + args.Handled);
            _editor.PreviewKeyUp += (_, args) => Log("Editor PreviewKeyUp key=" + args.Key + " handled=" + args.Handled);
            _editor.PreviewTextInput += (_, args) => Log("Editor PreviewTextInput text=" + MonacoInputTrace.SafeSnippet(args.Text) + " handled=" + args.Handled);
            _editor.GotKeyboardFocus += (_, args) => Log("Editor GotKeyboardFocus original=" + DescribeElement(args.OriginalSource as DependencyObject));
            _editor.LostKeyboardFocus += (_, args) => Log("Editor LostKeyboardFocus original=" + DescribeElement(args.OriginalSource as DependencyObject));
            _editor.IsKeyboardFocusWithinChanged += (_, __) => Log("Editor IsKeyboardFocusWithin=" + _editor.IsKeyboardFocusWithin);

            var hostField = typeof(PhialeMonacoEditor).GetField("_host", BindingFlags.Instance | BindingFlags.NonPublic);
            var hostElement = hostField?.GetValue(_editor) as UIElement;
            if (hostElement == null)
            {
                Log("Editor host reflection failed");
                return;
            }

            Log("Editor host type=" + hostElement.GetType().FullName);
            hostElement.PreviewMouseDown += (_, args) => Log("Host PreviewMouseDown button=" + args.ChangedButton);
            hostElement.PreviewKeyDown += (_, args) => Log("Host PreviewKeyDown key=" + args.Key + " handled=" + args.Handled);
            hostElement.PreviewKeyUp += (_, args) => Log("Host PreviewKeyUp key=" + args.Key + " handled=" + args.Handled);
            hostElement.PreviewTextInput += (_, args) => Log("Host PreviewTextInput text=" + MonacoInputTrace.SafeSnippet(args.Text) + " handled=" + args.Handled);
            hostElement.GotKeyboardFocus += (_, args) => Log("Host GotKeyboardFocus original=" + DescribeElement(args.OriginalSource as DependencyObject));
            hostElement.LostKeyboardFocus += (_, args) => Log("Host LostKeyboardFocus original=" + DescribeElement(args.OriginalSource as DependencyObject));
            hostElement.IsKeyboardFocusWithinChanged += (_, __) => Log("Host IsKeyboardFocusWithin=" + hostElement.IsKeyboardFocusWithin);
        }

        private void Log(string message)
        {
            MonacoInputTrace.Write("monaco.showcase", "MonacoEditorShowcaseView", message);
        }

        private static string DescribeElement(DependencyObject element)
        {
            if (element == null)
            {
                return "null";
            }

            if (element is FrameworkElement frameworkElement)
            {
                return frameworkElement.GetType().Name + "#" + (frameworkElement.Name ?? string.Empty);
            }

            return element.GetType().Name;
        }

        private static string NormalizeTheme(string theme)
        {
            return string.Equals(theme, "dark", StringComparison.OrdinalIgnoreCase) ? "dark" : "light";
        }

        private static Button CreateButton(string text)
        {
            return new Button
            {
                Margin = new Thickness(0, 0, 8, 0),
                Padding = new Thickness(12, 6, 12, 6),
                MinWidth = 92,
                Content = text
            };
        }
    }
}

