using System.Collections.Generic;

namespace PhialeTech.ReportDesigner.Abstractions
{
    public sealed class ReportDefinition
    {
        public int Version { get; set; } = 1;

        public ReportPageSettings Page { get; set; } = new ReportPageSettings();

        public ReportDocumentSections Sections { get; set; } = new ReportDocumentSections();

        public IList<ReportBlockDefinition> Blocks { get; set; } = new List<ReportBlockDefinition>();

        public string EditorMetadata { get; set; } = string.Empty;
    }
}
