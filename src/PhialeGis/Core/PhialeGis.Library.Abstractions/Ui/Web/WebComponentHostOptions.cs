using System;

namespace PhialeGis.Library.Abstractions.Ui.Web
{
    /// <summary>
    /// Configures a reusable browser host used by platform UI layers.
    /// </summary>
    public sealed class WebComponentHostOptions
    {
        /// <summary>
        /// Virtual host name used by engines that need local-folder mapping
        /// (for example WebView2).
        /// </summary>
        public string VirtualHostName { get; set; } = "phiale.webhost";

        /// <summary>
        /// When true, outgoing messages are queued until the browser host is ready.
        /// </summary>
        public bool QueueMessagesUntilReady { get; set; } = true;

        /// <summary>
        /// Optional JS message type that marks the host as ready.
        /// If empty, transport readiness is driven by native navigation completion.
        /// </summary>
        public string JavaScriptReadyMessageType { get; set; }

        /// <summary>
        /// Optional root folder for local entry pages.
        /// </summary>
        public string LocalContentRootPath { get; set; }

        public WebComponentHostOptions Clone()
        {
            return new WebComponentHostOptions
            {
                VirtualHostName = VirtualHostName,
                QueueMessagesUntilReady = QueueMessagesUntilReady,
                JavaScriptReadyMessageType = JavaScriptReadyMessageType,
                LocalContentRootPath = LocalContentRootPath
            };
        }
    }
}
