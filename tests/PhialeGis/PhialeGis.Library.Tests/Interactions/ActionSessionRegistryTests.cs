using NUnit.Framework;
using PhialeGis.Library.Core.Interactions;
using PhialeGis.Library.Tests.Support;

namespace PhialeGis.Library.Tests.Interactions
{
    [TestFixture]
    [Category("Unit")]
    public sealed class ActionSessionRegistryTests
    {
        [Test]
        public void TracksSessionsPerTarget()
        {
            var reg = new ActionSessionRegistry();
            var ed1 = new FakeEditorInteractive();
            var ed2 = new FakeEditorInteractive();
            var t1 = new object();
            var t2 = new object();

            var s1 = new ActionSession(ed1, t1, new MainInteractionFsm());
            var s2 = new ActionSession(ed2, t2, new MainInteractionFsm());

            reg.Add(s1);
            reg.Add(s2);

            Assert.AreSame(s2, reg.GetActive());
            Assert.AreSame(s1, reg.GetByInputTarget(t1));
            Assert.AreSame(s2, reg.GetByInputTarget(t2));
            Assert.AreSame(s1, reg.GetByEditor(ed1));
            Assert.AreSame(s2, reg.GetByEditor(ed2));
        }

        [Test]
        public void StackKeepsLastAsActive()
        {
            var reg = new ActionSessionRegistry();
            var ed1 = new FakeEditorInteractive();
            var ed2 = new FakeEditorInteractive();
            var target = new object();

            var s1 = new ActionSession(ed1, target, new MainInteractionFsm());
            var s2 = new ActionSession(ed2, target, new MainInteractionFsm());

            reg.Add(s1);
            reg.Add(s2);

            Assert.AreSame(s2, reg.GetActive());
            Assert.IsTrue(reg.IsActive(s2));
            Assert.IsFalse(reg.IsActive(s1));
        }

        [Test]
        public void TransferInputTarget_UpdatesLookupWithoutChangingEditor()
        {
            var reg = new ActionSessionRegistry();
            var editor = new FakeEditorInteractive();
            var target1 = new object();
            var target2 = new object();
            var session = new ActionSession(editor, target1, new MainInteractionFsm());

            reg.Add(session);
            reg.TransferInputTarget(session, target2);

            Assert.AreSame(session, reg.GetByEditor(editor));
            Assert.AreSame(session, reg.GetByInputTarget(target2));
            Assert.IsNull(reg.GetByInputTarget(target1));
        }
    }
}

