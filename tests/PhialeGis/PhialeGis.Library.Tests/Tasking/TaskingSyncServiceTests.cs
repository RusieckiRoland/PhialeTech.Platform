using System.Threading.Tasks;
using NUnit.Framework;
using PhialeGis.Library.Domain.Tasking;
using PhialeGis.Library.Geometry.Spatial.Primitives;
using PhialeGis.Library.Geometry.Spatial.Single;
using PhialeGis.Library.Sync.Tasking;

namespace PhialeGis.Library.Tests.Tasking
{
    [TestFixture]
    public sealed class TaskingSyncServiceTests
    {
        [Test]
        public async Task SaveAsync_ShouldRoundTripTaskAndCreateCompositeId()
        {
            var provider = new InMemoryTaskingSystemProvider("mock");
            var service = new TaskingSyncService(provider);

            var task = new TaskItem
            {
                Title = "Inspect hydrant",
                Description = "North zone inspection",
                Status = TaskItemStatus.Assigned,
                Priority = TaskItemPriority.High,
                Location = new TaskLocationBinding
                {
                    LayerName = "Hydrants",
                    FeatureId = 42,
                    Geometry = new PhPointEntity(new PhPoint(10, 20)),
                },
            };
            task.Metadata["ticket"] = "HZ-42";

            var saved = await service.SaveAsync(task);

            Assert.That(saved.Id, Is.EqualTo("mock:" + saved.SystemLink.NativeId));
            Assert.That(saved.SystemLink.ProviderId, Is.EqualTo("mock"));
            Assert.That(saved.Metadata["ticket"], Is.EqualTo("HZ-42"));
            Assert.That(saved.Location.Geometry, Is.Not.Null);

            var query = new TaskQueryCriteria
            {
                LayerName = "Hydrants",
                FeatureId = 42,
            };
            query.Statuses.Add(TaskItemStatus.Assigned);

            var results = await service.QueryAsync(query);

            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].Title, Is.EqualTo("Inspect hydrant"));
        }

        [Test]
        public async Task UpdateStatusAndAssignAsync_ShouldPersistChanges()
        {
            var provider = new InMemoryTaskingSystemProvider("mock");
            var service = new TaskingSyncService(provider);

            var saved = await service.SaveAsync(new TaskItem
            {
                Title = "Repair valve",
                Status = TaskItemStatus.Ready,
                Priority = TaskItemPriority.Normal,
                Location = new TaskLocationBinding
                {
                    LayerName = "Valves",
                    FeatureId = 7,
                    Geometry = new PhPointEntity(new PhPoint(5, 5)),
                },
            });

            var updated = await service.UpdateStatusAsync(saved.SystemLink, TaskItemStatus.InProgress);
            var assigned = await service.AssignAsync(saved.SystemLink, new TaskAssignee
            {
                Id = "u-17",
                DisplayName = "Anna Kowalska",
                TeamId = "ops",
                TeamName = "Operations",
            });

            Assert.That(updated.Status, Is.EqualTo(TaskItemStatus.InProgress));
            Assert.That(assigned.Assignee, Is.Not.Null);
            Assert.That(assigned.Assignee.Id, Is.EqualTo("u-17"));

            var query = new TaskQueryCriteria();
            query.Statuses.Add(TaskItemStatus.InProgress);
            query.AssigneeIds.Add("u-17");

            var results = await service.QueryAsync(query);

            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].SystemLink.NativeId, Is.EqualTo(saved.SystemLink.NativeId));
        }
    }
}

