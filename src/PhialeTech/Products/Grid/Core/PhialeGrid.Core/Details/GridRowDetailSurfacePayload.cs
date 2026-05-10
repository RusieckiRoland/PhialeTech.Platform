using System;

namespace PhialeGrid.Core.Details
{
    public sealed class GridRowDetailSurfacePayload
    {
        public GridRowDetailSurfacePayload(
            string detailKey,
            string ownerRowKey,
            GridRowDetailContext context,
            object contentDescriptor)
        {
            if (string.IsNullOrWhiteSpace(detailKey))
            {
                throw new ArgumentException("Row detail key is required.", nameof(detailKey));
            }

            if (string.IsNullOrWhiteSpace(ownerRowKey))
            {
                throw new ArgumentException("Owner row key is required.", nameof(ownerRowKey));
            }

            DetailKey = detailKey;
            OwnerRowKey = ownerRowKey;
            Context = context ?? throw new ArgumentNullException(nameof(context));
            ContentDescriptor = contentDescriptor ?? throw new ArgumentNullException(nameof(contentDescriptor));
        }

        public string DetailKey { get; }

        public string OwnerRowKey { get; }

        public GridRowDetailContext Context { get; }

        public object ContentDescriptor { get; }
    }
}
