using System;

namespace PhialeGrid.Core.State
{
    public sealed class GridNamedViewDefinition
    {
        public GridNamedViewDefinition(string name, string gridState)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("View name is required.", nameof(name));
            }

            Name = name.Trim();
            GridState = gridState ?? string.Empty;
        }

        public string Name { get; }

        public string GridState { get; }
    }
}
