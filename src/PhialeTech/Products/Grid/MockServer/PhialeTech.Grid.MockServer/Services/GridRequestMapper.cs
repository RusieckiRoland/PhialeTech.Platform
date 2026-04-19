using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using PhialeGrid.Core.Query;
using PhialeGrid.Core.Summaries;
using PhialeGrid.MockServer.Contracts;

namespace PhialeGrid.MockServer.Services
{
    public sealed class GridRequestMapper
    {
        private readonly GridQuerySchema _schema;

        public GridRequestMapper(GridQuerySchema schema)
        {
            _schema = schema ?? throw new ArgumentNullException(nameof(schema));
        }

        public GridQueryRequest Map(GridQueryHttpRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            return new GridQueryRequest(
                request.Offset,
                request.Size,
                MapSorts(request.Sorts),
                MapFilterGroup(request.FilterGroup),
                MapGroups(request.Groups),
                MapSummaries(request.Summaries));
        }

        public GridGroupedQueryRequest Map(GridGroupedQueryHttpRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var expansionState = new GridGroupExpansionState();
            foreach (var groupId in request.CollapsedGroupIds ?? Enumerable.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(groupId))
                {
                    continue;
                }

                expansionState.SetExpanded(groupId, false);
            }

            return new GridGroupedQueryRequest(
                request.Offset,
                request.Size,
                MapSorts(request.Sorts),
                MapFilterGroup(request.FilterGroup),
                MapGroups(request.Groups),
                MapSummaries(request.Summaries),
                expansionState);
        }

        private IReadOnlyList<GridSortDescriptor> MapSorts(IReadOnlyList<GridSortHttpDescriptor> sorts)
        {
            if (sorts == null || sorts.Count == 0)
            {
                return Array.Empty<GridSortDescriptor>();
            }

            return sorts
                .Select(sort => new GridSortDescriptor(
                    sort.ColumnId,
                    ParseEnum<GridSortDirection>(sort.Direction, nameof(sort.Direction))))
                .ToArray();
        }

        private IReadOnlyList<GridGroupDescriptor> MapGroups(IReadOnlyList<GridGroupHttpDescriptor> groups)
        {
            if (groups == null || groups.Count == 0)
            {
                return Array.Empty<GridGroupDescriptor>();
            }

            return groups
                .Select(group => new GridGroupDescriptor(
                    group.ColumnId,
                    ParseEnum<GridSortDirection>(group.Direction, nameof(group.Direction))))
                .ToArray();
        }

        private IReadOnlyList<GridSummaryDescriptor> MapSummaries(IReadOnlyList<GridSummaryHttpDescriptor> summaries)
        {
            if (summaries == null || summaries.Count == 0)
            {
                return Array.Empty<GridSummaryDescriptor>();
            }

            return summaries
                .Select(summary =>
                {
                    var type = ParseEnum<GridSummaryType>(summary.Type, nameof(summary.Type));
                    if (type == GridSummaryType.Custom)
                    {
                        throw new InvalidOperationException("Custom summaries are not supported over HTTP.");
                    }

                    return new GridSummaryDescriptor(summary.ColumnId, type, _schema.GetColumnType(summary.ColumnId));
                })
                .ToArray();
        }

        private GridFilterGroup MapFilterGroup(GridFilterGroupHttpDescriptor filterGroup)
        {
            if (filterGroup == null || filterGroup.Filters == null || filterGroup.Filters.Count == 0)
            {
                return GridFilterGroup.EmptyAnd();
            }

            var logicalOperator = ParseEnum<GridLogicalOperator>(filterGroup.LogicalOperator, nameof(filterGroup.LogicalOperator));
            var filters = filterGroup.Filters
                .Select(MapFilter)
                .ToArray();

            return new GridFilterGroup(filters, logicalOperator);
        }

        private GridFilterDescriptor MapFilter(GridFilterHttpDescriptor filter)
        {
            if (filter == null)
            {
                throw new InvalidOperationException("Filter entry cannot be null.");
            }

            var filterOperator = ParseEnum<GridFilterOperator>(filter.Operator, nameof(filter.Operator));
            if (filterOperator == GridFilterOperator.Custom)
            {
                throw new InvalidOperationException("Custom filters are not supported over HTTP.");
            }

            return new GridFilterDescriptor(
                filter.ColumnId,
                filterOperator,
                ConvertValue(filter.ColumnId, filter.Value),
                ConvertValue(filter.ColumnId, filter.SecondValue));
        }

        private object ConvertValue(string columnId, object value)
        {
            if (value == null)
            {
                return null;
            }

            var targetType = _schema.GetColumnType(columnId);
            if (value is JsonElement jsonElement)
            {
                return ConvertJsonElement(targetType, jsonElement);
            }

            if (targetType == typeof(object) || targetType.IsInstanceOfType(value))
            {
                return value;
            }

            return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
        }

        private static object ConvertJsonElement(Type targetType, JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Null || element.ValueKind == JsonValueKind.Undefined)
            {
                return null;
            }

            if (targetType == typeof(string))
            {
                return element.ValueKind == JsonValueKind.String ? element.GetString() : element.ToString();
            }

            if (targetType == typeof(decimal))
            {
                return element.ValueKind == JsonValueKind.Number
                    ? element.GetDecimal()
                    : decimal.Parse(element.ToString(), NumberStyles.Number, CultureInfo.InvariantCulture);
            }

            if (targetType == typeof(int))
            {
                return element.ValueKind == JsonValueKind.Number
                    ? element.GetInt32()
                    : int.Parse(element.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture);
            }

            if (targetType == typeof(bool))
            {
                return element.ValueKind == JsonValueKind.True || element.ValueKind == JsonValueKind.False
                    ? element.GetBoolean()
                    : bool.Parse(element.ToString());
            }

            if (targetType == typeof(DateTime))
            {
                if (element.ValueKind == JsonValueKind.String)
                {
                    return DateTime.Parse(element.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
                }

                return DateTime.Parse(element.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
            }

            return element.ValueKind == JsonValueKind.String
                ? Convert.ChangeType(element.GetString(), targetType, CultureInfo.InvariantCulture)
                : Convert.ChangeType(element.ToString(), targetType, CultureInfo.InvariantCulture);
        }

        private static TEnum ParseEnum<TEnum>(string rawValue, string argumentName)
            where TEnum : struct
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                throw new InvalidOperationException(argumentName + " is required.");
            }

            TEnum value;
            if (!Enum.TryParse(rawValue, true, out value))
            {
                throw new InvalidOperationException("Unsupported value '" + rawValue + "' for " + argumentName + ".");
            }

            return value;
        }
    }
}
