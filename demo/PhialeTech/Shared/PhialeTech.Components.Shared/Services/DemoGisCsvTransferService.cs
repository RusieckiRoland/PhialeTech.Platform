using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PhialeGrid.Core.Columns;
using PhialeGrid.Core.Export;
using PhialeTech.Components.Shared.Model;

namespace PhialeTech.Components.Shared.Services
{
    public static class DemoGisCsvTransferService
    {
        public static string BuildSampleCsv(IReadOnlyList<DemoGisRecordViewModel> sourceRows, IReadOnlyList<GridColumnDefinition> columns, int take = 12)
        {
            if (sourceRows == null)
            {
                throw new ArgumentNullException(nameof(sourceRows));
            }

            if (columns == null)
            {
                throw new ArgumentNullException(nameof(columns));
            }

            return Export(sourceRows.Take(Math.Max(1, take)).ToArray(), columns.Where(column => column.IsVisible).ToArray());
        }

        public static string Export(IReadOnlyList<DemoGisRecordViewModel> rows, IReadOnlyList<GridColumnDefinition> columns)
        {
            if (rows == null)
            {
                throw new ArgumentNullException(nameof(rows));
            }

            if (columns == null)
            {
                throw new ArgumentNullException(nameof(columns));
            }

            var visibleColumns = columns.Where(column => column.IsVisible).ToArray();
            var headerToColumnId = BuildHeaderMap(visibleColumns);
            var headers = visibleColumns.Select(column => ResolveHeader(column, headerToColumnId)).ToArray();
            return GridCsvExporter.Export(
                rows,
                headers,
                (row, header) => ResolveExportValue(row, headerToColumnId[header]),
                new GridCsvOptions { IncludeHeader = true });
        }

        public static IReadOnlyList<DemoGisRecordViewModel> Import(string csv, IReadOnlyList<GridColumnDefinition> columns)
        {
            if (columns == null)
            {
                throw new ArgumentNullException(nameof(columns));
            }

            var importedRows = GridCsvImporter.Import(csv, new GridCsvOptions { HasHeaderOnImport = true });
            return importedRows
                .Select((row, index) => CreateRecord(row, columns, index))
                .ToArray();
        }

        private static IReadOnlyDictionary<string, string> BuildHeaderMap(IReadOnlyList<GridColumnDefinition> columns)
        {
            var duplicates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var column in columns)
            {
                if (!seen.Add(column.Header))
                {
                    duplicates.Add(column.Header);
                }
            }

            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var column in columns)
            {
                var header = duplicates.Contains(column.Header)
                    ? column.Header + " (" + column.Id + ")"
                    : column.Header;
                map[header] = column.Id;
            }

            return map;
        }

        private static string ResolveHeader(GridColumnDefinition column, IReadOnlyDictionary<string, string> headerMap)
        {
            foreach (var pair in headerMap)
            {
                if (string.Equals(pair.Value, column.Id, StringComparison.OrdinalIgnoreCase))
                {
                    return pair.Key;
                }
            }

            return column.Header;
        }

        private static object ResolveExportValue(DemoGisRecordViewModel row, string columnId)
        {
            switch (columnId)
            {
                case "Category":
                    return row.Category;
                case "ObjectName":
                    return row.ObjectName;
                case "ObjectId":
                    return row.ObjectId;
                case "GeometryType":
                    return row.GeometryType;
                case "Municipality":
                    return row.Municipality;
                case "District":
                    return row.District;
                case "Status":
                    return row.Status;
                case "Priority":
                    return row.Priority;
                case "AreaSquareMeters":
                    return row.AreaSquareMeters;
                case "LengthMeters":
                    return row.LengthMeters;
                case "LastInspection":
                    return row.LastInspection.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                case "Owner":
                    return row.Owner;
                default:
                    return string.Empty;
            }
        }

        private static DemoGisRecordViewModel CreateRecord(IReadOnlyDictionary<string, string> row, IReadOnlyList<GridColumnDefinition> columns, int index)
        {
            var objectId = ResolveColumnValue(row, columns, "ObjectId");
            if (string.IsNullOrWhiteSpace(objectId))
            {
                objectId = "CSV-IMPORTED-" + (index + 1).ToString("0000", CultureInfo.InvariantCulture);
            }

            return new DemoGisRecordViewModel(
                objectId,
                ResolveColumnValue(row, columns, "Category"),
                ResolveColumnValue(row, columns, "ObjectName"),
                ResolveColumnValue(row, columns, "GeometryType"),
                "EPSG:2180",
                ResolveColumnValue(row, columns, "Municipality"),
                ResolveColumnValue(row, columns, "District"),
                ResolveColumnValue(row, columns, "Status"),
                ParseDecimal(ResolveColumnValue(row, columns, "AreaSquareMeters")),
                ParseDecimal(ResolveColumnValue(row, columns, "LengthMeters")),
                ParseDate(ResolveColumnValue(row, columns, "LastInspection")),
                "CsvImport",
                ResolveColumnValue(row, columns, "Priority"),
                visible: true,
                editableFlag: true,
                owner: ResolveColumnValue(row, columns, "Owner"),
                scaleHint: 1000,
                tags: string.Empty);
        }

        private static string ResolveColumnValue(IReadOnlyDictionary<string, string> row, IReadOnlyList<GridColumnDefinition> columns, string columnId)
        {
            if (row.TryGetValue(columnId, out var value))
            {
                return value ?? string.Empty;
            }

            var column = columns.FirstOrDefault(candidate => string.Equals(candidate.Id, columnId, StringComparison.OrdinalIgnoreCase));
            if (column != null && row.TryGetValue(column.Header, out value))
            {
                return value ?? string.Empty;
            }

            foreach (var pair in row)
            {
                if (string.Equals(NormalizeHeader(pair.Key), NormalizeHeader(column?.Header), StringComparison.OrdinalIgnoreCase))
                {
                    return pair.Value ?? string.Empty;
                }
            }

            return string.Empty;
        }

        private static string NormalizeHeader(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Replace("(", string.Empty).Replace(")", string.Empty).Replace("[", string.Empty).Replace("]", string.Empty).Trim();
        }

        private static decimal ParseDecimal(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return 0m;
            }

            if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var invariant))
            {
                return invariant;
            }

            if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out var current))
            {
                return current;
            }

            return 0m;
        }

        private static DateTime ParseDate(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return DateTime.Today;
            }

            if (DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var isoDate))
            {
                return isoDate;
            }

            if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var invariantDate))
            {
                return invariantDate;
            }

            if (DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.None, out var currentDate))
            {
                return currentDate;
            }

            return DateTime.Today;
        }
    }
}

