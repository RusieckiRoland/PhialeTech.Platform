using System;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using PhialeGrid.Core.Columns;
using PhialeGrid.Core.Surface;
using PhialeTech.Styles.Wpf;
using PhialeTech.PhialeGrid.Wpf.Controls.Editing;
using PhialeTech.PhialeGrid.Wpf.Diagnostics;
using UniversalInput.Contracts;

namespace PhialeTech.PhialeGrid.Wpf.Surface.Presenters
{
    /// <summary>
    /// Presenter dla jednej komórki grida.
    /// Służy tylko do wyświetlenia - logika selection, edit itp. jest w Core.
    /// </summary>
    public sealed class GridCellPresenter : ContentControl
    {
        private const string ChromeInlineActionButtonTemplateResourceKey = "ChromeInlineActionButtonTemplate";
        private const string ChromeHostedTextBoxTemplateResourceKey = "ChromeHostedTextBoxTemplate";
        private const string PgEditingComboBoxTemplateResourceKey = "PgEditingComboBoxTemplate";
        private const string PgEditingComboBoxItemStyleResourceKey = "PgEditingComboBoxItemStyle";
        private const string PgEditingCheckBoxStyleResourceKey = "PgEditingCheckBoxStyle";
        private const string CalendarGlyphText = "\uE787";
        private TextBox _editingTextBox;
        private ComboBox _comboEditor;
        private ComboBox _autocompleteEditor;
        private DatePicker _datePickerEditor;
        private CheckBox _checkBoxEditor;
        private bool _isSynchronizingEditorState;
        private Border _editingChromeHost;
        private ScaleTransform _editingChromeScaleTransform;
        private bool _wasEditingInPreviousSnapshot;
        private string _pendingSelectionEchoValue;
        private string _openedDropDownSelectionText;
        private bool _selectionCommittedDuringCurrentDropDown;
        private UIElement _selectablePopupLoggingRoot;

        public GridCellPresenter()
        {
            this.SetValue(ClipToBoundsProperty, false);
            this.SetResourceReference(StyleProperty, "PgGridSurfaceCellPresenterStyle");
            this.SetResourceReference(BackgroundProperty, "PgRowBackgroundBrush");
            this.SetResourceReference(ForegroundProperty, "PgPrimaryTextBrush");
            this.SetResourceReference(BorderBrushProperty, "PgGridLineBrush");
            this.SetValue(BorderThicknessProperty, new Thickness(0.5));
            this.SetValue(HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch);
            this.SetValue(VerticalContentAlignmentProperty, VerticalAlignment.Center);
        }

        /// <summary>
        /// Dane komórki z snapshotu.
        /// </summary>
        public GridCellSurfaceItem CellData
        {
            get { return (GridCellSurfaceItem)GetValue(CellDataProperty); }
            set { SetValue(CellDataProperty, value); }
        }

        public static readonly DependencyProperty CellDataProperty =
            DependencyProperty.Register(
                nameof(CellData),
                typeof(GridCellSurfaceItem),
                typeof(GridCellPresenter),
                new PropertyMetadata(null, OnCellDataChanged));

        private static void OnCellDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var presenter = (GridCellPresenter)d;
            var cellData = (GridCellSurfaceItem)e.NewValue;

            if (cellData == null)
            {
                presenter.Content = null;
                AutomationProperties.SetName(presenter, string.Empty);
                AutomationProperties.SetAutomationId(presenter, string.Empty);
                return;
            }

            AutomationProperties.SetAutomationId(presenter, "surface.cell." + cellData.RowKey + "." + cellData.ColumnKey);
            AutomationProperties.SetName(
                presenter,
                string.IsNullOrWhiteSpace(cellData.DisplayText)
                    ? cellData.ColumnKey
                    : cellData.ColumnKey + ": " + cellData.DisplayText);
            presenter.UpdateContent(cellData);

            // Ustawiam visual state
            presenter.UpdateVisualState(cellData);
        }

        internal void FocusEditor()
        {
            if (CellData?.IsEditing != true)
            {
                return;
            }

            switch (CellData.EditorKind)
            {
                case GridColumnEditorKind.Combo:
                    if (_comboEditor != null)
                    {
                        _comboEditor.Focus();
                        if (_comboEditor.IsEditable)
                        {
                            SelectEditableComboBoxText(_comboEditor);
                        }
                    }

                    break;
                case GridColumnEditorKind.Autocomplete:
                    if (_autocompleteEditor != null)
                    {
                        _autocompleteEditor.Focus();
                        if (_autocompleteEditor.IsEditable)
                        {
                            SelectEditableComboBoxText(_autocompleteEditor);
                        }
                    }

                    break;
                case GridColumnEditorKind.DatePicker:
                    _datePickerEditor?.Focus();
                    break;
                case GridColumnEditorKind.CheckBox:
                    _checkBoxEditor?.Focus();
                    break;
                default:
                    if (_editingTextBox != null)
                    {
                        _editingTextBox.Focus();
                        _editingTextBox.SelectAll();
                    }

                    break;
            }
        }

        public event EventHandler<GridCellEditingTextChangedEventArgs> EditingTextChanged;

        private void UpdateContent(GridCellSurfaceItem cellData)
        {
            if (!cellData.IsEditing)
            {
                _wasEditingInPreviousSnapshot = false;
                if (cellData.IsGroupCaptionCell)
                {
                    Content = CreateGroupCaptionContent(cellData);
                    return;
                }

                Content = CreateDisplayContent(cellData);
                return;
            }

            FrameworkElement editor;
            switch (cellData.EditorKind)
            {
                case GridColumnEditorKind.Combo:
                    editor = ConfigureComboEditor(cellData);
                    break;
                case GridColumnEditorKind.Autocomplete:
                    editor = ConfigureAutocompleteEditor(cellData);
                    break;
                case GridColumnEditorKind.DatePicker:
                    editor = ConfigureDatePickerEditor(cellData);
                    break;
                case GridColumnEditorKind.MaskedText:
                    editor = ConfigureTextEditor(cellData, useMask: true);
                    break;
                case GridColumnEditorKind.CheckBox:
                    editor = ConfigureCheckBoxEditor(cellData);
                    break;
                default:
                    editor = ConfigureTextEditor(cellData, useMask: false);
                    break;
            }

            Content = CreateEditingChrome(cellData, editor);
            _wasEditingInPreviousSnapshot = true;
        }

