using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using PhialeGrid.Core.Commit;
using PhialeGrid.Core.Details;
using PhialeGrid.Core.Editing;
using PhialeGrid.Core.Layout;
using PhialeGrid.Core.Query;
using PhialeGrid.Core.Rendering;
using PhialeGrid.Core.Surface;

namespace PhialeGrid.Core.Tests.Rendering
{
    [TestFixture]
    public class GridSurfaceBuilderTests
    {
        [Test]
        public void BuildSnapshot_UsesConfiguredCellValueProviderAndFormatter()
        {
            var sut = new GridSurfaceBuilder();
            var context = CreateContext();
            context.CellValueProvider = new TestCellValueProvider()
                .Add("row-1", "col-1", 1234.5m)
                .Add("row-1", "col-2", null);
            context.FormatProvider = CultureInfo.InvariantCulture;

            var snapshot = sut.BuildSnapshot(context);

            var priceCell = snapshot.Cells.Single(cell => cell.RowKey == "row-1" && cell.ColumnKey == "col-1");
            var emptyCell = snapshot.Cells.Single(cell => cell.RowKey == "row-1" && cell.ColumnKey == "col-2");

            Assert.Multiple(() =>
            {
                Assert.That(priceCell.RawValue, Is.EqualTo(1234.5m));
                Assert.That(priceCell.DisplayText, Is.EqualTo("1,234.50"));
                Assert.That(emptyCell.RawValue, Is.Null);
                Assert.That(emptyCell.DisplayText, Is.Empty);
            });
        }

        [Test]
        public void BuildSnapshot_UsesConfiguredHeaderMetricsToOffsetHeadersRowsAndCells()
        {
            var sut = new GridSurfaceBuilder();
            var context = CreateContext();
            context.RowHeaderWidth = 48;
            context.ColumnHeaderHeight = 32;

            var snapshot = sut.BuildSnapshot(context);

            var column = snapshot.Columns.Single(candidate => candidate.ColumnKey == "col-1");
            var row = snapshot.Rows.Single(candidate => candidate.RowKey == "row-1");
            var cell = snapshot.Cells.Single(candidate => candidate.RowKey == "row-1" && candidate.ColumnKey == "col-1");
            var header = snapshot.Headers.Single(candidate => candidate.Kind == PhialeGrid.Core.Surface.GridHeaderKind.ColumnHeader && candidate.HeaderKey == "col-1");

            Assert.Multiple(() =>
            {
                Assert.That(column.Bounds.X, Is.EqualTo(48));
                Assert.That(column.Bounds.Y, Is.EqualTo(0));
                Assert.That(column.Bounds.Height, Is.EqualTo(32));
                Assert.That(row.Bounds.X, Is.EqualTo(0));
                Assert.That(row.Bounds.Y, Is.EqualTo(32));
                Assert.That(row.Bounds.Width, Is.EqualTo(48));
                Assert.That(cell.Bounds.X, Is.EqualTo(48));
                Assert.That(cell.Bounds.Y, Is.EqualTo(32));
                Assert.That(header.Bounds.X, Is.EqualTo(48));
                Assert.That(header.Bounds.Height, Is.EqualTo(32));
                Assert.That(snapshot.ViewportState.TotalWidth, Is.EqualTo(228));
                Assert.That(snapshot.ViewportState.TotalHeight, Is.EqualTo(82));
            });
        }

        [Test]
        public void BuildSnapshot_UsesSingleRevisionAcrossSnapshotArtifacts()
        {
            var sut = new GridSurfaceBuilder();
            var context = CreateContext();
            context.RowHeaderWidth = 40;
            context.ColumnHeaderHeight = 30;
            context.CurrentCell = new GridCurrentCellCoordinate("row-1", "col-1");
            context.SelectedCellKeys.Add("row-1_col-1");

            var snapshot = sut.BuildSnapshot(context);
            var expectedRevision = snapshot.Revision;

            Assert.Multiple(() =>
            {
                Assert.That(snapshot.ViewportState.Revision, Is.EqualTo(expectedRevision));
                Assert.That(snapshot.Columns.All(item => item.SnapshotRevision == expectedRevision), Is.True);
                Assert.That(snapshot.Rows.All(item => item.SnapshotRevision == expectedRevision), Is.True);
                Assert.That(snapshot.Cells.All(item => item.SnapshotRevision == expectedRevision), Is.True);
                Assert.That(snapshot.Headers.All(item => item.SnapshotRevision == expectedRevision), Is.True);
                Assert.That(snapshot.Overlays.All(item => item.SnapshotRevision == expectedRevision), Is.True);
                Assert.That(snapshot.SelectionRegions.All(item => item.Revision == expectedRevision), Is.True);
                Assert.That(snapshot.CurrentCell.Revision, Is.EqualTo(expectedRevision));
            });
        }

        [Test]
        public void BuildSnapshot_MapsValidationErrorsToCells()
        {
            var sut = new GridSurfaceBuilder();
            var context = CreateContext();
            context.CellValidationErrors = new System.Collections.Generic.Dictionary<string, string>
            {
                ["row-1_col-1"] = "Value is required.",
            };

            var snapshot = sut.BuildSnapshot(context);
            var invalidCell = snapshot.Cells.Single(cell => cell.RowKey == "row-1" && cell.ColumnKey == "col-1");
            var validCell = snapshot.Cells.Single(cell => cell.RowKey == "row-1" && cell.ColumnKey == "col-2");

            Assert.Multiple(() =>
            {
                Assert.That(invalidCell.HasValidationError, Is.True);
                Assert.That(invalidCell.ValidationError, Is.EqualTo("Value is required."));
                Assert.That(validCell.HasValidationError, Is.False);
                Assert.That(validCell.ValidationError, Is.Null);
            });
        }

