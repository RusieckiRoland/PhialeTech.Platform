using System.IO;
using NUnit.Framework;
using PhialeGis.Library.Tests.Support;

namespace PhialeGis.Library.Tests.Demo
{
    public class DemoWpfUiTests
    {
        [Test]
        public void EditingToolbar_ShowsPendingBanner_AndDisablesActionsWhenNoPendingEdits()
        {
            var xaml = File.ReadAllText(GetMainWindowPath());

            Assert.That(xaml, Does.Contain("PendingEditBannerText"));
            Assert.That(xaml, Does.Contain("Path=HasPendingEdits"));
            Assert.That(xaml, Does.Contain("CommitToolbarButtonStyle"));
            Assert.That(xaml, Does.Contain("CancelToolbarButtonStyle"));
            Assert.That(xaml, Does.Contain("IsEnabled=\"{Binding RelativeSource={RelativeSource AncestorType={x:Type grid:PhialeGrid}}, Path=HasPendingEdits}\""));
            Assert.That(xaml, Does.Contain("TopCommandContent"));
        }

        [Test]
        public void AdvancedToolbar_ShowsSearchAndNamedViews_WithoutLegacyColumnChooserButtons()
        {
            var xaml = File.ReadAllText(GetMainWindowPath());

            Assert.That(xaml, Does.Contain("LayoutToolsHintText"));
            Assert.That(xaml, Does.Not.Contain("ColumnChooserToggleButton"));
            Assert.That(xaml, Does.Not.Contain("HandleColumnChooserItemChanged"));
            Assert.That(xaml, Does.Not.Contain("HandleFreezeObjectNameClick"));
            Assert.That(xaml, Does.Not.Contain("HandleAutoFitClick"));
            Assert.That(xaml, Does.Contain("GridSearchText"));
            Assert.That(xaml, Does.Contain("HandleApplySearchClick"));
            Assert.That(xaml, Does.Contain("PendingViewName"));
            Assert.That(xaml, Does.Contain("HandleSaveViewClick"));
            Assert.That(xaml, Does.Contain("HandleApplyViewClick"));
        }

        [Test]
        public void FilteringToolbar_ShouldExposeColumnFilterFocusAndResetActions()
        {
            var xaml = File.ReadAllText(GetMainWindowPath());
            var codeBehind = File.ReadAllText(Path.Combine(GetRepoRoot(), "demo", "PhialeTech", "Wpf", "PhialeTech.Components.Wpf", "MainWindow.xaml.cs"));

            Assert.Multiple(() =>
            {
                Assert.That(xaml, Does.Contain("ShowFilteringTools"));
                Assert.That(xaml, Does.Contain("HandleFocusMunicipalityFilterClick"));
                Assert.That(xaml, Does.Contain("HandleFocusOwnerFilterClick"));
                Assert.That(xaml, Does.Contain("HandleClearColumnFiltersClick"));
                Assert.That(codeBehind, Does.Contain("DemoGrid?.FocusColumnFilter(\"Municipality\")"));
                Assert.That(codeBehind, Does.Contain("DemoGrid?.FocusColumnFilter(\"Owner\")"));
                Assert.That(codeBehind, Does.Contain("DemoGrid?.ClearFilters()"));
            });
        }

        [Test]
        public void OverviewCards_ShouldUseWpfScenarioLoadTracingCommand()
        {
            var xaml = File.ReadAllText(GetMainWindowPath());
            var codeBehind = File.ReadAllText(Path.Combine(GetRepoRoot(), "demo", "PhialeTech", "Wpf", "PhialeTech.Components.Wpf", "MainWindow.xaml.cs"));

            Assert.Multiple(() =>
            {
                Assert.That(xaml, Does.Contain("SelectExampleWithTraceCommand"));
                Assert.That(xaml, Does.Contain("CommandParameter=\"{Binding Id}\""));
                Assert.That(codeBehind, Does.Contain("StartScenarioLoadTrace"));
                Assert.That(codeBehind, Does.Contain("TraceNextScenarioRender"));
                Assert.That(codeBehind, Does.Contain("SelectExampleWithGridBatch(exampleId)"));
                Assert.That(codeBehind, Does.Contain("BeginScenarioSelectionGridBatch()"));
                Assert.That(codeBehind, Does.Contain("CompleteScenarioSelectionGridBatch(\"activation-completed\")"));
            });
        }

