using PhialeTech.ActiveLayerSelector.Localization;

namespace PhialeTech.ActiveLayerSelector
{
    public sealed class ActiveLayerSelectorCapabilityViewModel
    {
        private readonly System.Func<string, string> _localize;

        public ActiveLayerSelectorCapabilityViewModel(string layerId, ActiveLayerSelectorCapabilityKind kind, bool isOn, bool isEnabled, System.Func<string, string> localize)
        {
            LayerId = layerId ?? string.Empty;
            Kind = kind;
            IsOn = isOn;
            IsEnabled = isEnabled;
            _localize = localize ?? throw new System.ArgumentNullException(nameof(localize));
        }

        public string LayerId { get; }
        public ActiveLayerSelectorCapabilityKind Kind { get; }
        public bool IsOn { get; }
        public bool IsEnabled { get; }

        public string ToolTip
        {
            get
            {
                switch (Kind)
                {
                    case ActiveLayerSelectorCapabilityKind.Visible:
                        return _localize(ActiveLayerSelectorTextKeys.CapabilityVisible);
                    case ActiveLayerSelectorCapabilityKind.Selectable:
                        return _localize(ActiveLayerSelectorTextKeys.CapabilitySelectable);
                    case ActiveLayerSelectorCapabilityKind.Editable:
                        return _localize(ActiveLayerSelectorTextKeys.CapabilityEditable);
                    default:
                        return _localize(ActiveLayerSelectorTextKeys.CapabilitySnappable);
                }
            }
        }
    }
}
