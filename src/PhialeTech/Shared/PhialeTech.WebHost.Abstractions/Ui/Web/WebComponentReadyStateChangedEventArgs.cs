using System;

namespace PhialeTech.WebHost.Abstractions.Ui.Web
{
    /// <summary>
    /// Describes lifecycle state changes of the reusable web host.
    /// </summary>
    public sealed class WebComponentReadyStateChangedEventArgs : EventArgs
    {
        public WebComponentReadyStateChangedEventArgs(bool isInitialized, bool isReady)
        {
            IsInitialized = isInitialized;
            IsReady = isReady;
        }

        public bool IsInitialized { get; }

        public bool IsReady { get; }
    }
}
