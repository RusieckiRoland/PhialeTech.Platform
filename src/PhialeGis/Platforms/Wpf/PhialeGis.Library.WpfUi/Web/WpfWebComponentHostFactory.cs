using PhialeGis.Library.Abstractions.Ui.Web;
using PhialeGis.Library.WpfUi.Controls;
using System;

namespace PhialeGis.Library.WpfUi.Web
{
    public sealed class WpfWebComponentHostFactory : IWebComponentHostFactory
    {
        public IWebComponentHost CreateHost(WebComponentHostOptions options)
        {
            return new PhialeWebComponentHost(options ?? new WebComponentHostOptions());
        }

        public static void WarmUpBrowserRuntime()
        {
            PhialeWebComponentHost.WarmUpBrowserRuntime();
        }
    }
}
