using System;
using System.Collections.Generic;
using PhialeGis.Library.Abstractions.Styling;

namespace PhialeGis.Library.Core.Styling
{
    internal static class StyleDefinitionCloner
    {
        public static SymbolDefinition Clone(SymbolDefinition definition)
        {
            if (definition == null)
                return null;

            var primitives = new StylePrimitive[definition.Primitives != null ? definition.Primitives.Count : 0];
            for (int i = 0; i < primitives.Length; i++)
                primitives[i] = Clone(definition.Primitives[i]);

            return new SymbolDefinition
            {
                Id = definition.Id ?? string.Empty,
                Name = definition.Name ?? string.Empty,
                AnchorX = definition.AnchorX,
                AnchorY = definition.AnchorY,
                DefaultSize = definition.DefaultSize,
                Primitives = primitives
            };
        }

        public static LineTypeDefinition Clone(LineTypeDefinition definition)
        {
            if (definition == null)
                return null;

            return new LineTypeDefinition
            {
                Id = definition.Id ?? string.Empty,
                Name = definition.Name ?? string.Empty,
                Kind = definition.Kind,
                Flow = definition.Flow,
                Repeat = definition.Repeat,
                ColorArgb = definition.ColorArgb,
                Width = definition.Width,
                Linecap = definition.Linecap,
                Linejoin = definition.Linejoin,
                MiterLimit = definition.MiterLimit,
                DashPattern = definition.DashPattern != null ? (double[])definition.DashPattern.Clone() : Array.Empty<double>(),
                DashOffset = definition.DashOffset,
                RasterPattern = Clone(definition.RasterPattern),
                SymbolId = definition.SymbolId ?? string.Empty,
                StampSize = definition.StampSize,
                Gap = definition.Gap,
                InitialGap = definition.InitialGap,
                OrientToTangent = definition.OrientToTangent,
                Perpendicular = definition.Perpendicular
            };
        }

        public static FillStyleDefinition Clone(FillStyleDefinition definition)
        {
            if (definition == null)
                return null;

            return new FillStyleDefinition
            {
                Id = definition.Id ?? string.Empty,
                Name = definition.Name ?? string.Empty,
                Kind = definition.Kind,
                ForeColorArgb = definition.ForeColorArgb,
                BackColorArgb = definition.BackColorArgb,
                GradientDirection = definition.GradientDirection,
                FillDirection = definition.FillDirection,
                FillFactor = definition.FillFactor,
                TileWidth = definition.TileWidth,
                TileHeight = definition.TileHeight,
                TileBytes = definition.TileBytes != null ? (byte[])definition.TileBytes.Clone() : Array.Empty<byte>(),
                HatchSpacing = definition.HatchSpacing,
                HatchThickness = definition.HatchThickness
            };
        }

        public static StylePrimitive Clone(StylePrimitive primitive)
        {
            if (primitive == null)
                return null;

            return new StylePrimitive
            {
                Kind = primitive.Kind,
                Coordinates = primitive.Coordinates != null ? (double[])primitive.Coordinates.Clone() : Array.Empty<double>(),
                StrokeColorArgb = primitive.StrokeColorArgb,
                FillColorArgb = primitive.FillColorArgb,
                StrokeWidth = primitive.StrokeWidth
            };
        }

        public static RasterLinePattern Clone(RasterLinePattern pattern)
        {
            if (pattern == null)
                return null;

            var lanes = new RasterLinePatternLane[pattern.Lanes != null ? pattern.Lanes.Count : 0];
            for (int i = 0; i < lanes.Length; i++)
                lanes[i] = Clone(pattern.Lanes[i]);

            return new RasterLinePattern
            {
                Lanes = lanes
            };
        }

        public static RasterLinePatternLane Clone(RasterLinePatternLane lane)
        {
            if (lane == null)
                return null;

            return new RasterLinePatternLane
            {
                OffsetY = lane.OffsetY,
                RunLengths = lane.RunLengths != null ? (int[])lane.RunLengths.Clone() : Array.Empty<int>(),
                StartsWithDash = lane.StartsWithDash
            };
        }

        public static IReadOnlyList<SymbolDefinition> CloneMany(IReadOnlyCollection<SymbolDefinition> definitions)
        {
            var items = new List<SymbolDefinition>(definitions != null ? definitions.Count : 0);
            if (definitions != null)
            {
                foreach (var definition in definitions)
                    items.Add(Clone(definition));
            }

            return items;
        }

        public static IReadOnlyList<LineTypeDefinition> CloneMany(IReadOnlyCollection<LineTypeDefinition> definitions)
        {
            var items = new List<LineTypeDefinition>(definitions != null ? definitions.Count : 0);
            if (definitions != null)
            {
                foreach (var definition in definitions)
                    items.Add(Clone(definition));
            }

            return items;
        }

        public static IReadOnlyList<FillStyleDefinition> CloneMany(IReadOnlyCollection<FillStyleDefinition> definitions)
        {
            var items = new List<FillStyleDefinition>(definitions != null ? definitions.Count : 0);
            if (definitions != null)
            {
                foreach (var definition in definitions)
                    items.Add(Clone(definition));
            }

            return items;
        }
    }
}
