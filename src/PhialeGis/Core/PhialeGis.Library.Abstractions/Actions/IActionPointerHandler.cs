namespace PhialeGis.Library.Abstractions.Actions
{
    /// <summary>
    /// Optional pointer handler for interactive actions (CAD + pointer).
    /// </summary>
    public interface IActionPointerHandler
    {
        bool TryHandlePointerDown(ActionPointerInput input);
        bool TryHandlePointerMove(ActionPointerInput input);
        bool TryHandlePointerUp(ActionPointerInput input);
    }
}
