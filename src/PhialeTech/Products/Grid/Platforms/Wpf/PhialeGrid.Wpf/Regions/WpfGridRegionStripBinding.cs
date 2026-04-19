using System;
using System.Windows;
using System.Windows.Controls;
using PhialeGrid.Core.Regions;

namespace PhialeTech.PhialeGrid.Wpf.Regions
{
    internal sealed class WpfGridRegionStripBinding
    {
        internal WpfGridRegionStripBinding(
            GridRegionKind regionKind,
            RowDefinition row,
            RowDefinition splitterRow,
            FrameworkElement host,
            FrameworkElement contentHost,
            double fallbackExpandedSize)
        {
            RegionKind = regionKind;
            Row = row ?? throw new ArgumentNullException(nameof(row));
            SplitterRow = splitterRow ?? throw new ArgumentNullException(nameof(splitterRow));
            Host = host ?? throw new ArgumentNullException(nameof(host));
            ContentHost = contentHost ?? throw new ArgumentNullException(nameof(contentHost));
            FallbackExpandedSize = fallbackExpandedSize;
        }

        internal GridRegionKind RegionKind { get; }

        internal RowDefinition Row { get; }

        internal RowDefinition SplitterRow { get; }

        internal FrameworkElement Host { get; }

        internal FrameworkElement ContentHost { get; }

        internal double FallbackExpandedSize { get; }
    }
}
