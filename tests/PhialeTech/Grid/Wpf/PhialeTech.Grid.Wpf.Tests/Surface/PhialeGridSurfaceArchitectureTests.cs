using System.IO;
using NUnit.Framework;

namespace PhialeGrid.Wpf.Tests.Surface
{
    [TestFixture]
    public sealed class PhialeGridSurfaceArchitectureTests
    {
        [Test]
        public void PhialeGridXaml_DoesNotContainLegacyRowsDataGridOrMasterDetailDataGrid()
        {
            var xaml = ReadRepositoryFile(
                "src",
                "PhialeTech",
                "Products",
                "Grid",
                "Platforms",
                "Wpf",
                "PhialeGrid.Wpf",
                "PhialeGrid.xaml");

            Assert.Multiple(() =>
            {
                Assert.That(xaml, Does.Not.Contain("RowsDataGrid"));
                Assert.That(xaml, Does.Not.Contain("MasterDetailRowsDataGrid"));
                Assert.That(xaml, Does.Not.Contain("<DataGrid"));
                Assert.That(xaml, Does.Not.Contain("DataGrid.RowDetailsTemplate"));
                Assert.That(xaml, Does.Not.Contain("Margin=\"0,30,0,0\""));
                Assert.That(xaml, Does.Not.Contain("x:Name=\"FilterOverlayHost\""));
                Assert.That(xaml, Does.Not.Contain("x:Name=\"GridOptionsButton\""));
                Assert.That(xaml, Does.Not.Contain("x:Name=\"SurfaceVerticalScrollBarTopSpacer\""));
                Assert.That(xaml, Does.Not.Contain("TranslateTransform Y="));
            });
        }

        [Test]
        public void PhialeGridCodeBehind_DoesNotContainLegacyRendererSwitchesOrRowsDataGridReferences()
        {
            var codeBehind = ReadRepositoryFile(
                "src",
                "PhialeTech",
                "Products",
                "Grid",
                "Platforms",
                "Wpf",
                "PhialeGrid.Wpf",
                "PhialeGrid.cs");

            Assert.Multiple(() =>
            {
                Assert.That(codeBehind, Does.Not.Contain("RowsDataGrid"));
                Assert.That(codeBehind, Does.Not.Contain("UseSurfaceRenderer"));
                Assert.That(codeBehind, Does.Not.Contain("UseExperimentalSurfaceRenderer"));
                Assert.That(codeBehind, Does.Not.Contain("IsSurfaceRendererActive"));
                Assert.That(codeBehind, Does.Not.Contain("UpdateSurfaceRendererMode"));
                Assert.That(codeBehind, Does.Not.Contain("CanUseSurfaceRenderer"));
                Assert.That(codeBehind, Does.Not.Contain("DataGridSortingEventArgs"));
                Assert.That(codeBehind, Does.Not.Contain("DataGridCellInfo"));
            });
        }

        [Test]
        public void WpfTests_DoNotReferenceLegacyRowsDataGridOrRendererSwitches()
        {
            var testsRoot = Path.Combine(
                GridTestRepositoryPaths.RepositoryRoot,
                "tests",
                "PhialeTech",
                "Grid",
                "Wpf",
                "PhialeTech.Grid.Wpf.Tests");

            foreach (var file in Directory.EnumerateFiles(testsRoot, "*.cs", SearchOption.AllDirectories))
            {
                if (string.Equals(
                    Path.GetFileName(file),
                    nameof(PhialeGridSurfaceArchitectureTests) + ".cs",
                    System.StringComparison.Ordinal))
                {
                    continue;
                }

                var contents = File.ReadAllText(file);
                Assert.Multiple(() =>
                {
                    Assert.That(contents, Does.Not.Contain("RowsDataGrid"), $"Legacy RowsDataGrid reference found in {file}.");
                    Assert.That(contents, Does.Not.Contain("UseSurfaceRenderer"), $"Legacy renderer switch reference found in {file}.");
                    Assert.That(contents, Does.Not.Contain("UseExperimentalSurfaceRenderer"), $"Experimental renderer switch reference found in {file}.");
                    Assert.That(contents, Does.Not.Contain("IsSurfaceRendererActive"), $"Legacy renderer activity flag reference found in {file}.");
                });
            }
        }

