using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
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
using PhialeGrid.Core.Summaries;
using PhialeTech.PhialeGrid.Wpf.Controls;
using PhialeTech.PhialeGrid.Wpf.Surface.Presenters;
using PhialeTech.Styles.Wpf;
using PhialeGrid.Wpf.Tests.Surface;

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

                var state = CreateRegionViewState(GridRegionKind.TopCommandRegion, GridRegionState.Open, 52d);

                grid.ApplyViewState(state);
                FlushDispatcher(grid.Dispatcher);

                var topCommandBand = (PhialeWorkspaceBand)grid.FindName("TopCommandBand");
                var host = (FrameworkElement)topCommandBand;
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
        public void FirstOpen_KeepsVisibleColumnHeaderContentInsideHeaderPresenterBounds()
        {
            var grid = CreateGrid();
            var window = CreateWindow(grid);

            try
            {
                window.Show();
                FlushDispatcher(grid.Dispatcher);

                var headerBand = (FrameworkElement)grid.FindName("SurfaceColumnHeaderBand");
                var firstHeader = FindVisualChild<GridColumnHeaderPresenter>(headerBand);
                var headerContent = firstHeader?.Content as Grid;
                var headerLayoutRoot = headerContent?.Children.Count > 0 ? headerContent.Children[0] as Grid : null;
                var headerText = GridSurfaceTestHost.FindElementByAutomationId<TextBlock>(firstHeader, "surface.column-header.Category.text");

                Assert.Multiple(() =>
                {
                    Assert.That(firstHeader, Is.Not.Null);
                    Assert.That(headerContent, Is.Not.Null);
                    Assert.That(headerLayoutRoot, Is.Not.Null);
                    Assert.That(headerText, Is.Not.Null);
                    Assert.That(headerLayoutRoot.Margin.Top, Is.GreaterThanOrEqualTo(0d));
                    Assert.That(headerLayoutRoot.Margin.Bottom, Is.GreaterThanOrEqualTo(0d));
                    Assert.That(headerLayoutRoot.Margin.Top + headerText.ActualHeight + headerLayoutRoot.Margin.Bottom,
                        Is.LessThanOrEqualTo(firstHeader.ActualHeight + 0.5d));
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
        public void FirstOpen_WhenSideToolRegionIsOpen_StartsPaneAtWorkspaceTop()
        {
            var grid = CreateGrid();
            grid.SideToolContent = new Border { Width = 120, Height = 600, Child = new TextBlock { Text = "Tools" } };

            var window = CreateWindow(grid);
            try
            {
                window.Show();
                FlushDispatcher(grid.Dispatcher);

                grid.ApplyViewState(CreateRegionViewState(GridRegionKind.SideToolRegion, GridRegionState.Open, 300d));
                FlushDispatcher(grid.Dispatcher);

                var topWorkspaceBandHost = (FrameworkElement)grid.FindName("TopWorkspaceBandHost");
                var sideToolHost = (FrameworkElement)grid.FindName("SideToolRegionHost");
                var sideToolScrollViewer = (ScrollViewer)grid.FindName("SideToolRegionContentScrollViewer");
                var sideToolExpandedShell = (FrameworkElement)grid.FindName("SideToolRegionExpandedShell");

                var workspaceTop = topWorkspaceBandHost.TranslatePoint(new Point(0d, 0d), grid).Y;
                var sideToolHostTop = sideToolHost.TranslatePoint(new Point(0d, 0d), grid).Y;
                var sideToolScrollTop = sideToolScrollViewer.TranslatePoint(new Point(0d, 0d), grid).Y;
                var sideToolShellTop = sideToolExpandedShell.TranslatePoint(new Point(0d, 0d), grid).Y;

                Assert.Multiple(() =>
                {
                    Assert.That(sideToolHost.Visibility, Is.EqualTo(Visibility.Visible));
                    Assert.That(sideToolScrollViewer.VerticalOffset, Is.EqualTo(0d).Within(0.1d));
                    Assert.That(sideToolHostTop, Is.EqualTo(workspaceTop).Within(1d));
                    Assert.That(sideToolShellTop, Is.EqualTo(workspaceTop).Within(1d));
                    Assert.That(sideToolScrollTop, Is.GreaterThanOrEqualTo(sideToolShellTop));
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
        public void ApplyViewState_WhenTopCommandBandHasContent_DoesNotClipCommandButtons()
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

                var state = CreateRegionViewState(GridRegionKind.TopCommandRegion, GridRegionState.Open, 52d);

                grid.ApplyViewState(state);
                FlushDispatcher(grid.Dispatcher);

                var row = (RowDefinition)grid.FindName("TopCommandStripRow");
                var topCommandBand = (PhialeWorkspaceBand)grid.FindName("TopCommandBand");
                var host = (FrameworkElement)topCommandBand;
                var shell = (FrameworkElement)topCommandBand.Template.FindName("WorkspaceBandShell", topCommandBand);
                var content = (FrameworkElement)grid.FindName("TopCommandStripContentHost");
                var toggle = (FrameworkElement)topCommandBand.Template.FindName("WorkspaceBandToggleButton", topCommandBand);
                var close = (Button)topCommandBand.Template.FindName("WorkspaceBandCloseButton", topCommandBand);
                var expectedCloseStyle = grid.FindResource("PgRegionCloseButtonStyle");

                Assert.Multiple(() =>
                {
                    Assert.That(row, Is.Not.Null);
                    Assert.That(host, Is.Not.Null);
                    Assert.That(host.Margin.Bottom, Is.EqualTo(0d));
                    Assert.That(row.MinHeight, Is.GreaterThanOrEqualTo(52d));
                    Assert.That(row.ActualHeight, Is.GreaterThanOrEqualTo(shell.DesiredSize.Height));
                    Assert.That(row.ActualHeight, Is.GreaterThanOrEqualTo(content.DesiredSize.Height));
                    Assert.That(toggle.Visibility, Is.EqualTo(Visibility.Collapsed));
                    Assert.That(close.Visibility, Is.EqualTo(Visibility.Visible));
                    Assert.That(close.Style, Is.SameAs(expectedCloseStyle));
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
        public void ApplyViewState_WhenTopCommandBandAndSideToolAreOpen_TopBandDoesNotSpanOverSideToolRegion()
        {
            var grid = CreateGrid();
            grid.TopCommandContent = new Border
            {
                Width = 260,
                Height = 30,
                Child = new TextBlock { Text = "Commands" }
            };
            grid.SideToolContent = new Border
            {
                Width = 180,
                Height = 300,
                Child = new TextBlock { Text = "Tools" }
            };

            var window = CreateWindow(grid);
            try
            {
                window.Show();
                FlushDispatcher(grid.Dispatcher);

                var state = CreateRegionViewState(GridRegionKind.TopCommandRegion, GridRegionState.Open, 52d);
                var sideState = state.RegionLayout.Single(region => region.RegionKind == GridRegionKind.SideToolRegion);
                sideState.State = GridRegionState.Open;
                sideState.Size = 300d;

                grid.ApplyViewState(state);
                FlushDispatcher(grid.Dispatcher);

                var topHost = (PhialeWorkspaceBand)grid.FindName("TopCommandBand");
                var topWorkspaceBandHost = (FrameworkElement)grid.FindName("TopWorkspaceBandHost");
                var surfaceHost = (FrameworkElement)grid.FindName("SurfaceTopViewportHost");
                var sideHost = (FrameworkElement)grid.FindName("SideToolRegionHost");
                var splitter = (FrameworkElement)grid.FindName("SideToolRegionSplitter");

                var topLeft = topHost.TranslatePoint(new Point(0d, 0d), grid);
                var topRight = topHost.TranslatePoint(new Point(topHost.ActualWidth, 0d), grid).X;
                var surfaceRight = surfaceHost.TranslatePoint(new Point(surfaceHost.ActualWidth, 0d), grid).X;
                var workspaceTop = topWorkspaceBandHost.TranslatePoint(new Point(0d, 0d), grid).Y;
                var sideTop = sideHost.TranslatePoint(new Point(0d, 0d), grid).Y;
                var splitterTop = splitter.TranslatePoint(new Point(0d, 0d), grid).Y;

                Assert.Multiple(() =>
                {
                    Assert.That(topHost.Visibility, Is.EqualTo(Visibility.Visible));
                    Assert.That(sideHost.Visibility, Is.EqualTo(Visibility.Visible));
                    Assert.That(topRight, Is.EqualTo(surfaceRight).Within(1d));
                    Assert.That(sideTop, Is.EqualTo(workspaceTop).Within(1d));
                    Assert.That(splitterTop, Is.EqualTo(workspaceTop).Within(1d));
                    Assert.That(sideTop, Is.LessThan(topLeft.Y));
                    Assert.That(sideHost.ActualHeight, Is.GreaterThan(topHost.ActualHeight));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void ApplyViewState_WhenRegionSurfacesAreVisible_UsesRegionSurfaceBackgroundToken()
        {
            var grid = CreateGrid();
            grid.TopCommandContent = new Border
            {
                Width = 260,
                Height = 30,
                Child = new TextBlock { Text = "Commands" }
            };
            grid.SideToolContent = new Border
            {
                Width = 180,
                Height = 300,
                Child = new TextBlock { Text = "Tools" }
            };

            var window = CreateWindow(grid);
            try
            {
                window.Show();
                FlushDispatcher(grid.Dispatcher);

                var state = CreateRegionViewState(GridRegionKind.TopCommandRegion, GridRegionState.Open, 52d);
                var sideState = state.RegionLayout.Single(region => region.RegionKind == GridRegionKind.SideToolRegion);
                sideState.State = GridRegionState.Open;
                sideState.Size = 300d;

                grid.ApplyViewState(state);
                FlushDispatcher(grid.Dispatcher);

                var expectedBrush = grid.FindResource("PgGridRegionSurfaceBackgroundBrush");
                var outerFrame = (Border)grid.FindName("GridOuterFrame");
                var rootFrame = (Grid)grid.FindName("GridRootFrame");
                var regionFrame = (Grid)grid.FindName("RegionLayoutFrame");
                var topHost = (PhialeWorkspaceBand)grid.FindName("TopCommandBand");
                var topShell = (Border)((PhialeWorkspaceBand)topHost).Template.FindName("WorkspaceBandShell", topHost);
                var viewportHost = (Grid)grid.FindName("SurfaceTopViewportHost");
                var sideHost = (Grid)grid.FindName("SideToolRegionHost");
                var sideShell = (Border)grid.FindName("SideToolRegionExpandedShell");
                var sideContentViewport = (ScrollViewer)grid.FindName("SideToolRegionContentScrollViewer");
                var splitter = (GridSplitter)grid.FindName("SideToolRegionSplitter");
                var sideHeaderChrome = (Border)grid.FindName("SideToolRegionHeaderChrome");
                var sideHeader = (DockPanel)grid.FindName("SideToolRegionHeader");
                var sideHeaderGrip = (TextBlock)grid.FindName("SideToolRegionDragGrip");
                var sideCloseButton = (Button)grid.FindName("SideToolRegionCloseButton");
                var expectedHeaderBrush = grid.FindResource("PgGridRegionHeaderBackgroundBrush");
                var expectedRegionRadius = (CornerRadius)grid.FindResource("CornerRadius.Grid.RegionSurface");
                var expectedHeaderRadius = (CornerRadius)grid.FindResource("CornerRadius.Grid.RegionHeader");

                Assert.Multiple(() =>
                {
                    Assert.That(expectedBrush, Is.Not.Null);
                    Assert.That(expectedHeaderBrush, Is.Not.Null);
                    Assert.That(outerFrame.Background, Is.SameAs(expectedBrush));
                    Assert.That(outerFrame.CornerRadius, Is.EqualTo(expectedRegionRadius));
                    Assert.That(rootFrame.Clip, Is.TypeOf<RectangleGeometry>());
                    Assert.That(((RectangleGeometry)rootFrame.Clip).RadiusX, Is.EqualTo(expectedRegionRadius.TopLeft).Within(0.1d));
                    Assert.That(RoundedChildClipBehavior.GetClipChildToBorder(outerFrame), Is.True);
                    Assert.That(RoundedChildClipBehavior.GetClipChildToBorder(sideShell), Is.False);
                    Assert.That(rootFrame.Background, Is.SameAs(expectedBrush));
                    Assert.That(regionFrame.Background, Is.SameAs(expectedBrush));
                    Assert.That(topHost.BandBackground, Is.SameAs(expectedBrush));
                    Assert.That(topShell.Background, Is.SameAs(expectedBrush));
                    Assert.That(viewportHost.Background, Is.SameAs(expectedBrush));
                    Assert.That(sideHost.Background, Is.SameAs(expectedBrush));
                    Assert.That(sideShell.Background, Is.SameAs(expectedBrush));
                    Assert.That(sideContentViewport.Background, Is.SameAs(expectedBrush));
                    Assert.That(splitter.Background, Is.SameAs(expectedBrush));
                    Assert.That(topShell.CornerRadius, Is.EqualTo(expectedRegionRadius));
                    Assert.That(sideShell.CornerRadius, Is.EqualTo(expectedRegionRadius));
                    Assert.That(sideHeaderChrome.Background, Is.SameAs(expectedHeaderBrush));
                    Assert.That(sideHeaderChrome.CornerRadius, Is.EqualTo(expectedHeaderRadius));
                    Assert.That(sideHeaderChrome.Padding.Left, Is.GreaterThanOrEqualTo(8d));
                    Assert.That(sideHeaderChrome.Padding.Right, Is.GreaterThanOrEqualTo(8d));
                    Assert.That(sideHeaderChrome.CornerRadius.TopRight, Is.LessThan(sideShell.CornerRadius.TopRight));
                    Assert.That(sideHeader.Background, Is.EqualTo(Brushes.Transparent));
                    Assert.That(sideHeader.Cursor, Is.SameAs(Cursors.SizeAll));
                    Assert.That(sideHeaderGrip.Visibility, Is.EqualTo(Visibility.Visible));
                    Assert.That(sideHeaderGrip.Text, Is.Not.Empty);
                    Assert.That(sideCloseButton.Margin.Right, Is.GreaterThan(0d));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void MoveRegion_WhenSideToolRegionMovesLeft_RepositionsWorkspacePanelAndExportsPlacement()
        {
            var grid = CreateGrid();
            grid.SideToolContent = new Border
            {
                Width = 180,
                Height = 300,
                Child = new TextBlock { Text = "Tools" }
            };

            var window = CreateWindow(grid);
            try
            {
                window.Show();
                FlushDispatcher(grid.Dispatcher);

                grid.ApplyViewState(CreateRegionViewState(GridRegionKind.SideToolRegion, GridRegionState.Open, 300d));
                FlushDispatcher(grid.Dispatcher);

                InvokePrivate(grid, "MoveRegion", GridRegionKind.SideToolRegion, GridRegionPlacement.Left);
                FlushDispatcher(grid.Dispatcher);

                var sideHost = (FrameworkElement)grid.FindName("SideToolRegionHost");
                var splitter = (FrameworkElement)grid.FindName("SideToolRegionSplitter");
                var topWorkspaceBandHost = (FrameworkElement)grid.FindName("TopWorkspaceBandHost");
                var regionFrame = (FrameworkElement)grid.FindName("RegionLayoutFrame");
                var surfaceHost = (FrameworkElement)grid.FindName("SurfaceTopViewportHost");
                var topBand = (FrameworkElement)grid.FindName("TopCommandBand");
                var expander = (Button)grid.FindName("SideToolRegionExpanderButton");
                var exported = grid.ExportViewState();
                var sideState = exported.RegionLayout.Single(region => region.RegionKind == GridRegionKind.SideToolRegion);

                Assert.Multiple(() =>
                {
                    Assert.That(Grid.GetColumn(sideHost), Is.EqualTo(0));
                    Assert.That(Grid.GetColumn(splitter), Is.EqualTo(1));
                    Assert.That(Grid.GetColumn(topWorkspaceBandHost), Is.EqualTo(2));
                    Assert.That(Grid.GetColumn(regionFrame), Is.EqualTo(2));
                    Assert.That(Grid.GetColumn(surfaceHost), Is.EqualTo(0));
                    Assert.That(Grid.GetColumn(topBand), Is.EqualTo(0));
                    Assert.That(expander.Content, Is.EqualTo("<"));
                    Assert.That(sideState.PlacementOverride, Is.EqualTo(GridRegionPlacement.Left));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void ApplyViewState_WhenWorkspacePanelsAreOnDifferentSides_FiltersBottomTabsByPlacement()
        {
            var grid = CreateGrid();
            grid.SideToolContent = new Border { Width = 120, Height = 80 };
            grid.ChangePanelContent = new Border { Width = 120, Height = 80 };
            grid.ValidationPanelContent = new Border { Width = 120, Height = 80 };

            var window = CreateWindow(grid);
            try
            {
                window.Show();
                FlushDispatcher(grid.Dispatcher);

                var viewState = CreateRegionViewState(GridRegionKind.SideToolRegion, GridRegionState.Open, 300d);
                viewState.RegionLayout.Single(region => region.RegionKind == GridRegionKind.SideToolRegion).PlacementOverride = GridRegionPlacement.Left;
                var changes = viewState.RegionLayout.Single(region => region.RegionKind == GridRegionKind.ChangePanelRegion);
                changes.State = GridRegionState.Open;
                changes.Size = 300d;
                changes.IsActive = true;
                var validation = viewState.RegionLayout.Single(region => region.RegionKind == GridRegionKind.ValidationPanelRegion);
                validation.State = GridRegionState.Collapsed;
                validation.Size = 300d;
                validation.PlacementOverride = GridRegionPlacement.Right;

                grid.ApplyViewState(viewState);
                FlushDispatcher(grid.Dispatcher);

                var toolsPanelToolsTab = (Button)grid.FindName("ToolsPanelToolsTabButton");
                var toolsPanelChangesTab = (Button)grid.FindName("ToolsPanelChangesTabButton");
                var toolsPanelValidationTab = (Button)grid.FindName("ToolsPanelValidationTabButton");
                var validationPanelToolsTab = (Button)grid.FindName("ValidationPanelToolsTabButton");
                var validationPanelChangesTab = (Button)grid.FindName("ValidationPanelChangesTabButton");
                var validationPanelValidationTab = (Button)grid.FindName("ValidationPanelValidationTabButton");
                var validationRail = (FrameworkElement)grid.FindName("ValidationPanelCollapsedRail");
                var changesContent = (FrameworkElement)grid.FindName("ChangePanelContentScrollViewer");
                var rightColumn = (ColumnDefinition)grid.FindName("SideToolRegionColumn");

                Assert.Multiple(() =>
                {
                    Assert.That(toolsPanelToolsTab.Visibility, Is.EqualTo(Visibility.Visible));
                    Assert.That(toolsPanelChangesTab.Visibility, Is.EqualTo(Visibility.Collapsed));
                    Assert.That(toolsPanelValidationTab.Visibility, Is.EqualTo(Visibility.Collapsed));
                    Assert.That(validationPanelToolsTab.Visibility, Is.EqualTo(Visibility.Collapsed));
                    Assert.That(validationPanelChangesTab.Visibility, Is.EqualTo(Visibility.Visible));
                    Assert.That(validationPanelValidationTab.Visibility, Is.EqualTo(Visibility.Visible));
                    Assert.That(validationRail.Visibility, Is.EqualTo(Visibility.Collapsed));
                    Assert.That(changesContent.Visibility, Is.EqualTo(Visibility.Visible));
                    Assert.That(rightColumn.Width.Value, Is.EqualTo(300d).Within(0.1d));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void ApplyViewState_WhenSummaryDesignerAndSummariesBandHaveContent_RendersThemAroundGrid()
        {
            var grid = CreateGrid(new object[]
            {
                new TestRow { Category = "Road", ObjectName = "Road 1" },
                new TestRow { Category = "Road", ObjectName = "Road 2" },
            });
            grid.SummaryDesignerContent = new Border { Width = 120, Height = 80 };
            grid.Summaries = new[]
            {
                new GridSummaryDescriptor("ObjectName", GridSummaryType.Count, typeof(string)),
            };

            var window = CreateWindow(grid);
            try
            {
                window.Show();
                FlushDispatcher(grid.Dispatcher);

                var viewState = CreateRegionViewState(GridRegionKind.SummaryDesignerRegion, GridRegionState.Open, 320d);
                var summariesBand = viewState.RegionLayout.Single(region => region.RegionKind == GridRegionKind.SummaryBottomRegion);
                summariesBand.State = GridRegionState.Open;
                summariesBand.Size = 56d;
                viewState.Summaries.Add(new GridViewSummaryState
                {
                    ColumnId = "ObjectName",
                    Type = GridSummaryType.Count,
                });

                grid.ApplyViewState(viewState);
                FlushDispatcher(grid.Dispatcher);

                var designerHost = (FrameworkElement)grid.FindName("SummaryDesignerRegionHost");
                var designerContent = (FrameworkElement)grid.FindName("SummaryDesignerContentScrollViewer");
                var summariesBandHost = (FrameworkElement)grid.FindName("SummaryBottomRegionHost");
                var summariesBandContent = (FrameworkElement)grid.FindName("SummaryBottomRegionContentScrollViewer");

                Assert.Multiple(() =>
                {
                    Assert.That(designerHost.Visibility, Is.EqualTo(Visibility.Visible));
                    Assert.That(designerContent.Visibility, Is.EqualTo(Visibility.Visible));
                    Assert.That(summariesBandHost.Visibility, Is.EqualTo(Visibility.Visible));
                    Assert.That(summariesBandContent.Visibility, Is.EqualTo(Visibility.Visible));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void ApplyViewState_WhenWorkspacePanelTabsAreVisible_SizesTabsToTextPlusOneAOnEachSide()
        {
            var grid = CreateGrid();
            grid.SideToolContent = new Border { Width = 120, Height = 80 };
            grid.ChangePanelContent = new Border { Width = 120, Height = 80 };
            grid.ValidationPanelContent = new Border { Width = 120, Height = 80 };

            var window = CreateWindow(grid);
            try
            {
                window.Show();
                FlushDispatcher(grid.Dispatcher);

                var viewState = CreateRegionViewState(GridRegionKind.ChangePanelRegion, GridRegionState.Open, 300d);
                var tools = viewState.RegionLayout.Single(region => region.RegionKind == GridRegionKind.SideToolRegion);
                tools.State = GridRegionState.Open;
                tools.Size = 300d;
                tools.PlacementOverride = GridRegionPlacement.Right;
                var validation = viewState.RegionLayout.Single(region => region.RegionKind == GridRegionKind.ValidationPanelRegion);
                validation.State = GridRegionState.Collapsed;
                validation.Size = 300d;
                validation.PlacementOverride = GridRegionPlacement.Right;

                grid.ApplyViewState(viewState);
                FlushDispatcher(grid.Dispatcher);

                var toolsTab = (Button)grid.FindName("ChangesPanelToolsTabButton");
                var validationTab = (Button)grid.FindName("ChangesPanelValidationTabButton");

                Assert.Multiple(() =>
                {
                    AssertTabWidthMatchesTextPlusOneAOnEachSide(toolsTab, "Grid options");
                    AssertTabWidthMatchesTextPlusOneAOnEachSide(validationTab, "Validation");
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void ApplyViewState_WhenWorkspacePanelTabsAreVisible_RendersTabsBelowExpandedShell()
        {
            var grid = CreateGrid();
            grid.SideToolContent = new Border { Width = 120, Height = 80 };
            grid.ChangePanelContent = new Border { Width = 120, Height = 80 };
            grid.ValidationPanelContent = new Border { Width = 120, Height = 80 };
            grid.SummaryDesignerContent = new Border { Width = 120, Height = 80 };

            var window = CreateWindow(grid);
            try
            {
                window.Show();
                FlushDispatcher(grid.Dispatcher);

                grid.ApplyViewState(CreateRegionViewState(GridRegionKind.SideToolRegion, GridRegionState.Open, 300d));
                FlushDispatcher(grid.Dispatcher);

                var panel = (PhialeWorkspacePanel)grid.FindName("ToolsPanel");
                var shell = (Border)grid.FindName("SideToolRegionExpandedShell");
                var tabStrip = (FrameworkElement)grid.FindName("ToolsPanelExpandedTabStrip");
                var toolsTab = (Button)grid.FindName("ToolsPanelToolsTabButton");
                var tabChrome = FindVisualChild<System.Windows.Shapes.Path>(toolsTab);
                var tabChromeFigure = PathGeometry.CreateFromGeometry(tabChrome.Data).Figures[0];
                var tabChromeFirstLine = (LineSegment)tabChromeFigure.Segments[0];
                var tabTopInShell = tabStrip.TranslatePoint(new Point(0d, 0d), shell).Y;

                Assert.Multiple(() =>
                {
                    Assert.That(IsVisualAncestor(panel, tabStrip), Is.True);
                    Assert.That(IsVisualAncestor(shell, tabStrip), Is.False);
                    Assert.That(IsVisualAncestor(shell, toolsTab), Is.False);
                    Assert.That(tabTopInShell, Is.GreaterThanOrEqualTo(shell.ActualHeight - 2d));
                    Assert.That(tabChromeFigure.StartPoint, Is.EqualTo(new Point(0.5d, 0.5d)));
                    Assert.That(tabChromeFirstLine.Point, Is.EqualTo(new Point(5.5d, 16.5d)));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void ApplyViewState_WhenSummaryDesignerIsWorkspacePanel_ShowsTheSameTabsAsOtherPanels()
        {
            var grid = CreateGrid();
            grid.SideToolContent = new Border { Width = 120, Height = 80 };
            grid.ChangePanelContent = new Border { Width = 120, Height = 80 };
            grid.ValidationPanelContent = new Border { Width = 120, Height = 80 };
            grid.SummaryDesignerContent = new Border { Width = 120, Height = 80 };

            var window = CreateWindow(grid);
            try
            {
                window.Show();
                FlushDispatcher(grid.Dispatcher);

                var viewState = CreateRegionViewState(GridRegionKind.SummaryDesignerRegion, GridRegionState.Open, 520d);
                viewState.RegionLayout.Single(region => region.RegionKind == GridRegionKind.SideToolRegion).State = GridRegionState.Collapsed;
                viewState.RegionLayout.Single(region => region.RegionKind == GridRegionKind.ChangePanelRegion).State = GridRegionState.Collapsed;
                var validation = viewState.RegionLayout.Single(region => region.RegionKind == GridRegionKind.ValidationPanelRegion);
                validation.State = GridRegionState.Collapsed;
                validation.PlacementOverride = GridRegionPlacement.Right;

                grid.ApplyViewState(viewState);
                FlushDispatcher(grid.Dispatcher);
                FlushDispatcher(grid.Dispatcher);

                var shell = (Border)grid.FindName("SummaryDesignerExpandedShell");
                var tabStrip = (FrameworkElement)grid.FindName("SummaryDesignerExpandedTabStrip");
                var toolsTab = (Button)grid.FindName("SummaryDesignerPanelToolsTabButton");
                var changesTab = (Button)grid.FindName("SummaryDesignerPanelChangesTabButton");
                var validationTab = (Button)grid.FindName("SummaryDesignerPanelValidationTabButton");
                var summaryDesignerTab = (Button)grid.FindName("SummaryDesignerPanelSummaryDesignerTabButton");

                Assert.Multiple(() =>
                {
                    Assert.That(shell.Visibility, Is.EqualTo(Visibility.Visible));
                    Assert.That(tabStrip.Visibility, Is.EqualTo(Visibility.Visible));
                    Assert.That(toolsTab.Visibility, Is.EqualTo(Visibility.Visible));
                    Assert.That(changesTab.Visibility, Is.EqualTo(Visibility.Visible));
                    Assert.That(validationTab.Visibility, Is.EqualTo(Visibility.Visible));
                    Assert.That(summaryDesignerTab.Visibility, Is.EqualTo(Visibility.Visible));
                    Assert.That(summaryDesignerTab.Style, Is.SameAs(grid.TryFindResource("PgWorkspacePanelExpandedActiveTabButtonStyle")));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void Click_WhenWorkspacePanelsAreExpanded_CollapsesSummaryDesignerWithTheOtherPanels()
        {
            var grid = CreateGrid();
            grid.SideToolContent = new Border { Width = 120, Height = 80 };
            grid.ChangePanelContent = new Border { Width = 120, Height = 80 };
            grid.ValidationPanelContent = new Border { Width = 120, Height = 80 };
            grid.SummaryDesignerContent = new Border { Width = 120, Height = 80 };

            var window = CreateWindow(grid);
            try
            {
                window.Show();
                FlushDispatcher(grid.Dispatcher);

                var viewState = CreateRegionViewState(GridRegionKind.SideToolRegion, GridRegionState.Open, 320d);
                viewState.RegionLayout.Single(region => region.RegionKind == GridRegionKind.ChangePanelRegion).State = GridRegionState.Open;
                viewState.RegionLayout.Single(region => region.RegionKind == GridRegionKind.SummaryDesignerRegion).State = GridRegionState.Open;

                grid.ApplyViewState(viewState);
                FlushDispatcher(grid.Dispatcher);

                var expander = (Button)grid.FindName("SideToolRegionExpanderButton");
                expander.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                FlushDispatcher(grid.Dispatcher);

                var exported = grid.ExportViewState();
                var sideTool = exported.RegionLayout.Single(region => region.RegionKind == GridRegionKind.SideToolRegion);
                var changes = exported.RegionLayout.Single(region => region.RegionKind == GridRegionKind.ChangePanelRegion);
                var summaryDesigner = exported.RegionLayout.Single(region => region.RegionKind == GridRegionKind.SummaryDesignerRegion);
                var summaryDesignerShell = (FrameworkElement)grid.FindName("SummaryDesignerExpandedShell");

                Assert.Multiple(() =>
                {
                    Assert.That(sideTool.State, Is.EqualTo(GridRegionState.Collapsed));
                    Assert.That(changes.State, Is.EqualTo(GridRegionState.Collapsed));
                    Assert.That(summaryDesigner.State, Is.EqualTo(GridRegionState.Collapsed));
                    Assert.That(summaryDesignerShell.Visibility, Is.Not.EqualTo(Visibility.Visible));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void ApplyViewState_WhenWorkspacePanelTabsDoNotFit_ShowsOverflowMenuForHiddenTabs()
        {
            var grid = CreateGrid();
            grid.SideToolContent = new Border { Width = 120, Height = 80 };
            grid.ChangePanelContent = new Border { Width = 120, Height = 80 };
            grid.ValidationPanelContent = new Border { Width = 120, Height = 80 };
            grid.SummaryDesignerContent = new Border { Width = 120, Height = 80 };

            var window = CreateWindow(grid);
            try
            {
                window.Show();
                FlushDispatcher(grid.Dispatcher);

                var viewState = CreateRegionViewState(GridRegionKind.SideToolRegion, GridRegionState.Open, 220d);
                var changes = viewState.RegionLayout.Single(region => region.RegionKind == GridRegionKind.ChangePanelRegion);
                changes.State = GridRegionState.Collapsed;
                changes.Size = 220d;
                changes.PlacementOverride = GridRegionPlacement.Right;
                var validation = viewState.RegionLayout.Single(region => region.RegionKind == GridRegionKind.ValidationPanelRegion);
                validation.State = GridRegionState.Collapsed;
                validation.Size = 220d;
                validation.PlacementOverride = GridRegionPlacement.Right;

                grid.ApplyViewState(viewState);
                FlushDispatcher(grid.Dispatcher);
                FlushDispatcher(grid.Dispatcher);

                var activeTab = (Button)grid.FindName("ToolsPanelToolsTabButton");
                var changesTab = (Button)grid.FindName("ToolsPanelChangesTabButton");
                var validationTab = (Button)grid.FindName("ToolsPanelValidationTabButton");
                var overflowTab = (Button)grid.FindName("ToolsPanelOverflowTabButton");

                Assert.Multiple(() =>
                {
                    Assert.That(activeTab.Visibility, Is.EqualTo(Visibility.Visible));
                    Assert.That(overflowTab.Visibility, Is.EqualTo(Visibility.Visible));
                    Assert.That(overflowTab.ContextMenu, Is.Not.Null);
                    Assert.That(overflowTab.ContextMenu.Items.Count, Is.GreaterThan(0));
                    Assert.That(
                        new[] { changesTab.Visibility, validationTab.Visibility }.Count(visibility => visibility == Visibility.Collapsed),
                        Is.GreaterThan(0));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void ApplyViewState_WhenWorkspacePanelIsCollapsed_SizesRailTabsToTextPlusOneAOnEachSideAndCentersStack()
        {
            var grid = CreateGrid();
            grid.SideToolContent = new Border { Width = 120, Height = 80 };

            var window = CreateWindow(grid);
            try
            {
                window.Show();
                FlushDispatcher(grid.Dispatcher);

                grid.ApplyViewState(CreateRegionViewState(GridRegionKind.SideToolRegion, GridRegionState.Collapsed, 300d));
                FlushDispatcher(grid.Dispatcher);

                var toolsTab = (Button)grid.FindName("ToolsRailToolsTabButton");
                var expandedTabStrip = (FrameworkElement)grid.FindName("ToolsPanelExpandedTabStrip");
                var tabStack = (FrameworkElement)VisualTreeHelper.GetParent(toolsTab);

                Assert.Multiple(() =>
                {
                    Assert.That(toolsTab.Visibility, Is.EqualTo(Visibility.Visible));
                    Assert.That(expandedTabStrip.Visibility, Is.EqualTo(Visibility.Collapsed));
                    AssertRailTabHeightMatchesTextPlusOneAOnEachSide(toolsTab, "Grid options");
                    Assert.That(tabStack.VerticalAlignment, Is.EqualTo(VerticalAlignment.Center));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void OpenWorkspacePanel_WhenPanelIsCollapsed_OpensBeforeActivating()
        {
            var grid = CreateGrid();
            grid.ChangePanelContent = new Border { Width = 120, Height = 80 };

            var window = CreateWindow(grid);
            try
            {
                window.Show();
                FlushDispatcher(grid.Dispatcher);

                grid.ApplyViewState(CreateRegionViewState(GridRegionKind.ChangePanelRegion, GridRegionState.Collapsed, 300d));
                FlushDispatcher(grid.Dispatcher);

                grid.OpenWorkspacePanel(GridRegionKind.ChangePanelRegion);
                FlushDispatcher(grid.Dispatcher);

                var exported = grid.ExportViewState();
                var changeState = exported.RegionLayout.Single(region => region.RegionKind == GridRegionKind.ChangePanelRegion);

                Assert.Multiple(() =>
                {
                    Assert.That(changeState.State, Is.EqualTo(GridRegionState.Open));
                    Assert.That(changeState.IsActive, Is.True);
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void Click_WhenCollapsedWorkspacePanelToggleIsPressed_ExpandsThatPanel()
        {
            var grid = CreateGrid();
            grid.ChangePanelContent = new Border { Width = 120, Height = 80 };
            grid.ValidationPanelContent = new Border { Width = 120, Height = 80 };

            var window = CreateWindow(grid);
            try
            {
                window.Show();
                FlushDispatcher(grid.Dispatcher);

                var viewState = CreateRegionViewState(GridRegionKind.ChangePanelRegion, GridRegionState.Collapsed, 300d);
                var validation = viewState.RegionLayout.Single(region => region.RegionKind == GridRegionKind.ValidationPanelRegion);
                validation.State = GridRegionState.Open;
                validation.Size = 300d;
                validation.PlacementOverride = GridRegionPlacement.Right;
                validation.IsActive = true;

                grid.ApplyViewState(viewState);
                FlushDispatcher(grid.Dispatcher);

                var collapsedToggle = new Button { Tag = "ChangePanelRegion" };
                InvokePrivate(grid, "HandleRegionToggleButtonClick", collapsedToggle, new RoutedEventArgs(Button.ClickEvent, collapsedToggle));
                FlushDispatcher(grid.Dispatcher);

                var exported = grid.ExportViewState();
                var changeState = exported.RegionLayout.Single(region => region.RegionKind == GridRegionKind.ChangePanelRegion);

                Assert.Multiple(() =>
                {
                    Assert.That(changeState.State, Is.EqualTo(GridRegionState.Open));
                    Assert.That(changeState.IsActive, Is.True);
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void Click_WhenWorkspacePanelTabOpensCollapsedPanel_ShowsSplitter()
        {
            var grid = CreateGrid();
            grid.ChangePanelContent = new Border { Width = 120, Height = 80 };
            grid.ValidationPanelContent = new Border { Width = 120, Height = 80 };

            var window = CreateWindow(grid);
            try
            {
                window.Show();
                FlushDispatcher(grid.Dispatcher);

                var viewState = CreateRegionViewState(GridRegionKind.ValidationPanelRegion, GridRegionState.Open, 300d);
                var validation = viewState.RegionLayout.Single(region => region.RegionKind == GridRegionKind.ValidationPanelRegion);
                validation.PlacementOverride = GridRegionPlacement.Right;
                validation.IsActive = true;
                var changes = viewState.RegionLayout.Single(region => region.RegionKind == GridRegionKind.ChangePanelRegion);
                changes.State = GridRegionState.Collapsed;
                changes.Size = 300d;
                changes.PlacementOverride = GridRegionPlacement.Right;

                grid.ApplyViewState(viewState);
                FlushDispatcher(grid.Dispatcher);

                var changesTab = (Button)grid.FindName("ValidationPanelChangesTabButton");
                changesTab.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, changesTab));
                FlushDispatcher(grid.Dispatcher);

                var exported = grid.ExportViewState();
                var changeState = exported.RegionLayout.Single(region => region.RegionKind == GridRegionKind.ChangePanelRegion);
                var splitterColumn = (ColumnDefinition)grid.FindName("SideToolRegionSplitterColumn");
                var changeSplitter = (FrameworkElement)grid.FindName("ChangePanelRegionSplitter");
                var changeHost = (FrameworkElement)grid.FindName("ChangePanelRegionHost");
                var changeTabStrip = (FrameworkElement)grid.FindName("ChangesPanelExpandedTabStrip");
                var validationHost = (FrameworkElement)grid.FindName("ValidationPanelRegionHost");
                var validationTabStrip = (FrameworkElement)grid.FindName("ValidationPanelExpandedTabStrip");

                Assert.Multiple(() =>
                {
                    Assert.That(changeState.State, Is.EqualTo(GridRegionState.Open));
                    Assert.That(changeState.IsActive, Is.True);
                    Assert.That(changeHost.Visibility, Is.EqualTo(Visibility.Visible));
                    Assert.That(changeTabStrip.Visibility, Is.EqualTo(Visibility.Visible));
                    Assert.That(validationHost.Visibility, Is.EqualTo(Visibility.Collapsed));
                    Assert.That(validationTabStrip.Visibility, Is.EqualTo(Visibility.Collapsed));
                    Assert.That(Grid.GetColumn(changeSplitter), Is.EqualTo(3));
                    Assert.That(splitterColumn.ActualWidth, Is.GreaterThan(0d));
                    Assert.That(changeSplitter.ActualWidth, Is.GreaterThan(0d));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void Click_WhenCollapsedRailTabOpensPanel_ShowsSplitter()
        {
            var grid = CreateGrid();
            grid.ChangePanelContent = new Border { Width = 120, Height = 80 };
            grid.ValidationPanelContent = new Border { Width = 120, Height = 80 };

            var window = CreateWindow(grid);
            try
            {
                window.Show();
                FlushDispatcher(grid.Dispatcher);

                var viewState = CreateRegionViewState(GridRegionKind.ValidationPanelRegion, GridRegionState.Collapsed, 300d);
                var validation = viewState.RegionLayout.Single(region => region.RegionKind == GridRegionKind.ValidationPanelRegion);
                validation.PlacementOverride = GridRegionPlacement.Right;
                var changes = viewState.RegionLayout.Single(region => region.RegionKind == GridRegionKind.ChangePanelRegion);
                changes.State = GridRegionState.Collapsed;
                changes.Size = 300d;
                changes.PlacementOverride = GridRegionPlacement.Right;

                grid.ApplyViewState(viewState);
                FlushDispatcher(grid.Dispatcher);

                var changesRailTab = (Button)grid.FindName("ValidationRailChangesTabButton");
                changesRailTab.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, changesRailTab));
                FlushDispatcher(grid.Dispatcher);

                var exported = grid.ExportViewState();
                var changeState = exported.RegionLayout.Single(region => region.RegionKind == GridRegionKind.ChangePanelRegion);
                var splitterColumn = (ColumnDefinition)grid.FindName("SideToolRegionSplitterColumn");
                var changeSplitter = (FrameworkElement)grid.FindName("ChangePanelRegionSplitter");
                var changeHost = (FrameworkElement)grid.FindName("ChangePanelRegionHost");

                Assert.Multiple(() =>
                {
                    Assert.That(changeState.State, Is.EqualTo(GridRegionState.Open));
                    Assert.That(changeState.IsActive, Is.True);
                    Assert.That(changeHost.Visibility, Is.EqualTo(Visibility.Visible));
                    Assert.That(Grid.GetColumn(changeSplitter), Is.EqualTo(3));
                    Assert.That(splitterColumn.ActualWidth, Is.GreaterThan(0d));
                    Assert.That(changeSplitter.ActualWidth, Is.GreaterThan(0d));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void MoveRegion_WhenGroupingRegionMovesBottom_ReparentsWorkspaceBandAndExportsPlacement()
        {
            var grid = CreateGrid();

            var window = CreateWindow(grid);
            try
            {
                window.Show();
                FlushDispatcher(grid.Dispatcher);

                grid.ApplyViewState(CreateRegionViewState(GridRegionKind.GroupingRegion, GridRegionState.Open, 56d));
                FlushDispatcher(grid.Dispatcher);

                InvokePrivate(grid, "MoveRegion", GridRegionKind.GroupingRegion, GridRegionPlacement.Bottom);
                FlushDispatcher(grid.Dispatcher);

                var groupingHost = (FrameworkElement)grid.FindName("GroupingRegionHost");
                var bottomBandHost = (Panel)grid.FindName("BottomWorkspaceBandHost");
                var exported = grid.ExportViewState();
                var groupingState = exported.RegionLayout.Single(region => region.RegionKind == GridRegionKind.GroupingRegion);

                Assert.Multiple(() =>
                {
                    Assert.That(groupingHost.Parent, Is.SameAs(bottomBandHost));
                    Assert.That(groupingState.PlacementOverride, Is.EqualTo(GridRegionPlacement.Bottom));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void MoveRegion_WhenTopCommandRegionMovesBottom_ReclaimsTopCommandRow()
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

                grid.ApplyViewState(CreateRegionViewState(GridRegionKind.TopCommandRegion, GridRegionState.Open, 52d));
                FlushDispatcher(grid.Dispatcher);

                InvokePrivate(grid, "MoveRegion", GridRegionKind.TopCommandRegion, GridRegionPlacement.Bottom);
                FlushDispatcher(grid.Dispatcher);

                var topCommandBand = (FrameworkElement)grid.FindName("TopCommandBand");
                var bottomBandHost = (Panel)grid.FindName("BottomWorkspaceBandHost");
                var topCommandRow = (RowDefinition)grid.FindName("TopCommandStripRow");
                var exported = grid.ExportViewState();
                var topCommandState = exported.RegionLayout.Single(region => region.RegionKind == GridRegionKind.TopCommandRegion);

                Assert.Multiple(() =>
                {
                    Assert.That(topCommandBand.Parent, Is.SameAs(bottomBandHost));
                    Assert.That(topCommandRow.ActualHeight, Is.EqualTo(0d).Within(0.1d));
                    Assert.That(topCommandState.PlacementOverride, Is.EqualTo(GridRegionPlacement.Bottom));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void Click_WhenSideToolRegionExpanderIsPressed_CollapsesWorkspacePanel()
        {
            var grid = CreateGrid();
            grid.SideToolContent = new Border
            {
                Width = 180,
                Height = 300,
                Child = new TextBlock { Text = "Tools" }
            };

            var window = CreateWindow(grid);
            try
            {
                window.Show();
                FlushDispatcher(grid.Dispatcher);

                grid.ApplyViewState(CreateRegionViewState(GridRegionKind.SideToolRegion, GridRegionState.Open, 300d));
                FlushDispatcher(grid.Dispatcher);

                var expander = (Button)grid.FindName("SideToolRegionExpanderButton");
                var expectedCursor = expander.Cursor;
                expander.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, expander));
                FlushDispatcher(grid.Dispatcher);

                var exported = grid.ExportViewState();
                var sideState = exported.RegionLayout.Single(region => region.RegionKind == GridRegionKind.SideToolRegion);
                var collapsedRail = (FrameworkElement)grid.FindName("SideToolRegionCollapsedRail");
                var expandedShell = (FrameworkElement)grid.FindName("SideToolRegionExpandedShell");

                Assert.Multiple(() =>
                {
                    Assert.That(expectedCursor, Is.SameAs(Cursors.Hand));
                    Assert.That(sideState.State, Is.EqualTo(GridRegionState.Collapsed));
                    Assert.That(collapsedRail.Visibility, Is.EqualTo(Visibility.Visible));
                    Assert.That(expandedShell.Visibility, Is.EqualTo(Visibility.Collapsed));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void DragStart_WhenPointerStartsOnRegionButton_DoesNotArmRegionDrag()
        {
            var grid = CreateGrid();
            var button = new Button { Content = ">" };

            var isInteractiveChild = (bool)InvokePrivate(grid, "IsRegionDragStartedFromInteractiveChild", button);

            Assert.That(isInteractiveChild, Is.True);
        }

        [Test]
        public void DragPreview_WhenSideToolRegionIsDragged_AnimatesWorkspacePanelPane()
        {
            var grid = CreateGrid();
            grid.SideToolContent = new Border
            {
                Width = 180,
                Height = 300,
                Child = new TextBlock { Text = "Tools" }
            };

            var window = CreateWindow(grid);
            try
            {
                window.Show();
                FlushDispatcher(grid.Dispatcher);

                grid.ApplyViewState(CreateRegionViewState(GridRegionKind.SideToolRegion, GridRegionState.Open, 300d));
                FlushDispatcher(grid.Dispatcher);

                InvokePrivate(grid, "BeginRegionDragPreview", GridRegionKind.SideToolRegion);
                InvokePrivate(grid, "UpdateRegionDragPreview", new Vector(-180d, 0d), new Point(40d, 20d));
                FlushDispatcher(grid.Dispatcher);

                var host = (Grid)grid.FindName("SideToolRegionHost");
                var panel = (Grid)grid.FindName("ToolsPanel");
                var shell = (Border)grid.FindName("SideToolRegionExpandedShell");
                var transform = (TranslateTransform)shell.RenderTransform;
                var overlay = (FrameworkElement)grid.FindName("RegionDockPreviewOverlay");
                var leftDockPreview = (Border)grid.FindName("RegionDockPreviewLeft");
                var rightDockPreview = (Border)grid.FindName("RegionDockPreviewRight");

                Assert.Multiple(() =>
                {
                    Assert.That(host.Background, Is.SameAs(Brushes.Transparent));
                    Assert.That(panel.Background, Is.SameAs(Brushes.Transparent));
                    Assert.That(shell.Opacity, Is.LessThan(1d));
                    Assert.That(transform.X, Is.LessThan(-100d));
                    Assert.That(overlay.Visibility, Is.EqualTo(Visibility.Visible));
                    Assert.That(leftDockPreview.Opacity, Is.GreaterThan(rightDockPreview.Opacity));
                });

                InvokePrivate(grid, "EndRegionDragPreview");
                FlushDispatcher(grid.Dispatcher);
                Assert.That(host.Background, Is.Not.SameAs(Brushes.Transparent));
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void DragPreview_WhenWorkspaceBandIsDragged_ShowsVerticalDockTargetsWithoutMovingWorkspacePanelPane()
        {
            var grid = CreateGrid();

            var window = CreateWindow(grid);
            try
            {
                window.Show();
                FlushDispatcher(grid.Dispatcher);

                grid.ApplyViewState(CreateRegionViewState(GridRegionKind.GroupingRegion, GridRegionState.Open, 56d));
                FlushDispatcher(grid.Dispatcher);

                InvokePrivate(grid, "BeginRegionDragPreview", GridRegionKind.GroupingRegion);
                InvokePrivate(grid, "UpdateRegionDragPreview", new Vector(0d, 140d), new Point(40d, 20d), new Point(40d, 400d));
                FlushDispatcher(grid.Dispatcher);

                var shell = (Border)grid.FindName("SideToolRegionExpandedShell");
                var transform = (TranslateTransform)shell.RenderTransform;
                var overlay = (FrameworkElement)grid.FindName("WorkspaceBandDockPreviewOverlay");
                var regionOverlay = (FrameworkElement)grid.FindName("RegionDockPreviewOverlay");
                var regionLayoutFrame = (FrameworkElement)grid.FindName("RegionLayoutFrame");
                var topDockPreview = (Border)grid.FindName("RegionDockPreviewTop");
                var bottomDockPreview = (Border)grid.FindName("RegionDockPreviewBottom");
                var leftDockPreview = (Border)grid.FindName("RegionDockPreviewLeft");
                var rightDockPreview = (Border)grid.FindName("RegionDockPreviewRight");

                Assert.Multiple(() =>
                {
                    Assert.That(transform.X, Is.EqualTo(0d).Within(0.1d));
                    Assert.That(overlay.Visibility, Is.EqualTo(Visibility.Visible));
                    Assert.That(Grid.GetColumn(overlay), Is.EqualTo(Grid.GetColumn(regionLayoutFrame)));
                    Assert.That(regionOverlay.Visibility, Is.EqualTo(Visibility.Collapsed));
                    Assert.That(topDockPreview.Visibility, Is.EqualTo(Visibility.Visible));
                    Assert.That(bottomDockPreview.Visibility, Is.EqualTo(Visibility.Visible));
                    Assert.That(leftDockPreview.Visibility, Is.EqualTo(Visibility.Collapsed));
                    Assert.That(rightDockPreview.Visibility, Is.EqualTo(Visibility.Collapsed));
                    Assert.That(bottomDockPreview.Opacity, Is.GreaterThan(topDockPreview.Opacity));
                });

                InvokePrivate(grid, "EndRegionDragPreview");
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void DragPreview_WhenWorkspaceBandIsDragged_UsesRootOverlayForBottomDockTargetBelowSurfaceScrollbar()
        {
            var grid = CreateGrid();
            grid.ItemsSource = Enumerable.Range(0, 8)
                .Select(index => new TestRow { Category = "A", ObjectName = "Wide object " + index })
                .ToArray();
            grid.Columns = new[]
                {
                    new GridColumnDefinition("Category", "Category", width: 140d, displayIndex: 0, valueType: typeof(string)),
                    new GridColumnDefinition("ObjectName", "Object name", width: 220d, displayIndex: 1, valueType: typeof(string)),
                }
                .Concat(Enumerable.Range(0, 12).Select(index => new GridColumnDefinition(
                    "extra" + index,
                    "Extra " + index,
                    width: 160d,
                    displayIndex: index + 2,
                    valueType: typeof(string))))
                .ToArray();

            var window = CreateWindow(grid);
            try
            {
                window.Width = 720;
                window.Height = 520;
                window.Show();
                FlushDispatcher(grid.Dispatcher);

                InvokePrivate(grid, "BeginRegionDragPreview", GridRegionKind.GroupingRegion);
                InvokePrivate(grid, "UpdateRegionDragPreview", new Vector(0d, 220d), new Point(40d, 20d), new Point(40d, 420d));
                FlushDispatcher(grid.Dispatcher);

                var bottomDockPreview = (Border)grid.FindName("RegionDockPreviewBottom");
                var workspaceBandOverlay = (Grid)grid.FindName("WorkspaceBandDockPreviewOverlay");
                var regionOverlay = (Grid)grid.FindName("RegionDockPreviewOverlay");

                Assert.Multiple(() =>
                {
                    Assert.That(grid.SurfaceHorizontalScrollBarGutterHeight, Is.GreaterThan(0d));
                    Assert.That(bottomDockPreview.Parent, Is.SameAs(workspaceBandOverlay));
                    Assert.That(workspaceBandOverlay.Visibility, Is.EqualTo(Visibility.Visible));
                    Assert.That(regionOverlay.Visibility, Is.EqualTo(Visibility.Collapsed));
                });

                InvokePrivate(grid, "EndRegionDragPreview");
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void ApplyViewState_WhenTopCommandBandIsClosed_RemovesBandWithoutLeavingDeadSpace()
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

                grid.ApplyViewState(CreateRegionViewState(GridRegionKind.TopCommandRegion, GridRegionState.Closed, 52d));
                FlushDispatcher(grid.Dispatcher);

                var host = (FrameworkElement)grid.FindName("TopCommandBand");
                var content = (FrameworkElement)grid.FindName("TopCommandStripContentHost");
                var row = (RowDefinition)grid.FindName("TopCommandStripRow");

                Assert.Multiple(() =>
                {
                    Assert.That(host.Visibility, Is.EqualTo(Visibility.Collapsed));
                    Assert.That(content.Visibility, Is.EqualTo(Visibility.Collapsed));
                    Assert.That(row.ActualHeight, Is.EqualTo(0d).Within(0.1d));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void ApplyViewState_WhenGroupingHasNoGroups_KeepsWorkspaceBandWithoutExpander()
        {
            var grid = CreateGrid();
            var window = CreateWindow(grid);

            try
            {
                window.Show();
                FlushDispatcher(grid.Dispatcher);

                var state = CreateRegionViewState(GridRegionKind.GroupingRegion, GridRegionState.Open, 56d);

                grid.ApplyViewState(state);
                FlushDispatcher(grid.Dispatcher);

                var row = (RowDefinition)grid.FindName("GroupingRegionRow");
                var groupingHost = (PhialeWorkspaceBand)grid.FindName("GroupingRegionHost");
                var groupingBand = (FrameworkElement)grid.FindName("GroupingBandContentHost");
                var close = (FrameworkElement)groupingHost.Template.FindName("WorkspaceBandCloseButton", groupingHost);

                Assert.Multiple(() =>
                {
                    Assert.That(row, Is.Not.Null);
                    Assert.That(row.Height.IsAuto, Is.True);
                    Assert.That(groupingHost.Height, Is.EqualTo(56d).Within(0.1d));
                    Assert.That(row.ActualHeight, Is.GreaterThanOrEqualTo(56d));
                    Assert.That(row.ActualHeight, Is.LessThanOrEqualTo(56d));
                    Assert.That(groupingBand, Is.Not.Null);
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

                var groupingHost = (PhialeWorkspaceBand)grid.FindName("GroupingRegionHost");
                var summaryHost = (PhialeWorkspaceBand)grid.FindName("SummaryBottomRegionHost");
                var statusHost = (FrameworkElement)grid.FindName("BottomStatusStripHost");

                Assert.Multiple(() =>
                {
                    Assert.That(groupingHost.BandBorderThickness.Bottom, Is.EqualTo(1d));
                    Assert.That(groupingHost.BandBorderThickness.Left, Is.EqualTo(0d));
                    Assert.That(groupingHost.BandCornerRadius.TopLeft, Is.EqualTo(0d));
                    Assert.That(summaryHost.BandBorderThickness.Top, Is.EqualTo(1d));
                    Assert.That(summaryHost.BandBorderThickness.Left, Is.EqualTo(0d));
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
        public void TopCommandBandCloseButton_FlowsThroughCoreRegionState_AndClosesTheBand()
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

                grid.ApplyViewState(CreateRegionViewState(GridRegionKind.TopCommandRegion, GridRegionState.Open, 52d));
                FlushDispatcher(grid.Dispatcher);

                var topCommandBand = (PhialeWorkspaceBand)grid.FindName("TopCommandBand");
                var closeButton = (Button)topCommandBand.Template.FindName("WorkspaceBandCloseButton", topCommandBand);
                closeButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                FlushDispatcher(grid.Dispatcher);

                var exported = grid.ExportViewState();
                var topState = exported.RegionLayout.Single(region => region.RegionKind == GridRegionKind.TopCommandRegion);

                Assert.That(topState.State, Is.EqualTo(GridRegionState.Closed));
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

        private static void AssertTabWidthMatchesTextPlusOneAOnEachSide(Button tab, string text)
        {
            var textWidth = MeasureTextWidth(tab, text);
            var letterWidth = MeasureTextWidth(tab, "a");
            var expectedWidth = textWidth + (2d * (letterWidth + 4d));

            Assert.That(tab.ActualWidth, Is.EqualTo(expectedWidth).Within(2.0d), tab.Name);
        }

        private static void AssertRailTabHeightMatchesTextPlusOneAOnEachSide(Button tab, string text)
        {
            var textWidth = MeasureTextWidth(tab, text);
            var letterWidth = MeasureTextWidth(tab, "a");
            var expectedHeight = textWidth + (2d * (letterWidth + 4d));

            Assert.That(tab.ActualHeight, Is.EqualTo(expectedHeight).Within(2.0d), tab.Name);
        }

        private static double MeasureTextWidth(Control control, string text)
        {
            var typeface = new Typeface(control.FontFamily, control.FontStyle, control.FontWeight, control.FontStretch);
            var pixelsPerDip = VisualTreeHelper.GetDpi(control).PixelsPerDip;
            var formattedText = new FormattedText(
                text,
                CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                typeface,
                control.FontSize,
                Brushes.Black,
                pixelsPerDip);

            return formattedText.WidthIncludingTrailingWhitespace;
        }

        private static bool IsVisualAncestor(DependencyObject ancestor, DependencyObject descendant)
        {
            for (var current = descendant; current != null; current = VisualTreeHelper.GetParent(current))
            {
                if (ReferenceEquals(current, ancestor))
                {
                    return true;
                }
            }

            return false;
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
            .GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic)
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
            viewState.RegionLayout.Add(new GridViewRegionState { RegionKind = GridRegionKind.TopCommandRegion, State = GridRegionState.Open, Size = 52d, IsActive = false });
            viewState.RegionLayout.Add(new GridViewRegionState { RegionKind = GridRegionKind.GroupingRegion, State = GridRegionState.Open, Size = 56d, IsActive = false });
            viewState.RegionLayout.Add(new GridViewRegionState { RegionKind = GridRegionKind.SummaryBottomRegion, State = GridRegionState.Open, Size = 56d, IsActive = false });
            viewState.RegionLayout.Add(new GridViewRegionState { RegionKind = GridRegionKind.SummaryDesignerRegion, State = GridRegionState.Closed, Size = 320d, IsActive = false });
            viewState.RegionLayout.Add(new GridViewRegionState { RegionKind = GridRegionKind.SideToolRegion, State = GridRegionState.Closed, Size = 320d, IsActive = false });
            viewState.RegionLayout.Add(new GridViewRegionState { RegionKind = GridRegionKind.ChangePanelRegion, State = GridRegionState.Closed, Size = 320d, IsActive = false });
            viewState.RegionLayout.Add(new GridViewRegionState { RegionKind = GridRegionKind.ValidationPanelRegion, State = GridRegionState.Closed, Size = 320d, IsActive = false });

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

