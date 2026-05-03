using System;
using System.Collections.Generic;
using System.Linq;
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
        public void Apply_MapsTopCommandBandOpenAndClosedStates_ToFullRowsAndVisibility()
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
            states[GridRegionKind.TopCommandRegion] = CreateState(GridRegionKind.TopCommandRegion, GridRegionState.Open, 52d);

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
                        [GridRegionKind.TopCommandRegion] = new WpfGridRegionRenderDirectives(forceCompactSize: false),
                        [GridRegionKind.GroupingRegion] = new WpfGridRegionRenderDirectives(forceCompactSize: false),
                        [GridRegionKind.SummaryBottomRegion] = new WpfGridRegionRenderDirectives(forceCompactSize: false),
                    },
                    useWorkspacePanelDrawerChrome: false));

            Assert.Multiple(() =>
            {
                Assert.That(strip.Host.Visibility, Is.EqualTo(Visibility.Visible));
                Assert.That(strip.ContentHost.Visibility, Is.EqualTo(Visibility.Visible));
                Assert.That(strip.Row.Height.Value, Is.EqualTo(52d).Within(0.1d));
                Assert.That(strip.SplitterRow.Height.Value, Is.EqualTo(0d).Within(0.1d));
            });

            states[GridRegionKind.TopCommandRegion] = CreateState(GridRegionKind.TopCommandRegion, GridRegionState.Closed, 52d);
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
        public void Apply_WhenWorkspacePanelMovesLeftOrRight_RepositionsPaneSplitterAndSurfaceColumns()
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
            states[GridRegionKind.SideToolRegion] = CreatePaneState(GridRegionState.Open, 320d, isActive: false, placement: GridRegionPlacement.Left);

            adapter.Apply(states.Values, CreateRenderableSnapshot(top: false, side: true, drawerChrome: false));

            Assert.Multiple(() =>
            {
                Assert.That(Grid.GetColumn(pane.Host), Is.EqualTo(0));
                Assert.That(Grid.GetColumn(pane.Splitter), Is.EqualTo(1));
                Assert.That(pane.SurfaceHosts.All(host => Grid.GetColumn(host) == 2), Is.True);
                Assert.That(pane.SurfaceColumn.Width.Value, Is.EqualTo(320d).Within(0.1d));
                Assert.That(pane.RegionColumn.Width.IsStar, Is.True);
            });

            states[GridRegionKind.SideToolRegion] = CreatePaneState(GridRegionState.Open, 320d, isActive: false, placement: GridRegionPlacement.Right);
            adapter.Apply(states.Values, CreateRenderableSnapshot(top: false, side: true, drawerChrome: false));

            Assert.Multiple(() =>
            {
                Assert.That(pane.SurfaceHosts.All(host => Grid.GetColumn(host) == 0), Is.True);
                Assert.That(Grid.GetColumn(pane.Splitter), Is.EqualTo(1));
                Assert.That(Grid.GetColumn(pane.Host), Is.EqualTo(2));
                Assert.That(pane.SurfaceColumn.Width.IsStar, Is.True);
                Assert.That(pane.RegionColumn.Width.Value, Is.EqualTo(320d).Within(0.1d));
            });
        }

        [Test]
        public void Apply_WhenWorkspacePanelPlacementChanges_UsesToggleDirectionForThatSide()
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

            states[GridRegionKind.SideToolRegion] = CreatePaneState(GridRegionState.Open, 320d, isActive: false, placement: GridRegionPlacement.Right);
            adapter.Apply(states.Values, CreateRenderableSnapshot(top: false, side: true, drawerChrome: false));
            var rightOpen = adapter.GetChromeState(GridRegionKind.SideToolRegion).ToggleText;

            states[GridRegionKind.SideToolRegion] = CreatePaneState(GridRegionState.Collapsed, 320d, isActive: false, placement: GridRegionPlacement.Right);
            adapter.Apply(states.Values, CreateRenderableSnapshot(top: false, side: true, drawerChrome: false));
            var rightCollapsed = adapter.GetChromeState(GridRegionKind.SideToolRegion).ToggleText;

            states[GridRegionKind.SideToolRegion] = CreatePaneState(GridRegionState.Open, 320d, isActive: false, placement: GridRegionPlacement.Left);
            adapter.Apply(states.Values, CreateRenderableSnapshot(top: false, side: true, drawerChrome: false));
            var leftOpen = adapter.GetChromeState(GridRegionKind.SideToolRegion).ToggleText;

            states[GridRegionKind.SideToolRegion] = CreatePaneState(GridRegionState.Collapsed, 320d, isActive: false, placement: GridRegionPlacement.Left);
            adapter.Apply(states.Values, CreateRenderableSnapshot(top: false, side: true, drawerChrome: false));
            var leftCollapsed = adapter.GetChromeState(GridRegionKind.SideToolRegion).ToggleText;

            Assert.Multiple(() =>
            {
                Assert.That(rightOpen, Is.EqualTo(">"));
                Assert.That(rightCollapsed, Is.EqualTo("<"));
                Assert.That(leftOpen, Is.EqualTo("<"));
                Assert.That(leftCollapsed, Is.EqualTo(">"));
            });
        }

        [Test]
        public void Apply_WhenWorkspaceBandMovesTopOrBottom_ReparentsBandHostToWorkspaceBandStacks()
        {
            var groupingBand = CreateStripBinding(GridRegionKind.GroupingRegion);
            var adapter = new WpfGridRegionLayoutAdapter(
                new[]
                {
                    CreateStripBinding(GridRegionKind.TopCommandRegion),
                    groupingBand,
                    CreateStripBinding(GridRegionKind.SummaryBottomRegion),
                },
                CreatePaneBinding());
            var states = CreateDefaultStates();

            states[GridRegionKind.GroupingRegion] = CreateState(
                GridRegionKind.GroupingRegion,
                GridRegionState.Open,
                56d,
                placement: GridRegionPlacement.Bottom);
            adapter.Apply(states.Values, CreateRenderableSnapshot(top: true, side: false, drawerChrome: false));

            Assert.That(groupingBand.Host.Parent, Is.SameAs(groupingBand.BottomWorkspaceBandHost));

            states[GridRegionKind.GroupingRegion] = CreateState(
                GridRegionKind.GroupingRegion,
                GridRegionState.Open,
                56d,
                placement: GridRegionPlacement.Top);
            adapter.Apply(states.Values, CreateRenderableSnapshot(top: true, side: false, drawerChrome: false));

            Assert.That(groupingBand.Host.Parent, Is.SameAs(groupingBand.TopWorkspaceBandHost));
        }

        [Test]
        public void Apply_WhenTopCommandBandMovesBottom_CollapsesOriginalTopCommandRowAndUsesBottomHostSize()
        {
            var topCommandBand = CreateStripBinding(GridRegionKind.TopCommandRegion);
            var adapter = new WpfGridRegionLayoutAdapter(
                new[]
                {
                    topCommandBand,
                    CreateStripBinding(GridRegionKind.GroupingRegion),
                    CreateStripBinding(GridRegionKind.SummaryBottomRegion),
                },
                CreatePaneBinding());
            var states = CreateDefaultStates();
            states[GridRegionKind.TopCommandRegion] = CreateState(
                GridRegionKind.TopCommandRegion,
                GridRegionState.Open,
                52d,
                placement: GridRegionPlacement.Bottom);

            adapter.Apply(states.Values, CreateRenderableSnapshot(top: true, side: false, drawerChrome: false));

            Assert.Multiple(() =>
            {
                Assert.That(topCommandBand.Host.Parent, Is.SameAs(topCommandBand.BottomWorkspaceBandHost));
                Assert.That(topCommandBand.Row.Height.Value, Is.EqualTo(0d).Within(0.1d));
                Assert.That(topCommandBand.Host.Height, Is.EqualTo(52d).Within(0.1d));
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
            states[GridRegionKind.TopCommandRegion] = CreateState(GridRegionKind.TopCommandRegion, GridRegionState.Open, 52d);
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
                        CreateState(GridRegionKind.TopCommandRegion, GridRegionState.Open, 52d),
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
                GridRegionHostKind.WorkspaceBand,
                GridRegionPlacement.Top,
                GridRegionContentKind.GroupingDropZone,
                GridRegionState.Open,
                isAvailable: true,
                isActive: false,
                canCollapse: false,
                canClose: true,
                canResize: false,
                canActivate: false,
                size: 180d,
                minSize: 56d,
                maxSize: 220d);

            adapter.Apply(states.Values, CreateRenderableSnapshot(top: true, side: false, drawerChrome: false));

            var chrome = adapter.GetChromeState(GridRegionKind.GroupingRegion);
            Assert.Multiple(() =>
            {
                Assert.That(chrome.CanCollapse, Is.False);
                Assert.That(chrome.CanClose, Is.True);
                Assert.That(chrome.ToggleText, Is.EqualTo("˄"));
            });
        }

        [Test]
        public void Apply_UsesTopBandDirectives_AndDoesNotExposeWorkspacePanelResizeBehavior()
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
            states[GridRegionKind.TopCommandRegion] = CreateState(GridRegionKind.TopCommandRegion, GridRegionState.Open, 52d);

            adapter.Apply(states.Values, CreateRenderableSnapshot(top: true, side: false, drawerChrome: false));

            var chrome = adapter.GetChromeState(GridRegionKind.TopCommandRegion);
            Assert.Multiple(() =>
            {
                Assert.That(strip.Row.Height.Value, Is.EqualTo(52d).Within(0.1d));
                Assert.That(strip.SplitterRow.Height.Value, Is.EqualTo(0d).Within(0.1d));
                Assert.That(chrome.CanCollapse, Is.False);
                Assert.That(chrome.CanClose, Is.True);
            });
        }

        private static Dictionary<GridRegionKind, GridRegionViewState> CreateDefaultStates()
        {
            return new Dictionary<GridRegionKind, GridRegionViewState>
            {
                [GridRegionKind.CoreGridSurface] = new GridRegionViewState(
                    GridRegionKind.CoreGridSurface,
                    GridRegionHostKind.CoreSurface,
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
                [GridRegionKind.TopCommandRegion] = CreateState(GridRegionKind.TopCommandRegion, GridRegionState.Closed, 52d),
                [GridRegionKind.GroupingRegion] = CreateState(GridRegionKind.GroupingRegion, GridRegionState.Open, 56d),
                [GridRegionKind.SummaryBottomRegion] = CreateState(GridRegionKind.SummaryBottomRegion, GridRegionState.Open, 56d),
                [GridRegionKind.SideToolRegion] = CreatePaneState(GridRegionState.Closed, 320d, isActive: false),
            };
        }

        private static GridRegionViewState CreateState(
            GridRegionKind regionKind,
            GridRegionState state,
            double size,
            bool? canCollapse = null,
            GridRegionPlacement? placement = null)
        {
            return new GridRegionViewState(
                regionKind,
                GridRegionHostKind.WorkspaceBand,
                placement ?? (regionKind == GridRegionKind.SummaryBottomRegion ? GridRegionPlacement.Bottom : GridRegionPlacement.Top),
                regionKind == GridRegionKind.TopCommandRegion ? GridRegionContentKind.CommandBar :
                    regionKind == GridRegionKind.GroupingRegion ? GridRegionContentKind.GroupingDropZone :
                    GridRegionContentKind.Summary,
                state,
                isAvailable: true,
                isActive: false,
                canCollapse: canCollapse ?? false,
                canClose: true,
                canResize: false,
                canActivate: false,
                size: size,
                minSize: regionKind == GridRegionKind.TopCommandRegion ? 52d : 56d,
                maxSize: regionKind == GridRegionKind.TopCommandRegion ? 52d : 56d);
        }

        private static GridRegionViewState CreatePaneState(
            GridRegionState state,
            double size,
            bool isActive,
            GridRegionPlacement placement = GridRegionPlacement.Right)
        {
            return new GridRegionViewState(
                GridRegionKind.SideToolRegion,
                GridRegionHostKind.WorkspacePanel,
                placement,
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

        private static WpfGridWorkspaceBandBinding CreateStripBinding(GridRegionKind regionKind)
        {
            var topHost = new StackPanel();
            var bottomHost = new StackPanel();
            var host = new Border();
            topHost.Children.Add(host);

            return new WpfGridWorkspaceBandBinding(
                regionKind,
                new RowDefinition(),
                new RowDefinition(),
                topHost,
                bottomHost,
                host,
                new Border(),
                regionKind == GridRegionKind.TopCommandRegion ? 52d : 56d,
                regionKind != GridRegionKind.TopCommandRegion);
        }

        private static WpfGridWorkspacePanelBinding CreatePaneBinding()
        {
            var host = new Border();
            host.Resources["PgGridSideToolCollapsedRailWidth"] = 44d;

            return new WpfGridWorkspacePanelBinding(
                GridRegionKind.SideToolRegion,
                new ColumnDefinition(),
                new ColumnDefinition(),
                new ColumnDefinition(),
                new FrameworkElement[]
                {
                    new Border(),
                    new Border(),
                },
                new Border(),
                host,
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
                    [GridRegionKind.TopCommandRegion] = new WpfGridRegionRenderDirectives(forceCompactSize: false),
                    [GridRegionKind.GroupingRegion] = new WpfGridRegionRenderDirectives(forceCompactSize: false),
                    [GridRegionKind.SummaryBottomRegion] = new WpfGridRegionRenderDirectives(forceCompactSize: false),
                },
                drawerChrome);
        }
    }
}
