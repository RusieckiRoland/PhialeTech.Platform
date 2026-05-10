using System;
using System.Linq;
using NUnit.Framework;
using PhialeGrid.Core.Columns;
using PhialeGrid.Core.Query;
using PhialeGrid.Core.Regions;
using PhialeGrid.Core.State;
using PhialeGrid.Core.Summaries;

namespace PhialeGis.Library.Tests.Grid
{
    public class GridStateCodecTests
    {
        [Test]
        public void RegionKindNumericValues_RemainStableForEncodedStatePayloads()
        {
            Assert.Multiple(() =>
            {
                Assert.That((int)GridRegionKind.CoreGridSurface, Is.EqualTo(0));
                Assert.That((int)GridRegionKind.TopCommandRegion, Is.EqualTo(1));
                Assert.That((int)GridRegionKind.GroupingRegion, Is.EqualTo(2));
                Assert.That((int)GridRegionKind.SummaryBottomRegion, Is.EqualTo(3));
                Assert.That((int)GridRegionKind.SideToolRegion, Is.EqualTo(4));
                Assert.That((int)GridRegionKind.ChangePanelRegion, Is.EqualTo(5));
                Assert.That((int)GridRegionKind.ValidationPanelRegion, Is.EqualTo(6));
            });
        }

        [Test]
        public void EncodeDecode_PreservesState()
        {
            var columns = new[]
            {
                new GridColumnDefinition("Id", "Id", width: 120, isFrozen: true, displayIndex: 0),
                new GridColumnDefinition("Name", "Name", width: 200, isVisible: false, displayIndex: 1),
            };
            var snapshot = new GridStateSnapshot(
                new GridLayoutSnapshot(columns),
                new[] { new GridSortDescriptor("Name", GridSortDirection.Descending) },
                new GridFilterGroup(new[] { new GridFilterDescriptor("Name", GridFilterOperator.Contains, "Al") }, GridLogicalOperator.And),
                new[] { new GridGroupDescriptor("Id") },
                new[] { new GridSummaryDescriptor("Id", GridSummaryType.Count) },
                new GridRegionLayoutSnapshot(new[]
                {
                    new GridRegionLayoutState(GridRegionKind.CoreGridSurface, GridRegionState.Open, null, false),
                    new GridRegionLayoutState(GridRegionKind.TopCommandRegion, GridRegionState.Open, 44d, false),
                    new GridRegionLayoutState(GridRegionKind.GroupingRegion, GridRegionState.Open, 56d, false),
                    new GridRegionLayoutState(GridRegionKind.SummaryBottomRegion, GridRegionState.Open, 56d, false),
                    new GridRegionLayoutState(GridRegionKind.SummaryDesignerRegion, GridRegionState.Closed, 320d, false),
                    new GridRegionLayoutState(GridRegionKind.SideToolRegion, GridRegionState.Open, 320d, true),
                    new GridRegionLayoutState(GridRegionKind.ChangePanelRegion, GridRegionState.Closed, 320d, false),
                    new GridRegionLayoutState(GridRegionKind.ValidationPanelRegion, GridRegionState.Closed, 320d, false),
                }));

            var encoded = GridStateCodec.Encode(snapshot);
            var decoded = GridStateCodec.Decode(encoded, columns);

            Assert.That(decoded.Layout.Columns.Single(c => c.Id == "Name").IsVisible, Is.False);
            Assert.That(decoded.Layout.Columns.Single(c => c.Id == "Id").IsFrozen, Is.True);
            Assert.That(decoded.Sorts.Count, Is.EqualTo(1));
            Assert.That(decoded.Groups.Count, Is.EqualTo(1));
            Assert.That(decoded.Summaries.Count, Is.EqualTo(1));
            Assert.That(decoded.RegionLayout.Regions.Single(region => region.RegionKind == GridRegionKind.SideToolRegion).Size, Is.EqualTo(320d));
            Assert.That(decoded.RegionLayout.Regions.Single(region => region.RegionKind == GridRegionKind.SideToolRegion).IsActive, Is.True);
        }

        [Test]
        public void InMemoryStore_SaveLoadWorks()
        {
            var store = new InMemoryGridStateStore();
            store.Save("gridA", "state-1");

            Assert.That(store.Load("gridA"), Is.EqualTo("state-1"));
            Assert.That(store.Load("missing"), Is.Null);
        }

        [Test]
        public void Decode_FailsFastForDuplicateRegionEntriesInNewPayload()
        {
            var columns = new[]
            {
                new GridColumnDefinition("Id", "Id", width: 120, isFrozen: true, displayIndex: 0),
            };
            var snapshot = new GridStateSnapshot(
                new GridLayoutSnapshot(columns),
                Array.Empty<GridSortDescriptor>(),
                GridFilterGroup.EmptyAnd(),
                Array.Empty<GridGroupDescriptor>(),
                Array.Empty<GridSummaryDescriptor>(),
                new GridRegionLayoutSnapshot(new[]
                {
                    new GridRegionLayoutState(GridRegionKind.CoreGridSurface, GridRegionState.Open, null, false),
                    new GridRegionLayoutState(GridRegionKind.TopCommandRegion, GridRegionState.Open, 44d, false),
                    new GridRegionLayoutState(GridRegionKind.GroupingRegion, GridRegionState.Open, 56d, false),
                    new GridRegionLayoutState(GridRegionKind.SummaryBottomRegion, GridRegionState.Open, 56d, false),
                    new GridRegionLayoutState(GridRegionKind.SummaryDesignerRegion, GridRegionState.Closed, 320d, false),
                    new GridRegionLayoutState(GridRegionKind.SideToolRegion, GridRegionState.Open, 320d, false),
                    new GridRegionLayoutState(GridRegionKind.ChangePanelRegion, GridRegionState.Closed, 320d, false),
                    new GridRegionLayoutState(GridRegionKind.ValidationPanelRegion, GridRegionState.Closed, 320d, false),
                }));
            var encoded = GridStateCodec.Encode(snapshot);
            var parts = encoded.Split('|').ToArray();
            parts[6] = "0,0,~,0;0,0,~,0";
            var duplicatePayload = string.Join("|", parts);

            Assert.That(
                () => GridStateCodec.Decode(duplicatePayload, columns),
                Throws.ArgumentException.With.Message.Contains("Duplicate"));
        }
    }
}

