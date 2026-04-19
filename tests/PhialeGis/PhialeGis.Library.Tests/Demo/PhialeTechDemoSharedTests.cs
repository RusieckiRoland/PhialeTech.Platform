using System;
using System.Linq;
using System.Threading.Tasks;
using PhialeGrid.Core.Columns;
using PhialeGrid.Core.Summaries;
using NUnit.Framework;
using PhialeGrid.Core.Hierarchy;
using PhialeTech.ActiveLayerSelector;
using PhialeTech.Components.Shared.Localization;
using PhialeTech.Components.Shared.Model;
using PhialeTech.Components.Shared.Services;
using PhialeTech.Components.Shared.ViewModels;

namespace PhialeGis.Library.Tests.Demo
{
    [TestFixture]
    public sealed class PhialeTechDemoSharedTests
    {
        [Test]
        public void LocalizationCatalog_ShouldLoadEnglishAndPolish()
        {
            var catalog = DemoLocalizationCatalog.LoadDefault();

            CollectionAssert.AreEquivalent(new[] { "en", "pl" }, catalog.AvailableLanguageCodes);
            Assert.That(catalog.GetText("pl", DemoTextKeys.ShellOverviewTitle), Is.EqualTo("Wybierz przyklad"));
            Assert.That(catalog.GetText("en", DemoTextKeys.ShellOverviewTitle), Is.EqualTo("Select an example"));
        }

        [Test]
        public void ViewModel_ShouldFilterExamplesUsingSearchText()
        {
            var viewModel = new DemoShellViewModel("Wpf");

            viewModel.SearchText = "filter";

            Assert.That(viewModel.VisibleSections.SelectMany(section => section.Examples).Any(example => example.Id == "filtering"), Is.True);
            Assert.That(viewModel.VisibleSections.SelectMany(section => section.Examples).Any(example => example.Id == "grouping"), Is.False);
        }

        [Test]
        public void ViewModel_ShouldPlaceFoundationsCardFirstInOverview_WithoutChangingDefaultGridEntry()
        {
            var viewModel = new DemoShellViewModel("Wpf");

            var overviewExamples = viewModel.VisibleSections
                .Single(section => section.Key == DemoTextKeys.SectionOverview)
                .Examples
                .Select(example => example.Id)
                .ToArray();

            viewModel.SelectComponent("grid");

            Assert.Multiple(() =>
            {
                Assert.That(overviewExamples.First(), Is.EqualTo("foundations"));
                Assert.That(viewModel.SelectedExampleTitle, Is.EqualTo("Grouping"));
            });
        }

        [Test]
        public void ViewModel_WhenGridDrawerGroupIsSelected_ShouldShowOnlyGridCardsInWorkspaceOverview()
        {
            var viewModel = new DemoShellViewModel("Wpf");

            viewModel.SelectDrawerGroup("grid");

            var visibleExampleIds = viewModel.VisibleSections
                .SelectMany(section => section.Examples)
                .Select(example => example.Id)
                .ToArray();

            Assert.Multiple(() =>
            {
                Assert.That(viewModel.IsOverviewVisible, Is.True);
                Assert.That(viewModel.WorkspaceOverviewTitle, Is.EqualTo("Grid"));
                Assert.That(visibleExampleIds, Does.Contain("grouping"));
                Assert.That(visibleExampleIds, Does.Not.Contain("foundations"));
                Assert.That(visibleExampleIds, Does.Not.Contain("active-layer-selector"));
            });
        }

        [Test]
        public void ViewModel_WhenFoundationsDrawerGroupIsSelected_ShouldOpenSingleFoundationsCardDirectly()
        {
            var viewModel = new DemoShellViewModel("Wpf");

            viewModel.SelectDrawerGroup("foundations");

            Assert.Multiple(() =>
            {
                Assert.That(viewModel.SelectedExample, Is.Not.Null);
                Assert.That(viewModel.SelectedExample.Id, Is.EqualTo("foundations"));
                Assert.That(viewModel.IsFoundationsExample, Is.True);
                Assert.That(viewModel.DetailHeadline, Is.EqualTo("Design Foundations"));
            });
        }

        [Test]
        public void ViewModel_WhenArchitectureDrawerGroupIsSelected_ShouldShowManagerCardsInWorkspaceOverview()
        {
            var viewModel = new DemoShellViewModel("Wpf");

            viewModel.SelectDrawerGroup("architecture");

            var visibleExampleIds = viewModel.VisibleSections
                .SelectMany(section => section.Examples)
                .Select(example => example.Id)
                .ToArray();

            Assert.Multiple(() =>
            {
                Assert.That(viewModel.SelectedExample, Is.Null);
                Assert.That(viewModel.IsOverviewVisible, Is.True);
                Assert.That(viewModel.IsArchitectureDrawerSelected, Is.True);
                Assert.That(viewModel.WorkspaceOverviewTitle, Is.EqualTo("Architecture"));
                Assert.That(visibleExampleIds, Is.EquivalentTo(new[] { "application-state-manager", "definition-manager" }));
            });
        }

        [Test]
        public void ViewModel_ShouldPlaceAboutDrawerGroupBeforeDesignFoundations()
        {
            var viewModel = new DemoShellViewModel("Wpf");

            Assert.Multiple(() =>
            {
                Assert.That(viewModel.DrawerGroups.Select(group => group.Id).Take(2).ToArray(), Is.EqualTo(new[] { "license", "foundations" }));
                Assert.That(viewModel.DrawerGroups.First().Title, Is.EqualTo("About"));
            });
        }

        [Test]
        public void ViewModel_ShouldExposeReportDesignerPrintLicenses_InThirdPartyAboutCard()
        {
            var viewModel = new DemoShellViewModel("Wpf");

            var pdfJs = viewModel.ThirdPartyLicenseEntries.Single(entry => entry.ComponentName == "PDF.js");
            var jsBarcode = viewModel.ThirdPartyLicenseEntries.Single(entry => entry.ComponentName == "JsBarcode");
            var qrCodeGenerator = viewModel.ThirdPartyLicenseEntries.Single(entry => entry.ComponentName == "qrcode-generator");

            Assert.Multiple(() =>
            {
                Assert.That(pdfJs.UsedBy, Does.Contain("PdfViewer"));
                Assert.That(pdfJs.UsedBy, Does.Contain("print"));
                Assert.That(pdfJs.LocalFiles, Does.Contain("src/PhialeTech/Shared/PhialeTech.WebAssets/Assets/PdfViewer/pdfjs.LICENSE"));

                Assert.That(jsBarcode.UsedBy, Does.Contain("ReportDesigner"));
                Assert.That(jsBarcode.UsedBy, Does.Contain("print-template"));
                Assert.That(jsBarcode.LocalFiles, Does.Contain("src/PhialeTech/Shared/PhialeTech.WebAssets/Assets/ReportDesigner/ThirdPartyNotices.md"));
                Assert.That(jsBarcode.LocalFiles, Does.Contain("src/PhialeTech/Shared/PhialeTech.WebAssets/Assets/ReportDesigner/vendor/JsBarcode.MIT-LICENSE.txt"));

                Assert.That(qrCodeGenerator.UsedBy, Does.Contain("ReportDesigner"));
                Assert.That(qrCodeGenerator.UsedBy, Does.Contain("print-template"));
                Assert.That(qrCodeGenerator.LocalFiles, Does.Contain("src/PhialeTech/Shared/PhialeTech.WebAssets/Assets/ReportDesigner/ThirdPartyNotices.md"));
                Assert.That(qrCodeGenerator.LocalFiles, Does.Contain("src/PhialeTech/Shared/PhialeTech.WebAssets/Assets/ReportDesigner/vendor/qrcode-generator.MIT-LICENSE.txt"));
            });
        }

