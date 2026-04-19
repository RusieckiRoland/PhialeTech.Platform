using Windows.UI.Xaml.Input;
using Windows.Foundation;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.Devices.Input;
using UniversalInput.Contracts;
using UniversalInput.Contracts.EditorEnums;


namespace PhialeGis.Library.UwpUi.Conversions
{
    /// <summary>
    /// Converts UWP input/manipulation events into platform-agnostic Universal* event args.
    /// Keep this layer thin: no domain logic here—only mapping of types and values.
    /// </summary>
    internal static class UniversalEventConverter
    {
        /// <summary>
        /// Pointer → UniversalPointerRoutedEventArgs.
        /// Includes position, device type, pointer id, and button/pressure properties.
        /// </summary>
        [Windows.Foundation.Metadata.DefaultOverload]
        internal static UniversalPointerRoutedEventArgs Convert(PointerRoutedEventArgs e, UIElement element)
        {
            var pointerDeviceType = ConvertPointerDeviceType(e.Pointer.PointerDeviceType);
            var currentPoint = e.GetCurrentPoint(element);
            var universalPoint = ToUniversalPoint(currentPoint.Position);
            var pointerProperties = ConvertToUniversalPointerPointProperties(currentPoint.Properties);

            var universalPointer = new UniversalPointer(pointerDeviceType, universalPoint)
            {
                Properties = pointerProperties,
                PointerId = e.Pointer.PointerId
            };

            return new UniversalPointerRoutedEventArgs(universalPointer)
            {
                Handled = e.Handled
            };
        }

        /// <summary>
        /// Maps UWP PointerDeviceType to universal DeviceType.
        /// NOTE (ADR-004): Touch is intentionally mapped to Pen to unify downstream handling.
        /// </summary>
        private static DeviceType ConvertPointerDeviceType(PointerDeviceType pointerDeviceType)
        {
            switch (pointerDeviceType)
            {
                case PointerDeviceType.Mouse: return DeviceType.Mouse;
                case PointerDeviceType.Pen: return DeviceType.Pen;
                case PointerDeviceType.Touch: return DeviceType.Pen; // ADR-004: treat Touch as Pen intentionally
                default: return DeviceType.Other;
            }
        }

        /// <summary>Point (UWP) → UniversalPoint.</summary>
        private static UniversalPoint ToUniversalPoint(Point point)
            => new UniversalPoint { X = point.X, Y = point.Y };

        /// <summary>
        /// PointerPointProperties (UWP) → UniversalPointerPointProperties.
        /// Only WinRT-safe, editor-agnostic fields are mapped.
        /// </summary>
        private static UniversalPointerPointProperties ConvertToUniversalPointerPointProperties(PointerPointProperties properties)
        {
            return new UniversalPointerPointProperties
            {
                IsLeftButtonPressed = properties.IsLeftButtonPressed,
                IsRightButtonPressed = properties.IsRightButtonPressed,
                IsMiddleButtonPressed = properties.IsMiddleButtonPressed,
                Pressure = properties.Pressure
            };
        }

        /// <summary>
        /// ManipulationStarted (UWP) → UniversalManipulationStartedRoutedEventArgs.
        /// UWP does not provide delta/cumulative on "Started"; we emit position + device type only.
        /// </summary>
        public static UniversalManipulationStartedRoutedEventArgs Convert(ManipulationStartedRoutedEventArgs e, UIElement element)
        {
            var point = ToUniversalPoint(e.Position);
            var pointerDeviceType = ConvertPointerDeviceType(e.PointerDeviceType);
            return new UniversalManipulationStartedRoutedEventArgs(point, pointerDeviceType, cumulative: null);
        }

        /// <summary>
        /// Heuristic: if any scale/rotation/expansion change is present, treat as multi-touch gesture.
        /// </summary>
        private static bool IsMultiTouch(ManipulationDelta delta)
            => delta.Expansion != 0 || delta.Rotation != 0 || delta.Scale != 1;

        /// <summary>
        /// ManipulationStarting (UWP) → UniversalManipulationStartingRoutedEventArgs.
        /// Transfers optional pivot data and uses a unified device type for gesture init.
        /// </summary>
        public static UniversalManipulationStartingRoutedEventArgs Convert(ManipulationStartingRoutedEventArgs e, UIElement element)
        {
            var pivot = e.Pivot != null
                ? new UniversalPivot
                {
                    Center = ToUniversalPoint(e.Pivot.Center),
                    Radius = e.Pivot.Radius
                }
                : null;

            var pointerDeviceType = DeviceType.Pen; // normalized for init stage
            return new UniversalManipulationStartingRoutedEventArgs(pointerDeviceType, pivot);
        }

        /// <summary>
        /// ManipulationDelta (UWP) → UniversalManipulationDeltaRoutedEventArgs.
        /// Emits both per-frame delta and cumulative values, with device type normalized to MultiTouch when applicable.
        /// </summary>
        public static UniversalManipulationDeltaRoutedEventArgs Convert(ManipulationDeltaRoutedEventArgs e, UIElement element)
        {
            var point = ToUniversalPoint(e.Position);
            var pointerDeviceType = IsMultiTouch(e.Delta) ? DeviceType.MultiTouch : ConvertPointerDeviceType(e.PointerDeviceType);

            // Frame delta (relative change since previous frame)
            var deltaTranslation = ToUniversalPoint(e.Delta.Translation);
            var delta = new UniversalManipulationDelta(deltaTranslation, e.Delta.Rotation, e.Delta.Scale);

            // Cumulative change (total since gesture start)
            var cumulativeTranslation = ToUniversalPoint(e.Cumulative.Translation);
            var cumulative = new UniversalManipulationDelta(cumulativeTranslation, e.Cumulative.Rotation, e.Cumulative.Scale);

            return new UniversalManipulationDeltaRoutedEventArgs(point, pointerDeviceType, delta, cumulative);
        }

