using System;
using System.Collections.Generic;

namespace PhialeGis.Library.Core.Scene
{
    /// <summary>
    /// Simple dictionary-based ECS world with a classic entity registry.
    /// </summary>
    public sealed class SimpleWorld : IWorld
    {
        private int _nextId = 1;

        // Entity registry used by systems to iterate deterministically.
        private readonly List<Entity> _entities = new List<Entity>(1024);
        public IReadOnlyList<Entity> Entities => _entities;

        // Component store: component type -> (entityId -> boxed component)
        private readonly Dictionary<Type, Dictionary<int, object>> _stores =
            new Dictionary<Type, Dictionary<int, object>>(32);

        public Entity Create()
        {
            var e = new Entity(_nextId++);
            _entities.Add(e);
            return e;
        }

        public void Destroy(Entity e)
        {
            // Remove from entity registry
            for (int i = 0; i < _entities.Count; i++)
            {
                if (_entities[i].Id == e.Id)
                {
                    _entities.RemoveAt(i);
                    break;
                }
            }

            // Purge this entity from all component stores
            foreach (var store in _stores.Values)
                store.Remove(e.Id);
        }

        public void Add<T>(Entity e, T component)
        {
            var store = GetStore(typeof(T));
            store[e.Id] = component;
        }

        public bool Remove<T>(Entity e)
        {
            if (_stores.TryGetValue(typeof(T), out var store))
                return store.Remove(e.Id);
            return false;
        }

        public bool Has<T>(Entity e)
        {
            return _stores.TryGetValue(typeof(T), out var store) && store.ContainsKey(e.Id);
        }

        public bool TryGet<T>(Entity e, out T component)
        {
            if (_stores.TryGetValue(typeof(T), out var store) &&
                store.TryGetValue(e.Id, out var boxed) &&
                boxed is T typed)
            {
                component = typed;
                return true;
            }
            component = default(T);
            return false;
        }

        // Query helpers (optional)
        public IEnumerable<Entity> All<T1, T2>()
        {
            if (!_stores.TryGetValue(typeof(T1), out var s1) ||
                !_stores.TryGetValue(typeof(T2), out var s2))
                yield break;

            var first = s1.Count <= s2.Count ? s1 : s2;
            var second = s1.Count <= s2.Count ? s2 : s1;

            foreach (var kv in first)
                if (second.ContainsKey(kv.Key))
                    yield return new Entity(kv.Key);
        }

        public IEnumerable<Entity> All<T1>()
        {
            if (!_stores.TryGetValue(typeof(T1), out var s1))
                yield break;

            foreach (var kv in s1)
                yield return new Entity(kv.Key);
        }

        private Dictionary<int, object> GetStore(Type t)
        {
            if (!_stores.TryGetValue(t, out var store))
            {
                store = new Dictionary<int, object>(1024);
                _stores[t] = store;
            }
            return store;
        }
    }
}
