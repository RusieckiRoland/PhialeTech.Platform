using System;

namespace PhialeGrid.Core.Editing
{
    public sealed class GridConcurrencyConflict<T>
    {
        public GridConcurrencyConflict(string rowId, T originalRow, T currentRow, T latestRow, string originalVersion, string latestVersion)
        {
            RowId = rowId ?? throw new ArgumentNullException(nameof(rowId));
            OriginalRow = originalRow;
            CurrentRow = currentRow;
            LatestRow = latestRow;
            OriginalVersion = originalVersion;
            LatestVersion = latestVersion;
        }

        public string RowId { get; }

        public T OriginalRow { get; }

        public T CurrentRow { get; }

        public T LatestRow { get; }

        public string OriginalVersion { get; }

        public string LatestVersion { get; }
    }
}
