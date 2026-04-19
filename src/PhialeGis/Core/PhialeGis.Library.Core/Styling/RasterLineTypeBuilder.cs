using System;
using System.Collections.Generic;
using PhialeGis.Library.Abstractions.Styling;

namespace PhialeGis.Library.Core.Styling
{
    public sealed class RasterLineTypeBuilder
    {
        public RasterLinePattern BuildFromArgb32(int width, int height, IReadOnlyList<int> pixels, byte alphaThreshold = 16)
        {
            if (width <= 0)
                throw new ArgumentOutOfRangeException(nameof(width));

            if (height <= 0)
                throw new ArgumentOutOfRangeException(nameof(height));

            if (pixels == null)
                throw new ArgumentNullException(nameof(pixels));

            if (pixels.Count != width * height)
            {
                throw new ArgumentException(
                    "Pixel buffer length must equal width * height.",
                    nameof(pixels));
            }

            var lanes = new List<RasterLinePatternLane>();
            var centerY = (height - 1) / 2d;

            for (int y = 0; y < height; y++)
            {
                if (!TryBuildLane(width, pixels, y, alphaThreshold, out var lane))
                    continue;

                lane.OffsetY = y - centerY;
                lanes.Add(lane);
            }

            return new RasterLinePattern
            {
                Lanes = lanes.ToArray()
            };
        }

        private static bool TryBuildLane(
            int width,
            IReadOnlyList<int> pixels,
            int y,
            byte alphaThreshold,
            out RasterLinePatternLane lane)
        {
            lane = null;
            var rowStart = y * width;
            var firstIsDash = IsVisible(pixels[rowStart], alphaThreshold);
            var currentIsDash = firstIsDash;
            var runLengths = new List<int>();
            var currentLength = 1;
            var hasVisiblePixel = currentIsDash;

            for (int x = 1; x < width; x++)
            {
                var isDash = IsVisible(pixels[rowStart + x], alphaThreshold);
                if (isDash)
                    hasVisiblePixel = true;

                if (isDash == currentIsDash)
                {
                    currentLength++;
                    continue;
                }

                runLengths.Add(currentLength);
                currentLength = 1;
                currentIsDash = isDash;
            }

            runLengths.Add(currentLength);

            if (!hasVisiblePixel)
                return false;

            lane = new RasterLinePatternLane
            {
                StartsWithDash = firstIsDash,
                RunLengths = runLengths.ToArray()
            };
            return true;
        }

        private static bool IsVisible(int argb, byte alphaThreshold)
        {
            var value = unchecked((uint)argb);
            return ((value >> 24) & 0xFF) >= alphaThreshold;
        }
    }
}
