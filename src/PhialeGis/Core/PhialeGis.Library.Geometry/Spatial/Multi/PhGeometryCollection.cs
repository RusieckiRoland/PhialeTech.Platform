using System.Collections.Generic;
using PhialeGis.Library.Geometry.Abstractions;
using PhialeGis.Library.Geometry.Spatial.Primitives;

namespace PhialeGis.Library.Geometry.Spatial.Multi
{
    public sealed class PhGeometryCollection : IPhGeometry
    {
        public PhGeometryCollection(IList<IPhGeometry> geometries)
        {
            Geometries = geometries ?? new List<IPhGeometry>(0);
            _env = PhEnvelope.Empty; for (int i = 0; i < Geometries.Count; i++) _env.Include(Geometries[i].Envelope);
        }

        public IList<IPhGeometry> Geometries { get; private set; }
        private PhEnvelope _env;

        public PhGeometryKind Kind => PhGeometryKind.GeometryCollection;
        public PhEnvelope Envelope => _env;

        public void Transform(in PhMatrix2D m)
        {
            for (int i = 0; i < Geometries.Count; i++) Geometries[i].Transform(m);
            _env = PhEnvelope.Empty; for (int i = 0; i < Geometries.Count; i++) _env.Include(Geometries[i].Envelope);
        }

        public IPhGeometry Bake(in PhMatrix2D m)
        {
            var list = new List<IPhGeometry>(Geometries.Count);
            for (int i = 0; i < Geometries.Count; i++) list.Add(Geometries[i].Bake(m));
            return new PhGeometryCollection(list);
        }
    }
}
