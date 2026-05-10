using System.IO;
using NUnit.Framework;
using PhialeGis.Library.Tests.Support;

namespace PhialeGis.Library.Tests.Grid
{
    public class GridWpfHostXamlTests
    {
        [Test]
        public void WpfHostXaml_UsesSurfaceViewportAndScrollSync()
        {
            var xaml = File.ReadAllText(GetXamlPath());

            Assert.That(xaml, Does.Contain("x:Name=\"SurfaceHost\""));
            Assert.That(xaml, Does.Contain("x:Name=\"FilterScrollViewer\""));
            Assert.That(xaml, Does.Contain("x:Name=\"FilterRowHeaderSpacer\""));
            Assert.That(xaml, Does.Contain("Path=ResolvedRowHeaderWidth"));
            Assert.That(xaml, Does.Not.Contain("Margin=\"0,30,0,0\""));
            Assert.That(xaml, Does.Not.Contain("x:Name=\"FilterOverlayHost\""));
            Assert.That(xaml, Does.Not.Contain("x:Name=\"GridOptionsButton\""));
            Assert.That(xaml, Does.Not.Contain("RowsDataGrid"));
            Assert.That(xaml, Does.Not.Contain("DataGrid.RowDetailsTemplate"));
        }

        [Test]
        public void WpfHostXaml_UsesIntegratedTopChromeRows_InsteadOfOverlayFilterComposition()
        {
            var xaml = File.ReadAllText(GetXamlPath());

            Assert.Multiple(() =>
            {
                Assert.That(xaml, Does.Contain("x:Name=\"SurfaceTopChromeHost\""));
                Assert.That(xaml, Does.Contain("x:Name=\"SurfaceHeaderBandHost\""));
                Assert.That(xaml, Does.Contain("x:Name=\"SurfaceColumnHeaderBand\""));
                Assert.That(xaml, Does.Contain("x:Name=\"SurfaceHeaderCornerHost\""));
                Assert.That(xaml, Does.Contain("x:Name=\"SurfaceHeaderRightInset\""));
                Assert.That(xaml, Does.Contain("x:Name=\"SurfaceFilterRowHost\""));
                Assert.That(xaml, Does.Contain("x:Name=\"SurfaceFilterRightInset\""));
                Assert.That(xaml, Does.Contain("x:Name=\"SurfaceTopViewportHost\""));
                Assert.That(xaml, Does.Contain("Path=SurfaceColumnHeaderHeight"));
                Assert.That(xaml, Does.Contain("Path=SurfaceFilterRowHeight"));
                Assert.That(xaml, Does.Contain("Path=SurfaceVerticalScrollBarGutterWidth"));
                Assert.That(xaml, Does.Contain("x:Name=\"SurfaceHost\""));
                Assert.That(xaml, Does.Contain("Grid.Row=\"2\""));
                Assert.That(xaml, Does.Not.Contain("TranslateTransform Y="));
                Assert.That(xaml, Does.Not.Contain("x:Name=\"SurfaceVerticalScrollBarTopSpacer\""));
            });
        }

        [Test]
        public void WpfHostXaml_UsesIntegratedHeaderBandAndRealFilterRow_InsteadOfOverlayOptionsCorner()
        {
            var xaml = File.ReadAllText(GetXamlPath());

            Assert.Multiple(() =>
            {
                Assert.That(xaml, Does.Contain("x:Name=\"SurfaceHeaderBandRow\""));
                Assert.That(xaml, Does.Contain("x:Name=\"SurfaceFilterRow\""));
                Assert.That(xaml, Does.Contain("x:Name=\"SurfaceHeaderCornerButton\""));
                Assert.That(xaml, Does.Contain("Grid.Row=\"0\""));
                Assert.That(xaml, Does.Contain("Grid.Row=\"1\""));
                Assert.That(xaml, Does.Not.Contain("x:Name=\"GridOptionsButton\""));
                Assert.That(xaml, Does.Not.Contain("Panel.ZIndex=\"5\""));
                Assert.That(xaml, Does.Not.Contain("Panel.ZIndex=\"4\""));
            });
        }

        [Test]
        public void WpfHostXaml_DefaultsGroupsToCollapsedAndExposesGroupToggleActions()
        {
            var xaml = File.ReadAllText(GetXamlPath());
            var groupingBandXaml = File.ReadAllText(GetGroupingBandXamlPath());

            Assert.That(xaml, Does.Contain("HandleExpandAllGroupsClick"));
            Assert.That(xaml, Does.Contain("HandleCollapseAllGroupsClick"));
            Assert.That(groupingBandXaml, Does.Contain("Visibility=\"{Binding ElementName=Root, Path=HasGroups"));
            Assert.That(xaml, Does.Contain("x:Name=\"SurfaceHost\""));
            Assert.That(xaml, Does.Not.Contain("<DataGrid.GroupStyle>"));
        }

