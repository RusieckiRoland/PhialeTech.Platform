using System;
using System.Windows;
using PhialeGrid.Core.Regions;

namespace PhialeTech.PhialeGrid.Wpf.Regions
{
    internal sealed class WpfGridWorkspaceBandPresenter
    {
        internal WpfGridRegionChromeState Apply(
            WpfGridWorkspaceBandBinding binding,
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

            if (state.HostKind != GridRegionHostKind.WorkspaceBand)
            {
                throw new InvalidOperationException(state.RegionKind + " must resolve to a workspace band in WPF.");
            }

            var isRenderable = state.IsAvailable && state.State != GridRegionState.Closed && contentAvailable;
            ApplyPlacement(binding, state.Placement);
            binding.Host.Visibility = isRenderable ? Visibility.Visible : Visibility.Collapsed;
            binding.SplitterRow.Height = new GridLength(0d);
            binding.Row.MinHeight = ResolveRowMinHeight(binding, state, isRenderable);
            binding.Host.MinHeight = isRenderable ? (state.MinSize ?? binding.FallbackExpandedSize) : 0d;

            if (!isRenderable)
            {
                ApplyHostSize(binding, 0d, isRenderable: false);
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
            ApplyHostSize(
                binding,
                state.State == GridRegionState.Collapsed ? (state.MinSize ?? binding.FallbackExpandedSize) : expandedSize,
                isRenderable: true,
                state.Placement);
            binding.ContentHost.Visibility = state.State == GridRegionState.Collapsed ? Visibility.Collapsed : Visibility.Visible;

            return new WpfGridRegionChromeState(
                state.RegionKind,
                state.State,
                true,
                state.CanCollapse && !directives.ForceCompactSize && isRenderable,
                state.CanClose && isRenderable,
                ResolveToggleText(state));
        }

        private static void ApplyHostSize(
            WpfGridWorkspaceBandBinding binding,
            double size,
            bool isRenderable,
            GridRegionPlacement placement = GridRegionPlacement.Top)
        {
            if (binding.UsesWorkspaceBandStackLayout)
            {
                binding.Row.Height = GridLength.Auto;
                binding.Host.Height = isRenderable ? size : 0d;
                return;
            }

            if (placement == GridRegionPlacement.Top)
            {
                binding.Host.ClearValue(FrameworkElement.HeightProperty);
                binding.Row.Height = new GridLength(size);
                return;
            }

            binding.Row.Height = new GridLength(0d);
            binding.Host.Height = isRenderable ? size : 0d;
        }

        private static double ResolveRowMinHeight(
            WpfGridWorkspaceBandBinding binding,
            GridRegionViewState state,
            bool isRenderable)
        {
            if (binding.UsesWorkspaceBandStackLayout ||
                state.Placement != GridRegionPlacement.Top ||
                !isRenderable)
            {
                return 0d;
            }

            return state.MinSize ?? binding.FallbackExpandedSize;
        }

        private static void ApplyPlacement(WpfGridWorkspaceBandBinding binding, GridRegionPlacement placement)
        {
            var targetHost = placement == GridRegionPlacement.Top
                ? binding.TopWorkspaceBandHost
                : binding.BottomWorkspaceBandHost;

            if (binding.Host.Parent == targetHost)
            {
                return;
            }

            var currentPanel = binding.Host.Parent as System.Windows.Controls.Panel;
            if (currentPanel == null)
            {
                throw new InvalidOperationException(binding.RegionKind + " workspace band host must be parented by a WPF Panel.");
            }

            currentPanel.Children.Remove(binding.Host);
            targetHost.Children.Add(binding.Host);
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
