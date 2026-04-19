using System;
using System.Collections.Generic;
using System.Windows;
using PhialeTech.ComponentHost.Abstractions.Presentation;

namespace PhialeTech.ComponentHost.Wpf.Hosting
{
    public sealed class WpfHostedSurfaceFactoryRegistry
    {
        private readonly List<IWpfHostedSurfaceFactory> _factories = new List<IWpfHostedSurfaceFactory>();

        public void Register(IWpfHostedSurfaceFactory factory)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            _factories.Add(factory);
        }

        public FrameworkElement CreateContent(IHostedSurfaceRequest request, IHostedSurfaceManager manager)
        {
            foreach (var factory in _factories)
            {
                if (factory.CanCreate(request))
                {
                    return factory.CreateContent(request, manager);
                }
            }

            throw new InvalidOperationException("No WPF hosted surface factory is registered for content key '" + request?.ContentKey + "'.");
        }
    }
}
