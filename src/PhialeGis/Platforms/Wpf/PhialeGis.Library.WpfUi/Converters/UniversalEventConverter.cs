using UniversalInput.Contracts;
using System;
using System.Windows;
using System.Windows.Input;
using UniversalInput.Contracts.EventEnums;
using UniversalInput.Contracts.EditorEnums;

namespace PhialeGis.Library.WpfUi.Converters
{
    public static class UniversalEventConverter
    {
        public static UniversalPointerRoutedEventArgs Convert(MouseButtonEventArgs e, UIElement element)
        {
            var universalPoint = ToUniversalPoint(e.GetPosition(element));
            var pointerProperties = new UniversalPointerPointProperties
            {
                IsLeftButtonPressed = e.LeftButton == MouseButtonState.Pressed,
                IsRightButtonPressed = e.RightButton == MouseButtonState.Pressed,
            };

            var universalPointer = new UniversalPointer(DeviceType.Mouse, universalPoint)
            {
                Properties = pointerProperties,
                PointerId = 0,
            };

            return new UniversalPointerRoutedEventArgs(universalPointer)
            {
                Handled = e.Handled
            };
        }

        public static UniversalPointerRoutedEventArgs Convert(TouchEventArgs e, UIElement element)
        {
            var universalPoint = ToUniversalPoint(e.GetTouchPoint(element).Position);
            var pointerProperties = new UniversalPointerPointProperties { };

            var universalPointer = new UniversalPointer(DeviceType.Touch, universalPoint)
            {
                Properties = pointerProperties,
                PointerId = (uint)e.TouchDevice.Id,
            };

            return new UniversalPointerRoutedEventArgs(universalPointer)
            {
                Handled = e.Handled
            };
        }

        public static UniversalPointerRoutedEventArgs Convert(StylusDownEventArgs e, UIElement element)
        {
            var universalPoint = ToUniversalPoint(e.GetPosition(element));
            var pointerProperties = new UniversalPointerPointProperties { };

            var universalPointer = new UniversalPointer(DeviceType.Pen, universalPoint)
            {
                Properties = pointerProperties,
                PointerId = (uint)e.StylusDevice.Id,
            };

            return new UniversalPointerRoutedEventArgs(universalPointer)
            {
                Handled = e.Handled
            };
        }

        private static UniversalPoint ToUniversalPoint(Point point)
            => new UniversalPoint { X = point.X, Y = point.Y };

        private static UniversalPoint ToUniversalPoint(Vector vector)
            => new UniversalPoint { X = vector.X, Y = vector.Y };

        private static UniversalVector ConvertToUniversalVector(ManipulationDelta manipulationDelta)
            => new UniversalVector { X = manipulationDelta.Translation.X, Y = manipulationDelta.Translation.Y };

        public static UniversalPointerRoutedEventArgs Convert(MouseEventArgs e, UIElement element)
        {
            var universalPoint = ToUniversalPoint(e.GetPosition(element));
            var universalPointer = new UniversalPointer(DeviceType.Mouse, universalPoint)
            {
                Properties = new UniversalPointerPointProperties(),
                PointerId = 0,
            };

            return new UniversalPointerRoutedEventArgs(universalPointer)
            {
                Handled = e.Handled
            };
        }

        public static UniversalPointerRoutedEventArgs Convert(StylusEventArgs e, UIElement element)
        {
            var universalPoint = ToUniversalPoint(e.GetPosition(element));
            var universalPointer = new UniversalPointer(DeviceType.Pen, universalPoint)
            {
                Properties = new UniversalPointerPointProperties(),
                PointerId = (uint)e.StylusDevice.Id,
            };

            return new UniversalPointerRoutedEventArgs(universalPointer)
            {
                Handled = e.Handled
            };
        }

        public static UniversalManipulationStartedRoutedEventArgs Convert(ManipulationStartedEventArgs e, UIElement element)
        {
            var position = e.ManipulationOrigin;
            var universalPosition = ToUniversalPoint(position);
            return new UniversalManipulationStartedRoutedEventArgs(universalPosition, DeviceType.Touch, null)
            {
                Handled = e.Handled
            };
        }

