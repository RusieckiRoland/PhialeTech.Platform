using System;
using System.Collections.Generic;
using System.Linq;
using PhialeGrid.Core.Capabilities;
using PhialeGrid.Core.Interaction;
using PhialeGrid.Core.State;

namespace PhialeGrid.Core.Regions
{
    public sealed class GridRegionLayoutManager
    {
        private readonly Dictionary<GridRegionKind, GridRegionDefinition> _definitionsByKind;
        private readonly Dictionary<GridRegionKind, GridRegionLayoutState> _layoutByKind;
        private IGridCapabilityPolicy _capabilityPolicy;

        public GridRegionLayoutManager(
            IEnumerable<GridRegionDefinition> definitions = null,
            IGridCapabilityPolicy capabilityPolicy = null)
        {
            _definitionsByKind = CreateDefinitionsMap(definitions ?? GridRegionDefinitionCatalog.CreateDefault());
            _layoutByKind = _definitionsByKind.Values.ToDictionary(
                definition => definition.RegionKind,
                CreateDefaultLayoutState);
            _capabilityPolicy = capabilityPolicy ?? GridAllowAllCapabilityPolicy.Instance;
            ValidateInvariantState();
        }

        public void SetCapabilityPolicy(IGridCapabilityPolicy capabilityPolicy)
        {
            _capabilityPolicy = capabilityPolicy ?? GridAllowAllCapabilityPolicy.Instance;
            ValidateInvariantState();
        }

        public GridRegionLayoutSnapshot ExportLayout()
        {
            return new GridRegionLayoutSnapshot(_layoutByKind.Values
                .OrderBy(state => state.RegionKind)
                .Select(Clone)
                .ToArray());
        }

        public void RestoreLayout(GridRegionLayoutSnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            var states = snapshot.Regions;
            if (states.Count != _definitionsByKind.Count)
            {
                throw new ArgumentException("The region layout snapshot must contain a complete set of regions.", nameof(snapshot));
            }

            var byKind = new Dictionary<GridRegionKind, GridRegionLayoutState>();
            foreach (var state in states)
            {
                if (!_definitionsByKind.ContainsKey(state.RegionKind))
                {
                    throw new ArgumentException("The region layout snapshot contains an unknown region kind: " + state.RegionKind + ".", nameof(snapshot));
                }

                if (byKind.ContainsKey(state.RegionKind))
                {
                    throw new ArgumentException("Duplicate region entry found for " + state.RegionKind + ".", nameof(snapshot));
                }

                ValidateLayoutState(_definitionsByKind[state.RegionKind], state);
                byKind[state.RegionKind] = Clone(state);
            }

            foreach (var definition in _definitionsByKind.Values)
            {
                if (!byKind.ContainsKey(definition.RegionKind))
                {
                    throw new ArgumentException("The region layout snapshot is missing " + definition.RegionKind + ".", nameof(snapshot));
                }
            }

            _layoutByKind.Clear();
            foreach (var pair in byKind)
            {
                _layoutByKind[pair.Key] = pair.Value;
            }

            ValidateInvariantState();
        }

        public IReadOnlyList<GridRegionViewState> ResolveAll()
        {
            return _definitionsByKind.Keys
                .OrderBy(kind => kind)
                .Select(Resolve)
                .ToArray();
        }

        public GridRegionViewState Resolve(GridRegionKind regionKind)
        {
            var definition = GetDefinition(regionKind);
            var stored = _layoutByKind[regionKind];
            var isAvailable = IsAvailable(regionKind);
            var effectiveState = isAvailable ? stored.State : GridRegionState.Closed;

            return new GridRegionViewState(
                regionKind,
                definition.HostKind,
                definition.Placement,
                definition.ContentKind,
                effectiveState,
                isAvailable,
                isAvailable && effectiveState == GridRegionState.Open && stored.IsActive,
                definition.CanCollapse,
                definition.CanClose,
                definition.CanResize,
                definition.CanActivate,
                ResolveEffectiveSize(definition, stored.Size),
                definition.MinSize,
                definition.MaxSize);
        }

        public void OpenRegion(GridRegionKind regionKind)
        {
            var definition = GetDefinition(regionKind);
            EnsureAvailable(regionKind);
            UpdateState(regionKind, GridRegionState.Open, preserveActive: _layoutByKind[regionKind].IsActive && definition.CanActivate);
        }

