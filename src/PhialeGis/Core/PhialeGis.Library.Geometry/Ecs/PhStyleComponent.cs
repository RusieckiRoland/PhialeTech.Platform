// PhialeGis.Library.Geometry/Ecs/PhStyleComponent.cs
namespace PhialeGis.Library.Geometry.Ecs
{
    /// <summary>
    /// Per-entity style references.
    /// Rendering is resolved exclusively through catalog identifiers.
    /// </summary>
    public sealed class PhStyleComponent : IPhComponent
    {
        public string LineTypeId { get; set; } = string.Empty;
        public string SymbolId { get; set; } = string.Empty;
        public string FillStyleId { get; set; } = string.Empty;
    }
}