        [Test]
        public void TransferToolbar_ShowsExportImportAndRestoreActions_WithPreviewSurface()
        {
            var xaml = File.ReadAllText(GetMainWindowPath());

            Assert.That(xaml, Does.Contain("ShowTransferTools"));
            Assert.That(xaml, Does.Contain("HandleExportCsvClick"));
            Assert.That(xaml, Does.Contain("HandleImportSampleCsvClick"));
            Assert.That(xaml, Does.Contain("HandleRestoreSourceClick"));
            Assert.That(xaml, Does.Contain("TransferStatusText"));
            Assert.That(xaml, Does.Contain("TransferPreviewText"));
        }

        [Test]
        public void GridExamples_ShouldKeepEditingActionsInsideGridRegions_NotInOuterDemoToolbar()
        {
            var xaml = File.ReadAllText(GetMainWindowPath());

            Assert.Multiple(() =>
            {
                Assert.That(xaml, Does.Contain("HandleAddDemoRecordClick"));
                Assert.That(xaml, Does.Contain("Text=\"{Binding AddRecordText}\""));
                Assert.That(xaml, Does.Contain("HandleBeginCurrentEditClick"));
                Assert.That(xaml, Does.Contain("Text=\"{Binding EditRecordText}\""));
                Assert.That(xaml, Does.Contain("SelectionScenarioStatusText"));
                Assert.That(xaml, Does.Contain("EditingScenarioStatusText"));
                Assert.That(xaml, Does.Contain("ConstraintScenarioStatusText"));
                Assert.That(xaml, Does.Not.Contain("x:Name=\"ShowRowStateBaselineScenarioButton\""));
                Assert.That(xaml, Does.Not.Contain("x:Name=\"ScrollEditedRowDemoButton\""));
            });
        }

        [Test]
        public void MasterDetailToolbar_ShowsPlacementToggleButton()
        {
            var xaml = File.ReadAllText(GetMainWindowPath());

            Assert.That(xaml, Does.Contain("HandleToggleMasterDetailPlacementClick"));
            Assert.That(xaml, Does.Contain("MasterDetailPlacementText"));
            Assert.That(xaml, Does.Contain("ShowMasterDetailPlacementToggle"));
        }

        [Test]
        public void SummaryDesignerToolbar_ShowsDesignerSelectorsAndActions()
        {
            var xaml = File.ReadAllText(GetMainWindowPath());

            Assert.That(xaml, Does.Contain("ShowSummaryDesignerTools"));
            Assert.That(xaml, Does.Contain("AvailableSummaryColumns"));
            Assert.That(xaml, Does.Contain("AvailableSummaryTypes"));
            Assert.That(xaml, Does.Contain("ConfiguredSummaries"));
            Assert.That(xaml, Does.Contain("HandleAddSummaryClick"));
            Assert.That(xaml, Does.Contain("HandleRemoveSummaryClick"));
            Assert.That(xaml, Does.Contain("HandleResetSummariesClick"));
        }

        [Test]
        public void MainWindow_ShouldIncludeActiveLayerSelectorSurface_AndDedicatedSidebarEntry()
        {
            var xaml = File.ReadAllText(GetMainWindowPath());

            Assert.That(xaml, Does.Contain("xmlns:selector=\"clr-namespace:PhialeTech.ActiveLayerSelector.Wpf.Controls;assembly=PhialeTech.ActiveLayerSelector.Wpf\""));
            Assert.That(xaml, Does.Contain("DemoActiveLayerSelector"));
            Assert.That(xaml, Does.Contain("DrawerGroupsItemsControl"));
            Assert.That(xaml, Does.Contain("SelectDrawerGroupCommand"));
            Assert.That(xaml, Does.Contain("CommandParameter=\"{Binding Id}\""));
            Assert.That(xaml, Does.Contain("ShowActiveLayerSelectorSurface"));
            Assert.That(xaml, Does.Contain("State=\"{Binding ActiveLayerSelectorState}\""));
        }

