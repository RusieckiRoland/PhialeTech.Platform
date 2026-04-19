using System;
using System.Windows;
using PhialeGrid.Core.Regions;

namespace PhialeTech.PhialeGrid.Wpf.Regions
{
    internal sealed class WpfGridStripRegionPresenter
    {
        internal WpfGridRegionChromeState Apply(
            WpfGridRegionStripBinding binding,
            GridRegionViewState state,
            bool contentAvailable,
            WpfGridRegionRenderDirectives directives)
        {
            if (binding == null)
            {
                throw new ArgumentNullException(nameof(binding));
            }

            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (state.HostKind != GridRegionHostKind.Strip)
            {
                throw new InvalidOperationException(state.RegionKind + " must resolve to a strip host in WPF.");
            }

            var isRenderable = state.IsAvailable && state.State != GridRegionState.Closed && contentAvailable;
            binding.Host.Visibility = isRenderable ? Visibility.Visible : Visibility.Collapsed;
            binding.SplitterRow.Height = isRenderable && directives.AllowResize && !directives.ForceCompactSize
                ? new GridLength(12d)
                : new GridLength(0d);
            binding.Row.MinHeight = isRenderable ? (state.MinSize ?? binding.FallbackExpandedSize) : 0d;
            binding.Host.MinHeight = isRenderable ? (state.MinSize ?? binding.FallbackExpandedSize) : 0d;

            if (!isRenderable)
            {
                binding.Row.Height = new GridLength(0d);
                binding.ContentHost.Visibility = Visibility.Collapsed;
                return new WpfGridRegionChromeState(
                    state.RegionKind,
                    state.State,
                    false,
                    false,
                    false,
                    ResolveToggleText(state));
            }

            var expandedSize = directives.ForceCompactSize
                ? ClampSize(state.MinSize ?? binding.FallbackExpandedSize, state.MinSize, state.MaxSize)
                : ClampSize(state.Size ?? binding.FallbackExpandedSize, state.MinSize, state.MaxSize);
            binding.Row.Height = new GridLength(state.State == GridRegionState.Collapsed ? (state.MinSize ?? binding.FallbackExpandedSize) : expandedSize);
            binding.ContentHost.Visibility = state.State == GridRegionState.Collapsed ? Visibility.Collapsed : Visibility.Visible;

            return new WpfGridRegionChromeState(
                state.RegionKind,
                state.State,
                true,
                state.CanCollapse && !directives.ForceCompactSize && isRenderable,
                state.CanClose && isRenderable,
                ResolveToggleText(state));
        }

        private static string ResolveToggleText(GridRegionViewState state)
        {
            return state.State == GridRegionState.Collapsed ? "˅" : "˄";
        }

        private static double ClampSize(double size, double? minSize, double? maxSize)
        {
            var clamped = size;
            if (minSize.HasValue)
            {
                clamped = Math.Max(clamped, minSize.Value);
            }

            if (maxSize.HasValue)
            {
                clamped = Math.Min(clamped, maxSize.Value);
            }

            return clamped;
        }
    }
}
