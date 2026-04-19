namespace PhialeGis.Library.Abstractions.Interactions.Dsl
{
    /// <summary>
    /// Semantic Tokens legend (LSP/Monaco). Order MUST be stable.
    /// WinRT-safe: only string[].
    /// </summary>
    public sealed class DslSemanticLegendDto
    {
        public string[] TokenTypes { get; set; } = System.Array.Empty<string>();
        public string[] TokenModifiers { get; set; } = System.Array.Empty<string>();
    }
}