        [Test]
        public void GridSurfaceHost_DoesNotContainSyntheticPointerRuntimeHook()
        {
            var hostCode = ReadRepositoryFile(
                "src",
                "PhialeTech",
                "Products",
                "Grid",
                "Platforms",
                "Wpf",
                "PhialeGrid.Wpf",
                "Surface",
                "GridSurfaceHost.cs");
            var syntheticInputPath = Path.Combine(
                GridTestRepositoryPaths.RepositoryRoot,
                "src",
                "PhialeTech",
                "Products",
                "Grid",
                "Platforms",
                "Wpf",
                "PhialeGrid.Wpf",
                "Surface",
                "GridSurfaceSyntheticInput.cs");

            Assert.Multiple(() =>
            {
                Assert.That(File.Exists(syntheticInputPath), Is.False, "Synthetic runtime pointer hook file should not exist.");
                Assert.That(hostCode, Does.Not.Contain("GridSurfaceSyntheticInput"));
                Assert.That(hostCode, Does.Not.Contain("_isSyntheticMouseDragActive"));
                Assert.That(hostCode, Does.Not.Contain("TryResolveSyntheticMousePosition"));
            });
        }

        [Test]
        public void GridSurfaceHost_MapsEditorValueChanges_ThroughUniversalContracts()
        {
            var hostCode = ReadRepositoryFile(
                "src",
                "PhialeTech",
                "Products",
                "Grid",
                "Platforms",
                "Wpf",
                "PhialeGrid.Wpf",
                "Surface",
                "GridSurfaceHost.cs");

            Assert.Multiple(() =>
            {
                Assert.That(hostCode, Does.Contain("CreateEditorValueChangedEventArgs("));
                Assert.That(hostCode, Does.Contain("CreateEditorValueInput("));
                Assert.That(hostCode, Does.Not.Contain("new GridEditorValueInput("));
            });
        }

        [Test]
        public void PhialeGrid_MapsEditCommands_ThroughUniversalContracts()
        {
            var gridCode = ReadRepositoryFile(
                "src",
                "PhialeTech",
                "Products",
                "Grid",
                "Platforms",
                "Wpf",
                "PhialeGrid.Wpf",
                "PhialeGrid.cs");

            Assert.Multiple(() =>
            {
                Assert.That(gridCode, Does.Contain("CreateCommandEventArgs("));
                Assert.That(gridCode, Does.Contain("CreateEditCommandInput("));
                Assert.That(gridCode, Does.Not.Contain("new GridEditCommandInput("));
            });
        }

