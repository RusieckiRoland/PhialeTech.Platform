using System;
using PhialeGis.Library.Abstractions.Interactions;

namespace PhialeGis.Library.DslEditor.Contracts
{
    /// <summary>
    /// Lightweight envelope carrying a universal editor command and its target.
    /// Kept in Core; public surface uses only WinRT-safe types.
    /// </summary>
    public sealed class DslCommandEnvelope
    {
        /// <summary>Originating editor instance (interactive source).</summary>
        public IEditorInteractive Editor { get; private set; }

        /// <summary>
        /// Optional target viewport adapter or other engine-specific target.
        /// May be null for broadcast/ambient execution.
        /// </summary>
        public object Target { get; private set; }

        /// <summary>Command identifier (e.g., "enter").</summary>
        public string CommandId { get; private set; }

        /// <summary>Optional flag 1 (meaning defined by the engine/host).</summary>
        public bool Flag1 { get; private set; }

        /// <summary>Optional flag 2 (meaning defined by the engine/host).</summary>
        public bool Flag2 { get; private set; }

        /// <summary>
        /// Creates a new envelope.
        /// NOTE: The parameter list mirrors the usage sites in Core/UwpUi.
        /// </summary>
        public DslCommandEnvelope(
            IEditorInteractive editor,
            object target,
            string commandId,
            bool flag1,
            bool flag2)
        {
            if (editor == null) throw new ArgumentNullException("editor");
            Editor = editor;
            Target = target;
            CommandId = commandId ?? string.Empty;
            Flag1 = flag1;
            Flag2 = flag2;
        }
    }
}