        [Test]
        public void BuildSnapshot_WhenAdvancedStateProjectionIsProvided_UsesCoreDerivedStatesWithoutRecalculation()
        {
            var sut = new GridSurfaceBuilder();
            var context = CreateContext();
            context.StateProjection = new GridSurfaceStateProjection(
                new System.Collections.Generic.Dictionary<string, GridRecordRenderState>
                {
                    ["row-1"] = new GridRecordRenderState(
                        "row-1",
                        RecordEditState.Modified,
                        RecordValidationState.Warning,
                        RecordAccessState.Locked,
                        RecordCommitState.Pending,
                        RecordCommitDetail.TryPending,
                        "session-1"),
                },
                new System.Collections.Generic.Dictionary<string, GridCellRenderState>
                {
                    ["row-1_col-1"] = new GridCellRenderState(
                        "row-1",
                        "col-1",
                        CellDisplayState.Current,
                        CellChangeState.Modified,
                        CellValidationState.Warning,
                        CellAccessState.Locked,
                        "session-1"),
                });

            var snapshot = sut.BuildSnapshot(context);
            var row = snapshot.Rows.Single(candidate => candidate.RowKey == "row-1");
            var cell = snapshot.Cells.Single(candidate => candidate.RowKey == "row-1" && candidate.ColumnKey == "col-1");

            Assert.Multiple(() =>
            {
                Assert.That(row.EditState, Is.EqualTo(RecordEditState.Modified));
                Assert.That(row.ValidationState, Is.EqualTo(RecordValidationState.Warning));
                Assert.That(row.AccessState, Is.EqualTo(RecordAccessState.Locked));
                Assert.That(row.CommitState, Is.EqualTo(RecordCommitState.Pending));
                Assert.That(row.CommitDetail, Is.EqualTo(RecordCommitDetail.TryPending));
                Assert.That(row.HasPendingChanges, Is.True);
                Assert.That(row.HasValidationError, Is.False);
                Assert.That(cell.DisplayState, Is.EqualTo(CellDisplayState.Current));
                Assert.That(cell.ChangeState, Is.EqualTo(CellChangeState.Modified));
                Assert.That(cell.ValidationState, Is.EqualTo(CellValidationState.Warning));
                Assert.That(cell.AccessState, Is.EqualTo(CellAccessState.Locked));
                Assert.That(cell.HasValidationError, Is.False);
            });
        }

        [Test]
        public void BuildSnapshot_MapsSortStateToColumnsAndHeaders()
        {
            var sut = new GridSurfaceBuilder();
            var context = CreateContext();
            context.Sorts = new[]
            {
                new GridSortDescriptor("col-2", GridSortDirection.Descending),
                new GridSortDescriptor("col-1", GridSortDirection.Ascending),
            };

            var snapshot = sut.BuildSnapshot(context);
            var firstColumn = snapshot.Columns.Single(column => column.ColumnKey == "col-1");
            var secondColumn = snapshot.Columns.Single(column => column.ColumnKey == "col-2");
            var firstHeader = snapshot.Headers.Single(header => header.Kind == PhialeGrid.Core.Surface.GridHeaderKind.ColumnHeader && header.HeaderKey == "col-1");
            var secondHeader = snapshot.Headers.Single(header => header.Kind == PhialeGrid.Core.Surface.GridHeaderKind.ColumnHeader && header.HeaderKey == "col-2");

            Assert.Multiple(() =>
            {
                Assert.That(firstColumn.SortDirection, Is.True);
                Assert.That(firstColumn.SortPriority, Is.EqualTo(1));
                Assert.That(firstHeader.IconKey, Is.EqualTo("sort-asc"));
                Assert.That(secondColumn.SortDirection, Is.False);
                Assert.That(secondColumn.SortPriority, Is.EqualTo(0));
                Assert.That(secondHeader.IconKey, Is.EqualTo("sort-desc"));
            });
        }

