using System.Windows;
using PhialeTech.YamlApp.Core.Controls.TextBox;

namespace PhialeTech.YamlApp.Wpf.Controls.TextBox
{
    internal sealed class YamlTextBoxDensityVisualMetrics
    {
        public YamlTextBoxLayoutMetrics LayoutMetrics { get; set; }

        public Thickness EditorPadding { get; set; }

        public Thickness PlaceholderMargin { get; set; }

        public Thickness ClearButtonMargin { get; set; }
    }
}
