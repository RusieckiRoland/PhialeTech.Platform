using PhialeGrid.Core.Summaries;

namespace PhialeTech.Components.Shared.ViewModels
{
    public sealed class DemoConfiguredSummaryViewModel
    {
        public DemoConfiguredSummaryViewModel(string columnId, GridSummaryType type, string label)
        {
            ColumnId = columnId ?? string.Empty;
            Type = type;
            Label = label ?? string.Empty;
        }

        public string ColumnId { get; }

        public GridSummaryType Type { get; }

        public string Label { get; }
    }
}
