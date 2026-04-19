using System;
using System.Linq;
using NUnit.Framework;
using PhialeGrid.Core.Interaction;
using PhialeGrid.Core.State;

namespace PhialeGrid.Core.Regions.Tests
{
    [TestFixture]
    public sealed class GridRegionContractTests
    {
        [Test]
        public void GridRegionLayoutState_ValidatesEnumValuesActivationAndSize()
        {
            Assert.That(
                () => new GridRegionLayoutState(
                    (GridRegionKind)(-1),
                    GridRegionState.Open,
                    size: 64d,
                    isActive: false),
                Throws.TypeOf<ArgumentOutOfRangeException>());

            Assert.That(
                () => new GridRegionLayoutState(
                    GridRegionKind.TopCommandRegion,
                    (GridRegionState)999,
                    size: 64d,
                    isActive: false),
                Throws.TypeOf<ArgumentOutOfRangeException>());

            Assert.That(
                () => new GridRegionLayoutState(
                    GridRegionKind.SideToolRegion,
                    GridRegionState.Collapsed,
                    size: 320d,
                    isActive: true),
                Throws.InvalidOperationException.With.Message.Contains("open regions"));

            Assert.That(
                () => new GridRegionLayoutState(
                    GridRegionKind.SideToolRegion,
                    GridRegionState.Open,
                    size: 0d,
                    isActive: false),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void GridRegionViewState_ValidatesPlacementBoundsAndActivation()
        {
            Assert.That(
                () => new GridRegionViewState(
                    GridRegionKind.SideToolRegion,
                    GridRegionHostKind.Pane,
                    GridRegionPlacement.Top,
                    GridRegionContentKind.ToolPane,
                    GridRegionState.Open,
                    isAvailable: true,
                    isActive: false,
                    canCollapse: true,
                    canClose: true,
                    canResize: true,
                    canActivate: true,
                    size: 320d,
                    minSize: 220d,
                    maxSize: 520d),
                Throws.ArgumentException.With.Message.Contains("placement"));

            Assert.That(
                () => new GridRegionViewState(
                    GridRegionKind.SideToolRegion,
                    GridRegionHostKind.Pane,
                    GridRegionPlacement.Right,
                    GridRegionContentKind.ToolPane,
                    GridRegionState.Open,
                    isAvailable: true,
                    isActive: true,
                    canCollapse: true,
                    canClose: true,
                    canResize: true,
                    canActivate: false,
                    size: 320d,
                    minSize: 220d,
                    maxSize: 520d),
                Throws.InvalidOperationException.With.Message.Contains("activation support"));

            Assert.That(
                () => new GridRegionViewState(
                    GridRegionKind.GroupingRegion,
                    GridRegionHostKind.Strip,
                    GridRegionPlacement.Top,
                    GridRegionContentKind.GroupingDropZone,
                    GridRegionState.Collapsed,
                    isAvailable: true,
                    isActive: false,
                    canCollapse: false,
                    canClose: true,
                    canResize: true,
                    canActivate: false,
                    size: 56d,
                    minSize: 56d,
                    maxSize: 220d),
                Throws.InvalidOperationException.With.Message.Contains("collapsed"));

            Assert.That(
                () => new GridRegionViewState(
                    GridRegionKind.SideToolRegion,
                    GridRegionHostKind.Pane,
                    GridRegionPlacement.Right,
                    GridRegionContentKind.ToolPane,
                    GridRegionState.Open,
                    isAvailable: true,
                    isActive: false,
                    canCollapse: true,
                    canClose: true,
                    canResize: true,
                    canActivate: true,
                    size: 600d,
                    minSize: 220d,
                    maxSize: 520d),
                Throws.InvalidOperationException.With.Message.Contains("bounds"));
        }

        [Test]
        public void GridRegionCommandInput_ValidatesRequestedSizeContracts()
        {
            Assert.That(
                () => new GridRegionCommandInput(
                    new DateTime(2026, 4, 2, 14, 0, 0, DateTimeKind.Utc),
                    GridRegionCommandKind.Resize,
                    GridRegionKind.SideToolRegion),
                Throws.InvalidOperationException.With.Message.Contains("requested size"));

            Assert.That(
                () => new GridRegionCommandInput(
                    new DateTime(2026, 4, 2, 14, 0, 1, DateTimeKind.Utc),
                    GridRegionCommandKind.Open,
                    GridRegionKind.SideToolRegion,
                    requestedSize: 320d),
                Throws.InvalidOperationException.With.Message.Contains("requested size"));

            Assert.That(
                () => new GridRegionCommandInput(
                    new DateTime(2026, 4, 2, 14, 0, 2, DateTimeKind.Utc),
                    (GridRegionCommandKind)999,
                    GridRegionKind.SideToolRegion),
                Throws.TypeOf<ArgumentOutOfRangeException>());

            Assert.That(
                () => new GridRegionCommandInput(
                    new DateTime(2026, 4, 2, 14, 0, 3, DateTimeKind.Utc),
                    GridRegionCommandKind.Resize,
                    GridRegionKind.SideToolRegion,
                    requestedSize: -1d),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void GridRegionLayoutSnapshot_RejectsNullAndDuplicateRegions_AndClonesInput()
        {
            var regions = new[]
            {
                new GridRegionLayoutState(GridRegionKind.TopCommandRegion, GridRegionState.Open, 64d, false),
                new GridRegionLayoutState(GridRegionKind.GroupingRegion, GridRegionState.Open, 56d, false),
            };

            var snapshot = new GridRegionLayoutSnapshot(regions);
            regions[0] = new GridRegionLayoutState(GridRegionKind.TopCommandRegion, GridRegionState.Closed, 64d, false);

            Assert.That(snapshot.Regions.Single(region => region.RegionKind == GridRegionKind.TopCommandRegion).State, Is.EqualTo(GridRegionState.Open));

            Assert.That(
                () => new GridRegionLayoutSnapshot(new GridRegionLayoutState[]
                {
                    null,
                }),
                Throws.ArgumentException.With.Message.Contains("null"));

            Assert.That(
                () => new GridRegionLayoutSnapshot(new[]
                {
                    new GridRegionLayoutState(GridRegionKind.TopCommandRegion, GridRegionState.Open, 64d, false),
                    new GridRegionLayoutState(GridRegionKind.TopCommandRegion, GridRegionState.Open, 64d, false),
                }),
                Throws.ArgumentException.With.Message.Contains("Duplicate"));
        }

        [Test]
        public void ResolvedCoreViewState_IsCompleteForFrontendAdapters()
        {
            var manager = new GridRegionLayoutManager(GridRegionDefinitionCatalog.CreateDefault());

            foreach (var state in manager.ResolveAll())
            {
                Assert.Multiple(() =>
                {
                    Assert.That(Enum.IsDefined(typeof(GridRegionKind), state.RegionKind), Is.True);
                    Assert.That(Enum.IsDefined(typeof(GridRegionHostKind), state.HostKind), Is.True);
                    Assert.That(Enum.IsDefined(typeof(GridRegionPlacement), state.Placement), Is.True);
                    Assert.That(Enum.IsDefined(typeof(GridRegionContentKind), state.ContentKind), Is.True);
                    Assert.That(Enum.IsDefined(typeof(GridRegionState), state.State), Is.True);
                });

                Assert.That(
                    () => new GridRegionViewState(
                        state.RegionKind,
                        state.HostKind,
                        state.Placement,
                        state.ContentKind,
                        state.State,
                        state.IsAvailable,
                        state.IsActive,
                        state.CanCollapse,
                        state.CanClose,
                        state.CanResize,
                        state.CanActivate,
                        state.Size,
                        state.MinSize,
                        state.MaxSize),
                    Throws.Nothing,
                    "Resolved Core region state should already satisfy the strict adapter contract.");
            }
        }
    }
}




