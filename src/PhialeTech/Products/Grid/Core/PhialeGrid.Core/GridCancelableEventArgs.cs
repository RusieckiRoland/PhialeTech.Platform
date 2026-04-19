using System;

namespace PhialeGrid.Core
{
    public sealed class GridCancelableEventArgs<TPayload> : EventArgs
    {
        public GridCancelableEventArgs(TPayload payload)
        {
            Payload = payload;
        }

        public TPayload Payload { get; }

        public bool Cancel { get; set; }

        public string CancelReason { get; set; }
    }
}
