using PhialeGis.Library.Abstractions.Interactions;

namespace PhialeGis.Library.Abstractions.Modes
{
    /// Provides a per-editor DSL context snapshot.
    public interface IDslContextProvider
    {
        /// Returns the current DSL context for the given editor (Normal if unknown).
        DslContext GetFor(IEditorInteractive editor);

        /// Sets/replaces context for the given editor (Core-only usage).
        void SetFor(IEditorInteractive editor, DslContext ctx);

        /// Clears context for the given editor (back to Normal).
        void ClearFor(IEditorInteractive editor);
    }
}
