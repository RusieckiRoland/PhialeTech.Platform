using NUnit.Framework;
using PhialeGis.Library.Abstractions.Actions;
using PhialeGis.Library.Abstractions.Interactions;
using PhialeGis.Library.Abstractions.Ui.Rendering;
using PhialeGis.Library.Actions.Ogc;
using PhialeGis.Library.Core.Interactions;
using PhialeGis.Library.Tests.Support;

namespace PhialeGis.Library.Tests.Interactions
{
    [TestFixture]
    [Category("Integration")]
    public sealed class AddLineStringIntegrationTests
    {
        [Test]
        public void AddLineString_CommitsResult()
        {
            var manager = new GisInteractionManager(new CountingRenderBackendFactory());
            var committer = new CaptureCommitter();
            var editor = new FakeEditorInteractive();
            var target = new object();

            manager.SetActionResultCommitter(committer);
            manager.StartInteractiveAction(new AddLineStringAction(), target, editor);

            Assert.IsTrue(manager.TryHandleInteractiveInput("0 0", editor));
            Assert.IsTrue(manager.TryHandleInteractiveInput("10 0", editor));
            Assert.IsTrue(manager.TryHandleInteractiveInput(string.Empty, editor));

            Assert.AreEqual(1, committer.CommitCount);
            Assert.NotNull(committer.Last);
            Assert.AreEqual(4, committer.Last.Points.Length);
        }

        [Test]
        public void TwoViewports_CanRunIndependently()
        {
            var manager = new GisInteractionManager(new CountingRenderBackendFactory());
            var committer = new CaptureCommitter();
            manager.SetActionResultCommitter(committer);

            var editor1 = new FakeEditorInteractive();
            var editor2 = new FakeEditorInteractive();
            var target1 = new object();
            var target2 = new object();

            manager.StartInteractiveAction(new AddLineStringAction(), target1, editor1);
            Assert.IsTrue(manager.CancelInteractiveAction(editor1));

            manager.StartInteractiveAction(new AddLineStringAction(), target2, editor2);

            Assert.IsFalse(manager.TryHandleInteractiveInput("0 0", editor1));
            Assert.IsTrue(manager.TryHandleInteractiveInput("0 0", editor2));
            Assert.IsTrue(manager.TryHandleInteractiveInput("0 1", editor2));
            Assert.IsTrue(manager.TryHandleInteractiveInput(string.Empty, editor2));

            Assert.AreEqual(1, committer.CommitCount);
        }

        [Test]
        public void RightClickMenu_IsExposedAndCommandIsRouted()
        {
            var manager = new GisInteractionManager(new CountingRenderBackendFactory());
            var editor = new FakeEditorInteractive();
            var target = new object();

            manager.StartInteractiveAction(new AddLineStringAction(), target, editor);

            var handledPointer = manager.TryHandleInteractivePointerDown(new ActionPointerInput
            {
                TargetDraw = target,
                HasModelPosition = true,
                ModelPosition = new UniversalInput.Contracts.UniversalPoint { X = 10, Y = 20 },
                ScreenPosition = new UniversalInput.Contracts.UniversalPoint { X = 100, Y = 200 },
                Button = PointerButton.Secondary
            });

            Assert.IsTrue(handledPointer);
            Assert.IsTrue(manager.TryConsumePendingContextMenu(target, out var payload));
            Assert.NotNull(payload);
            Assert.NotNull(payload.Items);
            Assert.Greater(payload.Items.Length, 0);

            var handledMenu = manager.TryHandleInteractiveMenuCommand(target, "undo");
            Assert.IsTrue(handledMenu);
        }

        [Test]
        public void Preview_IsRendered_OnAllRegisteredViewports()
        {
            var backendFactory = new CountingRenderBackendFactory();
            var manager = new GisInteractionManager(backendFactory);
            var editor = new FakeEditorInteractive();
            var viewport1 = new FakeRenderingComposition();
            var viewport2 = new FakeRenderingComposition();

            manager.RegisterControl(viewport1);
            manager.RegisterControl(viewport2);
            manager.StartInteractiveAction(new AddLineStringAction(), viewport1, editor);

            Assert.IsTrue(manager.TryHandleInteractiveInput("0 0", editor));
            Assert.IsTrue(manager.TryHandleInteractiveInput("10 0", editor));

            viewport1.RaisePaint();
            viewport2.RaisePaint();

            Assert.AreEqual(2, backendFactory.OverlayLineDrawCount);
        }

        [Test]
        public void Takeover_TransfersPointerInput_ToSecondViewport_WithoutRebindingEditor()
        {
            var backendFactory = new CountingRenderBackendFactory();
            var manager = new GisInteractionManager(backendFactory);
            var committer = new CaptureCommitter();
            var editor = new FakeEditorInteractive();
            var viewport1 = new FakeRenderingComposition();
            var viewport2 = new FakeRenderingComposition();

            manager.RegisterControl(viewport1);
            manager.RegisterControl(viewport2);
            manager.SetActionResultCommitter(committer);
            manager.StartInteractiveAction(new AddLineStringAction(), viewport1, editor);

            Assert.IsTrue(manager.TryHandleInteractivePointerDown(new ActionPointerInput
            {
                TargetDraw = viewport1,
                HasModelPosition = true,
                ModelPosition = new UniversalInput.Contracts.UniversalPoint { X = 0, Y = 0 }
            }));

            Assert.IsTrue(manager.TryTakeoverInteractiveSession(viewport2));

            Assert.IsFalse(manager.TryHandleInteractivePointerDown(new ActionPointerInput
            {
                TargetDraw = viewport1,
                HasModelPosition = true,
                ModelPosition = new UniversalInput.Contracts.UniversalPoint { X = 10, Y = 0 }
            }));

            Assert.IsTrue(manager.TryHandleInteractivePointerDown(new ActionPointerInput
            {
                TargetDraw = viewport2,
                HasModelPosition = true,
                ModelPosition = new UniversalInput.Contracts.UniversalPoint { X = 10, Y = 0 }
            }));

            Assert.IsTrue(manager.TryHandleInteractiveInput(string.Empty, editor));
            Assert.AreEqual(1, committer.CommitCount);
            Assert.NotNull(committer.Last);
            Assert.AreEqual(0d, committer.Last.Points[0]);
            Assert.AreEqual(0d, committer.Last.Points[1]);
            Assert.AreEqual(10d, committer.Last.Points[2]);
            Assert.AreEqual(0d, committer.Last.Points[3]);
        }

