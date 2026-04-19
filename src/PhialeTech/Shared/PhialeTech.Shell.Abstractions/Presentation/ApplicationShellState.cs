using System;
using System.Collections.Generic;
using System.Linq;

namespace PhialeTech.Shell.Abstractions.Presentation
{
    public sealed class ApplicationShellState
    {
        public ApplicationShellState(
            string applicationTitle,
            string applicationSubtitle,
            string viewTitle,
            string viewDescription,
            IEnumerable<ApplicationShellNavigationItem> navigationItems,
            IEnumerable<ApplicationShellCommandItem> titleBarCommands,
            IEnumerable<ApplicationShellStatusItem> statusItems)
        {
            if (string.IsNullOrWhiteSpace(applicationTitle))
            {
                throw new ArgumentException("Application title is required.", nameof(applicationTitle));
            }

            if (string.IsNullOrWhiteSpace(viewTitle))
            {
                throw new ArgumentException("View title is required.", nameof(viewTitle));
            }

            ApplicationTitle = applicationTitle;
            ApplicationSubtitle = applicationSubtitle ?? string.Empty;
            ViewTitle = viewTitle;
            ViewDescription = viewDescription ?? string.Empty;
            NavigationItems = (navigationItems ?? throw new ArgumentNullException(nameof(navigationItems))).ToList().AsReadOnly();
            TitleBarCommands = (titleBarCommands ?? throw new ArgumentNullException(nameof(titleBarCommands))).ToList().AsReadOnly();
            StatusItems = (statusItems ?? throw new ArgumentNullException(nameof(statusItems))).ToList().AsReadOnly();
        }

        public string ApplicationTitle { get; }

        public string ApplicationSubtitle { get; }

        public string ViewTitle { get; }

        public string ViewDescription { get; }

        public IReadOnlyList<ApplicationShellNavigationItem> NavigationItems { get; }

        public IReadOnlyList<ApplicationShellCommandItem> TitleBarCommands { get; }

        public IReadOnlyList<ApplicationShellStatusItem> StatusItems { get; }
    }
}
