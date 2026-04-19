using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PhialeGrid.Core.Commit;
using PhialeGrid.Core.Editing;
using PhialeGrid.Core.Presentation;
using PhialeGrid.Core.Query;
using PhialeGrid.Core.Surface;

namespace PhialeGrid.Core.Rendering
{
    /// <summary>
    /// Builder powierzchni grida - generuje GridSurfaceSnapshot na podstawie state i layout'u.
    /// Najpowozniejszy komponent architekrury - odpowiada za transformację stanu Core'u w opis wizualny.
    /// </summary>
    public sealed class GridSurfaceBuilder
    {
        private long _snapshotRevision = 0;

        /// <summary>
        /// Generuje kompletny snapshot grida.
        /// </summary>
        public GridSurfaceSnapshot BuildSnapshot(GridSurfaceBuildContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            _snapshotRevision++;

            // Buduję komponenty snapshotu
            var columns = BuildColumnItems(context);
            var rows = BuildRowItems(context);
            var cells = BuildCellItems(context, rows, columns);
            var headers = BuildHeaderItems(context, rows, columns);
            var overlays = BuildOverlayItems(context, rows, columns);
            var selectionRegions = BuildSelectionRegions(context);
            var currentCell = BuildCurrentCell(context);

            // Tworzę viewport state
            var viewportState = BuildViewportState(context, columns, rows);

            // Tworzę snapshot
            var snapshot = new GridSurfaceSnapshot(
                _snapshotRevision,
                viewportState,
                columns,
                rows,
                cells,
                headers,
                overlays,
                selectionRegions,
                currentCell);

            return snapshot;
        }

        private IReadOnlyList<GridColumnSurfaceItem> BuildColumnItems(GridSurfaceBuildContext context)
        {
            var columns = new List<GridColumnSurfaceItem>();

            for (int i = 0; i < context.ColumnDefinitions.Count; i++)
            {
                var colDef = context.ColumnDefinitions[i];
                var colLayout = context.ColumnLayouts[i];
                var sort = ResolveSort(context.Sorts, colDef.ColumnKey);

                var item = new GridColumnSurfaceItem(colDef.ColumnKey)
                {
                    DisplayName = colDef.Header,
                    Width = colLayout.Width,
                    MinWidth = colDef.MinWidth,
                    MaxWidth = colDef.MaxWidth,
                    IsVisible = colDef.IsVisible,
                    IsFrozen = colLayout.IsFrozen,
                    DisplayIndex = i,
                    SortDirection = sort == null ? (bool?)null : sort.Direction == GridSortDirection.Ascending,
                    SortPriority = sort?.Priority ?? -1,
                    Bounds = BuildColumnBounds(context, colLayout),
                    RenderLayer = 1,
                    SnapshotRevision = _snapshotRevision,
                };

                columns.Add(item);
            }

            return columns;
        }

        private IReadOnlyList<GridRowSurfaceItem> BuildRowItems(GridSurfaceBuildContext context)
        {
            var rows = new List<GridRowSurfaceItem>();

            for (int i = 0; i < context.RowDefinitions.Count; i++)
            {
                var rowDef = context.RowDefinitions[i];
                var rowLayout = context.RowLayouts[i];
                var recordState = ResolveRecordRenderState(context, rowDef.RowKey);

                var item = new GridRowSurfaceItem(rowDef.RowKey)
                {
                    Height = rowLayout.Height,
                    IsSelected = IsRowSelected(context, rowDef.RowKey),
                    HasValidationError = IsRecordValidationError(context, rowDef.RowKey, recordState),
                    HasPendingChanges = HasRecordPendingChanges(context, rowDef.RowKey, recordState),
                    EditState = recordState?.EditState ?? ResolveFallbackEditState(context, rowDef.RowKey),
                    ValidationState = recordState?.ValidationState ?? ResolveFallbackValidationState(context, rowDef.RowKey),
                    AccessState = recordState?.AccessState ?? RecordAccessState.Editable,
                    CommitState = recordState?.CommitState ?? RecordCommitState.Idle,
                    CommitDetail = recordState?.CommitDetail ?? RecordCommitDetail.None,
                    EditSessionId = recordState?.SessionId ?? string.Empty,
                    IsFrozen = rowLayout.IsFrozen,
                    HierarchyLevel = rowDef.HierarchyLevel > 0 ? rowDef.HierarchyLevel : rowLayout.HierarchyIndent / 20,
                    IsHierarchyExpanded = rowDef.IsHierarchyExpanded,
                    HasHierarchyChildren = rowDef.HasHierarchyChildren,
                    IsGroupHeader = rowDef.IsGroupHeader,
                    IsLoadMore = rowDef.IsLoadMore,
                    HasDetailsExpanded = rowDef.HasDetailsExpanded,
                    IsDetailsHost = rowDef.IsDetailsHost,
                    IsEditing = IsRecordActivelyEditing(context, rowDef.RowKey, recordState),
                    RepresentsDataRecord = rowDef.RepresentsDataRecord,
                    Bounds = BuildRowHeaderBounds(context, rowLayout),
                    RenderLayer = 1,
                    SnapshotRevision = _snapshotRevision,
                };

                rows.Add(item);
            }

            return rows;
        }

