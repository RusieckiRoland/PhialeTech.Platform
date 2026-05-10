using System.IO;
using NUnit.Framework;
using PhialeGis.Library.Tests.Support;

namespace PhialeGis.Library.Tests.Architecture
{
    [TestFixture]
    public sealed class StyledRenderPipelineArchitectureTests
    {
        [Test]
        public void GraphicsFacade_AndCoreRenderSystem_ShouldNotCallLegacyPrimitiveRenderMethods()
        {
            var root = GetRepoRoot();
            var graphicsFacade = File.ReadAllText(Path.Combine(root, "src", "PhialeGis", "Core", "PhialeGis.Library.Core", "Graphics", "GraphicsFacade.cs"));
            var renderSystem = File.ReadAllText(Path.Combine(root, "src", "PhialeGis", "Core", "PhialeGis.Library.Core", "Render", "PhGeometryCoreRenderSystem.cs"));

            Assert.That(graphicsFacade, Does.Contain("DrawOverlayStyledLine("));
            Assert.That(graphicsFacade, Does.Not.Contain(".DrawPolyline("));
            Assert.That(graphicsFacade, Does.Not.Contain(".DrawPolygon("));
            Assert.That(graphicsFacade, Does.Not.Contain(".DrawPoint("));
            Assert.That(renderSystem, Does.Not.Contain(".DrawPolyline("));
            Assert.That(renderSystem, Does.Not.Contain(".DrawPolygon("));
            Assert.That(renderSystem, Does.Not.Contain(".DrawPoint("));
        }

        [Test]
        public void RenderBackendContract_ShouldNotExposeLegacyPrimitiveRenderMethods()
        {
            var root = GetRepoRoot();
            var backendContract = File.ReadAllText(Path.Combine(root, "src", "PhialeGis", "Core", "PhialeGis.Library.Geometry", "Systems", "IPhRenderBackend.cs"));
            var skiaBackend = File.ReadAllText(Path.Combine(root, "src", "PhialeGis", "Core", "PhialeGis.Library.Renderer.Skia", "SkiaPhRenderBackend.cs"));
            var overlayInterface = File.ReadAllText(Path.Combine(root, "src", "PhialeGis", "Core", "PhialeGis.Library.Core", "Render", "IStyledOverlayRenderBackend.cs"));

            Assert.That(backendContract, Does.Not.Contain("DrawPolyline("));
            Assert.That(backendContract, Does.Not.Contain("DrawPolygon("));
            Assert.That(backendContract, Does.Not.Contain("DrawPoint("));
            Assert.That(overlayInterface, Does.Contain("DrawOverlayStyledLine("));
            Assert.That(skiaBackend, Does.Contain("DrawOverlayStyledLine("));
            Assert.That(skiaBackend, Does.Not.Contain("public void DrawPolyline("));
            Assert.That(skiaBackend, Does.Not.Contain("public void DrawPolygon("));
            Assert.That(skiaBackend, Does.Not.Contain("public void DrawPoint("));
        }

        private static string GetRepoRoot()
        {
            return RepositoryPaths.GetRepositoryRoot();
        }
    }
}

