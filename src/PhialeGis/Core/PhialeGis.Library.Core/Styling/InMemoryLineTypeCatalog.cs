using System.Collections.Generic;
using PhialeGis.Library.Abstractions.Styling;

namespace PhialeGis.Library.Core.Styling
{
    public sealed class InMemoryLineTypeCatalog : IMutableLineTypeCatalog
    {
        private readonly Dictionary<string, LineTypeDefinition> _map;

        public InMemoryLineTypeCatalog()
        {
            _map = new Dictionary<string, LineTypeDefinition>
            {
                [BuiltInStyleIds.LineSolid] = new LineTypeDefinition
                {
                    Id = BuiltInStyleIds.LineSolid,
                    Name = "Solid",
                    Kind = LineTypeKind.SimpleDash,
                    Flow = true,
                    Repeat = 0d,
                    ColorArgb = unchecked((int)0xFF163046),
                    Width = 2d,
                    Linecap = StrokeLinecap.Round,
                    Linejoin = StrokeLinejoin.Miter,
                    MiterLimit = 4d
                },
                [BuiltInStyleIds.LineDash] = new LineTypeDefinition
                {
                    Id = BuiltInStyleIds.LineDash,
                    Name = "Dash",
                    Kind = LineTypeKind.SimpleDash,
                    Flow = true,
                    Repeat = 14d,
                    ColorArgb = unchecked((int)0xFF163046),
                    Width = 2d,
                    Linecap = StrokeLinecap.Round,
                    Linejoin = StrokeLinejoin.Round,
                    DashPattern = new[] { 8d, 6d }
                },
                [BuiltInStyleIds.LineTicksPerpendicular] = new LineTypeDefinition
                {
                    Id = BuiltInStyleIds.LineTicksPerpendicular,
                    Name = "Perpendicular Ticks",
                    Kind = LineTypeKind.VectorStamp,
                    Flow = true,
                    Repeat = 10d,
                    ColorArgb = unchecked((int)0xFF163046),
                    Width = 2d,
                    SymbolId = BuiltInStyleIds.SymbolTick,
                    StampSize = 5d,
                    Gap = 10d,
                    InitialGap = 0d,
                    OrientToTangent = true,
                    Perpendicular = true
                },
                [BuiltInStyleIds.LineDoubleTrack] = new LineTypeDefinition
                {
                    Id = BuiltInStyleIds.LineDoubleTrack,
                    Name = "Double Track",
                    Kind = LineTypeKind.RasterPattern,
                    Flow = true,
                    Repeat = 8d,
                    ColorArgb = unchecked((int)0xFF163046),
                    Width = 1d,
                    RasterPattern = new RasterLinePattern
                    {
                        Lanes = new[]
                        {
                            new RasterLinePatternLane
                            {
                                OffsetY = -3d,
                                RunLengths = new[] { 8, 4 },
                                StartsWithDash = true
                            },
                            new RasterLinePatternLane
                            {
                                OffsetY = 3d,
                                RunLengths = new[] { 8, 4 },
                                StartsWithDash = true
                            }
                        }
                    }
                }
            };
        }

        public IReadOnlyCollection<LineTypeDefinition> GetAll()
        {
            return (IReadOnlyCollection<LineTypeDefinition>)StyleDefinitionCloner.CloneMany(_map.Values);
        }

        public bool TryGet(string id, out LineTypeDefinition definition)
        {
            if (_map.TryGetValue(id ?? string.Empty, out var stored))
            {
                definition = StyleDefinitionCloner.Clone(stored);
                return true;
            }

            definition = null;
            return false;
        }

        public void Set(LineTypeDefinition definition)
        {
            if (definition == null)
                throw new System.ArgumentNullException(nameof(definition));

            _map[definition.Id ?? string.Empty] = StyleDefinitionCloner.Clone(definition);
        }
    }
}
