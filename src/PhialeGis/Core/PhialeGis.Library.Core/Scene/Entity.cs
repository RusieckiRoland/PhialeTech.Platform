using System;

namespace PhialeGis.Library.Core.Scene
{
    /// <summary>
    /// Lightweight entity identifier for the ECS world.
    /// </summary>
    public struct Entity : IEquatable<Entity>
    {
        public int Id { get; private set; }

        public Entity(int id) { Id = id; }

        public bool Equals(Entity other) => Id == other.Id;
        public override bool Equals(object obj) => obj is Entity e && Equals(e);
        public override int GetHashCode() => Id;

        public static bool operator ==(Entity a, Entity b) => a.Id == b.Id;
        public static bool operator !=(Entity a, Entity b) => a.Id != b.Id;

        public override string ToString() => "Entity(" + Id + ")";
    }
}
