using System;
using UniversalInput.Contracts;

namespace PhialeGrid.Core.Interaction
{
    /// <summary>
    /// Maps shared Universal Input contracts to the grid's internal input model.
    /// </summary>
    public sealed class GridSurfaceUniversalInputAdapter
    {
        public GridPointerPressedInput CreatePointerPressedInput(UniversalPointerRoutedEventArgs args, DateTime timestamp)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            return new GridPointerPressedInput(
                timestamp,
                args.Pointer.Position.X,
                args.Pointer.Position.Y,
                MapPointerButton(args),
                Math.Max(1, args.Metadata?.ClickCount ?? 1),
                unchecked((int)args.Pointer.PointerId),
                MapPointerKind(args.Pointer.PointerDeviceType),
                MapModifiers(args.Metadata));
        }

        public GridPointerMovedInput CreatePointerMovedInput(UniversalPointerRoutedEventArgs args, DateTime timestamp)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            return new GridPointerMovedInput(
                timestamp,
                args.Pointer.Position.X,
                args.Pointer.Position.Y,
                unchecked((int)args.Pointer.PointerId),
                MapPointerKind(args.Pointer.PointerDeviceType),
                MapModifiers(args.Metadata));
        }

        public GridPointerReleasedInput CreatePointerReleasedInput(UniversalPointerRoutedEventArgs args, DateTime timestamp)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            return new GridPointerReleasedInput(
                timestamp,
                args.Pointer.Position.X,
                args.Pointer.Position.Y,
                MapPointerButton(args),
                unchecked((int)args.Pointer.PointerId),
                MapPointerKind(args.Pointer.PointerDeviceType),
                MapModifiers(args.Metadata));
        }

        public GridPointerCanceledInput CreatePointerCanceledInput(UniversalPointerCanceledEventArgs args, DateTime timestamp)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            return new GridPointerCanceledInput(
                timestamp,
                args.Pointer.Position.X,
                args.Pointer.Position.Y,
                unchecked((int)args.Pointer.PointerId),
                MapPointerKind(args.Pointer.PointerDeviceType),
                MapPointerCancelReason(args.Reason),
                MapModifiers(args.Metadata));
        }

        public GridWheelInput CreateWheelInput(UniversalPointerWheelChangedEventArgs args, DateTime timestamp)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            return new GridWheelInput(
                timestamp,
                args.Position.X,
                args.Position.Y,
                0,
                args.Delta,
                GridWheelMode.Pixel,
                MapModifiers(args.Metadata));
        }

        public GridKeyInput CreateKeyInput(UniversalKeyEventArgs args, DateTime timestamp)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            return new GridKeyInput(
                timestamp,
                MapKey(args.Key),
                args.IsKeyDown ? GridKeyEventKind.KeyDown : GridKeyEventKind.KeyUp,
                MapModifiers(args.Metadata))
            {
                IsRepeat = args.IsRepeat,
            };
        }

        public GridKeyInput CreateTextInput(UniversalTextChangedEventArgs args, DateTime timestamp)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            var character = string.IsNullOrEmpty(args.Text) ? (char?)null : args.Text[0];
            return new GridKeyInput(
                timestamp,
                GridKey.Unknown,
                GridKeyEventKind.KeyDown,
                MapModifiers(args.Metadata),
                character);
        }

        public GridEditorValueInput CreateEditorValueInput(UniversalEditorValueChangedEventArgs args, DateTime timestamp)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            return new GridEditorValueInput(
                timestamp,
                args.RowKey,
                args.ColumnKey,
                args.Text,
                MapEditorValueChangeKind(args.ChangeKind),
                MapModifiers(args.Metadata));
        }

        public GridEditCommandInput CreateEditCommandInput(UniversalCommandEventArgs args, DateTime timestamp)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            return new GridEditCommandInput(
                timestamp,
                MapEditCommandKind(args.CommandId),
                MapModifiers(args.Metadata));
        }

        public GridRegionCommandInput CreateRegionCommandInput(UniversalCommandEventArgs args, DateTime timestamp)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            return new GridRegionCommandInput(
                timestamp,
                GridRegionCommandKind.ToggleCollapse,
                MapRegionKind(args.CommandId),
                modifiers: MapModifiers(args.Metadata));
        }

        public GridRegionCommandInput CreateRegionStateInput(
            GridRegionCommandKind commandKind,
            Regions.GridRegionKind regionKind,
            DateTime timestamp,
            GridInputModifiers modifiers = GridInputModifiers.None)
        {
            return new GridRegionCommandInput(timestamp, commandKind, regionKind, modifiers: modifiers);
        }

        public GridRegionCommandInput CreateRegionResizeInput(
            Regions.GridRegionKind regionKind,
            double requestedSize,
            DateTime timestamp,
            GridInputModifiers modifiers = GridInputModifiers.None)
        {
            return new GridRegionCommandInput(
                timestamp,
                GridRegionCommandKind.Resize,
                regionKind,
                requestedSize: requestedSize,
                modifiers: modifiers);
        }

        public GridRegionCommandInput CreateRegionMoveInput(
            Regions.GridRegionKind regionKind,
            Regions.GridRegionPlacement requestedPlacement,
            DateTime timestamp,
            GridInputModifiers modifiers = GridInputModifiers.None)
        {
            return new GridRegionCommandInput(
                timestamp,
                GridRegionCommandKind.Move,
                regionKind,
                requestedPlacement: requestedPlacement,
                modifiers: modifiers);
        }

        public GridHostScrollChangedInput CreateScrollChangedInput(UniversalScrollChangedEventArgs args, DateTime timestamp)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            return new GridHostScrollChangedInput(
                timestamp,
                args.HorizontalOffset,
                args.VerticalOffset,
                MapModifiers(args.Metadata));
        }

        public GridHostViewportChangedInput CreateViewportChangedInput(UniversalViewportChangedEventArgs args, DateTime timestamp)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            return new GridHostViewportChangedInput(
                timestamp,
                args.Width,
                args.Height,
                MapModifiers(args.Metadata));
        }

        public GridFocusInput CreateFocusInput(UniversalFocusChangedEventArgs args, DateTime timestamp)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            return new GridFocusInput(
                timestamp,
                args.HasFocus,
                GridFocusCause.Programmatic);
        }

        public GridManipulationStartedInput CreateManipulationStartedInput(UniversalManipulationStartedRoutedEventArgs args, DateTime timestamp)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            return new GridManipulationStartedInput(
                timestamp,
                args.Position.X,
                args.Position.Y,
                MapManipulationKind(args.PointerDeviceType),
                MapModifiers(args.Metadata));
        }

        public GridManipulationDeltaInput CreateManipulationDeltaInput(UniversalManipulationDeltaRoutedEventArgs args, DateTime timestamp)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            return new GridManipulationDeltaInput(
                timestamp,
                args.Position.X,
                args.Position.Y,
                args.Delta == null ? 0 : args.Delta.Translation.X,
                args.Delta == null ? 0 : args.Delta.Translation.Y,
                args.Delta?.Scale ?? 1.0f,
                args.Delta?.Rotation ?? 0,
                MapManipulationKind(args.PointerDeviceType, args.Delta),
                MapModifiers(args.Metadata));
        }

        public GridManipulationCompletedInput CreateManipulationCompletedInput(UniversalManipulationCompletedRoutedEventArgs args, DateTime timestamp)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            return new GridManipulationCompletedInput(
                timestamp,
                args.Position.X,
                args.Position.Y,
                MapManipulationKind(args.PointerDeviceType, args.Cumulative),
                MapModifiers(args.Metadata))
            {
                IsSuccess = !args.IsInertial,
            };
        }

        private static GridInputModifiers MapModifiers(UniversalMetadata metadata)
        {
            var modifiers = GridInputModifiers.None;
            var universalModifiers = metadata?.Modifiers ?? UniversalModifierKeys.None;

            if ((universalModifiers & UniversalModifierKeys.Shift) != 0)
            {
                modifiers |= GridInputModifiers.Shift;
            }

            if ((universalModifiers & UniversalModifierKeys.Control) != 0)
            {
                modifiers |= GridInputModifiers.Control;
            }

            if ((universalModifiers & UniversalModifierKeys.Alt) != 0)
            {
                modifiers |= GridInputModifiers.Alt;
            }

            if ((universalModifiers & UniversalModifierKeys.Windows) != 0)
            {
                modifiers |= GridInputModifiers.Super;
            }

            return modifiers;
        }

        private static GridPointerKind MapPointerKind(DeviceType deviceType)
        {
            switch (deviceType)
            {
                case DeviceType.Touch:
                case DeviceType.MultiTouch:
                    return GridPointerKind.Touch;
                case DeviceType.Pen:
                    return GridPointerKind.Pen;
                default:
                    return GridPointerKind.Mouse;
            }
        }

        private static GridMouseButton MapPointerButton(UniversalPointerRoutedEventArgs args)
        {
            var changedButton = args.Metadata?.ChangedButton ?? UniversalPointerButton.None;
            switch (changedButton)
            {
                case UniversalPointerButton.Middle:
                    return GridMouseButton.Middle;
                case UniversalPointerButton.Right:
                    return GridMouseButton.Right;
                case UniversalPointerButton.Left:
                    return GridMouseButton.Left;
            }

            var properties = args.Pointer.Properties;
            if (properties?.IsRightButtonPressed == true)
            {
                return GridMouseButton.Right;
            }

            if (properties?.IsMiddleButtonPressed == true)
            {
                return GridMouseButton.Middle;
            }

            return GridMouseButton.Left;
        }

        private static GridKey MapKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return GridKey.Unknown;
            }

            switch (key.Trim().ToUpperInvariant())
            {
                case "LEFT":
                    return GridKey.Left;
                case "RIGHT":
                    return GridKey.Right;
                case "UP":
                    return GridKey.Up;
                case "DOWN":
                    return GridKey.Down;
                case "TAB":
                    return GridKey.Tab;
                case "ENTER":
                case "RETURN":
                    return GridKey.Return;
                case "ESC":
                case "ESCAPE":
                    return GridKey.Escape;
                case "DELETE":
                    return GridKey.Delete;
                case "HOME":
                    return GridKey.Home;
                case "END":
                    return GridKey.End;
                case "PAGEUP":
                    return GridKey.PageUp;
                case "PAGEDOWN":
                    return GridKey.PageDown;
                case "SPACE":
                    return GridKey.Space;
                case "F1":
                    return GridKey.F1;
                case "F2":
                    return GridKey.F2;
                case "F3":
                    return GridKey.F3;
                case "F4":
                    return GridKey.F4;
                case "F5":
                    return GridKey.F5;
                case "F6":
                    return GridKey.F6;
                case "F7":
                    return GridKey.F7;
                case "F8":
                    return GridKey.F8;
                case "F9":
                    return GridKey.F9;
                case "F10":
                    return GridKey.F10;
                case "F11":
                    return GridKey.F11;
                case "F12":
                    return GridKey.F12;
            }

            if (key.Length == 1)
            {
                var c = char.ToUpperInvariant(key[0]);
                if (c >= 'A' && c <= 'Z')
                {
                    return (GridKey)Enum.Parse(typeof(GridKey), c.ToString());
                }

                if (c >= '0' && c <= '9')
                {
                    return (GridKey)Enum.Parse(typeof(GridKey), "D" + c);
                }
            }

            return GridKey.Unknown;
        }

        private static GridManipulationKind MapManipulationKind(DeviceType deviceType, UniversalManipulationDelta delta = null)
        {
            if (delta != null)
            {
                if (delta.Scale != 1.0f)
                {
                    return GridManipulationKind.Pinch;
                }

                if (delta.Rotation != 0)
                {
                    return GridManipulationKind.Rotate;
                }
            }

            return deviceType == DeviceType.Touch || deviceType == DeviceType.MultiTouch
                ? GridManipulationKind.Pan
                : GridManipulationKind.Other;
        }

        private static GridPointerCancelReason MapPointerCancelReason(UniversalPointerCancelReason reason)
        {
            switch (reason)
            {
                case UniversalPointerCancelReason.CaptureLost:
                    return GridPointerCancelReason.CaptureLost;
                case UniversalPointerCancelReason.FocusLost:
                    return GridPointerCancelReason.FocusLost;
                case UniversalPointerCancelReason.Unloaded:
                    return GridPointerCancelReason.Unloaded;
                case UniversalPointerCancelReason.ManipulationStarted:
                    return GridPointerCancelReason.ManipulationStarted;
                default:
                    return GridPointerCancelReason.PlatformCanceled;
            }
        }

        private static GridEditCommandKind MapEditCommandKind(string commandId)
        {
            switch (commandId ?? string.Empty)
            {
                case GridUniversalCommandIds.BeginEdit:
                    return GridEditCommandKind.BeginEdit;
                case GridUniversalCommandIds.PostEdit:
                    return GridEditCommandKind.PostEdit;
                case GridUniversalCommandIds.CancelEdit:
                    return GridEditCommandKind.CancelEdit;
                default:
                    throw new ArgumentOutOfRangeException(nameof(commandId), commandId, "Unsupported grid command identifier.");
            }
        }

        private static Regions.GridRegionKind MapRegionKind(string commandId)
        {
            switch (commandId ?? string.Empty)
            {
                case GridUniversalCommandIds.ToggleTopCommandRegion:
                    return Regions.GridRegionKind.TopCommandRegion;
                case GridUniversalCommandIds.ToggleGroupingRegion:
                    return Regions.GridRegionKind.GroupingRegion;
                case GridUniversalCommandIds.ToggleSummaryBottomRegion:
                    return Regions.GridRegionKind.SummaryBottomRegion;
                case GridUniversalCommandIds.ToggleSideToolRegion:
                    return Regions.GridRegionKind.SideToolRegion;
                default:
                    throw new ArgumentOutOfRangeException(nameof(commandId), commandId, "Unsupported region command identifier.");
            }
        }

        private static GridEditorValueChangeKind MapEditorValueChangeKind(UniversalEditorValueChangeKind changeKind)
        {
            switch (changeKind)
            {
                case UniversalEditorValueChangeKind.SelectionCommitted:
                    return GridEditorValueChangeKind.SelectionCommitted;
                default:
                    return GridEditorValueChangeKind.TextEdited;
            }
        }
    }
}
