using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PhialeTech.ActiveLayerSelector.Core;
using PhialeTech.ActiveLayerSelector.Localization;
using UniversalInput.Contracts;

namespace PhialeTech.ActiveLayerSelector
{
    public sealed class ActiveLayerSelectorViewModel : BindableBase, IDisposable
    {
        private ActiveLayerSelectorLocalizationCatalog _localizationCatalog;
        private string _languageCode;
        private IActiveLayerSelectorState _state;
        private IReadOnlyList<ActiveLayerSelectorLayerViewModel> _allItems;
        private IReadOnlyList<ActiveLayerSelectorLayerViewModel> _filteredItems;
        private IReadOnlyList<ActiveLayerSelectorLayerViewModel> _visibleItems;
        private ActiveLayerSelectorLayerViewModel _activeItem;
        private bool _isExpanded;
        private int _visibleCount;
        private string _searchText = string.Empty;

        public ActiveLayerSelectorViewModel(ActiveLayerSelectorLocalizationCatalog localizationCatalog = null, string languageCode = "en", int initialVisibleItemCount = 5)
        {
            _localizationCatalog = localizationCatalog ?? ActiveLayerSelectorLocalizationCatalog.Empty;
            _languageCode = string.IsNullOrWhiteSpace(languageCode) ? "en" : languageCode;
            InitialVisibleItemCount = initialVisibleItemCount < 1 ? 5 : initialVisibleItemCount;
            _allItems = Array.Empty<ActiveLayerSelectorLayerViewModel>();
            _filteredItems = Array.Empty<ActiveLayerSelectorLayerViewModel>();
            _visibleItems = Array.Empty<ActiveLayerSelectorLayerViewModel>();
        }

        public int InitialVisibleItemCount { get; }
        public IReadOnlyList<ActiveLayerSelectorLayerViewModel> AllItems => _allItems;
        public IReadOnlyList<ActiveLayerSelectorLayerViewModel> VisibleItems => _visibleItems;
        public ActiveLayerSelectorLayerViewModel ActiveItem => _activeItem;
        public IReadOnlyList<ActiveLayerSelectorCapabilityViewModel> HeaderCapabilities => ActiveItem == null ? Array.Empty<ActiveLayerSelectorCapabilityViewModel>() : ActiveItem.Capabilities;
        public bool IsExpanded { get => _isExpanded; private set => SetProperty(ref _isExpanded, value); }
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (!SetProperty(ref _searchText, value ?? string.Empty))
                {
                    return;
                }

