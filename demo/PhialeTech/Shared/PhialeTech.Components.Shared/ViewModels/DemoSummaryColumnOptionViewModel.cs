using System;
using System.Collections.Generic;
using System.Linq;
using PhialeGrid.Core.Summaries;

namespace PhialeTech.Components.Shared.ViewModels
{
    public sealed class DemoSummaryColumnOptionViewModel
    {
        public DemoSummaryColumnOptionViewModel(string columnId, string header, IReadOnlyList<GridSummaryType> allowedTypes)
        {
            ColumnId = columnId ?? string.Empty;
            Header = header ?? string.Empty;
            AllowedTypes = (allowedTypes ?? Array.Empty<GridSummaryType>()).Distinct().ToArray();
        }

        public string ColumnId { get; }

        public string Header { get; }

        public IReadOnlyList<GridSummaryType> AllowedTypes { get; }
    }
}
