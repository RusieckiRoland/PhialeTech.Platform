using System;
using System.Collections.Generic;
using PhialeTech.ActiveLayerSelector.Localization;

namespace PhialeTech.ActiveLayerSelector
{
    public sealed class ActiveLayerSelectorLayerViewModel
    {
        private readonly Func<string, string> _localize;

        public ActiveLayerSelectorLayerViewModel(
            ActiveLayerSelectorItemState state,
            Func<string, string> localize)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            _localize = localize ?? throw new ArgumentNullException(nameof(localize));
            LayerId = state.LayerId ?? string.Empty;
            Name = state.Name ?? string.Empty;
            TreePath = state.TreePath ?? string.Empty;
            LayerType = state.LayerType ?? string.Empty;
            GeometryType = state.GeometryType ?? string.Empty;
            IsActive = state.IsActive;
            IsVisible = state.IsVisible;
            IsSelectable = state.IsSelectable;
            IsEditable = state.IsEditable;
            IsSnappable = state.IsSnappable;
            CanBecomeActive = state.CanBecomeActive;

            VisibleCapability = new ActiveLayerSelectorCapabilityViewModel(LayerId, ActiveLayerSelectorCapabilityKind.Visible, IsVisible, state.CanToggleVisible, localize);
            SelectableCapability = new ActiveLayerSelectorCapabilityViewModel(LayerId, ActiveLayerSelectorCapabilityKind.Selectable, IsSelectable, state.CanToggleSelectable, localize);
            EditableCapability = new ActiveLayerSelectorCapabilityViewModel(LayerId, ActiveLayerSelectorCapabilityKind.Editable, IsEditable, state.CanToggleEditable, localize);
            SnappableCapability = new ActiveLayerSelectorCapabilityViewModel(LayerId, ActiveLayerSelectorCapabilityKind.Snappable, IsSnappable, state.CanToggleSnappable, localize);
            Capabilities = new[] { VisibleCapability, SelectableCapability, EditableCapability, SnappableCapability };
        }

        public string LayerId { get; }
        public string Name { get; }
        public string TreePath { get; }
        public string LayerType { get; }
        public string GeometryType { get; }
        public bool IsActive { get; }
        public bool IsVisible { get; }
        public bool IsSelectable { get; }
        public bool IsEditable { get; }
        public bool IsSnappable { get; }
        public bool CanBecomeActive { get; }
        public bool CanSetActive => !IsActive && CanBecomeActive;
        public ActiveLayerSelectorCapabilityViewModel VisibleCapability { get; }
        public ActiveLayerSelectorCapabilityViewModel SelectableCapability { get; }
        public ActiveLayerSelectorCapabilityViewModel EditableCapability { get; }
        public ActiveLayerSelectorCapabilityViewModel SnappableCapability { get; }
        public IReadOnlyList<ActiveLayerSelectorCapabilityViewModel> Capabilities { get; }
        public string StatusText => IsActive ? _localize(ActiveLayerSelectorTextKeys.Active) : _localize(ActiveLayerSelectorTextKeys.SetActive);
        public string PathText => _localize(ActiveLayerSelectorTextKeys.Path) + ": " + TreePath;
        public string SourceBadgeText => _localize(ActiveLayerSelectorTextKeys.Source) + ": " + LayerType;
    }
}