        [Test]
        public void BuildSnapshot_WithFrozenRegions_KeepsFrozenItemsAnchoredAndClipsScrollableItemsAtZoneBoundary()
        {
            var sut = new GridSurfaceBuilder();
            var context = CreateFrozenContext();

            var snapshot = sut.BuildSnapshot(context);

            var frozenColumn = snapshot.Columns.Single(column => column.ColumnKey == "col-1");
            var firstScrollableColumn = snapshot.Columns.Single(column => column.ColumnKey == "col-2");
            var frozenRow = snapshot.Rows.Single(row => row.RowKey == "row-1");
            var firstScrollableRow = snapshot.Rows.Single(row => row.RowKey == "row-2");
            var frozenIntersectionCell = snapshot.Cells.Single(cell => cell.RowKey == "row-1" && cell.ColumnKey == "col-1");
            var frozenRowCell = snapshot.Cells.Single(cell => cell.RowKey == "row-1" && cell.ColumnKey == "col-2");
            var frozenColumnCell = snapshot.Cells.Single(cell => cell.RowKey == "row-2" && cell.ColumnKey == "col-1");
            var mainScrollableCell = snapshot.Cells.Single(cell => cell.RowKey == "row-2" && cell.ColumnKey == "col-2");

            Assert.Multiple(() =>
            {
                Assert.That(snapshot.ViewportState.FrozenDataWidth, Is.EqualTo(100d));
                Assert.That(snapshot.ViewportState.FrozenDataHeight, Is.EqualTo(20d));
                Assert.That(snapshot.ViewportState.ScrollableViewportWidth, Is.EqualTo(140d));
                Assert.That(snapshot.ViewportState.ScrollableViewportHeight, Is.EqualTo(70d));
                Assert.That(snapshot.ViewportState.FrozenCornerBounds, Is.EqualTo(new PhialeGrid.Core.Surface.GridBounds(40, 30, 100, 20)));
                Assert.That(snapshot.ViewportState.ScrollableBounds, Is.EqualTo(new PhialeGrid.Core.Surface.GridBounds(140, 50, 140, 70)));
                Assert.That(frozenColumn.Bounds.X, Is.EqualTo(40d));
                Assert.That(firstScrollableColumn.Bounds.X, Is.EqualTo(140d));
                Assert.That(firstScrollableColumn.Bounds.Width, Is.EqualTo(30d));
                Assert.That(frozenRow.Bounds.Y, Is.EqualTo(30d));
                Assert.That(firstScrollableRow.Bounds.Y, Is.EqualTo(50d));
                Assert.That(firstScrollableRow.Bounds.Height, Is.EqualTo(20d));
                Assert.That(frozenIntersectionCell.Bounds, Is.EqualTo(new PhialeGrid.Core.Surface.GridBounds(40, 30, 100, 20)));
                Assert.That(frozenRowCell.Bounds, Is.EqualTo(new PhialeGrid.Core.Surface.GridBounds(140, 30, 30, 20)));
                Assert.That(frozenColumnCell.Bounds, Is.EqualTo(new PhialeGrid.Core.Surface.GridBounds(40, 50, 100, 20)));
                Assert.That(mainScrollableCell.Bounds, Is.EqualTo(new PhialeGrid.Core.Surface.GridBounds(140, 50, 30, 20)));
            });
        }

        [Test]
        public void BuildSnapshot_WithGroupHeaderRow_MapsSpecialRowMetadataAndReadOnlyCells()
        {
            var sut = new GridSurfaceBuilder();
            var context = CreateContext();
            context.RowDefinitions = new[]
            {
                new GridRowDefinition
                {
                    RowKey = "group:city=alpha",
                    Height = 20,
                    HeaderText = string.Empty,
                    HierarchyLevel = 1,
                    IsGroupHeader = true,
                    HasHierarchyChildren = true,
                    IsHierarchyExpanded = true,
                    IsReadOnly = true,
                    RepresentsDataRecord = false,
                },
                new GridRowDefinition { RowKey = "row-1", Height = 30 },
            };
            context.RowLayouts = new[]
            {
                new GridRowLayout { RowKey = "group:city=alpha", Y = 0, Height = 20, HierarchyIndent = 20 },
                new GridRowLayout { RowKey = "row-1", Y = 20, Height = 30 },
            };
            context.RowRealizationRange = new GridRealizationRange { VisibleStart = 0, VisibleEnd = 2, BufferedStart = 0, BufferedEnd = 2 };
            context.CellValueProvider = new TestCellValueProvider()
                .Add("group:city=alpha", "col-1", "▼ City: Alpha (2)")
                .Add("group:city=alpha", "col-2", string.Empty)
                .Add("row-1", "col-1", "Alpha")
                .Add("row-1", "col-2", "Warsaw");

            var snapshot = sut.BuildSnapshot(context);
            var groupRow = snapshot.Rows.Single(row => row.RowKey == "group:city=alpha");
            var groupHeader = snapshot.Headers.Single(header => header.Kind == PhialeGrid.Core.Surface.GridHeaderKind.RowHeader && header.HeaderKey == "group:city=alpha");
            var groupCell = snapshot.Cells.Single(cell => cell.RowKey == "group:city=alpha" && cell.ColumnKey == "col-1");

            Assert.Multiple(() =>
            {
                Assert.That(groupRow.IsGroupHeader, Is.True);
                Assert.That(groupRow.HierarchyLevel, Is.EqualTo(1));
                Assert.That(groupRow.HasHierarchyChildren, Is.True);
                Assert.That(groupRow.IsHierarchyExpanded, Is.True);
                Assert.That(groupHeader.DisplayText, Is.Empty);
                Assert.That(groupCell.DisplayText, Is.EqualTo("City: Alpha (2)"));
                Assert.That(groupCell.IsGroupCaptionCell, Is.True);
                Assert.That(groupCell.ShowInlineChevron, Is.True);
                Assert.That(groupCell.IsInlineChevronExpanded, Is.True);
                Assert.That(groupCell.ContentIndent, Is.EqualTo(20d));
                Assert.That(groupCell.IsReadOnly, Is.True);
            });
        }

