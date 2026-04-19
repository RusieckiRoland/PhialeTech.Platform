namespace PhialeTech.ReportDesigner.Abstractions
{
    public sealed class ReportFieldListItemDefinition
    {
        public string Label { get; set; } = string.Empty;

        public string Binding { get; set; } = string.Empty;

        public ReportValueFormat Format { get; set; } = new ReportValueFormat();
    }
}
