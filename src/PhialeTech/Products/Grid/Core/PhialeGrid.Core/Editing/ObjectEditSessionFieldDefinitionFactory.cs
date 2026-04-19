using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using PhialeGrid.Core.Columns;

namespace PhialeGrid.Core.Editing
{
    public static class ObjectEditSessionFieldDefinitionFactory
    {
        private static readonly ConcurrentDictionary<string, PropertyInfo> PropertyCache =
            new ConcurrentDictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);

        public static IReadOnlyList<IEditSessionFieldDefinition> CreateFromGridColumns(IEnumerable<GridColumnDefinition> columns)
        {
            return (columns ?? Array.Empty<GridColumnDefinition>())
                .Where(column => column != null)
                .Select(column => (IEditSessionFieldDefinition)new EditSessionFieldDefinition<object>(
                    column.Id,
                    column.Header,
                    column.ValueType ?? typeof(object),
                    getter: record => GetValue(record, column.Id),
                    setter: (record, value) => SetValue(record, column.Id, value),
                    fieldPath: column.Id,
                    valueKind: column.ValueKind,
                    editorKind: column.EditorKind,
                    editorItems: column.EditorItems,
                    editorItemsMode: column.EditorItemsMode,
                    editMask: column.EditMask,
                    validationConstraints: column.ValidationConstraints,
                    gridColumnDefinition: CloneColumn(column),
                    isVisibleInGrid: column.IsVisible,
                    isVisibleInExpandedDetails: !column.IsVisible))
                .ToArray();
        }

        public static IReadOnlyList<IEditSessionFieldDefinition> CreateFromRecords(IEnumerable<object> records)
        {
            var firstRecord = (records ?? Array.Empty<object>())
                .FirstOrDefault(record => record != null);
            if (firstRecord == null)
            {
                return Array.Empty<IEditSessionFieldDefinition>();
            }

            var dictionaryDefinitions = TryCreateDictionaryFieldDefinitions(firstRecord);
            if (dictionaryDefinitions.Count > 0)
            {
                return dictionaryDefinitions;
            }

            var recordType = firstRecord.GetType();
            if (recordType == null)
            {
                return Array.Empty<IEditSessionFieldDefinition>();
            }

            return recordType
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(property => property.CanRead && property.CanWrite && property.GetIndexParameters().Length == 0)
                .Select(property => (IEditSessionFieldDefinition)new EditSessionFieldDefinition<object>(
                    property.Name,
                    property.Name,
                    property.PropertyType,
                    getter: record => GetValue(record, property.Name),
                    setter: (record, value) => SetValue(record, property.Name, value),
                    fieldPath: property.Name,
                    valueKind: string.Empty,
                    editorKind: ResolveEditorKind(property.PropertyType),
                    editorItems: Array.Empty<string>(),
                    editorItemsMode: GridEditorItemsMode.Suggestions,
                    editMask: string.Empty,
                    validationConstraints: null,
                    gridColumnDefinition: BuildReflectedColumn(property),
                    isVisibleInGrid: true,
                    isVisibleInExpandedDetails: false))
                .ToArray();
        }

        public static IReadOnlyList<GridColumnDefinition> CreateGridColumns(IEnumerable<IEditSessionFieldDefinition> fieldDefinitions)
        {
            return (fieldDefinitions ?? Array.Empty<IEditSessionFieldDefinition>())
                .Where(field => field != null && field.IsVisibleInGrid)
                .Select((field, index) =>
                {
                    if (field.GridColumnDefinition != null)
                    {
                        return CloneColumn(field.GridColumnDefinition).WithValidationConstraints(null);
                    }

                    return new GridColumnDefinition(
                        field.FieldId,
                        field.DisplayName,
                        width: ResolveDefaultWidth(field.ValueType),
                        minWidth: 30d,
                        isVisible: field.IsVisibleInGrid,
                        isFrozen: false,
                        isEditable: true,
                        displayIndex: index,
                        valueType: field.ValueType,
                        editorKind: field.EditorKind,
                        editorItems: field.EditorItems,
                        editorItemsMode: field.EditorItemsMode,
                        editMask: field.EditMask,
                        valueKind: field.ValueKind,
                        validationConstraints: null);
                })
                .OrderBy(column => column.DisplayIndex)
                .ToArray();
        }

        private static GridColumnDefinition CloneColumn(GridColumnDefinition column)
        {
            return new GridColumnDefinition(
                column.Id,
                column.Header,
                column.Width,
                column.MinWidth,
                column.IsVisible,
                column.IsFrozen,
                column.IsEditable,
                column.DisplayIndex,
                column.ValueType,
                column.EditorKind,
                column.EditorItems,
                column.EditMask,
                column.ValueKind,
                column.ValidationConstraints,
                column.EditorItemsMode);
        }

