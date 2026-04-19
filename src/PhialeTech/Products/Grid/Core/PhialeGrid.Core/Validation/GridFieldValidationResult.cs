using System;
using System.Collections.Generic;
using System.Linq;
using PhialeGrid.Core.Columns;

namespace PhialeGrid.Core.Validation
{
    public sealed class GridFieldValidationContext
    {
        public GridFieldValidationContext(
            string fieldId,
            string displayName,
            Type valueType,
            GridFieldValidationConstraints constraints,
            object value,
            string editingText = null,
            GridColumnEditorKind editorKind = GridColumnEditorKind.Text,
            System.Collections.Generic.IReadOnlyList<string> editorItems = null,
            GridEditorItemsMode editorItemsMode = GridEditorItemsMode.Suggestions)
        {
            if (string.IsNullOrWhiteSpace(fieldId))
            {
                throw new ArgumentException("Field id is required.", nameof(fieldId));
            }

            FieldId = fieldId;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? fieldId : displayName;
            ValueType = valueType ?? typeof(object);
            Constraints = constraints;
            Value = value;
            EditingText = editingText;
            EditorKind = editorKind;
            EditorItems = (editorItems ?? Array.Empty<string>())
                .Where(item => item != null)
                .ToArray();
            EditorItemsMode = editorItemsMode;
        }

        public string FieldId { get; }

        public string DisplayName { get; }

        public Type ValueType { get; }

        public GridFieldValidationConstraints Constraints { get; }

        public object Value { get; }

        public string EditingText { get; }

        public GridColumnEditorKind EditorKind { get; }

        public System.Collections.Generic.IReadOnlyList<string> EditorItems { get; }

        public GridEditorItemsMode EditorItemsMode { get; }
    }

    public sealed class GridFieldValidationFailure
    {
        public GridFieldValidationFailure(string fieldId, string errorCode, string message, string messageKey = null)
        {
            if (string.IsNullOrWhiteSpace(fieldId))
            {
                throw new ArgumentException("Field id is required.", nameof(fieldId));
            }

            if (string.IsNullOrWhiteSpace(errorCode))
            {
                throw new ArgumentException("Error code is required.", nameof(errorCode));
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException("Message is required.", nameof(message));
            }

            FieldId = fieldId;
            ErrorCode = errorCode;
            Message = message;
            MessageKey = messageKey ?? string.Empty;
        }

        public string FieldId { get; }

        public string ErrorCode { get; }

        public string Message { get; }

        public string MessageKey { get; }
    }

    public sealed class GridFieldValidationResult
    {
        public GridFieldValidationResult(IReadOnlyList<GridFieldValidationFailure> errors)
        {
            Errors = (errors ?? Array.Empty<GridFieldValidationFailure>()).ToArray();
        }

        public bool IsValid => Errors.Count == 0;

        public IReadOnlyList<GridFieldValidationFailure> Errors { get; }
    }
}
