using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PhialeGrid.Core.Query;

namespace PhialeGrid.Core.Summaries
{
    public static class GridSummaryEngine
    {
        public static GridSummarySet Calculate<T>(IReadOnlyList<T> rows, IReadOnlyList<GridSummaryDescriptor> descriptors, IGridRowAccessor<T> accessor)
        {
            return Calculate(rows, descriptors, accessor, null);
        }

        public static GridSummarySet Calculate<T>(IReadOnlyList<T> rows, IReadOnlyList<GridSummaryDescriptor> descriptors, IGridRowAccessor<T> accessor, GridQuerySchema schema)
        {
            if (rows == null)
            {
                throw new ArgumentNullException(nameof(rows));
            }

            if (descriptors == null)
            {
                throw new ArgumentNullException(nameof(descriptors));
            }

            if (accessor == null)
            {
                throw new ArgumentNullException(nameof(accessor));
            }

            var result = new Dictionary<string, object>();
            foreach (var descriptor in descriptors)
            {
                var key = BuildSummaryKey(descriptor);
                var values = rows
                    .Select(r => NormalizeValue(accessor.GetValue(r, descriptor.ColumnId), descriptor, schema))
                    .ToArray();
                result[key] = CalculateSummary(descriptor, values);
            }

            return new GridSummarySet(result);
        }

        public static GridSummaryAccumulator CreateAccumulator(IReadOnlyList<GridSummaryDescriptor> descriptors, GridQuerySchema schema)
        {
            return new GridSummaryAccumulator(descriptors, schema);
        }

        internal static string BuildSummaryKey(GridSummaryDescriptor descriptor)
        {
            return descriptor.ColumnId + ":" + descriptor.Type;
        }

        private static object NormalizeValue(object value, GridSummaryDescriptor descriptor, GridQuerySchema schema)
        {
            if (schema != null)
            {
                return schema.NormalizeValue(descriptor.ColumnId, value);
            }

            if (descriptor.ValueType != typeof(object) && value != null && !descriptor.ValueType.IsInstanceOfType(value))
            {
                return Convert.ChangeType(value, descriptor.ValueType, CultureInfo.InvariantCulture);
            }

            return value;
        }

        private static object CalculateSummary(GridSummaryDescriptor descriptor, object[] values)
        {
            switch (descriptor.Type)
            {
                case GridSummaryType.Count:
                    return values.Length;
                case GridSummaryType.Sum:
                    return values.Where(v => v != null).Sum(v => Convert.ToDecimal(v, CultureInfo.InvariantCulture));
                case GridSummaryType.Average:
                    var numeric = values.Where(v => v != null).Select(v => Convert.ToDecimal(v, CultureInfo.InvariantCulture)).ToArray();
                    return numeric.Length == 0 ? 0m : numeric.Average();
                case GridSummaryType.Min:
                    return values.Where(v => v != null).OrderBy(v => v, GridValueComparer.Instance).FirstOrDefault();
                case GridSummaryType.Max:
                    return values.Where(v => v != null).OrderBy(v => v, GridValueComparer.Instance).LastOrDefault();
                case GridSummaryType.Custom:
                    return descriptor.AggregateCustom(values);
                default:
                    throw new NotSupportedException("Unsupported summary type: " + descriptor.Type);
            }
        }
    }
}