        [Test]
        public void BuildSnapshot_WithGroupedAndDataRows_PreservesStableUtilityColumnWidthsAndCellStartX()
        {
            var sut = new GridSurfaceBuilder();
            var context = CreateContext();
            context.SelectCurrentRow = true;
            context.MultiSelect = true;
            context.ShowRowNumbers = true;
            context.RowIndicatorWidth = 14d;
            context.SelectionCheckboxWidth = 18d;
            context.RowMarkerWidth = 24d;
            context.RowHeaderWidth = 56d;
            context.RowDefinitions = new[]
            {
                new GridRowDefinition
                {
                    RowKey = "group:city=alpha",
                    Height = 20,
                    IsGroupHeader = true,
                    HasHierarchyChildren = true,
                    IsHierarchyExpanded = true,
                    RepresentsDataRecord = false,
                },
                new GridRowDefinition
                {
                    RowKey = "row-1",
                    Height = 20,
                    RepresentsDataRecord = true,
                },
            };
            context.RowLayouts = new[]
            {
                new GridRowLayout { RowKey = "group:city=alpha", Y = 0, Height = 20, HierarchyIndent = 20 },
                new GridRowLayout { RowKey = "row-1", Y = 20, Height = 20 },
            };
            context.RowRealizationRange = new GridRealizationRange { VisibleStart = 0, VisibleEnd = 2, BufferedStart = 0, BufferedEnd = 2 };
            context.CellValueProvider = new TestCellValueProvider()
                .Add("group:city=alpha", "col-1", "▼ City: Alpha (2)")
                .Add("row-1", "col-1", "Alpha");

            var snapshot = sut.BuildSnapshot(context);
            var groupedStateHeader = snapshot.Headers.Single(header => header.Kind == PhialeGrid.Core.Surface.GridHeaderKind.RowHeader && header.HeaderKey == "group:city=alpha");
            var groupedNumberHeader = snapshot.Headers.Single(header => header.Kind == PhialeGrid.Core.Surface.GridHeaderKind.RowNumberHeader && header.HeaderKey == "group:city=alpha");
            var dataStateHeader = snapshot.Headers.Single(header => header.Kind == PhialeGrid.Core.Surface.GridHeaderKind.RowHeader && header.HeaderKey == "row-1");
            var dataNumberHeader = snapshot.Headers.Single(header => header.Kind == PhialeGrid.Core.Surface.GridHeaderKind.RowNumberHeader && header.HeaderKey == "row-1");
            var groupedCell = snapshot.Cells.Single(cell => cell.RowKey == "group:city=alpha" && cell.ColumnKey == "col-1");
            var dataCell = snapshot.Cells.Single(cell => cell.RowKey == "row-1" && cell.ColumnKey == "col-1");

            Assert.Multiple(() =>
            {
                Assert.That(groupedStateHeader.Bounds.Width, Is.EqualTo(32d));
                Assert.That(dataStateHeader.Bounds.Width, Is.EqualTo(32d));
                Assert.That(groupedStateHeader.SelectionCheckboxWidth, Is.EqualTo(18d));
                Assert.That(groupedStateHeader.ShowSelectionCheckbox, Is.False);
                Assert.That(dataStateHeader.SelectionCheckboxWidth, Is.EqualTo(18d));
                Assert.That(dataStateHeader.ShowSelectionCheckbox, Is.True);
                Assert.That(groupedNumberHeader.Bounds.Width, Is.EqualTo(24d));
                Assert.That(dataNumberHeader.Bounds.Width, Is.EqualTo(24d));
                Assert.That(groupedNumberHeader.Bounds.X, Is.EqualTo(dataNumberHeader.Bounds.X));
                Assert.That(groupedCell.Bounds.X, Is.EqualTo(56d));
                Assert.That(dataCell.Bounds.X, Is.EqualTo(56d));
            });
        }

        [Test]
        public void BuildSnapshot_WhenRowStateFeaturesAreDisabled_StillKeepsDedicatedStateColumn()
        {
            var sut = new GridSurfaceBuilder();
            var context = CreateContext();
            context.SelectCurrentRow = false;
            context.MultiSelect = false;
            context.ShowRowNumbers = false;
            context.RowIndicatorWidth = 18d;
            context.SelectionCheckboxWidth = 18d;
            context.RowHeaderWidth = 18d;

            var snapshot = sut.BuildSnapshot(context);
            var rowHeader = snapshot.Headers.Single(header => header.Kind == PhialeGrid.Core.Surface.GridHeaderKind.RowHeader && header.HeaderKey == "row-1");
            var firstCell = snapshot.Cells.Single(cell => cell.RowKey == "row-1" && cell.ColumnKey == "col-1");

            Assert.Multiple(() =>
            {
                Assert.That(rowHeader.Bounds.Width, Is.EqualTo(18d));
                Assert.That(rowHeader.ShowRowIndicator, Is.False);
                Assert.That(rowHeader.RowIndicatorWidth, Is.EqualTo(18d));
                Assert.That(rowHeader.SelectionCheckboxWidth, Is.EqualTo(0d));
                Assert.That(snapshot.Headers.Any(header => header.Kind == PhialeGrid.Core.Surface.GridHeaderKind.RowNumberHeader), Is.False);
                Assert.That(firstCell.Bounds.X, Is.EqualTo(18d));
            });
        }