        [Test]
        public void PhialeGrid_MapsRegionCommands_ThroughUniversalContracts()
        {
            var gridCode = ReadRepositoryFile(
                "src",
                "PhialeTech",
                "Products",
                "Grid",
                "Platforms",
                "Wpf",
                "PhialeGrid.Wpf",
                "PhialeGrid.cs");

            Assert.Multiple(() =>
            {
                Assert.That(gridCode, Does.Contain("CreateCommandEventArgs(commandId"));
                Assert.That(gridCode, Does.Contain("CreateRegionCommandInput("));
                Assert.That(gridCode, Does.Contain("_surfaceCoordinator.ProcessInput(input);"));
                Assert.That(gridCode, Does.Not.Contain("_regionLayoutController"));
                Assert.That(gridCode, Does.Not.Contain("ApplyHorizontalRegion("));
                Assert.That(gridCode, Does.Not.Contain("ApplySideRegion("));
                Assert.That(gridCode, Does.Not.Contain("HasRenderableRegionContent("));
                Assert.That(gridCode, Does.Not.Contain("HandleTopCommandRegionToggleClick("));
                Assert.That(gridCode, Does.Not.Contain("HandleTopCommandRegionCloseClick("));
                Assert.That(gridCode, Does.Not.Contain("HandleTopCommandRegionSplitterDragCompleted("));
                Assert.That(gridCode, Does.Not.Contain("HandleGroupingRegionToggleClick("));
                Assert.That(gridCode, Does.Not.Contain("HandleGroupingRegionCloseClick("));
                Assert.That(gridCode, Does.Not.Contain("HandleSummaryBottomRegionToggleClick("));
                Assert.That(gridCode, Does.Not.Contain("HandleSummaryBottomRegionCloseClick("));
                Assert.That(gridCode, Does.Not.Contain("HandleSideToolRegionToggleClick("));
                Assert.That(gridCode, Does.Not.Contain("HandleSideToolRegionCloseClick("));
                Assert.That(gridCode, Does.Not.Contain("HandleGroupingRegionSplitterDragCompleted("));
                Assert.That(gridCode, Does.Not.Contain("HandleSummaryBottomRegionSplitterDragCompleted("));
                Assert.That(gridCode, Does.Not.Contain("HandleSideToolRegionSplitterDragCompleted("));
                Assert.That(gridCode, Does.Not.Contain("TopCommandRegionToggleText"));
                Assert.That(gridCode, Does.Not.Contain("CanCollapseTopCommandRegion"));
                Assert.That(gridCode, Does.Not.Contain("CanCloseTopCommandRegion"));
                Assert.That(gridCode, Does.Not.Contain("CommandsRegionTitleText"));
                Assert.That(gridCode, Does.Not.Contain("Margin = new Thickness(0d, 30d"));
            });
        }

        [Test]
        public void WpfRegionLayer_UsesDedicatedAdapterAndPresenterClasses()
        {
            var gridCode = ReadRepositoryFile(
                "src",
                "PhialeTech",
                "Products",
                "Grid",
                "Platforms",
                "Wpf",
                "PhialeGrid.Wpf",
                "PhialeGrid.cs");
            var adapterCode = ReadRepositoryFile(
                "src",
                "PhialeTech",
                "Products",
                "Grid",
                "Platforms",
                "Wpf",
                "PhialeGrid.Wpf",
                "Regions",
                "WpfGridRegionLayoutAdapter.cs");
            var stripPresenterCode = ReadRepositoryFile(
                "src",
                "PhialeTech",
                "Products",
                "Grid",
                "Platforms",
                "Wpf",
                "PhialeGrid.Wpf",
                "Regions",
                "WpfGridStripRegionPresenter.cs");
            var panePresenterCode = ReadRepositoryFile(
                "src",
                "PhialeTech",
                "Products",
                "Grid",
                "Platforms",
                "Wpf",
                "PhialeGrid.Wpf",
                "Regions",
                "WpfGridPaneRegionPresenter.cs");
            var surfacePanelCode = ReadRepositoryFile(
                "src",
                "PhialeTech",
                "Products",
                "Grid",
                "Platforms",
                "Wpf",
                "PhialeGrid.Wpf",
                "Surface",
                "GridSurfacePanel.cs");
            var surfaceHeaderBandCode = ReadRepositoryFile(
                "src",
                "PhialeTech",
                "Products",
                "Grid",
                "Platforms",
                "Wpf",
                "PhialeGrid.Wpf",
                "Surface",
                "GridSurfaceColumnHeaderBand.cs");

            Assert.Multiple(() =>
            {
                Assert.That(gridCode, Does.Contain("WpfGridRegionLayoutAdapter"));
                Assert.That(adapterCode, Does.Contain("WpfGridStripRegionPresenter"));
                Assert.That(adapterCode, Does.Contain("WpfGridPaneRegionPresenter"));
                Assert.That(surfaceHeaderBandCode, Does.Contain("GridSurfaceColumnHeaderBand"));
                Assert.That(stripPresenterCode, Does.Contain("GridRegionViewState"));
                Assert.That(panePresenterCode, Does.Contain("GridRegionViewState"));
                Assert.That(panePresenterCode, Does.Not.Contain("GridRegionLayoutManager"));
                Assert.That(surfacePanelCode, Does.Not.Contain("ColumnHeaderContainerType, _headerLayer"));
            });
        }

