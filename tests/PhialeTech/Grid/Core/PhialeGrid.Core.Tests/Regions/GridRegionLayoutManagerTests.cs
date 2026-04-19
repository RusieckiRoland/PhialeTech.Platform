using System;
using System.Linq;
using NUnit.Framework;
using PhialeGrid.Core.Capabilities;
using PhialeGrid.Core.State;

namespace PhialeGrid.Core.Regions.Tests
{
    [TestFixture]
    public sealed class GridRegionLayoutManagerTests
    {
        [Test]
        public void Definition_ValidatesPlacementDefaultStateAndSizeRules()
        {
            Assert.That(
                () => new GridRegionDefinition(
                    GridRegionKind.TopCommandRegion,
                    GridRegionHostKind.Strip,
                    GridRegionPlacement.Right,
                    GridRegionContentKind.CommandBar,
                    GridRegionState.Open,
                    defaultSize: 44d,
                    minSize: 36d,
                    maxSize: 44d,
                    canCollapse: true,
                    canClose: true,
                    canResize: false,
                    canActivate: false),
                Throws.ArgumentException.With.Message.Contains("placement"));

            Assert.That(
                () => new GridRegionDefinition(
                    GridRegionKind.SideToolRegion,
                    GridRegionHostKind.Pane,
                    GridRegionPlacement.Right,
                    GridRegionContentKind.ToolPane,
                    GridRegionState.Collapsed,
                    defaultSize: 320d,
                    minSize: 220d,
                    maxSize: 520d,
                    canCollapse: false,
                    canClose: true,
                    canResize: true,
                    canActivate: true),
                Throws.ArgumentException.With.Message.Contains("collapse"));

            Assert.That(
                () => new GridRegionDefinition(
                    GridRegionKind.SummaryBottomRegion,
                    GridRegionHostKind.Strip,
                    GridRegionPlacement.Bottom,
                    GridRegionContentKind.Summary,
                    GridRegionState.Open,
                    defaultSize: 56d,
                    minSize: 180d,
                    maxSize: 100d,
                    canCollapse: false,
                    canClose: true,
                    canResize: false,
                    canActivate: false),
                Throws.TypeOf<ArgumentOutOfRangeException>().With.Message.Contains("maxSize"));
        }

        [Test]
        public void DefaultCatalog_UsesExplicitPlacementsHostKindsAndContentKinds()
        {
            var definitions = GridRegionDefinitionCatalog.CreateDefault().ToDictionary(definition => definition.RegionKind);

            Assert.Multiple(() =>
            {
                Assert.That(definitions[GridRegionKind.CoreGridSurface].HostKind, Is.EqualTo(GridRegionHostKind.Surface));
                Assert.That(definitions[GridRegionKind.CoreGridSurface].Placement, Is.EqualTo(GridRegionPlacement.Center));
                Assert.That(definitions[GridRegionKind.CoreGridSurface].ContentKind, Is.EqualTo(GridRegionContentKind.GridSurface));

                Assert.That(definitions[GridRegionKind.TopCommandRegion].HostKind, Is.EqualTo(GridRegionHostKind.Strip));
                Assert.That(definitions[GridRegionKind.TopCommandRegion].Placement, Is.EqualTo(GridRegionPlacement.Top));
                Assert.That(definitions[GridRegionKind.TopCommandRegion].ContentKind, Is.EqualTo(GridRegionContentKind.CommandBar));

                Assert.That(definitions[GridRegionKind.GroupingRegion].HostKind, Is.EqualTo(GridRegionHostKind.Strip));
                Assert.That(definitions[GridRegionKind.GroupingRegion].Placement, Is.EqualTo(GridRegionPlacement.Top));
                Assert.That(definitions[GridRegionKind.GroupingRegion].ContentKind, Is.EqualTo(GridRegionContentKind.GroupingDropZone));

                Assert.That(definitions[GridRegionKind.SummaryBottomRegion].HostKind, Is.EqualTo(GridRegionHostKind.Strip));
                Assert.That(definitions[GridRegionKind.SummaryBottomRegion].Placement, Is.EqualTo(GridRegionPlacement.Bottom));
                Assert.That(definitions[GridRegionKind.SummaryBottomRegion].ContentKind, Is.EqualTo(GridRegionContentKind.Summary));

                Assert.That(definitions[GridRegionKind.SideToolRegion].HostKind, Is.EqualTo(GridRegionHostKind.Pane));
                Assert.That(definitions[GridRegionKind.SideToolRegion].Placement, Is.EqualTo(GridRegionPlacement.Right));
                Assert.That(definitions[GridRegionKind.SideToolRegion].ContentKind, Is.EqualTo(GridRegionContentKind.ToolPane));
            });
        }

