using System;

namespace PhialeGrid.Core.Editing
{
    public sealed class EditSessionValidationDetail
    {
        public EditSessionValidationDetail(string fieldId, string displayName, string message)
        {
            FieldId = fieldId ?? string.Empty;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? FieldId : displayName;
            Message = message ?? string.Empty;
        }

        public string FieldId { get; }

        public string DisplayName { get; }

        public string Message { get; }
    }
}
