namespace PhialeTech.Components.Shared.Model
{
    public sealed class DemoFoundationColorTokenViewModel
    {
        public DemoFoundationColorTokenViewModel(
            string tokenName,
            string usage,
            string dayHex,
            string nightHex)
        {
            TokenName = tokenName;
            Usage = usage;
            DayHex = dayHex;
            NightHex = nightHex;
        }

        public string TokenName { get; }

        public string Usage { get; }

        public string DayHex { get; }

        public string NightHex { get; }
    }
}

