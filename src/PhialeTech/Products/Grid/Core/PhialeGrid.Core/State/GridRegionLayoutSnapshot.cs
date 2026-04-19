using System;
using System.Collections.Generic;
using System.Linq;

namespace PhialeGrid.Core.State
{
    public sealed class GridRegionLayoutSnapshot
    {
        public GridRegionLayoutSnapshot(IReadOnlyList<GridRegionLayoutState> regions)
        {
            if (regions == null)
            {
                throw new ArgumentNullException(nameof(regions));
            }

            var copy = regions.ToArray();
            if (copy.Any(region => region == null))
            {
                throw new ArgumentException("Region layout snapshots cannot contain null regions.", nameof(regions));
            }

            var duplicateKind = copy
                .GroupBy(region => region.RegionKind)
                .FirstOrDefault(group => group.Count() > 1);
            if (duplicateKind != null)
            {
                throw new ArgumentException("Duplicate region entry found for " + duplicateKind.Key + ".", nameof(regions));
            }

            Regions = Array.AsReadOnly(copy);
        }

        public IReadOnlyList<GridRegionLayoutState> Regions { get; }
    }
}

