namespace PhialeGrid.Core.Rendering
{
    /// <summary>
    /// Współrzędne aktualnej komórki (simple container dla row/column key).
    /// </summary>
    public sealed class GridCurrentCellCoordinate
    {
        public GridCurrentCellCoordinate(string rowKey, string columnKey)
        {
            RowKey = rowKey;
            ColumnKey = columnKey;
        }

        public string RowKey { get; set; }
        public string ColumnKey { get; set; }
    }
}
