using System.Collections.Generic;
using PhialeGis.Library.Abstractions.Styling;

namespace PhialeGis.Library.Core.Styling
{
    public sealed class InMemorySymbolCatalog : IMutableSymbolCatalog
    {
        private readonly Dictionary<string, SymbolDefinition> _map;

        public InMemorySymbolCatalog()
        {
            _map = new Dictionary<string, SymbolDefinition>
            {
                [BuiltInStyleIds.SymbolSquare] = new SymbolDefinition
                {
                    Id = BuiltInStyleIds.SymbolSquare,
                    Name = "Square",
                    AnchorX = 4d,
                    AnchorY = 4d,
                    DefaultSize = 8d,
                    Primitives = new[]
                    {
                        new StylePrimitive
                        {
                            Kind = SymbolPrimitiveKind.Polygon,
                            Coordinates = new[] { 0d, 0d, 8d, 0d, 8d, 8d, 0d, 8d },
                            StrokeColorArgb = unchecked((int)0xFF163046),
                            FillColorArgb = unchecked((int)0xFFFFFFFF),
                            StrokeWidth = 1d
                        }
                    }
                },
                [BuiltInStyleIds.SymbolTriangle] = new SymbolDefinition
                {
                    Id = BuiltInStyleIds.SymbolTriangle,
                    Name = "Triangle",
                    AnchorX = 4d,
                    AnchorY = 4d,
                    DefaultSize = 8d,
                    Primitives = new[]
                    {
                        new StylePrimitive
                        {
                            Kind = SymbolPrimitiveKind.Polygon,
                            Coordinates = new[] { 4d, 0d, 8d, 8d, 0d, 8d },
                            StrokeColorArgb = unchecked((int)0xFF163046),
                            FillColorArgb = unchecked((int)0xFFFFFFFF),
                            StrokeWidth = 1d
                        }
                    }
                },
                [BuiltInStyleIds.SymbolTick] = new SymbolDefinition
                {
                    Id = BuiltInStyleIds.SymbolTick,
                    Name = "Tick",
                    AnchorX = 4d,
                    AnchorY = 4d,
                    DefaultSize = 8d,
                    Primitives = new[]
                    {
                        new StylePrimitive
                        {
                            Kind = SymbolPrimitiveKind.Polyline,
                            Coordinates = new[] { 0d, 4d, 8d, 4d },
                            StrokeColorArgb = unchecked((int)0xFF163046),
                            FillColorArgb = 0,
                            StrokeWidth = 1d
                        }
                    }
                }
            };
        }

        public IReadOnlyCollection<SymbolDefinition> GetAll()
        {
            return (IReadOnlyCollection<SymbolDefinition>)StyleDefinitionCloner.CloneMany(_map.Values);
        }

        public bool TryGet(string id, out SymbolDefinition definition)
        {
            if (_map.TryGetValue(id ?? string.Empty, out var stored))
            {
                definition = StyleDefinitionCloner.Clone(stored);
                return true;
            }

            definition = null;
            return false;
        }

        public void Set(SymbolDefinition definition)
        {
            if (definition == null)
                throw new System.ArgumentNullException(nameof(definition));

            _map[definition.Id ?? string.Empty] = StyleDefinitionCloner.Clone(definition);
        }
    }
}
