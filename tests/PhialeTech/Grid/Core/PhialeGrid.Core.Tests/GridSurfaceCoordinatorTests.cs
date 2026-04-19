using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PhialeGrid.Core.Editing;
using PhialeGrid.Core.Interaction;
using PhialeGrid.Core.Layout;
using PhialeGrid.Core.Rendering;
using PhialeGrid.Core.Surface;
using GridColumnEditorKind = PhialeGrid.Core.Columns.GridColumnEditorKind;
using GridEditorItemsMode = PhialeGrid.Core.Columns.GridEditorItemsMode;

namespace PhialeGrid.Core.Tests
{
    [TestFixture]
    public class GridSurfaceCoordinatorTests
    {
        [Test]
        public void ProcessInput_PointerPressedOnCell_SetsCurrentCellAndRequestsFocus()
        {
            var coordinator = CreateCoordinator();
            GridFocusRequestEventArgs focusRequest = null;
            coordinator.FocusRequested += (sender, args) => focusRequest = args;

            coordinator.ProcessInput(new GridPointerPressedInput(DateTime.UtcNow, 50, 40));

            var snapshot = coordinator.GetCurrentSnapshot();

            Assert.Multiple(() =>
            {
                Assert.That(snapshot.CurrentCell, Is.Not.Null);
                Assert.That(snapshot.CurrentCell.RowKey, Is.EqualTo("row-1"));
                Assert.That(snapshot.CurrentCell.ColumnKey, Is.EqualTo("col-1"));
                Assert.That(focusRequest, Is.Not.Null);
                Assert.That(focusRequest.TargetKind, Is.EqualTo(GridFocusTargetKind.Grid));
            });
        }

        [Test]
        public void ProcessInput_ControlPointerPressed_AddsCellToSelection()
        {
            var coordinator = CreateCoordinator();

            coordinator.ProcessInput(new GridPointerPressedInput(DateTime.UtcNow, 50, 40));
            coordinator.ProcessInput(new GridPointerPressedInput(DateTime.UtcNow, 150, 40, modifiers: GridInputModifiers.Control));

            var snapshot = coordinator.GetCurrentSnapshot();
            var selection = snapshot.SelectionRegions.Single();

            Assert.That(selection.SelectedKeys.OrderBy(key => key).ToArray(), Is.EqualTo(new[] { "row-1_col-1", "row-1_col-2" }));
        }

        [Test]
        public void ProcessInput_PointerPressedOnCell_InRowSelectionMode_SelectsWholeRow()
        {
            var coordinator = CreateCoordinator();
            coordinator.SelectionMode = GridSelectionMode.Row;

            coordinator.ProcessInput(new GridPointerPressedInput(DateTime.UtcNow, 50, 40));

            var snapshot = coordinator.GetCurrentSnapshot();

            Assert.Multiple(() =>
            {
                Assert.That(snapshot.Rows.Single(row => row.RowKey == "row-1").IsSelected, Is.True);
                Assert.That(snapshot.Rows.Single(row => row.RowKey == "row-2").IsSelected, Is.False);
                Assert.That(snapshot.Cells.Where(cell => cell.RowKey == "row-1").All(cell => cell.IsSelected), Is.True);
                Assert.That(snapshot.Cells.Where(cell => cell.RowKey == "row-2").All(cell => !cell.IsSelected), Is.True);
            });
        }

        [Test]
        public void ProcessInput_PointerPressedOnRowHeader_SelectsWholeRow()
        {
            var coordinator = CreateCoordinator();
            coordinator.SelectionMode = GridSelectionMode.Row;

            coordinator.ProcessInput(new GridPointerPressedInput(DateTime.UtcNow, 10, 40));

            var snapshot = coordinator.GetCurrentSnapshot();

            Assert.Multiple(() =>
            {
                Assert.That(snapshot.Rows.Single(row => row.RowKey == "row-1").IsSelected, Is.True);
                Assert.That(snapshot.Headers.Single(header => header.Kind == GridHeaderKind.RowHeader && header.HeaderKey == "row-1").IsSelected, Is.True);
                Assert.That(snapshot.CurrentCell, Is.Not.Null);
                Assert.That(snapshot.CurrentCell.RowKey, Is.EqualTo("row-1"));
            });
        }

        [Test]
        public void ProcessInput_PointerPressedOnRowHeader_InCellSelectionMode_SelectsWholeRowAsCells()
        {
            var coordinator = CreateCoordinator();
            coordinator.SelectionMode = GridSelectionMode.Cell;

            coordinator.ProcessInput(new GridPointerPressedInput(DateTime.UtcNow, 10, 40));

            var snapshot = coordinator.GetCurrentSnapshot();
            Assert.Multiple(() =>
            {
                Assert.That(snapshot.Cells.Where(cell => cell.RowKey == "row-1").All(cell => cell.IsSelected), Is.True);
                Assert.That(snapshot.Cells.Where(cell => cell.RowKey == "row-2").All(cell => !cell.IsSelected), Is.True);
                Assert.That(snapshot.Rows.Single(row => row.RowKey == "row-1").IsSelected, Is.False);
                Assert.That(snapshot.Headers.Single(header => header.Kind == GridHeaderKind.RowHeader && header.HeaderKey == "row-1").IsSelected, Is.False);
            });
        }

        [Test]
        public void ProcessInput_ShiftPointerPressedOnCell_WhenRangeSelectionEnabled_SelectsRectangularRange()
        {
            var coordinator = CreateCoordinator();
            coordinator.SelectionMode = GridSelectionMode.Cell;

            coordinator.ProcessInput(new GridPointerPressedInput(DateTime.UtcNow, 50, 40));
            coordinator.ProcessInput(new GridPointerPressedInput(DateTime.UtcNow, 150, 60, modifiers: GridInputModifiers.Shift));

            var snapshot = coordinator.GetCurrentSnapshot();
            var selection = snapshot.SelectionRegions.Single(region => region.Unit == GridSelectionUnit.Cell);

            Assert.That(
                selection.SelectedKeys.OrderBy(key => key).ToArray(),
                Is.EqualTo(new[] { "row-1_col-1", "row-1_col-2", "row-2_col-1", "row-2_col-2" }));
        }

        [Test]
        public void ProcessInput_ShiftPointerPressedOnCell_WhenRangeSelectionDisabled_ReplacesSelectionWithSingleCell()
        {
            var coordinator = CreateCoordinator();
            coordinator.SelectionMode = GridSelectionMode.Cell;
            coordinator.EnableRangeSelection = false;

            coordinator.ProcessInput(new GridPointerPressedInput(DateTime.UtcNow, 50, 40));
            coordinator.ProcessInput(new GridPointerPressedInput(DateTime.UtcNow, 150, 60, modifiers: GridInputModifiers.Shift));

            var snapshot = coordinator.GetCurrentSnapshot();
            var selection = snapshot.SelectionRegions.Single(region => region.Unit == GridSelectionUnit.Cell);

            Assert.That(selection.SelectedKeys, Is.EqualTo(new[] { "row-2_col-2" }));
        }

