using System;

namespace PhialeTech.ComponentHost.State
{
    public sealed class ApplicationStateRegistration : IDisposable
    {
        private readonly Action _disposeAction;

        internal ApplicationStateRegistration(string stateKey, bool restoredFromStore, Action disposeAction)
        {
            StateKey = stateKey ?? string.Empty;
            RestoredFromStore = restoredFromStore;
            _disposeAction = disposeAction;
        }

        public string StateKey { get; }

        public bool RestoredFromStore { get; }

        public void Dispose()
        {
            _disposeAction?.Invoke();
        }
    }
}
