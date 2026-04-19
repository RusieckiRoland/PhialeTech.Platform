using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PhialeGrid.Core.Columns;
using PhialeGrid.Core.Data;
using PhialeGrid.Core.Editing;
using PhialeGrid.Core.Query;
using PhialeGrid.Core.Summaries;
using PhialeGrid.Core.Validation;
using PhialeTech.ActiveLayerSelector;
using PhialeTech.ComponentHost.Definitions;
using PhialeTech.Components.Shared.Core;
using PhialeTech.Components.Shared.Localization;
using PhialeTech.Components.Shared.Model;
using PhialeTech.Components.Shared.Services;

namespace PhialeTech.Components.Shared.ViewModels
{
    public sealed class DemoShellViewModel : BindableBase
    {
        private const int RemotePageSize = 20;
        private readonly DemoFeatureCatalog _catalog;
        private readonly string _platformKey;
        private readonly IDemoRemoteGridClient _remoteGridClient;
        private readonly DemoDesignFoundationsCatalog _designFoundationsCatalog;
        private readonly IReadOnlyList<DemoThirdPartyLicenseEntryViewModel> _thirdPartyLicenseEntries;
        private readonly DefinitionManager _definitionManager;
        private readonly InMemoryEditSessionDataSource<DemoGisRecordViewModel> _gridEditSessionDataSource;
        private readonly EditSessionContext<DemoGisRecordViewModel> _ownedGridEditSessionContext;
        private int _deferGridEditSessionContextRebuildCount;
        private bool _gridEditSessionContextRebuildPending;
        private string _languageCode;
        private string _searchText;
        private DemoExampleDefinition _selectedExample;
        private int _selectedTabIndex;
        private DemoLanguageOption _selectedLanguage;
        private DemoCodeFileViewModel _selectedCodeFile;
        private IReadOnlyList<DemoSectionViewModel> _visibleSections;
        private IReadOnlyList<DemoDrawerGroupViewModel> _drawerGroups;
        private IReadOnlyList<DemoMetricCardViewModel> _metricCards;
        private IReadOnlyList<DemoGisPreviewRowViewModel> _previewRows;
        private IReadOnlyList<DemoCodeFileViewModel> _availableCodeFiles;
        private IReadOnlyList<string> _foundationsHighlights;
        private IReadOnlyList<DemoFoundationTypographyTokenViewModel> _foundationsTypographyTokens;
        private IReadOnlyList<DemoFoundationColorTokenViewModel> _foundationsTextColorTokens;
        private IReadOnlyList<DemoFoundationColorTokenViewModel> _foundationsSurfaceTokens;
        private IReadOnlyList<DemoFoundationColorTokenViewModel> _foundationsFormShellTokens;
        private IReadOnlyList<DemoFoundationMeasureTokenViewModel> _foundationsFormShellSpacingTokens;
        private IReadOnlyList<DemoFoundationColorTokenViewModel> _foundationsAccentTokens;
        private IReadOnlyList<DemoFoundationMeasureTokenViewModel> _foundationsShapeTokens;
        private IReadOnlyList<DemoFoundationMeasureTokenViewModel> _foundationsSpacingTokens;
        private IReadOnlyList<string> _applicationStateManagerResponsibilities;
        private IReadOnlyList<string> _applicationStateManagerOutOfScope;
        private DemoResolvedDefinitionViewModel _applicationStateManagerPageDefinition;
        private DemoResolvedDefinitionViewModel _applicationStateManagerSampleDefinition;
        private DemoResolvedDefinitionViewModel _definitionManagerPageDefinition;
        private DemoResolvedDefinitionViewModel _definitionManagerSampleDefinition;
        private IReadOnlyList<DemoColumnChooserItemViewModel> _columnChooserItems;
        private IReadOnlyList<string> _savedViewNames;
        private IReadOnlyList<GridColumnDefinition> _gridColumns;
        private IReadOnlyList<DemoGisRecordViewModel> _gridRecords;
        private IReadOnlyList<IEditSessionFieldDefinition> _gridFieldDefinitions;
        private IEditSessionContext _gridEditSessionContext;
        private IReadOnlyList<GridGroupDescriptor> _gridGroups;
        private IReadOnlyList<GridSortDescriptor> _gridSorts;
        private IReadOnlyList<GridSummaryDescriptor> _gridSummaries;
        private IReadOnlyList<DemoSummaryColumnOptionViewModel> _availableSummaryColumns;
        private IReadOnlyList<DemoSummaryTypeOptionViewModel> _availableSummaryTypes;
        private IReadOnlyList<DemoConfiguredSummaryViewModel> _configuredSummaries;
        private IReadOnlyList<PhialeGrid.Core.Hierarchy.GridHierarchyNode<object>> _gridHierarchyRoots;
        private PhialeGrid.Core.Hierarchy.GridHierarchyController<object> _gridHierarchyController;
        private IActiveLayerSelectorState _activeLayerSelectorState;
        private DemoSummaryColumnOptionViewModel _selectedSummaryColumn;
        private DemoSummaryTypeOptionViewModel _selectedSummaryType;
        private bool _isGridReadOnly;
        private string _stateStatusText;
        private string _remoteStatusText;
        private string _selectionScenarioStatusText;
        private string _editingScenarioStatusText;
        private string _constraintScenarioStatusText;
        private DemoRemoteDataStateKind _remoteDataState;
        private bool _isRemoteBusy;
        private bool _hasSavedState;
        private int _remotePageIndex;
        private int _remoteRefreshVersion;
        private int _remoteTotalCount;
        private string _remoteFailureDetail;
        private string _gridSearchText;
        private string _pendingViewName;
        private string _selectedSavedViewName;
        private string _transferStatusText;
        private string _transferPreviewText;
        private bool _isMasterDetailOutside;
        private string _selectedThemeCode;
        private string _selectedDrawerGroupId;
        private bool _isDrawerOpen;

        public DemoShellViewModel(string platformKey, DemoFeatureCatalog catalog = null, IDemoRemoteGridClient remoteGridClient = null, DefinitionManager definitionManager = null)
        {
            _platformKey = string.IsNullOrWhiteSpace(platformKey) ? "Wpf" : platformKey;
            _catalog = catalog ?? new DemoFeatureCatalog();
            _remoteGridClient = remoteGridClient;
            _designFoundationsCatalog = new DemoDesignFoundationsCatalog();
            _thirdPartyLicenseEntries = DemoLicenseCatalog.BuildThirdPartyEntries();
            _definitionManager = definitionManager ?? DemoDefinitionCatalog.CreateManager();
            _languageCode = "en";
            _visibleSections = Array.Empty<DemoSectionViewModel>();
            _drawerGroups = Array.Empty<DemoDrawerGroupViewModel>();
            _metricCards = Array.Empty<DemoMetricCardViewModel>();
            _previewRows = Array.Empty<DemoGisPreviewRowViewModel>();
            _availableCodeFiles = Array.Empty<DemoCodeFileViewModel>();
            _foundationsHighlights = Array.Empty<string>();
            _foundationsTypographyTokens = Array.Empty<DemoFoundationTypographyTokenViewModel>();
            _foundationsTextColorTokens = Array.Empty<DemoFoundationColorTokenViewModel>();
            _foundationsSurfaceTokens = Array.Empty<DemoFoundationColorTokenViewModel>();
            _foundationsFormShellTokens = Array.Empty<DemoFoundationColorTokenViewModel>();
            _foundationsFormShellSpacingTokens = Array.Empty<DemoFoundationMeasureTokenViewModel>();
            _foundationsAccentTokens = Array.Empty<DemoFoundationColorTokenViewModel>();
            _foundationsShapeTokens = Array.Empty<DemoFoundationMeasureTokenViewModel>();
            _foundationsSpacingTokens = Array.Empty<DemoFoundationMeasureTokenViewModel>();
            _applicationStateManagerResponsibilities = Array.Empty<string>();
            _applicationStateManagerOutOfScope = Array.Empty<string>();
            _applicationStateManagerPageDefinition = CreateEmptyResolvedDefinitionViewModel();
            _applicationStateManagerSampleDefinition = CreateEmptyResolvedDefinitionViewModel();
            _definitionManagerPageDefinition = CreateEmptyResolvedDefinitionViewModel();
            _definitionManagerSampleDefinition = CreateEmptyResolvedDefinitionViewModel();
            _columnChooserItems = Array.Empty<DemoColumnChooserItemViewModel>();
            _savedViewNames = Array.Empty<string>();
            _gridColumns = Array.Empty<GridColumnDefinition>();
            _gridRecords = _catalog.GetGisRecords();
            _gridFieldDefinitions = Array.Empty<IEditSessionFieldDefinition>();
            _gridGroups = Array.Empty<GridGroupDescriptor>();
            _gridSorts = Array.Empty<GridSortDescriptor>();
            _gridSummaries = Array.Empty<GridSummaryDescriptor>();
            _availableSummaryColumns = Array.Empty<DemoSummaryColumnOptionViewModel>();
            _availableSummaryTypes = Array.Empty<DemoSummaryTypeOptionViewModel>();
            _configuredSummaries = Array.Empty<DemoConfiguredSummaryViewModel>();
            _gridHierarchyRoots = Array.Empty<PhialeGrid.Core.Hierarchy.GridHierarchyNode<object>>();
            _stateStatusText = string.Empty;
            _remoteStatusText = string.Empty;
            _selectionScenarioStatusText = string.Empty;
            _editingScenarioStatusText = string.Empty;
            _constraintScenarioStatusText = string.Empty;
            _remoteFailureDetail = string.Empty;
            _remoteTotalCount = _catalog.GetGisRecords().Count;
            _gridSearchText = string.Empty;
            _pendingViewName = string.Empty;
            _selectedSavedViewName = string.Empty;
            _transferStatusText = string.Empty;
            _transferPreviewText = string.Empty;
            _selectedThemeCode = "system";
            _selectedDrawerGroupId = string.Empty;
            _isDrawerOpen = true;
            _gridEditSessionDataSource = new InMemoryEditSessionDataSource<DemoGisRecordViewModel>(_gridRecords);
            _ownedGridEditSessionContext = new EditSessionContext<DemoGisRecordViewModel>(_gridEditSessionDataSource, record => record?.ObjectId ?? string.Empty);
            _gridEditSessionContext = _ownedGridEditSessionContext;
            LanguageOptions = _catalog.GetLanguageOptions();
            SelectExampleCommand = new RelayCommand(parameter => SelectExample(parameter as string));
            SelectComponentCommand = new RelayCommand(parameter => SelectComponent(parameter as string));
            SelectDrawerGroupCommand = new RelayCommand(parameter => SelectDrawerGroup(parameter as string));
            ToggleDrawerCommand = new RelayCommand(ToggleDrawer);
            ShowOverviewCommand = new RelayCommand(ShowOverview, () => SelectedExample != null);
            RebuildLocalizedState();
        }

        public RelayCommand SelectExampleCommand { get; }

        public RelayCommand SelectComponentCommand { get; }

        public RelayCommand SelectDrawerGroupCommand { get; }

        public RelayCommand ToggleDrawerCommand { get; }

        public RelayCommand ShowOverviewCommand { get; }

        public IReadOnlyList<DemoLanguageOption> LanguageOptions { get; }

        public IReadOnlyList<DemoThemeOption> ThemeOptions =>
            new[]
            {
                new DemoThemeOption("system", Localize(DemoTextKeys.ShellThemeSystem)),
                new DemoThemeOption("day", Localize(DemoTextKeys.ShellThemeDay)),
                new DemoThemeOption("night", Localize(DemoTextKeys.ShellThemeNight)),
            };

        public IReadOnlyList<DemoSectionViewModel> VisibleSections
        {
            get => _visibleSections;
            private set => SetProperty(ref _visibleSections, value);
        }

        public IReadOnlyList<DemoDrawerGroupViewModel> DrawerGroups
        {
            get => _drawerGroups;
            private set => SetProperty(ref _drawerGroups, value ?? Array.Empty<DemoDrawerGroupViewModel>());
        }

        public IReadOnlyList<DemoMetricCardViewModel> MetricCards
        {
            get => _metricCards;
            private set => SetProperty(ref _metricCards, value);
        }

        public IReadOnlyList<DemoGisPreviewRowViewModel> PreviewRows
        {
            get => _previewRows;
            private set => SetProperty(ref _previewRows, value);
        }

        public IReadOnlyList<DemoCodeFileViewModel> AvailableCodeFiles
        {
            get => _availableCodeFiles;
            private set => SetProperty(ref _availableCodeFiles, value);
        }

        public IReadOnlyList<string> FoundationsHighlights
        {
            get => _foundationsHighlights;
            private set => SetProperty(ref _foundationsHighlights, value ?? Array.Empty<string>());
        }

        public IReadOnlyList<DemoFoundationTypographyTokenViewModel> FoundationsTypographyTokens
        {
            get => _foundationsTypographyTokens;
            private set => SetProperty(ref _foundationsTypographyTokens, value ?? Array.Empty<DemoFoundationTypographyTokenViewModel>());
        }

        public IReadOnlyList<DemoFoundationColorTokenViewModel> FoundationsTextColorTokens
        {
            get => _foundationsTextColorTokens;
            private set => SetProperty(ref _foundationsTextColorTokens, value ?? Array.Empty<DemoFoundationColorTokenViewModel>());
        }

        public IReadOnlyList<DemoFoundationColorTokenViewModel> FoundationsSurfaceTokens
        {
            get => _foundationsSurfaceTokens;
            private set => SetProperty(ref _foundationsSurfaceTokens, value ?? Array.Empty<DemoFoundationColorTokenViewModel>());
        }

        public IReadOnlyList<DemoFoundationColorTokenViewModel> FoundationsFormShellTokens
        {
            get => _foundationsFormShellTokens;
            private set => SetProperty(ref _foundationsFormShellTokens, value ?? Array.Empty<DemoFoundationColorTokenViewModel>());
        }

        public IReadOnlyList<DemoFoundationMeasureTokenViewModel> FoundationsFormShellSpacingTokens
        {
            get => _foundationsFormShellSpacingTokens;
            private set => SetProperty(ref _foundationsFormShellSpacingTokens, value ?? Array.Empty<DemoFoundationMeasureTokenViewModel>());
        }

        public IReadOnlyList<DemoFoundationColorTokenViewModel> FoundationsAccentTokens
        {
            get => _foundationsAccentTokens;
            private set => SetProperty(ref _foundationsAccentTokens, value ?? Array.Empty<DemoFoundationColorTokenViewModel>());
        }

        public IReadOnlyList<DemoFoundationMeasureTokenViewModel> FoundationsShapeTokens
        {
            get => _foundationsShapeTokens;
            private set => SetProperty(ref _foundationsShapeTokens, value ?? Array.Empty<DemoFoundationMeasureTokenViewModel>());
        }

        public IReadOnlyList<DemoFoundationMeasureTokenViewModel> FoundationsSpacingTokens
        {
            get => _foundationsSpacingTokens;
            private set => SetProperty(ref _foundationsSpacingTokens, value ?? Array.Empty<DemoFoundationMeasureTokenViewModel>());
        }

        public IReadOnlyList<string> ApplicationStateManagerResponsibilities
        {
            get => _applicationStateManagerResponsibilities;
            private set => SetProperty(ref _applicationStateManagerResponsibilities, value ?? Array.Empty<string>());
        }

        public IReadOnlyList<string> ApplicationStateManagerOutOfScope
        {
            get => _applicationStateManagerOutOfScope;
            private set => SetProperty(ref _applicationStateManagerOutOfScope, value ?? Array.Empty<string>());
        }

        public DemoResolvedDefinitionViewModel ApplicationStateManagerPageDefinition
        {
            get => _applicationStateManagerPageDefinition;
            private set => SetProperty(ref _applicationStateManagerPageDefinition, value ?? CreateEmptyResolvedDefinitionViewModel());
        }

        public DemoResolvedDefinitionViewModel ApplicationStateManagerSampleDefinition
        {
            get => _applicationStateManagerSampleDefinition;
            private set => SetProperty(ref _applicationStateManagerSampleDefinition, value ?? CreateEmptyResolvedDefinitionViewModel());
        }