        [Test]
        public void BuildSnapshot_WhenCurrentCellTargetsGroupHeader_SkipsCurrentCellAndCurrentRecordIndicator()
        {
            var sut = new GridSurfaceBuilder();
            var context = CreateContext();
            context.RowDefinitions = new[]
            {
                new GridRowDefinition
                {
                    RowKey = "group:city=alpha",
                    Height = 20,
                    IsGroupHeader = true,
                    HasHierarchyChildren = true,
                    IsHierarchyExpanded = true,
                    IsReadOnly = true,
                    RepresentsDataRecord = false,
                },
                new GridRowDefinition { RowKey = "row-1", Height = 30 },
            };
            context.RowLayouts = new[]
            {
                new GridRowLayout { RowKey = "group:city=alpha", Y = 0, Height = 20 },
                new GridRowLayout { RowKey = "row-1", Y = 20, Height = 30 },
            };
            context.RowRealizationRange = new GridRealizationRange { VisibleStart = 0, VisibleEnd = 2, BufferedStart = 0, BufferedEnd = 2 };
            context.CurrentCell = new GridCurrentCellCoordinate("group:city=alpha", "col-1");
            context.SelectedCellKeys.Add("group:city=alpha_col-1");
            context.ShowCurrentRecordIndicator = true;

            var snapshot = sut.BuildSnapshot(context);

            Assert.Multiple(() =>
            {
                Assert.That(snapshot.CurrentCell, Is.Null);
                Assert.That(snapshot.Overlays.Any(overlay => overlay.Kind == PhialeGrid.Core.Surface.GridOverlayKind.CurrentRecord), Is.False);
                Assert.That(snapshot.Rows.Single(row => row.RowKey == "group:city=alpha").IsSelected, Is.False);
            });
        }

        [Test]
        public void BuildSnapshot_RowIndicatorState_UsesInvalidEditedCurrentPriorityAndIndependentCheckboxColumn()
        {
            var sut = new GridSurfaceBuilder();
            var context = CreateContext();
            context.SelectCurrentRow = true;
            context.MultiSelect = true;
            context.RowIndicatorWidth = 14d;
            context.SelectionCheckboxWidth = 18d;
            context.CurrentCell = new GridCurrentCellCoordinate("row-1", "col-1");
            context.EditedRowKeys.Add("row-1");
            context.InvalidRowKeys.Add("row-1");
            context.CheckedRowKeys.Add("row-1");

            var snapshot = sut.BuildSnapshot(context);
            var row = snapshot.Rows.Single(candidate => candidate.RowKey == "row-1");
            var header = snapshot.Headers.Single(candidate => candidate.Kind == PhialeGrid.Core.Surface.GridHeaderKind.RowHeader && candidate.HeaderKey == "row-1");

            Assert.Multiple(() =>
            {
                Assert.That(row.HasPendingChanges, Is.True);
                Assert.That(row.HasValidationError, Is.True);
                Assert.That(header.ShowRowIndicator, Is.True);
                Assert.That(header.RowIndicatorState, Is.EqualTo(PhialeGrid.Core.Surface.GridRowIndicatorState.CurrentAndInvalid));
                Assert.That(header.SelectionCheckboxWidth, Is.EqualTo(18d));
                Assert.That(header.ShowSelectionCheckbox, Is.True);
                Assert.That(header.IsSelectionCheckboxChecked, Is.True);
            });
        }

        [Test]
        public void BuildSnapshot_RowIndicatorState_UsesEditedBeforeCurrentWhenRowIsDirtyButValid()
        {
            var sut = new GridSurfaceBuilder();
            var context = CreateContext();
            context.SelectCurrentRow = true;
            context.CurrentCell = new GridCurrentCellCoordinate("row-1", "col-1");
            context.EditedRowKeys.Add("row-1");

            var snapshot = sut.BuildSnapshot(context);
            var header = snapshot.Headers.Single(candidate => candidate.Kind == PhialeGrid.Core.Surface.GridHeaderKind.RowHeader && candidate.HeaderKey == "row-1");

            Assert.That(header.RowIndicatorState, Is.EqualTo(PhialeGrid.Core.Surface.GridRowIndicatorState.CurrentAndEdited));
        }

        [Test]
        public void BuildSnapshot_RowIndicatorState_UsesEditingMarkerWhenProjectionMarksActiveEdit()
        {
            var sut = new GridSurfaceBuilder();
            var context = CreateContext();
            context.SelectCurrentRow = true;
            context.CurrentCell = new GridCurrentCellCoordinate("row-1", "col-1");
            context.StateProjection = new GridSurfaceStateProjection(
                new Dictionary<string, GridRecordRenderState>(StringComparer.OrdinalIgnoreCase)
                {
                    ["row-1"] = new GridRecordRenderState(
                        "row-1",
                        RecordEditState.Editing,
                        RecordValidationState.Valid,
                        RecordAccessState.Editable,
                        RecordCommitState.Idle,
                        RecordCommitDetail.None),
                },
                new Dictionary<string, GridCellRenderState>(StringComparer.OrdinalIgnoreCase),
                editingRecordId: "row-1",
                activeEditingFieldId: "col-1");

            var snapshot = sut.BuildSnapshot(context);
            var header = snapshot.Headers.Single(candidate => candidate.Kind == PhialeGrid.Core.Surface.GridHeaderKind.RowHeader && candidate.HeaderKey == "row-1");

            Assert.That(header.RowIndicatorState, Is.EqualTo(PhialeGrid.Core.Surface.GridRowIndicatorState.Editing));
        }

