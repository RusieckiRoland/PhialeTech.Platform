using System;

namespace PhialeGrid.Core.Data
{
    public interface IGridVersionedDataSource
    {
        long Version { get; }

        event EventHandler VersionChanged;

        void Invalidate();
    }
}
