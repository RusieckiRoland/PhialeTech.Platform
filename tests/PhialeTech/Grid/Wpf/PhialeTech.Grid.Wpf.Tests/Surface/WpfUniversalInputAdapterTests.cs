using System.Windows;
using System.Windows.Input;
using NUnit.Framework;
using PhialeTech.PhialeGrid.Wpf.Controls;
using UniversalInput.Contracts;

namespace PhialeGrid.Wpf.Tests.Surface
{
    [TestFixture]
    public class WpfUniversalInputAdapterTests
    {
        [Test]
        public void CreateMousePointerPressedEventArgs_MapsPositionButtonClickCountAndModifiers()
        {
            var args = WpfUniversalInputAdapter.CreateMousePointerPressedEventArgs(
                new Point(10, 20),
                MouseButton.Left,
                2,
                ModifierKeys.Control,
                MouseButtonState.Pressed,
                MouseButtonState.Released,
                MouseButtonState.Released);

            Assert.Multiple(() =>
            {
                Assert.That(args.Pointer.PointerDeviceType, Is.EqualTo(DeviceType.Mouse));
                Assert.That(args.Pointer.Position.X, Is.EqualTo(10));
                Assert.That(args.Pointer.Position.Y, Is.EqualTo(20));
                Assert.That(args.Pointer.Properties.IsLeftButtonPressed, Is.True);
                Assert.That(args.Metadata.ClickCount, Is.EqualTo(2));
                Assert.That(args.Metadata.ChangedButton, Is.EqualTo(UniversalPointerButton.Left));
                Assert.That(args.Metadata.Modifiers, Is.EqualTo(UniversalModifierKeys.Control));
            });
        }

        [Test]
        public void CreateWheelEventArgs_MapsDeltaPositionAndModifiers()
        {
            var args = WpfUniversalInputAdapter.CreateWheelEventArgs(
                120,
                new Point(30, 40),
                ModifierKeys.Shift);

            Assert.Multiple(() =>
            {
                Assert.That(args.Delta, Is.EqualTo(-120));
                Assert.That(args.Position.X, Is.EqualTo(30));
                Assert.That(args.Position.Y, Is.EqualTo(40));
                Assert.That(args.Metadata.Modifiers, Is.EqualTo(UniversalModifierKeys.Shift));
            });
        }

        [Test]
        public void CreateFocusChangedEventArgs_MapsGainAndLoss()
        {
            var gained = WpfUniversalInputAdapter.CreateFocusChangedEventArgs(true);
            var lost = WpfUniversalInputAdapter.CreateFocusChangedEventArgs(false);

            Assert.Multiple(() =>
            {
                Assert.That(gained.HasFocus, Is.True);
                Assert.That(lost.HasFocus, Is.False);
            });
        }

        [Test]
        public void CreateTouchPointerPressedEventArgs_PreservesPointerIdentifier()
        {
            var args = WpfUniversalInputAdapter.CreateTouchPointerPressedEventArgs(
                new Point(5, 6),
                42,
                ModifierKeys.None);

            Assert.Multiple(() =>
            {
                Assert.That(args.Pointer.PointerDeviceType, Is.EqualTo(DeviceType.Touch));
                Assert.That(args.Pointer.PointerId, Is.EqualTo(42));
                Assert.That(args.Pointer.Position.X, Is.EqualTo(5));
                Assert.That(args.Pointer.Position.Y, Is.EqualTo(6));
            });
        }

