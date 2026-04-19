using System;

namespace PhialeGrid.Core.Surface
{
    /// <summary>
    /// Podstawowa klasa dla wszystkich elementów surface'u grida.
    /// Surface item opisuje, co ma być narysowane, gdzie i jak, bez konkretnych kontrolek UI.
    /// </summary>
    public abstract class GridSurfaceItem
    {
        /// <summary>
        /// Unikatowy klucz dla tego elementu w rámach snapshotu.
        /// Pozwala na identyfikację i mapowanie podczas renderings.
        /// </summary>
        public string ItemKey { get; set; }

        /// <summary>
        /// Prostokąt definiujący położenie i rozmiar elementu w layoutzie grida.
        /// Współrzędne są względem viewport'u grida.
        /// </summary>
        public GridBounds Bounds { get; set; }

        /// <summary>
        /// Klucz identyfikujący styl dla tego elementu.
        /// Frontend będzie szukał odpowiedniego stylu zasobu na podstawie tego klucza.
        /// </summary>
        public string StyleKey { get; set; }

        /// <summary>
        /// Warstwa rysowania dla tego elementu (z-order).
        /// Elementy z wyższą warstwą są rysowane na wierzchu.
        /// </summary>
        public int RenderLayer { get; set; }

        /// <summary>
        /// Numer wersji snapshotu, w którym ten element się pojawił.
        /// Przydatne dla detektowania zmian.
        /// </summary>
        public long SnapshotRevision { get; set; }
    }
}
