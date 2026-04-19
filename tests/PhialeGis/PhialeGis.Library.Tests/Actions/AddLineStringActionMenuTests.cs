using NUnit.Framework;
using PhialeGis.Library.Abstractions.Actions;
using PhialeGis.Library.Actions.Ogc;

namespace PhialeGis.Library.Tests.Actions
{
    [TestFixture]
    [Category("Unit")]
    public sealed class AddLineStringActionMenuTests
    {
        [Test]
        public void RightClick_EmitsLocalizedContextMenu()
        {
            var action = new AddLineStringAction();
            ActionContextMenuPayload menu = null;

            action.Changed += (s, payload) =>
            {
                var p = payload as ActionContextMenuPayload;
                if (p != null)
                    menu = p;
            };

            action.Start(new ActionContext
            {
                ActionId = System.Guid.NewGuid(),
                TargetDraw = new object(),
                LanguageId = "pl-PL"
            });

            var handled = action.TryHandlePointerDown(new ActionPointerInput
            {
                Button = PointerButton.Secondary,
                ScreenPosition = new UniversalInput.Contracts.UniversalPoint { X = 100, Y = 200 },
                ModelPosition = new UniversalInput.Contracts.UniversalPoint { X = 10, Y = 20 },
                HasModelPosition = true,
                TargetDraw = new object()
            });

            Assert.IsTrue(handled);
            Assert.NotNull(menu);
            Assert.NotNull(menu.Items);
            Assert.AreEqual(4, menu.Items.Length);
            Assert.AreEqual("Enter", menu.Items[0].Label);
            Assert.AreEqual("Cancel", menu.Items[1].Label);
            Assert.IsTrue(menu.Items[2].IsSeparator);
            Assert.AreEqual("Cofnij", menu.Items[3].Label);
        }

        [Test]
        public void MenuCommandUndo_RemovesLastPointPreview()
        {
            var action = new AddLineStringAction();
            ActionChangePayload lastChange = null;

            action.Changed += (s, payload) =>
            {
                var p = payload as ActionChangePayload;
                if (p != null)
                    lastChange = p;
            };

            action.Start(new ActionContext
            {
                ActionId = System.Guid.NewGuid(),
                TargetDraw = new object(),
                LanguageId = "en-US"
            });

            action.HandleInput("0 0");
            action.HandleInput("10 0");
            Assert.NotNull(lastChange);
            Assert.NotNull(lastChange.Preview);
            Assert.AreEqual(4, lastChange.Preview.Length);

            var handled = action.TryHandleMenuCommand("undo");
            Assert.IsTrue(handled);
            Assert.NotNull(lastChange);
            Assert.NotNull(lastChange.Preview);
            Assert.AreEqual(0, lastChange.Preview.Length);
        }

        [Test]
        public void MenuCommandEnter_FinishesActionWhenEnoughPoints()
        {
            var action = new AddLineStringAction();
            ActionFinishPayload finish = null;

            action.Finished += (s, payload) =>
            {
                var p = payload as ActionFinishPayload;
                if (p != null)
                    finish = p;
            };

            action.Start(new ActionContext
            {
                ActionId = System.Guid.NewGuid(),
                TargetDraw = new object(),
                LanguageId = "en-US"
            });

            action.HandleInput("0 0");
            action.HandleInput("10 0");

            var handled = action.TryHandleMenuCommand("enter");
            Assert.IsTrue(handled);
            Assert.NotNull(finish);
            Assert.IsTrue(finish.Success);
            Assert.NotNull(finish.Result);
        }
    }
}