        [Test]
        public void BuildSnapshot_RowIndicatorState_UsesEditingMarkerOnlyWhenProjectionContainsAllStates()
        {
            var sut = new GridSurfaceBuilder();
            var context = CreateContext();
            context.SelectCurrentRow = true;
            context.CurrentCell = new GridCurrentCellCoordinate("row-1", "col-1");
            context.StateProjection = new GridSurfaceStateProjection(
                new Dictionary<string, GridRecordRenderState>(StringComparer.OrdinalIgnoreCase)
                {
                    ["row-1"] = new GridRecordRenderState(
                        "row-1",
                        RecordEditState.Editing,
                        RecordValidationState.Invalid,
                        RecordAccessState.Editable,
                        RecordCommitState.Idle,
                        RecordCommitDetail.None),
                },
                new Dictionary<string, GridCellRenderState>(StringComparer.OrdinalIgnoreCase)
                {
                    ["row-1_col-1"] = new GridCellRenderState(
                        "row-1",
                        "col-1",
                        CellDisplayState.Current,
                        CellChangeState.Modified,
                        CellValidationState.Invalid,
                        CellAccessState.Editable),
                },
                editingRecordId: "row-1",
                activeEditingFieldId: "col-1");

            var snapshot = sut.BuildSnapshot(context);
            var header = snapshot.Headers.Single(candidate => candidate.Kind == PhialeGrid.Core.Surface.GridHeaderKind.RowHeader && candidate.HeaderKey == "row-1");

            Assert.That(header.RowIndicatorState, Is.EqualTo(PhialeGrid.Core.Surface.GridRowIndicatorState.Editing));
        }

        [Test]
        public void BuildSnapshot_WhenCellIsCurrentAndGridIsInEditMode_DoesNotAddCurrentCellOverlay()
        {
            var sut = new GridSurfaceBuilder();
            var context = CreateContext();
            context.CurrentCell = new GridCurrentCellCoordinate("row-1", "col-1");
            context.StateProjection = new GridSurfaceStateProjection(
                new Dictionary<string, GridRecordRenderState>(StringComparer.OrdinalIgnoreCase)
                {
                    ["row-1"] = new GridRecordRenderState(
                        "row-1",
                        RecordEditState.Editing,
                        RecordValidationState.Valid,
                        RecordAccessState.Editable,
                        RecordCommitState.Idle,
                        RecordCommitDetail.None),
                },
                new Dictionary<string, GridCellRenderState>(StringComparer.OrdinalIgnoreCase),
                editingRecordId: "row-1",
                activeEditingFieldId: "col-1");

            var snapshot = sut.BuildSnapshot(context);

            Assert.That(snapshot.Overlays.Any(overlay => overlay.Kind == PhialeGrid.Core.Surface.GridOverlayKind.CurrentCell), Is.False);
        }

        [Test]
        public void BuildSnapshot_WhenRowNumberingModeIsWithinGroup_ResetsNumbersAfterEachGroupHeader()
        {
            var sut = new GridSurfaceBuilder();
            var context = CreateContext();
            context.ShowRowNumbers = true;
            context.RowMarkerWidth = 24d;
            context.RowHeaderWidth = 64d;
            context.RowNumberingMode = GridRowNumberingMode.WithinGroup;
            context.RowDefinitions = new[]
            {
                new GridRowDefinition { RowKey = "group:a", Height = 20, IsGroupHeader = true, RepresentsDataRecord = false },
                new GridRowDefinition { RowKey = "row-1", Height = 20, RepresentsDataRecord = true },
                new GridRowDefinition { RowKey = "row-2", Height = 20, RepresentsDataRecord = true },
                new GridRowDefinition { RowKey = "group:b", Height = 20, IsGroupHeader = true, RepresentsDataRecord = false },
                new GridRowDefinition { RowKey = "row-3", Height = 20, RepresentsDataRecord = true },
            };
            context.RowLayouts = new[]
            {
                new GridRowLayout { RowKey = "group:a", Y = 0, Height = 20 },
                new GridRowLayout { RowKey = "row-1", Y = 20, Height = 20 },
                new GridRowLayout { RowKey = "row-2", Y = 40, Height = 20 },
                new GridRowLayout { RowKey = "group:b", Y = 60, Height = 20 },
                new GridRowLayout { RowKey = "row-3", Y = 80, Height = 20 },
            };
            context.RowRealizationRange = new GridRealizationRange { VisibleStart = 0, VisibleEnd = 5, BufferedStart = 0, BufferedEnd = 5 };

            var snapshot = sut.BuildSnapshot(context);
            var firstDataHeader = snapshot.Headers.Single(header => header.Kind == PhialeGrid.Core.Surface.GridHeaderKind.RowNumberHeader && header.HeaderKey == "row-1");
            var secondDataHeader = snapshot.Headers.Single(header => header.Kind == PhialeGrid.Core.Surface.GridHeaderKind.RowNumberHeader && header.HeaderKey == "row-2");
            var thirdDataHeader = snapshot.Headers.Single(header => header.Kind == PhialeGrid.Core.Surface.GridHeaderKind.RowNumberHeader && header.HeaderKey == "row-3");

            Assert.Multiple(() =>
            {
                Assert.That(firstDataHeader.RowNumberText, Is.EqualTo("1"));
                Assert.That(secondDataHeader.RowNumberText, Is.EqualTo("2"));
                Assert.That(thirdDataHeader.RowNumberText, Is.EqualTo("1"));
            });
        }

