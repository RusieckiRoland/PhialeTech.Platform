using PhialeGis.Library.Abstractions.Modes;

namespace PhialeGis.Library.Abstractions.Interactions.Dsl
{
    // Minimal, WinRT-safe DSL engine contract (synchronous).
    // Host apps may call it on a background thread if needed.
    public interface IDslEngine
    {
        DslResultDto Execute(string code ,object targetId, IEditorInteractive source, IDslContextProvider ctxProvider);
        DslValidationResultDto Validate(string code, IEditorInteractive source, IDslContextProvider ctxProvider);
        DslCompletionListDto GetCompletions(string code, int caretOffset, IEditorInteractive editor, IDslContextProvider ctxProvider);

        DslSemanticLegendDto GetSemanticLegend();
        DslSemanticTokensDto GetSemanticTokens(string code);
    }
}
