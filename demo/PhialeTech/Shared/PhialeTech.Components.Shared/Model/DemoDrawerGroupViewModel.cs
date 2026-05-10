namespace PhialeTech.Components.Shared.Model
{
    public sealed class DemoDrawerGroupViewModel
    {
        public DemoDrawerGroupViewModel(string id, string title, string description, string accentHex, bool isSelected, bool opensDirectly)
        {
            Id = id;
            Title = title;
            Description = description;
            AccentHex = accentHex;
            IsSelected = isSelected;
            OpensDirectly = opensDirectly;
        }

        public string Id { get; }

        public string Title { get; }

        public string Description { get; }

        public string AccentHex { get; }

        public bool IsSelected { get; }

        public bool OpensDirectly { get; }
    }
}

