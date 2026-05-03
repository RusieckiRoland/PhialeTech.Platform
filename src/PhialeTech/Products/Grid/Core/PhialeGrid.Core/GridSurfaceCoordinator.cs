using System;
using System.Collections.Generic;
using System.Linq;
using PhialeGrid.Core.Capabilities;
using PhialeGrid.Core.Editing;
using PhialeGrid.Core.HitTesting;
using PhialeGrid.Core.Interaction;
using PhialeGrid.Core.Layout;
using PhialeGrid.Core.Query;
using PhialeGrid.Core.Regions;
using PhialeGrid.Core.Rendering;
using PhialeGrid.Core.State;
using PhialeGrid.Core.Surface;
using GridColumnEditorKind = PhialeGrid.Core.Columns.GridColumnEditorKind;
using GridEditorItemsMode = PhialeGrid.Core.Columns.GridEditorItemsMode;

namespace PhialeGrid.Core
{
    /// <summary>
    /// Główny coordinator logiki grida dla frontendu.
    /// Spina Core (logika), Surface Model (opis wizualny) i Frontend (rendering).
    /// </summary>
    public sealed class GridSurfaceCoordinator
    {
        private const double HeaderDragThreshold = 12d;
        private const double HeaderGroupingVerticalDragThreshold = 18d;
        private const double HeaderActivationTolerance = 6d;
        private const double HeaderDropHorizontalTolerance = 12d;
        private const double HeaderDropVerticalTolerance = 12d;
        private readonly GridSurfaceBuilder _surfaceBuilder = new GridSurfaceBuilder();
        private readonly GridSurfaceLayoutEngine _layoutEngine = new GridSurfaceLayoutEngine();
        private readonly GridViewportCalculator _viewportCalculator = new GridViewportCalculator();
        private readonly GridHitTestingService _hitTesting = new GridHitTestingService();
        private readonly GridSurfaceCoordinatorState _state = new GridSurfaceCoordinatorState();
        private readonly GridEditState _editState = new GridEditState();
        private readonly GridRegionLayoutManager _regionLayoutManager = new GridRegionLayoutManager();

        private GridEditModel _editModel;
        private IGridEditCellAccessor _editAccessorForModel;
        private IGridEditValidator _editValidatorForModel;
        private Func<string, string, string, object> _editValueParserForModel;
        private GridSurfaceSnapshot _lastSnapshot;
        private int _snapshotUpdateBatchDepth;
        private bool _hasPendingSnapshotUpdate;

        public event EventHandler<GridSnapshotChangedEventArgs> SnapshotChanged;
        public event EventHandler<GridFocusRequestEventArgs> FocusRequested;
        public event EventHandler<GridHeaderActivatedEventArgs> HeaderActivated;
        public event EventHandler<GridColumnResizeRequestedEventArgs> ColumnResizeRequested;
        public event EventHandler<GridColumnReorderRequestedEventArgs> ColumnReorderRequested;
        public event EventHandler<GridColumnGroupingDragRequestedEventArgs> ColumnGroupingDragRequested;
        public event EventHandler<GridRowActionRequestedEventArgs> RowActionRequested;

        public IGridCellValueProvider CellValueProvider { get; set; }

        public IGridEditCellAccessor EditCellAccessor { get; set; }

        public IGridEditValidator EditValidator { get; set; }

        public Func<string, string, string, object> EditValueParser { get; set; }

        public double ColumnHeaderHeight { get; set; } = 30;

        public double FilterRowHeight { get; set; } = 32;

        public double DataTopInset { get; set; } = double.NaN;

        public double RowHeaderWidth { get; set; } = 40;

        public int FrozenColumnCount
        {
            get => _state.FrozenColumnCount;
            set
            {
                _state.FrozenColumnCount = Math.Max(0, value);
                UpdateSnapshot();
            }
        }

        public int FrozenRowCount
        {
            get => _state.FrozenRowCount;
            set
            {
                _state.FrozenRowCount = Math.Max(0, value);
                UpdateSnapshot();
            }
        }

        public IReadOnlyList<GridSortDescriptor> Sorts
        {
            get => _state.SortDescriptors;
            set
            {
                _state.SortDescriptors = value?.ToArray() ?? Array.Empty<GridSortDescriptor>();
                UpdateSnapshot();
            }
        }

        public GridSelectionMode SelectionMode
        {
            get => _state.SelectionMode;
            set
            {
                _state.SelectionMode = value;
                UpdateSnapshot();
            }
        }

        public bool EnableCellSelection
        {
            get => _state.EnableCellSelection;
            set
            {
                if (_state.EnableCellSelection == value)
                {
                    return;
                }

                _state.EnableCellSelection = value;
                if (!value)
                {
                    _state.SelectedRowKeys.Clear();
                    _state.SelectedCellKeys.Clear();
                    _state.SelectionAnchorCell = null;
                }

                UpdateSnapshot();
            }
        }

        public bool EnableRangeSelection
        {
            get => _state.EnableRangeSelection;
            set
            {
                if (_state.EnableRangeSelection == value)
                {
                    return;
                }

                _state.EnableRangeSelection = value;
                if (!value)
                {
                    _state.SelectionAnchorCell = null;
                }

                UpdateSnapshot();
            }
        }

        public GridEditState EditState => _editState;

        public IEditSessionContext EditSessionContext { get; set; }

        public GridEditActivationMode EditActivationMode { get; set; } = GridEditActivationMode.DirectInteraction;

        public bool ShowCurrentRecordIndicator { get; set; } = true;

        public bool SelectCurrentRow
        {
            get => _state.SelectCurrentRow;
            set
            {
                if (_state.SelectCurrentRow == value)
                {
                    return;
                }

                _state.SelectCurrentRow = value;
                UpdateSnapshot();
            }
        }

        public bool MultiSelect
        {
            get => _state.MultiSelect;
            set
            {
                if (_state.MultiSelect == value)
                {
                    return;
                }

                _state.MultiSelect = value;
                if (!value)
                {
                    _state.CheckedRowKeys.Clear();
                }

                UpdateSnapshot();
            }
        }

        public double RowIndicatorWidth
        {
            get => _state.RowIndicatorWidth;
            set
            {
                var nextWidth = Math.Max(0, value);
                if (Math.Abs(_state.RowIndicatorWidth - nextWidth) < 0.1d)
                {
                    return;
                }

                _state.RowIndicatorWidth = nextWidth;
                UpdateSnapshot();
            }
        }

        public double SelectionCheckboxWidth
        {
            get => _state.SelectionCheckboxWidth;
            set
            {
                var nextWidth = Math.Max(0, value);
                if (Math.Abs(_state.SelectionCheckboxWidth - nextWidth) < 0.1d)
                {
                    return;
                }

                _state.SelectionCheckboxWidth = nextWidth;
                UpdateSnapshot();
            }
        }

        public double RowMarkerWidth
        {
            get => _state.RowMarkerWidth;
            set
            {
                var nextWidth = Math.Max(0, value);
                if (Math.Abs(_state.RowMarkerWidth - nextWidth) < 0.1d)
                {
                    return;
                }

                _state.RowMarkerWidth = nextWidth;
                UpdateSnapshot();
            }
        }

        public double RowActionWidth
        {
            get => _state.RowActionWidth;
            set
            {
                var nextWidth = Math.Max(0, value);
                if (Math.Abs(_state.RowActionWidth - nextWidth) < 0.1d)
                {
                    return;
                }

                _state.RowActionWidth = nextWidth;
                UpdateSnapshot();
            }
        }

        public bool ShowRowNumbers
        {
            get => _state.ShowRowNumbers;
            set
            {
                if (_state.ShowRowNumbers == value)
                {
                    return;
                }

                _state.ShowRowNumbers = value;
                UpdateSnapshot();
            }
        }

        public GridRowNumberingMode RowNumberingMode
        {
            get => _state.RowNumberingMode;
            set
            {
                if (_state.RowNumberingMode == value)
                {
                    return;
                }

                _state.RowNumberingMode = value;
                UpdateSnapshot();
            }
        }

        public void SetEditedRows(IEnumerable<string> rowKeys)
        {
            ReplaceRowKeySet(_state.EditedRowKeys, rowKeys);
            UpdateSnapshot();
        }

        public void SetInvalidRows(IEnumerable<string> rowKeys)
        {
            ReplaceRowKeySet(_state.InvalidRowKeys, rowKeys);
            UpdateSnapshot();
        }

        public void SetRowIndicatorToolTips(IReadOnlyDictionary<string, string> toolTipsByRowKey)
        {
            _state.RowIndicatorToolTips = toolTipsByRowKey != null
                ? toolTipsByRowKey.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            UpdateSnapshot();
        }

        public void SetStateProjection(GridSurfaceStateProjection stateProjection)
        {
            _state.StateProjection = stateProjection ?? GridSurfaceStateProjection.Empty;
            UpdateSnapshot();
        }

        public void SetCheckedRows(IEnumerable<string> rowKeys)
        {
            ReplaceRowKeySet(_state.CheckedRowKeys, rowKeys);
            UpdateSnapshot();
        }

        public bool TryToggleSelectionCheckbox(string rowKey)
        {
            if (!_state.MultiSelect || string.IsNullOrWhiteSpace(rowKey) || !RepresentsDataRecord(rowKey))
            {
                return false;
            }

            var isChecked = ToggleCheckedRow(rowKey);
            SetSelectionForCheckedRow(rowKey, isChecked);
            UpdateSnapshot();
            return true;
        }

        /// <summary>
        /// Otrzymuje input event z frontendu i przetwarza go.
        /// </summary>
        public void ProcessInput(GridInputEvent input)
        {
            ProcessInput(input, GridHitTestSurfaceScope.FullSurface);
        }

        public void ProcessInput(GridInputEvent input, GridHitTestSurfaceScope surfaceScope)
        {
            if (input == null)
            {
                return;
            }

            switch (input)
            {
                case GridPointerPressedInput pointerPressed:
                    HandlePointerPressed(pointerPressed, surfaceScope);
                    break;
                case GridPointerMovedInput pointerMoved:
                    HandlePointerMoved(pointerMoved);
                    break;
                case GridPointerReleasedInput pointerReleased:
                    HandlePointerReleased(pointerReleased);
                    break;
                case GridPointerCanceledInput pointerCanceled:
                    HandlePointerCanceled(pointerCanceled);
                    break;
                case GridWheelInput wheel:
                    HandleWheel(wheel);
                    break;
                case GridKeyInput key:
                    HandleKeyInput(key);
                    break;
                case GridFocusInput focus:
                    HandleFocusInput(focus);
                    break;
                case GridManipulationStartedInput manipulationStarted:
                    HandleManipulationStarted(manipulationStarted);
                    break;
                case GridManipulationDeltaInput manipulationDelta:
                    HandleManipulationDelta(manipulationDelta);
                    break;
                case GridManipulationCompletedInput manipulationCompleted:
                    HandleManipulationCompleted(manipulationCompleted);
                    break;
                case GridEditCommandInput editCommand:
                    HandleEditCommand(editCommand);
                    break;
                case GridRegionCommandInput regionCommand:
                    _regionLayoutManager.Process(regionCommand);
                    break;
                case GridEditorValueInput editorValue:
                    HandleEditorValueInput(editorValue);
                    break;
                case GridHostScrollChangedInput hostScroll:
                    HandleHostScrollChanged(hostScroll);
                    break;
                case GridHostViewportChangedInput hostViewport:
                    HandleHostViewportChanged(hostViewport);
                    break;
                default:
                    throw new NotSupportedException("Unsupported grid input type: " + input.GetType().Name);
            }

            UpdateSnapshot();
        }

