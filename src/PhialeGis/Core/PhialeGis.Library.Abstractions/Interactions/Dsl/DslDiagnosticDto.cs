using System;

namespace PhialeGis.Library.Abstractions.Interactions.Dsl
{
    // Single validation diagnostic (WinRT-safe DTO).
    public sealed class DslDiagnosticDto
    {
        public int Line { get; set; }
        public int Column { get; set; }
        public int Length { get; set; } = 1;
        public string Message { get; set; } = string.Empty;
        public string Severity { get; set; } = "info"; // info|warning|error
    }
}
