using System;

namespace PhialeGrid.Core.Interaction
{
    /// <summary>
    /// Neutral host signal informing Core about viewport scroll offsets.
    /// </summary>
    public sealed class GridHostScrollChangedInput : GridInputEvent
    {
        public GridHostScrollChangedInput(
            DateTime timestamp,
            double horizontalOffset,
            double verticalOffset,
            GridInputModifiers modifiers = GridInputModifiers.None)
            : base(timestamp, modifiers)
        {
            HorizontalOffset = horizontalOffset;
            VerticalOffset = verticalOffset;
        }

        public double HorizontalOffset { get; }

        public double VerticalOffset { get; }
    }

    /// <summary>
    /// Neutral host signal informing Core about measured viewport size.
    /// </summary>
    public sealed class GridHostViewportChangedInput : GridInputEvent
    {
        public GridHostViewportChangedInput(
            DateTime timestamp,
            double width,
            double height,
            GridInputModifiers modifiers = GridInputModifiers.None)
            : base(timestamp, modifiers)
        {
            Width = width;
            Height = height;
        }

        public double Width { get; }

        public double Height { get; }
    }
}