        [Test]
        public void ViewModel_ShouldExposeApplicationStateManagerScenario_WithSharedManagerConceptAndResolvedBaseDefinition()
        {
            using var services = DemoApplicationServices.CreateIsolatedForWindow();
            var viewModel = new DemoShellViewModel("Wpf", definitionManager: services.DefinitionManager);

            viewModel.SelectExample("application-state-manager");

            Assert.Multiple(() =>
            {
                Assert.That(viewModel.SelectedComponentText, Is.EqualTo("ApplicationStateManager"));
                Assert.That(viewModel.IsApplicationStateManagerExample, Is.True);
                Assert.That(viewModel.ShowApplicationStateManagerSurface, Is.True);
                Assert.That(viewModel.ApplicationStateManagerStateKey, Is.EqualTo("Demo/Grid/application-state-manager"));
                Assert.That(viewModel.ApplicationStateManagerResponsibilities.Count, Is.EqualTo(3));
                Assert.That(viewModel.ApplicationStateManagerPageDefinition.DefinitionKey, Is.EqualTo("demo.application-state-manager"));
                Assert.That(viewModel.ApplicationStateManagerPageDefinition.SourceId, Is.EqualTo("demo-local"));
                Assert.That(viewModel.ApplicationStateManagerSampleDefinition.DefinitionKey, Is.EqualTo("demo.grid.grouping"));
                Assert.That(viewModel.ApplicationStateManagerSampleDefinition.ConsumerHint, Does.Contain("demo.grid.grouping"));
            });
        }

        [Test]
        public void ViewModel_ShouldExposeDefinitionManagerScenario_WithResolvedDefinitions()
        {
            using var services = DemoApplicationServices.CreateIsolatedForWindow();
            var viewModel = new DemoShellViewModel("Wpf", definitionManager: services.DefinitionManager);

            viewModel.SelectExample("definition-manager");

            Assert.Multiple(() =>
            {
                Assert.That(viewModel.SelectedComponentText, Is.EqualTo("DefinitionManager"));
                Assert.That(viewModel.DefinitionManagerPageDefinition.DefinitionKey, Is.EqualTo("demo.definition-manager"));
                Assert.That(viewModel.DefinitionManagerPageDefinition.SourceId, Is.EqualTo("demo-local"));
                Assert.That(viewModel.DefinitionManagerPageDefinition.Title, Is.EqualTo("DefinitionManager"));
                Assert.That(viewModel.DefinitionManagerSampleDefinition.DefinitionKey, Is.EqualTo("demo.grid.grouping"));
                Assert.That(viewModel.DefinitionManagerSampleDefinition.Title, Is.EqualTo("Grouping"));
                Assert.That(viewModel.DefinitionManagerSampleDefinition.Fields.Select(field => field.Label), Does.Contain("default.group"));
                Assert.That(viewModel.DefinitionManagerSampleDefinition.ConsumerHint, Does.Contain("demo.grid.grouping"));
            });
        }

        [Test]
        public void ViewModel_ShouldSwitchLocalizedTextsWhenLanguageChanges()
        {
            var viewModel = new DemoShellViewModel("Avalonia");

            viewModel.LanguageCode = "pl";

            Assert.That(viewModel.OverviewTitle, Is.EqualTo("Wybierz przyklad"));
            Assert.That(viewModel.GridComponentText, Is.EqualTo("Grid"));
            Assert.That(viewModel.MetricCards.First().Title, Is.EqualTo("Wszystkie obiekty"));
            Assert.That(viewModel.GridColumns.First().Header, Is.EqualTo("Kategoria"));
        }

        [Test]
        public void ViewModel_ShouldExposeThemeOptionsAndKeepSelectedThemeCode()
        {
            var viewModel = new DemoShellViewModel("Wpf");

            Assert.That(viewModel.SelectedThemeCode, Is.EqualTo("system"));
            Assert.That(viewModel.ThemeOptions.Select(option => option.Code), Is.EquivalentTo(new[] { "system", "day", "night" }));
            Assert.That(viewModel.ThemeLabelText, Is.EqualTo("Theme"));

            viewModel.SelectedThemeCode = "night";
            Assert.That(viewModel.SelectedThemeCode, Is.EqualTo("night"));

            viewModel.LanguageCode = "pl";
            Assert.That(viewModel.ThemeLabelText, Is.EqualTo("Motyw"));
            Assert.That(viewModel.ThemeOptions.Single(option => option.Code == "night").DisplayName, Is.EqualTo("Noc"));
        }

        [Test]
        public void ViewModel_WhenFoundationsScenarioIsSelected_ShouldExposeDedicatedSurfaceData()
        {
            var viewModel = new DemoShellViewModel("Wpf");

            viewModel.SelectExample("foundations");

            Assert.Multiple(() =>
            {
                Assert.That(viewModel.IsFoundationsExample, Is.True);
                Assert.That(viewModel.ShowFoundationsSurface, Is.True);
                Assert.That(viewModel.ShowGridSurface, Is.False);
                Assert.That(viewModel.HasDemoToolbar, Is.False);
                Assert.That(viewModel.PreviewHintText, Does.Contain("foundations").IgnoreCase);
                Assert.That(viewModel.DetailHeadline, Is.EqualTo("Design Foundations"));
                Assert.That(viewModel.FoundationsHighlights.Count, Is.GreaterThanOrEqualTo(4));
                Assert.That(viewModel.FoundationsTypographyTokens.Select(token => token.TokenName), Does.Contain("Text.Hero"));
                Assert.That(viewModel.FoundationsSurfaceTokens.Select(token => token.TokenName), Does.Contain("DemoPanelBackgroundBrush"));
                Assert.That(viewModel.FoundationsShapeTokens.Select(token => token.TokenName), Does.Contain("Radius.14"));
            });
        }

