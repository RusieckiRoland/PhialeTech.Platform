// PhialeGis.Library.WinUi/Controls/PhialeDslEditor.Events.cs
using System;
using PhialeGis.Library.Abstractions.Interactions;
using UniversalInput.Contracts;
using UniversalInput.Contracts.EditorEnums;

namespace PhialeGis.Library.WinUi.Controls
{
    public sealed partial class PhialeDslEditor
    {
        // Adapter exposes universal events to Core.
        internal sealed partial class EditorAdapter : IEditorInteractive
        {
            public event EventHandler<UniversalTextChangedEventArgs>? TextChangedUniversal;
            public event EventHandler<UniversalSelectionChangedEventArgs>? SelectionChangedUniversal;
            public event EventHandler<UniversalCaretMovedEventArgs>? CaretMovedUniversal;
            public event EventHandler<UniversalDirtyChangedEventArgs>? DirtyChangedUniversal;
            public event EventHandler<UniversalCommandEventArgs>? CommandUniversal;
            public event EventHandler<UniversalSaveRequestedEventArgs>? SaveRequestedUniversal;
            public event EventHandler<UniversalLanguageChangedEventArgs>? LanguageChangedUniversal;
            public event EventHandler<UniversalThemeChangedEventArgs>? ThemeChangedUniversal;
            public event EventHandler<UniversalFindRequestedEventArgs>? FindRequestedUniversal;
            public event EventHandler<UniversalReplaceRequestedEventArgs>? ReplaceRequestedUniversal;
            public event EventHandler<UniversalLinkClickedEventArgs>? LinkClickedUniversal;
            public event EventHandler<UniversalDiagnosticsUpdatedEventArgs>? DiagnosticsUpdatedUniversal;
            public event EventHandler<UniversalHoverRequestedEventArgs>? HoverRequestedUniversal;

            internal void RaiseTextChanged(string text)
                => TextChangedUniversal?.Invoke(_owner, new UniversalTextChangedEventArgs(text ?? string.Empty));

            internal void RaiseSelectionChanged(int start, int end, int caretLine, int caretColumn)
                => SelectionChangedUniversal?.Invoke(_owner, new UniversalSelectionChangedEventArgs(start, end, caretLine, caretColumn));

            internal void RaiseCaretMoved(int line, int column, int offset)
                => CaretMovedUniversal?.Invoke(_owner, new UniversalCaretMovedEventArgs(line, column, offset));

            internal void RaiseDirtyChanged(bool isDirty)
                => DirtyChangedUniversal?.Invoke(_owner, new UniversalDirtyChangedEventArgs(isDirty));

            internal void RaiseCommand(string id, bool ctrl, bool alt, bool shift)
                => CommandUniversal?.Invoke(_owner, new UniversalCommandEventArgs(id ?? string.Empty, ctrl, alt, shift));

            internal void RaiseSubmit(string rawLine)
                => CommandUniversal?.Invoke(_owner, new UniversalCommandEventArgs("enter", false, false, false));

            internal void RaiseSaveRequested(string reason)
                => SaveRequestedUniversal?.Invoke(_owner, new UniversalSaveRequestedEventArgs(reason ?? string.Empty));

            internal void RaiseLanguageChanged(string languageId)
                => LanguageChangedUniversal?.Invoke(_owner, new UniversalLanguageChangedEventArgs(languageId ?? "plaintext"));

            internal void RaiseThemeChanged(string themeId)
                => ThemeChangedUniversal?.Invoke(_owner, new UniversalThemeChangedEventArgs(themeId ?? "default"));

            internal void RaiseFindRequested(string query, bool matchCase, bool regex, bool wholeWord)
                => FindRequestedUniversal?.Invoke(_owner, new UniversalFindRequestedEventArgs(query ?? string.Empty, matchCase, regex, wholeWord));

            internal void RaiseReplaceRequested(string query, string replacement, bool matchCase, bool regex, bool wholeWord)
                => ReplaceRequestedUniversal?.Invoke(_owner, new UniversalReplaceRequestedEventArgs(query ?? string.Empty, replacement ?? string.Empty, matchCase, regex, wholeWord));

