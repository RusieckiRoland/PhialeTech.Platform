namespace PhialeGis.Library.Abstractions.Actions
{
    /// <summary>
    /// Payload for IInteractionAction.Suspended.
    /// </summary>
    public sealed class ActionSuspendPayload
    {
        /// <summary>Action identifier (if known).</summary>
        public System.Guid ActionId { get; set; }

        /// <summary>Action name (if known).</summary>
        public string ActionName { get; set; }

        /// <summary>Target draw surface (if known).</summary>
        public object TargetDraw { get; set; }

        /// <summary>Optional reason (free text).</summary>
        public string Reason { get; set; }
    }
}