        /// <summary>
        /// Initializuje coordinator z danymi kolumn i wierszy.
        /// </summary>
        public void Initialize(
            IReadOnlyList<GridColumnDefinition> columns,
            IReadOnlyList<GridRowDefinition> rows)
        {
            _state.ColumnDefinitions = columns?.ToList() ?? new List<GridColumnDefinition>();
            _state.RowDefinitions = rows?.ToList() ?? new List<GridRowDefinition>();
            NormalizeStateToDefinitions();

            UpdateSnapshot();
        }

        /// <summary>
        /// Ustawia aktualny scroll position.
        /// </summary>
        public void SetScrollPosition(double horizontalOffset, double verticalOffset)
        {
            _state.HorizontalOffset = Math.Max(0, horizontalOffset);
            _state.VerticalOffset = Math.Max(0, verticalOffset);
            UpdateSnapshot();
        }

        /// <summary>
        /// Ustawia rozmiar viewport'u.
        /// </summary>
        public void SetViewportSize(double width, double height)
        {
            _state.ViewportWidth = Math.Max(100, width);
            _state.ViewportHeight = Math.Max(100, height);
            UpdateSnapshot();
        }

        /// <summary>
        /// Pobiera ostatni snapshot.
        /// </summary>
        public GridSurfaceSnapshot GetCurrentSnapshot() => _lastSnapshot;

        public IDisposable BeginSnapshotUpdateBatch(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                throw new ArgumentException("Snapshot update batch reason is required.", nameof(reason));
            }

            _snapshotUpdateBatchDepth++;
            return new SnapshotUpdateBatch(this);
        }

        public bool CommitEdit()
        {
            var result = EnsureEditModel()?.Commit() ?? false;
            if (result)
            {
                EditSessionContext?.PostActiveEdit();
            }

            UpdateSnapshot();
            return result;
        }

        public bool CancelEdit()
        {
            var result = EnsureEditModel()?.Cancel() ?? false;
            if (result)
            {
                EditSessionContext?.CancelActiveEdit();
            }

            UpdateSnapshot();
            return result;
        }

        public void SetRegionCapabilityPolicy(IGridCapabilityPolicy capabilityPolicy)
        {
            _regionLayoutManager.SetCapabilityPolicy(capabilityPolicy);
            UpdateSnapshot();
        }

        public GridRegionViewState ResolveRegion(GridRegionKind regionKind)
        {
            return _regionLayoutManager.Resolve(regionKind);
        }

        public IReadOnlyList<GridRegionViewState> ResolveRegions()
        {
            return _regionLayoutManager.ResolveAll();
        }

        public IReadOnlyList<GridRegionViewState> ExportResolvedRegionStates()
        {
            return _regionLayoutManager.ResolveAll();
        }

        public GridRegionLayoutSnapshot ExportRegionLayout()
        {
            return _regionLayoutManager.ExportLayout();
        }

        public void RestoreRegionLayout(GridRegionLayoutSnapshot snapshot)
        {
            _regionLayoutManager.RestoreLayout(snapshot);
            UpdateSnapshot();
        }

        public void OpenRegion(GridRegionKind regionKind)
        {
            _regionLayoutManager.OpenRegion(regionKind);
            UpdateSnapshot();
        }

        public void CloseRegion(GridRegionKind regionKind)
        {
            _regionLayoutManager.CloseRegion(regionKind);
            UpdateSnapshot();
        }

        public void CollapseRegion(GridRegionKind regionKind)
        {
            _regionLayoutManager.CollapseRegion(regionKind);
            UpdateSnapshot();
        }

        public void ResizeRegion(GridRegionKind regionKind, double size)
        {
            _regionLayoutManager.ResizeRegion(regionKind, size);
            UpdateSnapshot();
        }

        public void ActivateRegion(GridRegionKind regionKind)
        {
            _regionLayoutManager.ActivateRegion(regionKind);
            UpdateSnapshot();
        }

        private void HandleHostScrollChanged(GridHostScrollChangedInput input)
        {
            _state.HorizontalOffset = Math.Max(0, input.HorizontalOffset);
            _state.VerticalOffset = Math.Max(0, input.VerticalOffset);
        }

        private void HandleHostViewportChanged(GridHostViewportChangedInput input)
        {
            _state.ViewportWidth = Math.Max(100, input.Width);
            _state.ViewportHeight = Math.Max(100, input.Height);
        }

        public bool UpdateEditingText(string text)
        {
            var session = _editState.CurrentSession;
            if (session != null && !CanAcceptEditingText(session.ColumnKey, text))
            {
                return false;
            }

            var result = EnsureEditModel()?.SetText(text) ?? false;
            if (result)
            {
                UpdateSnapshot();
            }

            return result;
        }

        public void SelectRows(IEnumerable<string> rowKeys)
        {
            var normalizedRowKeys = (rowKeys ?? Array.Empty<string>())
                .Where(rowKey => !string.IsNullOrWhiteSpace(rowKey))
                .Where(RepresentsDataRecord)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            _state.SelectedRowKeys.Clear();
            _state.SelectedCellKeys.Clear();
            if (!_state.EnableCellSelection && _state.SelectionMode != GridSelectionMode.Row)
            {
                // Preserve the current cell update below but do not materialize selection regions.
            }
            else if (_state.SelectionMode == GridSelectionMode.Row)
            {
                foreach (var rowKey in normalizedRowKeys)
                {
                    _state.SelectedRowKeys.Add(rowKey);
                }
            }
            else
            {
                foreach (var rowKey in normalizedRowKeys)
                {
                    foreach (var cellKey in EnumerateRowCellKeys(rowKey))
                    {
                        _state.SelectedCellKeys.Add(cellKey);
                    }
                }
            }

            var firstSelectedRow = normalizedRowKeys.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(firstSelectedRow) && _state.ColumnDefinitions.Count > 0)
            {
                var firstColumnKey = _state.ColumnDefinitions[0].ColumnKey;
                _state.CurrentCell = new GridCurrentCellCoordinate(firstSelectedRow, firstColumnKey);
                _state.SelectionAnchorCell = new GridCurrentCellCoordinate(firstSelectedRow, firstColumnKey);
            }

