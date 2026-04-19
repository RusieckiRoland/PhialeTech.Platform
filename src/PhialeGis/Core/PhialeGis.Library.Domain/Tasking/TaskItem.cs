using System;
using System.Collections.Generic;

namespace PhialeGis.Library.Domain.Tasking
{
    public sealed class TaskItem
    {
        private readonly Dictionary<string, string> _metadata = new Dictionary<string, string>(StringComparer.Ordinal);

        public string Id { get; set; }
        public TaskSystemLink SystemLink { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public TaskItemStatus Status { get; set; }
        public TaskItemPriority Priority { get; set; }
        public TaskAssignee Assignee { get; set; }
        public TaskLocationBinding Location { get; set; }
        public DateTimeOffset? DueAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public IDictionary<string, string> Metadata { get { return _metadata; } }
    }
}
