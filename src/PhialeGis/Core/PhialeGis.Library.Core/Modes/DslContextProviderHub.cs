// File: PhialeGis.Library.Dsl/Modes/DslContextProviderHub.cs
// English comments only.
// This is the tiny bridge the DSL layer uses to access the provider without Ambient.
using System;
using PhialeGis.Library.Abstractions.Modes;

namespace PhialeGis.Library.Dsl.Modes
{
    public static class DslContextProviderHub
    {
        private static IDslContextProvider _provider;

        public static void SetProvider(IDslContextProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException("provider");
        }

        public static IDslContextProvider Provider
        {
            get
            {
                var p = _provider;
                if (p == null) throw new InvalidOperationException("IDslContextProvider not set. Call BindDsl(...) first.");
                return p;
            }
        }
    }
}
