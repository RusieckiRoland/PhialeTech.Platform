using System;

namespace PhialeGrid.Core.Presentation
{
    public sealed class GridCurrentCellDescriptorRequest
    {
        public GridCurrentCellDescriptorKind Kind { get; set; }

        public string Caption { get; set; } = string.Empty;

        public string Header { get; set; } = string.Empty;

        public object Value { get; set; }

        public Type ValueType { get; set; }
    }
}
