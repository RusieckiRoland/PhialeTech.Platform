using System;
using System.Globalization;

namespace PhialeGrid.Core.Presentation
{
    public static class GridValueFormatter
    {
        public static string FormatDisplayValue(object value, IFormatProvider formatProvider = null)
        {
            var culture = formatProvider as CultureInfo ?? CultureInfo.CurrentCulture;
            if (value == null)
            {
                return string.Empty;
            }

            if (value is DateTime dateTime)
            {
                var hasTimeComponent = dateTime.TimeOfDay != TimeSpan.Zero;
                return dateTime.ToString(hasTimeComponent ? "yyyy-MM-dd HH:mm" : "yyyy-MM-dd", culture);
            }

            if (value is decimal decimalValue)
            {
                return decimalValue.ToString("N2", culture);
            }

            if (value is double doubleValue)
            {
                return doubleValue.ToString("N2", culture);
            }

            if (value is float floatValue)
            {
                return floatValue.ToString("N2", culture);
            }

            if (value is IFormattable formattable)
            {
                return formattable.ToString(null, culture);
            }

            return Convert.ToString(value, culture) ?? string.Empty;
        }
    }
}
