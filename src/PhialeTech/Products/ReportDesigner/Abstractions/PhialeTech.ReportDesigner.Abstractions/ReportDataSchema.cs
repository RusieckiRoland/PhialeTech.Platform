using System.Collections.Generic;

namespace PhialeTech.ReportDesigner.Abstractions
{
    public sealed class ReportDataSchema
    {
        public IList<ReportDataFieldDefinition> Fields { get; set; } = new List<ReportDataFieldDefinition>();
    }
}