        private IReadOnlyList<GridCellSurfaceItem> BuildCellItems(
            GridSurfaceBuildContext context,
            IReadOnlyList<GridRowSurfaceItem> rows,
            IReadOnlyList<GridColumnSurfaceItem> columns)
        {
            var cells = new List<GridCellSurfaceItem>();

            foreach (var rowIdx in EnumerateRealizedRowIndexes(context))
            {
                var rowDef = context.RowDefinitions[rowIdx];
                var rowLayout = context.RowLayouts[rowIdx];
                if (rowDef.IsDetailsHost)
                {
                    continue;
                }

                foreach (var colIdx in EnumerateRealizedColumnIndexes(context))
                {
                    var colDef = context.ColumnDefinitions[colIdx];
                    var colLayout = context.ColumnLayouts[colIdx];

                    var cellKey = $"{rowDef.RowKey}_{colDef.ColumnKey}";
                    var bounds = BuildCellBounds(context, rowLayout, colLayout);
                    if (bounds.Width <= 0 || bounds.Height <= 0)
                    {
                        continue;
                    }

                    var rawValue = GetCellRawValue(context, rowDef.RowKey, colDef.ColumnKey);
                    var isEditingCell = IsGridInEditMode(context) &&
                                        context.CurrentCell?.RowKey == rowDef.RowKey &&
                                        context.CurrentCell?.ColumnKey == colDef.ColumnKey;
                    var cellState = ResolveCellRenderState(context, rowDef.RowKey, colDef.ColumnKey);
                    var validationState = cellState?.ValidationState ?? ResolveFallbackCellValidationState(context, cellKey);

                    var cellItem = new GridCellSurfaceItem(rowDef.RowKey, colDef.ColumnKey, cellKey)
                    {
                        DisplayText = isEditingCell
                            ? context.EditingSession?.EditingText ?? string.Empty
                            : GetCellDisplayText(context, rowDef.RowKey, colDef.ColumnKey, rawValue),
                        RawValue = rawValue,
                        ValueKind = colDef.ValueKind ?? "Text",
                        IsSelected = IsRowSelected(context, rowDef.RowKey) ||
                            (context.SelectedCellKeys?.Contains(cellKey) ?? false),
                        IsCurrentRow = context.SelectCurrentRow &&
                            string.Equals(context.CurrentCell?.RowKey, rowDef.RowKey, StringComparison.OrdinalIgnoreCase),
                        IsCurrent = context.CurrentCell?.RowKey == rowDef.RowKey && 
                                    context.CurrentCell?.ColumnKey == colDef.ColumnKey,
                        IsEditing = isEditingCell,
                        EditingText = isEditingCell ? context.EditingSession?.EditingText ?? string.Empty : null,
                        IsReadOnly = colDef.IsReadOnly || rowDef.IsReadOnly || rowDef.IsGroupHeader,
                        HasValidationError = validationState == CellValidationState.Invalid,
                        DisplayState = cellState?.DisplayState ?? ResolveFallbackCellDisplayState(context, rowDef.RowKey, colDef.ColumnKey),
                        ChangeState = cellState?.ChangeState ?? ResolveFallbackCellChangeState(context, rowDef.RowKey),
                        ValidationState = validationState,
                        AccessState = cellState?.AccessState ?? (colDef.IsReadOnly || rowDef.IsReadOnly || rowDef.IsGroupHeader
                            ? CellAccessState.ReadOnly
                            : CellAccessState.Editable),
                        EditSessionId = cellState?.SessionId ?? string.Empty,
                        ValidationError = context.CellValidationErrors != null && context.CellValidationErrors.ContainsKey(cellKey)
                            ? context.CellValidationErrors[cellKey]
                            : null,
                        IsFrozen = rowLayout.IsFrozen || colLayout.IsFrozen,
                        EditorKind = colDef.EditorKind,
                        EditorItems = colDef.EditorItems ?? Array.Empty<string>(),
                        EditorItemsMode = colDef.EditorItemsMode,
                        EditMask = colDef.EditMask ?? string.Empty,
                        Bounds = bounds,
                        RenderLayer = 2,
                        SnapshotRevision = _snapshotRevision,
                        IsDummy = false,
                    };

                    if (rowDef.IsGroupHeader)
                    {
                        var normalizedGroupCaption = NormalizeGroupCaptionText(cellItem.DisplayText);
                        cellItem.DisplayText = normalizedGroupCaption;

                        if (!string.IsNullOrWhiteSpace(normalizedGroupCaption))
                        {
                            cellItem.IsGroupCaptionCell = true;
                            cellItem.ShowInlineChevron = rowDef.HasHierarchyChildren;
                            cellItem.IsInlineChevronExpanded = rowDef.IsHierarchyExpanded;
                            cellItem.ContentIndent = Math.Max(0d, rowLayout.HierarchyIndent);
                        }
                    }

                    cells.Add(cellItem);
                }
            }

            return cells;
        }

