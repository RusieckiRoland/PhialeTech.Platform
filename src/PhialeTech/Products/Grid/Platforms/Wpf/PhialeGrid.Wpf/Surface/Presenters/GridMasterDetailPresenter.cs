using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using PhialeGrid.Core.Surface;
using PhialeGrid.Localization;
using PhialeTech.PhialeGrid.Wpf.Controls;

namespace PhialeTech.PhialeGrid.Wpf.Surface.Presenters
{
    public sealed class GridMasterDetailPresenter : ContentControl
    {
        private const double MinColumnWidth = 80d;
        private readonly Border _border;
        private readonly StackPanel _headerPanel;
        private readonly StackPanel _filterPanel;
        private readonly StackPanel _rowsPanel;
        private readonly ScrollViewer _headerScrollViewer;
        private readonly ScrollViewer _filterScrollViewer;
        private readonly ScrollViewer _rowsScrollViewer;
        private GridMasterDetailMasterRowModel _currentMasterRow;
        private bool _isSyncingHorizontalScroll;

        public GridMasterDetailPresenter()
        {
            _headerPanel = new StackPanel { Orientation = Orientation.Horizontal };
            _filterPanel = new StackPanel { Orientation = Orientation.Horizontal };
            _rowsPanel = new StackPanel();

            _headerScrollViewer = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden,
                VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
                CanContentScroll = false,
                Content = _headerPanel,
            };

            _filterScrollViewer = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden,
                VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
                CanContentScroll = false,
                Content = _filterPanel,
            };

