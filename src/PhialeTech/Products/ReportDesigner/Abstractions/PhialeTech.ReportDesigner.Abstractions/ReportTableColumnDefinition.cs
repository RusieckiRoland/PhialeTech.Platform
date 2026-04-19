namespace PhialeTech.ReportDesigner.Abstractions
{
    public sealed class ReportTableColumnDefinition
    {
        public string Header { get; set; } = string.Empty;

        public string Binding { get; set; } = string.Empty;

        public string Width { get; set; } = string.Empty;

        public string TextAlign { get; set; } = string.Empty;

        public ReportValueFormat Format { get; set; } = new ReportValueFormat();
    }
}
