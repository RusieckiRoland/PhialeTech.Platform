using System;

namespace PhialeGis.Library.Abstractions.Ui.Rendering
{
    /// <summary>
    /// Minimal viewport contract for retained-mode rendering.
    /// Model space is Cartesian (y-up). Screen space is y-down.
    /// </summary>
    public interface IViewport
    {
        /// <summary>Current model-to-screen scalar factor (dimensionless).</summary>
        double Scale { get; }

        /// <summary>Dots-per-inch on X axis (screen).</summary>
        double GetDpiX();

        /// <summary>Dots-per-inch on Y axis (screen).</summary>
        double GetDpiY();

        /// <summary>
        /// Converts a model-space point (x,y) to screen-space (y-down).
        /// </summary>
        void ModelToScreen(double modelX, double modelY, out float screenX, out float screenY);

        /// <summary>
        /// Attempts to zoom the viewport by a relative scale factor around the viewport center.
        /// factor &gt; 1 zooms in; 0 &lt; factor &lt; 1 zooms out. Negative is invalid.
        /// </summary>
        bool Zoom(double factor);

        /// <summary>
        /// Pans the viewport by a screen-space offset (in pixels).
        /// Positive <paramref name="dx"/> moves the content to the right (viewport left),
        /// positive <paramref name="dy"/> moves the content down (viewport up), consistent with y-down screen space.
        /// Returns true if applied.
        /// </summary>
        bool PanByScreenOffset(double dx, double dy);

        /// <summary>
        /// Returns model→screen affine transform coefficients for current viewport:
        /// sx = m11*x + m12*y + tx
        /// sy = m21*x + m22*y + ty
        /// (Y-up model → Y-down device)
        /// </summary>
        void GetModelToScreenAffine(
            out double m11, out double m12,
            out double m21, out double m22,
            out double tx, out double ty);

        /// <summary>
        /// Optional: screen→model inverse coefficients.
        /// </summary>
        void GetScreenToModelAffine(
            out double m11, out double m12,
            out double m21, out double m22,
            out double tx, out double ty);
    }
}
