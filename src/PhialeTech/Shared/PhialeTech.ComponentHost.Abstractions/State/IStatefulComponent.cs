using System;

namespace PhialeTech.ComponentHost.Abstractions.State
{
    public interface IStatefulComponent<TState>
    {
        event EventHandler StateChanged;

        TState ExportState();

        void ApplyState(TState state);
    }
}
