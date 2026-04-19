using PhialeTech.WebHost.Abstractions.Ui.Web;
using PhialeTech.WebHost.Wpf.Controls;
using System;

namespace PhialeTech.WebHost.Wpf
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
