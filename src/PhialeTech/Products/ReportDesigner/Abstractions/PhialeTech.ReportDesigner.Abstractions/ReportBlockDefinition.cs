using System.Collections.Generic;

namespace PhialeTech.ReportDesigner.Abstractions
{
    public sealed class ReportBlockDefinition
    {
        public string Id { get; set; } = string.Empty;

        public string Type { get; set; } = "Text";

        public string Name { get; set; } = string.Empty;

        public string Text { get; set; } = string.Empty;

        public string Binding { get; set; } = string.Empty;

        public string ImageSource { get; set; } = string.Empty;

        public string ItemsSource { get; set; } = string.Empty;

        public int ColumnCount { get; set; }

        public string ColumnGap { get; set; } = string.Empty;

        public string BarcodeType { get; set; } = ReportBarcodeTypes.Code128;

        public bool ShowText { get; set; } = true;

        public string Size { get; set; } = string.Empty;

        public string ErrorCorrectionLevel { get; set; } = ReportQrCodeErrorCorrectionLevels.Medium;

        public string SpecialFieldKind { get; set; } = string.Empty;

        public bool PageBreakBefore { get; set; }

        public bool KeepTogether { get; set; }

        public bool RepeatHeader { get; set; } = true;

        public ReportValueFormat Format { get; set; } = new ReportValueFormat();

        public ReportBlockStyle Style { get; set; } = new ReportBlockStyle();

        public IList<ReportTableColumnDefinition> Columns { get; set; } = new List<ReportTableColumnDefinition>();

        public IList<ReportFieldListItemDefinition> Fields { get; set; } = new List<ReportFieldListItemDefinition>();

        public IList<ReportBlockDefinition> Children { get; set; } = new List<ReportBlockDefinition>();
    }
}
