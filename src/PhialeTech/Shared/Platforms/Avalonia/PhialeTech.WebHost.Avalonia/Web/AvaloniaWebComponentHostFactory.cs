using PhialeTech.WebHost.Abstractions.Ui.Web;
using PhialeTech.WebHost.Avalonia.Controls;

namespace PhialeTech.WebHost.Avalonia
{
    public sealed class AvaloniaWebComponentHostFactory : IWebComponentHostFactory
    {
        public IWebComponentHost CreateHost(WebComponentHostOptions options)
        {
            return new PhialeWebComponentHost(options ?? new WebComponentHostOptions());
        }
    }
}