        private IReadOnlyList<GridHeaderSurfaceItem> BuildHeaderItems(
            GridSurfaceBuildContext context,
            IReadOnlyList<GridRowSurfaceItem> rows,
            IReadOnlyList<GridColumnSurfaceItem> columns)
        {
            var headers = new List<GridHeaderSurfaceItem>();
            var rowNumberLookup = BuildRowNumberLookup(context);

            // Column headers
            foreach (var col in columns)
            {
                if (col.Bounds.Width <= 0 || col.Bounds.Height <= 0)
                {
                    continue;
                }

                var header = new GridHeaderSurfaceItem(col.ColumnKey, GridHeaderKind.ColumnHeader)
                {
                    DisplayText = col.DisplayName,
                    IconKey = col.SortDirection == null
                        ? null
                        : (col.SortDirection.Value ? "sort-asc" : "sort-desc"),
                    SortOrderText = col.SortPriority >= 1
                        ? (col.SortPriority + 1).ToString(CultureInfo.InvariantCulture)
                        : string.Empty,
                    IsResizable = true,
                    Bounds = new GridBounds(col.Bounds.X, 0, col.Bounds.Width, context.ColumnHeaderHeight),
                    RenderLayer = 1,
                    SnapshotRevision = _snapshotRevision,
                };
                headers.Add(header);
            }

            // Row headers: frozen rows + scrollable rows within realization range.
            foreach (var i in EnumerateRealizedRowIndexes(context))
            {
                if (context.RowHeaderWidth <= 0)
                {
                    continue;
                }

                if (context.RowDefinitions[i].IsDetailsHost)
                {
                    continue;
                }

                var row = rows[i];
                if (row.Bounds.Width <= 0 || row.Bounds.Height <= 0)
                {
                    continue;
                }

                var showSelectionCheckbox = context.MultiSelect && row.RepresentsDataRecord;
                var showRowNumbers = context.ShowRowNumbers;
                var stateHeaderWidth = context.RowIndicatorWidth +
                    (context.MultiSelect ? context.SelectionCheckboxWidth : 0d);
                var rowNumbersWidth = showRowNumbers ? context.RowMarkerWidth : 0d;
                var isCurrentRow = context.SelectCurrentRow &&
                    string.Equals(context.CurrentCell?.RowKey, row.RowKey, StringComparison.OrdinalIgnoreCase);

                if (stateHeaderWidth > 0d)
                {
                    headers.Add(new GridHeaderSurfaceItem(
                        row.RowKey,
                        GridHeaderKind.RowHeader,
                        $"header_{GridHeaderKind.RowHeader}_{row.RowKey}")
                    {
                        DisplayText = context.RowDefinitions[i].HeaderText ?? string.Empty,
                        IsSelected = IsRowSelected(context, row.RowKey),
                        IsCurrentRow = isCurrentRow,
                        RowIndicatorState = ResolveRowIndicatorState(context, row),
                        ShowRowIndicator = context.SelectCurrentRow,
                        ShowSelectionCheckbox = showSelectionCheckbox,
                        IsSelectionCheckboxChecked = context.CheckedRowKeys?.Contains(row.RowKey) ?? false,
                        ShowRowNumber = false,
                        RowNumberText = string.Empty,
                        RowIndicatorWidth = context.RowIndicatorWidth,
                        RowMarkerWidth = 0d,
                        SelectionCheckboxWidth = context.MultiSelect ? context.SelectionCheckboxWidth : 0d,
                        RowIndicatorToolTip = context.RowIndicatorToolTips != null &&
                            context.RowIndicatorToolTips.TryGetValue(row.RowKey, out var rowIndicatorToolTip)
                                ? rowIndicatorToolTip ?? string.Empty
                                : string.Empty,
                        Bounds = new GridBounds(0, row.Bounds.Y, stateHeaderWidth, row.Bounds.Height),
                        RenderLayer = 1,
                        SnapshotRevision = _snapshotRevision,
                    });
                }

                if (rowNumbersWidth > 0d)
                {
                    headers.Add(new GridHeaderSurfaceItem(
                        row.RowKey,
                        GridHeaderKind.RowNumberHeader,
                        $"header_{GridHeaderKind.RowNumberHeader}_{row.RowKey}")
                    {
                        DisplayText = context.RowDefinitions[i].HeaderText ?? string.Empty,
                        IsSelected = IsRowSelected(context, row.RowKey),
                        IsCurrentRow = isCurrentRow,
                        RowIndicatorState = GridRowIndicatorState.Empty,
                        ShowRowIndicator = false,
                        ShowSelectionCheckbox = false,
                        IsSelectionCheckboxChecked = false,
                        ShowRowNumber = showRowNumbers && row.RepresentsDataRecord,
                        RowNumberText = showRowNumbers && row.RepresentsDataRecord && rowNumberLookup.TryGetValue(row.RowKey, out var rowNumber)
                            ? rowNumber.ToString(CultureInfo.CurrentCulture)
                            : string.Empty,
                        RowIndicatorWidth = 0d,
                        RowMarkerWidth = rowNumbersWidth,
                        SelectionCheckboxWidth = 0d,
                        Bounds = new GridBounds(stateHeaderWidth, row.Bounds.Y, rowNumbersWidth, row.Bounds.Height),
                        RenderLayer = 1,
                        SnapshotRevision = _snapshotRevision,
                    });
                }
            }

            return headers;
        }

        private static IReadOnlyDictionary<string, int> BuildRowNumberLookup(GridSurfaceBuildContext context)
        {
            var lookup = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (context?.RowDefinitions == null || context.RowDefinitions.Count == 0)
            {
                return lookup;
            }

            var globalOrdinal = 0;
            var groupOrdinal = 0;

            foreach (var row in context.RowDefinitions)
            {
                if (row == null)
                {
                    continue;
                }

                if (context.RowNumberingMode == GridRowNumberingMode.WithinGroup && row.IsGroupHeader)
                {
                    groupOrdinal = 0;
                }

                if (!row.RepresentsDataRecord)
                {
                    continue;
                }

                globalOrdinal++;
                groupOrdinal++;
                lookup[row.RowKey] = context.RowNumberingMode == GridRowNumberingMode.WithinGroup
                    ? groupOrdinal
                    : globalOrdinal;
            }

            return lookup;
        }

