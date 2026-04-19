using System;
using System.Collections.Generic;
using System.Globalization;
using PhialeGrid.Core.Query;

namespace PhialeGrid.Core.Summaries
{
    public sealed class GridSummaryAccumulator
    {
        private readonly GridQuerySchema _schema;
        private readonly Dictionary<string, SummaryState[]> _statesByColumnId;
        private readonly SummaryState[] _states;

        public GridSummaryAccumulator(IReadOnlyList<GridSummaryDescriptor> descriptors, GridQuerySchema schema)
        {
            _schema = schema;
            descriptors = descriptors ?? Array.Empty<GridSummaryDescriptor>();
            _states = new SummaryState[descriptors.Count];
            _statesByColumnId = new Dictionary<string, SummaryState[]>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < descriptors.Count; i++)
            {
                var state = new SummaryState(descriptors[i]);
                _states[i] = state;

                SummaryState[] existingStates;
                if (!_statesByColumnId.TryGetValue(state.ColumnId, out existingStates))
                {
                    _statesByColumnId[state.ColumnId] = new[] { state };
                    continue;
                }

                var updated = new SummaryState[existingStates.Length + 1];
                Array.Copy(existingStates, updated, existingStates.Length);
                updated[existingStates.Length] = state;
                _statesByColumnId[state.ColumnId] = updated;
            }
        }

        public void AddValue(string columnId, object value)
        {
            SummaryState[] states;
            if (!_statesByColumnId.TryGetValue(columnId ?? string.Empty, out states))
            {
                return;
            }

            var normalizedValue = _schema == null ? value : _schema.NormalizeValue(columnId, value);
            for (var i = 0; i < states.Length; i++)
            {
                states[i].AddValue(normalizedValue);
            }
        }

        public GridSummarySet ToSummarySet()
        {
            var result = new Dictionary<string, object>(_states.Length, StringComparer.Ordinal);
            for (var i = 0; i < _states.Length; i++)
            {
                var state = _states[i];
                result[state.Key] = state.BuildValue();
            }

            return new GridSummarySet(result);
        }

        private sealed class SummaryState
        {
            private readonly GridSummaryDescriptor _descriptor;
            private readonly string _key;
            private readonly string _columnId;
            private readonly GridSummaryType _type;
            private List<object> _customValues;
            private int _count;
            private int _numericCount;
            private decimal _sum;
            private object _min;
            private object _max;

            public SummaryState(GridSummaryDescriptor descriptor)
            {
                _descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
                _key = GridSummaryEngine.BuildSummaryKey(descriptor);
                _columnId = descriptor.ColumnId;
                _type = descriptor.Type;
            }

            public string Key => _key;

            public string ColumnId => _columnId;

            public void AddValue(object value)
            {
                switch (_type)
                {
                    case GridSummaryType.Count:
                        _count++;
                        break;
                    case GridSummaryType.Sum:
                        if (value != null)
                        {
                            _sum += Convert.ToDecimal(value, CultureInfo.InvariantCulture);
                        }

                        break;
                    case GridSummaryType.Average:
                        if (value != null)
                        {
                            _sum += Convert.ToDecimal(value, CultureInfo.InvariantCulture);
                            _numericCount++;
                        }

                        break;
                    case GridSummaryType.Min:
                        if (value != null && (_min == null || GridValueComparer.Instance.Compare(value, _min) < 0))
                        {
                            _min = value;
                        }

                        break;
                    case GridSummaryType.Max:
                        if (value != null && (_max == null || GridValueComparer.Instance.Compare(value, _max) > 0))
                        {
                            _max = value;
                        }

                        break;
                    case GridSummaryType.Custom:
                        if (_customValues == null)
                        {
                            _customValues = new List<object>();
                        }

                        _customValues.Add(value);
                        break;
                    default:
                        throw new NotSupportedException("Unsupported summary type: " + _type);
                }
            }

            public object BuildValue()
            {
                switch (_type)
                {
                    case GridSummaryType.Count:
                        return _count;
                    case GridSummaryType.Sum:
                        return _sum;
                    case GridSummaryType.Average:
                        return _numericCount == 0 ? 0m : _sum / _numericCount;
                    case GridSummaryType.Min:
                        return _min;
                    case GridSummaryType.Max:
                        return _max;
                    case GridSummaryType.Custom:
                        return _descriptor.AggregateCustom(_customValues ?? (IReadOnlyList<object>)Array.Empty<object>());
                    default:
                        throw new NotSupportedException("Unsupported summary type: " + _type);
                }
            }
        }
    }
}