        [Test]
        public void Constructor_FailsFastWhenDefinitionsContainDuplicateKinds()
        {
            var duplicateDefinitions = new[]
            {
                new GridRegionDefinition(
                    GridRegionKind.TopCommandRegion,
                    GridRegionHostKind.Strip,
                    GridRegionPlacement.Top,
                    GridRegionContentKind.CommandBar,
                    GridRegionState.Open,
                    defaultSize: 44d,
                    minSize: 36d,
                    maxSize: 44d,
                    canCollapse: true,
                    canClose: true,
                    canResize: false,
                    canActivate: false),
                new GridRegionDefinition(
                    GridRegionKind.TopCommandRegion,
                    GridRegionHostKind.Strip,
                    GridRegionPlacement.Top,
                    GridRegionContentKind.CommandBar,
                    GridRegionState.Open,
                    defaultSize: 44d,
                    minSize: 36d,
                    maxSize: 44d,
                    canCollapse: true,
                    canClose: true,
                    canResize: false,
                    canActivate: false),
            };

            Assert.That(() => new GridRegionLayoutManager(duplicateDefinitions), Throws.ArgumentException.With.Message.Contains("Duplicate"));
        }

        [Test]
        public void ResolveAll_UsesDefinitionDefaults_AndExposesSingleActivePane()
        {
            var manager = new GridRegionLayoutManager(GridRegionDefinitionCatalog.CreateDefault());

            var states = manager.ResolveAll().ToDictionary(state => state.RegionKind);

            Assert.Multiple(() =>
            {
                Assert.That(states[GridRegionKind.CoreGridSurface].State, Is.EqualTo(GridRegionState.Open));
                Assert.That(states[GridRegionKind.TopCommandRegion].State, Is.EqualTo(GridRegionState.Open));
                Assert.That(states[GridRegionKind.GroupingRegion].State, Is.EqualTo(GridRegionState.Open));
                Assert.That(states[GridRegionKind.SummaryBottomRegion].State, Is.EqualTo(GridRegionState.Open));
                Assert.That(states[GridRegionKind.SideToolRegion].State, Is.EqualTo(GridRegionState.Closed));
                Assert.That(states.Values.Count(state => state.IsActive), Is.EqualTo(0));
                Assert.That(states[GridRegionKind.SideToolRegion].CanActivate, Is.True);
            });
        }

        [Test]
        public void StateTransitions_OpenCollapseCloseResizeAndActivate_PreserveManagerInvariants()
        {
            var manager = new GridRegionLayoutManager(GridRegionDefinitionCatalog.CreateDefault());

            manager.OpenRegion(GridRegionKind.SideToolRegion);
            manager.ResizeRegion(GridRegionKind.SideToolRegion, 410d);
            manager.ActivateRegion(GridRegionKind.SideToolRegion);
            manager.CollapseRegion(GridRegionKind.SideToolRegion);
            manager.OpenRegion(GridRegionKind.SideToolRegion);
            manager.ActivateRegion(GridRegionKind.SideToolRegion);

            var sideTools = manager.Resolve(GridRegionKind.SideToolRegion);
            var snapshot = manager.ExportLayout();

            Assert.Multiple(() =>
            {
                Assert.That(sideTools.State, Is.EqualTo(GridRegionState.Open));
                Assert.That(sideTools.Size, Is.EqualTo(410d));
                Assert.That(sideTools.IsActive, Is.True);
                Assert.That(snapshot.Regions.Single(region => region.RegionKind == GridRegionKind.SideToolRegion).IsActive, Is.True);
                Assert.That(snapshot.Regions.Single(region => region.RegionKind == GridRegionKind.SideToolRegion).State, Is.EqualTo(GridRegionState.Open));
                Assert.That(snapshot.Regions.Single(region => region.RegionKind == GridRegionKind.SideToolRegion).Size, Is.EqualTo(410d));
            });
        }

        [Test]
        public void RestoreLayout_RoundTripsStateSizeAndActivation()
        {
            var manager = new GridRegionLayoutManager(GridRegionDefinitionCatalog.CreateDefault());
            var snapshot = new GridRegionLayoutSnapshot(new[]
            {
                new GridRegionLayoutState(GridRegionKind.CoreGridSurface, GridRegionState.Open, null, false ),
                new GridRegionLayoutState(GridRegionKind.TopCommandRegion, GridRegionState.Open, 44d, false ),
                new GridRegionLayoutState(GridRegionKind.GroupingRegion, GridRegionState.Collapsed, 92d, false ),
                new GridRegionLayoutState(GridRegionKind.SummaryBottomRegion, GridRegionState.Open, 56d, false ),
                new GridRegionLayoutState(GridRegionKind.SideToolRegion, GridRegionState.Open, 360d, true),
            });

            manager.RestoreLayout(snapshot);

            var exported = manager.ExportLayout();
            Assert.Multiple(() =>
            {
                Assert.That(exported.Regions.Single(region => region.RegionKind == GridRegionKind.GroupingRegion).State, Is.EqualTo(GridRegionState.Collapsed));
                Assert.That(exported.Regions.Single(region => region.RegionKind == GridRegionKind.GroupingRegion).Size, Is.EqualTo(92d));
                Assert.That(exported.Regions.Single(region => region.RegionKind == GridRegionKind.SideToolRegion).IsActive, Is.True);
                Assert.That(manager.Resolve(GridRegionKind.SideToolRegion).IsActive, Is.True);
            });
        }

