namespace PhialeGis.Library.Abstractions.Actions
{
    /// <summary>
    /// Optional action capability to handle context-menu command ids.
    /// </summary>
    public interface IActionMenuCommandHandler
    {
        bool TryHandleMenuCommand(string commandId);
    }
}
