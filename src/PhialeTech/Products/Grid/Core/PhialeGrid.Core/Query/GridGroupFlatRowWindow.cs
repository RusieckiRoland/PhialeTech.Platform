using System;
using System.Collections.Generic;

namespace PhialeGrid.Core.Query
{
    public sealed class GridGroupFlatRowWindow<T>
    {
        public GridGroupFlatRowWindow(IReadOnlyList<GridGroupFlatRow<T>> rows, int totalRowCount)
        {
            Rows = rows ?? throw new ArgumentNullException(nameof(rows));
            TotalRowCount = totalRowCount;
        }

        public IReadOnlyList<GridGroupFlatRow<T>> Rows { get; }

        public int TotalRowCount { get; }
    }
}
