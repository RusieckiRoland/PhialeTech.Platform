using PhialeGrid.Core.Summaries;

namespace PhialeTech.Components.Shared.ViewModels
{
    public sealed class DemoConfiguredSummaryChipViewModel
    {
        public DemoConfiguredSummaryChipViewModel(string columnId, GridSummaryType type, string columnLabel, string typeLabel)
        {
            ColumnId = columnId ?? string.Empty;
            Type = type;
            ColumnLabel = columnLabel ?? string.Empty;
            TypeLabel = typeLabel ?? string.Empty;
            Label = ColumnLabel + " · " + TypeLabel;
        }

        public string ColumnId { get; }

        public GridSummaryType Type { get; }

        public string ColumnLabel { get; }

        public string TypeLabel { get; }

        public string Label { get; }
    }
}

