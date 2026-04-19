namespace PhialeGrid.Core.Surface
{
    /// <summary>
    /// Opisuje nagłówek grida (colummn header, row header itp.) na surface'u.
    /// </summary>
    public sealed class GridHeaderSurfaceItem : GridSurfaceItem
    {
        public GridHeaderSurfaceItem(
            string headerKey,
            GridHeaderKind kind,
            string itemKey = null)
        {
            HeaderKey = headerKey ?? throw new System.ArgumentNullException(nameof(headerKey));
            Kind = kind;
            ItemKey = itemKey ?? $"header_{kind}_{headerKey}";
        }

        /// <summary>
        /// Rodzaj nagłówka (column, row, corner itp.).
        /// </summary>
        public GridHeaderKind Kind { get; }

        /// <summary>
        /// Klucz nagłówka (np. column key, row key).
        /// </summary>
        public string HeaderKey { get; }

        /// <summary>
        /// Tekst wyświetlany w nagłówku.
        /// </summary>
        public string DisplayText { get; set; } = string.Empty;

        /// <summary>
        /// Czy nagłówek jest wybrany.
        /// </summary>
        public bool IsSelected { get; set; }

        /// <summary>
        /// Stan wskaźnika wiersza dla lewego structural column.
        /// </summary>
        public GridRowIndicatorState RowIndicatorState { get; set; } = GridRowIndicatorState.Empty;

        /// <summary>
        /// Czy należy pokazać kolumnę wskaźnika wiersza.
        /// </summary>
        public bool ShowRowIndicator { get; set; }

        /// <summary>
        /// Czy należy pokazać checkbox multi-select dla tego wiersza.
        /// </summary>
        public bool ShowSelectionCheckbox { get; set; }

        /// <summary>
        /// Czy checkbox multi-select jest zaznaczony.
        /// </summary>
        public bool IsSelectionCheckboxChecked { get; set; }

        /// <summary>
        /// Czy nagłówek należy do bieżącego wiersza.
        /// </summary>
        public bool IsCurrentRow { get; set; }

        /// <summary>
        /// Tekst tooltipa dla wskaźnika stanu wiersza.
        /// </summary>
        public string RowIndicatorToolTip { get; set; } = string.Empty;

        /// <summary>
        /// Szerokość pierwszego slotu opcji/akcji.
        /// </summary>
        public double RowActionWidth { get; set; }

        /// <summary>
        /// Szerokość slotu wskaźnika wiersza.
        /// </summary>
        public double RowIndicatorWidth { get; set; }

        /// <summary>
        /// Szerokość slotu markera wiersza.
        /// </summary>
        public double RowMarkerWidth { get; set; }

        /// <summary>
        /// Szerokość slotu checkbox multi-select.
        /// </summary>
        public double SelectionCheckboxWidth { get; set; }

        /// <summary>
        /// Czy w marker column należy pokazać liczbę porządkową.
        /// </summary>
        public bool ShowRowNumber { get; set; }

        /// <summary>
        /// Tekst liczby porządkowej w marker column.
        /// </summary>
        public string RowNumberText { get; set; } = string.Empty;

        /// <summary>
        /// Czy nagłówek ma menu (dropdown dla filtrów, sortowania itp.).
        /// </summary>
        public bool HasMenu { get; set; }

        /// <summary>
        /// Czy menu jest otwarte.
        /// </summary>
        public bool IsMenuOpen { get; set; }

        /// <summary>
        /// Klucz ikony (dla sortowania, filtrowania itp.).
        /// </summary>
        public string IconKey { get; set; }

        /// <summary>
        /// Widoczny tekst kolejności sortowania dla multisortu.
        /// Pusty dla pierwszego sortu lub gdy kolumna nie jest sortowana.
        /// </summary>
        public string SortOrderText { get; set; } = string.Empty;

        /// <summary>
        /// Czy nagłówek jest resizable.
        /// </summary>
        public bool IsResizable { get; set; }
    }

    /// <summary>
    /// Rodzaje nagłówków w gridzie.
    /// </summary>
    public enum GridHeaderKind
    {
        /// <summary>
        /// Nagłówek kolumny (na górze grida).
        /// </summary>
        ColumnHeader,

        /// <summary>
        /// Nagłówek kolumny stanu wiersza (po lewej stronie grida).
        /// </summary>
        RowHeader,

        /// <summary>
        /// Nagłówek kolumny numeracji wiersza.
        /// </summary>
        RowNumberHeader,

        /// <summary>
        /// Narożnik (intersection nagłówka kolumny i wiersza).
        /// </summary>
        Corner,

        /// <summary>
        /// Nagłówek grupy (dla grouping).
        /// </summary>
        GroupHeader,

        /// <summary>
        /// Nagłówek summaries (dla summaries row).
        /// </summary>
        SummaryHeader,
    }

    public enum GridRowIndicatorState
    {
        Empty,
        Current,
        Edited,
        Invalid,
        CurrentAndEdited,
        CurrentAndInvalid,
        Editing,
        EditingAndEdited,
        EditingAndInvalid,
        EditingAndEditedAndInvalid,
    }
}
