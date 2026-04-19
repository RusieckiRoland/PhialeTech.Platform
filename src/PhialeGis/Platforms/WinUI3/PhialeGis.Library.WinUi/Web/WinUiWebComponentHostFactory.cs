using PhialeGis.Library.Abstractions.Ui.Web;
using PhialeGis.Library.WinUi.Controls;

namespace PhialeGis.Library.WinUi.Web
{
    public sealed class WinUiWebComponentHostFactory : IWebComponentHostFactory
    {
        public IWebComponentHost CreateHost(WebComponentHostOptions options)
        {
            return new PhialeWebComponentHost(options ?? new WebComponentHostOptions());
        }
    }
}
