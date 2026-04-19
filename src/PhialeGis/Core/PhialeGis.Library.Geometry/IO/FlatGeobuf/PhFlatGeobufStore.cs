// PhialeGis.Library.Geometry.IO.FlatGeobuf/PhFlatGeobufStore.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PhialeGis.Library.Geometry.Abstractions;
using PhialeGis.Library.Geometry.Spatial.Single;
using PhialeGis.Library.Geometry.Spatial.Multi;

#if PH_FGB
using FlatGeobuf.NTS;                     // FeatureCollectionConversions
using NetTopologySuite.Features;          // IFeature, AttributesTable, FeatureCollection
using FgbGeometryType = FlatGeobuf.GeometryType; // <-- alias ONLY the enum to avoid Feature clash
#endif

namespace PhialeGis.Library.Geometry.IO.FlatGeobuf
{
    /// <summary>
    /// FlatGeobuf read/write bridge for PhialeGis features.
    /// Requires symbol PH_FGB and NuGets: FlatGeobuf + NetTopologySuite.
    /// </summary>
    public static class PhFlatGeobufStore
    {
#if PH_FGB
        /// <summary>
        /// Writes features to a FlatGeobuf file. Infers columns from attributes.
        /// </summary>
        public static void Write(string path, IEnumerable<IPhFeature> features)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path cannot be null or empty.", nameof(path));
            if (features == null)
                throw new ArgumentNullException(nameof(features));

            var list = features.ToList();
            if (list.Count == 0)
            {
                // Empty file with header (Unknown type)
                File.WriteAllBytes(path,
                    FeatureCollectionConversions.Serialize(new FeatureCollection(), FgbGeometryType.Unknown));
                return;
            }

            var gtype = DetectGeometryType(list);

            // Build NTS FeatureCollection (lets FGB infer columns from attributes).
            var fc = new FeatureCollection();
            for (int i = 0; i < list.Count; i++)
            {
                var f = list[i];
                var ntsGeom = PhNtsBridge.ToNts(f.Geometry);
                var attrs = new AttributesTable();
                foreach (var kv in f.Attributes)
                    attrs.Add(kv.Key, kv.Value);

                // NOTE: qualify to avoid clash with FlatGeobuf.Feature
                fc.Add(new NetTopologySuite.Features.Feature(ntsGeom, attrs));
            }

            var bytes = FeatureCollectionConversions.Serialize(fc, gtype, dimensions: 2);
            File.WriteAllBytes(path, bytes);
        }

        /// <summary>
        /// Reads features from a FlatGeobuf file and maps them to PhFeature.
        /// </summary>
        public static IEnumerable<IPhFeature> Read(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path cannot be null or empty.", nameof(path));
            if (!File.Exists(path))
                throw new FileNotFoundException("FlatGeobuf file not found.", path);

            using (var fs = File.OpenRead(path))
            {
                long nextId = 1;
                foreach (var ntsFeature in FeatureCollectionConversions.Deserialize(fs))
                {
                    var geom = PhNtsBridge.FromNts(ntsFeature.Geometry);
                    var ph = new Features.PhFeature(0, geom);

                    var names = ntsFeature.Attributes != null
                        ? ntsFeature.Attributes.GetNames()
                        : Array.Empty<string>();

                    for (int i = 0; i < names.Length; i++)
                    {
                        var name = names[i];
                        ph.Attributes[name] = ntsFeature.Attributes[name];
                    }

                    if (ph.Id == 0) ph.Id = nextId++;
                    yield return ph;
                }
            }
        }

        public static IEnumerable<IPhFeature> Read(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (!stream.CanRead) throw new ArgumentException("Stream must be readable.", nameof(stream));

            // nie zamykamy streama – właścicielem jest wywołujący
            foreach (var f in FeatureCollectionConversions.Deserialize(stream))
            {
                var ph = new Features.PhFeature(0, PhNtsBridge.FromNts(f.Geometry));
                if (f.Attributes != null)
                    foreach (var name in f.Attributes.GetNames())
                        ph.Attributes[name] = f.Attributes[name];
                yield return ph;
            }
        }

        /// <summary>
        /// Maps Ph geometry kinds across the set to a FlatGeobuf GeometryType.
        /// Returns GeometryCollection for mixed kinds.
        /// </summary>
        private static FgbGeometryType DetectGeometryType(IList<IPhFeature> features)
        {
            var kinds = new HashSet<PhGeometryKind>();
            for (int i = 0; i < features.Count; i++)
            {
                var g = features[i].Geometry;
                if (g != null) kinds.Add(g.Kind);
            }
            if (kinds.Count == 0) return FgbGeometryType.Unknown;
            if (kinds.Count > 1) return FgbGeometryType.GeometryCollection;

            PhGeometryKind kind = PhGeometryKind.Point;
            foreach (var k in kinds) { kind = k; break; }

            switch (kind)
            {
                case PhGeometryKind.Point: return FgbGeometryType.Point;
                case PhGeometryKind.LineString: return FgbGeometryType.LineString;
                case PhGeometryKind.Polygon: return FgbGeometryType.Polygon;
                case PhGeometryKind.MultiPoint: return FgbGeometryType.MultiPoint;
                case PhGeometryKind.MultiLineString: return FgbGeometryType.MultiLineString;
                case PhGeometryKind.MultiPolygon: return FgbGeometryType.MultiPolygon;
                case PhGeometryKind.GeometryCollection: return FgbGeometryType.GeometryCollection;
                default: return FgbGeometryType.Unknown;
            }
        }
#else
        public static void Write(string path, IEnumerable<IPhFeature> _)
            => throw new NotSupportedException("FlatGeobuf disabled. Add NuGet FlatGeobuf + NetTopologySuite and define PH_FGB.");

        public static IEnumerable<IPhFeature> Read(string path)
            => throw new NotSupportedException("FlatGeobuf disabled. Add NuGet FlatGeobuf + NetTopologySuite and define PH_FGB.");
#endif
    }
}
