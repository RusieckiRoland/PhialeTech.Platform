// PhialeGis.Library.Core.Scene/IWorld.cs
using System.Collections.Generic;

namespace PhialeGis.Library.Core.Scene
{
    public interface IWorld
    {
        // Entity lifecycle
        Entity Create();
        void Destroy(Entity e);

        // Component ops
        void Add<T>(Entity e, T component);
        bool Remove<T>(Entity e);
        bool Has<T>(Entity e);
        bool TryGet<T>(Entity e, out T component);

        // Classic ECS entity registry (systems iterate over this)
        IReadOnlyList<Entity> Entities { get; }

        // Optional query helpers (keep if you use them)
        IEnumerable<Entity> All<T1, T2>();
        IEnumerable<Entity> All<T1>();
    }
}
