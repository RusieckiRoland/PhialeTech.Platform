using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PhialeGis.Library.Abstractions.Integrations.Tasking;
using PhialeGis.Library.Geometry.IO.Wkt;

namespace PhialeGis.Library.Sync.Tasking
{
    public sealed class InMemoryTaskingSystemProvider : ITaskingSystemProvider
    {
        private readonly object _gate = new object();
        private readonly Dictionary<string, TaskingExternalRecord> _records = new Dictionary<string, TaskingExternalRecord>(StringComparer.Ordinal);

        public InMemoryTaskingSystemProvider(string providerId = "mock", string displayName = "Mock tasking provider")
        {
            Descriptor = new TaskingProviderDescriptor
            {
                ProviderId = string.IsNullOrWhiteSpace(providerId) ? "mock" : providerId,
                DisplayName = string.IsNullOrWhiteSpace(displayName) ? "Mock tasking provider" : displayName,
                Capabilities = new TaskingProviderCapabilities
                {
                    CanCreateTasks = true,
                    CanUpdateTasks = true,
                    CanUpdateStatus = true,
                    CanAssignTasks = true,
                    CanDeleteTasks = true,
                    CanFilterByBounds = true,
                    CanStoreGeometry = true,
                },
            };
        }

        public TaskingProviderDescriptor Descriptor { get; private set; }

        public Task<IReadOnlyList<TaskingExternalRecord>> QueryTasksAsync(TaskingExternalQuery query, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var criteria = query ?? new TaskingExternalQuery();
            var results = new List<TaskingExternalRecord>();

            lock (_gate)
            {
                foreach (var pair in _records)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!Matches(pair.Value, criteria))
                    {
                        continue;
                    }

                    results.Add(Clone(pair.Value));
                    if (criteria.Limit.HasValue && results.Count >= criteria.Limit.Value)
                    {
                        break;
                    }
                }
            }

