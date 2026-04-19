using PhialeGrid.Core.Summaries;

namespace PhialeTech.Components.Shared.ViewModels
{
    public sealed class DemoSummaryTypeOptionViewModel
    {
        public DemoSummaryTypeOptionViewModel(GridSummaryType type, string displayName)
        {
            Type = type;
            DisplayName = displayName ?? string.Empty;
        }

        public GridSummaryType Type { get; }

        public string DisplayName { get; }
    }
}
