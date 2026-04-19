using NUnit.Framework;
using PhialeGrid.Core.HitTesting;
using PhialeGrid.Core.Surface;

namespace PhialeGrid.Core.Tests.HitTesting
{
    [TestFixture]
    public class GridHitTestingServiceTests
    {
        [Test]
        public void HitTest_PrefersOverlayWithHighestDrawPriority()
        {
            var sut = new GridHitTestingService();
            var snapshot = CreateSnapshot(overlays: new[]
            {
                new GridOverlaySurfaceItem("high", GridOverlayKind.Custom)
                {
                    Bounds = new GridBounds(40, 30, 100, 20),
                    DrawPriority = 20,
                    IsInteractive = true,
                    SnapshotRevision = 1,
                },
                new GridOverlaySurfaceItem("low", GridOverlayKind.Custom)
                {
                    Bounds = new GridBounds(40, 30, 100, 20),
                    DrawPriority = 1,
                    IsInteractive = true,
                    SnapshotRevision = 1,
                },
            });

            var result = sut.HitTest(60, 40, snapshot);

            Assert.Multiple(() =>
            {
                Assert.That(result.TargetKind, Is.EqualTo(GridHitTargetKind.Overlay));
                Assert.That(result.TargetKey, Is.EqualTo("high"));
            });
        }

        [Test]
        public void HitTest_OnColumnHeaderRightEdge_ReturnsColumnResizeHandle()
        {
            var sut = new GridHitTestingService();
            var snapshot = CreateSnapshot();

            var result = sut.HitTest(138, 12, snapshot);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.TargetKind, Is.EqualTo(GridHitTargetKind.ColumnResizeHandle));
            Assert.That(result.HeaderKey, Is.EqualTo("col-1"));
            Assert.That(result.Zone, Is.EqualTo(GridHitZone.RightEdge));
        }

