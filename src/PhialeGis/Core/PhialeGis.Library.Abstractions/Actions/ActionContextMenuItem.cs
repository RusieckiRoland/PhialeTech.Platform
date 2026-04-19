namespace PhialeGis.Library.Abstractions.Actions
{
    /// <summary>
    /// One menu entry emitted by an interactive action.
    /// </summary>
    public sealed class ActionContextMenuItem
    {
        public string CommandId { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public bool Enabled { get; set; } = true;
        public bool IsSeparator { get; set; }
    }
}
