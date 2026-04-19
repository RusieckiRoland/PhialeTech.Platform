using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using PhialeGis.Library.Abstractions.Styling;

namespace PhialeGis.Library.Core.Styling
{
    public sealed class StylePreviewCache
    {
        private readonly object _syncRoot = new object();
        private readonly Dictionary<string, StylePreviewImage> _cache = new Dictionary<string, StylePreviewImage>(StringComparer.Ordinal);

        public StylePreviewImage GetOrAdd(StylePreviewRequest request, Func<StylePreviewImage> factory)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            var key = BuildKey(request);

            lock (_syncRoot)
            {
                if (_cache.TryGetValue(key, out var cached))
                    return cached;

                var created = factory();
                _cache[key] = created;
                return created;
            }
        }

        internal static string BuildKey(StylePreviewRequest request)
        {
            var builder = new StringBuilder();
            builder.Append((int)request.Kind).Append('|');
            builder.Append(request.WidthPx).Append('|');
            builder.Append(request.HeightPx).Append('|');
            builder.Append(request.BackgroundColorArgb.ToString("X8", CultureInfo.InvariantCulture)).Append('|');

            AppendLine(builder, request.LineType);
            AppendSymbol(builder, request.LineStampSymbol);
            AppendSymbol(builder, request.Symbol);
            AppendFill(builder, request.FillStyle);

            return builder.ToString();
        }

        private static void AppendLine(StringBuilder builder, LineTypeDefinition lineType)
        {
            if (lineType == null)
            {
                builder.Append("line:null|");
                return;
            }

            builder.Append(lineType.Id).Append('|');
            builder.Append((int)lineType.Kind).Append('|');
            builder.Append(lineType.ColorArgb.ToString("X8", CultureInfo.InvariantCulture)).Append('|');
            builder.Append(lineType.Width.ToString("R", CultureInfo.InvariantCulture)).Append('|');
            builder.Append(lineType.Flow ? '1' : '0').Append('|');
            builder.Append(lineType.Repeat.ToString("R", CultureInfo.InvariantCulture)).Append('|');
            builder.Append(lineType.DashOffset.ToString("R", CultureInfo.InvariantCulture)).Append('|');

            if (lineType.DashPattern != null)
            {
                for (int i = 0; i < lineType.DashPattern.Length; i++)
                    builder.Append(lineType.DashPattern[i].ToString("R", CultureInfo.InvariantCulture)).Append(',');
            }

            builder.Append('|');
        }

        private static void AppendSymbol(StringBuilder builder, SymbolDefinition symbol)
        {
            if (symbol == null)
            {
                builder.Append("symbol:null|");
                return;
            }

            builder.Append(symbol.Id).Append('|');
            builder.Append(symbol.AnchorX.ToString("R", CultureInfo.InvariantCulture)).Append('|');
            builder.Append(symbol.AnchorY.ToString("R", CultureInfo.InvariantCulture)).Append('|');
            builder.Append(symbol.DefaultSize.ToString("R", CultureInfo.InvariantCulture)).Append('|');
            builder.Append(symbol.Primitives.Count).Append('|');

            for (int i = 0; i < symbol.Primitives.Count; i++)
            {
                var primitive = symbol.Primitives[i];
                builder.Append((int)primitive.Kind).Append('|');
                builder.Append(primitive.StrokeColorArgb.ToString("X8", CultureInfo.InvariantCulture)).Append('|');
                builder.Append(primitive.FillColorArgb.ToString("X8", CultureInfo.InvariantCulture)).Append('|');
                builder.Append(primitive.StrokeWidth.ToString("R", CultureInfo.InvariantCulture)).Append('|');
                if (primitive.Coordinates != null)
                {
                    for (int c = 0; c < primitive.Coordinates.Length; c++)
                        builder.Append(primitive.Coordinates[c].ToString("R", CultureInfo.InvariantCulture)).Append(',');
                }

                builder.Append('|');
            }
        }

        private static void AppendFill(StringBuilder builder, FillStyleDefinition fillStyle)
        {
            if (fillStyle == null)
            {
                builder.Append("fill:null|");
                return;
            }

            builder.Append(fillStyle.Id).Append('|');
            builder.Append((int)fillStyle.Kind).Append('|');
            builder.Append(fillStyle.ForeColorArgb.ToString("X8", CultureInfo.InvariantCulture)).Append('|');
            builder.Append(fillStyle.BackColorArgb.ToString("X8", CultureInfo.InvariantCulture)).Append('|');
            builder.Append((int)fillStyle.GradientDirection).Append('|');
            builder.Append((int)fillStyle.FillDirection).Append('|');
            builder.Append(fillStyle.FillFactor.ToString("R", CultureInfo.InvariantCulture)).Append('|');
            builder.Append(fillStyle.TileWidth).Append('|');
            builder.Append(fillStyle.TileHeight).Append('|');
            builder.Append(fillStyle.HatchSpacing.ToString("R", CultureInfo.InvariantCulture)).Append('|');
            builder.Append(fillStyle.HatchThickness.ToString("R", CultureInfo.InvariantCulture)).Append('|');
            builder.Append(ComputeByteArrayHash(fillStyle.TileBytes)).Append('|');
        }

        private static int ComputeByteArrayHash(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
                return 0;

            unchecked
            {
                var hash = 17;
                for (int i = 0; i < bytes.Length; i++)
                    hash = (hash * 31) + bytes[i];

                return hash;
            }
        }
    }
}
