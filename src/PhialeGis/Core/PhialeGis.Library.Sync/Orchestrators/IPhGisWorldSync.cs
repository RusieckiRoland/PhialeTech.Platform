using PhialeGis.Library.Abstractions.Ui.Rendering;
using PhialeGis.Library.Core.Scene;
using PhialeGis.Library.Domain.Map;

namespace PhialeGis.Library.Sync.Orchestrators
{
    /// <summary>
    /// Projects visible PhGis layers into the ECS world for retained-mode rendering.
    /// Implementations may perform one-shot rebuilds or incremental diff-based updates.
    /// </summary>
    public interface IPhGisWorldSync
    {
        /// <summary>
        /// Synchronize Domain model (PhGis) to Core ECS (IWorld).
        /// Viewport is provided for potential culling decisions (not required for the simplest implementation).
        /// </summary>
        void Sync(PhGis gis, IWorld world, IViewport viewport);
    }
}
