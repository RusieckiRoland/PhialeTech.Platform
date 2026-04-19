using UniversalInput.Contracts;

namespace PhialeGis.Library.Abstractions.Actions
{
    /// <summary>
    /// Payload emitted by an action to request a context menu on a draw surface.
    /// </summary>
    public sealed class ActionContextMenuPayload
    {
        public object TargetDraw { get; set; }
        public UniversalPoint ScreenPosition { get; set; }
        public ActionContextMenuItem[] Items { get; set; } = new ActionContextMenuItem[0];
    }
}

