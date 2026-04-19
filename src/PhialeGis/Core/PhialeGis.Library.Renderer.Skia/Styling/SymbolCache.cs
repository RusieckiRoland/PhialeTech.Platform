using System;
using System.Collections.Generic;
using System.Globalization;
using PhialeGis.Library.Abstractions.Styling;

namespace PhialeGis.Library.Renderer.Skia.Styling
{
    public sealed class SymbolCache : IDisposable
    {
        private readonly object _syncRoot = new object();
        private readonly Dictionary<string, CompiledSymbol> _cache = new Dictionary<string, CompiledSymbol>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> _latestKeyBySymbolId = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly List<CompiledSymbol> _retiredSymbols = new List<CompiledSymbol>();
        private readonly SymbolCompiler _compiler;
        private bool _disposed;

        public SymbolCache(SymbolCompiler compiler = null)
        {
            _compiler = compiler ?? new SymbolCompiler();
        }

        public CompiledSymbol GetOrAdd(SymbolDefinition definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            lock (_syncRoot)
            {
                ThrowIfDisposed();

                var key = BuildKey(definition);
                if (_cache.TryGetValue(key, out var compiled))
                    return compiled;

                compiled = _compiler.Compile(definition);
                _cache[key] = compiled;

                var symbolId = definition.Id ?? string.Empty;
                if (_latestKeyBySymbolId.TryGetValue(symbolId, out var previousKey)
                    && !string.Equals(previousKey, key, StringComparison.Ordinal)
                    && _cache.TryGetValue(previousKey, out var previousCompiled))
                {
                    _cache.Remove(previousKey);
                    _retiredSymbols.Add(previousCompiled);
                }

                _latestKeyBySymbolId[symbolId] = key;
                return compiled;
            }
        }

        public bool TryGet(SymbolDefinition definition, out CompiledSymbol compiled)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            lock (_syncRoot)
            {
                ThrowIfDisposed();
                return _cache.TryGetValue(BuildKey(definition), out compiled);
            }
        }

        public void Clear()
        {
            lock (_syncRoot)
            {
                if (_disposed)
                    return;

                foreach (var entry in _cache.Values)
                    entry.Dispose();

                for (int i = 0; i < _retiredSymbols.Count; i++)
                    _retiredSymbols[i].Dispose();

                _cache.Clear();
                _latestKeyBySymbolId.Clear();
                _retiredSymbols.Clear();
            }
        }

        public void Dispose()
        {
            lock (_syncRoot)
            {
                if (_disposed)
                    return;

                Clear();
                _disposed = true;
            }
        }

        private string BuildKey(SymbolDefinition definition)
        {
            var hash = _compiler.ComputeContentHash(definition);
            return string.Format(CultureInfo.InvariantCulture, "{0}|{1:X8}", definition.Id ?? string.Empty, hash);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SymbolCache));
        }
    }
}