        [Test]
        public void ViewModel_WhenFoundationsScenarioIsSelected_ShouldExposeCalendarControlTitleTypographyToken()
        {
            var viewModel = new DemoShellViewModel("Wpf");

            viewModel.SelectExample("foundations");

            var token = viewModel.FoundationsTypographyTokens.Single(foundationsToken => foundationsToken.TokenName == "Text.ControlTitle");

            Assert.Multiple(() =>
            {
                Assert.That(token.Role, Is.EqualTo("Control title / compact header"));
                Assert.That(token.Usage, Does.Contain("calendar month captions"));
                Assert.That(token.FontFamilyName, Is.EqualTo("Bahnschrift SemiBold"));
                Assert.That(token.FontSize, Is.EqualTo(22d));
                Assert.That(token.SampleText, Is.EqualTo("April 2026"));
            });
        }

        [Test]
        public void ViewModel_WhenFoundationsScenarioIsSelected_ShouldExposeCalendarControlLabelTypographyToken()
        {
            var viewModel = new DemoShellViewModel("Wpf");

            viewModel.SelectExample("foundations");

            var token = viewModel.FoundationsTypographyTokens.Single(foundationsToken => foundationsToken.TokenName == "Text.ControlLabel");

            Assert.Multiple(() =>
            {
                Assert.That(token.Role, Is.EqualTo("Control label / choice"));
                Assert.That(token.Usage, Does.Contain("day values"));
                Assert.That(token.FontFamilyName, Is.EqualTo("Bahnschrift SemiBold"));
                Assert.That(token.FontSize, Is.EqualTo(15d));
            });
        }

        [Test]
        public void ViewModel_ShouldSeedGroupingScenarioWithCategoryGroup()
        {
            var viewModel = new DemoShellViewModel("Wpf");

            viewModel.SelectExample("grouping");

            Assert.That(viewModel.GridGroups.Count, Is.EqualTo(1));
            Assert.That(viewModel.GridGroups[0].ColumnId, Is.EqualTo("Category"));
        }

        [Test]
        public void ViewModel_ShouldClearSeedGroupsForFilteringScenario()
        {
            var viewModel = new DemoShellViewModel("Wpf");

            viewModel.SelectExample("grouping");
            viewModel.SelectExample("filtering");

            Assert.That(viewModel.GridGroups, Is.Empty);
        }

        [Test]
        public void ViewModel_ShouldExposeDedicatedFilteringTools_AndKeepSearchToolbarForPersonalizationOnly()
        {
            var viewModel = new DemoShellViewModel("Wpf");

            viewModel.SelectExample("filtering");

            Assert.Multiple(() =>
            {
                Assert.That(viewModel.ShowFilteringTools, Is.True);
                Assert.That(viewModel.ShowSearchTools, Is.False);
            });

            viewModel.SelectExample("personalization");

            Assert.Multiple(() =>
            {
                Assert.That(viewModel.ShowFilteringTools, Is.False);
                Assert.That(viewModel.ShowSearchTools, Is.True);
            });
        }

        [Test]
        public void ViewModel_ShouldSeedSortingScenarioWithCategoryAndInspectionSorts()
        {
            var viewModel = new DemoShellViewModel("Wpf");

            viewModel.SelectExample("sorting");

            Assert.That(viewModel.GridSorts.Count, Is.EqualTo(2));
            Assert.That(viewModel.GridSorts[0].ColumnId, Is.EqualTo("Category"));
            Assert.That(viewModel.GridSorts[1].ColumnId, Is.EqualTo("LastInspection"));
            Assert.That(viewModel.GridSorts[1].Direction.ToString(), Is.EqualTo("Descending"));
        }

        [Test]
        public void ViewModel_ShouldSeedSummariesScenario()
        {
            var viewModel = new DemoShellViewModel("Wpf");

            viewModel.SelectExample("summaries");

            Assert.That(viewModel.GridSummaries.Count, Is.EqualTo(3));
            Assert.That(viewModel.GridSummaries.Select(summary => summary.ColumnId), Is.EquivalentTo(new[] { "AreaSquareMeters", "LengthMeters", "ObjectId" }));
        }

        [Test]
        public void ViewModel_ShouldEnableEditingScenario()
        {
            var viewModel = new DemoShellViewModel("Wpf");

            viewModel.SelectExample("editing");

            Assert.That(viewModel.IsGridReadOnly, Is.False);
            Assert.That(viewModel.ShowEditingTools, Is.True);
            Assert.That(viewModel.GridColumns.Single(column => column.Id == "Category").IsEditable, Is.False);
            Assert.That(viewModel.GridColumns.Single(column => column.Id == "ObjectId").IsVisible, Is.False);
            Assert.That(viewModel.GridColumns.Single(column => column.Id == "ObjectName").IsEditable, Is.True);
            Assert.That(viewModel.GridColumns.Single(column => column.Id == "Owner").IsEditable, Is.True);
            Assert.That(viewModel.GridColumns.Single(column => column.Id == "Status").EditorKind, Is.EqualTo(GridColumnEditorKind.Combo));
            Assert.That(viewModel.GridColumns.Single(column => column.Id == "Priority").EditorKind, Is.EqualTo(GridColumnEditorKind.Combo));
            Assert.That(viewModel.GridColumns.Single(column => column.Id == "LastInspection").EditorKind, Is.EqualTo(GridColumnEditorKind.DatePicker));
            Assert.That(viewModel.GridColumns.Single(column => column.Id == "Owner").EditorKind, Is.EqualTo(GridColumnEditorKind.Autocomplete));
            Assert.That(viewModel.GridColumns.Single(column => column.Id == "ScaleHint").EditorKind, Is.EqualTo(GridColumnEditorKind.MaskedText));
            Assert.That(viewModel.GridColumns.Single(column => column.Id == "ScaleHint").IsVisible, Is.True);
        }

        [Test]
        public void ViewModel_SelectExample_ShouldBatchGridEditSessionContextStateChange()
        {
            var viewModel = new DemoShellViewModel("Wpf");
            var stateChangedCount = 0;
            viewModel.GridEditSessionContext.StateChanged += (sender, args) => stateChangedCount++;

            viewModel.SelectExample("editing");

            Assert.Multiple(() =>
            {
                Assert.That(stateChangedCount, Is.EqualTo(1));
                Assert.That(viewModel.GridEditSessionContext.FieldDefinitions.Count, Is.GreaterThan(0));
                Assert.That(viewModel.GridEditSessionContext.Records.Count, Is.EqualTo(viewModel.GridRecords.Count));
            });
        }

