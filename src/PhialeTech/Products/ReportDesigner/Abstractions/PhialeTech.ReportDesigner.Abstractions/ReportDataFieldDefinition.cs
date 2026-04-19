using System.Collections.Generic;

namespace PhialeTech.ReportDesigner.Abstractions
{
    public sealed class ReportDataFieldDefinition
    {
        public string Name { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string Type { get; set; } = "string";

        public bool IsCollection { get; set; }

        public IList<ReportDataFieldDefinition> Children { get; set; } = new List<ReportDataFieldDefinition>();
    }
}
