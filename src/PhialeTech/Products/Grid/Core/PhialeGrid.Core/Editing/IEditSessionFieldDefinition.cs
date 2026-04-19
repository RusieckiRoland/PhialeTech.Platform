using System;
using System.Collections.Generic;
using PhialeGrid.Core.Columns;
using PhialeGrid.Core.Validation;

namespace PhialeGrid.Core.Editing
{
    public interface IEditSessionFieldDefinition
    {
        string FieldId { get; }

        string DisplayName { get; }

        string FieldPath { get; }

        Type ValueType { get; }

        string ValueKind { get; }

        GridColumnEditorKind EditorKind { get; }

        IReadOnlyList<string> EditorItems { get; }

        GridEditorItemsMode EditorItemsMode { get; }

        string EditMask { get; }

        GridFieldValidationConstraints ValidationConstraints { get; }

        GridColumnDefinition GridColumnDefinition { get; }

        bool IsVisibleInGrid { get; }

        bool IsVisibleInExpandedDetails { get; }

        bool IsWeakAttribute { get; }

        object GetValue(object record);

        void SetValue(object record, object value);
    }
}
