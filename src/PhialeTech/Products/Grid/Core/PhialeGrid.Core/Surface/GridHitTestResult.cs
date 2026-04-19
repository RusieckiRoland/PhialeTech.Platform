namespace PhialeGrid.Core.Surface
{
    /// <summary>
    /// Wynik hit testing'u - co się znalazło pod podaną pozycją.
    /// </summary>
    public sealed class GridHitTestResult
    {
        public GridHitTestResult(GridHitTargetKind targetKind)
        {
            TargetKind = targetKind;
        }

        /// <summary>
        /// Rodzaj obiektu, na jaki trafiono (comórka, header, overlay itp.).
        /// </summary>
        public GridHitTargetKind TargetKind { get; }

        /// <summary>
        /// Klucz wiersza, jeśli hit było na wiersz/komórkę.
        /// </summary>
        public string RowKey { get; set; }

        /// <summary>
        /// Klucz kolumny, jeśli hit było na kolumnę/komórkę.
        /// </summary>
        public string ColumnKey { get; set; }

        /// <summary>
        /// Klucz nagłówka, jeśli hit było na header.
        /// </summary>
        public string HeaderKey { get; set; }

        /// <summary>
        /// Klucz obiektu będącego celem hitu (row key, column key, cell key itp.).
        /// </summary>
        public string TargetKey { get; set; }

        /// <summary>
        /// Typ nagłówka, jeśli hit było na header.
        /// </summary>
        public GridHeaderKind? HeaderKind { get; set; }

        /// <summary>
        /// Rodzaj overlay'u, jeśli hit było na overlay.
        /// </summary>
        public GridOverlayKind? OverlayKind { get; set; }

        /// <summary>
        /// Miejsce hita w obiekcie (dla resizingu, drag handles itp.).
        /// </summary>
        public GridHitZone Zone { get; set; } = GridHitZone.Interior;

        /// <summary>
        /// Czy hit jest na elemencie interaktywnym.
        /// </summary>
        public bool IsInteractive { get; set; }

        /// <summary>
        /// X pozycja hitu względem viewport'u.
        /// </summary>
        public double HitX { get; set; }

        /// <summary>
        /// Y pozycja hitu względem viewport'u.
        /// </summary>
        public double HitY { get; set; }
    }

    /// <summary>
    /// Rodzaje obiektów, na które można trafić.
    /// </summary>
    public enum GridHitTargetKind
    {
        /// <summary>
        /// Nic nie zostało trafione.
        /// </summary>
        None,

        /// <summary>
        /// Komórka.
        /// </summary>
        Cell,

        /// <summary>
        /// Nagłówek (column, row, group itp.).
        /// </summary>
        Header,

        /// <summary>
        /// Overlay/dekoracja.
        /// </summary>
        Overlay,

        /// <summary>
        /// Region zaznaczenia.
        /// </summary>
        SelectionRegion,

        /// <summary>
        /// Marker aktualnej komórki.
        /// </summary>
        CurrentCellMarker,

        /// <summary>
        /// Resize handle kolumny.
        /// </summary>
        ColumnResizeHandle,

        /// <summary>
        /// Resize handle wiersza.
        /// </summary>
        RowResizeHandle,

        /// <summary>
        /// Pusta przestrzeń w gridzie.
        /// </summary>
        EmptySpace,

        /// <summary>
        /// Custom zone.
        /// </summary>
        Custom,

        /// <summary>
        /// Toggle/details region for a row.
        /// </summary>
        Details,

        /// <summary>
        /// Toggle element for hierarchy rows.
        /// </summary>
        HierarchyToggle,

        /// <summary>
        /// Checkbox selection slot in the dedicated row selector column.
        /// </summary>
        SelectionCheckbox,
    }

    /// <summary>
    /// Miejsce hit testing'u w obiekcie.
    /// </summary>
    public enum GridHitZone
    {
        /// <summary>
        /// Środek obiektu.
        /// </summary>
        Interior,

        /// <summary>
        /// Lewy edge (do resizingu).
        /// </summary>
        LeftEdge,

        /// <summary>
        /// Prawy edge.
        /// </summary>
        RightEdge,

        /// <summary>
        /// Górny edge.
        /// </summary>
        TopEdge,

        /// <summary>
        /// Dolny edge.
        /// </summary>
        BottomEdge,

        /// <summary>
        /// Lewy-górny corner.
        /// </summary>
        TopLeftCorner,

        /// <summary>
        /// Prawy-górny corner.
        /// </summary>
        TopRightCorner,

        /// <summary>
        /// Lewy-dolny corner.
        /// </summary>
        BottomLeftCorner,

        /// <summary>
        /// Prawy-dolny corner.
        /// </summary>
        BottomRightCorner,
    }
}
