using System;
using System.Collections.Generic;

namespace PhialeGis.Library.Domain.Tasking
{
    public sealed class TaskQueryCriteria
    {
        private readonly List<TaskItemStatus> _statuses = new List<TaskItemStatus>();
        private readonly List<string> _assigneeIds = new List<string>();

        public string LayerName { get; set; }
        public long? FeatureId { get; set; }
        public double? MinX { get; set; }
        public double? MinY { get; set; }
        public double? MaxX { get; set; }
        public double? MaxY { get; set; }
        public DateTimeOffset? UpdatedSince { get; set; }
        public int? Limit { get; set; }
        public IList<TaskItemStatus> Statuses { get { return _statuses; } }
        public IList<string> AssigneeIds { get { return _assigneeIds; } }
    }
}