        [Test]
        public void MainWindow_ShouldExposeDedicatedFoundationsSurface_InsteadOfReusingGridHost()
        {
            var xaml = File.ReadAllText(GetMainWindowPath());

            Assert.That(xaml, Does.Contain("FoundationsSurfaceScrollViewer"));
            Assert.That(xaml, Does.Contain("ShowFoundationsSurface"));
            Assert.That(xaml, Does.Contain("FoundationsTypographyTokens"));
            Assert.That(xaml, Does.Contain("FoundationsSurfaceTokens"));
            Assert.That(xaml, Does.Contain("FoundationsShapeTokens"));
            Assert.That(xaml, Does.Contain("FoundationsSectionBorderStyle"));
        }

        [Test]
        public void MainWindow_ShouldExposeDefinitionManagerSurface_AndSharedDefinitionManagerWiring()
        {
            var xaml = File.ReadAllText(GetMainWindowPath());
            var appCode = File.ReadAllText(Path.Combine(GetRepoRoot(), "demo", "PhialeTech", "Wpf", "PhialeTech.Components.Wpf", "App.xaml.cs"));
            var mainWindowCode = File.ReadAllText(Path.Combine(GetRepoRoot(), "demo", "PhialeTech", "Wpf", "PhialeTech.Components.Wpf", "MainWindow.xaml.cs"));

            Assert.That(xaml, Does.Contain("DefinitionManagerSurfaceScrollViewer"));
            Assert.That(xaml, Does.Contain("ShowDefinitionManagerSurface"));
            Assert.That(xaml, Does.Contain("SelectDrawerGroupCommand"));
            Assert.That(mainWindowCode, Does.Contain("HandleArchitectureComponentClick"));
            Assert.That(mainWindowCode, Does.Contain("definitionManager: _applicationServices.DefinitionManager"));
            Assert.That(appCode, Does.Contain("DemoApplicationServices.CreateDefault()"));
        }

        [Test]
        public void MainWindow_ShouldExposeApplicationStateManagerSurface_WithDedicatedLiveGridAndSharedStateWiring()
        {
            var xaml = File.ReadAllText(GetMainWindowPath());
            var mainWindowCode = File.ReadAllText(Path.Combine(GetRepoRoot(), "demo", "PhialeTech", "Wpf", "PhialeTech.Components.Wpf", "MainWindow.xaml.cs"));

            Assert.That(xaml, Does.Contain("ApplicationStateManagerSurfaceScrollViewer"));
            Assert.That(xaml, Does.Contain("ShowApplicationStateManagerSurface"));
            Assert.That(xaml, Does.Contain("ApplicationStateDemoGrid"));
            Assert.That(xaml, Does.Contain("ApplicationStateManagerStateKey"));
            Assert.That(mainWindowCode, Does.Contain("GetCurrentStateGrid()"));
            Assert.That(mainWindowCode, Does.Contain("ApplicationStateDemoGrid"));
            Assert.That(mainWindowCode, Does.Contain("ApplicationStateDemoGrid.LanguageDirectory = gridLanguageDirectory;"));
        }

        [Test]
        public void MainWindow_ActiveLayerSelectorSurface_ShouldUseSingleSelectorBoundToGlobalTheme()
        {
            var xaml = File.ReadAllText(GetMainWindowPath());

            Assert.That(xaml, Does.Contain("x:Name=\"DemoActiveLayerSelector\""));
            Assert.That(xaml, Does.Not.Contain("x:Name=\"ActiveLayerSelectorModeTabs\""));
            Assert.That(xaml, Does.Not.Contain("x:Name=\"DemoActiveLayerSelectorNight\""));
        }

