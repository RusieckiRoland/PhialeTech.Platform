namespace PhialeGrid.Core.Surface
{
    using System;
    using System.Collections.Generic;
    using PhialeGrid.Core.Columns;
    using PhialeGrid.Core.Editing;

    /// <summary>
    /// Opisuje pojedynczą komórkę grida na surface'u.
    /// Zawiera wszystkie informacje potrzebne do narysowania komórki bez dostępu do oryginalnych danych.
    /// </summary>
    public sealed class GridCellSurfaceItem : GridSurfaceItem
    {
        public GridCellSurfaceItem(
            string rowKey,
            string columnKey,
            string itemKey = null)
        {
            RowKey = rowKey ?? throw new System.ArgumentNullException(nameof(rowKey));
            ColumnKey = columnKey ?? throw new System.ArgumentNullException(nameof(columnKey));
            ItemKey = itemKey ?? $"cell_{rowKey}_{columnKey}";
        }

        /// <summary>
        /// Klucz wiersza, do którego należy ta komórka.
        /// Może być indeksem, ID lub innym identyfikatorem zdefiniowanym przez logikę Core.
        /// </summary>
        public string RowKey { get; }

        /// <summary>
        /// Klucz kolumny, do której należy ta komórka.
        /// </summary>
        public string ColumnKey { get; }

        /// <summary>
        /// Tekst do wyświetlenia w komórce.
        /// To jest już sformatowany string, gotowy do wysłania na ekran.
        /// </summary>
        public string DisplayText { get; set; } = string.Empty;

        /// <summary>
        /// Surowa wartość z danych źródłowych.
        /// Może być null lub dowolnym typem.
        /// </summary>
        public object RawValue { get; set; }

        /// <summary>
        /// Rodzaj wartości dla wskazówek renderowania (liczba, tekst, data, bool itp.).
        /// Helps renderer choose appropriate presentation.
        /// </summary>
        public string ValueKind { get; set; } = "Text";

        /// <summary>
        /// Czy komórka jest aktualnie wybrana (część selection region).
        /// </summary>
        public bool IsSelected { get; set; }

        /// <summary>
        /// Czy komórka jest aktualną komórką (active cell).
        /// </summary>
        public bool IsCurrent { get; set; }

        /// <summary>
        /// Czy komórka należy do bieżącego wiersza.
        /// </summary>
        public bool IsCurrentRow { get; set; }

        /// <summary>
        /// Czy komórka jest w trybie edycji.
        /// </summary>
        public bool IsEditing { get; set; }

        /// <summary>
        /// Bieżący tekst edytora dla komórki będącej w trybie edycji.
        /// </summary>
        public string EditingText { get; set; }

        /// <summary>
        /// Czy komórka jest tylko do odczytu.
        /// </summary>
        public bool IsReadOnly { get; set; }

        /// <summary>
        /// Czy komórka ma błąd walidacji.
        /// </summary>
        public bool HasValidationError { get; set; }

        public CellDisplayState DisplayState { get; set; } = CellDisplayState.Normal;

        public CellChangeState ChangeState { get; set; } = CellChangeState.Unchanged;

        public CellValidationState ValidationState { get; set; } = CellValidationState.Unknown;

        public CellAccessState AccessState { get; set; } = CellAccessState.Editable;

        public string EditSessionId { get; set; } = string.Empty;

        /// <summary>
        /// Komunikt błędu walidacji, jeśli istnieje.
        /// </summary>
        public string ValidationError { get; set; }

        /// <summary>
        /// Czy komórka należy do zamarznięcia (frozen region).
        /// </summary>
        public bool IsFrozen { get; set; }

        /// <summary>
        /// Klucz template'u zawartości dla specjalnych typów komórek.
        /// Np. dla checkbox, imageów, custom controls itp.
        /// </summary>
        public string ContentTemplateKey { get; set; }

        /// <summary>
        /// Rodzaj edytora skojarzony z kolumną.
        /// </summary>
        public GridColumnEditorKind EditorKind { get; set; }

        /// <summary>
        /// Lista predefiniowanych wartości używana przez rich editory.
        /// </summary>
        public IReadOnlyList<string> EditorItems { get; set; } = Array.Empty<string>();

        public GridEditorItemsMode EditorItemsMode { get; set; } = GridEditorItemsMode.Suggestions;

        /// <summary>
        /// Maska tekstowa używana przez edytor maskowany.
        /// </summary>
        public string EditMask { get; set; } = string.Empty;

        /// <summary>
        /// Czy to jest dummy komórka (nie ma rzeczywistych danych).
        /// Używane dla filling viewport'u.
        /// </summary>
        public bool IsDummy { get; set; }

        /// <summary>
        /// Czy komórka renderuje grupowy caption z własnym chevronem wewnątrz komórki.
        /// </summary>
        public bool IsGroupCaptionCell { get; set; }

        /// <summary>
        /// Czy dla komórki należy narysować chevron expand/collapse.
        /// </summary>
        public bool ShowInlineChevron { get; set; }

        /// <summary>
        /// Czy inline chevron powinien być w stanie expanded.
        /// </summary>
        public bool IsInlineChevronExpanded { get; set; }

        /// <summary>
        /// Wewnętrzny indent contentu komórki w pikselach.
        /// Zachowuje stabilną geometrię kolumn, ale pozwala odsunąć zawartość w obrębie komórki.
        /// </summary>
        public double ContentIndent { get; set; }
    }
}