        [Test]
        public void ViewModel_ShouldExposeRichEditorsScenario_WithEditingToolsAndRichColumnMetadata()
        {
            var viewModel = new DemoShellViewModel("Wpf");

            viewModel.SelectExample("rich-editors");

            Assert.Multiple(() =>
            {
                Assert.That(viewModel.ShowEditingTools, Is.True);
                Assert.That(viewModel.IsGridReadOnly, Is.False);
                Assert.That(viewModel.SelectedExampleTitle, Is.EqualTo("Rich Editors"));
                Assert.That(viewModel.GridColumns.Single(column => column.Id == "Status").EditorKind, Is.EqualTo(GridColumnEditorKind.Combo));
                Assert.That(viewModel.GridColumns.Single(column => column.Id == "LastInspection").EditorKind, Is.EqualTo(GridColumnEditorKind.DatePicker));
                Assert.That(viewModel.GridColumns.Single(column => column.Id == "Owner").EditorKind, Is.EqualTo(GridColumnEditorKind.Autocomplete));
                Assert.That(viewModel.GridColumns.Single(column => column.Id == "ScaleHint").EditorKind, Is.EqualTo(GridColumnEditorKind.MaskedText));
            });
        }

        [Test]
        public void ViewModel_ShouldExposeSummaryDesignerOptions_AndMutateGridSummaries()
        {
            var viewModel = new DemoShellViewModel("Wpf");

            viewModel.SelectExample("summaries");

            Assert.Multiple(() =>
            {
                Assert.That(viewModel.ShowSummaryDesignerTools, Is.True);
                Assert.That(viewModel.AvailableSummaryColumns.Count, Is.GreaterThan(0));
                Assert.That(viewModel.AvailableSummaryTypes.Count, Is.GreaterThan(0));
                Assert.That(viewModel.ConfiguredSummaries.Count, Is.EqualTo(3));
            });

            viewModel.SelectedSummaryColumn = viewModel.AvailableSummaryColumns.Single(option => option.ColumnId == "Priority");
            viewModel.SelectedSummaryType = viewModel.AvailableSummaryTypes.Single(option => option.Type == GridSummaryType.Min);
            viewModel.AddSelectedSummary();

            Assert.That(viewModel.GridSummaries.Any(summary => summary.ColumnId == "Priority" && summary.Type == GridSummaryType.Min), Is.True);
            Assert.That(viewModel.ConfiguredSummaries.Any(summary => summary.ColumnId == "Priority" && summary.Type == GridSummaryType.Min), Is.True);

            viewModel.RemoveSummary("Priority", GridSummaryType.Min);
            Assert.That(viewModel.GridSummaries.Any(summary => summary.ColumnId == "Priority" && summary.Type == GridSummaryType.Min), Is.False);

            viewModel.ResetSummaries();
            Assert.That(viewModel.GridSummaries.Select(summary => summary.ColumnId), Is.EquivalentTo(new[] { "AreaSquareMeters", "LengthMeters", "ObjectId" }));
        }

        [Test]
        public void ViewModel_ShouldExposeSummaryDesignerScenario_WithDesignerTools()
        {
            var viewModel = new DemoShellViewModel("Wpf");

            viewModel.SelectExample("summary-designer");

            Assert.Multiple(() =>
            {
                Assert.That(viewModel.ShowSummaryDesignerTools, Is.True);
                Assert.That(viewModel.SelectedExampleTitle, Is.EqualTo("Summary Designer"));
                Assert.That(viewModel.AvailableSummaryColumns.Count, Is.GreaterThan(0));
                Assert.That(viewModel.ConfiguredSummaries.Count, Is.EqualTo(3));
            });
        }

        [Test]
        public async Task HierarchyBuilder_ShouldBuildMunicipalityRootsAndPageChildren()
        {
            var hierarchy = DemoGisHierarchyBuilder.Build(new DemoFeatureCatalog(), pageSize: 5);
            var firstRoot = hierarchy.Roots.First();

            await hierarchy.Controller.ExpandAsync(firstRoot);

            Assert.Multiple(() =>
            {
                Assert.That(hierarchy.Roots.Count, Is.GreaterThan(1));
                Assert.That(firstRoot.IsExpanded, Is.True);
                Assert.That(firstRoot.Children.Count, Is.EqualTo(5));
                Assert.That(firstRoot.HasMoreChildren, Is.True);
                Assert.That(firstRoot.Children.All(child => child.Item is DemoGisRecordViewModel), Is.True);
            });

            await hierarchy.Controller.LoadNextChildrenPageAsync(firstRoot);

            Assert.Multiple(() =>
            {
                Assert.That(firstRoot.Children.Count, Is.EqualTo(10));
                Assert.That(hierarchy.Controller.Flatten(hierarchy.Roots).Count, Is.GreaterThan(hierarchy.Roots.Count));
            });
        }

        [Test]
        public void ViewModel_ShouldExposeHierarchyScenarioWithRootsAndToolbar()
        {
            var viewModel = new DemoShellViewModel("Wpf");

            viewModel.SelectExample("hierarchy");

            Assert.Multiple(() =>
            {
                Assert.That(viewModel.ShowHierarchyTools, Is.True);
                Assert.That(viewModel.GridHierarchyController, Is.Not.Null);
                Assert.That(viewModel.GridHierarchyRoots.Count, Is.GreaterThan(1));
                Assert.That(viewModel.GridHierarchyRoots.All(root => root.CanExpand), Is.True);
            });
        }

        [Test]
        public async Task MasterDetailBuilder_ShouldBuildCategoryRootsAndPageChildren()
        {
            var masterDetail = DemoGisMasterDetailBuilder.Build(new DemoFeatureCatalog(), pageSize: 4);
            var firstRoot = masterDetail.Roots.First();

            await masterDetail.Controller.ExpandAsync(firstRoot);

            Assert.Multiple(() =>
            {
                Assert.That(masterDetail.Roots.Count, Is.GreaterThan(1));
                Assert.That(firstRoot.Item, Is.InstanceOf<System.Collections.Generic.IDictionary<string, object>>());
                Assert.That(((System.Collections.Generic.IDictionary<string, object>)firstRoot.Item)["Description"], Is.Not.EqualTo(string.Empty));
                Assert.That(firstRoot.Children.Count, Is.EqualTo(4));
                Assert.That(firstRoot.Children.All(child => child.Item is DemoGisRecordViewModel), Is.True);
                Assert.That(firstRoot.HasMoreChildren, Is.True);
            });
        }

        [Test]
        public void ViewModel_ShouldExposeMasterDetailScenarioWithRootsAndToolbar()
        {
            var viewModel = new DemoShellViewModel("Wpf");

            viewModel.SelectExample("master-detail");

            Assert.Multiple(() =>
            {
                Assert.That(viewModel.ShowHierarchyTools, Is.True);
                Assert.That(viewModel.IsMasterDetailExample, Is.True);
                Assert.That(viewModel.ShowMasterDetailPlacementToggle, Is.True);
                Assert.That(viewModel.IsMasterDetailOutside, Is.False);
                Assert.That(viewModel.MasterDetailPlacementText, Is.EqualTo("Show detail fields outside"));
                Assert.That(viewModel.GridHierarchyController, Is.Not.Null);
                Assert.That(viewModel.GridHierarchyRoots.Count, Is.GreaterThan(1));
                Assert.That(viewModel.GridColumns.Select(column => column.Id), Is.EqualTo(new[] { "Category", "Description", "ObjectName", "ObjectId", "GeometryType", "Status" }));
            });
        }

