using System;

namespace PhialeGis.Library.Abstractions.Interactions.Dsl
{
    // Single completion suggestion (WinRT-safe DTO).
    public sealed class DslCompletionItemDto
    {
        public string Label { get; set; } = string.Empty;
        public string InsertText { get; set; } = string.Empty;
        public string Kind { get; set; } = "text";
    }
}