        public static UniversalManipulationDeltaRoutedEventArgs Convert(ManipulationDeltaEventArgs e, UIElement element)
        {
            var position = ToUniversalPoint(e.ManipulationOrigin);

            var translation = ToUniversalPoint(e.DeltaManipulation.Translation);
            var rotation = (float)e.DeltaManipulation.Rotation;
            var scale = MaxIngridinetFromVector(e.DeltaManipulation.Scale);
            var delta = new UniversalManipulationDelta(translation, rotation, scale);
            DeviceType deviceType = IsMultiTouch(delta) ? DeviceType.MultiTouch : DeviceType.Touch;

            translation = ToUniversalPoint(e.CumulativeManipulation.Translation);
            rotation = (float)e.CumulativeManipulation.Rotation;
            scale = MaxIngridinetFromVector(e.CumulativeManipulation.Scale);

            var cumulative = new UniversalManipulationDelta(translation, rotation, scale);

            return new UniversalManipulationDeltaRoutedEventArgs(position, deviceType, delta, cumulative)
            {
                Handled = false
            };
        }

        public static UniversalManipulationCompletedRoutedEventArgs Convert(ManipulationCompletedEventArgs e, UIElement element)
        {
            var position = ToUniversalPoint(e.ManipulationOrigin);
            var translation = ToUniversalPoint(e.TotalManipulation.Translation);
            var rotation = (float)e.TotalManipulation.Rotation;
            var scale = MaxIngridinetFromVector(e.TotalManipulation.Scale);
            var totalManipulation = new UniversalManipulationDelta(translation, rotation, scale);

            var finalVelocities = new UniversalManipulationVelocities()
            {
                Linear = ToUniversalPoint(e.FinalVelocities.LinearVelocity),
                Angular = (float)e.FinalVelocities.AngularVelocity,
                Expansion = MaxIngridinetFromVector(e.FinalVelocities.ExpansionVelocity)
            };

            return new UniversalManipulationCompletedRoutedEventArgs(
                e.Handled, totalManipulation, e.IsInertial, DeviceType.Pen, position, finalVelocities)
            {
                Handled = e.Handled,
            };
        }

        public static UniversalManipulationStartingRoutedEventArgs Convert(ManipulationStartingEventArgs e, UIElement element)
        {
            var pivot = e.Pivot != null
                ? new UniversalPivot()
                {
                    Radius = e.Pivot.Radius,
                    Center = ToUniversalPoint(e.Pivot.Center)
                }
                : null;

            var universalEventArgs = new UniversalManipulationStartingRoutedEventArgs(DeviceType.Touch, pivot) { };
            return universalEventArgs;
        }

        public static UniversalManipulationModes ToManipulationModeUni(ManipulationModes modeWpf)
            => (UniversalManipulationModes)modeWpf;

        private static bool IsMultiTouch(UniversalManipulationDelta delta)
            => delta.Scale != 1 || delta.Rotation != 0;

        private static float MaxIngridinetFromVector(Vector vector)
            => (float)Math.Max(vector.X, vector.Y);

        // ==========================================================
        // ===============  EDITOR (CODE) CONVERSIONS  ==============
        // ==========================================================

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

        /// <summary>Command/shortcut → UniversalCommandEventArgs.</summary>
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
                lines ?? Array.Empty<int>(),
                columns ?? Array.Empty<int>(),
                lengths ?? Array.Empty<int>(),
                severities ?? Array.Empty<EditorDiagnosticSeverity>(),
                messages ?? Array.Empty<string>());

        /// <summary>Hover requested → UniversalHoverRequestedEventArgs.</summary>
        public static UniversalHoverRequestedEventArgs ConvertHover(int offset, int line, int column)
            => new UniversalHoverRequestedEventArgs(offset, line, column);
    }
}

