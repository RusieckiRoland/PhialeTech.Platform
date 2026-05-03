using System;
using System.Windows;
using System.Windows.Controls;
using PhialeGrid.Core.Regions;

namespace PhialeTech.PhialeGrid.Wpf.Regions
{
    internal sealed class WpfGridWorkspaceBandBinding
    {
        internal WpfGridWorkspaceBandBinding(
            GridRegionKind regionKind,
            RowDefinition row,
            RowDefinition splitterRow,
            Panel topWorkspaceBandHost,
            Panel bottomWorkspaceBandHost,
            FrameworkElement host,
            FrameworkElement contentHost,
            double fallbackExpandedSize,
            bool usesWorkspaceBandStackLayout)
        {
            RegionKind = regionKind;
            Row = row ?? throw new ArgumentNullException(nameof(row));
            SplitterRow = splitterRow ?? throw new ArgumentNullException(nameof(splitterRow));
            TopWorkspaceBandHost = topWorkspaceBandHost ?? throw new ArgumentNullException(nameof(topWorkspaceBandHost));
            BottomWorkspaceBandHost = bottomWorkspaceBandHost ?? throw new ArgumentNullException(nameof(bottomWorkspaceBandHost));
            Host = host ?? throw new ArgumentNullException(nameof(host));
            ContentHost = contentHost ?? throw new ArgumentNullException(nameof(contentHost));
            FallbackExpandedSize = fallbackExpandedSize;
            UsesWorkspaceBandStackLayout = usesWorkspaceBandStackLayout;
        }

        internal GridRegionKind RegionKind { get; }

        internal RowDefinition Row { get; }

        internal RowDefinition SplitterRow { get; }

        internal Panel TopWorkspaceBandHost { get; }

        internal Panel BottomWorkspaceBandHost { get; }

        internal FrameworkElement Host { get; }

        internal FrameworkElement ContentHost { get; }

        internal double FallbackExpandedSize { get; }

        internal bool UsesWorkspaceBandStackLayout { get; }
    }
}