        [Test]
        public void ProcessInput_ControlPointerPressedOnRowHeader_DoesNotAccumulateMultiRowSelection()
        {
            var coordinator = CreateCoordinator();
            coordinator.SelectionMode = GridSelectionMode.Row;

            coordinator.ProcessInput(new GridPointerPressedInput(DateTime.UtcNow, 10, 40));
            coordinator.ProcessInput(new GridPointerPressedInput(DateTime.UtcNow, 10, 60, modifiers: GridInputModifiers.Control));

            var snapshot = coordinator.GetCurrentSnapshot();

            Assert.Multiple(() =>
            {
                Assert.That(snapshot.Rows.Single(row => row.RowKey == "row-1").IsSelected, Is.False);
                Assert.That(snapshot.Rows.Single(row => row.RowKey == "row-2").IsSelected, Is.True);
                Assert.That(snapshot.SelectionRegions.Single(region => region.Unit == GridSelectionUnit.Row).SelectedKeys, Is.EqualTo(new[] { "row-2" }));
            });
        }

        [Test]
        public void ProcessInput_PointerPressedOnSelectionCheckbox_TogglesCheckedStateWithoutChangingSelectionOrCurrentCell()
        {
            var coordinator = CreateCoordinator();
            coordinator.MultiSelect = true;
            coordinator.RowHeaderWidth = 40d;
            coordinator.RowIndicatorWidth = 14d;
            coordinator.RowMarkerWidth = 24d;
            coordinator.SelectionCheckboxWidth = 18d;

            coordinator.ProcessInput(new GridPointerPressedInput(DateTime.UtcNow, 50, 40));
            var snapshotBefore = coordinator.GetCurrentSnapshot();
            var currentBefore = snapshotBefore.CurrentCell;
            var selectedBefore = snapshotBefore.Cells.Where(cell => cell.IsSelected).Select(cell => cell.ItemKey).OrderBy(key => key).ToArray();

            coordinator.ProcessInput(new GridPointerPressedInput(DateTime.UtcNow, 30, 40));

            var snapshot = coordinator.GetCurrentSnapshot();
            Assert.Multiple(() =>
            {
                Assert.That(snapshot.Headers.Single(header => header.Kind == GridHeaderKind.RowHeader && header.HeaderKey == "row-1").IsSelectionCheckboxChecked, Is.True);
                Assert.That(snapshot.Cells.Where(cell => cell.IsSelected).Select(cell => cell.ItemKey).OrderBy(key => key).ToArray(), Is.EqualTo(selectedBefore));
                Assert.That(snapshot.Headers.Single(header => header.Kind == GridHeaderKind.RowHeader && header.HeaderKey == "row-1").IsSelected, Is.EqualTo(snapshotBefore.Headers.Single(header => header.Kind == GridHeaderKind.RowHeader && header.HeaderKey == "row-1").IsSelected));
                Assert.That(snapshot.SelectionRegions.Select(region => string.Join("|", region.SelectedKeys.OrderBy(key => key))).ToArray(), Is.EqualTo(snapshotBefore.SelectionRegions.Select(region => string.Join("|", region.SelectedKeys.OrderBy(key => key))).ToArray()));
                Assert.That(snapshot.CurrentCell, Is.Not.Null);
                Assert.That(snapshot.CurrentCell.RowKey, Is.EqualTo(currentBefore.RowKey));
                Assert.That(snapshot.CurrentCell.ColumnKey, Is.EqualTo(currentBefore.ColumnKey));
            });
        }

        [Test]
        public void ProcessInput_PointerPressedOnSelectionCheckbox_WhenCurrentRowIndicatorHidden_StillTogglesCheckedState()
        {
            var coordinator = CreateCoordinator();
            coordinator.MultiSelect = true;
            coordinator.SelectCurrentRow = false;
            coordinator.RowHeaderWidth = 32d;
            coordinator.RowIndicatorWidth = 14d;
            coordinator.SelectionCheckboxWidth = 18d;

            coordinator.ProcessInput(new GridPointerPressedInput(DateTime.UtcNow, 50, 40));
            coordinator.ProcessInput(new GridPointerPressedInput(DateTime.UtcNow, 23, 40));

            var snapshot = coordinator.GetCurrentSnapshot();

            Assert.Multiple(() =>
            {
                Assert.That(snapshot.Headers.Single(header => header.Kind == GridHeaderKind.RowHeader && header.HeaderKey == "row-1").IsSelectionCheckboxChecked, Is.True);
                Assert.That(snapshot.Headers.Single(header => header.Kind == GridHeaderKind.RowHeader && header.HeaderKey == "row-1").ShowRowIndicator, Is.False);
                Assert.That(snapshot.Headers.Single(header => header.Kind == GridHeaderKind.RowHeader && header.HeaderKey == "row-1").RowIndicatorWidth, Is.EqualTo(14d));
            });
        }

        [Test]
        public void ProcessInput_Wheel_UpdatesViewportOffset()
        {
            var coordinator = CreateScrollableCoordinator();

            coordinator.ProcessInput(new GridWheelInput(DateTime.UtcNow, 50, 40, deltaY: 45));

            Assert.That(coordinator.GetCurrentSnapshot().ViewportState.VerticalOffset, Is.EqualTo(45));
        }

        [Test]
        public void ProcessInput_FocusInput_UpdatesSnapshotFocusState()
        {
            var coordinator = CreateCoordinator();
            coordinator.ProcessInput(new GridPointerPressedInput(DateTime.UtcNow, 50, 40));

            coordinator.ProcessInput(new GridFocusInput(DateTime.UtcNow, true, GridFocusCause.Programmatic));
            Assert.That(coordinator.GetCurrentSnapshot().CurrentCell.HasFocus, Is.True);

            coordinator.ProcessInput(new GridFocusInput(DateTime.UtcNow, false, GridFocusCause.Programmatic));
            Assert.That(coordinator.GetCurrentSnapshot().CurrentCell.HasFocus, Is.False);
        }

        [Test]
        public void ProcessInput_DoubleClick_StartsEditSessionUsingAccessorValue()
        {
            var coordinator = CreateCoordinator();

            coordinator.ProcessInput(new GridPointerPressedInput(DateTime.UtcNow, 50, 40, clickCount: 2));

            var snapshot = coordinator.GetCurrentSnapshot();

            Assert.Multiple(() =>
            {
                Assert.That(snapshot.ViewportState.IsInEditMode, Is.True);
                Assert.That(snapshot.CurrentCell.ShouldBeInEditMode, Is.True);
                Assert.That(snapshot.Cells.Single(cell => cell.RowKey == "row-1" && cell.ColumnKey == "col-1").IsEditing, Is.True);
            });
        }

