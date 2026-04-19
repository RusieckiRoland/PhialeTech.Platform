using System;
using System.Collections.Generic;
using System.Linq;

namespace PhialeGrid.Core.State
{
    public sealed class GridNamedViewCatalog
    {
        private readonly Dictionary<string, GridNamedViewDefinition> _views;

        public GridNamedViewCatalog(IEnumerable<GridNamedViewDefinition> views = null)
        {
            _views = new Dictionary<string, GridNamedViewDefinition>(StringComparer.OrdinalIgnoreCase);
            foreach (var view in views ?? Array.Empty<GridNamedViewDefinition>())
            {
                Save(view);
            }
        }

        public IReadOnlyList<GridNamedViewDefinition> Views =>
            _views.Values
                .OrderBy(view => view.Name, StringComparer.OrdinalIgnoreCase)
                .ToArray();

        public IReadOnlyList<string> Names => Views.Select(view => view.Name).ToArray();

        public void Save(GridNamedViewDefinition view)
        {
            if (view == null)
            {
                throw new ArgumentNullException(nameof(view));
            }

            _views[view.Name] = view;
        }

        public bool TryGet(string name, out GridNamedViewDefinition view)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                view = null;
                return false;
            }

            return _views.TryGetValue(name.Trim(), out view);
        }

        public bool Remove(string name)
        {
            return !string.IsNullOrWhiteSpace(name) && _views.Remove(name.Trim());
        }

        public string Encode()
        {
            return GridNamedViewCodec.Encode(Views);
        }

        public static GridNamedViewCatalog Decode(string encoded)
        {
            return new GridNamedViewCatalog(GridNamedViewCodec.Decode(encoded));
        }
    }
}