        private void HandleEditingTextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isSynchronizingEditorState || !(sender is TextBox textBox) || CellData == null || !CellData.IsEditing)
            {
                return;
            }

            RaiseEditorValueChanged(textBox.Text, UniversalEditorValueChangeKind.TextEdited);
        }

        private void HandleEditingTextBoxRequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
        {
            if (CellData?.IsEditing != true || e == null)
            {
                return;
            }

            // The cell is already visible when the in-place editor is realized.
            // Letting TextBox request BringIntoView causes the outer grid viewport to jump horizontally.
            e.Handled = true;
        }

        private void HandleComboEditorSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isSynchronizingEditorState || !(sender is ComboBox comboBox) || CellData?.IsEditing != true)
            {
                return;
            }

            var selectedText = comboBox.SelectedItem as string ?? comboBox.Text;
            PhialeGridDiagnostics.Write(
                "GridCellPresenter",
                $"Combo selection changed. Row='{CellData?.RowKey}', Column='{CellData?.ColumnKey}', Selected='{selectedText ?? string.Empty}', IsEditable={comboBox.IsEditable}.");
            if (comboBox.IsEditable &&
                !string.Equals(comboBox.Text, selectedText ?? string.Empty, StringComparison.Ordinal))
            {
                _isSynchronizingEditorState = true;
                try
                {
                    comboBox.Text = selectedText ?? string.Empty;
                }
                finally
                {
                    _isSynchronizingEditorState = false;
                }
            }

            _selectionCommittedDuringCurrentDropDown = true;
            RaiseEditorValueChanged(selectedText, UniversalEditorValueChangeKind.SelectionCommitted);
        }

        private void HandleSelectableEditorTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isSynchronizingEditorState || !(sender is ComboBox comboBox) || CellData?.IsEditing != true)
            {
                return;
            }

            if (!comboBox.IsEditable)
            {
                PhialeGridDiagnostics.Write(
                    "GridCellPresenter",
                    $"Selectable editor text change ignored because editor is not editable. Row='{CellData?.RowKey}', Column='{CellData?.ColumnKey}', Text='{comboBox.Text ?? string.Empty}'.");
                return;
            }

            if (ShouldSuppressSelectionEcho(comboBox.Text))
            {
                return;
            }

            var selectedText = comboBox.SelectedItem as string;
            if (string.IsNullOrWhiteSpace(comboBox.Text) && !string.IsNullOrWhiteSpace(selectedText))
            {
                return;
            }

            RaiseEditorValueChanged(comboBox.Text, UniversalEditorValueChangeKind.TextEdited);
        }

        private void HandleAutocompleteEditorSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isSynchronizingEditorState || !(sender is ComboBox comboBox) || CellData?.IsEditing != true)
            {
                return;
            }

            var selectedText = comboBox.SelectedItem as string;
            if (string.IsNullOrWhiteSpace(selectedText))
            {
                PhialeGridDiagnostics.Write(
                    "GridCellPresenter",
                    $"Autocomplete selection changed but selected item is empty. Row='{CellData?.RowKey}', Column='{CellData?.ColumnKey}', Text='{comboBox.Text ?? string.Empty}', IsEditable={comboBox.IsEditable}.");
                return;
            }

            PhialeGridDiagnostics.Write(
                "GridCellPresenter",
                $"Autocomplete selection changed. Row='{CellData?.RowKey}', Column='{CellData?.ColumnKey}', Selected='{selectedText}', IsEditable={comboBox.IsEditable}.");

            if (comboBox.IsEditable &&
                !string.Equals(comboBox.Text, selectedText, StringComparison.Ordinal))
            {
                _isSynchronizingEditorState = true;
                try
                {
                    comboBox.Text = selectedText;
                }
                finally
                {
                    _isSynchronizingEditorState = false;
                }
            }

            _selectionCommittedDuringCurrentDropDown = true;
            RaiseEditorValueChanged(selectedText, UniversalEditorValueChangeKind.SelectionCommitted);
        }

        private void HandleAutocompleteEditorTextChanged(object sender, TextChangedEventArgs e)
        {
            HandleSelectableEditorTextChanged(sender, e);
        }

        private void HandleDatePickerTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isSynchronizingEditorState || !(sender is DatePicker datePicker) || CellData?.IsEditing != true)
            {
                return;
            }

            var currentText = datePicker.Text ?? string.Empty;
            if (ShouldSuppressSelectionEcho(currentText))
            {
                return;
            }

            RaiseEditorValueChanged(currentText, UniversalEditorValueChangeKind.TextEdited);
        }

        private void HandleDatePickerSelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isSynchronizingEditorState || !(sender is DatePicker datePicker) || CellData?.IsEditing != true)
            {
                return;
            }

            RaiseEditorValueChanged(FormatDateForEditing(datePicker.SelectedDate, datePicker.Text), UniversalEditorValueChangeKind.SelectionCommitted);
        }

        private void HandleDatePickerValidationError(object sender, DatePickerDateValidationErrorEventArgs e)
        {
            if (e == null)
            {
                return;
            }

            e.ThrowException = false;
        }

        private void HandleCheckBoxEditorChecked(object sender, RoutedEventArgs e)
        {
            if (_isSynchronizingEditorState || !(sender is CheckBox checkBox) || CellData?.IsEditing != true)
            {
                return;
            }

            RaiseEditorValueChanged(checkBox.IsChecked == true ? bool.TrueString : bool.FalseString, UniversalEditorValueChangeKind.SelectionCommitted);
        }

        private FrameworkElement ConfigureTextEditor(GridCellSurfaceItem cellData, bool useMask)
        {
            EnsureEditingTextBox();
            var editorText = cellData.EditingText ?? cellData.DisplayText ?? string.Empty;

            _isSynchronizingEditorState = true;
            try
            {
                if (!string.Equals(_editingTextBox.Text, editorText, StringComparison.Ordinal))
                {
                    _editingTextBox.Text = editorText;
                }

                MaskedTextBoxBehavior.SetMaskPattern(_editingTextBox, useMask ? cellData.EditMask : string.Empty);
                _editingTextBox.ToolTip = cellData.HasValidationError ? cellData.ValidationError : null;
                _editingTextBox.TextAlignment = ResolveTextAlignment(cellData);
            }
            finally
            {
                _isSynchronizingEditorState = false;
            }

            return _editingTextBox;
        }

        private FrameworkElement ConfigureComboEditor(GridCellSurfaceItem cellData)
        {
            EnsureComboEditor();
            var editorText = cellData.EditingText ?? cellData.DisplayText ?? string.Empty;
            ConfigureSelectableEditor(_comboEditor, cellData);
            var selectedItem = ResolveEditorSelectedItem(cellData.EditorItems, editorText);

            _isSynchronizingEditorState = true;
            try
            {
                _comboEditor.ItemsSource = cellData.EditorItems ?? Array.Empty<string>();
                _comboEditor.SelectedItem = selectedItem;
                _comboEditor.ToolTip = cellData.HasValidationError ? cellData.ValidationError : null;
                _comboEditor.HorizontalContentAlignment = ResolveHorizontalAlignment(cellData);
            }
            finally
            {
                _isSynchronizingEditorState = false;
            }

            return _comboEditor;
        }

        private FrameworkElement ConfigureAutocompleteEditor(GridCellSurfaceItem cellData)
        {
            EnsureAutocompleteEditor();
            var editorText = cellData.EditingText ?? cellData.DisplayText ?? string.Empty;
            ConfigureSelectableEditor(_autocompleteEditor, cellData);
            var selectedItem = ResolveEditorSelectedItem(cellData.EditorItems, editorText);

            _isSynchronizingEditorState = true;
            try
            {
                _autocompleteEditor.ItemsSource = cellData.EditorItems ?? Array.Empty<string>();
                if (_autocompleteEditor.IsEditable &&
                    !string.Equals(_autocompleteEditor.Text, editorText, StringComparison.Ordinal))
                {
                    _autocompleteEditor.Text = editorText;
                }

                _autocompleteEditor.SelectedItem = selectedItem;
                _autocompleteEditor.ToolTip = cellData.HasValidationError ? cellData.ValidationError : null;
                _autocompleteEditor.HorizontalContentAlignment = ResolveHorizontalAlignment(cellData);
            }
            finally
            {
                _isSynchronizingEditorState = false;
            }

            return _autocompleteEditor;
        }

        private FrameworkElement ConfigureDatePickerEditor(GridCellSurfaceItem cellData)
        {
            EnsureDatePickerEditor();
            var editorText = cellData.EditingText ?? cellData.DisplayText ?? string.Empty;
            var selectedDate = TryParseEditingDate(editorText, cellData.RawValue);

            _isSynchronizingEditorState = true;
            try
            {
                if (_datePickerEditor.SelectedDate != selectedDate)
                {
                    _datePickerEditor.SelectedDate = selectedDate;
                }

                var formattedText = FormatDateForEditing(selectedDate, editorText);
                if (!string.Equals(_datePickerEditor.Text, formattedText, StringComparison.Ordinal))
                {
                    _datePickerEditor.Text = formattedText;
                }

                _datePickerEditor.ToolTip = cellData.HasValidationError ? cellData.ValidationError : null;
                _datePickerEditor.HorizontalContentAlignment = HorizontalAlignment.Center;
                _datePickerEditor.VerticalContentAlignment = VerticalAlignment.Center;
                ApplyDatePickerSharedCalendarStyle();
                ApplyDatePickerPopupThemeResources();
                ApplyDatePickerEditingAlignment(TextAlignment.Center);
            }
            finally
            {
                _isSynchronizingEditorState = false;
            }

            return _datePickerEditor;
        }

        private FrameworkElement ConfigureCheckBoxEditor(GridCellSurfaceItem cellData)
        {
            EnsureCheckBoxEditor();
            var isChecked = TryResolveEditingBooleanValue(cellData);

            _isSynchronizingEditorState = true;
            try
            {
                _checkBoxEditor.IsChecked = isChecked;
                _checkBoxEditor.ToolTip = cellData.HasValidationError ? cellData.ValidationError : null;
            }
            finally
            {
                _isSynchronizingEditorState = false;
            }

            return _checkBoxEditor;
        }

        private FrameworkElement CreateEditingChrome(GridCellSurfaceItem cellData, FrameworkElement editor)
        {
            EnsureEditingChromeHost();
            if (!ReferenceEquals(_editingChromeHost.Child, editor))
            {
                _editingChromeHost.Child = editor;
            }

            _editingChromeHost.ToolTip = cellData.HasValidationError ? cellData.ValidationError : null;
            _editingChromeHost.Padding = ResolveEditingChromePadding(cellData);
            _editingChromeHost.CornerRadius = new CornerRadius(IsCompactEditingChrome(cellData) ? 8d : 6d);
            _editingChromeHost.SetResourceReference(
                Border.BackgroundProperty,
                IsCompactEditingChrome(cellData) ? "PgRowAccentBackgroundBrush" : "PgRowBackgroundBrush");
            RoundedChildClipBehavior.UpdateChildClip(_editingChromeHost);
            ApplyEditingChromeScale(cellData, animate: !_wasEditingInPreviousSnapshot);
            return _editingChromeHost;
        }

        private FrameworkElement CreateDisplayContent(GridCellSurfaceItem cellData)
        {
            if (string.Equals(cellData.ValueKind, "Boolean", StringComparison.OrdinalIgnoreCase))
            {
                return CreateBooleanDisplayContent(cellData);
            }

            return CreateTextDisplayContent(cellData);
        }

        private FrameworkElement CreateTextDisplayContent(GridCellSurfaceItem cellData)
        {
            var textBlock = new TextBlock
            {
                Text = cellData.DisplayText ?? string.Empty,
                Margin = new Thickness(4, 0, 4, 0),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                TextAlignment = ResolveTextAlignment(cellData),
                TextTrimming = TextTrimming.CharacterEllipsis,
                IsHitTestVisible = false,
            };
            textBlock.SetResourceReference(TextBlock.ForegroundProperty, cellData.IsReadOnly ? "PgMutedTextBrush" : "PgPrimaryTextBrush");
            return textBlock;
        }

        private FrameworkElement CreateBooleanDisplayContent(GridCellSurfaceItem cellData)
        {
            var checkBox = new CheckBox
            {
                IsChecked = TryResolveBooleanValue(cellData.RawValue, cellData.DisplayText),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                IsHitTestVisible = false,
                Focusable = false,
                Margin = new Thickness(0),
                Padding = new Thickness(0),
            };
            checkBox.SetResourceReference(ForegroundProperty, cellData.IsReadOnly ? "PgMutedTextBrush" : "PgPrimaryTextBrush");
            return checkBox;
        }

        private void EnsureEditingTextBox()
        {
            if (_editingTextBox != null)
            {
                return;
            }

            _editingTextBox = CreateEditorTextBox();
            _editingTextBox.TextChanged += HandleEditingTextBoxTextChanged;
            _editingTextBox.RequestBringIntoView += HandleEditingTextBoxRequestBringIntoView;
        }

        private void EnsureComboEditor()
        {
            if (_comboEditor != null)
            {
                return;
            }

            _comboEditor = CreateEditorComboBox();
            _comboEditor.SelectionChanged += HandleComboEditorSelectionChanged;
            _comboEditor.DropDownOpened += HandleSelectableEditorDropDownOpened;
            _comboEditor.DropDownClosed += HandleSelectableEditorDropDownClosed;
            _comboEditor.AddHandler(TextBoxBase.TextChangedEvent, new TextChangedEventHandler(HandleSelectableEditorTextChanged));
            _comboEditor.Loaded += HandleSelectableEditorLoaded;
        }

        private void EnsureAutocompleteEditor()
        {
            if (_autocompleteEditor != null)
            {
                return;
            }

            _autocompleteEditor = CreateEditorComboBox();
            _autocompleteEditor.SelectionChanged += HandleAutocompleteEditorSelectionChanged;
            _autocompleteEditor.DropDownOpened += HandleSelectableEditorDropDownOpened;
            _autocompleteEditor.DropDownClosed += HandleSelectableEditorDropDownClosed;
            _autocompleteEditor.AddHandler(TextBoxBase.TextChangedEvent, new TextChangedEventHandler(HandleAutocompleteEditorTextChanged));
            _autocompleteEditor.Loaded += HandleSelectableEditorLoaded;
        }

        private void EnsureDatePickerEditor()
        {
            if (_datePickerEditor != null)
            {
                return;
            }

            _datePickerEditor = new DatePicker
            {
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0),
                Margin = new Thickness(0),
                MinHeight = 0,
                VerticalAlignment = VerticalAlignment.Stretch,
                Background = Brushes.Transparent,
                FocusVisualStyle = null,
                SelectedDateFormat = DatePickerFormat.Short,
            };
            _datePickerEditor.SetResourceReference(DatePicker.CalendarStyleProperty, "Calendar.Shared.Style");
            _datePickerEditor.Loaded += HandleDatePickerEditorLoaded;
            _datePickerEditor.SelectedDateChanged += HandleDatePickerSelectedDateChanged;
            _datePickerEditor.DateValidationError += HandleDatePickerValidationError;
            _datePickerEditor.AddHandler(TextBoxBase.TextChangedEvent, new TextChangedEventHandler(HandleDatePickerTextChanged));
        }

        private void ApplyDatePickerEditingAlignment(TextAlignment textAlignment)
        {
            if (_datePickerEditor == null)
            {
                return;
            }

            _datePickerEditor.ApplyTemplate();
            if (_datePickerEditor.Template?.FindName("PART_TextBox", _datePickerEditor) is DatePickerTextBox textBox)
            {
                textBox.SetResourceReference(Control.TemplateProperty, ChromeHostedTextBoxTemplateResourceKey);
                textBox.TextAlignment = textAlignment;
                textBox.HorizontalContentAlignment = HorizontalAlignment.Stretch;
                textBox.VerticalContentAlignment = VerticalAlignment.Center;
                textBox.VerticalAlignment = VerticalAlignment.Center;
                textBox.Padding = new Thickness(4d, 0d, 4d, 0d);
                textBox.MinHeight = 0d;
            }

            if (_datePickerEditor.Template?.FindName("PART_Button", _datePickerEditor) is Button button)
            {
                ApplyDatePickerButtonChrome(button);

                _datePickerEditor.Dispatcher.BeginInvoke(
                    DispatcherPriority.Loaded,
                    new Action(() =>
                    {
                        if (_datePickerEditor?.Template?.FindName("PART_Button", _datePickerEditor) is Button loadedButton)
                        {
                            ApplyDatePickerButtonChrome(loadedButton);
                        }
                    }));
            }

            if (_datePickerEditor.Template?.FindName("PART_Popup", _datePickerEditor) is Popup popup)
            {
                popup.Opened -= HandleDatePickerPopupOpened;
                popup.Opened += HandleDatePickerPopupOpened;
                ApplyDatePickerPopupChrome(popup);
            }
        }

        private void EnsureCheckBoxEditor()
        {
            if (_checkBoxEditor != null)
            {
                return;
            }

            _checkBoxEditor = new CheckBox
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0),
                Padding = new Thickness(0),
                FocusVisualStyle = null,
            };
            _checkBoxEditor.Loaded += HandleCheckBoxEditorLoaded;
            _checkBoxEditor.Checked += HandleCheckBoxEditorChecked;
            _checkBoxEditor.Unchecked += HandleCheckBoxEditorChecked;
        }

        private void EnsureEditingChromeHost()
        {
            if (_editingChromeHost != null)
            {
                return;
            }

            _editingChromeScaleTransform = new ScaleTransform(1d, 1d);
            _editingChromeHost = new Border
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                BorderThickness = new Thickness(1.25d),
                SnapsToDevicePixels = true,
                UseLayoutRounding = true,
                RenderTransform = _editingChromeScaleTransform,
                RenderTransformOrigin = new Point(0.5d, 0.5d),
            };
            RoundedChildClipBehavior.SetClipChildToBorder(_editingChromeHost, true);
            _editingChromeHost.SetResourceReference(Border.BackgroundProperty, "PgRowBackgroundBrush");
            _editingChromeHost.SetResourceReference(Border.BorderBrushProperty, "PgGridLineBrush");
        }

        private static TextBox CreateEditorTextBox()
        {
            var editor = new TextBox
            {
                BorderThickness = new Thickness(0),
                BorderBrush = Brushes.Transparent,
                Padding = new Thickness(4, 0, 4, 0),
                Margin = new Thickness(0),
                MinHeight = 0,
                VerticalContentAlignment = VerticalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Stretch,
                Background = Brushes.Transparent,
                FocusVisualStyle = null,
            };
            editor.SetResourceReference(Control.TemplateProperty, ChromeHostedTextBoxTemplateResourceKey);
            return editor;
        }

        private static ComboBox CreateEditorComboBox()
        {
            var editor = new ComboBox
            {
                BorderThickness = new Thickness(0),
                Padding = new Thickness(2, 0, 2, 0),
                Margin = new Thickness(0),
                MinHeight = 0,
                VerticalContentAlignment = VerticalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Stretch,
                Background = Brushes.Transparent,
                FocusVisualStyle = null,
                IsEditable = false,
                IsTextSearchEnabled = true,
                StaysOpenOnEdit = false,
            };
            editor.SetResourceReference(Control.TemplateProperty, PgEditingComboBoxTemplateResourceKey);
            editor.SetResourceReference(ItemsControl.ItemContainerStyleProperty, PgEditingComboBoxItemStyleResourceKey);
            editor.SetResourceReference(Control.ForegroundProperty, "PgPrimaryTextBrush");
            return editor;
        }

        private static void ApplyEditableComboBoxChrome(ComboBox comboBox)
        {
            if (comboBox == null || !comboBox.IsEditable)
            {
                return;
            }

            comboBox.ApplyTemplate();
            if (!(comboBox.Template?.FindName("PART_EditableTextBox", comboBox) is TextBox editableTextBox))
            {
                return;
            }

            editableTextBox.SetResourceReference(Control.TemplateProperty, ChromeHostedTextBoxTemplateResourceKey);
            editableTextBox.Background = Brushes.Transparent;
            editableTextBox.BorderBrush = Brushes.Transparent;
            editableTextBox.BorderThickness = new Thickness(0d);
            editableTextBox.FocusVisualStyle = null;
        }

        private void ApplyDatePickerButtonChrome(Button button)
        {
            if (button == null)
            {
                return;
            }

            button.VerticalAlignment = VerticalAlignment.Center;
            button.VerticalContentAlignment = VerticalAlignment.Center;
            button.Padding = new Thickness(0d);
            button.MinHeight = 0d;
            button.Margin = new Thickness(0d);
            button.BorderThickness = new Thickness(0d);
            button.BorderBrush = Brushes.Transparent;
            button.Background = Brushes.Transparent;
            button.FocusVisualStyle = null;
            button.Content = CalendarGlyphText;
            button.FontFamily = new FontFamily("Segoe MDL2 Assets");
            button.FontSize = 14d;
            button.Foreground = TryResolveDatePickerResource<Brush>("PgSecondaryMutedBrush") ??
                TryResolveDatePickerResource<Brush>("Brush.Text.Secondary") ??
                _datePickerEditor?.Foreground;
            button.SetResourceReference(Control.TemplateProperty, ChromeInlineActionButtonTemplateResourceKey);
        }

        private void ApplyDatePickerPopupThemeResources()
        {
            if (_datePickerEditor == null)
            {
                return;
            }

            ApplyDatePickerThemeResources(_datePickerEditor.Resources);
        }

        private void ApplyDatePickerSharedCalendarStyle()
        {
            if (_datePickerEditor == null)
            {
                return;
            }

            if (TryResolveDatePickerResource<Style>("Calendar.Shared.Style") is Style sharedCalendarStyle)
            {
                _datePickerEditor.CalendarStyle = sharedCalendarStyle;
            }
        }

        private void ApplyDatePickerSharedCalendarChrome(Calendar popupCalendar)
        {
            if (popupCalendar == null)
            {
                return;
            }

            ApplyDatePickerThemeResources(popupCalendar.Resources);

            var popupCalendarStyle = TryResolveDatePickerResource<Style>("Calendar.Shared.Style");
            var sharedCalendarStyle = popupCalendarStyle ??
                                      TryResolveDatePickerResource<Style>("Calendar.Shared.Style");
            if (sharedCalendarStyle != null)
            {
                popupCalendar.Style = sharedCalendarStyle;
                popupCalendar.Resources["Calendar.Shared.Style"] = sharedCalendarStyle;
            }

            if (TryResolveDatePickerResource<Style>("Calendar.Shared.DayButtonStyle") is Style sharedDayButtonStyle)
            {
                popupCalendar.CalendarDayButtonStyle = sharedDayButtonStyle;
                popupCalendar.Resources["Calendar.Shared.DayButtonStyle"] = sharedDayButtonStyle;
            }

            if (TryResolveDatePickerResource<Style>("Calendar.Shared.MonthButtonStyle") is Style sharedMonthButtonStyle)
            {
                popupCalendar.CalendarButtonStyle = sharedMonthButtonStyle;
                popupCalendar.Resources["Calendar.Shared.MonthButtonStyle"] = sharedMonthButtonStyle;
            }

            if (TryResolveDatePickerResource<Style>("Calendar.Shared.ItemStyle") is Style sharedCalendarItemStyle)
            {
                popupCalendar.Resources["Calendar.Shared.ItemStyle"] = sharedCalendarItemStyle;
                popupCalendar.Resources[typeof(CalendarItem)] = new Style(typeof(CalendarItem), sharedCalendarItemStyle);
            }
            else if (TryResolveDatePickerResource<Style>(typeof(CalendarItem)) is Style implicitCalendarItemStyle)
            {
                popupCalendar.Resources[typeof(CalendarItem)] = implicitCalendarItemStyle;
            }

            if (TryResolveDatePickerResource<Style>("Calendar.Shared.NavButtonStyle") is Style sharedNavButtonStyle)
            {
                popupCalendar.Resources["Calendar.Shared.NavButtonStyle"] = sharedNavButtonStyle;
            }

            if (TryResolveDatePickerResource<Style>("Calendar.Shared.HeaderButtonStyle") is Style sharedHeaderButtonStyle)
            {
                popupCalendar.Resources["Calendar.Shared.HeaderButtonStyle"] = sharedHeaderButtonStyle;
            }

            if (TryResolveDatePickerResource<Style>("Calendar.Shared.TodayButtonStyle") is Style sharedTodayButtonStyle)
            {
                popupCalendar.Resources["Calendar.Shared.TodayButtonStyle"] = sharedTodayButtonStyle;
            }

            if (TryResolveDatePickerResource<Style>(typeof(CalendarDayButton)) is Style implicitCalendarDayButtonStyle)
            {
                popupCalendar.Resources[typeof(CalendarDayButton)] = implicitCalendarDayButtonStyle;
            }

            if (TryResolveDatePickerResource<Style>(typeof(CalendarButton)) is Style implicitCalendarButtonStyle)
            {
                popupCalendar.Resources[typeof(CalendarButton)] = implicitCalendarButtonStyle;
            }

            if (TryResolveDatePickerResource<Brush>("Brush.Surface0") is Brush calendarBackgroundBrush)
            {
                popupCalendar.Background = calendarBackgroundBrush;
            }
            else if (TryResolveDatePickerResource<Brush>("PgPanelBackgroundBrush") is Brush popupBackgroundBrush)
            {
                popupCalendar.Background = popupBackgroundBrush;
            }

            if (TryResolveDatePickerResource<Brush>("Brush.Text.Primary") is Brush calendarForegroundBrush)
            {
                popupCalendar.Foreground = calendarForegroundBrush;
            }
            else if (TryResolveDatePickerResource<Brush>("PgPrimaryTextBrush") is Brush popupForegroundBrush)
            {
                popupCalendar.Foreground = popupForegroundBrush;
            }

            if (TryResolveDatePickerResource<Brush>("Brush.Border.Default") is Brush calendarBorderBrush)
            {
                popupCalendar.BorderBrush = calendarBorderBrush;
            }
            else if (TryResolveDatePickerResource<Brush>("PgControlBorderBrush") is Brush popupBorderBrush)
            {
                popupCalendar.BorderBrush = popupBorderBrush;
            }

            popupCalendar.ApplyTemplate();
            if (popupCalendar.Template?.FindName("PART_CalendarItem", popupCalendar) is CalendarItem popupCalendarItem)
            {
                if (TryResolveDatePickerResource<Style>("Calendar.Shared.ItemStyle") is Style popupCalendarItemSharedStyle)
                {
                    popupCalendarItem.Style = new Style(typeof(CalendarItem), popupCalendarItemSharedStyle);
                }
                else if (TryResolveDatePickerResource<Style>(typeof(CalendarItem)) is Style popupCalendarItemImplicitStyle)
                {
                    popupCalendarItem.Style = popupCalendarItemImplicitStyle;
                }

                popupCalendarItem.ApplyTemplate();
            }
        }

        private void ApplyDatePickerPopupChrome(Popup popup)
        {
            var popupRoot = popup?.Child as FrameworkElement;
            if (popupRoot == null)
            {
                return;
            }

            ApplyDatePickerThemeResources(popupRoot.Resources);
            var popupBorder = popupRoot as Border ?? FindVisualDescendant<Border>(popupRoot);
            if (popupBorder != null)
            {
                var popupBackground = TryResolveDatePickerResource<Brush>("Brush.Surface0") ??
                                      TryResolveDatePickerResource<Brush>("PgPanelBackgroundBrush");
                if (popupBackground != null)
                {
                    popupBorder.Background = popupBackground;
                }

                var popupBorderBrush = TryResolveDatePickerResource<Brush>("Brush.Border.Subtle") ??
                                       TryResolveDatePickerResource<Brush>("Brush.Border.Default") ??
                                       TryResolveDatePickerResource<Brush>("PgControlBorderBrush");
                if (popupBorderBrush != null)
                {
                    popupBorder.BorderBrush = popupBorderBrush;
                }
            }

            var popupCalendar = popupRoot as Calendar ?? FindVisualDescendant<Calendar>(popupRoot);
            if (popupCalendar != null)
            {
                ApplyDatePickerSharedCalendarChrome(popupCalendar);
            }
        }

        private void HandleDatePickerPopupOpened(object sender, EventArgs e)
        {
            ApplyDatePickerPopupChrome(sender as Popup);
        }

        private void ApplyDatePickerThemeResources(ResourceDictionary targetResources)
        {
            if (targetResources == null)
            {
                return;
            }

            CopyDatePickerThemeBrushResource(targetResources, "Brush.Surface0", "PgPanelBackgroundBrush");
            CopyDatePickerThemeBrushResource(targetResources, "Brush.Surface1", "PgRowAccentBackgroundBrush");
            CopyDatePickerThemeBrushResource(targetResources, "Brush.Surface2", "PgHoverBackgroundBrush");
            CopyDatePickerThemeBrushResource(targetResources, "Brush.Text.Primary", "PgPrimaryTextBrush");
            CopyDatePickerThemeBrushResource(targetResources, "Brush.Text.Secondary", "PgSecondaryMutedBrush");
            CopyDatePickerThemeBrushResource(targetResources, "Brush.Text.Muted", "PgMutedTextBrush");
            CopyDatePickerThemeBrushResource(targetResources, "Brush.Border.Subtle", "PgPanelBorderBrush");
            CopyDatePickerThemeBrushResource(targetResources, "Brush.Border.Default", "PgControlBorderBrush");
            CopyDatePickerThemeBrushResource(targetResources, "Brush.Focus.Ring", "PgFocusRingBrush");
            CopyDatePickerThemeBrushResource(targetResources, "Brush.Hover.Fill", "PgHoverBackgroundBrush");
            CopyDatePickerThemeBrushResource(targetResources, "Brush.Selection.Active.Fill", "PgSelectionBackgroundBrush");
            CopyDatePickerThemeBrushResource(targetResources, "Brush.Selection.Active.Text", "PgSelectionTextBrush");
            CopyDatePickerThemeBrushResource(targetResources, "Brush.Accent", "PgAccentBrush");
            CopyDatePickerThemeBrushResource(targetResources, "Brush.Accent.Strong", "PgSortOrderBrush");
            ApplyDatePickerPopupBrushResource(targetResources, "Brush.Surface0", SystemColors.WindowBrushKey, SystemColors.WindowColorKey);
            ApplyDatePickerPopupBrushResource(targetResources, "Brush.Surface1", SystemColors.ControlBrushKey, SystemColors.ControlColorKey);
            ApplyDatePickerPopupBrushResource(targetResources, "Brush.Surface2", SystemColors.ControlLightBrushKey, SystemColors.ControlLightColorKey);
            ApplyDatePickerPopupBrushResource(targetResources, "Brush.Surface0", SystemColors.ControlLightLightBrushKey, SystemColors.ControlLightLightColorKey);
            ApplyDatePickerPopupBrushResource(targetResources, "Brush.Border.Default", SystemColors.ControlDarkBrushKey, SystemColors.ControlDarkColorKey);
            ApplyDatePickerPopupBrushResource(targetResources, "Brush.Border.Default", SystemColors.ControlDarkDarkBrushKey, SystemColors.ControlDarkDarkColorKey);
            ApplyDatePickerPopupBrushResource(targetResources, "Brush.Text.Primary", SystemColors.ControlTextBrushKey, SystemColors.ControlTextColorKey);
            ApplyDatePickerPopupBrushResource(targetResources, "Brush.Text.Primary", SystemColors.WindowTextBrushKey, SystemColors.WindowTextColorKey);
            ApplyDatePickerPopupBrushResource(targetResources, "Brush.Text.Muted", SystemColors.GrayTextBrushKey, SystemColors.GrayTextColorKey);
            ApplyDatePickerPopupBrushResource(targetResources, "Brush.Selection.Active.Fill", SystemColors.HighlightBrushKey, SystemColors.HighlightColorKey);
            ApplyDatePickerPopupBrushResource(targetResources, "Brush.Selection.Active.Text", SystemColors.HighlightTextBrushKey, SystemColors.HighlightTextColorKey);
            ApplyDatePickerPopupBrushResource(targetResources, "PgSelectionInactiveBackgroundBrush", SystemColors.InactiveSelectionHighlightBrushKey, null);
            ApplyDatePickerPopupBrushResource(targetResources, "PgSelectionTextBrush", SystemColors.InactiveSelectionHighlightTextBrushKey, null);
        }

        private void CopyDatePickerThemeBrushResource(ResourceDictionary targetResources, string resourceKey, string fallbackResourceKey = null)
        {
            var sourceBrush = TryResolveDatePickerResource<Brush>(resourceKey);
            if (sourceBrush == null && !string.IsNullOrWhiteSpace(fallbackResourceKey))
            {
                sourceBrush = TryResolveDatePickerResource<Brush>(fallbackResourceKey);
            }

            if (sourceBrush == null)
            {
                return;
            }

            if (sourceBrush is SolidColorBrush solidBrush)
            {
                var clonedBrush = new SolidColorBrush(solidBrush.Color);
                if (clonedBrush.CanFreeze)
                {
                    clonedBrush.Freeze();
                }

                targetResources[resourceKey] = clonedBrush;
                return;
            }

            targetResources[resourceKey] = sourceBrush;
        }

        private void ApplyDatePickerPopupBrushResource(ResourceDictionary targetResources, string sourceBrushKey, object systemBrushKey, object systemColorKey)
        {
            var sourceBrush = TryResolveDatePickerResource<Brush>(sourceBrushKey) as SolidColorBrush;
            if (sourceBrush == null)
            {
                return;
            }

            var themedBrush = new SolidColorBrush(sourceBrush.Color);
            if (themedBrush.CanFreeze)
            {
                themedBrush.Freeze();
            }

            targetResources[systemBrushKey] = themedBrush;
            if (systemColorKey != null)
            {
                targetResources[systemColorKey] = sourceBrush.Color;
            }
        }

        private T TryResolveDatePickerResource<T>(object resourceKey)
            where T : class
        {
            if (resourceKey == null)
            {
                return null;
            }

            if (_datePickerEditor?.TryFindResource(resourceKey) is T editorResource)
            {
                return editorResource;
            }

            var editorDictionaryResource = FindResourceInDictionaryTree<T>(_datePickerEditor?.Resources, resourceKey);
            if (editorDictionaryResource != null)
            {
                return editorDictionaryResource;
            }

            if (TryFindResource(resourceKey) is T presenterResource)
            {
                return presenterResource;
            }

            var presenterDictionaryResource = FindResourceInDictionaryTree<T>(Resources, resourceKey);
            if (presenterDictionaryResource != null)
            {
                return presenterDictionaryResource;
            }

            var window = _datePickerEditor == null ? null : Window.GetWindow(_datePickerEditor);
            if (window?.TryFindResource(resourceKey) is T windowResource)
            {
                return windowResource;
            }

            var windowDictionaryResource = FindResourceInDictionaryTree<T>(window?.Resources, resourceKey);
            if (windowDictionaryResource != null)
            {
                return windowDictionaryResource;
            }

            if (Application.Current?.TryFindResource(resourceKey) is T applicationResource)
            {
                return applicationResource;
            }

            var applicationDictionaryResource = FindResourceInDictionaryTree<T>(Application.Current?.Resources, resourceKey);
            if (applicationDictionaryResource != null)
            {
                return applicationDictionaryResource;
            }

            return null;
        }

        private static T FindResourceInDictionaryTree<T>(ResourceDictionary resources, object resourceKey)
            where T : class
        {
            if (resources == null || resourceKey == null)
            {
                return null;
            }

            if (resources.Contains(resourceKey) && resources[resourceKey] is T directResource)
            {
                return directResource;
            }

            foreach (var mergedDictionary in resources.MergedDictionaries)
            {
                var mergedResource = FindResourceInDictionaryTree<T>(mergedDictionary, resourceKey);
                if (mergedResource != null)
                {
                    return mergedResource;
                }
            }

            return null;
        }

        private static T FindVisualDescendant<T>(DependencyObject root)
            where T : DependencyObject
        {
            if (root == null)
            {
                return null;
            }

            var childCount = VisualTreeHelper.GetChildrenCount(root);
            for (var childIndex = 0; childIndex < childCount; childIndex++)
            {
                var child = VisualTreeHelper.GetChild(root, childIndex);
                if (child is T match)
                {
                    return match;
                }

                var descendant = FindVisualDescendant<T>(child);
                if (descendant != null)
                {
                    return descendant;
                }
            }

            return null;
        }

        private static HorizontalAlignment ResolveHorizontalAlignment(GridCellSurfaceItem cellData)
        {
            switch (NormalizeValueKind(cellData))
            {
                case "Number":
                case "Currency":
                case "Percent":
                    return HorizontalAlignment.Right;
                case "Boolean":
                case "Status":
                    return HorizontalAlignment.Center;
                default:
                    return HorizontalAlignment.Left;
            }
        }

        private static TextAlignment ResolveTextAlignment(GridCellSurfaceItem cellData)
        {
            switch (NormalizeValueKind(cellData))
            {
                case "Number":
                case "Currency":
                case "Percent":
                    return TextAlignment.Right;
                case "Boolean":
                case "Status":
                    return TextAlignment.Center;
                default:
                    return TextAlignment.Left;
            }
        }

        private static string NormalizeValueKind(GridCellSurfaceItem cellData)
        {
            return string.IsNullOrWhiteSpace(cellData?.ValueKind) ? "Text" : cellData.ValueKind;
        }

        private static bool? TryResolveBooleanValue(object rawValue, string displayText)
        {
            if (rawValue is bool boolValue)
            {
                return boolValue;
            }

            if (bool.TryParse(displayText ?? string.Empty, out var parsed))
            {
                return parsed;
            }

            return null;
        }

        private static bool? TryResolveEditingBooleanValue(GridCellSurfaceItem cellData)
        {
            if (cellData != null && bool.TryParse(cellData.EditingText ?? string.Empty, out var editingValue))
            {
                return editingValue;
            }

            return TryResolveBooleanValue(cellData?.RawValue, cellData?.DisplayText);
        }

        private static bool IsCompactEditingChrome(GridCellSurfaceItem cellData)
        {
            return cellData.EditorKind == GridColumnEditorKind.CheckBox ||
                string.Equals(NormalizeValueKind(cellData), "Boolean", StringComparison.OrdinalIgnoreCase);
        }

        private static Thickness ResolveEditingChromePadding(GridCellSurfaceItem cellData)
        {
            return IsCompactEditingChrome(cellData)
                ? new Thickness(2d, 0d, 2d, 0d)
                : new Thickness(6d, 0d, 6d, 0d);
        }

        private void HandleSelectableEditorLoaded(object sender, RoutedEventArgs e)
        {
            ApplyComboEditorChromeResources(sender as ComboBox);
        }

        private void HandleDatePickerEditorLoaded(object sender, RoutedEventArgs e)
        {
            ApplyDatePickerSharedCalendarStyle();
            ApplyDatePickerPopupThemeResources();
            ApplyDatePickerEditingAlignment(TextAlignment.Center);
        }

        private void HandleCheckBoxEditorLoaded(object sender, RoutedEventArgs e)
        {
            ApplyCheckBoxEditorChromeResources(sender as CheckBox);
        }

        private static void ApplyComboEditorChromeResources(ComboBox comboBox)
        {
            if (comboBox == null)
            {
                return;
            }

            ApplyComboEditorChromeResourcesCore(comboBox);
        }

        private static void ApplyComboEditorChromeResourcesCore(ComboBox comboBox)
        {
            if (comboBox == null)
            {
                return;
            }

            comboBox.SetResourceReference(Control.TemplateProperty, PgEditingComboBoxTemplateResourceKey);
            comboBox.SetResourceReference(ItemsControl.ItemContainerStyleProperty, PgEditingComboBoxItemStyleResourceKey);
            comboBox.SetResourceReference(Control.ForegroundProperty, "PgPrimaryTextBrush");
            comboBox.ApplyTemplate();
            ApplyEditableComboBoxChrome(comboBox);
        }

        private static void ApplyCheckBoxEditorChromeResources(CheckBox checkBox)
        {
            if (checkBox == null)
            {
                return;
            }

            if (checkBox.TryFindResource(PgEditingCheckBoxStyleResourceKey) is Style style)
            {
                checkBox.Style = style;
            }

            checkBox.ApplyTemplate();
        }

        private void ApplyEditingChromeScale(GridCellSurfaceItem cellData, bool animate)
        {
            if (_editingChromeScaleTransform == null)
            {
                return;
            }

            const double targetScaleX = 1.00d;
            var targetScaleY = IsCompactEditingChrome(cellData) ? 1.20d : 1.00d;
            _editingChromeScaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            _editingChromeScaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, null);
            _editingChromeScaleTransform.ScaleX = targetScaleX;
            _editingChromeScaleTransform.ScaleY = targetScaleY;
            if (!animate)
            {
                return;
            }

            var widthAnimation = new DoubleAnimation(1d, targetScaleX, TimeSpan.FromMilliseconds(150d))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
                FillBehavior = FillBehavior.HoldEnd,
            };
            var heightAnimation = new DoubleAnimation(1d, targetScaleY, TimeSpan.FromMilliseconds(150d))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
                FillBehavior = FillBehavior.HoldEnd,
            };
            _editingChromeScaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, widthAnimation);
            _editingChromeScaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, heightAnimation);
            _editingChromeHost?.BeginAnimation(OpacityProperty, new DoubleAnimation(0.9d, 1d, TimeSpan.FromMilliseconds(140d))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
                FillBehavior = FillBehavior.HoldEnd,
            });
        }

        private void RaiseEditorValueChanged(string text, UniversalEditorValueChangeKind changeKind)
        {
            if (CellData == null)
            {
                return;
            }

            if (changeKind == UniversalEditorValueChangeKind.SelectionCommitted)
            {
                _pendingSelectionEchoValue = text ?? string.Empty;
            }

            EditingTextChanged?.Invoke(
                this,
                new GridCellEditingTextChangedEventArgs(CellData.RowKey, CellData.ColumnKey, text, changeKind));
        }

        private void HandleSelectableEditorDropDownOpened(object sender, EventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox == null || CellData?.IsEditing != true)
            {
                return;
            }

            _selectionCommittedDuringCurrentDropDown = false;
            _openedDropDownSelectionText = ResolveSelectableEditorSelectionText(comboBox);
            PhialeGridDiagnostics.Write(
                "GridCellPresenter",
                $"Selectable editor dropdown opened. Row='{CellData.RowKey}', Column='{CellData.ColumnKey}', Selected='{_openedDropDownSelectionText ?? string.Empty}', Text='{comboBox.Text ?? string.Empty}', IsEditable={comboBox.IsEditable}, ItemCount={comboBox.Items.Count}, GeneratedContainers={CountGeneratedItemContainers(comboBox)}.");

            comboBox.Dispatcher.BeginInvoke(
                new Action(() => AttachSelectablePopupLogging(comboBox)),
                System.Windows.Threading.DispatcherPriority.Input);
        }

        private void HandleSelectableEditorDropDownClosed(object sender, EventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (_isSynchronizingEditorState || comboBox == null || CellData?.IsEditing != true)
            {
                return;
            }

            DetachSelectablePopupLogging();
            var selectedText = ResolveSelectableEditorSelectionText(comboBox);
            PhialeGridDiagnostics.Write(
                "GridCellPresenter",
                $"Selectable editor dropdown closed. Row='{CellData.RowKey}', Column='{CellData.ColumnKey}', Selected='{selectedText ?? string.Empty}', OpenedSelection='{_openedDropDownSelectionText ?? string.Empty}', EditingText='{CellData.EditingText ?? string.Empty}', SelectionCommittedDuringDropDown={_selectionCommittedDuringCurrentDropDown}.");

            if (!_selectionCommittedDuringCurrentDropDown &&
                !string.IsNullOrWhiteSpace(selectedText) &&
                !string.Equals(selectedText, CellData.EditingText ?? string.Empty, StringComparison.Ordinal) &&
                !string.Equals(selectedText, _openedDropDownSelectionText ?? string.Empty, StringComparison.Ordinal))
            {
                RaiseEditorValueChanged(selectedText, UniversalEditorValueChangeKind.SelectionCommitted);
            }

            _selectionCommittedDuringCurrentDropDown = false;
            _openedDropDownSelectionText = null;
        }

        private void AttachSelectablePopupLogging(ComboBox comboBox)
        {
            DetachSelectablePopupLogging();
            if (comboBox == null)
            {
                return;
            }

            comboBox.ApplyTemplate();
            var popup = comboBox.Template?.FindName("PART_Popup", comboBox) as Popup;
            if (popup?.Child == null)
            {
                PhialeGridDiagnostics.Write(
                    "GridCellPresenter",
                    $"Selectable editor popup logging could not attach. Row='{CellData?.RowKey}', Column='{CellData?.ColumnKey}', PopupFound={popup != null}.");
                return;
            }

            _selectablePopupLoggingRoot = popup.Child;
            GridCellEditorInputScope.SetIsEditorOwnedPopupElement(_selectablePopupLoggingRoot, true);
            _selectablePopupLoggingRoot.AddHandler(UIElement.PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(HandleSelectablePopupPreviewMouseLeftButtonDown), true);
            _selectablePopupLoggingRoot.AddHandler(UIElement.PreviewMouseLeftButtonUpEvent, new MouseButtonEventHandler(HandleSelectablePopupPreviewMouseLeftButtonUp), true);
            _selectablePopupLoggingRoot.AddHandler(UIElement.MouseLeftButtonDownEvent, new MouseButtonEventHandler(HandleSelectablePopupMouseLeftButtonDown), true);
            _selectablePopupLoggingRoot.AddHandler(UIElement.MouseLeftButtonUpEvent, new MouseButtonEventHandler(HandleSelectablePopupMouseLeftButtonUp), true);
            _selectablePopupLoggingRoot.AddHandler(Selector.SelectedEvent, new RoutedEventHandler(HandleSelectablePopupSelected), true);
            _selectablePopupLoggingRoot.AddHandler(Selector.UnselectedEvent, new RoutedEventHandler(HandleSelectablePopupUnselected), true);
            PhialeGridDiagnostics.Write(
                "GridCellPresenter",
                $"Selectable editor popup logging attached. Row='{CellData?.RowKey}', Column='{CellData?.ColumnKey}', PopupChild='{_selectablePopupLoggingRoot.GetType().Name}'.");
        }

        private void DetachSelectablePopupLogging()
        {
            if (_selectablePopupLoggingRoot == null)
            {
                return;
            }

            _selectablePopupLoggingRoot.RemoveHandler(UIElement.PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(HandleSelectablePopupPreviewMouseLeftButtonDown));
            _selectablePopupLoggingRoot.RemoveHandler(UIElement.PreviewMouseLeftButtonUpEvent, new MouseButtonEventHandler(HandleSelectablePopupPreviewMouseLeftButtonUp));
            _selectablePopupLoggingRoot.RemoveHandler(UIElement.MouseLeftButtonDownEvent, new MouseButtonEventHandler(HandleSelectablePopupMouseLeftButtonDown));
            _selectablePopupLoggingRoot.RemoveHandler(UIElement.MouseLeftButtonUpEvent, new MouseButtonEventHandler(HandleSelectablePopupMouseLeftButtonUp));
            _selectablePopupLoggingRoot.RemoveHandler(Selector.SelectedEvent, new RoutedEventHandler(HandleSelectablePopupSelected));
            _selectablePopupLoggingRoot.RemoveHandler(Selector.UnselectedEvent, new RoutedEventHandler(HandleSelectablePopupUnselected));
            GridCellEditorInputScope.SetIsEditorOwnedPopupElement(_selectablePopupLoggingRoot, false);
            _selectablePopupLoggingRoot = null;
        }

        private void HandleSelectablePopupPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            LogSelectablePopupPointer("PreviewMouseLeftButtonDown", e);
        }

        private void HandleSelectablePopupPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            LogSelectablePopupPointer("PreviewMouseLeftButtonUp", e);
        }

        private void HandleSelectablePopupMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            LogSelectablePopupPointer("MouseLeftButtonDown", e);
        }

        private void HandleSelectablePopupMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            LogSelectablePopupPointer("MouseLeftButtonUp", e);
        }

        private void HandleSelectablePopupSelected(object sender, RoutedEventArgs e)
        {
            LogSelectablePopupSelection("Selected", e);
        }

        private void HandleSelectablePopupUnselected(object sender, RoutedEventArgs e)
        {
            LogSelectablePopupSelection("Unselected", e);
        }

        private void LogSelectablePopupPointer(string phase, MouseButtonEventArgs e)
        {
            var source = e?.OriginalSource as DependencyObject;
            var comboBoxItem = FindAncestor<ComboBoxItem>(source);
            var itemText = ResolveComboBoxItemText(comboBoxItem);
            PhialeGridDiagnostics.Write(
                "GridCellPresenter",
                $"Selectable editor popup {phase}. Row='{CellData?.RowKey}', Column='{CellData?.ColumnKey}', Source='{DescribeDependencyObject(source)}', Item='{itemText ?? string.Empty}', Handled={e?.Handled ?? false}, ItemSelected={comboBoxItem?.IsSelected ?? false}, ItemHighlighted={comboBoxItem?.IsHighlighted ?? false}, MouseCaptured='{DescribeDependencyObject(Mouse.Captured as DependencyObject)}', Focused='{DescribeDependencyObject(Keyboard.FocusedElement as DependencyObject)}'.");
        }

        private void LogSelectablePopupSelection(string phase, RoutedEventArgs e)
        {
            var source = e?.OriginalSource as DependencyObject;
            var comboBoxItem = FindAncestor<ComboBoxItem>(source);
            var itemText = ResolveComboBoxItemText(comboBoxItem);
            PhialeGridDiagnostics.Write(
                "GridCellPresenter",
                $"Selectable editor popup {phase}. Row='{CellData?.RowKey}', Column='{CellData?.ColumnKey}', Source='{DescribeDependencyObject(source)}', Item='{itemText ?? string.Empty}', Handled={e?.Handled ?? false}, ItemSelected={comboBoxItem?.IsSelected ?? false}, ItemHighlighted={comboBoxItem?.IsHighlighted ?? false}, MouseCaptured='{DescribeDependencyObject(Mouse.Captured as DependencyObject)}', Focused='{DescribeDependencyObject(Keyboard.FocusedElement as DependencyObject)}'.");
        }

        private bool ShouldSuppressSelectionEcho(string text)
        {
            var normalizedText = text ?? string.Empty;
            if (!string.Equals(_pendingSelectionEchoValue, normalizedText, StringComparison.Ordinal))
            {
                return false;
            }

            _pendingSelectionEchoValue = null;
            return true;
        }

        private static void ConfigureSelectableEditor(ComboBox comboBox, GridCellSurfaceItem cellData)
        {
            if (comboBox == null)
            {
                return;
            }

            var allowFreeText = cellData != null && cellData.EditorItemsMode == GridEditorItemsMode.Suggestions;
            comboBox.IsEditable = allowFreeText;
            comboBox.StaysOpenOnEdit = allowFreeText;
            comboBox.IsTextSearchEnabled = true;
            ApplyEditableComboBoxChrome(comboBox);
        }

        private static string ResolveEditorSelectedItem(System.Collections.Generic.IReadOnlyList<string> editorItems, string editorText)
        {
            if (editorItems == null || string.IsNullOrWhiteSpace(editorText))
            {
                return null;
            }

            return editorItems.FirstOrDefault(item => string.Equals(item, editorText, StringComparison.Ordinal));
        }

        private static string ResolveSelectableEditorSelectionText(ComboBox comboBox)
        {
            if (comboBox == null)
            {
                return string.Empty;
            }

            return comboBox.SelectedItem as string ??
                comboBox.Text ??
                string.Empty;
        }

        private static int CountGeneratedItemContainers(ComboBox comboBox)
        {
            if (comboBox == null)
            {
                return 0;
            }

            var count = 0;
            foreach (var item in comboBox.Items)
            {
                if (comboBox.ItemContainerGenerator.ContainerFromItem(item) is ComboBoxItem)
                {
                    count++;
                }
            }

            return count;
        }

        private static T FindAncestor<T>(DependencyObject source)
            where T : DependencyObject
        {
            var current = source;
            while (current != null)
            {
                if (current is T matched)
                {
                    return matched;
                }

                current = VisualTreeHelper.GetParent(current);
            }

            return null;
        }

        private static string ResolveComboBoxItemText(ComboBoxItem item)
        {
            if (item == null)
            {
                return string.Empty;
            }

            var value = item.DataContext ?? item.Content;
            return Convert.ToString(value, System.Globalization.CultureInfo.CurrentCulture) ?? string.Empty;
        }

        private static string DescribeDependencyObject(DependencyObject source)
        {
            if (source == null)
            {
                return "<null>";
            }

            if (source is FrameworkElement frameworkElement && !string.IsNullOrWhiteSpace(frameworkElement.Name))
            {
                return source.GetType().Name + "#" + frameworkElement.Name;
            }

            return source.GetType().Name;
        }

        private static void SelectEditableComboBoxText(ComboBox comboBox)
        {
            if (comboBox?.Template?.FindName("PART_EditableTextBox", comboBox) is TextBox editableTextBox)
            {
                editableTextBox.Focus();
                editableTextBox.SelectAll();
            }
        }

        private static DateTime? TryParseEditingDate(string editingText, object rawValue)
        {
            if (DateTime.TryParse(editingText, out var parsedDate))
            {
                return parsedDate;
            }

            if (rawValue is DateTime dateValue)
            {
                return dateValue;
            }

            return null;
        }

        private static string FormatDateForEditing(DateTime? selectedDate, string fallbackText)
        {
            if (selectedDate.HasValue)
            {
                return selectedDate.Value.ToString("yyyy-MM-dd");
            }

            return fallbackText ?? string.Empty;
        }

        private FrameworkElement CreateGroupCaptionContent(GridCellSurfaceItem cellData)
        {
            var root = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                IsHitTestVisible = false,
            };
            AutomationProperties.SetAutomationId(root, "surface.group-cell." + cellData.RowKey + "." + cellData.ColumnKey);

            root.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(Math.Max(0d, cellData.ContentIndent), GridUnitType.Pixel),
            });
            root.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = cellData.ShowInlineChevron ? new GridLength(16d, GridUnitType.Pixel) : new GridLength(0d, GridUnitType.Pixel),
            });
            root.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(1d, GridUnitType.Star),
            });

            if (cellData.ShowInlineChevron)
            {
                var chevron = new Path
                {
                    Width = 10d,
                    Height = 10d,
                    Stretch = Stretch.Uniform,
                    StrokeThickness = 1.6d,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round,
                    StrokeLineJoin = PenLineJoin.Round,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Data = Geometry.Parse(cellData.IsInlineChevronExpanded
                        ? "M 1 3 L 5 7 L 9 3"
                        : "M 3 1 L 7 5 L 3 9"),
                    IsHitTestVisible = false,
                };
                chevron.SetResourceReference(Path.StrokeProperty, cellData.IsReadOnly ? "PgMutedTextBrush" : "PgPrimaryTextBrush");
                AutomationProperties.SetAutomationId(chevron, "surface.group-toggle." + cellData.RowKey + "." + cellData.ColumnKey);
                Grid.SetColumn(chevron, 1);
                root.Children.Add(chevron);
            }

            var text = new TextBlock
            {
                Text = cellData.DisplayText ?? string.Empty,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                IsHitTestVisible = false,
            };
            text.SetResourceReference(TextBlock.ForegroundProperty, cellData.IsReadOnly ? "PgMutedTextBrush" : "PgPrimaryTextBrush");
            AutomationProperties.SetAutomationId(text, "surface.group-caption." + cellData.RowKey + "." + cellData.ColumnKey);
            Grid.SetColumn(text, 2);
            root.Children.Add(text);

            return root;
        }

        private void UpdateVisualState(GridCellSurfaceItem cellData)
        {
            Canvas.SetZIndex(this, cellData.IsEditing ? 100 : (cellData.IsCurrent ? 20 : 0));

            if (cellData.IsEditing)
            {
                Background = Brushes.Transparent;
                BorderBrush = Brushes.Transparent;
                BorderThickness = new Thickness(0d);
                SetResourceReference(ForegroundProperty, "PgPrimaryTextBrush");
                return;
            }

            // Selection background
            if (cellData.IsSelected)
            {
                SetResourceReference(BackgroundProperty, "PgSelectionBackgroundBrush");
                SetResourceReference(ForegroundProperty, "PgSelectionTextBrush");
            }
            else if (cellData.IsCurrentRow)
            {
                SetResourceReference(BackgroundProperty, "PgCurrentRowBackgroundBrush");
                SetResourceReference(ForegroundProperty, "PgPrimaryTextBrush");
            }
            else if (cellData.IsCurrent)
            {
                SetResourceReference(BackgroundProperty, "PgHoverBackgroundBrush");
                SetResourceReference(ForegroundProperty, "PgPrimaryTextBrush");
            }
            else if (cellData.HasValidationError)
            {
                SetResourceReference(BackgroundProperty, "PgLoadMoreBackgroundBrush");
                SetResourceReference(ForegroundProperty, "PgLoadMoreTextBrush");
            }
            else
            {
                SetResourceReference(BackgroundProperty, "PgRowBackgroundBrush");
                SetResourceReference(ForegroundProperty, "PgPrimaryTextBrush");
            }

            if (cellData.IsReadOnly)
            {
                SetResourceReference(ForegroundProperty, "PgMutedTextBrush");
            }

            if (cellData.IsCurrent)
            {
                this.BorderThickness = new Thickness(2);
                SetResourceReference(BorderBrushProperty, "PgAccentBrush");
            }
            else if (cellData.IsCurrentRow)
            {
                this.BorderThickness = new Thickness(0.5);
                SetResourceReference(BorderBrushProperty, "PgCurrentRowBorderBrush");
            }
            else
            {
                this.BorderThickness = new Thickness(0.5);
                SetResourceReference(BorderBrushProperty, "PgGridLineBrush");
            }
        }

        /// <summary>
        /// Bounds komórki (position i rozmiar).
        /// </summary>
        public GridBounds Bounds
        {
            get { return (GridBounds)GetValue(BoundsProperty); }
            set { SetValue(BoundsProperty, value); }
        }

        public static readonly DependencyProperty BoundsProperty =
            DependencyProperty.Register(
                nameof(Bounds),
                typeof(GridBounds),
                typeof(GridCellPresenter),
                new PropertyMetadata(GridBounds.Empty, OnBoundsChanged));

        private static void OnBoundsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var presenter = (GridCellPresenter)d;
            var bounds = (GridBounds)e.NewValue;
            
            Canvas.SetLeft(presenter, bounds.X);
            Canvas.SetTop(presenter, bounds.Y);
            presenter.Width = bounds.Width;
            presenter.Height = bounds.Height;
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new GridCellPresenterAutomationPeer(this);
        }

        private sealed class GridCellPresenterAutomationPeer : FrameworkElementAutomationPeer
        {
            public GridCellPresenterAutomationPeer(GridCellPresenter owner)
                : base(owner)
            {
            }

            protected override AutomationControlType GetAutomationControlTypeCore()
            {
                return AutomationControlType.DataItem;
            }

            protected override string GetClassNameCore()
            {
                return nameof(GridCellPresenter);
            }

            protected override string GetNameCore()
            {
                var owner = (GridCellPresenter)Owner;
                return AutomationProperties.GetName(owner) ?? base.GetNameCore();
            }
        }
    }

    public sealed class GridCellEditingTextChangedEventArgs : EventArgs
    {
        public GridCellEditingTextChangedEventArgs(string rowKey, string columnKey, string text, UniversalEditorValueChangeKind changeKind)
        {
            RowKey = rowKey ?? string.Empty;
            ColumnKey = columnKey ?? string.Empty;
            Text = text ?? string.Empty;
            ChangeKind = changeKind;
        }

        public string RowKey { get; }

        public string ColumnKey { get; }

        public string Text { get; }

        public UniversalEditorValueChangeKind ChangeKind { get; }
    }
}
