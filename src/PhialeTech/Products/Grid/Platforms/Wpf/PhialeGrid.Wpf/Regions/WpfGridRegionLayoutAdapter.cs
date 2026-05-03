using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using PhialeGrid.Core.Regions;

namespace PhialeTech.PhialeGrid.Wpf.Regions
{
    internal sealed class WpfGridRegionLayoutAdapter
    {
        private readonly IReadOnlyList<WpfGridWorkspaceBandBinding> _workspaceBandBindings;
        private readonly IReadOnlyList<WpfGridWorkspacePanelBinding> _workspacePanelBindings;
        private readonly WpfGridWorkspaceBandPresenter _workspaceBandPresenter = new WpfGridWorkspaceBandPresenter();
        private readonly WpfGridWorkspacePanelPresenter _workspacePanelPresenter = new WpfGridWorkspacePanelPresenter();
        private readonly Dictionary<GridRegionKind, WpfGridRegionChromeState> _chromeStateByKind = new Dictionary<GridRegionKind, WpfGridRegionChromeState>();

        internal WpfGridRegionLayoutAdapter(
            IReadOnlyList<WpfGridWorkspaceBandBinding> workspaceBandBindings,
            WpfGridWorkspacePanelBinding workspacePanelBinding)
            : this(workspaceBandBindings, new[] { workspacePanelBinding })
        {
        }

        internal WpfGridRegionLayoutAdapter(
            IReadOnlyList<WpfGridWorkspaceBandBinding> workspaceBandBindings,
            IReadOnlyList<WpfGridWorkspacePanelBinding> workspacePanelBindings)
        {
            _workspaceBandBindings = workspaceBandBindings ?? throw new ArgumentNullException(nameof(workspaceBandBindings));
            _workspacePanelBindings = workspacePanelBindings ?? throw new ArgumentNullException(nameof(workspacePanelBindings));
            if (_workspacePanelBindings.Count == 0)
            {
                throw new ArgumentException("At least one workspace panel binding is required.", nameof(workspacePanelBindings));
            }
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

            foreach (var band in _workspaceBandBindings)
            {
                EnsureRequiredState(stateByKind, band.RegionKind);
                if (!renderSnapshot.ContentAvailability.ContainsKey(band.RegionKind))
                {
                    throw new InvalidOperationException("Missing WPF content-availability state for " + band.RegionKind + ".");
                }

                if (!renderSnapshot.Directives.ContainsKey(band.RegionKind))
                {
                    throw new InvalidOperationException("Missing WPF render directives for " + band.RegionKind + ".");
                }

                _chromeStateByKind[band.RegionKind] = _workspaceBandPresenter.Apply(
                    band,
                    stateByKind[band.RegionKind],
                    renderSnapshot.ContentAvailability[band.RegionKind],
                    renderSnapshot.Directives[band.RegionKind]);
            }

            ResetDedicatedWorkspacePanelColumns();
            var placementsWithOpenWorkspacePanel = stateByKind.Values
                .Where(state => state.HostKind == GridRegionHostKind.WorkspacePanel && state.State == GridRegionState.Open)
                .Select(state => state.Placement)
                .ToArray();
            var placementsWithActiveOpenWorkspacePanel = stateByKind.Values
                .Where(state => state.HostKind == GridRegionHostKind.WorkspacePanel && state.State == GridRegionState.Open && state.IsActive)
                .Select(state => state.Placement)
                .ToArray();

            var panelRenderPlans = _workspacePanelBindings
                .Select(panel =>
                {
                    EnsureRequiredState(stateByKind, panel.RegionKind);
                    if (!renderSnapshot.ContentAvailability.ContainsKey(panel.RegionKind))
                    {
                        throw new InvalidOperationException("Missing WPF content-availability state for " + panel.RegionKind + ".");
                    }

                    var panelState = stateByKind[panel.RegionKind];
                    var renderPanelContent = renderSnapshot.ContentAvailability[panel.RegionKind] &&
                        !ShouldSuppressCollapsedWorkspacePanel(panelState, placementsWithOpenWorkspacePanel) &&
                        !ShouldSuppressInactiveOpenWorkspacePanel(panelState, placementsWithActiveOpenWorkspacePanel);
                    return new WorkspacePanelRenderPlan(panel, panelState, renderPanelContent);
                })
                .OrderBy(plan => plan.RenderContent ? 1 : 0)
                .ThenBy(plan => plan.State.IsActive ? 1 : 0)
                .ToArray();

            foreach (var plan in panelRenderPlans)
            {
                _chromeStateByKind[plan.Binding.RegionKind] = _workspacePanelPresenter.Apply(
                    plan.Binding,
                    plan.State,
                    plan.RenderContent,
                    renderSnapshot.UseWorkspacePanelDrawerChrome);
            }
        }

        private static bool ShouldSuppressCollapsedWorkspacePanel(
            GridRegionViewState state,
            IReadOnlyList<GridRegionPlacement> placementsWithOpenWorkspacePanel)
        {
            return state.HostKind == GridRegionHostKind.WorkspacePanel &&
                state.State == GridRegionState.Collapsed &&
                placementsWithOpenWorkspacePanel.Contains(state.Placement);
        }

        private static bool ShouldSuppressInactiveOpenWorkspacePanel(
            GridRegionViewState state,
            IReadOnlyList<GridRegionPlacement> placementsWithActiveOpenWorkspacePanel)
        {
            return state.HostKind == GridRegionHostKind.WorkspacePanel &&
                state.State == GridRegionState.Open &&
                !state.IsActive &&
                placementsWithActiveOpenWorkspacePanel.Contains(state.Placement);
        }

        private void ResetDedicatedWorkspacePanelColumns()
        {
            var visited = new HashSet<ColumnDefinition>();
            foreach (var panel in _workspacePanelBindings)
            {
                if (!panel.UsesDedicatedSideColumns)
                {
                    continue;
                }

                Reset(panel.LeftRegionColumn, visited);
                Reset(panel.LeftSplitterColumn, visited);
                Reset(panel.RightSplitterColumn, visited);
                Reset(panel.RightRegionColumn, visited);
            }
        }

        private static void Reset(ColumnDefinition column, ISet<ColumnDefinition> visited)
        {
            if (column == null || visited.Contains(column))
            {
                return;
            }

            visited.Add(column);
            column.Width = new GridLength(0d);
            column.MinWidth = 0d;
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

        private sealed class WorkspacePanelRenderPlan
        {
            internal WorkspacePanelRenderPlan(
                WpfGridWorkspacePanelBinding binding,
                GridRegionViewState state,
                bool renderContent)
            {
                Binding = binding ?? throw new ArgumentNullException(nameof(binding));
                State = state ?? throw new ArgumentNullException(nameof(state));
                RenderContent = renderContent;
            }

            internal WpfGridWorkspacePanelBinding Binding { get; }

            internal GridRegionViewState State { get; }

            internal bool RenderContent { get; }
        }
    }
}
