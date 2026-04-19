using System;
using NUnit.Framework;
using PhialeGrid.Core.Interaction;
using UniversalInput.Contracts;

namespace PhialeGrid.Core.Tests.Interaction
{
    [TestFixture]
    public class GridSurfaceUniversalInputAdapterTests
    {
        [Test]
        public void CreatePointerPressedInput_MapsUniversalPointerToGridInput()
        {
            var sut = new GridSurfaceUniversalInputAdapter();
            var universal = new UniversalPointerRoutedEventArgs(
                new UniversalPointer(DeviceType.Mouse, new UniversalPoint { X = 10, Y = 20 })
                {
                    PointerId = 7,
                    Properties = new UniversalPointerPointProperties { IsLeftButtonPressed = true },
                })
            {
                Metadata = new UniversalMetadata
                {
                    Modifiers = UniversalModifierKeys.Control,
                    ClickCount = 2,
                },
            };

            var input = sut.CreatePointerPressedInput(universal, new DateTime(2026, 3, 19, 10, 0, 0, DateTimeKind.Utc));

            Assert.Multiple(() =>
            {
                Assert.That(input.X, Is.EqualTo(10));
                Assert.That(input.Y, Is.EqualTo(20));
                Assert.That(input.PointerId, Is.EqualTo(7));
                Assert.That(input.Button, Is.EqualTo(GridMouseButton.Left));
                Assert.That(input.ClickCount, Is.EqualTo(2));
                Assert.That(input.PointerKind, Is.EqualTo(GridPointerKind.Mouse));
                Assert.That(input.HasControl, Is.True);
            });
        }

        [Test]
        public void CreateWheelInput_MapsDeltaPositionAndModifiers()
        {
            var sut = new GridSurfaceUniversalInputAdapter();
            var universal = new UniversalPointerWheelChangedEventArgs(120, new UniversalPoint { X = 30, Y = 40 })
            {
                Metadata = new UniversalMetadata { Modifiers = UniversalModifierKeys.Shift },
            };

            var input = sut.CreateWheelInput(universal, new DateTime(2026, 3, 19, 10, 0, 0, DateTimeKind.Utc));

            Assert.Multiple(() =>
            {
                Assert.That(input.X, Is.EqualTo(30));
                Assert.That(input.Y, Is.EqualTo(40));
                Assert.That(input.DeltaY, Is.EqualTo(120));
                Assert.That(input.HasShift, Is.True);
            });
        }

        [Test]
        public void CreateFocusInput_MapsFocusState()
        {
            var sut = new GridSurfaceUniversalInputAdapter();

            var gained = sut.CreateFocusInput(
                new UniversalFocusChangedEventArgs(true),
                new DateTime(2026, 3, 19, 10, 0, 0, DateTimeKind.Utc));
            var lost = sut.CreateFocusInput(
                new UniversalFocusChangedEventArgs(false),
                new DateTime(2026, 3, 19, 10, 0, 1, DateTimeKind.Utc));

            Assert.Multiple(() =>
            {
                Assert.That(gained.HasFocus, Is.True);
                Assert.That(lost.HasFocus, Is.False);
            });
        }

        [Test]
        public void CreatePointerPressedInput_ForTouch_PreservesPointerIdentifier()
        {
            var sut = new GridSurfaceUniversalInputAdapter();
            var universal = new UniversalPointerRoutedEventArgs(
                new UniversalPointer(DeviceType.Touch, new UniversalPoint { X = 5, Y = 6 })
                {
                    PointerId = 42,
                    Properties = new UniversalPointerPointProperties { Pressure = 0.5d },
                });

            var input = sut.CreatePointerPressedInput(universal, new DateTime(2026, 3, 19, 10, 0, 0, DateTimeKind.Utc));

            Assert.Multiple(() =>
            {
                Assert.That(input.PointerId, Is.EqualTo(42));
                Assert.That(input.PointerKind, Is.EqualTo(GridPointerKind.Touch));
            });
        }

        [Test]
        public void CreatePointerCanceledInput_MapsPointerCoordinatesAndReason()
        {
            var sut = new GridSurfaceUniversalInputAdapter();
            var universal = new UniversalPointerCanceledEventArgs(
                new UniversalPointer(DeviceType.Mouse, new UniversalPoint { X = 15, Y = 25 })
                {
                    PointerId = 9,
                },
                UniversalPointerCancelReason.FocusLost)
            {
                Metadata = new UniversalMetadata { Modifiers = UniversalModifierKeys.Alt },
            };

            var input = sut.CreatePointerCanceledInput(universal, new DateTime(2026, 3, 20, 10, 0, 0, DateTimeKind.Utc));

            Assert.Multiple(() =>
            {
                Assert.That(input.X, Is.EqualTo(15));
                Assert.That(input.Y, Is.EqualTo(25));
                Assert.That(input.PointerId, Is.EqualTo(9));
                Assert.That(input.PointerKind, Is.EqualTo(GridPointerKind.Mouse));
                Assert.That(input.Reason, Is.EqualTo(GridPointerCancelReason.FocusLost));
                Assert.That(input.HasAlt, Is.True);
            });
        }

