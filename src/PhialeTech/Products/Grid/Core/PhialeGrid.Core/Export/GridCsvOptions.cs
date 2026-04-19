namespace PhialeGrid.Core.Export
{
    public sealed class GridCsvOptions
    {
        public bool IncludeHeader { get; set; } = true;

        public string LineEnding { get; set; } = "\n";

        public char Delimiter { get; set; } = ',';

        public bool HasHeaderOnImport { get; set; } = true;
    }
}
