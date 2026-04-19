using System;
using System.Collections.Generic;
using System.Linq;
using PhialeGrid.Core.Columns;

namespace PhialeGrid.Core.Query
{
    public sealed class GridQuerySchema
    {
        private readonly Dictionary<string, Type> _columnTypes;

        public GridQuerySchema(IReadOnlyDictionary<string, Type> columnTypes)
        {
            if (columnTypes == null)
            {
                throw new ArgumentNullException(nameof(columnTypes));
            }

            _columnTypes = new Dictionary<string, Type>(StringComparer.Ordinal);
            foreach (var pair in columnTypes)
            {
                _columnTypes[pair.Key] = pair.Value;
            }
        }

        public static GridQuerySchema FromColumns(IReadOnlyList<GridColumnDefinition> columns)
        {
            if (columns == null)
            {
                throw new ArgumentNullException(nameof(columns));
            }

            return new GridQuerySchema(columns.ToDictionary(c => c.Id, c => c.ValueType, StringComparer.Ordinal));
        }

        public Type GetColumnType(string columnId)
        {
            Type valueType;
            return _columnTypes.TryGetValue(columnId, out valueType) ? valueType : typeof(object);
        }

        public object NormalizeValue(string columnId, object value)
        {
            if (value == null)
            {
                return null;
            }

            var targetType = GetColumnType(columnId);
            if (targetType == typeof(object) || targetType.IsInstanceOfType(value))
            {
                return value;
            }

            if (targetType.IsEnum)
            {
                return Enum.Parse(targetType, Convert.ToString(value));
            }

            return Convert.ChangeType(value, targetType, System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
