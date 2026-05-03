using System;
using System.Collections.Generic;
using System.Text.Json;
using PhialeTech.ComponentHost.Abstractions.State;

namespace PhialeTech.ComponentHost.State
{
    public sealed class ApplicationStateManager : IDisposable
    {
        private const int CurrentEnvelopeVersion = 1;

        private readonly IApplicationStateStore _store;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly Dictionary<string, Registration> _registrations = new Dictionary<string, Registration>(StringComparer.Ordinal);
        private readonly Dictionary<string, PreloadedState> _preloadedStates = new Dictionary<string, PreloadedState>(StringComparer.Ordinal);

        public ApplicationStateManager(IApplicationStateStore store, JsonSerializerOptions jsonOptions = null)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _jsonOptions = jsonOptions ?? new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true,
            };
        }

        public ApplicationStateRegistration Register<TState>(string stateKey, IStatefulComponent<TState> component)
        {
            if (component == null)
            {
                throw new ArgumentNullException(nameof(component));
            }

            var normalizedStateKey = NormalizeStateKey(stateKey);
            Unregister(normalizedStateKey);

            EventHandler stateChangedHandler = (sender, args) => SaveRegisteredState(normalizedStateKey);
            var registration = new Registration(
                normalizedStateKey,
                typeof(TState),
                () => component.ExportState(),
                exportedState => component.ApplyState((TState)exportedState),
                () => component.StateChanged -= stateChangedHandler);

            var restored = TryRestoreLoadedState(normalizedStateKey, registration, out _);

            component.StateChanged += stateChangedHandler;
            _registrations[normalizedStateKey] = registration;
            return new ApplicationStateRegistration(normalizedStateKey, restored, () => Unregister(normalizedStateKey));
        }

        public void SaveRegisteredState(string stateKey)
        {
            var normalizedStateKey = NormalizeStateKey(stateKey);
            if (!_registrations.TryGetValue(normalizedStateKey, out var registration))
            {
                throw new InvalidOperationException("No registered stateful component for key '" + normalizedStateKey + "'.");
            }

            var state = registration.ExportState();
            Save(normalizedStateKey, registration.StateType, state);
        }

        public bool TryRestoreRegisteredState(string stateKey)
        {
            var normalizedStateKey = NormalizeStateKey(stateKey);
            if (!_registrations.TryGetValue(normalizedStateKey, out var registration))
            {
                throw new InvalidOperationException("No registered stateful component for key '" + normalizedStateKey + "'.");
            }

            return TryRestoreLoadedState(normalizedStateKey, registration, out _);
        }

        public void Delete(string stateKey)
        {
            var normalizedStateKey = NormalizeStateKey(stateKey);
            _store.Delete(normalizedStateKey);
            _preloadedStates.Remove(normalizedStateKey);
        }

        public void Save<TState>(string stateKey, TState state)
        {
            Save(NormalizeStateKey(stateKey), typeof(TState), state);
        }

        public bool TryLoad<TState>(string stateKey, out TState state)
        {
            if (TryLoad(NormalizeStateKey(stateKey), typeof(TState), out var rawState, false, true) && rawState is TState typedState)
            {
                state = typedState;
                return true;
            }

            state = default(TState);
            return false;
        }

        public bool Preload<TState>(string stateKey)
        {
            return TryLoad(NormalizeStateKey(stateKey), typeof(TState), out _, true, false);
        }

        public void Unregister(string stateKey)
        {
            var normalizedStateKey = NormalizeStateKey(stateKey);
            if (!_registrations.TryGetValue(normalizedStateKey, out var registration))
            {
                return;
            }

            registration.Detach();
            _registrations.Remove(normalizedStateKey);
        }

        public void Dispose()
        {
            foreach (var registration in _registrations.Values)
            {
                registration.Detach();
            }

            _registrations.Clear();
        }

        private void Save(string stateKey, Type stateType, object state)
        {
            var envelope = new ApplicationStateEnvelope
            {
                Version = CurrentEnvelopeVersion,
                StateType = stateType.AssemblyQualifiedName ?? stateType.FullName ?? stateType.Name,
                State = JsonSerializer.SerializeToElement(state, stateType, _jsonOptions),
            };

            var payload = JsonSerializer.Serialize(envelope, _jsonOptions);
            _store.Save(stateKey, payload);
            _preloadedStates.Remove(stateKey);
        }

        private bool TryLoad(string stateKey, Type stateType, out object state, bool keepPreloadedState, bool consumePreloadedState)
        {
            var payload = _store.Load(stateKey);
            if (string.IsNullOrWhiteSpace(payload))
            {
                _preloadedStates.Remove(stateKey);
                state = null;
                return false;
            }

            try
            {
                if (_preloadedStates.TryGetValue(stateKey, out var preloadedState) &&
                    preloadedState.StateType == stateType &&
                    string.Equals(preloadedState.Payload, payload, StringComparison.Ordinal))
                {
                    state = preloadedState.State;
                    if (consumePreloadedState)
                    {
                        _preloadedStates.Remove(stateKey);
                    }

                    return state != null;
                }

                var envelope = JsonSerializer.Deserialize<ApplicationStateEnvelope>(payload, _jsonOptions);
                if (envelope == null || envelope.State.ValueKind == JsonValueKind.Undefined || envelope.State.ValueKind == JsonValueKind.Null)
                {
                    _preloadedStates.Remove(stateKey);
                    state = null;
                    return false;
                }

                var expectedTypeName = stateType.AssemblyQualifiedName ?? stateType.FullName ?? stateType.Name;
                if (!string.IsNullOrWhiteSpace(envelope.StateType) &&
                    !string.Equals(envelope.StateType, expectedTypeName, StringComparison.Ordinal))
                {
                    _preloadedStates.Remove(stateKey);
                    state = null;
                    return false;
                }

                state = envelope.State.Deserialize(stateType, _jsonOptions);
                if (state != null && keepPreloadedState)
                {
                    _preloadedStates[stateKey] = new PreloadedState(payload, stateType, state);
                }

                return state != null;
            }
            catch
            {
                _preloadedStates.Remove(stateKey);
                state = null;
                return false;
            }
        }

        private bool TryRestoreLoadedState(string stateKey, Registration registration, out object state)
        {
            if (!TryLoad(stateKey, registration.StateType, out state, false, true))
            {
                return false;
            }

            try
            {
                registration.ApplyState(state);
                return true;
            }
            catch
            {
                _store.Delete(stateKey);
                _preloadedStates.Remove(stateKey);
                state = null;
                return false;
            }
        }

        private static string NormalizeStateKey(string stateKey)
        {
            if (string.IsNullOrWhiteSpace(stateKey))
            {
                throw new ArgumentException("State key is required.", nameof(stateKey));
            }

            return stateKey.Trim();
        }

        private sealed class Registration
        {
            private readonly Action<object> _applyState;
            private readonly Action _detachAction;
            private readonly Func<object> _exportState;

            public Registration(string stateKey, Type stateType, Func<object> exportState, Action<object> applyState, Action detachAction)
            {
                StateKey = stateKey;
                StateType = stateType;
                _exportState = exportState;
                _applyState = applyState;
                _detachAction = detachAction;
            }

            public string StateKey { get; }

            public Type StateType { get; }

            public object ExportState()
            {
                return _exportState();
            }

            public void ApplyState(object state)
            {
                _applyState(state);
            }

            public void Detach()
            {
                _detachAction?.Invoke();
            }
        }

        private sealed class ApplicationStateEnvelope
        {
            public int Version { get; set; }

            public string StateType { get; set; }

            public JsonElement State { get; set; }
        }

        private sealed class PreloadedState
        {
            public PreloadedState(string payload, Type stateType, object state)
            {
                Payload = payload;
                StateType = stateType;
                State = state;
            }

            public string Payload { get; }

            public Type StateType { get; }

            public object State { get; }
        }
    }
}