        /// <summary>
        /// ManipulationCompleted (UWP) → UniversalManipulationCompletedRoutedEventArgs.
        /// Includes final cumulative transform, inertial flag, end position, and velocities.
        /// </summary>
        public static UniversalManipulationCompletedRoutedEventArgs Convert(ManipulationCompletedRoutedEventArgs e, UIElement element)
        {
            var handled = e.Handled;

            var cumulativeTranslation = ToUniversalPoint(e.Cumulative.Translation);
            var cumulative = new UniversalManipulationDelta(cumulativeTranslation, e.Cumulative.Rotation, e.Cumulative.Scale);

            var isInertial = e.IsInertial;
            var deviceType = ConvertPointerDeviceType(e.PointerDeviceType);
            var position = ToUniversalPoint(e.Position);
            var velocities = ToUniversalVelocities(e.Velocities);

            return new UniversalManipulationCompletedRoutedEventArgs(handled, cumulative, isInertial, deviceType, position, velocities)
            {
                Handled = handled
            };
        }

        /// <summary>
        /// ManipulationVelocities (UWP) → UniversalManipulationVelocities.
        /// </summary>
        private static UniversalManipulationVelocities ToUniversalVelocities(ManipulationVelocities velocities)
        {
            var linear = ToUniversalPoint(velocities.Linear);
            return new UniversalManipulationVelocities
            {
                Linear = linear,
                Angular = velocities.Angular,
                Expansion = velocities.Expansion
            };
        }

        // =======================================================================
        // ====================  EDITOR (CODE) CONVERSIONS  ======================
        // =======================================================================

        /// <summary>Text changed → UniversalTextChangedEventArgs.</summary>
        public static UniversalTextChangedEventArgs Convert(string text)
            => new UniversalTextChangedEventArgs(text ?? string.Empty);

        /// <summary>Selection changed → UniversalSelectionChangedEventArgs.</summary>
        public static UniversalSelectionChangedEventArgs ConvertSelection(int start, int end, int caretLine, int caretColumn)
            => new UniversalSelectionChangedEventArgs(start, end, caretLine, caretColumn);

        /// <summary>Caret moved → UniversalCaretMovedEventArgs.</summary>
        public static UniversalCaretMovedEventArgs ConvertCaret(int line, int column, int offset)
            => new UniversalCaretMovedEventArgs(line, column, offset);

        /// <summary>Dirty flag toggled → UniversalDirtyChangedEventArgs.</summary>
        public static UniversalDirtyChangedEventArgs ConvertDirty(bool isDirty)
            => new UniversalDirtyChangedEventArgs(isDirty);

        /// <summary>Command/shortcut → UniversalCommandEventArgs (e.g., "ctrl+s").</summary>
        public static UniversalCommandEventArgs ConvertCommand(string commandId, bool ctrl, bool alt, bool shift)
            => new UniversalCommandEventArgs(commandId ?? string.Empty, ctrl, alt, shift);

        /// <summary>Save requested → UniversalSaveRequestedEventArgs.</summary>
        public static UniversalSaveRequestedEventArgs ConvertSave(string reason)
            => new UniversalSaveRequestedEventArgs(reason ?? string.Empty);

        /// <summary>Language changed → UniversalLanguageChangedEventArgs.</summary>
        public static UniversalLanguageChangedEventArgs ConvertLanguage(string languageId)
            => new UniversalLanguageChangedEventArgs(languageId ?? "plaintext");

        /// <summary>Theme changed → UniversalThemeChangedEventArgs.</summary>
        public static UniversalThemeChangedEventArgs ConvertTheme(string themeId)
            => new UniversalThemeChangedEventArgs(themeId ?? "default");

        /// <summary>Find requested → UniversalFindRequestedEventArgs.</summary>
        public static UniversalFindRequestedEventArgs ConvertFind(string query, bool matchCase, bool regex, bool wholeWord)
            => new UniversalFindRequestedEventArgs(query ?? string.Empty, matchCase, regex, wholeWord);

        /// <summary>Replace requested → UniversalReplaceRequestedEventArgs.</summary>
        public static UniversalReplaceRequestedEventArgs ConvertReplace(string query, string replacement, bool matchCase, bool regex, bool wholeWord)
            => new UniversalReplaceRequestedEventArgs(query ?? string.Empty, replacement ?? string.Empty, matchCase, regex, wholeWord);

        /// <summary>Link clicked → UniversalLinkClickedEventArgs.</summary>
        public static UniversalLinkClickedEventArgs ConvertLink(string url)
            => new UniversalLinkClickedEventArgs(url ?? string.Empty);

        /// <summary>Diagnostics batch → UniversalDiagnosticsUpdatedEventArgs.</summary>
        public static UniversalDiagnosticsUpdatedEventArgs ConvertDiagnostics(
            string documentId,
            int[] lines,
            int[] columns,
            int[] lengths,
            EditorDiagnosticSeverity[] severities,
            string[] messages)
            => new UniversalDiagnosticsUpdatedEventArgs(
                documentId ?? string.Empty,
                lines ?? System.Array.Empty<int>(),
                columns ?? System.Array.Empty<int>(),
                lengths ?? System.Array.Empty<int>(),
                severities ?? System.Array.Empty<EditorDiagnosticSeverity>(),
                messages ?? System.Array.Empty<string>());

        /// <summary>Hover requested → UniversalHoverRequestedEventArgs.</summary>
        public static UniversalHoverRequestedEventArgs ConvertHover(int offset, int line, int column)
            => new UniversalHoverRequestedEventArgs(offset, line, column);
    }
}