        private static GridColumnDefinition BuildReflectedColumn(PropertyInfo property)
        {
            var editorKind = ResolveEditorKind(property.PropertyType);
            return new GridColumnDefinition(
                property.Name,
                property.Name,
                width: ResolveDefaultWidth(property.PropertyType),
                minWidth: 30d,
                isVisible: true,
                isFrozen: false,
                isEditable: property.CanWrite,
                displayIndex: -1,
                valueType: property.PropertyType,
                editorKind: editorKind,
                editorItems: Array.Empty<string>(),
                editMask: string.Empty,
                valueKind: string.Empty,
                validationConstraints: null,
                editorItemsMode: GridEditorItemsMode.Suggestions);
        }

        private static GridColumnDefinition BuildDictionaryColumn(string fieldId, Type valueType, int displayIndex)
        {
            var normalizedType = valueType ?? typeof(object);
            var editorKind = ResolveEditorKind(normalizedType);
            return new GridColumnDefinition(
                fieldId,
                fieldId,
                width: ResolveDefaultWidth(normalizedType),
                minWidth: 30d,
                isVisible: true,
                isFrozen: false,
                isEditable: true,
                displayIndex: displayIndex,
                valueType: normalizedType,
                editorKind: editorKind,
                editorItems: Array.Empty<string>(),
                editMask: string.Empty,
                valueKind: string.Empty,
                validationConstraints: null,
                editorItemsMode: GridEditorItemsMode.Suggestions);
        }

        private static object GetValue(object record, string fieldId)
        {
            if (record == null || string.IsNullOrWhiteSpace(fieldId))
            {
                return null;
            }

            if (TryGetDictionaryValue(record, fieldId, out var dictionaryValue))
            {
                return dictionaryValue;
            }

            var property = ResolveProperty(record.GetType(), fieldId);
            return property == null || !property.CanRead
                ? null
                : property.GetValue(record, null);
        }

        private static void SetValue(object record, string fieldId, object value)
        {
            if (record == null || string.IsNullOrWhiteSpace(fieldId))
            {
                return;
            }

            if (TrySetDictionaryValue(record, fieldId, value))
            {
                return;
            }

            var property = ResolveProperty(record.GetType(), fieldId);
            if (property == null || !property.CanWrite)
            {
                return;
            }

            var targetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            object convertedValue = value;
            if (value != null && targetType != typeof(string) && !targetType.IsInstanceOfType(value))
            {
                if (value is string stringValue &&
                    string.IsNullOrWhiteSpace(stringValue) &&
                    Nullable.GetUnderlyingType(property.PropertyType) != null)
                {
                    convertedValue = null;
                }
                else
                {
                    convertedValue = Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
                }
            }

            property.SetValue(record, convertedValue, null);
        }

        private static PropertyInfo ResolveProperty(Type recordType, string fieldId)
        {
            if (recordType == null || string.IsNullOrWhiteSpace(fieldId))
            {
                return null;
            }

            var cacheKey = recordType.FullName + "|" + fieldId;
            return PropertyCache.GetOrAdd(
                cacheKey,
                _ => recordType.GetProperty(fieldId, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase));
        }

        private static IReadOnlyList<IEditSessionFieldDefinition> TryCreateDictionaryFieldDefinitions(object record)
        {
            var entries = EnumerateDictionaryEntries(record);
            if (entries.Count == 0)
            {
                return Array.Empty<IEditSessionFieldDefinition>();
            }

            return entries
                .Select((entry, index) => (IEditSessionFieldDefinition)new EditSessionFieldDefinition<object>(
                    entry.Key,
                    entry.Key,
                    entry.Value?.GetType() ?? typeof(object),
                    getter: current => GetValue(current, entry.Key),
                    setter: (current, value) => SetValue(current, entry.Key, value),
                    fieldPath: entry.Key,
                    valueKind: string.Empty,
                    editorKind: ResolveEditorKind(entry.Value?.GetType() ?? typeof(object)),
                    editorItems: Array.Empty<string>(),
                    editorItemsMode: GridEditorItemsMode.Suggestions,
                    editMask: string.Empty,
                    validationConstraints: null,
                    gridColumnDefinition: BuildDictionaryColumn(entry.Key, entry.Value?.GetType() ?? typeof(object), index),
                    isVisibleInGrid: true,
                    isVisibleInExpandedDetails: false))
                .ToArray();
        }

