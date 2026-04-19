using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using PhialeGrid.Core.Commit;
using PhialeGrid.Core.Editing;

namespace PhialeGrid.Core.Data
{
    public sealed class InMemoryEditSessionDataSource<TRecord> : IEditSessionDataSource<TRecord>
    {
        private IReadOnlyList<TRecord> _items;
        private IReadOnlyList<IEditSessionFieldDefinition> _fieldDefinitions;

        public InMemoryEditSessionDataSource(
            IEnumerable<TRecord> items = null,
            IEnumerable<IEditSessionFieldDefinition> fieldDefinitions = null,
            SaveMode defaultSaveMode = SaveMode.Direct)
        {
            _items = (items ?? Array.Empty<TRecord>()).ToArray();
            _fieldDefinitions = (fieldDefinitions ?? Array.Empty<IEditSessionFieldDefinition>()).ToArray();
            DefaultSaveMode = defaultSaveMode;
        }

        public SaveMode DefaultSaveMode { get; }

        public long Version { get; private set; }

        public event EventHandler VersionChanged;

        public IReadOnlyList<TRecord> GetSnapshot()
        {
            return _items;
        }

        public IReadOnlyList<IEditSessionFieldDefinition> GetFieldDefinitions()
        {
            return _fieldDefinitions;
        }

        public TRecord CreateWorkingCopy(TRecord record)
        {
            if (record == null)
            {
                return default(TRecord);
            }

            var type = record.GetType();
            var copy = FormatterServices.GetUninitializedObject(type);
            foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (!property.CanRead || !property.CanWrite || property.GetIndexParameters().Length > 0)
                {
                    continue;
                }

                property.SetValue(copy, property.GetValue(record, null), null);
            }

            return (TRecord)copy;
        }

        public void ReplaceSnapshot(IEnumerable<TRecord> items)
        {
            _items = (items ?? Array.Empty<TRecord>()).ToArray();
            Invalidate();
        }

        public void ReplaceFieldDefinitions(IEnumerable<IEditSessionFieldDefinition> fieldDefinitions)
        {
            _fieldDefinitions = (fieldDefinitions ?? Array.Empty<IEditSessionFieldDefinition>()).ToArray();
            Invalidate();
        }

        public void ReplaceData(IEnumerable<TRecord> items, IEnumerable<IEditSessionFieldDefinition> fieldDefinitions)
        {
            _items = (items ?? Array.Empty<TRecord>()).ToArray();
            _fieldDefinitions = (fieldDefinitions ?? Array.Empty<IEditSessionFieldDefinition>()).ToArray();
            Invalidate();
        }

        public void Invalidate()
        {
            Version++;
            VersionChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
