using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Data;
using NUnit.Framework;
using PhialeGrid.Core.Rendering;
using PhialeTech.Components.Shared.ViewModels;
using PhialeTech.PhialeGrid.Wpf.Controls;
using PhialeGrid.Wpf.Tests.Surface;
using WpfGrid = PhialeTech.PhialeGrid.Wpf.Controls.PhialeGrid;

namespace PhialeGrid.Wpf.Tests.Layout
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class GridColumnLayoutBehaviorTests
    {
        [Test]
        public void ColumnVisibility_ShowHideAndShowAllColumns_UpdateVisibleColumnsAndSurfaceSnapshot()
        {
            var viewModel = new DemoShellViewModel("Wpf");
            var grid = CreateBoundDemoGrid(viewModel);
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 1200, height: 700);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                viewModel.SelectExample("column-layout");
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                Assert.That(grid.VisibleColumns.Any(column => column.ColumnId == "Owner"), Is.True);

                grid.SetColumnVisibility("Owner", false);
                GridSurfaceTestHost.FlushDispatcher(grid);

                Assert.Multiple(() =>
                {
                    Assert.That(grid.VisibleColumns.Any(column => column.ColumnId == "Owner"), Is.False);
                    Assert.That(surfaceHost.CurrentSnapshot.Columns.Any(column => column.ColumnKey == "Owner"), Is.False);
                });

                grid.ShowAllColumns();
                GridSurfaceTestHost.FlushDispatcher(grid);

                Assert.Multiple(() =>
                {
                    Assert.That(grid.VisibleColumns.Any(column => column.ColumnId == "Owner"), Is.True);
                    Assert.That(surfaceHost.CurrentSnapshot.Columns.Any(column => column.ColumnKey == "Owner"), Is.True);
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void FreezeAndUnfreezeColumns_UpdateFrozenColumnStateInSurfaceSnapshot()
        {
            var viewModel = new DemoShellViewModel("Wpf");
            var grid = CreateBoundDemoGrid(viewModel);
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 1200, height: 700);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                viewModel.SelectExample("column-layout");
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);

                grid.SetColumnFrozen("ObjectName", true);
                GridSurfaceTestHost.FlushDispatcher(grid);

                Assert.That(surfaceHost.CurrentSnapshot.Columns.Single(column => column.ColumnKey == "ObjectName").IsFrozen, Is.True);

                grid.SetColumnFrozen("ObjectName", false);
                GridSurfaceTestHost.FlushDispatcher(grid);

                Assert.That(surfaceHost.CurrentSnapshot.Columns.Single(column => column.ColumnKey == "ObjectName").IsFrozen, Is.False);
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void ReorderingColumns_UpdatesVisibleColumnOrderAndSurvivesStateRoundtrip()
        {
            var viewModel = new DemoShellViewModel("Wpf");
            var grid = CreateBoundDemoGrid(viewModel);
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 1200, height: 700);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                viewModel.SelectExample("column-layout");
                GridSurfaceTestHost.FlushDispatcher(grid);

                while (!string.Equals(grid.VisibleColumns.First().ColumnId, "Owner", System.StringComparison.Ordinal))
                {
                    grid.MoveColumnLeft("Owner");
                }
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                Assert.That(grid.VisibleColumns.First().ColumnId, Is.EqualTo("Owner"));

                var savedState = grid.SaveState();
                grid.ResetState();
                GridSurfaceTestHost.FlushDispatcher(grid);
                Assert.That(grid.VisibleColumns.First().ColumnId, Is.Not.EqualTo("Owner"));

                grid.LoadState(savedState);
                GridSurfaceTestHost.FlushDispatcher(grid);

                Assert.Multiple(() =>
                {
                    Assert.That(grid.VisibleColumns.First().ColumnId, Is.EqualTo("Owner"));
                    Assert.That(surfaceHost.CurrentSnapshot.Columns.First().ColumnKey, Is.EqualTo("Owner"));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void SaveStateAndLoadState_ShouldRoundtripRowIndicatorAndSelectionOptions()
        {
            var viewModel = new DemoShellViewModel("Wpf");
            var grid = CreateBoundDemoGrid(viewModel);
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 1200, height: 700);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                viewModel.SelectExample("grouping");
                GridSurfaceTestHost.FlushDispatcher(grid);

                grid.SelectCurrentRow = false;
                grid.MultiSelect = true;
                grid.ShowNb = true;
                grid.RowNumberingMode = GridRowNumberingMode.WithinGroup;
                GridSurfaceTestHost.FlushDispatcher(grid);

                var savedState = grid.SaveState();

                grid.SelectCurrentRow = true;
                grid.MultiSelect = false;
                grid.ShowNb = false;
                grid.RowNumberingMode = GridRowNumberingMode.Global;
                GridSurfaceTestHost.FlushDispatcher(grid);

                grid.LoadState(savedState);
                GridSurfaceTestHost.FlushDispatcher(grid);

                Assert.Multiple(() =>
                {
                    Assert.That(grid.SelectCurrentRow, Is.False);
                    Assert.That(grid.MultiSelect, Is.True);
                    Assert.That(grid.ShowNb, Is.True);
                    Assert.That(grid.RowNumberingMode, Is.EqualTo(GridRowNumberingMode.WithinGroup));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void AutoFitVisibleColumns_RecomputesWidthFromSurfaceContent()
        {
            var viewModel = new DemoShellViewModel("Wpf");
            var grid = CreateBoundDemoGrid(viewModel);
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 1200, height: 700);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                viewModel.SelectExample("column-layout");
                GridSurfaceTestHost.FlushDispatcher(grid);

                var objectNameColumn = grid.VisibleColumns.First(column => column.ColumnId == "ObjectName");
                objectNameColumn.UpdateWidth(120d);
                GridSurfaceTestHost.FlushDispatcher(grid);

                grid.AutoFitVisibleColumns();
                GridSurfaceTestHost.FlushDispatcher(grid);

                var bindingModel = grid.VisibleColumns.First(column => column.ColumnId == "ObjectName");
                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                Assert.That(bindingModel.Width, Is.GreaterThan(120d));
                Assert.That(surfaceHost.CurrentSnapshot.Columns.Single(column => column.ColumnKey == "ObjectName").Width, Is.EqualTo(bindingModel.Width).Within(1d));
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void ColumnContextMenu_InClassicMode_ExposesAutoFitFreezeAndUnfreezeActions()
        {
            var viewModel = new DemoShellViewModel("Wpf");
            var grid = CreateBoundDemoGrid(viewModel);
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 1200, height: 700);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                viewModel.SelectExample("column-layout");
                grid.InteractionMode = GridInteractionMode.Classic;
                GridSurfaceTestHost.FlushDispatcher(grid);

                var buildMethod = typeof(WpfGrid).GetMethod("BuildColumnContextMenu", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(buildMethod, Is.Not.Null);

                var objectNameColumn = grid.VisibleColumns.First(column => column.ColumnId == "ObjectName");
                var placementTarget = new Button { DataContext = objectNameColumn };
                var contextMenu = (ContextMenu)buildMethod.Invoke(grid, new object[] { objectNameColumn, placementTarget });
                var headers = contextMenu.Items
                    .OfType<MenuItem>()
                    .Select(item => Convert.ToString(item.Header, System.Globalization.CultureInfo.CurrentCulture) ?? string.Empty)
                    .ToArray();

                Assert.Multiple(() =>
                {
                    Assert.That(headers, Does.Contain(grid.AutoFitColumnText));
                    Assert.That(headers, Does.Contain(grid.FreezeColumnText));
                });

                grid.SetColumnFrozen("ObjectName", true);
                GridSurfaceTestHost.FlushDispatcher(grid);

                objectNameColumn = grid.VisibleColumns.First(column => column.ColumnId == "ObjectName");
                placementTarget = new Button { DataContext = objectNameColumn };
                contextMenu = (ContextMenu)buildMethod.Invoke(grid, new object[] { objectNameColumn, placementTarget });
                headers = contextMenu.Items
                    .OfType<MenuItem>()
                    .Select(item => Convert.ToString(item.Header, System.Globalization.CultureInfo.CurrentCulture) ?? string.Empty)
                    .ToArray();

                Assert.That(headers, Does.Contain(grid.UnfreezeColumnText));
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void TouchLayoutActions_MoveResizeAndAutoFitColumns_UpdateSurfaceSnapshot()
        {
            var viewModel = new DemoShellViewModel("Wpf");
            var grid = CreateBoundDemoGrid(viewModel);
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 1200, height: 700);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                viewModel.SelectExample("column-layout");
                grid.InteractionMode = GridInteractionMode.Touch;
                GridSurfaceTestHost.FlushDispatcher(grid);

                var initialFirstColumnId = grid.VisibleColumns.First().ColumnId;
                var objectNameBefore = grid.VisibleColumns.First(column => column.ColumnId == "ObjectName").Width;

                grid.MoveColumnRight(initialFirstColumnId);
                GridSurfaceTestHost.FlushDispatcher(grid);
                var firstAfterMoveRight = grid.VisibleColumns.First().ColumnId;

                grid.MoveColumnLeft(initialFirstColumnId);
                GridSurfaceTestHost.FlushDispatcher(grid);
                var firstAfterMoveLeft = grid.VisibleColumns.First().ColumnId;

                grid.WidenColumn("ObjectName");
                GridSurfaceTestHost.FlushDispatcher(grid);
                var widenedWidth = grid.VisibleColumns.First(column => column.ColumnId == "ObjectName").Width;

                grid.NarrowColumn("ObjectName");
                GridSurfaceTestHost.FlushDispatcher(grid);
                var narrowedWidth = grid.VisibleColumns.First(column => column.ColumnId == "ObjectName").Width;

                grid.AutoFitColumn("ObjectName");
                GridSurfaceTestHost.FlushDispatcher(grid);
                var autoFitWidth = grid.VisibleColumns.First(column => column.ColumnId == "ObjectName").Width;

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var snapshotColumn = surfaceHost.CurrentSnapshot.Columns.Single(column => column.ColumnKey == "ObjectName");

                Assert.Multiple(() =>
                {
                    Assert.That(firstAfterMoveRight, Is.Not.EqualTo(initialFirstColumnId));
                    Assert.That(firstAfterMoveLeft, Is.EqualTo(initialFirstColumnId));
                    Assert.That(widenedWidth, Is.GreaterThan(objectNameBefore));
                    Assert.That(narrowedWidth, Is.LessThan(widenedWidth));
                    Assert.That(narrowedWidth, Is.GreaterThanOrEqualTo(120d));
                    Assert.That(autoFitWidth, Is.GreaterThan(120d));
                    Assert.That(snapshotColumn.Width, Is.EqualTo(autoFitWidth).Within(1d));
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
                LanguageDirectory = global::PhialeGrid.Wpf.Tests.GridTestRepositoryPaths.GridLanguagesDirectory,
                Width = 1200,
                Height = 700,
                DataContext = viewModel,
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
    }
}
