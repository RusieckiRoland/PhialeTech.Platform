using System;
using System.Collections.Generic;

namespace PhialeGrid.Core.Surface
{
    public sealed class GridSurfaceStateProjection
    {
        public static GridSurfaceStateProjection Empty { get; } =
            new GridSurfaceStateProjection(
                new Dictionary<string, GridRecordRenderState>(StringComparer.Ordinal),
                new Dictionary<string, GridCellRenderState>(StringComparer.Ordinal));

        public GridSurfaceStateProjection(
            IReadOnlyDictionary<string, GridRecordRenderState> recordStates,
            IReadOnlyDictionary<string, GridCellRenderState> cellStates,
            string editingRecordId = null,
            string activeEditingFieldId = null)
        {
            RecordStates = recordStates ?? throw new ArgumentNullException(nameof(recordStates));
            CellStates = cellStates ?? throw new ArgumentNullException(nameof(cellStates));
            EditingRecordId = editingRecordId ?? string.Empty;
            ActiveEditingFieldId = activeEditingFieldId ?? string.Empty;
        }

        public IReadOnlyDictionary<string, GridRecordRenderState> RecordStates { get; }

        public IReadOnlyDictionary<string, GridCellRenderState> CellStates { get; }

        public string EditingRecordId { get; }

        public string ActiveEditingFieldId { get; }

        public bool IsInEditMode => !string.IsNullOrWhiteSpace(EditingRecordId) && !string.IsNullOrWhiteSpace(ActiveEditingFieldId);
    }
}
