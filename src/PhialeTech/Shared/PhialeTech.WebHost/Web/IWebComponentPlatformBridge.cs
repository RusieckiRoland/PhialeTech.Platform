using System;
using System.Threading.Tasks;
using PhialeTech.WebHost.Abstractions.Ui.Web;

namespace PhialeTech.WebHost
{
    /// <summary>
    /// Thin platform adapter around a native browser control.
    /// </summary>
    public interface IWebComponentPlatformBridge : IDisposable
    {
        bool IsInitialized { get; }

        event EventHandler<string> MessageReceived;

        event EventHandler<WebComponentPlatformNavigationEventArgs> NavigationCompleted;

        Task InitializeAsync(WebComponentHostOptions options);

        Task LoadEntryPageAsync(string contentRootPath, string entryPageRelativePath, string virtualHostName);

        Task NavigateAsync(Uri uri);

        Task LoadHtmlAsync(string html, string baseUrl);

        Task<string> ExecuteScriptAsync(string script);

        void Focus();
    }
}