        [Test]
        public void ProcessInput_WhenRestrictToItemsCellIsEditing_CharacterInputDoesNotMutateEditingText()
        {
            var coordinator = CreateCoordinatorWithRestrictToItemsColumn();

            coordinator.ProcessInput(new GridPointerPressedInput(DateTime.UtcNow, 50, 40, clickCount: 2));
            var before = coordinator.GetCurrentSnapshot();
            var editingCellBefore = before.Cells.Single(cell => cell.RowKey == "row-1" && cell.ColumnKey == "col-1");

            coordinator.ProcessInput(new GridKeyInput(DateTime.UtcNow, GridKey.Unknown, GridKeyEventKind.KeyDown, GridInputModifiers.None, 'Z'));

            var after = coordinator.GetCurrentSnapshot();
            var editingCellAfter = after.Cells.Single(cell => cell.RowKey == "row-1" && cell.ColumnKey == "col-1");

            Assert.Multiple(() =>
            {
                Assert.That(after.ViewportState.IsInEditMode, Is.True);
                Assert.That(editingCellBefore.EditingText, Is.EqualTo("Alpha"));
                Assert.That(editingCellAfter.EditingText, Is.EqualTo("Alpha"));
            });
        }

        [Test]
        public void ProcessInput_WhenRestrictToItemsCellIsNotEditing_CharacterInputDoesNotStartReplaceMode()
        {
            var coordinator = CreateCoordinatorWithRestrictToItemsColumn();

            coordinator.ProcessInput(new GridPointerPressedInput(DateTime.UtcNow, 50, 40));
            coordinator.ProcessInput(new GridKeyInput(DateTime.UtcNow, GridKey.Unknown, GridKeyEventKind.KeyDown, GridInputModifiers.None, 'Z'));

            var snapshot = coordinator.GetCurrentSnapshot();
            var cell = snapshot.Cells.Single(candidate => candidate.RowKey == "row-1" && candidate.ColumnKey == "col-1");

            Assert.Multiple(() =>
            {
                Assert.That(snapshot.ViewportState.IsInEditMode, Is.False);
                Assert.That(cell.IsEditing, Is.False);
                Assert.That(cell.DisplayText, Is.EqualTo("Alpha"));
            });
        }

        [Test]
        public void ProcessInput_WhenRestrictToItemsCellReceivesSelectionCommitted_UpdatesEditingText()
        {
            var coordinator = CreateCoordinatorWithRestrictToItemsColumn();

            coordinator.ProcessInput(new GridPointerPressedInput(DateTime.UtcNow, 50, 40, clickCount: 2));
            coordinator.ProcessInput(new GridEditorValueInput(
                DateTime.UtcNow,
                "row-1",
                "col-1",
                "Municipality",
                GridEditorValueChangeKind.SelectionCommitted));

            var snapshot = coordinator.GetCurrentSnapshot();
            var editingCell = snapshot.Cells.Single(cell => cell.RowKey == "row-1" && cell.ColumnKey == "col-1");

            Assert.Multiple(() =>
            {
                Assert.That(snapshot.ViewportState.IsInEditMode, Is.True);
                Assert.That(editingCell.IsEditing, Is.True);
                Assert.That(editingCell.EditingText, Is.EqualTo("Municipality"));
            });
        }

        [Test]
        public void ProcessInput_DoubleClick_WhenEditActivationModeIsExplicit_DoesNotStartEditSession()
        {
            var coordinator = CreateCoordinator();
            coordinator.EditActivationMode = GridEditActivationMode.ExplicitCommand;

            coordinator.ProcessInput(new GridPointerPressedInput(DateTime.UtcNow, 50, 40, clickCount: 2));

            var snapshot = coordinator.GetCurrentSnapshot();
            Assert.Multiple(() =>
            {
                Assert.That(snapshot.ViewportState.IsInEditMode, Is.False);
                Assert.That(snapshot.Cells.Single(cell => cell.RowKey == "row-1" && cell.ColumnKey == "col-1").IsEditing, Is.False);
            });
        }

        [Test]
        public void ProcessInput_EditCommandBegin_WhenEditActivationModeIsExplicit_StartsEditSession()
        {
            var coordinator = CreateCoordinator();
            coordinator.EditActivationMode = GridEditActivationMode.ExplicitCommand;
            coordinator.ProcessInput(new GridPointerPressedInput(DateTime.UtcNow, 50, 40));

            coordinator.ProcessInput(new GridEditCommandInput(DateTime.UtcNow, GridEditCommandKind.BeginEdit));

            var snapshot = coordinator.GetCurrentSnapshot();
            Assert.Multiple(() =>
            {
                Assert.That(snapshot.ViewportState.IsInEditMode, Is.True);
                Assert.That(snapshot.Cells.Single(cell => cell.RowKey == "row-1" && cell.ColumnKey == "col-1").IsEditing, Is.True);
            });
        }

        [Test]
        public void ProcessInput_TabWhileEditingInDirectMode_MovesToNextEditableCellAndKeepsEditMode()
        {
            var coordinator = CreateCoordinator();

            coordinator.ProcessInput(new GridPointerPressedInput(DateTime.UtcNow, 50, 40, clickCount: 2));
            coordinator.ProcessInput(new GridKeyInput(DateTime.UtcNow, GridKey.Tab));

            var snapshot = coordinator.GetCurrentSnapshot();
            Assert.Multiple(() =>
            {
                Assert.That(snapshot.ViewportState.IsInEditMode, Is.True);
                Assert.That(snapshot.CurrentCell.RowKey, Is.EqualTo("row-1"));
                Assert.That(snapshot.CurrentCell.ColumnKey, Is.EqualTo("col-2"));
                Assert.That(snapshot.Cells.Single(cell => cell.RowKey == "row-1" && cell.ColumnKey == "col-2").IsEditing, Is.True);
            });
        }

        [Test]
        public void ProcessInput_TabWhileEditingWhenNextCellIsOutsideViewport_ScrollsToKeepFocusedCellVisible()
        {
            var coordinator = CreateScrollableCoordinator();
            coordinator.SetCurrentCell("row-1", "col-2");
            coordinator.ProcessInput(new GridEditCommandInput(DateTime.UtcNow, GridEditCommandKind.BeginEdit));

            coordinator.ProcessInput(new GridKeyInput(DateTime.UtcNow, GridKey.Tab));

            var snapshot = coordinator.GetCurrentSnapshot();
            Assert.Multiple(() =>
            {
                Assert.That(snapshot.ViewportState.IsInEditMode, Is.True);
                Assert.That(snapshot.CurrentCell.RowKey, Is.EqualTo("row-1"));
                Assert.That(snapshot.CurrentCell.ColumnKey, Is.EqualTo("col-3"));
                Assert.That(snapshot.ViewportState.HorizontalOffset, Is.GreaterThan(0d));
            });
        }

