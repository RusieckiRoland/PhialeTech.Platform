using System;
using System.Collections.Generic;
using System.Linq;
using PhialeGis.Library.Abstractions.Styling;
using SkiaSharp;

namespace PhialeGis.Library.Renderer.Skia.Styling
{
    public sealed class CompiledSymbol : IDisposable
    {
        private bool _disposed;

        public CompiledSymbol(
            string symbolId,
            int contentHash,
            double anchorX,
            double anchorY,
            double defaultSize,
            SKRect bounds,
            SKPicture picture,
            IReadOnlyList<CompiledSymbolPrimitive> primitives)
        {
            SymbolId = symbolId ?? string.Empty;
            ContentHash = contentHash;
            AnchorX = anchorX;
            AnchorY = anchorY;
            DefaultSize = defaultSize;
            Bounds = bounds;
            Picture = picture ?? throw new ArgumentNullException(nameof(picture));
            Primitives = primitives ?? Array.Empty<CompiledSymbolPrimitive>();
        }

        public string SymbolId { get; }

        public int ContentHash { get; }

        public double AnchorX { get; }

        public double AnchorY { get; }

        public double DefaultSize { get; }

        public SKRect Bounds { get; }

        public SKPicture Picture { get; }

        public IReadOnlyList<CompiledSymbolPrimitive> Primitives { get; }

        public IReadOnlyList<SKPath> Paths
        {
            get { return Primitives.Select(x => x.GeometryPath).ToArray(); }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            for (int i = 0; i < Primitives.Count; i++)
                Primitives[i].Dispose();

            Picture.Dispose();
        }
    }

    public sealed class CompiledSymbolPrimitive : IDisposable
    {
        private bool _disposed;

        public CompiledSymbolPrimitive(
            SymbolPrimitiveKind kind,
            SKPath geometryPath,
            int strokeColorArgb,
            int fillColorArgb,
            double strokeWidth)
        {
            Kind = kind;
            GeometryPath = geometryPath ?? throw new ArgumentNullException(nameof(geometryPath));
            StrokeColorArgb = strokeColorArgb;
            FillColorArgb = fillColorArgb;
            StrokeWidth = strokeWidth;
        }

        public SymbolPrimitiveKind Kind { get; }

        public SKPath GeometryPath { get; }

        public int StrokeColorArgb { get; }

        public int FillColorArgb { get; }

        public double StrokeWidth { get; }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            GeometryPath.Dispose();
        }
    }
}
