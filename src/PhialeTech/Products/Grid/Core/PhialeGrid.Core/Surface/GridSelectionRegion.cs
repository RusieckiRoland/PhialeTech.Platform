using System.Collections.Generic;

namespace PhialeGrid.Core.Surface
{
    /// <summary>
    /// Opisuje region zaznaczenia (selection region) w gridzie.
    /// Region może obejmować komórki, wiersze lub kolumny.
    /// </summary>
    public sealed class GridSelectionRegion
    {
        public GridSelectionRegion(
            string regionKey,
            GridSelectionUnit unit)
        {
            RegionKey = regionKey ?? throw new System.ArgumentNullException(nameof(regionKey));
            Unit = unit;
        }

        /// <summary>
        /// Unikatowy klucz dla tego regionu zaznaczenia.
        /// </summary>
        public string RegionKey { get; }

        /// <summary>
        /// Jednostka zaznaczenia (cell, row, column).
        /// </summary>
        public GridSelectionUnit Unit { get; }

        /// <summary>
        /// Lista wybranych kluczy (row keys, column keys, lub cell keys w zależności od Unit).
        /// </summary>
        public IReadOnlyList<string> SelectedKeys { get; set; } = new List<string>();

        /// <summary>
        /// Lista zaczeć zaznaczonych regionów (dla zaznaczenia myszą czy klawiszem Shift).
        /// Każdy element to start/end para: (startKey, endKey).
        /// </summary>
        public IReadOnlyList<(string Start, string End)> SelectedRanges { get; set; } = new List<(string, string)>();

        /// <summary>
        /// Czy jest to zaznaczenie "all" (wszystko).
        /// </summary>
        public bool IsSelectAll { get; set; }

        /// <summary>
        /// Czy zaznaczenie jest "inverted" (wszystko oprócz wymienionych).
        /// </summary>
        public bool IsInverted { get; set; }

        /// <summary>
        /// Numer wersji (revision) tego regionu.
        /// Zmienia się za każdym razem, gdy zaznaczenie się zmienia.
        /// </summary>
        public long Revision { get; set; }
    }

    /// <summary>
    /// Jednostka zaznaczenia.
    /// </summary>
    public enum GridSelectionUnit
    {
        /// <summary>
        /// Zaznaczenie na poziomie komórek.
        /// </summary>
        Cell,

        /// <summary>
        /// Zaznaczenie na poziomie wierszy.
        /// </summary>
        Row,

        /// <summary>
        /// Zaznaczenie na poziomie kolumn.
        /// </summary>
        Column,
    }
}