        [Test]
        public void ProcessInput_ReturnWhileEditingInDirectMode_MovesToNextEditableCellAndKeepsEditMode()
        {
            var coordinator = CreateCoordinator();

            coordinator.ProcessInput(new GridPointerPressedInput(DateTime.UtcNow, 50, 40, clickCount: 2));
            coordinator.ProcessInput(new GridKeyInput(DateTime.UtcNow, GridKey.Return));

            var snapshot = coordinator.GetCurrentSnapshot();
            Assert.Multiple(() =>
            {
                Assert.That(snapshot.ViewportState.IsInEditMode, Is.True);
                Assert.That(snapshot.CurrentCell.RowKey, Is.EqualTo("row-1"));
                Assert.That(snapshot.CurrentCell.ColumnKey, Is.EqualTo("col-2"));
                Assert.That(snapshot.Cells.Single(cell => cell.RowKey == "row-1" && cell.ColumnKey == "col-2").IsEditing, Is.True);
            });
        }

        [Test]
        public void ProcessInput_TabOnLastEditableCellInDirectMode_StaysOnCurrentCellAndKeepsEditMode()
        {
            var coordinator = CreateCoordinator();
            coordinator.SetCurrentCell("row-1", "col-2");
            coordinator.ProcessInput(new GridEditCommandInput(DateTime.UtcNow, GridEditCommandKind.BeginEdit));

            coordinator.ProcessInput(new GridKeyInput(DateTime.UtcNow, GridKey.Tab));

            var snapshot = coordinator.GetCurrentSnapshot();
            Assert.Multiple(() =>
            {
                Assert.That(snapshot.ViewportState.IsInEditMode, Is.True);
                Assert.That(snapshot.CurrentCell.RowKey, Is.EqualTo("row-1"));
                Assert.That(snapshot.CurrentCell.ColumnKey, Is.EqualTo("col-2"));
                Assert.That(snapshot.Cells.Single(cell => cell.RowKey == "row-1" && cell.ColumnKey == "col-2").IsEditing, Is.True);
            });
        }

        [Test]
        public void ProcessInput_ReturnOnLastEditableCellInDirectMode_CommitsAndLeavesEditMode()
        {
            var coordinator = CreateCoordinator();
            coordinator.SetCurrentCell("row-1", "col-2");
            coordinator.ProcessInput(new GridEditCommandInput(DateTime.UtcNow, GridEditCommandKind.BeginEdit));

            coordinator.ProcessInput(new GridKeyInput(DateTime.UtcNow, GridKey.Return));

            var snapshot = coordinator.GetCurrentSnapshot();
            Assert.Multiple(() =>
            {
                Assert.That(snapshot.ViewportState.IsInEditMode, Is.False);
                Assert.That(snapshot.CurrentCell.RowKey, Is.EqualTo("row-1"));
                Assert.That(snapshot.CurrentCell.ColumnKey, Is.EqualTo("col-2"));
                Assert.That(snapshot.Cells.Single(cell => cell.RowKey == "row-1" && cell.ColumnKey == "col-2").IsEditing, Is.False);
            });
        }

        [Test]
        public void ProcessInput_ReturnOnLastEditableCellInExplicitMode_DoesNothing()
        {
            var coordinator = CreateCoordinator();
            coordinator.EditActivationMode = GridEditActivationMode.ExplicitCommand;
            coordinator.SetCurrentCell("row-1", "col-2");
            coordinator.ProcessInput(new GridEditCommandInput(DateTime.UtcNow, GridEditCommandKind.BeginEdit));

            coordinator.ProcessInput(new GridKeyInput(DateTime.UtcNow, GridKey.Return));

            var snapshot = coordinator.GetCurrentSnapshot();
            Assert.Multiple(() =>
            {
                Assert.That(snapshot.ViewportState.IsInEditMode, Is.True);
                Assert.That(snapshot.CurrentCell.RowKey, Is.EqualTo("row-1"));
                Assert.That(snapshot.CurrentCell.ColumnKey, Is.EqualTo("col-2"));
                Assert.That(snapshot.Cells.Single(cell => cell.RowKey == "row-1" && cell.ColumnKey == "col-2").IsEditing, Is.True);
            });
        }

        [Test]
        public void ProcessInput_ManipulationDelta_PansViewport()
        {
            var coordinator = CreateScrollableCoordinator();
            coordinator.SetScrollPosition(30, 40);

            coordinator.ProcessInput(new GridManipulationDeltaInput(DateTime.UtcNow, 0, 0, deltaX: 5, deltaY: 10, kind: GridManipulationKind.Pan));

            var viewport = coordinator.GetCurrentSnapshot().ViewportState;
            Assert.Multiple(() =>
            {
                Assert.That(viewport.HorizontalOffset, Is.EqualTo(25));
                Assert.That(viewport.VerticalOffset, Is.EqualTo(30));
            });
        }

        [Test]
        public void SetScrollPosition_WithFrozenRegions_ClampsOffsetsToScrollableViewportBounds()
        {
            var coordinator = CreateFrozenCoordinator();

            coordinator.SetScrollPosition(999, 999);

            var viewport = coordinator.GetCurrentSnapshot().ViewportState;
            Assert.Multiple(() =>
            {
                Assert.That(viewport.HorizontalOffset, Is.EqualTo(120d));
                Assert.That(viewport.VerticalOffset, Is.EqualTo(50d));
                Assert.That(viewport.MaxHorizontalOffset, Is.EqualTo(120d));
                Assert.That(viewport.MaxVerticalOffset, Is.EqualTo(50d));
            });
        }

        [Test]
        public void ProcessInput_Wheel_WithFrozenRegions_ClampsOffsetsToScrollableViewportBounds()
        {
            var coordinator = CreateFrozenCoordinator();

            coordinator.ProcessInput(new GridWheelInput(DateTime.UtcNow, 120, 80, deltaX: 999, deltaY: 999));

            var viewport = coordinator.GetCurrentSnapshot().ViewportState;
            Assert.Multiple(() =>
            {
                Assert.That(viewport.HorizontalOffset, Is.EqualTo(120d));
                Assert.That(viewport.VerticalOffset, Is.EqualTo(50d));
            });
        }

        [Test]
        public void ProcessInput_ManipulationDelta_WithFrozenRegions_ClampsOffsetsToScrollableViewportBounds()
        {
            var coordinator = CreateFrozenCoordinator();

            coordinator.ProcessInput(new GridManipulationDeltaInput(DateTime.UtcNow, 0, 0, deltaX: -999, deltaY: -999, kind: GridManipulationKind.Pan));

            var viewport = coordinator.GetCurrentSnapshot().ViewportState;
            Assert.Multiple(() =>
            {
                Assert.That(viewport.HorizontalOffset, Is.EqualTo(120d));
                Assert.That(viewport.VerticalOffset, Is.EqualTo(50d));
            });
        }