        public void CollapseRegion(GridRegionKind regionKind)
        {
            var definition = GetDefinition(regionKind);
            EnsureAvailable(regionKind);
            if (!definition.CanCollapse)
            {
                throw new InvalidOperationException(regionKind + " is not collapsible.");
            }

            if (_layoutByKind[regionKind].State == GridRegionState.Closed)
            {
                throw new InvalidOperationException(regionKind + " cannot be collapsed while closed.");
            }

            UpdateState(regionKind, GridRegionState.Collapsed, preserveActive: false);
        }

        public void CloseRegion(GridRegionKind regionKind)
        {
            var definition = GetDefinition(regionKind);
            EnsureAvailable(regionKind);
            if (!definition.CanClose)
            {
                throw new InvalidOperationException(regionKind + " is not closable.");
            }

            UpdateState(regionKind, GridRegionState.Closed, preserveActive: false);
        }

        public void ResizeRegion(GridRegionKind regionKind, double size)
        {
            var definition = GetDefinition(regionKind);
            EnsureAvailable(regionKind);
            if (!definition.CanResize)
            {
                throw new InvalidOperationException(regionKind + " is not resizable.");
            }

            if (_layoutByKind[regionKind].State == GridRegionState.Closed)
            {
                throw new InvalidOperationException(regionKind + " cannot be resized while closed.");
            }

            var clampedSize = ClampSize(definition, size);
            var current = _layoutByKind[regionKind];
            _layoutByKind[regionKind] = new GridRegionLayoutState(
                regionKind,
                current.State,
                clampedSize,
                current.IsActive && current.State == GridRegionState.Open);
        }

        public void ActivateRegion(GridRegionKind regionKind)
        {
            var definition = GetDefinition(regionKind);
            EnsureAvailable(regionKind);
            if (!definition.CanActivate)
            {
                throw new InvalidOperationException(regionKind + " is not activatable.");
            }

            if (_layoutByKind[regionKind].State != GridRegionState.Open)
            {
                throw new InvalidOperationException(regionKind + " must be open before it can be activated.");
            }

            foreach (var kind in _layoutByKind.Keys.ToArray())
            {
                var current = _layoutByKind[kind];
                _layoutByKind[kind] = new GridRegionLayoutState(
                    current.RegionKind,
                    current.State,
                    current.Size,
                    kind == regionKind);
            }
        }

        public void Process(GridRegionCommandInput input)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            switch (input.CommandKind)
            {
                case GridRegionCommandKind.ToggleCollapse:
                    if (_layoutByKind[input.RegionKind].State == GridRegionState.Collapsed)
                    {
                        OpenRegion(input.RegionKind);
                        return;
                    }

                    if (_layoutByKind[input.RegionKind].State == GridRegionState.Closed)
                    {
                        throw new InvalidOperationException(input.RegionKind + " cannot toggle collapse while closed.");
                    }

                    CollapseRegion(input.RegionKind);
                    return;
                case GridRegionCommandKind.Open:
                    OpenRegion(input.RegionKind);
                    return;
                case GridRegionCommandKind.Collapse:
                    CollapseRegion(input.RegionKind);
                    return;
                case GridRegionCommandKind.Close:
                    CloseRegion(input.RegionKind);
                    return;
                case GridRegionCommandKind.Resize:
                    ResizeRegion(input.RegionKind, input.RequestedSize.Value);
                    return;
                case GridRegionCommandKind.Activate:
                    ActivateRegion(input.RegionKind);
                    return;
                default:
                    throw new NotSupportedException("Unsupported grid region command kind: " + input.CommandKind);
            }
        }

        private static Dictionary<GridRegionKind, GridRegionDefinition> CreateDefinitionsMap(IEnumerable<GridRegionDefinition> definitions)
        {
            var map = new Dictionary<GridRegionKind, GridRegionDefinition>();
            foreach (var definition in definitions ?? Array.Empty<GridRegionDefinition>())
            {
                if (definition == null)
                {
                    throw new ArgumentException("Region definitions cannot contain null entries.", nameof(definitions));
                }

                if (map.ContainsKey(definition.RegionKind))
                {
                    throw new ArgumentException("Duplicate region definition found for " + definition.RegionKind + ".", nameof(definitions));
                }

                map[definition.RegionKind] = definition;
            }

            if (map.Count == 0)
            {
                throw new ArgumentException("At least one region definition is required.", nameof(definitions));
            }

            return map;
        }

