namespace UniversalInput.Contracts.EditorEnums
{
    /// <summary>Severity of an editor diagnostic.</summary>
    public enum EditorDiagnosticSeverity // int32 (WinRT-safe)
    {
        Hint = 0,
        Info = 1,
        Warning = 2,
        Error = 3
    }
}
