using System;
using System.Linq;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Media;
using NUnit.Framework;
using PhialeGrid.Core.Details;
using PhialeGrid.Core.Surface;
using PhialeTech.PhialeGrid.Wpf.Surface;
using PhialeTech.PhialeGrid.Wpf.Surface.Presenters;

namespace PhialeGrid.Wpf.Tests.Surface
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class GridSurfacePanelBehaviorTests
    {
        [Test]
        public void RenderSnapshot_CreatesDedicatedHeaderCellAndOverlayLayers()
        {
            var panel = new GridSurfacePanel();

            panel.RenderSnapshot(CreateSnapshot(
                new[] { CreateColumnHeader("col-1", 40, 0, 100, 30) },
                new[] { CreateCell("row-1", "col-1", 40, 30, 100, 20, "Alpha") },
                new[] { CreateOverlay("overlay-1", 40, 30, 100, 20) }));

            Assert.Multiple(() =>
            {
                Assert.That(panel.Children.OfType<Canvas>().Count(), Is.EqualTo(3));
                Assert.That(Panel.GetZIndex((Canvas)panel.Children[0]), Is.GreaterThan(Panel.GetZIndex((Canvas)panel.Children[1])));
                Assert.That(Panel.GetZIndex((Canvas)panel.Children[2]), Is.GreaterThan(Panel.GetZIndex((Canvas)panel.Children[0])));
            });
        }

        [Test]
        public void RenderSnapshot_WhenSameItemRemains_ReusesExistingPresenterInstance()
        {
            var panel = new GridSurfacePanel();
            panel.RenderSnapshot(CreateSnapshot(
                new[] { CreateColumnHeader("col-1", 40, 0, 100, 30) },
                new[] { CreateCell("row-1", "col-1", 40, 30, 100, 20, "Alpha") },
                null));

            var initialCellPresenter = FindLayerChild(panel, 1, 0);

            panel.RenderSnapshot(CreateSnapshot(
                new[] { CreateColumnHeader("col-1", 40, 0, 100, 30) },
                new[] { CreateCell("row-1", "col-1", 40, 30, 100, 20, "Beta") },
                null));

            var updatedCellPresenter = FindLayerChild(panel, 1, 0);

            Assert.That(ReferenceEquals(initialCellPresenter, updatedCellPresenter), Is.True);
        }

        [Test]
        public void RenderSnapshot_WhenEquivalentCellSnapshotArrives_DoesNotRebindPresenter()
        {
            var panel = new GridSurfacePanel();
            panel.RenderSnapshot(CreateSnapshot(
                new[] { CreateColumnHeader("col-1", 40, 0, 100, 30) },
                new[] { CreateCell("row-1", "col-1", 40, 30, 100, 20, "Alpha") },
                null));

            var presenter = (GridCellPresenter)FindLayerChild(panel, 1, 0);
            var initialCellData = presenter.CellData;

            panel.RenderSnapshot(CreateSnapshot(
                new[] { CreateColumnHeader("col-1", 40, 0, 100, 30) },
                new[] { CreateCell("row-1", "col-1", 40, 30, 100, 20, "Alpha") },
                null));

            Assert.That(ReferenceEquals(initialCellData, presenter.CellData), Is.True);
        }

        [Test]
        public void RenderSnapshot_WhenItemDisappears_ReusesReleasedContainerForNextCell()
        {
            var panel = new GridSurfacePanel();
            panel.RenderSnapshot(CreateSnapshot(
                new[] { CreateColumnHeader("col-1", 40, 0, 100, 30) },
                new[] { CreateCell("row-1", "col-1", 40, 30, 100, 20, "Alpha") },
                null));

            var initialCellPresenter = FindLayerChild(panel, 1, 0);

            panel.RenderSnapshot(CreateSnapshot(
                new[] { CreateColumnHeader("col-1", 40, 0, 100, 30) },
                new[] { CreateCell("row-2", "col-2", 140, 50, 100, 20, "Beta") },
                null));

            var reusedCellPresenter = FindLayerChild(panel, 1, 0);

            Assert.That(ReferenceEquals(initialCellPresenter, reusedCellPresenter), Is.True);
        }

        [Test]
        public void RenderSnapshot_WithViewportOffset_AppliesCounterTransformAndLeavesFrozenBoundsUnchanged()
        {
            var panel = new GridSurfacePanel();
            panel.RenderSnapshot(CreateFrozenSnapshot(horizontalOffset: 50, verticalOffset: 10));

            var frozenCellPresenter = (GridCellPresenter)FindLayerChild(panel, 1, 0);
            var scrollableCellPresenter = (GridCellPresenter)FindLayerChild(panel, 1, 1);
            var transform = panel.RenderTransform as TranslateTransform;

            Assert.Multiple(() =>
            {
                Assert.That(transform, Is.Not.Null);
                Assert.That(transform.X, Is.EqualTo(50d));
                Assert.That(transform.Y, Is.EqualTo(10d));
                Assert.That(Canvas.GetLeft(frozenCellPresenter), Is.EqualTo(40d));
                Assert.That(Canvas.GetTop(frozenCellPresenter), Is.EqualTo(30d));
                Assert.That(Canvas.GetLeft(scrollableCellPresenter), Is.EqualTo(140d));
                Assert.That(Canvas.GetTop(scrollableCellPresenter), Is.EqualTo(50d));
                Assert.That(scrollableCellPresenter.Width, Is.EqualTo(30d));
                Assert.That(scrollableCellPresenter.Height, Is.EqualTo(20d));
            });
        }

        [Test]
        public void RenderSnapshot_WithRowDetailOverlay_UsesRowDetailPresenterAndFactoryContent()
        {
            var factory = new RecordingRowDetailFactory();
            var panel = new GridSurfacePanel
            {
                RowDetailContentFactory = factory,
            };
            var payload = CreateRowDetailPayload();

            panel.RenderSnapshot(CreateSnapshot(
                new[] { CreateColumnHeader("col-1", 40, 0, 100, 30) },
                null,
                new[] { CreateOverlay("row-detail-1", 40, 50, 120, 72, GridOverlayKind.RowDetail, payload) }));

            var presenter = (GridRowDetailPresenter)FindLayerChild(panel, 2, 0);

            Assert.Multiple(() =>
            {
                Assert.That(presenter.Content, Is.SameAs(factory.CreatedContent));
                Assert.That(factory.LastContext.CoreContext.RowKey, Is.EqualTo("row-1"));
                Assert.That(factory.LastContext.ContentDescriptor, Is.SameAs(payload.ContentDescriptor));
                Assert.That(Canvas.GetLeft(presenter), Is.EqualTo(40d));
                Assert.That(Canvas.GetTop(presenter), Is.EqualTo(50d));
                Assert.That(presenter.Width, Is.EqualTo(120d));
                Assert.That(presenter.Height, Is.EqualTo(72d));
            });
        }

        [Test]
        public void RenderSnapshot_WithRowDetailOverlayWithoutFactory_FailsFast()
        {
            var panel = new GridSurfacePanel();

            Assert.Throws<InvalidOperationException>(() =>
                panel.RenderSnapshot(CreateSnapshot(
                    new[] { CreateColumnHeader("col-1", 40, 0, 100, 30) },
                    null,
                    new[] { CreateOverlay("row-detail-1", 40, 50, 120, 72, GridOverlayKind.RowDetail, CreateRowDetailPayload()) })));
        }

        private static GridSurfaceSnapshot CreateSnapshot(
            GridHeaderSurfaceItem[] headers,
            GridCellSurfaceItem[] cells,
            GridOverlaySurfaceItem[] overlays)
        {
            return new GridSurfaceSnapshot(
                revision: 1,
                viewportState: new GridViewportState(0, 0, 400, 200, new GridViewportMetrics(new[] { 20d }, new[] { 100d })),
                columns: new[]
                {
                    new GridColumnSurfaceItem("col-1") { Bounds = new GridBounds(40, 0, 100, 30), SnapshotRevision = 1 },
                },
                rows: new[]
                {
                    new GridRowSurfaceItem("row-1") { Bounds = new GridBounds(0, 30, 40, 20), SnapshotRevision = 1 },
                },
                cells: cells ?? System.Array.Empty<GridCellSurfaceItem>(),
                headers: headers ?? System.Array.Empty<GridHeaderSurfaceItem>(),
                overlays: overlays ?? System.Array.Empty<GridOverlaySurfaceItem>());
        }

        private static GridHeaderSurfaceItem CreateColumnHeader(string key, double x, double y, double width, double height)
        {
            return new GridHeaderSurfaceItem(key, GridHeaderKind.ColumnHeader)
            {
                DisplayText = key,
                Bounds = new GridBounds(x, y, width, height),
                SnapshotRevision = 1,
            };
        }

        private static GridCellSurfaceItem CreateCell(string rowKey, string columnKey, double x, double y, double width, double height, string text)
        {
            return new GridCellSurfaceItem(rowKey, columnKey)
            {
                DisplayText = text,
                Bounds = new GridBounds(x, y, width, height),
                SnapshotRevision = 1,
            };
        }

        private static GridOverlaySurfaceItem CreateOverlay(string key, double x, double y, double width, double height)
        {
            return CreateOverlay(key, x, y, width, height, GridOverlayKind.CurrentCell, null);
        }

        private static GridOverlaySurfaceItem CreateOverlay(string key, double x, double y, double width, double height, GridOverlayKind kind, object payload)
        {
            return new GridOverlaySurfaceItem(key, kind)
            {
                Bounds = new GridBounds(x, y, width, height),
                Payload = payload,
                SnapshotRevision = 1,
            };
        }

        private static GridRowDetailSurfacePayload CreateRowDetailPayload()
        {
            var context = new GridRowDetailContext(
                "row-1",
                "row-1",
                new object(),
                new System.Collections.Generic.Dictionary<string, object>(),
                new System.Collections.Generic.Dictionary<string, GridRowDetailFieldContext>());
            return new GridRowDetailSurfacePayload(
                "detail:row-1",
                "row-1",
                context,
                new object());
        }

        private static object FindLayerChild(GridSurfacePanel panel, int layerIndex, int childIndex)
        {
            var layer = (Canvas)panel.Children[layerIndex];
            return layer.Children[childIndex];
        }

        private static GridSurfaceSnapshot CreateFrozenSnapshot(double horizontalOffset, double verticalOffset)
        {
            return new GridSurfaceSnapshot(
                revision: 1,
                viewportState: new GridViewportState(horizontalOffset, verticalOffset, 280, 120, new GridViewportMetrics(new[] { 20d, 30d }, new[] { 100d, 80d }))
                {
                    TotalWidth = 280,
                    TotalHeight = 120,
                    FrozenColumnCount = 1,
                    FrozenRowCount = 1,
                    FrozenDataWidth = 100,
                    FrozenDataHeight = 20,
                },
                columns: new[]
                {
                    new GridColumnSurfaceItem("col-1") { Bounds = new GridBounds(40, 0, 100, 30), SnapshotRevision = 1, IsFrozen = true },
                    new GridColumnSurfaceItem("col-2") { Bounds = new GridBounds(140, 0, 30, 30), SnapshotRevision = 1 },
                },
                rows: new[]
                {
                    new GridRowSurfaceItem("row-1") { Bounds = new GridBounds(0, 30, 40, 20), SnapshotRevision = 1, IsFrozen = true },
                    new GridRowSurfaceItem("row-2") { Bounds = new GridBounds(0, 50, 40, 20), SnapshotRevision = 1 },
                },
                cells: new[]
                {
                    CreateCell("row-1", "col-1", 40, 30, 100, 20, "Frozen"),
                    CreateCell("row-2", "col-2", 140, 50, 30, 20, "Scroll"),
                },
                headers: new[]
                {
                    CreateColumnHeader("col-1", 40, 0, 100, 30),
                    CreateColumnHeader("col-2", 140, 0, 30, 30),
                },
                overlays: System.Array.Empty<GridOverlaySurfaceItem>());
        }

        private sealed class RecordingRowDetailFactory : IGridRowDetailContentFactory
        {
            public GridRowDetailWpfContext LastContext { get; private set; }

            public TextBlock CreatedContent { get; private set; }

            public System.Windows.FrameworkElement CreateContent(GridRowDetailWpfContext context)
            {
                LastContext = context;
                CreatedContent = new TextBlock { Text = "detail" };
                return CreatedContent;
            }
        }
    }
}

