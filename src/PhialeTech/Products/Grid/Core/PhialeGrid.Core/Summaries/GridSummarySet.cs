using System;
using System.Collections.Generic;
using System.Globalization;

namespace PhialeGrid.Core.Summaries
{
    public sealed class GridSummarySet
    {
        public static readonly GridSummarySet Empty = new GridSummarySet(new Dictionary<string, object>());

        public GridSummarySet(IReadOnlyDictionary<string, object> values)
        {
            Values = values ?? throw new ArgumentNullException(nameof(values));
        }

        public IReadOnlyDictionary<string, object> Values { get; }

        public object this[string key] => Values[key];

        public TValue GetValue<TValue>(string key)
        {
            return (TValue)Convert.ChangeType(Values[key], typeof(TValue), CultureInfo.InvariantCulture);
        }
    }
}
