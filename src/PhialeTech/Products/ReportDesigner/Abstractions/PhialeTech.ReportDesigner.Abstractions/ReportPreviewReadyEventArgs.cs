using System;

namespace PhialeTech.ReportDesigner.Abstractions
{
    public sealed class ReportPreviewReadyEventArgs : EventArgs
    {
        public ReportPreviewReadyEventArgs(int pageCount, bool usedSampleData)
        {
            PageCount = pageCount;
            UsedSampleData = usedSampleData;
        }

        public int PageCount { get; }

        public bool UsedSampleData { get; }
    }
}