        [Test]
        public void ViewModel_ShouldToggleMasterDetailPlacementTextAndState()
        {
            var viewModel = new DemoShellViewModel("Wpf");

            viewModel.SelectExample("master-detail");
            viewModel.ToggleMasterDetailPlacement();

            Assert.Multiple(() =>
            {
                Assert.That(viewModel.IsMasterDetailOutside, Is.True);
                Assert.That(viewModel.MasterDetailPlacementText, Is.EqualTo("Show detail fields inside only"));
            });

            viewModel.ToggleMasterDetailPlacement();

            Assert.Multiple(() =>
            {
                Assert.That(viewModel.IsMasterDetailOutside, Is.False);
                Assert.That(viewModel.MasterDetailPlacementText, Is.EqualTo("Show detail fields outside"));
            });
        }

        [Test]
        public void ViewModel_ShouldExposePersonalizationScenario_WithChooserAndSavedViewState()
        {
            var viewModel = new DemoShellViewModel("Wpf");

            viewModel.SelectExample("personalization");
            viewModel.SetColumnChooserVisibility("Owner", false);
            viewModel.SetSavedViewNames(new[] { "Night", "Day" });

            Assert.Multiple(() =>
            {
                Assert.That(viewModel.ShowPersonalizationTools, Is.True);
                Assert.That(viewModel.ShowSearchTools, Is.True);
                Assert.That(viewModel.ShowLayoutTools, Is.True);
                Assert.That(viewModel.ColumnChooserItems.Count, Is.EqualTo(viewModel.GridColumns.Count));
                Assert.That(viewModel.ColumnChooserItems.Single(item => item.ColumnId == "Owner").IsVisible, Is.False);
                Assert.That(viewModel.SavedViewNames, Is.EqualTo(new[] { "Day", "Night" }));
                Assert.That(viewModel.SelectedSavedViewName, Is.EqualTo("Day"));
            });
        }

        [Test]
        public void ViewModel_WhenGridComponentIsSelected_ShouldBuildOverviewWithoutActiveLayerSelectorCard()
        {
            var viewModel = new DemoShellViewModel("Wpf");

            viewModel.SelectComponent("grid");
            viewModel.ShowOverview();

            var visibleExampleIds = viewModel.VisibleSections
                .SelectMany(section => section.Examples)
                .Select(example => example.Id)
                .ToArray();

            Assert.Multiple(() =>
            {
                Assert.That(visibleExampleIds, Does.Contain("grouping"));
                Assert.That(visibleExampleIds, Does.Not.Contain("active-layer-selector"));
            });
        }

        [Test]
        public void GisRecord_ShouldKeepStableIdWhenObjectNameChanges()
        {
            var record = new DemoGisRecordViewModel("OBJ-1", "Parcel", "Original", "Polygon", "EPSG:2180", "Krakow", "Stare Miasto", "Active", 1m, 2m, new DateTime(2025, 1, 1), "CityGIS", "High", true, true, "Municipality", 1000, "cadastre");
            var originalId = record.Id;

            record.ObjectName = "Changed";

            Assert.That(record.Id, Is.EqualTo(originalId));
        }

        [Test]
        public void ViewModel_ShouldExposeStateAndLayoutToolsForStateScenario()
        {
            var viewModel = new DemoShellViewModel("Wpf");

            viewModel.SelectExample("state-persistence");

            Assert.That(viewModel.ShowStateTools, Is.True);
            Assert.That(viewModel.ShowLayoutTools, Is.True);
        }

        [Test]
        public async Task ViewModel_ShouldPageAndRefreshRemoteScenario()
        {
            var viewModel = new DemoShellViewModel("Wpf");

            viewModel.SelectExample("remote-data");
            var firstPageFirstId = viewModel.GridRecords.First().ObjectId;

            Assert.That(viewModel.ShowRemoteTools, Is.True);
            Assert.That(viewModel.GridRecords.Count, Is.EqualTo(20));
            Assert.That(viewModel.CanMoveNextRemotePage, Is.True);

            await viewModel.LoadNextRemotePageAsync();
            var secondPageFirstId = viewModel.GridRecords.First().ObjectId;
            var beforeRefreshInspection = viewModel.GridRecords.First().LastInspection;

            await viewModel.RefreshRemotePageAsync();

            Assert.That(secondPageFirstId, Is.Not.EqualTo(firstPageFirstId));
            Assert.That(viewModel.GridRecords.First().LastInspection, Is.GreaterThan(beforeRefreshInspection));
            Assert.That(viewModel.RemoteStatusText, Does.Contain("2"));
        }

        [Test]
        public void FeatureCatalog_ShouldReturnClonedGisRecords()
        {
            var catalog = new DemoFeatureCatalog();

            var first = catalog.GetGisRecords();
            var second = catalog.GetGisRecords();
            first[0].ObjectName = "Changed";

            Assert.That(second[0].ObjectName, Is.Not.EqualTo("Changed"));
        }

        [Test]
        public void FeatureCatalog_ShouldLoadEmbeddedGisRecords()
        {
            var catalog = new DemoFeatureCatalog();

            var records = catalog.GetGisRecords();

            Assert.That(records.Count, Is.EqualTo(530));
            Assert.That(records.First().ObjectId, Is.EqualTo("DZ-KRA-STA-0001"));
            Assert.That(records.First().Category, Is.EqualTo("Parcel"));
        }

        [Test]
        public void FeatureCatalog_ShouldDistributeExamplesAcrossRealSections()
        {
            var catalog = new DemoFeatureCatalog();

            var sections = catalog.BuildSections("en", string.Empty);

            Assert.That(sections.Select(section => section.Title), Is.EquivalentTo(new[]
            {
                "Overview",
                "Customization",
                "Editing",
                "Aggregates",
                "Layout",
                "Inputs",
                "Enterprise Inputs",
                "Data Components",
                "Presentation",
                "Actions",
                "Validation / States",
                "Extensibility",
            }));
            Assert.That(sections.Single(section => section.Title == "Aggregates").Examples.Select(example => example.Id), Is.EquivalentTo(new[] { "summaries", "summary-designer" }));
        }

