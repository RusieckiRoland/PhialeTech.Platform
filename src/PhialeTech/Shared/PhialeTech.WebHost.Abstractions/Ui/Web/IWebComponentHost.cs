using System;
using System.Threading.Tasks;

namespace PhialeTech.WebHost.Abstractions.Ui.Web
{
    /// <summary>
    /// Shared browser-host abstraction used by platform UI controls.
    /// </summary>
    public interface IWebComponentHost : IDisposable
    {
        WebComponentHostOptions Options { get; }

        bool IsInitialized { get; }

        bool IsReady { get; }

        event EventHandler<WebComponentMessageEventArgs> MessageReceived;

        event EventHandler<WebComponentReadyStateChangedEventArgs> ReadyStateChanged;

        Task InitializeAsync();

        Task LoadEntryPageAsync(string entryPageRelativePath);

        Task NavigateAsync(Uri uri);

        Task LoadHtmlAsync(string html, string baseUrl = null);

        Task PostMessageAsync(object message);

        Task PostRawMessageAsync(string rawMessage);

        Task<string> ExecuteScriptAsync(string script);

        void FocusHost();
    }
}
