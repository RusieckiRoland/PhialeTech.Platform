using System.Collections.Generic;

namespace PhialeGis.Library.Abstractions.Styling
{
    public interface IFillStyleCatalog
    {
        IReadOnlyCollection<FillStyleDefinition> GetAll();

        bool TryGet(string id, out FillStyleDefinition definition);
    }
}
