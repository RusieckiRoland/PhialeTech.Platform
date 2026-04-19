using PhialeGis.Library.Abstractions.Ui.Rendering;

namespace PhialeGis.Library.Abstractions.Interactions
{
    public sealed class SnapRequest
    {
        public object TargetDraw { get; set; }

        public IViewport Viewport { get; set; }

        public double ModelX { get; set; }

        public double ModelY { get; set; }

        public double ScreenX { get; set; }

        public double ScreenY { get; set; }

        public double TolerancePx { get; set; } = 12d;

        public SnapModes Modes { get; set; } = SnapModes.Default;
    }
}