        [Test]
        public void WpfHostXaml_ExposesSurfaceSummariesAndStatusPanels()
        {
            var xaml = File.ReadAllText(GetXamlPath());

            Assert.Multiple(() =>
            {
                Assert.That(xaml, Does.Contain("Path=SummaryItems"));
                Assert.That(xaml, Does.Contain("Path=SelectionStatusText"));
                Assert.That(xaml, Does.Contain("Path=CurrentCellText"));
                Assert.That(xaml, Does.Contain("Path=EditStatusText"));
                Assert.That(xaml, Does.Contain("Path=PagingStatusText"));
            });
        }

        [Test]
        public void WpfHostXaml_UsesSharedWorkspacePlaygroundForChipAreas()
        {
            var gridXaml = File.ReadAllText(GetXamlPath());
            var groupingBandXaml = File.ReadAllText(GetGroupingBandXamlPath());
            var playgroundCode = File.ReadAllText(GetWorkspacePlaygroundCodePath());
            var sharedStylesXaml = File.ReadAllText(GetSharedGridControlsXamlPath());
            var sharedTokensXaml = File.ReadAllText(GetSharedGridTokensXamlPath());
            var demoXaml = File.ReadAllText(GetDemoMainWindowXamlPath());

            Assert.Multiple(() =>
            {
                Assert.That(playgroundCode, Does.Contain("public sealed partial class PhialeWorkspacePlayground"));
                Assert.That(playgroundCode, Does.Contain("PlaygroundContentProperty"));
                Assert.That(sharedStylesXaml, Does.Contain("PgWorkspacePlaygroundBorderStyle"));
                Assert.That(sharedStylesXaml, Does.Contain("PgWorkspacePlaygroundScrollViewerStyle"));
                Assert.That(sharedStylesXaml, Does.Contain("Margin\" Value=\"{DynamicResource PgWorkspacePlaygroundMargin}\""));
                Assert.That(sharedStylesXaml, Does.Not.Contain("Margin\" Value=\"{DynamicResource PgGridToolsOptionSectionMargin}\""));
                Assert.That(sharedTokensXaml, Does.Contain("<Thickness x:Key=\"PgWorkspacePlaygroundMargin\">0</Thickness>"));
                Assert.That(sharedTokensXaml, Does.Contain("<Thickness x:Key=\"PgWorkspacePlaygroundPadding\">10,6</Thickness>"));
                Assert.That(sharedTokensXaml, Does.Contain("<system:Double x:Key=\"PgWorkspacePlaygroundMinHeight\">38</system:Double>"));
                Assert.That(sharedTokensXaml, Does.Contain("<Thickness x:Key=\"PgSummaryBottomRegionShellPadding\">10,6,10,6</Thickness>"));
                Assert.That(groupingBandXaml, Does.Contain("<controls:PhialeWorkspacePlayground"));
                Assert.That(gridXaml, Does.Contain("<controls:PhialeWorkspacePlayground"));
                Assert.That(demoXaml, Does.Contain("<grid:PhialeWorkspacePlayground"));
                Assert.That(demoXaml, Does.Not.Contain("PgSummaryDesignerPlaygroundBorderStyle"));
                Assert.That(demoXaml, Does.Not.Contain("PgSummaryDesignerPlaygroundScrollViewerStyle"));
            });
        }

        [Test]
        public void WpfHostXaml_DefinesStageOneGridRegionsAndSplitters()
        {
            var xaml = File.ReadAllText(GetXamlPath());

            Assert.Multiple(() =>
            {
                Assert.That(xaml, Does.Contain("x:Name=\"TopCommandBand\""));
                Assert.That(xaml, Does.Contain("x:Name=\"TopCommandStripContentPresenter\""));
                Assert.That(xaml, Does.Not.Contain("x:Name=\"TopCommandRegionHost\""));
                Assert.That(xaml, Does.Contain("x:Name=\"GroupingRegionHost\""));
                Assert.That(xaml, Does.Contain("x:Name=\"SummaryBottomRegionHost\""));
                Assert.That(xaml, Does.Contain("x:Name=\"SideToolRegionHost\""));
                Assert.That(xaml, Does.Not.Contain("x:Name=\"TopCommandRegionSplitter\""));
                Assert.That(xaml, Does.Not.Contain("<GridSplitter x:Name=\"GroupingRegionSplitter\""));
                Assert.That(xaml, Does.Contain("x:Name=\"SummaryBottomRegionSplitter\""));
                Assert.That(xaml, Does.Contain("x:Name=\"SideToolRegionSplitter\""));
                Assert.That(xaml, Does.Contain("TopCommandContent"));
                Assert.That(xaml, Does.Contain("SideToolContent"));
            });
        }

