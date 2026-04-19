using System;
using System.Collections.Generic;

namespace PhialeGrid.Core.Columns
{
    public sealed class GridLayoutSnapshot
    {
        public GridLayoutSnapshot(IReadOnlyList<GridColumnDefinition> columns)
        {
            Columns = columns ?? throw new ArgumentNullException(nameof(columns));
        }

        public IReadOnlyList<GridColumnDefinition> Columns { get; }
    }
}
