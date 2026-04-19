using NUnit.Framework;
using PhialeTech.Components.Shared.Services;

namespace PhialeGis.Library.Tests.Demo;

public sealed class DemoPdfPaginationPlannerTests
{
    [Test]
    public void PlanSlices_ReturnsSingleSlice_WhenSourceFitsOnePage()
    {
        var slices = DemoPdfPaginationPlanner.PlanSlices(
            sourceWidth: 1000d,
            sourceHeight: 900d,
            targetPageContentWidth: 500d,
            targetPageContentHeight: 600d);

        Assert.That(slices, Has.Count.EqualTo(1));
        Assert.That(slices[0].SourceOffsetY, Is.EqualTo(0d));
        Assert.That(slices[0].SourceHeight, Is.EqualTo(900d));
        Assert.That(slices[0].RenderedHeight, Is.EqualTo(450d).Within(0.01d));
    }

    [Test]
    public void PlanSlices_SplitsSourceIntoMultiplePages_UsingStableScale()
    {
        var slices = DemoPdfPaginationPlanner.PlanSlices(
            sourceWidth: 1000d,
            sourceHeight: 2500d,
            targetPageContentWidth: 500d,
            targetPageContentHeight: 700d);

        Assert.That(slices, Has.Count.EqualTo(2));
        Assert.That(slices[0].SourceOffsetY, Is.EqualTo(0d));
        Assert.That(slices[0].SourceHeight, Is.EqualTo(1400d).Within(0.01d));
        Assert.That(slices[0].RenderedHeight, Is.EqualTo(700d).Within(0.01d));
        Assert.That(slices[1].SourceOffsetY, Is.EqualTo(1400d).Within(0.01d));
        Assert.That(slices[1].SourceHeight, Is.EqualTo(1100d).Within(0.01d));
        Assert.That(slices[1].RenderedHeight, Is.EqualTo(550d).Within(0.01d));
    }
}
