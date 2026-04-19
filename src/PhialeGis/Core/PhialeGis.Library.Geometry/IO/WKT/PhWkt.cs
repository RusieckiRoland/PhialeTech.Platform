using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using PhialeGis.Library.Geometry.Abstractions;
using PhialeGis.Library.Geometry.Spatial.Multi;
using PhialeGis.Library.Geometry.Spatial.Primitives;
using PhialeGis.Library.Geometry.Spatial.Single;

namespace PhialeGis.Library.Geometry.IO.Wkt
{
    public static class PhWkt
    {
        public static IPhGeometry Parse(string wkt)
        {
            if (wkt == null) throw new ArgumentNullException(nameof(wkt));
            wkt = wkt.Trim();
            if (wkt.StartsWith("POINT", StringComparison.OrdinalIgnoreCase)) return ParsePoint(wkt);
            if (wkt.StartsWith("LINESTRING", StringComparison.OrdinalIgnoreCase)) return ParseLine(wkt);
            if (wkt.StartsWith("POLYGON", StringComparison.OrdinalIgnoreCase)) return ParsePolygon(wkt);
            if (wkt.StartsWith("MULTIPOINT", StringComparison.OrdinalIgnoreCase)) return ParseMultiPoint(wkt);
            if (wkt.StartsWith("MULTILINESTRING", StringComparison.OrdinalIgnoreCase)) return ParseMultiLine(wkt);
            if (wkt.StartsWith("MULTIPOLYGON", StringComparison.OrdinalIgnoreCase)) return ParseMultiPolygon(wkt);
            throw new NotSupportedException("Unsupported WKT kind.");
        }

        static PhPoint Tok(string t)
        {
            var parts = t.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return new PhPoint(double.Parse(parts[0], CultureInfo.InvariantCulture),
                               double.Parse(parts[1], CultureInfo.InvariantCulture));
        }

        static IPhGeometry ParsePoint(string wkt)
        {
            int l = wkt.IndexOf('('), r = wkt.LastIndexOf(')');
            var c = wkt.Substring(l + 1, r - l - 1).Trim();
            return new PhPointEntity(Tok(c));
        }

        static IPhGeometry ParseLine(string wkt)
        {
            int l = wkt.IndexOf('('), r = wkt.LastIndexOf(')');
            var c = wkt.Substring(l + 1, r - l - 1);
            var toks = c.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            var pts = new List<PhPoint>(toks.Length);
            for (int i = 0; i < toks.Length; i++) pts.Add(Tok(toks[i]));
            return new PhPolyLine(pts);
        }

        static IPhGeometry ParsePolygon(string wkt)
        {
            int l = wkt.IndexOf('('), r = wkt.LastIndexOf(')');
            var content = wkt.Substring(l + 1, r - l - 1).Trim();
            var rings = SplitTopLevel(content);
            var outer = ParseRing(rings[0]);
            var holes = new List<IList<PhPoint>>();
            for (int i = 1; i < rings.Count; i++) holes.Add(ParseRing(rings[i]));
            PhPolygon.EnsureClosed(outer); PhPolygon.EnsureOrientation(outer, true);
            for (int i = 0; i < holes.Count; i++) { PhPolygon.EnsureClosed(holes[i]); PhPolygon.EnsureOrientation(holes[i], false); }
            return new PhPolygon(outer, holes);
        }

        static IPhGeometry ParseMultiPoint(string wkt)
        {
            int l = wkt.IndexOf('('), r = wkt.LastIndexOf(')');
            var c = wkt.Substring(l + 1, r - l - 1);
            var toks = c.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            var pts = new List<PhPoint>(toks.Length);
            for (int i = 0; i < toks.Length; i++)
            {
                var t = toks[i].Trim();
                if (t.StartsWith("(") && t.EndsWith(")")) t = t.Substring(1, t.Length - 2);
                pts.Add(Tok(t));
            }
            return new PhMultiPoint(pts);
        }

        static IPhGeometry ParseMultiLine(string wkt)
        {
            int l = wkt.IndexOf('('), r = wkt.LastIndexOf(')');
            var content = wkt.Substring(l + 1, r - l - 1);
            var parts = SplitTopLevel(content);
            var list = new List<PhPolyLine>(parts.Count);
            for (int i = 0; i < parts.Count; i++)
            {
                var ring = parts[i];
                var toks = ring.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                var pts = new List<PhPoint>(toks.Length);
                for (int j = 0; j < toks.Length; j++) pts.Add(Tok(toks[j]));
                list.Add(new PhPolyLine(pts));
            }
            return new PhMultiLineString(list);
        }

        static IPhGeometry ParseMultiPolygon(string wkt)
        {
            int l = wkt.IndexOf('('), r = wkt.LastIndexOf(')');
            var content = wkt.Substring(l + 1, r - l - 1);
            var polys = SplitTopLevel(content);
            var list = new List<PhPolygon>(polys.Count);
            for (int i = 0; i < polys.Count; i++)
            {
                var rings = SplitTopLevel(polys[i]);
                var outer = ParseRing(rings[0]);
                var holes = new List<IList<PhPoint>>();
                for (int h = 1; h < rings.Count; h++) holes.Add(ParseRing(rings[h]));
                PhPolygon.EnsureClosed(outer); PhPolygon.EnsureOrientation(outer, true);
                for (int h = 0; h < holes.Count; h++) { PhPolygon.EnsureClosed(holes[h]); PhPolygon.EnsureOrientation(holes[h], false); }
                list.Add(new PhPolygon(outer, holes));
            }
            return new PhMultiPolygon(list);
        }

        static List<string> SplitTopLevel(string s)
        {
            var list = new List<string>();
            var sb = new StringBuilder();
            int depth = 0;
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '(') { depth++; if (depth == 1) continue; }
                if (c == ')') { depth--; if (depth == 0) { list.Add(sb.ToString()); sb.Length = 0; continue; } }
                if (depth >= 1) sb.Append(c);
            }
            if (sb.Length > 0) list.Add(sb.ToString());
            return list;
        }

        static IList<PhPoint> ParseRing(string ring)
        {
            var toks = ring.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);
            var pts = new List<PhPoint>(toks.Length);
            for (int i = 0; i < toks.Length; i++) pts.Add(Tok(toks[i]));
            return pts;
        }
    }
}
