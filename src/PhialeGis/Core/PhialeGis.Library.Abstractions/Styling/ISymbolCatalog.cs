using System.Collections.Generic;

namespace PhialeGis.Library.Abstractions.Styling
{
    public interface ISymbolCatalog
    {
        IReadOnlyCollection<SymbolDefinition> GetAll();

        bool TryGet(string id, out SymbolDefinition definition);
    }
}