        [Test]
        public void WpfHostXaml_UsesCompactTopCommandStripInsteadOfLegacyTopCommandChrome()
        {
            var xaml = File.ReadAllText(GetXamlPath());

            Assert.Multiple(() =>
            {
                Assert.That(xaml, Does.Contain("x:Name=\"TopCommandBand\""));
                Assert.That(xaml, Does.Contain("IsToggleVisible=\"{Binding ElementName=Root, Path=CanToggleTopCommandStrip}\""));
                Assert.That(xaml, Does.Contain("IsCloseVisible=\"{Binding ElementName=Root, Path=CanCloseTopCommandStrip}\""));
                Assert.That(xaml, Does.Not.Contain("Path=TopCommandRegionToggleText"));
                Assert.That(xaml, Does.Not.Contain("CommandsRegionTitleText"));
                Assert.That(xaml, Does.Not.Contain("TopCommandRegionExpanderButton"));
                Assert.That(xaml, Does.Not.Contain("TopCommandRegionCloseButton"));
                Assert.That(xaml, Does.Not.Contain("TopCommandStripToggleButton"));
                Assert.That(xaml, Does.Not.Contain("TopCommandStripCloseButton"));
                Assert.That(xaml, Does.Not.Contain("Content=\"+\""));
                Assert.That(xaml, Does.Contain("HorizontalScrollBarVisibility=\"Auto\""));
                Assert.That(xaml, Does.Contain("BorderThickness=\"0,0,0,1\""));
            });
        }

        [Test]
        public void WpfHostXaml_UsesWorkspaceBandForGroupingInsteadOfResizableTwoRowPanel()
        {
            var xaml = File.ReadAllText(GetXamlPath());

            Assert.Multiple(() =>
            {
                Assert.That(xaml, Does.Contain("x:Name=\"GroupingRegionHost\""));
                Assert.That(xaml, Does.Contain("x:Name=\"GroupingBandContentHost\""));
                Assert.That(xaml, Does.Contain("Tag=\"GroupingRegion\""));
                Assert.That(xaml, Does.Not.Contain("x:Name=\"GroupingRegionDragGrip\""));
                Assert.That(xaml, Does.Not.Contain("x:Name=\"GroupingRegionSplitter\""));
                Assert.That(xaml, Does.Not.Contain("x:Name=\"GroupingRegionContentScrollViewer\""));
                Assert.That(xaml, Does.Not.Contain("x:Name=\"GroupingPanelHost\""));
            });
        }

        [Test]
        public void WpfHostXaml_WorkspaceBandsUseSharedChromeAndDockPreview()
        {
            var xaml = File.ReadAllText(GetXamlPath());
            var workspaceBandCode = File.ReadAllText(Path.Combine(
                GetRepoRoot(),
                "src",
                "PhialeTech",
                "Products",
                "Grid",
                "Platforms",
                "Wpf",
                "PhialeGrid.Wpf",
                "Controls",
                "PhialeWorkspaceBand.cs"));

            Assert.Multiple(() =>
            {
                Assert.That(workspaceBandCode, Does.Contain("BandPaddingProperty"));
                Assert.That(workspaceBandCode, Does.Contain("IsCloseVisibleProperty"));
                Assert.That(workspaceBandCode, Does.Contain("ToggleTextProperty"));
                Assert.That(xaml, Does.Contain("TargetType=\"{x:Type controls:PhialeWorkspaceBand}\""));
                Assert.That(xaml, Does.Contain("x:Name=\"WorkspaceBandDragGrip\""));
                Assert.That(xaml, Does.Contain("x:Name=\"WorkspaceBandCloseButton\""));
                Assert.That(xaml, Does.Contain("x:Name=\"WorkspaceBandToggleButton\""));
                Assert.That(xaml, Does.Not.Contain("x:Name=\"TopCommandStripDragGrip\""));
                Assert.That(xaml, Does.Not.Contain("x:Name=\"GroupingRegionDragGrip\""));
                Assert.That(xaml, Does.Not.Contain("x:Name=\"SummaryBottomRegionDragGrip\""));
                Assert.That(xaml, Does.Contain("x:Key=\"PgGridWorkspaceBandDragGripStyle\""));
                Assert.That(xaml, Does.Contain("LostMouseCapture=\"HandleRegionDragLostMouseCapture\""));
                Assert.That(xaml, Does.Contain("x:Key=\"PgGridRegionDockPreviewBandZoneStyle\""));
                Assert.That(xaml, Does.Contain("x:Name=\"WorkspaceBandDockPreviewOverlay\""));
                Assert.That(xaml, Does.Contain("x:Name=\"RegionDockPreviewTop\""));
                Assert.That(xaml, Does.Contain("x:Name=\"RegionDockPreviewBottom\""));
            });
        }

