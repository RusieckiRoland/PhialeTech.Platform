using System;

namespace PhialeGrid.Core.Editing
{
    public sealed class EditSessionFieldChange
    {
        public EditSessionFieldChange(string fieldId, string displayName, object originalValue, object currentValue)
        {
            FieldId = fieldId ?? throw new ArgumentNullException(nameof(fieldId));
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? fieldId : displayName;
            OriginalValue = originalValue;
            CurrentValue = currentValue;
        }

        public string FieldId { get; }

        public string DisplayName { get; }

        public object OriginalValue { get; }

        public object CurrentValue { get; }
    }
}
