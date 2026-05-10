using UniversalInput.Contracts;

namespace PhialeGrid.Wpf.Tests.Surface
{
    internal static class GridSurfaceLowLevelInputHost
    {
        public static void ClickPoint(PhialeTech.PhialeGrid.Wpf.Surface.GridSurfaceHost surfaceHost, double x, double y, UniversalModifierKeys modifiers = UniversalModifierKeys.None)
        {
            surfaceHost.HandlePointerPressedForTesting(CreatePointerArgs(x, y, clickCount: 1, modifiers));
            surfaceHost.HandlePointerReleasedForTesting(CreatePointerReleaseArgs(x, y, modifiers));
        }

        public static void DoubleClickPoint(PhialeTech.PhialeGrid.Wpf.Surface.GridSurfaceHost surfaceHost, double x, double y, UniversalModifierKeys modifiers = UniversalModifierKeys.None)
        {
            surfaceHost.HandlePointerPressedForTesting(CreatePointerArgs(x, y, clickCount: 2, modifiers));
            surfaceHost.HandlePointerReleasedForTesting(CreatePointerReleaseArgs(x, y, modifiers));
        }

        public static void DragPoint(PhialeTech.PhialeGrid.Wpf.Surface.GridSurfaceHost surfaceHost, double pressX, double pressY, double moveX, double moveY, UniversalModifierKeys modifiers = UniversalModifierKeys.None)
        {
            surfaceHost.HandlePointerPressedForTesting(CreatePointerArgs(pressX, pressY, clickCount: 1, modifiers));
            surfaceHost.HandlePointerMovedForTesting(CreatePointerMoveArgs(moveX, moveY, modifiers));
            surfaceHost.HandlePointerReleasedForTesting(CreatePointerReleaseArgs(moveX, moveY, modifiers));
        }

        public static void SendText(PhialeTech.PhialeGrid.Wpf.Surface.GridSurfaceHost surfaceHost, string text)
        {
            surfaceHost.HandleTextForTesting(new UniversalTextChangedEventArgs(text)
            {
                Metadata = new UniversalMetadata(),
            });
        }

        public static void SendKey(PhialeTech.PhialeGrid.Wpf.Surface.GridSurfaceHost surfaceHost, string key, bool isDown = true)
        {
            surfaceHost.HandleKeyForTesting(new UniversalKeyEventArgs(key, isDown)
            {
                Metadata = new UniversalMetadata(),
            });
        }

        private static UniversalPointerRoutedEventArgs CreatePointerArgs(double x, double y, int clickCount, UniversalModifierKeys modifiers)
        {
            return new UniversalPointerRoutedEventArgs(
                new UniversalPointer(DeviceType.Mouse, new UniversalPoint { X = x, Y = y })
                {
                    Properties = new UniversalPointerPointProperties { IsLeftButtonPressed = true },
                })
            {
                Metadata = new UniversalMetadata
                {
                    ClickCount = clickCount,
                    ChangedButton = UniversalPointerButton.Left,
                    Modifiers = modifiers,
                },
            };
        }

        private static UniversalPointerRoutedEventArgs CreatePointerMoveArgs(double x, double y, UniversalModifierKeys modifiers)
        {
            return new UniversalPointerRoutedEventArgs(
                new UniversalPointer(DeviceType.Mouse, new UniversalPoint { X = x, Y = y })
                {
                    Properties = new UniversalPointerPointProperties { IsLeftButtonPressed = true },
                })
            {
                Metadata = new UniversalMetadata
                {
                    ChangedButton = UniversalPointerButton.Left,
                    Modifiers = modifiers,
                },
            };
        }

        private static UniversalPointerRoutedEventArgs CreatePointerReleaseArgs(double x, double y, UniversalModifierKeys modifiers)
        {
            return new UniversalPointerRoutedEventArgs(
                new UniversalPointer(DeviceType.Mouse, new UniversalPoint { X = x, Y = y }))
            {
                Metadata = new UniversalMetadata
                {
                    ChangedButton = UniversalPointerButton.Left,
                    Modifiers = modifiers,
                },
            };
        }
    }
}

