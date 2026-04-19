using System;

namespace PhialeGis.Library.Abstractions.Interactions.Dsl
{
    // Completion list for a given caret position (WinRT-safe DTO).
    public sealed class DslCompletionListDto
    {
        public DslCompletionItemDto[] Items { get; set; } = new DslCompletionItemDto[0];
        public bool IsIncomplete { get; set; }
    }
}
