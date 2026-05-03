using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using PhialeGrid.Core.Regions;

namespace PhialeTech.PhialeGrid.Wpf.Regions
{
    internal sealed class WpfGridWorkspacePanelPresenter
    {
        private bool _hasAppliedDrawerState;
        private bool _lastDrawerCollapsed;

        internal WpfGridRegionChromeState Apply(
            WpfGridWorkspacePanelBinding binding,
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

            if (state.HostKind != GridRegionHostKind.WorkspacePanel)
            {
                throw new InvalidOperationException(state.RegionKind + " must resolve to a workspace panel in WPF.");
            }

            var collapsedRailWidth = ResolveRequiredCollapsedRailWidth(binding.Host);
            var renderCollapsedRail = state.State == GridRegionState.Collapsed;
            var isRenderable = state.IsAvailable && state.State != GridRegionState.Closed && contentAvailable;
            ApplyPlacement(binding, state.Placement);
            binding.Host.Visibility = isRenderable ? Visibility.Visible : Visibility.Collapsed;
            var isHorizontalPlacement = state.Placement == GridRegionPlacement.Left || state.Placement == GridRegionPlacement.Right;
            var effectiveSplitterColumn = ResolveSplitterColumn(binding, state.Placement);
            effectiveSplitterColumn.Width = isRenderable && !renderCollapsedRail && isHorizontalPlacement ? new GridLength(12d) : new GridLength(0d);
            var effectiveRegionColumn = ResolveRegionColumn(binding, state.Placement);
            var effectiveSurfaceColumn = binding.UsesDedicatedSideColumns
                ? binding.SurfaceColumn
                : (state.Placement == GridRegionPlacement.Left ? binding.RegionColumn : binding.SurfaceColumn);
            effectiveRegionColumn.MinWidth = isRenderable ? (renderCollapsedRail ? collapsedRailWidth : (state.MinSize ?? binding.FallbackExpandedSize)) : 0d;
            effectiveSurfaceColumn.MinWidth = 0d;
            binding.Host.MinWidth = isRenderable ? (renderCollapsedRail ? collapsedRailWidth : (state.MinSize ?? binding.FallbackExpandedSize)) : 0d;
            Panel.SetZIndex(binding.Host, state.IsActive ? 1 : 0);

            if (!isRenderable)
            {
                effectiveRegionColumn.MinWidth = 0d;
                if (!binding.UsesDedicatedSideColumns)
                {
                    effectiveRegionColumn.Width = new GridLength(0d);
                    effectiveSurfaceColumn.Width = new GridLength(1d, GridUnitType.Star);
                }

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
            effectiveRegionColumn.Width = new GridLength(renderCollapsedRail ? collapsedRailWidth : expandedSize);
            effectiveSurfaceColumn.Width = new GridLength(1d, GridUnitType.Star);
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
            if (state.Placement == GridRegionPlacement.Left)
            {
                return state.State == GridRegionState.Collapsed ? ">" : "<";
            }

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

        private static void ApplyPlacement(WpfGridWorkspacePanelBinding binding, GridRegionPlacement placement)
        {
            switch (placement)
            {
                case GridRegionPlacement.Left:
                    Grid.SetColumn(binding.Host, binding.UsesDedicatedSideColumns ? 0 : 0);
                    Grid.SetColumn(binding.Splitter, binding.UsesDedicatedSideColumns ? 1 : 1);
                    SetSurfaceHostsColumn(binding, binding.UsesDedicatedSideColumns ? 2 : 2);
                    return;
                case GridRegionPlacement.Right:
                    SetSurfaceHostsColumn(binding, binding.UsesDedicatedSideColumns ? 2 : 0);
                    Grid.SetColumn(binding.Splitter, binding.UsesDedicatedSideColumns ? 3 : 1);
                    Grid.SetColumn(binding.Host, binding.UsesDedicatedSideColumns ? 4 : 2);
                    return;
                default:
                    throw new InvalidOperationException("WPF workspace panels currently support Left and Right placements.");
            }
        }

        private static ColumnDefinition ResolveRegionColumn(WpfGridWorkspacePanelBinding binding, GridRegionPlacement placement)
        {
            if (!binding.UsesDedicatedSideColumns)
            {
                return placement == GridRegionPlacement.Left ? binding.SurfaceColumn : binding.RegionColumn;
            }

            return placement == GridRegionPlacement.Left
                ? binding.LeftRegionColumn
                : binding.RightRegionColumn;
        }

        private static ColumnDefinition ResolveSplitterColumn(WpfGridWorkspacePanelBinding binding, GridRegionPlacement placement)
        {
            if (!binding.UsesDedicatedSideColumns)
            {
                return binding.SplitterColumn;
            }

            return placement == GridRegionPlacement.Left
                ? binding.LeftSplitterColumn
                : binding.RightSplitterColumn;
        }

        private static void SetSurfaceHostsColumn(WpfGridWorkspacePanelBinding binding, int column)
        {
            for (var index = 0; index < binding.SurfaceHosts.Count; index++)
            {
                Grid.SetColumn(binding.SurfaceHosts[index], column);
            }
        }

        private static double ResolveRequiredCollapsedRailWidth(FrameworkElement host)
        {
            if (host?.TryFindResource("PgGridSideToolCollapsedRailWidth") is double width)
            {
                return width;
            }

            throw new InvalidOperationException("Missing required grid resource 'PgGridSideToolCollapsedRailWidth'.");
        }
    }
}
