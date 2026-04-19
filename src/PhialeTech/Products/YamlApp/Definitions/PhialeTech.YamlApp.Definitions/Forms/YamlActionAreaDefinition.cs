using PhialeTech.YamlApp.Abstractions.Enums;
using PhialeTech.YamlApp.Abstractions.Interfaces;

namespace PhialeTech.YamlApp.Definitions.Documents
{
    public class YamlActionAreaDefinition : IActionAreaDefinition
    {
        public string Id { get; set; }

        public string Extends { get; set; }

        public string Name { get; set; }

        public ActionPlacement? Placement { get; set; }

        public ActionAlignment? HorizontalAlignment { get; set; }

        public ActionAreaChromeMode? ChromeMode { get; set; }

        public bool? Shared { get; set; }

        public bool? Sticky { get; set; }

        public bool? Visible { get; set; }
    }
}
