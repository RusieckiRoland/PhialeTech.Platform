using System;
using System.Collections.Generic;

namespace PhialeGrid.Core.Editing
{
    public sealed class GridRowChange<T>
    {
        public GridRowChange(
            string rowId,
            GridRowChangeType changeType,
            T originalRow,
            T currentRow,
            IReadOnlyList<GridValidationError> errors,
            IReadOnlyList<GridCellChange> cellChanges,
            string originalVersion,
            string latestVersion,
            bool hasConflict,
            T latestRow,
            T mergedRow)
        {
            RowId = rowId ?? throw new ArgumentNullException(nameof(rowId));
            ChangeType = changeType;
            OriginalRow = originalRow;
            CurrentRow = currentRow;
            Errors = errors ?? Array.Empty<GridValidationError>();
            CellChanges = cellChanges ?? Array.Empty<GridCellChange>();
            OriginalVersion = originalVersion;
            LatestVersion = latestVersion;
            HasConflict = hasConflict;
            LatestRow = latestRow;
            MergedRow = mergedRow;
        }

        public string RowId { get; }

        public GridRowChangeType ChangeType { get; }

        public T OriginalRow { get; }

        public T CurrentRow { get; }

        public IReadOnlyList<GridValidationError> Errors { get; }

        public IReadOnlyList<GridCellChange> CellChanges { get; }

        public string OriginalVersion { get; }

        public string LatestVersion { get; }

        public bool HasConflict { get; }

        public T LatestRow { get; }

        public T MergedRow { get; }

        public bool IsValid => Errors.Count == 0 && !HasConflict;
    }
}
