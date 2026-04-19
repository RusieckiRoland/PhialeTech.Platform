using System;
using PhialeGrid.Core.Regions;

namespace PhialeTech.PhialeGrid.Wpf.Regions
{
    internal sealed class WpfGridRegionChromeState
    {
        internal static readonly WpfGridRegionChromeState Hidden = new WpfGridRegionChromeState(
            default,
            default,
            false,
            false,
            false,
            string.Empty);

        internal WpfGridRegionChromeState(
            GridRegionKind regionKind,
            GridRegionState state,
            bool isVisible,
            bool canCollapse,
            bool canClose,
            string toggleText)
        {
            RegionKind = regionKind;
            State = state;
            IsVisible = isVisible;
            CanCollapse = canCollapse;
            CanClose = canClose;
            ToggleText = toggleText ?? string.Empty;
        }

        internal GridRegionKind RegionKind { get; }

        internal GridRegionState State { get; }

        internal bool IsVisible { get; }

        internal bool CanCollapse { get; }

        internal bool CanClose { get; }

        internal string ToggleText { get; }
    }
}
