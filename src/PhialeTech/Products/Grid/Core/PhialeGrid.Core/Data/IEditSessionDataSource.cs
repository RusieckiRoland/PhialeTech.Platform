using System.Collections.Generic;
using PhialeGrid.Core.Commit;
using PhialeGrid.Core.Editing;

namespace PhialeGrid.Core.Data
{
    public interface IEditSessionDataSource<TRecord> : IGridVersionedDataSource
    {
        SaveMode DefaultSaveMode { get; }

        IReadOnlyList<TRecord> GetSnapshot();

        IReadOnlyList<IEditSessionFieldDefinition> GetFieldDefinitions();

        TRecord CreateWorkingCopy(TRecord record);
    }
}
