using System;
using System.Collections.Generic;

namespace PhialeTech.ComponentHost.Abstractions.Layers
{
    public interface IPhialeLayerCollectionState<TLayer>
        where TLayer : IPhialeLayerState
    {
        IReadOnlyList<TLayer> Items { get; }

        string ActiveLayerId { get; }

        event EventHandler StateChanged;

        void SetActiveLayer(string layerId);

        void SetLayerVisible(string layerId, bool value);

        void SetLayerSelectable(string layerId, bool value);

        void SetLayerEditable(string layerId, bool value);

        void SetLayerSnappable(string layerId, bool value);
    }
}
