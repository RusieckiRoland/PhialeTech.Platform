using System;
using System.Collections.Generic;
using NUnit.Framework;
using PhialeTech.Shell.Abstractions.Presentation;
using PhialeTech.Shell.Presentation;

namespace PhialeTech.Shell.Tests.Presentation
{
    [TestFixture]
    public sealed class ApplicationShellStateValidatorTests
    {
        [Test]
        public void Validate_ShouldAcceptStateWithExactlyOneSelectedNavigationItem()
        {
            var state = CreateShellState();

            Assert.DoesNotThrow(() => ApplicationShellStateValidator.Validate(state));
        }

        [Test]
        public void Validate_ShouldRejectStateWithoutSelectedNavigationItem()
        {
            var state = new ApplicationShellState(
                "PhialeTech",
                "Platform shell",
                "Dashboard",
                "Overview surface",
                new[]
                {
                    new ApplicationShellNavigationItem("overview", "Overview"),
                    new ApplicationShellNavigationItem("components", "Components"),
                },
                new[]
                {
                    new ApplicationShellCommandItem("shell.refresh", "Refresh", ApplicationShellCommandPlacement.Trailing),
                },
                new[]
                {
                    new ApplicationShellStatusItem("status.branch", "Branch", "main"),
                });

            var exception = Assert.Throws<InvalidOperationException>(() => ApplicationShellStateValidator.Validate(state));
            Assert.That(exception.Message, Does.Contain("exactly one selected navigation item"));
        }

        [Test]
        public void SelectNavigation_ShouldReturnStateWithRequestedItemSelected()
        {
            var state = CreateShellState();

            var transitioned = ApplicationShellStateTransitions.SelectNavigation(state, "components");

            Assert.That(transitioned.NavigationItems[0].IsSelected, Is.False);
            Assert.That(transitioned.NavigationItems[1].IsSelected, Is.True);
        }

        [Test]
        public void SelectNavigation_ShouldRejectUnknownItemId()
        {
            var state = CreateShellState();

            var exception = Assert.Throws<InvalidOperationException>(() => ApplicationShellStateTransitions.SelectNavigation(state, "missing"));
            Assert.That(exception.Message, Does.Contain("does not exist"));
        }

        private static ApplicationShellState CreateShellState()
        {
            return new ApplicationShellState(
                "PhialeTech",
                "Platform shell",
                "Dashboard",
                "Overview surface",
                new List<ApplicationShellNavigationItem>
                {
                    new ApplicationShellNavigationItem("overview", "Overview")
                    {
                        IsSelected = true,
                        IconGlyph = "\uE80F",
                    },
                    new ApplicationShellNavigationItem("components", "Components")
                    {
                        IconGlyph = "\uE8A5",
                    },
                },
                new List<ApplicationShellCommandItem>
                {
                    new ApplicationShellCommandItem("shell.refresh", "Refresh", ApplicationShellCommandPlacement.Trailing),
                    new ApplicationShellCommandItem("shell.search", "Search", ApplicationShellCommandPlacement.Leading),
                },
                new List<ApplicationShellStatusItem>
                {
                    new ApplicationShellStatusItem("status.branch", "Branch", "main"),
                    new ApplicationShellStatusItem("status.mode", "Theme", "Day"),
                });
        }
    }
}
