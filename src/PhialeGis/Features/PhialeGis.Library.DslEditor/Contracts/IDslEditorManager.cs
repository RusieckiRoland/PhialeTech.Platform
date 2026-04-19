// PhialeGis.Library.DslEditor/Contracts/IDslEditorManager.cs
using PhialeGis.Library.Abstractions.Actions;
using PhialeGis.Library.Abstractions.Interactions;
using PhialeGis.Library.Abstractions.Interactions.Dsl;
using System;
using System.Threading.Tasks;


namespace PhialeGis.Library.DslEditor.Contracts
{
    /// <summary>Public surface for the DSL editor manager.</summary>
    public interface IDslEditorManager
    {
        // Editor registry
        void RegisterEditor(object editorObj);
        void UnregisterEditor(object editorObj);


        Task<DslResultDto> ExecuteAsync(string code, DslCommandEnvelope envelope);
        Task<DslValidationResultDto> ValidateAsync(string code, IEditorInteractive editor);
        Task<DslCompletionListDto> GetCompletionsAsync(string code, int caretOffset, IEditorInteractive editor);

        Task<DslSemanticLegendDto> GetSemanticLegendAsync();
        Task<DslSemanticTokensDto> GetSemanticTokensAsync(string code, IEditorInteractive editor);

        // Currently active editor, if any
        IEditorInteractive ActiveEditor { get; }
       
        event EventHandler<DslCommandEnvelope> CommandReceived;
        /// <summary>
        /// Raised when a prompt should be shown in the active editor.
        /// </summary>
        event EventHandler<DslPromptDto> PromptChanged;
        /// <summary>
        /// Raised when a prompt should be shown in a specific editor.
        /// </summary>
        event EventHandler<EditorPromptChangedEventArgs> PromptChangedForEditor;
        /// <summary>
        /// Push a prompt to the active editor (as tracked by the manager).
        /// </summary>
        void PushPromptForActiveEditor(DslPromptDto dto);
        /// <summary>
        /// Push a prompt to a specific editor.
        /// </summary>
        void PushPromptForEditor(IEditorInteractive editor, DslPromptDto dto);
    }
}
