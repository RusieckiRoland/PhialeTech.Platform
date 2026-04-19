using System.Collections.Generic;

namespace PhialeTech.ReportDesigner.Abstractions
{
    public class ReportSectionDefinition
    {
        public IList<ReportBlockDefinition> Blocks { get; set; } = new List<ReportBlockDefinition>();
    }
}
