// PhialeGis.Library.Sync/Utils/LayerBoundingBox.cs
using System.Collections.Generic;
using PhialeGis.Library.Domain.Map;
using PhialeGis.Library.Geometry.Abstractions;
using PhialeGis.Library.Geometry.Utils;

namespace PhialeGis.Library.Sync.Utils
{
    /// <summary>
    /// Domain-side helpers to compute AABB using Geometry's PhBoundingBox.
    /// Keeps Geometry package free of Domain references.
    /// </summary>
    public static class LayerBoundingBox
    {
        /// <summary>Compute AABB for a whole layer (returns false if empty).</summary>
        public static bool TryCompute(PhLayer layer, out PhBBox bbox)
        {
            bbox = PhBBox.Empty();
            if (layer == null || layer.Features == null || layer.Features.Count == 0)
                return false;

            // Gather geometries and delegate to Geometry helper
            var geoms = new List<IPhGeometry>(layer.Features.Count);
            for (int i = 0; i < layer.Features.Count; i++)
            {
                var f = layer.Features[i];
                if (f?.Geometry != null) geoms.Add(f.Geometry);
            }

            return PhBoundingBox.TryCompute(geoms, out bbox);
        }

        /// <summary>Compute AABB for selected features of a layer.</summary>
        public static bool TryCompute(IEnumerable<IPhFeature> features, out PhBBox bbox)
        {
            bbox = PhBBox.Empty();
            if (features == null) return false;

            var geoms = new List<IPhGeometry>();
            foreach (var f in features)
            {
                if (f?.Geometry != null) geoms.Add(f.Geometry);
            }
            return PhBoundingBox.TryCompute(geoms, out bbox);
        }
    }
}
