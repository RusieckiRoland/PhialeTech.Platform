using System;
using System.Collections.Generic;

namespace PhialeGis.Library.Abstractions.Integrations.Tasking
{
    public sealed class TaskingExternalRecord
    {
        private readonly Dictionary<string, string> _metadata = new Dictionary<string, string>(StringComparer.Ordinal);

        public string NativeId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public TaskingExternalStatus Status { get; set; }
        public TaskingExternalPriority Priority { get; set; }
        public TaskingExternalAssignee Assignee { get; set; }
        public TaskingExternalLocation Location { get; set; }
        public DateTimeOffset? DueAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public IDictionary<string, string> Metadata { get { return _metadata; } }
    }
}