        [Test]
        public void FeatureCatalog_ShouldBuildDeterministicOverviewRows_WithThreeCardsPerRow()
        {
            var catalog = new DemoFeatureCatalog();

            var overviewSection = catalog.BuildSections("en", string.Empty)
                .Single(section => section.Title == "Overview");

            Assert.Multiple(() =>
            {
                Assert.That(overviewSection.Rows.Count, Is.EqualTo(3));
                Assert.That(overviewSection.Rows[0].Examples.Select(example => example.Id), Is.EqualTo(new[] { "foundations", "web-host", "pdf-viewer" }));
                Assert.That(overviewSection.Rows[1].Examples.Select(example => example.Id), Is.EqualTo(new[] { "report-designer", "monaco-editor", "my-license" }));
                Assert.That(overviewSection.Rows[2].Examples.Select(example => example.Id), Is.EqualTo(new[] { "third-party-licenses" }));
            });
        }

        [Test]
        public void FeatureCatalog_ShouldExposeDedicatedExamples_ForRichEditorsAndSummaryDesigner()
        {
            var catalog = new DemoFeatureCatalog();

            var examples = catalog.GetExamples();

            Assert.That(examples.Any(example => example.Id == "rich-editors"), Is.True);
            Assert.That(examples.Any(example => example.Id == "summary-designer"), Is.True);
        }

        [Test]
        public void FeatureCatalog_ShouldGenerateDedicatedCodeSamples_ForRichEditorsAndSummaryDesigner()
        {
            var catalog = new DemoFeatureCatalog();

            var richEditorFiles = catalog.GetCodeFiles("Wpf", "rich-editors");
            var summaryDesignerFiles = catalog.GetCodeFiles("Wpf", "summary-designer");

            Assert.That(richEditorFiles.Any(file => file.Text.Contains("GridColumnEditorKind.Combo")), Is.True);
            Assert.That(richEditorFiles.Any(file => file.Text.Contains("GridColumnEditorKind.DatePicker")), Is.True);
            Assert.That(richEditorFiles.Any(file => file.Text.Contains("GridColumnEditorKind.Autocomplete")), Is.True);
            Assert.That(richEditorFiles.Any(file => file.Text.Contains("GridColumnEditorKind.MaskedText")), Is.True);
            Assert.That(summaryDesignerFiles.Any(file => file.Text.Contains("HandleAddSummaryClick")), Is.True);
            Assert.That(summaryDesignerFiles.Any(file => file.Text.Contains("AvailableSummaryColumns")), Is.True);
        }

        [Test]
        public void DemoCodeFileViewModel_ShouldRenderFileNameWhenConvertedToString()
        {
            var file = new DemoCodeFileViewModel("ExampleViewModel.cs", "public sealed class ExampleViewModel {}");

            Assert.That(file.ToString(), Is.EqualTo("ExampleViewModel.cs"));
        }

        [Test]
        public void FeatureCatalog_ShouldLoadPlatformSpecificCodeSamples()
        {
            var catalog = new DemoFeatureCatalog();

            var wpfFiles = catalog.GetCodeFiles("Wpf", "summaries");
            var winUiFiles = catalog.GetCodeFiles("WinUI", "sorting");

            Assert.That(wpfFiles.Select(file => file.FileName), Does.Contain("Example.xaml"));
            Assert.That(wpfFiles.Any(file => file.Text.Contains("Summaries=\"{Binding GridSummaries}\"")), Is.True);
            Assert.That(wpfFiles.Any(file => file.Text.Contains("new GridSummaryDescriptor(\"AreaSquareMeters\", GridSummaryType.Sum")), Is.True);
            Assert.That(winUiFiles.Select(file => file.FileName), Does.Contain("Example.xaml"));
            Assert.That(winUiFiles.Any(file => file.Text.Contains("using:PhialeTech.PhialeGrid.WinUI.Controls")), Is.True);
            Assert.That(winUiFiles.Any(file => file.Text.Contains("Sorts=\"{Binding GridSorts, Mode=TwoWay}\"")), Is.True);
        }

        [Test]
        public void FeatureCatalog_ShouldRenderGroupingCodeSamplesWithReadableNamesAndFullScenarioContext()
        {
            var catalog = new DemoFeatureCatalog();

            var files = catalog.GetCodeFiles("Wpf", "grouping");
            var markup = files.Single(file => file.FileName == "Example.xaml");
            var viewModel = files.Single(file => file.FileName == "ExampleViewModel.cs");
            var host = files.Single(file => file.FileName == "ExampleHost.xaml.cs");

            Assert.Multiple(() =>
            {
                Assert.That(files.All(file => !string.IsNullOrWhiteSpace(file.FileName)), Is.True);
                Assert.That(files.All(file => !string.IsNullOrWhiteSpace(file.Text)), Is.True);
                Assert.That(markup.Text, Does.Contain("x:Class=\"Demo.Snippets.ExampleHost\""));
                Assert.That(markup.Text, Does.Contain("Groups=\"{Binding GridGroups, Mode=TwoWay}\""));
                Assert.That(viewModel.Text, Does.Contain("namespace Demo.Snippets"));
                Assert.That(viewModel.Text, Does.Contain("new GridGroupDescriptor(\"Category\", GridSortDirection.Ascending)"));
                Assert.That(viewModel.Text.Length, Is.GreaterThan(1200));
                Assert.That(host.Text, Does.Contain("public sealed partial class ExampleHost : UserControl"));
                Assert.That(host.Text, Does.Contain("public GroupingExampleViewModel ViewModel { get; } = new GroupingExampleViewModel();"));
                Assert.That(host.Text, Does.Not.Contain("ConfiguredSummaryViewModel"));
            });
        }

        [Test]
        public void FeatureCatalog_ShouldRenderFoundationsCodeSamplesWithDedicatedSurfaceSnippet()
        {
            var catalog = new DemoFeatureCatalog();

            var files = catalog.GetCodeFiles("Wpf", "foundations");
            var markup = files.Single(file => file.FileName == "Example.xaml");
            var viewModel = files.Single(file => file.FileName == "ExampleViewModel.cs");
            var host = files.Single(file => file.FileName == "ExampleHost.xaml.cs");

            Assert.Multiple(() =>
            {
                Assert.That(files.Select(file => file.FileName), Is.EquivalentTo(new[] { "Example.xaml", "ExampleViewModel.cs", "ExampleHost.xaml.cs" }));
                Assert.That(markup.Text, Does.Contain("TypographyTokens"));
                Assert.That(markup.Text, Does.Contain("SurfaceTokens"));
                Assert.That(markup.Text, Does.Not.Contain("grid:PhialeGrid"));
                Assert.That(viewModel.Text, Does.Contain("Text.Hero"));
                Assert.That(viewModel.Text, Does.Contain("DemoPanelBackgroundBrush"));
                Assert.That(viewModel.Text, Does.Contain("Consolas"));
                Assert.That(host.Text, Does.Contain("public FoundationsExampleViewModel ViewModel { get; } = new FoundationsExampleViewModel();"));
            });
        }

