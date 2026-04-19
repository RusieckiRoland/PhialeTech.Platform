using System;
using System.Collections.Generic;
using System.Linq;
using PhialeGrid.Core.Regions;

namespace PhialeTech.PhialeGrid.Wpf.Regions
{
    internal sealed class WpfGridRegionLayoutAdapter
    {
        private readonly IReadOnlyList<WpfGridRegionStripBinding> _stripBindings;
        private readonly WpfGridRegionPaneBinding _paneBinding;
        private readonly WpfGridStripRegionPresenter _stripPresenter = new WpfGridStripRegionPresenter();
        private readonly WpfGridPaneRegionPresenter _panePresenter = new WpfGridPaneRegionPresenter();
        private readonly Dictionary<GridRegionKind, WpfGridRegionChromeState> _chromeStateByKind = new Dictionary<GridRegionKind, WpfGridRegionChromeState>();

        internal WpfGridRegionLayoutAdapter(
            IReadOnlyList<WpfGridRegionStripBinding> stripBindings,
            WpfGridRegionPaneBinding paneBinding)
        {
            _stripBindings = stripBindings ?? throw new ArgumentNullException(nameof(stripBindings));
            _paneBinding = paneBinding ?? throw new ArgumentNullException(nameof(paneBinding));
        }

        internal void Apply(IEnumerable<GridRegionViewState> states, WpfGridRegionRenderSnapshot renderSnapshot)
        {
            if (states == null)
            {
                throw new ArgumentNullException(nameof(states));
            }

            if (renderSnapshot == null)
            {
                throw new ArgumentNullException(nameof(renderSnapshot));
            }

            var stateByKind = states.ToDictionary(state => state.RegionKind);
            EnsureRequiredState(stateByKind, GridRegionKind.CoreGridSurface);

            _chromeStateByKind.Clear();

            foreach (var strip in _stripBindings)
            {
                EnsureRequiredState(stateByKind, strip.RegionKind);
                if (!renderSnapshot.ContentAvailability.ContainsKey(strip.RegionKind))
                {
                    throw new InvalidOperationException("Missing WPF content-availability state for " + strip.RegionKind + ".");
                }

                if (!renderSnapshot.Directives.ContainsKey(strip.RegionKind))
                {
                    throw new InvalidOperationException("Missing WPF render directives for " + strip.RegionKind + ".");
                }

                _chromeStateByKind[strip.RegionKind] = _stripPresenter.Apply(
                    strip,
                    stateByKind[strip.RegionKind],
                    renderSnapshot.ContentAvailability[strip.RegionKind],
                    renderSnapshot.Directives[strip.RegionKind]);
            }

            EnsureRequiredState(stateByKind, _paneBinding.RegionKind);
            if (!renderSnapshot.ContentAvailability.ContainsKey(_paneBinding.RegionKind))
            {
                throw new InvalidOperationException("Missing WPF content-availability state for " + _paneBinding.RegionKind + ".");
            }

            _chromeStateByKind[_paneBinding.RegionKind] = _panePresenter.Apply(
                _paneBinding,
                stateByKind[_paneBinding.RegionKind],
                renderSnapshot.ContentAvailability[_paneBinding.RegionKind],
                renderSnapshot.UsePaneDrawerChrome);
        }

        internal WpfGridRegionChromeState GetChromeState(GridRegionKind regionKind)
        {
            return _chromeStateByKind.TryGetValue(regionKind, out var chromeState)
                ? chromeState
                : WpfGridRegionChromeState.Hidden;
        }

        private static void EnsureRequiredState(IReadOnlyDictionary<GridRegionKind, GridRegionViewState> states, GridRegionKind regionKind)
        {
            if (!states.ContainsKey(regionKind))
            {
                throw new InvalidOperationException("Missing required Core region state for " + regionKind + ".");
            }
        }
    }
}
