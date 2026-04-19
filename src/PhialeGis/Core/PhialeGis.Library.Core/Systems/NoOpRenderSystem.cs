using PhialeGis.Library.Core.Render;
using PhialeGis.Library.Core.Scene;

namespace PhialeGis.Library.Core.Systems
{
    /// <summary>
    /// Placeholder renderer (draws nothing). Used to wire the pipeline first.
    /// </summary>
    public sealed class NoOpRenderSystem : IRenderSystem
    {
        public void Render(IWorld world, RenderContext ctx)
        {
            // Intentionally empty.
            // Next steps:
            // 1) Query visible entities via spatial index (R-tree).
            // 2) Group by symbolizers.
            // 3) Draw onto ctx.Canvas using ctx.Viewport transforms.
        }
    }
}
