using UniversalInput.Contracts;
using UniversalInput.Contracts.EventEnums;
using UniversalInput.Contracts.EditorEnums;
using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace PhialeGis.Library.AvaloniaUi.Converters
{
    public static class UniversalEventConverter
    {
        // ===========================
        // ====== POINTER INPUT ======
        // ===========================

        public static UniversalPointerRoutedEventArgs Convert(PointerPressedEventArgs e, Control element)
        {
            var pt = e.GetPosition(element);
            var pp = e.GetCurrentPoint(element).Properties;

            var universalPoint = ToUniversalPoint(pt);
            var pointerProperties = new UniversalPointerPointProperties
            {
                IsLeftButtonPressed = pp.IsLeftButtonPressed,
                IsRightButtonPressed = pp.IsRightButtonPressed,
            };

            var universalPointer = new UniversalPointer(ToDeviceType(e.Pointer.Type), universalPoint)
            {
                Properties = pointerProperties,
                PointerId = (uint)e.Pointer.Id,
            };

            return new UniversalPointerRoutedEventArgs(universalPointer)
            {
                Handled = e.Handled
            };
        }

        /// <summary>Pointer move/enter etc. (bez stanów przycisków poza tym, co wystawia Avalonia).</summary>
        public static UniversalPointerRoutedEventArgs Convert(PointerEventArgs e, Control element)
        {
            var pt = e.GetPosition(element);
            var pp = e.GetCurrentPoint(element).Properties;

            var universalPoint = ToUniversalPoint(pt);
            var pointerProperties = new UniversalPointerPointProperties
            {
                IsLeftButtonPressed = pp.IsLeftButtonPressed,
                IsRightButtonPressed = pp.IsRightButtonPressed,
            };

            var universalPointer = new UniversalPointer(ToDeviceType(e.Pointer.Type), universalPoint)
            {
                Properties = pointerProperties,
                PointerId = (uint)e.Pointer.Id,
            };

            return new UniversalPointerRoutedEventArgs(universalPointer)
            {
                Handled = e.Handled
            };
        }

        public static UniversalPointerRoutedEventArgs Convert(PointerReleasedEventArgs e, Control element)
        {
            var pt = e.GetPosition(element);
            var pp = e.GetCurrentPoint(element).Properties;

            var universalPoint = ToUniversalPoint(pt);
            var pointerProperties = new UniversalPointerPointProperties
            {
                IsLeftButtonPressed = pp.IsLeftButtonPressed,
                IsRightButtonPressed = pp.IsRightButtonPressed,
            };

            var universalPointer = new UniversalPointer(ToDeviceType(e.Pointer.Type), universalPoint)
            {
                Properties = pointerProperties,
                PointerId = (uint)e.Pointer.Id,
            };

            return new UniversalPointerRoutedEventArgs(universalPointer)
            {
                Handled = e.Handled
            };
        }

        // ===========================
        // ======== HELPERS ==========
        // ===========================

        private static DeviceType ToDeviceType(PointerType t) =>
            t switch
            {
                PointerType.Mouse => DeviceType.Mouse,
                PointerType.Touch => DeviceType.Touch,
                PointerType.Pen => DeviceType.Pen,
                _ => DeviceType.Mouse
            };

        private static UniversalPoint ToUniversalPoint(Point point)
            => new UniversalPoint { X = point.X, Y = point.Y };

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

