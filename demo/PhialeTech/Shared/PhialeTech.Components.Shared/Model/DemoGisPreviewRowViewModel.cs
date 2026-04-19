namespace PhialeTech.Components.Shared.Model
{
    public sealed class DemoGisPreviewRowViewModel
    {
        public DemoGisPreviewRowViewModel(
            string category,
            string objectName,
            string municipality,
            string status,
            string areaDisplay,
            string inspectionDisplay,
            string statusForegroundHex,
            string inspectionForegroundHex)
        {
            Category = category;
            ObjectName = objectName;
            Municipality = municipality;
            Status = status;
            AreaDisplay = areaDisplay;
            InspectionDisplay = inspectionDisplay;
            StatusForegroundHex = statusForegroundHex;
            InspectionForegroundHex = inspectionForegroundHex;
        }

        public string Category { get; }

        public string ObjectName { get; }

        public string Municipality { get; }

        public string Status { get; }

        public string AreaDisplay { get; }

        public string InspectionDisplay { get; }

        public string StatusForegroundHex { get; }

        public string InspectionForegroundHex { get; }
    }
}