        [Test]
        public void FeatureCatalog_ShouldIncludeHostActionsForInteractiveExamples()
        {
            var catalog = new DemoFeatureCatalog();

            var stateFiles = catalog.GetCodeFiles("Wpf", "state-persistence");
            var layoutFiles = catalog.GetCodeFiles("Wpf", "column-layout");
            var editingFiles = catalog.GetCodeFiles("Wpf", "editing");
            var filteringFiles = catalog.GetCodeFiles("Wpf", "filtering");
            var summaryFiles = catalog.GetCodeFiles("Wpf", "summaries");

            Assert.That(stateFiles.Select(file => file.FileName), Does.Contain("ExampleHost.xaml.cs"));
            Assert.That(stateFiles.Select(file => file.FileName), Does.Contain("App.xaml.cs"));
            Assert.That(stateFiles.Any(file => file.Text.Contains("ApplicationStateManager")), Is.True);
            Assert.That(stateFiles.Any(file => file.Text.Contains("JsonApplicationStateStore")), Is.True);
            Assert.That(stateFiles.Any(file => file.Text.Contains("Register(GridStateKey, _gridStateComponent)")), Is.True);
            Assert.That(stateFiles.Any(file => file.Text.Contains("SaveRegisteredState(GridStateKey)")), Is.True);
            Assert.That(stateFiles.Any(file => file.Text.Contains("TryRestoreRegisteredState(GridStateKey)")), Is.True);
            Assert.That(layoutFiles.Any(file => file.Text.Contains("Use the grid options menu for column visibility")), Is.True);
            Assert.That(layoutFiles.Any(file => file.Text.Contains("DemoGrid.SetColumnVisibility(\"Owner\"")), Is.False);
            Assert.That(layoutFiles.Any(file => file.Text.Contains("DemoGrid.SetColumnFrozen(\"ObjectName\"")), Is.False);
            Assert.That(layoutFiles.Any(file => file.Text.Contains("DemoGrid.AutoFitVisibleColumns()")), Is.False);
            Assert.That(editingFiles.Any(file => file.Text.Contains("IsGridReadOnly=\"{Binding IsGridReadOnly}\"")), Is.True);
            Assert.That(editingFiles.Any(file => file.Text.Contains("isVisible: false")), Is.True);
            Assert.That(editingFiles.Any(file => file.Text.Contains("editorKind: GridColumnEditorKind.Combo")), Is.True);
            Assert.That(editingFiles.Any(file => file.Text.Contains("editorKind: GridColumnEditorKind.DatePicker")), Is.True);
            Assert.That(editingFiles.Any(file => file.Text.Contains("editorKind: GridColumnEditorKind.Autocomplete")), Is.True);
            Assert.That(editingFiles.Any(file => file.Text.Contains("editorKind: GridColumnEditorKind.MaskedText")), Is.True);
            Assert.That(editingFiles.Any(file => file.Text.Contains("DemoGrid.CancelEdits()")), Is.True);
            Assert.That(filteringFiles.Any(file => file.Text.Contains("HandleFocusMunicipalityFilterClick")), Is.True);
            Assert.That(filteringFiles.Any(file => file.Text.Contains("DemoGrid.FocusColumnFilter(\"Municipality\")")), Is.True);
            Assert.That(filteringFiles.Any(file => file.Text.Contains("DemoGrid.ClearFilters()")), Is.True);
            Assert.That(filteringFiles.Any(file => file.Text.Contains("MinHeight=\"620\"")), Is.False);
            Assert.That(summaryFiles.Any(file => file.Text.Contains("HandleAddSummaryClick")), Is.True);
            Assert.That(summaryFiles.Any(file => file.Text.Contains("AvailableSummaryColumns")), Is.True);
            Assert.That(summaryFiles.Any(file => file.Text.Contains("DemoConfiguredSummaryViewModel")), Is.True);
        }

        [Test]
        public void FeatureCatalog_ShouldRenderDefinitionManagerCodeSamplesWithSharedDefinitionManagerWiring()
        {
            var catalog = new DemoFeatureCatalog();

            var files = catalog.GetCodeFiles("Wpf", "definition-manager");

            Assert.Multiple(() =>
            {
                Assert.That(files.Select(file => file.FileName), Is.EquivalentTo(new[] { "Example.xaml", "ExampleViewModel.cs", "ExampleDefinition.cs", "App.xaml.cs" }));
                Assert.That(files.Any(file => file.Text.Contains("DefinitionManager")), Is.True);
                Assert.That(files.Any(file => file.Text.Contains("InMemoryDefinitionSource")), Is.True);
                Assert.That(files.Any(file => file.Text.Contains("demo.grid.grouping")), Is.True);
                Assert.That(files.Any(file => file.Text.Contains("ApplicationStateManager")), Is.True);
            });
        }

        [Test]
        public void FeatureCatalog_ShouldRenderApplicationStateManagerCodeSamplesWithSharedStateWiring()
        {
            var catalog = new DemoFeatureCatalog();

            var files = catalog.GetCodeFiles("Wpf", "application-state-manager");

            Assert.Multiple(() =>
            {
                Assert.That(files.Select(file => file.FileName), Is.EquivalentTo(new[] { "Example.xaml", "ExampleViewModel.cs", "ExampleHost.xaml.cs", "App.xaml.cs" }));
                Assert.That(files.Any(file => file.Text.Contains("ApplicationStateManager")), Is.True);
                Assert.That(files.Any(file => file.Text.Contains("JsonApplicationStateStore")), Is.True);
                Assert.That(files.Any(file => file.Text.Contains("GridStateKey")), Is.True);
                Assert.That(files.Any(file => file.Text.Contains("Register(GridStateKey, _gridStateComponent)")), Is.True);
            });
        }

        [Test]
        public void FeatureCatalog_ShouldRenderRemoteCodeSamplesAgainstHttpClientInsteadOfLocalSimulation()
        {
            var catalog = new DemoFeatureCatalog();

            var remoteFiles = catalog.GetCodeFiles("Wpf", "remote-data");

            Assert.That(remoteFiles.Any(file => file.Text.Contains("DemoRemoteGridHttpClient")), Is.True);
            Assert.That(remoteFiles.Any(file => file.Text.Contains("new DemoRemoteQueryRequest(")), Is.True);
            Assert.That(remoteFiles.Any(file => file.Text.Contains("Sorts=\"{Binding GridSorts, Mode=TwoWay}\"")), Is.True);
            Assert.That(remoteFiles.Any(file => file.Text.Contains("Task.Delay(320)")), Is.False);
            Assert.That(remoteFiles.Any(file => file.Text.Contains("BuildPage(")), Is.False);
        }