        [Test]
        public void ProcessInput_ReturnCommitFailure_PreservesEditModeAndMarksCellValidationError()
        {
            var coordinator = CreateCoordinator();

            coordinator.ProcessInput(new GridPointerPressedInput(DateTime.UtcNow, 50, 40, clickCount: 2));
            coordinator.ProcessInput(new GridKeyInput(DateTime.UtcNow, GridKey.Delete));
            coordinator.ProcessInput(new GridKeyInput(DateTime.UtcNow, GridKey.Return));

            var snapshot = coordinator.GetCurrentSnapshot();
            var cell = snapshot.Cells.Single(candidate => candidate.RowKey == "row-1" && candidate.ColumnKey == "col-1");

            Assert.Multiple(() =>
            {
                Assert.That(snapshot.ViewportState.IsInEditMode, Is.True);
                Assert.That(snapshot.CurrentCell.ShouldBeInEditMode, Is.True);
                Assert.That(cell.HasValidationError, Is.True);
                Assert.That(cell.ValidationError, Is.EqualTo("Value is required."));
            });
        }

        [Test]
        public void ProcessInput_PointerPressedAndReleasedOnColumnHeader_RaisesHeaderActivated()
        {
            var coordinator = CreateCoordinator();
            GridHeaderActivatedEventArgs activated = null;
            coordinator.HeaderActivated += (sender, args) => activated = args;

            coordinator.ProcessInput(new GridPointerPressedInput(DateTime.UtcNow, 50, 15));
            coordinator.ProcessInput(new GridPointerReleasedInput(DateTime.UtcNow, 50, 15));

            Assert.Multiple(() =>
            {
                Assert.That(activated, Is.Not.Null);
                Assert.That(activated.HeaderKey, Is.EqualTo("col-1"));
                Assert.That(activated.HeaderKind, Is.EqualTo(GridHeaderKind.ColumnHeader));
                Assert.That(activated.Modifiers, Is.EqualTo(GridInputModifiers.None));
                Assert.That(coordinator.GetCurrentSnapshot().CurrentCell, Is.Null);
            });
        }

        [Test]
        public void ProcessInput_RightPointerPressedAndReleasedOnColumnHeader_DoesNotRaiseHeaderActivated()
        {
            var coordinator = CreateCoordinator();
            GridHeaderActivatedEventArgs activated = null;
            coordinator.HeaderActivated += (sender, args) => activated = args;

            coordinator.ProcessInput(new GridPointerPressedInput(DateTime.UtcNow, 50, 15, button: GridMouseButton.Right));
            coordinator.ProcessInput(new GridPointerReleasedInput(DateTime.UtcNow, 50, 15, button: GridMouseButton.Right));

            Assert.Multiple(() =>
            {
                Assert.That(activated, Is.Null);
                Assert.That(coordinator.GetCurrentSnapshot().CurrentCell, Is.Null);
            });
        }

        [Test]
        public void ProcessInput_PointerPressedOnLoadMoreRow_RaisesLoadMoreActionInsteadOfActivatingCell()
        {
            var coordinator = CreateLoadMoreCoordinator();
            GridRowActionRequestedEventArgs action = null;
            coordinator.RowActionRequested += (sender, args) => action = args;

            coordinator.ProcessInput(new GridPointerPressedInput(DateTime.UtcNow, 50, 60));

            Assert.Multiple(() =>
            {
                Assert.That(action, Is.Not.Null);
                Assert.That(action.ActionKind, Is.EqualTo(GridRowActionKind.LoadMoreHierarchy));
                Assert.That(action.RowKey, Is.EqualTo("load-more"));
                Assert.That(coordinator.GetCurrentSnapshot().CurrentCell, Is.Null);
            });
        }

        [Test]
        public void ProcessInput_PointerPressedOnGroupHeader_DoesNotCreateSelectionOrCurrentCell()
        {
            var coordinator = CreateHierarchyToggleCoordinator();
            GridRowActionRequestedEventArgs action = null;
            coordinator.RowActionRequested += (sender, args) => action = args;

            coordinator.ProcessInput(new GridPointerPressedInput(DateTime.UtcNow, 50, 40));

            var snapshot = coordinator.GetCurrentSnapshot();
            Assert.Multiple(() =>
            {
                Assert.That(action, Is.Not.Null);
                Assert.That(action.ActionKind, Is.EqualTo(GridRowActionKind.ToggleHierarchy));
                Assert.That(snapshot.CurrentCell, Is.Null);
                Assert.That(snapshot.Rows.Single(row => row.RowKey == "group:alpha").IsSelected, Is.False);
                Assert.That(snapshot.Headers.Single(header => header.Kind == GridHeaderKind.RowHeader && header.HeaderKey == "group:alpha").IsSelected, Is.False);
                Assert.That(snapshot.SelectionRegions, Is.Empty);
            });
        }

        [Test]
        public void ProcessInput_PointerReleasedOutsideColumnHeader_DoesNotRaiseHeaderActivated()
        {
            var coordinator = CreateCoordinator();
            GridHeaderActivatedEventArgs activated = null;
            coordinator.HeaderActivated += (sender, args) => activated = args;

            coordinator.ProcessInput(new GridPointerPressedInput(DateTime.UtcNow, 50, 15));
            coordinator.ProcessInput(new GridPointerReleasedInput(DateTime.UtcNow, 50, 40));

            Assert.That(activated, Is.Null);
        }

        [Test]
        public void ProcessInput_PointerReleasedNearHeaderWithinActivationTolerance_RaisesHeaderActivated()
        {
            var coordinator = CreateCoordinator();
            GridHeaderActivatedEventArgs activated = null;
            coordinator.HeaderActivated += (sender, args) => activated = args;

            coordinator.ProcessInput(new GridPointerPressedInput(DateTime.UtcNow, 50, 28));
            coordinator.ProcessInput(new GridPointerReleasedInput(DateTime.UtcNow, 50, 32));

            Assert.Multiple(() =>
            {
                Assert.That(activated, Is.Not.Null);
                Assert.That(activated.HeaderKey, Is.EqualTo("col-1"));
                Assert.That(activated.HeaderKind, Is.EqualTo(GridHeaderKind.ColumnHeader));
            });
        }

        [Test]
        public void ProcessInput_HeaderPointerMoveBeyondDragThreshold_DoesNotActivateSortOnRelease()
        {
            var coordinator = CreateCoordinator();
            GridHeaderActivatedEventArgs activated = null;
            coordinator.HeaderActivated += (sender, args) => activated = args;

            coordinator.ProcessInput(new GridPointerPressedInput(DateTime.UtcNow, 50, 15));
            coordinator.ProcessInput(new GridPointerMovedInput(DateTime.UtcNow, 63, 15));
            coordinator.ProcessInput(new GridPointerReleasedInput(DateTime.UtcNow, 63, 15));

            Assert.That(activated, Is.Null);
        }