        [Test]
        public void CreateKeyInput_AndCreateTextInput_DoNotDuplicateCharacterPayload()
        {
            var sut = new GridSurfaceUniversalInputAdapter();
            var keyArgs = new UniversalKeyEventArgs("A", true)
            {
                Metadata = new UniversalMetadata { Modifiers = UniversalModifierKeys.None },
            };
            var textArgs = new UniversalTextChangedEventArgs("a");

            var keyInput = sut.CreateKeyInput(keyArgs, new DateTime(2026, 3, 19, 10, 0, 0, DateTimeKind.Utc));
            var textInput = sut.CreateTextInput(textArgs, new DateTime(2026, 3, 19, 10, 0, 0, DateTimeKind.Utc));

            Assert.Multiple(() =>
            {
                Assert.That(keyInput.Key, Is.EqualTo(GridKey.A));
                Assert.That(keyInput.Character, Is.Null);
                Assert.That(textInput.Key, Is.EqualTo(GridKey.Unknown));
                Assert.That(textInput.Character, Is.EqualTo('a'));
            });
        }

        [Test]
        public void CreateEditorValueInput_MapsUniversalEditorValueChangedEvent()
        {
            var sut = new GridSurfaceUniversalInputAdapter();
            var universal = new UniversalEditorValueChangedEventArgs("row-1", "Owner", "Municipality", UniversalEditorValueChangeKind.SelectionCommitted)
            {
                Metadata = new UniversalMetadata { Modifiers = UniversalModifierKeys.Control | UniversalModifierKeys.Shift },
            };

            var input = sut.CreateEditorValueInput(universal, new DateTime(2026, 3, 29, 18, 0, 0, DateTimeKind.Utc));

            Assert.Multiple(() =>
            {
                Assert.That(input.RowKey, Is.EqualTo("row-1"));
                Assert.That(input.ColumnKey, Is.EqualTo("Owner"));
                Assert.That(input.Value, Is.EqualTo("Municipality"));
                Assert.That(input.ChangeKind, Is.EqualTo(GridEditorValueChangeKind.SelectionCommitted));
                Assert.That(input.HasControl, Is.True);
                Assert.That(input.HasShift, Is.True);
            });
        }

        [Test]
        public void CreateEditCommandInput_MapsUniversalCommandToGridEditCommand()
        {
            var sut = new GridSurfaceUniversalInputAdapter();
            var universal = new UniversalCommandEventArgs(GridUniversalCommandIds.PostEdit, ctrl: true, alt: false, shift: true)
            {
                Metadata = new UniversalMetadata { Modifiers = UniversalModifierKeys.Control | UniversalModifierKeys.Shift },
            };

            var input = sut.CreateEditCommandInput(universal, new DateTime(2026, 3, 29, 18, 5, 0, DateTimeKind.Utc));

            Assert.Multiple(() =>
            {
                Assert.That(input.CommandKind, Is.EqualTo(GridEditCommandKind.PostEdit));
                Assert.That(input.HasControl, Is.True);
                Assert.That(input.HasShift, Is.True);
            });
        }

        [Test]
        public void CreateScrollChangedInput_MapsUniversalHostScrollSignal()
        {
            var sut = new GridSurfaceUniversalInputAdapter();
            var universal = new UniversalScrollChangedEventArgs(320d, 180d)
            {
                Metadata = new UniversalMetadata { Modifiers = UniversalModifierKeys.None },
            };

            var input = sut.CreateScrollChangedInput(universal, new DateTime(2026, 3, 29, 18, 6, 0, DateTimeKind.Utc));

            Assert.Multiple(() =>
            {
                Assert.That(input.HorizontalOffset, Is.EqualTo(320d));
                Assert.That(input.VerticalOffset, Is.EqualTo(180d));
            });
        }

        [Test]
        public void CreateViewportChangedInput_MapsUniversalHostViewportSignal()
        {
            var sut = new GridSurfaceUniversalInputAdapter();
            var universal = new UniversalViewportChangedEventArgs(1024d, 768d)
            {
                Metadata = new UniversalMetadata { Modifiers = UniversalModifierKeys.None },
            };

            var input = sut.CreateViewportChangedInput(universal, new DateTime(2026, 3, 29, 18, 7, 0, DateTimeKind.Utc));

            Assert.Multiple(() =>
            {
                Assert.That(input.Width, Is.EqualTo(1024d));
                Assert.That(input.Height, Is.EqualTo(768d));
            });
        }

        [Test]
        public void CreateRegionCommandInput_MapsUniversalRegionCommandToCoreToggleCommand()
        {
            var sut = new GridSurfaceUniversalInputAdapter();
            var universal = new UniversalCommandEventArgs(GridUniversalCommandIds.ToggleSideToolRegion, ctrl: false, alt: false, shift: true)
            {
                Metadata = new UniversalMetadata { Modifiers = UniversalModifierKeys.Shift },
            };

            var input = sut.CreateRegionCommandInput(universal, new DateTime(2026, 4, 2, 12, 15, 0, DateTimeKind.Utc));

            Assert.Multiple(() =>
            {
                Assert.That(input.CommandKind, Is.EqualTo(GridRegionCommandKind.ToggleCollapse));
                Assert.That(input.RegionKind, Is.EqualTo(Regions.GridRegionKind.SideToolRegion));
                Assert.That(input.HasShift, Is.True);
            });
        }
    }
}
