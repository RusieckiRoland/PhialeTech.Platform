// PhialeGis.Library.Sync/Orchestrators/SimplePhGisWorldSync.cs
using System.Collections.Generic;
using PhialeGis.Library.Abstractions.Ui.Rendering;
using PhialeGis.Library.Abstractions.Styling;
using PhialeGis.Library.Core.Components;
using PhialeGis.Library.Core.Scene;
using PhialeGis.Library.Domain.Map;
using PhialeGis.Library.Geometry.Ecs;
using PhialeGis.Library.Geometry.Spatial.Primitives;

namespace PhialeGis.Library.Sync.Orchestrators
{
    /// <summary>
    /// One-shot synchronizer: clears last batch and rebuilds entities from visible layers.
    /// Stores IPhGeometry directly in ECS components; no DTO conversions.
    /// </summary>
    public sealed class SimplePhGisWorldSync : IPhGisWorldSync
    {
        private readonly List<Entity> _lastBatch = new List<Entity>();

        public void Sync(PhGis gis, IWorld world, IViewport viewport)
        {
            if (gis == null || world == null) return;

            for (int i = 0; i < _lastBatch.Count; i++)
                world.Destroy(_lastBatch[i]);
            _lastBatch.Clear();

            var layers = gis.Layers;
            for (int li = 0; li < layers.Count; li++)
            {
                var layer = layers[li];
                if (layer == null || !layer.Visible) continue;

                var feats = layer.Features;
                for (int fi = 0; fi < feats.Count; fi++)
                {
                    var feature = feats[fi];
                    if (feature == null || feature.Geometry == null) continue;

                    var e = world.Create();

                    world.Add(e, new LayerTag { LayerId = li, ZIndex = li });
                    world.Add(e, new Visibility { IsVisible = true });

                    world.Add(e, new PhGeometryComponent
                    {
                        Geometry = feature.Geometry,
                        // If absent in your ECS, remove the next line:
                        LocalTransform = PhMatrix2D.Identity
                    });

                    world.Add(e, new PhStyleComponent
                    {
                        LineTypeId = BuiltInStyleIds.LineSolid,
                        FillStyleId = BuiltInStyleIds.FillSolidWhite,
                        SymbolId = BuiltInStyleIds.SymbolSquare
                    });

                    _lastBatch.Add(e);
                }
            }
        }
    }
}