        [Test]
        public void ProcessInput_HeaderPointerVerticalJitterWithinSameHeader_ActivatesSortOnRelease()
        {
            var coordinator = CreateCoordinator();
            GridHeaderActivatedEventArgs activated = null;
            coordinator.HeaderActivated += (sender, args) => activated = args;

            coordinator.ProcessInput(new GridPointerPressedInput(DateTime.UtcNow, 50, 15));
            coordinator.ProcessInput(new GridPointerMovedInput(DateTime.UtcNow, 50, 28));
            coordinator.ProcessInput(new GridPointerReleasedInput(DateTime.UtcNow, 50, 18));

            Assert.Multiple(() =>
            {
                Assert.That(activated, Is.Not.Null);
                Assert.That(activated.HeaderKey, Is.EqualTo("col-1"));
                Assert.That(activated.HeaderKind, Is.EqualTo(GridHeaderKind.ColumnHeader));
            });
        }

        [Test]
        public void ProcessInput_ColumnResizeHandleDrag_RaisesColumnResizeRequestedWithUpdatedWidth()
        {
            var coordinator = CreateCoordinator();
            GridColumnResizeRequestedEventArgs resize = null;
            coordinator.ColumnResizeRequested += (sender, args) => resize = args;

            coordinator.ProcessInput(new GridPointerPressedInput(DateTime.UtcNow, 138, 15));
            coordinator.ProcessInput(new GridPointerMovedInput(DateTime.UtcNow, 158, 15));

            Assert.Multiple(() =>
            {
                Assert.That(resize, Is.Not.Null);
                Assert.That(resize.ColumnKey, Is.EqualTo("col-1"));
                Assert.That(resize.Width, Is.EqualTo(120).Within(0.1d));
            });
        }

        [Test]
        public void ProcessInput_PointerNearColumnEdgeOutsideResizeStrip_DoesNotTriggerResize()
        {
            var coordinator = CreateCoordinator();
            GridColumnResizeRequestedEventArgs resize = null;
            coordinator.ColumnResizeRequested += (sender, args) => resize = args;

            coordinator.ProcessInput(new GridPointerPressedInput(DateTime.UtcNow, 134, 15));
            coordinator.ProcessInput(new GridPointerMovedInput(DateTime.UtcNow, 154, 15));

            Assert.That(resize, Is.Null);
        }

        [Test]
        public void ProcessInput_TouchPressedAndReleasedOnColumnHeader_RaisesHeaderActivated()
        {
            var coordinator = CreateCoordinator();
            GridHeaderActivatedEventArgs activated = null;
            coordinator.HeaderActivated += (sender, args) => activated = args;

            coordinator.ProcessInput(new GridPointerPressedInput(DateTime.UtcNow, 50, 15, pointerId: 7, pointerKind: GridPointerKind.Touch));
            coordinator.ProcessInput(new GridPointerReleasedInput(DateTime.UtcNow, 50, 15, pointerId: 7, pointerKind: GridPointerKind.Touch));

            Assert.Multiple(() =>
            {
                Assert.That(activated, Is.Not.Null);
                Assert.That(activated.HeaderKey, Is.EqualTo("col-1"));
                Assert.That(activated.HeaderKind, Is.EqualTo(GridHeaderKind.ColumnHeader));
            });
        }

        [Test]
        public void ProcessInput_ColumnHeaderDragToAnotherHeader_RaisesColumnReorderRequested()
        {
            var coordinator = CreateCoordinator();
            GridColumnReorderRequestedEventArgs reorder = null;
            GridColumnGroupingDragRequestedEventArgs grouping = null;
            coordinator.ColumnReorderRequested += (sender, args) => reorder = args;
            coordinator.ColumnGroupingDragRequested += (sender, args) => grouping = args;

            coordinator.ProcessInput(new GridPointerPressedInput(DateTime.UtcNow, 90, 15));
            coordinator.ProcessInput(new GridPointerMovedInput(DateTime.UtcNow, 190, 15));
            coordinator.ProcessInput(new GridPointerReleasedInput(DateTime.UtcNow, 190, 15));

            Assert.Multiple(() =>
            {
                Assert.That(reorder, Is.Not.Null);
                Assert.That(reorder.ColumnKey, Is.EqualTo("col-1"));
                Assert.That(reorder.TargetColumnKey, Is.EqualTo("col-2"));
                Assert.That(grouping, Is.Null);
            });
        }

        [Test]
        public void ProcessInput_ColumnHeaderDraggedUpward_RaisesColumnGroupingDragRequested()
        {
            var coordinator = CreateCoordinator();
            GridColumnGroupingDragRequestedEventArgs grouping = null;
            GridColumnReorderRequestedEventArgs reorder = null;
            GridHeaderActivatedEventArgs activated = null;
            coordinator.ColumnGroupingDragRequested += (sender, args) => grouping = args;
            coordinator.ColumnReorderRequested += (sender, args) => reorder = args;
            coordinator.HeaderActivated += (sender, args) => activated = args;

            coordinator.ProcessInput(new GridPointerPressedInput(DateTime.UtcNow, 90, 15));
            coordinator.ProcessInput(new GridPointerMovedInput(DateTime.UtcNow, 94, -6));
            coordinator.ProcessInput(new GridPointerReleasedInput(DateTime.UtcNow, 94, -6));

            Assert.Multiple(() =>
            {
                Assert.That(grouping, Is.Not.Null);
                Assert.That(grouping.ColumnKey, Is.EqualTo("col-1"));
                Assert.That(grouping.PointerKind, Is.EqualTo(GridPointerKind.Mouse));
                Assert.That(reorder, Is.Null);
                Assert.That(activated, Is.Null);
            });
        }

        [Test]
        public void ProcessInput_PointerCanceledAfterHeaderDrag_PreventsColumnReorderCommit()
        {
            var coordinator = CreateCoordinator();
            GridColumnReorderRequestedEventArgs reorder = null;
            coordinator.ColumnReorderRequested += (sender, args) => reorder = args;

            coordinator.ProcessInput(new GridPointerPressedInput(DateTime.UtcNow, 90, 15, pointerId: 0));
            coordinator.ProcessInput(new GridPointerMovedInput(DateTime.UtcNow, 190, 15, pointerId: 0));
            coordinator.ProcessInput(new GridPointerCanceledInput(DateTime.UtcNow, 190, 15, pointerId: 0, reason: GridPointerCancelReason.CaptureLost));
            coordinator.ProcessInput(new GridPointerReleasedInput(DateTime.UtcNow, 190, 15, pointerId: 0));

            Assert.That(reorder, Is.Null);
        }

