using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NUnit.Framework;
using PhialeGrid.Core.Regions;
using PhialeTech.PhialeGrid.Wpf.Regions;

namespace PhialeGrid.Wpf.Tests.Regions
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public sealed class WpfGridRegionLayoutAdapterTests
    {
        [Test]
        public void Apply_MapsTopCommandStripOpenCollapsedAndClosedStates_ToCompactRowsAndVisibility()
        {
            var strip = CreateStripBinding(GridRegionKind.TopCommandRegion);
            var pane = CreatePaneBinding();
            var adapter = new WpfGridRegionLayoutAdapter(
                new[]
                {
                    strip,
                    CreateStripBinding(GridRegionKind.GroupingRegion),
                    CreateStripBinding(GridRegionKind.SummaryBottomRegion),
                },
                pane);

            var states = CreateDefaultStates();
            states[GridRegionKind.TopCommandRegion] = CreateState(GridRegionKind.TopCommandRegion, GridRegionState.Open, 44d);

            adapter.Apply(
                states.Values,
                new WpfGridRegionRenderSnapshot(
                    new Dictionary<GridRegionKind, bool>
                    {
                        [GridRegionKind.TopCommandRegion] = true,
                        [GridRegionKind.GroupingRegion] = true,
                        [GridRegionKind.SummaryBottomRegion] = true,
                        [GridRegionKind.SideToolRegion] = false,
                    },
                    new Dictionary<GridRegionKind, WpfGridRegionRenderDirectives>
                    {
                        [GridRegionKind.TopCommandRegion] = new WpfGridRegionRenderDirectives(forceCompactSize: false, allowResize: false),
                        [GridRegionKind.GroupingRegion] = new WpfGridRegionRenderDirectives(forceCompactSize: false, allowResize: true),
                        [GridRegionKind.SummaryBottomRegion] = new WpfGridRegionRenderDirectives(forceCompactSize: false, allowResize: false),
                    },
                    usePaneDrawerChrome: false));

            Assert.Multiple(() =>
            {
                Assert.That(strip.Host.Visibility, Is.EqualTo(Visibility.Visible));
                Assert.That(strip.ContentHost.Visibility, Is.EqualTo(Visibility.Visible));
                Assert.That(strip.Row.Height.Value, Is.EqualTo(44d).Within(0.1d));
                Assert.That(strip.SplitterRow.Height.Value, Is.EqualTo(0d).Within(0.1d));
            });

            states[GridRegionKind.TopCommandRegion] = CreateState(GridRegionKind.TopCommandRegion, GridRegionState.Collapsed, 44d, canCollapse: true);
            adapter.Apply(states.Values, CreateRenderableSnapshot(top: true, side: false, drawerChrome: false));

            Assert.Multiple(() =>
            {
                Assert.That(strip.Host.Visibility, Is.EqualTo(Visibility.Visible));
                Assert.That(strip.ContentHost.Visibility, Is.EqualTo(Visibility.Collapsed));
                Assert.That(strip.Row.Height.Value, Is.EqualTo(36d).Within(0.1d));
            });

            states[GridRegionKind.TopCommandRegion] = CreateState(GridRegionKind.TopCommandRegion, GridRegionState.Closed, 44d);
            adapter.Apply(states.Values, CreateRenderableSnapshot(top: true, side: false, drawerChrome: false));

            Assert.Multiple(() =>
            {
                Assert.That(strip.Host.Visibility, Is.EqualTo(Visibility.Collapsed));
                Assert.That(strip.Row.Height.Value, Is.EqualTo(0d).Within(0.1d));
                Assert.That(strip.SplitterRow.Height.Value, Is.EqualTo(0d).Within(0.1d));
            });
        }

        [Test]
        public void Apply_MapsPaneDrawerCollapsedAndOpenState_ToDrawerChromeAndActiveZIndex()
        {
            var pane = CreatePaneBinding();
            var adapter = new WpfGridRegionLayoutAdapter(
                new[]
                {
                    CreateStripBinding(GridRegionKind.TopCommandRegion),
                    CreateStripBinding(GridRegionKind.GroupingRegion),
                    CreateStripBinding(GridRegionKind.SummaryBottomRegion),
                },
                pane);
            var states = CreateDefaultStates();
            states[GridRegionKind.SideToolRegion] = CreatePaneState(GridRegionState.Collapsed, 380d, isActive: false);

            adapter.Apply(states.Values, CreateRenderableSnapshot(top: false, side: true, drawerChrome: true));

            Assert.Multiple(() =>
            {
                Assert.That(pane.Host.Visibility, Is.EqualTo(Visibility.Visible));
                Assert.That(pane.CollapsedRail.Visibility, Is.EqualTo(Visibility.Visible));
                Assert.That(pane.ExpandedCard.Visibility, Is.EqualTo(Visibility.Collapsed));
                Assert.That(pane.RegionColumn.Width.Value, Is.EqualTo(44d).Within(0.1d));
                Assert.That(Panel.GetZIndex(pane.Host), Is.EqualTo(0));
            });

            states[GridRegionKind.SideToolRegion] = CreatePaneState(GridRegionState.Open, 380d, isActive: true);
            adapter.Apply(states.Values, CreateRenderableSnapshot(top: false, side: true, drawerChrome: true));

            Assert.Multiple(() =>
            {
                Assert.That(pane.CollapsedRail.Visibility, Is.EqualTo(Visibility.Collapsed));
                Assert.That(pane.ExpandedCard.Visibility, Is.EqualTo(Visibility.Visible));
                Assert.That(pane.ContentHost.Visibility, Is.EqualTo(Visibility.Visible));
                Assert.That(Panel.GetZIndex(pane.Host), Is.EqualTo(1));
            });
        }

        [Test]
        public void Apply_WhenPaneIsCollapsedWithoutDrawerChrome_UsesRailWidthAndDoesNotLeaveExpandedShellVisible()
        {
            var pane = CreatePaneBinding();
            var adapter = new WpfGridRegionLayoutAdapter(
                new[]
                {
                    CreateStripBinding(GridRegionKind.TopCommandRegion),
                    CreateStripBinding(GridRegionKind.GroupingRegion),
                    CreateStripBinding(GridRegionKind.SummaryBottomRegion),
                },
                pane);
            var states = CreateDefaultStates();
            states[GridRegionKind.SideToolRegion] = CreatePaneState(GridRegionState.Collapsed, 380d, isActive: false);

            adapter.Apply(states.Values, CreateRenderableSnapshot(top: false, side: true, drawerChrome: false));

            Assert.Multiple(() =>
            {
                Assert.That(pane.Host.Visibility, Is.EqualTo(Visibility.Visible));
                Assert.That(pane.CollapsedRail.Visibility, Is.EqualTo(Visibility.Visible));
                Assert.That(pane.ExpandedCard.Visibility, Is.EqualTo(Visibility.Collapsed));
                Assert.That(pane.ContentHost.Visibility, Is.EqualTo(Visibility.Collapsed));
                Assert.That(pane.RegionColumn.Width.Value, Is.EqualTo(44d).Within(0.1d));
                Assert.That(pane.SplitterColumn.Width.Value, Is.EqualTo(0d).Within(0.1d));
            });
        }

        [Test]
        public void Apply_WhenPaneIsClosed_RemovesPaneFromLayoutCompletely()
        {
            var pane = CreatePaneBinding();
            var adapter = new WpfGridRegionLayoutAdapter(
                new[]
                {
                    CreateStripBinding(GridRegionKind.TopCommandRegion),
                    CreateStripBinding(GridRegionKind.GroupingRegion),
                    CreateStripBinding(GridRegionKind.SummaryBottomRegion),
                },
                pane);
            var states = CreateDefaultStates();
            states[GridRegionKind.SideToolRegion] = CreatePaneState(GridRegionState.Closed, 320d, isActive: false);

            adapter.Apply(states.Values, CreateRenderableSnapshot(top: false, side: true, drawerChrome: false));

            Assert.Multiple(() =>
            {
                Assert.That(pane.Host.Visibility, Is.EqualTo(Visibility.Collapsed));
                Assert.That(pane.RegionColumn.Width.Value, Is.EqualTo(0d).Within(0.1d));
                Assert.That(pane.RegionColumn.MinWidth, Is.EqualTo(0d).Within(0.1d));
                Assert.That(pane.SplitterColumn.Width.Value, Is.EqualTo(0d).Within(0.1d));
                Assert.That(pane.CollapsedRail.Visibility, Is.EqualTo(Visibility.Collapsed));
                Assert.That(pane.ExpandedCard.Visibility, Is.EqualTo(Visibility.Collapsed));
            });
        }

        [Test]
        public void Apply_HidesRegionsWithoutRenderableContent_EvenWhenCoreStateIsOpen()
        {
            var strip = CreateStripBinding(GridRegionKind.TopCommandRegion);
            var pane = CreatePaneBinding();
            var adapter = new WpfGridRegionLayoutAdapter(
                new[]
                {
                    strip,
                    CreateStripBinding(GridRegionKind.GroupingRegion),
                    CreateStripBinding(GridRegionKind.SummaryBottomRegion),
                },
                pane);
            var states = CreateDefaultStates();
            states[GridRegionKind.TopCommandRegion] = CreateState(GridRegionKind.TopCommandRegion, GridRegionState.Open, 44d);
            states[GridRegionKind.SideToolRegion] = CreatePaneState(GridRegionState.Open, 320d, isActive: false);

            adapter.Apply(states.Values, CreateRenderableSnapshot(top: false, side: false, drawerChrome: false));

            Assert.Multiple(() =>
            {
                Assert.That(strip.Host.Visibility, Is.EqualTo(Visibility.Collapsed));
                Assert.That(strip.Row.Height.Value, Is.EqualTo(0d).Within(0.1d));
                Assert.That(pane.Host.Visibility, Is.EqualTo(Visibility.Collapsed));
                Assert.That(pane.RegionColumn.Width.Value, Is.EqualTo(0d).Within(0.1d));
            });
        }

        [Test]
        public void Apply_FailsFast_WhenRequiredCoreRegionsAreMissing()
        {
            var adapter = new WpfGridRegionLayoutAdapter(
                new[]
                {
                    CreateStripBinding(GridRegionKind.TopCommandRegion),
                    CreateStripBinding(GridRegionKind.GroupingRegion),
                    CreateStripBinding(GridRegionKind.SummaryBottomRegion),
                },
                CreatePaneBinding());

            Assert.That(
                () => adapter.Apply(
                    new[]
                    {
                        CreateState(GridRegionKind.TopCommandRegion, GridRegionState.Open, 44d),
                        CreateState(GridRegionKind.GroupingRegion, GridRegionState.Open, 56d),
                    },
                    CreateRenderableSnapshot(top: true, side: false, drawerChrome: false)),
                Throws.InvalidOperationException.With.Message.Contains("CoreGridSurface"));
        }

        [Test]
        public void Apply_ExposesChromeState_WithoutRecomputingCoreSemantics()
        {
            var adapter = new WpfGridRegionLayoutAdapter(
                new[]
                {
                    CreateStripBinding(GridRegionKind.TopCommandRegion),
                    CreateStripBinding(GridRegionKind.GroupingRegion),
                    CreateStripBinding(GridRegionKind.SummaryBottomRegion),
                },
                CreatePaneBinding());

            var states = CreateDefaultStates();
            states[GridRegionKind.GroupingRegion] = new GridRegionViewState(
                GridRegionKind.GroupingRegion,
                GridRegionHostKind.Strip,
                GridRegionPlacement.Top,
                GridRegionContentKind.GroupingDropZone,
                GridRegionState.Open,
                isAvailable: true,
                isActive: false,
                canCollapse: true,
                canClose: true,
                canResize: true,
                canActivate: false,
                size: 180d,
                minSize: 56d,
                maxSize: 220d);

            adapter.Apply(states.Values, CreateRenderableSnapshot(top: true, side: false, drawerChrome: false));

            var chrome = adapter.GetChromeState(GridRegionKind.GroupingRegion);
            Assert.Multiple(() =>
            {
                Assert.That(chrome.CanCollapse, Is.True);
                Assert.That(chrome.CanClose, Is.True);
                Assert.That(chrome.ToggleText, Is.EqualTo("˄"));
            });
        }

        [Test]
        public void Apply_UsesCompactTopStripDirectives_AndDoesNotExposeLegacyResizablePanelBehavior()
        {
            var strip = CreateStripBinding(GridRegionKind.TopCommandRegion);
            var adapter = new WpfGridRegionLayoutAdapter(
                new[]
                {
                    strip,
                    CreateStripBinding(GridRegionKind.GroupingRegion),
                    CreateStripBinding(GridRegionKind.SummaryBottomRegion),
                },
                CreatePaneBinding());

            var states = CreateDefaultStates();
            states[GridRegionKind.TopCommandRegion] = CreateState(GridRegionKind.TopCommandRegion, GridRegionState.Open, 44d, canCollapse: true);

            adapter.Apply(states.Values, CreateRenderableSnapshot(top: true, side: false, drawerChrome: false));

            var chrome = adapter.GetChromeState(GridRegionKind.TopCommandRegion);
            Assert.Multiple(() =>
            {
                Assert.That(strip.Row.Height.Value, Is.EqualTo(44d).Within(0.1d));
                Assert.That(strip.SplitterRow.Height.Value, Is.EqualTo(0d).Within(0.1d));
                Assert.That(chrome.CanCollapse, Is.True);
                Assert.That(chrome.CanClose, Is.True);
            });
        }

        private static Dictionary<GridRegionKind, GridRegionViewState> CreateDefaultStates()
        {
            return new Dictionary<GridRegionKind, GridRegionViewState>
            {
                [GridRegionKind.CoreGridSurface] = new GridRegionViewState(
                    GridRegionKind.CoreGridSurface,
                    GridRegionHostKind.Surface,
                    GridRegionPlacement.Center,
                    GridRegionContentKind.GridSurface,
                    GridRegionState.Open,
                    isAvailable: true,
                    isActive: false,
                    canCollapse: false,
                    canClose: false,
                    canResize: false,
                    canActivate: false,
                    size: null,
                    minSize: null,
                    maxSize: null),
                [GridRegionKind.TopCommandRegion] = CreateState(GridRegionKind.TopCommandRegion, GridRegionState.Closed, 44d),
                [GridRegionKind.GroupingRegion] = CreateState(GridRegionKind.GroupingRegion, GridRegionState.Open, 56d),
                [GridRegionKind.SummaryBottomRegion] = CreateState(GridRegionKind.SummaryBottomRegion, GridRegionState.Open, 56d),
                [GridRegionKind.SideToolRegion] = CreatePaneState(GridRegionState.Closed, 320d, isActive: false),
            };
        }

        private static GridRegionViewState CreateState(GridRegionKind regionKind, GridRegionState state, double size, bool? canCollapse = null)
        {
            return new GridRegionViewState(
                regionKind,
                GridRegionHostKind.Strip,
                regionKind == GridRegionKind.SummaryBottomRegion ? GridRegionPlacement.Bottom : GridRegionPlacement.Top,
                regionKind == GridRegionKind.TopCommandRegion ? GridRegionContentKind.CommandBar :
                    regionKind == GridRegionKind.GroupingRegion ? GridRegionContentKind.GroupingDropZone :
                    GridRegionContentKind.Summary,
                state,
                isAvailable: true,
                isActive: false,
                canCollapse: canCollapse ?? regionKind == GridRegionKind.GroupingRegion,
                canClose: true,
                canResize: regionKind == GridRegionKind.GroupingRegion,
                canActivate: false,
                size: size,
                minSize: regionKind == GridRegionKind.TopCommandRegion ? 36d : 56d,
                maxSize: regionKind == GridRegionKind.TopCommandRegion ? 44d : regionKind == GridRegionKind.GroupingRegion ? 220d : 180d);
        }

        private static GridRegionViewState CreatePaneState(GridRegionState state, double size, bool isActive)
        {
            return new GridRegionViewState(
                GridRegionKind.SideToolRegion,
                GridRegionHostKind.Pane,
                GridRegionPlacement.Right,
                GridRegionContentKind.ToolPane,
                state,
                isAvailable: true,
                isActive: isActive,
                canCollapse: true,
                canClose: true,
                canResize: true,
                canActivate: true,
                size: size,
                minSize: 220d,
                maxSize: 520d);
        }

        private static WpfGridRegionStripBinding CreateStripBinding(GridRegionKind regionKind)
        {
            return new WpfGridRegionStripBinding(
                regionKind,
                new RowDefinition(),
                new RowDefinition(),
                new Border(),
                new Border(),
                regionKind == GridRegionKind.TopCommandRegion ? 44d : 56d);
        }

        private static WpfGridRegionPaneBinding CreatePaneBinding()
        {
            return new WpfGridRegionPaneBinding(
                GridRegionKind.SideToolRegion,
                new ColumnDefinition(),
                new ColumnDefinition(),
                new Border(),
                new Border(),
                new Border(),
                new Border(),
                new TranslateTransform(),
                320d);
        }

        private static WpfGridRegionRenderSnapshot CreateRenderableSnapshot(bool top, bool side, bool drawerChrome)
        {
            return new WpfGridRegionRenderSnapshot(
                new Dictionary<GridRegionKind, bool>
                {
                    [GridRegionKind.TopCommandRegion] = top,
                    [GridRegionKind.GroupingRegion] = true,
                    [GridRegionKind.SummaryBottomRegion] = true,
                    [GridRegionKind.SideToolRegion] = side,
                },
                new Dictionary<GridRegionKind, WpfGridRegionRenderDirectives>
                {
                    [GridRegionKind.TopCommandRegion] = new WpfGridRegionRenderDirectives(forceCompactSize: false, allowResize: false),
                    [GridRegionKind.GroupingRegion] = new WpfGridRegionRenderDirectives(forceCompactSize: false, allowResize: true),
                    [GridRegionKind.SummaryBottomRegion] = new WpfGridRegionRenderDirectives(forceCompactSize: false, allowResize: false),
                },
                drawerChrome);
        }
    }
}
