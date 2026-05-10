using System;

namespace PhialeGrid.Core.Details
{
    public sealed class GridRowDetailFieldContext
    {
        public GridRowDetailFieldContext(string fieldId, string displayName, Type valueType, bool isReadOnly)
        {
            if (string.IsNullOrWhiteSpace(fieldId))
            {
                throw new ArgumentException("Field id is required.", nameof(fieldId));
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("Field display name is required.", nameof(displayName));
            }

            FieldId = fieldId;
            DisplayName = displayName;
            ValueType = valueType ?? throw new ArgumentNullException(nameof(valueType));
            IsReadOnly = isReadOnly;
        }

        public string FieldId { get; }

        public string DisplayName { get; }

        public Type ValueType { get; }

        public bool IsReadOnly { get; }
    }
}
