using System;

namespace PhialeGis.Library.Geometry.Systems
{
    /// <summary>
    /// Optional screen-space drawing API for overlays (pixel coordinates).
    /// Implemented by backends that can draw directly in device pixels.
    /// </summary>
    public interface IPhScreenSpaceBackend
    {
        void DrawScreenLine(float x1, float y1, float x2, float y2, uint strokeArgb, float thicknessPx);
        void DrawScreenRect(float x, float y, float width, float height, uint strokeArgb, float thicknessPx);
    }
}
