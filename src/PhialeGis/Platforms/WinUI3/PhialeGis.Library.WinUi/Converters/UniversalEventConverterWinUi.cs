// PhialeGis.Library.WinUi/Conversions/UniversalEventConverterWinUi.cs
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Input;          // ← WinUI 3 input
using UniversalInput.Contracts;
using Windows.Foundation;           // Point

namespace PhialeGis.Library.WinUi.Conversions
{
    internal static class UniversalEventConverterWinUi
    {
        // ---------- Pointer ----------------------------------------------------
        internal static UniversalPointerRoutedEventArgs Convert(PointerRoutedEventArgs e, UIElement element)
        {
            PointerPoint pp = e.GetCurrentPoint(element);     // Microsoft.UI.Input.PointerPoint
            var pt = ToUniversalPoint(pp.Position);
            var props = ToUniversalProps(pp.Properties);

            var uniPtr = new UniversalPointer(MapDeviceType(e.Pointer.PointerDeviceType), pt)
            {
                Properties = props,
                PointerId = e.Pointer.PointerId
            };
            return new UniversalPointerRoutedEventArgs(uniPtr) { Handled = e.Handled };
        }

        private static UniversalInput.Contracts.DeviceType MapDeviceType(PointerDeviceType t)
            => t switch
            {
                PointerDeviceType.Mouse => UniversalInput.Contracts.DeviceType.Mouse,
                PointerDeviceType.Pen => UniversalInput.Contracts.DeviceType.Pen,
                PointerDeviceType.Touch => UniversalInput.Contracts.DeviceType.Pen, // normalize touch→pen
                _ => UniversalInput.Contracts.DeviceType.Other
            };

        private static UniversalPoint ToUniversalPoint(Point p) => new UniversalPoint { X = p.X, Y = p.Y };

        private static UniversalPointerPointProperties ToUniversalProps(PointerPointProperties p)
            => new UniversalPointerPointProperties
            {
                IsLeftButtonPressed = p.IsLeftButtonPressed,
                IsRightButtonPressed = p.IsRightButtonPressed,
                IsMiddleButtonPressed = p.IsMiddleButtonPressed,
                Pressure = p.Pressure
            };

        // ---------- Manipulations ---------------------------------------------
        public static UniversalManipulationStartingRoutedEventArgs Convert(ManipulationStartingRoutedEventArgs e, UIElement _)
        {
            var pivot = e.Pivot != null
                ? new UniversalPivot { Center = ToUniversalPoint(e.Pivot.Center), Radius = e.Pivot.Radius }
                : null;
            return new UniversalManipulationStartingRoutedEventArgs(
                UniversalInput.Contracts.DeviceType.Pen, pivot);
        }

        public static UniversalManipulationStartedRoutedEventArgs Convert(ManipulationStartedRoutedEventArgs e, UIElement _)
        {
            var pos = ToUniversalPoint(e.Position);
            var dev = MapDeviceType(e.PointerDeviceType);
            return new UniversalManipulationStartedRoutedEventArgs(pos, dev, cumulative: null);
        }

        public static UniversalManipulationDeltaRoutedEventArgs Convert(ManipulationDeltaRoutedEventArgs e, UIElement _)
        {
            var dev = (e.Delta.Expansion != 0 || e.Delta.Rotation != 0 || e.Delta.Scale != 1)
                ? UniversalInput.Contracts.DeviceType.MultiTouch
                : MapDeviceType(e.PointerDeviceType);

            var pos = ToUniversalPoint(e.Position);

            var frameDelta = new UniversalManipulationDelta(
                ToUniversalPoint(e.Delta.Translation), e.Delta.Rotation, e.Delta.Scale);

            var cumulative = new UniversalManipulationDelta(
                ToUniversalPoint(e.Cumulative.Translation), e.Cumulative.Rotation, e.Cumulative.Scale);

            return new UniversalManipulationDeltaRoutedEventArgs(pos, dev, frameDelta, cumulative);
        }

        public static UniversalManipulationCompletedRoutedEventArgs Convert(ManipulationCompletedRoutedEventArgs e, UIElement _)
        {
            var cumulative = new UniversalManipulationDelta(
                ToUniversalPoint(e.Cumulative.Translation), e.Cumulative.Rotation, e.Cumulative.Scale);

            var vel = new UniversalManipulationVelocities
            {
                Linear = ToUniversalPoint(e.Velocities.Linear),
                Angular = e.Velocities.Angular,
                Expansion = e.Velocities.Expansion
            };

            // Używamy argumentów POZYCYJNYCH (Twoja klasa nie ma nazw 'deviceType' itd.)
            var result = new UniversalManipulationCompletedRoutedEventArgs(
                e.Handled,
                cumulative,
                e.IsInertial,
                MapDeviceType(e.PointerDeviceType),
                ToUniversalPoint(e.Position),
                vel);

            result.Handled = e.Handled;
            return result;
        }
    }
}

