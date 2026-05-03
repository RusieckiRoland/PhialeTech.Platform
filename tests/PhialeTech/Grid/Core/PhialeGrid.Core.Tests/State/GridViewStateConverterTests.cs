using System;
using System.Linq;
using NUnit.Framework;
using PhialeGrid.Core.Columns;
using PhialeGrid.Core.Query;
using PhialeGrid.Core.Regions;
using PhialeGrid.Core.State;
using PhialeGrid.Core.Summaries;

namespace PhialeGrid.Core.Tests.State
{
    [TestFixture]
    public class GridViewStateConverterTests
    {
        [Test]
        public void FromSnapshot_ShouldCreateVersionedViewState()
        {
            var snapshot = BuildSnapshot();

            var state = GridViewStateConverter.FromSnapshot(snapshot);

            Assert.Multiple(() =>
            {
                Assert.That(state.Version, Is.EqualTo(4));
                Assert.That(state.Columns.Count, Is.EqualTo(2));
                Assert.That(state.Columns.Single(column => column.ColumnId == "ObjectName").Width, Is.EqualTo(280d));
                Assert.That(state.Filters.Filters.Single().ValueText, Is.EqualTo("Krakow"));
                Assert.That(state.Groups.Single().ColumnId, Is.EqualTo("Category"));
                Assert.That(state.Summaries.Single().Type, Is.EqualTo(GridSummaryType.Count));
                Assert.That(state.RegionLayout.Single(region => region.RegionKind == GridRegionKind.GroupingRegion).State, Is.EqualTo(GridRegionState.Collapsed));
                Assert.That(state.RegionLayout.Single(region => region.RegionKind == GridRegionKind.SideToolRegion).PlacementOverride, Is.EqualTo(GridRegionPlacement.Left));
                Assert.That(state.GlobalSearchText, Is.EqualTo("owner:alpha"));
            });
        }

