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
                "Shared",
                "PhialeTech.Styles.Wpf",
                "Themes.Linked",
                "Grid",
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
            var bandPresenterCode = ReadRepositoryFile(
                "src",
                "PhialeTech",
                "Products",
                "Grid",
                "Platforms",
                "Wpf",
                "PhialeGrid.Wpf",
                "Regions",
                "WpfGridWorkspaceBandPresenter.cs");
            var workspacePanelPresenterCode = ReadRepositoryFile(
                "src",
                "PhialeTech",
                "Products",
                "Grid",
                "Platforms",
                "Wpf",
                "PhialeGrid.Wpf",
                "Regions",
                "WpfGridWorkspacePanelPresenter.cs");
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
                Assert.That(adapterCode, Does.Contain("WpfGridWorkspaceBandPresenter"));
                Assert.That(adapterCode, Does.Contain("WpfGridWorkspacePanelPresenter"));
                Assert.That(surfaceHeaderBandCode, Does.Contain("GridSurfaceColumnHeaderBand"));
                Assert.That(bandPresenterCode, Does.Contain("GridRegionViewState"));
                Assert.That(workspacePanelPresenterCode, Does.Contain("GridRegionViewState"));
                Assert.That(workspacePanelPresenterCode, Does.Not.Contain("GridRegionLayoutManager"));
                Assert.That(surfacePanelCode, Does.Not.Contain("ColumnHeaderContainerType, _headerLayer"));
            });
        }

        [Test]
        public void PhialeGridRegionFrame_UsesDedicatedThemeTokenForRegionSurfaceBackground()
        {
            var gridXaml = ReadRepositoryFile(
                "src",
                "PhialeTech",
                "Shared",
                "PhialeTech.Styles.Wpf",
                "Themes.Linked",
                "Grid",
                "PhialeGrid.xaml");
            var sharedStyles = ReadRepositoryFile(
                "src",
                "PhialeTech",
                "Shared",
                "PhialeTech.Styles.Wpf",
                "Themes",
                "PhialeGrid.Shared.xaml");
            var dayTokens = ReadRepositoryFile(
                "src",
                "PhialeTech",
                "Shared",
                "PhialeTech.Styles.Wpf",
                "Themes",
                "ThemeTokens.Day.xaml");
            var nightTokens = ReadRepositoryFile(
                "src",
                "PhialeTech",
                "Shared",
                "PhialeTech.Styles.Wpf",
                "Themes",
                "ThemeTokens.Night.xaml");
            var highContrastTokens = ReadRepositoryFile(
                "src",
                "PhialeTech",
                "Shared",
                "PhialeTech.Styles.Wpf",
                "Themes",
                "ThemeTokens.HighContrast.xaml");

            Assert.Multiple(() =>
            {
                Assert.That(gridXaml, Does.Contain("x:Name=\"GridRootFrame\""));
                Assert.That(gridXaml, Does.Contain("x:Name=\"GridOuterFrame\""));
                Assert.That(gridXaml, Does.Contain("x:Name=\"RegionLayoutFrame\""));
                Assert.That(gridXaml, Does.Contain("x:Key=\"PgGridRegionSurfaceGridStyle\""));
                Assert.That(gridXaml, Does.Contain("x:Key=\"PgGridOuterFrameBorderStyle\""));
                Assert.That(gridXaml, Does.Contain("x:Key=\"PgGridRegionSurfaceBorderStyle\""));
                Assert.That(gridXaml, Does.Contain("x:Key=\"PgGridRegionHeaderChromeBorderStyle\""));
                Assert.That(gridXaml, Does.Contain("x:Key=\"PgGridRegionDockPreviewZoneStyle\""));
                Assert.That(gridXaml, Does.Contain("x:Name=\"RegionDockPreviewOverlay\""));
                Assert.That(gridXaml, Does.Contain("x:Name=\"RegionDockPreviewLeft\""));
                Assert.That(gridXaml, Does.Contain("x:Name=\"RegionDockPreviewRight\""));
                Assert.That(gridXaml, Does.Contain("RoundedChildClipBehavior.ClipChildToBorder"));
                Assert.That(gridXaml, Does.Contain("Value=\"{DynamicResource PgGridRegionSurfaceBackgroundBrush}\""));
                Assert.That(gridXaml, Does.Contain("Value=\"{DynamicResource CornerRadius.Grid.RegionSurface}\""));
                Assert.That(gridXaml, Does.Contain("Value=\"{DynamicResource CornerRadius.Grid.RegionHeader}\""));
                Assert.That(gridXaml, Does.Contain("Style=\"{StaticResource PgGridRegionSurfaceGridStyle}\""));
                Assert.That(gridXaml, Does.Contain("Style=\"{StaticResource PgGridRegionSurfaceBorderStyle}\""));
                Assert.That(gridXaml, Does.Contain("<Grid Background=\"{TemplateBinding Background}\">"));
                Assert.That(sharedStyles, Does.Contain("PgGridRegionSurfaceBackgroundBrush"));
                Assert.That(sharedStyles, Does.Contain("Color.Grid.RegionSurface.Background"));
                Assert.That(dayTokens, Does.Contain("Color.Grid.RegionSurface.Background"));
                Assert.That(dayTokens, Does.Contain("CornerRadius.Grid.RegionSurface"));
                Assert.That(dayTokens, Does.Contain("CornerRadius.Grid.RegionHeader"));
                Assert.That(nightTokens, Does.Contain("Color.Grid.RegionSurface.Background"));
                Assert.That(nightTokens, Does.Contain("CornerRadius.Grid.RegionSurface"));
                Assert.That(nightTokens, Does.Contain("CornerRadius.Grid.RegionHeader"));
                Assert.That(highContrastTokens, Does.Contain("Color.Grid.RegionSurface.Background"));
                Assert.That(highContrastTokens, Does.Contain("CornerRadius.Grid.RegionSurface"));
                Assert.That(highContrastTokens, Does.Contain("CornerRadius.Grid.RegionHeader"));
            });
        }

        [Test]
        public void HeaderBandPointerBridge_BelongsToDedicatedBandComponent_NotPhialeGridShell()
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
                Assert.That(gridCode, Does.Not.Contain("SurfaceColumnHeaderBand.PreviewMouseLeftButtonDown +="));
                Assert.That(gridCode, Does.Not.Contain("SurfaceColumnHeaderBand.PreviewMouseMove +="));
                Assert.That(gridCode, Does.Not.Contain("SurfaceColumnHeaderBand.PreviewMouseLeftButtonUp +="));
                Assert.That(gridCode, Does.Not.Contain("HandleSurfaceColumnHeaderBandPreviewMouseLeftButtonDown("));
                Assert.That(gridCode, Does.Not.Contain("HandleSurfaceColumnHeaderBandPreviewMouseMove("));
                Assert.That(gridCode, Does.Not.Contain("HandleSurfaceColumnHeaderBandPreviewMouseLeftButtonUp("));
                Assert.That(gridCode, Does.Not.Contain("HandleSurfaceColumnHeaderBandMouseLeave("));
                Assert.That(gridCode, Does.Not.Contain("HandlePointerPressedForTesting("));
                Assert.That(surfaceHeaderBandCode, Does.Contain("PreviewMouseLeftButtonDown"));
                Assert.That(surfaceHeaderBandCode, Does.Contain("PreviewMouseMove"));
                Assert.That(surfaceHeaderBandCode, Does.Contain("PreviewMouseLeftButtonUp"));
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

        [Test]
        public void SurfaceColumnHeaderBand_UpdatesCursorUsingColumnHeaderSurface()
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
                Assert.That(hostCode, Does.Contain("HandleExternalPointerMoved"));
                Assert.That(hostCode, Does.Contain("args.Pointer.Position.X"));
                Assert.That(hostCode, Does.Contain("GridHitTestSurfaceScope.ColumnHeaderSurface"));
                Assert.That(hostCode, Does.Contain("UpdatePointerCursor(position, GridHitTestSurfaceScope.DataSurface)"));
            });
        }

        [Test]
        public void SurfaceColumnHeaderBand_OwnsMouseCaptureForExternalHeaderInteractions()
        {
            var headerBandCode = ReadRepositoryFile(
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
                Assert.That(headerBandCode, Does.Contain("CaptureMouse();"));
                Assert.That(headerBandCode, Does.Not.Contain("BeginExternalMousePointerCapture"));
                Assert.That(headerBandCode, Does.Contain("HandleExternalPointerPressed(input)"));
                Assert.That(headerBandCode, Does.Contain("HandleExternalPointerMoved(input)"));
                Assert.That(headerBandCode, Does.Contain("HandleExternalPointerReleased(input)"));
            });
        }

        private static string ReadRepositoryFile(params string[] segments)
        {
            return File.ReadAllText(Path.Combine(GridTestRepositoryPaths.RepositoryRoot, Path.Combine(segments)));
        }
    }
}