        private IReadOnlyList<GridOverlaySurfaceItem> BuildOverlayItems(
            GridSurfaceBuildContext context,
            IReadOnlyList<GridRowSurfaceItem> rows,
            IReadOnlyList<GridColumnSurfaceItem> columns)
        {
            var overlays = new List<GridOverlaySurfaceItem>();

            // Current cell overlay (jeśli jest)
            if (context.CurrentCell != null)
            {
                var currentRow = rows.FirstOrDefault(r => r.RowKey == context.CurrentCell.RowKey);
                var currentCol = columns.FirstOrDefault(c => c.ColumnKey == context.CurrentCell.ColumnKey);

                if (!IsGridInEditMode(context) && currentRow != null && currentCol != null)
                {
                    var bounds = BuildCellBounds(
                        context,
                        context.RowLayouts.First(row => row.RowKey == currentRow.RowKey),
                        context.ColumnLayouts.First(column => column.ColumnKey == currentCol.ColumnKey));

                    if (bounds.Width <= 0 || bounds.Height <= 0)
                    {
                        return overlays;
                    }

                    var overlay = new GridOverlaySurfaceItem(
                        $"current_{context.CurrentCell.RowKey}_{context.CurrentCell.ColumnKey}",
                        GridOverlayKind.CurrentCell)
                    {
                        TargetKey = $"{context.CurrentCell.RowKey}_{context.CurrentCell.ColumnKey}",
                        TargetKind = GridOverlayTargetKind.Cell,
                        Bounds = bounds,
                        RenderLayer = 10,
                        SnapshotRevision = _snapshotRevision,
                    };
                    overlays.Add(overlay);
                }

                if (context.ShowCurrentRecordIndicator && currentRow != null && RepresentsDataRecord(context, currentRow.RowKey))
                {
                    var indicatorBounds = BuildCurrentRecordIndicatorBounds(context, currentRow.RowKey);
                    if (indicatorBounds.Width > 0 && indicatorBounds.Height > 0)
                    {
                        overlays.Add(new GridOverlaySurfaceItem(
                            $"current-record_{currentRow.RowKey}",
                            GridOverlayKind.CurrentRecord)
                        {
                            TargetKey = currentRow.RowKey,
                            TargetKind = GridOverlayTargetKind.Row,
                            Bounds = indicatorBounds,
                            DrawPriority = 6,
                            RenderLayer = 9,
                            SnapshotRevision = _snapshotRevision,
                        });
                    }
                }
            }

            foreach (var rowIdx in EnumerateRealizedRowIndexes(context))
            {
                var rowDef = context.RowDefinitions[rowIdx];
                if (!rowDef.IsDetailsHost || rowDef.DetailsPayload == null)
                {
                    continue;
                }

                var rowLayout = context.RowLayouts[rowIdx];
                var bounds = BuildDetailsOverlayBounds(context, rowLayout);
                if (bounds.Width <= 0 || bounds.Height <= 0)
                {
                    continue;
                }

                overlays.Add(new GridOverlaySurfaceItem("details_" + rowDef.RowKey, GridOverlayKind.Custom)
                {
                    TargetKey = rowDef.RowKey,
                    TargetKind = GridOverlayTargetKind.Row,
                    Payload = rowDef.DetailsPayload,
                    Bounds = bounds,
                    IsInteractive = true,
                    DrawPriority = 5,
                    RenderLayer = 8,
                    SnapshotRevision = _snapshotRevision,
                });
            }

            return overlays;
        }

        private IReadOnlyList<GridSelectionRegion> BuildSelectionRegions(GridSurfaceBuildContext context)
        {
            if (context.SelectionMode == GridSelectionMode.None)
                return Array.Empty<GridSelectionRegion>();

            var regions = new List<GridSelectionRegion>();

            if (context.SelectedRowKeys?.Count > 0)
            {
                var rowRegion = new GridSelectionRegion("row_selection", GridSelectionUnit.Row)
                {
                    SelectedKeys = context.SelectedRowKeys.ToList(),
                    Revision = _snapshotRevision,
                };
                regions.Add(rowRegion);
            }

            if (context.SelectionMode == GridSelectionMode.Cell && context.SelectedCellKeys?.Count > 0)
            {
                var region = new GridSelectionRegion("cell_selection", GridSelectionUnit.Cell)
                {
                    SelectedKeys = context.SelectedCellKeys.ToList(),
                    Revision = _snapshotRevision,
                };
                regions.Add(region);
            }

            return regions;
        }

        private GridCurrentCellMarker BuildCurrentCell(GridSurfaceBuildContext context)
        {
            if (context.CurrentCell == null || !RepresentsDataRecord(context, context.CurrentCell.RowKey))
                return null;

            return new GridCurrentCellMarker(context.CurrentCell.RowKey, context.CurrentCell.ColumnKey)
            {
                HasFocus = context.HasFocus,
                ShouldBeInEditMode = IsGridInEditMode(context),
                Revision = _snapshotRevision,
            };
        }

        private bool IsRowSelected(GridSurfaceBuildContext context, string rowKey)
        {
            if (!RepresentsDataRecord(context, rowKey))
            {
                return false;
            }

            return context.SelectedRowKeys?.Contains(rowKey) ?? false;
        }

        private GridBounds BuildCurrentRecordIndicatorBounds(GridSurfaceBuildContext context, string rowKey)
        {
            var rowIndex = context.RowDefinitions
                .Select((row, index) => new { row.RowKey, Index = index })
                .FirstOrDefault(candidate => string.Equals(candidate.RowKey, rowKey, StringComparison.OrdinalIgnoreCase));
            if (rowIndex == null)
            {
                return GridBounds.Empty;
            }

            var realizedColumnIndexes = EnumerateRealizedColumnIndexes(context).ToArray();
            if (realizedColumnIndexes.Length == 0)
            {
                return GridBounds.Empty;
            }

            var rowLayout = context.RowLayouts[rowIndex.Index];
            for (var index = realizedColumnIndexes.Length - 1; index >= 0; index--)
            {
                var columnLayout = context.ColumnLayouts[realizedColumnIndexes[index]];
                var bounds = BuildCellBounds(context, rowLayout, columnLayout);
                if (bounds.Width > 0 && bounds.Height > 0)
                {
                    return bounds;
                }
            }

            return GridBounds.Empty;
        }


        private static bool RepresentsDataRecord(GridSurfaceBuildContext context, string rowKey)
        {
            var row = context.RowDefinitions.FirstOrDefault(candidate =>
                string.Equals(candidate.RowKey, rowKey, StringComparison.OrdinalIgnoreCase));
            return row?.RepresentsDataRecord ?? false;
        }

