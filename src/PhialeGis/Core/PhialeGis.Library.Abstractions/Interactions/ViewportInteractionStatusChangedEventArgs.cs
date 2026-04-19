using System;

namespace PhialeGis.Library.Abstractions.Interactions
{
    public sealed class ViewportInteractionStatusChangedEventArgs : EventArgs
    {
        public object TargetDraw { get; set; }
    }
}
