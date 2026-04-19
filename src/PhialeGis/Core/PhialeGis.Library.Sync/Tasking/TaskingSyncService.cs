using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PhialeGis.Library.Abstractions.Integrations.Tasking;
using PhialeGis.Library.Domain.Tasking;

namespace PhialeGis.Library.Sync.Tasking
{
    public sealed class TaskingSyncService
    {
        private readonly ITaskingSystemProvider _provider;
        private readonly TaskingDomainMapper _mapper;

        public TaskingSyncService(ITaskingSystemProvider provider, TaskingDomainMapper mapper = null)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _mapper = mapper ?? new TaskingDomainMapper();
        }

        public TaskingProviderDescriptor Descriptor { get { return _provider.Descriptor; } }

        public async Task<IReadOnlyList<TaskItem>> QueryAsync(TaskQueryCriteria criteria, CancellationToken cancellationToken = default(CancellationToken))
        {
            var externalQuery = MapQuery(criteria ?? new TaskQueryCriteria());
            var records = await _provider.QueryTasksAsync(externalQuery, cancellationToken).ConfigureAwait(false);
            var tasks = new List<TaskItem>(records.Count);

            for (int i = 0; i < records.Count; i++)
            {
                tasks.Add(_mapper.ToDomain(records[i], _provider.Descriptor.ProviderId));
            }

            return tasks.AsReadOnly();
        }

        public async Task<TaskItem> SaveAsync(TaskItem task, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (task == null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            EnsureProviderMatches(task.SystemLink);

            var saved = await _provider.CreateOrUpdateTaskAsync(
                _mapper.ToExternal(task, _provider.Descriptor.ProviderId),
                cancellationToken).ConfigureAwait(false);

            return _mapper.ToDomain(saved, _provider.Descriptor.ProviderId);
        }

        public async Task<TaskItem> UpdateStatusAsync(TaskSystemLink systemLink, TaskItemStatus status, CancellationToken cancellationToken = default(CancellationToken))
        {
            var link = RequireLink(systemLink);
            var saved = await _provider.UpdateTaskStatusAsync(
                link.NativeId,
                MapStatus(status),
                cancellationToken).ConfigureAwait(false);

            return _mapper.ToDomain(saved, _provider.Descriptor.ProviderId);
        }

        public async Task<TaskItem> AssignAsync(TaskSystemLink systemLink, TaskAssignee assignee, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (assignee == null)
            {
                throw new ArgumentNullException(nameof(assignee));
            }

            var link = RequireLink(systemLink);
            var saved = await _provider.AssignTaskAsync(
                link.NativeId,
                new TaskingExternalAssignee
                {
                    Id = assignee.Id,
                    DisplayName = assignee.DisplayName,
                    TeamId = assignee.TeamId,
                    TeamName = assignee.TeamName,
                },
                cancellationToken).ConfigureAwait(false);

            return _mapper.ToDomain(saved, _provider.Descriptor.ProviderId);
        }

        public Task<bool> DeleteAsync(TaskSystemLink systemLink, CancellationToken cancellationToken = default(CancellationToken))
        {
            var link = RequireLink(systemLink);
            return _provider.DeleteTaskAsync(link.NativeId, cancellationToken);
        }

        private TaskingExternalQuery MapQuery(TaskQueryCriteria criteria)
        {
            var query = new TaskingExternalQuery
            {
                LayerName = criteria.LayerName,
                FeatureId = criteria.FeatureId,
                MinX = criteria.MinX,
                MinY = criteria.MinY,
                MaxX = criteria.MaxX,
                MaxY = criteria.MaxY,
                UpdatedSince = criteria.UpdatedSince,
                Limit = criteria.Limit,
            };

            for (int i = 0; i < criteria.Statuses.Count; i++)
            {
                query.Statuses.Add(MapStatus(criteria.Statuses[i]));
            }

            for (int i = 0; i < criteria.AssigneeIds.Count; i++)
            {
                query.AssigneeIds.Add(criteria.AssigneeIds[i]);
            }

            return query;
        }

        private TaskSystemLink RequireLink(TaskSystemLink systemLink)
        {
            if (systemLink == null)
            {
                throw new ArgumentNullException(nameof(systemLink));
            }

            EnsureProviderMatches(systemLink);
            if (string.IsNullOrWhiteSpace(systemLink.NativeId))
            {
                throw new ArgumentException("Task link must provide a native task id.", nameof(systemLink));
            }

            return systemLink;
        }

        private void EnsureProviderMatches(TaskSystemLink systemLink)
        {
            if (systemLink == null || string.IsNullOrWhiteSpace(systemLink.ProviderId))
            {
                return;
            }

            if (!string.Equals(systemLink.ProviderId, _provider.Descriptor.ProviderId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Task belongs to a different tasking provider.");
            }
        }

        private static TaskingExternalStatus MapStatus(TaskItemStatus status)
        {
            switch (status)
            {
                case TaskItemStatus.Draft:
                    return TaskingExternalStatus.Draft;
                case TaskItemStatus.Ready:
                    return TaskingExternalStatus.Ready;
                case TaskItemStatus.Assigned:
                    return TaskingExternalStatus.Assigned;
                case TaskItemStatus.InProgress:
                    return TaskingExternalStatus.InProgress;
                case TaskItemStatus.OnHold:
                    return TaskingExternalStatus.OnHold;
                case TaskItemStatus.Completed:
                    return TaskingExternalStatus.Completed;
                case TaskItemStatus.Cancelled:
                    return TaskingExternalStatus.Cancelled;
                default:
                    return TaskingExternalStatus.Unknown;
            }
        }
    }
}
