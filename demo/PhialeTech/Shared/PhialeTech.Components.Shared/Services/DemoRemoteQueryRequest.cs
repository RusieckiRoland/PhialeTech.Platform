using System;
using System.Collections.Generic;
using PhialeGrid.Core.Query;

namespace PhialeTech.Components.Shared.Services
{
    public sealed class DemoRemoteQueryRequest
    {
        public DemoRemoteQueryRequest(
            int offset,
            int size,
            IReadOnlyList<GridSortDescriptor> sorts,
            GridFilterGroup filterGroup,
            int refreshGeneration)
        {
            Offset = Math.Max(0, offset);
            Size = Math.Max(1, size);
            Sorts = sorts ?? Array.Empty<GridSortDescriptor>();
            FilterGroup = filterGroup ?? GridFilterGroup.EmptyAnd();
            RefreshGeneration = Math.Max(0, refreshGeneration);
        }

        public int Offset { get; }

        public int Size { get; }

        public IReadOnlyList<GridSortDescriptor> Sorts { get; }

        public GridFilterGroup FilterGroup { get; }

        public int RefreshGeneration { get; }
    }
}

