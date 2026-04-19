using PhialeTech.YamlApp.Abstractions.Enums;
using PhialeTech.YamlApp.Abstractions.Interfaces;

namespace PhialeTech.YamlApp.Core.Resolved
{
    public sealed class ResolvedActionAreaDefinition
    {
        public ResolvedActionAreaDefinition(
            IActionAreaDefinition definition,
            ActionPlacement placement,
            ActionAlignment horizontalAlignment,
            bool shared,
            bool sticky,
            bool visible)
        {
            Definition = definition;
            Placement = placement;
            HorizontalAlignment = horizontalAlignment;
            Shared = shared;
            Sticky = sticky;
            Visible = visible;
        }

        public IActionAreaDefinition Definition { get; }

        public string Id => Definition == null ? null : Definition.Id;

        public string Extends => Definition == null ? null : Definition.Extends;

        public string Name => Definition == null ? null : Definition.Name;

        public ActionPlacement Placement { get; }

        public ActionAlignment HorizontalAlignment { get; }

        public bool Shared { get; }

        public bool Sticky { get; }

        public bool Visible { get; }
    }
}
