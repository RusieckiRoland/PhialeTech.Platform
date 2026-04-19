namespace PhialeGis.Library.Abstractions.Interactions.Dsl
{
    /// <summary>
    /// LSP-encoded tokens: [deltaLine, deltaStart, length, tokenType, tokenModifiersBitset] * N.
    /// WinRT-safe: only int[] and string.
    /// </summary>
    public sealed class DslSemanticTokensDto
    {
        public int[] Data { get; set; } = System.Array.Empty<int>();
        public string ResultId { get; set; }  // optional (for incremental updates)
    }
}
