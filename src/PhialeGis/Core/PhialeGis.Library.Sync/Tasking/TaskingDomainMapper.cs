using System;
using System.Collections.Generic;
using PhialeGis.Library.Abstractions.Integrations.Tasking;
using PhialeGis.Library.Domain.Tasking;
using PhialeGis.Library.Geometry.Abstractions;
using PhialeGis.Library.Geometry.IO.Wkt;
using PhialeGis.Library.Geometry.Spatial.Primitives;
using PhialeGis.Library.Geometry.Spatial.Single;

namespace PhialeGis.Library.Sync.Tasking
{
    public sealed class TaskingDomainMapper
    {
        public TaskItem ToDomain(TaskingExternalRecord record, string providerId)
        {
            if (record == null)
            {
                throw new ArgumentNullException(nameof(record));
            }

            var task = new TaskItem
            {
                Id = ComposeTaskId(providerId, record.NativeId),
                SystemLink = new TaskSystemLink
                {
                    ProviderId = providerId,
                    NativeId = record.NativeId,
                },
                Title = record.Title,
                Description = record.Description,
                Status = MapStatus(record.Status),
                Priority = MapPriority(record.Priority),
                Assignee = MapAssignee(record.Assignee),
                Location = MapLocation(record.Location),
                DueAt = record.DueAt,
                UpdatedAt = record.UpdatedAt,
            };

            CopyMetadata(record.Metadata, task.Metadata);
            return task;
        }

        public TaskingExternalRecord ToExternal(TaskItem task, string providerId)
        {
            if (task == null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            var record = new TaskingExternalRecord
            {
                NativeId = task.SystemLink != null ? task.SystemLink.NativeId : null,
                Title = task.Title,
                Description = task.Description,
                Status = MapStatus(task.Status),
                Priority = MapPriority(task.Priority),
                Assignee = MapAssignee(task.Assignee),
                Location = MapLocation(task.Location),
                DueAt = task.DueAt,
                UpdatedAt = task.UpdatedAt,
            };

            CopyMetadata(task.Metadata, record.Metadata);
            return record;
        }

        public string ComposeTaskId(string providerId, string nativeId)
        {
            if (string.IsNullOrWhiteSpace(nativeId))
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(providerId))
            {
                return nativeId;
            }

            return providerId + ":" + nativeId;
        }

        private static TaskItemStatus MapStatus(TaskingExternalStatus status)
        {
            switch (status)
            {
                case TaskingExternalStatus.Draft:
                    return TaskItemStatus.Draft;
                case TaskingExternalStatus.Ready:
                    return TaskItemStatus.Ready;
                case TaskingExternalStatus.Assigned:
                    return TaskItemStatus.Assigned;
                case TaskingExternalStatus.InProgress:
                    return TaskItemStatus.InProgress;
                case TaskingExternalStatus.OnHold:
                    return TaskItemStatus.OnHold;
                case TaskingExternalStatus.Completed:
                    return TaskItemStatus.Completed;
                case TaskingExternalStatus.Cancelled:
                    return TaskItemStatus.Cancelled;
                default:
                    return TaskItemStatus.Unknown;
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

        private static TaskItemPriority MapPriority(TaskingExternalPriority priority)
        {
            switch (priority)
            {
                case TaskingExternalPriority.Low:
                    return TaskItemPriority.Low;
                case TaskingExternalPriority.Normal:
                    return TaskItemPriority.Normal;
                case TaskingExternalPriority.High:
                    return TaskItemPriority.High;
                case TaskingExternalPriority.Critical:
                    return TaskItemPriority.Critical;
                default:
                    return TaskItemPriority.Unspecified;
            }
        }

        private static TaskingExternalPriority MapPriority(TaskItemPriority priority)
        {
            switch (priority)
            {
                case TaskItemPriority.Low:
                    return TaskingExternalPriority.Low;
                case TaskItemPriority.Normal:
                    return TaskingExternalPriority.Normal;
                case TaskItemPriority.High:
                    return TaskingExternalPriority.High;
                case TaskItemPriority.Critical:
                    return TaskingExternalPriority.Critical;
                default:
                    return TaskingExternalPriority.Unspecified;
            }
        }

        private static TaskAssignee MapAssignee(TaskingExternalAssignee assignee)
        {
            if (assignee == null)
            {
                return null;
            }

            return new TaskAssignee
            {
                Id = assignee.Id,
                DisplayName = assignee.DisplayName,
                TeamId = assignee.TeamId,
                TeamName = assignee.TeamName,
            };
        }

        private static TaskingExternalAssignee MapAssignee(TaskAssignee assignee)
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

        private static TaskLocationBinding MapLocation(TaskingExternalLocation location)
        {
            if (location == null)
            {
                return null;
            }

            return new TaskLocationBinding
            {
                LayerName = location.LayerName,
                FeatureId = location.FeatureId,
                Geometry = ParseGeometry(location),
            };
        }

        private static TaskingExternalLocation MapLocation(TaskLocationBinding location)
        {
            if (location == null)
            {
                return null;
            }

            var externalLocation = new TaskingExternalLocation
            {
                LayerName = location.LayerName,
                FeatureId = location.FeatureId,
                GeometryWkt = TaskingGeometryWktSerializer.Serialize(location.Geometry),
            };

            if (location.Geometry is PhPointEntity point)
            {
                externalLocation.X = point.Point.X;
                externalLocation.Y = point.Point.Y;
            }

            return externalLocation;
        }

        private static IPhGeometry ParseGeometry(TaskingExternalLocation location)
        {
            if (!string.IsNullOrWhiteSpace(location.GeometryWkt))
            {
                return PhWkt.Parse(location.GeometryWkt);
            }

            if (location.X.HasValue && location.Y.HasValue)
            {
                return new PhPointEntity(new PhPoint(location.X.Value, location.Y.Value));
            }

            return null;
        }

        private static void CopyMetadata(IDictionary<string, string> source, IDictionary<string, string> target)
        {
            if (source == null || target == null)
            {
                return;
            }

            foreach (var pair in source)
            {
                target[pair.Key] = pair.Value;
            }
        }
    }
}
