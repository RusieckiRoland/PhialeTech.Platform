namespace PhialeGrid.Core.Surface
{
    /// <summary>
    /// Opisuje overlay/dekorację na surface'u grida.
    /// Overlaye są rysowane na wierzchu głównychkomórek i nagłówków.
    /// </summary>
    public sealed class GridOverlaySurfaceItem : GridSurfaceItem
    {
        public GridOverlaySurfaceItem(
            string overlayKey,
            GridOverlayKind kind,
            string itemKey = null)
        {
            OverlayKey = overlayKey ?? throw new System.ArgumentNullException(nameof(overlayKey));
            Kind = kind;
            ItemKey = itemKey ?? $"overlay_{kind}_{overlayKey}";
        }

        /// <summary>
        /// Rodzaj overlay'u (selection, current cell, loading itp.).
        /// </summary>
        public GridOverlayKind Kind { get; }

        /// <summary>
        /// Klucz identyfikujący ten overlay.
        /// </summary>
        public string OverlayKey { get; }

        /// <summary>
        /// Klucz obiektu, na którym overlay się pojawia (np. row key, cell key).
        /// </summary>
        public string TargetKey { get; set; }

        /// <summary>
        /// Typ celu overlay'u.
        /// </summary>
        public GridOverlayTargetKind TargetKind { get; set; }

        /// <summary>
        /// Dodatkowe dane potrzebne do rysowania overlay'u.
        /// Np. kolor, grubość linii, ikonka itp.
        /// </summary>
        public object Payload { get; set; }

        /// <summary>
        /// Czy overlay powinien być animowany.
        /// </summary>
        public bool IsAnimated { get; set; }

        /// <summary>
        /// Czas trwania animacji w millisekund.
        /// </summary>
        public int AnimationDurationMs { get; set; }

        /// <summary>
        /// Priorytet rysowania (0 = na dnie, wyższe = wyżej).
        /// </summary>
        public int DrawPriority { get; set; }

        /// <summary>
        /// Czy overlay jest interaktywny (reaguje na kliknięcia).
        /// </summary>
        public bool IsInteractive { get; set; }
    }

    /// <summary>
    /// Rodzaje overlayów w gridzie.
    /// </summary>
    public enum GridOverlayKind
    {
        /// <summary>
        /// Overlay zaznaczenia (selection region).
        /// </summary>
        Selection,

        /// <summary>
        /// Overlay aktualnej komórki (current cell indicator).
        /// </summary>
        CurrentCell,

        /// <summary>
        /// Wskaźnik bieżącego rekordu przy prawej krawędzi widoku.
        /// </summary>
        CurrentRecord,

        /// <summary>
        /// Overlay loading (busy spinner).
        /// </summary>
        Loading,

        /// <summary>
        /// Overlay walidacji (error indicator).
        /// </summary>
        Validation,

        /// <summary>
        /// Indicator resize kolumny/wiersza.
        /// </summary>
        ResizeIndicator,

        /// <summary>
        /// Indicator drop (dla drag &amp; drop).
        /// </summary>
        DropIndicator,

        /// <summary>
        /// Highlight row (np. przy hover).
        /// </summary>
        RowHighlight,

        /// <summary>
        /// Custom overlay.
        /// </summary>
        Custom,
    }

    /// <summary>
    /// Typ celu overlay'u.
    /// </summary>
    public enum GridOverlayTargetKind
    {
        /// <summary>
        /// Overlay na komórce.
        /// </summary>
        Cell,

        /// <summary>
        /// Overlay na wierszu.
        /// </summary>
        Row,

        /// <summary>
        /// Overlay na kolumnie.
        /// </summary>
        Column,

        /// <summary>
        /// Overlay na regionie (może być mehrere cells/rows).
        /// </summary>
        Region,

        /// <summary>
        /// Overlay na całym gridzie.
        /// </summary>
        Grid,
    }
}
