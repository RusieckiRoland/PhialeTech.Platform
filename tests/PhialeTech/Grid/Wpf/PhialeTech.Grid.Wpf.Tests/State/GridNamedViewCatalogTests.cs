using System.Linq;
using NUnit.Framework;
using PhialeGrid.Core.State;

namespace PhialeGrid.Wpf.Tests.State
{
    [TestFixture]
    public sealed class GridNamedViewCatalogTests
    {
        [Test]
        public void SaveReplaceRemoveAndCodecRoundtrip_ShouldPreserveNamedViews()
        {
            var catalog = new GridNamedViewCatalog();

            catalog.Save(new GridNamedViewDefinition("Operations", "state-1"));
            catalog.Save(new GridNamedViewDefinition("Review", "state-2"));
            catalog.Save(new GridNamedViewDefinition("operations", "state-3"));

            Assert.Multiple(() =>
            {
                Assert.That(catalog.Names.Select(name => name.ToLowerInvariant()), Is.EqualTo(new[] { "operations", "review" }));
                Assert.That(catalog.TryGet("OPERATIONS", out var operationsView), Is.True);
                Assert.That(operationsView.GridState, Is.EqualTo("state-3"));
            });

            var encoded = catalog.Encode();
            var restoredCatalog = GridNamedViewCatalog.Decode(encoded);

            Assert.Multiple(() =>
            {
                Assert.That(restoredCatalog.Names.Select(name => name.ToLowerInvariant()), Is.EqualTo(new[] { "operations", "review" }));
                Assert.That(restoredCatalog.TryGet("Review", out var reviewView), Is.True);
                Assert.That(reviewView.GridState, Is.EqualTo("state-2"));
            });

            Assert.That(restoredCatalog.Remove("operations"), Is.True);
            Assert.That(restoredCatalog.Names.Single(), Is.EqualTo("Review"));
        }
    }
}