        [Test]
        public void GroupingBandUserControlXaml_OwnsBusinessGroupingContentWithoutWorkspaceCloseChrome()
        {
            var xaml = File.ReadAllText(GetGroupingBandXamlPath());
            var code = File.ReadAllText(GetGroupingBandCodePath());

            Assert.Multiple(() =>
            {
                Assert.That(xaml, Does.Contain("x:Class=\"PhialeTech.PhialeGrid.Wpf.Controls.PhialeGroupingBand\""));
                Assert.That(xaml, Does.Contain("Text=\"{Binding ElementName=Root, Path=BandLabelText}\""));
                Assert.That(xaml, Does.Contain("Text=\"{Binding ElementName=Root, Path=DropText}\""));
                Assert.That(xaml, Does.Contain("x:Name=\"GroupingDropZone\""));
                Assert.That(xaml, Does.Contain("CornerRadius=\"{DynamicResource CornerRadius.Grid.RegionHeader}\""));
                Assert.That(xaml, Does.Contain("x:Name=\"DirectionGlyph\""));
                Assert.That(xaml, Does.Contain("Text=\"{Binding DirectionGlyph}\""));
                Assert.That(xaml, Does.Contain("x:Name=\"HierarchyArrow\""));
                Assert.That(xaml, Does.Contain("C 24 8 24 12 32 12"));
                Assert.That(xaml, Does.Not.Contain("AppendDropText"));
                Assert.That(code, Does.Contain("ExpandAllGroupsRequestedEvent"));
                Assert.That(code, Does.Contain("CollapseAllGroupsRequestedEvent"));
                Assert.That(xaml, Does.Not.Contain("GroupingRegionCloseButton"));
                Assert.That(xaml, Does.Not.Contain("HandleRegionCloseButtonClick"));
                Assert.That(xaml, Does.Not.Contain("PhialeWorkspaceBand"));
            });
        }

        [Test]
        public void WpfHostXaml_UsesWorkspacePanelForSideTools()
        {
            var xaml = File.ReadAllText(GetXamlPath());

            Assert.Multiple(() =>
            {
                Assert.That(xaml, Does.Contain("<controls:PhialeWorkspacePanel x:Name=\"ToolsPanel\""));
                Assert.That(xaml, Does.Contain("x:Name=\"SideToolRegionSplitter\""));
                Assert.That(xaml, Does.Contain("x:Name=\"SideToolRegionContentPresenter\""));
            });
        }

