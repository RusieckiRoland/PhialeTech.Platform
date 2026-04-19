namespace PhialeTech.Components.Shared.Model
{
    public sealed class DemoThemeOption
    {
        public DemoThemeOption(string code, string displayName)
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
