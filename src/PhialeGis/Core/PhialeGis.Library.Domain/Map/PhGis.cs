using System.Collections.Generic;

namespace PhialeGis.Library.Domain.Map
{
    /// <summary>GIS model composed of layers.</summary>
    public sealed class PhGis
    {
        private readonly List<PhLayer> _layers = new List<PhLayer>();
        public IReadOnlyList<PhLayer> Layers { get { return _layers.AsReadOnly(); } }

        public void AddLayer(PhLayer layer)
        {
            if (layer != null) _layers.Add(layer);
        }
    }
}
