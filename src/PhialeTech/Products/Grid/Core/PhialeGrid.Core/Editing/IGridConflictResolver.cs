namespace PhialeGrid.Core.Editing
{
    public interface IGridConflictResolver<T>
    {
        T Resolve(GridConcurrencyConflict<T> conflict);
    }
}
