namespace PhialeTech.Components.Shared.Model
{
    public sealed class DemoLanguageOption
    {
        public DemoLanguageOption(string code, string displayName)
        {
            Code = code;
            DisplayName = displayName;
        }

        public string Code { get; }

        public string DisplayName { get; }

        public override string ToString()
        {
            return DisplayName ?? string.Empty;
        }
    }
}