        [Test]
        public void HitTest_OnRowHeaderDetailsToggle_ReturnsDetails()
        {
            var sut = new GridHitTestingService();
            var snapshot = CreateSnapshot(hasDetails: true);

            var result = sut.HitTest(8, 42, snapshot);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.TargetKind, Is.EqualTo(GridHitTargetKind.Details));
            Assert.That(result.RowKey, Is.EqualTo("row-1"));
        }

        [Test]
        public void HitTest_OnRowHeaderHierarchyToggle_ReturnsHierarchyToggle()
        {
            var sut = new GridHitTestingService();
            var snapshot = CreateSnapshot(hasHierarchyChildren: true, hierarchyLevel: 1);

            var result = sut.HitTest(24, 42, snapshot);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.TargetKind, Is.EqualTo(GridHitTargetKind.HierarchyToggle));
            Assert.That(result.RowKey, Is.EqualTo("row-1"));
        }

        [Test]
        public void HitTest_OnRowSelectionCheckbox_ReturnsSelectionCheckbox()
        {
            var sut = new GridHitTestingService();
            var snapshot = CreateSnapshot(showRowIndicator: true, showSelectionCheckbox: true, rowIndicatorWidth: 14d, rowMarkerWidth: 18d, selectionCheckboxWidth: 18d);

            var result = sut.HitTest(22, 42, snapshot);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.TargetKind, Is.EqualTo(GridHitTargetKind.SelectionCheckbox));
            Assert.That(result.RowKey, Is.EqualTo("row-1"));
        }

        [Test]
        public void HitTest_OnLeftSideOfRowMarkerColumn_WhenCheckboxIsVisible_ReturnsSelectionCheckbox()
        {
            var sut = new GridHitTestingService();
            var snapshot = CreateSnapshot(showRowIndicator: true, showSelectionCheckbox: true, rowIndicatorWidth: 14d, rowMarkerWidth: 18d, selectionCheckboxWidth: 18d);

            var result = sut.HitTest(15, 42, snapshot);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.TargetKind, Is.EqualTo(GridHitTargetKind.SelectionCheckbox));
            Assert.That(result.RowKey, Is.EqualTo("row-1"));
        }

        [Test]
        public void HitTest_WhenCurrentCellOverlayTouchesRowHeader_PrefersSelectionCheckboxInRowMarkerColumn()
        {
            var sut = new GridHitTestingService();
            var snapshot = CreateSnapshot(
                showRowIndicator: true,
                showSelectionCheckbox: true,
                rowIndicatorWidth: 14d,
                rowMarkerWidth: 18d,
                selectionCheckboxWidth: 18d,
                currentCell: new GridCurrentCellMarker("row-1", "col-1"),
                overlays: new[]
                {
                    new GridOverlaySurfaceItem("current_row-1_col-1", GridOverlayKind.CurrentCell)
                    {
                        Bounds = new GridBounds(18, 30, 120, 24),
                        SnapshotRevision = 1,
                    },
                });

            var result = sut.HitTest(20, 42, snapshot);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.TargetKind, Is.EqualTo(GridHitTargetKind.SelectionCheckbox));
            Assert.That(result.RowKey, Is.EqualTo("row-1"));
        }

        [Test]
        public void HitTest_OnSelectionCheckbox_WhenIndicatorFeatureHiddenButStateSlotPreserved_ReturnsSelectionCheckbox()
        {
            var sut = new GridHitTestingService();
            var snapshot = CreateSnapshot(
                showRowIndicator: false,
                showSelectionCheckbox: true,
                rowIndicatorWidth: 18d,
                rowMarkerWidth: 18d,
                selectionCheckboxWidth: 18d);

            var result = sut.HitTest(27, 42, snapshot);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.TargetKind, Is.EqualTo(GridHitTargetKind.SelectionCheckbox));
            Assert.That(result.RowKey, Is.EqualTo("row-1"));
        }

        [Test]
        public void HitTest_OnEmptyIndicatorSlot_WhenIndicatorFeatureHidden_DoesNotReturnSelectionCheckbox()
        {
            var sut = new GridHitTestingService();
            var snapshot = CreateSnapshot(
                showRowIndicator: false,
                showSelectionCheckbox: true,
                rowIndicatorWidth: 18d,
                rowMarkerWidth: 18d,
                selectionCheckboxWidth: 18d);

            var result = sut.HitTest(9, 42, snapshot);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.TargetKind, Is.Not.EqualTo(GridHitTargetKind.SelectionCheckbox));
        }

        [Test]
        public void HitTest_WithFrozenAndScrollableCells_PicksCellFromCorrectZone()
        {
            var sut = new GridHitTestingService();
            var snapshot = CreateFrozenSnapshot();

            var frozenResult = sut.HitTest(60, 60, snapshot);
            var scrollableResult = sut.HitTest(145, 60, snapshot);

            Assert.Multiple(() =>
            {
                Assert.That(frozenResult.TargetKind, Is.EqualTo(GridHitTargetKind.Cell));
                Assert.That(frozenResult.RowKey, Is.EqualTo("row-2"));
                Assert.That(frozenResult.ColumnKey, Is.EqualTo("col-1"));
                Assert.That(scrollableResult.TargetKind, Is.EqualTo(GridHitTargetKind.Cell));
                Assert.That(scrollableResult.RowKey, Is.EqualTo("row-2"));
                Assert.That(scrollableResult.ColumnKey, Is.EqualTo("col-2"));
            });
        }

        [Test]
        public void HitTest_WhenScrollableCellWouldOverlapFrozenBoundary_PrefersFrozenZoneGeometry()
        {
            var sut = new GridHitTestingService();
            var snapshot = CreateFrozenSnapshot();

            var result = sut.HitTest(120, 60, snapshot);

            Assert.Multiple(() =>
            {
                Assert.That(result.TargetKind, Is.EqualTo(GridHitTargetKind.Cell));
                Assert.That(result.RowKey, Is.EqualTo("row-2"));
                Assert.That(result.ColumnKey, Is.EqualTo("col-1"));
            });
        }

        private static GridSurfaceSnapshot CreateSnapshot(
            bool hasDetails = false,
            bool hasHierarchyChildren = false,
            int hierarchyLevel = 0,
            bool showRowIndicator = false,
            bool showSelectionCheckbox = false,
            double rowIndicatorWidth = 0d,
            double rowMarkerWidth = 0d,
            double selectionCheckboxWidth = 0d,
            GridCurrentCellMarker currentCell = null,
            System.Collections.Generic.IReadOnlyList<GridOverlaySurfaceItem> overlays = null)
        {
            return new GridSurfaceSnapshot(
                revision: 1,
                viewportState: new GridViewportState(0, 0, 300, 200, new GridViewportMetrics(new[] { 20d }, new[] { 100d })),
                columns: new[]
                {
                    new GridColumnSurfaceItem("col-1")
                    {
                        Bounds = new GridBounds(40, 0, 100, 30),
                        SnapshotRevision = 1,
                    },
                },
                rows: new[]
                {
                    new GridRowSurfaceItem("row-1")
                    {
                        Bounds = new GridBounds(0, 30, 40, 20),
                        SnapshotRevision = 1,
                        HasDetailsExpanded = hasDetails,
                        HasHierarchyChildren = hasHierarchyChildren,
                        HierarchyLevel = hierarchyLevel,
                    },
                },
                cells: new[]
                {
                    new GridCellSurfaceItem("row-1", "col-1")
                    {
                        Bounds = new GridBounds(40, 30, 100, 20),
                        SnapshotRevision = 1,
                    },
                },
                headers: new[]
                {
                    new GridHeaderSurfaceItem("col-1", GridHeaderKind.ColumnHeader)
                    {
                        Bounds = new GridBounds(40, 0, 100, 30),
                        SnapshotRevision = 1,
                        IsResizable = true,
                    },
                    new GridHeaderSurfaceItem("row-1", GridHeaderKind.RowHeader)
                    {
                        Bounds = new GridBounds(0, 30, 40, 20),
                        ShowRowIndicator = showRowIndicator,
                        ShowSelectionCheckbox = showSelectionCheckbox,
                        RowIndicatorWidth = rowIndicatorWidth,
                        RowMarkerWidth = rowMarkerWidth,
                        SelectionCheckboxWidth = selectionCheckboxWidth,
                        SnapshotRevision = 1,
                    },
                },
                overlays: overlays,
                currentCell: currentCell);
        }

        private static GridSurfaceSnapshot CreateFrozenSnapshot()
        {
            var viewportState = new GridViewportState(50, 10, 280, 120, new GridViewportMetrics(new[] { 20d, 30d }, new[] { 100d, 80d }))
            {
                FrozenColumnCount = 1,
                FrozenRowCount = 1,
                FrozenDataWidth = 100,
                FrozenDataHeight = 20,
                FrozenCornerBounds = new GridBounds(40, 30, 100, 20),
                FrozenRowsBounds = new GridBounds(140, 30, 140, 20),
                FrozenColumnsBounds = new GridBounds(40, 50, 100, 70),
                ScrollableBounds = new GridBounds(140, 50, 140, 70),
            };

            return new GridSurfaceSnapshot(
                revision: 1,
                viewportState: viewportState,
                columns: new[]
                {
                    new GridColumnSurfaceItem("col-1")
                    {
                        Bounds = new GridBounds(40, 0, 100, 30),
                        SnapshotRevision = 1,
                        IsFrozen = true,
                    },
                    new GridColumnSurfaceItem("col-2")
                    {
                        Bounds = new GridBounds(140, 0, 30, 30),
                        SnapshotRevision = 1,
                    },
                },
                rows: new[]
                {
                    new GridRowSurfaceItem("row-1")
                    {
                        Bounds = new GridBounds(0, 30, 40, 20),
                        SnapshotRevision = 1,
                        IsFrozen = true,
                    },
                    new GridRowSurfaceItem("row-2")
                    {
                        Bounds = new GridBounds(0, 50, 40, 20),
                        SnapshotRevision = 1,
                    },
                },
                cells: new[]
                {
                    new GridCellSurfaceItem("row-1", "col-1")
                    {
                        Bounds = new GridBounds(40, 30, 100, 20),
                        SnapshotRevision = 1,
                        IsFrozen = true,
                    },
                    new GridCellSurfaceItem("row-1", "col-2")
                    {
                        Bounds = new GridBounds(140, 30, 30, 20),
                        SnapshotRevision = 1,
                    },
                    new GridCellSurfaceItem("row-2", "col-1")
                    {
                        Bounds = new GridBounds(40, 50, 100, 20),
                        SnapshotRevision = 1,
                    },
                    new GridCellSurfaceItem("row-2", "col-2")
                    {
                        Bounds = new GridBounds(140, 50, 30, 20),
                        SnapshotRevision = 1,
                    },
                },
                headers: new[]
                {
                    new GridHeaderSurfaceItem("col-1", GridHeaderKind.ColumnHeader)
                    {
                        Bounds = new GridBounds(40, 0, 100, 30),
                        SnapshotRevision = 1,
                    },
                    new GridHeaderSurfaceItem("col-2", GridHeaderKind.ColumnHeader)
                    {
                        Bounds = new GridBounds(140, 0, 30, 30),
                        SnapshotRevision = 1,
                    },
                    new GridHeaderSurfaceItem("row-1", GridHeaderKind.RowHeader)
                    {
                        Bounds = new GridBounds(0, 30, 40, 20),
                        SnapshotRevision = 1,
                    },
                    new GridHeaderSurfaceItem("row-2", GridHeaderKind.RowHeader)
                    {
                        Bounds = new GridBounds(0, 50, 40, 20),
                        SnapshotRevision = 1,
                    },
                });
        }
    }
}
