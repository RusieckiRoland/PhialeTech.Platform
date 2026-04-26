using PhialeTech.DocumentEditor.Abstractions;
using PhialeTech.DocumentEditor.Wpf.Controls;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace PhialeTech.Components.Wpf
{
    public sealed class DocumentEditorShowcaseView : UserControl, IDisposable, IWebDemoFocusModeSource
    {
        private readonly PhialeDocumentEditor _editor;
        private readonly TextBlock _headlineText;
        private readonly TextBlock _descriptionText;
        private readonly TextBox _resultTextBox;
        private readonly Button _loadHtmlButton;
        private readonly Button _loadMarkdownButton;
        private readonly Button _exportHtmlButton;
        private readonly Button _exportMarkdownButton;
        private readonly Button _saveJsonButton;
        private readonly Button _restoreJsonButton;
        private readonly Button _toggleReadOnlyButton;
        private readonly Button _focusButton;
        private string _savedDocumentJson;
        private string _theme;
        private string _languageCode;
        private bool _opened;
        private bool _disposed;
        private bool _isFocusMode;
        private bool _isReadOnly;

        private event EventHandler<WebDemoFocusModeChangedEventArgs> _focusModeChanged;

        bool IWebDemoFocusModeSource.IsFocusMode => _isFocusMode;
        bool IWebDemoFocusModeSource.ShowPrimaryFocusAction => true;
        string IWebDemoFocusModeSource.PrimaryFocusActionText => "Load HTML";
        Task IWebDemoFocusModeSource.ExecutePrimaryFocusActionAsync() => LoadHtmlSampleAsync();

        event EventHandler<WebDemoFocusModeChangedEventArgs> IWebDemoFocusModeSource.FocusModeChanged
        {
            add => _focusModeChanged += value;
            remove => _focusModeChanged -= value;
        }

        public DocumentEditorShowcaseView(string theme = "light")
        {
            _theme = NormalizeTheme(theme);
            _languageCode = "en";
            _headlineText = new TextBlock
            {
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                Text = "PhialeTech.DocumentEditor",
            };

            _descriptionText = new TextBlock
            {
                Margin = new Thickness(0, 8, 0, 0),
                TextWrapping = TextWrapping.Wrap,
                Text = "MIT-safe Tiptap OSS editor hosted inside the shared WebHost. The sample exercises HTML import/export, Markdown import/export, and native JSON save/load.",
            };

            _editor = new PhialeDocumentEditor(new PhialeTech.WebHost.Wpf.WpfWebComponentHostFactory(), new DocumentEditorOptions
            {
                InitialTheme = _theme,
                InitialLanguageCode = _languageCode,
            })
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            _editor.ContentChanged += HandleEditorContentChanged;

            _loadHtmlButton = CreateButton("Load HTML");
            _loadHtmlButton.Click += async (_, __) => await LoadHtmlSampleAsync().ConfigureAwait(true);

            _loadMarkdownButton = CreateButton("Load Markdown");
            _loadMarkdownButton.Click += async (_, __) => await LoadMarkdownSampleAsync().ConfigureAwait(true);

            _exportHtmlButton = CreateButton("Export HTML");
            _exportHtmlButton.Click += async (_, __) => await ExportHtmlAsync().ConfigureAwait(true);

            _exportMarkdownButton = CreateButton("Export Markdown");
            _exportMarkdownButton.Click += async (_, __) => await ExportMarkdownAsync().ConfigureAwait(true);

            _saveJsonButton = CreateButton("Save JSON");
            _saveJsonButton.Click += async (_, __) => await SaveJsonAsync().ConfigureAwait(true);

            _restoreJsonButton = CreateButton("Restore JSON");
            _restoreJsonButton.Click += async (_, __) => await RestoreJsonAsync().ConfigureAwait(true);

            _toggleReadOnlyButton = CreateButton("Read only: off");
            _toggleReadOnlyButton.Click += async (_, __) => await ToggleReadOnlyAsync().ConfigureAwait(true);

            _focusButton = CreateButton("Focus");
            _focusButton.Click += (_, __) => _editor.FocusEditor();

            var buttonRow = new WrapPanel
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(18, 0, 0, 0),
            };
            buttonRow.Children.Add(_loadHtmlButton);
            buttonRow.Children.Add(_loadMarkdownButton);
            buttonRow.Children.Add(_exportHtmlButton);
            buttonRow.Children.Add(_exportMarkdownButton);
            buttonRow.Children.Add(_saveJsonButton);
            buttonRow.Children.Add(_restoreJsonButton);
            buttonRow.Children.Add(_toggleReadOnlyButton);
            buttonRow.Children.Add(_focusButton);

            var topBar = new Grid
            {
                Margin = new Thickness(0, 0, 0, 12)
            };
            topBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            topBar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titlePanel = new StackPanel();
            titlePanel.Children.Add(_headlineText);
            titlePanel.Children.Add(_descriptionText);
            Grid.SetColumn(titlePanel, 0);
            Grid.SetColumn(buttonRow, 1);
            topBar.Children.Add(titlePanel);
            topBar.Children.Add(buttonRow);

            _resultTextBox = new TextBox
            {
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                IsReadOnly = true,
                MinHeight = 110,
                FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                FontSize = 12,
                Text = "Result channel will show the latest export payload or editor state summary.",
            };

            var resultBorder = new Border
            {
                Margin = new Thickness(0, 0, 0, 12),
                Padding = new Thickness(12),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Child = _resultTextBox
            };
            resultBorder.SetResourceReference(Border.BackgroundProperty, "DemoTokenBackgroundBrush");
            resultBorder.SetResourceReference(Border.BorderBrushProperty, "DemoTokenBorderBrush");

            var editorBorder = new Border
            {
                Padding = new Thickness(0),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Child = _editor
            };
            editorBorder.SetResourceReference(Border.BackgroundProperty, "DemoPanelBackgroundBrush");
            editorBorder.SetResourceReference(Border.BorderBrushProperty, "DemoPanelBorderBrush");

            var root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            Grid.SetRow(topBar, 0);
            Grid.SetRow(resultBorder, 1);
            Grid.SetRow(editorBorder, 2);
            root.Children.Add(topBar);
            root.Children.Add(resultBorder);
            root.Children.Add(editorBorder);

            Content = root;
            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;
            Loaded += HandleLoaded;
            IsKeyboardFocusWithinChanged += HandleIsKeyboardFocusWithinChanged;
        }

        public Task ApplyEnvironmentAsync(string languageCode, string theme)
        {
            _theme = NormalizeTheme(theme);
            _languageCode = NormalizeLanguageCode(languageCode);
            if (_disposed || !_opened)
            {
                return Task.CompletedTask;
            }

            return ApplyEditorEnvironmentAsync();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            Loaded -= HandleLoaded;
            IsKeyboardFocusWithinChanged -= HandleIsKeyboardFocusWithinChanged;
            _editor.ContentChanged -= HandleEditorContentChanged;
            _editor.Dispose();
        }

        void IWebDemoFocusModeSource.ExitFocusMode()
        {
            if (!_isFocusMode)
            {
                return;
            }

            _isFocusMode = false;
            _focusModeChanged?.Invoke(this, new WebDemoFocusModeChangedEventArgs(false));
        }

        private void HandleLoaded(object sender, RoutedEventArgs e)
        {
            if (_opened || _disposed)
            {
                return;
            }

            _opened = true;
            _ = LoadHtmlSampleAsync();
        }

        private async Task LoadHtmlSampleAsync()
        {
            await _editor.InitializeAsync().ConfigureAwait(true);
            await ApplyEditorEnvironmentAsync().ConfigureAwait(true);
            await _editor.SetReadOnlyAsync(false).ConfigureAwait(true);
            _isReadOnly = false;
            UpdateReadOnlyButtonText();
            await _editor.SetToolbarAsync(DocumentEditorToolbarConfig.CreateDefault()).ConfigureAwait(true);
            await _editor.SetHtmlAsync("<h1>Project brief</h1><p><strong>PhialeTech.DocumentEditor</strong> keeps JSON as the primary persistence format while HTML and Markdown stay interchange formats.</p><blockquote>Toolbar layout is configurable and only MIT-safe Tiptap OSS extensions are enabled.</blockquote><ul><li>HTML import/export</li><li>Markdown import/export</li><li>JSON save/load</li></ul>").ConfigureAwait(true);
            _editor.FocusEditor();
            _resultTextBox.Text = "Loaded HTML sample into the editor.";
        }

        private async Task LoadMarkdownSampleAsync()
        {
            await _editor.InitializeAsync().ConfigureAwait(true);
            await ApplyEditorEnvironmentAsync().ConfigureAwait(true);
            await _editor.SetReadOnlyAsync(false).ConfigureAwait(true);
            _isReadOnly = false;
            UpdateReadOnlyButtonText();
            await _editor.SetMarkdownAsync("# Release notes\n\nThis sample was loaded from **Markdown**.\n\n- Paragraphs, headings, and emphasis\n- Lists, blockquotes, and horizontal rules\n- Native JSON persistence for save/load\n").ConfigureAwait(true);
            _editor.FocusEditor();
            _resultTextBox.Text = "Loaded Markdown sample into the editor.";
        }

        private async Task ExportHtmlAsync()
        {
            string html = await _editor.GetHtmlAsync().ConfigureAwait(true);
            _resultTextBox.Text = html;
        }

        private async Task ExportMarkdownAsync()
        {
            string markdown = await _editor.GetMarkdownAsync().ConfigureAwait(true);
            _resultTextBox.Text = markdown;
        }

        private async Task SaveJsonAsync()
        {
            _savedDocumentJson = await _editor.GetDocumentJsonAsync().ConfigureAwait(true);
            _resultTextBox.Text = _savedDocumentJson;
        }

        private async Task RestoreJsonAsync()
        {
            if (string.IsNullOrWhiteSpace(_savedDocumentJson))
            {
                _resultTextBox.Text = "No saved JSON snapshot is available yet. Use Save JSON first.";
                return;
            }

            await _editor.SetDocumentJsonAsync(_savedDocumentJson).ConfigureAwait(true);
            _resultTextBox.Text = "Restored the last saved JSON snapshot into the editor.";
        }

        private async Task ToggleReadOnlyAsync()
        {
            _isReadOnly = !_isReadOnly;
            await _editor.SetReadOnlyAsync(_isReadOnly).ConfigureAwait(true);
            UpdateReadOnlyButtonText();
            _resultTextBox.Text = _isReadOnly
                ? "Editor switched to read-only mode."
                : "Editor switched back to editable mode.";
        }

        private void UpdateReadOnlyButtonText()
        {
            _toggleReadOnlyButton.Content = _isReadOnly ? "Read only: on" : "Read only: off";
        }

        private async Task ApplyEditorEnvironmentAsync()
        {
            await _editor.SetThemeAsync(_theme).ConfigureAwait(true);
            await _editor.SetLanguageAsync(_languageCode).ConfigureAwait(true);
        }

        private void HandleEditorContentChanged(object sender, DocumentEditorContentChangedEventArgs e)
        {
            _resultTextBox.Text = "State: canUndo=" + e.State.CanUndo + ", canRedo=" + e.State.CanRedo + ", isDirty=" + e.State.IsDirty + Environment.NewLine + Environment.NewLine + e.DocumentJson;
        }

        private void HandleIsKeyboardFocusWithinChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            bool isFocusMode = IsKeyboardFocusWithin;
            if (_isFocusMode == isFocusMode)
            {
                return;
            }

            _isFocusMode = isFocusMode;
            _focusModeChanged?.Invoke(this, new WebDemoFocusModeChangedEventArgs(_isFocusMode));
        }

        private static Button CreateButton(string text)
        {
            return new Button
            {
                Content = text,
                Margin = new Thickness(0, 0, 8, 8),
                MinHeight = 34,
                Padding = new Thickness(12, 4, 12, 4)
            };
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