        [Test]
        public void ToolsPanelUserControlXaml_ContainsOnlyDensityContentWithoutWorkspaceChrome()
        {
            var toolsPanelXaml = File.ReadAllText(GetToolsPanelXamlPath());
            var demoXaml = File.ReadAllText(GetDemoMainWindowXamlPath());
            var gridControlsXaml = File.ReadAllText(GetSharedGridControlsXamlPath());

            Assert.Multiple(() =>
            {
                Assert.That(toolsPanelXaml, Does.Contain("x:Class=\"PhialeTech.PhialeGrid.Wpf.Controls.PhialeToolsPanel\""));
                Assert.That(toolsPanelXaml, Does.Contain("Text=\"Density\""));
                Assert.That(toolsPanelXaml, Does.Contain("Style=\"{DynamicResource PgGridToolsPanelLabelTextStyle}\""));
                Assert.That(toolsPanelXaml, Does.Contain("Style=\"{DynamicResource PgGridToolsComboBoxStyle}\""));
                Assert.That(toolsPanelXaml, Does.Contain("GridDensity.Compact"));
                Assert.That(toolsPanelXaml, Does.Contain("GridDensity.Normal"));
                Assert.That(toolsPanelXaml, Does.Contain("GridDensity.Comfortable"));
                Assert.That(toolsPanelXaml, Does.Not.Contain("Interaction"));
                Assert.That(toolsPanelXaml, Does.Not.Contain("Editing scenarios"));
                Assert.That(toolsPanelXaml, Does.Not.Contain("HandleRegionDrag"));
                Assert.That(toolsPanelXaml, Does.Not.Contain("SideToolRegionToggle"));
                Assert.That(toolsPanelXaml, Does.Not.Contain("SideToolRegionClose"));
                Assert.That(demoXaml, Does.Contain("<grid:PhialeToolsPanel"));
                Assert.That(demoXaml, Does.Not.Contain("Show row-state baseline"));
                Assert.That(demoXaml, Does.Not.Contain("Show current + edited"));
                Assert.That(demoXaml, Does.Not.Contain("Show current + error"));
                Assert.That(demoXaml, Does.Not.Contain("Reset row-state demo"));
                Assert.That(demoXaml, Does.Not.Contain("Scroll to edited row"));
                Assert.That(demoXaml, Does.Not.Contain("Scroll to error cell"));
                Assert.That(demoXaml, Does.Not.Contain("Scroll to last column"));
                Assert.That(gridControlsXaml, Does.Contain("x:Key=\"PgGridToolsComboBoxStyle\""));
                Assert.That(gridControlsXaml, Does.Contain("ItemContainerStyle\" Value=\"{StaticResource PgGridToolsComboBoxItemStyle}\""));
                Assert.That(gridControlsXaml, Does.Contain("x:Name=\"PgGridToolsComboBoxDropDownToggle\""));
                Assert.That(gridControlsXaml, Does.Contain("Grid.ColumnSpan=\"2\""));
                Assert.That(gridControlsXaml, Does.Contain("ClickMode=\"Press\""));
                Assert.That(gridControlsXaml, Does.Contain("x:Name=\"PgGridToolsComboBoxChevron\""));
                Assert.That(gridControlsXaml, Does.Contain("IsHitTestVisible=\"False\""));
                Assert.That(gridControlsXaml, Does.Contain("PgGridToolsComboBoxDropDownBorder"));
                Assert.That(gridControlsXaml, Does.Not.Contain("SystemColors.HighlightBrushKey"));
            });
        }

