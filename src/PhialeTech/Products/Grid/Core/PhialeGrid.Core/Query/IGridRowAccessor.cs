namespace PhialeGrid.Core.Query
{
    public interface IGridRowAccessor<in T>
    {
        object GetValue(T row, string columnId);
    }
}
