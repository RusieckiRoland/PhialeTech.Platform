namespace PhialeGrid.Core.Rendering
{
    /// <summary>
    /// Dostarcza wartości komórek dla buildera surface snapshotu.
    /// </summary>
    public interface IGridCellValueProvider
    {
        /// <summary>
        /// Próbuje pobrać surową wartość dla wskazanej komórki.
        /// </summary>
        bool TryGetValue(string rowKey, string columnKey, out object value);
    }
}
