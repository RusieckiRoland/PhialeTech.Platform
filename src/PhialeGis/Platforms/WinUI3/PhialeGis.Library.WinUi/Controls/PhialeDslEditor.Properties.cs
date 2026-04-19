// PhialeGis.Library.WinUi/Controls/PhialeDslEditor.Properties.cs

using PhialeGis.Library.Abstractions.Interactions;
using PhialeGis.Library.Abstractions.Interactions.Editors;
using PhialeGis.Library.Abstractions.Ui.Rendering;
using PhialeGis.Library.Core.Interactions;
using PhialeGis.Library.DslEditor.Runtime;
using PhialeGis.Library.WinUi.Interactions.Bridges;
using System;
using Microsoft.UI.Xaml;

namespace PhialeGis.Library.WinUi.Controls
{
    public sealed partial class PhialeDslEditor
    {
        private GisInteractionManager? _core;

        /// <summary>
        /// Core manager provided via XAML binding (expected to implement IGisInteractionManager;
        /// if it is the concrete GisInteractionManager, the connector is created).
        /// </summary>
        public object GisInteractionManager
        {
            get => GetValue(GisInteractionManagerProperty);
            set => SetValue(GisInteractionManagerProperty, value);
        }

        public static DependencyProperty GisInteractionManagerProperty { get; } =
            DependencyProperty.Register(
                nameof(GisInteractionManager),
                typeof(object),
                typeof(PhialeDslEditor),
                new PropertyMetadata(default(object), OnGisInteractionManagerChanged));

        private static void OnGisInteractionManagerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var editor = (PhialeDslEditor)d;

            // Dispose previous connector and unregister editor
            try { editor._connector?.Dispose(); } catch { }
            editor._connector = null;

            var oldMgr = e.OldValue as IGisInteractionManager;
            if (oldMgr != null)
                oldMgr.UnregisterControl(editor.Adapter);

            // Register editor with new manager
            var newMgr = e.NewValue as IGisInteractionManager;
            editor._core = newMgr as GisInteractionManager;
            if (newMgr != null)
                newMgr.RegisterControl(editor.Adapter);

            // If we can access concrete core, create connector
            var core = newMgr as GisInteractionManager;
            if (core != null)
            {
                editor._connector = new DslEditorConnector(
                    core.Editors,                 // IMPORTANT: IDslEditorManager
                    (IEditorInteractive)editor.Adapter,
                    (IEditorTextSource)editor.Adapter,
                    250);

                // Completions UI hookup
                try { editor._connector.CompletionsAvailable -= editor.OnCompletionsAvailableFromCore; } catch { }
                editor._connector.CompletionsAvailable += editor.OnCompletionsAvailableFromCore;

                // Semantic hookup + initial push
                editor.HookSemanticEvents();
                editor._connector.RequestSemanticRefreshNow();
                editor._connector.PromptAvailable += (s, dto) =>
                {
                    try
                    {
                        editor.CachePrompt(dto.ModeText ?? string.Empty,
                                           dto.ChipHtml ?? string.Empty,
                                           dto.Kind ?? "idle");
                        // Forward to JS (WebView2).
                        editor.SetFSMStateAsync(dto.ModeText ?? string.Empty,
                                                dto.ChipHtml ?? string.Empty,
                                                string.IsNullOrWhiteSpace(dto.Kind) ? "idle" : dto.Kind);
                    }
                    catch
                    {
                        // Best-effort: keep UI alive if JS fails.
                    }
                };

                // Apply last known prompt if it arrived before WebView/connector was ready.
                if (core.Editors is DslInteractionManager dm &&
                    dm.TryGetLastPrompt((IEditorInteractive)editor.Adapter, out var last))
                {
                    editor.SetFSMStateAsync(last.ModeText ?? string.Empty,
                                            last.ChipHtml ?? string.Empty,
                                            string.IsNullOrWhiteSpace(last.Kind) ? "idle" : last.Kind);
                }
            }