            UpdateSnapshot();
        }

        public void ClearSelection()
        {
            _state.SelectedCellKeys.Clear();
            _state.SelectedRowKeys.Clear();
            _state.SelectionAnchorCell = null;
            UpdateSnapshot();
        }

        public void SetCurrentCell(string rowKey, string columnKey)
        {
            if (string.IsNullOrWhiteSpace(rowKey) || !RepresentsDataRecord(rowKey))
            {
                return;
            }

            var resolvedColumnKey = columnKey;
            if (string.IsNullOrWhiteSpace(resolvedColumnKey) ||
                _state.ColumnDefinitions.All(column => !string.Equals(column.ColumnKey, resolvedColumnKey, StringComparison.OrdinalIgnoreCase)))
            {
                resolvedColumnKey = _state.ColumnDefinitions.FirstOrDefault()?.ColumnKey;
            }

            if (string.IsNullOrWhiteSpace(resolvedColumnKey))
            {
                return;
            }

            SetCurrentCellCoordinate(rowKey, resolvedColumnKey, ensureVisible: true);
            _state.SelectedRowKeys.Clear();
            _state.SelectedCellKeys.Clear();
            if (_state.SelectionMode == GridSelectionMode.Row)
            {
                _state.SelectedRowKeys.Add(rowKey);
            }
            else if (_state.EnableCellSelection && _state.SelectionMode != GridSelectionMode.None)
            {
                _state.SelectedCellKeys.Add(rowKey + "_" + resolvedColumnKey);
            }

            _state.SelectionAnchorCell = new GridCurrentCellCoordinate(rowKey, resolvedColumnKey);
            EnsureCurrentCellVisible();

            UpdateSnapshot();
        }

        private bool BeginEditCurrentCell()
        {
            if (_state.CurrentCell == null)
            {
                return false;
            }

            return StartEditingCell(_state.CurrentCell.RowKey, _state.CurrentCell.ColumnKey, GridEditStartMode.Programmatic);
        }

        public bool TryScrollRowIntoView(string rowKey, GridScrollAlignment alignment = GridScrollAlignment.Nearest)
        {
            if (!TryResolveScrollableTarget(rowKey, columnKey: null, out var target) || target.Row == null)
            {
                return false;
            }

            ApplyScrollTarget(target, scrollHorizontally: false, scrollVertically: true, alignment);
            UpdateSnapshot();
            return true;
        }

        public bool TryScrollColumnIntoView(string columnKey, GridScrollAlignment alignment = GridScrollAlignment.Nearest)
        {
            if (!TryResolveScrollableTarget(rowKey: null, columnKey, out var target) || target.Column == null)
            {
                return false;
            }

            ApplyScrollTarget(target, scrollHorizontally: true, scrollVertically: false, alignment);
            UpdateSnapshot();
            return true;
        }

        public bool TryScrollCellIntoView(string rowKey, string columnKey, GridScrollAlignment alignment = GridScrollAlignment.Nearest, bool setCurrentCell = false)
        {
            if (!TryResolveScrollableTarget(rowKey, columnKey, out var target))
            {
                return false;
            }

            ApplyScrollTarget(target, scrollHorizontally: true, scrollVertically: true, alignment);
            _state.PendingEnsureCurrentCellVisible = false;
            if (setCurrentCell)
            {
                SetCurrentCellCoordinate(target.Row.RowKey, target.Column.ColumnKey, ensureVisible: false);
                _state.SelectionAnchorCell = new GridCurrentCellCoordinate(target.Row.RowKey, target.Column.ColumnKey);
            }

            UpdateSnapshot();
            return true;
        }

        private void HandlePointerPressed(GridPointerPressedInput input, GridHitTestSurfaceScope surfaceScope)
        {
            if (_lastSnapshot == null)
            {
                return;
            }

            CancelPointerInteractionSession();
            _state.LastPointerX = input.X;
            _state.LastPointerY = input.Y;

            RequestFocusIfNeeded();

            var hit = _hitTesting.HitTest(input.X, input.Y, _lastSnapshot, surfaceScope);
            if (hit == null)
            {
                return;
            }

            if (!TryCompleteActiveEditBeforePointerAction(hit))
            {
                return;
            }

            switch (hit.TargetKind)
            {
                case GridHitTargetKind.Cell:
                case GridHitTargetKind.CurrentCellMarker:
                    if (TryRequestNonDataRowAction(hit.RowKey))
                    {
                        break;
                    }

                    ActivateCell(hit.RowKey, hit.ColumnKey, input);
                    break;
                case GridHitTargetKind.Header:
                    if (hit.HeaderKind.HasValue &&
                        IsRowUtilityHeaderKind(hit.HeaderKind.Value) &&
                        !string.IsNullOrWhiteSpace(hit.RowKey))
                    {
                        if (TryRequestNonDataRowAction(hit.RowKey))
                        {
                            break;
                        }

                        ActivateRow(hit.RowKey, input);
                    }
                    else
                    {
                        BeginHeaderInteraction(hit, input);
                    }
                    break;
                case GridHitTargetKind.ColumnResizeHandle:
                    BeginColumnResize(hit, input);
                    break;
                case GridHitTargetKind.RowResizeHandle:
                    break;
                case GridHitTargetKind.Details:
                    RequestRowAction(hit.RowKey, GridRowActionKind.ToggleDetails);
                    break;
                case GridHitTargetKind.HierarchyToggle:
                    RequestRowAction(hit.RowKey, GridRowActionKind.ToggleHierarchy);
                    break;
                case GridHitTargetKind.SelectionCheckbox:
                    var isChecked = ToggleCheckedRow(hit.RowKey);
                    SetSelectionForCheckedRow(hit.RowKey, isChecked);
                    break;
                case GridHitTargetKind.Overlay:
                case GridHitTargetKind.EmptySpace:
                    break;
                default:
                    throw new NotSupportedException("Unsupported pointer target: " + hit.TargetKind);
            }
        }

        private bool TryRequestNonDataRowAction(string rowKey)
        {
            if (string.IsNullOrWhiteSpace(rowKey) || _lastSnapshot == null)
            {
                return false;
            }

            var row = _lastSnapshot.Rows.FirstOrDefault(candidate => candidate.RowKey == rowKey);
            if (row == null)
            {
                return false;
            }

            if (row.IsLoadMore)
            {
                RequestRowAction(rowKey, GridRowActionKind.LoadMoreHierarchy);
                return true;
            }

            if (row.IsGroupHeader || row.HasHierarchyChildren)
            {
                RequestRowAction(rowKey, GridRowActionKind.ToggleHierarchy);
                return true;
            }

            return false;
        }

        private void HandlePointerMoved(GridPointerMovedInput input)
        {
            if (_lastSnapshot == null)
            {
                return;
            }

            var session = _state.PointerSession;
            if (DoesSessionMatch(session, input.PointerId, input.PointerKind))
            {
                _state.LastPointerX = input.X;
                _state.LastPointerY = input.Y;
                session.LastX = input.X;
                session.LastY = input.Y;

                switch (session.Mode)
                {
                    case GridPointerInteractionMode.PendingResize:
                    case GridPointerInteractionMode.ActiveResize:
                        UpdateColumnResize(session, input.X);
                        break;
                    case GridPointerInteractionMode.PendingActivation:
                    case GridPointerInteractionMode.PendingReorder:
                    case GridPointerInteractionMode.ActiveReorder:
                        UpdateColumnReorder(session, input.X, input.Y);
                        break;
                }
            }
        }

        private void HandlePointerReleased(GridPointerReleasedInput input)
        {
            var session = _state.PointerSession;
            if (!DoesSessionMatch(session, input.PointerId, input.PointerKind))
            {
                return;
            }

            _state.LastPointerX = input.X;
            _state.LastPointerY = input.Y;
            session.LastX = input.X;
            session.LastY = input.Y;

            switch (session.Mode)
            {
                case GridPointerInteractionMode.PendingResize:
                case GridPointerInteractionMode.ActiveResize:
                    UpdateColumnResize(session, input.X);
                    break;
                case GridPointerInteractionMode.PendingActivation:
                    TryActivateHeader(session, input);
                    break;
                case GridPointerInteractionMode.PendingReorder:
                case GridPointerInteractionMode.ActiveReorder:
                    TryCommitColumnReorder(session, input.X, input.Y);
                    break;
            }

            CancelPointerInteractionSession();
        }

        private void HandleWheel(GridWheelInput input)
        {
            var newHorizontalOffset = _state.HorizontalOffset + input.DeltaX;
            var newVerticalOffset = _state.VerticalOffset + input.DeltaY;
            _state.HorizontalOffset = Math.Max(0, newHorizontalOffset);
            _state.VerticalOffset = Math.Max(0, newVerticalOffset);
        }

        private void HandleKeyInput(GridKeyInput input)
        {
            if (input.Kind != GridKeyEventKind.KeyDown)
            {
                return;
            }

            if (_state.CurrentCell == null)
            {
                return;
            }

            switch (input.Key)
            {
                case GridKey.Left:
                    if (!TryCompleteActiveEditBeforeNavigation())
                    {
                        break;
                    }

                    NavigateLeft();
                    break;
                case GridKey.Right:
                    if (!TryCompleteActiveEditBeforeNavigation())
                    {
                        break;
                    }

                    NavigateRight();
                    break;
                case GridKey.Up:
                    if (!TryCompleteActiveEditBeforeNavigation())
                    {
                        break;
                    }

                    NavigateUp();
                    break;
                case GridKey.Down:
                    if (!TryCompleteActiveEditBeforeNavigation())
                    {
                        break;
                    }

                    NavigateDown();
                    break;
                case GridKey.Tab:
                    if (TryHandleEditingAdvance(input.HasShift, commitLastCellForDirectMode: false))
                    {
                        break;
                    }

                    if (!TryCompleteActiveEditBeforeNavigation())
                    {
                        break;
                    }

                    if (input.HasShift)
                    {
                        NavigateLeft();
                    }
                    else
                    {
                        NavigateRight();
                    }
                    break;
                case GridKey.Return:
                    if (_editState.IsInEditMode)
                    {
                        if (TryHandleEditingAdvance(moveBackward: false, commitLastCellForDirectMode: true))
                        {
                            break;
                        }

                        if (EditActivationMode == GridEditActivationMode.DirectInteraction)
                        {
                            CommitEdit();
                        }
                    }
                    else if (EditActivationMode == GridEditActivationMode.DirectInteraction)
                    {
                        StartEditingCell(_state.CurrentCell.RowKey, _state.CurrentCell.ColumnKey, GridEditStartMode.Enter);
                    }
                    break;
                case GridKey.Escape:
                    if (_editState.IsInEditMode && EditActivationMode == GridEditActivationMode.DirectInteraction)
                    {
                        CancelEdit();
                    }
                    break;
                case GridKey.Delete:
                    if (_editState.IsInEditMode)
                    {
                        EnsureEditModel()?.Clear();
                    }
                    break;
                case GridKey.F2:
                    if (EditActivationMode == GridEditActivationMode.DirectInteraction)
                    {
                        StartEditingCell(_state.CurrentCell.RowKey, _state.CurrentCell.ColumnKey, GridEditStartMode.F2);
                    }

                    break;
                default:
                    HandleCharacterInput(input);
                    break;
            }
        }

        private void HandleFocusInput(GridFocusInput input)
        {
            _state.HasFocus = input.HasFocus;
            _state.IsFocusRequestPending = false;

            if (!input.HasFocus)
            {
                CancelPointerInteractionSession();
            }
        }

        private void HandleManipulationStarted(GridManipulationStartedInput input)
        {
            _state.IsManipulating = true;
            _state.LastPointerX = input.X;
            _state.LastPointerY = input.Y;
            CancelPointerInteractionSession();
        }

        private void HandleManipulationDelta(GridManipulationDeltaInput input)
        {
            if (input.Kind == GridManipulationKind.Pan)
            {
                _state.HorizontalOffset = Math.Max(0, _state.HorizontalOffset - input.DeltaX);
                _state.VerticalOffset = Math.Max(0, _state.VerticalOffset - input.DeltaY);
            }

            _state.LastPointerX = input.X;
            _state.LastPointerY = input.Y;
        }

        private void HandleManipulationCompleted(GridManipulationCompletedInput input)
        {
            _state.IsManipulating = false;
            _state.LastPointerX = input.X;
            _state.LastPointerY = input.Y;
        }

        private void HandleEditCommand(GridEditCommandInput input)
        {
            if (input == null)
            {
                return;
            }

            switch (input.CommandKind)
            {
                case GridEditCommandKind.BeginEdit:
                    if (_state.CurrentCell != null)
                    {
                        StartEditingCell(_state.CurrentCell.RowKey, _state.CurrentCell.ColumnKey, GridEditStartMode.Programmatic);
                    }

                    break;
                case GridEditCommandKind.PostEdit:
                    if (_editState.IsInEditMode)
                    {
                        CommitEdit();
                    }
                    else
                    {
                        EditSessionContext?.PostActiveEdit();
                    }

                    break;
                case GridEditCommandKind.CancelEdit:
                    if (_editState.IsInEditMode)
                    {
                        CancelEdit();
                    }
                    else
                    {
                        EditSessionContext?.CancelActiveEdit();
                    }

                    break;
                default:
                    throw new NotSupportedException("Unsupported grid edit command: " + input.CommandKind);
            }
        }

        private void HandleEditorValueInput(GridEditorValueInput input)
        {
            if (input == null)
            {
                return;
            }

            var session = _editState.CurrentSession;
            if (session == null)
            {
                return;
            }

            if (!string.Equals(session.RowKey, input.RowKey, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(session.ColumnKey, input.ColumnKey, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            switch (input.ChangeKind)
            {
                case GridEditorValueChangeKind.TextEdited:
                    if (!CanAcceptEditingText(session.ColumnKey, input.Value))
                    {
                        return;
                    }

                    EnsureEditModel()?.SetText(input.Value);
                    break;

                case GridEditorValueChangeKind.SelectionCommitted:
                    if (!CanAcceptEditorSelection(session.ColumnKey, input.Value))
                    {
                        return;
                    }

                    EnsureEditModel()?.SetText(input.Value);
                    break;
            }
        }

        private void HandlePointerCanceled(GridPointerCanceledInput input)
        {
            _state.LastPointerX = input.X;
            _state.LastPointerY = input.Y;
            CancelPointerInteractionSession();
        }

        private void BeginHeaderInteraction(GridHitTestResult hit, GridPointerPressedInput input)
        {
            if (string.IsNullOrEmpty(hit?.HeaderKey) ||
                !hit.HeaderKind.HasValue ||
                input == null ||
                input.Button != GridMouseButton.Left)
            {
                return;
            }

            _state.PointerSession = GridPointerInteractionSession.BeginHeader(
                input.PointerId,
                input.PointerKind,
                input.Button,
                input.X,
                input.Y,
                hit.HeaderKey,
                hit.HeaderKind.Value);
        }

        private void BeginColumnResize(GridHitTestResult hit, GridPointerPressedInput input)
        {
            var resizeColumn = ResolveColumnResizeTarget(hit);
            if (resizeColumn == null)
            {
                return;
            }

            var resizeState = new GridColumnResizeState(
                resizeColumn.ColumnKey,
                Math.Max(resizeColumn.MinWidth, resizeColumn.Width),
                input.X,
                resizeColumn.MinWidth,
                resizeColumn.MaxWidth);
            _state.PointerSession = GridPointerInteractionSession.BeginResize(
                input.PointerId,
                input.PointerKind,
                input.Button,
                input.X,
                input.Y,
                resizeState);
        }

        private Layout.GridColumnDefinition ResolveColumnResizeTarget(GridHitTestResult hit)
        {
            if (hit == null || !hit.HeaderKind.HasValue || hit.HeaderKind.Value != GridHeaderKind.ColumnHeader)
            {
                return null;
            }

            if (_state.ColumnDefinitions.Count == 0)
            {
                return null;
            }

            var columnIndex = _state.ColumnDefinitions.FindIndex(candidate =>
                string.Equals(candidate.ColumnKey, hit.HeaderKey, StringComparison.OrdinalIgnoreCase));
            if (columnIndex < 0)
            {
                return null;
            }

            if (hit.Zone == GridHitZone.LeftEdge && columnIndex > 0)
            {
                return _state.ColumnDefinitions[columnIndex - 1];
            }

            return _state.ColumnDefinitions[columnIndex];
        }

        private void UpdateColumnResize(GridPointerInteractionSession session, double currentX)
        {
            var resize = session?.ResizeState;
            if (resize == null)
            {
                return;
            }

            session.Mode = GridPointerInteractionMode.ActiveResize;
            var nextWidth = resize.InitialWidth + (currentX - resize.OriginX);
            nextWidth = Math.Max(resize.MinWidth, Math.Min(resize.MaxWidth, nextWidth));

            ColumnResizeRequested?.Invoke(this, new GridColumnResizeRequestedEventArgs
            {
                ColumnKey = resize.ColumnKey,
                Width = nextWidth,
            });
        }

        private void UpdateColumnReorder(GridPointerInteractionSession session, double currentX, double currentY)
        {
            if (session == null || session.HeaderKind != GridHeaderKind.ColumnHeader)
            {
                return;
            }

            if (session.Mode == GridPointerInteractionMode.PendingActivation &&
                HasExceededHeaderGroupingDragThreshold(session, currentX, currentY))
            {
                ColumnGroupingDragRequested?.Invoke(this, new GridColumnGroupingDragRequestedEventArgs
                {
                    ColumnKey = session.HeaderKey,
                    PointerKind = session.PointerKind,
                    StartX = session.StartX,
                    StartY = session.StartY,
                    CurrentX = currentX,
                    CurrentY = currentY,
                });
                CancelPointerInteractionSession();
                return;
            }

            if (!HasExceededColumnHeaderDragThreshold(session, currentX))
            {
                return;
            }

            var targetColumnKey = ResolveReorderTargetColumnKey(currentX, currentY, session.HeaderKey);
            session.ReorderTargetColumnKey = targetColumnKey;
            session.Mode = string.IsNullOrWhiteSpace(targetColumnKey)
                ? GridPointerInteractionMode.PendingReorder
                : GridPointerInteractionMode.ActiveReorder;
        }

        private void TryActivateHeader(GridPointerInteractionSession session, GridPointerReleasedInput input)
        {
            if (session == null || string.IsNullOrEmpty(session.HeaderKey) || !session.HeaderKind.HasValue)
            {
                return;
            }

            var releaseHit = _hitTesting.HitTest(input.X, input.Y, _lastSnapshot);
            if (releaseHit?.TargetKind == GridHitTargetKind.Header &&
                releaseHit.HeaderKind.HasValue &&
                string.Equals(releaseHit.HeaderKey, session.HeaderKey, StringComparison.OrdinalIgnoreCase) &&
                releaseHit.HeaderKind.Value == session.HeaderKind.Value)
            {
                HeaderActivated?.Invoke(this, new GridHeaderActivatedEventArgs
                {
                    HeaderKey = releaseHit.HeaderKey,
                    HeaderKind = releaseHit.HeaderKind.Value,
                    Modifiers = input.Modifiers,
                });
                return;
            }

            if (session.HeaderKind.Value != GridHeaderKind.ColumnHeader)
            {
                return;
            }

            var pressedHeader = FindHeader(session.HeaderKey, session.HeaderKind.Value);
            if (pressedHeader == null ||
                !IsPointWithinBoundsWithTolerance(pressedHeader.Bounds, input.X, input.Y, HeaderActivationTolerance))
            {
                return;
            }

            HeaderActivated?.Invoke(this, new GridHeaderActivatedEventArgs
            {
                HeaderKey = session.HeaderKey,
                HeaderKind = session.HeaderKind.Value,
                Modifiers = input.Modifiers,
            });
        }

        private GridHeaderSurfaceItem FindHeader(string headerKey, GridHeaderKind headerKind)
        {
            if (_lastSnapshot?.Headers == null || string.IsNullOrWhiteSpace(headerKey))
            {
                return null;
            }

            return _lastSnapshot.Headers.FirstOrDefault(header =>
                header.Kind == headerKind &&
                string.Equals(header.HeaderKey, headerKey, StringComparison.OrdinalIgnoreCase));
        }

        private void TryCommitColumnReorder(GridPointerInteractionSession session, double currentX, double currentY)
        {
            if (session == null || session.HeaderKind != GridHeaderKind.ColumnHeader)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(session.ReorderTargetColumnKey))
            {
                session.ReorderTargetColumnKey = ResolveReorderTargetColumnKey(currentX, currentY, session.HeaderKey);
            }

            if (string.IsNullOrWhiteSpace(session.ReorderTargetColumnKey))
            {
                return;
            }

            ColumnReorderRequested?.Invoke(this, new GridColumnReorderRequestedEventArgs
            {
                ColumnKey = session.HeaderKey,
                TargetColumnKey = session.ReorderTargetColumnKey,
            });
        }

        private string ResolveReorderTargetColumnKey(double x, double y, string sourceHeaderKey)
        {
            var releaseHit = _hitTesting.HitTest(x, y, _lastSnapshot);
            if (releaseHit?.TargetKind == GridHitTargetKind.Header &&
                releaseHit.HeaderKind.HasValue &&
                releaseHit.HeaderKind.Value == GridHeaderKind.ColumnHeader &&
                !string.IsNullOrWhiteSpace(releaseHit.HeaderKey) &&
                !string.Equals(releaseHit.HeaderKey, sourceHeaderKey, StringComparison.OrdinalIgnoreCase))
            {
                return releaseHit.HeaderKey;
            }

            if (_lastSnapshot == null)
            {
                return null;
            }

            var candidate = _lastSnapshot.Headers
                .Where(header =>
                    header.Kind == GridHeaderKind.ColumnHeader &&
                    !string.IsNullOrWhiteSpace(header.HeaderKey) &&
                    !string.Equals(header.HeaderKey, sourceHeaderKey, StringComparison.OrdinalIgnoreCase))
                .Select(header => new
                {
                    Header = header,
                    HorizontalDistance = ComputeAxisDistance(x, header.Bounds.Left, header.Bounds.Right),
                    VerticalDistance = ComputeAxisDistance(y, header.Bounds.Top, header.Bounds.Bottom),
                })
                .Where(entry =>
                    entry.HorizontalDistance <= HeaderDropHorizontalTolerance &&
                    entry.VerticalDistance <= HeaderDropVerticalTolerance)
                .OrderBy(entry => entry.HorizontalDistance + entry.VerticalDistance)
                .ThenBy(entry => Math.Abs(x - (entry.Header.Bounds.Left + (entry.Header.Bounds.Width / 2d))))
                .FirstOrDefault();

            return candidate?.Header.HeaderKey;
        }

        private static double ComputeAxisDistance(double value, double start, double end)
        {
            if (value < start)
            {
                return start - value;
            }

            if (value > end)
            {
                return value - end;
            }

            return 0d;
        }

        private static bool HasExceededColumnHeaderDragThreshold(GridPointerInteractionSession session, double currentX)
        {
            if (session == null)
            {
                return false;
            }

            return Math.Abs(currentX - session.StartX) >= HeaderDragThreshold;
        }

        private static bool HasExceededHeaderGroupingDragThreshold(GridPointerInteractionSession session, double currentX, double currentY)
        {
            if (session == null)
            {
                return false;
            }

            var upwardDistance = session.StartY - currentY;
            if (upwardDistance < HeaderGroupingVerticalDragThreshold)
            {
                return false;
            }

            var horizontalDistance = Math.Abs(currentX - session.StartX);
            return upwardDistance > horizontalDistance;
        }

        private static bool IsPointWithinBoundsWithTolerance(GridBounds bounds, double x, double y, double tolerance)
        {
            return x >= bounds.Left - tolerance &&
                x <= bounds.Right + tolerance &&
                y >= bounds.Top - tolerance &&
                y <= bounds.Bottom + tolerance;
        }

        private static bool DoesSessionMatch(GridPointerInteractionSession session, int pointerId, GridPointerKind pointerKind)
        {
            return session != null &&
                session.PointerId == pointerId &&
                session.PointerKind == pointerKind &&
                session.Mode != GridPointerInteractionMode.None;
        }

        private static bool IsRowUtilityHeaderKind(GridHeaderKind kind)
        {
            return kind == GridHeaderKind.RowHeader || kind == GridHeaderKind.RowNumberHeader;
        }

        private void CancelPointerInteractionSession()
        {
            _state.PointerSession = null;
        }

        private void ActivateCell(string rowKey, string columnKey, GridPointerPressedInput input)
        {
            if (string.IsNullOrEmpty(rowKey) || string.IsNullOrEmpty(columnKey))
            {
                return;
            }

            if (!RepresentsDataRecord(rowKey))
            {
                return;
            }

            _state.CurrentCell = new GridCurrentCellCoordinate(rowKey, columnKey);

            if (!_editState.IsInEditMode)
            {
                if (_state.SelectionMode == GridSelectionMode.Row)
                {
                    UpdateRowSelection(rowKey, input);
                }
                else if (_state.SelectionMode == GridSelectionMode.Cell || _state.SelectionMode == GridSelectionMode.Mixed)
                {
                    UpdateCellSelection(rowKey, columnKey, input);
                }
            }

            if (EditActivationMode == GridEditActivationMode.DirectInteraction &&
                input.ClickCount >= 2 &&
                !IsEditingCell(rowKey, columnKey))
            {
                StartEditingCell(rowKey, columnKey, GridEditStartMode.DoubleClick);
            }
        }

        private void ActivateRow(string rowKey, GridPointerPressedInput input)
        {
            if (string.IsNullOrWhiteSpace(rowKey))
            {
                return;
            }

            if (!RepresentsDataRecord(rowKey))
            {
                return;
            }

            var currentColumnKey = _state.CurrentCell?.ColumnKey;
            if (string.IsNullOrWhiteSpace(currentColumnKey) ||
                _state.ColumnDefinitions.All(column => !string.Equals(column.ColumnKey, currentColumnKey, StringComparison.OrdinalIgnoreCase)))
            {
                currentColumnKey = _state.ColumnDefinitions.FirstOrDefault()?.ColumnKey;
            }

            if (!string.IsNullOrWhiteSpace(currentColumnKey))
            {
                _state.CurrentCell = new GridCurrentCellCoordinate(rowKey, currentColumnKey);
            }

            if (!_editState.IsInEditMode)
            {
                if (_state.SelectionMode == GridSelectionMode.Row)
                {
                    UpdateRowSelection(rowKey, input);
                }
                else if (_state.SelectionMode == GridSelectionMode.Cell || _state.SelectionMode == GridSelectionMode.Mixed)
                {
                    UpdateWholeRowCellSelection(rowKey, input);
                }
            }
        }

        private void UpdateCellSelection(string rowKey, string columnKey, GridPointerPressedInput input)
        {
            if (!_state.EnableCellSelection)
            {
                return;
            }

            var cellKey = rowKey + "_" + columnKey;
            _state.SelectedRowKeys.Clear();
            if (input.HasShift && _state.EnableRangeSelection && TryApplyCellRangeSelection(rowKey, columnKey))
            {
                return;
            }

            if (input.HasControl && _state.EnableRangeSelection)
            {
                if (_state.SelectedCellKeys.Contains(cellKey))
                {
                    _state.SelectedCellKeys.Remove(cellKey);
                }
                else
                {
                    _state.SelectedCellKeys.Add(cellKey);
                }

                _state.SelectionAnchorCell = new GridCurrentCellCoordinate(rowKey, columnKey);
                return;
            }

            _state.SelectedCellKeys.Clear();
            _state.SelectedCellKeys.Add(cellKey);
            _state.SelectionAnchorCell = new GridCurrentCellCoordinate(rowKey, columnKey);
        }

        private void UpdateRowSelection(string rowKey, GridPointerPressedInput input)
        {
            _ = input;
            _state.SelectedCellKeys.Clear();
            _state.SelectedRowKeys.Clear();
            _state.SelectedRowKeys.Add(rowKey);
            var currentColumnKey = _state.CurrentCell?.ColumnKey ?? _state.ColumnDefinitions.FirstOrDefault()?.ColumnKey;
            if (!string.IsNullOrWhiteSpace(currentColumnKey))
            {
                _state.SelectionAnchorCell = new GridCurrentCellCoordinate(rowKey, currentColumnKey);
            }
        }

        private void UpdateWholeRowCellSelection(string rowKey, GridPointerPressedInput input)
        {
            if (!_state.EnableCellSelection)
            {
                return;
            }

            var rowCellKeys = EnumerateRowCellKeys(rowKey).ToArray();
            if (rowCellKeys.Length == 0)
            {
                return;
            }

            _state.SelectedRowKeys.Clear();
            if (input.HasShift && _state.EnableRangeSelection && TryApplyWholeRowRangeSelection(rowKey))
            {
                return;
            }

            if (input.HasControl && _state.EnableRangeSelection)
            {
                var allSelected = rowCellKeys.All(cellKey => _state.SelectedCellKeys.Contains(cellKey));
                foreach (var cellKey in rowCellKeys)
                {
                    if (allSelected)
                    {
                        _state.SelectedCellKeys.Remove(cellKey);
                    }
                    else
                    {
                        _state.SelectedCellKeys.Add(cellKey);
                    }
                }

                var anchorColumnKey = _state.ColumnDefinitions.FirstOrDefault()?.ColumnKey;
                if (!string.IsNullOrWhiteSpace(anchorColumnKey))
                {
                    _state.SelectionAnchorCell = new GridCurrentCellCoordinate(rowKey, anchorColumnKey);
                }
                return;
            }

            _state.SelectedCellKeys.Clear();
            foreach (var cellKey in rowCellKeys)
            {
                _state.SelectedCellKeys.Add(cellKey);
            }

            var firstColumnKey = _state.ColumnDefinitions.FirstOrDefault()?.ColumnKey;
            if (!string.IsNullOrWhiteSpace(firstColumnKey))
            {
                _state.SelectionAnchorCell = new GridCurrentCellCoordinate(rowKey, firstColumnKey);
            }
        }

        private IEnumerable<string> EnumerateRowCellKeys(string rowKey)
        {
            foreach (var column in _state.ColumnDefinitions)
            {
                yield return rowKey + "_" + column.ColumnKey;
            }
        }

        private bool TryApplyCellRangeSelection(string rowKey, string columnKey)
        {
            if (_state.SelectionAnchorCell == null)
            {
                return false;
            }

            if (!TryResolveCellIndices(_state.SelectionAnchorCell.RowKey, _state.SelectionAnchorCell.ColumnKey, out var anchorRowIndex, out var anchorColumnIndex) ||
                !TryResolveCellIndices(rowKey, columnKey, out var targetRowIndex, out var targetColumnIndex))
            {
                return false;
            }

            _state.SelectedCellKeys.Clear();
            var startRow = Math.Min(anchorRowIndex, targetRowIndex);
            var endRow = Math.Max(anchorRowIndex, targetRowIndex);
            var startColumn = Math.Min(anchorColumnIndex, targetColumnIndex);
            var endColumn = Math.Max(anchorColumnIndex, targetColumnIndex);

            for (var rowIndex = startRow; rowIndex <= endRow; rowIndex++)
            {
                var currentRowKey = _state.RowDefinitions[rowIndex].RowKey;
                if (!RepresentsDataRecord(currentRowKey))
                {
                    continue;
                }

                for (var columnIndex = startColumn; columnIndex <= endColumn; columnIndex++)
                {
                    _state.SelectedCellKeys.Add(currentRowKey + "_" + _state.ColumnDefinitions[columnIndex].ColumnKey);
                }
            }

            return true;
        }

        private bool TryApplyWholeRowRangeSelection(string rowKey)
        {
            if (_state.SelectionAnchorCell == null)
            {
                return false;
            }

            if (!TryResolveRowIndex(_state.SelectionAnchorCell.RowKey, out var anchorRowIndex) ||
                !TryResolveRowIndex(rowKey, out var targetRowIndex))
            {
                return false;
            }

            _state.SelectedCellKeys.Clear();
            var startRow = Math.Min(anchorRowIndex, targetRowIndex);
            var endRow = Math.Max(anchorRowIndex, targetRowIndex);
            for (var rowIndex = startRow; rowIndex <= endRow; rowIndex++)
            {
                var currentRowKey = _state.RowDefinitions[rowIndex].RowKey;
                if (!RepresentsDataRecord(currentRowKey))
                {
                    continue;
                }

                foreach (var cellKey in EnumerateRowCellKeys(currentRowKey))
                {
                    _state.SelectedCellKeys.Add(cellKey);
                }
            }

            return true;
        }

        private bool TryResolveCellIndices(string rowKey, string columnKey, out int rowIndex, out int columnIndex)
        {
            rowIndex = -1;
            columnIndex = -1;

            return TryResolveRowIndex(rowKey, out rowIndex) &&
                TryResolveColumnIndex(columnKey, out columnIndex);
        }

        private bool TryResolveRowIndex(string rowKey, out int rowIndex)
        {
            rowIndex = _state.RowDefinitions.FindIndex(row =>
                string.Equals(row.RowKey, rowKey, StringComparison.OrdinalIgnoreCase));
            return rowIndex >= 0;
        }

        private bool TryResolveColumnIndex(string columnKey, out int columnIndex)
        {
            columnIndex = _state.ColumnDefinitions.FindIndex(column =>
                string.Equals(column.ColumnKey, columnKey, StringComparison.OrdinalIgnoreCase));
            return columnIndex >= 0;
        }

        private void HandleCharacterInput(GridKeyInput input)
        {
            if (EditActivationMode != GridEditActivationMode.DirectInteraction)
            {
                return;
            }

            if (!input.Character.HasValue)
            {
                return;
            }

            var editModel = EnsureEditModel();
            if (_editState.IsInEditMode)
            {
                var currentSession = _editState.CurrentSession;
                if (currentSession != null && IsRestrictToItemsColumn(currentSession.ColumnKey))
                {
                    return;
                }

                editModel?.AppendText(input.Character.Value.ToString());
                return;
            }

            if (IsRestrictToItemsColumn(_state.CurrentCell?.ColumnKey))
            {
                return;
            }

            if (StartEditingCell(_state.CurrentCell.RowKey, _state.CurrentCell.ColumnKey, GridEditStartMode.ReplaceMode))
            {
                editModel = EnsureEditModel();
                editModel?.Clear();
                editModel?.AppendText(input.Character.Value.ToString());
            }
        }

        private void NavigateLeft()
        {
            if (_state.CurrentCell == null)
            {
                return;
            }

            var colIdx = _state.ColumnDefinitions.FindIndex(c => c.ColumnKey == _state.CurrentCell.ColumnKey);
            if (colIdx > 0)
            {
                var newColKey = _state.ColumnDefinitions[colIdx - 1].ColumnKey;
                SetCurrentCellCoordinate(_state.CurrentCell.RowKey, newColKey, ensureVisible: true);
                EnsureCurrentCellVisible();
            }
        }

        private void NavigateRight()
        {
            if (_state.CurrentCell == null)
            {
                return;
            }

            var colIdx = _state.ColumnDefinitions.FindIndex(c => c.ColumnKey == _state.CurrentCell.ColumnKey);
            if (colIdx >= 0 && colIdx < _state.ColumnDefinitions.Count - 1)
            {
                var newColKey = _state.ColumnDefinitions[colIdx + 1].ColumnKey;
                SetCurrentCellCoordinate(_state.CurrentCell.RowKey, newColKey, ensureVisible: true);
                EnsureCurrentCellVisible();
            }
        }

        private void NavigateUp()
        {
            if (_state.CurrentCell == null)
            {
                return;
            }

            var rowIdx = _state.RowDefinitions.FindIndex(r => r.RowKey == _state.CurrentCell.RowKey);
            if (rowIdx > 0)
            {
                var newRowKey = _state.RowDefinitions[rowIdx - 1].RowKey;
                SetCurrentCellCoordinate(newRowKey, _state.CurrentCell.ColumnKey, ensureVisible: true);
                EnsureCurrentCellVisible();
            }
        }

        private void NavigateDown()
        {
            if (_state.CurrentCell == null)
            {
                return;
            }

            var rowIdx = _state.RowDefinitions.FindIndex(r => r.RowKey == _state.CurrentCell.RowKey);
            if (rowIdx >= 0 && rowIdx < _state.RowDefinitions.Count - 1)
            {
                var newRowKey = _state.RowDefinitions[rowIdx + 1].RowKey;
                SetCurrentCellCoordinate(newRowKey, _state.CurrentCell.ColumnKey, ensureVisible: true);
                EnsureCurrentCellVisible();
            }
        }

        private bool StartEditingCell(string rowKey, string columnKey, GridEditStartMode startMode)
        {
            var editModel = EnsureEditModel();
            if (editModel == null || !CanEditCell(rowKey, columnKey))
            {
                return false;
            }

            if (IsEditingCell(rowKey, columnKey))
            {
                SetCurrentCellCoordinate(rowKey, columnKey, ensureVisible: false);
                EnsureEditingCellVisible();
                return true;
            }

            SetCurrentCellCoordinate(rowKey, columnKey, ensureVisible: false);
            EnsureEditingCellVisible();
            var started = editModel.StartSession(rowKey, columnKey, startMode);
            if (started)
            {
                EditSessionContext?.BeginFieldEdit(rowKey, columnKey);
            }

            return started;
        }

        private bool TryToggleCheckBoxCell(string rowKey, string columnKey)
        {
            if (EditActivationMode != GridEditActivationMode.DirectInteraction ||
                !IsCheckBoxColumn(columnKey) ||
                !CanEditCell(rowKey, columnKey))
            {
                return false;
            }

            var editModel = EnsureEditModel();
            if (editModel == null || !StartEditingCell(rowKey, columnKey, GridEditStartMode.Enter))
            {
                return false;
            }

            var currentValue = _editState.CurrentSession?.OriginalValue;
            var nextValue = !(currentValue is bool boolValue && boolValue);
            if (!editModel.SetText(nextValue ? bool.TrueString : bool.FalseString))
            {
                return false;
            }

            return editModel.Commit();
        }

        private bool TryResolveScrollableTarget(string rowKey, string columnKey, out GridScrollTarget target)
        {
            target = default;

            var layout = _layoutEngine.ComputeLayout(
                _state.ColumnDefinitions,
                _state.RowDefinitions,
                _state.FrozenColumnCount,
                _state.FrozenRowCount);

            var frozenDataWidth = layout.ColumnLayouts.Where(column => column.IsFrozen).Sum(column => column.Width);
            var frozenDataHeight = layout.RowLayouts.Where(row => row.IsFrozen).Sum(row => row.Height);
            var scrollableViewportWidth = Math.Max(0, _state.ViewportWidth - RowHeaderWidth - frozenDataWidth);
            var dataTopInset = double.IsNaN(DataTopInset)
                ? ColumnHeaderHeight + FilterRowHeight
                : Math.Max(0d, DataTopInset);
            var scrollableViewportHeight = Math.Max(0, _state.ViewportHeight - dataTopInset - frozenDataHeight);
            var maxHorizontalOffset = Math.Max(0, (layout.TotalWidth - frozenDataWidth) - scrollableViewportWidth);
            var maxVerticalOffset = Math.Max(0, (layout.TotalHeight - frozenDataHeight) - scrollableViewportHeight);

            GridRowLayout rowLayout = null;
            if (!string.IsNullOrWhiteSpace(rowKey))
            {
                if (!RepresentsDataRecord(rowKey))
                {
                    return false;
                }

                rowLayout = layout.RowLayouts.FirstOrDefault(candidate =>
                    string.Equals(candidate.RowKey, rowKey, StringComparison.OrdinalIgnoreCase));
                if (rowLayout == null)
                {
                    return false;
                }
            }

            GridColumnLayout columnLayout = null;
            if (!string.IsNullOrWhiteSpace(columnKey))
            {
                columnLayout = layout.ColumnLayouts.FirstOrDefault(candidate =>
                    string.Equals(candidate.ColumnKey, columnKey, StringComparison.OrdinalIgnoreCase));
                if (columnLayout == null)
                {
                    return false;
                }
            }

            target = new GridScrollTarget(
                rowLayout,
                columnLayout,
                frozenDataWidth,
                frozenDataHeight,
                scrollableViewportWidth,
                scrollableViewportHeight,
                maxHorizontalOffset,
                maxVerticalOffset);
            return true;
        }

        private void ApplyScrollTarget(GridScrollTarget target, bool scrollHorizontally, bool scrollVertically, GridScrollAlignment alignment)
        {
            if (scrollHorizontally && target.Column != null && !target.Column.IsFrozen)
            {
                var targetStart = Math.Max(0d, target.Column.X - target.FrozenDataWidth);
                var targetEnd = Math.Max(targetStart, target.Column.Right - target.FrozenDataWidth);
                _state.HorizontalOffset = ResolveOffsetForTarget(
                    _state.HorizontalOffset,
                    target.ScrollableViewportWidth,
                    targetStart,
                    targetEnd,
                    target.MaxHorizontalOffset,
                    alignment);
            }

            if (scrollVertically && target.Row != null && !target.Row.IsFrozen)
            {
                var targetStart = Math.Max(0d, target.Row.Y - target.FrozenDataHeight);
                var targetEnd = Math.Max(targetStart, target.Row.Bottom - target.FrozenDataHeight);
                _state.VerticalOffset = ResolveOffsetForTarget(
                    _state.VerticalOffset,
                    target.ScrollableViewportHeight,
                    targetStart,
                    targetEnd,
                    target.MaxVerticalOffset,
                    alignment);
            }
        }

        private static double ResolveOffsetForTarget(
            double currentOffset,
            double viewportSize,
            double targetStart,
            double targetEnd,
            double maxOffset,
            GridScrollAlignment alignment)
        {
            var safeMaxOffset = Math.Max(0d, maxOffset);
            if (viewportSize <= 0d)
            {
                return ClampOffset(currentOffset, safeMaxOffset);
            }

            var targetSize = Math.Max(0d, targetEnd - targetStart);
            double nextOffset;
            switch (alignment)
            {
                case GridScrollAlignment.Start:
                    nextOffset = targetStart;
                    break;
                case GridScrollAlignment.Center:
                    nextOffset = targetStart - ((viewportSize - targetSize) / 2d);
                    break;
                case GridScrollAlignment.End:
                    nextOffset = targetEnd - viewportSize;
                    break;
                default:
                    var viewportEnd = currentOffset + viewportSize;
                    if (targetSize <= viewportSize &&
                        targetStart >= currentOffset &&
                        targetEnd <= viewportEnd)
                    {
                        nextOffset = currentOffset;
                    }
                    else if (targetSize > viewportSize || targetStart < currentOffset)
                    {
                        // Dla targetu wiekszego od viewportu zachowujemy deterministyczna regule: wyrownanie do poczatku.
                        nextOffset = targetStart;
                    }
                    else
                    {
                        nextOffset = targetEnd - viewportSize;
                    }

                    break;
            }

            return ClampOffset(nextOffset, safeMaxOffset);
        }

        private bool TryCompleteActiveEditBeforePointerAction(GridHitTestResult hit)
        {
            if (!_editState.IsInEditMode)
            {
                return true;
            }

            var session = _editState.CurrentSession;
            if (session == null)
            {
                _editState.IsInEditMode = false;
                return true;
            }

            if (IsHitOnEditingCell(hit, session))
            {
                SetCurrentCellCoordinate(session.RowKey, session.ColumnKey);
                return true;
            }

            if (EditActivationMode != GridEditActivationMode.DirectInteraction)
            {
                SetCurrentCellCoordinate(session.RowKey, session.ColumnKey);
                return false;
            }

            var committed = CommitEdit();
            if (!committed)
            {
                SetCurrentCellCoordinate(session.RowKey, session.ColumnKey);
                return false;
            }

            return true;
        }

        private bool TryCompleteActiveEditBeforeNavigation()
        {
            if (!_editState.IsInEditMode)
            {
                return true;
            }

            if (EditActivationMode != GridEditActivationMode.DirectInteraction)
            {
                return false;
            }

            return CommitEdit();
        }

        private bool TryHandleEditingAdvance(bool moveBackward, bool commitLastCellForDirectMode)
        {
            if (!_editState.IsInEditMode)
            {
                return false;
            }

            if (_state.CurrentCell == null)
            {
                return true;
            }

            if (EditActivationMode != GridEditActivationMode.DirectInteraction)
            {
                return true;
            }

            if (TryResolveNextEditableCellInRow(_state.CurrentCell.RowKey, _state.CurrentCell.ColumnKey, moveBackward, out var nextCell))
            {
                if (!CommitEdit())
                {
                    return true;
                }

                _state.CurrentCell = nextCell;
                StartEditingCell(nextCell.RowKey, nextCell.ColumnKey, GridEditStartMode.Programmatic);
                return true;
            }

            if (commitLastCellForDirectMode)
            {
                CommitEdit();
                return true;
            }

            return true;
        }

        private bool IsEditingCell(string rowKey, string columnKey)
        {
            var session = _editState.CurrentSession;
            return _editState.IsInEditMode &&
                session != null &&
                string.Equals(session.RowKey, rowKey, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(session.ColumnKey, columnKey, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsHitOnEditingCell(GridHitTestResult hit, GridEditSession session)
        {
            if (hit == null || session == null)
            {
                return false;
            }

            return (hit.TargetKind == GridHitTargetKind.Cell || hit.TargetKind == GridHitTargetKind.CurrentCellMarker) &&
                string.Equals(hit.RowKey, session.RowKey, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(hit.ColumnKey, session.ColumnKey, StringComparison.OrdinalIgnoreCase);
        }

        private void SetCurrentCellCoordinate(string rowKey, string columnKey, bool ensureVisible = false)
        {
            _state.CurrentCell = new GridCurrentCellCoordinate(rowKey, columnKey);
            if (ensureVisible)
            {
                _state.PendingEnsureCurrentCellVisible = true;
            }
        }

        private void EnsureCurrentCellVisible()
        {
            EnsureCurrentCellVisible(GridScrollAlignment.Nearest, GridScrollAlignment.Nearest, force: false);
        }

        private void EnsureEditingCellVisible()
        {
            if (_state.CurrentCell == null)
            {
                _state.PendingEnsureCurrentCellVisible = false;
                return;
            }

            if (TryResolveScrollableTarget(_state.CurrentCell.RowKey, _state.CurrentCell.ColumnKey, out var target))
            {
                ApplyEditingScrollTarget(target);
            }

            _state.PendingEnsureCurrentCellVisible = false;
        }

        private void EnsureCurrentCellVisible(
            GridScrollAlignment horizontalAlignment,
            GridScrollAlignment verticalAlignment,
            bool force)
        {
            if ((!force && !_state.PendingEnsureCurrentCellVisible) || _state.CurrentCell == null)
            {
                return;
            }

            if (TryResolveScrollableTarget(_state.CurrentCell.RowKey, _state.CurrentCell.ColumnKey, out var target))
            {
                ApplyScrollTarget(target, scrollHorizontally: true, scrollVertically: false, horizontalAlignment);
                ApplyScrollTarget(target, scrollHorizontally: false, scrollVertically: true, verticalAlignment);
            }

            _state.PendingEnsureCurrentCellVisible = false;
        }

        private void ApplyEditingScrollTarget(GridScrollTarget target)
        {
            if (target.Column != null && !target.Column.IsFrozen)
            {
                var targetStart = Math.Max(0d, target.Column.X - target.FrozenDataWidth);
                var targetEnd = Math.Max(targetStart, target.Column.Right - target.FrozenDataWidth);
                var nearestHorizontalOffset = ResolveOffsetForTarget(
                    _state.HorizontalOffset,
                    target.ScrollableViewportWidth,
                    targetStart,
                    targetEnd,
                    target.MaxHorizontalOffset,
                    GridScrollAlignment.Nearest);

                _state.HorizontalOffset = Math.Abs(nearestHorizontalOffset - _state.HorizontalOffset) < 0.1d
                    ? _state.HorizontalOffset
                    : ResolveOffsetForTarget(
                        _state.HorizontalOffset,
                        target.ScrollableViewportWidth,
                        targetStart,
                        targetEnd,
                        target.MaxHorizontalOffset,
                        GridScrollAlignment.Center);
            }

            ApplyScrollTarget(target, scrollHorizontally: false, scrollVertically: true, GridScrollAlignment.Nearest);
        }

        private GridEditModel EnsureEditModel()
        {
            var accessor = EditCellAccessor ?? CellValueProvider as IGridEditCellAccessor;
            if (accessor == null)
            {
                return null;
            }

            if (_editModel == null ||
                !ReferenceEquals(_editAccessorForModel, accessor) ||
                !ReferenceEquals(_editValidatorForModel, EditValidator) ||
                !ReferenceEquals(_editValueParserForModel, EditValueParser))
            {
                _editModel = new GridEditModel(accessor, EditValidator, EditValueParser, state: _editState);
                _editAccessorForModel = accessor;
                _editValidatorForModel = EditValidator;
                _editValueParserForModel = EditValueParser;
            }

            return _editModel;
        }

        private void RequestFocusIfNeeded()
        {
            if (_state.HasFocus || _state.IsFocusRequestPending)
            {
                return;
            }

            _state.IsFocusRequestPending = true;
            FocusRequested?.Invoke(this, new GridFocusRequestEventArgs
            {
                TargetKind = GridFocusTargetKind.Grid,
            });
        }

        private void UpdateSnapshot()
        {
            if (_snapshotUpdateBatchDepth > 0)
            {
                _hasPendingSnapshotUpdate = true;
                return;
            }

            UpdateSnapshotCore();
        }

        private void CompleteSnapshotUpdateBatch()
        {
            if (_snapshotUpdateBatchDepth <= 0)
            {
                throw new InvalidOperationException("Snapshot update batch was completed without an active batch.");
            }

            _snapshotUpdateBatchDepth--;
            if (_snapshotUpdateBatchDepth > 0 || !_hasPendingSnapshotUpdate)
            {
                return;
            }

            _hasPendingSnapshotUpdate = false;
            UpdateSnapshotCore();
        }

        private void UpdateSnapshotCore()
        {
            SyncCurrentRecordToEditSessionContext();

            var layout = _layoutEngine.ComputeLayout(
                _state.ColumnDefinitions,
                _state.RowDefinitions,
                _state.FrozenColumnCount,
                _state.FrozenRowCount);

            var frozenDataWidth = layout.ColumnLayouts.Where(column => column.IsFrozen).Sum(column => column.Width);
            var frozenDataHeight = layout.RowLayouts.Where(row => row.IsFrozen).Sum(row => row.Height);
            var scrollableViewportWidth = Math.Max(0, _state.ViewportWidth - RowHeaderWidth - frozenDataWidth);
            var dataTopInset = double.IsNaN(DataTopInset)
                ? ColumnHeaderHeight + FilterRowHeight
                : Math.Max(0d, DataTopInset);
            var scrollableViewportHeight = Math.Max(0, _state.ViewportHeight - dataTopInset - frozenDataHeight);
            var maxHorizontalOffset = Math.Max(0, (layout.TotalWidth - frozenDataWidth) - scrollableViewportWidth);
            var maxVerticalOffset = Math.Max(0, (layout.TotalHeight - frozenDataHeight) - scrollableViewportHeight);

            _state.HorizontalOffset = ClampOffset(_state.HorizontalOffset, maxHorizontalOffset);
            _state.VerticalOffset = ClampOffset(_state.VerticalOffset, maxVerticalOffset);

            var rowRange = _viewportCalculator.CalculateRowRange(
                _state.VerticalOffset + frozenDataHeight,
                scrollableViewportHeight,
                layout.RowLayouts.ToList());

            var columnRange = _viewportCalculator.CalculateColumnRange(
                _state.HorizontalOffset + frozenDataWidth,
                scrollableViewportWidth,
                layout.ColumnLayouts.ToList());

            var context = new GridSurfaceBuildContext
            {
                ColumnDefinitions = _state.ColumnDefinitions,
                RowDefinitions = _state.RowDefinitions,
                ColumnLayouts = layout.ColumnLayouts,
                RowLayouts = layout.RowLayouts,
                HorizontalOffset = _state.HorizontalOffset,
                VerticalOffset = _state.VerticalOffset,
                ViewportWidth = _state.ViewportWidth,
                ViewportHeight = _state.ViewportHeight,
                RowRealizationRange = rowRange,
                ColumnRealizationRange = columnRange,
                SelectedRowKeys = _state.SelectedRowKeys,
                SelectedCellKeys = _state.SelectedCellKeys,
                SelectionMode = _state.SelectionMode,
                CurrentCell = _state.CurrentCell,
                HasFocus = _state.HasFocus,
                IsInEditMode = _editState.IsInEditMode,
                ShowCurrentRecordIndicator = ShowCurrentRecordIndicator,
                SelectCurrentRow = _state.SelectCurrentRow,
                MultiSelect = _state.MultiSelect,
                ShowRowNumbers = _state.ShowRowNumbers,
                RowNumberingMode = _state.RowNumberingMode,
                RowActionWidth = _state.RowActionWidth,
                RowIndicatorWidth = _state.RowIndicatorWidth,
                RowMarkerWidth = _state.RowMarkerWidth,
                SelectionCheckboxWidth = _state.SelectionCheckboxWidth,
                EditingSession = _editState.CurrentSession,
                FrozenColumnCount = _state.FrozenColumnCount,
                FrozenRowCount = _state.FrozenRowCount,
                ColumnHeaderHeight = ColumnHeaderHeight,
                FilterRowHeight = FilterRowHeight,
                DataTopInset = DataTopInset,
                RowHeaderWidth = RowHeaderWidth,
                CellValueProvider = CellValueProvider,
                Sorts = _state.SortDescriptors,
                EditedRowKeys = _state.EditedRowKeys,
                InvalidRowKeys = ResolveSnapshotInvalidRowKeys(),
                CheckedRowKeys = _state.CheckedRowKeys,
                RowIndicatorToolTips = _state.RowIndicatorToolTips,
                StateProjection = _state.StateProjection,
                CellValidationErrors = _editModel?.ValidationErrors.ToDictionary(
                    pair => pair.Key,
                    pair => pair.Value.FirstOrDefault()?.Message),
            };

            _lastSnapshot = _surfaceBuilder.BuildSnapshot(context);
            SnapshotChanged?.Invoke(this, new GridSnapshotChangedEventArgs { Snapshot = _lastSnapshot });
        }

        private void SyncCurrentRecordToEditSessionContext()
        {
            if (EditSessionContext == null)
            {
                return;
            }

            var currentRowKey = _state.CurrentCell?.RowKey;
            if (string.IsNullOrWhiteSpace(currentRowKey) || !RepresentsDataRecord(currentRowKey))
            {
                EditSessionContext.ClearCurrentRecord();
                return;
            }

            EditSessionContext.SetCurrentRecord(currentRowKey);
        }

        private ISet<string> ResolveSnapshotInvalidRowKeys()
        {
            if (_editModel?.ValidationErrors == null || _editModel.ValidationErrors.Count == 0 || _editState.CurrentSession == null)
            {
                return _state.InvalidRowKeys;
            }

            return new HashSet<string>(
                _state.InvalidRowKeys.Concat(new[] { _editState.CurrentSession.RowKey }),
                StringComparer.OrdinalIgnoreCase);
        }

        private static double ClampOffset(double offset, double maxOffset)
        {
            return Math.Min(Math.Max(0, offset), Math.Max(0, maxOffset));
        }

        private void RequestRowAction(string rowKey, GridRowActionKind actionKind)
        {
            if (string.IsNullOrWhiteSpace(rowKey))
            {
                return;
            }

            RowActionRequested?.Invoke(this, new GridRowActionRequestedEventArgs
            {
                RowKey = rowKey,
                ActionKind = actionKind,
            });
        }

        private bool CanEditCell(string rowKey, string columnKey)
        {
            var row = _state.RowDefinitions.FirstOrDefault(candidate =>
                string.Equals(candidate.RowKey, rowKey, StringComparison.OrdinalIgnoreCase));
            if (row == null || row.IsReadOnly || row.IsGroupHeader)
            {
                return false;
            }

            var column = _state.ColumnDefinitions.FirstOrDefault(candidate =>
                string.Equals(candidate.ColumnKey, columnKey, StringComparison.OrdinalIgnoreCase));
            return column != null && !column.IsReadOnly;
        }

        private bool IsCheckBoxColumn(string columnKey)
        {
            if (string.IsNullOrWhiteSpace(columnKey))
            {
                return false;
            }

            var column = FindColumnDefinition(columnKey);
            return column != null && column.EditorKind == GridColumnEditorKind.CheckBox;
        }

        private bool IsRestrictToItemsColumn(string columnKey)
        {
            var column = FindColumnDefinition(columnKey);
            return column != null && column.EditorItemsMode == GridEditorItemsMode.RestrictToItems;
        }

        private bool CanAcceptEditingText(string columnKey, string text)
        {
            var column = FindColumnDefinition(columnKey);
            if (column == null || column.EditorItemsMode != GridEditorItemsMode.RestrictToItems)
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                return true;
            }

            return column.EditorItems != null &&
                column.EditorItems.Contains(text, StringComparer.Ordinal);
        }

        private bool CanAcceptEditorSelection(string columnKey, string value)
        {
            var column = FindColumnDefinition(columnKey);
            if (column == null)
            {
                return false;
            }

            if (column.EditorItemsMode != GridEditorItemsMode.RestrictToItems)
            {
                return true;
            }

            return column.EditorItems != null &&
                column.EditorItems.Contains(value ?? string.Empty, StringComparer.Ordinal);
        }

        private GridColumnDefinition FindColumnDefinition(string columnKey)
        {
            if (string.IsNullOrWhiteSpace(columnKey))
            {
                return null;
            }

            return _state.ColumnDefinitions.FirstOrDefault(candidate =>
                string.Equals(candidate.ColumnKey, columnKey, StringComparison.OrdinalIgnoreCase));
        }

        private bool TryResolveNextEditableCellInRow(string rowKey, string columnKey, bool moveBackward, out GridCurrentCellCoordinate nextCell)
        {
            nextCell = null;
            if (string.IsNullOrWhiteSpace(rowKey) || string.IsNullOrWhiteSpace(columnKey))
            {
                return false;
            }

            var currentColumnIndex = _state.ColumnDefinitions.FindIndex(candidate =>
                string.Equals(candidate.ColumnKey, columnKey, StringComparison.OrdinalIgnoreCase));
            if (currentColumnIndex < 0)
            {
                return false;
            }

            var step = moveBackward ? -1 : 1;
            for (var index = currentColumnIndex + step; index >= 0 && index < _state.ColumnDefinitions.Count; index += step)
            {
                var candidate = _state.ColumnDefinitions[index];
                if (CanEditCell(rowKey, candidate.ColumnKey))
                {
                    nextCell = new GridCurrentCellCoordinate(rowKey, candidate.ColumnKey);
                    return true;
                }
            }

            return false;
        }

        private void NormalizeStateToDefinitions()
        {
            var validRowKeys = new HashSet<string>(_state.RowDefinitions.Select(row => row.RowKey), StringComparer.OrdinalIgnoreCase);
            var selectableRowKeys = new HashSet<string>(
                _state.RowDefinitions.Where(row => row.RepresentsDataRecord).Select(row => row.RowKey),
                StringComparer.OrdinalIgnoreCase);
            var validColumnKeys = new HashSet<string>(_state.ColumnDefinitions.Select(column => column.ColumnKey), StringComparer.OrdinalIgnoreCase);

            foreach (var rowKey in _state.SelectedRowKeys.Where(rowKey => !selectableRowKeys.Contains(rowKey)).ToArray())
            {
                _state.SelectedRowKeys.Remove(rowKey);
            }

            foreach (var rowKey in _state.CheckedRowKeys.Where(rowKey => !selectableRowKeys.Contains(rowKey)).ToArray())
            {
                _state.CheckedRowKeys.Remove(rowKey);
            }

            foreach (var rowKey in _state.EditedRowKeys.Where(rowKey => !selectableRowKeys.Contains(rowKey)).ToArray())
            {
                _state.EditedRowKeys.Remove(rowKey);
            }

            foreach (var rowKey in _state.InvalidRowKeys.Where(rowKey => !selectableRowKeys.Contains(rowKey)).ToArray())
            {
                _state.InvalidRowKeys.Remove(rowKey);
            }

            foreach (var cellKey in _state.SelectedCellKeys.Where(cellKey => !IsCellKeyValid(cellKey, selectableRowKeys, validColumnKeys)).ToArray())
            {
                _state.SelectedCellKeys.Remove(cellKey);
            }

            if (_state.CurrentCell != null &&
                (!selectableRowKeys.Contains(_state.CurrentCell.RowKey) || !validColumnKeys.Contains(_state.CurrentCell.ColumnKey)))
            {
                _state.CurrentCell = null;
                EnsureEditModel()?.Cancel();
            }

            if (_state.SelectionAnchorCell != null &&
                (!selectableRowKeys.Contains(_state.SelectionAnchorCell.RowKey) || !validColumnKeys.Contains(_state.SelectionAnchorCell.ColumnKey)))
            {
                _state.SelectionAnchorCell = null;
            }
        }

        private bool RepresentsDataRecord(string rowKey)
        {
            var row = _state.RowDefinitions.FirstOrDefault(candidate =>
                string.Equals(candidate.RowKey, rowKey, StringComparison.OrdinalIgnoreCase));
            return row?.RepresentsDataRecord ?? false;
        }

        private bool ToggleCheckedRow(string rowKey)
        {
            if (!_state.MultiSelect || string.IsNullOrWhiteSpace(rowKey) || !RepresentsDataRecord(rowKey))
            {
                return false;
            }

            if (_state.CheckedRowKeys.Contains(rowKey))
            {
                _state.CheckedRowKeys.Remove(rowKey);
                return false;
            }

            _state.CheckedRowKeys.Add(rowKey);
            return true;
        }

        private void SetSelectionForCheckedRow(string rowKey, bool isChecked)
        {
            _ = rowKey;
            _ = isChecked;
        }

        private static void ReplaceRowKeySet(ISet<string> target, IEnumerable<string> rowKeys)
        {
            target.Clear();
            foreach (var rowKey in (rowKeys ?? Array.Empty<string>())
                .Where(rowKey => !string.IsNullOrWhiteSpace(rowKey))
                .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                target.Add(rowKey);
            }
        }

        private static bool IsCellKeyValid(string cellKey, ISet<string> validRowKeys, ISet<string> validColumnKeys)
        {
            if (string.IsNullOrWhiteSpace(cellKey))
            {
                return false;
            }

            foreach (var columnKey in validColumnKeys)
            {
                var suffix = "_" + columnKey;
                if (!cellKey.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var rowKey = cellKey.Substring(0, cellKey.Length - suffix.Length);
                return validRowKeys.Contains(rowKey);
            }

            return false;
        }

        private sealed class SnapshotUpdateBatch : IDisposable
        {
            private GridSurfaceCoordinator _owner;

            public SnapshotUpdateBatch(GridSurfaceCoordinator owner)
            {
                _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            }

            public void Dispose()
            {
                if (_owner == null)
                {
                    return;
                }

                var owner = _owner;
                _owner = null;
                owner.CompleteSnapshotUpdateBatch();
            }
        }
    }

    /// <summary>
    /// Stan wewnętrzny coordinatora.
    /// </summary>
    internal sealed class GridSurfaceCoordinatorState
    {
        public List<GridColumnDefinition> ColumnDefinitions = new List<GridColumnDefinition>();
        public List<GridRowDefinition> RowDefinitions = new List<GridRowDefinition>();

        public double HorizontalOffset;
        public double VerticalOffset;
        public double ViewportWidth = 800;
        public double ViewportHeight = 600;

        public ISet<string> SelectedRowKeys = new HashSet<string>();
        public ISet<string> SelectedCellKeys = new HashSet<string>();
        public GridSelectionMode SelectionMode = GridSelectionMode.Cell;
        public bool EnableCellSelection = true;
        public bool EnableRangeSelection = true;

        public GridCurrentCellCoordinate CurrentCell;
        public GridCurrentCellCoordinate SelectionAnchorCell;
        public bool HasFocus;
        public int FrozenColumnCount;
        public int FrozenRowCount;
        public bool IsFocusRequestPending;
        public bool IsManipulating;
        public double LastPointerX;
        public double LastPointerY;
        public IReadOnlyList<GridSortDescriptor> SortDescriptors = Array.Empty<GridSortDescriptor>();
        public bool SelectCurrentRow = true;
        public bool MultiSelect;
        public bool ShowRowNumbers;
        public GridRowNumberingMode RowNumberingMode = GridRowNumberingMode.Global;
        public double RowActionWidth = 16d;
        public double RowIndicatorWidth = 14d;
        public double RowMarkerWidth = 18d;
        public double SelectionCheckboxWidth = 18d;
        public ISet<string> CheckedRowKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public ISet<string> EditedRowKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public ISet<string> InvalidRowKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public IReadOnlyDictionary<string, string> RowIndicatorToolTips = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public GridSurfaceStateProjection StateProjection = GridSurfaceStateProjection.Empty;
        public GridPointerInteractionSession PointerSession;
        public bool PendingEnsureCurrentCellVisible;
    }

    /// <summary>
    /// Event args dla zmiany snapshotu.
    /// </summary>
    public sealed class GridSnapshotChangedEventArgs : EventArgs
    {
        public GridSurfaceSnapshot Snapshot { get; set; }
    }

    /// <summary>
    /// Event args dla requesty fokusa.
    /// </summary>
    public sealed class GridFocusRequestEventArgs : EventArgs
    {
        public string TargetKey { get; set; }

        public GridFocusTargetKind TargetKind { get; set; }
    }

    public sealed class GridHeaderActivatedEventArgs : EventArgs
    {
        public string HeaderKey { get; set; }

        public GridHeaderKind HeaderKind { get; set; }

        public GridInputModifiers Modifiers { get; set; }
    }

    public sealed class GridColumnResizeRequestedEventArgs : EventArgs
    {
        public string ColumnKey { get; set; }

        public double Width { get; set; }
    }

    public sealed class GridColumnReorderRequestedEventArgs : EventArgs
    {
        public string ColumnKey { get; set; }

        public string TargetColumnKey { get; set; }
    }

    public sealed class GridColumnGroupingDragRequestedEventArgs : EventArgs
    {
        public string ColumnKey { get; set; }

        public GridPointerKind PointerKind { get; set; }

        public double StartX { get; set; }

        public double StartY { get; set; }

        public double CurrentX { get; set; }

        public double CurrentY { get; set; }
    }

    public sealed class GridRowActionRequestedEventArgs : EventArgs
    {
        public string RowKey { get; set; }

        public GridRowActionKind ActionKind { get; set; }
    }

    public enum GridRowActionKind
    {
        ToggleHierarchy,
        ToggleDetails,
        LoadMoreHierarchy,
    }

    internal sealed class GridColumnResizeState
    {
        public GridColumnResizeState(string columnKey, double initialWidth, double originX, double minWidth, double maxWidth)
        {
            ColumnKey = columnKey;
            InitialWidth = initialWidth;
            OriginX = originX;
            MinWidth = minWidth;
            MaxWidth = maxWidth;
        }

        public string ColumnKey { get; }

        public double InitialWidth { get; }

        public double OriginX { get; }

        public double MinWidth { get; }

        public double MaxWidth { get; }
    }

    internal enum GridPointerInteractionMode
    {
        None,
        PendingActivation,
        PendingResize,
        PendingReorder,
        ActiveResize,
        ActiveReorder,
    }

    internal sealed class GridPointerInteractionSession
    {
        private GridPointerInteractionSession(
            int pointerId,
            GridPointerKind pointerKind,
            GridMouseButton button,
            double startX,
            double startY,
            GridPointerInteractionMode mode)
        {
            PointerId = pointerId;
            PointerKind = pointerKind;
            Button = button;
            StartX = startX;
            StartY = startY;
            LastX = startX;
            LastY = startY;
            Mode = mode;
        }

        public int PointerId { get; }

        public GridPointerKind PointerKind { get; }

        public GridMouseButton Button { get; }

        public double StartX { get; }

        public double StartY { get; }

        public double LastX { get; set; }

        public double LastY { get; set; }

        public GridPointerInteractionMode Mode { get; set; }

        public string HeaderKey { get; private set; }

        public GridHeaderKind? HeaderKind { get; private set; }

        public GridColumnResizeState ResizeState { get; private set; }

        public string ReorderTargetColumnKey { get; set; }

        public static GridPointerInteractionSession BeginHeader(
            int pointerId,
            GridPointerKind pointerKind,
            GridMouseButton button,
            double startX,
            double startY,
            string headerKey,
            GridHeaderKind headerKind)
        {
            return new GridPointerInteractionSession(
                pointerId,
                pointerKind,
                button,
                startX,
                startY,
                GridPointerInteractionMode.PendingActivation)
            {
                HeaderKey = headerKey,
                HeaderKind = headerKind,
            };
        }

        public static GridPointerInteractionSession BeginResize(
            int pointerId,
            GridPointerKind pointerKind,
            GridMouseButton button,
            double startX,
            double startY,
            GridColumnResizeState resizeState)
        {
            return new GridPointerInteractionSession(
                pointerId,
                pointerKind,
                button,
                startX,
                startY,
                GridPointerInteractionMode.PendingResize)
            {
                ResizeState = resizeState,
            };
        }
    }

    internal readonly struct GridScrollTarget
    {
        public GridScrollTarget(
            GridRowLayout row,
            GridColumnLayout column,
            double frozenDataWidth,
            double frozenDataHeight,
            double scrollableViewportWidth,
            double scrollableViewportHeight,
            double maxHorizontalOffset,
            double maxVerticalOffset)
        {
            Row = row;
            Column = column;
            FrozenDataWidth = frozenDataWidth;
            FrozenDataHeight = frozenDataHeight;
            ScrollableViewportWidth = scrollableViewportWidth;
            ScrollableViewportHeight = scrollableViewportHeight;
            MaxHorizontalOffset = maxHorizontalOffset;
            MaxVerticalOffset = maxVerticalOffset;
        }

        public GridRowLayout Row { get; }

        public GridColumnLayout Column { get; }

        public double FrozenDataWidth { get; }

        public double FrozenDataHeight { get; }

        public double ScrollableViewportWidth { get; }

        public double ScrollableViewportHeight { get; }

        public double MaxHorizontalOffset { get; }

        public double MaxVerticalOffset { get; }
    }

    public enum GridFocusTargetKind
    {
        Grid,
        Cell,
        Header,
    }
}
