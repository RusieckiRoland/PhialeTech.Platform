using System.Windows;
using System.Windows.Input;
using UniversalInput.Contracts;

namespace PhialeTech.PhialeGrid.Wpf.Controls
{
    internal static class WpfUniversalInputAdapter
    {
        public static UniversalPointerRoutedEventArgs CreatePointerEventArgs(MouseEventArgs args, IInputElement relativeTo)
        {
            return CreateMousePointerMovedEventArgs(
                args.GetPosition(relativeTo),
                Keyboard.Modifiers,
                args.LeftButton,
                args.RightButton,
                args.MiddleButton);
        }

        public static UniversalPointerRoutedEventArgs CreateMousePointerPressedEventArgs(
            Point position,
            MouseButton changedButton,
            int clickCount,
            ModifierKeys modifiers,
            MouseButtonState leftButton,
            MouseButtonState rightButton,
            MouseButtonState middleButton)
        {
            return CreateMousePointerEventArgs(
                position,
                modifiers,
                leftButton,
                rightButton,
                middleButton,
                clickCount,
                changedButton);
        }

        public static UniversalPointerRoutedEventArgs CreateMousePointerMovedEventArgs(
            Point position,
            ModifierKeys modifiers,
            MouseButtonState leftButton,
            MouseButtonState rightButton,
            MouseButtonState middleButton)
        {
            return CreateMousePointerEventArgs(
                position,
                modifiers,
                leftButton,
                rightButton,
                middleButton,
                clickCount: 0,
                changedButton: null);
        }

        public static UniversalPointerRoutedEventArgs CreateMousePointerReleasedEventArgs(
            Point position,
            MouseButton changedButton,
            ModifierKeys modifiers,
            MouseButtonState leftButton,
            MouseButtonState rightButton,
            MouseButtonState middleButton)
        {
            return CreateMousePointerEventArgs(
                position,
                modifiers,
                leftButton,
                rightButton,
                middleButton,
                clickCount: 0,
                changedButton);
        }

        public static UniversalPointerRoutedEventArgs CreateTouchPointerPressedEventArgs(Point position, int pointerId, ModifierKeys modifiers)
        {
            return CreateTouchPointerEventArgs(position, pointerId, modifiers, isPressed: true);
        }

        public static UniversalPointerRoutedEventArgs CreateTouchPointerMovedEventArgs(Point position, int pointerId, ModifierKeys modifiers)
        {
            return CreateTouchPointerEventArgs(position, pointerId, modifiers, isPressed: false);
        }

        public static UniversalPointerRoutedEventArgs CreateTouchPointerReleasedEventArgs(Point position, int pointerId, ModifierKeys modifiers)
        {
            return CreateTouchPointerEventArgs(position, pointerId, modifiers, isPressed: false);
        }

        public static UniversalPointerCanceledEventArgs CreatePointerCanceledEventArgs(
            DeviceType deviceType,
            uint pointerId,
            Point position,
            ModifierKeys modifiers,
            UniversalPointerCancelReason reason)
        {
            return new UniversalPointerCanceledEventArgs(
                new UniversalPointer(deviceType, ToUniversalPoint(position))
                {
                    PointerId = pointerId,
                },
                reason)
            {
                Metadata = CreateMetadata(modifiers),
            };
        }

        public static UniversalPointerWheelChangedEventArgs CreateWheelEventArgs(int delta, Point position, ModifierKeys modifiers)
        {
            // WPF reports positive delta for wheel-up. Grid wheel input uses positive as scroll-down,
            // so we invert here to keep natural desktop scrolling direction.
            return new UniversalPointerWheelChangedEventArgs(-delta, ToUniversalPoint(position))
            {
                Metadata = CreateMetadata(modifiers),
            };
        }

        public static UniversalKeyEventArgs CreateKeyEventArgs(Key key, bool isKeyDown, ModifierKeys modifiers, bool isRepeat)
        {
            return new UniversalKeyEventArgs(key.ToString(), isKeyDown)
            {
                IsRepeat = isRepeat,
                Metadata = CreateMetadata(modifiers),
            };
        }

