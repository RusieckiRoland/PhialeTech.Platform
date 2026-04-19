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

            Assert.That(xaml, Does.Contain("HandleExpandAllGroupsClick"));
            Assert.That(xaml, Does.Contain("HandleCollapseAllGroupsClick"));
            Assert.That(xaml, Does.Contain("Visibility=\"{Binding ElementName=Root, Path=HasGroups"));
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
        public void WpfHostXaml_DefinesStageOneGridRegionsAndSplitters()
        {
            var xaml = File.ReadAllText(GetXamlPath());

            Assert.Multiple(() =>
            {
                Assert.That(xaml, Does.Contain("x:Name=\"TopCommandStripHost\""));
                Assert.That(xaml, Does.Contain("x:Name=\"TopCommandStripShell\""));
                Assert.That(xaml, Does.Contain("x:Name=\"TopCommandStripContentPresenter\""));
                Assert.That(xaml, Does.Not.Contain("x:Name=\"TopCommandRegionHost\""));
                Assert.That(xaml, Does.Contain("x:Name=\"GroupingRegionShell\""));
                Assert.That(xaml, Does.Contain("x:Name=\"SummaryBottomRegionHost\""));
                Assert.That(xaml, Does.Contain("x:Name=\"SideToolRegionHost\""));
                Assert.That(xaml, Does.Not.Contain("x:Name=\"TopCommandRegionSplitter\""));
                Assert.That(xaml, Does.Contain("x:Name=\"GroupingRegionSplitter\""));
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
                Assert.That(xaml, Does.Contain("x:Name=\"TopCommandStripToggleButton\""));
                Assert.That(xaml, Does.Contain("x:Name=\"TopCommandStripCloseButton\""));
                Assert.That(xaml, Does.Not.Contain("Path=TopCommandRegionToggleText"));
                Assert.That(xaml, Does.Not.Contain("CommandsRegionTitleText"));
                Assert.That(xaml, Does.Not.Contain("TopCommandRegionExpanderButton"));
                Assert.That(xaml, Does.Not.Contain("TopCommandRegionCloseButton"));
                Assert.That(xaml, Does.Not.Contain("Content=\"+\""));
                Assert.That(xaml, Does.Contain("HorizontalScrollBarVisibility=\"Auto\""));
                Assert.That(xaml, Does.Contain("BorderThickness=\"0,0,0,1\""));
            });
        }

        [Test]
        public void WpfHostXaml_UsesLightRegionShells_AndRemovesHeavyPanelComposition()
        {
            var xaml = File.ReadAllText(GetXamlPath());

            Assert.Multiple(() =>
            {
                Assert.That(xaml, Does.Contain("x:Name=\"GroupingRegionShell\""));
                Assert.That(xaml, Does.Contain("x:Name=\"SummaryBottomRegionShell\""));
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
            return Path.Combine(GetRepoRoot(), "src", "PhialeTech", "Products", "Grid", "Platforms", "Wpf", "PhialeGrid.Wpf", "PhialeGrid.xaml");
        }

        private static string GetSharedGridControlsXamlPath()
        {
            return Path.Combine(GetRepoRoot(), "src", "PhialeTech", "Shared", "PhialeTech.Styles.Wpf", "Themes", "PhialeGrid.Controls.xaml");
        }

        private static string GetDemoMainWindowXamlPath()
        {
            return Path.Combine(GetRepoRoot(), "demo", "PhialeTech", "Wpf", "PhialeTech.Components.Wpf", "MainWindow.xaml");
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