        [Test]
        public void ViewportWithoutEditor_CanTakeOver_Action_AndOriginalEditorStillControlsCommandLine()
        {
            var backendFactory = new CountingRenderBackendFactory();
            var manager = new GisInteractionManager(backendFactory);
            var committer = new CaptureCommitter();
            var editor = new FakeEditorInteractive();
            var viewport1 = new FakeRenderingComposition();
            var viewport2 = new FakeRenderingComposition();

            manager.RegisterControl(viewport1);
            manager.RegisterControl(viewport2);
            manager.SetActionResultCommitter(committer);
            manager.StartInteractiveAction(new AddLineStringAction(), viewport1, editor);

            Assert.IsTrue(manager.TryHandleInteractivePointerDown(new ActionPointerInput
            {
                TargetDraw = viewport1,
                HasModelPosition = true,
                ModelPosition = new UniversalInput.Contracts.UniversalPoint { X = 1, Y = 1 }
            }));

            Assert.IsTrue(manager.TryTakeoverInteractiveSession(viewport2));

            Assert.IsTrue(manager.TryHandleInteractivePointerDown(new ActionPointerInput
            {
                TargetDraw = viewport2,
                HasModelPosition = true,
                ModelPosition = new UniversalInput.Contracts.UniversalPoint { X = 2, Y = 2 }
            }));

            Assert.IsTrue(manager.TryHandleInteractiveInput(string.Empty, editor));
            Assert.AreEqual(1, committer.CommitCount);
        }

        [Test]
        public void Snapping_IsApplied_BeforePointerInput_ReachesAction()
        {
            var backendFactory = new CountingRenderBackendFactory();
            var manager = new GisInteractionManager(backendFactory);
            var committer = new CaptureCommitter();
            var editor = new FakeEditorInteractive();
            var viewport = new FakeRenderingComposition();

            manager.RegisterControl(viewport);
            manager.SetActionResultCommitter(committer);
            manager.SetSnapService(new AlwaysSnapService(5d, 6d));
            manager.StartInteractiveAction(new AddLineStringAction(), viewport, editor);

            Assert.IsTrue(manager.TryHandleInteractivePointerDown(new ActionPointerInput
            {
                TargetDraw = viewport,
                HasModelPosition = true,
                ModelPosition = new UniversalInput.Contracts.UniversalPoint { X = 0, Y = 0 }
            }));

            Assert.IsTrue(manager.TryHandleInteractiveInput("10 10", editor));
            Assert.IsTrue(manager.TryHandleInteractiveInput(string.Empty, editor));

            Assert.AreEqual(1, committer.CommitCount);
            Assert.NotNull(committer.Last);
            Assert.AreEqual(5d, committer.Last.Points[0]);
            Assert.AreEqual(6d, committer.Last.Points[1]);
        }

        [Test]
        public void ViewportInteractionStatus_ReflectsInputOwnershipAndCommands()
        {
            var backendFactory = new CountingRenderBackendFactory();
            var manager = new GisInteractionManager(backendFactory);
            var editor = new FakeEditorInteractive();
            var viewport1 = new FakeRenderingComposition();
            var viewport2 = new FakeRenderingComposition();

            manager.RegisterControl(viewport1);
            manager.RegisterControl(viewport2);
            manager.StartInteractiveAction(new AddLineStringAction(), viewport1, editor);
            manager.TryHandleInteractiveInput("0 0", editor);

            Assert.IsTrue(manager.TryGetViewportInteractionStatus(viewport1, out var ownerStatus));
            Assert.IsTrue(manager.TryGetViewportInteractionStatus(viewport2, out var secondaryStatus));

            Assert.IsTrue(ownerStatus.HasActiveSession);
            Assert.IsTrue(ownerStatus.IsInputViewport);
            Assert.IsFalse(ownerStatus.CanTakeOver);
            Assert.IsNotEmpty(ownerStatus.Commands);

            Assert.IsTrue(secondaryStatus.HasActiveSession);
            Assert.IsFalse(secondaryStatus.IsInputViewport);
            Assert.IsTrue(secondaryStatus.CanTakeOver);
            Assert.IsEmpty(secondaryStatus.Commands);
        }

        private sealed class CaptureCommitter : IActionResultCommitter
        {
            public int CommitCount { get; private set; }
            public LineStringActionResult Last { get; private set; }

            public void Commit(LineStringActionResult result)
            {
                CommitCount++;
                Last = result;
            }
        }

        private sealed class AlwaysSnapService : ISnapService
        {
            private readonly double _x;
            private readonly double _y;

            public AlwaysSnapService(double x, double y)
            {
                _x = x;
                _y = y;
            }

            public bool TrySnap(SnapRequest request, out SnapResult result)
            {
                result = new SnapResult
                {
                    HasSnap = true,
                    X = _x,
                    Y = _y,
                    Kind = SnapKind.Vertex
                };
                return true;
            }
        }
    }
}


