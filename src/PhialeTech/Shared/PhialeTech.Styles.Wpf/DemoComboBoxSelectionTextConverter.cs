using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace PhialeTech.Styles.Wpf
{
    public sealed class DemoComboBoxSelectionTextConverter : IValueConverter
    {
        private static readonly string[] PreferredPropertyNames =
        {
            "DisplayName",
            "FileName",
            "Text",
            "Header",
            "Label",
            "Title",
            "Name",
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch (value)
            {
                case null:
                    return string.Empty;
                case string text:
                    return text;
                case ComboBoxItem comboBoxItem when comboBoxItem.Content is string contentText:
                    return contentText;
                case ComboBoxItem comboBoxItem when comboBoxItem.Content != null:
                    return comboBoxItem.Content.ToString() ?? string.Empty;
            }

            var preferredText = TryReadPreferredProperty(value);
            return string.IsNullOrWhiteSpace(preferredText)
                ? value.ToString() ?? string.Empty
                : preferredText;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        private static string TryReadPreferredProperty(object value)
        {
            var type = value.GetType();
            foreach (var propertyName in PreferredPropertyNames)
            {
                var property = type.GetProperty(propertyName);
                if (property == null || !property.CanRead || property.PropertyType != typeof(string))
                {
                    continue;
                }

                var propertyValue = property.GetValue(value) as string;
                if (!string.IsNullOrWhiteSpace(propertyValue))
                {
                    return propertyValue;
                }
            }

            return string.Empty;
        }
    }
}
