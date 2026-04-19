using System;
using PhialeGis.Library.Abstractions.Ui.Rendering;
using SkiaSharp;

namespace PhialeGis.Library.Renderer.Skia
{
    /// <summary>
    /// Factory that creates Skia-based render drivers.
    /// This class bridges Core rendering logic with SkiaSharp.
    /// </summary>
    public sealed class SkiaPhRenderBackendFactory : IPhRenderBackendFactory
    {
        /// <summary>
        /// Creates a Skia rendering driver bound to a given SKCanvas and viewport.
        /// </summary>
        /// <param name="canvas">Drawing surface (must be SKCanvas).</param>
        /// <param name="viewport">Viewport providing coordinate transformations.</param>
        /// <returns>New instance of SkiaPhRenderDriver.</returns>
        public IPhRenderDriver Create(object canvas, IViewport viewport)
        {
            if (canvas == null)
                throw new ArgumentNullException(nameof(canvas));

            if (viewport == null)
                throw new ArgumentNullException(nameof(viewport));

            // Ensure canvas is of correct type
            var skCanvas = canvas as SKCanvas;
            if (skCanvas == null)
                throw new ArgumentException("Canvas must be an SKCanvas instance.", nameof(canvas));

            // Create driver (the class that implements IPhRenderDriver)
            return new SkiaPhRenderBackend(skCanvas, viewport);
        }
    }
}
