using PhialeGis.Library.Core.Render;
using PhialeGis.Library.Core.Scene;

namespace PhialeGis.Library.Core.Systems
{
    /// <summary>
    /// Render system contract: draws the current world to the provided context.
    /// </summary>
    public interface IRenderSystem
    {
        void Render(IWorld world, RenderContext ctx);
    }
}
