using System;
using System.Collections.Generic;
using System.Linq;
using PhialeTech.Shell.Abstractions.Presentation;

namespace PhialeTech.Shell.Presentation
{
    public static class ApplicationShellStateTransitions
    {
        public static ApplicationShellState SelectNavigation(ApplicationShellState shellState, string navigationItemId)
        {
            if (shellState == null)
            {
                throw new ArgumentNullException(nameof(shellState));
            }

            if (string.IsNullOrWhiteSpace(navigationItemId))
            {
                throw new ArgumentException("Navigation item id is required.", nameof(navigationItemId));
            }

            ApplicationShellStateValidator.Validate(shellState);

            if (!shellState.NavigationItems.Any(item => string.Equals(item.ItemId, navigationItemId, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException("Cannot select a navigation item that does not exist in the current shell state. Requested id: " + navigationItemId);
            }

            return new ApplicationShellState(
                shellState.ApplicationTitle,
                shellState.ApplicationSubtitle,
                shellState.ViewTitle,
                shellState.ViewDescription,
                shellState.NavigationItems.Select(item => CloneNavigationItem(item, navigationItemId)),
                shellState.TitleBarCommands.Select(CloneCommandItem),
                shellState.StatusItems.Select(CloneStatusItem));
        }

        private static ApplicationShellNavigationItem CloneNavigationItem(ApplicationShellNavigationItem item, string selectedNavigationItemId)
        {
            var clone = new ApplicationShellNavigationItem(item.ItemId, item.DisplayText)
            {
                Description = item.Description,
                IconGlyph = item.IconGlyph,
                IsEnabled = item.IsEnabled,
                IsSelected = string.Equals(item.ItemId, selectedNavigationItemId, StringComparison.OrdinalIgnoreCase),
            };

            return clone;
        }

        private static ApplicationShellCommandItem CloneCommandItem(ApplicationShellCommandItem item)
        {
            var clone = new ApplicationShellCommandItem(item.CommandId, item.DisplayText, item.Placement)
            {
                ToolTipText = item.ToolTipText,
                IconGlyph = item.IconGlyph,
                IsEnabled = item.IsEnabled,
            };

            return clone;
        }

        private static ApplicationShellStatusItem CloneStatusItem(ApplicationShellStatusItem item)
        {
            return new ApplicationShellStatusItem(item.ItemId, item.LabelText, item.ValueText);
        }
    }
}
