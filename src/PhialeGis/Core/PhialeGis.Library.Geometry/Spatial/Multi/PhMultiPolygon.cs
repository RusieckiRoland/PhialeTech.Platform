using System.Collections.Generic;
using PhialeGis.Library.Geometry.Abstractions;
using PhialeGis.Library.Geometry.Spatial.Primitives;
using PhialeGis.Library.Geometry.Spatial.Single;

namespace PhialeGis.Library.Geometry.Spatial.Multi
{
    public sealed class PhMultiPolygon : IPhGeometry
    {
        public PhMultiPolygon(IList<PhPolygon> polygons)
        {
            Polygons = polygons ?? new List<PhPolygon>(0);
            _env = PhEnvelope.Empty; for (int i = 0; i < Polygons.Count; i++) _env.Include(Polygons[i].Envelope);
        }

        public IList<PhPolygon> Polygons { get; private set; }
        private PhEnvelope _env;

        public PhGeometryKind Kind => PhGeometryKind.MultiPolygon;
        public PhEnvelope Envelope => _env;

        public void Transform(in PhMatrix2D m)
        {
            for (int i = 0; i < Polygons.Count; i++) Polygons[i].Transform(m);
            _env = PhEnvelope.Empty; for (int i = 0; i < Polygons.Count; i++) _env.Include(Polygons[i].Envelope);
        }

        public IPhGeometry Bake(in PhMatrix2D m)
        {
            var list = new List<PhPolygon>(Polygons.Count);
            for (int i = 0; i < Polygons.Count; i++) list.Add((PhPolygon)Polygons[i].Bake(m));
            return new PhMultiPolygon(list);
        }
    }
}
