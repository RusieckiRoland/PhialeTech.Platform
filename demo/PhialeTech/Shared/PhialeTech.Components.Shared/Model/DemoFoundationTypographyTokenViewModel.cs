namespace PhialeTech.Components.Shared.Model
{
    public sealed class DemoFoundationTypographyTokenViewModel
    {
        public DemoFoundationTypographyTokenViewModel(
            string tokenName,
            string role,
            string usage,
            string sampleText,
            string fontFamilyName,
            double fontSize,
            string fontWeight,
            string sampleTone,
            string styleSummary)
        {
            TokenName = tokenName;
            Role = role;
            Usage = usage;
            SampleText = sampleText;
            FontFamilyName = fontFamilyName;
            FontSize = fontSize;
            FontWeight = fontWeight;
            SampleTone = sampleTone;
            StyleSummary = styleSummary;
        }

        public string TokenName { get; }

        public string Role { get; }

        public string Usage { get; }

        public string SampleText { get; }

        public string FontFamilyName { get; }

        public double FontSize { get; }

        public string FontWeight { get; }

        public string SampleTone { get; }

        public string StyleSummary { get; }
    }
}
