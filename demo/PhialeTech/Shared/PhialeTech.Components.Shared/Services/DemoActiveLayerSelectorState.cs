using System;
using System.Collections.Generic;
using System.Linq;
using PhialeTech.ActiveLayerSelector;

namespace PhialeTech.Components.Shared.Services
{
    public sealed class DemoActiveLayerSelectorState : IActiveLayerSelectorState
    {
        private List<ActiveLayerSelectorItemState> _items;

        public DemoActiveLayerSelectorState(IEnumerable<ActiveLayerSelectorItemState> items)
        {
            _items = (items ?? Array.Empty<ActiveLayerSelectorItemState>())
                .Select(item => Clone(item))
                .ToList();
            ActiveLayerId = _items.FirstOrDefault(item => item.IsActive)?.LayerId ?? string.Empty;
        }

        public IReadOnlyList<ActiveLayerSelectorItemState> Items => _items;

        public string ActiveLayerId { get; private set; }

        public event EventHandler StateChanged;

        public void SetActiveLayer(string layerId)
        {
            if (string.IsNullOrWhiteSpace(layerId))
            {
                return;
            }

            var target = _items.FirstOrDefault(item => string.Equals(item.LayerId, layerId, StringComparison.Ordinal));
            if (target == null || !target.CanBecomeActive)
            {
                return;
            }

            ActiveLayerId = layerId;
            _items = _items
                .Select(item => Clone(item, isActive: string.Equals(item.LayerId, layerId, StringComparison.Ordinal)))
                .ToList();
            RaiseStateChanged();
        }

        public void SetLayerVisible(string layerId, bool value)
        {
            UpdateLayer(layerId, item => item.CanToggleVisible, (item, clone) => clone.IsVisible = value);
        }

        public void SetLayerSelectable(string layerId, bool value)
        {
            UpdateLayer(layerId, item => item.CanToggleSelectable, (item, clone) => clone.IsSelectable = value);
        }

        public void SetLayerEditable(string layerId, bool value)
        {
            UpdateLayer(layerId, item => item.CanToggleEditable, (item, clone) => clone.IsEditable = value);
        }

        public void SetLayerSnappable(string layerId, bool value)
        {
            UpdateLayer(layerId, item => item.CanToggleSnappable, (item, clone) => clone.IsSnappable = value);
        }

        private void UpdateLayer(
            string layerId,
            Func<ActiveLayerSelectorItemState, bool> canUpdate,
            Action<ActiveLayerSelectorItemState, ActiveLayerSelectorItemState> apply)
        {
            if (string.IsNullOrWhiteSpace(layerId))
            {
                return;
            }

            var updated = false;
            _items = _items
                .Select(item =>
                {
                    if (!string.Equals(item.LayerId, layerId, StringComparison.Ordinal) || !canUpdate(item))
                    {
                        return item;
                    }

                    var clone = Clone(item);
                    apply(item, clone);
                    updated = true;
                    return clone;
                })
                .ToList();

            if (updated)
            {
                RaiseStateChanged();
            }
        }

        private void RaiseStateChanged()
        {
            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        private static ActiveLayerSelectorItemState Clone(
            ActiveLayerSelectorItemState item,
            bool? isActive = null)
        {
            return new ActiveLayerSelectorItemState
            {
                LayerId = item.LayerId,
                Name = item.Name,
                TreePath = item.TreePath,
                LayerType = item.LayerType,
                GeometryType = item.GeometryType,
                IsActive = isActive ?? item.IsActive,
                IsVisible = item.IsVisible,
                IsSelectable = item.IsSelectable,
                IsEditable = item.IsEditable,
                IsSnappable = item.IsSnappable,
                CanBecomeActive = item.CanBecomeActive,
                CanToggleVisible = item.CanToggleVisible,
                CanToggleSelectable = item.CanToggleSelectable,
                CanToggleEditable = item.CanToggleEditable,
                CanToggleSnappable = item.CanToggleSnappable,
            };
        }
    }
}