        [Test]
        public void CreatePointerCanceledEventArgs_MapsPointerAndReason()
        {
            var args = WpfUniversalInputAdapter.CreatePointerCanceledEventArgs(
                DeviceType.Touch,
                77,
                new Point(12, 34),
                ModifierKeys.Control,
                UniversalPointerCancelReason.CaptureLost);

            Assert.Multiple(() =>
            {
                Assert.That(args.Pointer.PointerDeviceType, Is.EqualTo(DeviceType.Touch));
                Assert.That(args.Pointer.PointerId, Is.EqualTo(77));
                Assert.That(args.Pointer.Position.X, Is.EqualTo(12));
                Assert.That(args.Pointer.Position.Y, Is.EqualTo(34));
                Assert.That(args.Reason, Is.EqualTo(UniversalPointerCancelReason.CaptureLost));
                Assert.That(args.Metadata.Modifiers, Is.EqualTo(UniversalModifierKeys.Control));
            });
        }

        [Test]
        public void CreateKeyEventArgs_AndCreateTextEventArgs_KeepKeyAndTextSeparate()
        {
            var keyArgs = WpfUniversalInputAdapter.CreateKeyEventArgs(Key.A, true, ModifierKeys.None, isRepeat: false);
            var textArgs = WpfUniversalInputAdapter.CreateTextEventArgs("a", ModifierKeys.None);

            Assert.Multiple(() =>
            {
                Assert.That(keyArgs.Key, Is.EqualTo("A"));
                Assert.That(keyArgs.IsKeyDown, Is.True);
                Assert.That(textArgs.Text, Is.EqualTo("a"));
            });
        }

        [Test]
        public void CreateEditorValueChangedEventArgs_MapsCellIdentityValueKindAndModifiers()
        {
            var args = WpfUniversalInputAdapter.CreateEditorValueChangedEventArgs(
                "row-1",
                "Owner",
                "Municipality",
                UniversalEditorValueChangeKind.SelectionCommitted,
                ModifierKeys.Control);

            Assert.Multiple(() =>
            {
                Assert.That(args.RowKey, Is.EqualTo("row-1"));
                Assert.That(args.ColumnKey, Is.EqualTo("Owner"));
                Assert.That(args.Text, Is.EqualTo("Municipality"));
                Assert.That(args.ChangeKind, Is.EqualTo(UniversalEditorValueChangeKind.SelectionCommitted));
                Assert.That(args.Metadata.Modifiers, Is.EqualTo(UniversalModifierKeys.Control));
            });
        }

        [Test]
        public void CreateCommandEventArgs_MapsGridEditCommandAndModifiers()
        {
            var args = WpfUniversalInputAdapter.CreateCommandEventArgs(
                PhialeGrid.Core.Interaction.GridUniversalCommandIds.BeginEdit,
                ModifierKeys.Control | ModifierKeys.Shift);

            Assert.Multiple(() =>
            {
                Assert.That(args.CommandId, Is.EqualTo(PhialeGrid.Core.Interaction.GridUniversalCommandIds.BeginEdit));
                Assert.That(args.Ctrl, Is.True);
                Assert.That(args.Shift, Is.True);
                Assert.That(args.Alt, Is.False);
                Assert.That(args.Metadata.Modifiers, Is.EqualTo(UniversalModifierKeys.Control | UniversalModifierKeys.Shift));
            });
        }

        [Test]
        public void CreateScrollChangedEventArgs_MapsOffsets()
        {
            var args = WpfUniversalInputAdapter.CreateScrollChangedEventArgs(120.5d, 40.25d);

            Assert.Multiple(() =>
            {
                Assert.That(args.HorizontalOffset, Is.EqualTo(120.5d));
                Assert.That(args.VerticalOffset, Is.EqualTo(40.25d));
                Assert.That(args.PointerDeviceType, Is.EqualTo(DeviceType.Other));
            });
        }

        [Test]
        public void CreateViewportChangedEventArgs_MapsMeasuredSize()
        {
            var args = WpfUniversalInputAdapter.CreateViewportChangedEventArgs(960d, 540d);

            Assert.Multiple(() =>
            {
                Assert.That(args.Width, Is.EqualTo(960d));
                Assert.That(args.Height, Is.EqualTo(540d));
                Assert.That(args.PointerDeviceType, Is.EqualTo(DeviceType.Other));
            });
        }
    }
}

