using System;

namespace PhialeTech.Shell.Abstractions.Presentation
{
    public sealed class ApplicationShellCommandItem
    {
        public ApplicationShellCommandItem(string commandId, string displayText, ApplicationShellCommandPlacement placement)
        {
            if (string.IsNullOrWhiteSpace(commandId))
            {
                throw new ArgumentException("Command id is required.", nameof(commandId));
            }

            CommandId = commandId;
            DisplayText = displayText ?? string.Empty;
            Placement = placement;
            ToolTipText = string.Empty;
            IconGlyph = string.Empty;
            IsEnabled = true;
        }

        public string CommandId { get; }

        public string DisplayText { get; set; }

        public string ToolTipText { get; set; }

        public string IconGlyph { get; set; }

        public ApplicationShellCommandPlacement Placement { get; set; }

        public bool IsEnabled { get; set; }
    }
}
