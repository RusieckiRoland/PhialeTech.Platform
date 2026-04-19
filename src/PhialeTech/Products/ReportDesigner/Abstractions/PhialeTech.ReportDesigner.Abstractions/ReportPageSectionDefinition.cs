namespace PhialeTech.ReportDesigner.Abstractions
{
    public sealed class ReportPageSectionDefinition : ReportSectionDefinition
    {
        public bool SkipFirstPage { get; set; }

        public bool SkipLastPage { get; set; }
    }
}
