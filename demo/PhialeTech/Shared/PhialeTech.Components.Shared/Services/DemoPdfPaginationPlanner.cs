using System;
using System.Collections.Generic;

namespace PhialeTech.Components.Shared.Services
{
    public static class DemoPdfPaginationPlanner
    {
        public static IReadOnlyList<DemoPdfPageSlice> PlanSlices(
            double sourceWidth,
            double sourceHeight,
            double targetPageContentWidth,
            double targetPageContentHeight)
        {
            if (sourceWidth <= 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(sourceWidth));
            }

            if (sourceHeight <= 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(sourceHeight));
            }

            if (targetPageContentWidth <= 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(targetPageContentWidth));
            }

            if (targetPageContentHeight <= 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(targetPageContentHeight));
            }

            var slices = new List<DemoPdfPageSlice>();
            var scale = targetPageContentWidth / sourceWidth;
            var maxSliceHeight = targetPageContentHeight / scale;
            var sourceOffsetY = 0d;

            while (sourceOffsetY < sourceHeight - 0.01d)
            {
                var remainingHeight = sourceHeight - sourceOffsetY;
                var sliceHeight = Math.Min(maxSliceHeight, remainingHeight);
                var renderedHeight = sliceHeight * scale;
                slices.Add(new DemoPdfPageSlice(sourceOffsetY, sliceHeight, renderedHeight));
                sourceOffsetY += sliceHeight;
            }

            return slices;
        }
    }

    public sealed class DemoPdfPageSlice
    {
        public DemoPdfPageSlice(double sourceOffsetY, double sourceHeight, double renderedHeight)
        {
            SourceOffsetY = sourceOffsetY;
            SourceHeight = sourceHeight;
            RenderedHeight = renderedHeight;
        }

        public double SourceOffsetY { get; }

        public double SourceHeight { get; }

        public double RenderedHeight { get; }
    }
}
