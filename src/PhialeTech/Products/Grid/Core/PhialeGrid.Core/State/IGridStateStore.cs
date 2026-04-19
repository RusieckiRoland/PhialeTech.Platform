using System;
using System.Collections.Generic;

namespace PhialeGrid.Core.State
{
    public interface IGridStateStore
    {
        void Save(string key, string encodedState);

        string Load(string key);
    }

    public sealed class InMemoryGridStateStore : IGridStateStore
    {
        private readonly Dictionary<string, string> _state = new Dictionary<string, string>(StringComparer.Ordinal);

        public void Save(string key, string encodedState)
        {
            _state[key] = encodedState;
        }

        public string Load(string key)
        {
            return _state.TryGetValue(key, out var value) ? value : null;
        }
    }
}
