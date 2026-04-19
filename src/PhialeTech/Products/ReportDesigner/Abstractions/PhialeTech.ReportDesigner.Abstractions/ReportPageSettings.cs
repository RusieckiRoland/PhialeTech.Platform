namespace PhialeTech.ReportDesigner.Abstractions
{
    public sealed class ReportPageSettings
    {
        public string Size { get; set; } = "A4";

        public string Orientation { get; set; } = "Portrait";

        public string Margin { get; set; } = "20mm";
    }
}
