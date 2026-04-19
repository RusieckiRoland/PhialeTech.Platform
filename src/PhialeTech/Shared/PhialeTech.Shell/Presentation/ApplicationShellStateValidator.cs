using System;
using System.Collections.Generic;
using System.Linq;
using PhialeTech.Shell.Abstractions.Presentation;

namespace PhialeTech.Shell.Presentation
{
    public static class ApplicationShellStateValidator
    {
        public static void Validate(ApplicationShellState shellState)
        {
            if (shellState == null)
            {
                throw new ArgumentNullException(nameof(shellState));
            }

            if (string.IsNullOrWhiteSpace(shellState.ApplicationTitle))
            {
                throw new InvalidOperationException("Application shell state requires a non-empty application title.");
            }

            if (string.IsNullOrWhiteSpace(shellState.ViewTitle))
            {
                throw new InvalidOperationException("Application shell state requires a non-empty view title.");
            }

            EnsureUniqueIds(shellState.NavigationItems.Select(item => item == null ? null : item.ItemId), "navigation");
            EnsureUniqueIds(shellState.TitleBarCommands.Select(item => item == null ? null : item.CommandId), "title bar command");
            EnsureUniqueIds(shellState.StatusItems.Select(item => item == null ? null : item.ItemId), "status");

            if (shellState.NavigationItems.Any(item => item == null))
            {
                throw new InvalidOperationException("Application shell state cannot contain null navigation items.");
            }

            if (shellState.TitleBarCommands.Any(item => item == null))
            {
                throw new InvalidOperationException("Application shell state cannot contain null title bar commands.");
            }

            if (shellState.StatusItems.Any(item => item == null))
            {
                throw new InvalidOperationException("Application shell state cannot contain null status items.");
            }

            if (shellState.NavigationItems.Count > 0)
            {
                var selectedCount = shellState.NavigationItems.Count(item => item.IsSelected);
                if (selectedCount != 1)
                {
                    throw new InvalidOperationException("Application shell state requires exactly one selected navigation item.");
                }
            }
        }

        private static void EnsureUniqueIds(IEnumerable<string> ids, string itemKind)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var id in ids)
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    throw new InvalidOperationException("Application shell state contains a " + itemKind + " item without a required id.");
                }

                if (!seen.Add(id))
                {
                    throw new InvalidOperationException("Application shell state contains duplicate " + itemKind + " ids. Duplicate id: " + id);
                }
            }
        }
    }
}