        [Test]
        public void RestoreLayout_FailsFastForUnknownMissingDuplicateOrInvalidEntries()
        {
            var manager = new GridRegionLayoutManager(GridRegionDefinitionCatalog.CreateDefault());

            Assert.That(
                () => manager.RestoreLayout(
                    new GridRegionLayoutSnapshot(new[]
                    {
                        new GridRegionLayoutState(GridRegionKind.TopCommandRegion, GridRegionState.Open, 44d, false),
                    })),
                Throws.ArgumentException.With.Message.Contains("complete"));

            Assert.That(
                () => manager.RestoreLayout(
                    new GridRegionLayoutSnapshot(new[]
                    {
                    new GridRegionLayoutState(GridRegionKind.CoreGridSurface, GridRegionState.Open, null, false ),
                    new GridRegionLayoutState(GridRegionKind.TopCommandRegion, GridRegionState.Open, 44d, false ),
                    new GridRegionLayoutState(GridRegionKind.TopCommandRegion, GridRegionState.Open, 44d, false ),
                    new GridRegionLayoutState(GridRegionKind.GroupingRegion, GridRegionState.Open, 56d, false ),
                    new GridRegionLayoutState(GridRegionKind.SummaryBottomRegion, GridRegionState.Open, 56d, false),
                    })),
                Throws.ArgumentException.With.Message.Contains("Duplicate"));

            Assert.That(
                () => manager.RestoreLayout(
                    new GridRegionLayoutSnapshot(new[]
                    {
                        new GridRegionLayoutState(GridRegionKind.CoreGridSurface, GridRegionState.Open, null, false ),
                        new GridRegionLayoutState(GridRegionKind.TopCommandRegion, GridRegionState.Open, 44d, false ),
                        new GridRegionLayoutState(GridRegionKind.GroupingRegion, GridRegionState.Open, 56d, false ),
                        new GridRegionLayoutState(GridRegionKind.SummaryBottomRegion, GridRegionState.Collapsed, 56d, false ),
                        new GridRegionLayoutState(GridRegionKind.SideToolRegion, GridRegionState.Open, 320d, false),
                    })),
                Throws.InvalidOperationException.With.Message.Contains("SummaryBottomRegion"));
        }

        [Test]
        public void ResizeRegion_ClampsSizeToDefinitionBounds_AndRejectsClosedRegions()
        {
            var manager = new GridRegionLayoutManager(GridRegionDefinitionCatalog.CreateDefault());

            manager.OpenRegion(GridRegionKind.SideToolRegion);
            manager.ResizeRegion(GridRegionKind.SideToolRegion, 999d);
            Assert.That(manager.Resolve(GridRegionKind.SideToolRegion).Size, Is.EqualTo(520d));

            manager.CloseRegion(GridRegionKind.SideToolRegion);
            Assert.That(
                () => manager.ResizeRegion(GridRegionKind.SideToolRegion, 300d),
                Throws.InvalidOperationException.With.Message.Contains("closed"));
        }

        [Test]
        public void Activation_RequiresOpenActivatableRegion_AndEnforcesSingleActiveRegion()
        {
            var manager = new GridRegionLayoutManager(new[]
            {
                new GridRegionDefinition(GridRegionKind.CoreGridSurface, GridRegionHostKind.Surface, GridRegionPlacement.Center, GridRegionContentKind.GridSurface, GridRegionState.Open, null, null, null, false, false, false, false),
                new GridRegionDefinition(GridRegionKind.TopCommandRegion, GridRegionHostKind.Strip, GridRegionPlacement.Top, GridRegionContentKind.CommandBar, GridRegionState.Open, 44d, 36d, 44d, true, true, false, false),
                new GridRegionDefinition(GridRegionKind.GroupingRegion, GridRegionHostKind.Strip, GridRegionPlacement.Top, GridRegionContentKind.GroupingDropZone, GridRegionState.Open, 56d, 56d, 220d, true, true, true, false),
                new GridRegionDefinition(GridRegionKind.SummaryBottomRegion, GridRegionHostKind.Strip, GridRegionPlacement.Bottom, GridRegionContentKind.Summary, GridRegionState.Open, 56d, 56d, 180d, false, true, false, false),
                new GridRegionDefinition(GridRegionKind.SideToolRegion, GridRegionHostKind.Pane, GridRegionPlacement.Right, GridRegionContentKind.ToolPane, GridRegionState.Open, 320d, 220d, 520d, true, true, true, true),
            });

            manager.ActivateRegion(GridRegionKind.SideToolRegion);
            Assert.That(manager.Resolve(GridRegionKind.SideToolRegion).IsActive, Is.True);

            Assert.That(
                () => manager.ActivateRegion(GridRegionKind.GroupingRegion),
                Throws.InvalidOperationException.With.Message.Contains("activatable"));

            manager.CollapseRegion(GridRegionKind.SideToolRegion);
            Assert.That(manager.Resolve(GridRegionKind.SideToolRegion).IsActive, Is.False);
            Assert.That(
                () => manager.ActivateRegion(GridRegionKind.SideToolRegion),
                Throws.InvalidOperationException.With.Message.Contains("open"));
        }

