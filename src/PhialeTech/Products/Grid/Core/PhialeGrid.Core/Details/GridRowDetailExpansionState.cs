using System;
using System.Collections.Generic;
using System.Linq;

namespace PhialeGrid.Core.Details
{
    public sealed class GridRowDetailExpansionState
    {
        public static readonly GridRowDetailExpansionState Empty =
            new GridRowDetailExpansionState(Array.Empty<string>());

        private readonly HashSet<string> _expandedRowKeys;

        public GridRowDetailExpansionState(IEnumerable<string> expandedRowKeys)
        {
            if (expandedRowKeys == null)
            {
                throw new ArgumentNullException(nameof(expandedRowKeys));
            }

            _expandedRowKeys = new HashSet<string>(
                expandedRowKeys.Select(RequireRowKey),
                StringComparer.OrdinalIgnoreCase);
        }

        public bool IsExpanded(string rowKey)
        {
            return _expandedRowKeys.Contains(RequireRowKey(rowKey));
        }

        public GridRowDetailExpansionState Expand(string rowKey)
        {
            var key = RequireRowKey(rowKey);
            var next = new HashSet<string>(_expandedRowKeys, StringComparer.OrdinalIgnoreCase);
            next.Add(key);
            return new GridRowDetailExpansionState(next);
        }

        public GridRowDetailExpansionState Collapse(string rowKey)
        {
            var key = RequireRowKey(rowKey);
            var next = new HashSet<string>(_expandedRowKeys, StringComparer.OrdinalIgnoreCase);
            next.Remove(key);
            return new GridRowDetailExpansionState(next);
        }

        public GridRowDetailExpansionState Toggle(string rowKey)
        {
            return IsExpanded(rowKey) ? Collapse(rowKey) : Expand(rowKey);
        }

        private static string RequireRowKey(string rowKey)
        {
            if (string.IsNullOrWhiteSpace(rowKey))
            {
                throw new ArgumentException("Row key is required.", nameof(rowKey));
            }

            return rowKey;
        }
    }
}
