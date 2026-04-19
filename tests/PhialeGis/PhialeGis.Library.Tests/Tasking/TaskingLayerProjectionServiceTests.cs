using NUnit.Framework;
using PhialeGis.Library.Domain.Map;
using PhialeGis.Library.Domain.Tasking;
using PhialeGis.Library.Geometry.Spatial.Primitives;
using PhialeGis.Library.Geometry.Spatial.Single;
using PhialeGis.Library.Sync.Tasking;

namespace PhialeGis.Library.Tests.Tasking
{
    [TestFixture]
    public sealed class TaskingLayerProjectionServiceTests
    {
        [Test]
        public void BuildLayer_ShouldProjectSpatialTasksToMemoryLayer()
        {
            var projector = new TaskingLayerProjectionService();
            var tasks = new[]
            {
                new TaskItem
                {
                    Id = "mock:1",
                    Title = "Inspect pipe",
                    Status = TaskItemStatus.Ready,
                    Priority = TaskItemPriority.High,
                    SystemLink = new TaskSystemLink
                    {
                        ProviderId = "mock",
                        NativeId = "1",
                    },
                    Assignee = new TaskAssignee
                    {
                        DisplayName = "Anna Kowalska",
                    },
                    Location = new TaskLocationBinding
                    {
                        LayerName = "Pipes",
                        FeatureId = 11,
                        Geometry = new PhPointEntity(new PhPoint(1, 2)),
                    },
                },
                new TaskItem
                {
                    Id = "mock:2",
                    Title = "Non spatial task",
                },
            };

            var layer = projector.BuildLayer(tasks, "Dispatch Tasks");

            Assert.That(layer.Name, Is.EqualTo("Dispatch Tasks"));
            Assert.That(layer.Type, Is.EqualTo(PhLayerType.Memory));
            Assert.That(layer.Features.Count, Is.EqualTo(1));
            Assert.That(layer.Features[0].Attributes["taskId"], Is.EqualTo("mock:1"));
            Assert.That(layer.Features[0].Attributes["status"], Is.EqualTo("Ready"));
            Assert.That(layer.Features[0].Attributes["sourceLayer"], Is.EqualTo("Pipes"));
            Assert.That(layer.Features[0].Attributes["sourceFeatureId"], Is.EqualTo(11L));
            Assert.That(layer.Features[0].Attributes["assignee"], Is.EqualTo("Anna Kowalska"));
        }
    }
}
