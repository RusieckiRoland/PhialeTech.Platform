// PhialeDslEditor.Properties.cs (Avalonia)
using Avalonia;
using PhialeGis.Library.Abstractions.Interactions;
using PhialeGis.Library.Abstractions.Interactions.Editors;
using PhialeGis.Library.Abstractions.Ui.Rendering;
using PhialeGis.Library.AvaloniaUi.Interactions.Bridges;
using PhialeGis.Library.Core.Interactions;
using System;

namespace PhialeGis.Library.AvaloniaUi.Controls
{
    public sealed partial class PhialeDslEditor
    {
        private GisInteractionManager? _core;

        public static readonly StyledProperty<object?> GisInteractionManagerProperty =
            AvaloniaProperty.Register<PhialeDslEditor, object?>(nameof(GisInteractionManager));

        public object? GisInteractionManager
        {
            get => GetValue(GisInteractionManagerProperty);
            set => SetValue(GisInteractionManagerProperty, value);
        }

        public static readonly StyledProperty<object?> TargetDrawProperty =
            AvaloniaProperty.Register<PhialeDslEditor, object?>(nameof(TargetDraw));

        public object? TargetDraw
        {
            get => GetValue(TargetDrawProperty);
            set => SetValue(TargetDrawProperty, value);
        }

        public static readonly StyledProperty<bool> IsScriptModeProperty =
            AvaloniaProperty.Register<PhialeDslEditor, bool>(nameof(IsScriptMode), false);

        public bool IsScriptMode
        {
            get => GetValue(IsScriptModeProperty);
            set => SetValue(IsScriptModeProperty, value);
        }

        public static readonly StyledProperty<string> TextProperty =
            AvaloniaProperty.Register<PhialeDslEditor, string>(nameof(Text), string.Empty);

        public string Text
        {
            get => GetValue(TextProperty);
            set => SetValue(TextProperty, value ?? string.Empty);
        }

        public static readonly StyledProperty<bool> IsReadOnlyProperty =
            AvaloniaProperty.Register<PhialeDslEditor, bool>(nameof(IsReadOnly), false);

        public bool IsReadOnly
        {
            get => GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }

        public static readonly StyledProperty<string> LanguageIdProperty =
            AvaloniaProperty.Register<PhialeDslEditor, string>(nameof(LanguageId), "plaintext");

        public string LanguageId
        {
            get => GetValue(LanguageIdProperty);
            set => SetValue(LanguageIdProperty, value ?? "plaintext");
        }

        static PhialeDslEditor()
        {
            GisInteractionManagerProperty.Changed.AddClassHandler<PhialeDslEditor>(OnGisInteractionManagerChanged);
            TargetDrawProperty.Changed.AddClassHandler<PhialeDslEditor>(OnTargetDrawChanged);
            IsScriptModeProperty.Changed.AddClassHandler<PhialeDslEditor>(OnIsScriptModeChanged);
            TextProperty.Changed.AddClassHandler<PhialeDslEditor>(OnTextChanged);
            IsReadOnlyProperty.Changed.AddClassHandler<PhialeDslEditor>(OnReadOnlyChanged);
            LanguageIdProperty.Changed.AddClassHandler<PhialeDslEditor>(OnLanguageIdChanged);
        }

        private static void OnGisInteractionManagerChanged(PhialeDslEditor editor, AvaloniaPropertyChangedEventArgs e)
        {
            try
            {
                editor._connector?.Dispose();
            }
            catch
            {
                // best effort
            }

            editor._connector = null;

            var oldMgr = e.OldValue as IGisInteractionManager;
            if (oldMgr != null)
                oldMgr.UnregisterControl(editor.Adapter);

            var newMgr = e.NewValue as IGisInteractionManager;
            editor._core = newMgr as GisInteractionManager;
            if (newMgr != null)
                newMgr.RegisterControl(editor.Adapter);

            var core = newMgr as GisInteractionManager;
            if (core != null)
            {
                editor._connector = new DslEditorConnector(
                    core.Editors,
                    (IEditorInteractive)editor.Adapter,
                    (IEditorTextSource)editor.Adapter,
                    250);

                try
                {
                    editor._connector.CompletionsAvailable -= editor.OnCompletionsAvailableFromCore;
                }
                catch
                {
                    // best effort
                }

                editor._connector.CompletionsAvailable += editor.OnCompletionsAvailableFromCore;

                editor.HookSemanticEvents();
                editor._connector.RequestSemanticRefreshNow();

                editor._connector.PromptAvailable += (_, dto) =>
                {
                    try
                    {
                        editor.SetFSMStateAsync(
                            dto.ModeText ?? string.Empty,
                            dto.ChipHtml ?? string.Empty,
                            string.IsNullOrWhiteSpace(dto.Kind) ? "idle" : dto.Kind);
                    }
                    catch
                    {
                        // best effort
                    }
                };
            }

            editor.InvalidateArrange();
        }

        private static void OnTargetDrawChanged(PhialeDslEditor editor, AvaloniaPropertyChangedEventArgs e)
        {
            var rc = ResolveDrawAdapter(e.NewValue);
            editor.Adapter.InternalSetTargetDraw(rc);
        }

        private static IRenderingComposition? ResolveDrawAdapter(object? value)
        {
            if (value is IRenderingComposition rc)
                return rc;

            if (value is PhialeDrawBoxAvalonia box && box.CompositionAdapter is IRenderingComposition rc2)
                return rc2;

            return null;
        }

        private static void OnIsScriptModeChanged(PhialeDslEditor editor, AvaloniaPropertyChangedEventArgs e)
        {
            var jsMode = (bool)e.NewValue! ? "script" : "command";
            editor.JsEval($"if(window.__ph_setMode) window.__ph_setMode('{jsMode}');");
            editor.JsEval("if(window.__ph_focus) window.__ph_focus();");
        }

        private static void OnTextChanged(PhialeDslEditor editor, AvaloniaPropertyChangedEventArgs e)
        {
            var oldText = e.OldValue as string ?? string.Empty;
            var newText = e.NewValue as string ?? string.Empty;

            if (string.Equals(oldText, newText, StringComparison.Ordinal))
                return;

            if (editor.IsApplyingTextFromJs)
                return;

            editor.JsEval($"window.__ph_setText({ToJsString(newText)})");
        }

        private static void OnReadOnlyChanged(PhialeDslEditor editor, AvaloniaPropertyChangedEventArgs e)
        {
            editor.JsEval($"window.__ph_setReadOnly({((bool)e.NewValue! ? "true" : "false")})");
        }

        private static void OnLanguageIdChanged(PhialeDslEditor editor, AvaloniaPropertyChangedEventArgs e)
        {
            // No-op for this host. Left for parity with other UI stacks.
        }
    }
}
