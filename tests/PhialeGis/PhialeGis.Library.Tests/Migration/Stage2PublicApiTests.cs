using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using PhialeGis.Library.Abstractions.Interactions;
using PhialeGis.Library.Abstractions.Ui.Rendering;
using PhialeGis.Library.Core.Interactions;

namespace PhialeGis.Library.Tests.Migration
{
    [TestFixture]
    [Category("Unit")]
    [Category("MigrationGuard")]
    public sealed class Stage2PublicApiTests
    {
        [Test]
        public void ExtensionContracts_ArePublic()
        {
            Assert.IsTrue(typeof(IUserInteractive).IsPublic, "IUserInteractive should be public for external controls.");
            Assert.IsTrue(typeof(IRenderingComposition).IsPublic, "IRenderingComposition should be public for external controls.");
        }

        [Test]
        public void GisInteractionManager_ExposesTypedRegisterAndUnregisterOverloads()
        {
            var managerType = typeof(GisInteractionManager);

            Assert.NotNull(managerType.GetMethod("RegisterControl", new[] { typeof(IRenderingComposition) }));
            Assert.NotNull(managerType.GetMethod("RegisterControl", new[] { typeof(IEditorInteractive) }));
            Assert.NotNull(managerType.GetMethod("RegisterControl", new[] { typeof(object) }));

            Assert.NotNull(managerType.GetMethod("UnregisterControl", new[] { typeof(IRenderingComposition) }));
            Assert.NotNull(managerType.GetMethod("UnregisterControl", new[] { typeof(IEditorInteractive) }));
            Assert.NotNull(managerType.GetMethod("UnregisterControl", new[] { typeof(object) }));
        }

        [Test]
        public void InternalsVisibleTo_IsReduced()
        {
            var abstractionsFriends = typeof(IGisInteractionManager).Assembly
                .GetCustomAttributes<InternalsVisibleToAttribute>()
                .Select(a => a.AssemblyName)
                .ToArray();

            Assert.IsEmpty(abstractionsFriends, "Abstractions should not require friend assemblies after exposing public extension contracts.");

            var coreFriends = typeof(GisInteractionManager).Assembly
                .GetCustomAttributes<InternalsVisibleToAttribute>()
                .Select(a => a.AssemblyName)
                .ToArray();

            CollectionAssert.DoesNotContain(coreFriends, "PhialeGis.Library.Core");
            CollectionAssert.DoesNotContain(coreFriends, "PhialeGis.Library.Drawing");
            CollectionAssert.DoesNotContain(coreFriends, "PhialeGis.Library.Drawing.Wpf");
            CollectionAssert.DoesNotContain(coreFriends, "PhialeGis.ComponentSandbox.Avalonia");
        }
    }
}
