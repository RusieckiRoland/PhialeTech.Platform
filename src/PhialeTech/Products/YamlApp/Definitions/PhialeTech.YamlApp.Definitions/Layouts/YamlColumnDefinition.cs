using PhialeTech.YamlApp.Abstractions.Enums;
using System.Collections.Generic;
using PhialeTech.YamlApp.Abstractions.Interfaces;

namespace PhialeTech.YamlApp.Definitions.Layouts
{
    public class YamlColumnDefinition : YamlLayoutItemDefinition, IColumnDefinition
    {
        public double? Width { get; set; }

        public FieldWidthHint? WidthHint { get; set; }

        public double? Weight { get; set; }

        public bool IsOverlayScope { get; set; }

        public bool? Visible { get; set; }

        public bool? Enabled { get; set; }

        public bool? ShowOldValueRestoreButton { get; set; }

        public ValidationTrigger? ValidationTrigger { get; set; }

        public InteractionMode? InteractionMode { get; set; }

        public DensityMode? DensityMode { get; set; }

        public FieldChromeMode? FieldChromeMode { get; set; }

        public CaptionPlacement? CaptionPlacement { get; set; }

        public List<ILayoutItemDefinition> Items { get; set; } = new List<ILayoutItemDefinition>();

        IEnumerable<ILayoutItemDefinition> ILayoutContainerDefinition.Items => Items;
    }
}
