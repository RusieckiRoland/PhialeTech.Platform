using System;

namespace PhialeTech.ReportDesigner.Abstractions
{
    public sealed class ReportDesignerReadyStateChangedEventArgs : EventArgs
    {
        public ReportDesignerReadyStateChangedEventArgs(bool isInitialized, bool isReady)
        {
            IsInitialized = isInitialized;
            IsReady = isReady;
        }

        public bool IsInitialized { get; }

        public bool IsReady { get; }
    }
}
