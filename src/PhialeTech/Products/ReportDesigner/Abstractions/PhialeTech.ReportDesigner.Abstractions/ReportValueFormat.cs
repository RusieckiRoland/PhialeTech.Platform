namespace PhialeTech.ReportDesigner.Abstractions
{
    public sealed class ReportValueFormat
    {
        public string Kind { get; set; } = "text";

        public string Pattern { get; set; } = string.Empty;

        public string Currency { get; set; } = string.Empty;

        public int? Decimals { get; set; }
    }
}
