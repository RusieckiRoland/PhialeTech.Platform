namespace PhialeTech.Components.Shared.Model
{
    public sealed class DemoChoiceOption
    {
        public DemoChoiceOption(string code, string displayName)
        {
            Code = code ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
        }

        public string Code { get; }

        public string DisplayName { get; }

        public override string ToString()
        {
            return DisplayName;
        }
    }
}