        [Test]
        public void MainWindow_ToolbarContainer_ShouldCollapseWhenScenarioHasNoToolbar()
        {
            var xaml = File.ReadAllText(GetMainWindowPath());
            var viewModelCode = File.ReadAllText(Path.Combine(GetRepoRoot(), "demo", "PhialeTech", "Shared", "PhialeTech.Components.Shared", "ViewModels", "DemoShellViewModel.cs"));

            Assert.That(viewModelCode, Does.Contain("HasDemoToolbar"));
            Assert.That(xaml, Does.Contain("ShowGridEditCommandBar"));
            Assert.That(xaml, Does.Contain("BooleanToVisibilityConverter"));
        }

        [Test]
        public void WpfDemoApp_ShouldLoadSharedStylesLibrary()
        {
            var appXaml = File.ReadAllText(Path.Combine(GetRepoRoot(), "demo", "PhialeTech", "Wpf", "PhialeTech.Components.Wpf", "App.xaml"));
            var appCsproj = File.ReadAllText(Path.Combine(GetRepoRoot(), "demo", "PhialeTech", "Wpf", "PhialeTech.Components.Wpf", "PhialeTech.Components.Wpf.csproj"));

            Assert.That(appXaml, Does.Contain("PhialeTech.Styles.Wpf;component/Themes/ActiveLayerSelector.Shared.xaml"));
            Assert.That(appCsproj, Does.Contain("PhialeTech.Styles.Wpf\\PhialeTech.Styles.Wpf.csproj"));
        }

        [Test]
        public void WpfDemoApp_ShouldCreateOneSharedApplicationStateManager_AndPassItToMainWindow()
        {
            var appCode = File.ReadAllText(Path.Combine(GetRepoRoot(), "demo", "PhialeTech", "Wpf", "PhialeTech.Components.Wpf", "App.xaml.cs"));
            var mainWindowCode = File.ReadAllText(Path.Combine(GetRepoRoot(), "demo", "PhialeTech", "Wpf", "PhialeTech.Components.Wpf", "MainWindow.xaml.cs"));

            Assert.That(appCode, Does.Contain("DemoApplicationServices.CreateDefault()"));
            Assert.That(appCode, Does.Contain("new MainWindow(_applicationServices)"));
            Assert.That(mainWindowCode, Does.Contain("ApplicationStateRegistration"));
            Assert.That(mainWindowCode, Does.Contain("PhialeGridViewStateComponent"));
            Assert.That(mainWindowCode, Does.Not.Contain("_savedGridState"));
        }

        [Test]
        public void ActiveLayerSelector_ShouldUseSharedStylesDictionary_InsteadOfInlineControlStyles()
        {
            var selectorXaml = File.ReadAllText(Path.Combine(GetRepoRoot(), "src", "PhialeTech", "Products", "ActiveLayerSelector", "Platforms", "Wpf", "PhialeTech.ActiveLayerSelector.Wpf", "Controls", "ActiveLayerSelector.xaml"));

            Assert.That(selectorXaml, Does.Contain("PhialeTech.Styles.Wpf;component/Themes/ActiveLayerSelector.Shared.xaml"));
            Assert.That(selectorXaml, Does.Not.Contain("<Style x:Key=\"HeaderActionButtonStyle\""));
            Assert.That(selectorXaml, Does.Not.Contain("<Style x:Key=\"StatusButtonStyle\""));
            Assert.That(selectorXaml, Does.Not.Contain("<Style x:Key=\"AlsScrollBarStyle\""));
        }

