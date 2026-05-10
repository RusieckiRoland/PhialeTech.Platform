using System;
using System.Linq;
using System.Threading;
using System.Windows.Data;
using NUnit.Framework;
using PhialeTech.Components.Shared.Model;
using PhialeTech.Components.Shared.ViewModels;
using PhialeTech.PhialeGrid.Wpf.Controls;
using PhialeGrid.Core.Query;
using PhialeGrid.Wpf.Tests.Surface;
using WpfGrid = PhialeTech.PhialeGrid.Wpf.Controls.PhialeGrid;

namespace PhialeGrid.Wpf.Tests.Grouping
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class GridGroupingBehaviorTests
    {
        [Test]
        public void DemoBinding_WhenGroupingScenarioIsSelected_StartsCollapsedWithOnlyTopLevelGroupHeadersVisible()
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

                var visibleRows = grid.RowsView.Cast<object>().Take(16).ToArray();
                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);

                Assert.Multiple(() =>
                {
                    Assert.That(viewModel.GridGroups.Count, Is.EqualTo(1));
                    Assert.That(grid.HasGroups, Is.True);
                    Assert.That(grid.GroupChips.Count, Is.EqualTo(1));
                    Assert.That(visibleRows.Length, Is.GreaterThan(0));
                    Assert.That(visibleRows.All(row => row is GridGroupHeaderRowModel), Is.True);
                    Assert.That(visibleRows.OfType<GridGroupHeaderRowModel>().All(row => row.GroupColumnId == "Category"), Is.True);
                    Assert.That(surfaceHost.CurrentSnapshot.Rows.All(row => row.IsGroupHeader), Is.True);
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void ExpandAllAndCollapseAllGroups_ToggleBetweenHeaderOnlyAndMixedRows()
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

                var initialRows = grid.RowsView.Cast<object>().Take(12).ToArray();
                Assert.That(initialRows.All(row => row is GridGroupHeaderRowModel), Is.True);

                grid.ExpandAllGroups();
                GridSurfaceTestHost.FlushDispatcher(grid);

                var expandedRows = grid.RowsView.Cast<object>().Take(20).ToArray();
                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                Assert.Multiple(() =>
                {
                    Assert.That(expandedRows.Any(row => row is GridDataRowModel), Is.True);
                    Assert.That(expandedRows.OfType<GridGroupHeaderRowModel>().Any(row => row.IsExpanded), Is.True);
                    Assert.That(surfaceHost.CurrentSnapshot.Rows.Any(row => !row.IsGroupHeader), Is.True);
                });

                grid.CollapseAllGroups();
                GridSurfaceTestHost.FlushDispatcher(grid);

                var collapsedRows = grid.RowsView.Cast<object>().Take(12).ToArray();
                Assert.Multiple(() =>
                {
                    Assert.That(collapsedRows.Length, Is.GreaterThan(0));
                    Assert.That(collapsedRows.All(row => row is GridGroupHeaderRowModel), Is.True);
                    Assert.That(collapsedRows.OfType<GridGroupHeaderRowModel>().All(row => row.IsExpanded == false), Is.True);
                    Assert.That(surfaceHost.CurrentSnapshot.Rows.All(row => row.IsGroupHeader), Is.True);
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void GroupChips_PreserveDirectionGlyphAndHierarchyConnectorState()
        {
            var grid = new WpfGrid
            {
                LanguageDirectory = global::PhialeGrid.Wpf.Tests.GridTestRepositoryPaths.GridLanguagesDirectory,
            };

            grid.SetGroups(new[]
            {
                new GridGroupDescriptor("GeometryType", GridSortDirection.Ascending),
                new GridGroupDescriptor("Municipality", GridSortDirection.Descending),
                new GridGroupDescriptor("District", GridSortDirection.Ascending),
            });

            Assert.Multiple(() =>
            {
                Assert.That(grid.GroupChips.Count, Is.EqualTo(3));
                Assert.That(grid.GroupChips[0].DirectionGlyph, Is.EqualTo("↑"));
                Assert.That(grid.GroupChips[0].HasFollowingGroup, Is.True);
                Assert.That(grid.GroupChips[1].DirectionGlyph, Is.EqualTo("↓"));
                Assert.That(grid.GroupChips[1].HasFollowingGroup, Is.True);
                Assert.That(grid.GroupChips[2].DirectionGlyph, Is.EqualTo("↑"));
                Assert.That(grid.GroupChips[2].HasFollowingGroup, Is.False);
            });
        }

        [Test]
        public void FilteringWhileGrouped_ReducesVisibleDatasetAndKeepsGroupedRowsConsistent()
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

                grid.ExpandAllGroups();
                GridSurfaceTestHost.FlushDispatcher(grid);

                var municipalityColumn = grid.VisibleColumns.FirstOrDefault(column => column.ColumnId == "Municipality");
                Assert.That(municipalityColumn, Is.Not.Null);

                municipalityColumn.FilterText = "Wroclaw";
                GridSurfaceTestHost.FlushDispatcher(grid);

                var rows = grid.RowsView.Cast<object>().Take(40).ToArray();
                var dataRows = rows.OfType<GridDataRowModel>().ToArray();
                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var visibleRowIds = dataRows
                    .Select(row => ((DemoGisRecordViewModel)row.SourceRow).Id)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                Assert.Multiple(() =>
                {
                    Assert.That(rows.Length, Is.GreaterThan(0));
                    Assert.That(rows.Any(row => row is GridGroupHeaderRowModel), Is.True);
                    Assert.That(dataRows.Length, Is.GreaterThan(0));
                    Assert.That(dataRows.Select(row => (DemoGisRecordViewModel)row.SourceRow).All(row =>
                        row.Municipality.IndexOf("Wroclaw", StringComparison.OrdinalIgnoreCase) >= 0), Is.True);
                    Assert.That(surfaceHost.CurrentSnapshot.Cells
                        .Where(cell => string.Equals(cell.ColumnKey, "Municipality", StringComparison.Ordinal) &&
                                       visibleRowIds.Contains(cell.RowKey))
                        .All(cell => cell.DisplayText.IndexOf("Wroclaw", StringComparison.OrdinalIgnoreCase) >= 0), Is.True);
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
            BindingOperations.SetBinding(grid, WpfGrid.IsGridReadOnlyProperty, new Binding(nameof(DemoShellViewModel.IsGridReadOnly)));
            return grid;
        }
    }
}