        [Test]
        public void BuildSnapshot_WhenCellProjectionMarksRowAsModified_AddsPendingEditTrackMarker()
        {
            var sut = new GridSurfaceBuilder();
            var context = CreateContext();
            context.StateProjection = new GridSurfaceStateProjection(
                new Dictionary<string, GridRecordRenderState>(StringComparer.OrdinalIgnoreCase),
                new Dictionary<string, GridCellRenderState>(StringComparer.OrdinalIgnoreCase)
                {
                    ["row-2_col-1"] = new GridCellRenderState(
                        "row-2",
                        "col-1",
                        CellDisplayState.Normal,
                        CellChangeState.Modified,
                        CellValidationState.Valid,
                        CellAccessState.Editable),
                });

            var snapshot = sut.BuildSnapshot(context);
            var marker = snapshot.ViewportState.VerticalTrackMarkers.Single();

            Assert.Multiple(() =>
            {
                Assert.That(marker.TargetKey, Is.EqualTo("row-2"));
                Assert.That(marker.Kind, Is.EqualTo(GridViewportTrackMarkerKind.PendingEdit));
            });
        }

        [Test]
        public void BuildSnapshot_WhenNoRowsHaveState_DoesNotAddTrackMarkers()
        {
            var sut = new GridSurfaceBuilder();
            var context = CreateContext();

            var snapshot = sut.BuildSnapshot(context);

            Assert.That(snapshot.ViewportState.VerticalTrackMarkers, Is.Empty);
        }

        [Test]
        public void BuildSnapshot_WhenCurrentDataRowExists_DoesNotAddOpaqueRowOverlay()
        {
            var sut = new GridSurfaceBuilder();
            var context = CreateContext();
            context.CurrentCell = new GridCurrentCellCoordinate("row-1", "col-1");

            var snapshot = sut.BuildSnapshot(context);

            Assert.That(snapshot.Overlays.Any(overlay => overlay.Kind == PhialeGrid.Core.Surface.GridOverlayKind.RowHighlight), Is.False);
        }

        [Test]
        public void BuildSnapshot_WithDetailsHostRow_CreatesCustomOverlayAndSkipsStandardCellsAndHeaders()
        {
            var sut = new GridSurfaceBuilder();
            var context = CreateContext();
            var payload = new object();
            context.RowDefinitions = new[]
            {
                new GridRowDefinition { RowKey = "row-1", Height = 20 },
                new GridRowDefinition
                {
                    RowKey = "details:row-1",
                    Height = 60,
                    IsDetailsHost = true,
                    DetailsPayload = payload,
                    HasDetails = true,
                    HasDetailsExpanded = true,
                    IsReadOnly = true,
                },
            };
            context.RowLayouts = new[]
            {
                new GridRowLayout { RowKey = "row-1", Y = 0, Height = 20 },
                new GridRowLayout { RowKey = "details:row-1", Y = 20, Height = 60 },
            };
            context.RowRealizationRange = new GridRealizationRange { VisibleStart = 0, VisibleEnd = 2, BufferedStart = 0, BufferedEnd = 2 };

            var snapshot = sut.BuildSnapshot(context);
            var detailsRow = snapshot.Rows.Single(row => row.RowKey == "details:row-1");
            var detailsOverlay = snapshot.Overlays.Single(overlay => overlay.TargetKey == "details:row-1");

            Assert.Multiple(() =>
            {
                Assert.That(detailsRow.IsDetailsHost, Is.True);
                Assert.That(snapshot.Cells.Any(cell => cell.RowKey == "details:row-1"), Is.False);
                Assert.That(snapshot.Headers.Any(header => header.Kind == PhialeGrid.Core.Surface.GridHeaderKind.RowHeader && header.HeaderKey == "details:row-1"), Is.False);
                Assert.That(detailsOverlay.Kind, Is.EqualTo(PhialeGrid.Core.Surface.GridOverlayKind.Custom));
                Assert.That(detailsOverlay.Payload, Is.SameAs(payload));
                Assert.That(detailsOverlay.Bounds, Is.EqualTo(new PhialeGrid.Core.Surface.GridBounds(40, 82, 360, 60)));
            });
        }

        [Test]
        public void BuildSnapshot_WithRowDetailPayload_CreatesRowDetailOverlayAndSkipsStandardCellsAndHeaders()
        {
            var sut = new GridSurfaceBuilder();
            var context = CreateContext();
            var detailContext = new GridRowDetailContext(
                "row-1",
                "row-1",
                new object(),
                new Dictionary<string, object>(),
                new Dictionary<string, GridRowDetailFieldContext>());
            var payload = new GridRowDetailSurfacePayload(
                "detail:row-1",
                "row-1",
                detailContext,
                new object());
            context.RowDefinitions = new[]
            {
                new GridRowDefinition { RowKey = "row-1", Height = 20 },
                new GridRowDefinition
                {
                    RowKey = "detail:row-1",
                    Height = 72,
                    IsDetailsHost = true,
                    DetailsPayload = payload,
                    HasDetails = true,
                    HasDetailsExpanded = true,
                    RepresentsDataRecord = false,
                },
            };
            context.RowLayouts = new[]
            {
                new GridRowLayout { RowKey = "row-1", Y = 0, Height = 20 },
                new GridRowLayout { RowKey = "detail:row-1", Y = 20, Height = 72 },
            };
            context.RowRealizationRange = new GridRealizationRange { VisibleStart = 0, VisibleEnd = 2, BufferedStart = 0, BufferedEnd = 2 };

            var snapshot = sut.BuildSnapshot(context);
            var detailsRow = snapshot.Rows.Single(row => row.RowKey == "detail:row-1");
            var detailsOverlay = snapshot.Overlays.Single(overlay => overlay.TargetKey == "detail:row-1");

            Assert.Multiple(() =>
            {
                Assert.That(detailsRow.IsDetailsHost, Is.True);
                Assert.That(snapshot.Cells.Any(cell => cell.RowKey == "detail:row-1"), Is.False);
                Assert.That(snapshot.Headers.Any(header => header.Kind == PhialeGrid.Core.Surface.GridHeaderKind.RowHeader && header.HeaderKey == "detail:row-1"), Is.False);
                Assert.That(detailsOverlay.Kind, Is.EqualTo(PhialeGrid.Core.Surface.GridOverlayKind.RowDetail));
                Assert.That(detailsOverlay.Payload, Is.SameAs(payload));
                Assert.That(detailsOverlay.Bounds, Is.EqualTo(new PhialeGrid.Core.Surface.GridBounds(40, 82, 360, 72)));
            });
        }

