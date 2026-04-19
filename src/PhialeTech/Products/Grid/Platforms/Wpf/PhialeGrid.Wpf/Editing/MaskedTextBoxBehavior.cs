using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace PhialeTech.PhialeGrid.Wpf.Controls.Editing
{
    public static class MaskedTextBoxBehavior
    {
        private static readonly DependencyProperty LastValidTextProperty =
            DependencyProperty.RegisterAttached(
                "LastValidText",
                typeof(string),
                typeof(MaskedTextBoxBehavior),
                new PropertyMetadata(string.Empty));

        private static readonly DependencyProperty IsUpdatingProperty =
            DependencyProperty.RegisterAttached(
                "IsUpdating",
                typeof(bool),
                typeof(MaskedTextBoxBehavior),
                new PropertyMetadata(false));

        public static readonly DependencyProperty MaskPatternProperty =
            DependencyProperty.RegisterAttached(
                "MaskPattern",
                typeof(string),
                typeof(MaskedTextBoxBehavior),
                new PropertyMetadata(string.Empty, HandleMaskPatternChanged));

        public static void SetMaskPattern(DependencyObject element, string value)
        {
            element.SetValue(MaskPatternProperty, value);
        }

        public static string GetMaskPattern(DependencyObject element)
        {
            return (string)element.GetValue(MaskPatternProperty);
        }

        private static void HandleMaskPatternChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            if (!(dependencyObject is TextBox textBox))
            {
                return;
            }

            textBox.TextChanged -= HandleTextChanged;

            if (string.IsNullOrWhiteSpace(e.NewValue as string))
            {
                return;
            }

            textBox.SetValue(LastValidTextProperty, textBox.Text ?? string.Empty);
            textBox.TextChanged += HandleTextChanged;
        }

        private static void HandleTextChanged(object sender, TextChangedEventArgs e)
        {
            if (!(sender is TextBox textBox) || (bool)textBox.GetValue(IsUpdatingProperty))
            {
                return;
            }

            var pattern = GetMaskPattern(textBox);
            if (string.IsNullOrWhiteSpace(pattern))
            {
                textBox.SetValue(LastValidTextProperty, textBox.Text ?? string.Empty);
                return;
            }

            var currentText = textBox.Text ?? string.Empty;
            if (Regex.IsMatch(currentText, pattern, RegexOptions.CultureInvariant))
            {
                textBox.SetValue(LastValidTextProperty, currentText);
                return;
            }

            textBox.SetValue(IsUpdatingProperty, true);
            try
            {
                var previousText = (string)textBox.GetValue(LastValidTextProperty) ?? string.Empty;
                textBox.Text = previousText;
                textBox.CaretIndex = previousText.Length;
            }
            finally
            {
                textBox.SetValue(IsUpdatingProperty, false);
            }
        }
    }
}
