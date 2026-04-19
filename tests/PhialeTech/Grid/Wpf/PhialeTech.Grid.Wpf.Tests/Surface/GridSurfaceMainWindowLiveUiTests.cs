using System;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using PhialeGrid.Core.HitTesting;
using PhialeGrid.Core.Rendering;
using PhialeGrid.Localization;
using PhialeTech.Components.Shared.ViewModels;
using PhialeTech.Components.Wpf;
using PhialeTech.PhialeGrid.Wpf.Controls;
using PhialeTech.PhialeGrid.Wpf.Surface;
using PhialeTech.PhialeGrid.Wpf.Surface.Presenters;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;
using PhialeTech.Components.Shared.Model;
using WpfGrid = PhialeTech.PhialeGrid.Wpf.Controls.PhialeGrid;

namespace PhialeGrid.Wpf.Tests.Surface
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    [NonParallelizable]
    public sealed class GridSurfaceMainWindowLiveUiTests
    {
        [Test]
        public void MainWindow_WhenGroupedRowCellClicked_ExpandsVisibleRows()
        {
            var window = new MainWindow();

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(window);

                var viewModel = (DemoShellViewModel)window.DataContext;
                var grid = FindDemoGrid(window);
                viewModel.SelectExample("grouping");
                GridSurfaceTestHost.FlushDispatcher(window);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var groupRow = surfaceHost.CurrentSnapshot.Rows[0];
                var groupCell = surfaceHost.CurrentSnapshot.Cells
                    .First(cell => cell.RowKey == groupRow.RowKey && cell.ColumnKey == "Category");

                GridSurfaceTestHost.ClickPointViaRoutedUi(
                    surfaceHost,
                    groupCell.Bounds.X + (groupCell.Bounds.Width / 2d),
                    groupCell.Bounds.Y + (groupCell.Bounds.Height / 2d));
                GridSurfaceTestHost.FlushDispatcher(window);

                Assert.Multiple(() =>
                {
                    Assert.That(surfaceHost.CurrentSnapshot.Rows.Count, Is.GreaterThan(10), "Grouped row click should expand the visible dataset in the live demo.");
                    Assert.That(surfaceHost.CurrentSnapshot.CurrentCell, Is.Null);
                    Assert.That(surfaceHost.CurrentSnapshot.Overlays.Any(overlay => overlay.Kind == PhialeGrid.Core.Surface.GridOverlayKind.CurrentRecord && overlay.TargetKey == groupRow.RowKey), Is.False);
                    Assert.That(grid.HasSelectedRows, Is.False);
                    Assert.That(grid.SelectionStatusText, Does.Contain("Selected rows: 0"));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void MainWindow_WhenGroupingUsesUtilityColumns_GroupRowsKeepStableLeftGeometryWithoutStep()
        {
            var window = new MainWindow();

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(window);

                var viewModel = (DemoShellViewModel)window.DataContext;
                var grid = FindDemoGrid(window);
                viewModel.SelectExample("grouping");
                GridSurfaceTestHost.FlushDispatcher(window);

                grid.MultiSelect = true;
                grid.ShowNb = true;
                GridSurfaceTestHost.FlushDispatcher(window);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var initialSnapshot = surfaceHost.CurrentSnapshot;
                var groupRow = initialSnapshot.Rows.First(row => row.IsGroupHeader);
                var groupCell = initialSnapshot.Cells.Single(cell => cell.RowKey == groupRow.RowKey && cell.ColumnKey == "Category");

                GridSurfaceTestHost.ClickPointViaRoutedUi(
                    surfaceHost,
                    groupCell.Bounds.X + (groupCell.Bounds.Width / 2d),
                    groupCell.Bounds.Y + (groupCell.Bounds.Height / 2d));
                GridSurfaceTestHost.FlushDispatcher(window);

                var expandedSnapshot = surfaceHost.CurrentSnapshot;
                var expandedGroupStateHeader = expandedSnapshot.Headers.Single(header =>
                    header.Kind == PhialeGrid.Core.Surface.GridHeaderKind.RowHeader &&
                    header.HeaderKey == groupRow.RowKey);
                var expandedGroupNumberHeader = expandedSnapshot.Headers.Single(header =>
                    header.Kind == PhialeGrid.Core.Surface.GridHeaderKind.RowNumberHeader &&
                    header.HeaderKey == groupRow.RowKey);
                var expandedGroupCell = expandedSnapshot.Cells.Single(cell =>
                    cell.RowKey == groupRow.RowKey &&
                    cell.ColumnKey == "Category");
                var dataRow = expandedSnapshot.Rows.First(row => row.RepresentsDataRecord);
                var dataStateHeader = expandedSnapshot.Headers.Single(header =>
                    header.Kind == PhialeGrid.Core.Surface.GridHeaderKind.RowHeader &&
                    header.HeaderKey == dataRow.RowKey);
                var dataNumberHeader = expandedSnapshot.Headers.Single(header =>
                    header.Kind == PhialeGrid.Core.Surface.GridHeaderKind.RowNumberHeader &&
                    header.HeaderKey == dataRow.RowKey);
                var dataCell = expandedSnapshot.Cells.Single(cell => cell.RowKey == dataRow.RowKey && cell.ColumnKey == "Category");
                var groupPresenter = FindCellPresenter(surfaceHost, groupRow.RowKey, "Category");
                var groupRoot = GridSurfaceTestHost.FindElementByAutomationId<FrameworkElement>(surfaceHost, "surface.group-cell." + groupRow.RowKey + ".Category");
                var groupToggle = GridSurfaceTestHost.FindElementByAutomationId<Path>(surfaceHost, "surface.group-toggle." + groupRow.RowKey + ".Category");
                var groupCaption = GridSurfaceTestHost.FindElementByAutomationId<TextBlock>(surfaceHost, "surface.group-caption." + groupRow.RowKey + ".Category");
                var groupRootLeft = groupRoot?.TranslatePoint(new Point(0d, 0d), surfaceHost).X ?? double.NaN;

                Assert.Multiple(() =>
                {
                    Assert.That(expandedGroupStateHeader.Bounds.Width, Is.EqualTo(grid.ResolvedRowStateWidth).Within(1d));
                    Assert.That(dataStateHeader.Bounds.Width, Is.EqualTo(grid.ResolvedRowStateWidth).Within(1d));
                    Assert.That(expandedGroupNumberHeader.Bounds.Width, Is.EqualTo(grid.ResolvedRowNumbersWidth).Within(1d));
                    Assert.That(dataNumberHeader.Bounds.Width, Is.EqualTo(grid.ResolvedRowNumbersWidth).Within(1d));
                    Assert.That(expandedGroupCell.Bounds.X, Is.EqualTo(dataCell.Bounds.X).Within(1d));
                    Assert.That(groupPresenter, Is.Not.Null);
                    Assert.That(groupPresenter.CellData?.IsGroupCaptionCell, Is.True);
                    Assert.That(groupRoot, Is.Not.Null);
                    Assert.That(groupToggle, Is.Not.Null);
                    Assert.That(groupCaption, Is.Not.Null);
                    Assert.That(groupCaption.Text, Does.StartWith("Category:"));
                    Assert.That(groupRootLeft, Is.EqualTo(expandedGroupCell.Bounds.X).Within(1d));
                    Assert.That(GridSurfaceTestHost.ReadVisibleText(groupPresenter), Does.Not.Contain("▼"));
                    Assert.That(GridSurfaceTestHost.ReadVisibleText(groupPresenter), Does.Not.Contain("▶"));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void MainWindow_WhenAllLeftUtilityFeaturesAreDisabled_RowStateColumnStillRemainsVisible()
        {
            var window = new MainWindow();

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(window);

                var viewModel = (DemoShellViewModel)window.DataContext;
                var grid = FindDemoGrid(window);
                viewModel.SelectExample("grouping");
                GridSurfaceTestHost.FlushDispatcher(window);

                grid.SelectCurrentRow = false;
                grid.MultiSelect = false;
                grid.ShowNb = false;
                grid.ShowCurrentRecordIndicator = false;
                GridSurfaceTestHost.FlushDispatcher(window);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var firstRowHeader = surfaceHost.CurrentSnapshot.Headers
                    .First(header => header.Kind == PhialeGrid.Core.Surface.GridHeaderKind.RowHeader);
                var firstGroupCell = surfaceHost.CurrentSnapshot.Cells
                    .First(cell => cell.RowKey == firstRowHeader.HeaderKey && cell.ColumnKey == "Category");
                var indicatorSlot = GridSurfaceTestHost.FindElementByAutomationId<Border>(surfaceHost, "surface.row-indicator." + firstRowHeader.HeaderKey);

                Assert.Multiple(() =>
                {
                    Assert.That(grid.ResolvedRowStateWidth, Is.GreaterThan(0d));
                    Assert.That(grid.ResolvedRowHeaderWidth, Is.EqualTo(grid.ResolvedRowStateWidth).Within(1d));
                    Assert.That(firstRowHeader.ShowRowIndicator, Is.False);
                    Assert.That(firstRowHeader.Bounds.Width, Is.EqualTo(grid.ResolvedRowStateWidth).Within(1d));
                    Assert.That(firstGroupCell.Bounds.X, Is.EqualTo(grid.ResolvedRowHeaderWidth).Within(1d));
                    Assert.That(indicatorSlot, Is.Not.Null);
                    Assert.That(indicatorSlot.ActualWidth, Is.GreaterThan(0d));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void MainWindow_WhenGroupingCodeTabIsOpened_ShowsReadableFileNameAndCompleteCodePreview()
        {
            var window = new MainWindow();

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(window);

                var viewModel = (DemoShellViewModel)window.DataContext;
                viewModel.SelectExample("grouping");
                viewModel.SelectedTabIndex = 1;
                GridSurfaceTestHost.FlushDispatcher(window);

                var comboBox = (ComboBox)window.FindName("CodeFileComboBox");
                var preview = (TextBox)window.FindName("CodePreviewTextBox");
                Assert.That(comboBox, Is.Not.Null);
                Assert.That(preview, Is.Not.Null);

                comboBox.ApplyTemplate();
                preview.ApplyTemplate();
                var toggle = comboBox.Template.FindName("DropDownToggle", comboBox) as ToggleButton;
                toggle?.ApplyTemplate();

                var selectionPresenter = toggle?.Template.FindName("SelectionPresenter", toggle) as FrameworkElement;
                var displayedText = selectionPresenter == null ? string.Empty : GridSurfaceTestHost.ReadVisibleText(selectionPresenter);
                var selectedFile = comboBox.SelectedItem as DemoCodeFileViewModel;
                var contentHost = preview.Template.FindName("PART_ContentHost", preview) as ScrollViewer;
                var templateRoot = VisualTreeHelper.GetChildrenCount(preview) > 0
                    ? VisualTreeHelper.GetChild(preview, 0) as Border
                    : null;

                Assert.Multiple(() =>
                {
                    Assert.That(selectedFile, Is.Not.Null);
                    Assert.That(selectedFile.FileName, Is.EqualTo("Example.xaml"));
                    Assert.That(displayedText, Is.EqualTo("Example.xaml"));
                    Assert.That(displayedText, Does.Not.Contain("DemoCodeFileViewModel"));
                    Assert.That(preview.Text, Does.Contain("x:Class=\"Demo.Snippets.ExampleHost\""));
                    Assert.That(preview.Text, Does.Contain("Groups=\"{Binding GridGroups, Mode=TwoWay}\""));
                    Assert.That(preview.Text.Length, Is.GreaterThan(250));
                    Assert.That(ReadBrushColor(preview.Background), Is.EqualTo(ReadBrushColor(window.TryFindResource("DemoCodeBackgroundBrush"))));
                    Assert.That(ReadBrushColor(preview.Foreground), Is.EqualTo(ReadBrushColor(window.TryFindResource("DemoCodeTextBrush"))));
                    Assert.That(templateRoot, Is.Not.Null);
                    Assert.That(ReadBrushColor(templateRoot?.Background), Is.EqualTo(ReadBrushColor(window.TryFindResource("DemoCodeBackgroundBrush"))));
                    Assert.That(contentHost, Is.Not.Null);
                    Assert.That(ReadBrushColor(contentHost?.Background), Is.EqualTo(Colors.Transparent));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void MainWindow_WhenSelectionCellClicked_SelectsCurrentCell()
        {
            var window = new MainWindow();

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(window);

                var viewModel = (DemoShellViewModel)window.DataContext;
                var grid = FindDemoGrid(window);
                viewModel.SelectExample("selection");
                GridSurfaceTestHost.FlushDispatcher(window);

                grid.ClearSelection();
                GridSurfaceTestHost.FlushDispatcher(window);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var targetRow = surfaceHost.CurrentSnapshot.Rows[1];
                var firstDataCell = surfaceHost.CurrentSnapshot.Cells
                    .First(cell => cell.RowKey == targetRow.RowKey &&
                                   cell.ColumnKey == "ObjectName");

                GridSurfaceTestHost.ClickPointViaRoutedUi(
                    surfaceHost,
                    firstDataCell.Bounds.X + (firstDataCell.Bounds.Width / 2d),
                    firstDataCell.Bounds.Y + (firstDataCell.Bounds.Height / 2d));
                GridSurfaceTestHost.FlushDispatcher(window);

                var updatedSnapshot = surfaceHost.CurrentSnapshot;
                var updatedCell = updatedSnapshot.Cells.Single(cell => cell.RowKey == firstDataCell.RowKey && cell.ColumnKey == firstDataCell.ColumnKey);
                var updatedRowHeader = updatedSnapshot.Headers.Single(header => header.Kind == PhialeGrid.Core.Surface.GridHeaderKind.RowHeader && header.HeaderKey == firstDataCell.RowKey);
                var indicatorElement = GridSurfaceTestHost.FindElementByAutomationId<System.Windows.FrameworkElement>(surfaceHost, "surface.row-indicator." + firstDataCell.RowKey);
                var currentRecordPresenter = GridSurfaceTestHost.FindVisualChildren<GridOverlayPresenter>(surfaceHost)
                    .FirstOrDefault(presenter => presenter.OverlayData?.Kind == PhialeGrid.Core.Surface.GridOverlayKind.CurrentRecord &&
                                                presenter.OverlayData.TargetKey == firstDataCell.RowKey);

                Assert.Multiple(() =>
                {
                    Assert.That(updatedSnapshot.CurrentCell, Is.Not.Null);
                    Assert.That(updatedSnapshot.CurrentCell.RowKey, Is.EqualTo(firstDataCell.RowKey));
                    Assert.That(updatedSnapshot.CurrentCell.ColumnKey, Is.EqualTo(firstDataCell.ColumnKey));
                    Assert.That(updatedSnapshot.Rows.Single(row => row.RowKey == firstDataCell.RowKey).IsSelected, Is.False);
                    Assert.That(updatedRowHeader.IsSelected, Is.False);
                    Assert.That(updatedRowHeader.ShowRowIndicator, Is.True);
                    Assert.That(updatedRowHeader.RowIndicatorState, Is.EqualTo(PhialeGrid.Core.Surface.GridRowIndicatorState.Current));
                    Assert.That(updatedCell.IsSelected, Is.True);
                    Assert.That(indicatorElement, Is.Not.Null);
                    Assert.That(updatedSnapshot.Overlays.Any(overlay => overlay.Kind == PhialeGrid.Core.Surface.GridOverlayKind.CurrentRecord && overlay.TargetKey == firstDataCell.RowKey), Is.True);
                    Assert.That(currentRecordPresenter, Is.Not.Null);
                    Assert.That(GridSurfaceTestHost.FindVisualChildren<Path>(currentRecordPresenter).Any(), Is.True);
                    Assert.That(grid.SelectionStatusText, Does.Contain("1"));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void MainWindow_WhenSelectionRowHeaderClicked_SelectsWholeRecord()
        {
            var window = new MainWindow();

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(window);

                var viewModel = (DemoShellViewModel)window.DataContext;
                var grid = FindDemoGrid(window);
                viewModel.SelectExample("selection");
                GridSurfaceTestHost.FlushDispatcher(window);

                grid.ClearSelection();
                GridSurfaceTestHost.FlushDispatcher(window);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var targetRowHeader = surfaceHost.CurrentSnapshot.Headers
                    .First(header => header.Kind == PhialeGrid.Core.Surface.GridHeaderKind.RowHeader &&
                                     header.HeaderKey == surfaceHost.CurrentSnapshot.Rows[1].RowKey);

                GridSurfaceTestHost.ClickPointViaRoutedUi(
                    surfaceHost,
                    targetRowHeader.Bounds.X + (targetRowHeader.Bounds.Width / 2d),
                    targetRowHeader.Bounds.Y + (targetRowHeader.Bounds.Height / 2d));
                GridSurfaceTestHost.FlushDispatcher(window);

                var updatedSnapshot = surfaceHost.CurrentSnapshot;
                var selectedHeader = updatedSnapshot.Headers.Single(header => header.Kind == PhialeGrid.Core.Surface.GridHeaderKind.RowHeader && header.HeaderKey == targetRowHeader.HeaderKey);
                var selectedRowCells = updatedSnapshot.Cells.Where(cell => cell.RowKey == targetRowHeader.HeaderKey).ToArray();

                Assert.Multiple(() =>
                {
                    Assert.That(selectedHeader.IsSelected, Is.False);
                    Assert.That(selectedRowCells.Length, Is.GreaterThan(0));
                    Assert.That(selectedRowCells.All(cell => cell.IsSelected), Is.True);
                    Assert.That(grid.SelectionStatusText, Does.Contain("1"));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void MainWindow_WhenSelectionExampleOpens_ShowsDedicatedRowIndicatorColumnByDefault()
        {
            var window = new MainWindow();

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(window);

                var viewModel = (DemoShellViewModel)window.DataContext;
                var grid = FindDemoGrid(window);
                viewModel.SelectExample("selection");
                GridSurfaceTestHost.FlushDispatcher(window);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var firstHeader = surfaceHost.CurrentSnapshot.Headers.First(header => header.Kind == PhialeGrid.Core.Surface.GridHeaderKind.RowHeader);
                var indicatorElement = GridSurfaceTestHost.FindElementByAutomationId<Border>(surfaceHost, "surface.row-indicator." + firstHeader.HeaderKey);
                var currentRowKey = surfaceHost.CurrentSnapshot.CurrentCell?.RowKey;
                var currentCellPresenter = FindCellPresenter(surfaceHost, currentRowKey, "ObjectName");
                var comparisonRowKey = surfaceHost.CurrentSnapshot.Rows.First(row => row.RowKey != currentRowKey).RowKey;
                var comparisonCellPresenter = FindCellPresenter(surfaceHost, comparisonRowKey, "ObjectName");
                var scenarioStatus = (TextBlock)window.FindName("SelectionScenarioStatusTextBlock");

                Assert.Multiple(() =>
                {
                    Assert.That(grid.SelectCurrentRow, Is.True);
                    Assert.That(grid.MultiSelect, Is.False);
                    Assert.That(surfaceHost.CurrentSnapshot.CurrentCell, Is.Not.Null);
                    Assert.That(firstHeader.ShowRowIndicator, Is.True);
                    Assert.That(firstHeader.RowIndicatorWidth, Is.GreaterThanOrEqualTo(18d));
                    Assert.That(firstHeader.SelectionCheckboxWidth, Is.EqualTo(0d));
                    Assert.That(firstHeader.Bounds.Width, Is.EqualTo(grid.ResolvedRowHeaderWidth).Within(1d));
                    Assert.That(indicatorElement, Is.Not.Null);
                    Assert.That(indicatorElement.ActualWidth, Is.GreaterThanOrEqualTo(18d));
                    Assert.That(indicatorElement.Background, Is.TypeOf<SolidColorBrush>());
                    Assert.That(currentCellPresenter, Is.Not.Null);
                    Assert.That(comparisonCellPresenter, Is.Not.Null);
                    Assert.That(ReadBrushColor(currentCellPresenter.Background), Is.Not.EqualTo(ReadBrushColor(comparisonCellPresenter.Background)));
                    Assert.That(scenarioStatus.Text, Does.Contain("MultiSelect off"));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void MainWindow_WhenLanguageIsPolish_GridOptionsMenuShowsPolishLabels()
        {
            var window = new MainWindow();

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(window);

                var viewModel = (DemoShellViewModel)window.DataContext;
                viewModel.LanguageCode = "pl";
                viewModel.SelectExample("grouping");
                GridSurfaceTestHost.FlushDispatcher(window);

                var grid = FindDemoGrid(window);
                var optionsButton = (Button)grid.FindName("GridOptionsButton");
                Assert.That(optionsButton, Is.Not.Null);

                optionsButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                GridSurfaceTestHost.FlushDispatcher(window);

                var contextMenu = optionsButton.ContextMenu;
                var headers = contextMenu.Items.OfType<MenuItem>().Select(item => item.Header?.ToString() ?? string.Empty).ToArray();
                var visibleTexts = GridSurfaceTestHost.FindVisualChildren<TextBlock>(contextMenu)
                    .Select(text => text.Text)
                    .Where(text => !string.IsNullOrWhiteSpace(text))
                    .ToArray();
                var showColumnsItem = contextMenu.Items.OfType<MenuItem>().Single(item => Equals(item.Header, "Pokaż kolumny"));
                var showAllColumnsItem = showColumnsItem.Items.OfType<MenuItem>().Single(item => Equals(item.Header, "Pokaż wszystkie kolumny"));

                Assert.Multiple(() =>
                {
                    Assert.That(contextMenu, Is.Not.Null);
                    Assert.That(headers, Does.Contain("Pokaż wskaźnik wiersza"));
                    Assert.That(headers, Does.Contain("Pokaż numery wierszy"));
                    Assert.That(headers, Does.Contain("Pokaż kolumny"));
                    Assert.That(headers, Does.Contain("Włącz wybór komórek"));
                    Assert.That(headers, Does.Contain("Włącz wybór zakresów"));
                    Assert.That(visibleTexts, Does.Contain("Stan wiersza"));
                    Assert.That(visibleTexts, Does.Contain("Numeracja wierszy"));
                    Assert.That(visibleTexts, Does.Contain("Kolumny"));
                    Assert.That(visibleTexts, Does.Contain("Interakcja komórek"));
                    Assert.That(showColumnsItem.Items.Count, Is.GreaterThan(1));
                    Assert.That(showAllColumnsItem, Is.Not.Null);
                    Assert.That(contextMenu.Items.OfType<MenuItem>().Single(item => Equals(item.Header, "Pokaż wskaźnik wiersza")).Icon, Is.Not.Null);
                    Assert.That(contextMenu.Items.OfType<MenuItem>().Single(item => Equals(item.Header, "Pokaż wskaźnik wiersza")).IsChecked, Is.True);
                    Assert.That(contextMenu.Items.OfType<MenuItem>().Single(item => Equals(item.Header, "Pokaż wskaźnik wiersza")).Tag, Is.EqualTo("toggle"));
                    Assert.That(contextMenu.Items.OfType<MenuItem>().Single(item => Equals(item.Header, "Wielokrotny wybór")).Icon, Is.Not.Null);
                    Assert.That(contextMenu.Items.OfType<MenuItem>().Single(item => Equals(item.Header, "Pokaż numery wierszy")).Icon, Is.Not.Null);
                    Assert.That(headers, Does.Not.Contain("Numeracja ogólna"));
                    Assert.That(headers, Does.Not.Contain("Numeruj w obrębie grupy"));
                    Assert.That(visibleTexts.Any(text => string.Equals(text, "v", System.StringComparison.Ordinal)), Is.False);
                });

                contextMenu.IsOpen = false;

                grid.ShowNb = true;
                GridSurfaceTestHost.FlushDispatcher(window);
                optionsButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                GridSurfaceTestHost.FlushDispatcher(window);

                var enabledHeaders = optionsButton.ContextMenu.Items
                    .OfType<MenuItem>()
                    .Select(item => item.Header?.ToString() ?? string.Empty)
                    .ToArray();
                var globalNumberingItem = optionsButton.ContextMenu.Items.OfType<MenuItem>().Single(item => Equals(item.Header, "Numeracja ogólna"));
                var withinGroupItem = optionsButton.ContextMenu.Items.OfType<MenuItem>().Single(item => Equals(item.Header, "Numeruj w obrębie grupy"));

                Assert.Multiple(() =>
                {
                    Assert.That(enabledHeaders, Does.Contain("Numeracja ogólna"));
                    Assert.That(enabledHeaders, Does.Contain("Numeruj w obrębie grupy"));
                    Assert.That(globalNumberingItem.IsEnabled, Is.True);
                    Assert.That(withinGroupItem.IsEnabled, Is.True);
                    Assert.That(globalNumberingItem.Tag, Is.EqualTo("radio"));
                    Assert.That(withinGroupItem.Tag, Is.EqualTo("radio"));
                    Assert.That(GridSurfaceTestHost.FindVisualChildren<Ellipse>(globalNumberingItem).Any(), Is.True);
                    Assert.That(GridSurfaceTestHost.FindVisualChildren<Ellipse>(withinGroupItem).Any(), Is.True);
                });

                optionsButton.ContextMenu.IsOpen = false;
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void MainWindow_WhenGridOptionsColumnsSectionUsed_CanRestoreHiddenColumns()
        {
            var window = new MainWindow();

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(window);

                var viewModel = (DemoShellViewModel)window.DataContext;
                viewModel.SelectExample("personalization");
                GridSurfaceTestHost.FlushDispatcher(window);

                var grid = FindDemoGrid(window);
                grid.SetColumnVisibility("Owner", false);
                grid.SetColumnVisibility("Status", false);
                GridSurfaceTestHost.FlushDispatcher(window);

                var optionsButton = (Button)grid.FindName("GridOptionsButton");
                Assert.That(optionsButton, Is.Not.Null);

                optionsButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                GridSurfaceTestHost.FlushDispatcher(window);

                var contextMenu = optionsButton.ContextMenu;
                var showColumnsItem = contextMenu.Items
                    .OfType<MenuItem>()
                    .Single(item => Equals(item.Header, grid.GetText(GridTextKeys.OptionsShowColumns)));
                var ownerItem = showColumnsItem.Items
                    .OfType<MenuItem>()
                    .Single(item => Equals(item.Header, viewModel.OwnerColumnText));
                var showAllItem = showColumnsItem.Items
                    .OfType<MenuItem>()
                    .Single(item => Equals(item.Header, grid.GetText(GridTextKeys.ColumnsShowAll)));

                showColumnsItem.IsSubmenuOpen = true;
                GridSurfaceTestHost.FlushDispatcher(window);

                var submenuPopup = showColumnsItem.Template.FindName("SubmenuPopup", showColumnsItem) as Popup;
                var submenuRoot = submenuPopup?.Child;
                var submenuRenderedItems = submenuRoot == null
                    ? System.Array.Empty<MenuItem>()
                    : GridSurfaceTestHost.FindVisualChildren<MenuItem>(submenuRoot).ToArray();

                Assert.Multiple(() =>
                {
                    Assert.That(contextMenu, Is.Not.Null);
                    Assert.That(showColumnsItem.Items.Count, Is.GreaterThan(1));
                    Assert.That(ownerItem.IsChecked, Is.False);
                    Assert.That(showAllItem.IsEnabled, Is.True);
                    Assert.That(submenuPopup, Is.Not.Null);
                    Assert.That(submenuPopup?.IsOpen, Is.True);
                    Assert.That(submenuRoot, Is.Not.Null);
                    Assert.That(submenuRenderedItems.Any(item => Equals(item.Header, viewModel.OwnerColumnText)), Is.True);
                    Assert.That(submenuRenderedItems.Any(item => Equals(item.Header, grid.GetText(GridTextKeys.ColumnsShowAll))), Is.True);
                });

                ownerItem.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent, ownerItem));
                GridSurfaceTestHost.FlushDispatcher(window);

                Assert.That(grid.VisibleColumns.Any(column => column.ColumnId == "Owner"), Is.True);

                optionsButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                GridSurfaceTestHost.FlushDispatcher(window);

                showColumnsItem = optionsButton.ContextMenu.Items
                    .OfType<MenuItem>()
                    .Single(item => Equals(item.Header, grid.GetText(GridTextKeys.OptionsShowColumns)));
                var ownerAfterShowAllItem = showColumnsItem.Items
                    .OfType<MenuItem>()
                    .Single(item => Equals(item.Header, viewModel.OwnerColumnText));
                var statusAfterShowAllItem = showColumnsItem.Items
                    .OfType<MenuItem>()
                    .Single(item => Equals(item.Header, viewModel.StatusColumnText));
                showAllItem = showColumnsItem.Items
                    .OfType<MenuItem>()
                    .Single(item => Equals(item.Header, grid.GetText(GridTextKeys.ColumnsShowAll)));
                showAllItem.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent, showAllItem));
                GridSurfaceTestHost.FlushDispatcher(window);

                Assert.Multiple(() =>
                {
                    Assert.That(grid.VisibleColumns.Any(column => column.ColumnId == "Owner"), Is.True);
                    Assert.That(grid.VisibleColumns.Any(column => column.ColumnId == "Status"), Is.True);
                    Assert.That(ownerAfterShowAllItem.IsChecked, Is.True);
                    Assert.That(statusAfterShowAllItem.IsChecked, Is.True);
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void MainWindow_WhenRowNumbersSwitchBetweenGlobalAndWithinGroup_RowMarkerWidthAdaptsToDigitCount()
        {
            var window = new MainWindow();

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(window);

                var viewModel = (DemoShellViewModel)window.DataContext;
                viewModel.SelectExample("grouping");
                GridSurfaceTestHost.FlushDispatcher(window);

                var grid = FindDemoGrid(window);
                grid.ShowNb = true;
                grid.RowNumberingMode = GridRowNumberingMode.Global;
                GridSurfaceTestHost.FlushDispatcher(window);
                var globalWidth = grid.ResolvedRowMarkerWidth;

                grid.RowNumberingMode = GridRowNumberingMode.WithinGroup;
                GridSurfaceTestHost.FlushDispatcher(window);
                var withinGroupWidth = grid.ResolvedRowMarkerWidth;

                Assert.Multiple(() =>
                {
                    Assert.That(globalWidth, Is.GreaterThan(withinGroupWidth));
                    Assert.That(withinGroupWidth, Is.GreaterThan(0d));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void MainWindow_WhenGroupedDataRowClicked_ShowsVisibleCurrentRowHighlightAndDedicatedIndicatorColumn()
        {
            var window = new MainWindow();

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(window);

                var viewModel = (DemoShellViewModel)window.DataContext;
                var grid = FindDemoGrid(window);
                viewModel.SelectExample("grouping");
                GridSurfaceTestHost.FlushDispatcher(window);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var firstGroupRow = surfaceHost.CurrentSnapshot.Rows[0];
                var firstGroupCell = surfaceHost.CurrentSnapshot.Cells
                    .First(cell => cell.RowKey == firstGroupRow.RowKey && cell.ColumnKey == "Category");

                GridSurfaceTestHost.ClickPointViaRoutedUi(
                    surfaceHost,
                    firstGroupCell.Bounds.X + (firstGroupCell.Bounds.Width / 2d),
                    firstGroupCell.Bounds.Y + (firstGroupCell.Bounds.Height / 2d));
                GridSurfaceTestHost.FlushDispatcher(window);

                var expandedSnapshot = surfaceHost.CurrentSnapshot;
                var dataRow = expandedSnapshot.Rows.First(row => row.RepresentsDataRecord);
                var dataCell = expandedSnapshot.Cells.First(cell => cell.RowKey == dataRow.RowKey && cell.ColumnKey == "ObjectName");

                GridSurfaceTestHost.ClickPointViaRoutedUi(
                    surfaceHost,
                    dataCell.Bounds.X + (dataCell.Bounds.Width / 2d),
                    dataCell.Bounds.Y + (dataCell.Bounds.Height / 2d));
                GridSurfaceTestHost.FlushDispatcher(window);

                var currentSnapshot = surfaceHost.CurrentSnapshot;
                var currentCellPresenter = FindCellPresenter(surfaceHost, dataRow.RowKey, "ObjectName");
                var currentRowSiblingPresenter = FindCellPresenter(surfaceHost, dataRow.RowKey, "Status");
                var currentRowCategoryPresenter = FindCellPresenter(surfaceHost, dataRow.RowKey, "Category");
                var comparisonRow = currentSnapshot.Rows.First(row => row.RepresentsDataRecord && row.RowKey != dataRow.RowKey);
                var comparisonPresenter = FindCellPresenter(surfaceHost, comparisonRow.RowKey, "ObjectName");
                var indicatorElement = GridSurfaceTestHost.FindElementByAutomationId<Border>(surfaceHost, "surface.row-indicator." + dataRow.RowKey);
                var rowHighlightPresenter = GridSurfaceTestHost.FindVisualChildren<GridOverlayPresenter>(surfaceHost)
                    .FirstOrDefault(presenter => presenter.OverlayData?.Kind == PhialeGrid.Core.Surface.GridOverlayKind.RowHighlight &&
                                                 presenter.OverlayData.TargetKey == dataRow.RowKey);

                Assert.Multiple(() =>
                {
                    Assert.That(currentSnapshot.CurrentCell, Is.Not.Null);
                    Assert.That(currentSnapshot.CurrentCell.RowKey, Is.EqualTo(dataRow.RowKey));
                    Assert.That(currentCellPresenter, Is.Not.Null);
                    Assert.That(currentRowSiblingPresenter, Is.Not.Null);
                    Assert.That(currentRowCategoryPresenter, Is.Not.Null);
                    Assert.That(comparisonPresenter, Is.Not.Null);
                    Assert.That(ReadBrushColor(currentCellPresenter.Background), Is.Not.EqualTo(ReadBrushColor(comparisonPresenter.Background)));
                    Assert.That(ReadBrushColor(currentRowCategoryPresenter.Background), Is.Not.EqualTo(ReadBrushColor(comparisonPresenter.Background)));
                    Assert.That(ReadBrushColor(currentRowSiblingPresenter.Background), Is.Not.EqualTo(ReadBrushColor(comparisonPresenter.Background)));
                    Assert.That(indicatorElement, Is.Not.Null);
                    Assert.That(indicatorElement.ActualWidth, Is.GreaterThanOrEqualTo(18d));
                    Assert.That(indicatorElement.Background, Is.TypeOf<SolidColorBrush>());
                    Assert.That(rowHighlightPresenter, Is.Null);
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void MainWindow_WhenSpecificGroupedRowBecomesCurrent_KeepsObjectNameVisible()
        {
            var window = new MainWindow();

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(window);

                var viewModel = (DemoShellViewModel)window.DataContext;
                var grid = FindDemoGrid(window);
                viewModel.SelectExample("grouping");
                GridSurfaceTestHost.FlushDispatcher(window);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var firstGroupRow = surfaceHost.CurrentSnapshot.Rows[0];
                var firstGroupCell = surfaceHost.CurrentSnapshot.Cells
                    .First(cell => cell.RowKey == firstGroupRow.RowKey && cell.ColumnKey == "Category");

                GridSurfaceTestHost.ClickPointViaRoutedUi(
                    surfaceHost,
                    firstGroupCell.Bounds.X + (firstGroupCell.Bounds.Width / 2d),
                    firstGroupCell.Bounds.Y + (firstGroupCell.Bounds.Height / 2d));
                GridSurfaceTestHost.FlushDispatcher(window);

                var expandedSnapshot = surfaceHost.CurrentSnapshot;
                var targetObjectNameCell = expandedSnapshot.Cells.First(cell =>
                    cell.ColumnKey == "ObjectName" &&
                    string.Equals(cell.DisplayText, "Address Point Oliwa 27", System.StringComparison.Ordinal));
                var targetCategoryCell = expandedSnapshot.Cells.First(cell =>
                    cell.RowKey == targetObjectNameCell.RowKey &&
                    cell.ColumnKey == "Category");

                GridSurfaceTestHost.ClickPointViaRoutedUi(
                    surfaceHost,
                    targetCategoryCell.Bounds.X + (targetCategoryCell.Bounds.Width / 2d),
                    targetCategoryCell.Bounds.Y + (targetCategoryCell.Bounds.Height / 2d));
                GridSurfaceTestHost.FlushDispatcher(window);

                var currentSnapshot = surfaceHost.CurrentSnapshot;
                var currentObjectNameCell = currentSnapshot.Cells.Single(cell => cell.RowKey == targetObjectNameCell.RowKey && cell.ColumnKey == "ObjectName");
                var currentCategoryCell = currentSnapshot.Cells.Single(cell => cell.RowKey == targetObjectNameCell.RowKey && cell.ColumnKey == "Category");
                var objectNamePresenter = FindCellPresenter(surfaceHost, targetObjectNameCell.RowKey, "ObjectName");
                var categoryPresenter = FindCellPresenter(surfaceHost, targetObjectNameCell.RowKey, "Category");
                var objectNameOverlayAncestor = GridSurfaceTestHost.FindVisibleAncestorAtPoint<GridOverlayPresenter>(
                    surfaceHost,
                    currentObjectNameCell.Bounds.X + (currentObjectNameCell.Bounds.Width / 2d),
                    currentObjectNameCell.Bounds.Y + (currentObjectNameCell.Bounds.Height / 2d));
                var categoryOverlayAncestor = GridSurfaceTestHost.FindVisibleAncestorAtPoint<GridOverlayPresenter>(
                    surfaceHost,
                    currentCategoryCell.Bounds.X + (currentCategoryCell.Bounds.Width / 2d),
                    currentCategoryCell.Bounds.Y + (currentCategoryCell.Bounds.Height / 2d));
                var objectNameCellAncestor = GridSurfaceTestHost.FindVisibleAncestorAtPoint<GridCellPresenter>(
                    surfaceHost,
                    currentObjectNameCell.Bounds.X + (currentObjectNameCell.Bounds.Width / 2d),
                    currentObjectNameCell.Bounds.Y + (currentObjectNameCell.Bounds.Height / 2d));

                Assert.Multiple(() =>
                {
                    Assert.That(currentSnapshot.CurrentCell, Is.Not.Null);
                    Assert.That(currentSnapshot.CurrentCell.RowKey, Is.EqualTo(targetObjectNameCell.RowKey));
                    Assert.That(currentSnapshot.CurrentCell.ColumnKey, Is.EqualTo("Category"));
                    Assert.That(currentObjectNameCell.DisplayText, Is.EqualTo("Address Point Oliwa 27"));
                    Assert.That(GridSurfaceTestHost.ReadVisibleText(objectNamePresenter), Is.EqualTo("Address Point Oliwa 27"));
                    Assert.That(GridSurfaceTestHost.ReadVisibleText(categoryPresenter), Is.EqualTo("AddressPoint"));
                    Assert.That(objectNameOverlayAncestor, Is.Null, "Current-row visuals must not cover the ObjectName cell.");
                    Assert.That(objectNameCellAncestor, Is.Not.Null);
                    Assert.That(objectNameCellAncestor.CellData?.ColumnKey, Is.EqualTo("ObjectName"));
                    Assert.That(categoryOverlayAncestor, Is.Not.Null);
                    Assert.That(categoryOverlayAncestor.OverlayData?.Kind, Is.EqualTo(PhialeGrid.Core.Surface.GridOverlayKind.CurrentCell));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void MainWindow_WhenMultiSelectCheckboxUsed_CurrentRowRemainsIndependentFromCheckedRows()
        {
            var window = new MainWindow();

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(window);

                var viewModel = (DemoShellViewModel)window.DataContext;
                var grid = FindDemoGrid(window);
                viewModel.SelectExample("selection");
                GridSurfaceTestHost.FlushDispatcher(window);

                var multiSelectButton = (Button)window.FindName("ShowMultiSelectScenarioButton");
                Assert.That(multiSelectButton, Is.Not.Null);
                multiSelectButton.RaiseEvent(new System.Windows.RoutedEventArgs(Button.ClickEvent));
                GridSurfaceTestHost.FlushDispatcher(window);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var targetHeader = surfaceHost.CurrentSnapshot.Headers
                    .First(header => header.Kind == PhialeGrid.Core.Surface.GridHeaderKind.RowHeader &&
                                     header.ShowSelectionCheckbox);
                var checkedHeaders = surfaceHost.CurrentSnapshot.Headers
                    .Where(header => header.Kind == PhialeGrid.Core.Surface.GridHeaderKind.RowHeader && header.IsSelectionCheckboxChecked)
                    .ToArray();
                var currentHeader = surfaceHost.CurrentSnapshot.Headers.FirstOrDefault(header =>
                    header.Kind == PhialeGrid.Core.Surface.GridHeaderKind.RowHeader &&
                    header.IsCurrentRow);
                var scenarioStatus = (TextBlock)window.FindName("SelectionScenarioStatusTextBlock");

                Assert.Multiple(() =>
                {
                    Assert.That(grid.MultiSelect, Is.True);
                    Assert.That(checkedHeaders.Length, Is.GreaterThanOrEqualTo(2));
                    Assert.That(surfaceHost.CurrentSnapshot.CurrentCell, Is.Not.Null);
                    Assert.That(targetHeader.SelectionCheckboxWidth, Is.GreaterThan(0d));
                    Assert.That(targetHeader.RowIndicatorWidth, Is.GreaterThan(0d));
                    Assert.That(currentHeader, Is.Not.Null);
                    Assert.That(currentHeader.RowIndicatorState, Is.EqualTo(PhialeGrid.Core.Surface.GridRowIndicatorState.Current));
                    Assert.That(currentHeader.IsSelectionCheckboxChecked, Is.True);
                    Assert.That(scenarioStatus.Text, Does.Contain("MultiSelect on"));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void MainWindow_WhenMultiSelectCheckboxSlotClicked_TogglesCheckedStateInSeparateSecondHeaderPart()
        {
            var window = new MainWindow();

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(window);

                var viewModel = (DemoShellViewModel)window.DataContext;
                var grid = FindDemoGrid(window);
                viewModel.SelectExample("selection");
                GridSurfaceTestHost.FlushDispatcher(window);

                var multiSelectButton = (Button)window.FindName("ShowMultiSelectScenarioButton");
                Assert.That(multiSelectButton, Is.Not.Null);
                multiSelectButton.RaiseEvent(new System.Windows.RoutedEventArgs(Button.ClickEvent));
                GridSurfaceTestHost.FlushDispatcher(window);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var currentBefore = surfaceHost.CurrentSnapshot.CurrentCell;
                var uncheckedHeader = surfaceHost.CurrentSnapshot.Headers
                    .First(header =>
                        header.Kind == PhialeGrid.Core.Surface.GridHeaderKind.RowHeader &&
                        header.ShowSelectionCheckbox &&
                        !header.IsSelectionCheckboxChecked);

                var selectorElement = GridSurfaceTestHost.FindElementByAutomationId<FrameworkElement>(surfaceHost, "surface.row-selector." + uncheckedHeader.HeaderKey);
                Assert.That(selectorElement, Is.Not.Null);
                var checkboxCenter = selectorElement.TranslatePoint(
                    new Point(selectorElement.ActualWidth / 2d, selectorElement.ActualHeight / 2d),
                    surfaceHost);

                GridSurfaceTestHost.ClickPointViaRoutedUi(surfaceHost, checkboxCenter.X, checkboxCenter.Y);
                GridSurfaceTestHost.FlushDispatcher(window);

                var updatedSnapshot = surfaceHost.CurrentSnapshot;
                var updatedHeader = updatedSnapshot.Headers.Single(header =>
                    header.Kind == PhialeGrid.Core.Surface.GridHeaderKind.RowHeader &&
                    header.HeaderKey == uncheckedHeader.HeaderKey);
                var selectedRowCells = updatedSnapshot.Cells.Where(cell => cell.RowKey == uncheckedHeader.HeaderKey).ToArray();

                Assert.Multiple(() =>
                {
                    Assert.That(grid.MultiSelect, Is.True);
                    Assert.That(selectorElement.ActualWidth, Is.GreaterThan(0d));
                    Assert.That(updatedHeader.RowIndicatorWidth, Is.GreaterThan(0d));
                    Assert.That(updatedHeader.SelectionCheckboxWidth, Is.GreaterThan(0d));
                    Assert.That(updatedHeader.IsSelectionCheckboxChecked, Is.True);
                    Assert.That(updatedHeader.IsSelected, Is.False);
                    Assert.That(updatedSnapshot.SelectionRegions.Any(region => region.Unit == PhialeGrid.Core.Surface.GridSelectionUnit.Row && region.SelectedKeys.Contains(uncheckedHeader.HeaderKey)), Is.False);
                    Assert.That(selectedRowCells.All(cell => !cell.IsSelected || cell.ColumnKey == currentBefore.ColumnKey), Is.True);
                    Assert.That(updatedSnapshot.CurrentCell, Is.Not.Null);
                    Assert.That(updatedSnapshot.CurrentCell.RowKey, Is.EqualTo(currentBefore.RowKey));
                    Assert.That(updatedSnapshot.CurrentCell.ColumnKey, Is.EqualTo(currentBefore.ColumnKey));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void MainWindow_WhenMultiSelectCheckboxClickedAcrossVisibleCheckboxArea_TogglesOnlyInsideDedicatedCheckboxColumn()
        {
            var window = new MainWindow();

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(window);

                var viewModel = (DemoShellViewModel)window.DataContext;
                var grid = FindDemoGrid(window);
                viewModel.SelectExample("selection");
                GridSurfaceTestHost.FlushDispatcher(window);

                var multiSelectButton = (Button)window.FindName("ShowMultiSelectScenarioButton");
                Assert.That(multiSelectButton, Is.Not.Null);
                multiSelectButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                GridSurfaceTestHost.FlushDispatcher(window);

                grid.ShowNb = true;
                GridSurfaceTestHost.FlushDispatcher(window);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                surfaceHost.Coordinator.SetCheckedRows(System.Array.Empty<string>());
                surfaceHost.Coordinator.ClearSelection();
                GridSurfaceTestHost.FlushDispatcher(window);
                var targetHeaders = surfaceHost.CurrentSnapshot.Headers
                    .Where(header =>
                        header.Kind == PhialeGrid.Core.Surface.GridHeaderKind.RowHeader &&
                        header.ShowSelectionCheckbox &&
                        !header.IsSelectionCheckboxChecked)
                    .Take(4)
                    .ToArray();
                Assert.That(targetHeaders.Length, Is.EqualTo(4));

                VerifyCheckboxToggle(surfaceHost, targetHeaders[0].HeaderKey, GetSelectorCenter);
                VerifyCheckboxToggle(surfaceHost, targetHeaders[1].HeaderKey, GetSelectorTopLeft);
                VerifyCheckboxToggle(surfaceHost, targetHeaders[2].HeaderKey, GetSelectorBottomRight);
                VerifyCheckboxDoesNotToggle(surfaceHost, targetHeaders[3].HeaderKey, GetRowNumberColumnPoint);
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void MainWindow_WhenMultiSelectEnabledAndRowIndicatorHidden_CheckboxStillTogglesInDedicatedCheckboxSlot()
        {
            var window = new MainWindow();

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(window);

                var viewModel = (DemoShellViewModel)window.DataContext;
                var grid = FindDemoGrid(window);
                viewModel.SelectExample("selection");
                GridSurfaceTestHost.FlushDispatcher(window);

                var multiSelectButton = (Button)window.FindName("ShowMultiSelectScenarioButton");
                Assert.That(multiSelectButton, Is.Not.Null);
                multiSelectButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                grid.SelectCurrentRow = false;
                GridSurfaceTestHost.FlushDispatcher(window);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var uncheckedHeader = surfaceHost.CurrentSnapshot.Headers
                    .First(header =>
                        header.Kind == PhialeGrid.Core.Surface.GridHeaderKind.RowHeader &&
                        header.ShowSelectionCheckbox &&
                        !header.IsSelectionCheckboxChecked);
                var selectorElement = GridSurfaceTestHost.FindElementByAutomationId<FrameworkElement>(surfaceHost, "surface.row-selector." + uncheckedHeader.HeaderKey);
                Assert.That(selectorElement, Is.Not.Null);
                var checkboxCenter = selectorElement.TranslatePoint(
                    new Point(selectorElement.ActualWidth / 2d, selectorElement.ActualHeight / 2d),
                    surfaceHost);

                GridSurfaceTestHost.ClickPointViaRoutedUi(surfaceHost, checkboxCenter.X, checkboxCenter.Y);
                GridSurfaceTestHost.FlushDispatcher(window);

                var updatedHeader = surfaceHost.CurrentSnapshot.Headers.Single(header =>
                    header.Kind == PhialeGrid.Core.Surface.GridHeaderKind.RowHeader &&
                    header.HeaderKey == uncheckedHeader.HeaderKey);

                Assert.Multiple(() =>
                {
                    Assert.That(grid.MultiSelect, Is.True);
                    Assert.That(grid.SelectCurrentRow, Is.False);
                    Assert.That(updatedHeader.ShowRowIndicator, Is.False);
                    Assert.That(updatedHeader.RowIndicatorWidth, Is.GreaterThan(0d));
                    Assert.That(updatedHeader.IsSelectionCheckboxChecked, Is.True);
                    Assert.That(updatedHeader.IsSelected, Is.False);
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void MainWindow_WhenRowNumbersAndMultiSelectEnabled_LeftUtilityColumnsRemainVisuallySeparated()
        {
            var window = new MainWindow();

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(window);

                var viewModel = (DemoShellViewModel)window.DataContext;
                var grid = FindDemoGrid(window);
                viewModel.SelectExample("selection");
                GridSurfaceTestHost.FlushDispatcher(window);

                var multiSelectButton = (Button)window.FindName("ShowMultiSelectScenarioButton");
                Assert.That(multiSelectButton, Is.Not.Null);
                multiSelectButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                grid.ShowNb = true;
                GridSurfaceTestHost.FlushDispatcher(window);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var currentRowKey = surfaceHost.CurrentSnapshot.CurrentCell?.RowKey;
                var targetStateHeader = surfaceHost.CurrentSnapshot.Headers.First(header =>
                    header.Kind == PhialeGrid.Core.Surface.GridHeaderKind.RowHeader &&
                    header.ShowSelectionCheckbox &&
                    !string.Equals(header.HeaderKey, currentRowKey, System.StringComparison.OrdinalIgnoreCase));
                var targetNumberHeader = surfaceHost.CurrentSnapshot.Headers.Single(header =>
                    header.Kind == PhialeGrid.Core.Surface.GridHeaderKind.RowNumberHeader &&
                    header.HeaderKey == targetStateHeader.HeaderKey);

                var checkboxSlot = GridSurfaceTestHost.FindElementByAutomationId<Border>(surfaceHost, "surface.row-checkbox-slot." + targetStateHeader.HeaderKey);
                var rowNumberSlot = GridSurfaceTestHost.FindElementByAutomationId<Border>(surfaceHost, "surface.row-number." + targetStateHeader.HeaderKey);

                Assert.Multiple(() =>
                {
                    Assert.That(targetStateHeader.Bounds.Width, Is.EqualTo(grid.ResolvedRowStateWidth).Within(1d));
                    Assert.That(targetNumberHeader.Bounds.Width, Is.EqualTo(grid.ResolvedRowNumbersWidth).Within(1d));
                    Assert.That(targetNumberHeader.Bounds.X, Is.GreaterThanOrEqualTo(targetStateHeader.Bounds.Right - 1d));
                    Assert.That(checkboxSlot, Is.Not.Null);
                    Assert.That(rowNumberSlot, Is.Not.Null);
                    Assert.That(checkboxSlot.Background, Is.TypeOf<SolidColorBrush>());
                    Assert.That(rowNumberSlot.Background, Is.TypeOf<SolidColorBrush>());
                    Assert.That(((SolidColorBrush)checkboxSlot.Background).Color, Is.Not.EqualTo(((SolidColorBrush)rowNumberSlot.Background).Color));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void MainWindow_WhenEditingExampleOpens_ShowsBaselineCurrentEditedAndInvalidStates()
        {
            var window = new MainWindow();

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(window);

                var viewModel = (DemoShellViewModel)window.DataContext;
                viewModel.SelectExample("editing");
                GridSurfaceTestHost.FlushDispatcher(window);

                var grid = FindDemoGrid(window);
                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var rowHeaders = surfaceHost.CurrentSnapshot.Headers
                    .Where(header => header.Kind == PhialeGrid.Core.Surface.GridHeaderKind.RowHeader)
                    .ToArray();
                var scenarioStatus = (TextBlock)window.FindName("EditingScenarioStatusTextBlock");

                Assert.Multiple(() =>
                {
                    Assert.That(surfaceHost.CurrentSnapshot.CurrentCell, Is.Not.Null);
                    Assert.That(rowHeaders.Any(header => header.RowIndicatorState == PhialeGrid.Core.Surface.GridRowIndicatorState.Current), Is.True);
                    Assert.That(rowHeaders.Any(header => header.RowIndicatorState == PhialeGrid.Core.Surface.GridRowIndicatorState.Edited), Is.True);
                    Assert.That(rowHeaders.Any(header => header.RowIndicatorState == PhialeGrid.Core.Surface.GridRowIndicatorState.Invalid), Is.True);
                    Assert.That(rowHeaders.Any(header => (header.RowIndicatorToolTip ?? string.Empty).Contains("Object name", StringComparison.OrdinalIgnoreCase)), Is.True);
                    Assert.That(rowHeaders.Any(header => (header.RowIndicatorToolTip ?? string.Empty).Contains("Owner is required.", StringComparison.OrdinalIgnoreCase)), Is.True);
                    Assert.That(grid.MultiSelect, Is.False);
                    Assert.That(scenarioStatus.Text, Does.Contain("Baseline row-state demo"));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void MainWindow_WhenEditedPriorityScenarioActivated_CurrentEditedCompositeStateIsShown()
        {
            var window = new MainWindow();

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(window);

                var viewModel = (DemoShellViewModel)window.DataContext;
                viewModel.SelectExample("editing");
                GridSurfaceTestHost.FlushDispatcher(window);

                var button = (Button)window.FindName("ShowEditedPriorityScenarioButton");
                Assert.That(button, Is.Not.Null);
                button.RaiseEvent(new System.Windows.RoutedEventArgs(Button.ClickEvent));
                GridSurfaceTestHost.FlushDispatcher(window);

                var grid = FindDemoGrid(window);
                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var currentRowKey = surfaceHost.CurrentSnapshot.CurrentCell.RowKey;
                var currentHeader = surfaceHost.CurrentSnapshot.Headers.FirstOrDefault(header =>
                    header.Kind == PhialeGrid.Core.Surface.GridHeaderKind.RowHeader &&
                    header.IsCurrentRow);
                var currentCellPresenter = FindCellPresenter(surfaceHost, currentRowKey, "ObjectName");
                var comparisonRowKey = surfaceHost.CurrentSnapshot.Rows.First(row => row.RowKey != currentRowKey).RowKey;
                var comparisonCellPresenter = FindCellPresenter(surfaceHost, comparisonRowKey, "ObjectName");
                var scenarioStatus = (TextBlock)window.FindName("EditingScenarioStatusTextBlock");

                Assert.Multiple(() =>
                {
                    Assert.That(surfaceHost.CurrentSnapshot.CurrentCell, Is.Not.Null);
                    Assert.That(currentHeader, Is.Not.Null);
                    Assert.That(currentHeader.RowIndicatorState, Is.EqualTo(PhialeGrid.Core.Surface.GridRowIndicatorState.CurrentAndEdited));
                    Assert.That(currentHeader.RowIndicatorToolTip, Does.Contain("Object name"));
                    Assert.That(currentHeader.RowIndicatorToolTip, Does.Contain("Parcel Stare Miasto 1001"));
                    Assert.That(ReadBrushColor(currentCellPresenter.Background), Is.Not.EqualTo(ReadBrushColor(comparisonCellPresenter.Background)));
                    Assert.That(grid.HasPendingEdits, Is.True);
                    Assert.That(scenarioStatus.Text, Does.Contain("Current + Edited"));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void MainWindow_WhenInvalidPriorityScenarioActivated_CurrentInvalidCompositeStateIsShown()
        {
            var window = new MainWindow();

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(window);

                var viewModel = (DemoShellViewModel)window.DataContext;
                viewModel.SelectExample("editing");
                GridSurfaceTestHost.FlushDispatcher(window);

                var button = (Button)window.FindName("ShowInvalidPriorityScenarioButton");
                Assert.That(button, Is.Not.Null);
                button.RaiseEvent(new System.Windows.RoutedEventArgs(Button.ClickEvent));
                GridSurfaceTestHost.FlushDispatcher(window);

                var grid = FindDemoGrid(window);
                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var currentHeader = surfaceHost.CurrentSnapshot.Headers.FirstOrDefault(header =>
                    header.Kind == PhialeGrid.Core.Surface.GridHeaderKind.RowHeader &&
                    header.IsCurrentRow);
                var scenarioStatus = (TextBlock)window.FindName("EditingScenarioStatusTextBlock");

                Assert.Multiple(() =>
                {
                    Assert.That(surfaceHost.CurrentSnapshot.CurrentCell, Is.Not.Null);
                    Assert.That(currentHeader, Is.Not.Null);
                    Assert.That(currentHeader.RowIndicatorState, Is.EqualTo(PhialeGrid.Core.Surface.GridRowIndicatorState.CurrentAndInvalid));
                    Assert.That(currentHeader.RowIndicatorToolTip, Does.Contain("Owner"));
                    Assert.That(currentHeader.RowIndicatorToolTip, Does.Contain("Owner is required."));
                    Assert.That(grid.HasPendingEdits, Is.True);
                    Assert.That(grid.HasValidationIssues, Is.True);
                    Assert.That(scenarioStatus.Text, Does.Contain("Current + Error"));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void MainWindow_WhenEditingExampleOpens_ShowsViewportTrackMarkersForEditedAndInvalidRows()
        {
            var window = new MainWindow();

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(window);

                var viewModel = (DemoShellViewModel)window.DataContext;
                viewModel.SelectExample("editing");
                GridSurfaceTestHost.FlushDispatcher(window);

                var grid = FindDemoGrid(window);
                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);

                Assert.That(surfaceHost.CurrentSnapshot.ViewportState.VerticalTrackMarkers.Count, Is.GreaterThanOrEqualTo(2));
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void MainWindow_WhenScrollErrorCellDemoActivated_ScrollsToInvalidOwnerCell()
        {
            var window = new MainWindow();

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(window);

                var viewModel = (DemoShellViewModel)window.DataContext;
                viewModel.SelectExample("editing");
                GridSurfaceTestHost.FlushDispatcher(window);

                var button = (Button)window.FindName("ScrollErrorCellDemoButton");
                Assert.That(button, Is.Not.Null);
                button.RaiseEvent(new System.Windows.RoutedEventArgs(Button.ClickEvent));
                GridSurfaceTestHost.FlushDispatcher(window);

                var grid = FindDemoGrid(window);
                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var scenarioStatus = (TextBlock)window.FindName("EditingScenarioStatusTextBlock");

                Assert.Multiple(() =>
                {
                    Assert.That(surfaceHost.CurrentSnapshot.CurrentCell, Is.Not.Null);
                    Assert.That(surfaceHost.CurrentSnapshot.CurrentCell.ColumnKey, Is.EqualTo("Owner"));
                    Assert.That(surfaceHost.CurrentSnapshot.ViewportState.HorizontalOffset, Is.GreaterThan(0d));
                    Assert.That(scenarioStatus.Text, Does.Contain("Scroll cell demo"));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void MainWindow_WhenScrollEditedRowDemoActivated_MakesEditedObjectNameCellCurrent()
        {
            var window = new MainWindow();

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(window);

                var viewModel = (DemoShellViewModel)window.DataContext;
                viewModel.SelectExample("editing");
                GridSurfaceTestHost.FlushDispatcher(window);

                var button = (Button)window.FindName("ScrollEditedRowDemoButton");
                Assert.That(button, Is.Not.Null);
                button.RaiseEvent(new System.Windows.RoutedEventArgs(Button.ClickEvent));
                GridSurfaceTestHost.FlushDispatcher(window);

                var grid = FindDemoGrid(window);
                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);

                Assert.Multiple(() =>
                {
                    Assert.That(surfaceHost.CurrentSnapshot.CurrentCell, Is.Not.Null);
                    Assert.That(surfaceHost.CurrentSnapshot.CurrentCell.RowKey, Is.EqualTo("DZ-KRA-STA-0001"));
                    Assert.That(surfaceHost.CurrentSnapshot.CurrentCell.ColumnKey, Is.EqualTo("ObjectName"));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void MainWindow_WhenScrollLastColumnDemoActivated_RevealsScaleHintCell()
        {
            var window = new MainWindow();

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(window);

                var viewModel = (DemoShellViewModel)window.DataContext;
                viewModel.SelectExample("editing");
                GridSurfaceTestHost.FlushDispatcher(window);

                var button = (Button)window.FindName("ScrollFarColumnDemoButton");
                Assert.That(button, Is.Not.Null);
                button.RaiseEvent(new System.Windows.RoutedEventArgs(Button.ClickEvent));
                GridSurfaceTestHost.FlushDispatcher(window);

                var grid = FindDemoGrid(window);
                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);

                Assert.Multiple(() =>
                {
                    Assert.That(surfaceHost.CurrentSnapshot.CurrentCell, Is.Not.Null);
                    Assert.That(surfaceHost.CurrentSnapshot.CurrentCell.RowKey, Is.EqualTo("RD-GDA-OLI-0003"));
                    Assert.That(surfaceHost.CurrentSnapshot.CurrentCell.ColumnKey, Is.EqualTo("ScaleHint"));
                    Assert.That(surfaceHost.CurrentSnapshot.ViewportState.HorizontalOffset, Is.GreaterThan(0d));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void MainWindow_WhenMultipleEditedRowsExist_ScrollEditedRowDemoCyclesToNextEditedRow()
        {
            var window = new MainWindow();

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(window);

                var viewModel = (DemoShellViewModel)window.DataContext;
                viewModel.SelectExample("editing");
                GridSurfaceTestHost.FlushDispatcher(window);

                var grid = FindDemoGrid(window);
                Assert.That(grid.SetRowValueForDemo("WAT-POZ-JEZ-0004", "ObjectName", "Water Main Jezyce 4 (edited)"), Is.True);
                GridSurfaceTestHost.FlushDispatcher(window);

                var button = (Button)window.FindName("ScrollEditedRowDemoButton");
                Assert.That(button, Is.Not.Null);

                button.RaiseEvent(new System.Windows.RoutedEventArgs(Button.ClickEvent));
                GridSurfaceTestHost.FlushDispatcher(window);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                Assert.That(surfaceHost.CurrentSnapshot.CurrentCell?.RowKey, Is.EqualTo("DZ-KRA-STA-0001"));

                button.RaiseEvent(new System.Windows.RoutedEventArgs(Button.ClickEvent));
                GridSurfaceTestHost.FlushDispatcher(window);

                Assert.That(surfaceHost.CurrentSnapshot.CurrentCell?.RowKey, Is.EqualTo("WAT-POZ-JEZ-0004"));
                Assert.That(surfaceHost.CurrentSnapshot.CurrentCell?.ColumnKey, Is.EqualTo("ObjectName"));
            }
            finally
            {
                window.Close();
            }
        }

        private static void VerifyCheckboxToggle(
            GridSurfaceHost surfaceHost,
            string rowKey,
            System.Func<GridSurfaceHost, string, Point> pointResolver)
        {
            Assert.That(surfaceHost, Is.Not.Null);
            Assert.That(pointResolver, Is.Not.Null);

            var before = surfaceHost.CurrentSnapshot.Headers.Single(header =>
                header.Kind == PhialeGrid.Core.Surface.GridHeaderKind.RowHeader &&
                header.HeaderKey == rowKey);
            Assert.That(before.IsSelectionCheckboxChecked, Is.False, "Expected an unchecked row before the test click.");

            var point = pointResolver(surfaceHost, rowKey);
            GridSurfaceTestHost.ClickPointViaRoutedUi(surfaceHost, point.X, point.Y);
            GridSurfaceTestHost.FlushDispatcher(surfaceHost);

            var after = surfaceHost.CurrentSnapshot.Headers.Single(header =>
                header.Kind == PhialeGrid.Core.Surface.GridHeaderKind.RowHeader &&
                header.HeaderKey == rowKey);
            Assert.That(after.IsSelectionCheckboxChecked, Is.True, "Expected row marker click to toggle the checkbox.");
            Assert.That(after.IsSelected, Is.False, "Checkbox click should not create row selection.");
        }

        private static Point GetSelectorCenter(GridSurfaceHost surfaceHost, string rowKey)
        {
            var selector = GridSurfaceTestHost.FindElementByAutomationId<FrameworkElement>(surfaceHost, "surface.row-selector." + rowKey);
            Assert.That(selector, Is.Not.Null);
            return selector.TranslatePoint(new Point(selector.ActualWidth / 2d, selector.ActualHeight / 2d), surfaceHost);
        }

        private static Point GetSelectorTopLeft(GridSurfaceHost surfaceHost, string rowKey)
        {
            var selector = GridSurfaceTestHost.FindElementByAutomationId<FrameworkElement>(surfaceHost, "surface.row-selector." + rowKey);
            Assert.That(selector, Is.Not.Null);
            return selector.TranslatePoint(new Point(1d, 1d), surfaceHost);
        }

        private static Point GetSelectorBottomRight(GridSurfaceHost surfaceHost, string rowKey)
        {
            var selector = GridSurfaceTestHost.FindElementByAutomationId<FrameworkElement>(surfaceHost, "surface.row-selector." + rowKey);
            Assert.That(selector, Is.Not.Null);
            return selector.TranslatePoint(new Point(selector.ActualWidth - 1d, selector.ActualHeight - 1d), surfaceHost);
        }

        private static void VerifyCheckboxDoesNotToggle(
            GridSurfaceHost surfaceHost,
            string rowKey,
            System.Func<GridSurfaceHost, string, Point> pointResolver)
        {
            var before = surfaceHost.CurrentSnapshot.Headers.Single(header =>
                header.Kind == PhialeGrid.Core.Surface.GridHeaderKind.RowHeader &&
                header.HeaderKey == rowKey);
            Assert.That(before.IsSelectionCheckboxChecked, Is.False, "Expected an unchecked row before the test click.");

            var point = pointResolver(surfaceHost, rowKey);
            GridSurfaceTestHost.ClickPointViaRoutedUi(surfaceHost, point.X, point.Y);
            GridSurfaceTestHost.FlushDispatcher(surfaceHost);

            var after = surfaceHost.CurrentSnapshot.Headers.Single(header =>
                header.Kind == PhialeGrid.Core.Surface.GridHeaderKind.RowHeader &&
                header.HeaderKey == rowKey);
            Assert.That(after.IsSelectionCheckboxChecked, Is.False, "Blank row-number area must not toggle the checkbox.");
        }

        private static Point GetRowNumberColumnPoint(GridSurfaceHost surfaceHost, string rowKey)
        {
            var marker = GridSurfaceTestHost.FindElementByAutomationId<FrameworkElement>(surfaceHost, "surface.row-number." + rowKey);
            var selector = GridSurfaceTestHost.FindElementByAutomationId<FrameworkElement>(surfaceHost, "surface.row-selector." + rowKey);
            Assert.That(marker, Is.Not.Null);
            Assert.That(selector, Is.Not.Null);

            var markerBounds = marker.TransformToAncestor(surfaceHost)
                .TransformBounds(new Rect(0, 0, marker.ActualWidth, marker.ActualHeight));
            var selectorBounds = selector.TransformToAncestor(surfaceHost)
                .TransformBounds(new Rect(0, 0, selector.ActualWidth, selector.ActualHeight));
            var blankX = markerBounds.Left + 2d;
            if (blankX >= selectorBounds.Left && blankX <= selectorBounds.Right)
            {
                blankX = selectorBounds.Right + 2d;
            }

            if (blankX > markerBounds.Right - 2d)
            {
                blankX = markerBounds.Left + 2d;
            }

            return new Point(blankX, markerBounds.Top + (markerBounds.Height / 2d));
        }

        private static WpfGrid FindDemoGrid(MainWindow window)
        {
            var grid = (WpfGrid)window.FindName("DemoGrid");
            Assert.That(grid, Is.Not.Null);
            return grid;
        }

        private static GridCellPresenter FindCellPresenter(GridSurfaceHost surfaceHost, string rowKey, string columnKey)
        {
            return GridSurfaceTestHost.FindElementByAutomationId<GridCellPresenter>(surfaceHost, "surface.cell." + rowKey + "." + columnKey);
        }

        private static Color ReadBrushColor(object brush)
        {
            Assert.That(brush, Is.TypeOf<SolidColorBrush>());
            return ((SolidColorBrush)brush).Color;
        }
    }
}