        [Test]
        public void MainWindow_ShouldUseSharedDemoStylesDictionary_InsteadOfInlineStyles()
        {
            var xaml = File.ReadAllText(GetMainWindowPath());

            Assert.That(xaml, Does.Contain("PhialeTech.Styles.Wpf;component/Themes/Demo.Wpf.Shared.xaml"));
            Assert.That(xaml, Does.Not.Contain("<Style x:Key=\"SectionHeadlineStyle\""));
            Assert.That(xaml, Does.Not.Contain("<Style x:Key=\"OverviewCardButtonStyle\""));
            Assert.That(xaml, Does.Not.Contain("<Style x:Key=\"ToolbarButtonStyle\""));
            Assert.That(xaml, Does.Not.Contain("<Style x:Key=\"ComponentCardButtonStyle\""));
        }

        [Test]
        public void MainWindow_Overview_ShouldUseDeterministicSectionRows_InsteadOfWrapPanelLayout()
        {
            var xaml = File.ReadAllText(GetMainWindowPath());

            Assert.Multiple(() =>
            {
                Assert.That(xaml, Does.Contain("ItemsSource=\"{Binding Rows}\""));
                Assert.That(xaml, Does.Contain("StackPanel Orientation=\"Horizontal\""));
                Assert.That(xaml, Does.Not.Contain("<WrapPanel ItemWidth=\"330\" ItemHeight=\"168\" />"));
            });
        }

        [Test]
        public void MainWindow_Overview_ShouldStretchToViewportWidth_AndDisableHorizontalScrolling()
        {
            var xaml = File.ReadAllText(GetMainWindowPath());

            Assert.Multiple(() =>
            {
                Assert.That(xaml, Does.Contain("HorizontalScrollBarVisibility=\"Disabled\""));
                Assert.That(xaml, Does.Contain("AncestorType={x:Type ScrollContentPresenter}"));
                Assert.That(xaml, Does.Contain("Path=ActualWidth"));
            });
        }

        [Test]
        public void ShellControl_ShouldCollapseNavigationRail_WhenNoNavigationItemsExist()
        {
            var shellControlCode = File.ReadAllText(Path.Combine(
                GetRepoRoot(),
                "src",
                "PhialeTech",
                "Shared",
                "Platforms",
                "Wpf",
                "PhialeTech.Shell.Wpf",
                "Controls",
                "PhialeAppShell.cs"));

            Assert.Multiple(() =>
            {
                Assert.That(shellControlCode, Does.Contain("PartNavigationRailHost"));
                Assert.That(shellControlCode, Does.Contain("PartNavigationRailColumn"));
                Assert.That(shellControlCode, Does.Contain("ApplyNavigationRailVisibility"));
                Assert.That(shellControlCode, Does.Contain("ShellState.NavigationItems.Count > 0"));
                Assert.That(shellControlCode, Does.Contain("GridLength.Shell.NavigationRail.Collapsed"));
            });
        }

        [Test]
        public void MainWindow_ShouldApplyDedicatedComboAndScrollStyles_AndAvoidGridMinHeightClipping()
        {
            var xaml = File.ReadAllText(GetMainWindowPath());

            Assert.Multiple(() =>
            {
                Assert.That(xaml, Does.Contain("BasedOn=\"{StaticResource DemoComboBoxStyle}\""));
                Assert.That(xaml, Does.Contain("BasedOn=\"{StaticResource DemoScrollBarStyle}\""));
                Assert.That(xaml, Does.Contain("BasedOn=\"{StaticResource DemoScrollViewerStyle}\""));
                Assert.That(xaml, Does.Not.Contain("MinHeight=\"620\""));
            });
        }

        [Test]
        public void DemoComboBoxItemStyle_ShouldDifferentiateHighlightedAndSelectedStates()
        {
            var controlsXaml = File.ReadAllText(Path.Combine(GetRepoRoot(), "src", "PhialeTech", "Shared", "PhialeTech.Styles.Wpf", "Themes", "Demo.Controls.xaml"));

            Assert.Multiple(() =>
            {
                Assert.That(controlsXaml, Does.Contain("Trigger Property=\"IsHighlighted\" Value=\"True\""));
                Assert.That(controlsXaml, Does.Contain("Trigger Property=\"IsSelected\" Value=\"True\""));
                Assert.That(controlsXaml, Does.Contain("Value=\"{DynamicResource Brush.Hover.Fill}\""));
                Assert.That(controlsXaml, Does.Contain("Value=\"{DynamicResource Brush.Selection.Active.Fill}\""));
                Assert.That(controlsXaml, Does.Contain("Value=\"{DynamicResource Brush.Selection.Active.Border}\""));
                Assert.That(controlsXaml, Does.Not.Contain("Trigger Property=\"IsSelected\" Value=\"True\">\r\n                            <Setter TargetName=\"ItemBorder\" Property=\"Background\" Value=\"{DynamicResource DemoPlatformBadgeBackgroundBrush}\" />"));
            });
        }

