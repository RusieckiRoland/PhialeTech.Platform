using System;
using System.Globalization;

namespace PhialeGrid.Core.Presentation
{
    public sealed class GridCurrentCellDescriptionBuilder
    {
        public string BuildDescription(GridCurrentCellDescriptorRequest request, IFormatProvider formatProvider = null)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            switch (request.Kind)
            {
                case GridCurrentCellDescriptorKind.Empty:
                    return string.Empty;
                case GridCurrentCellDescriptorKind.Caption:
                    return request.Caption ?? string.Empty;
                case GridCurrentCellDescriptorKind.Data:
                    if (string.IsNullOrWhiteSpace(request.Header))
                    {
                        return string.Empty;
                    }

                    return request.Header + ": " + GridValueFormatter.FormatDisplayValue(
                        request.Value,
                        formatProvider as CultureInfo ?? CultureInfo.CurrentCulture);
                default:
                    return string.Empty;
            }
        }
    }
}