            _rowsScrollViewer = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = _rowsPanel,
            };
            _rowsScrollViewer.SetResourceReference(BackgroundProperty, "PgDetailsBackgroundBrush");
            _rowsScrollViewer.ScrollChanged += HandleRowsScrollChanged;

            var layoutRoot = new Grid();
            layoutRoot.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            layoutRoot.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            layoutRoot.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1d, GridUnitType.Star) });

            Grid.SetRow(_headerScrollViewer, 0);
            layoutRoot.Children.Add(_headerScrollViewer);
            Grid.SetRow(_filterScrollViewer, 1);
            layoutRoot.Children.Add(_filterScrollViewer);
            Grid.SetRow(_rowsScrollViewer, 2);
            layoutRoot.Children.Add(_rowsScrollViewer);

            _border = new Border
            {
                Padding = new Thickness(10),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Child = layoutRoot,
            };
            _border.SetResourceReference(Border.BackgroundProperty, "PgDetailsBackgroundBrush");
            _border.SetResourceReference(Border.BorderBrushProperty, "PgDetailsBorderBrush");
            SetResourceReference(ForegroundProperty, "PgPrimaryTextBrush");

            Content = _border;
            ClipToBounds = true;
            Focusable = false;
        }

        public GridOverlaySurfaceItem OverlayData
        {
            get => (GridOverlaySurfaceItem)GetValue(OverlayDataProperty);
            set => SetValue(OverlayDataProperty, value);
        }

        public static readonly DependencyProperty OverlayDataProperty =
            DependencyProperty.Register(
                nameof(OverlayData),
                typeof(GridOverlaySurfaceItem),
                typeof(GridMasterDetailPresenter),
                new PropertyMetadata(null, OnOverlayDataChanged));

        public GridBounds Bounds
        {
            get => (GridBounds)GetValue(BoundsProperty);
            set => SetValue(BoundsProperty, value);
        }

        public static readonly DependencyProperty BoundsProperty =
            DependencyProperty.Register(
                nameof(Bounds),
                typeof(GridBounds),
                typeof(GridMasterDetailPresenter),
                new PropertyMetadata(GridBounds.Empty, OnBoundsChanged));

        private static void OnOverlayDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var presenter = (GridMasterDetailPresenter)d;
            presenter.ApplyOverlay((GridOverlaySurfaceItem)e.NewValue);
        }

        private static void OnBoundsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var presenter = (GridMasterDetailPresenter)d;
            var bounds = (GridBounds)e.NewValue;
            Canvas.SetLeft(presenter, bounds.X);
            Canvas.SetTop(presenter, bounds.Y);
            presenter.Width = bounds.Width;
            presenter.Height = bounds.Height;
        }

        private void ApplyOverlay(GridOverlaySurfaceItem overlay)
        {
            if (!(overlay?.Payload is GridMasterDetailMasterRowModel masterRow))
            {
                ClearState();
                AutomationProperties.SetName(this, string.Empty);
                AutomationProperties.SetAutomationId(this, string.Empty);
                return;
            }

            var requiresStructureRebuild = !ReferenceEquals(masterRow, _currentMasterRow);
            var filterFocusState = requiresStructureRebuild ? CaptureFilterFocusState() : null;

            AutomationProperties.SetAutomationId(this, "surface.master-detail." + masterRow.Node.PathId);
            AutomationProperties.SetName(this, "Details for " + masterRow.Caption);
            AutomationProperties.SetAutomationId(_headerScrollViewer, "surface.master-detail.header-scroll." + masterRow.Node.PathId);
            AutomationProperties.SetAutomationId(_filterScrollViewer, "surface.master-detail.filter-scroll." + masterRow.Node.PathId);
            AutomationProperties.SetAutomationId(_rowsScrollViewer, "surface.master-detail.rows-scroll." + masterRow.Node.PathId);
            if (requiresStructureRebuild)
            {
                BuildHeaderPanel(masterRow);
                BuildFilterPanel(masterRow);
            }

            BuildDetailRows(masterRow);
            SyncHorizontalOffsets(_rowsScrollViewer.HorizontalOffset);
            _currentMasterRow = masterRow;

            if (requiresStructureRebuild)
            {
                RestoreFilterFocus(masterRow, filterFocusState);
            }
        }

        private void BuildHeaderPanel(GridMasterDetailMasterRowModel masterRow)
        {
            _headerPanel.Children.Clear();
            AppendStructuralHeaderCells(_headerPanel, masterRow);
            foreach (var column in masterRow.DetailColumns)
            {
                var headerText = new TextBlock
                {
                    Margin = new Thickness(8, 6, 8, 6),
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold,
                    Text = column.Header,
                    TextWrapping = TextWrapping.Wrap,
                };
                headerText.SetResourceReference(TextBlock.ForegroundProperty, "PgPrimaryTextBrush");

                var headerCell = new Border
                {
                    Width = Math.Max(column.Width, MinColumnWidth),
                    BorderThickness = new Thickness(0, 0, 1, 1),
                    Child = headerText,
                };
                AutomationProperties.SetAutomationId(headerCell, "surface.master-detail.header." + masterRow.Node.PathId + "." + column.ColumnId);
                headerCell.SetResourceReference(Border.BorderBrushProperty, "PgDetailsBorderLightBrush");
                headerCell.SetResourceReference(Border.BackgroundProperty, "PgMasterDetailHeaderBackgroundBrush");

                _headerPanel.Children.Add(headerCell);
            }
        }

        private void BuildFilterPanel(GridMasterDetailMasterRowModel masterRow)
        {
            _filterPanel.Children.Clear();
            _filterPanel.Margin = new Thickness(0, 0, 0, 8);
            AppendStructuralFilterCells(_filterPanel, masterRow);

            foreach (var column in masterRow.DetailColumns)
            {
                var textBox = new TextBox
                {
                    IsEnabled = true,
                    IsReadOnly = false,
                };
                var filterStyle = TryFindStyle("PgGridFilterTextBoxStyle");
                if (filterStyle != null)
                {
                    textBox.Style = filterStyle;
                }
                textBox.Tag = column.ColumnId;
                AutomationProperties.SetAutomationId(textBox, "surface.master-detail.filter." + masterRow.Node.PathId + "." + column.ColumnId);
                AutomationProperties.SetName(textBox, "Filter " + column.Header);
                BindingOperations.SetBinding(
                    textBox,
                    TextBox.TextProperty,
                    new Binding(nameof(GridMasterDetailColumnModel.FilterText))
                    {
                        Source = column,
                        Mode = BindingMode.TwoWay,
                        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                    });

                var filterCell = new Border
                {
                    Width = Math.Max(column.Width, MinColumnWidth),
                    BorderThickness = new Thickness(0, 0, 1, 1),
                    Child = textBox,
                };
                AutomationProperties.SetAutomationId(filterCell, "surface.master-detail.filter-cell." + masterRow.Node.PathId + "." + column.ColumnId);
                filterCell.SetResourceReference(Border.BorderBrushProperty, "PgDetailsBorderLightBrush");
                filterCell.SetResourceReference(Border.BackgroundProperty, "PgFilterRowBackgroundBrush");

                _filterPanel.Children.Add(filterCell);
            }
        }

        private void BuildDetailRows(GridMasterDetailMasterRowModel masterRow)
        {
            _rowsPanel.Children.Clear();
            var rowIndex = 0;
            EnsureCurrentDetailRow(masterRow);

            foreach (var detailRow in masterRow.DetailRows)
            {
                rowIndex++;
                var isCurrentRow = masterRow.ShowRowIndicator &&
                    masterRow.Owner.IsCurrentMasterDetailRow(masterRow.Node.PathId, detailRow);
                var rowPanel = new StackPanel { Orientation = Orientation.Horizontal };
                AutomationProperties.SetAutomationId(rowPanel, "surface.master-detail.row." + masterRow.Node.PathId + "." + rowIndex);
                AppendStructuralRowCells(rowPanel, masterRow, detailRow, rowIndex, isCurrentRow);
                foreach (var column in masterRow.DetailColumns)
                {
                    var value = detailRow[column.ColumnId];
                    var valueText = new TextBlock
                    {
                        Margin = new Thickness(8, 6, 8, 6),
                        Text = Convert.ToString(value, CultureInfo.CurrentCulture) ?? string.Empty,
                        TextWrapping = TextWrapping.Wrap,
                        TextTrimming = TextTrimming.None,
                    };
                    valueText.SetResourceReference(TextBlock.ForegroundProperty, "PgPrimaryTextBrush");

                    var detailCell = new Border
                    {
                        Width = Math.Max(column.Width, MinColumnWidth),
                        BorderThickness = new Thickness(0, 0, 1, 1),
                        Child = valueText,
                    };
                    AutomationProperties.SetAutomationId(detailCell, "surface.master-detail.detail-cell." + masterRow.Node.PathId + "." + rowIndex + "." + column.ColumnId);
                    detailCell.SetResourceReference(Border.BorderBrushProperty, "PgDetailsBorderLightBrush");
                    detailCell.SetResourceReference(
                        Border.BackgroundProperty,
                        isCurrentRow ? "PgCurrentRowBackgroundBrush" : "PgMasterDetailDetailBackgroundBrush");
                    AttachDetailRowActivation(detailCell, masterRow, detailRow);

                    rowPanel.Children.Add(detailCell);
                }

                _rowsPanel.Children.Add(rowPanel);
            }
        }

        private void AppendStructuralHeaderCells(Panel panel, GridMasterDetailMasterRowModel masterRow)
        {
            var cornerWidth = ResolveCornerWidth(masterRow);
            if (cornerWidth > 0d)
            {
                panel.Children.Add(CreateStructuralCell(cornerWidth, BuildOptionsButton(masterRow), "PgMasterDetailHeaderBackgroundBrush"));
            }
        }

        private void AppendStructuralFilterCells(Panel panel, GridMasterDetailMasterRowModel masterRow)
        {
            var cornerWidth = ResolveCornerWidth(masterRow);
            if (cornerWidth > 0d)
            {
                panel.Children.Add(CreateStructuralCell(
                    cornerWidth,
                    BuildFilterIcon(masterRow),
                    "PgFilterRowBackgroundBrush"));
            }
        }

        private void AppendStructuralRowCells(Panel panel, GridMasterDetailMasterRowModel masterRow, GridMasterDetailDetailRowModel detailRow, int rowIndex, bool isCurrentRow)
        {
            if (masterRow.RowIndicatorWidth > 0d)
            {
                var indicatorCell = CreateStructuralCell(
                    masterRow.RowIndicatorWidth,
                    isCurrentRow ? BuildCurrentIndicatorGlyph() : new Border { Background = Brushes.Transparent },
                    isCurrentRow ? "PgRowIndicatorColumnCurrentBackgroundBrush" : "PgRowIndicatorColumnBackgroundBrush");
                AutomationProperties.SetAutomationId(indicatorCell, "surface.master-detail.row-indicator." + masterRow.Node.PathId + "." + rowIndex);
                AttachDetailRowActivation(indicatorCell, masterRow, detailRow);
                panel.Children.Add(indicatorCell);
            }

            if (masterRow.SelectionCheckboxWidth > 0d)
            {
                var selectionCell = CreateStructuralCell(
                    masterRow.SelectionCheckboxWidth,
                    masterRow.ShowSelectionCheckbox
                        ? (UIElement)BuildSelectionCheckbox(detailRow)
                        : new Border { Background = Brushes.Transparent },
                    isCurrentRow ? "PgRowIndicatorColumnCurrentBackgroundBrush" : "PgRowIndicatorColumnBackgroundBrush");
                if (!masterRow.ShowSelectionCheckbox)
                {
                    AttachDetailRowActivation(selectionCell, masterRow, detailRow);
                }

                panel.Children.Add(selectionCell);
            }

            if (masterRow.ShowRowNumbers && masterRow.RowNumberWidth > 0d)
            {
                var rowNumberCell = CreateStructuralCell(
                    masterRow.RowNumberWidth,
                    BuildRowNumberText(rowIndex),
                    isCurrentRow ? "PgRowIndicatorColumnCurrentBackgroundBrush" : "PgRowIndicatorColumnBackgroundBrush");
                AutomationProperties.SetAutomationId(rowNumberCell, "surface.master-detail.row-number." + masterRow.Node.PathId + "." + rowIndex);
                AttachDetailRowActivation(rowNumberCell, masterRow, detailRow);
                panel.Children.Add(rowNumberCell);
            }
        }

        private static double ResolveCornerWidth(GridMasterDetailMasterRowModel masterRow)
        {
            return Math.Max(0d, masterRow.RowIndicatorWidth) +
                Math.Max(0d, masterRow.SelectionCheckboxWidth) +
                Math.Max(0d, masterRow.RowNumberWidth);
        }

        private static Border CreateStructuralCell(double width, UIElement content, string backgroundResourceKey, double minWidth = 0d)
        {
            var cell = new Border
            {
                Width = Math.Max(width, minWidth),
                BorderThickness = new Thickness(0, 0, 1, 1),
                Child = content,
            };
            cell.SetResourceReference(Border.BorderBrushProperty, "PgDetailsBorderLightBrush");
            cell.SetResourceReference(Border.BackgroundProperty, backgroundResourceKey);
            return cell;
        }

        private static UIElement BuildOptionsButton(GridMasterDetailMasterRowModel masterRow)
        {
            var button = new Button
            {
                Margin = new Thickness(2),
                Padding = new Thickness(0),
                Content = "...",
                Style = masterRow.Owner.TryFindResource("PgMasterDetailCornerOptionsButtonStyle") as Style,
                Focusable = false,
                IsEnabled = true,
            };
            AutomationProperties.SetAutomationId(button, "surface.master-detail.options." + masterRow.Node.PathId);
            AutomationProperties.SetName(button, "Grid options");
            button.Click += (sender, args) =>
            {
                var placementTarget = sender as FrameworkElement;
                if (placementTarget == null)
                {
                    return;
                }

                var anchorPoint = placementTarget.TranslatePoint(
                    new Point(0d, placementTarget.ActualHeight),
                    masterRow.Owner);
                var contextMenu = masterRow.Owner.CreateGridOptionsContextMenuForPlacementTarget(masterRow.Owner);
                contextMenu.PlacementTarget = masterRow.Owner;
                contextMenu.Placement = PlacementMode.RelativePoint;
                contextMenu.HorizontalOffset = anchorPoint.X;
                contextMenu.VerticalOffset = anchorPoint.Y;
                placementTarget.ContextMenu = contextMenu;
                placementTarget.Dispatcher.BeginInvoke(
                    new Action(() =>
                    {
                        if (placementTarget.IsLoaded && masterRow.Owner.IsLoaded)
                        {
                            contextMenu.IsOpen = true;
                        }
                    }),
                    DispatcherPriority.Input);
            };
            return button;
        }

        private static UIElement BuildRowNumberText(int rowIndex)
        {
            var text = new TextBlock
            {
                Text = rowIndex.ToString(CultureInfo.CurrentCulture),
                FontSize = 10,
                FontWeight = FontWeights.Normal,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
            };
            text.SetResourceReference(TextBlock.ForegroundProperty, "PgRowNumberTextBrush");
            return text;
        }

        private static UIElement BuildCurrentIndicatorGlyph()
        {
            var host = new Grid
            {
                Width = 14,
                Height = 14,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                UseLayoutRounding = true,
                SnapsToDevicePixels = true,
            };

            var glyph = new Polygon
            {
                Width = 8,
                Height = 10,
                Stretch = Stretch.Fill,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Points = new PointCollection
                {
                    new Point(0, 0),
                    new Point(8, 5),
                    new Point(0, 10),
                },
            };
            glyph.SetResourceReference(Shape.FillProperty, "PgAccentBrush");
            host.Children.Add(glyph);
            return host;
        }

        private static UIElement BuildSelectionCheckbox(GridMasterDetailDetailRowModel detailRow)
        {
            var checkBox = new CheckBox
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Focusable = false,
            };
            BindingOperations.SetBinding(
                checkBox,
                ToggleButton.IsCheckedProperty,
                new Binding(nameof(GridMasterDetailDetailRowModel.IsMarkerChecked))
                {
                    Source = detailRow,
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                });
            return checkBox;
        }

        private UIElement BuildFilterIcon(GridMasterDetailMasterRowModel masterRow)
        {
            var icon = new Path
            {
                Data = Geometry.Parse("F1 M 0 1 L 12 1 L 7.2 6.2 L 7.2 10.6 L 4.8 9.2 L 4.8 6.2 Z"),
            };
            var iconStyle = masterRow.Owner.TryFindResource("PgGridFilterIconPathStyle") as Style;
            if (iconStyle != null)
            {
                icon.Style = iconStyle;
            }

            AutomationProperties.SetAutomationId(icon, "surface.master-detail.filter-icon." + masterRow.Node.PathId);
            AutomationProperties.SetName(icon, "Filter");

            return icon;
        }

        private void ClearState()
        {
            _currentMasterRow = null;
            _headerPanel.Children.Clear();
            _filterPanel.Children.Clear();
            _rowsPanel.Children.Clear();
        }

        private static void EnsureCurrentDetailRow(GridMasterDetailMasterRowModel masterRow)
        {
            if (masterRow == null || masterRow.DetailRows == null || masterRow.DetailRows.Count == 0)
            {
                return;
            }

            if (masterRow.DetailRows.Any(detailRow => masterRow.Owner.IsCurrentMasterDetailRow(masterRow.Node.PathId, detailRow)))
            {
                return;
            }

            masterRow.Owner.SetCurrentMasterDetailRow(masterRow.Node.PathId, masterRow.DetailRows[0]);
        }

        private void AttachDetailRowActivation(FrameworkElement element, GridMasterDetailMasterRowModel masterRow, GridMasterDetailDetailRowModel detailRow)
        {
            if (element == null || masterRow == null || detailRow == null)
            {
                return;
            }

            element.MouseLeftButtonDown += (sender, args) =>
            {
                masterRow.Owner.SetCurrentMasterDetailRow(masterRow.Node.PathId, detailRow);
                BuildDetailRows(masterRow);
            };
        }

        private void HandleRowsScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (_isSyncingHorizontalScroll)
            {
                return;
            }

            SyncHorizontalOffsets(e.HorizontalOffset);
        }

        private void SyncHorizontalOffsets(double horizontalOffset)
        {
            _isSyncingHorizontalScroll = true;
            try
            {
                _headerScrollViewer.ScrollToHorizontalOffset(horizontalOffset);
                _filterScrollViewer.ScrollToHorizontalOffset(horizontalOffset);
            }
            finally
            {
                _isSyncingHorizontalScroll = false;
            }
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new GridMasterDetailPresenterAutomationPeer(this);
        }

        private static Style TryFindStyle(string resourceKey)
        {
            return Application.Current?.TryFindResource(resourceKey) as Style;
        }

        private FilterFocusState CaptureFilterFocusState()
        {
            var focusedTextBox = Keyboard.FocusedElement as TextBox;
            if (focusedTextBox == null || !IsDescendantOfThisPresenter(focusedTextBox))
            {
                return null;
            }

            var columnId = focusedTextBox.Tag as string;
            if (string.IsNullOrWhiteSpace(columnId))
            {
                return null;
            }

            return new FilterFocusState(columnId, focusedTextBox.SelectionStart, focusedTextBox.SelectionLength);
        }

        private void RestoreFilterFocus(GridMasterDetailMasterRowModel masterRow, FilterFocusState filterFocusState)
        {
            if (masterRow == null || filterFocusState == null || !IsLoaded)
            {
                return;
            }

            var target = FindFilterTextBox(masterRow.Node.PathId, filterFocusState.ColumnId);
            if (target == null)
            {
                return;
            }

            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (!IsLoaded)
                {
                    return;
                }

                var latestTarget = FindFilterTextBox(masterRow.Node.PathId, filterFocusState.ColumnId);
                if (latestTarget == null)
                {
                    return;
                }

                latestTarget.Focus();
                Keyboard.Focus(latestTarget);

                var textLength = latestTarget.Text?.Length ?? 0;
                var selectionStart = Math.Max(0, Math.Min(filterFocusState.SelectionStart, textLength));
                var selectionLength = Math.Max(0, Math.Min(filterFocusState.SelectionLength, textLength - selectionStart));
                latestTarget.Select(selectionStart, selectionLength);
            }), System.Windows.Threading.DispatcherPriority.Input);
        }

        private TextBox FindFilterTextBox(string pathId, string columnId)
        {
            var automationId = "surface.master-detail.filter." + (pathId ?? string.Empty) + "." + (columnId ?? string.Empty);
            return FindDescendantFilterTextBoxByAutomationId(_filterPanel, automationId);
        }

        private static TextBox FindDescendantFilterTextBoxByAutomationId(DependencyObject root, string automationId)
        {
            if (root == null || string.IsNullOrWhiteSpace(automationId))
            {
                return null;
            }

            var childCount = VisualTreeHelper.GetChildrenCount(root);
            for (var index = 0; index < childCount; index++)
            {
                var child = VisualTreeHelper.GetChild(root, index);
                if (child is TextBox textBox &&
                    string.Equals(AutomationProperties.GetAutomationId(textBox), automationId, StringComparison.OrdinalIgnoreCase))
                {
                    return textBox;
                }

                var nested = FindDescendantFilterTextBoxByAutomationId(child, automationId);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }

        private bool IsDescendantOfThisPresenter(DependencyObject element)
        {
            var current = element;
            while (current != null)
            {
                if (ReferenceEquals(current, this))
                {
                    return true;
                }

                current = current is Visual || current is System.Windows.Media.Media3D.Visual3D
                    ? VisualTreeHelper.GetParent(current)
                    : LogicalTreeHelper.GetParent(current);
            }

            return false;
        }

        private sealed class FilterFocusState
        {
            public FilterFocusState(string columnId, int selectionStart, int selectionLength)
            {
                ColumnId = columnId ?? string.Empty;
                SelectionStart = selectionStart;
                SelectionLength = selectionLength;
            }

            public string ColumnId { get; }

            public int SelectionStart { get; }

            public int SelectionLength { get; }
        }

        private sealed class GridMasterDetailPresenterAutomationPeer : FrameworkElementAutomationPeer
        {
            public GridMasterDetailPresenterAutomationPeer(GridMasterDetailPresenter owner)
                : base(owner)
            {
            }

            protected override AutomationControlType GetAutomationControlTypeCore()
            {
                return AutomationControlType.Group;
            }

            protected override string GetClassNameCore()
            {
                return nameof(GridMasterDetailPresenter);
            }

            protected override string GetNameCore()
            {
                var owner = (GridMasterDetailPresenter)Owner;
                return AutomationProperties.GetName(owner) ?? base.GetNameCore();
            }
        }
    }
}
