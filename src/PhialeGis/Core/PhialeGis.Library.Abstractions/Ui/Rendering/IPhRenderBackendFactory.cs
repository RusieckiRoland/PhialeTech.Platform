namespace PhialeGis.Library.Abstractions.Ui.Rendering
{
    public interface IPhRenderBackendFactory
    {
        /// <summary>
        /// Creates a render backend bound to a canvas and viewport.
        /// The viewport provides coordinate transformation from model to screen.
        /// </summary>
        IPhRenderDriver Create(object canvas, IViewport viewport);
    }

}
