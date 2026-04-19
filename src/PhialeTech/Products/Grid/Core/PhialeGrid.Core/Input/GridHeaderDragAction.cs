namespace PhialeGrid.Core.Input
{
    public readonly struct GridHeaderDragAction
    {
        public GridHeaderDragAction(GridHeaderDragActionKind kind, string columnId)
        {
            Kind = kind;
            ColumnId = columnId ?? string.Empty;
        }

        public GridHeaderDragActionKind Kind { get; }

        public string ColumnId { get; }

        public static GridHeaderDragAction None => new GridHeaderDragAction(GridHeaderDragActionKind.None, string.Empty);
    }
}