        [Test]
        public void FeatureCatalog_ShouldRenderHierarchyCodeSamplesWithRealHostWiring()
        {
            var catalog = new DemoFeatureCatalog();

            var hierarchyFiles = catalog.GetCodeFiles("Wpf", "hierarchy");

            Assert.That(hierarchyFiles.Any(file => file.Text.Contains("DemoGisHierarchyBuilder.Build")), Is.True);
            Assert.That(hierarchyFiles.Any(file => file.Text.Contains("GridHierarchyController<object>")), Is.True);
            Assert.That(hierarchyFiles.Any(file => file.Text.Contains("DemoGrid.SetHierarchySource(ViewModel.GridHierarchyRoots, ViewModel.GridHierarchyController)")), Is.True);
            Assert.That(hierarchyFiles.Any(file => file.Text.Contains("ExpandAllHierarchyAsync")), Is.True);
        }

        [Test]
        public void FeatureCatalog_ShouldRenderMasterDetailCodeSamplesWithMasterDetailWiring()
        {
            var catalog = new DemoFeatureCatalog();

            var files = catalog.GetCodeFiles("Wpf", "master-detail");

            Assert.That(files.Any(file => file.Text.Contains("DemoGisMasterDetailBuilder.Build")), Is.True);
            Assert.That(files.Any(file => file.Text.Contains("new GridColumnDefinition(\"Description\"")), Is.True);
            Assert.That(files.Any(file => file.Text.Contains("public IReadOnlyList<GridColumnDefinition> MasterColumns")), Is.True);
            Assert.That(files.Any(file => file.Text.Contains("public IReadOnlyList<GridColumnDefinition> DetailColumns")), Is.True);
            Assert.That(files.Any(file => file.Text.Contains("public IReadOnlyList<string> DetailColumnIds")), Is.True);
            Assert.That(files.Any(file => file.Text.Contains("DemoGrid.SetMasterDetailSource(")), Is.True);
            Assert.That(files.Any(file => file.Text.Contains("ViewModel.DetailColumnIds")), Is.True);
            Assert.That(files.Any(file => file.Text.Contains("ViewModel.MasterDisplayColumnId")), Is.True);
            Assert.That(files.Any(file => file.Text.Contains("ViewModel.DetailDisplayColumnId")), Is.True);
            Assert.That(files.Any(file => file.Text.Contains("HandleToggleMasterDetailPlacementClick")), Is.True);
            Assert.That(files.Any(file => file.Text.Contains("GridMasterDetailHeaderPlacementMode.Outside")), Is.True);
            Assert.That(files.Any(file => file.Text.Contains("GridMasterDetailHeaderPlacementMode.Inside")), Is.True);
        }

        [Test]
        public void FeatureCatalog_ShouldRenderPersonalizationCodeSamplesWithSearchAndNamedViews()
        {
            var catalog = new DemoFeatureCatalog();

            var files = catalog.GetCodeFiles("Wpf", "personalization");

            Assert.That(files.Any(file => file.Text.Contains("GridSearchText")), Is.True);
            Assert.That(files.Any(file => file.Text.Contains("DemoGrid.ApplyGlobalSearch")), Is.True);
            Assert.That(files.Any(file => file.Text.Contains("DemoGrid.SaveState()")), Is.True);
            Assert.That(files.Any(file => file.Text.Contains("DemoGrid.LoadState")), Is.True);
        }

        [Test]
        public void FeatureCatalog_ShouldExposeActiveLayerSelectorExample_AndDedicatedCodeSamples()
        {
            var catalog = new DemoFeatureCatalog();

            var example = catalog.GetExamples().SingleOrDefault(item => item.Id == "active-layer-selector");
            var files = catalog.GetCodeFiles("Wpf", "active-layer-selector");

            Assert.Multiple(() =>
            {
                Assert.That(example, Is.Not.Null);
                Assert.That(example.ComponentId, Is.EqualTo("active-layer-selector"));
                Assert.That(files.Any(file => file.Text.Contains("selector:ActiveLayerSelector")), Is.True);
                Assert.That(files.Any(file => file.Text.Contains("IActiveLayerSelectorState")), Is.True);
                Assert.That(files.Any(file => file.Text.Contains("DemoActiveLayerSelectorFactory.CreateDefaultState()")), Is.True);
            });
        }

        [Test]
        public void DemoChoiceOption_ToString_ShouldReturnDisplayName_ForStableComboRendering()
        {
            var option = new DemoChoiceOption("touch", "Touch");

            Assert.That(option.ToString(), Is.EqualTo("Touch"));
        }

        [Test]
        public void ActiveLayerSelectorFactory_ShouldBuildPortableLayerStackWithActiveLayerAndShowMoreCandidate()
        {
            var state = DemoActiveLayerSelectorFactory.CreateDefaultState();

            Assert.Multiple(() =>
            {
                Assert.That(state.Items.Count, Is.GreaterThan(5));
                Assert.That(state.ActiveLayerId, Is.EqualTo("roads"));
                Assert.That(state.Items.First().Name, Is.EqualTo("Roads"));
                Assert.That(state.Items.Any(item => item.GeometryType == "Raster"), Is.True);
                Assert.That(state.Items.Any(item => item.LayerType == "WMS"), Is.True);
            });
        }

        [Test]
        public void ViewModel_WhenSelectingActiveLayerSelectorComponent_ShouldOpenItsDefaultExample()
        {
            var viewModel = new DemoShellViewModel("Wpf");

            viewModel.SelectComponent("active-layer-selector");

            Assert.Multiple(() =>
            {
                Assert.That(viewModel.SelectedExample, Is.Not.Null);
                Assert.That(viewModel.SelectedExample.Id, Is.EqualTo("active-layer-selector"));
                Assert.That(viewModel.ShowActiveLayerSelectorSurface, Is.True);
                Assert.That(viewModel.ShowGridSurface, Is.False);
            });
        }

        [Test]
        public void ViewModel_ShouldExposeActiveLayerSelectorScenario_WithDedicatedStateAndNoGridToolbar()
        {
            var viewModel = new DemoShellViewModel("Wpf");

            viewModel.SelectExample("active-layer-selector");

            Assert.Multiple(() =>
            {
                Assert.That(viewModel.IsActiveLayerSelectorExample, Is.True);
                Assert.That(viewModel.ShowActiveLayerSelectorSurface, Is.True);
                Assert.That(viewModel.ShowGridSurface, Is.False);
                Assert.That(viewModel.ActiveLayerSelectorState, Is.Not.Null);
                Assert.That(viewModel.ActiveLayerSelectorState.Items.Count, Is.GreaterThan(5));
                Assert.That(viewModel.SelectedComponentText, Is.EqualTo("Active Layer Selector"));
                Assert.That(viewModel.DetailHeadline, Is.EqualTo("Active Layer Selector"));
                Assert.That(viewModel.HasDemoToolbar, Is.False);
            });
        }
    }
}
