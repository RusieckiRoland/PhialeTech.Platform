using System;

namespace PhialeGrid.Core
{
    public sealed class GridOperationEventArgs<TPayload> : EventArgs
    {
        public GridOperationEventArgs(TPayload payload)
        {
            Payload = payload;
        }

        public TPayload Payload { get; }
    }
}
