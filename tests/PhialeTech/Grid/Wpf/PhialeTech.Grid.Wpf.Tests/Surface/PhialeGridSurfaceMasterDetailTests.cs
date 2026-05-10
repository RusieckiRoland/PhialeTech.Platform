using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using NUnit.Framework;
using PhialeGrid.Core.Columns;
using PhialeGrid.Core.Hierarchy;
using PhialeGrid.Core.Surface;
using PhialeGrid.Localization;
using PhialeTech.PhialeGrid.Wpf.Controls;
using PhialeTech.PhialeGrid.Wpf.Surface.Presenters;
using WpfGrid = PhialeTech.PhialeGrid.Wpf.Controls.PhialeGrid;

namespace PhialeGrid.Wpf.Tests.Surface
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public sealed class PhialeGridSurfaceMasterDetailTests
    {
        [Test]
        public void MasterDetailOutsideSurface_InitialSnapshotContainsOnlyMasterRows()
        {
            var (roots, controller) = CreateMasterDetailSource();
            var grid = CreateMasterDetailGrid();
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 900, height: 420);

            try
            {
                grid.ItemsSource = roots.Select(root => root.Item).ToArray();
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                grid.SetMasterDetailSource(
                    roots,
                    controller,
                    new[] { "ObjectName", "ObjectId", "GeometryType", "Status" },
                    masterDisplayColumnId: "Category",
                    detailDisplayColumnId: "ObjectName",
                    detailHeaderPlacementMode: GridMasterDetailHeaderPlacementMode.Outside);
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);

                Assert.Multiple(() =>
                {
                    Assert.That(surfaceHost.CurrentSnapshot.Rows.Count, Is.EqualTo(roots.Count));
                    Assert.That(surfaceHost.CurrentSnapshot.Rows.All(row => row.HierarchyLevel == 0), Is.True);
                    Assert.That(surfaceHost.CurrentSnapshot.Rows.All(row => row.HasHierarchyChildren), Is.True);
                    Assert.That(surfaceHost.CurrentSnapshot.Cells.Any(cell => cell.DisplayText.Contains("Category Alpha")), Is.True);
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void MasterDetailOutsideSurface_WhenTogglePressed_ExpandsAndCollapsesSnapshot()
        {
            var (roots, controller) = CreateMasterDetailSource();
            var grid = CreateMasterDetailGrid();
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 900, height: 420);

            try
            {
                grid.ItemsSource = roots.Select(root => root.Item).ToArray();
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                grid.SetMasterDetailSource(
                    roots,
                    controller,
                    new[] { "ObjectName", "ObjectId", "GeometryType", "Status" },
                    masterDisplayColumnId: "Category",
                    detailDisplayColumnId: "ObjectName",
                    detailHeaderPlacementMode: GridMasterDetailHeaderPlacementMode.Outside);
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var firstRow = surfaceHost.CurrentSnapshot.Rows[0];
                Assert.That(surfaceHost.CurrentSnapshot.Rows.Count, Is.EqualTo(roots.Count));

                GridSurfaceTestHost.ClickPoint(surfaceHost, x: 10, y: firstRow.Bounds.Y + (firstRow.Bounds.Height / 2d));
                GridSurfaceTestHost.FlushDispatcher(grid);

                Assert.Multiple(() =>
                {
                    Assert.That(surfaceHost.CurrentSnapshot.Rows.Count, Is.GreaterThan(roots.Count));
                    Assert.That(surfaceHost.CurrentSnapshot.Rows.Any(row => row.HierarchyLevel > 0), Is.True);
                    Assert.That(surfaceHost.CurrentSnapshot.Cells.Any(cell => string.Equals(cell.DisplayText, "Object Name", StringComparison.Ordinal)), Is.True);
                    Assert.That(surfaceHost.CurrentSnapshot.Cells.Any(cell => cell.DisplayText.Contains("Building A1")), Is.True);
                });

                GridSurfaceTestHost.ClickPoint(surfaceHost, x: 10, y: firstRow.Bounds.Y + (firstRow.Bounds.Height / 2d));
                GridSurfaceTestHost.FlushDispatcher(grid);

                Assert.That(surfaceHost.CurrentSnapshot.Rows.Count, Is.EqualTo(roots.Count));
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void MasterDetailInsideSurface_InitialSnapshotKeepsMastersAndHierarchyChildrenMetadata()
        {
            var (roots, controller) = CreateMasterDetailSource();
            var grid = CreateMasterDetailGrid();
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 900, height: 420);

            try
            {
                grid.ItemsSource = roots.Select(root => root.Item).ToArray();
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                grid.SetMasterDetailSource(
                    roots,
                    controller,
                    new[] { "ObjectName", "ObjectId", "GeometryType", "Status" },
                    masterDisplayColumnId: "Category",
                    detailDisplayColumnId: "ObjectName",
                    detailHeaderPlacementMode: GridMasterDetailHeaderPlacementMode.Inside);
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);

                Assert.Multiple(() =>
                {
                    Assert.That(surfaceHost.CurrentSnapshot.Rows.Count, Is.EqualTo(roots.Count));
                    Assert.That(surfaceHost.CurrentSnapshot.Rows.All(row => row.HasHierarchyChildren), Is.True);
                    Assert.That(surfaceHost.CurrentSnapshot.Overlays.All(overlay => overlay.Kind != GridOverlayKind.Custom), Is.True);
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void MasterDetailInsideSurface_WhenTogglePressed_ShowsDetailOverlay()
        {
            var (roots, controller) = CreateMasterDetailSource();
            var grid = CreateMasterDetailGrid();
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 900, height: 420);

            try
            {
                grid.ItemsSource = roots.Select(root => root.Item).ToArray();
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                grid.SetMasterDetailSource(
                    roots,
                    controller,
                    new[] { "ObjectName", "ObjectId", "GeometryType", "Status" },
                    masterDisplayColumnId: "Category",
                    detailDisplayColumnId: "ObjectName",
                    detailHeaderPlacementMode: GridMasterDetailHeaderPlacementMode.Inside);
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var firstRow = surfaceHost.CurrentSnapshot.Rows[0];

                GridSurfaceTestHost.ClickPoint(surfaceHost, x: 10, y: firstRow.Bounds.Y + (firstRow.Bounds.Height / 2d));
                GridSurfaceTestHost.FlushDispatcher(grid);

                Assert.Multiple(() =>
                {
                    Assert.That(surfaceHost.CurrentSnapshot.Rows.Count, Is.GreaterThan(roots.Count));
                    Assert.That(surfaceHost.CurrentSnapshot.Overlays.Any(overlay => overlay.Kind == GridOverlayKind.Custom), Is.True);
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void MasterDetailInsideSurface_WhenDetailColumnsAreHidden_StillRendersDetailRows()
        {
            var (roots, controller) = CreateMasterDetailSource();
            var grid = CreateMasterDetailGrid();
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 900, height: 420);

            try
            {
                grid.ItemsSource = roots.Select(root => root.Item).ToArray();
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                grid.SetColumnVisibility("ObjectName", false);
                grid.SetColumnVisibility("ObjectId", false);
                grid.SetColumnVisibility("GeometryType", false);
                grid.SetColumnVisibility("Status", false);
                GridSurfaceTestHost.FlushDispatcher(grid);

                grid.SetMasterDetailSource(
                    roots,
                    controller,
                    new[] { "ObjectName", "ObjectId", "GeometryType", "Status" },
                    masterDisplayColumnId: "Category",
                    detailDisplayColumnId: "ObjectName",
                    detailHeaderPlacementMode: GridMasterDetailHeaderPlacementMode.Inside);
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var firstRow = surfaceHost.CurrentSnapshot.Rows[0];
                GridSurfaceTestHost.ClickPoint(surfaceHost, x: 10, y: firstRow.Bounds.Y + (firstRow.Bounds.Height / 2d));
                GridSurfaceTestHost.FlushDispatcher(grid);

                var detailPresenter = GridSurfaceTestHost.FindVisualChildren<GridMasterDetailPresenter>(surfaceHost).FirstOrDefault();
                var detailText = GridSurfaceTestHost.ReadVisibleText(detailPresenter);

                Assert.Multiple(() =>
                {
                    Assert.That(surfaceHost.CurrentSnapshot.Overlays.Any(overlay => overlay.Kind == GridOverlayKind.Custom), Is.True);
                    Assert.That(detailPresenter, Is.Not.Null);
                    Assert.That(detailText, Does.Contain("Object Name"));
                    Assert.That(detailText, Does.Contain("Building A1"));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void MasterDetailInsideSurface_WhenMasterHasManyDetailRows_KeepsDetailsHostHeightBounded()
        {
            var (roots, controller) = CreateMasterDetailSource(alphaDetailCount: 24);
            var grid = CreateMasterDetailGrid();
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 900, height: 420);

            try
            {
                grid.ItemsSource = roots.Select(root => root.Item).ToArray();
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                grid.SetMasterDetailSource(
                    roots,
                    controller,
                    new[] { "ObjectName", "ObjectId", "GeometryType", "Status" },
                    masterDisplayColumnId: "Category",
                    detailDisplayColumnId: "ObjectName",
                    detailHeaderPlacementMode: GridMasterDetailHeaderPlacementMode.Inside);
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var firstRow = surfaceHost.CurrentSnapshot.Rows[0];
                GridSurfaceTestHost.ClickPoint(surfaceHost, x: 10, y: firstRow.Bounds.Y + (firstRow.Bounds.Height / 2d));
                GridSurfaceTestHost.FlushDispatcher(grid);

                var rows = surfaceHost.CurrentSnapshot.Rows.ToArray();
                var detailsHostRow = rows.FirstOrDefault(row => row.IsDetailsHost);
                var followingMasterRow = rows.FirstOrDefault(row => row.RowKey == "master:category-beta");
                var detailPresenter = GridSurfaceTestHost.FindVisualChildren<GridMasterDetailPresenter>(surfaceHost).FirstOrDefault();

                Assert.Multiple(() =>
                {
                    Assert.That(detailsHostRow, Is.Not.Null);
                    Assert.That(detailsHostRow.Bounds.Height, Is.LessThanOrEqualTo(340d));
                    Assert.That(followingMasterRow, Is.Not.Null);
                    Assert.That(followingMasterRow.Bounds.Y, Is.LessThanOrEqualTo(380d));
                    Assert.That(detailPresenter, Is.Not.Null);
                    Assert.That(detailPresenter.ActualHeight, Is.LessThanOrEqualTo(340d));
                    Assert.That(GridSurfaceTestHost.ReadVisibleText(detailPresenter), Does.Contain("Building A1"));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void MasterDetailInsideSurface_WhenShowNbAndMultiSelectEnabled_RendersOptionsIndicatorAndMarkerColumnsInDetailPresenter()
        {
            var (roots, controller) = CreateMasterDetailSource();
            var grid = CreateMasterDetailGrid();
            grid.MultiSelect = true;
            grid.ShowNb = true;
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 900, height: 420);

            try
            {
                grid.ItemsSource = roots.Select(root => root.Item).ToArray();
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                grid.SetMasterDetailSource(
                    roots,
                    controller,
                    new[] { "ObjectName", "ObjectId", "GeometryType", "Status" },
                    masterDisplayColumnId: "Category",
                    detailDisplayColumnId: "ObjectName",
                    detailHeaderPlacementMode: GridMasterDetailHeaderPlacementMode.Inside);
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var firstRow = surfaceHost.CurrentSnapshot.Rows[0];
                GridSurfaceTestHost.ClickPoint(surfaceHost, x: 10, y: firstRow.Bounds.Y + (firstRow.Bounds.Height / 2d));
                GridSurfaceTestHost.FlushDispatcher(grid);

                var detailPresenter = GridSurfaceTestHost.FindVisualChildren<GridMasterDetailPresenter>(surfaceHost).FirstOrDefault();

                Assert.Multiple(() =>
                {
                    Assert.That(detailPresenter, Is.Not.Null);
                    Assert.That(GridSurfaceTestHost.FindVisualChildren<Button>(detailPresenter).Any(button => Equals(button.Content, "...")), Is.True);
                    Assert.That(GridSurfaceTestHost.FindVisualChildren<CheckBox>(detailPresenter).Any(), Is.True);
                    Assert.That(GridSurfaceTestHost.FindVisualChildren<TextBlock>(detailPresenter).Any(text => text.Text == "1"), Is.True);
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void MasterDetailOutsideSurface_WhenRowNumbersEnabled_NumbersOnlyDetailRecords()
        {
            var (roots, controller) = CreateMasterDetailSource();
            var grid = CreateMasterDetailGrid();
            grid.ShowNb = true;
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 900, height: 420);

            try
            {
                grid.ItemsSource = roots.Select(root => root.Item).ToArray();
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                grid.SetMasterDetailSource(
                    roots,
                    controller,
                    new[] { "ObjectName", "ObjectId", "GeometryType", "Status" },
                    masterDisplayColumnId: "Category",
                    detailDisplayColumnId: "ObjectName",
                    detailHeaderPlacementMode: GridMasterDetailHeaderPlacementMode.Outside);
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var firstRow = surfaceHost.CurrentSnapshot.Rows[0];
                GridSurfaceTestHost.ClickPoint(surfaceHost, x: 10, y: firstRow.Bounds.Y + (firstRow.Bounds.Height / 2d));
                GridSurfaceTestHost.FlushDispatcher(grid);

                var rowNumberHeaders = surfaceHost.CurrentSnapshot.Headers
                    .Where(header => header.Kind == GridHeaderKind.RowNumberHeader)
                    .ToArray();

                var masterNumberHeader = rowNumberHeaders.FirstOrDefault(header => string.Equals(header.HeaderKey, "hierarchy:category-alpha", StringComparison.OrdinalIgnoreCase));
                var firstDetailNumberHeader = rowNumberHeaders.FirstOrDefault(header => string.Equals(header.HeaderKey, "alpha-1", StringComparison.OrdinalIgnoreCase));
                var secondDetailNumberHeader = rowNumberHeaders.FirstOrDefault(header => string.Equals(header.HeaderKey, "alpha-2", StringComparison.OrdinalIgnoreCase));

                Assert.Multiple(() =>
                {
                    Assert.That(masterNumberHeader, Is.Not.Null);
                    Assert.That(masterNumberHeader.ShowRowNumber, Is.False, "Master rows should not be numbered like detail records.");
                    Assert.That(masterNumberHeader.RowNumberText, Is.Empty);
                    Assert.That(firstDetailNumberHeader, Is.Not.Null);
                    Assert.That(firstDetailNumberHeader.RowNumberText, Is.EqualTo("1"));
                    Assert.That(secondDetailNumberHeader, Is.Not.Null);
                    Assert.That(secondDetailNumberHeader.RowNumberText, Is.EqualTo("2"));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void MasterDetailInsideSurface_FilterEditorIsEnabledAndFiltersDetailRows()
        {
            var (roots, controller) = CreateMasterDetailSource();
            var grid = CreateMasterDetailGrid();
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 900, height: 420);

            try
            {
                grid.ItemsSource = roots.Select(root => root.Item).ToArray();
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                grid.SetMasterDetailSource(
                    roots,
                    controller,
                    new[] { "ObjectName", "ObjectId", "GeometryType", "Status" },
                    masterDisplayColumnId: "Category",
                    detailDisplayColumnId: "ObjectName",
                    detailHeaderPlacementMode: GridMasterDetailHeaderPlacementMode.Inside);
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var firstRow = surfaceHost.CurrentSnapshot.Rows[0];
                GridSurfaceTestHost.ClickPoint(surfaceHost, x: 10, y: firstRow.Bounds.Y + (firstRow.Bounds.Height / 2d));
                GridSurfaceTestHost.FlushDispatcher(grid);

                var detailPresenter = GridSurfaceTestHost.FindVisualChildren<GridMasterDetailPresenter>(surfaceHost).FirstOrDefault();
                var filterTextBox = GridSurfaceTestHost.FindElementByAutomationId<TextBox>(
                    detailPresenter,
                    "surface.master-detail.filter.category-alpha.ObjectName");
                var firstMaster = grid.RowsView.Cast<object>().OfType<GridMasterDetailMasterRowModel>().First();

                Assert.Multiple(() =>
                {
                    Assert.That(detailPresenter, Is.Not.Null);
                    Assert.That(filterTextBox, Is.Not.Null);
                    Assert.That(filterTextBox?.IsEnabled, Is.True);
                    Assert.That(filterTextBox?.IsReadOnly, Is.False);
                    Assert.That(firstMaster.DetailRows.Count, Is.EqualTo(2));
                });

                if (filterTextBox == null)
                {
                    return;
                }

                filterTextBox.Text = "Building A2";
                GridSurfaceTestHost.FlushDispatcher(grid);

                Assert.Multiple(() =>
                {
                    Assert.That(firstMaster.DetailRows.Count, Is.EqualTo(1));
                    Assert.That(Convert.ToString(firstMaster.DetailRows[0]["ObjectName"], System.Globalization.CultureInfo.CurrentCulture), Does.Contain("Building A2"));
                    Assert.That(GridSurfaceTestHost.ReadVisibleText(detailPresenter), Does.Contain("Building A2"));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void MasterDetailInsideSurface_FilterRefreshPreservesFocusAndAppliesFullText()
        {
            var (roots, controller) = CreateMasterDetailSource(alphaDetailCount: 12);
            var grid = CreateMasterDetailGrid();
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 900, height: 420);

            try
            {
                grid.ItemsSource = roots.Select(root => root.Item).ToArray();
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                grid.SetMasterDetailSource(
                    roots,
                    controller,
                    new[] { "ObjectName", "ObjectId", "GeometryType", "Status" },
                    masterDisplayColumnId: "Category",
                    detailDisplayColumnId: "ObjectName",
                    detailHeaderPlacementMode: GridMasterDetailHeaderPlacementMode.Inside);
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var firstRow = surfaceHost.CurrentSnapshot.Rows[0];
                GridSurfaceTestHost.ClickPoint(surfaceHost, x: 10, y: firstRow.Bounds.Y + (firstRow.Bounds.Height / 2d));
                GridSurfaceTestHost.FlushDispatcher(grid);

                var detailPresenter = GridSurfaceTestHost.FindVisualChildren<GridMasterDetailPresenter>(surfaceHost).FirstOrDefault();
                var filterTextBox = GridSurfaceTestHost.FindElementByAutomationId<TextBox>(
                    detailPresenter,
                    "surface.master-detail.filter.category-alpha.ObjectName");
                var firstMaster = grid.RowsView.Cast<object>().OfType<GridMasterDetailMasterRowModel>().First();
                const string expectedFilter = "Building A12";

                Assert.Multiple(() =>
                {
                    Assert.That(detailPresenter, Is.Not.Null);
                    Assert.That(filterTextBox, Is.Not.Null);
                    Assert.That(firstMaster.DetailRows.Count, Is.EqualTo(12));
                });

                if (filterTextBox == null)
                {
                    return;
                }

                FocusElement(filterTextBox);
                filterTextBox.Dispatcher.Invoke(() => filterTextBox.Text = "Building");
                GridSurfaceTestHost.FlushDispatcher(grid);

                var focusedTextBox = Keyboard.FocusedElement as TextBox;
                var refreshedFilterTextBox = GridSurfaceTestHost.FindElementByAutomationId<TextBox>(
                    detailPresenter,
                    "surface.master-detail.filter.category-alpha.ObjectName");

                Assert.Multiple(() =>
                {
                    Assert.That(focusedTextBox, Is.Not.Null, "Filter should keep keyboard focus after the detail overlay refresh.");
                    Assert.That(AutomationProperties.GetAutomationId(focusedTextBox), Is.EqualTo("surface.master-detail.filter.category-alpha.ObjectName"));
                    Assert.That(refreshedFilterTextBox, Is.Not.Null);
                    Assert.That(refreshedFilterTextBox.Text, Is.EqualTo("Building"));
                });

                focusedTextBox = Keyboard.FocusedElement as TextBox;
                Assert.That(focusedTextBox, Is.Not.Null);
                focusedTextBox.Dispatcher.Invoke(() => focusedTextBox.Text = expectedFilter);
                GridSurfaceTestHost.FlushDispatcher(grid);

                focusedTextBox = Keyboard.FocusedElement as TextBox;
                refreshedFilterTextBox = GridSurfaceTestHost.FindElementByAutomationId<TextBox>(
                    detailPresenter,
                    "surface.master-detail.filter.category-alpha.ObjectName");

                Assert.Multiple(() =>
                {
                    Assert.That(focusedTextBox, Is.Not.Null, "Filter should keep keyboard focus while typing.");
                    Assert.That(AutomationProperties.GetAutomationId(focusedTextBox), Is.EqualTo("surface.master-detail.filter.category-alpha.ObjectName"));
                    Assert.That(refreshedFilterTextBox, Is.Not.Null);
                    Assert.That(refreshedFilterTextBox.Text, Is.EqualTo(expectedFilter));
                    Assert.That(firstMaster.DetailRows.Count, Is.EqualTo(1));
                    Assert.That(Convert.ToString(firstMaster.DetailRows[0]["ObjectName"], System.Globalization.CultureInfo.CurrentCulture), Is.EqualTo("Building A12"));
                    Assert.That(GridSurfaceTestHost.ReadVisibleText(detailPresenter), Does.Contain("Building A12"));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void MasterDetailInsideSurface_FilterRefreshKeepsFilterAndOptionsControlsAlive()
        {
            var (roots, controller) = CreateMasterDetailSource(alphaDetailCount: 12);
            var grid = CreateMasterDetailGrid();
            grid.ShowNb = true;
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 900, height: 420);

            try
            {
                grid.ItemsSource = roots.Select(root => root.Item).ToArray();
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                grid.SetMasterDetailSource(
                    roots,
                    controller,
                    new[] { "ObjectName", "ObjectId", "GeometryType", "Status" },
                    masterDisplayColumnId: "Category",
                    detailDisplayColumnId: "ObjectName",
                    detailHeaderPlacementMode: GridMasterDetailHeaderPlacementMode.Inside);
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var firstRow = surfaceHost.CurrentSnapshot.Rows[0];
                GridSurfaceTestHost.ClickPoint(surfaceHost, x: 10, y: firstRow.Bounds.Y + (firstRow.Bounds.Height / 2d));
                GridSurfaceTestHost.FlushDispatcher(grid);

                var detailPresenter = GridSurfaceTestHost.FindVisualChildren<GridMasterDetailPresenter>(surfaceHost).FirstOrDefault();
                var filterTextBox = GridSurfaceTestHost.FindElementByAutomationId<TextBox>(
                    detailPresenter,
                    "surface.master-detail.filter.category-alpha.ObjectName");
                var optionsButton = GridSurfaceTestHost.FindElementByAutomationId<Button>(
                    detailPresenter,
                    "surface.master-detail.options.category-alpha");

                Assert.Multiple(() =>
                {
                    Assert.That(detailPresenter, Is.Not.Null);
                    Assert.That(filterTextBox, Is.Not.Null);
                    Assert.That(optionsButton, Is.Not.Null);
                });

                if (filterTextBox == null || optionsButton == null)
                {
                    return;
                }

                FocusElement(filterTextBox);
                filterTextBox.Dispatcher.Invoke(() => filterTextBox.Text = "Building A12");
                GridSurfaceTestHost.FlushDispatcher(grid);

                var refreshedFilterTextBox = GridSurfaceTestHost.FindElementByAutomationId<TextBox>(
                    detailPresenter,
                    "surface.master-detail.filter.category-alpha.ObjectName");
                var refreshedOptionsButton = GridSurfaceTestHost.FindElementByAutomationId<Button>(
                    detailPresenter,
                    "surface.master-detail.options.category-alpha");

                Assert.Multiple(() =>
                {
                    Assert.That(refreshedFilterTextBox, Is.SameAs(filterTextBox), "Filter refresh should keep the same editor instance alive instead of rebuilding the whole detail header.");
                    Assert.That(refreshedOptionsButton, Is.SameAs(optionsButton), "Filter refresh should keep the same options button alive instead of replacing it mid-interaction.");
                    Assert.That(Keyboard.FocusedElement, Is.SameAs(filterTextBox));
                    Assert.That(refreshedFilterTextBox.Text, Is.EqualTo("Building A12"));
                });

                refreshedOptionsButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, refreshedOptionsButton));
                GridSurfaceTestHost.FlushDispatcher(grid);

                Assert.That(refreshedOptionsButton.ContextMenu?.IsOpen, Is.True, "Detail options button should remain clickable after filter refresh.");
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void MasterDetailInsideSurface_FilterRowShowsMainGridFilterIcon()
        {
            var (roots, controller) = CreateMasterDetailSource();
            var grid = CreateMasterDetailGrid();
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 900, height: 420);

            try
            {
                grid.ItemsSource = roots.Select(root => root.Item).ToArray();
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                grid.SetMasterDetailSource(
                    roots,
                    controller,
                    new[] { "ObjectName", "ObjectId", "GeometryType", "Status" },
                    masterDisplayColumnId: "Category",
                    detailDisplayColumnId: "ObjectName",
                    detailHeaderPlacementMode: GridMasterDetailHeaderPlacementMode.Inside);
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var firstRow = surfaceHost.CurrentSnapshot.Rows[0];
                GridSurfaceTestHost.ClickPoint(surfaceHost, x: 10, y: firstRow.Bounds.Y + (firstRow.Bounds.Height / 2d));
                GridSurfaceTestHost.FlushDispatcher(grid);

                var detailPresenter = GridSurfaceTestHost.FindVisualChildren<GridMasterDetailPresenter>(surfaceHost).FirstOrDefault();
                var filterIcon = GridSurfaceTestHost.FindElementByAutomationId<FrameworkElement>(
                    detailPresenter,
                    "surface.master-detail.filter-icon.category-alpha");

                Assert.That(filterIcon, Is.Not.Null, "Detail filter row should expose the same corner filter icon affordance as the main grid.");
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void MasterDetailInsideSurface_WhenOnlyRowStateColumnRemains_HeaderFilterAndDataStayAligned()
        {
            var (roots, controller) = CreateMasterDetailSource();
            var grid = CreateMasterDetailGrid();
            grid.MultiSelect = false;
            grid.ShowNb = false;
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 900, height: 420);

            try
            {
                grid.ItemsSource = roots.Select(root => root.Item).ToArray();
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                grid.SetMasterDetailSource(
                    roots,
                    controller,
                    new[] { "ObjectName", "ObjectId", "GeometryType", "Status" },
                    masterDisplayColumnId: "Category",
                    detailDisplayColumnId: "ObjectName",
                    detailHeaderPlacementMode: GridMasterDetailHeaderPlacementMode.Inside);
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var firstRow = surfaceHost.CurrentSnapshot.Rows[0];
                GridSurfaceTestHost.ClickPoint(surfaceHost, x: 10, y: firstRow.Bounds.Y + (firstRow.Bounds.Height / 2d));
                GridSurfaceTestHost.FlushDispatcher(grid);

                var detailPresenter = GridSurfaceTestHost.FindVisualChildren<GridMasterDetailPresenter>(surfaceHost).FirstOrDefault();
                var headerCell = GridSurfaceTestHost.FindElementByAutomationId<FrameworkElement>(
                    detailPresenter,
                    "surface.master-detail.header.category-alpha.ObjectName");
                var filterCell = GridSurfaceTestHost.FindElementByAutomationId<FrameworkElement>(
                    detailPresenter,
                    "surface.master-detail.filter-cell.category-alpha.ObjectName");
                var firstDetailCell = GridSurfaceTestHost.FindElementByAutomationId<FrameworkElement>(
                    detailPresenter,
                    "surface.master-detail.detail-cell.category-alpha.1.ObjectName");

                Assert.Multiple(() =>
                {
                    Assert.That(detailPresenter, Is.Not.Null);
                    Assert.That(headerCell, Is.Not.Null);
                    Assert.That(filterCell, Is.Not.Null);
                    Assert.That(firstDetailCell, Is.Not.Null);
                });

                if (detailPresenter == null || headerCell == null || filterCell == null || firstDetailCell == null)
                {
                    return;
                }

                var headerX = headerCell.TransformToAncestor(detailPresenter).Transform(new Point(0d, 0d)).X;
                var filterX = filterCell.TransformToAncestor(detailPresenter).Transform(new Point(0d, 0d)).X;
                var dataX = firstDetailCell.TransformToAncestor(detailPresenter).Transform(new Point(0d, 0d)).X;

                Assert.Multiple(() =>
                {
                    Assert.That(filterX, Is.EqualTo(headerX).Within(0.1d));
                    Assert.That(dataX, Is.EqualTo(headerX).Within(0.1d));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void MasterDetailInsideSurface_WhenOnlyRowStateColumnRemains_ShowsActiveDetailRecord()
        {
            var (roots, controller) = CreateMasterDetailSource();
            var grid = CreateMasterDetailGrid();
            grid.MultiSelect = false;
            grid.ShowNb = false;
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 900, height: 420);

            try
            {
                grid.ItemsSource = roots.Select(root => root.Item).ToArray();
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                grid.SetMasterDetailSource(
                    roots,
                    controller,
                    new[] { "ObjectName", "ObjectId", "GeometryType", "Status" },
                    masterDisplayColumnId: "Category",
                    detailDisplayColumnId: "ObjectName",
                    detailHeaderPlacementMode: GridMasterDetailHeaderPlacementMode.Inside);
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var firstRow = surfaceHost.CurrentSnapshot.Rows[0];
                GridSurfaceTestHost.ClickPoint(surfaceHost, x: 10, y: firstRow.Bounds.Y + (firstRow.Bounds.Height / 2d));
                GridSurfaceTestHost.FlushDispatcher(grid);

                var detailPresenter = GridSurfaceTestHost.FindVisualChildren<GridMasterDetailPresenter>(surfaceHost).FirstOrDefault();
                var firstDetailCell = GridSurfaceTestHost.FindElementByAutomationId<Border>(
                    detailPresenter,
                    "surface.master-detail.detail-cell.category-alpha.1.ObjectName");
                var secondDetailCell = GridSurfaceTestHost.FindElementByAutomationId<Border>(
                    detailPresenter,
                    "surface.master-detail.detail-cell.category-alpha.2.ObjectName");
                var firstIndicatorCell = GridSurfaceTestHost.FindElementByAutomationId<Border>(
                    detailPresenter,
                    "surface.master-detail.row-indicator.category-alpha.1");

                Assert.Multiple(() =>
                {
                    Assert.That(detailPresenter, Is.Not.Null);
                    Assert.That(firstDetailCell, Is.Not.Null);
                    Assert.That(secondDetailCell, Is.Not.Null);
                    Assert.That(firstIndicatorCell, Is.Not.Null);
                });

                if (detailPresenter == null || firstDetailCell == null || secondDetailCell == null || firstIndicatorCell == null)
                {
                    return;
                }

                Assert.Multiple(() =>
                {
                    Assert.That(
                        ReadBrushColor(firstDetailCell.Background),
                        Is.EqualTo(ReadBrushColor(grid.TryFindResource("PgCurrentRowBackgroundBrush"))));
                    Assert.That(
                        ReadBrushColor(secondDetailCell.Background),
                        Is.EqualTo(ReadBrushColor(grid.TryFindResource("PgMasterDetailDetailBackgroundBrush"))));
                    Assert.That(
                        ReadBrushColor(firstIndicatorCell.Background),
                        Is.EqualTo(ReadBrushColor(grid.TryFindResource("PgRowIndicatorColumnCurrentBackgroundBrush"))));
                    Assert.That(GridSurfaceTestHost.FindVisualChildren<Polygon>(firstIndicatorCell).Any(), Is.True);
                });

                ClickElement(secondDetailCell);
                GridSurfaceTestHost.FlushDispatcher(grid);

                var refreshedFirstDetailCell = GridSurfaceTestHost.FindElementByAutomationId<Border>(
                    detailPresenter,
                    "surface.master-detail.detail-cell.category-alpha.1.ObjectName");
                var refreshedSecondDetailCell = GridSurfaceTestHost.FindElementByAutomationId<Border>(
                    detailPresenter,
                    "surface.master-detail.detail-cell.category-alpha.2.ObjectName");
                var refreshedSecondIndicatorCell = GridSurfaceTestHost.FindElementByAutomationId<Border>(
                    detailPresenter,
                    "surface.master-detail.row-indicator.category-alpha.2");

                Assert.Multiple(() =>
                {
                    Assert.That(
                        ReadBrushColor(refreshedFirstDetailCell?.Background),
                        Is.EqualTo(ReadBrushColor(grid.TryFindResource("PgMasterDetailDetailBackgroundBrush"))));
                    Assert.That(
                        ReadBrushColor(refreshedSecondDetailCell?.Background),
                        Is.EqualTo(ReadBrushColor(grid.TryFindResource("PgCurrentRowBackgroundBrush"))));
                    Assert.That(
                        ReadBrushColor(refreshedSecondIndicatorCell?.Background),
                        Is.EqualTo(ReadBrushColor(grid.TryFindResource("PgRowIndicatorColumnCurrentBackgroundBrush"))));
                    Assert.That(GridSurfaceTestHost.FindVisualChildren<Polygon>(refreshedSecondIndicatorCell).Any(), Is.True);
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void MasterDetailInsideSurface_HorizontalScrollKeepsHeadersFiltersAndRowsAligned()
        {
            var (roots, controller) = CreateMasterDetailSource(alphaDetailCount: 12);
            var grid = CreateMasterDetailGrid();
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 520, height: 420);

            try
            {
                grid.ItemsSource = roots.Select(root => root.Item).ToArray();
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                grid.SetMasterDetailSource(
                    roots,
                    controller,
                    new[] { "ObjectName", "ObjectId", "GeometryType", "Status" },
                    masterDisplayColumnId: "Category",
                    detailDisplayColumnId: "ObjectName",
                    detailHeaderPlacementMode: GridMasterDetailHeaderPlacementMode.Inside);
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var firstRow = surfaceHost.CurrentSnapshot.Rows[0];
                GridSurfaceTestHost.ClickPoint(surfaceHost, x: 10, y: firstRow.Bounds.Y + (firstRow.Bounds.Height / 2d));
                GridSurfaceTestHost.FlushDispatcher(grid);

                var detailPresenter = GridSurfaceTestHost.FindVisualChildren<GridMasterDetailPresenter>(surfaceHost).FirstOrDefault();
                var headerScroll = GridSurfaceTestHost.FindElementByAutomationId<ScrollViewer>(
                    detailPresenter,
                    "surface.master-detail.header-scroll.category-alpha");
                var filterScroll = GridSurfaceTestHost.FindElementByAutomationId<ScrollViewer>(
                    detailPresenter,
                    "surface.master-detail.filter-scroll.category-alpha");
                var rowsScroll = GridSurfaceTestHost.FindElementByAutomationId<ScrollViewer>(
                    detailPresenter,
                    "surface.master-detail.rows-scroll.category-alpha");

                Assert.Multiple(() =>
                {
                    Assert.That(detailPresenter, Is.Not.Null);
                    Assert.That(headerScroll, Is.Not.Null);
                    Assert.That(filterScroll, Is.Not.Null);
                    Assert.That(rowsScroll, Is.Not.Null);
                });

                if (headerScroll == null || filterScroll == null || rowsScroll == null)
                {
                    return;
                }

                rowsScroll.Dispatcher.Invoke(() => rowsScroll.ScrollToHorizontalOffset(120d));
                GridSurfaceTestHost.FlushDispatcher(grid);

                Assert.Multiple(() =>
                {
                    Assert.That(rowsScroll.HorizontalOffset, Is.GreaterThan(0d));
                    Assert.That(headerScroll.HorizontalOffset, Is.EqualTo(rowsScroll.HorizontalOffset).Within(0.1d));
                    Assert.That(filterScroll.HorizontalOffset, Is.EqualTo(rowsScroll.HorizontalOffset).Within(0.1d));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void MasterDetailInsideSurface_DetailDisplayColumnDoesNotAddHierarchyIndent()
        {
            var (roots, controller) = CreateMasterDetailSource();
            var grid = CreateMasterDetailGrid();
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 900, height: 420);

            try
            {
                grid.ItemsSource = roots.Select(root => root.Item).ToArray();
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                grid.SetMasterDetailSource(
                    roots,
                    controller,
                    new[] { "ObjectName", "ObjectId", "GeometryType", "Status" },
                    masterDisplayColumnId: "Category",
                    detailDisplayColumnId: "ObjectName",
                    detailHeaderPlacementMode: GridMasterDetailHeaderPlacementMode.Inside);
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var firstRow = surfaceHost.CurrentSnapshot.Rows[0];
                GridSurfaceTestHost.ClickPoint(surfaceHost, x: 10, y: firstRow.Bounds.Y + (firstRow.Bounds.Height / 2d));
                GridSurfaceTestHost.FlushDispatcher(grid);

                var firstMaster = grid.RowsView.Cast<object>().OfType<GridMasterDetailMasterRowModel>().First();
                var firstDetailValue = Convert.ToString(firstMaster.DetailRows[0]["ObjectName"], System.Globalization.CultureInfo.CurrentCulture);

                Assert.Multiple(() =>
                {
                    Assert.That(firstMaster.DetailRows.Count, Is.GreaterThan(0));
                    Assert.That(firstDetailValue, Is.EqualTo("Building A1"), "Inside master-detail overlay should align detail data with the column, without extra hierarchy indent.");
                    Assert.That(firstDetailValue?.StartsWith("\u2003", StringComparison.Ordinal), Is.False);
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void MasterDetailInsideSurface_OptionsMenuUsesEnabledGridOptionsEntries()
        {
            var (roots, controller) = CreateMasterDetailSource();
            var grid = CreateMasterDetailGrid();
            grid.ShowNb = true;
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 900, height: 420);

            try
            {
                grid.ItemsSource = roots.Select(root => root.Item).ToArray();
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                grid.SetMasterDetailSource(
                    roots,
                    controller,
                    new[] { "ObjectName", "ObjectId", "GeometryType", "Status" },
                    masterDisplayColumnId: "Category",
                    detailDisplayColumnId: "ObjectName",
                    detailHeaderPlacementMode: GridMasterDetailHeaderPlacementMode.Inside);
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var firstRow = surfaceHost.CurrentSnapshot.Rows[0];
                GridSurfaceTestHost.ClickPoint(surfaceHost, x: 10, y: firstRow.Bounds.Y + (firstRow.Bounds.Height / 2d));
                GridSurfaceTestHost.FlushDispatcher(grid);

                var detailPresenter = GridSurfaceTestHost.FindVisualChildren<GridMasterDetailPresenter>(surfaceHost).FirstOrDefault();
                var optionsButton = GridSurfaceTestHost.FindElementByAutomationId<Button>(
                    detailPresenter,
                    "surface.master-detail.options.category-alpha");

                Assert.That(optionsButton, Is.Not.Null);

                optionsButton.RaiseEvent(new System.Windows.RoutedEventArgs(Button.ClickEvent, optionsButton));
                GridSurfaceTestHost.FlushDispatcher(grid);

                var contextMenu = optionsButton.ContextMenu;
                var menuItems = contextMenu?.Items.OfType<MenuItem>().ToArray() ?? Array.Empty<MenuItem>();
                var showRowNumbersItem = menuItems.FirstOrDefault(item => string.Equals(
                    Convert.ToString(item.Header, System.Globalization.CultureInfo.CurrentCulture),
                    grid.GetText(GridTextKeys.OptionsShowRowNumbers),
                    StringComparison.CurrentCulture));

                Assert.Multiple(() =>
                {
                    Assert.That(contextMenu, Is.Not.Null);
                    Assert.That(contextMenu?.IsOpen, Is.True);
                    Assert.That(menuItems.Length, Is.GreaterThan(0));
                    Assert.That(menuItems.All(item => item.IsEnabled), Is.True, "Master-detail options should expose active menu commands, not disabled info rows.");
                    Assert.That(showRowNumbersItem, Is.Not.Null);
                });

                if (showRowNumbersItem == null)
                {
                    return;
                }

                showRowNumbersItem.RaiseEvent(new System.Windows.RoutedEventArgs(MenuItem.ClickEvent, showRowNumbersItem));
                GridSurfaceTestHost.FlushDispatcher(grid);

                Assert.That(grid.ShowNb, Is.False, "Master-detail options menu should be interactive.");
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void MasterDetailInsideSurface_OptionsButtonUsesTransparentOverlayChromeInsteadOfMainGridHeaderFill()
        {
            var (roots, controller) = CreateMasterDetailSource();
            var grid = CreateMasterDetailGrid();
            grid.IsNightMode = true;
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 900, height: 420);

            try
            {
                grid.ItemsSource = roots.Select(root => root.Item).ToArray();
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                grid.SetMasterDetailSource(
                    roots,
                    controller,
                    new[] { "ObjectName", "ObjectId", "GeometryType", "Status" },
                    masterDisplayColumnId: "Category",
                    detailDisplayColumnId: "ObjectName",
                    detailHeaderPlacementMode: GridMasterDetailHeaderPlacementMode.Inside);
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var firstRow = surfaceHost.CurrentSnapshot.Rows[0];
                GridSurfaceTestHost.ClickPoint(surfaceHost, x: 10, y: firstRow.Bounds.Y + (firstRow.Bounds.Height / 2d));
                GridSurfaceTestHost.FlushDispatcher(grid);

                var detailPresenter = GridSurfaceTestHost.FindVisualChildren<GridMasterDetailPresenter>(surfaceHost).FirstOrDefault();
                var optionsButton = GridSurfaceTestHost.FindElementByAutomationId<Button>(
                    detailPresenter,
                    "surface.master-detail.options.category-alpha");

                Assert.Multiple(() =>
                {
                    Assert.That(detailPresenter, Is.Not.Null);
                    Assert.That(optionsButton, Is.Not.Null);
                    Assert.That(optionsButton?.IsEnabled, Is.True);
                    Assert.That(optionsButton?.Opacity, Is.EqualTo(1d).Within(0.01d));
                    Assert.That((optionsButton?.Background as SolidColorBrush)?.Color, Is.EqualTo(Colors.Transparent));
                    Assert.That((optionsButton?.BorderBrush as SolidColorBrush)?.Color, Is.EqualTo(Colors.Transparent));
                });
            }
            finally
            {
                window.Close();
            }
        }

        private static (IReadOnlyList<GridHierarchyNode<object>> Roots, GridHierarchyController<object> Controller) CreateMasterDetailSource(int alphaDetailCount = 2)
        {
            var rootAlpha = new GridHierarchyNode<object>(
                "category-alpha",
                new Dictionary<string, object>
                {
                    ["Id"] = "category-alpha",
                    ["Category"] = "Category Alpha",
                    ["Description"] = "Buildings",
                },
                canExpand: true);
            var rootBeta = new GridHierarchyNode<object>(
                "category-beta",
                new Dictionary<string, object>
                {
                    ["Id"] = "category-beta",
                    ["Category"] = "Category Beta",
                    ["Description"] = "Roads",
                },
                canExpand: true);

            var alphaChildren = Enumerable.Range(1, Math.Max(1, alphaDetailCount))
                .Select(index => new GridHierarchyNode<object>(
                    "alpha-" + index,
                    new Dictionary<string, object>
                    {
                        ["Id"] = "alpha-" + index,
                        ["ObjectName"] = "Building A" + index,
                        ["ObjectId"] = "A" + index,
                        ["GeometryType"] = "Polygon",
                        ["Status"] = "Ready",
                    },
                    canExpand: false,
                    parentId: rootAlpha.PathId))
                .ToArray();

            var provider = new TestHierarchyProvider(new Dictionary<string, IReadOnlyList<GridHierarchyNode<object>>>(StringComparer.OrdinalIgnoreCase)
            {
                [rootAlpha.PathId] = alphaChildren,
                [rootBeta.PathId] = new[]
                {
                    new GridHierarchyNode<object>(
                        "beta-1",
                        new Dictionary<string, object>
                        {
                            ["Id"] = "beta-1",
                            ["ObjectName"] = "Road B1",
                            ["ObjectId"] = "B1",
                            ["GeometryType"] = "Line",
                            ["Status"] = "Draft",
                        },
                        canExpand: false,
                        parentId: rootBeta.PathId),
                },
            });

            return (new[] { rootAlpha, rootBeta }, new GridHierarchyController<object>(provider));
        }

        private static WpfGrid CreateMasterDetailGrid()
        {
            return new WpfGrid
            {
                Width = 880,
                Height = 360,
                IsGridReadOnly = true,
                LanguageDirectory = global::PhialeGrid.Wpf.Tests.GridTestRepositoryPaths.GridLanguagesDirectory,
                Columns = new[]
                {
                    new GridColumnDefinition("Category", "Category", width: 180, displayIndex: 0),
                    new GridColumnDefinition("Description", "Description", width: 160, displayIndex: 1),
                    new GridColumnDefinition("ObjectName", "Object Name", width: 180, displayIndex: 2),
                    new GridColumnDefinition("ObjectId", "Object Id", width: 120, displayIndex: 3),
                    new GridColumnDefinition("GeometryType", "Geometry Type", width: 140, displayIndex: 4),
                    new GridColumnDefinition("Status", "Status", width: 120, displayIndex: 5),
                },
                ItemsSource = Array.Empty<object>(),
            };
        }

        private static void FocusElement(UIElement element)
        {
            Assert.That(element, Is.Not.Null);
            element.Dispatcher.Invoke(() =>
            {
                element.Focus();
                Keyboard.Focus(element);
            });
        }

        private static void ClickElement(UIElement element)
        {
            Assert.That(element, Is.Not.Null);
            element.Dispatcher.Invoke(() =>
            {
                var mouseDown = new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left)
                {
                    RoutedEvent = UIElement.MouseLeftButtonDownEvent,
                    Source = element,
                };
                element.RaiseEvent(mouseDown);

                var mouseUp = new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left)
                {
                    RoutedEvent = UIElement.MouseLeftButtonUpEvent,
                    Source = element,
                };
                element.RaiseEvent(mouseUp);
            });
        }

        private static Color ReadBrushColor(object brush)
        {
            Assert.That(brush, Is.TypeOf<SolidColorBrush>());
            return ((SolidColorBrush)brush).Color;
        }

        private sealed class TestHierarchyProvider : IGridHierarchyProvider<object>
        {
            private readonly IReadOnlyDictionary<string, IReadOnlyList<GridHierarchyNode<object>>> _childrenByPath;

            public TestHierarchyProvider(IReadOnlyDictionary<string, IReadOnlyList<GridHierarchyNode<object>>> childrenByPath)
            {
                _childrenByPath = childrenByPath ?? throw new ArgumentNullException(nameof(childrenByPath));
            }

            public Task<IReadOnlyList<GridHierarchyNode<object>>> LoadChildrenAsync(GridHierarchyNode<object> parent, CancellationToken cancellationToken)
            {
                if (parent == null || !_childrenByPath.TryGetValue(parent.PathId, out var children))
                {
                    return Task.FromResult((IReadOnlyList<GridHierarchyNode<object>>)Array.Empty<GridHierarchyNode<object>>());
                }

                return Task.FromResult(children);
            }
        }
    }
}

