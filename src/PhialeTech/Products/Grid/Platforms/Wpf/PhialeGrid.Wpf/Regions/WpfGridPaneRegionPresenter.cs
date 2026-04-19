using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using PhialeGrid.Core.Regions;

namespace PhialeTech.PhialeGrid.Wpf.Regions
{
    internal sealed class WpfGridPaneRegionPresenter
    {
        private const double CollapsedRailWidth = 44d;
        private bool _hasAppliedDrawerState;
        private bool _lastDrawerCollapsed;

        internal WpfGridRegionChromeState Apply(
            WpfGridRegionPaneBinding binding,
            GridRegionViewState state,
            bool contentAvailable,
            bool useDrawerChrome)
        {
            if (binding == null)
            {
                throw new ArgumentNullException(nameof(binding));
            }

            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (state.HostKind != GridRegionHostKind.Pane)
            {
                throw new InvalidOperationException(state.RegionKind + " must resolve to a pane host in WPF.");
            }

            var renderCollapsedRail = state.State == GridRegionState.Collapsed;
            var isRenderable = state.IsAvailable && state.State != GridRegionState.Closed && contentAvailable;
            binding.Host.Visibility = isRenderable ? Visibility.Visible : Visibility.Collapsed;
            binding.SplitterColumn.Width = isRenderable && !renderCollapsedRail ? new GridLength(12d) : new GridLength(0d);
            binding.RegionColumn.MinWidth = isRenderable ? (renderCollapsedRail ? CollapsedRailWidth : (state.MinSize ?? binding.FallbackExpandedSize)) : 0d;
            binding.Host.MinWidth = isRenderable ? (renderCollapsedRail ? CollapsedRailWidth : (state.MinSize ?? binding.FallbackExpandedSize)) : 0d;
            Panel.SetZIndex(binding.Host, state.IsActive ? 1 : 0);

            if (!isRenderable)
            {
                binding.RegionColumn.Width = new GridLength(0d);
                binding.CollapsedRail.Visibility = Visibility.Collapsed;
                binding.ExpandedCard.Visibility = Visibility.Collapsed;
                binding.ContentHost.Visibility = Visibility.Collapsed;
                _hasAppliedDrawerState = false;
                return new WpfGridRegionChromeState(
                    state.RegionKind,
                    state.State,
                    false,
                    false,
                    false,
                    ResolveToggleText(state));
            }

            var expandedSize = ClampSize(state.Size ?? binding.FallbackExpandedSize, state.MinSize, state.MaxSize);
            binding.RegionColumn.Width = new GridLength(renderCollapsedRail ? CollapsedRailWidth : expandedSize);
            binding.CollapsedRail.Visibility = renderCollapsedRail ? Visibility.Visible : Visibility.Collapsed;
            binding.ExpandedCard.Visibility = renderCollapsedRail ? Visibility.Collapsed : Visibility.Visible;
            binding.ContentHost.Visibility = state.State == GridRegionState.Collapsed ? Visibility.Collapsed : Visibility.Visible;

            if (useDrawerChrome && !renderCollapsedRail)
            {
                if (!_hasAppliedDrawerState || _lastDrawerCollapsed)
                {
                    binding.ExpandedCardTransform.X = 28d;
                    binding.ExpandedCard.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation
                    {
                        From = 0d,
                        To = 1d,
                        Duration = TimeSpan.FromMilliseconds(180),
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                    }, HandoffBehavior.SnapshotAndReplace);
                    binding.ExpandedCardTransform.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation
                    {
                        From = 28d,
                        To = 0d,
                        Duration = TimeSpan.FromMilliseconds(180),
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                    }, HandoffBehavior.SnapshotAndReplace);
                }
            }
            else
            {
                binding.ExpandedCard.BeginAnimation(UIElement.OpacityProperty, null);
                binding.ExpandedCardTransform.BeginAnimation(TranslateTransform.XProperty, null);
                binding.ExpandedCard.Opacity = 1d;
                binding.ExpandedCardTransform.X = 0d;
            }

            _hasAppliedDrawerState = true;
            _lastDrawerCollapsed = renderCollapsedRail;

            return new WpfGridRegionChromeState(
                state.RegionKind,
                state.State,
                true,
                state.CanCollapse && isRenderable,
                state.CanClose && isRenderable,
                ResolveToggleText(state));
        }

        private static string ResolveToggleText(GridRegionViewState state)
        {
            return state.State == GridRegionState.Collapsed ? "<" : ">";
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
