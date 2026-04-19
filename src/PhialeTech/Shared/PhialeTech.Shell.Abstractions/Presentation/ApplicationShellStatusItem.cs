using System;

namespace PhialeTech.Shell.Abstractions.Presentation
{
    public sealed class ApplicationShellStatusItem
    {
        public ApplicationShellStatusItem(string itemId, string labelText, string valueText)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                throw new ArgumentException("Status item id is required.", nameof(itemId));
            }

            ItemId = itemId;
            LabelText = labelText ?? string.Empty;
            ValueText = valueText ?? string.Empty;
        }

        public string ItemId { get; }

        public string LabelText { get; set; }

        public string ValueText { get; set; }
    }
}
