using PhialeTech.ComponentHost.Abstractions.Layers;

namespace PhialeTech.ActiveLayerSelector
{
    public sealed class ActiveLayerSelectorItemState : IPhialeLayerState
    {
        public string LayerId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string TreePath { get; set; } = string.Empty;
        public string LayerType { get; set; } = string.Empty;
        public string GeometryType { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool IsVisible { get; set; }
        public bool IsSelectable { get; set; }
        public bool IsEditable { get; set; }
        public bool IsSnappable { get; set; }
        public bool CanBecomeActive { get; set; } = true;
        public bool CanToggleVisible { get; set; } = true;
        public bool CanToggleSelectable { get; set; } = true;
        public bool CanToggleEditable { get; set; } = true;
        public bool CanToggleSnappable { get; set; } = true;
    }
}
