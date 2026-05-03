using System;
using System.Linq;
using NUnit.Framework;
using PhialeGrid.Core.Interaction;
using PhialeGrid.Core.State;

namespace PhialeGrid.Core.Regions.Tests
{
    [TestFixture]
    public sealed class GridSurfaceCoordinatorRegionPipelineTests
    {
        [Test]
        public void ProcessInput_RoutesRegionCommandsIntoCoreRegionLayoutManager()
        {
            var coordinator = new GridSurfaceCoordinator();

            coordinator.ProcessInput(new GridRegionCommandInput(
                new DateTime(2026, 4, 2, 12, 30, 0, DateTimeKind.Utc),
                GridRegionCommandKind.Open,
                GridRegionKind.SideToolRegion));
            coordinator.ProcessInput(new GridRegionCommandInput(
                new DateTime(2026, 4, 2, 12, 30, 1, DateTimeKind.Utc),
                GridRegionCommandKind.Resize,
                GridRegionKind.SideToolRegion,
                requestedSize: 380d));
            coordinator.ProcessInput(new GridRegionCommandInput(
                new DateTime(2026, 4, 2, 12, 30, 2, DateTimeKind.Utc),
                GridRegionCommandKind.Activate,
                GridRegionKind.SideToolRegion));
            coordinator.ProcessInput(new GridRegionCommandInput(
                new DateTime(2026, 4, 2, 12, 30, 3, DateTimeKind.Utc),
                GridRegionCommandKind.ToggleCollapse,
                GridRegionKind.SideToolRegion));

            var sideTools = coordinator.ResolveRegion(GridRegionKind.SideToolRegion);
            var snapshot = coordinator.ExportRegionLayout();

            Assert.Multiple(() =>
            {
                Assert.That(sideTools.State, Is.EqualTo(GridRegionState.Collapsed));
                Assert.That(sideTools.Size, Is.EqualTo(380d));
                Assert.That(sideTools.IsActive, Is.False);
                Assert.That(snapshot.Regions.Single(region => region.RegionKind == GridRegionKind.SideToolRegion).State, Is.EqualTo(GridRegionState.Collapsed));
                Assert.That(snapshot.Regions.Single(region => region.RegionKind == GridRegionKind.SideToolRegion).Size, Is.EqualTo(380d));
            });
        }

        [Test]
        public void ProcessInput_FailsFastForInvalidRegionTransitions()
        {
            var coordinator = new GridSurfaceCoordinator();

            Assert.That(
                () => coordinator.ProcessInput(new GridRegionCommandInput(
                    new DateTime(2026, 4, 2, 12, 31, 0, DateTimeKind.Utc),
                    GridRegionCommandKind.ToggleCollapse,
                    GridRegionKind.SideToolRegion)),
                Throws.InvalidOperationException.With.Message.Contains("closed"));
        }

        [Test]
        public void RestoreRegionLayout_ReplacesCoordinatorRegionStateForPersistenceRoundTrip()
        {
            var coordinator = new GridSurfaceCoordinator();
            var snapshot = new GridRegionLayoutSnapshot(new[]
            {
                new GridRegionLayoutState(GridRegionKind.CoreGridSurface, GridRegionState.Open, null, false ),
                new GridRegionLayoutState(GridRegionKind.TopCommandRegion, GridRegionState.Open, 52d, false ),
                new GridRegionLayoutState(GridRegionKind.GroupingRegion, GridRegionState.Open, 56d, false ),
                new GridRegionLayoutState(GridRegionKind.SummaryBottomRegion, GridRegionState.Open, 56d, false ),
                new GridRegionLayoutState(GridRegionKind.SideToolRegion, GridRegionState.Open, 330d, true),
            });

            coordinator.RestoreRegionLayout(snapshot);

            var resolved = coordinator.ResolveRegion(GridRegionKind.SideToolRegion);
            var exported = coordinator.ExportRegionLayout();
            Assert.Multiple(() =>
            {
                Assert.That(resolved.State, Is.EqualTo(GridRegionState.Open));
                Assert.That(resolved.Size, Is.EqualTo(330d));
                Assert.That(resolved.IsActive, Is.True);
                Assert.That(exported.Regions.Single(region => region.RegionKind == GridRegionKind.SideToolRegion).IsActive, Is.True);
            });
        }
    }
}


