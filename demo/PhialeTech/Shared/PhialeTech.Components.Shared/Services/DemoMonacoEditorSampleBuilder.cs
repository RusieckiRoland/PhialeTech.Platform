namespace PhialeTech.Components.Shared.Services
{
    public static class DemoMonacoEditorSampleBuilder
    {
        public static string CreateYamlSample()
        {
            return string.Join("\n", new[]
            {
                "id: monaco-demo-form",
                "name: Monaco demo",
                "interactionMode: Classic",
                "densityMode: Normal",
                "fieldChromeMode: Framed",
                "fields:",
                "  firstName:",
                "    caption: First name",
                "    placeholder: Type first name",
                "    required: true",
                "  notes:",
                "    caption: Notes",
                "    placeholder: Paste extra details",
                "layout:",
                "  type: Column",
                "  items:",
                "    - fieldRef: firstName",
                "    - fieldRef: notes",
                "actions:",
                "  - id: ok",
                "    semantic: Ok",
                "    caption: OK",
            });
        }

        public static string CreateCSharpSample()
        {
            return string.Join("\n", new[]
            {
                "using System;",
                "",
                "namespace Demo.Monaco",
                "{",
                "    public static class Bootstrap",
                "    {",
                "        public static string BuildGreeting(string name)",
                "        {",
                "            name = string.IsNullOrWhiteSpace(name) ? \"world\" : name.Trim();",
                "            return $\"Hello, {name}!\";",
                "        }",
                "    }",
                "}",
            });
        }
    }
}
