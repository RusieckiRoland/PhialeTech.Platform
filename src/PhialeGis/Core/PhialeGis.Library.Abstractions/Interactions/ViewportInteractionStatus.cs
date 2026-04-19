using System;
using PhialeGis.Library.Abstractions.Actions;

namespace PhialeGis.Library.Abstractions.Interactions
{
    public sealed class ViewportInteractionStatus
    {
        public bool HasActiveSession { get; set; }

        public bool IsInputViewport { get; set; }

        public bool CanTakeOver => HasActiveSession && !IsInputViewport;

        public string ActionName { get; set; } = string.Empty;

        public string PromptText { get; set; } = string.Empty;

        public string CoordinateText { get; set; } = string.Empty;

        public string SnapText { get; set; } = string.Empty;

        public ActionContextMenuItem[] Commands { get; set; } = Array.Empty<ActionContextMenuItem>();
    }
}
