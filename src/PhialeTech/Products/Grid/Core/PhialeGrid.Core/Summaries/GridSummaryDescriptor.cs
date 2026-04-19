using System;
using System.Collections.Generic;
using System.Linq;

namespace PhialeGrid.Core.Summaries
{
    public sealed class GridSummaryDescriptor
    {
        private readonly Func<IReadOnlyList<object>, object> _customAggregator;

        public GridSummaryDescriptor(string columnId, GridSummaryType type, Func<object[], object> customAggregator = null)
            : this(columnId, type, typeof(object), customAggregator == null ? null : new Func<IReadOnlyList<object>, object>(values => customAggregator(values.ToArray())))
        {
        }

        public GridSummaryDescriptor(string columnId, GridSummaryType type, Type valueType, Func<IReadOnlyList<object>, object> customAggregator = null)
        {
            if (string.IsNullOrWhiteSpace(columnId))
            {
                throw new ArgumentException("Column id is required.", nameof(columnId));
            }

            ColumnId = columnId;
            Type = type;
            ValueType = valueType ?? typeof(object);
            _customAggregator = customAggregator;
        }

        public string ColumnId { get; }

        public GridSummaryType Type { get; }

        public Type ValueType { get; }

        public object AggregateCustom(IReadOnlyList<object> values)
        {
            if (_customAggregator == null)
            {
                throw new InvalidOperationException("Custom summary descriptor requires aggregator.");
            }

            return _customAggregator(values);
        }
    }
}
