using PhialeGis.Library.Core.Graphics;
using PhialeGis.Library.Core.Models.Geometry;
using PhialeGis.Library.Core.Models.RenderSpace;
using System;
using System.Runtime.CompilerServices;

namespace PhialeGis.Library.Core.GeometryExtensions
{
    /// <summary>
    /// Represents a point in the model's coordinate system, with context to a ViewportManager.
    /// This struct provides methods to convert points between model and SkiaSharp coordinates.
    /// </summary>
    internal struct ModelPoint
    {
        private readonly ViewportManager _viewPort;
        private readonly PhPoint _point;

        /// <summary>
        /// Initializes a new instance of the ModelPoint struct with a specified ViewportManager.
        /// </summary>
        /// <param name="viewportManager">The ViewportManager to be used for coordinate transformations.</param>
        /// <exception cref="ArgumentNullException">Thrown when viewportManager is null.</exception>
        public ModelPoint(ViewportManager viewportManager) : this(viewportManager, new PhPoint())
        {
        }

        // Private constructor used for initializing the ModelPoint.
        private ModelPoint(ViewportManager viewportManager, PhPoint point)
        {
            _viewPort = viewportManager ?? throw new ArgumentNullException(nameof(viewportManager), "ViewportManager cannot be null.");
            _point = point;
        }

        /// <summary>
        /// Creates a ModelPoint from an existing PhPoint. This method is aggressively inlined for performance.
        /// </summary>
        /// <param name="phPoint">The PhPoint to base this ModelPoint on.</param>
        /// <returns>A new ModelPoint based on the specified PhPoint.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ModelPoint FromPhPoint(PhPoint phPoint)
        {
            return new ModelPoint(_viewPort, phPoint);
        }

        /// <summary>
        /// Creates a ModelPoint from specified coordinates. This method is aggressively inlined for performance.
        /// </summary>
        /// <param name="X">The X coordinate.</param>
        /// <param name="Y">The Y coordinate.</param>
        /// <returns>A new ModelPoint based on the specified coordinates.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ModelPoint FromCoords(double X, double Y)
        {
            var phPoint = new PhPoint(X, Y);
            return new ModelPoint(_viewPort, phPoint);
        }

        /// <summary>
        /// Converts this model-space point to its corresponding canvas-space point
        /// using the associated <see cref="ViewportManager"/>.
        /// </summary>
        /// <returns>
        /// A <see cref="CanvasPoint"/> representing this point in device (canvas) coordinates.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CanvasPoint ToCanvasPoint()
        {
            return _viewPort.PhPointToCanvasPoint(_point);
        }
    }
}