        [Test]
        public void PhialeGrid_UsesNeutralRegionInputsForResizeAndVisibilityChanges()
        {
            var gridCode = ReadRepositoryFile(
                "src",
                "PhialeTech",
                "Products",
                "Grid",
                "Platforms",
                "Wpf",
                "PhialeGrid.Wpf",
                "PhialeGrid.cs");

            Assert.Multiple(() =>
            {
                Assert.That(gridCode, Does.Contain("CreateRegionResizeInput("));
                Assert.That(gridCode, Does.Contain("CreateRegionStateInput("));
                Assert.That(gridCode, Does.Contain("ResolveRegionKindFromTag("));
                Assert.That(gridCode, Does.Contain("HandleRegionSplitterDragCompleted("));
                Assert.That(gridCode, Does.Not.Contain("_surfaceCoordinator.ResizeRegion("));
                Assert.That(gridCode, Does.Not.Contain("_surfaceCoordinator.OpenRegion("));
                Assert.That(gridCode, Does.Not.Contain("_surfaceCoordinator.CloseRegion("));
                Assert.That(gridCode, Does.Not.Contain("_surfaceCoordinator.ResolveRegions("));
                Assert.That(gridCode, Does.Not.Contain("_surfaceCoordinator.ResolveRegion("));
            });
        }

        [Test]
        public void GridSurfaceHost_MapsViewportSignals_ThroughUniversalContracts()
        {
            var hostCode = ReadRepositoryFile(
                "src",
                "PhialeTech",
                "Products",
                "Grid",
                "Platforms",
                "Wpf",
                "PhialeGrid.Wpf",
                "Surface",
                "GridSurfaceHost.cs");

            Assert.Multiple(() =>
            {
                Assert.That(hostCode, Does.Contain("CreateScrollChangedEventArgs("));
                Assert.That(hostCode, Does.Contain("CreateViewportChangedEventArgs("));
                Assert.That(hostCode, Does.Contain("CreateScrollChangedInput("));
                Assert.That(hostCode, Does.Contain("CreateViewportChangedInput("));
                Assert.That(hostCode, Does.Not.Contain("_coordinator.SetScrollPosition("));
                Assert.That(hostCode, Does.Not.Contain("_coordinator.SetViewportSize("));
                Assert.That(hostCode, Does.Contain("ScrollViewer.SizeChanged += OnScrollViewerSizeChanged;"));
            });
        }

        [Test]
        public void PhialeGrid_MapsFilterScroll_ThroughUniversalHostSignals()
        {
            var gridCode = ReadRepositoryFile(
                "src",
                "PhialeTech",
                "Products",
                "Grid",
                "Platforms",
                "Wpf",
                "PhialeGrid.Wpf",
                "PhialeGrid.cs");

            Assert.Multiple(() =>
            {
                Assert.That(gridCode, Does.Contain("CreateScrollChangedEventArgs("));
                Assert.That(gridCode, Does.Contain("CreateScrollChangedInput("));
                Assert.That(gridCode, Does.Not.Contain("_surfaceCoordinator.SetScrollPosition("));
            });
        }