        [Test]
        public void MainWindow_ShouldExposeThemeSwitcherNearPlatformBadge_AndThemeAwareGridBrushes()
        {
            var xaml = File.ReadAllText(GetMainWindowPath());
            var codeBehind = File.ReadAllText(Path.Combine(GetRepoRoot(), "demo", "PhialeTech", "Wpf", "PhialeTech.Components.Wpf", "MainWindow.xaml.cs"));

            Assert.That(xaml, Does.Contain("ThemeLabelText"));
            Assert.That(xaml, Does.Contain("ItemsSource=\"{Binding ThemeOptions}\""));
            Assert.That(xaml, Does.Contain("SelectedValue=\"{Binding SelectedThemeCode, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}\""));
            Assert.That(xaml, Does.Contain("Text=\"{Binding PlatformBadgeText}\""));
            Assert.That(xaml, Does.Contain("Background=\"{DynamicResource DemoGridBackgroundBrush}\""));
            Assert.That(xaml, Does.Contain("BorderBrush=\"{DynamicResource DemoGridBorderBrush}\""));
            Assert.That(codeBehind, Does.Contain("DemoGrid.IsNightMode = useNight;"));
            Assert.That(codeBehind, Does.Contain("DemoActiveLayerSelector.IsNightMode = useNight;"));
        }

        [Test]
        public void MainWindow_ShouldUseThemedTabsAndDynamicDetailSurfaceResources()
        {
            var xaml = File.ReadAllText(GetMainWindowPath());
            var sharedStyles = File.ReadAllText(Path.Combine(GetRepoRoot(), "src", "PhialeTech", "Shared", "PhialeTech.Styles.Wpf", "Themes", "Demo.Wpf.Shared.xaml"));

            Assert.That(sharedStyles, Does.Contain("x:Key=\"DemoTabControlStyle\""));
            Assert.That(sharedStyles, Does.Contain("x:Key=\"DemoTabItemStyle\""));
            Assert.That(sharedStyles, Does.Contain("x:Key=\"DemoStatusBannerBorderStyle\""));
            Assert.That(sharedStyles, Does.Contain("x:Key=\"DemoCodeSurfaceBorderStyle\""));
            Assert.That(xaml, Does.Contain("Style=\"{StaticResource DemoTabControlStyle}\""));
            Assert.That(xaml, Does.Contain("ItemContainerStyle=\"{StaticResource DemoTabItemStyle}\""));
            Assert.That(xaml, Does.Contain("Style=\"{StaticResource DemoStatusBannerBorderStyle}\""));
            Assert.That(xaml, Does.Contain("Style=\"{StaticResource DemoCodeSurfaceBorderStyle}\""));
            Assert.That(xaml, Does.Not.Contain("Foreground=\"#17212B\""));
            Assert.That(xaml, Does.Not.Contain("Foreground=\"#52606D\""));
            Assert.That(xaml, Does.Not.Contain("Background=\"#FDF6E3\""));
        }

        private static string GetMainWindowPath()
        {
            return Path.Combine(GetRepoRoot(), "src", "PhialeTech", "Shared", "PhialeTech.Styles.Wpf", "Themes.Linked", "Demo", "PhialeTech.Components.Wpf.MainWindow.xaml");
        }

        private static string GetRepoRoot()
        {
            return RepositoryPaths.GetRepositoryRoot();
        }
    }
}
