using System;
using System.Collections.Generic;
using PhialeGrid.Core.Regions;

namespace PhialeTech.PhialeGrid.Wpf.Regions
{
    internal sealed class WpfGridRegionRenderSnapshot
    {
        internal WpfGridRegionRenderSnapshot(
            IReadOnlyDictionary<GridRegionKind, bool> contentAvailability,
            IReadOnlyDictionary<GridRegionKind, WpfGridRegionRenderDirectives> directives,
            bool useWorkspacePanelDrawerChrome)
        {
            ContentAvailability = contentAvailability ?? throw new ArgumentNullException(nameof(contentAvailability));
            Directives = directives ?? throw new ArgumentNullException(nameof(directives));
            UseWorkspacePanelDrawerChrome = useWorkspacePanelDrawerChrome;
        }

        internal IReadOnlyDictionary<GridRegionKind, bool> ContentAvailability { get; }

        internal IReadOnlyDictionary<GridRegionKind, WpfGridRegionRenderDirectives> Directives { get; }

        internal bool UseWorkspacePanelDrawerChrome { get; }
    }
}
