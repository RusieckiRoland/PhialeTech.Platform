namespace PhialeTech.Components.Shared.Model
{
    public sealed class DemoMetricCardViewModel
    {
        public DemoMetricCardViewModel(string title, string accentHex, string deltaText, bool isPositive)
        {
            Title = title;
            AccentHex = accentHex;
            DeltaText = deltaText;
            IsPositive = isPositive;
        }

        public string Title { get; }

        public string AccentHex { get; }

        public string DeltaText { get; }

        public bool IsPositive { get; }
    }
}

