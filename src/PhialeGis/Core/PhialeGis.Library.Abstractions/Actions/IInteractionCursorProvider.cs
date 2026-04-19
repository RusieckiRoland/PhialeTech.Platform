namespace PhialeGis.Library.Abstractions.Actions
{
    /// <summary>
    /// Optional hook for actions to provide a dedicated cursor.
    /// </summary>
    public interface IInteractionCursorProvider
    {
        CursorSpec Cursor { get; }
    }
}
