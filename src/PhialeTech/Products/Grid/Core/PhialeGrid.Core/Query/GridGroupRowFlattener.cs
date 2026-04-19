using System;
using System.Collections.Generic;

namespace PhialeGrid.Core.Query
{
    public static class GridGroupRowFlattener
    {
        public static IReadOnlyList<GridGroupFlatRow<T>> Flatten<T>(IReadOnlyList<GridGroupNode<T>> groups)
        {
            if (groups == null)
            {
                throw new ArgumentNullException(nameof(groups));
            }

            return GridGroupFlatRowWindowBuilder.BuildWindow(groups, 0, GridGroupFlatRowWindowBuilder.CountRows(groups)).Rows;
        }
    }
}
