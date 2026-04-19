using System;

namespace PhialeTech.MonacoEditor.Abstractions
{
    public sealed class MonacoEditorReadyStateChangedEventArgs : EventArgs
    {
        public MonacoEditorReadyStateChangedEventArgs(bool isInitialized, bool isReady)
        {
            IsInitialized = isInitialized;
            IsReady = isReady;
        }

        public bool IsInitialized { get; }

        public bool IsReady { get; }
    }
}