        private static IReadOnlyList<KeyValuePair<string, object>> EnumerateDictionaryEntries(object record)
        {
            if (record is IDictionary<string, object> genericDictionary)
            {
                return genericDictionary
                    .Where(entry => !string.IsNullOrWhiteSpace(entry.Key))
                    .Select(entry => new KeyValuePair<string, object>(entry.Key, entry.Value))
                    .ToArray();
            }

            if (record is IDictionary dictionary)
            {
                return dictionary.Keys
                    .Cast<object>()
                    .Select(key => key?.ToString())
                    .Where(key => !string.IsNullOrWhiteSpace(key))
                    .Select(key => new KeyValuePair<string, object>(key, dictionary[key]))
                    .ToArray();
            }

            return Array.Empty<KeyValuePair<string, object>>();
        }

        private static bool TryGetDictionaryValue(object record, string fieldId, out object value)
        {
            if (record is IDictionary<string, object> genericDictionary)
            {
                if (genericDictionary.TryGetValue(fieldId, out value))
                {
                    return true;
                }

                var matchedKey = genericDictionary.Keys.FirstOrDefault(key => string.Equals(key, fieldId, StringComparison.OrdinalIgnoreCase));
                if (matchedKey != null)
                {
                    value = genericDictionary[matchedKey];
                    return true;
                }
            }

            if (record is IDictionary dictionary)
            {
                foreach (var key in dictionary.Keys.Cast<object>())
                {
                    if (string.Equals(key?.ToString(), fieldId, StringComparison.OrdinalIgnoreCase))
                    {
                        value = dictionary[key];
                        return true;
                    }
                }
            }

            value = null;
            return false;
        }

        private static bool TrySetDictionaryValue(object record, string fieldId, object value)
        {
            if (record is IDictionary<string, object> genericDictionary)
            {
                var existingKey = genericDictionary.Keys.FirstOrDefault(key => string.Equals(key, fieldId, StringComparison.OrdinalIgnoreCase)) ?? fieldId;
                genericDictionary[existingKey] = ConvertDictionaryValue(genericDictionary.TryGetValue(existingKey, out var currentValue) ? currentValue : null, value);
                return true;
            }

            if (record is IDictionary dictionary)
            {
                object existingKey = null;
                foreach (var key in dictionary.Keys.Cast<object>())
                {
                    if (string.Equals(key?.ToString(), fieldId, StringComparison.OrdinalIgnoreCase))
                    {
                        existingKey = key;
                        break;
                    }
                }

                if (existingKey == null)
                {
                    existingKey = fieldId;
                }
                var currentValue = dictionary.Contains(existingKey) ? dictionary[existingKey] : null;
                dictionary[existingKey] = ConvertDictionaryValue(currentValue, value);
                return true;
            }

            return false;
        }

        private static object ConvertDictionaryValue(object currentValue, object nextValue)
        {
            if (nextValue == null || currentValue == null)
            {
                return nextValue;
            }

            var targetType = Nullable.GetUnderlyingType(currentValue.GetType()) ?? currentValue.GetType();
            if (targetType == typeof(string) || targetType.IsInstanceOfType(nextValue))
            {
                return nextValue;
            }

            if (nextValue is string stringValue && string.IsNullOrWhiteSpace(stringValue))
            {
                return Nullable.GetUnderlyingType(currentValue.GetType()) != null ? null : nextValue;
            }

            return Convert.ChangeType(nextValue, targetType, CultureInfo.InvariantCulture);
        }

        private static GridColumnEditorKind ResolveEditorKind(Type valueType)
        {
            var normalizedType = Nullable.GetUnderlyingType(valueType) ?? valueType ?? typeof(object);
            if (normalizedType == typeof(bool))
            {
                return GridColumnEditorKind.CheckBox;
            }

            if (normalizedType == typeof(DateTime) || normalizedType == typeof(DateTimeOffset))
            {
                return GridColumnEditorKind.DatePicker;
            }

            return GridColumnEditorKind.Text;
        }

        private static double ResolveDefaultWidth(Type valueType)
        {
            var normalizedType = Nullable.GetUnderlyingType(valueType) ?? valueType ?? typeof(object);
            if (normalizedType == typeof(DateTime) || normalizedType == typeof(DateTimeOffset))
            {
                return 150d;
            }

            if (normalizedType == typeof(bool))
            {
                return 80d;
            }

            if (normalizedType == typeof(int) ||
                normalizedType == typeof(long) ||
                normalizedType == typeof(short) ||
                normalizedType == typeof(decimal) ||
                normalizedType == typeof(double) ||
                normalizedType == typeof(float))
            {
                return 120d;
            }

            return 160d;
        }
    }
}
