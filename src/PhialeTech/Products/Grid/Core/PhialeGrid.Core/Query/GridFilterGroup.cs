using System;
using System.Collections.Generic;

namespace PhialeGrid.Core.Query
{
    public sealed class GridFilterGroup
    {
        public GridFilterGroup(IReadOnlyList<GridFilterDescriptor> filters, GridLogicalOperator logicalOperator)
        {
            Filters = filters ?? throw new ArgumentNullException(nameof(filters));
            LogicalOperator = logicalOperator;
        }

        public IReadOnlyList<GridFilterDescriptor> Filters { get; }

        public GridLogicalOperator LogicalOperator { get; }

        public static GridFilterGroup EmptyAnd()
        {
            return new GridFilterGroup(Array.Empty<GridFilterDescriptor>(), GridLogicalOperator.And);
        }
    }
}
