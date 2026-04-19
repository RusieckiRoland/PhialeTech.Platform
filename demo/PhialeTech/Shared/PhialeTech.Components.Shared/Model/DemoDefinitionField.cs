using System;

namespace PhialeTech.Components.Shared.Model
{
    public sealed class DemoDefinitionField
    {
        public DemoDefinitionField(string label, string value)
        {
            if (string.IsNullOrWhiteSpace(label))
            {
                throw new ArgumentException("Field label is required.", nameof(label));
            }

            Label = label.Trim();
            Value = value ?? string.Empty;
        }

        public string Label { get; }

        public string Value { get; }
    }
}