        [Test]
        public void WorkspacePanels_HostToolsChangesAndValidationAsSeparatePanelsFromStatusBadges()
        {
            var demoXaml = File.ReadAllText(GetDemoMainWindowXamlPath());
            var projectFile = File.ReadAllText(GetGridWpfProjectPath());
            var changePanelXaml = File.ReadAllText(GetChangePanelXamlPath());
            var validationPanelXaml = File.ReadAllText(GetValidationPanelXamlPath());
            var gridControlsXaml = File.ReadAllText(GetSharedGridControlsXamlPath());
            var gridXaml = File.ReadAllText(GetXamlPath());
            var gridCode = File.ReadAllText(GetGridWpfCodePath());

            Assert.Multiple(() =>
            {
                Assert.That(demoXaml, Does.Contain("<grid:PhialeToolsPanel"));
                Assert.That(demoXaml, Does.Contain("<grid:PhialeGrid.ChangePanelContent>"));
                Assert.That(demoXaml, Does.Contain("<grid:PhialeGrid.ValidationPanelContent>"));
                Assert.That(demoXaml, Does.Contain("<grid:PhialeChangePanel"));
                Assert.That(demoXaml, Does.Contain("<grid:PhialeValidationPanel"));
                Assert.That(demoXaml, Does.Contain("Click=\"HandleOpenChangesPanelClick\""));
                Assert.That(demoXaml, Does.Contain("Click=\"HandleOpenValidationPanelClick\""));
                Assert.That(demoXaml, Does.Not.Contain("GridWorkspacePanelTabs"));
                Assert.That(projectFile, Does.Contain("Controls\\PhialeChangePanel.xaml"));
                Assert.That(projectFile, Does.Contain("Controls\\PhialeValidationPanel.xaml"));
                Assert.That(gridXaml, Does.Contain("x:Name=\"ChangePanelRegionHost\""));
                Assert.That(gridXaml, Does.Contain("x:Name=\"ValidationPanelRegionHost\""));
                Assert.That(gridXaml, Does.Contain("x:Name=\"LeftWorkspacePanelColumn\""));
                Assert.That(gridXaml, Does.Contain("x:Name=\"SideToolRegionColumn\""));
                Assert.That(gridXaml, Does.Not.Contain("PgWorkspacePanelBottomTabButtonStyle"));
                Assert.That(gridXaml, Does.Contain("Click=\"HandleOpenWorkspacePanelTabClick\""));
                Assert.That(gridXaml, Does.Contain("Path=ToolsPanelValidationTabVisibility"));
                Assert.That(gridXaml, Does.Contain("Path=ValidationPanelToolsTabVisibility"));
                Assert.That(gridXaml, Does.Contain("x:Name=\"ValidationRailChangesTabButton\""));
                Assert.That(gridXaml, Does.Contain("x:Name=\"ToolsRailValidationTabButton\""));
                Assert.That(gridXaml, Does.Contain("PgWorkspacePanelExpandedTabStripStyle"));
                Assert.That(gridXaml, Does.Contain("PgWorkspacePanelExpandedTabButtonStyle"));
                Assert.That(gridXaml, Does.Contain("PgWorkspacePanelCollapsedRailBorderStyle"));
                Assert.That(gridXaml, Does.Contain("PgWorkspacePanelCollapsedRailTabStackStyle"));
                Assert.That(gridXaml, Does.Contain("PgWorkspacePanelCollapsedRailTabButtonStyle"));
                Assert.That(gridCode, Does.Contain("public void OpenWorkspacePanel(GridRegionKind regionKind)"));
                Assert.That(gridCode, Does.Contain("ResolveWorkspacePanelTabVisibility(GridRegionKind hostRegionKind, GridRegionKind targetRegionKind)"));
                Assert.That(gridCode, Does.Contain("HideWorkspacePanelAndActivateNext(regionKind)"));
                Assert.That(gridCode, Does.Contain("ResolveWorkspacePanelDragChrome(_regionDragPreviewKind.Value"));
                Assert.That(gridCode, Does.Contain("HideWorkspacePanels()"));
                Assert.That(gridControlsXaml, Does.Contain("PgWorkspacePanelBottomTabItemPadding"));
                Assert.That(gridControlsXaml, Does.Contain("PgWorkspacePanelBottomTabItemMargin"));
                Assert.That(gridControlsXaml, Does.Contain("x:Key=\"PgWorkspacePanelExpandedTabButtonStyle\""));
                Assert.That(gridControlsXaml, Does.Contain("x:Key=\"PgWorkspacePanelCollapsedRailTabButtonStyle\""));
                Assert.That(gridControlsXaml, Does.Contain("x:Key=\"PgWorkspacePanelCollapsedRailBorderStyle\""));
                Assert.That(changePanelXaml, Does.Contain("x:Class=\"PhialeTech.PhialeGrid.Wpf.Controls.PhialeChangePanel\""));
                Assert.That(changePanelXaml, Does.Contain("ItemsSource=\"{Binding ChangePanelItems}\""));
                Assert.That(changePanelXaml, Does.Contain("Loaded=\"HandleChangePanelLoaded\""));
                Assert.That(changePanelXaml, Does.Contain("PgWorkspacePanelChangeCardBorderStyle"));
                Assert.That(changePanelXaml, Does.Contain("Text=\"{Binding Title}\""));
                Assert.That(changePanelXaml, Does.Contain("Text=\"{Binding Description}\""));
                Assert.That(changePanelXaml, Does.Contain("Click=\"HandleGoToRowClick\""));
                Assert.That(changePanelXaml, Does.Contain("Content=\"{Binding ChangePanelRowsFilterToggleText}\""));
                Assert.That(changePanelXaml, Does.Contain("Click=\"HandleChangedRowsFilterToggleClick\""));
                Assert.That(changePanelXaml, Does.Not.Contain("SideToolRegionClose"));
                Assert.That(validationPanelXaml, Does.Contain("x:Class=\"PhialeTech.PhialeGrid.Wpf.Controls.PhialeValidationPanel\""));
                Assert.That(validationPanelXaml, Does.Contain("ItemsSource=\"{Binding ValidationIssueItems}\""));
                Assert.That(validationPanelXaml, Does.Contain("Loaded=\"HandleValidationPanelLoaded\""));
                Assert.That(validationPanelXaml, Does.Contain("PgWorkspacePanelValidationIssueCardBorderStyle"));
                Assert.That(validationPanelXaml, Does.Contain("Text=\"{Binding Title}\""));
                Assert.That(validationPanelXaml, Does.Contain("Text=\"{Binding Message}\""));
                Assert.That(validationPanelXaml, Does.Contain("Click=\"HandleGoToCellClick\""));
                Assert.That(validationPanelXaml, Does.Not.Contain("SideToolRegionClose"));
                Assert.That(gridControlsXaml, Does.Contain("x:Key=\"PgWorkspacePanelBottomTabButtonStyle\""));
                Assert.That(gridControlsXaml, Does.Contain("x:Key=\"PgWorkspacePanelCardBorderStyle\""));
                Assert.That(gridControlsXaml, Does.Contain("x:Key=\"PgWorkspacePanelChangeCardBorderStyle\""));
                Assert.That(gridControlsXaml, Does.Contain("x:Key=\"PgWorkspacePanelValidationIssueCardBorderStyle\""));
            });
        }

