using System;
using System.Collections.Generic;
using PhialeTech.Components.Shared.Model;

namespace PhialeTech.Components.Shared.Services
{
    public sealed class DemoRemoteQueryResult
    {
        public DemoRemoteQueryResult(
            int offset,
            int size,
            int returnedCount,
            int totalCount,
            IReadOnlyList<DemoGisRecordViewModel> items)
        {
            Offset = Math.Max(0, offset);
            Size = Math.Max(0, size);
            ReturnedCount = Math.Max(0, returnedCount);
            TotalCount = Math.Max(0, totalCount);
            Items = items ?? Array.Empty<DemoGisRecordViewModel>();
        }

        public int Offset { get; }

        public int Size { get; }

        public int ReturnedCount { get; }

        public int TotalCount { get; }

        public IReadOnlyList<DemoGisRecordViewModel> Items { get; }
    }
}
