// PhialeGis.Library.Abstractions/Interactions/IEditorTargetAware.cs
using System;

namespace PhialeGis.Library.Abstractions.Interactions
{
    /// <summary>
    /// Optional capability for editors that can expose their bound rendering target (drawbox).
    /// The target object is treated as an opaque handle and compared by reference.
    /// This avoids any platform-specific types in public contracts.
    /// </summary>
    public interface IEditorTargetAware
    {
        /// <summary>
        /// Returns the opaque target object (typically a composition adapter) this editor is bound to,
        /// or null when the editor is not bound to any drawbox (e.g., headless/script-only usage).
        /// </summary>
        object TargetDraw { get; }
    }
}
