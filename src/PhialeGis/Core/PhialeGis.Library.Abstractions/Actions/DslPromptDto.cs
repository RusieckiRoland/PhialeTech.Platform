using System;

namespace PhialeGis.Library.Abstractions.Actions
{
    /// <summary>
    /// Lightweight DTO for the prompt displayed above the DSL editor.
    /// Kind: "idle" | "draw" | "error".
    /// </summary>
    public sealed class DslPromptDto
    {
        public string ModeText { get; set; } = string.Empty;  // Left part, mode description.
        public string ChipHtml { get; set; } = string.Empty;  // Right part, HTML shown in the chip.
        public string Kind { get; set; } = "idle";            // Color scheme hint.
    }
}
