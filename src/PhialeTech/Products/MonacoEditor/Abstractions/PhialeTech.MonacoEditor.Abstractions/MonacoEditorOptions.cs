namespace PhialeTech.MonacoEditor.Abstractions
{
    public sealed class MonacoEditorOptions
    {
        public string EntryPageRelativePath { get; set; } = "MonacoEditor/index.html";

        public string ReadyMessageType { get; set; } = "monaco.ready";

        public string VirtualHostName { get; set; } = "phialetech.monacoeditor";

        public string AssetRootRelativePath { get; set; } = "MonacoEditor";

        public string MonacoAssetRootRelativePath { get; set; } = "Monaco";

        public string InitialValue { get; set; } = string.Empty;

        public string InitialLanguage { get; set; } = "yaml";

        public string InitialTheme { get; set; } = "light";

        public MonacoEditorOptions Clone()
        {
            return new MonacoEditorOptions
            {
                EntryPageRelativePath = EntryPageRelativePath,
                ReadyMessageType = ReadyMessageType,
                VirtualHostName = VirtualHostName,
                AssetRootRelativePath = AssetRootRelativePath,
                MonacoAssetRootRelativePath = MonacoAssetRootRelativePath,
                InitialValue = InitialValue,
                InitialLanguage = InitialLanguage,
                InitialTheme = InitialTheme
            };
        }
    }
}
