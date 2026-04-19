// PhialeGis.Library.Core/Interactions/IEditorViewportLink.cs
namespace PhialeGis.Library.DslEditor.Interop
{
    /// <summary>
    /// Optional capability for editors: expose attached viewport adapter (or null).
    /// Returned value is the same object that is passed to IGisInteractionManager.RegisterControl.
    /// The core may cast it to IRenderingComposition when needed.
    /// </summary>
    public interface IEditorViewportLink
    {
        object GetAttachedViewportAdapterOrNull();
    }
}