        [Test]
        public void CapabilityPolicy_HidesUnavailableRegionsWithoutDiscardingTheirStoredLayout()
        {
            var manager = new GridRegionLayoutManager(GridRegionDefinitionCatalog.CreateDefault(), new DenySideToolsCapabilityPolicy());

            manager.RestoreLayout(new GridRegionLayoutSnapshot(new[]
            {
                new GridRegionLayoutState(GridRegionKind.CoreGridSurface, GridRegionState.Open, null, false ),
                new GridRegionLayoutState(GridRegionKind.TopCommandRegion, GridRegionState.Open, 44d, false ),
                new GridRegionLayoutState(GridRegionKind.GroupingRegion, GridRegionState.Open, 56d, false ),
                new GridRegionLayoutState(GridRegionKind.SummaryBottomRegion, GridRegionState.Open, 56d, false ),
                new GridRegionLayoutState(GridRegionKind.SideToolRegion, GridRegionState.Open, 420d, true),
            }));

            var resolved = manager.Resolve(GridRegionKind.SideToolRegion);
            var exported = manager.ExportLayout();

            Assert.Multiple(() =>
            {
                Assert.That(resolved.IsAvailable, Is.False);
                Assert.That(resolved.State, Is.EqualTo(GridRegionState.Closed));
                Assert.That(resolved.IsActive, Is.False);
                Assert.That(exported.Regions.Single(region => region.RegionKind == GridRegionKind.SideToolRegion).State, Is.EqualTo(GridRegionState.Open));
                Assert.That(exported.Regions.Single(region => region.RegionKind == GridRegionKind.SideToolRegion).Size, Is.EqualTo(420d));
                Assert.That(exported.Regions.Single(region => region.RegionKind == GridRegionKind.SideToolRegion).IsActive, Is.True);
            });
        }

        [Test]
        public void RegionCommands_ExecuteAgainstCoreManagerAndFailFastForInvalidTransitions()
        {
            var manager = new GridRegionLayoutManager(GridRegionDefinitionCatalog.CreateDefault());

            manager.Process(new Interaction.GridRegionCommandInput(
                new DateTime(2026, 4, 2, 12, 0, 0, DateTimeKind.Utc),
                Interaction.GridRegionCommandKind.Open,
                GridRegionKind.SideToolRegion));
            manager.Process(new Interaction.GridRegionCommandInput(
                new DateTime(2026, 4, 2, 12, 0, 1, DateTimeKind.Utc),
                Interaction.GridRegionCommandKind.ToggleCollapse,
                GridRegionKind.SideToolRegion));
            manager.Process(new Interaction.GridRegionCommandInput(
                new DateTime(2026, 4, 2, 12, 0, 2, DateTimeKind.Utc),
                Interaction.GridRegionCommandKind.Open,
                GridRegionKind.SideToolRegion));
            manager.Process(new Interaction.GridRegionCommandInput(
                new DateTime(2026, 4, 2, 12, 0, 3, DateTimeKind.Utc),
                Interaction.GridRegionCommandKind.Close,
                GridRegionKind.SideToolRegion));

            Assert.That(manager.Resolve(GridRegionKind.SideToolRegion).State, Is.EqualTo(GridRegionState.Closed));
            Assert.That(
                () => manager.Process(new Interaction.GridRegionCommandInput(
                    new DateTime(2026, 4, 2, 12, 0, 4, DateTimeKind.Utc),
                    Interaction.GridRegionCommandKind.ToggleCollapse,
                    GridRegionKind.SideToolRegion)),
                Throws.InvalidOperationException.With.Message.Contains("closed"));
        }

        private sealed class DenySideToolsCapabilityPolicy : IGridCapabilityPolicy
        {
            public bool CanViewRegion(GridRegionKind regionKind)
            {
                return regionKind != GridRegionKind.SideToolRegion;
            }

            public bool CanExecuteCommand(string commandKey)
            {
                return true;
            }
        }
    }
}