        [Test]
        public void ToSnapshot_ShouldRestoreLayoutDescriptorsAndSearchText()
        {
            var baselineColumns = new[]
            {
                new GridColumnDefinition("Category", "Category", width: 150d, displayIndex: 0, valueType: typeof(string)),
                new GridColumnDefinition("ObjectName", "Object name", width: 220d, displayIndex: 1, valueType: typeof(string)),
            };

            var state = new GridViewState
            {
                Version = 1,
                GlobalSearchText = "hydrant",
                Filters = new GridViewFilterGroupState
                {
                    LogicalOperator = GridLogicalOperator.Or,
                    Filters =
                    {
                        new GridViewFilterState
                        {
                            ColumnId = "Category",
                            Operator = GridFilterOperator.Contains,
                            HasValue = true,
                            ValueText = "Water",
                        },
                    },
                },
            };
            state.Columns.Add(new GridViewColumnState
            {
                ColumnId = "ObjectName",
                DisplayIndex = 0,
                Width = 310d,
                IsVisible = true,
                IsFrozen = true,
            });
            state.Columns.Add(new GridViewColumnState
            {
                ColumnId = "Category",
                DisplayIndex = 1,
                Width = 160d,
                IsVisible = false,
                IsFrozen = false,
            });
            state.Sorts.Add(new GridViewSortState { ColumnId = "ObjectName", Direction = GridSortDirection.Descending });
            state.Groups.Add(new GridViewGroupState { ColumnId = "Category", Direction = GridSortDirection.Ascending });
            state.Summaries.Add(new GridViewSummaryState { ColumnId = "Category", Type = GridSummaryType.Count });
            state.RegionLayout.Add(new GridViewRegionState { RegionKind = GridRegionKind.CoreGridSurface, State = GridRegionState.Open, Size = null, IsActive = false });
            state.RegionLayout.Add(new GridViewRegionState { RegionKind = GridRegionKind.TopCommandRegion, State = GridRegionState.Open, Size = 44d, IsActive = false });
            state.RegionLayout.Add(new GridViewRegionState { RegionKind = GridRegionKind.GroupingRegion, State = GridRegionState.Open, Size = 56d, IsActive = false });
            state.RegionLayout.Add(new GridViewRegionState { RegionKind = GridRegionKind.SummaryBottomRegion, State = GridRegionState.Open, Size = 56d, IsActive = false });
            state.RegionLayout.Add(new GridViewRegionState { RegionKind = GridRegionKind.SideToolRegion, State = GridRegionState.Open, Size = 340d, IsActive = true, PlacementOverride = GridRegionPlacement.Left });

            var snapshot = GridViewStateConverter.ToSnapshot(state, baselineColumns);

            Assert.Multiple(() =>
            {
                Assert.That(snapshot.Layout.Columns.First().Id, Is.EqualTo("ObjectName"));
                Assert.That(snapshot.Layout.Columns.First().Width, Is.EqualTo(310d));
                Assert.That(snapshot.Layout.Columns.First().IsFrozen, Is.True);
                Assert.That(snapshot.Layout.Columns.Single(column => column.Id == "Category").IsVisible, Is.False);
                Assert.That(snapshot.Sorts.Single().Direction, Is.EqualTo(GridSortDirection.Descending));
                Assert.That(snapshot.Filters.LogicalOperator, Is.EqualTo(GridLogicalOperator.Or));
                Assert.That(snapshot.Filters.Filters.Single().Value, Is.EqualTo("Water"));
                Assert.That(snapshot.Groups.Single().ColumnId, Is.EqualTo("Category"));
                Assert.That(snapshot.Summaries.Single().Type, Is.EqualTo(GridSummaryType.Count));
                Assert.That(snapshot.RegionLayout.Regions.Single(region => region.RegionKind == GridRegionKind.SideToolRegion).State, Is.EqualTo(GridRegionState.Open));
                Assert.That(snapshot.RegionLayout.Regions.Single(region => region.RegionKind == GridRegionKind.SideToolRegion).IsActive, Is.True);
                Assert.That(snapshot.RegionLayout.Regions.Single(region => region.RegionKind == GridRegionKind.SideToolRegion).Size, Is.EqualTo(340d));
                Assert.That(snapshot.RegionLayout.Regions.Single(region => region.RegionKind == GridRegionKind.SideToolRegion).PlacementOverride, Is.EqualTo(GridRegionPlacement.Left));
                Assert.That(snapshot.GlobalSearchText, Is.EqualTo("hydrant"));
            });
        }

        private static GridStateSnapshot BuildSnapshot()
        {
            var layout = new GridLayoutSnapshot(new[]
            {
                new GridColumnDefinition("Category", "Category", width: 150d, displayIndex: 0, valueType: typeof(string)),
                new GridColumnDefinition("ObjectName", "Object name", width: 280d, displayIndex: 1, valueType: typeof(string), isFrozen: true),
            });

            return new GridStateSnapshot(
                layout,
                new[] { new GridSortDescriptor("ObjectName", GridSortDirection.Descending) },
                new GridFilterGroup(
                    new[] { new GridFilterDescriptor("Category", GridFilterOperator.Contains, "Krakow") },
                    GridLogicalOperator.And),
                new[] { new GridGroupDescriptor("Category", GridSortDirection.Ascending) },
                new[] { new GridSummaryDescriptor("Category", GridSummaryType.Count) },
                new GridRegionLayoutSnapshot(new[]
                {
                    new GridRegionLayoutState(GridRegionKind.CoreGridSurface, GridRegionState.Open, null, false ),
                    new GridRegionLayoutState(GridRegionKind.TopCommandRegion, GridRegionState.Open, 44d, false ),
                    new GridRegionLayoutState(GridRegionKind.GroupingRegion, GridRegionState.Collapsed, 120d, false ),
                    new GridRegionLayoutState(GridRegionKind.SummaryBottomRegion, GridRegionState.Open, 56d, false ),
                    new GridRegionLayoutState(GridRegionKind.SideToolRegion, GridRegionState.Closed, 320d, false, GridRegionPlacement.Left),
                }),
                "owner:alpha");
        }
    }
}



