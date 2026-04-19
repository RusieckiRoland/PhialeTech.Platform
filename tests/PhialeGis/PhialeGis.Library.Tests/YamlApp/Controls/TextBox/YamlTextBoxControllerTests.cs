using PhialeTech.YamlApp.Abstractions.Enums;
using PhialeTech.YamlApp.Core.Controls.TextBox;
using UniversalInput.Contracts;
using NUnit.Framework;

namespace PhialeGis.Library.Tests.YamlApp.Controls.TextBox
{
    [TestFixture]
    public sealed class YamlTextBoxControllerTests
    {
        [Test]
        public void GetChromeState_ShouldUseErrorAndFramedMetrics()
        {
            var controller = new YamlTextBoxController();

            controller.SetCaption("Asset title");
            controller.SetRequired(true);
            controller.SetErrorMessage("This field is required.");

            var state = controller.GetChromeState();

            Assert.Multiple(() =>
            {
                Assert.That(state.HasError, Is.True);
                Assert.That(state.ShowClearButton, Is.False);
                Assert.That(state.Caption, Is.EqualTo("Asset title"));
                Assert.That(state.SupportText, Is.EqualTo("This field is required."));
                Assert.That(state.FieldChromeMode, Is.EqualTo(FieldChromeMode.Framed));
                Assert.That(state.LayoutMetrics.MinimumHeight, Is.GreaterThan(90));
            });
        }

        [Test]
        public void HandleFocusChanged_ShouldUpdateFocusInInlineMode()
        {
            var controller = new YamlTextBoxController();

            controller.SetChromeMode(FieldChromeMode.InlineHint);
            controller.HandleFocusChanged(new UniversalFocusChangedEventArgs(true));

            var state = controller.GetChromeState();

            Assert.Multiple(() =>
            {
                Assert.That(state.HasFocus, Is.True);
                Assert.That(state.UsesInlineChrome, Is.True);
                Assert.That(state.LayoutMetrics.MinimumHeight, Is.GreaterThan(90));
            });
        }

        [Test]
        public void GetChromeState_ShouldSuppressRequiredError_WhenTextIsPresent()
        {
            var controller = new YamlTextBoxController();

            controller.SetCaption("Asset title");
            controller.SetRequired(true);
            controller.SetErrorMessage("This field is required.");
            controller.SetText("DFPDD");

            var state = controller.GetChromeState();

            Assert.Multiple(() =>
            {
                Assert.That(state.HasError, Is.False);
                Assert.That(state.SupportText, Is.Empty);
            });
        }

        [Test]
        public void GetChromeState_ShouldKeepSupportTextEmpty_WhenFieldIsValid()
        {
            var controller = new YamlTextBoxController();

            controller.SetCaption("Asset title");
            controller.SetRequired(true);
            controller.SetText("dd");

            var state = controller.GetChromeState();

            Assert.Multiple(() =>
            {
                Assert.That(state.HasError, Is.False);
                Assert.That(state.SupportText, Is.Empty);
            });
        }

        [Test]
        public void GetChromeState_ShouldShowRestoreOldValueAction_WhenOldValueExistsAndTextIsEmpty()
        {
            var controller = new YamlTextBoxController();

            controller.SetShowOldValueRestoreButton(true);
            controller.SetOldValue("Roland");

            var state = controller.GetChromeState();

            Assert.Multiple(() =>
            {
                Assert.That(state.ShowClearButton, Is.False);
                Assert.That(state.ShowRestoreOldValueButton, Is.True);
                Assert.That(state.TrailingActionKind, Is.EqualTo(YamlTextBoxTrailingActionKind.RestoreOldValue));
            });
        }

        [Test]
        public void GetChromeState_ShouldPreferClearAction_WhenTextIsPresentEvenWithOldValue()
        {
            var controller = new YamlTextBoxController();

            controller.SetShowOldValueRestoreButton(true);
            controller.SetOldValue("Roland");
            controller.SetText("Rusiecki");

            var state = controller.GetChromeState();

            Assert.Multiple(() =>
            {
                Assert.That(state.ShowClearButton, Is.True);
                Assert.That(state.ShowRestoreOldValueButton, Is.False);
                Assert.That(state.TrailingActionKind, Is.EqualTo(YamlTextBoxTrailingActionKind.Clear));
            });
        }

        [Test]
        public void GetChromeState_ShouldUseLeftCaptionMetrics_WhenCaptionPlacementIsLeft()
        {
            var controller = new YamlTextBoxController();

            controller.SetCaptionPlacement(CaptionPlacement.Left);

            var state = controller.GetChromeState();

            Assert.Multiple(() =>
            {
                Assert.That(state.CaptionPlacement, Is.EqualTo(CaptionPlacement.Left));
                Assert.That(state.LayoutMetrics.EditorLeft, Is.GreaterThan(40));
                Assert.That(state.LayoutMetrics.CaptionWidth, Is.GreaterThan(0));
            });
        }

        [Test]
        public void GetChromeState_ShouldUseSameVerticalMetrics_ForInlineAndFramed()
        {
            var controller = new YamlTextBoxController();

            var framedState = controller.GetChromeState();

            controller.SetChromeMode(FieldChromeMode.InlineHint);
            var inlineState = controller.GetChromeState();

            Assert.Multiple(() =>
            {
                Assert.That(inlineState.LayoutMetrics.MinimumHeight, Is.EqualTo(framedState.LayoutMetrics.MinimumHeight));
                Assert.That(inlineState.LayoutMetrics.EditorTop, Is.EqualTo(framedState.LayoutMetrics.EditorTop));
                Assert.That(inlineState.LayoutMetrics.EditorBottom, Is.EqualTo(framedState.LayoutMetrics.EditorBottom));
            });
        }

        [Test]
        public void GetChromeState_ShouldReserveHorizontalSpace_WhenCaptionPlacementIsLeft()
        {
            var controller = new YamlTextBoxController();

            var topState = controller.GetChromeState();

            controller.SetCaptionPlacement(CaptionPlacement.Left);
            var leftState = controller.GetChromeState();

            Assert.Multiple(() =>
            {
                Assert.That(leftState.LayoutMetrics.EditorLeft, Is.GreaterThan(topState.LayoutMetrics.EditorLeft));
                Assert.That(leftState.LayoutMetrics.CaptionWidth, Is.GreaterThan(0));
            });
        }

        [Test]
        public void GetChromeState_ShouldCarryTouchInteractionState()
        {
            var controller = new YamlTextBoxController();

            controller.SetInteractionMode(InteractionMode.Touch);
            controller.SetPressed(true);
            controller.SetHover(true);

            var state = controller.GetChromeState();

            Assert.Multiple(() =>
            {
                Assert.That(state.InteractionMode, Is.EqualTo(InteractionMode.Touch));
                Assert.That(state.HasPressed, Is.True);
                Assert.That(state.HasHover, Is.True);
            });
        }
    }
}