        private static GridSurfaceBuildContext CreateContext()
        {
            return new GridSurfaceBuildContext
            {
                ColumnDefinitions = new[]
                {
                    new GridColumnDefinition { ColumnKey = "col-1", Header = "Col 1", Width = 100 },
                    new GridColumnDefinition { ColumnKey = "col-2", Header = "Col 2", Width = 80 },
                },
                RowDefinitions = new[]
                {
                    new GridRowDefinition { RowKey = "row-1", Height = 20 },
                    new GridRowDefinition { RowKey = "row-2", Height = 30 },
                },
                ColumnLayouts = new[]
                {
                    new GridColumnLayout { ColumnKey = "col-1", X = 0, Width = 100 },
                    new GridColumnLayout { ColumnKey = "col-2", X = 100, Width = 80 },
                },
                RowLayouts = new[]
                {
                    new GridRowLayout { RowKey = "row-1", Y = 0, Height = 20 },
                    new GridRowLayout { RowKey = "row-2", Y = 20, Height = 30 },
                },
                RowRealizationRange = new GridRealizationRange { VisibleStart = 0, VisibleEnd = 2, BufferedStart = 0, BufferedEnd = 2 },
                ColumnRealizationRange = new GridRealizationRange { VisibleStart = 0, VisibleEnd = 2, BufferedStart = 0, BufferedEnd = 2 },
                ViewportWidth = 400,
                ViewportHeight = 200,
                SelectionMode = GridSelectionMode.Cell,
            };
        }

        private static GridSurfaceBuildContext CreateFrozenContext()
        {
            return new GridSurfaceBuildContext
            {
                ColumnDefinitions = new[]
                {
                    new GridColumnDefinition { ColumnKey = "col-1", Header = "Col 1", Width = 100 },
                    new GridColumnDefinition { ColumnKey = "col-2", Header = "Col 2", Width = 80 },
                    new GridColumnDefinition { ColumnKey = "col-3", Header = "Col 3", Width = 60 },
                },
                RowDefinitions = new[]
                {
                    new GridRowDefinition { RowKey = "row-1", Height = 20 },
                    new GridRowDefinition { RowKey = "row-2", Height = 30 },
                    new GridRowDefinition { RowKey = "row-3", Height = 40 },
                },
                ColumnLayouts = new[]
                {
                    new GridColumnLayout { ColumnKey = "col-1", X = 0, Width = 100, IsFrozen = true },
                    new GridColumnLayout { ColumnKey = "col-2", X = 100, Width = 80, IsFrozen = false },
                    new GridColumnLayout { ColumnKey = "col-3", X = 180, Width = 60, IsFrozen = false },
                },
                RowLayouts = new[]
                {
                    new GridRowLayout { RowKey = "row-1", Y = 0, Height = 20, IsFrozen = true },
                    new GridRowLayout { RowKey = "row-2", Y = 20, Height = 30, IsFrozen = false },
                    new GridRowLayout { RowKey = "row-3", Y = 50, Height = 40, IsFrozen = false },
                },
                RowRealizationRange = new GridRealizationRange { VisibleStart = 1, VisibleEnd = 3, BufferedStart = 1, BufferedEnd = 3 },
                ColumnRealizationRange = new GridRealizationRange { VisibleStart = 1, VisibleEnd = 3, BufferedStart = 1, BufferedEnd = 3 },
                ViewportWidth = 280,
                ViewportHeight = 120,
                HorizontalOffset = 50,
                VerticalOffset = 10,
                RowHeaderWidth = 40,
                ColumnHeaderHeight = 30,
                FrozenColumnCount = 1,
                FrozenRowCount = 1,
                SelectionMode = GridSelectionMode.Cell,
            };
        }

        private sealed class TestCellValueProvider : IGridCellValueProvider
        {
            private readonly System.Collections.Generic.Dictionary<string, object> _values = new System.Collections.Generic.Dictionary<string, object>();

            public TestCellValueProvider Add(string rowKey, string columnKey, object value)
            {
                _values[rowKey + "::" + columnKey] = value;
                return this;
            }

            public bool TryGetValue(string rowKey, string columnKey, out object value)
            {
                return _values.TryGetValue(rowKey + "::" + columnKey, out value);
            }
        }
    }
}

