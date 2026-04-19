using System;
using System.Collections.Generic;
using PhialeGis.Library.Abstractions.Actions;
using PhialeGis.Library.Domain.Map;
using PhialeGis.Library.Geometry.Spatial.Primitives;
using PhialeGis.Library.Geometry.Spatial.Single;

namespace PhialeGis.Library.Sync.Orchestrators
{
    internal sealed class LineStringActionResultCommitter : IActionResultCommitter
    {
        private readonly PhGis _gis;

        public LineStringActionResultCommitter(PhGis gis)
        {
            _gis = gis ?? throw new ArgumentNullException(nameof(gis));
        }

        public void Commit(LineStringActionResult result)
        {
            if (result == null || result.Points == null || result.Points.Length < 4) return;

            var layer = ResolveLayer(result);
            if (layer == null) return;

            var pts = new List<PhPoint>(result.Points.Length / 2);
            for (int i = 0; i + 1 < result.Points.Length; i += 2)
                pts.Add(new PhPoint(result.Points[i], result.Points[i + 1]));

            if (pts.Count < 2) return;

            var geom = new PhPolyLine(pts);
            var nextId = NextFeatureId(layer);

            layer.AddFeature(new PhialeGis.Library.Geometry.Features.PhFeature(nextId, geom));
        }

        private PhLayer ResolveLayer(LineStringActionResult result)
        {
            var layers = _gis.Layers;
            if (layers == null || layers.Count == 0)
            {
                var name = !string.IsNullOrWhiteSpace(result.LayerId)
                    ? result.LayerId
                    : (!string.IsNullOrWhiteSpace(result.LayerHint) ? result.LayerHint : "Sketch");

                var created = new PhLayer(name, PhLayerType.Memory);
                _gis.AddLayer(created);
                return created;
            }

            if (!string.IsNullOrWhiteSpace(result.LayerId))
            {
                for (int i = 0; i < layers.Count; i++)
                {
                    if (string.Equals(layers[i].Name, result.LayerId, StringComparison.OrdinalIgnoreCase))
                        return layers[i];
                }

                var created = new PhLayer(result.LayerId, PhLayerType.Memory);
                _gis.AddLayer(created);
                return created;
            }

            if (!string.IsNullOrWhiteSpace(result.LayerHint))
            {
                for (int i = 0; i < layers.Count; i++)
                {
                    if (string.Equals(layers[i].Name, result.LayerHint, StringComparison.OrdinalIgnoreCase))
                        return layers[i];
                }
            }

            return layers[0];
        }

        private static long NextFeatureId(PhLayer layer)
        {
            long max = 0;
            var features = layer.Features;
            if (features == null) return 1;
            for (int i = 0; i < features.Count; i++)
                if (features[i] != null && features[i].Id > max) max = features[i].Id;

            return max + 1;
        }
    }
}
