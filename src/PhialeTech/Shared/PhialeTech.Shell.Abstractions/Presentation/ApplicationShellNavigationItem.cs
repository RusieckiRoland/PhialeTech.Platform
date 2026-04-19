using System;

namespace PhialeTech.Shell.Abstractions.Presentation
{
    public sealed class ApplicationShellNavigationItem
    {
        public ApplicationShellNavigationItem(string itemId, string displayText)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                throw new ArgumentException("Navigation item id is required.", nameof(itemId));
            }

            ItemId = itemId;
            DisplayText = displayText ?? string.Empty;
            Description = string.Empty;
            IconGlyph = string.Empty;
            IsEnabled = true;
        }

        public string ItemId { get; }

        public string DisplayText { get; set; }

        public string Description { get; set; }

        public string IconGlyph { get; set; }

        public bool IsSelected { get; set; }

        public bool IsEnabled { get; set; }
    }
}