        [Test]
        public void SurfaceHeaderPresenter_UsesStyleLibraryTemplateThatRendersBackgroundAndBorder()
        {
            var presenterCode = ReadRepositoryFile(
                "src",
                "PhialeTech",
                "Products",
                "Grid",
                "Platforms",
                "Wpf",
                "PhialeGrid.Wpf",
                "Surface",
                "Presenters",
                "GridColumnHeaderPresenter.cs");
            var styleLibrary = ReadRepositoryFile(
                "src",
                "PhialeTech",
                "Shared",
                "PhialeTech.Styles.Wpf",
                "Themes",
                "PhialeGrid.Controls.xaml");

            Assert.Multiple(() =>
            {
                Assert.That(presenterCode, Does.Contain("PgGridSurfaceHeaderPresenterStyle"));
                Assert.That(styleLibrary, Does.Contain("x:Key=\"PgGridSurfaceHeaderPresenterStyle\""));
                Assert.That(styleLibrary, Does.Contain("Background=\"{TemplateBinding Background}\""));
                Assert.That(styleLibrary, Does.Contain("BorderBrush=\"{TemplateBinding BorderBrush}\""));
                Assert.That(styleLibrary, Does.Contain("BorderThickness=\"{TemplateBinding BorderThickness}\""));
            });
        }

        [Test]
        public void ScrollBarStyle_UsesOrientationSpecificDirectionAndPagingCommands()
        {
            var styleLibrary = ReadRepositoryFile(
                "src",
                "PhialeTech",
                "Shared",
                "PhialeTech.Styles.Wpf",
                "Themes",
                "PhialeGrid.Controls.xaml");

            Assert.Multiple(() =>
            {
                Assert.That(styleLibrary, Does.Contain("IsDirectionReversed=\"True\""));
                Assert.That(styleLibrary, Does.Contain("TargetName=\"PART_Track\" Property=\"IsDirectionReversed\" Value=\"False\""));
                Assert.That(styleLibrary, Does.Contain("Command=\"ScrollBar.PageUpCommand\""));
                Assert.That(styleLibrary, Does.Contain("Command=\"ScrollBar.PageDownCommand\""));
                Assert.That(styleLibrary, Does.Contain("TargetName=\"PART_DecreaseButton\" Property=\"Command\" Value=\"ScrollBar.PageLeftCommand\""));
                Assert.That(styleLibrary, Does.Contain("TargetName=\"PART_IncreaseButton\" Property=\"Command\" Value=\"ScrollBar.PageRightCommand\""));
            });
        }

        [Test]
        public void LiveUiHelpers_DoNotBypassMoveOrReleaseWithTestingMethods()
        {
            var liveHelperCode = ReadRepositoryFile(
                "tests",
                "PhialeTech",
                "Grid",
                "Wpf",
                "PhialeTech.Grid.Wpf.Tests",
                "Surface",
                "GridSurfaceTestHost.cs");
            var rowHeaderSelectionCode = ReadRepositoryFile(
                "tests",
                "PhialeTech",
                "Grid",
                "Wpf",
                "PhialeTech.Grid.Wpf.Tests",
                "Selection",
                "GridRowHeaderSelectionBehaviorTests.cs");

            Assert.Multiple(() =>
            {
                Assert.That(liveHelperCode, Does.Not.Contain("HandlePointerPressedForTesting("));
                Assert.That(liveHelperCode, Does.Not.Contain("HandlePointerMovedForTesting("));
                Assert.That(liveHelperCode, Does.Not.Contain("HandlePointerReleasedForTesting("));
                Assert.That(liveHelperCode, Does.Not.Contain("GridSurfaceSyntheticInput."));
                Assert.That(rowHeaderSelectionCode, Does.Not.Contain("HandlePointerPressedForTesting("));
                Assert.That(rowHeaderSelectionCode, Does.Not.Contain("HandlePointerReleasedForTesting("));
            });
        }

        private static string ReadRepositoryFile(params string[] segments)
        {
            return File.ReadAllText(Path.Combine(GridTestRepositoryPaths.RepositoryRoot, Path.Combine(segments)));
        }
    }
}
