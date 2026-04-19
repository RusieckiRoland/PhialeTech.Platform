using System;
using System.Collections.Generic;

namespace PhialeGrid.Core.Commit
{
    public sealed class ChangeSetChange
    {
        public ChangeSetChange(
            ChangeTargetKind targetKind,
            string targetId,
            string targetPath,
            ChangeOperation operation,
            IReadOnlyList<FieldChange> fieldChanges = null,
            string versionToken = null)
        {
            TargetKind = targetKind;
            TargetId = targetId ?? throw new ArgumentNullException(nameof(targetId));
            TargetPath = targetPath ?? string.Empty;
            Operation = operation;
            FieldChanges = fieldChanges ?? Array.Empty<FieldChange>();
            VersionToken = versionToken ?? string.Empty;
        }

        public ChangeTargetKind TargetKind { get; }

        public string TargetId { get; }

        public string TargetPath { get; }

        public ChangeOperation Operation { get; }

        public IReadOnlyList<FieldChange> FieldChanges { get; }

        public string VersionToken { get; }
    }
}
