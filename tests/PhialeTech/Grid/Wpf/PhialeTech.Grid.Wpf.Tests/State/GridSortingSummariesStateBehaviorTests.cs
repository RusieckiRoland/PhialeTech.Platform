using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows.Data;
using NUnit.Framework;
using PhialeGrid.Core.Query;
using PhialeTech.Components.Shared.Model;
using PhialeTech.Components.Shared.ViewModels;
using PhialeTech.PhialeGrid.Wpf.Controls;
using PhialeGrid.Wpf.Tests.Surface;
using WpfGrid = PhialeTech.PhialeGrid.Wpf.Controls.PhialeGrid;

namespace PhialeGrid.Wpf.Tests.State
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class GridSortingSummariesStateBehaviorTests
    {
        [Test]
        public void DemoBinding_WhenSortingScenarioIsSelected_DataRowsFollowConfiguredMultiSort()
        {
            var viewModel = new DemoShellViewModel("Wpf");
            var grid = CreateBoundDemoGrid(viewModel);
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 1200, height: 700);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                viewModel.SelectExample("sorting");
                GridSurfaceTestHost.FlushDispatcher(grid);

                var rows = grid.RowsView.Cast<object>().OfType<GridDataRowModel>()
                    .Take(80)
                    .Select(row => (DemoGisRecordViewModel)row.SourceRow)
                    .ToArray();

                Assert.Multiple(() =>
                {
                    Assert.That(viewModel.GridSorts.Count, Is.EqualTo(2));
                    Assert.That(rows.Length, Is.GreaterThan(5));
                });

                for (var index = 1; index < rows.Length; index++)
                {
                    var previous = rows[index - 1];
                    var current = rows[index];

                    var categoryCompare = string.Compare(previous.Category, current.Category, StringComparison.OrdinalIgnoreCase);
                    Assert.That(categoryCompare, Is.LessThanOrEqualTo(0), $"Category sort broke at row {index}.");
                    if (categoryCompare == 0)
                    {
                        Assert.That(previous.LastInspection >= current.LastInspection, Is.True, $"LastInspection descending sort broke at row {index}.");
                    }
                }

                var sortGlyphColumns = grid.VisibleColumns.Where(column => !string.IsNullOrWhiteSpace(column.SortGlyph)).Select(column => column.ColumnId).ToArray();
                CollectionAssert.AreEquivalent(new[] { "Category", "LastInspection" }, sortGlyphColumns);
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void DemoBinding_WhenSummariesScenarioIsFiltered_SummaryItemsTrackFilteredRows()
        {
            var viewModel = new DemoShellViewModel("Wpf");
            var grid = CreateBoundDemoGrid(viewModel);
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 1200, height: 700);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                viewModel.SelectExample("summaries");
                GridSurfaceTestHost.FlushDispatcher(grid);

                var municipalityColumn = grid.VisibleColumns.FirstOrDefault(column => column.ColumnId == "Municipality");
                Assert.That(municipalityColumn, Is.Not.Null);
                municipalityColumn.FilterText = "Wroclaw";
                GridSurfaceTestHost.FlushDispatcher(grid);

                var filteredRows = grid.RowsView.Cast<object>().OfType<GridDataRowModel>()
                    .Select(row => (DemoGisRecordViewModel)row.SourceRow)
                    .ToArray();

                Assert.That(filteredRows.Length, Is.GreaterThan(0));

                var expectedValues = new[]
                {
                    filteredRows.Sum(row => row.AreaSquareMeters).ToString("N2", CultureInfo.CurrentCulture),
                    filteredRows.Sum(row => row.LengthMeters).ToString("N2", CultureInfo.CurrentCulture),
                    filteredRows.Length.ToString(CultureInfo.CurrentCulture),
                };

                var actualValues = grid.SummaryItems.Select(item => item.ValueText).ToArray();

                Assert.Multiple(() =>
                {
                    Assert.That(grid.HasSummaries, Is.True);
                    Assert.That(grid.SummaryItems.Count, Is.EqualTo(3));
                    CollectionAssert.AreEqual(expectedValues, actualValues);
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void SaveLoadAndResetState_RestoreLiveGroupingFilteringAndLayout()
        {
            var viewModel = new DemoShellViewModel("Wpf");
            var grid = CreateBoundDemoGrid(viewModel);
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 1200, height: 700);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                viewModel.SelectExample("state-persistence");
                GridSurfaceTestHost.FlushDispatcher(grid);

                grid.SetGroups(new[] { new GridGroupDescriptor("Category", GridSortDirection.Ascending) });
                grid.SetColumnVisibility("Owner", false);
                grid.SetColumnFrozen("ObjectName", true);
                grid.VisibleColumns.First(column => column.ColumnId == "Municipality").FilterText = "Wroclaw";
                grid.ApplyGlobalSearch("Wroclaw");
                GridSurfaceTestHost.FlushDispatcher(grid);

                var savedState = grid.SaveState();
                GridSurfaceTestHost.FlushDispatcher(grid);

                Assert.That(savedState, Is.Not.Null.And.Not.Empty);
                Assert.That(grid.SavedStatePreview, Is.EqualTo(savedState));

                grid.ResetState();
                GridSurfaceTestHost.FlushDispatcher(grid);

                Assert.Multiple(() =>
                {
                    Assert.That(grid.HasGroups, Is.False);
                    Assert.That(grid.GlobalSearchText, Is.Empty);
                    Assert.That(grid.VisibleColumns.Any(column => column.ColumnId == "Owner"), Is.True);
                    Assert.That(grid.VisibleColumns.First(column => column.ColumnId == "Municipality").FilterText, Is.Empty);
                });

                grid.LoadState(savedState);
                GridSurfaceTestHost.FlushDispatcher(grid);

                var visibleRows = grid.RowsView.Cast<object>().Take(20).ToArray();
                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);

                Assert.Multiple(() =>
                {
                    Assert.That(grid.HasGroups, Is.True);
                    Assert.That(grid.GroupChips.Count, Is.EqualTo(1));
                    Assert.That(grid.GroupChips[0].ColumnId, Is.EqualTo("Category"));
                    Assert.That(grid.GlobalSearchText, Is.EqualTo("Wroclaw"));
                    Assert.That(grid.VisibleColumns.Any(column => column.ColumnId == "Owner"), Is.False);
                    Assert.That(grid.VisibleColumns.First(column => column.ColumnId == "Municipality").FilterText, Is.EqualTo("Wroclaw"));
                    Assert.That(surfaceHost.CurrentSnapshot.Columns.Any(column => column.IsFrozen), Is.True);
                    Assert.That(grid.HasSummaries, Is.True);
                    Assert.That(visibleRows.Length, Is.GreaterThan(0));
                    Assert.That(visibleRows[0], Is.InstanceOf<GridGroupHeaderRowModel>());
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
                LanguageDirectory = GetLanguageDirectory(),
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

        private static string GetLanguageDirectory()
        {
            return global::PhialeGrid.Wpf.Tests.GridTestRepositoryPaths.GridLanguagesDirectory;
        }
    }
}



