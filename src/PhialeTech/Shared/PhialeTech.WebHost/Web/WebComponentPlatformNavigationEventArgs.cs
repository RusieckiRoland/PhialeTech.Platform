using System;

namespace PhialeTech.WebHost
{
    /// <summary>
    /// Native navigation completion reported by a platform browser adapter.
    /// </summary>
    public sealed class WebComponentPlatformNavigationEventArgs : EventArgs
    {
        public WebComponentPlatformNavigationEventArgs(bool isSuccess, string error)
        {
            IsSuccess = isSuccess;
            Error = error ?? string.Empty;
        }

        public bool IsSuccess { get; }

        public string Error { get; }
    }
}
