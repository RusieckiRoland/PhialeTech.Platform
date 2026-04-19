using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using PhialeGrid.Core.Columns;
using PhialeGrid.Core.Query;
using PhialeGrid.Core.Regions;
using PhialeGrid.Core.Rendering;
using PhialeGrid.Core.Summaries;

namespace PhialeGrid.Core.State
{
    public static class GridStateCodec
    {
        private const string NullToken = "~";
        private const string Version7Prefix = "v7";

        public static string Encode(GridStateSnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            var columns = string.Join(";", snapshot.Layout.Columns.Select(c => string.Join(",",
                EncodeText(c.Id),
                c.DisplayIndex.ToString(CultureInfo.InvariantCulture),
                c.Width.ToString(CultureInfo.InvariantCulture),
                c.IsVisible ? "1" : "0",
                c.IsFrozen ? "1" : "0",
                EncodeText(c.ValueType.AssemblyQualifiedName ?? c.ValueType.FullName ?? typeof(object).AssemblyQualifiedName))));

            var sorts = string.Join(";", snapshot.Sorts.Select(s => string.Join(",",
                EncodeText(s.ColumnId),
                ((int)s.Direction).ToString(CultureInfo.InvariantCulture))));

            var filters = string.Join(";", new[]
            {
                ((int)snapshot.Filters.LogicalOperator).ToString(CultureInfo.InvariantCulture)
            }.Concat(snapshot.Filters.Filters.Select(f => string.Join(",",
                EncodeText(f.ColumnId),
                ((int)f.Operator).ToString(CultureInfo.InvariantCulture),
                EncodeObject(f.Value),
                EncodeObject(f.SecondValue)))));

            var groups = string.Join(";", snapshot.Groups.Select(g => string.Join(",",
                EncodeText(g.ColumnId),
                ((int)g.Direction).ToString(CultureInfo.InvariantCulture))));

            var summaries = string.Join(";", snapshot.Summaries.Select(s => string.Join(",",
                EncodeText(s.ColumnId),
                ((int)s.Type).ToString(CultureInfo.InvariantCulture))));

            var regions = string.Join(";", snapshot.RegionLayout.Regions.Select(r => string.Join(",",
                ((int)r.RegionKind).ToString(CultureInfo.InvariantCulture),
                ((int)r.State).ToString(CultureInfo.InvariantCulture),
                r.Size.HasValue ? r.Size.Value.ToString(CultureInfo.InvariantCulture) : NullToken,
                r.IsActive ? "1" : "0")));

            var globalSearch = EncodeText(snapshot.GlobalSearchText);
            var options = string.Join(",",
                EncodeNullableBoolean(snapshot.SelectCurrentRow),
                EncodeNullableBoolean(snapshot.MultiSelect),
                EncodeNullableBoolean(snapshot.ShowRowNumbers),
                snapshot.RowNumberingMode.HasValue
                    ? ((int)snapshot.RowNumberingMode.Value).ToString(CultureInfo.InvariantCulture)
                    : NullToken);

            return string.Join("|", new[] { Version7Prefix, columns, sorts, filters, groups, summaries, regions, globalSearch, options });
        }

        public static GridStateSnapshot Decode(string encoded, IReadOnlyList<GridColumnDefinition> baselineColumns)
        {
            if (baselineColumns == null)
            {
                throw new ArgumentNullException(nameof(baselineColumns));
            }

            var parts = (encoded ?? string.Empty).Split('|');
            if (parts.Length == 0 || !string.Equals(parts[0], Version7Prefix, StringComparison.Ordinal))
            {
                throw new NotSupportedException("Unsupported grid state payload version. Only v7 is supported.");
            }

            var columns = DecodeColumns(parts.ElementAtOrDefault(1), baselineColumns);
            var sorts = DecodeSorts(parts.ElementAtOrDefault(2));
            var filters = DecodeFilters(parts.ElementAtOrDefault(3));
            var groups = DecodeGroups(parts.ElementAtOrDefault(4));
            var summaries = DecodeSummaries(parts.ElementAtOrDefault(5));
            var regions = DecodeRegions(parts.ElementAtOrDefault(6));
            var globalSearch = DecodeText(parts.ElementAtOrDefault(7));
            var options = DecodeOptions(parts.ElementAtOrDefault(8));
            return new GridStateSnapshot(
                new GridLayoutSnapshot(columns),
                sorts,
                filters,
                groups,
                summaries,
                regions,
                globalSearch,
                options.selectCurrentRow,
                options.multiSelect,
                options.showRowNumbers,
                options.rowNumberingMode);
        }

        private static (bool? selectCurrentRow, bool? multiSelect, bool? showRowNumbers, GridRowNumberingMode? rowNumberingMode) DecodeOptions(string encoded)
        {
            var parts = (encoded ?? string.Empty).Split(',');
            return (
                DecodeNullableBoolean(parts.ElementAtOrDefault(0)),
                DecodeNullableBoolean(parts.ElementAtOrDefault(1)),
                DecodeNullableBoolean(parts.ElementAtOrDefault(2)),
                DecodeNullableRowNumberingMode(parts.ElementAtOrDefault(3)));
        }

