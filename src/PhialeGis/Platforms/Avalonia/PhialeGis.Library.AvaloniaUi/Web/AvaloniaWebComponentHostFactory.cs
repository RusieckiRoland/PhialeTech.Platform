using PhialeGis.Library.Abstractions.Ui.Web;
using PhialeGis.Library.AvaloniaUi.Controls;

namespace PhialeGis.Library.AvaloniaUi.Web
{
    public sealed class AvaloniaWebComponentHostFactory : IWebComponentHostFactory
    {
        public IWebComponentHost CreateHost(WebComponentHostOptions options)
        {
            return new PhialeWebComponentHost(options ?? new WebComponentHostOptions());
        }
    }
}