        private static GridRowIndicatorState ResolveRowIndicatorState(GridSurfaceBuildContext context, GridRowSurfaceItem row)
        {
            if (row == null)
            {
                return GridRowIndicatorState.Empty;
            }

            var isCurrent = row.RepresentsDataRecord &&
                context.SelectCurrentRow &&
                string.Equals(context.CurrentCell?.RowKey, row.RowKey, StringComparison.OrdinalIgnoreCase);
            var isInvalid = row.ValidationState == RecordValidationState.Invalid;
            var isEdited = row.HasPendingChanges;
            var isEditing = row.IsEditing;

            if (!row.RepresentsDataRecord && !isInvalid && !isEdited && !isEditing)
            {
                return GridRowIndicatorState.Empty;
            }

            if (isEditing)
            {
                return GridRowIndicatorState.Editing;
            }

            if (isCurrent && isInvalid)
            {
                return GridRowIndicatorState.CurrentAndInvalid;
            }

            if (isCurrent && isEdited)
            {
                return GridRowIndicatorState.CurrentAndEdited;
            }

            if (isInvalid)
            {
                return GridRowIndicatorState.Invalid;
            }

            if (isEdited)
            {
                return GridRowIndicatorState.Edited;
            }

            if (isCurrent)
            {
                return GridRowIndicatorState.Current;
            }

            return GridRowIndicatorState.Empty;
        }

        private static string NormalizeGroupCaptionText(string displayText)
        {
            if (string.IsNullOrWhiteSpace(displayText))
            {
                return string.Empty;
            }

            var trimmed = displayText.TrimStart();
            if (trimmed.Length == 0)
            {
                return string.Empty;
            }

            var firstCharacter = trimmed[0];
            if (firstCharacter == '\u25bc' || firstCharacter == '\u25b6' || firstCharacter == '\u2022')
            {
                trimmed = trimmed.Substring(1).TrimStart();
            }

            return trimmed;
        }

        private GridBounds BuildColumnBounds(GridSurfaceBuildContext context, Layout.GridColumnLayout columnLayout)
        {
            var rawLeft = context.RowHeaderWidth + columnLayout.X - (columnLayout.IsFrozen ? 0 : context.HorizontalOffset);
            var rawRight = rawLeft + columnLayout.Width;
            var leftBoundary = columnLayout.IsFrozen
                ? context.RowHeaderWidth
                : context.RowHeaderWidth + GetFrozenDataWidth(context);

            return ClipBounds(rawLeft, 0, rawRight, context.ColumnHeaderHeight, leftBoundary, 0, context.ViewportWidth, context.ColumnHeaderHeight);
        }

        private static double GetTopChromeHeight(GridSurfaceBuildContext context)
        {
            return context.ColumnHeaderHeight + context.FilterRowHeight;
        }

        private static double GetDataTopInset(GridSurfaceBuildContext context)
        {
            if (!double.IsNaN(context.DataTopInset))
            {
                return Math.Max(0d, context.DataTopInset);
            }

            return GetTopChromeHeight(context);
        }

        private GridBounds BuildRowHeaderBounds(GridSurfaceBuildContext context, Layout.GridRowLayout rowLayout)
        {
            var dataTopInset = GetDataTopInset(context);
            var rawTop = dataTopInset + rowLayout.Y - (rowLayout.IsFrozen ? 0 : context.VerticalOffset);
            var rawBottom = rawTop + rowLayout.Height;
            var topBoundary = rowLayout.IsFrozen
                ? dataTopInset
                : dataTopInset + GetFrozenDataHeight(context);

            return ClipBounds(0, rawTop, context.RowHeaderWidth, rawBottom, 0, topBoundary, context.RowHeaderWidth, context.ViewportHeight);
        }

        private GridBounds BuildCellBounds(
            GridSurfaceBuildContext context,
            Layout.GridRowLayout rowLayout,
            Layout.GridColumnLayout columnLayout)
        {
            var dataTopInset = GetDataTopInset(context);
            var rawLeft = context.RowHeaderWidth + columnLayout.X - (columnLayout.IsFrozen ? 0 : context.HorizontalOffset);
            var rawTop = dataTopInset + rowLayout.Y - (rowLayout.IsFrozen ? 0 : context.VerticalOffset);
            var rawRight = rawLeft + columnLayout.Width;
            var rawBottom = rawTop + rowLayout.Height;
            var leftBoundary = columnLayout.IsFrozen
                ? context.RowHeaderWidth
                : context.RowHeaderWidth + GetFrozenDataWidth(context);
            var topBoundary = rowLayout.IsFrozen
                ? dataTopInset
                : dataTopInset + GetFrozenDataHeight(context);

            return ClipBounds(rawLeft, rawTop, rawRight, rawBottom, leftBoundary, topBoundary, context.ViewportWidth, context.ViewportHeight);
        }

        private GridBounds BuildDetailsOverlayBounds(
            GridSurfaceBuildContext context,
            Layout.GridRowLayout rowLayout)
        {
            var dataTopInset = GetDataTopInset(context);
            var rawTop = dataTopInset + rowLayout.Y - (rowLayout.IsFrozen ? 0 : context.VerticalOffset);
            var rawBottom = rawTop + rowLayout.Height;
            var topBoundary = rowLayout.IsFrozen
                ? dataTopInset
                : dataTopInset + GetFrozenDataHeight(context);

            return ClipBounds(
                context.RowHeaderWidth,
                rawTop,
                context.ViewportWidth,
                rawBottom,
                context.RowHeaderWidth,
                topBoundary,
                context.ViewportWidth,
                context.ViewportHeight);
        }

