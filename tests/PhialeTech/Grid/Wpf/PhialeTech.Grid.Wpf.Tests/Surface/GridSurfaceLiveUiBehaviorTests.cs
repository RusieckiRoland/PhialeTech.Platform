using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using NUnit.Framework;
using PhialeGrid.Core.Columns;
using PhialeGrid.Core.Hierarchy;
using PhialeGrid.Core.Query;
using PhialeGrid.Core.Surface;
using PhialeGrid.Core.Summaries;
using PhialeTech.Components.Shared.ViewModels;
using PhialeTech.PhialeGrid.Wpf.Controls;
using PhialeTech.PhialeGrid.Wpf.Surface;
using PhialeTech.PhialeGrid.Wpf.Surface.Presenters;
using UniversalInput.Contracts;
using WpfGrid = PhialeTech.PhialeGrid.Wpf.Controls.PhialeGrid;

namespace PhialeGrid.Wpf.Tests.Surface
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    [NonParallelizable]
    public sealed class GridSurfaceLiveUiBehaviorTests
    {
        [Test]
        public void RoutedUi_WhenGroupedHeaderClicked_ExpandsAndCollapsesVisibleSurface()
        {
            var grid = CreateGroupedGrid();
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 640, height: 320);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var firstRow = surfaceHost.CurrentSnapshot.Rows[0];
                var initialRenderedCells = CountRenderedCells(surfaceHost);

                GridSurfaceTestHost.ClickPointViaRoutedUi(surfaceHost, x: 10, y: firstRow.Bounds.Y + (firstRow.Bounds.Height / 2d));
                GridSurfaceTestHost.FlushDispatcher(grid);

                var expandedRows = surfaceHost.CurrentSnapshot.Rows.Count;
                var expandedRenderedCells = CountRenderedCells(surfaceHost);

                GridSurfaceTestHost.ClickPointViaRoutedUi(surfaceHost, x: 10, y: firstRow.Bounds.Y + (firstRow.Bounds.Height / 2d));
                GridSurfaceTestHost.FlushDispatcher(grid);

                Assert.Multiple(() =>
                {
                    Assert.That(expandedRows, Is.EqualTo(4));
                    Assert.That(expandedRenderedCells, Is.GreaterThan(initialRenderedCells));
                    Assert.That(surfaceHost.CurrentSnapshot.Rows.Count, Is.EqualTo(2));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void RoutedUi_WhenGroupedHierarchyToggleClicked_ExpandsAndCollapsesVisibleSurface()
        {
            var grid = CreateGroupedGrid();
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 640, height: 320);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var firstRow = surfaceHost.CurrentSnapshot.Rows[0];
                var toggleX = 10d;
                var toggleY = firstRow.Bounds.Y + (firstRow.Bounds.Height / 2d);

                GridSurfaceTestHost.ClickPointViaRoutedUi(surfaceHost, toggleX, toggleY);
                GridSurfaceTestHost.FlushDispatcher(grid);

                var expandedRows = surfaceHost.CurrentSnapshot.Rows.Count;

                GridSurfaceTestHost.ClickPointViaRoutedUi(surfaceHost, toggleX, toggleY);
                GridSurfaceTestHost.FlushDispatcher(grid);

                Assert.Multiple(() =>
                {
                    Assert.That(expandedRows, Is.EqualTo(4));
                    Assert.That(surfaceHost.CurrentSnapshot.Rows.Count, Is.EqualTo(2));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void RoutedUi_WhenHierarchyToggleAndLoadMoreClicked_ShowsAdditionalVisibleRows()
        {
            var (roots, controller) = CreatePagedHierarchySource();
            var grid = CreateHierarchyGrid();
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 640, height: 320);

            try
            {
                grid.ItemsSource = roots.Select(root => root.Item).ToArray();
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                grid.SetHierarchySource(roots, controller, displayColumnId: "ObjectName");
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var rootRow = surfaceHost.CurrentSnapshot.Rows[0];

                GridSurfaceTestHost.ClickPointViaRoutedUi(surfaceHost, x: 10, y: rootRow.Bounds.Y + (rootRow.Bounds.Height / 2d));
                GridSurfaceTestHost.FlushDispatcher(grid);

                Assert.That(VisibleCellTexts(surfaceHost).Any(text => text.Contains("Child A1", StringComparison.Ordinal)), Is.True);

                var loadMoreRow = surfaceHost.CurrentSnapshot.Rows.Single(row => row.IsLoadMore);
                GridSurfaceTestHost.ClickPointViaRoutedUi(surfaceHost, x: 90, y: loadMoreRow.Bounds.Y + (loadMoreRow.Bounds.Height / 2d));
                GridSurfaceTestHost.FlushDispatcher(grid);

                Assert.Multiple(() =>
                {
                    Assert.That(surfaceHost.CurrentSnapshot.Rows.Any(row => row.IsLoadMore), Is.False);
                    Assert.That(VisibleCellTexts(surfaceHost).Any(text => text.Contains("Child A2", StringComparison.Ordinal)), Is.True);
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void RoutedUi_WhenHierarchyToggleClicked_ShowsAdditionalVisibleRows()
        {
            var (roots, controller) = CreatePagedHierarchySource();
            var grid = CreateHierarchyGrid();
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 640, height: 320);

            try
            {
                grid.ItemsSource = roots.Select(root => root.Item).ToArray();
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                grid.SetHierarchySource(roots, controller, displayColumnId: "ObjectName");
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var rootRow = surfaceHost.CurrentSnapshot.Rows[0];
                var toggleX = 10d;
                var toggleY = rootRow.Bounds.Y + (rootRow.Bounds.Height / 2d);

                GridSurfaceTestHost.ClickPointViaRoutedUi(surfaceHost, toggleX, toggleY);
                GridSurfaceTestHost.FlushDispatcher(grid);

                Assert.That(VisibleCellTexts(surfaceHost).Any(text => text.Contains("Child A1", StringComparison.Ordinal)), Is.True);

                GridSurfaceTestHost.ClickPointViaRoutedUi(surfaceHost, toggleX, toggleY);
                GridSurfaceTestHost.FlushDispatcher(grid);

                Assert.That(VisibleCellTexts(surfaceHost).Any(text => text.Contains("Child A1", StringComparison.Ordinal)), Is.False);
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void VisibleSurface_WhenMultiSortApplied_RendersVisibleSortOrdinalBadge()
        {
            var grid = CreateThreeColumnGrid();
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 720, height: 320);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                grid.Sorts = new[]
                {
                    new GridSortDescriptor("Name", GridSortDirection.Ascending),
                    new GridSortDescriptor("City", GridSortDirection.Ascending),
                    new GridSortDescriptor("Status", GridSortDirection.Ascending),
                };
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);

                var citySortOrder = GridSurfaceTestHost.FindElementByAutomationId<TextBlock>(surfaceHost, "surface.column-header.City.sort-order");
                var statusSortOrder = GridSurfaceTestHost.FindElementByAutomationId<TextBlock>(surfaceHost, "surface.column-header.Status.sort-order");

                Assert.Multiple(() =>
                {
                    Assert.That(citySortOrder, Is.Not.Null);
                    Assert.That(statusSortOrder, Is.Not.Null);
                    Assert.That(citySortOrder.Text, Is.EqualTo("2"));
                    Assert.That(statusSortOrder.Text, Is.EqualTo("3"));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void RoutedUi_WhenColumnHeaderCenterClicked_ActivatesSortFromVisibleHeaderSurface()
        {
            var grid = CreateThreeColumnGrid();
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 720, height: 320);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var nameHeader = surfaceHost.CurrentSnapshot.Headers.Single(header => header.Kind == GridHeaderKind.ColumnHeader && header.HeaderKey == "Name");
                var centerX = nameHeader.Bounds.X + (nameHeader.Bounds.Width / 2d);
                var centerY = nameHeader.Bounds.Y + (nameHeader.Bounds.Height / 2d);

                var hitPresenter = GridSurfaceTestHost.FindVisibleAncestorAtPoint<GridColumnHeaderPresenter>(surfaceHost, centerX, centerY);
                GridSurfaceTestHost.ClickPointViaRoutedUi(surfaceHost, centerX, centerY);
                GridSurfaceTestHost.FlushDispatcher(grid);

                var sorts = grid.Sorts.ToArray();

                Assert.Multiple(() =>
                {
                    Assert.That(hitPresenter, Is.Not.Null);
                    Assert.That(sorts.Length, Is.EqualTo(1));
                    Assert.That(sorts[0].ColumnId, Is.EqualTo("Name"));
                    Assert.That(GridSurfaceTestHost.ReadVisibleText(hitPresenter), Does.Contain("▲"));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void RoutedUi_WhenColumnHeaderTextClicked_ActivatesSortFromVisibleHeaderSurface()
        {
            var grid = CreateThreeColumnGrid();
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 720, height: 320);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var headerText = GridSurfaceTestHost.FindElementByAutomationId<TextBlock>(surfaceHost, "surface.column-header.Name.text");
                Assert.That(headerText, Is.Not.Null);

                var point = headerText.TranslatePoint(
                    new System.Windows.Point(headerText.ActualWidth / 2d, headerText.ActualHeight / 2d),
                    surfaceHost.SurfacePanelForTesting);

                GridSurfaceTestHost.ClickPointViaRoutedUi(surfaceHost, point.X, point.Y);
                GridSurfaceTestHost.FlushDispatcher(grid);

                var sorts = grid.Sorts.ToArray();
                Assert.Multiple(() =>
                {
                    Assert.That(sorts.Length, Is.EqualTo(1));
                    Assert.That(sorts[0].ColumnId, Is.EqualTo("Name"));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void RoutedUi_WhenHeaderHasVerticalPointerJitter_StillActivatesSort()
        {
            var grid = CreateThreeColumnGrid();
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 720, height: 320);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var nameHeader = surfaceHost.CurrentSnapshot.Headers.Single(header => header.Kind == GridHeaderKind.ColumnHeader && header.HeaderKey == "Name");
                var centerX = nameHeader.Bounds.X + (nameHeader.Bounds.Width / 2d);
                var centerY = nameHeader.Bounds.Y + (nameHeader.Bounds.Height / 2d);

                GridSurfaceTestHost.PointerDownViaRoutedUi(surfaceHost, centerX, centerY);
                GridSurfaceTestHost.PointerMoveViaRoutedUi(surfaceHost, centerX, centerY + 14d);
                GridSurfaceTestHost.PointerUpViaRoutedUi(surfaceHost, centerX, centerY + 2d);
                GridSurfaceTestHost.FlushDispatcher(grid);

                Assert.That(grid.Sorts.Any(sort => string.Equals(sort.ColumnId, "Name", StringComparison.Ordinal)), Is.True);
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void RoutedUi_WhenColumnHeaderCenterDragged_ReordersFromVisibleHeaderSurface()
        {
            var grid = CreateThreeColumnGrid();
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 720, height: 320);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var nameHeader = surfaceHost.CurrentSnapshot.Headers.Single(header => header.Kind == GridHeaderKind.ColumnHeader && header.HeaderKey == "Name");
                var cityHeader = surfaceHost.CurrentSnapshot.Headers.Single(header => header.Kind == GridHeaderKind.ColumnHeader && header.HeaderKey == "City");

                var sourceX = nameHeader.Bounds.X + (nameHeader.Bounds.Width / 2d);
                var sourceY = nameHeader.Bounds.Y + (nameHeader.Bounds.Height / 2d);
                var targetX = cityHeader.Bounds.X + (cityHeader.Bounds.Width / 2d);
                var targetY = cityHeader.Bounds.Y + (cityHeader.Bounds.Height / 2d);

                var hitPresenter = GridSurfaceTestHost.FindVisibleAncestorAtPoint<GridColumnHeaderPresenter>(surfaceHost, sourceX, sourceY);
                GridSurfaceTestHost.DragPointViaRoutedUi(surfaceHost, sourceX, sourceY, targetX, targetY);
                GridSurfaceTestHost.FlushDispatcher(grid);

                Assert.Multiple(() =>
                {
                    Assert.That(hitPresenter, Is.Not.Null);
                    Assert.That(grid.VisibleColumns.Select(column => column.ColumnId).Take(2), Is.EqualTo(new[] { "City", "Name" }));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void RoutedUi_WhenGroupedDemoHeaderCenterClicked_ActivatesSortFromVisibleHeaderSurface()
        {
            var viewModel = new DemoShellViewModel("Wpf");
            viewModel.SelectExample("grouping");
            var grid = CreateBoundDemoGrid(viewModel);
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 1200, height: 720);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var objectNameHeader = surfaceHost.CurrentSnapshot.Headers.Single(header => header.Kind == GridHeaderKind.ColumnHeader && header.HeaderKey == "ObjectName");
                var centerX = objectNameHeader.Bounds.X + (objectNameHeader.Bounds.Width / 2d);
                var centerY = objectNameHeader.Bounds.Y + (objectNameHeader.Bounds.Height / 2d);

                var hitPresenter = GridSurfaceTestHost.FindVisibleAncestorAtPoint<GridColumnHeaderPresenter>(surfaceHost, centerX, centerY);
                GridSurfaceTestHost.ClickPointViaRoutedUi(surfaceHost, centerX, centerY);
                GridSurfaceTestHost.FlushDispatcher(grid);

                Assert.Multiple(() =>
                {
                    Assert.That(hitPresenter, Is.Not.Null);
                    Assert.That(grid.Sorts.Any(sort => string.Equals(sort.ColumnId, "ObjectName", StringComparison.Ordinal)), Is.True);
                    Assert.That(GridSurfaceTestHost.ReadVisibleText(hitPresenter), Does.Contain("▲"));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void RoutedUi_WhenColumnHeaderRightClicked_OpensContextMenuWithoutChangingSort()
        {
            var viewModel = new DemoShellViewModel("Wpf");
            viewModel.SelectExample("column-layout");
            var grid = CreateBoundDemoGrid(viewModel);
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 1200, height: 720);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var headerBand = (FrameworkElement)grid.FindName("SurfaceColumnHeaderBand");
                var header = GridSurfaceTestHost.FindVisualChildren<GridColumnHeaderPresenter>(headerBand)
                    .FirstOrDefault(candidate => string.Equals(candidate.HeaderData?.HeaderKey, "ObjectName", StringComparison.Ordinal));
                Assert.That(header, Is.Not.Null);

                var sortCountBefore = grid.Sorts.Count();
                var viewportHost = (FrameworkElement)grid.FindName("SurfaceTopViewportHost");
                Assert.That(viewportHost, Is.Not.Null);

                var headerCenter = header.TranslatePoint(
                    new Point(header.ActualWidth / 2d, header.ActualHeight / 2d),
                    viewportHost);
                var hitElement = GridSurfaceTestHost.FindVisibleElementAtPoint(viewportHost, headerCenter.X, headerCenter.Y);
                var hitHeader = FindAncestorOrSelf<GridColumnHeaderPresenter>(hitElement);
                Assert.That(hitHeader, Is.Not.Null, "Expected viewport hit-testing to land on the live column header presenter.");

                var tryOpenContextMenuMethod = typeof(WpfGrid).GetMethod("TryOpenColumnHeaderContextMenu", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(tryOpenContextMenuMethod, Is.Not.Null);

                var opened = (bool)tryOpenContextMenuMethod.Invoke(grid, new object[] { hitElement, null });
                Assert.That(opened, Is.True);

                var contextMenu = GridSurfaceTestHost.FindVisualChildren<GridColumnHeaderPresenter>(headerBand)
                    .Select(candidate => candidate.ContextMenu)
                    .FirstOrDefault(candidate => candidate != null);
                Assert.That(contextMenu, Is.Not.Null);
                var headers = contextMenu.Items
                    .OfType<MenuItem>()
                    .Select(item => Convert.ToString(item.Header, CultureInfo.CurrentCulture) ?? string.Empty)
                    .ToArray();

                Assert.Multiple(() =>
                {
                    Assert.That(contextMenu.IsOpen, Is.True);
                    Assert.That(grid.Sorts.Count(), Is.EqualTo(sortCountBefore));
                    Assert.That(headers, Does.Contain(grid.AutoFitColumnText));
                    Assert.That(headers, Does.Contain(grid.FreezeColumnText));
                    Assert.That(headers, Does.Contain(grid.HideColumnText));
                });

                contextMenu.IsOpen = false;

                grid.SetColumnFrozen("ObjectName", true);
                GridSurfaceTestHost.FlushDispatcher(grid);

                header = GridSurfaceTestHost.FindVisualChildren<GridColumnHeaderPresenter>(headerBand)
                    .FirstOrDefault(candidate => string.Equals(candidate.HeaderData?.HeaderKey, "ObjectName", StringComparison.Ordinal));
                Assert.That(header, Is.Not.Null);

                headerCenter = header.TranslatePoint(
                    new Point(header.ActualWidth / 2d, header.ActualHeight / 2d),
                    viewportHost);
                hitElement = GridSurfaceTestHost.FindVisibleElementAtPoint(viewportHost, headerCenter.X, headerCenter.Y);
                hitHeader = FindAncestorOrSelf<GridColumnHeaderPresenter>(hitElement);
                Assert.That(hitHeader, Is.Not.Null, "Expected viewport hit-testing to keep landing on the live column header presenter after refreeze.");

                opened = (bool)tryOpenContextMenuMethod.Invoke(grid, new object[] { hitElement, null });
                Assert.That(opened, Is.True);

                contextMenu = GridSurfaceTestHost.FindVisualChildren<GridColumnHeaderPresenter>(headerBand)
                    .Select(candidate => candidate.ContextMenu)
                    .FirstOrDefault(candidate => candidate != null);
                Assert.That(contextMenu, Is.Not.Null);
                headers = contextMenu.Items
                    .OfType<MenuItem>()
                    .Select(item => Convert.ToString(item.Header, CultureInfo.CurrentCulture) ?? string.Empty)
                    .ToArray();

                Assert.That(headers, Does.Contain(grid.UnfreezeColumnText));
                contextMenu.IsOpen = false;
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void RoutedUi_WhenHeaderDraggedAndResized_UpdatesVisibleLayout()
        {
            var grid = CreateThreeColumnGrid();
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 720, height: 320);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var nameHeader = surfaceHost.CurrentSnapshot.Headers.Single(header => header.Kind == GridHeaderKind.ColumnHeader && header.HeaderKey == "Name");
                var cityHeader = surfaceHost.CurrentSnapshot.Headers.Single(header => header.Kind == GridHeaderKind.ColumnHeader && header.HeaderKey == "City");

                var originalWidth = grid.VisibleColumns.First(column => column.ColumnId == "Name").Width;
                GridSurfaceTestHost.DragPointViaRoutedUi(
                    surfaceHost,
                    nameHeader.Bounds.X + nameHeader.Bounds.Width - 2d,
                    nameHeader.Bounds.Y + (nameHeader.Bounds.Height / 2d),
                    nameHeader.Bounds.X + nameHeader.Bounds.Width + 46d,
                    nameHeader.Bounds.Y + (nameHeader.Bounds.Height / 2d));
                GridSurfaceTestHost.FlushDispatcher(grid);

                var resizedWidth = grid.VisibleColumns.First(column => column.ColumnId == "Name").Width;

                GridSurfaceTestHost.DragPointViaRoutedUi(
                    surfaceHost,
                    nameHeader.Bounds.X + (nameHeader.Bounds.Width / 2d),
                    nameHeader.Bounds.Y + (nameHeader.Bounds.Height / 2d),
                    cityHeader.Bounds.X + (cityHeader.Bounds.Width * 0.75d),
                    cityHeader.Bounds.Y + (cityHeader.Bounds.Height / 2d));
                GridSurfaceTestHost.FlushDispatcher(grid);

                Assert.Multiple(() =>
                {
                    Assert.That(resizedWidth, Is.GreaterThan(originalWidth));
                    Assert.That(grid.VisibleColumns.Select(column => column.ColumnId).Take(2), Is.EqualTo(new[] { "City", "Name" }));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void RoutedUi_WhenHeaderDraggedNearNeighborHeader_ReordersColumns()
        {
            var grid = CreateThreeColumnGrid();
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 720, height: 320);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var nameHeader = surfaceHost.CurrentSnapshot.Headers.Single(header => header.Kind == GridHeaderKind.ColumnHeader && header.HeaderKey == "Name");
                var cityHeader = surfaceHost.CurrentSnapshot.Headers.Single(header => header.Kind == GridHeaderKind.ColumnHeader && header.HeaderKey == "City");

                GridSurfaceTestHost.DragPointViaRoutedUi(
                    surfaceHost,
                    nameHeader.Bounds.X + (nameHeader.Bounds.Width / 2d),
                    nameHeader.Bounds.Y + (nameHeader.Bounds.Height / 2d),
                    cityHeader.Bounds.X - 6d,
                    cityHeader.Bounds.Y + (cityHeader.Bounds.Height / 2d));
                GridSurfaceTestHost.FlushDispatcher(grid);

                Assert.That(grid.VisibleColumns.Select(column => column.ColumnId).Take(2), Is.EqualTo(new[] { "City", "Name" }));
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void RoutedUi_WhenResizeGrabIsNearHeaderEdge_UpdatesVisibleLayout()
        {
            var grid = CreateThreeColumnGrid();
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 720, height: 320);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var nameHeader = surfaceHost.CurrentSnapshot.Headers.Single(header => header.Kind == GridHeaderKind.ColumnHeader && header.HeaderKey == "Name");
                var originalWidth = grid.VisibleColumns.First(column => column.ColumnId == "Name").Width;

                GridSurfaceTestHost.DragPointViaRoutedUi(
                    surfaceHost,
                    nameHeader.Bounds.X + nameHeader.Bounds.Width - 2d,
                    nameHeader.Bounds.Y + (nameHeader.Bounds.Height / 2d),
                    nameHeader.Bounds.X + nameHeader.Bounds.Width + 32d,
                    nameHeader.Bounds.Y + (nameHeader.Bounds.Height / 2d));
                GridSurfaceTestHost.FlushDispatcher(grid);

                Assert.That(grid.VisibleColumns.First(column => column.ColumnId == "Name").Width, Is.GreaterThan(originalWidth));
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void RoutedUi_WhenCellEdited_UsesLiveEditorAndUpdatesPendingStatus()
        {
            var grid = CreateEditableGrid();
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 640, height: 320);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var nameCell = surfaceHost.CurrentSnapshot.Cells.Single(cell => cell.RowKey == "row-1" && cell.ColumnKey == "Name");

                GridSurfaceTestHost.DoubleClickPointViaRoutedUi(surfaceHost, nameCell.Bounds.X + 10d, nameCell.Bounds.Y + (nameCell.Bounds.Height / 2d));
                GridSurfaceTestHost.FlushDispatcher(grid);

                var editor = GridSurfaceTestHost.FindDescendant<TextBox>(surfaceHost);
                Assert.That(editor, Is.Not.Null, "Expected a live TextBox editor inside the surface.");

                GridSurfaceTestHost.SendKeyViaRoutedUi(surfaceHost, "DELETE");
                GridSurfaceTestHost.SendTextViaRoutedUi(surfaceHost, "Zed");
                GridSurfaceTestHost.SendKeyViaRoutedUi(surfaceHost, "ENTER");
                GridSurfaceTestHost.FlushDispatcher(grid);

                var selectionStatusBlock = FindTextBlock(grid, grid.SelectionStatusText);
                var editStatusBlock = FindTextBlock(grid, grid.EditStatusText);

                Assert.Multiple(() =>
                {
                    Assert.That(grid.HasPendingEdits, Is.True);
                    Assert.That(grid.PendingEditCount, Is.EqualTo(1));
                    Assert.That(selectionStatusBlock, Is.Not.Null);
                    Assert.That(editStatusBlock, Is.Not.Null);
                    Assert.That(editStatusBlock.Text, Is.EqualTo(grid.EditStatusText));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void RoutedUi_WhenEditIsCommittedByClickingAnotherCell_OldTextStaysInSourceCell()
        {
            var grid = CreateEditableGrid();
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 640, height: 320);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var nameCell = surfaceHost.CurrentSnapshot.Cells.Single(cell => cell.RowKey == "row-1" && cell.ColumnKey == "Name");
                var targetCell = surfaceHost.CurrentSnapshot.Cells.Single(cell => cell.RowKey == "row-2" && cell.ColumnKey == "City");

                GridSurfaceTestHost.DoubleClickPointViaRoutedUi(surfaceHost, nameCell.Bounds.X + 10d, nameCell.Bounds.Y + (nameCell.Bounds.Height / 2d));
                GridSurfaceTestHost.FlushDispatcher(grid);

                GridSurfaceTestHost.SendKeyViaRoutedUi(surfaceHost, "DELETE");
                GridSurfaceTestHost.SendTextViaRoutedUi(surfaceHost, "Zed");
                GridSurfaceTestHost.FlushDispatcher(grid);

                GridSurfaceTestHost.ClickPointViaRoutedUi(surfaceHost, targetCell.Bounds.X + 10d, targetCell.Bounds.Y + (targetCell.Bounds.Height / 2d));
                GridSurfaceTestHost.FlushDispatcher(grid);

                var rows = grid.ItemsSource.Cast<SurfaceRow>().ToArray();
                var committedNameCell = surfaceHost.CurrentSnapshot.Cells.Single(cell => cell.RowKey == "row-1" && cell.ColumnKey == "Name");
                var untouchedTargetCell = surfaceHost.CurrentSnapshot.Cells.Single(cell => cell.RowKey == "row-2" && cell.ColumnKey == "City");

                Assert.Multiple(() =>
                {
                    Assert.That(rows[0].Name, Is.EqualTo("Zed"));
                    Assert.That(committedNameCell.DisplayText, Is.EqualTo("Zed"));
                    Assert.That(untouchedTargetCell.DisplayText, Is.EqualTo("Gdansk"));
                    Assert.That(surfaceHost.CurrentSnapshot.CurrentCell, Is.Not.Null);
                    Assert.That(surfaceHost.CurrentSnapshot.CurrentCell.RowKey, Is.EqualTo("row-2"));
                    Assert.That(surfaceHost.CurrentSnapshot.CurrentCell.ColumnKey, Is.EqualTo("City"));
                    Assert.That(surfaceHost.CurrentSnapshot.ViewportState.IsInEditMode, Is.False);
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void LiveUi_WhenTouchAndDensityChange_VisibleHeadersAndRowsGrow()
        {
            var viewModel = new DemoShellViewModel("Wpf");
            viewModel.SelectExample("selection");

            var grid = CreateBoundDemoGrid(viewModel);
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 1200, height: 720);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var classicHeader = GridSurfaceTestHost.FindVisualChildren<GridRowHeaderPresenter>(surfaceHost).First();
                var classicHeight = classicHeader.ActualHeight;
                var classicWidth = classicHeader.ActualWidth;

                grid.InteractionMode = GridInteractionMode.Touch;
                grid.Density = GridDensity.Touch;
                GridSurfaceTestHost.FlushDispatcher(grid);

                var touchHeader = GridSurfaceTestHost.FindVisualChildren<GridRowHeaderPresenter>(surfaceHost).First();

                Assert.Multiple(() =>
                {
                    Assert.That(touchHeader.ActualHeight, Is.GreaterThan(classicHeight));
                    Assert.That(touchHeader.ActualWidth, Is.GreaterThan(classicWidth));
                    Assert.That(grid.ShowsTouchRowSelectors, Is.True);
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void LiveUi_WhenSummariesScenarioIsFilteredAndGrouped_SummaryValuesRemainVisible()
        {
            var viewModel = new DemoShellViewModel("Wpf");
            var grid = CreateBoundDemoGrid(viewModel);
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 1200, height: 720);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                viewModel.SelectExample("summaries");
                GridSurfaceTestHost.FlushDispatcher(grid);

                var municipalityColumn = grid.VisibleColumns.First(column => column.ColumnId == "Municipality");
                municipalityColumn.FilterText = "Wroclaw";
                grid.SetGroups(new[] { new GridGroupDescriptor("Category", GridSortDirection.Ascending) });
                GridSurfaceTestHost.FlushDispatcher(grid);

                var summaryValues = grid.SummaryItems.Select(item => item.ValueText).Where(text => !string.IsNullOrWhiteSpace(text)).ToArray();
                var visibleText = string.Join(" | ", GridSurfaceTestHost.FindVisualChildren<TextBlock>(grid).Select(block => block.Text).Where(text => !string.IsNullOrWhiteSpace(text)));

                Assert.Multiple(() =>
                {
                    Assert.That(grid.HasSummaries, Is.True);
                    Assert.That(summaryValues.Length, Is.GreaterThan(0));
                    Assert.That(summaryValues.All(value => visibleText.Contains(value, StringComparison.Ordinal)), Is.True);
                });
            }
            finally
            {
                window.Close();
            }
        }

        private static int CountRenderedCells(GridSurfaceHost surfaceHost)
        {
            return GridSurfaceTestHost.FindVisualChildren<GridCellPresenter>(surfaceHost)
                .Count(presenter => presenter.CellData != null && presenter.IsVisible);
        }

        private static string[] VisibleCellTexts(GridSurfaceHost surfaceHost)
        {
            return GridSurfaceTestHost.FindVisualChildren<GridCellPresenter>(surfaceHost)
                .Select(GridSurfaceTestHost.ReadVisibleText)
                .Where(text => !string.IsNullOrWhiteSpace(text))
                .ToArray();
        }

        private static TextBlock FindTextBlock(DependencyObject root, string text)
        {
            return GridSurfaceTestHost.FindVisualChildren<TextBlock>(root)
                .FirstOrDefault(block => string.Equals(block.Text, text, StringComparison.Ordinal));
        }

        private static T FindAncestorOrSelf<T>(DependencyObject source)
            where T : DependencyObject
        {
            while (source != null)
            {
                if (source is T match)
                {
                    return match;
                }

                source = VisualTreeHelper.GetParent(source);
            }

            return null;
        }

        private static WpfGrid CreateThreeColumnGrid()
        {
            return new WpfGrid
            {
                Width = 720,
                Height = 320,
                IsGridReadOnly = true,
                LanguageDirectory = global::PhialeGrid.Wpf.Tests.GridTestRepositoryPaths.GridLanguagesDirectory,
                Columns = new[]
                {
                    new GridColumnDefinition("Name", "Name", width: 160, displayIndex: 0),
                    new GridColumnDefinition("City", "City", width: 160, displayIndex: 1),
                    new GridColumnDefinition("Status", "Status", width: 160, displayIndex: 2),
                },
                ItemsSource = new[]
                {
                    new SurfaceRow { Id = "row-1", Name = "Alpha", City = "Warsaw", Status = "Ready" },
                    new SurfaceRow { Id = "row-2", Name = "Beta", City = "Gdansk", Status = "Draft" },
                    new SurfaceRow { Id = "row-3", Name = "Gamma", City = "Krakow", Status = "Ready" },
                },
            };
        }

        private static WpfGrid CreateGroupedGrid()
        {
            return new WpfGrid
            {
                Width = 640,
                Height = 320,
                IsGridReadOnly = true,
                LanguageDirectory = global::PhialeGrid.Wpf.Tests.GridTestRepositoryPaths.GridLanguagesDirectory,
                Columns = new[]
                {
                    new GridColumnDefinition("Name", "Name", width: 140, displayIndex: 0),
                    new GridColumnDefinition("City", "City", width: 140, displayIndex: 1),
                },
                Groups = new[] { new GridGroupDescriptor("City") },
                ItemsSource = new[]
                {
                    new SurfaceRow { Id = "row-1", Name = "Alpha One", City = "Alpha" },
                    new SurfaceRow { Id = "row-2", Name = "Alpha Two", City = "Alpha" },
                    new SurfaceRow { Id = "row-3", Name = "Beta One", City = "Beta" },
                },
            };
        }

        private static WpfGrid CreateEditableGrid()
        {
            return new WpfGrid
            {
                Width = 640,
                Height = 320,
                IsGridReadOnly = false,
                LanguageDirectory = global::PhialeGrid.Wpf.Tests.GridTestRepositoryPaths.GridLanguagesDirectory,
                Columns = new[]
                {
                    new GridColumnDefinition("Name", "Name", width: 160, displayIndex: 0),
                    new GridColumnDefinition("City", "City", width: 160, displayIndex: 1),
                },
                ItemsSource = new[]
                {
                    new SurfaceRow { Id = "row-1", Name = "Alpha", City = "Warsaw" },
                    new SurfaceRow { Id = "row-2", Name = "Beta", City = "Gdansk" },
                },
            };
        }

        private static WpfGrid CreateHierarchyGrid()
        {
            return new WpfGrid
            {
                Width = 640,
                Height = 320,
                IsGridReadOnly = true,
                LanguageDirectory = global::PhialeGrid.Wpf.Tests.GridTestRepositoryPaths.GridLanguagesDirectory,
                Columns = new[]
                {
                    new GridColumnDefinition("ObjectName", "Object Name", width: 180, displayIndex: 0),
                    new GridColumnDefinition("Status", "Status", width: 120, displayIndex: 1),
                },
                ItemsSource = Array.Empty<object>(),
            };
        }

        private static (IReadOnlyList<GridHierarchyNode<object>> Roots, GridHierarchyController<object> Controller) CreatePagedHierarchySource()
        {
            var rootAlpha = new GridHierarchyNode<object>(
                "root-alpha",
                new System.Collections.Generic.Dictionary<string, object> { ["Id"] = "root-alpha", ["ObjectName"] = "Root Alpha", ["Status"] = "Ready" },
                canExpand: true);

            var provider = new TestPagedHierarchyProvider(new System.Collections.Generic.Dictionary<string, System.Collections.Generic.IReadOnlyList<System.Collections.Generic.IReadOnlyList<GridHierarchyNode<object>>>>(StringComparer.OrdinalIgnoreCase)
            {
                [rootAlpha.PathId] = new[]
                {
                    (System.Collections.Generic.IReadOnlyList<GridHierarchyNode<object>>)new[]
                    {
                        new GridHierarchyNode<object>("child-1", new System.Collections.Generic.Dictionary<string, object> { ["Id"] = "child-1", ["ObjectName"] = "Child A1", ["Status"] = "Ok" }, canExpand: false, parentId: rootAlpha.PathId),
                    },
                    new[]
                    {
                        new GridHierarchyNode<object>("child-2", new System.Collections.Generic.Dictionary<string, object> { ["Id"] = "child-2", ["ObjectName"] = "Child A2", ["Status"] = "Ok" }, canExpand: false, parentId: rootAlpha.PathId),
                    },
                },
            });

            return (new[] { rootAlpha }, new GridHierarchyController<object>(provider, pageSize: 1));
        }

        private static WpfGrid CreateBoundDemoGrid(DemoShellViewModel viewModel)
        {
            var grid = new WpfGrid
            {
                Width = 1200,
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

        private sealed class SurfaceRow : IDataErrorInfo
        {
            public string Id { get; set; }

            public string Name { get; set; }

            public string City { get; set; }

            public string Status { get; set; }

            public string Error => string.Empty;

            public string this[string columnName]
            {
                get
                {
                    if (string.Equals(columnName, nameof(Name), StringComparison.OrdinalIgnoreCase) &&
                        string.IsNullOrWhiteSpace(Name))
                    {
                        return "Name is required.";
                    }

                    return string.Empty;
                }
            }
        }

        private sealed class TestPagedHierarchyProvider : IGridHierarchyPagingProvider<object>
        {
            private readonly System.Collections.Generic.IReadOnlyDictionary<string, System.Collections.Generic.IReadOnlyList<System.Collections.Generic.IReadOnlyList<GridHierarchyNode<object>>>> _pagesByPath;

            public TestPagedHierarchyProvider(System.Collections.Generic.IReadOnlyDictionary<string, System.Collections.Generic.IReadOnlyList<System.Collections.Generic.IReadOnlyList<GridHierarchyNode<object>>>> pagesByPath)
            {
                _pagesByPath = pagesByPath;
            }

            public System.Threading.Tasks.Task<System.Collections.Generic.IReadOnlyList<GridHierarchyNode<object>>> LoadChildrenAsync(GridHierarchyNode<object> parent, System.Threading.CancellationToken cancellationToken)
            {
                if (parent == null || !_pagesByPath.TryGetValue(parent.PathId, out var pages) || pages.Count == 0)
                {
                    return System.Threading.Tasks.Task.FromResult((System.Collections.Generic.IReadOnlyList<GridHierarchyNode<object>>)Array.Empty<GridHierarchyNode<object>>());
                }

                return System.Threading.Tasks.Task.FromResult(pages[0]);
            }

            public System.Threading.Tasks.Task<GridHierarchyPage<object>> LoadChildrenPageAsync(GridHierarchyNode<object> parent, int offset, int size, System.Threading.CancellationToken cancellationToken)
            {
                if (parent == null || !_pagesByPath.TryGetValue(parent.PathId, out var pages))
                {
                    return System.Threading.Tasks.Task.FromResult(new GridHierarchyPage<object>(Array.Empty<GridHierarchyNode<object>>(), hasMore: false));
                }

                var pageIndex = Math.Min(offset, pages.Count - 1);
                var page = pages[pageIndex];
                return System.Threading.Tasks.Task.FromResult(new GridHierarchyPage<object>(page, hasMore: pageIndex < pages.Count - 1));
            }
        }
    }
}
