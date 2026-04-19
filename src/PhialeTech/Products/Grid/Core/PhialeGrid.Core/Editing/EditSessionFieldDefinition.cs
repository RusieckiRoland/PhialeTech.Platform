using System;
using System.Collections.Generic;
using System.Linq;
using PhialeGrid.Core.Columns;
using PhialeGrid.Core.Validation;

namespace PhialeGrid.Core.Editing
{
    public sealed class EditSessionFieldDefinition<TRecord> : IEditSessionFieldDefinition
    {
        private readonly Func<TRecord, object> _getter;
        private readonly Action<TRecord, object> _setter;

        public EditSessionFieldDefinition(
            string fieldId,
            string displayName,
            Type valueType,
            Func<TRecord, object> getter,
            Action<TRecord, object> setter,
            string fieldPath = null,
            string valueKind = null,
            GridColumnEditorKind editorKind = GridColumnEditorKind.Text,
            IReadOnlyList<string> editorItems = null,
            string editMask = null,
            GridFieldValidationConstraints validationConstraints = null,
            GridColumnDefinition gridColumnDefinition = null,
            bool isVisibleInGrid = true,
            bool isVisibleInExpandedDetails = false,
            bool isWeakAttribute = false,
            GridEditorItemsMode? editorItemsMode = null)
        {
            if (string.IsNullOrWhiteSpace(fieldId))
            {
                throw new ArgumentException("Field id is required.", nameof(fieldId));
            }

            FieldId = fieldId;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? fieldId : displayName;
            FieldPath = string.IsNullOrWhiteSpace(fieldPath) ? fieldId : fieldPath;
            ValueType = valueType ?? typeof(object);
            _getter = getter ?? throw new ArgumentNullException(nameof(getter));
            _setter = setter ?? throw new ArgumentNullException(nameof(setter));
            ValueKind = valueKind ?? string.Empty;
            EditorKind = editorKind;
            EditorItems = (editorItems ?? Array.Empty<string>()).Where(item => item != null).ToArray();
            EditorItemsMode = editorItemsMode
                ?? gridColumnDefinition?.EditorItemsMode
                ?? ResolveEditorItemsMode(editorKind, EditorItems, validationConstraints);
            EditMask = editMask ?? string.Empty;
            ValidationConstraints = validationConstraints;
            GridColumnDefinition = gridColumnDefinition;
            IsVisibleInGrid = isVisibleInGrid;
            IsVisibleInExpandedDetails = isVisibleInExpandedDetails;
            IsWeakAttribute = isWeakAttribute;
        }

        public string FieldId { get; }

        public string DisplayName { get; }

        public string FieldPath { get; }

        public Type ValueType { get; }

        public string ValueKind { get; }

        public GridColumnEditorKind EditorKind { get; }

        public IReadOnlyList<string> EditorItems { get; }

        public GridEditorItemsMode EditorItemsMode { get; }

        public string EditMask { get; }

        public GridFieldValidationConstraints ValidationConstraints { get; }

        public GridColumnDefinition GridColumnDefinition { get; }

        public bool IsVisibleInGrid { get; }

        public bool IsVisibleInExpandedDetails { get; }

        public bool IsWeakAttribute { get; }

        public object GetValue(object record)
        {
            if (!(record is TRecord))
            {
                throw new InvalidOperationException("Record instance does not match field definition type.");
            }

            var typedRecord = (TRecord)record;
            return _getter(typedRecord);
        }

        public void SetValue(object record, object value)
        {
            if (!(record is TRecord))
            {
                throw new InvalidOperationException("Record instance does not match field definition type.");
            }

            var typedRecord = (TRecord)record;
            _setter(typedRecord, value);
        }

        private static GridEditorItemsMode ResolveEditorItemsMode(
            GridColumnEditorKind editorKind,
            IReadOnlyList<string> editorItems,
            GridFieldValidationConstraints validationConstraints)
        {
            if (validationConstraints is LookupValidationConstraints)
            {
                return GridEditorItemsMode.RestrictToItems;
            }

            if (validationConstraints is TextValidationConstraints textConstraints &&
                textConstraints.AllowedValues.Count > 0)
            {
                return GridEditorItemsMode.RestrictToItems;
            }

            if (editorKind == GridColumnEditorKind.Combo &&
                (editorItems?.Count ?? 0) > 0)
            {
                return GridEditorItemsMode.RestrictToItems;
            }

            return GridEditorItemsMode.Suggestions;
        }
    }
}