        [Test]
        public void WpfHostXaml_UsesLightRegionShells_AndRemovesHeavyPanelComposition()
        {
            var xaml = File.ReadAllText(GetXamlPath());

            Assert.Multiple(() =>
            {
                Assert.That(xaml, Does.Contain("x:Name=\"WorkspaceBandShell\""));
                Assert.That(xaml, Does.Contain("x:Name=\"GroupingRegionHost\""));
                Assert.That(xaml, Does.Contain("x:Name=\"SummaryBottomRegionHost\""));
                Assert.That(xaml, Does.Contain("x:Name=\"BottomStatusStripHost\""));
                Assert.That(xaml, Does.Contain("x:Name=\"SideToolRegionExpandedShell\""));
                Assert.That(xaml, Does.Not.Contain("x:Name=\"GroupingRegionContainer\""));
                Assert.That(xaml, Does.Not.Contain("x:Name=\"SideToolRegionExpandedCard\""));
                Assert.That(xaml, Does.Contain("BorderThickness=\"0,0,0,1\""));
                Assert.That(xaml, Does.Contain("BorderThickness=\"0,1,0,0\""));
            });
        }

        [Test]
        public void DemoMainWindow_UsesCompactTopCommandStripContentInsteadOfLegacyCommandBarLayout()
        {
            var xaml = File.ReadAllText(GetDemoMainWindowXamlPath());

            Assert.Multiple(() =>
            {
                Assert.That(xaml, Does.Contain("Tag=\"DemoGridTopCommandStrip\""));
                Assert.That(xaml, Does.Contain("Tag=\"DemoGridTopCommandStatusStrip\""));
                Assert.That(xaml, Does.Contain("GridCommandStripPrimaryButtonStyle"));
                Assert.That(xaml, Does.Contain("GridCommandStripBadgeBorderStyle"));
                Assert.That(xaml, Does.Not.Contain("CommandBarPrimaryButtonStyle"));
                Assert.That(xaml, Does.Not.Contain("CommandBarDangerButtonStyle"));
                Assert.That(xaml, Does.Not.Contain("CommandBarBadgeBorderStyle"));
                Assert.That(xaml, Does.Not.Contain("CommandBarBadgeTextStyle"));
            });
        }

        [Test]
        public void WpfHostXaml_ShouldUseSymmetricFilterAndHeaderBorderThickness()
        {
            var xaml = File.ReadAllText(GetXamlPath());

            Assert.Multiple(() =>
            {
                Assert.That(xaml, Does.Contain("BorderThickness=\"{DynamicResource PgGridRowHeaderBorderThickness}\""));
                Assert.That(xaml, Does.Contain("BorderThickness=\"0,0,1,2\""));
            });
        }

        [Test]
        public void WpfHostXaml_UsesSharedSurfaceThemeTokens()
        {
            var sharedStylesXaml = File.ReadAllText(GetSharedGridControlsXamlPath());
            var sharedTokensXaml = File.ReadAllText(GetSharedGridSharedXamlPath());

            Assert.Multiple(() =>
            {
                Assert.That(sharedStylesXaml, Does.Contain("PgGroupingPanelVisibilityStyle"));
                Assert.That(sharedStylesXaml, Does.Contain("PgEmptyRowsBorderStyle"));
                Assert.That(sharedTokensXaml, Does.Contain("PgMasterDetailHeaderBackgroundBrush"));
                Assert.That(sharedTokensXaml, Does.Contain("PgGridBackgroundBrush"));
            });
        }

