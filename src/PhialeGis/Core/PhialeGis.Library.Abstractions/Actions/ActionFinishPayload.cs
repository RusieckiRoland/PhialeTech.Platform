namespace PhialeGis.Library.Abstractions.Actions
{
    /// <summary>
    /// Payload for IInteractionAction.Finished.
    /// </summary>
    public sealed class ActionFinishPayload
    {
        /// <summary>True if action completed successfully; false when canceled/failed.</summary>
        public bool Success { get; set; }

        /// <summary>Short user-facing message (status or error).</summary>
        public string Message { get; set; }

        /// <summary>
        /// Optional canonical DSL command (e.g., "ADD LINESTRING (...)").
        /// Null/empty when not applicable (e.g., cancel).
        /// </summary>
        public string CanonicalCommand { get; set; }

        /// <summary>Optional action result payload.</summary>
        public LineStringActionResult Result { get; set; }
    }
}
