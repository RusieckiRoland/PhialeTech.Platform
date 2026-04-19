namespace PhialeGrid.Core.Surface
{
    using PhialeGrid.Core.Commit;
    using PhialeGrid.Core.Editing;

    /// <summary>
    /// Opisuje wiersz grida na surface'u.
    /// </summary>
    public sealed class GridRowSurfaceItem : GridSurfaceItem
    {
        public GridRowSurfaceItem(string rowKey, string itemKey = null)
        {
            RowKey = rowKey ?? throw new System.ArgumentNullException(nameof(rowKey));
            ItemKey = itemKey ?? $"row_{rowKey}";
        }

        /// <summary>
        /// Unikatowy klucz wiersza.
        /// </summary>
        public string RowKey { get; }

        /// <summary>
        /// Wysokość wiersza w pixelach.
        /// </summary>
        public double Height { get; set; }

        /// <summary>
        /// Czy wiersz jest wybrany (selected).
        /// </summary>
        public bool IsSelected { get; set; }

        /// <summary>
        /// Czy wiersz zawiera błąd walidacji.
        /// </summary>
        public bool HasValidationError { get; set; }

        public RecordEditState EditState { get; set; } = RecordEditState.Unchanged;

        public RecordValidationState ValidationState { get; set; } = RecordValidationState.Unknown;

        public RecordAccessState AccessState { get; set; } = RecordAccessState.Editable;

        public RecordCommitState CommitState { get; set; } = RecordCommitState.Idle;

        public RecordCommitDetail CommitDetail { get; set; } = RecordCommitDetail.None;

        public string EditSessionId { get; set; } = string.Empty;

        /// <summary>
        /// Czy wiersz ma pending zmiany (dirty).
        /// </summary>
        public bool HasPendingChanges { get; set; }

        /// <summary>
        /// Poziom zagnieżdżenia hierarchii (dla tree view).
        /// 0 = główny poziom, 1+ = vnętrze.
        /// </summary>
        public int HierarchyLevel { get; set; }

        /// <summary>
        /// Czy wiersz jest rozwinięty w hierarchii.
        /// </summary>
        public bool IsHierarchyExpanded { get; set; }

        /// <summary>
        /// Czy wiersz ma dzieci w hierarchii.
        /// </summary>
        public bool HasHierarchyChildren { get; set; }

        /// <summary>
        /// Czy wiersz jest w trakcie edycji.
        /// </summary>
        public bool IsEditing { get; set; }

        /// <summary>
        /// Czy to dummy wiersz (dla wypełnienia viewport'u).
        /// </summary>
        public bool IsDummy { get; set; }

        /// <summary>
        /// Czy wiersz jest grupy (dla paging, grouping itp.).
        /// </summary>
        public bool IsGroupHeader { get; set; }

        /// <summary>
        /// Czy ten wiersz reprezentuje akcję "load more" dla hierarchii.
        /// </summary>
        public bool IsLoadMore { get; set; }

        /// <summary>
        /// Czy wiersz ma row details sekcję.
        /// </summary>
        public bool HasDetailsExpanded { get; set; }

        /// <summary>
        /// Czy ten wiersz jest hostem dla dedykowanego obszaru details.
        /// </summary>
        public bool IsDetailsHost { get; set; }

        /// <summary>
        /// Czy wiersz należy do frozen rows.
        /// </summary>
        public bool IsFrozen { get; set; }

        /// <summary>
        /// Czy wiersz reprezentuje rekord danych, a nie wiersz strukturalny.
        /// </summary>
        public bool RepresentsDataRecord { get; set; } = true;
    }
}
