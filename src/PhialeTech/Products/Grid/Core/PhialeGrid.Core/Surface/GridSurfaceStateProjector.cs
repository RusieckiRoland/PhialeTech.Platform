using System;
using System.Collections.Generic;
using PhialeGrid.Core.Editing;

namespace PhialeGrid.Core.Surface
{
    public static class GridSurfaceStateProjector
    {
        public static GridSurfaceStateProjection Project(EditSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            var recordStates = new Dictionary<string, GridRecordRenderState>(StringComparer.Ordinal);
            var cellStates = new Dictionary<string, GridCellRenderState>(StringComparer.Ordinal);

            foreach (var participant in session.Participants)
            {
                recordStates[participant.TargetId] = new GridRecordRenderState(
                    participant.TargetId,
                    participant.EditState,
                    participant.ValidationState,
                    participant.AccessState,
                    participant.CommitState,
                    participant.CommitDetail,
                    session.SessionId);

                foreach (var cell in participant.Cells.Values)
                {
                    var key = participant.TargetId + "_" + cell.FieldName;
                    cellStates[key] = new GridCellRenderState(
                        participant.TargetId,
                        cell.FieldName,
                        cell.DisplayState,
                        cell.ChangeState,
                        cell.ValidationState,
                        cell.AccessState,
                        session.SessionId);
                }
            }

            return new GridSurfaceStateProjection(
                recordStates,
                cellStates,
                session.EditingRecordId,
                session.ActiveEditingFieldId);
        }
    }
}
