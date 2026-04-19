using System;
using System.Collections.Generic;
using PhialeGis.Library.Abstractions.Ui.Rendering;          // IViewport
using PhialeGis.Library.Core.Models.Geometry;               // PhMatrix
using SkiaSharp;

// aliasy, żeby jasno rozróżnić typy punktów
using CorePoint = PhialeGis.Library.Core.Models.Geometry.PhPoint;
using SpatialPoint = PhialeGis.Library.Geometry.Spatial.Primitives.PhPoint;

namespace PhialeGis.Library.Renderer.Skia.ViewportProjections
{
    /// <summary>
    /// CPU-side projector:
    /// PrepareMatrix() buduje własną PhMatrix (model→screen, Y-up → Y-down),
    /// ProjectPoints(...) przelicza punkty na SKPoint[] (hurtowo),
    /// ReleaseMatrix() zeruje stan.
    /// </summary>
    public sealed class SkiaViewportProjector
    {
        private readonly IViewport _viewport;
        private PhMatrix _M;
        private bool _prepared;

        public SkiaViewportProjector(IViewport viewport)
        {
            if (viewport == null) throw new ArgumentNullException("viewport");
            _viewport = viewport;
        }

        public void PrepareMatrix()
        {
            double m11, m12, m21, m22, tx, ty;
            _viewport.GetModelToScreenAffine(out m11, out m12, out m21, out m22, out tx, out ty);

            _M = new PhMatrix(new double[3, 3]
            {
                { m11, m12, tx },
                { m21, m22, ty },
                {  0 ,  0 ,  1 }
            });

            _prepared = true;
        }

        public void ReleaseMatrix()
        {
            _prepared = false;
            _M = new PhMatrix(new double[3, 3]
            {
                { 1, 0, 0 },
                { 0, 1, 0 },
                { 0, 0, 1 }
            });
        }

        // ---------- OVERLOAD 1: CorePoint ----------
        public SKPoint[] ProjectPoints(IList<CorePoint> src)
        {
            if (!_prepared) throw new InvalidOperationException("Matrix not prepared. Call PrepareMatrix() first.");
            if (src == null || src.Count == 0) return new SKPoint[0];

            var dst = new SKPoint[src.Count];

            // odczyt współczynników JEDNORAZOWO
            double m11 = _M[0, 0], m12 = _M[0, 1], tx = _M[0, 2];
            double m21 = _M[1, 0], m22 = _M[1, 1], ty = _M[1, 2];

            for (int i = 0; i < src.Count; i++)
            {
                var p = src[i];
                dst[i].X = (float)(m11 * p.X + m12 * p.Y + tx);
                dst[i].Y = (float)(m21 * p.X + m22 * p.Y + ty);
            }
            return dst;
        }

        // ---------- OVERLOAD 2: SpatialPoint ----------
        public SKPoint[] ProjectPoints(IList<SpatialPoint> src)
        {
            if (!_prepared) throw new InvalidOperationException("Matrix not prepared. Call PrepareMatrix() first.");
            if (src == null || src.Count == 0) return new SKPoint[0];

            var dst = new SKPoint[src.Count];

            // odczyt współczynników JEDNORAZOWO
            double m11 = _M[0, 0], m12 = _M[0, 1], tx = _M[0, 2];
            double m21 = _M[1, 0], m22 = _M[1, 1], ty = _M[1, 2];

            for (int i = 0; i < src.Count; i++)
            {
                var p = src[i];
                dst[i].X = (float)(m11 * p.X + m12 * p.Y + tx);
                dst[i].Y = (float)(m21 * p.X + m22 * p.Y + ty);
            }
            return dst;
        }
    }
}
