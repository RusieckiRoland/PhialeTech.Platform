using System;

namespace PhialeTech.WebHost.Abstractions.Ui.Web
{
    /// <summary>
    /// Raw browser-host message forwarded from JavaScript to .NET.
    /// </summary>
    public sealed class WebComponentMessageEventArgs : EventArgs
    {
        public WebComponentMessageEventArgs(string rawMessage, string messageType)
        {
            RawMessage = rawMessage ?? string.Empty;
            MessageType = messageType ?? string.Empty;
        }

        public string RawMessage { get; }

        public string MessageType { get; }
    }
}