        public static UniversalTextChangedEventArgs CreateTextEventArgs(string text, ModifierKeys modifiers)
        {
            return new UniversalTextChangedEventArgs(text ?? string.Empty)
            {
                Metadata = CreateMetadata(modifiers),
            };
        }

        public static UniversalEditorValueChangedEventArgs CreateEditorValueChangedEventArgs(
            string rowKey,
            string columnKey,
            string text,
            UniversalEditorValueChangeKind changeKind,
            ModifierKeys modifiers)
        {
            return new UniversalEditorValueChangedEventArgs(rowKey, columnKey, text, changeKind)
            {
                Metadata = CreateMetadata(modifiers),
            };
        }

        public static UniversalCommandEventArgs CreateCommandEventArgs(string commandId, ModifierKeys modifiers)
        {
            return new UniversalCommandEventArgs(
                commandId,
                (modifiers & ModifierKeys.Control) == ModifierKeys.Control,
                (modifiers & ModifierKeys.Alt) == ModifierKeys.Alt,
                (modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                Metadata = CreateMetadata(modifiers),
            };
        }

        public static UniversalScrollChangedEventArgs CreateScrollChangedEventArgs(
            double horizontalOffset,
            double verticalOffset,
            ModifierKeys modifiers = ModifierKeys.None)
        {
            return new UniversalScrollChangedEventArgs(horizontalOffset, verticalOffset)
            {
                Metadata = CreateMetadata(modifiers),
            };
        }

        public static UniversalViewportChangedEventArgs CreateViewportChangedEventArgs(
            double width,
            double height,
            ModifierKeys modifiers = ModifierKeys.None)
        {
            return new UniversalViewportChangedEventArgs(width, height)
            {
                Metadata = CreateMetadata(modifiers),
            };
        }

        public static UniversalFocusChangedEventArgs CreateFocusChangedEventArgs(bool hasFocus, ModifierKeys modifiers = ModifierKeys.None)
        {
            return new UniversalFocusChangedEventArgs(hasFocus)
            {
                Metadata = CreateMetadata(modifiers),
            };
        }

        public static UniversalManipulationStartedRoutedEventArgs CreateManipulationStartedEventArgs(Point position, ModifierKeys modifiers)
        {
            return new UniversalManipulationStartedRoutedEventArgs(
                ToUniversalPoint(position),
                DeviceType.Touch,
                new UniversalManipulationDelta(new UniversalPoint(), 0, 1))
            {
                Metadata = CreateMetadata(modifiers),
            };
        }

        public static UniversalManipulationDeltaRoutedEventArgs CreateManipulationDeltaEventArgs(
            Point position,
            ManipulationDelta delta,
            ManipulationDelta cumulative,
            ModifierKeys modifiers)
        {
            return new UniversalManipulationDeltaRoutedEventArgs(
                ToUniversalPoint(position),
                IsMultiTouch(delta) ? DeviceType.MultiTouch : DeviceType.Touch,
                ToUniversalManipulationDelta(delta),
                ToUniversalManipulationDelta(cumulative))
            {
                Metadata = CreateMetadata(modifiers),
            };
        }

        public static UniversalManipulationCompletedRoutedEventArgs CreateManipulationCompletedEventArgs(
            Point position,
            ManipulationDelta total,
            bool isInertial,
            ManipulationVelocities velocities,
            ModifierKeys modifiers)
        {
            return new UniversalManipulationCompletedRoutedEventArgs(
                handled: false,
                cumulative: ToUniversalManipulationDelta(total),
                isInertial: isInertial,
                pointerDeviceType: IsMultiTouch(total) ? DeviceType.MultiTouch : DeviceType.Touch,
                position: ToUniversalPoint(position),
                velocities: new UniversalManipulationVelocities
                {
                    Linear = ToUniversalPoint(velocities.LinearVelocity),
                    Angular = (float)velocities.AngularVelocity,
                    Expansion = (float)System.Math.Max(velocities.ExpansionVelocity.X, velocities.ExpansionVelocity.Y),
                })
            {
                Metadata = CreateMetadata(modifiers),
            };
        }

        public static UniversalMetadata CreateMetadata(ModifierKeys modifiers)
        {
            return new UniversalMetadata
            {
                Modifiers = ConvertModifiers(modifiers),
            };
        }

        private static UniversalPointerRoutedEventArgs CreateMousePointerEventArgs(
            Point position,
            ModifierKeys modifiers,
            MouseButtonState leftButton,
            MouseButtonState rightButton,
            MouseButtonState middleButton,
            int clickCount,
            MouseButton? changedButton)
        {
            var pointer = new UniversalPointer(DeviceType.Mouse, ToUniversalPoint(position))
            {
                PointerId = 0,
                Properties = new UniversalPointerPointProperties
                {
                    IsLeftButtonPressed = leftButton == MouseButtonState.Pressed,
                    IsRightButtonPressed = rightButton == MouseButtonState.Pressed,
                    IsMiddleButtonPressed = middleButton == MouseButtonState.Pressed,
                },
            };

            var metadata = CreateMetadata(modifiers);
            metadata.ClickCount = clickCount;
            metadata.ChangedButton = ConvertButton(changedButton);

            return new UniversalPointerRoutedEventArgs(pointer)
            {
                Metadata = metadata,
            };
        }

        private static UniversalPointerRoutedEventArgs CreateTouchPointerEventArgs(Point position, int pointerId, ModifierKeys modifiers, bool isPressed)
        {
            var pointer = new UniversalPointer(DeviceType.Touch, ToUniversalPoint(position))
            {
                PointerId = unchecked((uint)pointerId),
                Properties = new UniversalPointerPointProperties
                {
                    IsLeftButtonPressed = isPressed,
                },
            };

            var metadata = CreateMetadata(modifiers);
            metadata.ClickCount = isPressed ? 1 : 0;
            metadata.ChangedButton = UniversalPointerButton.Left;

            return new UniversalPointerRoutedEventArgs(pointer)
            {
                Metadata = metadata,
            };
        }

        private static UniversalManipulationDelta ToUniversalManipulationDelta(ManipulationDelta delta)
        {
            return new UniversalManipulationDelta(
                ToUniversalPoint(delta.Translation),
                (float)delta.Rotation,
                (float)System.Math.Max(delta.Scale.X, delta.Scale.Y));
        }

        private static bool IsMultiTouch(ManipulationDelta delta)
        {
            return delta.Scale.X != 1.0 || delta.Scale.Y != 1.0 || delta.Rotation != 0;
        }

        private static UniversalPoint ToUniversalPoint(Point point)
        {
            return new UniversalPoint
            {
                X = point.X,
                Y = point.Y,
            };
        }

        private static UniversalPoint ToUniversalPoint(Vector vector)
        {
            return new UniversalPoint
            {
                X = vector.X,
                Y = vector.Y,
            };
        }

        private static UniversalPointerButton ConvertButton(MouseButton? button)
        {
            if (!button.HasValue)
            {
                return UniversalPointerButton.None;
            }

            switch (button.Value)
            {
                case MouseButton.Middle:
                    return UniversalPointerButton.Middle;
                case MouseButton.Right:
                    return UniversalPointerButton.Right;
                default:
                    return UniversalPointerButton.Left;
            }
        }

        private static UniversalModifierKeys ConvertModifiers(ModifierKeys modifiers)
        {
            var result = UniversalModifierKeys.None;
            if ((modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                result |= UniversalModifierKeys.Shift;
            }

            if ((modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                result |= UniversalModifierKeys.Control;
            }

            if ((modifiers & ModifierKeys.Alt) == ModifierKeys.Alt)
            {
                result |= UniversalModifierKeys.Alt;
            }

            if ((modifiers & ModifierKeys.Windows) == ModifierKeys.Windows)
            {
                result |= UniversalModifierKeys.Windows;
            }

            return result;
        }
    }
}
