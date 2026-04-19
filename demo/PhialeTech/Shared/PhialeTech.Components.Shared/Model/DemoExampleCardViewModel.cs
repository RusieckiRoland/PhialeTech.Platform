namespace PhialeTech.Components.Shared.Model
{
    public sealed class DemoExampleCardViewModel
    {
        public DemoExampleCardViewModel(string id, string title, string description, string accentHex, bool isSelected = false)
        {
            Id = id;
            Title = title;
            Description = description;
            AccentHex = accentHex;
            IsSelected = isSelected;
        }

        public string Id { get; }

        public string Title { get; }

        public string Description { get; }

        public string AccentHex { get; }

        public bool IsSelected { get; }
    }
}
