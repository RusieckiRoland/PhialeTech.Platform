using System;
using System.Collections.Generic;
using PhialeGrid.Core.Hierarchy;

namespace PhialeTech.Components.Shared.Services
{
    public sealed class DemoGisHierarchyDefinition
    {
        public DemoGisHierarchyDefinition(IReadOnlyList<GridHierarchyNode<object>> roots, GridHierarchyController<object> controller)
        {
            Roots = roots ?? throw new ArgumentNullException(nameof(roots));
            Controller = controller ?? throw new ArgumentNullException(nameof(controller));
        }

        public IReadOnlyList<GridHierarchyNode<object>> Roots { get; }

        public GridHierarchyController<object> Controller { get; }
    }
}