        private static IReadOnlyList<GridColumnDefinition> DecodeColumns(string encoded, IReadOnlyList<GridColumnDefinition> baseline)
        {
            var map = CreateBaselineColumnMap(baseline);

            foreach (var token in SplitEntries(encoded))
            {
                var item = token.Split(',');
                if (item.Length < 6)
                {
                    continue;
                }

                var columnId = DecodeText(item[0]);
                if (!map.ContainsKey(columnId))
                {
                    continue;
                }

                var typeName = DecodeText(item[5]);
                var valueType = string.IsNullOrWhiteSpace(typeName)
                    ? map[columnId].ValueType
                    : (Type.GetType(typeName, false) ?? map[columnId].ValueType);
                map[columnId] = map[columnId]
                    .WithDisplayIndex(int.Parse(item[1], CultureInfo.InvariantCulture))
                    .WithWidth(double.Parse(item[2], CultureInfo.InvariantCulture))
                    .WithVisibility(item[3] == "1")
                    .WithFrozen(item[4] == "1")
                    .WithValueType(valueType);
            }

            return map.Values.OrderBy(c => c.DisplayIndex).ToArray();
        }

        private static IReadOnlyList<GridSortDescriptor> DecodeSorts(string encoded)
        {
            return SplitEntries(encoded)
                .Select(x => x.Split(','))
                .Where(x => x.Length >= 2)
                .Select(x => new GridSortDescriptor(
                    DecodeText(x[0]),
                    (GridSortDirection)int.Parse(x[1], CultureInfo.InvariantCulture)))
                .ToArray();
        }

        private static GridFilterGroup DecodeFilters(string encoded)
        {
            var entries = SplitEntries(encoded).ToArray();
            if (entries.Length == 0)
            {
                return GridFilterGroup.EmptyAnd();
            }

            var logicalOperator = (GridLogicalOperator)int.Parse(entries[0], CultureInfo.InvariantCulture);
            var filters = entries
                .Skip(1)
                .Select(x => x.Split(','))
                .Where(x => x.Length >= 4)
                .Select(x => new GridFilterDescriptor(
                    DecodeText(x[0]),
                    (GridFilterOperator)int.Parse(x[1], CultureInfo.InvariantCulture),
                    DecodeObject(x[2]),
                    DecodeObject(x[3])))
                .ToArray();

            return new GridFilterGroup(filters, logicalOperator);
        }

        private static IReadOnlyList<GridGroupDescriptor> DecodeGroups(string encoded)
        {
            return SplitEntries(encoded)
                .Select(x => x.Split(','))
                .Where(x => x.Length >= 2)
                .Select(x => new GridGroupDescriptor(
                    DecodeText(x[0]),
                    (GridSortDirection)int.Parse(x[1], CultureInfo.InvariantCulture)))
                .ToArray();
        }

        private static IReadOnlyList<GridSummaryDescriptor> DecodeSummaries(string encoded)
        {
            return SplitEntries(encoded)
                .Select(x => x.Split(','))
                .Where(x => x.Length >= 2)
                .Select(x => new GridSummaryDescriptor(
                    DecodeText(x[0]),
                    (GridSummaryType)int.Parse(x[1], CultureInfo.InvariantCulture)))
                .ToArray();
        }

        private static GridRegionLayoutSnapshot DecodeRegions(string encoded)
        {
            return new GridRegionLayoutSnapshot(SplitEntries(encoded)
                .Select(x => x.Split(','))
                .Where(x => x.Length >= 4)
                .Select(x => new GridRegionLayoutState(
                    (GridRegionKind)int.Parse(x[0], CultureInfo.InvariantCulture),
                    (GridRegionState)int.Parse(x[1], CultureInfo.InvariantCulture),
                    x[2] == NullToken
                        ? (double?)null
                        : double.Parse(x[2], CultureInfo.InvariantCulture),
                    x[3] == "1"))
                .ToArray());
        }

        private static Dictionary<string, GridColumnDefinition> CreateBaselineColumnMap(IReadOnlyList<GridColumnDefinition> baseline)
        {
            return baseline.ToDictionary(c => c.Id, c => new GridColumnDefinition(
                c.Id,
                c.Header,
                c.Width,
                c.MinWidth,
                c.IsVisible,
                c.IsFrozen,
                c.IsEditable,
                c.DisplayIndex,
                c.ValueType,
                c.EditorKind,
                c.EditorItems,
                c.EditMask,
                c.ValueKind,
                c.ValidationConstraints,
                c.EditorItemsMode));
        }

        private static IEnumerable<string> SplitEntries(string encoded)
        {
            return string.IsNullOrWhiteSpace(encoded)
                ? Enumerable.Empty<string>()
                : encoded.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static string EncodeText(string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value ?? string.Empty);
            return Convert.ToBase64String(bytes);
        }

        private static string DecodeText(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var bytes = Convert.FromBase64String(value);
            return Encoding.UTF8.GetString(bytes);
        }

        private static string EncodeObject(object value)
        {
            if (value == null)
            {
                return NullToken;
            }

            return EncodeText(Convert.ToString(value, CultureInfo.InvariantCulture));
        }

        private static string EncodeNullableBoolean(bool? value)
        {
            if (!value.HasValue)
            {
                return NullToken;
            }

            return value.Value ? "1" : "0";
        }

        private static object DecodeObject(string value)
        {
            return value == NullToken ? null : (object)DecodeText(value);
        }

        private static bool? DecodeNullableBoolean(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value == NullToken)
            {
                return null;
            }

            if (value == "1")
            {
                return true;
            }

            if (value == "0")
            {
                return false;
            }

            return null;
        }

        private static GridRowNumberingMode? DecodeNullableRowNumberingMode(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value == NullToken)
            {
                return null;
            }

            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            {
                return (GridRowNumberingMode)parsed;
            }

            return null;
        }
    }
}


