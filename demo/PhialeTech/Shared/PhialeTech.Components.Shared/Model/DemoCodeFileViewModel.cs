namespace PhialeTech.Components.Shared.Model
{
    public sealed class DemoCodeFileViewModel
    {
        public DemoCodeFileViewModel(string fileName, string text)
        {
            FileName = fileName;
            Text = text;
        }

        public string FileName { get; }

        public string Text { get; }

        public override string ToString()
        {
            return FileName ?? string.Empty;
        }
    }
}
