using System;
using System.Globalization;
using PhialeGis.Library.Abstractions.Styling;

namespace PhialeGis.Library.Core.Styling
{
    public static class StyleValidation
    {
        public static void ValidateSymbol(SymbolDefinition definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            ValidateIdentifier(definition.Id, nameof(definition.Id));

            if (definition.DefaultSize <= 0d)
                throw new ArgumentOutOfRangeException(nameof(definition.DefaultSize), "Symbol default size must be positive.");

            if (definition.Primitives == null || definition.Primitives.Count == 0)
                throw new ArgumentException("Symbol must define at least one primitive.", nameof(definition));

            for (int i = 0; i < definition.Primitives.Count; i++)
                ValidatePrimitive(definition.Primitives[i], i);
        }

        public static void ValidateLineType(LineTypeDefinition definition, ISymbolCatalog symbolCatalog = null)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            ValidateIdentifier(definition.Id, nameof(definition.Id));

            if (definition.Width < 0d)
                throw new ArgumentOutOfRangeException(nameof(definition.Width), "Line width cannot be negative.");

            if (definition.Kind == LineTypeKind.SimpleDash)
            {
                if (definition.DashPattern != null && definition.DashPattern.Length == 1)
                    throw new ArgumentException("Dash pattern must contain zero entries or at least two entries.", nameof(definition));
            }
            else if (definition.Kind == LineTypeKind.VectorStamp)
            {
                ValidateIdentifier(definition.SymbolId, nameof(definition.SymbolId));

                if (definition.StampSize <= 0d)
                    throw new ArgumentOutOfRangeException(nameof(definition.StampSize), "Vector stamp size must be positive.");

                if (definition.Gap <= 0d)
                    throw new ArgumentOutOfRangeException(nameof(definition.Gap), "Vector stamp gap must be positive.");

                if (symbolCatalog != null && !symbolCatalog.TryGet(definition.SymbolId, out _))
                {
                    throw new InvalidOperationException(
                        string.Format(CultureInfo.InvariantCulture, "Vector stamp line type references missing symbol '{0}'.", definition.SymbolId));
                }
            }
            else if (definition.Kind == LineTypeKind.RasterPattern)
            {
                ValidateRasterPattern(definition.RasterPattern);
            }
        }

        public static void ValidateFillStyle(FillStyleDefinition definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            ValidateIdentifier(definition.Id, nameof(definition.Id));

            if (definition.FillFactor < 0d || definition.FillFactor > 1d)
                throw new ArgumentOutOfRangeException(nameof(definition.FillFactor), "Fill factor must be in range 0..1.");

            if (definition.Kind == FillStyleKind.Hatch)
            {
                if (definition.HatchSpacing <= 0d)
                    throw new ArgumentOutOfRangeException(nameof(definition.HatchSpacing), "Hatch spacing must be positive.");

                if (definition.HatchThickness <= 0d)
                    throw new ArgumentOutOfRangeException(nameof(definition.HatchThickness), "Hatch thickness must be positive.");
            }
            else if (definition.Kind == FillStyleKind.PatternTile)
            {
                if (definition.TileWidth <= 0)
                    throw new ArgumentOutOfRangeException(nameof(definition.TileWidth), "Tile width must be positive.");

                if (definition.TileHeight <= 0)
                    throw new ArgumentOutOfRangeException(nameof(definition.TileHeight), "Tile height must be positive.");

                if (definition.TileBytes == null || definition.TileBytes.Length == 0)
                    throw new ArgumentException("Pattern tile fill must provide tile bytes.", nameof(definition));

                if (definition.TileBytes.Length > 0)
                {
                    var oneBytePerPixel = definition.TileWidth * definition.TileHeight;
                    var argb32 = oneBytePerPixel * 4;
                    if (definition.TileBytes.Length != oneBytePerPixel && definition.TileBytes.Length != argb32)
                    {
                        // Encoded images are allowed, so no exception here.
                    }
                }
            }
        }

        public static void ValidateRasterPattern(RasterLinePattern pattern)
        {
            if (pattern == null)
                throw new ArgumentNullException(nameof(pattern));

            if (pattern.Lanes == null || pattern.Lanes.Count == 0)
                throw new ArgumentException("Raster line pattern must contain at least one lane.", nameof(pattern));

            for (int i = 0; i < pattern.Lanes.Count; i++)
            {
                var lane = pattern.Lanes[i];
                if (lane == null)
                    throw new ArgumentException("Raster line pattern cannot contain null lanes.", nameof(pattern));

                if (lane.RunLengths == null || lane.RunLengths.Length == 0)
                    throw new ArgumentException("Raster line pattern lane must define at least one run length.", nameof(pattern));

                for (int j = 0; j < lane.RunLengths.Length; j++)
                {
                    if (lane.RunLengths[j] <= 0)
                    {
                        throw new ArgumentOutOfRangeException(
                            nameof(pattern),
                            "Raster line pattern run lengths must be positive.");
                    }
                }
            }
        }

        private static void ValidatePrimitive(StylePrimitive primitive, int index)
        {
            if (primitive == null)
                throw new ArgumentException("Symbol cannot contain null primitives.", nameof(primitive));

            if (primitive.StrokeWidth < 0d)
                throw new ArgumentOutOfRangeException(nameof(primitive.StrokeWidth), "Primitive stroke width cannot be negative.");

            if (primitive.Kind == SymbolPrimitiveKind.Circle)
            {
                if (primitive.Coordinates == null || primitive.Coordinates.Length != 3 || primitive.Coordinates[2] <= 0d)
                {
                    throw new ArgumentException(
                        string.Format(CultureInfo.InvariantCulture, "Circle primitive at index {0} must define centerX, centerY and positive radius.", index),
                        nameof(primitive));
                }

                return;
            }

            if (primitive.Coordinates == null || primitive.Coordinates.Length < 4 || (primitive.Coordinates.Length % 2) != 0)
            {
                throw new ArgumentException(
                    string.Format(CultureInfo.InvariantCulture, "Primitive at index {0} must define an even coordinate array.", index),
                    nameof(primitive));
            }

            if (primitive.Kind == SymbolPrimitiveKind.Polygon && primitive.Coordinates.Length < 6)
            {
                throw new ArgumentException(
                    string.Format(CultureInfo.InvariantCulture, "Polygon primitive at index {0} must define at least three points.", index),
                    nameof(primitive));
            }
        }

        private static void ValidateIdentifier(string id, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Style identifier cannot be empty.", parameterName);
        }
    }
}
