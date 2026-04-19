using System.Collections.Generic;
using PhialeGis.Library.Abstractions.Styling;

namespace PhialeGis.Library.Core.Styling
{
    public sealed class InMemoryFillStyleCatalog : IMutableFillStyleCatalog
    {
        private readonly Dictionary<string, FillStyleDefinition> _map;

        public InMemoryFillStyleCatalog()
        {
            _map = new Dictionary<string, FillStyleDefinition>
            {
                [BuiltInStyleIds.FillSolidWhite] = new FillStyleDefinition
                {
                    Id = BuiltInStyleIds.FillSolidWhite,
                    Name = "Solid White",
                    Kind = FillStyleKind.Solid,
                    ForeColorArgb = unchecked((int)0xFFFFFFFF),
                    BackColorArgb = 0,
                    FillFactor = 1d
                },
                [BuiltInStyleIds.FillHatch45] = new FillStyleDefinition
                {
                    Id = BuiltInStyleIds.FillHatch45,
                    Name = "Hatch 45",
                    Kind = FillStyleKind.Hatch,
                    ForeColorArgb = unchecked((int)0xFF163046),
                    BackColorArgb = unchecked((int)0xFFFFFFFF),
                    FillDirection = FillDirection.Diagonal45,
                    FillFactor = 0.5d,
                    HatchSpacing = 8d,
                    HatchThickness = 1d
                }
            };
        }

        public IReadOnlyCollection<FillStyleDefinition> GetAll()
        {
            return (IReadOnlyCollection<FillStyleDefinition>)StyleDefinitionCloner.CloneMany(_map.Values);
        }

        public bool TryGet(string id, out FillStyleDefinition definition)
        {
            if (_map.TryGetValue(id ?? string.Empty, out var stored))
            {
                definition = StyleDefinitionCloner.Clone(stored);
                return true;
            }

            definition = null;
            return false;
        }

        public void Set(FillStyleDefinition definition)
        {
            if (definition == null)
                throw new System.ArgumentNullException(nameof(definition));

            _map[definition.Id ?? string.Empty] = StyleDefinitionCloner.Clone(definition);
        }
    }
}
