using System;

namespace PhialeGrid.Core.Query
{
    public sealed class GridFilterDescriptor
    {
        public GridFilterDescriptor(string columnId, GridFilterOperator @operator, object value = null, object secondValue = null, Func<object, bool> customPredicate = null)
        {
            if (string.IsNullOrWhiteSpace(columnId))
            {
                throw new ArgumentException("Column id is required.", nameof(columnId));
            }

            ColumnId = columnId;
            Operator = @operator;
            Value = value;
            SecondValue = secondValue;
            CustomPredicate = customPredicate;
        }

        public string ColumnId { get; }

        public GridFilterOperator Operator { get; }

        public object Value { get; }

        public object SecondValue { get; }

        public Func<object, bool> CustomPredicate { get; }
    }
}
