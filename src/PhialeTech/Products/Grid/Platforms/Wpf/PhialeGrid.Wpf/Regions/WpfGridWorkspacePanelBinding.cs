using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PhialeGrid.Core.Regions;

namespace PhialeTech.PhialeGrid.Wpf.Regions
{
    internal sealed class WpfGridWorkspacePanelBinding
    {
        internal WpfGridWorkspacePanelBinding(
            GridRegionKind regionKind,
            ColumnDefinition surfaceColumn,
            ColumnDefinition splitterColumn,
            ColumnDefinition regionColumn,
            IReadOnlyList<FrameworkElement> surfaceHosts,
            FrameworkElement splitter,
            FrameworkElement host,
            FrameworkElement contentHost,
            FrameworkElement collapsedRail,
            FrameworkElement expandedCard,
            TranslateTransform expandedCardTransform,
            double fallbackExpandedSize)
        {
            RegionKind = regionKind;
            SurfaceColumn = surfaceColumn ?? throw new ArgumentNullException(nameof(surfaceColumn));
            SplitterColumn = splitterColumn ?? throw new ArgumentNullException(nameof(splitterColumn));
            RegionColumn = regionColumn ?? throw new ArgumentNullException(nameof(regionColumn));
            SurfaceHosts = surfaceHosts ?? throw new ArgumentNullException(nameof(surfaceHosts));
            Splitter = splitter ?? throw new ArgumentNullException(nameof(splitter));
            Host = host ?? throw new ArgumentNullException(nameof(host));
            ContentHost = contentHost ?? throw new ArgumentNullException(nameof(contentHost));
            CollapsedRail = collapsedRail ?? throw new ArgumentNullException(nameof(collapsedRail));
            ExpandedCard = expandedCard ?? throw new ArgumentNullException(nameof(expandedCard));
            ExpandedCardTransform = expandedCardTransform ?? throw new ArgumentNullException(nameof(expandedCardTransform));
            FallbackExpandedSize = fallbackExpandedSize;
        }

        internal WpfGridWorkspacePanelBinding(
            GridRegionKind regionKind,
            ColumnDefinition surfaceColumn,
            ColumnDefinition leftSplitterColumn,
            ColumnDefinition leftRegionColumn,
            ColumnDefinition rightSplitterColumn,
            ColumnDefinition rightRegionColumn,
            IReadOnlyList<FrameworkElement> surfaceHosts,
            FrameworkElement splitter,
            FrameworkElement host,
            FrameworkElement contentHost,
            FrameworkElement collapsedRail,
            FrameworkElement expandedCard,
            TranslateTransform expandedCardTransform,
            double fallbackExpandedSize)
            : this(
                regionKind,
                surfaceColumn,
                rightSplitterColumn,
                rightRegionColumn,
                surfaceHosts,
                splitter,
                host,
                contentHost,
                collapsedRail,
                expandedCard,
                expandedCardTransform,
                fallbackExpandedSize)
        {
            LeftSplitterColumn = leftSplitterColumn ?? throw new ArgumentNullException(nameof(leftSplitterColumn));
            LeftRegionColumn = leftRegionColumn ?? throw new ArgumentNullException(nameof(leftRegionColumn));
            RightSplitterColumn = rightSplitterColumn ?? throw new ArgumentNullException(nameof(rightSplitterColumn));
            RightRegionColumn = rightRegionColumn ?? throw new ArgumentNullException(nameof(rightRegionColumn));
        }

        internal GridRegionKind RegionKind { get; }

        internal ColumnDefinition SurfaceColumn { get; }

        internal ColumnDefinition SplitterColumn { get; }

        internal ColumnDefinition RegionColumn { get; }

        internal IReadOnlyList<FrameworkElement> SurfaceHosts { get; }

        internal FrameworkElement Splitter { get; }

        internal FrameworkElement Host { get; }

        internal FrameworkElement ContentHost { get; }

        internal FrameworkElement CollapsedRail { get; }

        internal FrameworkElement ExpandedCard { get; }

        internal TranslateTransform ExpandedCardTransform { get; }

        internal double FallbackExpandedSize { get; }

        internal bool UsesDedicatedSideColumns => LeftRegionColumn != null;

        internal ColumnDefinition LeftSplitterColumn { get; }

        internal ColumnDefinition LeftRegionColumn { get; }

        internal ColumnDefinition RightSplitterColumn { get; }

        internal ColumnDefinition RightRegionColumn { get; }
    }
}
