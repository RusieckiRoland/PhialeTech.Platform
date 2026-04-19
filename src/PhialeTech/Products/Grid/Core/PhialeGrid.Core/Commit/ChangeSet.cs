using System;
using System.Collections.Generic;

namespace PhialeGrid.Core.Commit
{
    public sealed class ChangeSet
    {
        public ChangeSet(
            string changeSetId,
            string sessionId,
            IReadOnlyList<ChangeSetChange> changes)
        {
            ChangeSetId = string.IsNullOrWhiteSpace(changeSetId)
                ? throw new ArgumentException("Change set id is required.", nameof(changeSetId))
                : changeSetId;
            SessionId = string.IsNullOrWhiteSpace(sessionId)
                ? throw new ArgumentException("Session id is required.", nameof(sessionId))
                : sessionId;
            Changes = changes ?? throw new ArgumentNullException(nameof(changes));
        }

        public string ChangeSetId { get; }

        public string SessionId { get; }

        public IReadOnlyList<ChangeSetChange> Changes { get; }
    }
}
