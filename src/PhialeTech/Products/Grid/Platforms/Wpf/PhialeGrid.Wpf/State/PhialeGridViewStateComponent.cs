using System;
using System.Windows.Threading;
using PhialeGrid.Core.State;
using PhialeTech.ComponentHost.Abstractions.State;
using PhialeTech.PhialeGrid.Wpf.Diagnostics;
using GridControl = PhialeTech.PhialeGrid.Wpf.Controls.PhialeGrid;

namespace PhialeTech.PhialeGrid.Wpf.State
{
    public sealed class PhialeGridViewStateComponent : IStatefulComponent<GridViewState>, IDisposable
    {
        private readonly GridControl _grid;
        private readonly DispatcherTimer _saveDebounceTimer;
        private bool _isApplyingState;

        public PhialeGridViewStateComponent(GridControl grid, TimeSpan? saveDebounce = null)
        {
            _grid = grid ?? throw new ArgumentNullException(nameof(grid));
            _saveDebounceTimer = new DispatcherTimer(DispatcherPriority.Background, _grid.Dispatcher)
            {
                Interval = saveDebounce ?? TimeSpan.FromMilliseconds(350d),
            };
            _saveDebounceTimer.Tick += HandleSaveDebounceTimerTick;
            _grid.ViewStateChanged += HandleGridViewStateChanged;
        }

        public event EventHandler StateChanged;

        public GridViewState ExportState()
        {
            PhialeGridDiagnostics.Write("PhialeGridViewStateComponent", $"ExportState requested for {DescribeGrid()}.");
            return _grid.ExportViewState();
        }

        public void ApplyState(GridViewState state)
        {
            if (state == null)
            {
                PhialeGridDiagnostics.Write("PhialeGridViewStateComponent", $"ApplyState skipped for {DescribeGrid()} because state is null.");
                return;
            }

            PhialeGridDiagnostics.Write("PhialeGridViewStateComponent", $"ApplyState started for {DescribeGrid()}. Columns={state.Columns?.Count ?? 0}, Groups={state.Groups?.Count ?? 0}, Sorts={state.Sorts?.Count ?? 0}.");
            _isApplyingState = true;
            try
            {
                _saveDebounceTimer.Stop();
                _grid.ApplyViewState(state);
            }
            finally
            {
                _isApplyingState = false;
                PhialeGridDiagnostics.Write("PhialeGridViewStateComponent", $"ApplyState finished for {DescribeGrid()}.");
            }
        }

        public void Dispose()
        {
            _saveDebounceTimer.Stop();
            _saveDebounceTimer.Tick -= HandleSaveDebounceTimerTick;
            _grid.ViewStateChanged -= HandleGridViewStateChanged;
        }

        private void HandleGridViewStateChanged(object sender, EventArgs e)
        {
            if (_isApplyingState)
            {
                PhialeGridDiagnostics.Write("PhialeGridViewStateComponent", $"StateChanged ignored for {DescribeGrid()} because state is currently being applied.");
                return;
            }

            PhialeGridDiagnostics.Write("PhialeGridViewStateComponent", $"StateChanged observed for {DescribeGrid()}. Starting debounce timer.");
            _saveDebounceTimer.Stop();
            _saveDebounceTimer.Start();
        }

        private void HandleSaveDebounceTimerTick(object sender, EventArgs e)
        {
            _saveDebounceTimer.Stop();
            PhialeGridDiagnostics.Write("PhialeGridViewStateComponent", $"Debounced StateChanged emitted for {DescribeGrid()}.");
            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        private string DescribeGrid()
        {
            var name = string.IsNullOrWhiteSpace(_grid.Name) ? "<unnamed>" : _grid.Name;
            return name + "#" + _grid.GetHashCode().ToString("X8");
        }
    }
}
