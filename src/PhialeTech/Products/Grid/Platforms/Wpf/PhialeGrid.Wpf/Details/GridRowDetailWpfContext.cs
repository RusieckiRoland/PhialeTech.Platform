using System;
using PhialeGrid.Core.Details;

namespace PhialeTech.PhialeGrid.Wpf.Surface
{
    public sealed class GridRowDetailWpfContext
    {
        public GridRowDetailWpfContext(GridRowDetailContext coreContext, object contentDescriptor)
        {
            CoreContext = coreContext ?? throw new ArgumentNullException(nameof(coreContext));
            ContentDescriptor = contentDescriptor ?? throw new ArgumentNullException(nameof(contentDescriptor));
        }

        public GridRowDetailContext CoreContext { get; }

        public object ContentDescriptor { get; }
    }
}
