namespace PhialeGrid.Core.Clipboard
{
    public sealed class GridClipboardOptions
    {
        public char Delimiter { get; set; } = '\t';

        public string LineEnding { get; set; } = "\n";
    }
}
