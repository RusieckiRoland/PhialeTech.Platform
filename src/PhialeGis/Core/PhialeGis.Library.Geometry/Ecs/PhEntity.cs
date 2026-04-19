namespace PhialeGis.Library.Geometry.Ecs
{
    public struct PhEntity
    {
        public int Id;
        public PhEntity(int id) { Id = id; }
        public override string ToString() => Id.ToString();
    }
}
