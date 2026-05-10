using System.Linq;
using NUnit.Framework;
using PhialeGrid.Core.Columns;

namespace PhialeGis.Library.Tests.Grid
{
    public class GridLayoutStateTests
    {
        [Test]
        public void ReorderColumn_ChangesDisplayOrder()
        {
            var state = CreateState();

            state.ReorderColumn("Age", 0);

            Assert.That(state.Columns.OrderBy(c => c.DisplayIndex).Select(c => c.Id).ToArray(),
                Is.EqualTo(new[] { "Age", "Id", "Name" }));
        }

        [Test]
        public void ResizeColumn_RespectsMinWidth()
        {
            var state = CreateState();
            state.SetMinWidth("Name", 80);

            state.ResizeColumn("Name", 30);

            Assert.That(state.Columns.Single(c => c.Id == "Name").Width, Is.EqualTo(80));
        }

        [Test]
        public void Snapshot_RoundTripRestoresState()
        {
            var state = CreateState();
            state.SetColumnVisibility("Name", false);
            state.SetFrozen("Id", true);
            var snapshot = state.CreateSnapshot();

            var restored = CreateState();
            restored.ApplySnapshot(snapshot);

            Assert.That(restored.Columns.Single(c => c.Id == "Name").IsVisible, Is.False);
            Assert.That(restored.Columns.Single(c => c.Id == "Id").IsFrozen, Is.True);
        }

        private static GridLayoutState CreateState()
        {
            return new GridLayoutState(new[]
            {
                new GridColumnDefinition("Id", "Id", displayIndex: 0),
                new GridColumnDefinition("Name", "Name", displayIndex: 1),
                new GridColumnDefinition("Age", "Age", displayIndex: 2),
            });
        }
    }
}

