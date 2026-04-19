using System;
using System.Collections.Generic;

namespace PhialeGis.Library.Abstractions.Integrations.Tasking
{
    public sealed class TaskingExternalQuery
    {
        private readonly List<TaskingExternalStatus> _statuses = new List<TaskingExternalStatus>();
        private readonly List<string> _assigneeIds = new List<string>();

        public string LayerName { get; set; }
        public long? FeatureId { get; set; }
        public double? MinX { get; set; }
        public double? MinY { get; set; }
        public double? MaxX { get; set; }
        public double? MaxY { get; set; }
        public DateTimeOffset? UpdatedSince { get; set; }
        public int? Limit { get; set; }
        public IList<TaskingExternalStatus> Statuses { get { return _statuses; } }
        public IList<string> AssigneeIds { get { return _assigneeIds; } }
    }
}
