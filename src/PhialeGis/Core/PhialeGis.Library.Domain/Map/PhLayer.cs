// PhialeGis.Library.Domain/Map/PhLayer.cs
using System.Collections.Generic;
using PhialeGis.Library.Geometry.Abstractions;

namespace PhialeGis.Library.Domain.Map
{
    /// <summary>
    /// Logical map layer carrying Geometry features and basic rendering metadata.
    /// Stores IPhFeature/IPhGeometry directly to avoid interim DTOs.
    /// </summary>
    public sealed class PhLayer
    {
        public string Name { get; private set; }
        public PhLayerType Type { get; private set; }
        public bool Visible { get; set; }
        public double Opacity { get; set; }

        public bool Selectable { get; set; }
        public bool Snappable { get; set; }

        // In-memory feature container; for DB-backed layers this can be a cache.
        private readonly List<IPhFeature> _features = new List<IPhFeature>();
        public IList<IPhFeature> Features { get { return _features.AsReadOnly(); } }

        public PhLayer(string name, PhLayerType type)
        {
            Name = name;
            Type = type;
            Visible = true;
            Selectable = true;
            Snappable = true;
            Opacity = 1.0d;
        }

        /// <summary>Adds a fully-formed feature (geometry + attributes).</summary>
        public void AddFeature(IPhFeature feature)
        {
            if (feature != null) _features.Add(feature);
        }
    }
}
