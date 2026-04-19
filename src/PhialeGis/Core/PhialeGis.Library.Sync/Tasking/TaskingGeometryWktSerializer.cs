using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using PhialeGis.Library.Geometry.Abstractions;
using PhialeGis.Library.Geometry.Spatial.Multi;
using PhialeGis.Library.Geometry.Spatial.Primitives;
using PhialeGis.Library.Geometry.Spatial.Single;

namespace PhialeGis.Library.Sync.Tasking
{
    internal static class TaskingGeometryWktSerializer
    {
        public static string Serialize(IPhGeometry geometry)
        {
            if (geometry == null)
            {
                return null;
            }

            switch (geometry.Kind)
            {
                case PhGeometryKind.Point:
                    return SerializePoint((PhPointEntity)geometry);
                case PhGeometryKind.LineString:
                    return SerializeLine((PhPolyLine)geometry);
                case PhGeometryKind.Polygon:
                    return SerializePolygon((PhPolygon)geometry);
                case PhGeometryKind.MultiPoint:
                    return SerializeMultiPoint((PhMultiPoint)geometry);
                case PhGeometryKind.MultiLineString:
                    return SerializeMultiLine((PhMultiLineString)geometry);
                case PhGeometryKind.MultiPolygon:
                    return SerializeMultiPolygon((PhMultiPolygon)geometry);
                default:
                    throw new NotSupportedException("Unsupported geometry kind for tasking serialization.");
            }
        }

        private static string SerializePoint(PhPointEntity point)
        {
            return "POINT (" + FormatPoint(point.Point) + ")";
        }

        private static string SerializeLine(PhPolyLine line)
        {
            return "LINESTRING (" + JoinPoints(line.Points) + ")";
        }

        private static string SerializePolygon(PhPolygon polygon)
        {
            var builder = new StringBuilder();
            builder.Append("POLYGON (");
            builder.Append('(').Append(JoinPoints(polygon.Outer)).Append(')');
            for (int i = 0; i < polygon.Holes.Count; i++)
            {
                builder.Append(",(").Append(JoinPoints(polygon.Holes[i])).Append(')');
            }

            builder.Append(')');
            return builder.ToString();
        }

        private static string SerializeMultiPoint(PhMultiPoint points)
        {
            return "MULTIPOINT (" + JoinPoints(points.Points) + ")";
        }

        private static string SerializeMultiLine(PhMultiLineString lines)
        {
            var builder = new StringBuilder();
            builder.Append("MULTILINESTRING (");
            for (int i = 0; i < lines.Lines.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(',');
                }

                builder.Append('(').Append(JoinPoints(lines.Lines[i].Points)).Append(')');
            }

            builder.Append(')');
            return builder.ToString();
        }

        private static string SerializeMultiPolygon(PhMultiPolygon polygons)
        {
            var builder = new StringBuilder();
            builder.Append("MULTIPOLYGON (");
            for (int i = 0; i < polygons.Polygons.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(',');
                }

                builder.Append('(');
                builder.Append('(').Append(JoinPoints(polygons.Polygons[i].Outer)).Append(')');
                for (int h = 0; h < polygons.Polygons[i].Holes.Count; h++)
                {
                    builder.Append(",(").Append(JoinPoints(polygons.Polygons[i].Holes[h])).Append(')');
                }

                builder.Append(')');
            }

            builder.Append(')');
            return builder.ToString();
        }

        private static string JoinPoints(IList<PhPoint> points)
        {
            var builder = new StringBuilder();
            for (int i = 0; i < points.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(',');
                }

                builder.Append(FormatPoint(points[i]));
            }

            return builder.ToString();
        }

        private static string FormatPoint(PhPoint point)
        {
            return FormatDouble(point.X) + " " + FormatDouble(point.Y);
        }

        private static string FormatDouble(double value)
        {
            return value.ToString("0.###############", CultureInfo.InvariantCulture);
        }
    }
}
