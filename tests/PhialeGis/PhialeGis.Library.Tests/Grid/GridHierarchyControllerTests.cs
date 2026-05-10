using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using PhialeGrid.Core.Hierarchy;

namespace PhialeGis.Library.Tests.Grid
{
    public class GridHierarchyControllerTests
    {
        [Test]
        public async Task ExpandAsync_LoadsChildrenOnceAndFlattens()
        {
            var provider = new FakeHierarchyProvider();
            var controller = new GridHierarchyController<string>(provider);
            var root = new GridHierarchyNode<string>("1", "root", true);

            await controller.ExpandAsync(root);
            await controller.ExpandAsync(root);

            Assert.That(provider.LoadCalls, Is.EqualTo(1));
            Assert.That(root.Children.Count, Is.EqualTo(2));

            var flat = controller.Flatten(new[] { root });
            Assert.That(flat.Count, Is.EqualTo(3));

            controller.Collapse(root);
            flat = controller.Flatten(new[] { root });
            Assert.That(flat.Count, Is.EqualTo(1));
        }

        private sealed class FakeHierarchyProvider : IGridHierarchyProvider<string>
        {
            public int LoadCalls { get; private set; }

            public Task<IReadOnlyList<GridHierarchyNode<string>>> LoadChildrenAsync(GridHierarchyNode<string> parent, CancellationToken cancellationToken)
            {
                LoadCalls++;
                IReadOnlyList<GridHierarchyNode<string>> children = new[]
                {
                    new GridHierarchyNode<string>(parent.Id + ".1", "c1", false),
                    new GridHierarchyNode<string>(parent.Id + ".2", "c2", false),
                };

                return Task.FromResult(children);
            }
        }
    }
}