        private static GridBounds ClipBounds(
            double rawLeft,
            double rawTop,
            double rawRight,
            double rawBottom,
            double leftBoundary,
            double topBoundary,
            double rightBoundary,
            double bottomBoundary)
        {
            var left = Math.Max(rawLeft, leftBoundary);
            var top = Math.Max(rawTop, topBoundary);
            var right = Math.Min(rawRight, rightBoundary);
            var bottom = Math.Min(rawBottom, bottomBoundary);

            return new GridBounds(left, top, Math.Max(0, right - left), Math.Max(0, bottom - top));
        }

        private IEnumerable<int> EnumerateRealizedRowIndexes(GridSurfaceBuildContext context)
        {
            var indexes = new SortedSet<int>();
            for (int i = 0; i < Math.Min(context.FrozenRowCount, context.RowDefinitions.Count); i++)
            {
                indexes.Add(i);
            }

            var start = Math.Max(context.FrozenRowCount, context.RowRealizationRange?.BufferedStart ?? 0);
            var end = Math.Min(context.RowDefinitions.Count, Math.Max(start, context.RowRealizationRange?.BufferedEnd ?? context.RowDefinitions.Count));
            for (int i = start; i < end; i++)
            {
                indexes.Add(i);
            }

            return indexes;
        }

        private IEnumerable<int> EnumerateRealizedColumnIndexes(GridSurfaceBuildContext context)
        {
            var indexes = new SortedSet<int>();
            for (int i = 0; i < Math.Min(context.FrozenColumnCount, context.ColumnDefinitions.Count); i++)
            {
                indexes.Add(i);
            }

            var start = Math.Max(context.FrozenColumnCount, context.ColumnRealizationRange?.BufferedStart ?? 0);
            var end = Math.Min(context.ColumnDefinitions.Count, Math.Max(start, context.ColumnRealizationRange?.BufferedEnd ?? context.ColumnDefinitions.Count));
            for (int i = start; i < end; i++)
            {
                indexes.Add(i);
            }

            return indexes;
        }

        private double GetFrozenDataWidth(GridSurfaceBuildContext context)
        {
            return context.ColumnLayouts?.Where(layout => layout.IsFrozen).Sum(layout => layout.Width) ?? 0d;
        }

        private double GetFrozenDataHeight(GridSurfaceBuildContext context)
        {
            return context.RowLayouts?.Where(layout => layout.IsFrozen).Sum(layout => layout.Height) ?? 0d;
        }

        private GridViewportState BuildViewportState(
            GridSurfaceBuildContext context,
            IReadOnlyList<GridColumnSurfaceItem> columns,
            IReadOnlyList<GridRowSurfaceItem> rows)
        {
            var topChromeHeight = GetTopChromeHeight(context);
            var dataTopInset = GetDataTopInset(context);
            var totalWidth = context.RowHeaderWidth + (context.ColumnLayouts.Count > 0 ? context.ColumnLayouts[context.ColumnLayouts.Count - 1].Right : 0);
            var totalHeight = dataTopInset + (context.RowLayouts.Count > 0 ? context.RowLayouts[context.RowLayouts.Count - 1].Bottom : 0);
            var frozenDataWidth = GetFrozenDataWidth(context);
            var frozenDataHeight = GetFrozenDataHeight(context);
            var scrollableViewportWidth = Math.Max(0, context.ViewportWidth - context.RowHeaderWidth - frozenDataWidth);
            var scrollableViewportHeight = Math.Max(0, context.ViewportHeight - dataTopInset - frozenDataHeight);

            var metrics = new GridViewportMetrics(
                context.RowLayouts.Select(r => r.Height).ToList(),
                context.ColumnLayouts.Select(c => c.Width).ToList());
            metrics.AverageRowHeight = metrics.RowHeights.Count > 0 ? metrics.RowHeights.Average() : 0;
            metrics.AverageColumnWidth = metrics.ColumnWidths.Count > 0 ? metrics.ColumnWidths.Average() : 0;
            metrics.RowHeaderWidth = context.RowHeaderWidth;
            metrics.ColumnHeaderHeight = context.ColumnHeaderHeight;
            metrics.FilterRowHeight = context.FilterRowHeight;
            metrics.FrozenColumnWidth = frozenDataWidth;
            metrics.FrozenRowHeight = frozenDataHeight;
            metrics.Revision = _snapshotRevision;

            return new GridViewportState(
                context.HorizontalOffset,
                context.VerticalOffset,
                context.ViewportWidth,
                context.ViewportHeight,
                metrics)
            {
                TotalWidth = Math.Max(totalWidth, context.RowHeaderWidth),
                TotalHeight = Math.Max(totalHeight, dataTopInset),
                FrozenColumnCount = context.FrozenColumnCount,
                FrozenRowCount = context.FrozenRowCount,
                FrozenDataWidth = frozenDataWidth,
                FrozenDataHeight = frozenDataHeight,
                RowHeaderWidth = context.RowHeaderWidth,
                ColumnHeaderHeight = context.ColumnHeaderHeight,
                FilterRowHeight = context.FilterRowHeight,
                DataTopInset = dataTopInset,
                ScrollableViewportWidth = scrollableViewportWidth,
                ScrollableViewportHeight = scrollableViewportHeight,
                FrozenCornerBounds = new GridBounds(context.RowHeaderWidth, dataTopInset, frozenDataWidth, frozenDataHeight),
                FrozenRowsBounds = new GridBounds(context.RowHeaderWidth + frozenDataWidth, dataTopInset, scrollableViewportWidth, frozenDataHeight),
                FrozenColumnsBounds = new GridBounds(context.RowHeaderWidth, dataTopInset + frozenDataHeight, frozenDataWidth, scrollableViewportHeight),
                ScrollableBounds = new GridBounds(context.RowHeaderWidth + frozenDataWidth, dataTopInset + frozenDataHeight, scrollableViewportWidth, scrollableViewportHeight),
                VerticalTrackMarkers = BuildVerticalTrackMarkers(context, frozenDataHeight),
                IsInEditMode = IsGridInEditMode(context),
                Revision = _snapshotRevision,
            };
        }

