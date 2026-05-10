using System;

namespace PhialeGrid.Core.Details
{
    public sealed class GridRowDetailDescriptor
    {
        public GridRowDetailDescriptor(
            string detailKey,
            string ownerRowKey,
            GridRowDetailHeightPolicy heightPolicy,
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
            HeightPolicy = heightPolicy ?? throw new ArgumentNullException(nameof(heightPolicy));
            ContentDescriptor = contentDescriptor ?? throw new ArgumentNullException(nameof(contentDescriptor));
        }

        public string DetailKey { get; }

        public string OwnerRowKey { get; }

        public GridRowDetailHeightPolicy HeightPolicy { get; }

        public object ContentDescriptor { get; }
    }
}
