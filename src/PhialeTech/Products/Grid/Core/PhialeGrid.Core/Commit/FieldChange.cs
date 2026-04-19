using System;

namespace PhialeGrid.Core.Commit
{
    public sealed class FieldChange
    {
        public FieldChange(string fieldName, object oldValue, object newValue)
        {
            FieldName = fieldName ?? throw new ArgumentNullException(nameof(fieldName));
            OldValue = oldValue;
            NewValue = newValue;
        }

        public string FieldName { get; }

        public object OldValue { get; }

        public object NewValue { get; }
    }
}
