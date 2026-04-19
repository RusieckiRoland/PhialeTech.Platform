namespace PhialeTech.ReportDesigner.Abstractions
{
    public sealed class ReportDocumentSections
    {
        public ReportSectionDefinition ReportHeader { get; set; } = new ReportSectionDefinition();

        public ReportSectionDefinition Body { get; set; } = new ReportSectionDefinition();

        public ReportSectionDefinition ReportFooter { get; set; } = new ReportSectionDefinition();

        public ReportPageSectionDefinition PageHeader { get; set; } = new ReportPageSectionDefinition();

        public ReportPageSectionDefinition PageFooter { get; set; } = new ReportPageSectionDefinition();
    }
}