        private IReadOnlyList<GridViewportTrackMarker> BuildVerticalTrackMarkers(GridSurfaceBuildContext context, double frozenDataHeight)
        {
            if (context.RowLayouts == null || context.RowLayouts.Count == 0)
            {
                return Array.Empty<GridViewportTrackMarker>();
            }

            var contentHeight = Math.Max(1d, context.RowLayouts[context.RowLayouts.Count - 1].Bottom - frozenDataHeight);
            var markers = new List<GridViewportTrackMarker>();

            foreach (var rowLayout in context.RowLayouts)
            {
                var rowDefinition = context.RowDefinitions.FirstOrDefault(candidate =>
                    string.Equals(candidate.RowKey, rowLayout.RowKey, StringComparison.OrdinalIgnoreCase));
                var recordState = ResolveRecordRenderState(context, rowLayout.RowKey);
                var isInvalid = IsRecordValidationError(context, rowLayout.RowKey, recordState);
                var isEdited = HasRecordPendingChanges(context, rowLayout.RowKey, recordState);
                if (!isInvalid && !isEdited)
                {
                    continue;
                }

                if (rowDefinition == null)
                {
                    continue;
                }

                var kind = isInvalid
                    ? GridViewportTrackMarkerKind.ValidationError
                    : GridViewportTrackMarkerKind.PendingEdit;
                var startRatio = Math.Max(0d, Math.Min(1d, (rowLayout.Y - frozenDataHeight) / contentHeight));
                var endRatio = Math.Max(startRatio, Math.Min(1d, (rowLayout.Bottom - frozenDataHeight) / contentHeight));
                var toolTip = context.RowIndicatorToolTips != null &&
                              context.RowIndicatorToolTips.TryGetValue(rowLayout.RowKey, out var value)
                    ? value
                    : string.Empty;

                markers.Add(new GridViewportTrackMarker(
                    key: kind + "_" + rowLayout.RowKey,
                    targetKey: rowLayout.RowKey,
                    kind: kind,
                    startRatio: startRatio,
                    endRatio: endRatio,
                    toolTip: toolTip));
            }

            return markers;
        }

        private string GetCellDisplayText(GridSurfaceBuildContext context, string rowKey, string columnKey, object rawValue)
        {
            return GridValueFormatter.FormatDisplayValue(rawValue, context.FormatProvider);
        }

        private object GetCellRawValue(GridSurfaceBuildContext context, string rowKey, string columnKey)
        {
            if (context.CellValueProvider != null &&
                context.CellValueProvider.TryGetValue(rowKey, columnKey, out var value))
            {
                return value;
            }

            return null;
        }

        private static GridRecordRenderState ResolveRecordRenderState(GridSurfaceBuildContext context, string rowKey)
        {
            if (context?.StateProjection?.RecordStates == null || string.IsNullOrWhiteSpace(rowKey))
            {
                return null;
            }

            GridRecordRenderState state;
            return context.StateProjection.RecordStates.TryGetValue(rowKey, out state) ? state : null;
        }

        private static GridCellRenderState ResolveCellRenderState(GridSurfaceBuildContext context, string rowKey, string columnKey)
        {
            if (context?.StateProjection?.CellStates == null ||
                string.IsNullOrWhiteSpace(rowKey) ||
                string.IsNullOrWhiteSpace(columnKey))
            {
                return null;
            }

            var key = rowKey + "_" + columnKey;
            GridCellRenderState state;
            return context.StateProjection.CellStates.TryGetValue(key, out state) ? state : null;
        }

        private static bool IsRecordValidationError(
            GridSurfaceBuildContext context,
            string rowKey,
            GridRecordRenderState recordState)
        {
            return (recordState?.ValidationState ?? ResolveFallbackValidationState(context, rowKey)) == RecordValidationState.Invalid;
        }

        private static bool HasRecordPendingChanges(
            GridSurfaceBuildContext context,
            string rowKey,
            GridRecordRenderState recordState)
        {
            var editState = recordState?.EditState ?? ResolveFallbackEditState(context, rowKey);
            return editState == RecordEditState.Modified ||
                   editState == RecordEditState.New ||
                   editState == RecordEditState.MarkedForDelete ||
                   HasProjectedCellChanges(context, rowKey);
        }

        private static bool HasProjectedCellChanges(GridSurfaceBuildContext context, string rowKey)
        {
            if (context?.StateProjection?.CellStates == null || string.IsNullOrWhiteSpace(rowKey))
            {
                return false;
            }

            return context.StateProjection.CellStates.Values.Any(cell =>
                string.Equals(cell.RecordKey, rowKey, StringComparison.OrdinalIgnoreCase) &&
                cell.ChangeState == CellChangeState.Modified);
        }

