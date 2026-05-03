using System.Linq;
using NUnit.Framework;
using PhialeTech.Components.Shared.Services;

namespace PhialeGis.Library.Tests.Demo;

public sealed class DemoPdfBookPlanBuilderTests
{
    [Test]
    public void Build_ReturnsDrawerGroupsInCatalogOrder()
    {
        var plan = DemoPdfBookPlanBuilder.Build(new DemoFeatureCatalog(), "pl");

        Assert.That(plan.Chapters.Select(chapter => chapter.DrawerGroupId), Is.EqualTo(new[]
        {
            "license",
            "foundations",
            "architecture",
            "grid",
            "active-layer-selector",
            "yaml-ui",
            "web-components",
        }));
    }

    [Test]
    public void Build_IncludesExamplesOrderedWithinChapter()
    {
        var plan = DemoPdfBookPlanBuilder.Build(new DemoFeatureCatalog(), "en");
        var webComponents = plan.Chapters.Single(chapter => chapter.DrawerGroupId == "web-components");

        Assert.That(webComponents.Examples.Select(example => example.Id), Is.EqualTo(new[]
        {
            "web-host",
            "pdf-viewer",
            "report-designer",
            "monaco-editor",
            "document-editor",
            "web-component-scroll-host",
        }));
    }
}
