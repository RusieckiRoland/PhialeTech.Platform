using System;

namespace PhialeGis.Library.Abstractions.Interactions.Dsl
{
    // Result of a validation request (WinRT-safe DTO).
    public sealed class DslValidationResultDto
    {
        public bool IsValid { get; set; }
        public DslDiagnosticDto[] Diagnostics { get; set; } = new DslDiagnosticDto[0];
    }
}
