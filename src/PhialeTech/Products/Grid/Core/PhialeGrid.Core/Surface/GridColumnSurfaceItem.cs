namespace PhialeGrid.Core.Surface
{
    /// <summary>
    /// Opisuje kolumnę grida na surface'u.
    /// Zawiera informacje potrzebne do rysowania nagłówka i komórek kolumny.
    /// </summary>
    public sealed class GridColumnSurfaceItem : GridSurfaceItem
    {
        public GridColumnSurfaceItem(string columnKey, string itemKey = null)
        {
            ColumnKey = columnKey ?? throw new System.ArgumentNullException(nameof(columnKey));
            ItemKey = itemKey ?? $"column_{columnKey}";
        }

        /// <summary>
        /// Unikatowy klucz kolumny w definiach kolumn.
        /// </summary>
        public string ColumnKey { get; }

        /// <summary>
        /// Wyświetlana nazwa kolumny.
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Szerokość kolumny w pixelach.
        /// </summary>
        public double Width { get; set; }

        /// <summary>
        /// Minimalna szerokość, którą kolumna może mieć.
        /// </summary>
        public double MinWidth { get; set; }

        /// <summary>
        /// Maksymalna szerokość, którą kolumna może mieć.
        /// </summary>
        public double MaxWidth { get; set; } = double.PositiveInfinity;

        /// <summary>
        /// Czy kolumna jest vidoczna.
        /// </summary>
        public bool IsVisible { get; set; } = true;

        /// <summary>
        /// Czy kolumna jest zamarznięta (frozen).
        /// </summary>
        public bool IsFrozen { get; set; }

        /// <summary>
        /// Czy kolumna jest możliwa do zmiany rozmiaru.
        /// </summary>
        public bool IsResizable { get; set; } = true;

        /// <summary>
        /// Czy kolumna jest możliwa do sortowania.
        /// </summary>
        public bool IsSortable { get; set; } = true;

        /// <summary>
        /// Czy kolumna jest możliwa do filtrowania.
        /// </summary>
        public bool IsFilterable { get; set; } = true;

        /// <summary>
        /// Aktualny porządek wyświetlania (display index).
        /// Kolumny o niższym indeksie są wyświetlane na lewo.
        /// </summary>
        public int DisplayIndex { get; set; }

        /// <summary>
        /// Czy kolumna jest obecnie sortowana i w jakim kierunku.
        /// null = nie sortowana, true = ascending, false = descending
        /// </summary>
        public bool? SortDirection { get; set; }

        /// <summary>
        /// Pozycja w porządku sortowania. -1 = niestosowana.
        /// </summary>
        public int SortPriority { get; set; } = -1;

        /// <summary>
        /// Czy kolumna ma aktywny filtr.
        /// </summary>
        public bool HasActiveFilter { get; set; }

        /// <summary>
        /// Czy kolumna je zaznaczona (selected).
        /// </summary>
        public bool IsSelected { get; set; }
    }
}
