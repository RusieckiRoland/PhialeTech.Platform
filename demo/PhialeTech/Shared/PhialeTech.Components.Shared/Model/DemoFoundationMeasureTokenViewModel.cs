namespace PhialeTech.Components.Shared.Model
{
    public sealed class DemoFoundationMeasureTokenViewModel
    {
        public DemoFoundationMeasureTokenViewModel(string tokenName, string value, string usage)
        {
            TokenName = tokenName;
            Value = value;
            Usage = usage;
        }

        public string TokenName { get; }

        public string Value { get; }

        public string Usage { get; }
    }
}

