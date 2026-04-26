using System;

namespace PhialeTech.DocumentEditor.Abstractions
{
    public sealed class DocumentEditorReadyStateChangedEventArgs : EventArgs
    {
        public DocumentEditorReadyStateChangedEventArgs(bool isInitialized, bool isReady)
        {
            IsInitialized = isInitialized;
            IsReady = isReady;
        }

        public bool IsInitialized { get; }

        public bool IsReady { get; }
    }
}
