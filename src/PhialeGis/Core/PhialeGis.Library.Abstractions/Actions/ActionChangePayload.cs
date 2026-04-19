namespace PhialeGis.Library.Abstractions.Actions
{
    /// <summary>
    /// Payload for IInteractionAction.Changed.
    /// </summary>
    public sealed class ActionChangePayload
    {
        /// <summary>User-facing prompt for the command chip.</summary>
        public string Prompt { get; set; }

        /// <summary>Opaque handle indicating which draw surface should render preview.</summary>
        public object TargetDraw { get; set; }

        /// <summary>Flattened preview coordinates: [x1,y1,x2,y2,...]. Empty if not applicable.</summary>
        public double[] Preview { get; set; }
    }
}