        private static GridRegionLayoutState CreateDefaultLayoutState(GridRegionDefinition definition)
        {
            return new GridRegionLayoutState(
                definition.RegionKind,
                definition.DefaultState,
                definition.DefaultSize,
                false);
        }

        private GridRegionDefinition GetDefinition(GridRegionKind regionKind)
        {
            if (!_definitionsByKind.ContainsKey(regionKind))
            {
                throw new ArgumentOutOfRangeException(nameof(regionKind), regionKind, "Unknown grid region kind.");
            }

            return _definitionsByKind[regionKind];
        }

        private bool IsAvailable(GridRegionKind regionKind)
        {
            var definition = GetDefinition(regionKind);
            return definition.IsRequired || (_capabilityPolicy?.CanViewRegion(regionKind) ?? true);
        }

        private void EnsureAvailable(GridRegionKind regionKind)
        {
            if (!IsAvailable(regionKind))
            {
                throw new InvalidOperationException(regionKind + " is not available under the current capability policy.");
            }
        }

        private void UpdateState(GridRegionKind regionKind, GridRegionState state, bool preserveActive)
        {
            var current = _layoutByKind[regionKind];
            _layoutByKind[regionKind] = new GridRegionLayoutState(
                regionKind,
                state,
                current.Size,
                preserveActive && state == GridRegionState.Open);

            if (state != GridRegionState.Open)
            {
                Deactivate(regionKind);
            }

            ValidateInvariantState();
        }

        private void Deactivate(GridRegionKind regionKind)
        {
            var current = _layoutByKind[regionKind];
            if (!current.IsActive)
            {
                return;
            }

            _layoutByKind[regionKind] = new GridRegionLayoutState(
                current.RegionKind,
                current.State,
                current.Size,
                false);
        }

        private void ValidateInvariantState()
        {
            foreach (var definition in _definitionsByKind.Values)
            {
                ValidateLayoutState(definition, _layoutByKind[definition.RegionKind]);
            }

            var activeCount = _layoutByKind.Values.Count(state =>
                state.IsActive
                && state.State == GridRegionState.Open
                && _definitionsByKind[state.RegionKind].CanActivate
                && IsAvailable(state.RegionKind));
            if (activeCount > 1)
            {
                throw new InvalidOperationException("Only one active region is allowed at a time.");
            }
        }

        private static void ValidateLayoutState(GridRegionDefinition definition, GridRegionLayoutState state)
        {
            if (state.RegionKind != definition.RegionKind)
            {
                throw new InvalidOperationException("Region layout state does not match its definition.");
            }

            if (definition.IsRequired && state.State != GridRegionState.Open)
            {
                throw new InvalidOperationException(definition.RegionKind + " must stay open.");
            }

            if (state.State == GridRegionState.Collapsed && !definition.CanCollapse)
            {
                throw new InvalidOperationException(definition.RegionKind + " does not support the collapsed state.");
            }

            if (state.State == GridRegionState.Closed && !definition.CanClose && !definition.IsRequired)
            {
                throw new InvalidOperationException(definition.RegionKind + " does not support the closed state.");
            }

            if (state.IsActive && (!definition.CanActivate || state.State != GridRegionState.Open))
            {
                throw new InvalidOperationException(definition.RegionKind + " cannot be active in its current state.");
            }

            if (state.Size.HasValue)
            {
                var clamped = ClampSize(definition, state.Size.Value);
                if (Math.Abs(clamped - state.Size.Value) > 0.001d)
                {
                    throw new InvalidOperationException(definition.RegionKind + " contains a size outside its allowed bounds.");
                }
            }
        }

        private static double ClampSize(GridRegionDefinition definition, double size)
        {
            var resolved = size;
            if (resolved <= 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(size), "Region size must be positive.");
            }

            if (definition.MinSize.HasValue)
            {
                resolved = Math.Max(resolved, definition.MinSize.Value);
            }

            if (definition.MaxSize.HasValue)
            {
                resolved = Math.Min(resolved, definition.MaxSize.Value);
            }

            return resolved;
        }

        private static double? ResolveEffectiveSize(GridRegionDefinition definition, double? size)
        {
            if (!size.HasValue)
            {
                return definition.DefaultSize;
            }

            return ClampSize(definition, size.Value);
        }

        private static GridRegionLayoutState Clone(GridRegionLayoutState state)
        {
            return new GridRegionLayoutState(
                state.RegionKind,
                state.State,
                state.Size,
                state.IsActive);
        }
    }
}
