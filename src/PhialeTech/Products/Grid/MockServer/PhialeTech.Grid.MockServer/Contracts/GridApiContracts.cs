using System;
using System.Collections.Generic;

namespace PhialeGrid.MockServer.Contracts
{
    public sealed class GridQueryHttpRequest
    {
        public int Offset { get; set; } = 0;

        public int Size { get; set; } = 50;

        public List<GridSortHttpDescriptor> Sorts { get; set; } = new List<GridSortHttpDescriptor>();

        public GridFilterGroupHttpDescriptor FilterGroup { get; set; } = new GridFilterGroupHttpDescriptor();

        public List<GridGroupHttpDescriptor> Groups { get; set; } = new List<GridGroupHttpDescriptor>();

        public List<GridSummaryHttpDescriptor> Summaries { get; set; } = new List<GridSummaryHttpDescriptor>();
    }

    public sealed class GridGroupedQueryHttpRequest
    {
        public int Offset { get; set; } = 0;

        public int Size { get; set; } = 50;

        public List<GridSortHttpDescriptor> Sorts { get; set; } = new List<GridSortHttpDescriptor>();

        public GridFilterGroupHttpDescriptor FilterGroup { get; set; } = new GridFilterGroupHttpDescriptor();

        public List<GridGroupHttpDescriptor> Groups { get; set; } = new List<GridGroupHttpDescriptor>();

        public List<GridSummaryHttpDescriptor> Summaries { get; set; } = new List<GridSummaryHttpDescriptor>();

        public List<string> CollapsedGroupIds { get; set; } = new List<string>();
    }

    public sealed class GridSortHttpDescriptor
    {
        public string ColumnId { get; set; } = string.Empty;

        public string Direction { get; set; } = "Ascending";
    }

    public sealed class GridGroupHttpDescriptor
    {
        public string ColumnId { get; set; } = string.Empty;

        public string Direction { get; set; } = "Ascending";
    }

    public sealed class GridSummaryHttpDescriptor
    {
        public string ColumnId { get; set; } = string.Empty;

        public string Type { get; set; } = "Count";
    }

    public sealed class GridFilterGroupHttpDescriptor
    {
        public string LogicalOperator { get; set; } = "And";

        public List<GridFilterHttpDescriptor> Filters { get; set; } = new List<GridFilterHttpDescriptor>();
    }

    public sealed class GridFilterHttpDescriptor
    {
        public string ColumnId { get; set; } = string.Empty;

        public string Operator { get; set; } = "Equals";

        public object Value { get; set; }

        public object SecondValue { get; set; }
    }

    public sealed class GridSchemaHttpResponse
    {
        public IReadOnlyList<GridColumnHttpDescriptor> Columns { get; set; } = Array.Empty<GridColumnHttpDescriptor>();

        public int TotalRecordCount { get; set; }
    }

    public sealed class GridColumnHttpDescriptor
    {
        public string Id { get; set; } = string.Empty;

        public string Header { get; set; } = string.Empty;

        public string ValueType { get; set; } = string.Empty;

        public double Width { get; set; }

        public bool IsEditable { get; set; }

        public bool IsVisible { get; set; }
    }

    public sealed class GridQueryHttpResponse
    {
        public int Offset { get; set; }

        public int Size { get; set; }

        public int ReturnedCount { get; set; }

        public int TotalCount { get; set; }

        public IReadOnlyDictionary<string, object> Summary { get; set; } = new Dictionary<string, object>();

        public IReadOnlyList<DemoGisRecordHttpDto> Items { get; set; } = Array.Empty<DemoGisRecordHttpDto>();
    }

    public sealed class GridGroupedQueryHttpResponse
    {
        public int Offset { get; set; }

        public int Size { get; set; }

        public int ReturnedRowCount { get; set; }

        public int VisibleRowCount { get; set; }

        public int TotalItemCount { get; set; }

        public int TopLevelGroupCount { get; set; }

        public IReadOnlyList<string> GroupIds { get; set; } = Array.Empty<string>();

        public IReadOnlyDictionary<string, object> Summary { get; set; } = new Dictionary<string, object>();

        public IReadOnlyList<GridGroupedRowHttpDto> Rows { get; set; } = Array.Empty<GridGroupedRowHttpDto>();
    }

    public sealed class GridGroupedRowHttpDto
    {
        public string Kind { get; set; } = string.Empty;

        public int Level { get; set; }

        public string GroupId { get; set; }

        public string GroupColumnId { get; set; }

        public object GroupKey { get; set; }

        public int GroupItemCount { get; set; }

        public bool IsExpanded { get; set; }

        public DemoGisRecordHttpDto Item { get; set; }
    }

    public sealed class DemoGisRecordHttpDto
    {
        public string Id { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public string ObjectId { get; set; } = string.Empty;

        public string ObjectName { get; set; } = string.Empty;

        public string GeometryType { get; set; } = string.Empty;

        public string Crs { get; set; } = string.Empty;

        public string Municipality { get; set; } = string.Empty;

        public string District { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public decimal AreaSquareMeters { get; set; }

        public decimal LengthMeters { get; set; }

        public DateTime LastInspection { get; set; }

        public string Source { get; set; } = string.Empty;

        public string Priority { get; set; } = string.Empty;

        public bool Visible { get; set; }

        public bool EditableFlag { get; set; }

        public string Owner { get; set; } = string.Empty;

        public int ScaleHint { get; set; }

        public string Tags { get; set; } = string.Empty;
    }
}
