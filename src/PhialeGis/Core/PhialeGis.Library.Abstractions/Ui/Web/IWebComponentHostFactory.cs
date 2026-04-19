namespace PhialeGis.Library.Abstractions.Ui.Web
{
    /// <summary>
    /// Platform-specific factory creating reusable browser hosts.
    /// </summary>
    public interface IWebComponentHostFactory
    {
        IWebComponentHost CreateHost(WebComponentHostOptions options);
    }
}
