using System.Collections.Generic;
using PhialeGis.Library.Geometry.Abstractions;
using PhialeGis.Library.Geometry.Spatial.Primitives;
using PhialeGis.Library.Geometry.Spatial.Single;

namespace PhialeGis.Library.Geometry.Spatial.Multi
{
    public sealed class PhMultiLineString : IPhGeometry
    {
        public PhMultiLineString(IList<PhPolyLine> lines)
        {
            Lines = lines ?? new List<PhPolyLine>(0);
            _env = PhEnvelope.Empty; for (int i = 0; i < Lines.Count; i++) _env.Include(Lines[i].Envelope);
        }

        public IList<PhPolyLine> Lines { get; private set; }
        private PhEnvelope _env;

        public PhGeometryKind Kind => PhGeometryKind.MultiLineString;
        public PhEnvelope Envelope => _env;

        public void Transform(in PhMatrix2D m)
        {
            for (int i = 0; i < Lines.Count; i++) Lines[i].Transform(m);
            _env = PhEnvelope.Empty; for (int i = 0; i < Lines.Count; i++) _env.Include(Lines[i].Envelope);
        }

        public IPhGeometry Bake(in PhMatrix2D m)
        {
            var list = new List<PhPolyLine>(Lines.Count);
            for (int i = 0; i < Lines.Count; i++) list.Add((PhPolyLine)Lines[i].Bake(m));
            return new PhMultiLineString(list);
        }
    }
}