        public DemoResolvedDefinitionViewModel DefinitionManagerPageDefinition
        {
            get => _definitionManagerPageDefinition;
            private set => SetProperty(ref _definitionManagerPageDefinition, value ?? CreateEmptyResolvedDefinitionViewModel());
        }

        public DemoResolvedDefinitionViewModel DefinitionManagerSampleDefinition
        {
            get => _definitionManagerSampleDefinition;
            private set => SetProperty(ref _definitionManagerSampleDefinition, value ?? CreateEmptyResolvedDefinitionViewModel());
        }

        public IReadOnlyList<DemoColumnChooserItemViewModel> ColumnChooserItems
        {
            get => _columnChooserItems;
            private set => SetProperty(ref _columnChooserItems, value ?? Array.Empty<DemoColumnChooserItemViewModel>());
        }

        public IReadOnlyList<string> SavedViewNames
        {
            get => _savedViewNames;
            private set => SetProperty(ref _savedViewNames, value ?? Array.Empty<string>());
        }

        public IReadOnlyList<GridColumnDefinition> GridColumns
        {
            get => _gridColumns;
            private set
            {
                if (SetProperty(ref _gridColumns, value))
                {
                    RequestGridEditSessionContextRebuild();
                }
            }
        }

        public IReadOnlyList<DemoGisRecordViewModel> GridRecords
        {
            get => _gridRecords;
            private set
            {
                if (SetProperty(ref _gridRecords, value))
                {
                    RequestGridEditSessionContextRebuild();
                }
            }
        }

        public IEditSessionContext GridEditSessionContext
        {
            get => _gridEditSessionContext;
            private set => SetProperty(ref _gridEditSessionContext, value);
        }

        public IReadOnlyList<GridGroupDescriptor> GridGroups
        {
            get => _gridGroups;
            set => SetProperty(ref _gridGroups, value ?? Array.Empty<GridGroupDescriptor>());
        }

        public IReadOnlyList<GridSortDescriptor> GridSorts
        {
            get => _gridSorts;
            set => SetProperty(ref _gridSorts, value ?? Array.Empty<GridSortDescriptor>());
        }

        public IReadOnlyList<GridSummaryDescriptor> GridSummaries
        {
            get => _gridSummaries;
            set
            {
                if (SetProperty(ref _gridSummaries, value ?? Array.Empty<GridSummaryDescriptor>()))
                {
                    RebuildConfiguredSummaries();
                }
            }
        }

        public IReadOnlyList<DemoSummaryColumnOptionViewModel> AvailableSummaryColumns
        {
            get => _availableSummaryColumns;
            private set => SetProperty(ref _availableSummaryColumns, value ?? Array.Empty<DemoSummaryColumnOptionViewModel>());
        }

        public IReadOnlyList<DemoSummaryTypeOptionViewModel> AvailableSummaryTypes
        {
            get => _availableSummaryTypes;
            private set => SetProperty(ref _availableSummaryTypes, value ?? Array.Empty<DemoSummaryTypeOptionViewModel>());
        }

        public IReadOnlyList<DemoConfiguredSummaryViewModel> ConfiguredSummaries
        {
            get => _configuredSummaries;
            private set => SetProperty(ref _configuredSummaries, value ?? Array.Empty<DemoConfiguredSummaryViewModel>());
        }

        public DemoSummaryColumnOptionViewModel SelectedSummaryColumn
        {
            get => _selectedSummaryColumn;
            set
            {
                if (SetProperty(ref _selectedSummaryColumn, value))
                {
                    RebuildAvailableSummaryTypes();
                }
            }
        }

        public DemoSummaryTypeOptionViewModel SelectedSummaryType
        {
            get => _selectedSummaryType;
            set => SetProperty(ref _selectedSummaryType, value);
        }

        public bool IsGridReadOnly
        {
            get => _isGridReadOnly;
            set => SetProperty(ref _isGridReadOnly, value);
        }

        public IReadOnlyList<PhialeGrid.Core.Hierarchy.GridHierarchyNode<object>> GridHierarchyRoots
        {
            get => _gridHierarchyRoots;
            private set => SetProperty(ref _gridHierarchyRoots, value ?? Array.Empty<PhialeGrid.Core.Hierarchy.GridHierarchyNode<object>>());
        }

        public PhialeGrid.Core.Hierarchy.GridHierarchyController<object> GridHierarchyController
        {
            get => _gridHierarchyController;
            private set => SetProperty(ref _gridHierarchyController, value);
        }

        public IActiveLayerSelectorState ActiveLayerSelectorState
        {
            get => _activeLayerSelectorState;
            private set => SetProperty(ref _activeLayerSelectorState, value);
        }

        public string StateStatusText
        {
            get => _stateStatusText;
            private set => SetProperty(ref _stateStatusText, value);
        }

        public string RemoteStatusText
        {
            get => _remoteStatusText;
            private set => SetProperty(ref _remoteStatusText, value);
        }

        public string SelectionScenarioStatusText
        {
            get => _selectionScenarioStatusText;
            set => SetProperty(ref _selectionScenarioStatusText, value ?? string.Empty);
        }

        public string EditingScenarioStatusText
        {
            get => _editingScenarioStatusText;
            set => SetProperty(ref _editingScenarioStatusText, value ?? string.Empty);
        }

        public string ConstraintScenarioStatusText
        {
            get => _constraintScenarioStatusText;
            set => SetProperty(ref _constraintScenarioStatusText, value ?? string.Empty);
        }

        public DemoRemoteDataStateKind RemoteDataState
        {
            get => _remoteDataState;
            private set => SetProperty(ref _remoteDataState, value);
        }

        public bool IsRemoteBusy
        {
            get => _isRemoteBusy;
            private set => SetProperty(ref _isRemoteBusy, value);
        }

        public DemoCodeFileViewModel SelectedCodeFile
        {
            get => _selectedCodeFile;
            set
            {
                if (SetProperty(ref _selectedCodeFile, value))
                {
                    OnPropertyChanged(nameof(SourceCodeText));
                }
            }
        }

        public string LanguageCode
        {
            get => _languageCode;
            set
            {
                if (SetProperty(ref _languageCode, string.IsNullOrWhiteSpace(value) ? "en" : value))
                {
                    SyncSelectedLanguage();
                    RebuildLocalizedState();
                }
            }
        }

