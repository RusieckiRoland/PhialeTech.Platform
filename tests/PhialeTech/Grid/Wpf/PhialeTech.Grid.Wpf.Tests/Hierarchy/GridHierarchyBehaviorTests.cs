using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using NUnit.Framework;
using PhialeTech.Components.Shared.ViewModels;
using PhialeTech.Components.Wpf;
using PhialeTech.PhialeGrid.Wpf.Controls;
using PhialeTech.PhialeGrid.Wpf.Surface.Presenters;
using PhialeGrid.Wpf.Tests.Surface;
using WpfGrid = PhialeTech.PhialeGrid.Wpf.Controls.PhialeGrid;

namespace PhialeGrid.Wpf.Tests.Hierarchy
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public sealed class GridHierarchyBehaviorTests
    {
        [Test]
        public void DemoBinding_WhenHierarchyScenarioIsSelected_StartsCollapsedWithRootRowsOnly()
        {
            var viewModel = new DemoShellViewModel("Wpf");
            var grid = CreateBoundDemoGrid(viewModel);
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 1280, height: 720);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                viewModel.SelectExample("hierarchy");
                grid.SetHierarchySource(viewModel.GridHierarchyRoots, viewModel.GridHierarchyController);
                GridSurfaceTestHost.FlushDispatcher(grid);

                var rows = grid.RowsView.Cast<object>().ToArray();
                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);

                Assert.Multiple(() =>
                {
                    Assert.That(viewModel.GridHierarchyRoots.Count, Is.GreaterThan(1));
                    Assert.That(rows.Length, Is.EqualTo(viewModel.GridHierarchyRoots.Count));
                    Assert.That(rows.All(row => row is GridHierarchyNodeRowModel), Is.True);
                    Assert.That(rows.Cast<GridHierarchyNodeRowModel>().All(row => row.Node.IsExpanded == false), Is.True);
                    Assert.That(surfaceHost.CurrentSnapshot.Rows.Count, Is.EqualTo(viewModel.GridHierarchyRoots.Count));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void ExpandAndLoadNextHierarchyPage_InjectChildrenAndCollapseAllRestoresRootOnlyView()
        {
            var viewModel = new DemoShellViewModel("Wpf");
            var grid = CreateBoundDemoGrid(viewModel);
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 1280, height: 720);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                viewModel.SelectExample("hierarchy");
                grid.SetHierarchySource(viewModel.GridHierarchyRoots, viewModel.GridHierarchyController);
                GridSurfaceTestHost.FlushDispatcher(grid);

                var rootPathId = viewModel.GridHierarchyRoots[0].PathId;
                var collapsedCount = grid.RowsView.Cast<object>().Count();

                grid.ExpandHierarchyNodeAsync(rootPathId).GetAwaiter().GetResult();
                GridSurfaceTestHost.FlushDispatcher(grid);

                var expandedRows = grid.RowsView.Cast<object>().ToArray();
                var afterExpandCount = expandedRows.Length;

                grid.LoadNextHierarchyChildrenPageAsync(rootPathId).GetAwaiter().GetResult();
                GridSurfaceTestHost.FlushDispatcher(grid);

                var afterLoadMoreRows = grid.RowsView.Cast<object>().ToArray();

                grid.CollapseAllHierarchy();
                GridSurfaceTestHost.FlushDispatcher(grid);

                var collapsedAgainRows = grid.RowsView.Cast<object>().ToArray();

                Assert.Multiple(() =>
                {
                    Assert.That(afterExpandCount, Is.GreaterThan(collapsedCount));
                    Assert.That(expandedRows.Any(row => row is GridHierarchyLoadMoreRowModel), Is.True);
                    Assert.That(expandedRows.OfType<GridHierarchyNodeRowModel>().Count(row => row.Level > 0), Is.GreaterThan(0));
                    Assert.That(afterLoadMoreRows.Length, Is.GreaterThan(afterExpandCount));
                    Assert.That(collapsedAgainRows.Length, Is.EqualTo(collapsedCount));
                    Assert.That(collapsedAgainRows.All(row => row is GridHierarchyNodeRowModel hierarchyRow && hierarchyRow.Level == 0), Is.True);
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void DemoBinding_WhenMasterDetailScenarioIsSelected_StartsCollapsedWithCategoryMasterRowsOnly()
        {
            var viewModel = new DemoShellViewModel("Wpf");
            var grid = CreateBoundDemoGrid(viewModel);
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 1280, height: 720);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                viewModel.SelectExample("master-detail");
                grid.SetMasterDetailSource(
                    viewModel.GridHierarchyRoots,
                    viewModel.GridHierarchyController,
                    new[] { "ObjectName", "ObjectId", "GeometryType", "Status" },
                    masterDisplayColumnId: "Category",
                    detailDisplayColumnId: "ObjectName");
                GridSurfaceTestHost.FlushDispatcher(grid);

                var rows = grid.RowsView.Cast<object>().ToArray();
                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);

                Assert.Multiple(() =>
                {
                    Assert.That(viewModel.IsMasterDetailExample, Is.True);
                    Assert.That(rows.Length, Is.EqualTo(viewModel.GridHierarchyRoots.Count));
                    Assert.That(rows.All(row => row is GridMasterDetailMasterRowModel), Is.True);
                    Assert.That(rows.Cast<GridMasterDetailMasterRowModel>().All(row => row.Level == 0 && row.Node.IsExpanded == false), Is.True);
                    Assert.That(rows.Cast<GridMasterDetailMasterRowModel>().All(row => row.SourceRow is System.Collections.Generic.IDictionary<string, object>), Is.True);
                    Assert.That(surfaceHost.CurrentSnapshot.Rows.Count, Is.EqualTo(viewModel.GridHierarchyRoots.Count));
                    Assert.That(grid.VisibleColumns.Select(column => column.ColumnId), Is.EqualTo(new[] { "Category", "Description" }));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void ExpandMasterDetailNode_ShowsInsideDetailOverlayAndLocalFilters_DoNotAffectOtherMasters()
        {
            var viewModel = new DemoShellViewModel("Wpf");
            var grid = CreateBoundDemoGrid(viewModel);
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 1280, height: 720);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                viewModel.SelectExample("master-detail");
                grid.SetMasterDetailSource(
                    viewModel.GridHierarchyRoots,
                    viewModel.GridHierarchyController,
                    new[] { "ObjectName", "ObjectId", "GeometryType", "Status" },
                    masterDisplayColumnId: "Category",
                    detailDisplayColumnId: "ObjectName",
                    detailHeaderPlacementMode: GridMasterDetailHeaderPlacementMode.Inside);
                GridSurfaceTestHost.FlushDispatcher(grid);

                var firstRootPathId = viewModel.GridHierarchyRoots[0].PathId;
                var secondRootPathId = viewModel.GridHierarchyRoots[1].PathId;
                var collapsedCount = grid.RowsView.Cast<object>().Count();

                grid.ExpandHierarchyNodeAsync(firstRootPathId).GetAwaiter().GetResult();
                grid.ExpandHierarchyNodeAsync(secondRootPathId).GetAwaiter().GetResult();
                GridSurfaceTestHost.FlushDispatcher(grid);

                var masterRows = grid.RowsView.Cast<object>().OfType<GridMasterDetailMasterRowModel>().ToArray();
                var firstMaster = masterRows[0];
                var secondMaster = masterRows[1];
                var beforeFirstCount = firstMaster.DetailRows.Count;
                var beforeSecondCount = secondMaster.DetailRows.Count;

                Assert.That(firstMaster.DetailColumns.Select(column => column.ColumnId), Is.EqualTo(new[] { "ObjectName", "ObjectId", "GeometryType", "Status" }));

                firstMaster.DetailColumns[0].FilterText = "zzzz-not-found";
                GridSurfaceTestHost.FlushDispatcher(grid);

                var afterLocalFilterFirstCount = firstMaster.DetailRows.Count;
                var afterLocalFilterSecondCount = secondMaster.DetailRows.Count;

                grid.LoadNextHierarchyChildrenPageAsync(firstRootPathId).GetAwaiter().GetResult();
                GridSurfaceTestHost.FlushDispatcher(grid);

                masterRows = grid.RowsView.Cast<object>().OfType<GridMasterDetailMasterRowModel>().ToArray();
                firstMaster = masterRows[0];
                secondMaster = masterRows[1];
                var afterLoadMoreFirstCount = firstMaster.DetailRows.Count;
                var afterLoadMoreSecondCount = secondMaster.DetailRows.Count;

                grid.CollapseAllHierarchy();
                GridSurfaceTestHost.FlushDispatcher(grid);
                var collapsedAgainRows = grid.RowsView.Cast<object>().ToArray();
                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);

                Assert.Multiple(() =>
                {
                    Assert.That(masterRows.Length, Is.EqualTo(collapsedCount));
                    Assert.That(beforeFirstCount, Is.GreaterThan(0));
                    Assert.That(beforeSecondCount, Is.GreaterThan(0));
                    Assert.That(afterLocalFilterFirstCount, Is.EqualTo(0));
                    Assert.That(afterLocalFilterSecondCount, Is.EqualTo(beforeSecondCount));
                    Assert.That(afterLoadMoreFirstCount, Is.EqualTo(0));
                    Assert.That(afterLoadMoreSecondCount, Is.EqualTo(beforeSecondCount));
                    Assert.That(collapsedAgainRows.Length, Is.EqualTo(collapsedCount));
                    Assert.That(collapsedAgainRows.All(row => row is GridMasterDetailMasterRowModel), Is.True);
                    Assert.That(surfaceHost.CurrentSnapshot.Overlays.Any(), Is.False);
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void MainWindow_WhenMasterDetailScenarioIsSelected_ConfiguresLiveGridAndShowsDetailOverlayAfterExpand()
        {
            var window = new MainWindow();

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(window);

                var viewModel = (DemoShellViewModel)window.DataContext;
                var grid = (WpfGrid)window.FindName("DemoGrid");

                viewModel.SelectExample("master-detail");
                GridSurfaceTestHost.FlushDispatcher(window);

                Assert.That(grid, Is.Not.Null);
                Assert.That(grid.HasHierarchy, Is.True);

                var rootPathId = viewModel.GridHierarchyRoots[0].PathId;
                grid.ExpandHierarchyNodeAsync(rootPathId).GetAwaiter().GetResult();
                GridSurfaceTestHost.FlushDispatcher(window);

                var rows = grid.RowsView.Cast<object>().ToArray();
                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);

                Assert.Multiple(() =>
                {
                    Assert.That(rows.First(), Is.InstanceOf<GridMasterDetailMasterRowModel>());
                    Assert.That(rows.All(row => row is GridMasterDetailMasterRowModel), Is.True);
                    Assert.That(surfaceHost.CurrentSnapshot.Overlays.Any(), Is.True);
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void MainWindow_WhenCustomDetailScenarioIsSelected_ConfiguresRowDetailProviderAndShowsCustomDetailAfterToggle()
        {
            var window = new MainWindow();

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(window);

                var viewModel = (DemoShellViewModel)window.DataContext;
                var grid = (WpfGrid)window.FindName("DemoGrid");

                viewModel.SelectExample("custom-detail");
                GridSurfaceTestHost.FlushDispatcher(window);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var firstRow = surfaceHost.CurrentSnapshot.Rows.First(row => !row.IsGroupHeader && !row.IsDetailsHost);

                GridSurfaceTestHost.ClickPointViaRoutedUi(surfaceHost, x: 8d, y: firstRow.Bounds.Y + (firstRow.Bounds.Height / 2d));
                GridSurfaceTestHost.FlushDispatcher(window);
                surfaceHost.UpdateLayout();
                GridSurfaceTestHost.FlushDispatcher(window);

                var detailPresenter = GridSurfaceTestHost.FindVisualChildren<GridRowDetailPresenter>(surfaceHost)
                    .FirstOrDefault(presenter => presenter.OverlayData != null);
                var detailText = detailPresenter == null ? string.Empty : GridSurfaceTestHost.ReadVisibleText(detailPresenter);

                Assert.Multiple(() =>
                {
                    Assert.That(grid.RowDetailProvider, Is.Not.Null);
                    Assert.That(grid.RowDetailContentFactory, Is.Not.Null);
                    Assert.That(surfaceHost.CurrentSnapshot.Rows.Any(row => row.IsDetailsHost), Is.True);
                    Assert.That(detailPresenter, Is.Not.Null);
                    Assert.That(detailText, Does.Contain("Parcel Stare Miasto 1001"));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void MainWindow_WhenInsideMasterDetailExpandsManyRows_KeepsDetailPresenterScrollableAndBounded()
        {
            var window = new MainWindow();

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(window);

                var viewModel = (DemoShellViewModel)window.DataContext;
                var grid = (WpfGrid)window.FindName("DemoGrid");

                viewModel.SelectExample("master-detail");
                GridSurfaceTestHost.FlushDispatcher(window);

                var buildingRootPathId = viewModel.GridHierarchyRoots[1].PathId;
                grid.ExpandHierarchyNodeAsync(buildingRootPathId).GetAwaiter().GetResult();
                GridSurfaceTestHost.FlushDispatcher(window);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var detailPresenter = GridSurfaceTestHost.FindVisualChildren<GridMasterDetailPresenter>(surfaceHost).FirstOrDefault();
                var visibleText = GridSurfaceTestHost.ReadVisibleText(detailPresenter);
                var detailsHostRow = surfaceHost.CurrentSnapshot.Rows.FirstOrDefault(row => row.IsDetailsHost);

                Assert.Multiple(() =>
                {
                    Assert.That(detailPresenter, Is.Not.Null);
                    Assert.That(detailsHostRow, Is.Not.Null);
                    Assert.That(detailsHostRow.Bounds.Height, Is.LessThanOrEqualTo(340d));
                    Assert.That(detailPresenter.ActualHeight, Is.LessThanOrEqualTo(340d));
                    Assert.That(visibleText, Does.Contain("Building Fabryczna 2"));
                    Assert.That(visibleText, Does.Contain("Object name"));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void MainWindow_WhenMasterDetailOverlayIsExpanded_DetailFilterAndOptionsRemainInteractive()
        {
            var window = new MainWindow();

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(window);

                var viewModel = (DemoShellViewModel)window.DataContext;
                var grid = (WpfGrid)window.FindName("DemoGrid");

                viewModel.SelectExample("master-detail");
                GridSurfaceTestHost.FlushDispatcher(window);

                var rootPathId = viewModel.GridHierarchyRoots[0].PathId;
                grid.ExpandHierarchyNodeAsync(rootPathId).GetAwaiter().GetResult();
                GridSurfaceTestHost.FlushDispatcher(window);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var detailPresenter = GridSurfaceTestHost.FindVisualChildren<GridMasterDetailPresenter>(surfaceHost).FirstOrDefault();
                var filterTextBox = GridSurfaceTestHost.FindElementByAutomationId<TextBox>(
                    detailPresenter,
                    "surface.master-detail.filter." + rootPathId + ".ObjectName");
                var optionsButton = GridSurfaceTestHost.FindElementByAutomationId<Button>(
                    detailPresenter,
                    "surface.master-detail.options." + rootPathId);

                Assert.Multiple(() =>
                {
                    Assert.That(detailPresenter, Is.Not.Null);
                    Assert.That(filterTextBox, Is.Not.Null);
                    Assert.That(filterTextBox?.IsEnabled, Is.True);
                    Assert.That(filterTextBox?.IsReadOnly, Is.False);
                    Assert.That(optionsButton, Is.Not.Null);
                    Assert.That(optionsButton?.IsEnabled, Is.True);
                });

                if (detailPresenter == null || filterTextBox == null || optionsButton == null)
                {
                    return;
                }

                var filterPoint = filterTextBox.TranslatePoint(
                    new Point(filterTextBox.ActualWidth / 2d, filterTextBox.ActualHeight / 2d),
                    surfaceHost);
                var filterHit = GridSurfaceTestHost.FindVisibleElementAtPoint(surfaceHost, filterPoint.X, filterPoint.Y);
                var firstMaster = grid.RowsView.Cast<object>().OfType<GridMasterDetailMasterRowModel>().First();
                var initialDetailCount = firstMaster.DetailRows.Count;

                optionsButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, optionsButton));
                GridSurfaceTestHost.FlushDispatcher(window);

                var contextMenu = optionsButton.ContextMenu;
                Assert.Multiple(() =>
                {
                    Assert.That(contextMenu, Is.Not.Null);
                    Assert.That(contextMenu?.IsOpen, Is.True, "Detail options button should open the grid options menu in the live demo window.");
                });

                var matchingObjectName = Convert.ToString(firstMaster.DetailRows.Last()["ObjectName"], System.Globalization.CultureInfo.CurrentCulture) ?? string.Empty;
                var filterToken = matchingObjectName
                    .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .LastOrDefault() ?? matchingObjectName;

                FocusElement(filterTextBox);
                filterTextBox.Text = filterToken;
                GridSurfaceTestHost.FlushDispatcher(window);

                Assert.Multiple(() =>
                {
                    Assert.That(IsSameElementOrDescendant(filterTextBox, filterHit), Is.True, "Pointer hit should resolve to the detail filter editor.");
                    Assert.That(firstMaster.DetailRows.Count, Is.LessThan(initialDetailCount).And.GreaterThan(0), "Detail filter should actively narrow the visible detail records.");
                    Assert.That(firstMaster.DetailRows.All(row =>
                        Convert.ToString(row["ObjectName"], System.Globalization.CultureInfo.CurrentCulture)?.Contains(filterToken, StringComparison.OrdinalIgnoreCase) == true), Is.True);
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void MainWindow_WhenMasterDetailPlacementIsToggled_SwitchesBetweenInsideAndOutsideLayouts()
        {
            var window = new MainWindow();

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(window);

                var viewModel = (DemoShellViewModel)window.DataContext;
                var grid = (WpfGrid)window.FindName("DemoGrid");

                viewModel.SelectExample("master-detail");
                GridSurfaceTestHost.FlushDispatcher(window);

                Assert.That(grid.VisibleColumns.Select(column => column.ColumnId), Is.EqualTo(new[] { "Category", "Description" }));
                Assert.That(grid.RowsView.Cast<object>().All(row => row is GridMasterDetailMasterRowModel), Is.True);

                InvokePrivateClick(window, "HandleToggleMasterDetailPlacementClick");
                GridSurfaceTestHost.FlushDispatcher(window);

                Assert.Multiple(() =>
                {
                    Assert.That(viewModel.IsMasterDetailOutside, Is.True);
                    Assert.That(grid.VisibleColumns.Select(column => column.ColumnId), Is.EqualTo(new[] { "Category", "Description", "ObjectName", "ObjectId", "GeometryType", "Status" }));
                    Assert.That(grid.RowsView.Cast<object>().All(row => row is GridHierarchyNodeRowModel), Is.True);
                });

                var rootPathId = viewModel.GridHierarchyRoots[0].PathId;
                grid.ExpandHierarchyNodeAsync(rootPathId).GetAwaiter().GetResult();
                GridSurfaceTestHost.FlushDispatcher(window);

                var expandedRows = grid.RowsView.Cast<object>().ToArray();

                Assert.Multiple(() =>
                {
                    Assert.That(expandedRows.Any(row => row is GridMasterDetailHeaderRowModel), Is.True);
                    Assert.That(expandedRows.Any(row => row is GridMasterDetailDetailRowModel), Is.True);
                });

                InvokePrivateClick(window, "HandleToggleMasterDetailPlacementClick");
                GridSurfaceTestHost.FlushDispatcher(window);

                Assert.Multiple(() =>
                {
                    Assert.That(viewModel.IsMasterDetailOutside, Is.False);
                    Assert.That(grid.VisibleColumns.Select(column => column.ColumnId), Is.EqualTo(new[] { "Category", "Description" }));
                    Assert.That(grid.RowsView.Cast<object>().All(row => row is GridMasterDetailMasterRowModel), Is.True);
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void FilterRow_FilterEditors_ShouldAlignWithVisibleHeaders_AndMatchHeaderWidths()
        {
            var viewModel = new DemoShellViewModel("Wpf");
            var grid = CreateBoundDemoGrid(viewModel);
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 1280, height: 720);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                viewModel.SelectExample("filtering");
                GridSurfaceTestHost.FlushDispatcher(grid);

                var filterItemsControl = (ItemsControl)grid.FindName("FilterItemsControl");
                Assert.That(filterItemsControl, Is.Not.Null);

                grid.UpdateLayout();
                GridSurfaceTestHost.FlushDispatcher(grid);

                var headers = GridSurfaceTestHost.FindVisualChildren<GridColumnHeaderPresenter>(grid)
                    .Where(header => header.HeaderData != null)
                    .OrderBy(header => header.HeaderData.Bounds.X)
                    .Take(4)
                    .ToArray();

                Assert.That(headers.Length, Is.EqualTo(4));

                for (var index = 0; index < headers.Length; index++)
                {
                    var filterPresenter = (FrameworkElement)filterItemsControl.ItemContainerGenerator.ContainerFromIndex(index);
                    Assert.That(filterPresenter, Is.Not.Null, $"Filter presenter {index} was not generated.");
                    Assert.That(GridSurfaceTestHost.FindDescendant<TextBox>(filterPresenter), Is.Not.Null, $"Filter editor {index} was not generated.");

                    var filterPoint = filterPresenter.TranslatePoint(new Point(0, 0), grid);
                    var headerPoint = headers[index].TranslatePoint(new Point(0, 0), grid);

                    Assert.Multiple(() =>
                    {
                        Assert.That(Math.Abs(filterPoint.X - headerPoint.X), Is.LessThanOrEqualTo(2d), $"Filter {index} X offset drifted from the matching header.");
                        Assert.That(Math.Abs(filterPresenter.ActualWidth - headers[index].ActualWidth), Is.LessThanOrEqualTo(2d), $"Filter {index} width drifted from the matching header.");

                        var separator = GridSurfaceTestHost.FindVisualChildren<Border>(headers[index])
                            .FirstOrDefault(border => border.Width == 1d && border.HorizontalAlignment == HorizontalAlignment.Right);
                        Assert.That(separator, Is.Not.Null, $"Header separator {index} was not generated.");
                        if (separator != null)
                        {
                            var separatorPoint = separator.TranslatePoint(new Point(0, 0), grid);
                            var expectedSeparatorX = filterPoint.X + filterPresenter.ActualWidth - separator.ActualWidth;
                            Assert.That(Math.Abs(separatorPoint.X - expectedSeparatorX), Is.LessThanOrEqualTo(1d), $"Filter separator {index} drifted from the matching header separator.");
                        }
                    });
                }
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void FocusColumnFilter_WhenTargetFilterIsOffscreen_ShouldScrollSurfaceAndKeepFilterAligned()
        {
            var viewModel = new DemoShellViewModel("Wpf");
            var grid = CreateBoundDemoGrid(viewModel);
            grid.Width = 900;
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 900, height: 720);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                viewModel.SelectExample("filtering");
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var filterScrollViewer = (ScrollViewer)grid.FindName("FilterScrollViewer");
                var filterItemsControl = (ItemsControl)grid.FindName("FilterItemsControl");

                Assert.That(filterScrollViewer, Is.Not.Null);
                Assert.That(filterItemsControl, Is.Not.Null);
                Assert.That(grid.VisibleColumns.Count, Is.GreaterThan(4));

                var targetColumnId = grid.VisibleColumns.Last().ColumnId;
                var targetColumnIndex = grid.VisibleColumns
                    .Select((column, index) => new { column.ColumnId, Index = index })
                    .First(candidate => string.Equals(candidate.ColumnId, targetColumnId, StringComparison.OrdinalIgnoreCase))
                    .Index;
                Assert.That(grid.FocusColumnFilter(targetColumnId), Is.True);

                GridSurfaceTestHost.FlushDispatcher(grid);
                grid.UpdateLayout();
                GridSurfaceTestHost.FlushDispatcher(grid);

                var filterContainer = (FrameworkElement)filterItemsControl.ItemContainerGenerator.ContainerFromIndex(targetColumnIndex);
                var filterEditor = FindFilterEditor(grid, targetColumnId);
                var header = GridSurfaceTestHost.FindElementByAutomationId<GridColumnHeaderPresenter>(
                    surfaceHost,
                    "surface.column-header." + targetColumnId);

                Assert.Multiple(() =>
                {
                    Assert.That(filterScrollViewer.HorizontalOffset, Is.GreaterThan(0d), "Focusing an offscreen filter should reveal it horizontally.");
                    Assert.That(Math.Abs(filterScrollViewer.HorizontalOffset - surfaceHost.HorizontalOffset), Is.LessThanOrEqualTo(1d), "Filter row and surface must keep the same horizontal offset.");
                    Assert.That(filterContainer, Is.Not.Null, "Focused filter container should be generated.");
                    Assert.That(filterEditor, Is.Not.Null, "Focused filter editor should be available.");
                    Assert.That(header, Is.Not.Null, "Matching header should be rendered after scrolling.");
                });

                if (filterContainer == null || filterEditor == null || header == null)
                {
                    return;
                }

                var filterPoint = filterContainer.TranslatePoint(new Point(0, 0), grid);
                var headerPoint = header.TranslatePoint(new Point(0, 0), grid);

                Assert.Multiple(() =>
                {
                    Assert.That(Math.Abs(filterPoint.X - headerPoint.X), Is.LessThanOrEqualTo(2d), "Focused filter drifted away from its header.");
                });
            }
            finally
            {
                window.Close();
            }
        }

        private static WpfGrid CreateBoundDemoGrid(DemoShellViewModel viewModel)
        {
            var grid = new WpfGrid
            {
                Width = 1280,
                Height = 720,
                DataContext = viewModel,
                LanguageDirectory = global::PhialeGrid.Wpf.Tests.GridTestRepositoryPaths.GridLanguagesDirectory,
            };

            BindingOperations.SetBinding(grid, WpfGrid.ItemsSourceProperty, new Binding(nameof(DemoShellViewModel.GridRecords)));
            BindingOperations.SetBinding(grid, WpfGrid.ColumnsProperty, new Binding(nameof(DemoShellViewModel.GridColumns)));
            BindingOperations.SetBinding(grid, WpfGrid.EditSessionContextProperty, new Binding(nameof(DemoShellViewModel.GridEditSessionContext)));
            BindingOperations.SetBinding(grid, WpfGrid.GroupsProperty, new Binding(nameof(DemoShellViewModel.GridGroups)) { Mode = BindingMode.TwoWay });
            BindingOperations.SetBinding(grid, WpfGrid.SortsProperty, new Binding(nameof(DemoShellViewModel.GridSorts)) { Mode = BindingMode.TwoWay });
            BindingOperations.SetBinding(grid, WpfGrid.SummariesProperty, new Binding(nameof(DemoShellViewModel.GridSummaries)));
            BindingOperations.SetBinding(grid, WpfGrid.IsGridReadOnlyProperty, new Binding(nameof(DemoShellViewModel.IsGridReadOnly)));
            return grid;
        }

        private static TextBox FindFilterEditor(WpfGrid grid, string columnId)
        {
            var filterItemsControl = (ItemsControl)grid.FindName("FilterItemsControl");
            Assert.That(filterItemsControl, Is.Not.Null);

            return GridSurfaceTestHost.FindVisualChildren<TextBox>(filterItemsControl)
                .FirstOrDefault(textBox => textBox.DataContext is GridColumnBindingModel model &&
                                           string.Equals(model.ColumnId, columnId, StringComparison.OrdinalIgnoreCase));
        }

        private static void FocusElement(UIElement element)
        {
            Assert.That(element, Is.Not.Null);
            element.Dispatcher.Invoke(() =>
            {
                element.Focus();
            });
        }

        private static bool IsSameElementOrDescendant(DependencyObject ancestor, DependencyObject candidate)
        {
            while (candidate != null)
            {
                if (ReferenceEquals(ancestor, candidate))
                {
                    return true;
                }

                candidate = VisualTreeHelper.GetParent(candidate);
            }

            return false;
        }

        private static void InvokePrivateClick(object target, string methodName)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, methodName + " should exist on the target type.");
            method.Invoke(target, new object[] { target, new RoutedEventArgs(Button.ClickEvent) });
        }
    }
}

