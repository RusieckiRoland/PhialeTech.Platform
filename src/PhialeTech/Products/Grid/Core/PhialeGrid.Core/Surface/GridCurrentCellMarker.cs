namespace PhialeGrid.Core.Surface
{
    /// <summary>
    /// Opisuje aktualną komórkę (current cell / active cell) w gridzie.
    /// </summary>
    public sealed class GridCurrentCellMarker
    {
        public GridCurrentCellMarker(string rowKey, string columnKey)
        {
            RowKey = rowKey ?? throw new System.ArgumentNullException(nameof(rowKey));
            ColumnKey = columnKey ?? throw new System.ArgumentNullException(nameof(columnKey));
        }

        /// <summary>
        /// Klucz wiersza aktualnej komórki.
        /// </summary>
        public string RowKey { get; set; }

        /// <summary>
        /// Klucz kolumny aktualnej komórki.
        /// </summary>
        public string ColumnKey { get; set; }

        /// <summary>
        /// Czy ta komórka powinna być w edycji.
        /// </summary>
        public bool ShouldBeInEditMode { get; set; }

        /// <summary>
        /// Czy ta komórka ma fokus.
        /// </summary>
        public bool HasFocus { get; set; }

        /// <summary>
        /// Numer wersji (revision).
        /// </summary>
        public long Revision { get; set; }

        /// <summary>
        /// Czy marker jest ważny (komórka istnieje i jest widoczna).
        /// </summary>
        public bool IsValid { get; set; } = true;

        /// <summary>
        /// Czy current cell powinien być auto-scrollowany do widoku.
        /// </summary>
        public bool ShouldEnsureVisible { get; set; }
    }
}
