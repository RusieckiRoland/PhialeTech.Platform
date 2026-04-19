using System;
using PhialeGis.Library.Abstractions.Actions;
using PhialeGis.Library.Abstractions.Interactions;
using PhialeGis.Library.Abstractions.Interactions.Dsl;

namespace PhialeGis.Library.DslEditor.Contracts
{
    public sealed class EditorPromptChangedEventArgs : EventArgs
    {
        public EditorPromptChangedEventArgs(IEditorInteractive editor, DslPromptDto prompt)
        {
            Editor = editor;
            Prompt = prompt;
        }

        public IEditorInteractive Editor { get; }
        public DslPromptDto Prompt { get; }
    }
}
