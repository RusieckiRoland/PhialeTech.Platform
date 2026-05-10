using System;
using System.Collections.Generic;

namespace PhialeGrid.Core.Details
{
    public sealed class GridRowDetailContext
    {
        public GridRowDetailContext(
            string rowKey,
            string recordKey,
            object record,
            IReadOnlyDictionary<string, object> values,
            IReadOnlyDictionary<string, GridRowDetailFieldContext> fields)
        {
            if (string.IsNullOrWhiteSpace(rowKey))
            {
                throw new ArgumentException("Row key is required.", nameof(rowKey));
            }

            if (string.IsNullOrWhiteSpace(recordKey))
            {
                throw new ArgumentException("Record key is required.", nameof(recordKey));
            }

            RowKey = rowKey;
            RecordKey = recordKey;
            Record = record ?? throw new ArgumentNullException(nameof(record));
            Values = values ?? throw new ArgumentNullException(nameof(values));
            Fields = fields ?? throw new ArgumentNullException(nameof(fields));
        }

        public string RowKey { get; }

        public string RecordKey { get; }

        public object Record { get; }

        public IReadOnlyDictionary<string, object> Values { get; }

        public IReadOnlyDictionary<string, GridRowDetailFieldContext> Fields { get; }
    }
}
