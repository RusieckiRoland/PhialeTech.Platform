using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PhialeGrid.Core.Regions;

namespace PhialeTech.PhialeGrid.Wpf.Regions
{
    internal sealed class WpfGridRegionPaneBinding
    {
        internal WpfGridRegionPaneBinding(
            GridRegionKind regionKind,
            ColumnDefinition splitterColumn,
            ColumnDefinition regionColumn,
            FrameworkElement host,
            FrameworkElement contentHost,
            FrameworkElement collapsedRail,
            FrameworkElement expandedCard,
            TranslateTransform expandedCardTransform,
            double fallbackExpandedSize)
        {
            RegionKind = regionKind;
            SplitterColumn = splitterColumn ?? throw new ArgumentNullException(nameof(splitterColumn));
            RegionColumn = regionColumn ?? throw new ArgumentNullException(nameof(regionColumn));
            Host = host ?? throw new ArgumentNullException(nameof(host));
            ContentHost = contentHost ?? throw new ArgumentNullException(nameof(contentHost));
            CollapsedRail = collapsedRail ?? throw new ArgumentNullException(nameof(collapsedRail));
            ExpandedCard = expandedCard ?? throw new ArgumentNullException(nameof(expandedCard));
            ExpandedCardTransform = expandedCardTransform ?? throw new ArgumentNullException(nameof(expandedCardTransform));
            FallbackExpandedSize = fallbackExpandedSize;
        }

        internal GridRegionKind RegionKind { get; }

        internal ColumnDefinition SplitterColumn { get; }

        internal ColumnDefinition RegionColumn { get; }

        internal FrameworkElement Host { get; }

        internal FrameworkElement ContentHost { get; }

        internal FrameworkElement CollapsedRail { get; }

        internal FrameworkElement ExpandedCard { get; }

        internal TranslateTransform ExpandedCardTransform { get; }

        internal double FallbackExpandedSize { get; }
    }
}
