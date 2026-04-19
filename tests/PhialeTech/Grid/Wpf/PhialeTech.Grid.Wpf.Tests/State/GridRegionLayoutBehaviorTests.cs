using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using PhialeGrid.Core.Columns;
using PhialeGrid.Core.Hierarchy;
using PhialeGrid.Core.Query;
using PhialeGrid.Core.Regions;
using PhialeGrid.Core.State;
using PhialeTech.PhialeGrid.Wpf.Controls;
using PhialeTech.PhialeGrid.Wpf.Surface.Presenters;

namespace PhialeGrid.Wpf.Tests.State
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public sealed class GridRegionLayoutBehaviorTests
    {
        [Test]
        public void ApplyViewState_WhenSideToolRegionIsVisibleAndCollapsed_UpdatesHostAndContentVisibility()
        {
            var grid = CreateGrid();
            grid.SideToolContent = new Border { Width = 120, Height = 80 };

            var window = CreateWindow(grid);
            try
            {
                window.Show();
                FlushDispatcher(grid.Dispatcher);

                var state = CreateRegionViewState(GridRegionKind.SideToolRegion, GridRegionState.Collapsed, 280d);

                grid.ApplyViewState(state);
                FlushDispatcher(grid.Dispatcher);

                var host = (FrameworkElement)grid.FindName("SideToolRegionHost");
                var content = (FrameworkElement)grid.FindName("SideToolRegionContentScrollViewer");

                Assert.Multiple(() =>
                {
                    Assert.That(host, Is.Not.Null);
                    Assert.That(content, Is.Not.Null);
                    Assert.That(host.Visibility, Is.EqualTo(Visibility.Visible));
                    Assert.That(content.Visibility, Is.EqualTo(Visibility.Collapsed));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void ApplyViewState_WhenTopCommandContentIsMissing_KeepsTopRegionHidden()
        {
            var grid = CreateGrid();
            var window = CreateWindow(grid);

            try
            {
                window.Show();
                FlushDispatcher(grid.Dispatcher);

                var state = CreateRegionViewState(GridRegionKind.TopCommandRegion, GridRegionState.Open, 44d);

                grid.ApplyViewState(state);
                FlushDispatcher(grid.Dispatcher);

                var host = (FrameworkElement)grid.FindName("TopCommandStripHost");
                Assert.That(host.Visibility, Is.EqualTo(Visibility.Collapsed));
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void Constructor_DoesNotThrow_WhenInitialXamlBindingsReadRegionChromeBeforeAdapterIsCreated()
        {
            Assert.That(
                () =>
                {
                    var grid = new PhialeTech.PhialeGrid.Wpf.Controls.PhialeGrid();
                    Assert.That(grid, Is.Not.Null);
                },
                Throws.Nothing);
        }

        [Test]
        public void FirstOpen_UsesIntegratedTopChromeGeometry_WithoutSecondInteraction()
        {
            var grid = CreateGrid(Enumerable.Range(1, 60).Select(index => new TestRow
            {
                Category = "Fabryczna",
                ObjectName = "Object " + index,
            }).ToArray());

            var window = CreateWindow(grid);

            try
            {
                window.Show();
                FlushDispatcher(grid.Dispatcher);

                var headerBand = (FrameworkElement)grid.FindName("SurfaceHeaderBandHost");
                var filterRow = (FrameworkElement)grid.FindName("SurfaceFilterRowHost");
                var surfaceHost = (PhialeTech.PhialeGrid.Wpf.Surface.GridSurfaceHost)grid.FindName("SurfaceHost");
                var viewportBefore = surfaceHost.CurrentSnapshot.ViewportState;

                Assert.Multiple(() =>
                {
                    Assert.That(headerBand, Is.Not.Null);
                    Assert.That(filterRow, Is.Not.Null);
                    Assert.That(headerBand.ActualHeight, Is.EqualTo(grid.SurfaceColumnHeaderHeight).Within(1d));
                    Assert.That(filterRow.ActualHeight, Is.EqualTo(grid.SurfaceFilterRowHeight).Within(1d));
                    Assert.That(grid.SurfaceTopChromeHeight, Is.EqualTo(
                        grid.SurfaceColumnHeaderHeight + grid.SurfaceFilterRowHeight).Within(0.1d));
                    Assert.That(viewportBefore.ViewportWidth, Is.GreaterThan(150d));
                    Assert.That(viewportBefore.ViewportHeight, Is.GreaterThan(150d));
                });

                surfaceHost.HandleWheelForTesting(new UniversalInput.Contracts.UniversalPointerWheelChangedEventArgs(
                    120,
                    new UniversalInput.Contracts.UniversalPoint { X = 140, Y = 120 }));
                FlushDispatcher(grid.Dispatcher);

                var viewportAfter = surfaceHost.CurrentSnapshot.ViewportState;
                Assert.Multiple(() =>
                {
                    Assert.That(viewportAfter.ViewportWidth, Is.EqualTo(viewportBefore.ViewportWidth).Within(0.1d));
                    Assert.That(viewportAfter.ViewportHeight, Is.EqualTo(viewportBefore.ViewportHeight).Within(0.1d));
                    Assert.That(filterRow.ActualHeight, Is.EqualTo(grid.SurfaceFilterRowHeight).Within(1d));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void FirstOpen_WhenVerticalScrollbarIsVisible_UsesIntegratedHeaderAndFilterInsets()
        {
            var grid = CreateGrid(Enumerable.Range(1, 120).Select(index => new TestRow
            {
                Category = "Fabryczna",
                ObjectName = "Object " + index,
            }).ToArray());
            var window = CreateWindow(grid);

            try
            {
                window.Show();
                FlushDispatcher(grid.Dispatcher);

                var headerInset = (FrameworkElement)grid.FindName("SurfaceHeaderRightInset");
                var filterInset = (FrameworkElement)grid.FindName("SurfaceFilterRightInset");
                var viewportHost = (FrameworkElement)grid.FindName("SurfaceTopViewportHost");

                Assert.Multiple(() =>
                {
                    Assert.That(headerInset, Is.Not.Null);
                    Assert.That(filterInset, Is.Not.Null);
                    Assert.That(viewportHost, Is.Not.Null);
                    Assert.That(headerInset.ActualWidth, Is.GreaterThanOrEqualTo(0d));
                    Assert.That(filterInset.ActualWidth, Is.GreaterThanOrEqualTo(0d));
                    Assert.That(headerInset.ActualWidth, Is.EqualTo(grid.SurfaceVerticalScrollBarGutterWidth).Within(2d));
                    Assert.That(filterInset.ActualWidth, Is.EqualTo(grid.SurfaceVerticalScrollBarGutterWidth).Within(2d));
                    Assert.That(grid.SurfaceVerticalScrollBarGutterWidth, Is.GreaterThan(0d));
                    Assert.That(viewportHost.ActualHeight, Is.GreaterThan(200d));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void FirstOpen_WhenVerticalScrollbarIsVisible_MatchesLiveScrollbarWidthAndUsesFlatTopChrome()
        {
            var grid = CreateGrid(Enumerable.Range(1, 120).Select(index => new TestRow
            {
                Category = "Fabryczna",
                ObjectName = "Object " + index,
            }).ToArray());
            var window = CreateWindow(grid);

            try
            {
                window.Show();
                FlushDispatcher(grid.Dispatcher);

                var surfaceHost = (PhialeTech.PhialeGrid.Wpf.Surface.GridSurfaceHost)grid.FindName("SurfaceHost");
                var headerInset = (FrameworkElement)grid.FindName("SurfaceHeaderRightInset");
                var filterInset = (FrameworkElement)grid.FindName("SurfaceFilterRightInset");
                var verticalScrollBar = FindVisualChildren<ScrollBar>(surfaceHost)
                    .FirstOrDefault(candidate => candidate.Orientation == Orientation.Vertical &&
                                                 candidate.Visibility == Visibility.Visible &&
                                                 candidate.ActualWidth > 0d);
                var scrollBarChrome = verticalScrollBar?.Template.FindName("ScrollBarChrome", verticalScrollBar) as Border;

                Assert.Multiple(() =>
                {
                    Assert.That(surfaceHost, Is.Not.Null);
                    Assert.That(headerInset, Is.Not.Null);
                    Assert.That(filterInset, Is.Not.Null);
                    Assert.That(verticalScrollBar, Is.Not.Null, "Expected a live vertical scrollbar for the surface viewport.");
                    Assert.That(scrollBarChrome, Is.Not.Null, "Expected the custom scrollbar chrome to be present.");
                    Assert.That(headerInset.ActualWidth, Is.EqualTo(verticalScrollBar.ActualWidth).Within(1d));
                    Assert.That(filterInset.ActualWidth, Is.EqualTo(verticalScrollBar.ActualWidth).Within(1d));
                    Assert.That(grid.SurfaceVerticalScrollBarGutterWidth, Is.EqualTo(verticalScrollBar.ActualWidth).Within(1d));
                    Assert.That(scrollBarChrome.CornerRadius.TopLeft, Is.EqualTo(0d));
                    Assert.That(scrollBarChrome.CornerRadius.TopRight, Is.EqualTo(0d));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void FirstOpen_WhenGroupedRowsArePresent_MatchesPostInteractionViewportParity()
        {
            var grid = CreateGrid(Enumerable.Range(1, 120).Select(index => new TestRow
            {
                Category = index % 3 == 0 ? "A" : "B",
                ObjectName = "Object " + index,
            }).ToArray());
            grid.Groups = new[] { new GridGroupDescriptor("Category") };

            var window = CreateWindow(grid);

            try
            {
                window.Show();
                FlushDispatcher(grid.Dispatcher);

                var surfaceHost = (PhialeTech.PhialeGrid.Wpf.Surface.GridSurfaceHost)grid.FindName("SurfaceHost");
                var stateA = CaptureLayoutParityState(grid, surfaceHost);

                grid.CollapseAllGroups();
                FlushDispatcher(grid.Dispatcher);

                var stateB = CaptureLayoutParityState(grid, surfaceHost);

                Assert.Multiple(() =>
                {
                    Assert.That(stateA.SurfaceViewportWidth, Is.EqualTo(stateB.SurfaceViewportWidth).Within(0.1d));
                    Assert.That(stateA.SurfaceViewportHeight, Is.EqualTo(stateB.SurfaceViewportHeight).Within(0.1d));
                    Assert.That(stateA.GridViewportWidth, Is.EqualTo(stateB.GridViewportWidth).Within(0.1d));
                    Assert.That(stateA.GridViewportHeight, Is.EqualTo(stateB.GridViewportHeight).Within(0.1d));
                    Assert.That(stateA.TopChromeHeight, Is.EqualTo(stateB.TopChromeHeight).Within(0.1d));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void FirstOpen_WhenFiltersAreCollapsed_UsesZeroHeightFilterRowWithoutBlankGap()
        {
            var grid = CreateGrid(Enumerable.Range(1, 24).Select(index => new TestRow
            {
                Category = "Fabryczna",
                ObjectName = "Object " + index,
            }).ToArray());
            var root = new GridHierarchyNode<object>(
                "root",
                grid.ItemsSource.Cast<object>().First(),
                canExpand: false);
            var controller = new GridHierarchyController<object>(new EmptyHierarchyProvider());
            grid.SetHierarchySource(new[] { root }, controller);

            var window = CreateWindow(grid);
            try
            {
                window.Show();
                FlushDispatcher(grid.Dispatcher);

                var filterRow = (FrameworkElement)grid.FindName("SurfaceFilterRowHost");
                var filterRowDefinition = (RowDefinition)grid.FindName("SurfaceFilterRow");
                var viewportHost = (FrameworkElement)grid.FindName("SurfaceTopViewportHost");

                Assert.Multiple(() =>
                {
                    Assert.That(grid.SurfaceFilterRowHeight, Is.EqualTo(0d).Within(0.1d));
                    Assert.That(filterRow.ActualHeight, Is.EqualTo(0d).Within(0.5d));
                    Assert.That(filterRowDefinition.ActualHeight, Is.EqualTo(0d).Within(0.5d));
                    Assert.That(viewportHost.Margin.Top, Is.EqualTo(0d).Within(0.1d));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void FirstOpen_WhenFiltersAreVisible_UsesRealFilterRowBelowIntegratedHeaderBand()
        {
            var grid = CreateGrid();
            var window = CreateWindow(grid);

            try
            {
                window.Show();
                FlushDispatcher(grid.Dispatcher);

                var headerBand = (FrameworkElement)grid.FindName("SurfaceHeaderBandHost");
                var filterRow = (FrameworkElement)grid.FindName("SurfaceFilterRowHost");
                var viewportHost = (FrameworkElement)grid.FindName("SurfaceTopViewportHost");
                var headerTop = headerBand.TranslatePoint(new Point(0d, 0d), viewportHost).Y;
                var filterTop = filterRow.TranslatePoint(new Point(0d, 0d), viewportHost).Y;

                Assert.Multiple(() =>
                {
                    Assert.That(headerBand.ActualHeight, Is.EqualTo(grid.SurfaceColumnHeaderHeight).Within(1d));
                    Assert.That(filterRow.ActualHeight, Is.EqualTo(grid.SurfaceFilterRowHeight).Within(1d));
                    Assert.That(grid.SurfaceFilterRowHeight, Is.GreaterThan(0d));
                    Assert.That(headerTop, Is.EqualTo(0d).Within(1d));
                    Assert.That(filterTop, Is.EqualTo(grid.SurfaceColumnHeaderHeight).Within(1d));
                    Assert.That(filterTop + filterRow.ActualHeight, Is.EqualTo(grid.SurfaceTopChromeHeight).Within(2d));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void FirstOpen_WhenFiltersAreVisible_HasNoGapBetweenFilterRowAndDataViewport()
        {
            var grid = CreateGrid(Enumerable.Range(1, 20).Select(index => new TestRow
            {
                Category = "Fabryczna",
                ObjectName = "Object " + index,
            }).ToArray());
            grid.Groups = new[] { new GridGroupDescriptor("Category") };

            var window = CreateWindow(grid);
            try
            {
                window.Show();
                FlushDispatcher(grid.Dispatcher);

                var viewportHost = (FrameworkElement)grid.FindName("SurfaceTopViewportHost");
                var filterRow = (FrameworkElement)grid.FindName("SurfaceFilterRowHost");
                var surfaceHost = (FrameworkElement)grid.FindName("SurfaceHost");
                var snapshot = ((PhialeTech.PhialeGrid.Wpf.Surface.GridSurfaceHost)surfaceHost).CurrentSnapshot;
                var firstRow = snapshot.Rows.FirstOrDefault();

                var filterBottom = filterRow.TranslatePoint(new Point(0d, filterRow.ActualHeight), viewportHost).Y;
                var surfaceTop = surfaceHost.TranslatePoint(new Point(0d, 0d), viewportHost).Y;

                Assert.Multiple(() =>
                {
                    Assert.That(firstRow, Is.Not.Null);
                    Assert.That(surfaceTop, Is.EqualTo(filterBottom).Within(1d));
                    Assert.That(firstRow.Bounds.Y, Is.EqualTo(0d).Within(0.1d));
                    Assert.That(snapshot.ViewportState.DataTopInset, Is.EqualTo(0d).Within(0.1d));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void FirstOpen_WhenGroupedRowsAreVisible_RendersFirstRealizedPresentersAtTopOfSurfaceViewport()
        {
            var grid = CreateGrid(Enumerable.Range(1, 20).Select(index => new TestRow
            {
                Category = "Fabryczna",
                ObjectName = "Object " + index,
            }).ToArray());
            grid.Groups = new[] { new GridGroupDescriptor("Category") };

            var window = CreateWindow(grid);
            try
            {
                window.Show();
                FlushDispatcher(grid.Dispatcher);

                var surfaceHost = (PhialeTech.PhialeGrid.Wpf.Surface.GridSurfaceHost)grid.FindName("SurfaceHost");
                var viewportHost = (FrameworkElement)grid.FindName("SurfaceTopViewportHost");
                var filterRow = (FrameworkElement)grid.FindName("SurfaceFilterRowHost");
                var snapshot = surfaceHost.CurrentSnapshot;
                var firstRow = snapshot.Rows.FirstOrDefault();
                var rowHeaders = FindVisualChildren<GridRowHeaderPresenter>(surfaceHost).ToArray();
                var cells = FindVisualChildren<GridCellPresenter>(surfaceHost).ToArray();
                var minHeaderTop = rowHeaders.Length == 0
                    ? double.NaN
                    : rowHeaders.Min(candidate => candidate.TranslatePoint(new Point(0d, 0d), viewportHost).Y);
                var minCellTop = cells.Length == 0
                    ? double.NaN
                    : cells.Min(candidate => candidate.TranslatePoint(new Point(0d, 0d), viewportHost).Y);
                var filterBottom = filterRow.TranslatePoint(new Point(0d, filterRow.ActualHeight), viewportHost).Y;
                var surfaceTop = surfaceHost.TranslatePoint(new Point(0d, 0d), viewportHost).Y;

                Assert.Multiple(() =>
                {
                    Assert.That(firstRow, Is.Not.Null);
                    Assert.That(rowHeaders, Is.Not.Empty);
                    Assert.That(cells, Is.Not.Empty);
                    Assert.That(surfaceTop, Is.EqualTo(filterBottom).Within(1d));
                    Assert.That(firstRow.Bounds.Y, Is.EqualTo(0d).Within(0.1d));
                    Assert.That(minHeaderTop, Is.EqualTo(surfaceTop).Within(1d));
                    Assert.That(minCellTop, Is.EqualTo(surfaceTop).Within(1d));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void FirstOpen_RendersVisibleColumnHeadersInsideIntegratedHeaderBand()
        {
            var grid = CreateGrid();
            var window = CreateWindow(grid);

            try
            {
                window.Show();
                FlushDispatcher(grid.Dispatcher);

                var headerBand = (FrameworkElement)grid.FindName("SurfaceColumnHeaderBand");
                var firstHeader = FindVisualChild<GridColumnHeaderPresenter>(headerBand);
                var cornerButton = (FrameworkElement)grid.FindName("SurfaceHeaderCornerButton");

                Assert.Multiple(() =>
                {
                    Assert.That(headerBand, Is.Not.Null);
                    Assert.That(firstHeader, Is.Not.Null);
                    Assert.That(firstHeader.TranslatePoint(new Point(0d, 0d), grid).Y, Is.EqualTo(cornerButton.TranslatePoint(new Point(0d, 0d), grid).Y).Within(1d));
                    Assert.That(firstHeader.Height, Is.EqualTo(grid.SurfaceColumnHeaderHeight).Within(1d));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void ApplyViewState_WhenSideToolRegionIsCollapsed_UsesRailOnlyAndReclaimsWidth()
        {
            var grid = CreateGrid();
            grid.SideToolContent = new Border { Width = 120, Height = 80 };

            var window = CreateWindow(grid);
            try
            {
                window.Show();
                FlushDispatcher(grid.Dispatcher);

                grid.ApplyViewState(CreateRegionViewState(GridRegionKind.SideToolRegion, GridRegionState.Open, 300d));
                FlushDispatcher(grid.Dispatcher);

                var surfaceOpenWidth = ((FrameworkElement)grid.FindName("SurfaceHost")).ActualWidth;

                grid.ApplyViewState(CreateRegionViewState(GridRegionKind.SideToolRegion, GridRegionState.Collapsed, 300d));
                FlushDispatcher(grid.Dispatcher);

                var regionColumn = (ColumnDefinition)grid.FindName("SideToolRegionColumn");
                var splitterColumn = (ColumnDefinition)grid.FindName("SideToolRegionSplitterColumn");
                var collapsedRail = (FrameworkElement)grid.FindName("SideToolRegionCollapsedRail");
                var expandedShell = (FrameworkElement)grid.FindName("SideToolRegionExpandedShell");
                var surfaceCollapsedWidth = ((FrameworkElement)grid.FindName("SurfaceHost")).ActualWidth;

                Assert.Multiple(() =>
                {
                    Assert.That(collapsedRail.Visibility, Is.EqualTo(Visibility.Visible));
                    Assert.That(expandedShell.Visibility, Is.EqualTo(Visibility.Collapsed));
                    Assert.That(regionColumn.ActualWidth, Is.GreaterThanOrEqualTo(40d));
                    Assert.That(regionColumn.ActualWidth, Is.LessThanOrEqualTo(48d));
                    Assert.That(splitterColumn.ActualWidth, Is.EqualTo(0d).Within(0.1d));
                    Assert.That(surfaceCollapsedWidth, Is.GreaterThan(surfaceOpenWidth + 150d));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void ApplyViewState_WhenSideToolRegionIsClosed_RemovesPaneFromLayoutAndRestoresSurfaceWidth()
        {
            var grid = CreateGrid();
            grid.SideToolContent = new Border { Width = 120, Height = 80 };

            var window = CreateWindow(grid);
            try
            {
                window.Show();
                FlushDispatcher(grid.Dispatcher);

                grid.ApplyViewState(CreateRegionViewState(GridRegionKind.SideToolRegion, GridRegionState.Closed, 300d));
                FlushDispatcher(grid.Dispatcher);

                var regionColumn = (ColumnDefinition)grid.FindName("SideToolRegionColumn");
                var splitterColumn = (ColumnDefinition)grid.FindName("SideToolRegionSplitterColumn");
                var host = (FrameworkElement)grid.FindName("SideToolRegionHost");
                var surfaceWidth = ((FrameworkElement)grid.FindName("SurfaceHost")).ActualWidth;

                Assert.Multiple(() =>
                {
                    Assert.That(host.Visibility, Is.EqualTo(Visibility.Collapsed));
                    Assert.That(regionColumn.ActualWidth, Is.EqualTo(0d).Within(0.1d));
                    Assert.That(splitterColumn.ActualWidth, Is.EqualTo(0d).Within(0.1d));
                    Assert.That(surfaceWidth, Is.GreaterThan(850d));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void ApplyViewState_WhenTopCommandStripHasContent_UsesCompactSingleLineStripWithoutLegacyTopBarChrome()
        {
            var grid = CreateGrid();
            grid.TopCommandContent = new Border
            {
                Height = 28,
                Child = new TextBlock { Text = "Commands" }
            };

            var window = CreateWindow(grid);
            try
            {
                window.Show();
                FlushDispatcher(grid.Dispatcher);

                var state = CreateRegionViewState(GridRegionKind.TopCommandRegion, GridRegionState.Open, 44d);

                grid.ApplyViewState(state);
                FlushDispatcher(grid.Dispatcher);

                var row = (RowDefinition)grid.FindName("TopCommandStripRow");
                var host = (FrameworkElement)grid.FindName("TopCommandStripHost");
                var toggle = (FrameworkElement)grid.FindName("TopCommandStripToggleButton");
                var close = (FrameworkElement)grid.FindName("TopCommandStripCloseButton");

                Assert.Multiple(() =>
                {
                    Assert.That(row, Is.Not.Null);
                    Assert.That(host, Is.Not.Null);
                    Assert.That(row.MinHeight, Is.GreaterThanOrEqualTo(36d));
                    Assert.That(row.ActualHeight, Is.GreaterThanOrEqualTo(36d));
                    Assert.That(row.ActualHeight, Is.LessThanOrEqualTo(44d));
                    Assert.That(toggle.Visibility, Is.EqualTo(Visibility.Visible));
                    Assert.That(close.Visibility, Is.EqualTo(Visibility.Visible));
                    Assert.That(grid.FindName("TopCommandRegionExpanderButton"), Is.Null);
                    Assert.That(grid.FindName("TopCommandRegionCloseButton"), Is.Null);
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void ApplyViewState_WhenTopCommandStripIsCollapsed_KeepsCompactShellVisibleAndHidesContent()
        {
            var grid = CreateGrid();
            grid.TopCommandContent = new Border
            {
                Height = 28,
                Child = new TextBlock { Text = "Commands" }
            };

            var window = CreateWindow(grid);
            try
            {
                window.Show();
                FlushDispatcher(grid.Dispatcher);

                grid.ApplyViewState(CreateRegionViewState(GridRegionKind.TopCommandRegion, GridRegionState.Collapsed, 44d));
                FlushDispatcher(grid.Dispatcher);

                var host = (FrameworkElement)grid.FindName("TopCommandStripHost");
                var content = (FrameworkElement)grid.FindName("TopCommandStripContentHost");
                var row = (RowDefinition)grid.FindName("TopCommandStripRow");

                Assert.Multiple(() =>
                {
                    Assert.That(host.Visibility, Is.EqualTo(Visibility.Visible));
                    Assert.That(content.Visibility, Is.EqualTo(Visibility.Collapsed));
                    Assert.That(row.ActualHeight, Is.GreaterThanOrEqualTo(36d));
                    Assert.That(row.ActualHeight, Is.LessThanOrEqualTo(44d));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void ApplyViewState_WhenGroupingHasNoGroupsAndLargeSavedSize_KeepsCompactHeaderWithoutExpander()
        {
            var grid = CreateGrid();
            var window = CreateWindow(grid);

            try
            {
                window.Show();
                FlushDispatcher(grid.Dispatcher);

                var state = CreateRegionViewState(GridRegionKind.GroupingRegion, GridRegionState.Open, 180d);

                grid.ApplyViewState(state);
                FlushDispatcher(grid.Dispatcher);

                var row = (RowDefinition)grid.FindName("GroupingRegionRow");
                var expander = (FrameworkElement)grid.FindName("GroupingRegionExpanderButton");
                var close = (FrameworkElement)grid.FindName("GroupingRegionCloseButton");

                Assert.Multiple(() =>
                {
                    Assert.That(row, Is.Not.Null);
                    Assert.That(row.MinHeight, Is.GreaterThanOrEqualTo(56d));
                    Assert.That(row.ActualHeight, Is.GreaterThanOrEqualTo(56d));
                    Assert.That(row.ActualHeight, Is.LessThanOrEqualTo(56d));
                    Assert.That(expander.Visibility, Is.EqualTo(Visibility.Collapsed));
                    Assert.That(close.Visibility, Is.EqualTo(Visibility.Visible));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void ApplyViewState_WhenGroupingAndSummaryRegionsAreOpen_UsesLightHostsInsteadOfHeavyCards()
        {
            var grid = CreateGrid();
            var window = CreateWindow(grid);

            try
            {
                window.Show();
                FlushDispatcher(grid.Dispatcher);

                grid.ApplyViewState(CreateRegionViewState(GridRegionKind.GroupingRegion, GridRegionState.Open, 56d));
                FlushDispatcher(grid.Dispatcher);

                var groupingShell = (Border)grid.FindName("GroupingRegionShell");
                var summaryShell = (Border)grid.FindName("SummaryBottomRegionShell");
                var statusHost = (FrameworkElement)grid.FindName("BottomStatusStripHost");

                Assert.Multiple(() =>
                {
                    Assert.That(groupingShell.BorderThickness.Bottom, Is.EqualTo(1d));
                    Assert.That(groupingShell.BorderThickness.Left, Is.EqualTo(0d));
                    Assert.That(groupingShell.CornerRadius.TopLeft, Is.EqualTo(0d));
                    Assert.That(summaryShell.BorderThickness.Top, Is.EqualTo(1d));
                    Assert.That(summaryShell.BorderThickness.Left, Is.EqualTo(0d));
                    Assert.That(statusHost, Is.Not.Null);
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void CloseButtonFlow_UpdatesExportedViewStateThroughCoreRegionState()
        {
            var grid = CreateGrid();
            grid.SideToolContent = new Border { Width = 120, Height = 80 };
            var window = CreateWindow(grid);

            try
            {
                window.Show();
                FlushDispatcher(grid.Dispatcher);

                grid.ApplyViewState(CreateRegionViewState(GridRegionKind.SideToolRegion, GridRegionState.Open, 300d));
                FlushDispatcher(grid.Dispatcher);

                var closeButton = (Button)grid.FindName("SideToolRegionCloseButton");
                closeButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                FlushDispatcher(grid.Dispatcher);

                var exported = grid.ExportViewState();
                var sideToolState = exported.RegionLayout.Single(region => region.RegionKind == GridRegionKind.SideToolRegion);

                Assert.Multiple(() =>
                {
                    Assert.That(sideToolState.State, Is.EqualTo(GridRegionState.Closed));
                    Assert.That(sideToolState.Size, Is.EqualTo(300d));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void TopCommandStripToggleButton_FlowsThroughCoreRegionState_AndCollapsesTheStrip()
        {
            var grid = CreateGrid();
            grid.TopCommandContent = new Border
            {
                Height = 28,
                Child = new TextBlock { Text = "Commands" }
            };

            var window = CreateWindow(grid);
            try
            {
                window.Show();
                FlushDispatcher(grid.Dispatcher);

                grid.ApplyViewState(CreateRegionViewState(GridRegionKind.TopCommandRegion, GridRegionState.Open, 44d));
                FlushDispatcher(grid.Dispatcher);

                var toggleButton = (Button)grid.FindName("TopCommandStripToggleButton");
                toggleButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                FlushDispatcher(grid.Dispatcher);

                var exported = grid.ExportViewState();
                var topState = exported.RegionLayout.Single(region => region.RegionKind == GridRegionKind.TopCommandRegion);

                Assert.That(topState.State, Is.EqualTo(GridRegionState.Collapsed));
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void SplitterResizeFlow_PersistsSizeBackIntoCoreRegionState()
        {
            var grid = CreateGrid();
            grid.SideToolContent = new Border { Width = 120, Height = 80 };
            var window = CreateWindow(grid);

            try
            {
                window.Show();
                FlushDispatcher(grid.Dispatcher);

                grid.ApplyViewState(CreateRegionViewState(GridRegionKind.SideToolRegion, GridRegionState.Open, 300d));
                FlushDispatcher(grid.Dispatcher);

                var regionColumn = (ColumnDefinition)grid.FindName("SideToolRegionColumn");
                regionColumn.Width = new GridLength(410d);

                InvokePrivate(grid, "PersistColumnRegionSize", GridRegionKind.SideToolRegion, regionColumn);
                FlushDispatcher(grid.Dispatcher);

                var exported = grid.ExportViewState();
                var sideToolState = exported.RegionLayout.Single(region => region.RegionKind == GridRegionKind.SideToolRegion);

                Assert.That(sideToolState.Size, Is.EqualTo(410d).Within(0.1d));
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void InvalidRegionCommandId_FailsFast()
        {
            var grid = CreateGrid();
            var window = CreateWindow(grid);

            try
            {
                window.Show();
                FlushDispatcher(grid.Dispatcher);

                Assert.That(
                    () => InvokePrivate(grid, "HandleRegionCommand", "grid.region.toggle.invalid"),
                    Throws.TypeOf<ArgumentOutOfRangeException>());
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void RegionChromeButton_WithInvalidRegionTag_FailsFast()
        {
            var grid = CreateGrid();
            var window = CreateWindow(grid);

            try
            {
                window.Show();
                FlushDispatcher(grid.Dispatcher);

                var invalidButton = new Button { Tag = "NotARegionKind" };
                Assert.That(
                    () => InvokePrivate(grid, "HandleRegionToggleButtonClick", invalidButton, new RoutedEventArgs(Button.ClickEvent)),
                    Throws.TypeOf<InvalidOperationException>().With.Message.Contains("valid GridRegionKind"));
            }
            finally
            {
                window.Close();
            }
        }

        private static PhialeTech.PhialeGrid.Wpf.Controls.PhialeGrid CreateGrid(object[] itemsSource = null)
        {
            return new PhialeTech.PhialeGrid.Wpf.Controls.PhialeGrid
            {
                Width = 900,
                Height = 600,
                Columns = new[]
                {
                    new GridColumnDefinition("Category", "Category", width: 140d, displayIndex: 0, valueType: typeof(string)),
                    new GridColumnDefinition("ObjectName", "Object name", width: 220d, displayIndex: 1, valueType: typeof(string)),
                },
                ItemsSource = itemsSource ?? Array.Empty<object>(),
            };
        }

        private static Window CreateWindow(FrameworkElement content)
        {
            return new Window
            {
                Width = 960,
                Height = 720,
                Content = content,
                ShowInTaskbar = false,
                WindowStyle = WindowStyle.None,
            };
        }

        private static void FlushDispatcher(Dispatcher dispatcher)
        {
            dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        }

        private static T FindVisualChild<T>(DependencyObject root) where T : DependencyObject
        {
            if (root == null)
            {
                return null;
            }

            var childrenCount = VisualTreeHelper.GetChildrenCount(root);
            for (var index = 0; index < childrenCount; index++)
            {
                var child = VisualTreeHelper.GetChild(root, index);
                if (child is T match)
                {
                    return match;
                }

                var nested = FindVisualChild<T>(child);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }

        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject root) where T : DependencyObject
        {
            if (root == null)
            {
                yield break;
            }

            var childrenCount = VisualTreeHelper.GetChildrenCount(root);
            for (var index = 0; index < childrenCount; index++)
            {
                var child = VisualTreeHelper.GetChild(root, index);
                if (child is T match)
                {
                    yield return match;
                }

                foreach (var nested in FindVisualChildren<T>(child))
                {
                    yield return nested;
                }
            }
        }

    private static object InvokePrivate(object target, string methodName, params object[] args)
    {
        var argumentTypes = args.Select(static arg => arg?.GetType()).ToArray();
        var method = target.GetType()
            .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
            .SingleOrDefault(candidate =>
            {
                if (!string.Equals(candidate.Name, methodName, StringComparison.Ordinal))
                {
                    return false;
                }

                var parameters = candidate.GetParameters();
                if (parameters.Length != argumentTypes.Length)
                {
                    return false;
                }

                for (var index = 0; index < parameters.Length; index++)
                {
                    if (argumentTypes[index] == null)
                    {
                        continue;
                    }

                    if (!parameters[index].ParameterType.IsAssignableFrom(argumentTypes[index]))
                    {
                        return false;
                    }
                }

                return true;
            });

        Assert.That(method, Is.Not.Null, $"Expected private method {methodName}.");
        try
        {
            return method.Invoke(target, args);
        }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                throw ex.InnerException;
            }
        }

        private static GridViewState CreateRegionViewState(GridRegionKind regionKind, GridRegionState state, double? size)
        {
            var viewState = new GridViewState();
            viewState.RegionLayout.Add(new GridViewRegionState { RegionKind = GridRegionKind.CoreGridSurface, State = GridRegionState.Open, Size = null, IsActive = false });
            viewState.RegionLayout.Add(new GridViewRegionState { RegionKind = GridRegionKind.TopCommandRegion, State = GridRegionState.Open, Size = 44d, IsActive = false });
            viewState.RegionLayout.Add(new GridViewRegionState { RegionKind = GridRegionKind.GroupingRegion, State = GridRegionState.Open, Size = 56d, IsActive = false });
            viewState.RegionLayout.Add(new GridViewRegionState { RegionKind = GridRegionKind.SummaryBottomRegion, State = GridRegionState.Open, Size = 56d, IsActive = false });
            viewState.RegionLayout.Add(new GridViewRegionState { RegionKind = GridRegionKind.SideToolRegion, State = GridRegionState.Closed, Size = 320d, IsActive = false });

            var target = viewState.RegionLayout.Single(region => region.RegionKind == regionKind);
            target.State = state;
            target.Size = size;
            return viewState;
        }

        private static LayoutParityState CaptureLayoutParityState(
            PhialeTech.PhialeGrid.Wpf.Controls.PhialeGrid grid,
            PhialeTech.PhialeGrid.Wpf.Surface.GridSurfaceHost surfaceHost)
        {
            return new LayoutParityState(
                surfaceHost.CurrentSnapshot.ViewportState.ViewportWidth,
                surfaceHost.CurrentSnapshot.ViewportState.ViewportHeight,
                grid.Viewport.ViewportWidth,
                grid.Viewport.ViewportHeight,
                grid.SurfaceTopChromeHeight);
        }

        private sealed class TestRow
        {
            public string Category { get; set; }

            public string ObjectName { get; set; }
        }

        private sealed class LayoutParityState
        {
            public LayoutParityState(
                double surfaceViewportWidth,
                double surfaceViewportHeight,
                double gridViewportWidth,
                double gridViewportHeight,
                double topChromeHeight)
            {
                SurfaceViewportWidth = surfaceViewportWidth;
                SurfaceViewportHeight = surfaceViewportHeight;
                GridViewportWidth = gridViewportWidth;
                GridViewportHeight = gridViewportHeight;
                TopChromeHeight = topChromeHeight;
            }

            public double SurfaceViewportWidth { get; }

            public double SurfaceViewportHeight { get; }

            public double GridViewportWidth { get; }

            public double GridViewportHeight { get; }

            public double TopChromeHeight { get; }
        }

        private sealed class EmptyHierarchyProvider : IGridHierarchyProvider<object>
        {
            public System.Threading.Tasks.Task<IReadOnlyList<GridHierarchyNode<object>>> LoadChildrenAsync(GridHierarchyNode<object> parent, CancellationToken cancellationToken)
            {
                return System.Threading.Tasks.Task.FromResult((IReadOnlyList<GridHierarchyNode<object>>)Array.Empty<GridHierarchyNode<object>>());
            }
        }
    }
}
