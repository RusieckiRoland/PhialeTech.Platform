using PhialeGis.Library.Core.Graphics;
using PhialeGis.Library.Core.Models.Geometry;
using PhialeGis.Library.Core.Models.RenderSpace;
using System;
using System.Runtime.CompilerServices;

namespace PhialeGis.Library.Core.GeometryExtensions
{
    /// <summary>
    /// Represents a rectangle in the model's coordinate system, with context to a ViewportManager.
    /// This class provides methods to convert rectangles between model and SkiaSharp coordinates.
    /// </summary>
    internal class ModelRect
    {
        private readonly ViewportManager _viewPort;
        private readonly PhRect _phRect;

        /// <summary>
        /// Initializes a new instance of the ModelRect class with a specified ViewportManager.
        /// </summary>
        /// <param name="viewportManager">The ViewportManager to be used for coordinate transformations.</param>
        /// <exception cref="ArgumentNullException">Thrown when viewportManager is null.</exception>
        public ModelRect(ViewportManager viewportManager) : this(viewportManager, new PhRect())
        {
        }

        // Private constructor used for initializing the ModelRect.
        private ModelRect(ViewportManager viewportManager, PhRect phRect)
        {
            _viewPort = viewportManager ?? throw new ArgumentNullException(nameof(viewportManager), "ViewportManager cannot be null.");
            _phRect = phRect;
        }

        /// <summary>
        /// Creates a ModelRect from an existing PhRect. This method is aggressively inlined for performance.
        /// </summary>
        /// <param name="phRect">The PhRect to base this ModelRect on.</param>
        /// <returns>A new ModelRect based on the specified PhRect.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ModelRect FromPhRect(PhRect phRect)
        {
            return new ModelRect(_viewPort, phRect);
        }

        /// <summary>
        /// Creates a ModelRect from specified PhPoint coordinates. This method is aggressively inlined for performance.
        /// </summary>
        /// <param name="Emin">The minimum point (lower-left) of the rectangle.</param>
        /// <param name="Emax">The maximum point (upper-right) of the rectangle.</param>
        /// <returns>A new ModelRect based on the specified points.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ModelRect FromPhPoints(PhPoint Emin, PhPoint Emax)
        {
            var phRect = new PhRect(Emin, Emax);
            return new ModelRect(_viewPort, phRect);
        }

        /// <summary>
        /// Converts this model-space rectangle to its corresponding canvas-space rectangle
        /// using the associated <see cref="ViewportManager"/>.
        /// </summary>
        /// <returns>
        /// A <see cref="CanvasRect"/> representing this rectangle in device (canvas) coordinates.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the associated <see cref="ViewportManager"/> is null.
        /// </exception>
        public CanvasRect ToCanvasRect()
        {
            if (_viewPort == null)
            {
                throw new InvalidOperationException(
                    "CanvasRect conversion requires a valid ViewportManager instance.");
            }

            return _viewPort.RectToCanvasRect(_phRect);
        }

    }
}