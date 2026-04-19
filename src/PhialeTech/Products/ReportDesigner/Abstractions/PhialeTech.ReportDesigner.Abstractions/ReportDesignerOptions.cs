namespace PhialeTech.ReportDesigner.Abstractions
{
    public sealed class ReportDesignerOptions
    {
        public string EntryPageRelativePath { get; set; } = "ReportDesigner/index.html";

        public string ReadyMessageType { get; set; } = "reportDesigner.ready";

        public string VirtualHostName { get; set; } = "phialetech.reportdesigner";

        public string AssetRootRelativePath { get; set; } = "ReportDesigner";

        public string InitialLocale { get; set; } = "en";

        public string InitialTheme { get; set; } = "light";

        public ReportDesignerOptions Clone()
        {
            return new ReportDesignerOptions
            {
                EntryPageRelativePath = EntryPageRelativePath,
                ReadyMessageType = ReadyMessageType,
                VirtualHostName = VirtualHostName,
                AssetRootRelativePath = AssetRootRelativePath,
                InitialLocale = InitialLocale,
                InitialTheme = InitialTheme
            };
        }
    }
}