        [Test]
        public void CommitEdit_WhenSessionIsActive_CommitsValueAndLeavesEditMode()
        {
            var coordinator = CreateCoordinator();

            coordinator.ProcessInput(new GridPointerPressedInput(DateTime.UtcNow, 50, 40, clickCount: 2));
            coordinator.ProcessInput(new GridKeyInput(DateTime.UtcNow, GridKey.Delete));
            coordinator.ProcessInput(new GridKeyInput(DateTime.UtcNow, GridKey.A, character: 'Z'));

            var result = coordinator.CommitEdit();
            var snapshot = coordinator.GetCurrentSnapshot();
            var cell = snapshot.Cells.Single(candidate => candidate.RowKey == "row-1" && candidate.ColumnKey == "col-1");

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.True);
                Assert.That(snapshot.ViewportState.IsInEditMode, Is.False);
                Assert.That(cell.DisplayText, Is.EqualTo("Z"));
                Assert.That(cell.IsEditing, Is.False);
            });
        }

        [Test]
        public void CancelEdit_WhenSessionIsActive_DiscardsPendingTextAndLeavesEditMode()
        {
            var coordinator = CreateCoordinator();

            coordinator.ProcessInput(new GridPointerPressedInput(DateTime.UtcNow, 50, 40, clickCount: 2));
            coordinator.ProcessInput(new GridKeyInput(DateTime.UtcNow, GridKey.Delete));
            coordinator.ProcessInput(new GridKeyInput(DateTime.UtcNow, GridKey.A, character: 'Z'));

            var result = coordinator.CancelEdit();
            var snapshot = coordinator.GetCurrentSnapshot();
            var cell = snapshot.Cells.Single(candidate => candidate.RowKey == "row-1" && candidate.ColumnKey == "col-1");

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.True);
                Assert.That(snapshot.ViewportState.IsInEditMode, Is.False);
                Assert.That(cell.DisplayText, Is.EqualTo("Alpha"));
                Assert.That(cell.IsEditing, Is.False);
            });
        }

        [Test]
        public void SelectRows_WhenCalled_ReplacesSelectionWithSpecifiedRows()
        {
            var coordinator = CreateCoordinator();
            coordinator.SelectionMode = GridSelectionMode.Row;

            coordinator.SelectRows(new[] { "row-1", "row-2" });

            var snapshot = coordinator.GetCurrentSnapshot();
            Assert.Multiple(() =>
            {
                Assert.That(snapshot.Rows.All(row => row.IsSelected), Is.True);
                Assert.That(snapshot.SelectionRegions.Single().SelectedKeys.OrderBy(key => key).ToArray(), Is.EqualTo(new[] { "row-1", "row-2" }));
            });
        }

        [Test]
        public void ClearSelection_WhenCalled_ClearsRowSelection()
        {
            var coordinator = CreateCoordinator();
            coordinator.SelectionMode = GridSelectionMode.Row;
            coordinator.SelectRows(new[] { "row-1" });

            coordinator.ClearSelection();

            var snapshot = coordinator.GetCurrentSnapshot();
            Assert.Multiple(() =>
            {
                Assert.That(snapshot.Rows.All(row => !row.IsSelected), Is.True);
                Assert.That(snapshot.SelectionRegions, Is.Empty);
            });
        }

        [Test]
        public void SelectRows_WhenCalledInCellSelectionMode_SelectsWholeRowsAsCells()
        {
            var coordinator = CreateCoordinator();
            coordinator.SelectionMode = GridSelectionMode.Cell;

            coordinator.SelectRows(new[] { "row-1" });

            var snapshot = coordinator.GetCurrentSnapshot();
            Assert.Multiple(() =>
            {
                Assert.That(snapshot.Cells.Where(cell => cell.RowKey == "row-1").All(cell => cell.IsSelected), Is.True);
                Assert.That(snapshot.Rows.Single(row => row.RowKey == "row-1").IsSelected, Is.False);
                Assert.That(snapshot.Headers.Single(header => header.Kind == GridHeaderKind.RowHeader && header.HeaderKey == "row-1").IsSelected, Is.False);
                Assert.That(snapshot.SelectionRegions.Single().SelectedKeys.Count, Is.EqualTo(2));
            });
        }

        [Test]
        public void ProcessInput_HierarchyToggle_RaisesRowActionRequested()
        {
            var coordinator = CreateHierarchyToggleCoordinator();
            GridRowActionRequestedEventArgs action = null;
            coordinator.RowActionRequested += (sender, args) => action = args;

            coordinator.ProcessInput(new GridPointerPressedInput(DateTime.UtcNow, 10, 40));

            Assert.Multiple(() =>
            {
                Assert.That(action, Is.Not.Null);
                Assert.That(action.RowKey, Is.EqualTo("group:alpha"));
                Assert.That(action.ActionKind, Is.EqualTo(GridRowActionKind.ToggleHierarchy));
            });
        }

        [Test]
        public void Initialize_WhenCurrentCellRowDisappears_ClearsCurrentCell()
        {
            var coordinator = CreateCoordinator();
            coordinator.ProcessInput(new GridPointerPressedInput(DateTime.UtcNow, 50, 40));

            coordinator.Initialize(
                new[]
                {
                    new GridColumnDefinition { ColumnKey = "col-1", Header = "Col 1", Width = 100 },
                },
                new[]
                {
                    new GridRowDefinition { RowKey = "row-x", Height = 20 },
                });

            Assert.That(coordinator.GetCurrentSnapshot().CurrentCell, Is.Null);
        }

        private static GridSurfaceCoordinator CreateCoordinator()
        {
            var accessor = new TestCellAccessor();
            accessor.Add("row-1", "col-1", "Alpha")
                .Add("row-1", "col-2", "Beta")
                .Add("row-2", "col-1", "Gamma")
                .Add("row-2", "col-2", "Delta");

            var coordinator = new GridSurfaceCoordinator
            {
                CellValueProvider = accessor,
                EditCellAccessor = accessor,
                EditValidator = new RequiredValueValidator(),
            };

            coordinator.Initialize(
                new[]
                {
                    new GridColumnDefinition { ColumnKey = "col-1", Header = "Col 1", Width = 100 },
                    new GridColumnDefinition { ColumnKey = "col-2", Header = "Col 2", Width = 100 },
                },
                new[]
                {
                    new GridRowDefinition { RowKey = "row-1", Height = 20 },
                    new GridRowDefinition { RowKey = "row-2", Height = 20 },
                });
            coordinator.SetViewportSize(300, 200);
            return coordinator;
        }

        private static GridSurfaceCoordinator CreateCoordinatorWithRestrictToItemsColumn()
        {
            var accessor = new TestCellAccessor();
            accessor.Add("row-1", "col-1", "Alpha")
                .Add("row-1", "col-2", "Beta")
                .Add("row-2", "col-1", "Gamma")
                .Add("row-2", "col-2", "Delta");

            var coordinator = new GridSurfaceCoordinator
            {
                CellValueProvider = accessor,
                EditCellAccessor = accessor,
                EditValidator = new RequiredValueValidator(),
            };

            coordinator.Initialize(
                new[]
                {
                    new GridColumnDefinition
                    {
                        ColumnKey = "col-1",
                        Header = "Col 1",
                        Width = 100,
                        EditorKind = GridColumnEditorKind.Autocomplete,
                        EditorItems = new[] { "Alpha", "Municipality", "Parks Department" },
                        EditorItemsMode = GridEditorItemsMode.RestrictToItems,
                    },
                    new GridColumnDefinition { ColumnKey = "col-2", Header = "Col 2", Width = 100 },
                },
                new[]
                {
                    new GridRowDefinition { RowKey = "row-1", Height = 20 },
                    new GridRowDefinition { RowKey = "row-2", Height = 20 },
                });
            coordinator.SetViewportSize(300, 200);
            return coordinator;
        }

        private static GridSurfaceCoordinator CreateScrollableCoordinator()
        {
            var accessor = new TestCellAccessor();
            foreach (var row in new[] { "row-1", "row-2", "row-3", "row-4" })
            {
                accessor.Add(row, "col-1", row + "-A")
                    .Add(row, "col-2", row + "-B")
                    .Add(row, "col-3", row + "-C")
                    .Add(row, "col-4", row + "-D");
            }

            var coordinator = new GridSurfaceCoordinator
            {
                CellValueProvider = accessor,
            };

            coordinator.Initialize(
                new[]
                {
                    new GridColumnDefinition { ColumnKey = "col-1", Header = "Col 1", Width = 100 },
                    new GridColumnDefinition { ColumnKey = "col-2", Header = "Col 2", Width = 80 },
                    new GridColumnDefinition { ColumnKey = "col-3", Header = "Col 3", Width = 60 },
                    new GridColumnDefinition { ColumnKey = "col-4", Header = "Col 4", Width = 120 },
                },
                new[]
                {
                    new GridRowDefinition { RowKey = "row-1", Height = 20 },
                    new GridRowDefinition { RowKey = "row-2", Height = 30 },
                    new GridRowDefinition { RowKey = "row-3", Height = 40 },
                    new GridRowDefinition { RowKey = "row-4", Height = 50 },
                });
            coordinator.SetViewportSize(180, 100);
            return coordinator;
        }

        private static GridSurfaceCoordinator CreateFrozenCoordinator()
        {
            var accessor = new TestCellAccessor();
            accessor.Add("row-1", "col-1", "A")
                .Add("row-1", "col-2", "B")
                .Add("row-1", "col-3", "C")
                .Add("row-1", "col-4", "D")
                .Add("row-2", "col-1", "E")
                .Add("row-2", "col-2", "F")
                .Add("row-2", "col-3", "G")
                .Add("row-2", "col-4", "H")
                .Add("row-3", "col-1", "I")
                .Add("row-3", "col-2", "J")
                .Add("row-3", "col-3", "K")
                .Add("row-3", "col-4", "L")
                .Add("row-4", "col-1", "M")
                .Add("row-4", "col-2", "N")
                .Add("row-4", "col-3", "O")
                .Add("row-4", "col-4", "P");

            var coordinator = new GridSurfaceCoordinator
            {
                CellValueProvider = accessor,
                FrozenColumnCount = 1,
                FrozenRowCount = 1,
            };

            coordinator.Initialize(
                new[]
                {
                    new GridColumnDefinition { ColumnKey = "col-1", Header = "Col 1", Width = 100, IsFrozen = true },
                    new GridColumnDefinition { ColumnKey = "col-2", Header = "Col 2", Width = 80 },
                    new GridColumnDefinition { ColumnKey = "col-3", Header = "Col 3", Width = 60 },
                    new GridColumnDefinition { ColumnKey = "col-4", Header = "Col 4", Width = 120 },
                },
                new[]
                {
                    new GridRowDefinition { RowKey = "row-1", Height = 20 },
                    new GridRowDefinition { RowKey = "row-2", Height = 30 },
                    new GridRowDefinition { RowKey = "row-3", Height = 40 },
                    new GridRowDefinition { RowKey = "row-4", Height = 50 },
                });
            coordinator.SetViewportSize(280, 120);
            return coordinator;
        }

        private static GridSurfaceCoordinator CreateLoadMoreCoordinator()
        {
            var accessor = new TestCellAccessor();
            accessor.Add("row-1", "col-1", "Alpha")
                .Add("load-more", "col-1", "Load more");

            var coordinator = new GridSurfaceCoordinator
            {
                CellValueProvider = accessor,
            };

            coordinator.Initialize(
                new[]
                {
                    new GridColumnDefinition { ColumnKey = "col-1", Header = "Col 1", Width = 100 },
                },
                new[]
                {
                    new GridRowDefinition { RowKey = "row-1", Height = 20 },
                    new GridRowDefinition { RowKey = "load-more", Height = 20, IsLoadMore = true, IsReadOnly = true },
                });
            coordinator.SetViewportSize(200, 120);
            return coordinator;
        }

        private static GridSurfaceCoordinator CreateHierarchyToggleCoordinator()
        {
            var coordinator = new GridSurfaceCoordinator
            {
                CellValueProvider = new TestCellAccessor()
                    .Add("group:alpha", "col-1", "▼ City: Alpha (2)"),
            };

            coordinator.Initialize(
                new[]
                {
                    new GridColumnDefinition { ColumnKey = "col-1", Header = "Col 1", Width = 100 },
                },
                new[]
                {
                    new GridRowDefinition
                    {
                        RowKey = "group:alpha",
                        Height = 20,
                        IsGroupHeader = true,
                        HasHierarchyChildren = true,
                        IsHierarchyExpanded = true,
                        HeaderText = string.Empty,
                        RepresentsDataRecord = false,
                    },
                });
            coordinator.SetViewportSize(220, 120);
            return coordinator;
        }

        private sealed class TestCellAccessor : IGridCellValueProvider, IGridEditCellAccessor
        {
            private readonly Dictionary<string, object> _values = new Dictionary<string, object>();

            public TestCellAccessor Add(string rowKey, string columnKey, object value)
            {
                _values[rowKey + "_" + columnKey] = value;
                return this;
            }

            public bool TryGetValue(string rowKey, string columnKey, out object value)
            {
                return _values.TryGetValue(rowKey + "_" + columnKey, out value);
            }

            public void SetValue(string rowKey, string columnKey, object value)
            {
                _values[rowKey + "_" + columnKey] = value;
            }
        }

        private sealed class RequiredValueValidator : IGridEditValidator
        {
            public IReadOnlyList<GridValidationError> Validate(string rowKey, string columnKey, object parsedValue, string editingText)
            {
                if (string.IsNullOrWhiteSpace(editingText))
                {
                    return new[] { new GridValidationError(columnKey, "Value is required.") };
                }

                return Array.Empty<GridValidationError>();
            }
        }
    }
}