            internal void RaiseLinkClicked(string url)
                => LinkClickedUniversal?.Invoke(_owner, new UniversalLinkClickedEventArgs(url ?? string.Empty));

            internal void RaiseDiagnosticsUpdated(
                string documentId, int[] lines, int[] columns, int[] lengths,
                EditorDiagnosticSeverity[] severities, string[] messages)
                => DiagnosticsUpdatedUniversal?.Invoke(_owner,
                    new UniversalDiagnosticsUpdatedEventArgs(
                        documentId ?? string.Empty,
                        lines ?? Array.Empty<int>(),
                        columns ?? Array.Empty<int>(),
                        lengths ?? Array.Empty<int>(),
                        severities ?? Array.Empty<EditorDiagnosticSeverity>(),
                        messages ?? Array.Empty<string>()));

            internal void RaiseHoverRequested(int offset, int line, int column)
                => HoverRequestedUniversal?.Invoke(_owner, new UniversalHoverRequestedEventArgs(offset, line, column));

            public void SetText(string text)
                => RunOnUI(() =>
                {
                    var v = text ?? string.Empty;
                    if (!string.Equals(_owner.Text, v, StringComparison.Ordinal))
                        _owner.SetValue(PhialeDslEditor.TextProperty, v);
                    _owner.JsEval($"window.__ph_setText({ToJsString(v)})");
                });

            public void SetLanguageId(string id)
                => RunOnUI(() =>
                {
                    var v = id ?? "plaintext";
                    if (!string.Equals(_owner.LanguageId, v, StringComparison.Ordinal))
                        _owner.SetValue(PhialeDslEditor.LanguageIdProperty, v);
                    _owner.JsEval($"if(window.__ph_setLanguage) window.__ph_setLanguage({ToJsString(v)});");
                });

            public void SetReadOnly(bool isReadOnly)
                => RunOnUI(() =>
                {
                    if (_owner.IsReadOnly != isReadOnly)
                        _owner.SetValue(PhialeDslEditor.IsReadOnlyProperty, isReadOnly);
                    _owner.JsEval($"window.__ph_setReadOnly({(isReadOnly ? "true" : "false")})");
                });
        }

        // Forwarders if you need them elsewhere in the control
        internal void OnNativeTextChanged(string text) => Adapter?.RaiseTextChanged(text);
        internal void OnNativeSelectionChanged(int start, int end, int caretLine, int col) => Adapter?.RaiseSelectionChanged(start, end, caretLine, col);
        internal void OnNativeCaretMoved(int line, int column, int offset) => Adapter?.RaiseCaretMoved(line, column, offset);
        internal void OnNativeDirtyChanged(bool isDirty) => Adapter?.RaiseDirtyChanged(isDirty);
        internal void OnNativeCommand(string commandId, bool ctrl, bool alt, bool shift) => Adapter?.RaiseCommand(commandId, ctrl, alt, shift);
        internal void OnNativeSaveRequested(string reason) => Adapter?.RaiseSaveRequested(reason);
        internal void OnNativeLanguageChanged(string languageId) => Adapter?.RaiseLanguageChanged(languageId);
        internal void OnNativeThemeChanged(string themeId) => Adapter?.RaiseThemeChanged(themeId);
        internal void OnNativeFindRequested(string q, bool mc, bool rx, bool ww) => Adapter?.RaiseFindRequested(q, mc, rx, ww);
        internal void OnNativeReplaceRequested(string q, string r, bool mc, bool rx, bool ww) => Adapter?.RaiseReplaceRequested(q, r, mc, rx, ww);
        internal void OnNativeLinkClicked(string url) => Adapter?.RaiseLinkClicked(url);
        internal void OnNativeDiagnosticsUpdated(string doc, int[] lines, int[] cols, int[] lens,
            EditorDiagnosticSeverity[] sev, string[] msgs)
            => Adapter?.RaiseDiagnosticsUpdated(doc, lines, cols, lens, sev, msgs);
        internal void OnNativeHoverRequested(int offset, int line, int column) => Adapter?.RaiseHoverRequested(offset, line, column);
    }
}

