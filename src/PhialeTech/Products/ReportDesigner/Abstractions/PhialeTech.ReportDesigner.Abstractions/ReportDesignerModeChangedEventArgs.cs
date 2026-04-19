using System;

namespace PhialeTech.ReportDesigner.Abstractions
{
    public sealed class ReportDesignerModeChangedEventArgs : EventArgs
    {
        public ReportDesignerModeChangedEventArgs(ReportDesignerMode mode)
        {
            Mode = mode;
        }

        public ReportDesignerMode Mode { get; }
    }
}
