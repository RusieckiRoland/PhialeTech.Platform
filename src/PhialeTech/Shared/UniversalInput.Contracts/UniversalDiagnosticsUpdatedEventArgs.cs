using UniversalInput.Contracts.EditorEnums;

namespace UniversalInput.Contracts
{
    /// <summary>Batch update of diagnostics for the current document.</summary>
    public sealed class UniversalDiagnosticsUpdatedEventArgs : IUniversalBase
    {
        /// <summary>
        /// Optional document identity (path/uri) if edytor pracuje na wielu dokumentach.
        /// </summary>
        public string DocumentId { get; set; } = string.Empty;

        /// <summary>Zero-based line numbers.</summary>
        public int[] Lines { get; set; } = System.Array.Empty<int>();
        /// <summary>Zero-based columns.</summary>
        public int[] Columns { get; set; } = System.Array.Empty<int>();
        /// <summary>Lengths (e.g., token length). Optional: set 0 if unknown.</summary>
        public int[] Lengths { get; set; } = System.Array.Empty<int>();
        /// <summary>Severities aligned by index.</summary>
        public EditorDiagnosticSeverity[] Severities { get; set; } = System.Array.Empty<EditorDiagnosticSeverity>();
        /// <summary>Messages aligned by index.</summary>
        public string[] Messages { get; set; } = System.Array.Empty<string>();

        public DeviceType PointerDeviceType { get; private set; } = DeviceType.Other;
        public UniversalMetadata Metadata { get; set; }

        public UniversalDiagnosticsUpdatedEventArgs(
            string documentId,
            int[] lines,
            int[] columns,
            int[] lengths,
            EditorDiagnosticSeverity[] severities,
            string[] messages)
        {
            DocumentId = documentId ?? string.Empty;
            Lines = lines ?? System.Array.Empty<int>();
            Columns = columns ?? System.Array.Empty<int>();
            Lengths = lengths ?? System.Array.Empty<int>();
            Severities = severities ?? System.Array.Empty<EditorDiagnosticSeverity>();
            Messages = messages ?? System.Array.Empty<string>();
        }
    }
}