            editor.InvalidateArrange();
        }

        /// <summary>
        /// Opaque render target (e.g., IRenderingComposition) provided via XAML binding.
        /// </summary>
        public object TargetDraw
        {
            get => GetValue(TargetDrawProperty);
            set => SetValue(TargetDrawProperty, value);
        }

        public static DependencyProperty TargetDrawProperty { get; } =
            DependencyProperty.Register(
                nameof(TargetDraw),
                typeof(object),
                typeof(PhialeDslEditor),
                new PropertyMetadata(default(object), OnTargetDrawChanged));

        private static void OnTargetDrawChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var editor = (PhialeDslEditor)d;
            var rc = ResolveDrawAdapter(e.NewValue);
            editor.Adapter.InternalSetTargetDraw(rc);
        }

        private static IRenderingComposition? ResolveDrawAdapter(object value)
        {
            if (value is IRenderingComposition rc) return rc;

            // WinUI drawbox wrapper exposing CompositionAdapter:
            if (value is PhialeDrawBoxWinUI box && box.CompositionAdapter is IRenderingComposition rc2)
                return rc2;

            return null;
        }

        /// <summary>Command vs script UX.</summary>
        public bool IsScriptMode
        {
            get => (bool)GetValue(IsScriptModeProperty);
            set => SetValue(IsScriptModeProperty, value);
        }

        public static DependencyProperty IsScriptModeProperty { get; } =
            DependencyProperty.Register(
                nameof(IsScriptMode),
                typeof(bool),
                typeof(PhialeDslEditor),
                new PropertyMetadata(false, OnIsScriptModeChanged));

        private static void OnIsScriptModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ed = (PhialeDslEditor)d;
            var jsMode = ((bool)e.NewValue) ? "script" : "command";
            ed.JsEval($"if(window.__ph_setMode) window.__ph_setMode('{jsMode}');");
            ed.JsEval("if(window.__ph_focus) window.__ph_focus();");
        }

        /// <summary>Plain text content.</summary>
        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value ?? string.Empty);
        }

        public static DependencyProperty TextProperty { get; } =
            DependencyProperty.Register(
                nameof(Text),
                typeof(string),
                typeof(PhialeDslEditor),
                new PropertyMetadata(string.Empty, OnTextDpChanged));

        private static void OnTextDpChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ed = (PhialeDslEditor)d;
            var newText = (string)e.NewValue ?? string.Empty;

            if (string.Equals(ed.Text, newText, StringComparison.Ordinal))
                return;

            ed.JsEval($"window.__ph_setText({ToJsString(newText)})");
            // Semantic refresh is handled by connector debounce when JS emits 'text'
        }

        /// <summary>Read-only toggle.</summary>
        public bool IsReadOnly
        {
            get => (bool)GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }

        public static DependencyProperty IsReadOnlyProperty { get; } =
            DependencyProperty.Register(
                nameof(IsReadOnly),
                typeof(bool),
                typeof(PhialeDslEditor),
                new PropertyMetadata(false, OnReadOnlyDpChanged));

        private static void OnReadOnlyDpChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ed = (PhialeDslEditor)d;
            ed.JsEval($"window.__ph_setReadOnly({(((bool)e.NewValue) ? "true" : "false")})");
        }

        /// <summary>Language id forwarding (reserved for richer JS host).</summary>
        public string LanguageId
        {
            get => (string)GetValue(LanguageIdProperty);
            set => SetValue(LanguageIdProperty, value ?? "plaintext");
        }

        public static DependencyProperty LanguageIdProperty { get; } =
            DependencyProperty.Register(
                nameof(LanguageId),
                typeof(string),
                typeof(PhialeDslEditor),
                new PropertyMetadata("plaintext", OnLanguageIdChanged));

        private static void OnLanguageIdChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // No-op for this minimal host.
        }
    }
}
