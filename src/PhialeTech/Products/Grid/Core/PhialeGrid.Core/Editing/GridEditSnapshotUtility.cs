using System;
using System.Collections.Generic;
using System.Linq;

namespace PhialeGrid.Core.Editing
{
    public static class GridEditSnapshotUtility
    {
        public static IReadOnlyList<string> ResolveChangedRowIds<T>(
            IReadOnlyDictionary<string, T> snapshots,
            Func<string, T> currentRowResolver,
            Func<T, T, bool> equalityComparer)
        {
            if (snapshots == null)
            {
                throw new ArgumentNullException(nameof(snapshots));
            }

            if (currentRowResolver == null)
            {
                throw new ArgumentNullException(nameof(currentRowResolver));
            }

            if (equalityComparer == null)
            {
                throw new ArgumentNullException(nameof(equalityComparer));
            }

            return snapshots
                .Where(entry =>
                {
                    var currentRow = currentRowResolver(entry.Key);
                    return currentRow != null && !equalityComparer(entry.Value, currentRow);
                })
                .Select(entry => entry.Key)
                .ToArray();
        }
    }
}
