using PhialeTech.WebHost.Abstractions.Ui.Web;
using PhialeTech.WebHost.WinUI.Controls;

namespace PhialeTech.WebHost.WinUI
{
    public sealed class WinUiWebComponentHostFactory : IWebComponentHostFactory
    {
        public IWebComponentHost CreateHost(WebComponentHostOptions options)
        {
            return new PhialeWebComponentHost(options ?? new WebComponentHostOptions());
        }
    }
}
