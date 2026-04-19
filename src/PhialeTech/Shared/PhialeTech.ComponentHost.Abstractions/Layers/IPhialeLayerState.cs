namespace PhialeTech.ComponentHost.Abstractions.Layers
{
    public interface IPhialeLayerState
    {
        string LayerId { get; }

        string Name { get; }

        string TreePath { get; }

        string LayerType { get; }

        string GeometryType { get; }

        bool IsActive { get; }

        bool IsVisible { get; }

        bool IsSelectable { get; }

        bool IsEditable { get; }

        bool IsSnappable { get; }

        bool CanBecomeActive { get; }

        bool CanToggleVisible { get; }

        bool CanToggleSelectable { get; }

        bool CanToggleEditable { get; }

        bool CanToggleSnappable { get; }
    }
}