            return Task.FromResult<IReadOnlyList<TaskingExternalRecord>>(results.AsReadOnly());
        }

        public Task<TaskingExternalRecord> CreateOrUpdateTaskAsync(TaskingExternalRecord task, CancellationToken cancellationToken)
        {
            if (task == null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            cancellationToken.ThrowIfCancellationRequested();
            TaskingExternalRecord snapshot;

            lock (_gate)
            {
                snapshot = Clone(task);
                if (string.IsNullOrWhiteSpace(snapshot.NativeId))
                {
                    snapshot.NativeId = Guid.NewGuid().ToString("N");
                }

                snapshot.UpdatedAt = DateTimeOffset.UtcNow;
                _records[snapshot.NativeId] = Clone(snapshot);
            }

            return Task.FromResult(Clone(snapshot));
        }

        public Task<TaskingExternalRecord> UpdateTaskStatusAsync(string nativeId, TaskingExternalStatus status, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            TaskingExternalRecord updated;

            lock (_gate)
            {
                updated = GetRequired(nativeId);
                updated.Status = status;
                updated.UpdatedAt = DateTimeOffset.UtcNow;
                _records[nativeId] = Clone(updated);
            }

            return Task.FromResult(Clone(updated));
        }

        public Task<TaskingExternalRecord> AssignTaskAsync(string nativeId, TaskingExternalAssignee assignee, CancellationToken cancellationToken)
        {
            if (assignee == null)
            {
                throw new ArgumentNullException(nameof(assignee));
            }

            cancellationToken.ThrowIfCancellationRequested();
            TaskingExternalRecord updated;

            lock (_gate)
            {
                updated = GetRequired(nativeId);
                updated.Assignee = Clone(assignee);
                updated.UpdatedAt = DateTimeOffset.UtcNow;
                _records[nativeId] = Clone(updated);
            }

            return Task.FromResult(Clone(updated));
        }

        public Task<bool> DeleteTaskAsync(string nativeId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            bool removed;
            lock (_gate)
            {
                removed = _records.Remove(nativeId);
            }

            return Task.FromResult(removed);
        }

        private bool Matches(TaskingExternalRecord record, TaskingExternalQuery query)
        {
            if (!string.IsNullOrWhiteSpace(query.LayerName) &&
                !string.Equals(query.LayerName, record.Location != null ? record.Location.LayerName : null, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (query.FeatureId.HasValue &&
                (!record.Location?.FeatureId.HasValue ?? true || record.Location.FeatureId.Value != query.FeatureId.Value))
            {
                return false;
            }

            if (query.Statuses.Count > 0 && !query.Statuses.Contains(record.Status))
            {
                return false;
            }

            if (query.AssigneeIds.Count > 0)
            {
                var assigneeId = record.Assignee != null ? record.Assignee.Id : null;
                if (string.IsNullOrWhiteSpace(assigneeId) || !query.AssigneeIds.Contains(assigneeId))
                {
                    return false;
                }
            }

            if (query.UpdatedSince.HasValue &&
                (!record.UpdatedAt.HasValue || record.UpdatedAt.Value < query.UpdatedSince.Value))
            {
                return false;
            }

            if (HasBounds(query) && !IntersectsBounds(record.Location, query))
            {
                return false;
            }

            return true;
        }

        private static bool HasBounds(TaskingExternalQuery query)
        {
            return query.MinX.HasValue && query.MinY.HasValue && query.MaxX.HasValue && query.MaxY.HasValue;
        }

        private static bool IntersectsBounds(TaskingExternalLocation location, TaskingExternalQuery query)
        {
            if (location == null)
            {
                return false;
            }

            if (location.X.HasValue && location.Y.HasValue)
            {
                return location.X.Value >= query.MinX.Value &&
                       location.X.Value <= query.MaxX.Value &&
                       location.Y.Value >= query.MinY.Value &&
                       location.Y.Value <= query.MaxY.Value;
            }

            if (string.IsNullOrWhiteSpace(location.GeometryWkt))
            {
                return false;
            }

            var geometry = PhWkt.Parse(location.GeometryWkt);
            var envelope = geometry.Envelope;
            return envelope.MinX <= query.MaxX.Value &&
                   envelope.MaxX >= query.MinX.Value &&
                   envelope.MinY <= query.MaxY.Value &&
                   envelope.MaxY >= query.MinY.Value;
        }

        private TaskingExternalRecord GetRequired(string nativeId)
        {
            if (string.IsNullOrWhiteSpace(nativeId))
            {
                throw new ArgumentException("Task id cannot be empty.", nameof(nativeId));
            }

            if (!_records.TryGetValue(nativeId, out var record))
            {
                throw new KeyNotFoundException("Unknown task id: " + nativeId);
            }

            return Clone(record);
        }

        private static TaskingExternalRecord Clone(TaskingExternalRecord record)
        {
            var clone = new TaskingExternalRecord
            {
                NativeId = record.NativeId,
                Title = record.Title,
                Description = record.Description,
                Status = record.Status,
                Priority = record.Priority,
                Assignee = Clone(record.Assignee),
                Location = Clone(record.Location),
                DueAt = record.DueAt,
                UpdatedAt = record.UpdatedAt,
            };

            foreach (var pair in record.Metadata)
            {
                clone.Metadata[pair.Key] = pair.Value;
            }

            return clone;
        }

        private static TaskingExternalAssignee Clone(TaskingExternalAssignee assignee)
        {
            if (assignee == null)
            {
                return null;
            }

            return new TaskingExternalAssignee
            {
                Id = assignee.Id,
                DisplayName = assignee.DisplayName,
                TeamId = assignee.TeamId,
                TeamName = assignee.TeamName,
            };
        }

        private static TaskingExternalLocation Clone(TaskingExternalLocation location)
        {
            if (location == null)
            {
                return null;
            }

            return new TaskingExternalLocation
            {
                LayerName = location.LayerName,
                FeatureId = location.FeatureId,
                X = location.X,
                Y = location.Y,
                GeometryWkt = location.GeometryWkt,
            };
        }
    }
}
