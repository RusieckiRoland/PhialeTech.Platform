using System;
using System.Windows;
using PhialeTech.YamlApp.Abstractions.Interfaces;
using PhialeTech.YamlApp.Runtime.Model;
using PhialeTech.YamlApp.Wpf.Controls.DocumentEditor;
using PhialeTech.YamlApp.Wpf.Controls.IntegerBox;
using PhialeTech.YamlApp.Wpf.Controls.TextBox;

namespace PhialeTech.YamlApp.Wpf.Document
{
    public sealed class YamlFieldControlFactory
    {
        public FrameworkElement Create(RuntimeFieldState runtimeField)
        {
            return Create(runtimeField, "light", "en");
        }

        public FrameworkElement Create(RuntimeFieldState runtimeField, string theme, string languageCode)
        {
            if (runtimeField == null)
            {
                throw new InvalidOperationException("Runtime field state is required.");
            }

            if (runtimeField.Field != null && runtimeField.Field.Definition is IIntegerFieldDefinition)
            {
                return new YamlIntegerBox
                {
                    RuntimeFieldState = runtimeField,
                    MinValue = runtimeField.MinValue,
                    MaxValue = runtimeField.MaxNumericValue,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                };
            }

            if (runtimeField.Field != null && runtimeField.Field.Definition is IStringFieldDefinition)
            {
                return new YamlTextBox
                {
                    RuntimeFieldState = runtimeField,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                };
            }

            if (runtimeField.Field != null && runtimeField.Field.Definition is IDocumentEditorFieldDefinition)
            {
                return new YamlDocumentEditor
                {
                    RuntimeFieldState = runtimeField,
                    Theme = NormalizeTheme(theme),
                    LanguageCode = NormalizeLanguageCode(languageCode),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                };
            }

            throw new NotSupportedException(string.Format("Unsupported field type: {0}", runtimeField.Id ?? runtimeField.Name ?? "unknown"));
        }

        private static string NormalizeTheme(string theme)
        {
            return string.Equals(theme, "dark", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(theme, "night", StringComparison.OrdinalIgnoreCase)
                ? "dark"
                : "light";
        }

        private static string NormalizeLanguageCode(string languageCode)
        {
            return string.Equals(languageCode, "pl", StringComparison.OrdinalIgnoreCase)
                ? "pl"
                : "en";
        }
    }
}
