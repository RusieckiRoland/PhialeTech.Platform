using System;
using System.Collections.Generic;
using System.Linq;

namespace PhialeTech.Components.Shared.Model
{
    public sealed class DemoExampleDefinition
    {
        public DemoExampleDefinition(
            string id,
            string componentId,
            string sectionKey,
            int sectionOrder,
            int displayOrder,
            string titleKey,
            string descriptionKey,
            string accentHex,
            IEnumerable<string> tags = null,
            string drawerGroupId = null)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Example id is required.", nameof(id));
            }

            Id = id;
            ComponentId = componentId ?? string.Empty;
            SectionKey = sectionKey ?? string.Empty;
            SectionOrder = sectionOrder;
            DisplayOrder = displayOrder;
            TitleKey = titleKey ?? string.Empty;
            DescriptionKey = descriptionKey ?? string.Empty;
            AccentHex = accentHex ?? "#0F766E";
            DrawerGroupId = string.IsNullOrWhiteSpace(drawerGroupId) ? ComponentId : drawerGroupId.Trim();
            Tags = (tags ?? Array.Empty<string>()).ToArray();
        }

        public string Id { get; }

        public string ComponentId { get; }

        public string SectionKey { get; }

        public int SectionOrder { get; }

        public int DisplayOrder { get; }

        public string TitleKey { get; }

        public string DescriptionKey { get; }

        public string AccentHex { get; }

        public string DrawerGroupId { get; }

        public IReadOnlyList<string> Tags { get; }
    }
}
