using System;

namespace PhialeTech.ReportDesigner.Abstractions
{
    public sealed class ReportDefinitionChangedEventArgs : EventArgs
    {
        public ReportDefinitionChangedEventArgs(ReportDefinition definition)
        {
            Definition = definition ?? new ReportDefinition();
        }

        public ReportDefinition Definition { get; }
    }
}
