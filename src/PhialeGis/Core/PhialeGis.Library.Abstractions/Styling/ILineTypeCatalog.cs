using System.Collections.Generic;

namespace PhialeGis.Library.Abstractions.Styling
{
    public interface ILineTypeCatalog
    {
        IReadOnlyCollection<LineTypeDefinition> GetAll();

        bool TryGet(string id, out LineTypeDefinition definition);
    }
}