        [Test]
        public void MasterDetailPresenter_ShouldUseThemeResourcesWithoutHardcodedLightColorsOrTextTrimming()
        {
            var presenterCode = File.ReadAllText(GetMasterDetailPresenterPath());

            Assert.Multiple(() =>
            {
                Assert.That(presenterCode, Does.Contain("PgDetailsBackgroundBrush"));
                Assert.That(presenterCode, Does.Contain("PgDetailsBorderBrush"));
                Assert.That(presenterCode, Does.Contain("PgMasterDetailHeaderBackgroundBrush"));
                Assert.That(presenterCode, Does.Contain("PgMasterDetailDetailBackgroundBrush"));
                Assert.That(presenterCode, Does.Not.Contain("Brushes.White"));
                Assert.That(presenterCode, Does.Not.Contain("Color.FromRgb"));
                Assert.That(presenterCode, Does.Not.Contain("TextTrimming.CharacterEllipsis"));
            });
        }

        private static string GetXamlPath()
        {
            return Path.Combine(GetRepoRoot(), "src", "PhialeTech", "Shared", "PhialeTech.Styles.Wpf", "Themes.Linked", "Grid", "PhialeGrid.xaml");
        }

        private static string GetGroupingBandXamlPath()
        {
            return Path.Combine(GetRepoRoot(), "src", "PhialeTech", "Products", "Grid", "Platforms", "Wpf", "PhialeGrid.Wpf", "Controls", "PhialeGroupingBand.xaml");
        }

        private static string GetGroupingBandCodePath()
        {
            return Path.Combine(GetRepoRoot(), "src", "PhialeTech", "Products", "Grid", "Platforms", "Wpf", "PhialeGrid.Wpf", "Controls", "PhialeGroupingBand.xaml.cs");
        }

        private static string GetWorkspacePlaygroundCodePath()
        {
            return Path.Combine(GetRepoRoot(), "src", "PhialeTech", "Products", "Grid", "Platforms", "Wpf", "PhialeGrid.Wpf", "Controls", "PhialeWorkspacePlayground.xaml.cs");
        }

        private static string GetSharedGridTokensXamlPath()
        {
            return Path.Combine(GetRepoRoot(), "src", "PhialeTech", "Shared", "PhialeTech.Styles.Wpf", "Themes", "PhialeGrid.Shared.xaml");
        }

        private static string GetToolsPanelXamlPath()
        {
            return Path.Combine(GetRepoRoot(), "src", "PhialeTech", "Products", "Grid", "Platforms", "Wpf", "PhialeGrid.Wpf", "Controls", "PhialeToolsPanel.xaml");
        }

        private static string GetChangePanelXamlPath()
        {
            return Path.Combine(GetRepoRoot(), "src", "PhialeTech", "Products", "Grid", "Platforms", "Wpf", "PhialeGrid.Wpf", "Controls", "PhialeChangePanel.xaml");
        }

        private static string GetValidationPanelXamlPath()
        {
            return Path.Combine(GetRepoRoot(), "src", "PhialeTech", "Products", "Grid", "Platforms", "Wpf", "PhialeGrid.Wpf", "Controls", "PhialeValidationPanel.xaml");
        }

        private static string GetGridWpfProjectPath()
        {
            return Path.Combine(GetRepoRoot(), "src", "PhialeTech", "Products", "Grid", "Platforms", "Wpf", "PhialeGrid.Wpf", "PhialeGrid.Wpf.csproj");
        }

        private static string GetGridWpfCodePath()
        {
            return Path.Combine(GetRepoRoot(), "src", "PhialeTech", "Products", "Grid", "Platforms", "Wpf", "PhialeGrid.Wpf", "PhialeGrid.cs");
        }

        private static string GetSharedGridControlsXamlPath()
        {
            return Path.Combine(GetRepoRoot(), "src", "PhialeTech", "Shared", "PhialeTech.Styles.Wpf", "Themes", "PhialeGrid.Controls.xaml");
        }

        private static string GetDemoMainWindowXamlPath()
        {
            return Path.Combine(GetRepoRoot(), "src", "PhialeTech", "Shared", "PhialeTech.Styles.Wpf", "Themes.Linked", "Demo", "PhialeTech.Components.Wpf.MainWindow.xaml");
        }

        private static string GetSharedGridSharedXamlPath()
        {
            return Path.Combine(GetRepoRoot(), "src", "PhialeTech", "Shared", "PhialeTech.Styles.Wpf", "Themes", "PhialeGrid.Shared.xaml");
        }

        private static string GetMasterDetailPresenterPath()
        {
            return Path.Combine(GetRepoRoot(), "src", "PhialeTech", "Products", "Grid", "Platforms", "Wpf", "PhialeGrid.Wpf", "Surface", "Presenters", "GridMasterDetailPresenter.cs");
        }

        private static string GetRepoRoot()
        {
            return RepositoryPaths.GetRepositoryRoot();
        }
    }
}

