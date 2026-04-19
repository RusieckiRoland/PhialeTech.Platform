using System;
using System.Linq;
using PhialeGrid.Core.Validation;

namespace PhialeGrid.Core.Columns
{
    public sealed class GridColumnDefinition
    {
        public GridColumnDefinition(
            string id,
            string header,
            double width = 120d,
            double minWidth = 30d,
            bool isVisible = true,
            bool isFrozen = false,
            bool isEditable = true,
            int displayIndex = -1,
            Type valueType = null,
            GridColumnEditorKind editorKind = GridColumnEditorKind.Text,
            System.Collections.Generic.IReadOnlyList<string> editorItems = null,
            string editMask = null,
            string valueKind = null,
            GridFieldValidationConstraints validationConstraints = null,
            GridEditorItemsMode? editorItemsMode = null)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Column id is required.", nameof(id));
            }

            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width));
            }

            if (minWidth <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(minWidth));
            }

            Id = id;
            Header = string.IsNullOrWhiteSpace(header) ? id : header;
            Width = width;
            MinWidth = minWidth;
            IsVisible = isVisible;
            IsFrozen = isFrozen;
            IsEditable = isEditable;
            DisplayIndex = displayIndex;
            ValueType = valueType ?? typeof(object);
            EditorKind = editorKind;
            EditorItems = (editorItems ?? Array.Empty<string>()).Where(item => item != null).ToArray();
            EditMask = editMask ?? string.Empty;
            ValueKind = string.IsNullOrWhiteSpace(valueKind) ? string.Empty : valueKind.Trim();
            ValidationConstraints = validationConstraints;
            EditorItemsMode = ResolveEditorItemsMode(editorKind, EditorItems, ValidationConstraints, editorItemsMode);

            GridFieldValidationConfigurationValidator.EnsureCompatible(
                Id,
                ValueType,
                EditorKind,
                ValidationConstraints);
        }

        public string Id { get; }

        public string Header { get; }

        public double Width { get; }

        public double MinWidth { get; }

        public bool IsVisible { get; }

        public bool IsFrozen { get; }

        public bool IsEditable { get; }

        public int DisplayIndex { get; }

        public Type ValueType { get; }

        public GridColumnEditorKind EditorKind { get; }

        public System.Collections.Generic.IReadOnlyList<string> EditorItems { get; }

        public string EditMask { get; }

        public string ValueKind { get; }

        public GridFieldValidationConstraints ValidationConstraints { get; }

        public GridEditorItemsMode EditorItemsMode { get; }

        public GridColumnDefinition WithHeader(string header)
        {
            return new GridColumnDefinition(Id, header, Width, MinWidth, IsVisible, IsFrozen, IsEditable, DisplayIndex, ValueType, EditorKind, EditorItems, EditMask, ValueKind, ValidationConstraints, EditorItemsMode);
        }

        public GridColumnDefinition WithWidth(double width)
        {
            return new GridColumnDefinition(Id, Header, width, MinWidth, IsVisible, IsFrozen, IsEditable, DisplayIndex, ValueType, EditorKind, EditorItems, EditMask, ValueKind, ValidationConstraints, EditorItemsMode);
        }

        public GridColumnDefinition WithMinWidth(double minWidth)
        {
            return new GridColumnDefinition(Id, Header, Width, minWidth, IsVisible, IsFrozen, IsEditable, DisplayIndex, ValueType, EditorKind, EditorItems, EditMask, ValueKind, ValidationConstraints, EditorItemsMode);
        }

        public GridColumnDefinition WithVisibility(bool isVisible)
        {
            return new GridColumnDefinition(Id, Header, Width, MinWidth, isVisible, IsFrozen, IsEditable, DisplayIndex, ValueType, EditorKind, EditorItems, EditMask, ValueKind, ValidationConstraints, EditorItemsMode);
        }

        public GridColumnDefinition WithFrozen(bool isFrozen)
        {
            return new GridColumnDefinition(Id, Header, Width, MinWidth, IsVisible, isFrozen, IsEditable, DisplayIndex, ValueType, EditorKind, EditorItems, EditMask, ValueKind, ValidationConstraints, EditorItemsMode);
        }

        public GridColumnDefinition WithEditable(bool isEditable)
        {
            return new GridColumnDefinition(Id, Header, Width, MinWidth, IsVisible, IsFrozen, isEditable, DisplayIndex, ValueType, EditorKind, EditorItems, EditMask, ValueKind, ValidationConstraints, EditorItemsMode);
        }

        public GridColumnDefinition WithDisplayIndex(int displayIndex)
        {
            return new GridColumnDefinition(Id, Header, Width, MinWidth, IsVisible, IsFrozen, IsEditable, displayIndex, ValueType, EditorKind, EditorItems, EditMask, ValueKind, ValidationConstraints, EditorItemsMode);
        }

        public GridColumnDefinition WithValueType(Type valueType)
        {
            return new GridColumnDefinition(Id, Header, Width, MinWidth, IsVisible, IsFrozen, IsEditable, DisplayIndex, valueType, EditorKind, EditorItems, EditMask, ValueKind, ValidationConstraints, EditorItemsMode);
        }

        public GridColumnDefinition WithValueKind(string valueKind)
        {
            return new GridColumnDefinition(Id, Header, Width, MinWidth, IsVisible, IsFrozen, IsEditable, DisplayIndex, ValueType, EditorKind, EditorItems, EditMask, valueKind, ValidationConstraints, EditorItemsMode);
        }

        public GridColumnDefinition WithEditor(
            GridColumnEditorKind editorKind,
            System.Collections.Generic.IReadOnlyList<string> editorItems = null,
            string editMask = null,
            GridEditorItemsMode? editorItemsMode = null)
        {
            return new GridColumnDefinition(Id, Header, Width, MinWidth, IsVisible, IsFrozen, IsEditable, DisplayIndex, ValueType, editorKind, editorItems ?? EditorItems, editMask ?? EditMask, ValueKind, ValidationConstraints, editorItemsMode ?? EditorItemsMode);
        }

        public GridColumnDefinition WithValidationConstraints(GridFieldValidationConstraints validationConstraints)
        {
            return new GridColumnDefinition(Id, Header, Width, MinWidth, IsVisible, IsFrozen, IsEditable, DisplayIndex, ValueType, EditorKind, EditorItems, EditMask, ValueKind, validationConstraints, EditorItemsMode);
        }

        public GridColumnDefinition WithEditorItemsMode(GridEditorItemsMode editorItemsMode)
        {
            return new GridColumnDefinition(Id, Header, Width, MinWidth, IsVisible, IsFrozen, IsEditable, DisplayIndex, ValueType, EditorKind, EditorItems, EditMask, ValueKind, ValidationConstraints, editorItemsMode);
        }

        private static GridEditorItemsMode ResolveEditorItemsMode(
            GridColumnEditorKind editorKind,
            System.Collections.Generic.IReadOnlyList<string> editorItems,
            GridFieldValidationConstraints validationConstraints,
            GridEditorItemsMode? editorItemsMode)
        {
            if (editorItemsMode.HasValue)
            {
                return editorItemsMode.Value;
            }

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
