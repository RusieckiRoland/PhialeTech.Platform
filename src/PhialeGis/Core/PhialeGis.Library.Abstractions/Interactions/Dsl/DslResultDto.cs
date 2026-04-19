using System;

namespace PhialeGis.Library.Abstractions.Interactions.Dsl
{
    // Result of an execution request (WinRT-safe DTO).
    public sealed class DslResultDto
    {
        public bool Success { get; set; }
        public string Output { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
    }
}