        private static bool IsRecordActivelyEditing(
            GridSurfaceBuildContext context,
            string rowKey,
            GridRecordRenderState recordState)
        {
            if (string.IsNullOrWhiteSpace(rowKey))
            {
                return false;
            }

            if (context?.StateProjection?.IsInEditMode == true &&
                string.Equals(context.StateProjection.EditingRecordId, rowKey, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return recordState?.EditState == RecordEditState.Editing;
        }

        private static bool IsGridInEditMode(GridSurfaceBuildContext context)
        {
            if (context?.StateProjection?.IsInEditMode == true)
            {
                return true;
            }

            return context?.IsInEditMode == true;
        }

        private static RecordEditState ResolveFallbackEditState(GridSurfaceBuildContext context, string rowKey)
        {
            if (IsGridInEditMode(context) &&
                string.Equals(context.CurrentCell?.RowKey, rowKey, StringComparison.OrdinalIgnoreCase))
            {
                return RecordEditState.Editing;
            }

            if (context.EditedRowKeys?.Contains(rowKey) ?? false)
            {
                return RecordEditState.Modified;
            }

            return RecordEditState.Unchanged;
        }

        private static RecordValidationState ResolveFallbackValidationState(GridSurfaceBuildContext context, string rowKey)
        {
            return (context.InvalidRowKeys?.Contains(rowKey) ?? false)
                ? RecordValidationState.Invalid
                : RecordValidationState.Unknown;
        }

        private static CellValidationState ResolveFallbackCellValidationState(GridSurfaceBuildContext context, string cellKey)
        {
            return context.CellValidationErrors != null && context.CellValidationErrors.ContainsKey(cellKey)
                ? CellValidationState.Invalid
                : CellValidationState.Unknown;
        }

        private static CellDisplayState ResolveFallbackCellDisplayState(
            GridSurfaceBuildContext context,
            string rowKey,
            string columnKey)
        {
            return context.CurrentCell?.RowKey == rowKey && context.CurrentCell?.ColumnKey == columnKey
                ? CellDisplayState.Current
                : CellDisplayState.Normal;
        }

        private static CellChangeState ResolveFallbackCellChangeState(GridSurfaceBuildContext context, string rowKey)
        {
            return (context.EditedRowKeys?.Contains(rowKey) ?? false)
                ? CellChangeState.Modified
                : CellChangeState.Unchanged;
        }

        private static SortState ResolveSort(IReadOnlyList<GridSortDescriptor> sorts, string columnKey)
        {
            if (sorts == null || string.IsNullOrWhiteSpace(columnKey))
            {
                return null;
            }

            for (int i = 0; i < sorts.Count; i++)
            {
                var sort = sorts[i];
                if (string.Equals(sort.ColumnId, columnKey, StringComparison.OrdinalIgnoreCase))
                {
                    return new SortState(sort.Direction, i);
                }
            }

            return null;
        }

        private sealed class SortState
        {
            public SortState(GridSortDirection direction, int priority)
            {
                Direction = direction;
                Priority = priority;
            }

            public GridSortDirection Direction { get; }

            public int Priority { get; }
        }
    }

    /// <summary>
    /// Context dla budowania snapshotu - zawiera wszystkie dane potrzebne do generacji.
    /// </summary>
    public sealed class GridSurfaceBuildContext
    {
        public IReadOnlyList<Layout.GridColumnDefinition> ColumnDefinitions { get; set; }
        public IReadOnlyList<Layout.GridRowDefinition> RowDefinitions { get; set; }
        public IReadOnlyList<Layout.GridColumnLayout> ColumnLayouts { get; set; }
        public IReadOnlyList<Layout.GridRowLayout> RowLayouts { get; set; }
        
        public double HorizontalOffset { get; set; }
        public double VerticalOffset { get; set; }
        public double ViewportWidth { get; set; }
        public double ViewportHeight { get; set; }

        public Layout.GridRealizationRange RowRealizationRange { get; set; }
        public Layout.GridRealizationRange ColumnRealizationRange { get; set; }

        public ISet<string> SelectedRowKeys { get; set; } = new HashSet<string>();
        public ISet<string> SelectedCellKeys { get; set; } = new HashSet<string>();
        public GridSelectionMode SelectionMode { get; set; } = GridSelectionMode.None;

        public GridCurrentCellCoordinate CurrentCell { get; set; }
        public bool HasFocus { get; set; }
        public bool IsInEditMode { get; set; }
        public bool ShowCurrentRecordIndicator { get; set; } = true;
        public bool SelectCurrentRow { get; set; } = true;
        public bool MultiSelect { get; set; }
        public bool ShowRowNumbers { get; set; }
        public GridRowNumberingMode RowNumberingMode { get; set; } = GridRowNumberingMode.Global;
        public double RowActionWidth { get; set; }
        public double RowIndicatorWidth { get; set; } = 14d;
        public double RowMarkerWidth { get; set; } = 18d;
        public double SelectionCheckboxWidth { get; set; } = 18d;
        public GridEditSession EditingSession { get; set; }
        public double ColumnHeaderHeight { get; set; } = 30;

        public double FilterRowHeight { get; set; } = 32;
        public double DataTopInset { get; set; } = double.NaN;
        public double RowHeaderWidth { get; set; } = 40;
        public IFormatProvider FormatProvider { get; set; }
        public IGridCellValueProvider CellValueProvider { get; set; }
        public IReadOnlyDictionary<string, string> CellValidationErrors { get; set; }
        public IReadOnlyDictionary<string, string> RowIndicatorToolTips { get; set; }
        public ISet<string> EditedRowKeys { get; set; } = new HashSet<string>();
        public ISet<string> InvalidRowKeys { get; set; } = new HashSet<string>();
        public ISet<string> CheckedRowKeys { get; set; } = new HashSet<string>();
        public IReadOnlyList<GridSortDescriptor> Sorts { get; set; } = Array.Empty<GridSortDescriptor>();
        public GridSurfaceStateProjection StateProjection { get; set; } = GridSurfaceStateProjection.Empty;

        public int FrozenColumnCount { get; set; }
        public int FrozenRowCount { get; set; }
    }

    /// <summary>
    /// Tryb zaznaczenia grida.
    /// </summary>
    public enum GridSelectionMode
    {
        None,
        Cell,
        Row,
        Column,
        Mixed,
    }
}
