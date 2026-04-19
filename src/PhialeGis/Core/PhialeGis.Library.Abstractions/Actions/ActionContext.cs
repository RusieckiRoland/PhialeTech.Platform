using System;

namespace PhialeGis.Library.Abstractions.Actions
{
    /// <summary>
    /// Context passed to actions on start. Keep WinRT-safe.
    /// </summary>
    public sealed class ActionContext
    {
        public Guid ActionId { get; set; }
        public object TargetDraw { get; set; } // opaque handle to draw surface/viewport
        public string LanguageId { get; set; } = "en";
    }
}
