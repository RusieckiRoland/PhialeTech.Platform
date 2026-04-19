using System;
using System.Collections.Generic;
using PhialeGrid.Core.Columns;
using PhialeGrid.Core.Query;
using PhialeTech.Components.Shared.Model;
using PhialeTech.Components.Shared.Services;

namespace PhialeGrid.MockServer.Services
{
    public static class DemoGisGridDefinition
    {
        private static readonly IReadOnlyList<GridColumnDefinition> ColumnsInternal = new[]
        {
            new GridColumnDefinition("Category", "Category", width: 150d, displayIndex: 0, valueType: typeof(string), isEditable: false),
            new GridColumnDefinition("ObjectName", "Object name", width: 260d, displayIndex: 1, valueType: typeof(string), isEditable: true),
            new GridColumnDefinition("ObjectId", "Object ID", width: 180d, displayIndex: 2, valueType: typeof(string), isEditable: false),
            new GridColumnDefinition("GeometryType", "Geometry type", width: 130d, displayIndex: 3, valueType: typeof(string), isEditable: false),
            new GridColumnDefinition("Municipality", "Municipality", width: 130d, displayIndex: 4, valueType: typeof(string), isEditable: false),
            new GridColumnDefinition("District", "District", width: 140d, displayIndex: 5, valueType: typeof(string), isEditable: false),
            new GridColumnDefinition("Status", "Status", width: 150d, displayIndex: 6, valueType: typeof(string), isEditable: true),
            new GridColumnDefinition("Priority", "Priority", width: 120d, displayIndex: 7, valueType: typeof(string), isEditable: true),
            new GridColumnDefinition("AreaSquareMeters", "Area [m2]", width: 140d, displayIndex: 8, valueType: typeof(decimal), isEditable: false),
            new GridColumnDefinition("LengthMeters", "Length [m]", width: 140d, displayIndex: 9, valueType: typeof(decimal), isEditable: false),
            new GridColumnDefinition("LastInspection", "Last inspection", width: 150d, displayIndex: 10, valueType: typeof(DateTime), isEditable: false),
            new GridColumnDefinition("Owner", "Owner", width: 180d, displayIndex: 11, valueType: typeof(string), isEditable: true),
        };

        private static readonly IReadOnlyList<DemoGisRecordViewModel> RecordsInternal = DemoGisDataLoader.LoadDefaultRecords();

        private static readonly Dictionary<string, Func<DemoGisRecordViewModel, object>> ValueGetters =
            new Dictionary<string, Func<DemoGisRecordViewModel, object>>(StringComparer.Ordinal)
            {
                ["Category"] = row => row.Category,
                ["ObjectName"] = row => row.ObjectName,
                ["ObjectId"] = row => row.ObjectId,
                ["GeometryType"] = row => row.GeometryType,
                ["Municipality"] = row => row.Municipality,
                ["District"] = row => row.District,
                ["Status"] = row => row.Status,
                ["Priority"] = row => row.Priority,
                ["AreaSquareMeters"] = row => row.AreaSquareMeters,
                ["LengthMeters"] = row => row.LengthMeters,
                ["LastInspection"] = row => row.LastInspection,
                ["Owner"] = row => row.Owner,
            };

        public static IReadOnlyList<GridColumnDefinition> Columns => ColumnsInternal;

        public static IReadOnlyList<DemoGisRecordViewModel> Records => RecordsInternal;

        public static GridQuerySchema Schema { get; } = GridQuerySchema.FromColumns(ColumnsInternal);

        public static IGridRowAccessor<DemoGisRecordViewModel> Accessor { get; } =
            new DelegateGridRowAccessor<DemoGisRecordViewModel>((row, columnId) =>
            {
                Func<DemoGisRecordViewModel, object> getter;
                return ValueGetters.TryGetValue(columnId, out getter) ? getter(row) : null;
            });
    }
}