                RebuildFilteredItems();
                EnsureVisibleCountInRange();
                RefreshVisibleItems();
                OnPropertyChanged(nameof(HasItems));
                OnPropertyChanged(nameof(HasNoItems));
            }
        }

        public bool HasItems => _filteredItems.Count > 0;
        public bool HasNoItems => !HasItems;
        public bool HasActiveItem => _activeItem != null;
        public bool CanShowMore => IsExpanded && _visibleCount < _filteredItems.Count;
        public string ShowMoreText => Localize(ActiveLayerSelectorTextKeys.ShowMore);
        public string ShowLayersButtonText => string.Format(CultureInfo.CurrentCulture, Localize(ActiveLayerSelectorTextKeys.ShowLayersButton), _allItems.Count);
        public string SearchPlaceholderText => Localize(ActiveLayerSelectorTextKeys.SearchPlaceholder);
        public string EmptyStateText => Localize(ActiveLayerSelectorTextKeys.NoLayers);
        public string HeaderTitle => ActiveItem?.Name ?? Localize(ActiveLayerSelectorTextKeys.NoActiveLayer);
        public string HeaderPath => ActiveItem?.PathText ?? string.Empty;
        public string StatusHeaderText => Localize(ActiveLayerSelectorTextKeys.ColumnStatus);
        public string VisibleHeaderText => Localize(ActiveLayerSelectorTextKeys.ColumnVisible);
        public string SelectableHeaderText => Localize(ActiveLayerSelectorTextKeys.ColumnSelectable);
        public string EditableHeaderText => Localize(ActiveLayerSelectorTextKeys.ColumnEditable);
        public string SnappableHeaderText => Localize(ActiveLayerSelectorTextKeys.ColumnSnappable);
        public string TypeHeaderText => Localize(ActiveLayerSelectorTextKeys.ColumnType);
        public string LayerInfoHeaderText => Localize(ActiveLayerSelectorTextKeys.ColumnLayerInfo);
        public string ChevronToolTip => IsExpanded ? Localize(ActiveLayerSelectorTextKeys.Collapse) : Localize(ActiveLayerSelectorTextKeys.Expand);

        public void HandleCommand(UniversalCommandEventArgs args)
        {
            if (args == null || string.IsNullOrWhiteSpace(args.CommandId))
            {
                return;
            }

            switch (args.CommandId)
            {
                case ActiveLayerSelectorCommandIds.ToggleExpanded:
                    ToggleExpanded();
                    break;
                case ActiveLayerSelectorCommandIds.ShowMore:
                    ShowMore();
                    break;
                case ActiveLayerSelectorCommandIds.SetActive:
                    HandleSetActive(args);
                    break;
                case ActiveLayerSelectorCommandIds.ToggleCapability:
                    HandleToggleCapability(args);
                    break;
            }
        }

        public void AttachState(IActiveLayerSelectorState state)
        {
            if (ReferenceEquals(_state, state))
            {
                return;
            }

            if (_state != null)
            {
                _state.StateChanged -= HandleStateChanged;
            }

            _state = state;
            if (_state != null)
            {
                _state.StateChanged += HandleStateChanged;
            }

            SyncFromState();
        }

        public void UpdateLocalization(ActiveLayerSelectorLocalizationCatalog localizationCatalog, string languageCode)
        {
            _localizationCatalog = localizationCatalog ?? ActiveLayerSelectorLocalizationCatalog.Empty;
            _languageCode = string.IsNullOrWhiteSpace(languageCode) ? "en" : languageCode;
            SyncFromState();
        }

        public void Dispose()
        {
            if (_state != null)
            {
                _state.StateChanged -= HandleStateChanged;
            }
        }

        private void ToggleExpanded()
        {
            IsExpanded = !IsExpanded;
            if (IsExpanded && _visibleCount == 0)
            {
                _visibleCount = Math.Min(InitialVisibleItemCount, _filteredItems.Count);
            }

            EnsureVisibleCountInRange();
            RefreshVisibleItems();
        }

        private void ShowMore()
        {
            if (!CanShowMore)
            {
                return;
            }

            _visibleCount = Math.Min(_filteredItems.Count, _visibleCount + InitialVisibleItemCount);
            RefreshVisibleItems();
        }

        private void HandleStateChanged(object sender, EventArgs e)
        {
            SyncFromState();
        }

        private void SyncFromState()
        {
            var states = _state?.Items ?? Array.Empty<ActiveLayerSelectorItemState>();
            _allItems = states.Select(CreateItemViewModel).ToArray();
            _activeItem = _allItems.FirstOrDefault(item => string.Equals(item.LayerId, _state?.ActiveLayerId ?? string.Empty, StringComparison.Ordinal))
                ?? _allItems.FirstOrDefault(item => item.IsActive);
            RebuildFilteredItems();
            EnsureVisibleCountInRange();

            RefreshVisibleItems();
            RaiseAllProperties();
        }

        private ActiveLayerSelectorLayerViewModel CreateItemViewModel(ActiveLayerSelectorItemState item)
        {
            return new ActiveLayerSelectorLayerViewModel(
                item,
                Localize);
        }

        private void RefreshVisibleItems()
        {
            _visibleItems = _filteredItems.Take(_visibleCount).ToArray();
            OnPropertyChanged(nameof(VisibleItems));
            OnPropertyChanged(nameof(CanShowMore));
            OnPropertyChanged(nameof(ShowMoreText));
            OnPropertyChanged(nameof(ChevronToolTip));
        }

        private void RebuildFilteredItems()
        {
            var filter = _searchText?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(filter))
            {
                _filteredItems = _allItems;
                return;
            }

            _filteredItems = _allItems
                .Where(item => ContainsIgnoreCase(item.Name, filter) ||
                               ContainsIgnoreCase(item.TreePath, filter) ||
                               ContainsIgnoreCase(item.LayerType, filter))
                .ToArray();
        }

        private void EnsureVisibleCountInRange()
        {
            var total = _filteredItems.Count;
            if (!IsExpanded)
            {
                _visibleCount = Math.Min(InitialVisibleItemCount, total);
                return;
            }

            if (_visibleCount == 0)
            {
                _visibleCount = Math.Min(InitialVisibleItemCount, total);
                return;
            }

            _visibleCount = Math.Min(_visibleCount, total);
        }

        private void HandleSetActive(UniversalCommandEventArgs args)
        {
            if (_state == null || !TryGetArgument(args, "layerId", out var layerId))
            {
                return;
            }

            _state.SetActiveLayer(layerId);
        }

        private void HandleToggleCapability(UniversalCommandEventArgs args)
        {
            if (_state == null ||
                !TryGetArgument(args, "layerId", out var layerId) ||
                !TryGetArgument(args, "capability", out var capabilityText))
            {
                return;
            }

            var item = _state.Items.FirstOrDefault(candidate => string.Equals(candidate.LayerId, layerId, StringComparison.Ordinal));
            if (item == null)
            {
                return;
            }

            if (!Enum.TryParse(capabilityText, ignoreCase: true, out ActiveLayerSelectorCapabilityKind capability))
            {
                return;
            }

            switch (capability)
            {
                case ActiveLayerSelectorCapabilityKind.Visible:
                    _state.SetLayerVisible(layerId, !item.IsVisible);
                    break;
                case ActiveLayerSelectorCapabilityKind.Selectable:
                    _state.SetLayerSelectable(layerId, !item.IsSelectable);
                    break;
                case ActiveLayerSelectorCapabilityKind.Editable:
                    _state.SetLayerEditable(layerId, !item.IsEditable);
                    break;
                case ActiveLayerSelectorCapabilityKind.Snappable:
                    _state.SetLayerSnappable(layerId, !item.IsSnappable);
                    break;
            }
        }

        private static bool TryGetArgument(UniversalCommandEventArgs args, string key, out string value)
        {
            value = string.Empty;
            return args.Arguments != null &&
                args.Arguments.TryGetValue(key, out value) &&
                !string.IsNullOrWhiteSpace(value);
        }

        private static bool ContainsIgnoreCase(string source, string candidate)
        {
            return !string.IsNullOrWhiteSpace(source) &&
                source.IndexOf(candidate, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private string Localize(string key)
        {
            return _localizationCatalog.GetText(_languageCode, key);
        }

        private void RaiseAllProperties()
        {
            OnPropertyChanged(nameof(AllItems));
            OnPropertyChanged(nameof(ActiveItem));
            OnPropertyChanged(nameof(HeaderCapabilities));
            OnPropertyChanged(nameof(HasItems));
            OnPropertyChanged(nameof(HasNoItems));
            OnPropertyChanged(nameof(HasActiveItem));
            OnPropertyChanged(nameof(HeaderTitle));
            OnPropertyChanged(nameof(HeaderPath));
            OnPropertyChanged(nameof(ShowLayersButtonText));
            OnPropertyChanged(nameof(SearchPlaceholderText));
            OnPropertyChanged(nameof(StatusHeaderText));
            OnPropertyChanged(nameof(VisibleHeaderText));
            OnPropertyChanged(nameof(SelectableHeaderText));
            OnPropertyChanged(nameof(EditableHeaderText));
            OnPropertyChanged(nameof(SnappableHeaderText));
            OnPropertyChanged(nameof(TypeHeaderText));
            OnPropertyChanged(nameof(LayerInfoHeaderText));
            OnPropertyChanged(nameof(EmptyStateText));
            RefreshVisibleItems();
        }
    }
}