        public DemoLanguageOption SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                if (SetProperty(ref _selectedLanguage, value) && value != null && value.Code != LanguageCode)
                {
                    LanguageCode = value.Code;
                }
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    RebuildSections();
                }
            }
        }

        public string GridSearchText
        {
            get => _gridSearchText;
            set => SetProperty(ref _gridSearchText, value ?? string.Empty);
        }

        public string PendingViewName
        {
            get => _pendingViewName;
            set => SetProperty(ref _pendingViewName, value ?? string.Empty);
        }

        public string SelectedSavedViewName
        {
            get => _selectedSavedViewName;
            set => SetProperty(ref _selectedSavedViewName, value ?? string.Empty);
        }

        public string TransferStatusText
        {
            get => _transferStatusText;
            private set => SetProperty(ref _transferStatusText, value ?? string.Empty);
        }

        public string TransferPreviewText
        {
            get => _transferPreviewText;
            private set => SetProperty(ref _transferPreviewText, value ?? string.Empty);
        }

        public DemoExampleDefinition SelectedExample
        {
            get => _selectedExample;
            private set
            {
                if (SetProperty(ref _selectedExample, value))
                {
                    OnPropertyChanged(nameof(IsOverviewVisible));
                    OnPropertyChanged(nameof(IsDetailVisible));
                    OnPropertyChanged(nameof(DetailHeadline));
                    OnPropertyChanged(nameof(IsGridExample));
                    OnPropertyChanged(nameof(IsActiveLayerSelectorExample));
                    OnPropertyChanged(nameof(IsFoundationsExample));
                    OnPropertyChanged(nameof(IsApplicationStateManagerExample));
                    OnPropertyChanged(nameof(IsDefinitionManagerExample));
                    OnPropertyChanged(nameof(IsWebComponentsExample));
                    OnPropertyChanged(nameof(IsWebHostExample));
                    OnPropertyChanged(nameof(IsPdfViewerExample));
                    OnPropertyChanged(nameof(IsReportDesignerExample));
                    OnPropertyChanged(nameof(IsMonacoEditorExample));
                    OnPropertyChanged(nameof(ShowGridSurface));
                    OnPropertyChanged(nameof(ShowActiveLayerSelectorSurface));
                    OnPropertyChanged(nameof(ShowFoundationsSurface));
                    OnPropertyChanged(nameof(ShowApplicationStateManagerSurface));
                    OnPropertyChanged(nameof(ShowDefinitionManagerSurface));
                    OnPropertyChanged(nameof(ShowWebComponentsSurface));
                    OnPropertyChanged(nameof(ShowWebHostSurface));
                    OnPropertyChanged(nameof(ShowPdfViewerSurface));
                    OnPropertyChanged(nameof(ShowReportDesignerSurface));
                    OnPropertyChanged(nameof(ShowMonacoEditorSurface));
                    OnPropertyChanged(nameof(ShowGridTopCommandRegionContent));
                    OnPropertyChanged(nameof(ShowGridSideToolRegionContent));
                    OnPropertyChanged(nameof(SelectedComponentText));
                    OnPropertyChanged(nameof(PreviewHintText));
                    OnPropertyChanged(nameof(SelectedExampleTitle));
                    OnPropertyChanged(nameof(SelectedExampleDescription));
                    OnPropertyChanged(nameof(PreviewUsageTitle));
                    ShowOverviewCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set => SetProperty(ref _selectedTabIndex, value);
        }

        public bool IsOverviewVisible => SelectedExample == null;

        public bool IsDetailVisible => SelectedExample != null;

        public bool HasOverviewResults => VisibleSections.Any();

        public bool HasNoOverviewResults => !HasOverviewResults;

        public string AppTitle => Localize(DemoTextKeys.AppTitle);

        public string AppSubtitle => Localize(DemoTextKeys.AppSubtitle);

        public string ComponentsTitle => Localize(DemoTextKeys.ShellComponentsTitle);

        public string DrawerOpenText => Localize(DemoTextKeys.ShellDrawerOpen);

        public string DrawerCloseText => Localize(DemoTextKeys.ShellDrawerClose);

        public string SearchPlaceholder => Localize(DemoTextKeys.ShellSearchPlaceholder);

        public string OverviewTitle => Localize(DemoTextKeys.ShellOverviewTitle);

        public string OverviewSubtitle => Localize(DemoTextKeys.ShellOverviewSubtitle);

        public string BackToOverviewText => Localize(DemoTextKeys.ShellBackToOverview);

        public string DemoTabText => Localize(DemoTextKeys.ShellDemoTab);

        public string CodeTabText => Localize(DemoTextKeys.ShellCodeTab);

        public string ExplanationTabText => Localize(DemoTextKeys.ShellExplanationTab);

        public string FileLabelText => Localize(DemoTextKeys.ShellFileLabel);

        public string LanguageLabelText => Localize(DemoTextKeys.ShellLanguageLabel);

        public string ThemeLabelText => Localize(DemoTextKeys.ShellThemeLabel);

        public string SelectedThemeCode
        {
            get => _selectedThemeCode;
            set => SetProperty(ref _selectedThemeCode, NormalizeThemeCode(value, _selectedThemeCode));
        }

        public string EmptySearchText => Localize(DemoTextKeys.ShellEmptySearch);

        public string GridComponentText => Localize(DemoTextKeys.ShellComponentGrid);

        public string GridComponentDescription => Localize(DemoTextKeys.ShellComponentGridDescription);

        public string ActiveLayerSelectorComponentText => Localize(DemoTextKeys.ShellComponentActiveLayerSelector);

        public string ActiveLayerSelectorComponentDescription => Localize(DemoTextKeys.ShellComponentActiveLayerSelectorDescription);

        public string FoundationsComponentText => Localize(DemoTextKeys.ShellComponentFoundations);

        public string FoundationsComponentDescription => Localize(DemoTextKeys.ShellComponentFoundationsDescription);

        public string ArchitectureComponentText => Localize(DemoTextKeys.ShellComponentArchitecture);

        public string ArchitectureComponentDescription => Localize(DemoTextKeys.ShellComponentArchitectureDescription);

        public string WebComponentsComponentText => Localize(DemoTextKeys.ShellComponentWebComponents);

        public string WebComponentsComponentDescription => Localize(DemoTextKeys.ShellComponentWebComponentsDescription);

        public string WebComponentsScrollHostTitle => Localize(DemoTextKeys.WebComponentsScrollHostTitle);

        public string WebComponentsScrollHostDescription => Localize(DemoTextKeys.WebComponentsScrollHostDescription);

        public string WebComponentsScrollHostPointOne => Localize(DemoTextKeys.WebComponentsScrollHostPointOne);

        public string WebComponentsScrollHostPointTwo => Localize(DemoTextKeys.WebComponentsScrollHostPointTwo);

        public string WebComponentsScrollHostPointThree => Localize(DemoTextKeys.WebComponentsScrollHostPointThree);

        public string YamlUiComponentText => Localize(DemoTextKeys.ShellComponentYamlUi);

        public string YamlUiComponentDescription => Localize(DemoTextKeys.ShellComponentYamlUiDescription);

        public string LicenseComponentText => Localize(DemoTextKeys.ShellComponentLicense);

        public string LicenseComponentDescription => Localize(DemoTextKeys.ShellComponentLicenseDescription);

        public string DefinitionManagerComponentText => Localize(DemoTextKeys.ShellComponentDefinitionManager);

        public string DefinitionManagerComponentDescription => Localize(DemoTextKeys.ShellComponentDefinitionManagerDescription);

        public string ApplicationStateManagerComponentText => Localize(DemoTextKeys.ExampleApplicationStateManagerTitle);

        public string SelectedComponentText => IsActiveLayerSelectorExample
            ? ActiveLayerSelectorComponentText
            : IsApplicationStateManagerExample
                ? ApplicationStateManagerComponentText
            : IsDefinitionManagerExample
                ? DefinitionManagerComponentText
            : IsWebComponentsExample
                ? WebComponentsComponentText
            : IsYamlUiExample
                ? YamlUiComponentText
            : IsLicenseExample
                ? LicenseComponentText
                : IsFoundationsExample
                    ? FoundationsComponentText
                    : GridComponentText;

        public bool IsFoundationsDrawerSelected => string.Equals(_selectedDrawerGroupId, "foundations", StringComparison.OrdinalIgnoreCase);

        public bool IsArchitectureDrawerSelected => string.Equals(_selectedDrawerGroupId, "architecture", StringComparison.OrdinalIgnoreCase);

        public bool IsGridDrawerSelected => string.Equals(_selectedDrawerGroupId, "grid", StringComparison.OrdinalIgnoreCase);

        public bool IsActiveLayerSelectorDrawerSelected => string.Equals(_selectedDrawerGroupId, "active-layer-selector", StringComparison.OrdinalIgnoreCase);

        public bool IsYamlUiDrawerSelected => string.Equals(_selectedDrawerGroupId, "yaml-ui", StringComparison.OrdinalIgnoreCase);

        public bool IsWebComponentsDrawerSelected => string.Equals(_selectedDrawerGroupId, "web-components", StringComparison.OrdinalIgnoreCase);

        public bool IsLicenseDrawerSelected => string.Equals(_selectedDrawerGroupId, "license", StringComparison.OrdinalIgnoreCase);

        public bool IsDrawerOpen
        {
            get => _isDrawerOpen;
            set
            {
                if (SetProperty(ref _isDrawerOpen, value))
                {
                    OnPropertyChanged(nameof(IsDrawerClosed));
                }
            }
        }

        public bool IsDrawerClosed => !IsDrawerOpen;

        public string GroupingBarText => Localize(DemoTextKeys.PreviewGroupingBar);

        public string PreviewHintText => IsFoundationsExample
            ? Localize(DemoTextKeys.PreviewFoundationsHint)
            : IsActiveLayerSelectorExample
            ? Localize(DemoTextKeys.PreviewActiveLayerSelectorHint)
            : IsConstraintsExample
            ? Localize(DemoTextKeys.PreviewConstraintsHint)
            : IsApplicationStateManagerExample
                ? Localize(DemoTextKeys.PreviewApplicationStateManagerHint)
            : IsDefinitionManagerExample
                ? Localize(DemoTextKeys.PreviewDefinitionManagerHint)
            : IsWebHostExample
                ? Localize(DemoTextKeys.PreviewWebHostHint)
            : IsPdfViewerExample
                ? Localize(DemoTextKeys.PreviewPdfViewerHint)
            : IsReportDesignerExample
                ? Localize(DemoTextKeys.PreviewReportDesignerHint)
            : IsMonacoEditorExample
                ? Localize(DemoTextKeys.PreviewMonacoEditorHint)
            : IsLicenseExample
                ? Localize(DemoTextKeys.PreviewLicenseHint)
            : Localize(DemoTextKeys.PreviewHint);

        public string CategoryColumnText => Localize(DemoTextKeys.PreviewCategory);

        public string DescriptionColumnText => Localize(DemoTextKeys.PreviewDescription);

        public string ObjectNameColumnText => Localize(DemoTextKeys.PreviewObjectName);

        public string ObjectIdColumnText => Localize(DemoTextKeys.PreviewObjectId);

        public string GeometryTypeColumnText => Localize(DemoTextKeys.PreviewGeometryType);

        public string MunicipalityColumnText => Localize(DemoTextKeys.PreviewMunicipality);

        public string DistrictColumnText => Localize(DemoTextKeys.PreviewDistrict);

        public string StatusColumnText => Localize(DemoTextKeys.PreviewStatus);

        public string PriorityColumnText => Localize(DemoTextKeys.PreviewPriority);

        public string AreaColumnText => Localize(DemoTextKeys.PreviewArea);

        public string LengthColumnText => Localize(DemoTextKeys.PreviewLength);

        public string LastInspectionColumnText => Localize(DemoTextKeys.PreviewLastInspection);

        public string UpdatedAtColumnText => Localize(DemoTextKeys.PreviewUpdatedAt);

        public string OwnerColumnText => Localize(DemoTextKeys.PreviewOwner);

        public string ScaleHintColumnText => Localize(DemoTextKeys.PreviewScaleHint);

        public string MaintenanceBudgetColumnText => Localize(DemoTextKeys.PreviewMaintenanceBudget);

        public string CompletionPercentColumnText => Localize(DemoTextKeys.PreviewCompletionPercent);

        public string VisibleColumnText => Localize(DemoTextKeys.PreviewVisible);

        public string EditableFlagColumnText => Localize(DemoTextKeys.PreviewEditableFlag);

        public string MetricDeckTitle => Localize(DemoTextKeys.PreviewMetricDeck);

        public string PreviewUsageTitle => SelectedExample == null ? Localize(DemoTextKeys.PreviewUsageTitle) : SelectedExampleTitle;

        public string PreviewCodeTitle => Localize(DemoTextKeys.PreviewCodeTitle);

        public string PlatformBadgeText => Localize(DemoTextKeys.PreviewPlatformBadge) + ": " + _platformKey;

        public string ConstraintRulesTitle => string.Equals(LanguageCode, "pl", StringComparison.OrdinalIgnoreCase)
            ? "Zasady walidacji dla tej karty"
            : "Validation rules on this card";

        public IReadOnlyList<string> ConstraintRules => string.Equals(LanguageCode, "pl", StringComparison.OrdinalIgnoreCase)
            ? new[]
            {
                "Object name: wymagane, od 3 do 120 znakow. Bledne: pusty tekst, 1-2 znaki.",
                "Object ID: wymagane, format duzymi literami 'AA-AAA-AAA-0000' albo 'AAAA-AAA-AAA-0000'. Poprawne: 'DZ-KRA-STA-0001', 'BLD-WRO-FAB-0002'. Bledne: 'bad-id', male litery, brak segmentow.",
                "Status: wymagane. Dozwolone tylko: Verified, Retired, UnderMaintenance, NeedsReview, Planned, Active.",
                "Priority: wymagane. Dozwolone tylko: Critical, High, Medium, Low.",
                "Last inspection: wymagane. Zakres od 2020-01-01 do 2035-12-31.",
                "Owner: wymagane, od 3 do 120 znakow. Bledne: pusty tekst, 1-2 znaki.",
                "Budget [PLN]: wymagane. Zakres od 0.00 do 999999.99, maksymalnie 2 miejsca po przecinku i 8 cyfr lacznie. Bledne: '12.345', '-1', '1000000'.",
                "Completion [%]: wymagane. Zakres od 0.0 do 100.0, maksymalnie 1 miejsce po przecinku i 4 cyfry lacznie. Bledne: '120.0', '10.55', '-2'.",
                "Scale hint: wymagane. Liczba calkowita od 100 do 100000. Bledne: '50', '100001', '12.5'.",
                "Visible / Editable: pola boolean edytowane checkboxem."
            }
            : new[]
            {
                "Object name: required, 3 to 120 characters. Invalid: empty text, 1-2 characters.",
                "Object ID: required, uppercase format 'AA-AAA-AAA-0000' or 'AAAA-AAA-AAA-0000'. Valid: 'DZ-KRA-STA-0001', 'BLD-WRO-FAB-0002'. Invalid: 'bad-id', lowercase letters, missing segments.",
                "Status: required. Allowed only: Verified, Retired, UnderMaintenance, NeedsReview, Planned, Active.",
                "Priority: required. Allowed only: Critical, High, Medium, Low.",
                "Last inspection: required. Range from 2020-01-01 to 2035-12-31.",
                "Owner: required, 3 to 120 characters. Invalid: empty text, 1-2 characters.",
                "Budget [PLN]: required. Range from 0.00 to 999999.99, maximum 2 fractional digits and 8 digits total. Invalid: '12.345', '-1', '1000000'.",
                "Completion [%]: required. Range from 0.0 to 100.0, maximum 1 fractional digit and 4 digits total. Invalid: '120.0', '10.55', '-2'.",
                "Scale hint: required. Whole number from 100 to 100000. Invalid: '50', '100001', '12.5'.",
                "Visible / Editable: boolean fields edited with a checkbox."
            };

        public string CopySelectionText => Localize(DemoTextKeys.DemoToolbarCopy);

        public string SelectVisibleRowsText => Localize(DemoTextKeys.DemoToolbarSelectVisibleRows);

        public string ClearSelectionText => Localize(DemoTextKeys.DemoToolbarClearSelection);

        public string AddRecordText => IsPolishLanguage ? "Dodaj" : "Add";

        public string EditRecordText => IsPolishLanguage ? "Edytuj" : "Edit";

        public string ApplyCurrentEditText => IsPolishLanguage ? "Zatwierdz" : "Apply";

        public string CancelCurrentEditText => IsPolishLanguage ? "Anuluj" : "Cancel";

        public string DirtyRowsBadgeLabelText => IsPolishLanguage ? "wiersze zmian" : "dirty rows";

        public string ValidationIssuesBadgeLabelText => IsPolishLanguage ? "bledy" : "errors";

        public string CommitText => Localize(DemoTextKeys.DemoToolbarCommit);

        public string CancelText => Localize(DemoTextKeys.DemoToolbarCancel);

        public string SaveStateText => Localize(DemoTextKeys.DemoToolbarSaveState);

        public string RestoreStateText => Localize(DemoTextKeys.DemoToolbarRestoreState);

        public string ResetStateText => Localize(DemoTextKeys.DemoToolbarResetState);

        public string ToggleOwnerColumnText => Localize(DemoTextKeys.DemoToolbarToggleOwner);

        public string FreezeObjectNameText => Localize(DemoTextKeys.DemoToolbarFreezeObjectName);

        public string UnfreezeAllText => Localize(DemoTextKeys.DemoToolbarUnfreezeAll);

        public string AutoFitText => Localize(DemoTextKeys.DemoToolbarAutoFit);

        public string ShowAllColumnsText => Localize(DemoTextKeys.DemoToolbarShowAllColumns);

        public string ColumnChooserText => Localize(DemoTextKeys.DemoToolbarColumnChooser);

        public string LayoutToolsHintText => Localize(DemoTextKeys.DemoToolbarLayoutHint);

        public string GridSearchPlaceholderText => Localize(DemoTextKeys.DemoToolbarSearchPlaceholder);

        public string ApplySearchText => Localize(DemoTextKeys.DemoToolbarApplySearch);

        public string ClearSearchText => Localize(DemoTextKeys.DemoToolbarClearSearch);

        public string FocusMunicipalityFilterText => Localize(DemoTextKeys.DemoToolbarFocusMunicipalityFilter);

        public string FocusOwnerFilterText => Localize(DemoTextKeys.DemoToolbarFocusOwnerFilter);

        public string ClearColumnFiltersText => Localize(DemoTextKeys.DemoToolbarClearColumnFilters);

        public string PrevPageText => Localize(DemoTextKeys.DemoToolbarPrevPage);

        public string NextPageText => Localize(DemoTextKeys.DemoToolbarNextPage);

        public string RefreshRemoteText => Localize(DemoTextKeys.DemoToolbarRefreshRemote);

        public string ExpandHierarchyText => Localize(DemoTextKeys.DemoToolbarExpandHierarchy);

        public string CollapseHierarchyText => Localize(DemoTextKeys.DemoToolbarCollapseHierarchy);

        public string MasterDetailPlacementText => Localize(_isMasterDetailOutside
            ? DemoTextKeys.DemoToolbarMasterDetailShowInside
            : DemoTextKeys.DemoToolbarMasterDetailShowOutside);

        public string ViewNameText => Localize(DemoTextKeys.DemoToolbarViewName);

        public string SaveViewText => Localize(DemoTextKeys.DemoToolbarSaveView);

        public string ApplyViewText => Localize(DemoTextKeys.DemoToolbarApplyView);

        public string DeleteViewText => Localize(DemoTextKeys.DemoToolbarDeleteView);

        public string ExportCsvText => Localize(DemoTextKeys.DemoToolbarExportCsv);

        public string SaveFoundationsPdfText => Localize(DemoTextKeys.DemoToolbarSaveFoundationsPdf);

        public string SaveDemoBookPdfText => Localize(DemoTextKeys.DemoToolbarSaveDemoBookPdf);

        public string ImportSampleCsvText => Localize(DemoTextKeys.DemoToolbarImportSampleCsv);

        public string RestoreDataText => Localize(DemoTextKeys.DemoToolbarRestoreData);

        public string AddSummaryText => Localize(DemoTextKeys.DemoToolbarAddSummary);

        public string ResetSummariesText => Localize(DemoTextKeys.DemoToolbarResetSummaries);

        public string DetailHeadline => string.Equals(SelectedComponentText, SelectedExampleTitle, StringComparison.OrdinalIgnoreCase)
            ? SelectedExampleTitle
            : SelectedComponentText + " | " + SelectedExampleTitle;

        public string SelectedExampleTitle => SelectedExample == null
            ? OverviewTitle
            : _catalog.Localize(LanguageCode, SelectedExample.TitleKey);

        public string SelectedExampleDescription => SelectedExample == null
            ? OverviewSubtitle
            : _catalog.Localize(LanguageCode, SelectedExample.DescriptionKey);

        public string WorkspaceOverviewTitle => SelectedDrawerGroupTitle ?? OverviewTitle;

        public string WorkspaceOverviewSubtitle => SelectedDrawerGroupDescription ?? OverviewSubtitle;

        public string SelectedDrawerGroupTitle => GetDrawerGroupTitle(_selectedDrawerGroupId);

        public string SelectedDrawerGroupDescription => GetDrawerGroupDescription(_selectedDrawerGroupId);

        public string SourceCodeText => SelectedCodeFile == null ? Localize(DemoTextKeys.CodeEmpty) : SelectedCodeFile.Text;

        public string FoundationsIntroTitle => _designFoundationsCatalog.GetIntroTitle(LanguageCode);

        public string FoundationsIntroDescription => _designFoundationsCatalog.GetIntroDescription(LanguageCode);

        public string FoundationsTypographyTitle => _designFoundationsCatalog.GetTypographyTitle(LanguageCode);

        public string FoundationsTypographyDescription => _designFoundationsCatalog.GetTypographyDescription(LanguageCode);

        public string FoundationsColorsTitle => _designFoundationsCatalog.GetColorsTitle(LanguageCode);

        public string FoundationsColorsDescription => _designFoundationsCatalog.GetColorsDescription(LanguageCode);

        public string FoundationsRhythmTitle => _designFoundationsCatalog.GetRhythmTitle(LanguageCode);

        public string FoundationsRhythmDescription => _designFoundationsCatalog.GetRhythmDescription(LanguageCode);

        public string FoundationsTextColorsTitle => _designFoundationsCatalog.GetTextColorsTitle(LanguageCode);

        public string FoundationsSurfaceColorsTitle => _designFoundationsCatalog.GetSurfaceColorsTitle(LanguageCode);

        public string FoundationsFormShellColorsTitle => _designFoundationsCatalog.GetFormShellColorsTitle(LanguageCode);

        public string FoundationsFormShellColorsDescription => _designFoundationsCatalog.GetFormShellColorsDescription(LanguageCode);

        public string FoundationsFormShellSpacingTitle => _designFoundationsCatalog.GetFormShellSpacingTitle(LanguageCode);

        public string FoundationsFormShellSpacingDescription => _designFoundationsCatalog.GetFormShellSpacingDescription(LanguageCode);

        public string FoundationsAccentColorsTitle => _designFoundationsCatalog.GetAccentColorsTitle(LanguageCode);

        public string FoundationsShapesTitle => _designFoundationsCatalog.GetShapesTitle(LanguageCode);

        public string FoundationsSpacingTitle => _designFoundationsCatalog.GetSpacingTitle(LanguageCode);

        public string FoundationsTokenLabel => _designFoundationsCatalog.GetTokenLabel(LanguageCode);

        public string FoundationsRoleLabel => _designFoundationsCatalog.GetRoleLabel(LanguageCode);

        public string FoundationsUseLabel => _designFoundationsCatalog.GetUseLabel(LanguageCode);

        public string FoundationsDayLabel => _designFoundationsCatalog.GetDayLabel(LanguageCode);

        public string FoundationsNightLabel => _designFoundationsCatalog.GetNightLabel(LanguageCode);

        public string FoundationsValueLabel => _designFoundationsCatalog.GetValueLabel(LanguageCode);

        public string ApplicationStateManagerIntroTitle => Localize(DemoTextKeys.ApplicationStateManagerIntroTitle);

        public string ApplicationStateManagerIntroDescription => Localize(DemoTextKeys.ApplicationStateManagerIntroDescription);

        public string ApplicationStateManagerResponsibilitiesTitle => Localize(DemoTextKeys.ApplicationStateManagerResponsibilitiesTitle);

        public string ApplicationStateManagerOutOfScopeTitle => Localize(DemoTextKeys.ApplicationStateManagerOutOfScopeTitle);

        public string ApplicationStateManagerCooperationTitle => Localize(DemoTextKeys.ApplicationStateManagerCooperationTitle);

        public string ApplicationStateManagerCooperationDescription => Localize(DemoTextKeys.ApplicationStateManagerCooperationDescription);

        public string ApplicationStateManagerLiveSampleTitle => Localize(DemoTextKeys.ApplicationStateManagerLiveSampleTitle);

        public string ApplicationStateManagerLiveSampleDescription => Localize(DemoTextKeys.ApplicationStateManagerLiveSampleDescription);

        public string ApplicationStateManagerFieldSharedManager => Localize(DemoTextKeys.ApplicationStateManagerFieldSharedManager);

        public string ApplicationStateManagerFieldStateKey => Localize(DemoTextKeys.ApplicationStateManagerFieldStateKey);

        public string ApplicationStateManagerFieldRestoreBehavior => Localize(DemoTextKeys.ApplicationStateManagerFieldRestoreBehavior);

        public string ApplicationStateManagerFieldSaveBehavior => Localize(DemoTextKeys.ApplicationStateManagerFieldSaveBehavior);

        public string ApplicationStateManagerSharedManagerValue => Localize(DemoTextKeys.ApplicationStateManagerSharedManagerValue);

        public string ApplicationStateManagerRestoreBehaviorValue => Localize(DemoTextKeys.ApplicationStateManagerRestoreBehaviorValue);

        public string ApplicationStateManagerSaveBehaviorValue => Localize(DemoTextKeys.ApplicationStateManagerSaveBehaviorValue);

        public string ApplicationStateManagerStateKey => DemoApplicationStateKeys.ForGridExample("application-state-manager");

        public string DefinitionManagerIntroTitle => Localize(DemoTextKeys.DefinitionManagerIntroTitle);

        public string DefinitionManagerIntroDescription => Localize(DemoTextKeys.DefinitionManagerIntroDescription);

        public string DefinitionManagerResponsibilitiesTitle => Localize(DemoTextKeys.DefinitionManagerResponsibilitiesTitle);

        public string DefinitionManagerOutOfScopeTitle => Localize(DemoTextKeys.DefinitionManagerOutOfScopeTitle);

        public string DefinitionManagerResolutionTitle => Localize(DemoTextKeys.DefinitionManagerResolutionTitle);

        public string DefinitionManagerResolutionDescription => Localize(DemoTextKeys.DefinitionManagerResolutionDescription);

        public string DefinitionManagerSampleTitle => Localize(DemoTextKeys.DefinitionManagerSampleTitle);

        public string DefinitionManagerSampleDescription => Localize(DemoTextKeys.DefinitionManagerSampleDescription);

        public string DefinitionManagerFieldDefinitionKey => Localize(DemoTextKeys.DefinitionManagerFieldDefinitionKey);

        public string DefinitionManagerFieldSource => Localize(DemoTextKeys.DefinitionManagerFieldSource);

        public string DefinitionManagerFieldKind => Localize(DemoTextKeys.DefinitionManagerFieldKind);

        public string DefinitionManagerFieldComponent => Localize(DemoTextKeys.DefinitionManagerFieldComponent);

        public string DefinitionManagerFieldConsumer => Localize(DemoTextKeys.DefinitionManagerFieldConsumer);

        public string DefinitionManagerFieldStateOverlay => Localize(DemoTextKeys.DefinitionManagerFieldStateOverlay);

        public string LicenseMyPlaceholderTitle => Localize(DemoTextKeys.LicenseMyPlaceholderTitle);

        public string LicenseMyPlaceholderDescription => Localize(DemoTextKeys.LicenseMyPlaceholderDescription);

        public string LicenseThirdPartyIntroTitle => Localize(DemoTextKeys.LicenseThirdPartyIntroTitle);

        public string LicenseThirdPartyIntroDescription => Localize(DemoTextKeys.LicenseThirdPartyIntroDescription);

        public string LicenseFieldUsedBy => Localize(DemoTextKeys.LicenseFieldUsedBy);

        public string LicenseFieldLicense => Localize(DemoTextKeys.LicenseFieldLicense);

        public string LicenseFieldCopyright => Localize(DemoTextKeys.LicenseFieldCopyright);

        public string LicenseFieldRequirement => Localize(DemoTextKeys.LicenseFieldRequirement);

        public string LicenseFieldFiles => Localize(DemoTextKeys.LicenseFieldFiles);

        public string FoundationsExportErrorTitle => Localize(DemoTextKeys.FoundationsExportErrorTitle);

        public string FoundationsExportErrorMessage => Localize(DemoTextKeys.FoundationsExportErrorMessage);

        public string DemoBookExportErrorTitle => Localize(DemoTextKeys.DemoBookExportErrorTitle);

        public string DemoBookExportErrorMessage => Localize(DemoTextKeys.DemoBookExportErrorMessage);

        public IReadOnlyList<DemoThirdPartyLicenseEntryViewModel> ThirdPartyLicenseEntries => _thirdPartyLicenseEntries;

        public bool IsGridExample => SelectedExample != null && string.Equals(SelectedExample.ComponentId, "grid", StringComparison.OrdinalIgnoreCase);

        public bool IsConstraintsExample => SelectedExample != null && string.Equals(SelectedExample.Id, "constraints", StringComparison.OrdinalIgnoreCase);

        public bool IsActiveLayerSelectorExample => SelectedExample != null && string.Equals(SelectedExample.ComponentId, "active-layer-selector", StringComparison.OrdinalIgnoreCase);

        public bool IsFoundationsExample => SelectedExample != null && string.Equals(SelectedExample.Id, "foundations", StringComparison.OrdinalIgnoreCase);

        public bool IsApplicationStateManagerExample => SelectedExample != null && string.Equals(SelectedExample.Id, "application-state-manager", StringComparison.OrdinalIgnoreCase);

        public bool IsDefinitionManagerExample => SelectedExample != null && string.Equals(SelectedExample.ComponentId, "definition-manager", StringComparison.OrdinalIgnoreCase);

        public bool IsYamlUiExample => SelectedExample != null && string.Equals(SelectedExample.ComponentId, "yaml-ui", StringComparison.OrdinalIgnoreCase);

        public bool IsYamlInputsExample => SelectedExample != null && string.Equals(SelectedExample.Id, "yaml-inputs", StringComparison.OrdinalIgnoreCase);

        public bool IsYamlPrimitivesExample => SelectedExample != null && string.Equals(SelectedExample.Id, "yaml-primitives", StringComparison.OrdinalIgnoreCase);

        public bool IsYamlDocumentExample => SelectedExample != null && string.Equals(SelectedExample.Id, "yaml-document", StringComparison.OrdinalIgnoreCase);

        public bool IsYamlActionsExample => SelectedExample != null && string.Equals(SelectedExample.Id, "yaml-actions", StringComparison.OrdinalIgnoreCase);

        public bool IsYamlDocumentSurfaceExample => IsYamlDocumentExample || IsYamlActionsExample;

        public bool IsWebComponentsExample => SelectedExample != null && string.Equals(SelectedExample.ComponentId, "web-components", StringComparison.OrdinalIgnoreCase);

        public bool IsLicenseExample => SelectedExample != null && string.Equals(SelectedExample.ComponentId, "license", StringComparison.OrdinalIgnoreCase);

        public bool IsWebHostExample => SelectedExample != null && string.Equals(SelectedExample.Id, "web-host", StringComparison.OrdinalIgnoreCase);

        public bool IsPdfViewerExample => SelectedExample != null && string.Equals(SelectedExample.Id, "pdf-viewer", StringComparison.OrdinalIgnoreCase);

        public bool IsReportDesignerExample => SelectedExample != null && string.Equals(SelectedExample.Id, "report-designer", StringComparison.OrdinalIgnoreCase);

        public bool IsMonacoEditorExample => SelectedExample != null && string.Equals(SelectedExample.Id, "monaco-editor", StringComparison.OrdinalIgnoreCase);

        public bool IsWebComponentScrollHostExample => SelectedExample != null && string.Equals(SelectedExample.Id, "web-component-scroll-host", StringComparison.OrdinalIgnoreCase);

        public bool IsMyLicenseExample => SelectedExample != null && string.Equals(SelectedExample.Id, "my-license", StringComparison.OrdinalIgnoreCase);

        public bool IsThirdPartyLicensesExample => SelectedExample != null && string.Equals(SelectedExample.Id, "third-party-licenses", StringComparison.OrdinalIgnoreCase);

        public bool ShowGridSurface => IsGridExample && !IsFoundationsExample;

        public bool ShowActiveLayerSelectorSurface => IsActiveLayerSelectorExample;

        public bool ShowFoundationsSurface => IsFoundationsExample;

        public bool ShowApplicationStateManagerSurface => IsApplicationStateManagerExample;

        public bool ShowDefinitionManagerSurface => IsDefinitionManagerExample;

        public bool ShowYamlUiSurface => IsYamlUiExample;

        public bool ShowWebComponentsSurface => IsWebHostExample || IsPdfViewerExample || IsReportDesignerExample || IsMonacoEditorExample || IsWebComponentScrollHostExample;

        public bool ShowWebComponentsExplanationTab => IsWebComponentsExample;

        public bool ShowWebHostSurface => IsWebHostExample;

        public bool ShowPdfViewerSurface => IsPdfViewerExample;

        public bool ShowReportDesignerSurface => IsReportDesignerExample;

        public bool ShowMonacoEditorSurface => IsMonacoEditorExample;

        public bool ShowLicenseSurface => IsLicenseExample;

        public bool ShowSelectionTools => SelectedExample != null && SelectedExample.Id == "selection";

        public bool ShowEditingTools => SelectedExample != null && (SelectedExample.Id == "editing" || SelectedExample.Id == "rich-editors");

        public bool ShowConstraintTools => IsConstraintsExample;

        public bool ShowStateTools => SelectedExample != null && SelectedExample.Id == "state-persistence";

        public bool ShowLayoutTools => SelectedExample != null && (SelectedExample.Id == "column-layout" || SelectedExample.Id == "state-persistence" || SelectedExample.Id == "personalization");

        public bool ShowRemoteTools => SelectedExample != null && SelectedExample.Id == "remote-data";

        public bool ShowHierarchyTools => SelectedExample != null && (SelectedExample.Id == "hierarchy" || SelectedExample.Id == "master-detail");

        public bool IsMasterDetailExample => SelectedExample != null && SelectedExample.Id == "master-detail";

        public bool ShowMasterDetailPlacementToggle => IsMasterDetailExample;

        public bool IsMasterDetailOutside => _isMasterDetailOutside;

        public bool ShowSearchTools => SelectedExample != null && SelectedExample.Id == "personalization";

        public bool ShowFilteringTools => SelectedExample != null && SelectedExample.Id == "filtering";

        public bool ShowPersonalizationTools => SelectedExample != null && SelectedExample.Id == "personalization";

        public bool ShowTransferTools => SelectedExample != null && SelectedExample.Id == "export-import";

        public bool ShowSummaryDesignerTools => SelectedExample != null && (SelectedExample.Id == "summaries" || SelectedExample.Id == "state-persistence" || SelectedExample.Id == "summary-designer");

        public string GridSideToolRegionTitleText => ShowSummaryDesignerTools ? "Summary designer" : "Tools";

        public bool ShowGridEditCommandBar => ShowGridSurface;

        public bool ShowGridTopCommandRegionContent => ShowGridEditCommandBar;

        public bool ShowGridSideToolRegionContent => ShowGridSurface;

        public bool ShowGridModeTools => ShowGridSurface;

        public bool HasDemoToolbar =>
            ShowFoundationsSurface ||
            ShowLicenseSurface;

        public bool CanMovePreviousRemotePage => ShowRemoteTools && !IsRemoteBusy && _remotePageIndex > 0;

        public bool CanMoveNextRemotePage => ShowRemoteTools && !IsRemoteBusy && (_remotePageIndex + 1) < GetRemotePageCount();

        public bool CanRefreshRemotePage => ShowRemoteTools && !IsRemoteBusy;

        public void ShowOverview()
        {
            DeferGridEditSessionContextRebuild(() =>
            {
                SelectedExample = null;
                SelectedTabIndex = (int)DemoTabKind.Demo;
                AvailableCodeFiles = Array.Empty<DemoCodeFileViewModel>();
                SelectedCodeFile = null;
                RebuildSections();
                RebuildScenarioState(true);
            });
        }

        public void ToggleDrawer()
        {
            IsDrawerOpen = !IsDrawerOpen;
        }

        public void SelectExample(string exampleId)
        {
            var example = _catalog.GetExampleById(exampleId);
            if (example == null)
            {
                return;
            }

            DeferGridEditSessionContextRebuild(() =>
            {
                _selectedDrawerGroupId = NormalizeDrawerGroupId(example.DrawerGroupId);
                RebuildDrawerGroups();
                RebuildSections();
                SelectedExample = example;
                SetMasterDetailOutside(false);
                SelectedTabIndex = (int)DemoTabKind.Demo;
                if (!IsWebComponentsExampleDefinition(example))
                {
                    RebuildGridColumns();
                    RebuildScenarioState(true);
                }
                RebuildCodeFiles();
            });
        }

        public void SelectComponent(string componentId)
        {
            _selectedDrawerGroupId = NormalizeDrawerGroupId(componentId);
            RebuildDrawerGroups();
            RebuildSections();

            var example = _catalog.GetDefaultExampleByComponentId(componentId);
            if (example == null)
            {
                return;
            }

            SelectExample(example.Id);
        }

        public void SelectDrawerGroup(string drawerGroupId)
        {
            _selectedDrawerGroupId = NormalizeDrawerGroupId(drawerGroupId);
            RebuildDrawerGroups();

            var examples = _catalog.GetExamplesByDrawerGroupId(_selectedDrawerGroupId);
            if (examples.Count == 1)
            {
                SelectExample(examples[0].Id);
                return;
            }

            DeferGridEditSessionContextRebuild(() =>
            {
                SelectedExample = null;
                SelectedTabIndex = (int)DemoTabKind.Demo;
                AvailableCodeFiles = Array.Empty<DemoCodeFileViewModel>();
                SelectedCodeFile = null;
                RebuildSections();
                RebuildScenarioState(true);
            });
        }

        public void ToggleMasterDetailPlacement()
        {
            if (!IsMasterDetailExample)
            {
                return;
            }

            SetMasterDetailOutside(!_isMasterDetailOutside);
        }

        public void AddSelectedSummary()
        {
            if (SelectedSummaryColumn == null || SelectedSummaryType == null)
            {
                return;
            }

            if (GridSummaries.Any(summary =>
                string.Equals(summary.ColumnId, SelectedSummaryColumn.ColumnId, StringComparison.OrdinalIgnoreCase) &&
                summary.Type == SelectedSummaryType.Type))
            {
                return;
            }

            GridSummaries = GridSummaries
                .Concat(new[]
                {
                    BuildSummaryDescriptor(SelectedSummaryColumn.ColumnId, SelectedSummaryType.Type),
                })
                .ToArray();

            RebuildSummaryDesignerOptions();
        }

        public void RemoveSummary(string columnId, GridSummaryType summaryType)
        {
            GridSummaries = GridSummaries
                .Where(summary =>
                    !string.Equals(summary.ColumnId, columnId, StringComparison.OrdinalIgnoreCase) ||
                    summary.Type != summaryType)
                .ToArray();

            RebuildSummaryDesignerOptions();
        }

        public void ResetSummaries()
        {
            GridSummaries = BuildDefaultSummaries();
            RebuildSummaryDesignerOptions();
        }

        public void ReplaceGridRecords(IReadOnlyList<DemoGisRecordViewModel> records)
        {
            GridRecords = records ?? Array.Empty<DemoGisRecordViewModel>();
        }

        public void RestoreDefaultGridRecords()
        {
            GridRecords = _catalog.GetGisRecords();
        }

        public string BuildSampleImportCsv()
        {
            return DemoGisCsvTransferService.BuildSampleCsv(_catalog.GetGisRecords(), GridColumns);
        }

        public void MarkTransferIdle()
        {
            TransferStatusText = Localize(DemoTextKeys.DemoTransferIdle);
            TransferPreviewText = string.Empty;
        }

        public void MarkTransferExported(int rowCount, int columnCount, string csv)
        {
            TransferStatusText = string.Format(CultureInfo.CurrentCulture, Localize(DemoTextKeys.DemoTransferExported), rowCount, columnCount);
            TransferPreviewText = csv ?? string.Empty;
        }

        public void MarkTransferImported(int rowCount, string csv)
        {
            TransferStatusText = string.Format(CultureInfo.CurrentCulture, Localize(DemoTextKeys.DemoTransferImported), rowCount);
            TransferPreviewText = csv ?? string.Empty;
        }

        public void MarkTransferRestored(int rowCount)
        {
            TransferStatusText = string.Format(CultureInfo.CurrentCulture, Localize(DemoTextKeys.DemoTransferRestored), rowCount);
            TransferPreviewText = string.Empty;
        }

        public void MarkStateSaved()
        {
            _hasSavedState = true;
            StateStatusText = Localize(DemoTextKeys.DemoToolbarStateReady);
        }

        public void MarkStateCleared()
        {
            _hasSavedState = false;
            StateStatusText = Localize(DemoTextKeys.DemoToolbarStateEmpty);
        }

        public async Task LoadPreviousRemotePageAsync()
        {
            if (!CanMovePreviousRemotePage)
            {
                return;
            }

            await LoadRemotePageAsync(_remotePageIndex - 1, false);
        }

        public async Task LoadNextRemotePageAsync()
        {
            if (!CanMoveNextRemotePage)
            {
                return;
            }

            await LoadRemotePageAsync(_remotePageIndex + 1, false);
        }

        public async Task RefreshRemotePageAsync()
        {
            if (!ShowRemoteTools || IsRemoteBusy)
            {
                return;
            }

            await LoadRemotePageAsync(_remotePageIndex, true);
        }

        public async Task LoadCurrentRemotePageAsync()
        {
            if (!ShowRemoteTools || IsRemoteBusy)
            {
                return;
            }

            await LoadRemotePageAsync(_remotePageIndex, false);
        }

        private void RebuildLocalizedState()
        {
            DeferGridEditSessionContextRebuild(() =>
            {
                RebuildDrawerGroups();
                RebuildSections();
                MetricCards = _catalog.BuildMetricCards(LanguageCode);
                PreviewRows = _catalog.BuildPreviewRows(LanguageCode);
                RebuildFoundationsState();
                RebuildApplicationStateManagerState();
                RebuildDefinitionManagerState();
                RebuildGridColumns();
                RebuildScenarioState(false);
                RebuildCodeFiles();
                RaiseLocalizedPropertyChanges();
            });
        }

        private void RebuildFoundationsState()
        {
            FoundationsHighlights = _designFoundationsCatalog.BuildHighlights(LanguageCode);
            FoundationsTypographyTokens = _designFoundationsCatalog.BuildTypographyTokens(LanguageCode);
            FoundationsTextColorTokens = _designFoundationsCatalog.BuildTextColorTokens(LanguageCode);
            FoundationsSurfaceTokens = _designFoundationsCatalog.BuildSurfaceTokens(LanguageCode);
            FoundationsFormShellTokens = _designFoundationsCatalog.BuildFormShellTokens(LanguageCode);
            FoundationsFormShellSpacingTokens = _designFoundationsCatalog.BuildFormShellSpacingTokens(LanguageCode);
            FoundationsAccentTokens = _designFoundationsCatalog.BuildAccentTokens(LanguageCode);
            FoundationsShapeTokens = _designFoundationsCatalog.BuildShapeTokens(LanguageCode);
            FoundationsSpacingTokens = _designFoundationsCatalog.BuildSpacingTokens(LanguageCode);
        }

        private void RebuildApplicationStateManagerState()
        {
            ApplicationStateManagerResponsibilities = new[]
            {
                Localize(DemoTextKeys.ApplicationStateManagerResponsibilitySharedManager),
                Localize(DemoTextKeys.ApplicationStateManagerResponsibilityRegisterStatefulUi),
                Localize(DemoTextKeys.ApplicationStateManagerResponsibilityPersistOverlay),
            };

            ApplicationStateManagerOutOfScope = new[]
            {
                Localize(DemoTextKeys.ApplicationStateManagerOutOfScopeDefinitions),
                Localize(DemoTextKeys.ApplicationStateManagerOutOfScopeRules),
                Localize(DemoTextKeys.ApplicationStateManagerOutOfScopeData),
            };

            ApplicationStateManagerPageDefinition = BuildResolvedDefinitionViewModel("demo.application-state-manager");
            ApplicationStateManagerSampleDefinition = BuildResolvedDefinitionViewModel("demo.grid.grouping");
        }

        private void RebuildDefinitionManagerState()
        {
            DefinitionManagerPageDefinition = BuildResolvedDefinitionViewModel("demo.definition-manager");
            DefinitionManagerSampleDefinition = BuildResolvedDefinitionViewModel("demo.grid.grouping");
        }

        private void RebuildGridColumns()
        {
            if (IsMasterDetailExample)
            {
                _gridFieldDefinitions = Array.Empty<IEditSessionFieldDefinition>();
                GridColumns = new[]
                {
                    new GridColumnDefinition("Category", CategoryColumnText, width: 170d, displayIndex: 0, valueType: typeof(string), isEditable: false),
                    new GridColumnDefinition("Description", DescriptionColumnText, width: 360d, displayIndex: 1, valueType: typeof(string), isEditable: false),
                    new GridColumnDefinition("ObjectName", ObjectNameColumnText, width: 260d, displayIndex: 2, valueType: typeof(string), isEditable: false),
                    new GridColumnDefinition("ObjectId", ObjectIdColumnText, width: 180d, displayIndex: 3, valueType: typeof(string), isEditable: false),
                    new GridColumnDefinition("GeometryType", GeometryTypeColumnText, width: 140d, displayIndex: 4, valueType: typeof(string), isEditable: false),
                    new GridColumnDefinition("Status", StatusColumnText, width: 150d, displayIndex: 5, valueType: typeof(string), isEditable: false),
                };
                ColumnChooserItems = GridColumns.Select(column => new DemoColumnChooserItemViewModel(column.Id, column.Header, column.IsVisible)).ToArray();
                RebuildSummaryDesignerOptions();
                return;
            }

            var hideObjectIdColumn = SelectedExample != null && SelectedExample.Id == "editing";
            var showConstraintsColumns = IsConstraintsExample;
            var showRichEditorSemanticColumns = SelectedExample != null && (SelectedExample.Id == "rich-editors" || showConstraintsColumns);
            var showScaleHintColumn = SelectedExample != null &&
                                      (SelectedExample.Id == "editing" || SelectedExample.Id == "rich-editors" || showConstraintsColumns);
            var ownerOptions = BuildOwnerOptions();
            var statusOptions = BuildStatusOptions();
            var priorityOptions = BuildPriorityOptions();
            if (showConstraintsColumns)
            {
                _gridFieldDefinitions = BuildConstraintFieldDefinitions(
                    statusOptions,
                    priorityOptions,
                    ownerOptions);
                GridColumns = ObjectEditSessionFieldDefinitionFactory.CreateGridColumns(_gridFieldDefinitions);
            }
            else
            {
                _gridFieldDefinitions = Array.Empty<IEditSessionFieldDefinition>();
                GridColumns = new[]
                {
                    new GridColumnDefinition("Category", CategoryColumnText, width: 150d, displayIndex: 0, valueType: typeof(string), isEditable: false, valueKind: "Text"),
                    new GridColumnDefinition("ObjectName", ObjectNameColumnText, width: 260d, displayIndex: 1, valueType: typeof(string), isEditable: true, valueKind: "Text"),
                    new GridColumnDefinition("ObjectId", ObjectIdColumnText, width: 180d, displayIndex: 2, valueType: typeof(string), isEditable: showConstraintsColumns, isVisible: !hideObjectIdColumn, valueKind: "Code"),
                    new GridColumnDefinition("GeometryType", GeometryTypeColumnText, width: 130d, displayIndex: 3, valueType: typeof(string), isEditable: false, valueKind: "Text"),
                    new GridColumnDefinition("Municipality", MunicipalityColumnText, width: 130d, displayIndex: showRichEditorSemanticColumns ? 16 : 4, valueType: typeof(string), isEditable: false, isVisible: !showRichEditorSemanticColumns, valueKind: "Text"),
                    new GridColumnDefinition("District", DistrictColumnText, width: 140d, displayIndex: showRichEditorSemanticColumns ? 17 : 5, valueType: typeof(string), isEditable: false, isVisible: !showRichEditorSemanticColumns, valueKind: "Text"),
                    new GridColumnDefinition("Status", StatusColumnText, width: 150d, displayIndex: showRichEditorSemanticColumns ? 4 : 6, valueType: typeof(string), isEditable: true, editorKind: GridColumnEditorKind.Combo, editorItems: statusOptions, valueKind: "Status"),
                    new GridColumnDefinition("Priority", PriorityColumnText, width: 120d, displayIndex: showRichEditorSemanticColumns ? 5 : 7, valueType: typeof(string), isEditable: true, editorKind: GridColumnEditorKind.Combo, editorItems: priorityOptions, valueKind: "Status"),
                    new GridColumnDefinition("Visible", VisibleColumnText, width: 90d, displayIndex: 6, valueType: typeof(bool), isEditable: showRichEditorSemanticColumns, isVisible: showRichEditorSemanticColumns, editorKind: GridColumnEditorKind.CheckBox, valueKind: "Boolean"),
                    new GridColumnDefinition("EditableFlag", EditableFlagColumnText, width: 96d, displayIndex: 7, valueType: typeof(bool), isEditable: showRichEditorSemanticColumns, isVisible: showRichEditorSemanticColumns, editorKind: GridColumnEditorKind.CheckBox, valueKind: "Boolean"),
                    new GridColumnDefinition("LastInspection", LastInspectionColumnText, width: 150d, displayIndex: showRichEditorSemanticColumns ? 8 : 10, valueType: typeof(DateTime), isEditable: true, editorKind: GridColumnEditorKind.DatePicker, valueKind: "DateTime"),
                    new GridColumnDefinition("UpdatedAt", UpdatedAtColumnText, width: 170d, displayIndex: 9, valueType: typeof(DateTime), isEditable: false, isVisible: showRichEditorSemanticColumns, valueKind: "DateTime"),
                    new GridColumnDefinition("Owner", OwnerColumnText, width: 180d, displayIndex: showRichEditorSemanticColumns ? 10 : 11, valueType: typeof(string), isEditable: true, editorKind: GridColumnEditorKind.Autocomplete, editorItems: ownerOptions, valueKind: "Text", editorItemsMode: GridEditorItemsMode.RestrictToItems),
                    new GridColumnDefinition("AreaSquareMeters", AreaColumnText, width: 140d, displayIndex: showRichEditorSemanticColumns ? 11 : 8, valueType: typeof(decimal), isEditable: false, valueKind: "Number"),
                    new GridColumnDefinition("LengthMeters", LengthColumnText, width: 140d, displayIndex: showRichEditorSemanticColumns ? 12 : 9, valueType: typeof(decimal), isEditable: false, valueKind: "Number"),
                    new GridColumnDefinition("MaintenanceBudget", MaintenanceBudgetColumnText, width: 150d, displayIndex: 13, valueType: typeof(decimal), isEditable: showConstraintsColumns, isVisible: showRichEditorSemanticColumns, valueKind: "Currency"),
                    new GridColumnDefinition("CompletionPercent", CompletionPercentColumnText, width: 140d, displayIndex: 14, valueType: typeof(decimal), isEditable: showConstraintsColumns, isVisible: showRichEditorSemanticColumns, valueKind: "Percent"),
                    new GridColumnDefinition("ScaleHint", ScaleHintColumnText, width: 120d, displayIndex: showRichEditorSemanticColumns ? 15 : 12, valueType: typeof(int), isEditable: true, isVisible: showScaleHintColumn, editorKind: GridColumnEditorKind.MaskedText, editMask: "^[0-9]{0,6}$", valueKind: "Number"),
                };
            }
            ColumnChooserItems = GridColumns.Select(column => new DemoColumnChooserItemViewModel(column.Id, column.Header, column.IsVisible)).ToArray();
            RebuildSummaryDesignerOptions();
        }

        private void RebuildGridEditSessionContext()
        {
            var records = GridRecords ?? Array.Empty<DemoGisRecordViewModel>();
            var fieldDefinitions = _gridFieldDefinitions != null && _gridFieldDefinitions.Count > 0
                ? _gridFieldDefinitions
                : (GridColumns != null && GridColumns.Count > 0
                    ? ObjectEditSessionFieldDefinitionFactory.CreateFromGridColumns(GridColumns)
                    : ObjectEditSessionFieldDefinitionFactory.CreateFromRecords(records.Cast<object>()));

            var previousCurrentRecordId = _ownedGridEditSessionContext.CurrentRecordId;
            _gridEditSessionDataSource.ReplaceData(records, fieldDefinitions);

            if (!string.IsNullOrWhiteSpace(previousCurrentRecordId))
            {
                _ownedGridEditSessionContext.SetCurrentRecord(previousCurrentRecordId);
            }

            if (!ReferenceEquals(GridEditSessionContext, _ownedGridEditSessionContext))
            {
                GridEditSessionContext = _ownedGridEditSessionContext;
            }
        }

        private void RequestGridEditSessionContextRebuild()
        {
            if (_deferGridEditSessionContextRebuildCount > 0)
            {
                _gridEditSessionContextRebuildPending = true;
                return;
            }

            RebuildGridEditSessionContext();
        }

        private void DeferGridEditSessionContextRebuild(Action action)
        {
            _deferGridEditSessionContextRebuildCount++;
            try
            {
                action?.Invoke();
            }
            finally
            {
                _deferGridEditSessionContextRebuildCount--;
                if (_deferGridEditSessionContextRebuildCount == 0 && _gridEditSessionContextRebuildPending)
                {
                    _gridEditSessionContextRebuildPending = false;
                    RebuildGridEditSessionContext();
                }
            }
        }

        private IReadOnlyList<IEditSessionFieldDefinition> BuildConstraintFieldDefinitions(
            IReadOnlyList<string> statusOptions,
            IReadOnlyList<string> priorityOptions,
            IReadOnlyList<string> ownerOptions)
        {
            return new IEditSessionFieldDefinition[]
            {
                new EditSessionFieldDefinition<DemoGisRecordViewModel>(
                    "Category",
                    CategoryColumnText,
                    typeof(string),
                    getter: record => record.Category,
                    setter: (_, __) => { },
                    fieldPath: nameof(DemoGisRecordViewModel.Category),
                    valueKind: "Text",
                    gridColumnDefinition: CreatePresentationColumn("Category", CategoryColumnText, typeof(string), 150d, 0, isEditable: false, valueKind: "Text")),
                new EditSessionFieldDefinition<DemoGisRecordViewModel>(
                    "ObjectName",
                    ObjectNameColumnText,
                    typeof(string),
                    getter: record => record.ObjectName,
                    setter: (record, value) => record.ObjectName = value as string ?? string.Empty,
                    fieldPath: nameof(DemoGisRecordViewModel.ObjectName),
                    valueKind: "Text",
                    validationConstraints: new TextValidationConstraints(required: true, minLength: 3, maxLength: 120),
                    gridColumnDefinition: CreatePresentationColumn("ObjectName", ObjectNameColumnText, typeof(string), 260d, 1, isEditable: true, valueKind: "Text")),
                new EditSessionFieldDefinition<DemoGisRecordViewModel>(
                    "ObjectId",
                    ObjectIdColumnText,
                    typeof(string),
                    getter: record => record.ObjectId,
                    setter: (_, __) => { },
                    fieldPath: nameof(DemoGisRecordViewModel.ObjectId),
                    valueKind: "Code",
                    validationConstraints: new TextValidationConstraints(required: true, minLength: 8, maxLength: 24, pattern: "^[A-Z]{2,4}-[A-Z]{3}-[A-Z]{3}-[0-9]{4}$"),
                    gridColumnDefinition: CreatePresentationColumn("ObjectId", ObjectIdColumnText, typeof(string), 180d, 2, isEditable: true, valueKind: "Code")),
                new EditSessionFieldDefinition<DemoGisRecordViewModel>(
                    "GeometryType",
                    GeometryTypeColumnText,
                    typeof(string),
                    getter: record => record.GeometryType,
                    setter: (_, __) => { },
                    fieldPath: nameof(DemoGisRecordViewModel.GeometryType),
                    valueKind: "Text",
                    gridColumnDefinition: CreatePresentationColumn("GeometryType", GeometryTypeColumnText, typeof(string), 130d, 3, isEditable: false, valueKind: "Text")),
                new EditSessionFieldDefinition<DemoGisRecordViewModel>(
                    "Status",
                    StatusColumnText,
                    typeof(string),
                    getter: record => record.Status,
                    setter: (record, value) => record.Status = value as string ?? string.Empty,
                    fieldPath: nameof(DemoGisRecordViewModel.Status),
                    valueKind: "Status",
                    editorKind: GridColumnEditorKind.Combo,
                    editorItems: statusOptions,
                    validationConstraints: new LookupValidationConstraints(statusOptions.Cast<object>().ToArray(), required: true),
                    gridColumnDefinition: CreatePresentationColumn("Status", StatusColumnText, typeof(string), 150d, 4, isEditable: true, editorKind: GridColumnEditorKind.Combo, editorItems: statusOptions, valueKind: "Status")),
                new EditSessionFieldDefinition<DemoGisRecordViewModel>(
                    "Priority",
                    PriorityColumnText,
                    typeof(string),
                    getter: record => record.Priority,
                    setter: (record, value) => record.Priority = value as string ?? string.Empty,
                    fieldPath: nameof(DemoGisRecordViewModel.Priority),
                    valueKind: "Status",
                    editorKind: GridColumnEditorKind.Combo,
                    editorItems: priorityOptions,
                    validationConstraints: new LookupValidationConstraints(priorityOptions.Cast<object>().ToArray(), required: true),
                    gridColumnDefinition: CreatePresentationColumn("Priority", PriorityColumnText, typeof(string), 120d, 5, isEditable: true, editorKind: GridColumnEditorKind.Combo, editorItems: priorityOptions, valueKind: "Status")),
                new EditSessionFieldDefinition<DemoGisRecordViewModel>(
                    "Visible",
                    VisibleColumnText,
                    typeof(bool),
                    getter: record => record.Visible,
                    setter: (record, value) => record.Visible = value is bool boolValue && boolValue,
                    fieldPath: nameof(DemoGisRecordViewModel.Visible),
                    valueKind: "Boolean",
                    editorKind: GridColumnEditorKind.CheckBox,
                    validationConstraints: new BooleanValidationConstraints(required: true),
                    gridColumnDefinition: CreatePresentationColumn("Visible", VisibleColumnText, typeof(bool), 90d, 6, isEditable: true, editorKind: GridColumnEditorKind.CheckBox, valueKind: "Boolean")),
                new EditSessionFieldDefinition<DemoGisRecordViewModel>(
                    "EditableFlag",
                    EditableFlagColumnText,
                    typeof(bool),
                    getter: record => record.EditableFlag,
                    setter: (record, value) => record.EditableFlag = value is bool boolValue && boolValue,
                    fieldPath: nameof(DemoGisRecordViewModel.EditableFlag),
                    valueKind: "Boolean",
                    editorKind: GridColumnEditorKind.CheckBox,
                    validationConstraints: new BooleanValidationConstraints(required: true),
                    gridColumnDefinition: CreatePresentationColumn("EditableFlag", EditableFlagColumnText, typeof(bool), 96d, 7, isEditable: true, editorKind: GridColumnEditorKind.CheckBox, valueKind: "Boolean")),
                new EditSessionFieldDefinition<DemoGisRecordViewModel>(
                    "LastInspection",
                    LastInspectionColumnText,
                    typeof(DateTime),
                    getter: record => record.LastInspection,
                    setter: (record, value) => record.LastInspection = value is DateTime dateTimeValue ? dateTimeValue : default,
                    fieldPath: nameof(DemoGisRecordViewModel.LastInspection),
                    valueKind: "DateTime",
                    editorKind: GridColumnEditorKind.DatePicker,
                    validationConstraints: new DateValidationConstraints(required: true, minDate: new DateTime(2020, 1, 1), maxDate: new DateTime(2035, 12, 31)),
                    gridColumnDefinition: CreatePresentationColumn("LastInspection", LastInspectionColumnText, typeof(DateTime), 150d, 8, isEditable: true, editorKind: GridColumnEditorKind.DatePicker, valueKind: "DateTime")),
                new EditSessionFieldDefinition<DemoGisRecordViewModel>(
                    "UpdatedAt",
                    UpdatedAtColumnText,
                    typeof(DateTime),
                    getter: record => record.UpdatedAt,
                    setter: (_, __) => { },
                    fieldPath: nameof(DemoGisRecordViewModel.UpdatedAt),
                    valueKind: "DateTime",
                    gridColumnDefinition: CreatePresentationColumn("UpdatedAt", UpdatedAtColumnText, typeof(DateTime), 170d, 9, isEditable: false, valueKind: "DateTime")),
                new EditSessionFieldDefinition<DemoGisRecordViewModel>(
                    "Owner",
                    OwnerColumnText,
                    typeof(string),
                    getter: record => record.Owner,
                    setter: (record, value) => record.Owner = value as string ?? string.Empty,
                    fieldPath: nameof(DemoGisRecordViewModel.Owner),
                    valueKind: "Text",
                    editorKind: GridColumnEditorKind.Autocomplete,
                    editorItems: ownerOptions,
                    validationConstraints: new TextValidationConstraints(required: true, minLength: 3, maxLength: 120, allowedValues: ownerOptions),
                    gridColumnDefinition: CreatePresentationColumn("Owner", OwnerColumnText, typeof(string), 180d, 10, isEditable: true, editorKind: GridColumnEditorKind.Autocomplete, editorItems: ownerOptions, valueKind: "Text", editorItemsMode: GridEditorItemsMode.RestrictToItems)),
                new EditSessionFieldDefinition<DemoGisRecordViewModel>(
                    "MaintenanceBudget",
                    MaintenanceBudgetColumnText,
                    typeof(decimal),
                    getter: record => record.MaintenanceBudget,
                    setter: (_, __) => { },
                    fieldPath: nameof(DemoGisRecordViewModel.MaintenanceBudget),
                    valueKind: "Currency",
                    validationConstraints: new DecimalValidationConstraints(required: true, minValue: 0m, maxValue: 999999.99m, scale: 2, precision: 8, allowNegative: false),
                    gridColumnDefinition: CreatePresentationColumn("MaintenanceBudget", MaintenanceBudgetColumnText, typeof(decimal), 150d, 11, isEditable: true, valueKind: "Currency")),
                new EditSessionFieldDefinition<DemoGisRecordViewModel>(
                    "CompletionPercent",
                    CompletionPercentColumnText,
                    typeof(decimal),
                    getter: record => record.CompletionPercent,
                    setter: (_, __) => { },
                    fieldPath: nameof(DemoGisRecordViewModel.CompletionPercent),
                    valueKind: "Percent",
                    validationConstraints: new DecimalValidationConstraints(required: true, minValue: 0m, maxValue: 100m, scale: 1, precision: 4, allowNegative: false),
                    gridColumnDefinition: CreatePresentationColumn("CompletionPercent", CompletionPercentColumnText, typeof(decimal), 140d, 12, isEditable: true, valueKind: "Percent")),
                new EditSessionFieldDefinition<DemoGisRecordViewModel>(
                    "ScaleHint",
                    ScaleHintColumnText,
                    typeof(int),
                    getter: record => record.ScaleHint,
                    setter: (record, value) => record.ScaleHint = value is int intValue ? intValue : 0,
                    fieldPath: nameof(DemoGisRecordViewModel.ScaleHint),
                    valueKind: "Number",
                    editorKind: GridColumnEditorKind.MaskedText,
                    editMask: "^[0-9]{0,6}$",
                    validationConstraints: new IntegerValidationConstraints(required: true, minValue: 100, maxValue: 100000, allowZero: false, allowNegative: false),
                    gridColumnDefinition: CreatePresentationColumn("ScaleHint", ScaleHintColumnText, typeof(int), 120d, 13, isEditable: true, editorKind: GridColumnEditorKind.MaskedText, editMask: "^[0-9]{0,6}$", valueKind: "Number")),
            };
        }

        private static GridColumnDefinition CreatePresentationColumn(
            string id,
            string header,
            Type valueType,
            double width,
            int displayIndex,
            bool isEditable,
            bool isVisible = true,
            GridColumnEditorKind editorKind = GridColumnEditorKind.Text,
            IReadOnlyList<string> editorItems = null,
            string editMask = null,
            string valueKind = null,
            GridEditorItemsMode? editorItemsMode = null)
        {
            return new GridColumnDefinition(
                id,
                header,
                width: width,
                displayIndex: displayIndex,
                valueType: valueType,
                isEditable: isEditable,
                isVisible: isVisible,
                editorKind: editorKind,
                editorItems: editorItems,
                editMask: editMask,
                valueKind: valueKind,
                validationConstraints: null,
                editorItemsMode: editorItemsMode);
        }

        private void RebuildScenarioState(bool resetRemotePaging)
        {
            GridGroups = SelectedExample != null && SelectedExample.Id == "grouping"
                ? new[] { new GridGroupDescriptor("Category", GridSortDirection.Ascending) }
                : Array.Empty<GridGroupDescriptor>();

            GridSorts = SelectedExample != null && SelectedExample.Id == "sorting"
                ? new[]
                {
                    new GridSortDescriptor("Category", GridSortDirection.Ascending),
                    new GridSortDescriptor("LastInspection", GridSortDirection.Descending),
                }
                : Array.Empty<GridSortDescriptor>();

            GridSummaries = SelectedExample != null && (SelectedExample.Id == "summaries" || SelectedExample.Id == "summary-designer" || SelectedExample.Id == "state-persistence")
                ? BuildDefaultSummaries()
                : Array.Empty<GridSummaryDescriptor>();

            if (SelectedExample != null && SelectedExample.Id == "hierarchy")
            {
                var hierarchy = DemoGisHierarchyBuilder.Build(_catalog);
                GridHierarchyRoots = hierarchy.Roots;
                GridHierarchyController = hierarchy.Controller;
            }
            else if (IsMasterDetailExample)
            {
                var hierarchy = DemoGisMasterDetailBuilder.Build(_catalog);
                GridHierarchyRoots = hierarchy.Roots;
                GridHierarchyController = hierarchy.Controller;
            }
            else
            {
                GridHierarchyRoots = Array.Empty<PhialeGrid.Core.Hierarchy.GridHierarchyNode<object>>();
                GridHierarchyController = null;
            }

            ActiveLayerSelectorState = IsActiveLayerSelectorExample
                ? DemoActiveLayerSelectorFactory.CreateDefaultState()
                : null;
            IsGridReadOnly = SelectedExample == null || (SelectedExample.Id != "editing" && SelectedExample.Id != "rich-editors" && SelectedExample.Id != "constraints");
            StateStatusText = Localize(_hasSavedState ? DemoTextKeys.DemoToolbarStateReady : DemoTextKeys.DemoToolbarStateEmpty);
            if (ShowTransferTools)
            {
                MarkTransferIdle();
            }
            else
            {
                TransferStatusText = string.Empty;
                TransferPreviewText = string.Empty;
            }

            if (ShowRemoteTools)
            {
                if (resetRemotePaging)
                {
                    _remotePageIndex = 0;
                    _remoteRefreshVersion = 0;
                }

                IsRemoteBusy = false;
                GridRecords = _remoteGridClient == null
                    ? BuildRemotePage(_remotePageIndex)
                    : Array.Empty<DemoGisRecordViewModel>();
                SetRemoteDataState(_remoteGridClient == null ? DemoRemoteDataStateKind.Ready : DemoRemoteDataStateKind.Idle);
            }
            else
            {
                GridRecords = _catalog.GetGisRecords();
                IsRemoteBusy = false;
                SetRemoteDataState(DemoRemoteDataStateKind.Idle);
            }

            RebuildSummaryDesignerOptions();
            RaiseScenarioPropertyChanges();
        }

        public void SetColumnChooserVisibility(string columnId, bool isVisible)
        {
            var match = ColumnChooserItems.FirstOrDefault(item => string.Equals(item.ColumnId, columnId, StringComparison.OrdinalIgnoreCase));
            if (match != null)
            {
                match.IsVisible = isVisible;
            }
        }

        public void SetAllColumnChooserVisibility(bool isVisible)
        {
            foreach (var item in ColumnChooserItems)
            {
                item.IsVisible = isVisible;
            }
        }

        public void SetSavedViewNames(IEnumerable<string> viewNames)
        {
            SavedViewNames = (viewNames ?? Array.Empty<string>()).OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray();
            if (!SavedViewNames.Contains(SelectedSavedViewName, StringComparer.OrdinalIgnoreCase))
            {
                SelectedSavedViewName = SavedViewNames.FirstOrDefault() ?? string.Empty;
            }
        }

        private async Task LoadRemotePageAsync(int requestedPageIndex, bool refreshGeneration)
        {
            if (!ShowRemoteTools)
            {
                return;
            }

            var totalPages = GetRemotePageCount();
            var pageIndex = Math.Max(0, Math.Min(requestedPageIndex, totalPages - 1));
            IsRemoteBusy = true;
            SetRemoteDataState(DemoRemoteDataStateKind.Loading, pageIndex, totalPages);
            RaiseScenarioPropertyChanges();

            try
            {
                if (refreshGeneration)
                {
                    _remoteRefreshVersion++;
                }

                if (_remoteGridClient == null)
                {
                    await Task.Delay(320).ConfigureAwait(true);
                    _remotePageIndex = pageIndex;
                    GridRecords = BuildRemotePage(_remotePageIndex);
                    _remoteTotalCount = _catalog.GetGisRecords().Count;
                }
                else
                {
                    var result = await _remoteGridClient.QueryAsync(
                        new DemoRemoteQueryRequest(
                            pageIndex * RemotePageSize,
                            RemotePageSize,
                            GridSorts,
                            GridFilterGroup.EmptyAnd(),
                            _remoteRefreshVersion),
                        CancellationToken.None).ConfigureAwait(true);

                    _remotePageIndex = pageIndex;
                    _remoteTotalCount = Math.Max(result.TotalCount, result.Items.Count);
                    GridRecords = result.Items;
                }

                IsRemoteBusy = false;
                SetRemoteDataState(GridRecords.Count == 0 ? DemoRemoteDataStateKind.Empty : DemoRemoteDataStateKind.Ready);
                RaiseScenarioPropertyChanges();
            }
            catch (DemoRemoteQueryException ex) when (ex.FailureKind == DemoRemoteQueryFailureKind.Forbidden)
            {
                IsRemoteBusy = false;
                _remoteTotalCount = 0;
                GridRecords = Array.Empty<DemoGisRecordViewModel>();
                SetRemoteDataState(DemoRemoteDataStateKind.Forbidden, failureDetail: ex.Message);
                RaiseScenarioPropertyChanges();
            }
            catch (Exception ex)
            {
                IsRemoteBusy = false;
                _remoteTotalCount = 0;
                GridRecords = Array.Empty<DemoGisRecordViewModel>();
                SetRemoteDataState(DemoRemoteDataStateKind.Error, failureDetail: ex.Message);
                RaiseScenarioPropertyChanges();
            }
        }

        private IReadOnlyList<DemoGisRecordViewModel> BuildRemotePage(int pageIndex)
        {
            var source = BuildRemoteSource();
            var safePageIndex = Math.Max(0, Math.Min(pageIndex, GetRemotePageCount() - 1));
            return source
                .Skip(safePageIndex * RemotePageSize)
                .Take(RemotePageSize)
                .ToArray();
        }

        private IReadOnlyList<DemoGisRecordViewModel> BuildRemoteSource()
        {
            return _catalog.GetGisRecords()
                .Select((record, index) =>
                {
                    if (_remoteRefreshVersion == 0)
                    {
                        return record;
                    }

                    var magnitude = _remoteRefreshVersion * (index % 3 + 1);
                    if (string.Equals(record.GeometryType, "Polygon", StringComparison.OrdinalIgnoreCase))
                    {
                        record.AreaSquareMeters += magnitude * 12.5m;
                    }

                    if (string.Equals(record.GeometryType, "LineString", StringComparison.OrdinalIgnoreCase))
                    {
                        record.LengthMeters += magnitude * 8.75m;
                    }

                    record.LastInspection = record.LastInspection.AddDays(magnitude);
                    if (index % 4 == 0)
                    {
                        record.Status = CycleStatus(record.Status);
                    }

                    return record;
                })
                .ToArray();
        }

        private int GetRemotePageCount()
        {
            var totalRows = _remoteGridClient == null
                ? Math.Max(_remoteTotalCount, _catalog.GetGisRecords().Count)
                : Math.Max(_remoteTotalCount, 0);
            return Math.Max(1, (int)Math.Ceiling(totalRows / (double)RemotePageSize));
        }

        private void SetRemoteDataState(
            DemoRemoteDataStateKind state,
            int? pageIndexOverride = null,
            int? totalPagesOverride = null,
            string failureDetail = null)
        {
            RemoteDataState = state;
            _remoteFailureDetail = failureDetail ?? string.Empty;
            RefreshRemoteStatusText(pageIndexOverride, totalPagesOverride);
        }

        private void RefreshRemoteStatusText(int? pageIndexOverride = null, int? totalPagesOverride = null)
        {
            if (!ShowRemoteTools)
            {
                RemoteStatusText = string.Empty;
                return;
            }

            var pageNumber = (pageIndexOverride ?? _remotePageIndex) + 1;
            var totalPages = totalPagesOverride ?? GetRemotePageCount();
            switch (RemoteDataState)
            {
                case DemoRemoteDataStateKind.Loading:
                    RemoteStatusText = string.Format(CultureInfo.CurrentCulture, Localize(DemoTextKeys.DemoToolbarRemoteLoading), pageNumber, totalPages);
                    break;
                case DemoRemoteDataStateKind.Ready:
                    RemoteStatusText = string.Format(CultureInfo.CurrentCulture, Localize(DemoTextKeys.DemoToolbarRemoteStatus), pageNumber, totalPages);
                    break;
                case DemoRemoteDataStateKind.Empty:
                    RemoteStatusText = Localize(DemoTextKeys.DemoToolbarRemoteEmpty);
                    break;
                case DemoRemoteDataStateKind.Forbidden:
                    RemoteStatusText = Localize(DemoTextKeys.DemoToolbarRemoteForbidden);
                    break;
                case DemoRemoteDataStateKind.Error:
                    RemoteStatusText = string.Format(CultureInfo.CurrentCulture, Localize(DemoTextKeys.DemoToolbarRemoteError), _remoteFailureDetail);
                    break;
                default:
                    RemoteStatusText = string.Empty;
                    break;
            }
        }

        private GridSummaryDescriptor[] BuildDefaultSummaries()
        {
            return new[]
            {
                BuildSummaryDescriptor("AreaSquareMeters", GridSummaryType.Sum),
                BuildSummaryDescriptor("LengthMeters", GridSummaryType.Sum),
                BuildSummaryDescriptor("ObjectId", GridSummaryType.Count),
            };
        }

        private GridSummaryDescriptor BuildSummaryDescriptor(string columnId, GridSummaryType summaryType)
        {
            var column = GridColumns.FirstOrDefault(item => string.Equals(item.Id, columnId, StringComparison.OrdinalIgnoreCase));
            var valueType = column?.ValueType ?? typeof(object);
            return new GridSummaryDescriptor(columnId, summaryType, valueType);
        }

        private void RebuildSummaryDesignerOptions()
        {
            AvailableSummaryColumns = GridColumns
                .Select(column => new DemoSummaryColumnOptionViewModel(column.Id, column.Header, GetAllowedSummaryTypes(column.ValueType)))
                .Where(option => option.AllowedTypes.Count > 0)
                .ToArray();

            if (SelectedSummaryColumn == null ||
                !AvailableSummaryColumns.Any(option => string.Equals(option.ColumnId, SelectedSummaryColumn.ColumnId, StringComparison.OrdinalIgnoreCase)))
            {
                SelectedSummaryColumn = AvailableSummaryColumns.FirstOrDefault();
            }
            else
            {
                SelectedSummaryColumn = AvailableSummaryColumns.FirstOrDefault(option =>
                    string.Equals(option.ColumnId, SelectedSummaryColumn.ColumnId, StringComparison.OrdinalIgnoreCase));
            }

            RebuildAvailableSummaryTypes();
            RebuildConfiguredSummaries();
        }

        private void RebuildAvailableSummaryTypes()
        {
            var allowedTypes = SelectedSummaryColumn?.AllowedTypes ?? Array.Empty<GridSummaryType>();
            AvailableSummaryTypes = allowedTypes
                .Select(type => new DemoSummaryTypeOptionViewModel(type, LocalizeSummaryType(type)))
                .ToArray();

            if (SelectedSummaryType == null || !AvailableSummaryTypes.Any(option => option.Type == SelectedSummaryType.Type))
            {
                SelectedSummaryType = AvailableSummaryTypes.FirstOrDefault();
            }
            else
            {
                SelectedSummaryType = AvailableSummaryTypes.FirstOrDefault(option => option.Type == SelectedSummaryType.Type);
            }
        }

        private void RebuildConfiguredSummaries()
        {
            ConfiguredSummaries = GridSummaries
                .Select(summary =>
                {
                    var header = GridColumns.FirstOrDefault(column => string.Equals(column.Id, summary.ColumnId, StringComparison.OrdinalIgnoreCase))?.Header ?? summary.ColumnId;
                    return new DemoConfiguredSummaryViewModel(summary.ColumnId, summary.Type, header + " · " + LocalizeSummaryType(summary.Type));
                })
                .ToArray();
        }

        private IReadOnlyList<GridSummaryType> GetAllowedSummaryTypes(Type valueType)
        {
            var normalizedType = Nullable.GetUnderlyingType(valueType) ?? valueType ?? typeof(object);
            if (normalizedType == typeof(decimal) ||
                normalizedType == typeof(double) ||
                normalizedType == typeof(float) ||
                normalizedType == typeof(int) ||
                normalizedType == typeof(long) ||
                normalizedType == typeof(short) ||
                normalizedType == typeof(byte))
            {
                return new[] { GridSummaryType.Count, GridSummaryType.Sum, GridSummaryType.Average, GridSummaryType.Min, GridSummaryType.Max };
            }

            if (normalizedType == typeof(DateTime))
            {
                return new[] { GridSummaryType.Count, GridSummaryType.Min, GridSummaryType.Max };
            }

            if (normalizedType == typeof(string))
            {
                return new[] { GridSummaryType.Count, GridSummaryType.Min, GridSummaryType.Max };
            }

            return new[] { GridSummaryType.Count };
        }

        private string LocalizeSummaryType(GridSummaryType summaryType)
        {
            switch (summaryType)
            {
                case GridSummaryType.Count:
                    return Localize(DemoTextKeys.SummaryTypeCount);
                case GridSummaryType.Sum:
                    return Localize(DemoTextKeys.SummaryTypeSum);
                case GridSummaryType.Average:
                    return Localize(DemoTextKeys.SummaryTypeAverage);
                case GridSummaryType.Min:
                    return Localize(DemoTextKeys.SummaryTypeMin);
                case GridSummaryType.Max:
                    return Localize(DemoTextKeys.SummaryTypeMax);
                default:
                    return summaryType.ToString();
            }
        }

        private string[] BuildOwnerOptions()
        {
            return _catalog.GetGisRecords()
                .Select(record => record.Owner)
                .Where(owner => !string.IsNullOrWhiteSpace(owner))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(owner => owner, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static string[] BuildStatusOptions()
        {
            return new[]
            {
                "Active",
                "Verified",
                "NeedsReview",
                "UnderMaintenance",
                "Planned",
                "Retired",
            };
        }

        private static string[] BuildPriorityOptions()
        {
            return new[]
            {
                "Critical",
                "High",
                "Medium",
                "Low",
            };
        }

        private void RebuildSections()
        {
            VisibleSections = string.IsNullOrWhiteSpace(_selectedDrawerGroupId)
                ? _catalog.BuildSections(LanguageCode, SearchText)
                : _catalog.BuildSectionsForDrawerGroup(LanguageCode, SearchText, _selectedDrawerGroupId);
            OnPropertyChanged(nameof(HasOverviewResults));
            OnPropertyChanged(nameof(HasNoOverviewResults));
        }

        private void RebuildDrawerGroups()
        {
            DrawerGroups = _catalog.BuildDrawerGroups(LanguageCode, _selectedDrawerGroupId);
            OnPropertyChanged(nameof(SelectedDrawerGroupTitle));
            OnPropertyChanged(nameof(SelectedDrawerGroupDescription));
            OnPropertyChanged(nameof(WorkspaceOverviewTitle));
            OnPropertyChanged(nameof(WorkspaceOverviewSubtitle));
            OnPropertyChanged(nameof(IsFoundationsDrawerSelected));
            OnPropertyChanged(nameof(IsArchitectureDrawerSelected));
            OnPropertyChanged(nameof(IsGridDrawerSelected));
            OnPropertyChanged(nameof(IsActiveLayerSelectorDrawerSelected));
            OnPropertyChanged(nameof(IsWebComponentsDrawerSelected));
        }

        private void RebuildCodeFiles()
        {
            var selectedFileName = SelectedCodeFile?.FileName;
            AvailableCodeFiles = SelectedExample == null
                ? Array.Empty<DemoCodeFileViewModel>()
                : _catalog.GetCodeFiles(_platformKey, SelectedExample.Id);

            SelectedCodeFile = AvailableCodeFiles.FirstOrDefault(file => file.FileName == selectedFileName) ?? AvailableCodeFiles.FirstOrDefault();
        }

        private static bool IsWebComponentsExampleDefinition(DemoExampleDefinition example)
        {
            return example != null &&
                   string.Equals(example.ComponentId, "web-components", StringComparison.OrdinalIgnoreCase);
        }

        private void RaiseLocalizedPropertyChanges()
        {
            OnPropertyChanged(nameof(AppTitle));
            OnPropertyChanged(nameof(AppSubtitle));
            OnPropertyChanged(nameof(ComponentsTitle));
            OnPropertyChanged(nameof(DrawerOpenText));
            OnPropertyChanged(nameof(DrawerCloseText));
            OnPropertyChanged(nameof(SearchPlaceholder));
            OnPropertyChanged(nameof(OverviewTitle));
            OnPropertyChanged(nameof(OverviewSubtitle));
            OnPropertyChanged(nameof(BackToOverviewText));
            OnPropertyChanged(nameof(DemoTabText));
            OnPropertyChanged(nameof(CodeTabText));
            OnPropertyChanged(nameof(ExplanationTabText));
            OnPropertyChanged(nameof(FileLabelText));
            OnPropertyChanged(nameof(LanguageLabelText));
            OnPropertyChanged(nameof(ThemeLabelText));
            OnPropertyChanged(nameof(ThemeOptions));
            OnPropertyChanged(nameof(EmptySearchText));
            OnPropertyChanged(nameof(FoundationsComponentText));
            OnPropertyChanged(nameof(FoundationsComponentDescription));
            OnPropertyChanged(nameof(ArchitectureComponentText));
            OnPropertyChanged(nameof(ArchitectureComponentDescription));
            OnPropertyChanged(nameof(WebComponentsComponentText));
            OnPropertyChanged(nameof(WebComponentsComponentDescription));
            OnPropertyChanged(nameof(WebComponentsScrollHostTitle));
            OnPropertyChanged(nameof(WebComponentsScrollHostDescription));
            OnPropertyChanged(nameof(WebComponentsScrollHostPointOne));
            OnPropertyChanged(nameof(WebComponentsScrollHostPointTwo));
            OnPropertyChanged(nameof(WebComponentsScrollHostPointThree));
            OnPropertyChanged(nameof(YamlUiComponentText));
            OnPropertyChanged(nameof(YamlUiComponentDescription));
            OnPropertyChanged(nameof(LicenseComponentText));
            OnPropertyChanged(nameof(LicenseComponentDescription));
            OnPropertyChanged(nameof(ApplicationStateManagerComponentText));
            OnPropertyChanged(nameof(DefinitionManagerComponentText));
            OnPropertyChanged(nameof(DefinitionManagerComponentDescription));
            OnPropertyChanged(nameof(GridComponentText));
            OnPropertyChanged(nameof(GridComponentDescription));
            OnPropertyChanged(nameof(ActiveLayerSelectorComponentText));
            OnPropertyChanged(nameof(ActiveLayerSelectorComponentDescription));
            OnPropertyChanged(nameof(SelectedComponentText));
            OnPropertyChanged(nameof(SelectedDrawerGroupTitle));
            OnPropertyChanged(nameof(SelectedDrawerGroupDescription));
            OnPropertyChanged(nameof(WorkspaceOverviewTitle));
            OnPropertyChanged(nameof(WorkspaceOverviewSubtitle));
            OnPropertyChanged(nameof(GroupingBarText));
            OnPropertyChanged(nameof(PreviewHintText));
            OnPropertyChanged(nameof(CategoryColumnText));
            OnPropertyChanged(nameof(DescriptionColumnText));
            OnPropertyChanged(nameof(ObjectNameColumnText));
            OnPropertyChanged(nameof(ObjectIdColumnText));
            OnPropertyChanged(nameof(GeometryTypeColumnText));
            OnPropertyChanged(nameof(MunicipalityColumnText));
            OnPropertyChanged(nameof(DistrictColumnText));
            OnPropertyChanged(nameof(StatusColumnText));
            OnPropertyChanged(nameof(PriorityColumnText));
            OnPropertyChanged(nameof(AreaColumnText));
            OnPropertyChanged(nameof(LengthColumnText));
            OnPropertyChanged(nameof(LastInspectionColumnText));
            OnPropertyChanged(nameof(UpdatedAtColumnText));
            OnPropertyChanged(nameof(OwnerColumnText));
            OnPropertyChanged(nameof(ScaleHintColumnText));
            OnPropertyChanged(nameof(MaintenanceBudgetColumnText));
            OnPropertyChanged(nameof(CompletionPercentColumnText));
            OnPropertyChanged(nameof(VisibleColumnText));
            OnPropertyChanged(nameof(EditableFlagColumnText));
            OnPropertyChanged(nameof(MetricDeckTitle));
            OnPropertyChanged(nameof(PreviewUsageTitle));
            OnPropertyChanged(nameof(PreviewCodeTitle));
            OnPropertyChanged(nameof(PlatformBadgeText));
            OnPropertyChanged(nameof(ConstraintRulesTitle));
            OnPropertyChanged(nameof(ConstraintRules));
            OnPropertyChanged(nameof(FoundationsIntroTitle));
            OnPropertyChanged(nameof(FoundationsIntroDescription));
            OnPropertyChanged(nameof(FoundationsTypographyTitle));
            OnPropertyChanged(nameof(FoundationsTypographyDescription));
            OnPropertyChanged(nameof(FoundationsColorsTitle));
            OnPropertyChanged(nameof(FoundationsColorsDescription));
            OnPropertyChanged(nameof(FoundationsRhythmTitle));
            OnPropertyChanged(nameof(FoundationsRhythmDescription));
            OnPropertyChanged(nameof(FoundationsTextColorsTitle));
            OnPropertyChanged(nameof(FoundationsSurfaceColorsTitle));
            OnPropertyChanged(nameof(FoundationsFormShellColorsTitle));
            OnPropertyChanged(nameof(FoundationsFormShellColorsDescription));
            OnPropertyChanged(nameof(FoundationsFormShellSpacingTitle));
            OnPropertyChanged(nameof(FoundationsFormShellSpacingDescription));
            OnPropertyChanged(nameof(FoundationsAccentColorsTitle));
            OnPropertyChanged(nameof(FoundationsShapesTitle));
            OnPropertyChanged(nameof(FoundationsSpacingTitle));
            OnPropertyChanged(nameof(FoundationsTokenLabel));
            OnPropertyChanged(nameof(FoundationsRoleLabel));
            OnPropertyChanged(nameof(FoundationsUseLabel));
            OnPropertyChanged(nameof(FoundationsDayLabel));
            OnPropertyChanged(nameof(FoundationsNightLabel));
            OnPropertyChanged(nameof(FoundationsValueLabel));
            OnPropertyChanged(nameof(ApplicationStateManagerIntroTitle));
            OnPropertyChanged(nameof(ApplicationStateManagerIntroDescription));
            OnPropertyChanged(nameof(ApplicationStateManagerResponsibilitiesTitle));
            OnPropertyChanged(nameof(ApplicationStateManagerOutOfScopeTitle));
            OnPropertyChanged(nameof(ApplicationStateManagerCooperationTitle));
            OnPropertyChanged(nameof(ApplicationStateManagerCooperationDescription));
            OnPropertyChanged(nameof(ApplicationStateManagerLiveSampleTitle));
            OnPropertyChanged(nameof(ApplicationStateManagerLiveSampleDescription));
            OnPropertyChanged(nameof(ApplicationStateManagerFieldSharedManager));
            OnPropertyChanged(nameof(ApplicationStateManagerFieldStateKey));
            OnPropertyChanged(nameof(ApplicationStateManagerFieldRestoreBehavior));
            OnPropertyChanged(nameof(ApplicationStateManagerFieldSaveBehavior));
            OnPropertyChanged(nameof(ApplicationStateManagerSharedManagerValue));
            OnPropertyChanged(nameof(ApplicationStateManagerRestoreBehaviorValue));
            OnPropertyChanged(nameof(ApplicationStateManagerSaveBehaviorValue));
            OnPropertyChanged(nameof(ApplicationStateManagerStateKey));
            OnPropertyChanged(nameof(DefinitionManagerIntroTitle));
            OnPropertyChanged(nameof(DefinitionManagerIntroDescription));
            OnPropertyChanged(nameof(DefinitionManagerResponsibilitiesTitle));
            OnPropertyChanged(nameof(DefinitionManagerOutOfScopeTitle));
            OnPropertyChanged(nameof(DefinitionManagerResolutionTitle));
            OnPropertyChanged(nameof(DefinitionManagerResolutionDescription));
            OnPropertyChanged(nameof(DefinitionManagerSampleTitle));
            OnPropertyChanged(nameof(DefinitionManagerSampleDescription));
            OnPropertyChanged(nameof(DefinitionManagerFieldDefinitionKey));
            OnPropertyChanged(nameof(DefinitionManagerFieldSource));
            OnPropertyChanged(nameof(DefinitionManagerFieldKind));
            OnPropertyChanged(nameof(DefinitionManagerFieldComponent));
            OnPropertyChanged(nameof(DefinitionManagerFieldConsumer));
            OnPropertyChanged(nameof(DefinitionManagerFieldStateOverlay));
            OnPropertyChanged(nameof(LicenseMyPlaceholderTitle));
            OnPropertyChanged(nameof(LicenseMyPlaceholderDescription));
            OnPropertyChanged(nameof(LicenseThirdPartyIntroTitle));
            OnPropertyChanged(nameof(LicenseThirdPartyIntroDescription));
            OnPropertyChanged(nameof(LicenseFieldUsedBy));
            OnPropertyChanged(nameof(LicenseFieldLicense));
            OnPropertyChanged(nameof(LicenseFieldCopyright));
            OnPropertyChanged(nameof(LicenseFieldRequirement));
            OnPropertyChanged(nameof(LicenseFieldFiles));
            OnPropertyChanged(nameof(CopySelectionText));
            OnPropertyChanged(nameof(SelectVisibleRowsText));
            OnPropertyChanged(nameof(ClearSelectionText));
            OnPropertyChanged(nameof(AddRecordText));
            OnPropertyChanged(nameof(EditRecordText));
            OnPropertyChanged(nameof(ApplyCurrentEditText));
            OnPropertyChanged(nameof(CancelCurrentEditText));
            OnPropertyChanged(nameof(DirtyRowsBadgeLabelText));
            OnPropertyChanged(nameof(ValidationIssuesBadgeLabelText));
            OnPropertyChanged(nameof(CommitText));
            OnPropertyChanged(nameof(CancelText));
            OnPropertyChanged(nameof(SaveStateText));
            OnPropertyChanged(nameof(RestoreStateText));
            OnPropertyChanged(nameof(ResetStateText));
            OnPropertyChanged(nameof(ToggleOwnerColumnText));
            OnPropertyChanged(nameof(FreezeObjectNameText));
            OnPropertyChanged(nameof(UnfreezeAllText));
            OnPropertyChanged(nameof(AutoFitText));
            OnPropertyChanged(nameof(ShowAllColumnsText));
            OnPropertyChanged(nameof(ColumnChooserText));
            OnPropertyChanged(nameof(LayoutToolsHintText));
            OnPropertyChanged(nameof(GridSearchPlaceholderText));
            OnPropertyChanged(nameof(ApplySearchText));
            OnPropertyChanged(nameof(ClearSearchText));
            OnPropertyChanged(nameof(FocusMunicipalityFilterText));
            OnPropertyChanged(nameof(FocusOwnerFilterText));
            OnPropertyChanged(nameof(ClearColumnFiltersText));
            OnPropertyChanged(nameof(PrevPageText));
            OnPropertyChanged(nameof(NextPageText));
            OnPropertyChanged(nameof(RefreshRemoteText));
            OnPropertyChanged(nameof(ExpandHierarchyText));
            OnPropertyChanged(nameof(CollapseHierarchyText));
            OnPropertyChanged(nameof(MasterDetailPlacementText));
            OnPropertyChanged(nameof(ViewNameText));
            OnPropertyChanged(nameof(SaveViewText));
            OnPropertyChanged(nameof(ApplyViewText));
            OnPropertyChanged(nameof(DeleteViewText));
            OnPropertyChanged(nameof(ExportCsvText));
            OnPropertyChanged(nameof(SaveFoundationsPdfText));
            OnPropertyChanged(nameof(SaveDemoBookPdfText));
            OnPropertyChanged(nameof(ImportSampleCsvText));
            OnPropertyChanged(nameof(RestoreDataText));
            OnPropertyChanged(nameof(AddSummaryText));
            OnPropertyChanged(nameof(ResetSummariesText));
            OnPropertyChanged(nameof(FoundationsExportErrorTitle));
            OnPropertyChanged(nameof(FoundationsExportErrorMessage));
            OnPropertyChanged(nameof(DemoBookExportErrorTitle));
            OnPropertyChanged(nameof(DemoBookExportErrorMessage));
            OnPropertyChanged(nameof(DetailHeadline));
            OnPropertyChanged(nameof(SelectedExampleTitle));
            OnPropertyChanged(nameof(SelectedExampleDescription));
            OnPropertyChanged(nameof(SourceCodeText));
            OnPropertyChanged(nameof(StateStatusText));
            RefreshRemoteStatusText();
            OnPropertyChanged(nameof(TransferStatusText));
            OnPropertyChanged(nameof(TransferPreviewText));
            RebuildDrawerGroups();
            RebuildDefinitionManagerState();
            RebuildSummaryDesignerOptions();
            RaiseScenarioPropertyChanges();
        }

        private string Localize(string key)
        {
            return _catalog.Localize(LanguageCode, key);
        }

        private bool IsPolishLanguage => string.Equals(LanguageCode, "pl", StringComparison.OrdinalIgnoreCase);

        private void SyncSelectedLanguage()
        {
            _selectedLanguage = LanguageOptions.FirstOrDefault(option => option.Code == LanguageCode) ?? LanguageOptions.FirstOrDefault();
            OnPropertyChanged(nameof(SelectedLanguage));
        }

        private void RaiseScenarioPropertyChanges()
        {
            OnPropertyChanged(nameof(ShowSelectionTools));
            OnPropertyChanged(nameof(ShowGridModeTools));
            OnPropertyChanged(nameof(ShowEditingTools));
            OnPropertyChanged(nameof(ShowConstraintTools));
            OnPropertyChanged(nameof(ShowStateTools));
            OnPropertyChanged(nameof(ShowLayoutTools));
            OnPropertyChanged(nameof(ShowRemoteTools));
            OnPropertyChanged(nameof(ShowHierarchyTools));
            OnPropertyChanged(nameof(IsMasterDetailExample));
            OnPropertyChanged(nameof(ShowMasterDetailPlacementToggle));
            OnPropertyChanged(nameof(IsMasterDetailOutside));
            OnPropertyChanged(nameof(ShowGridTopCommandRegionContent));
            OnPropertyChanged(nameof(ShowGridSideToolRegionContent));
            OnPropertyChanged(nameof(HasDemoToolbar));
            OnPropertyChanged(nameof(IsGridExample));
            OnPropertyChanged(nameof(IsConstraintsExample));
            OnPropertyChanged(nameof(IsActiveLayerSelectorExample));
            OnPropertyChanged(nameof(IsFoundationsExample));
            OnPropertyChanged(nameof(IsApplicationStateManagerExample));
            OnPropertyChanged(nameof(IsDefinitionManagerExample));
            OnPropertyChanged(nameof(IsWebComponentsExample));
            OnPropertyChanged(nameof(IsYamlUiExample));
            OnPropertyChanged(nameof(IsYamlInputsExample));
            OnPropertyChanged(nameof(IsYamlPrimitivesExample));
            OnPropertyChanged(nameof(IsYamlDocumentExample));
            OnPropertyChanged(nameof(IsYamlActionsExample));
            OnPropertyChanged(nameof(IsYamlDocumentSurfaceExample));
            OnPropertyChanged(nameof(IsLicenseExample));
            OnPropertyChanged(nameof(IsWebHostExample));
            OnPropertyChanged(nameof(IsPdfViewerExample));
            OnPropertyChanged(nameof(IsReportDesignerExample));
            OnPropertyChanged(nameof(IsMonacoEditorExample));
            OnPropertyChanged(nameof(IsWebComponentScrollHostExample));
            OnPropertyChanged(nameof(IsMyLicenseExample));
            OnPropertyChanged(nameof(IsThirdPartyLicensesExample));
            OnPropertyChanged(nameof(IsFoundationsDrawerSelected));
            OnPropertyChanged(nameof(IsArchitectureDrawerSelected));
            OnPropertyChanged(nameof(IsGridDrawerSelected));
            OnPropertyChanged(nameof(IsActiveLayerSelectorDrawerSelected));
            OnPropertyChanged(nameof(IsWebComponentsDrawerSelected));
            OnPropertyChanged(nameof(ShowWebComponentsExplanationTab));
            OnPropertyChanged(nameof(IsYamlUiDrawerSelected));
            OnPropertyChanged(nameof(IsLicenseDrawerSelected));
            OnPropertyChanged(nameof(ShowGridSurface));
            OnPropertyChanged(nameof(ShowActiveLayerSelectorSurface));
            OnPropertyChanged(nameof(ShowFoundationsSurface));
            OnPropertyChanged(nameof(ShowApplicationStateManagerSurface));
            OnPropertyChanged(nameof(ShowDefinitionManagerSurface));
            OnPropertyChanged(nameof(ShowYamlUiSurface));
            OnPropertyChanged(nameof(ShowWebComponentsSurface));
            OnPropertyChanged(nameof(ShowLicenseSurface));
            OnPropertyChanged(nameof(ShowWebHostSurface));
            OnPropertyChanged(nameof(ShowPdfViewerSurface));
            OnPropertyChanged(nameof(ShowReportDesignerSurface));
            OnPropertyChanged(nameof(ShowMonacoEditorSurface));
            OnPropertyChanged(nameof(ShowSearchTools));
            OnPropertyChanged(nameof(ShowPersonalizationTools));
            OnPropertyChanged(nameof(ShowTransferTools));
            OnPropertyChanged(nameof(ShowSummaryDesignerTools));
            OnPropertyChanged(nameof(GridSideToolRegionTitleText));
            OnPropertyChanged(nameof(ShowFilteringTools));
            OnPropertyChanged(nameof(ShowGridEditCommandBar));
            OnPropertyChanged(nameof(CanMovePreviousRemotePage));
            OnPropertyChanged(nameof(CanMoveNextRemotePage));
            OnPropertyChanged(nameof(CanRefreshRemotePage));
            OnPropertyChanged(nameof(RemoteStatusText));
        }

        private void SetMasterDetailOutside(bool isOutside)
        {
            if (_isMasterDetailOutside == isOutside)
            {
                return;
            }

            _isMasterDetailOutside = isOutside;
            OnPropertyChanged(nameof(IsMasterDetailOutside));
            OnPropertyChanged(nameof(MasterDetailPlacementText));
        }

        private static string CycleStatus(string currentStatus)
        {
            var statuses = new[]
            {
                "Active",
                "Verified",
                "NeedsReview",
                "UnderMaintenance",
                "Planned",
                "Retired",
            };

            var index = Array.FindIndex(statuses, status => string.Equals(status, currentStatus, StringComparison.OrdinalIgnoreCase));
            if (index < 0)
            {
                return statuses[0];
            }

            return statuses[(index + 1) % statuses.Length];
        }

        private static string NormalizeThemeCode(string themeCode, string fallbackThemeCode = "system")
        {
            if (string.IsNullOrWhiteSpace(themeCode))
            {
                return NormalizeThemeFallback(fallbackThemeCode);
            }

            var normalized = themeCode.Trim().ToLowerInvariant();
            if (normalized == "day" || normalized == "night" || normalized == "system")
            {
                return normalized;
            }

            return NormalizeThemeFallback(fallbackThemeCode);
        }

        private static string NormalizeThemeFallback(string themeCode)
        {
            var normalized = themeCode?.Trim().ToLowerInvariant();
            if (normalized == "day" || normalized == "night" || normalized == "system")
            {
                return normalized;
            }

            return "system";
        }

        private string GetDrawerGroupTitle(string drawerGroupId)
        {
            if (string.IsNullOrWhiteSpace(drawerGroupId))
            {
                return null;
            }

            switch (NormalizeDrawerGroupId(drawerGroupId))
            {
                case "foundations":
                    return FoundationsComponentText;
                case "architecture":
                    return ArchitectureComponentText;
                case "active-layer-selector":
                    return ActiveLayerSelectorComponentText;
                case "yaml-ui":
                    return YamlUiComponentText;
                case "web-components":
                    return WebComponentsComponentText;
                case "license":
                    return LicenseComponentText;
                case "grid":
                    return GridComponentText;
                default:
                    return null;
            }
        }

        private string GetDrawerGroupDescription(string drawerGroupId)
        {
            if (string.IsNullOrWhiteSpace(drawerGroupId))
            {
                return null;
            }

            switch (NormalizeDrawerGroupId(drawerGroupId))
            {
                case "foundations":
                    return FoundationsComponentDescription;
                case "architecture":
                    return ArchitectureComponentDescription;
                case "active-layer-selector":
                    return ActiveLayerSelectorComponentDescription;
                case "yaml-ui":
                    return YamlUiComponentDescription;
                case "web-components":
                    return WebComponentsComponentDescription;
                case "license":
                    return LicenseComponentDescription;
                case "grid":
                    return GridComponentDescription;
                default:
                    return null;
            }
        }

        private static string NormalizeDrawerGroupId(string drawerGroupId)
        {
            return string.IsNullOrWhiteSpace(drawerGroupId)
                ? string.Empty
                : drawerGroupId.Trim().ToLowerInvariant();
        }


        private DemoResolvedDefinitionViewModel BuildResolvedDefinitionViewModel(string definitionKey)
        {
            if (!_definitionManager.TryResolve<DemoComponentDefinition>(definitionKey, out var resolution) || resolution?.Definition == null)
            {
                return CreateEmptyResolvedDefinitionViewModel();
            }

            var definition = resolution.Definition;
            return new DemoResolvedDefinitionViewModel(
                resolution.DefinitionKey,
                resolution.SourceId,
                definition.DefinitionKind,
                definition.ComponentId,
                Localize(definition.TitleKey),
                Localize(definition.SummaryKey),
                Localize(definition.ConsumerHintKey),
                Localize(definition.StateOverlayHintKey),
                definition.ResponsibilityTextKeys.Select(Localize).ToArray(),
                definition.OutOfScopeTextKeys.Select(Localize).ToArray(),
                definition.Fields);
        }

        private static DemoResolvedDefinitionViewModel CreateEmptyResolvedDefinitionViewModel()
        {
            return new DemoResolvedDefinitionViewModel(
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                Array.Empty<string>(),
                Array.Empty<string>(),
                Array.Empty<DemoDefinitionField>());
        }
    }
}
