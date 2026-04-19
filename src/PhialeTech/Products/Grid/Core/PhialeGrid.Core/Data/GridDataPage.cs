using System;
using System.Collections.Generic;

namespace PhialeGrid.Core.Data
{
    public sealed class GridDataPage<T>
    {
        public GridDataPage(int offset, IReadOnlyList<T> items, int totalCount)
        {
            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            if (totalCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(totalCount));
            }

            Offset = offset;
            Items = items ?? throw new ArgumentNullException(nameof(items));
            TotalCount = totalCount;
        }

        public int Offset { get; }

        public IReadOnlyList<T> Items { get; }

        public int TotalCount { get; }
    }
}
