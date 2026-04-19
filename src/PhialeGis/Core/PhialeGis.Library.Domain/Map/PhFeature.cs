namespace PhialeGis.Library.Domain.Map
{
    /// <summary>
    /// Feature with an Id and a geometry payload (Point2D / Polyline2D / ...).
    /// Geometry remains a plain DTO for clean separation from rendering types.
    /// </summary>
    public sealed class PhFeature
    {
        public long Id { get; set; }
        public object Geometry { get; set; } // Domain.Geometry.Point2D / Polyline2D / ...

        public PhFeature(long id, object geometry)
        {
            Id = id;
            Geometry = geometry;
        }
    }
}
