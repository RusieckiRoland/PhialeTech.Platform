using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Shapes;
using System.Windows.Threading;
using PhialeGrid.Core;
using PhialeGrid.Core.Capabilities;
using PhialeGrid.Core.Columns;
using PhialeGrid.Core.Data;
using PhialeGrid.Core.Details;
using PhialeGrid.Core.Editing;
using PhialeGrid.Core.Export;
using PhialeGrid.Core.Hierarchy;
using PhialeGrid.Core.Input;
using PhialeGrid.Core.Interaction;
using PhialeGrid.Core.Presentation;
using PhialeGrid.Core.Query;
using PhialeGrid.Core.Regions;
using PhialeGrid.Core.Rendering;
using PhialeGrid.Core.State;
using PhialeGrid.Core.Summaries;
using PhialeGrid.Core.Surface;
using PhialeGrid.Core.Validation;
using PhialeGrid.Core.Virtualization;
using PhialeGrid.Localization;
using PhialeTech.PhialeGrid.Wpf.Controls.Editing;
using PhialeTech.PhialeGrid.Wpf.Diagnostics;
using PhialeTech.PhialeGrid.Wpf.Regions;
using PhialeTech.PhialeGrid.Wpf.Surface;
using PhialeTech.PhialeGrid.Wpf.Surface.Presenters;
using UniversalInput.Contracts;

namespace PhialeTech.PhialeGrid.Wpf.Controls
{
    public enum GridMasterDetailHeaderPlacementMode
    {
        Outside,
        Inside,
    }

    public partial class PhialeGrid : UserControl, INotifyPropertyChanged
    {
        private enum GridHierarchyPresentationMode
        {
            Tree,
            MasterDetail,
        }

        private const string GroupingDragFormat = "PhialeGrid.Wpf.ColumnId";
        private const double RegionDragThreshold = 12d;
        private const double RowDetailToggleWidth = 16d;
        private static readonly long AutoTouchPromotionWindowTicks = (long)(Stopwatch.Frequency * 0.8d);
        private const double ClassicColumnResizeStep = 24d;
        private const double TouchColumnResizeStep = 48d;
        private static readonly GridCellBindingConverter CellBindingConverter = new GridCellBindingConverter();

        private readonly GridGroupingController _groupingController = new GridGroupingController();
        private readonly GridSortInteractionController _sortInteractionController = new GridSortInteractionController();
        private readonly GridCurrentCellDescriptionBuilder _currentCellDescriptionBuilder = new GridCurrentCellDescriptionBuilder();
        private readonly ICommand _openColumnMenuCommand;
        private readonly GridSurfaceCoordinator _surfaceCoordinator = new GridSurfaceCoordinator();
        private readonly GridSurfaceUniversalInputAdapter _surfaceInputAdapter = new GridSurfaceUniversalInputAdapter();
        private readonly WpfGridRegionLayoutAdapter _regionLayoutAdapter;
        private readonly Dictionary<string, PropertyInfo> _propertyCache = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);
        private readonly InMemoryEditSessionDataSource<object> _editSessionDataSource = new InMemoryEditSessionDataSource<object>();
        private readonly EditSessionContext<object> _internalEditSessionContext;
        private IEditSessionContext _editSessionContext;
        private readonly SurfaceGridDataBridge _surfaceDataBridge;
        private static readonly Uri DayThemeTokensUri = new Uri("pack://application:,,,/PhialeTech.Styles.Wpf;component/Themes/ThemeTokens.Day.xaml", UriKind.Absolute);
        private static readonly Uri NightThemeTokensUri = new Uri("pack://application:,,,/PhialeTech.Styles.Wpf;component/Themes/ThemeTokens.Night.xaml", UriKind.Absolute);
        private static readonly Uri HighContrastThemeTokensUri = new Uri("pack://application:,,,/PhialeTech.Styles.Wpf;component/Themes/ThemeTokens.HighContrast.xaml", UriKind.Absolute);
        private ObservableCollection<GridColumnBindingModel> _visibleColumns = new ObservableCollection<GridColumnBindingModel>();
        private ObservableCollection<GridGroupChipModel> _groupChips = new ObservableCollection<GridGroupChipModel>();
        private ObservableCollection<GridSummaryDisplayItem> _summaryItems = new ObservableCollection<GridSummaryDisplayItem>();
        private IReadOnlyList<GridColumnDefinition> _baselineColumns = Array.Empty<GridColumnDefinition>();
        private IReadOnlyList<GridGroupDescriptor> _groupDescriptors = Array.Empty<GridGroupDescriptor>();
        private IReadOnlyList<GridSortDescriptor> _sortDescriptors = Array.Empty<GridSortDescriptor>();
        private IReadOnlyList<GridSummaryDescriptor> _summaryDescriptors = Array.Empty<GridSummaryDescriptor>();
        private GridLayoutState _layoutState;
        private FrameworkElement _regionDragSource;
        private GridRegionKind? _regionDragKind;
        private Point _regionDragStartPoint;
        private bool _isRegionDragArmed;
        private bool _isRegionDragPreviewActive;
        private GridRegionHostKind? _regionDragPreviewHostKind;
        private GridRegionKind? _regionDragPreviewKind;
        private int? _regionDragPreviousHostZIndex;
        private Brush _regionDragPreviousHostBackground;
        private Brush _regionDragPreviousPanelBackground;
        private IEnumerable _rowsView = Array.Empty<GridDisplayRowModel>();
        private INotifyCollectionChanged _observableItemsSource;
        private IReadOnlyList<object> _currentFilteredRows = Array.Empty<object>();
        private IReadOnlyList<string> _currentGroupIds = Array.Empty<string>();
        private GridSummarySet _currentSummary = GridSummarySet.Empty;
        private readonly GridGroupExpansionState _groupExpansionState = new GridGroupExpansionState();
        private GridVirtualizedRowCollection _virtualizedRows;
        private GridVirtualizedGroupedRowCollection _virtualizedGroupedRows;
        private IReadOnlyList<GridHierarchyNode<object>> _hierarchyRoots = Array.Empty<GridHierarchyNode<object>>();
        private GridHierarchyController<object> _hierarchyController;
        private GridHierarchyPresentationMode _hierarchyPresentationMode;
        private IGridRowDetailProvider _rowDetailProvider;
        private IGridRowDetailContentFactory _rowDetailContentFactory;
        private GridRowDetailExpansionState _rowDetailExpansionState = GridRowDetailExpansionState.Empty;
        private GridMasterDetailHeaderPlacementMode _masterDetailHeaderPlacementMode = GridMasterDetailHeaderPlacementMode.Inside;
        private string _hierarchyDisplayColumnId = "ObjectName";
        private string _masterDetailDisplayColumnId = "ObjectName";
        private IReadOnlyDictionary<string, string> _masterDetailHeaderMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private ISet<string> _masterDetailHeaderColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private ISet<string> _masterDetailDetailColumnIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private ISet<string> _masterDetailMasterColumnIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Dictionary<string, string>> _masterDetailFilterStateByPathId = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, object> _masterDetailCurrentRowsByPathId = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        private bool _hasRows;
        private bool _hasSummaries;
        private bool _isSyncingGroups;
        private bool _isSyncingSorts;
        private bool _isSyncingFilterScroll;
        private bool _collapseGroupsOnNextRefresh;
        private string _globalSearchText = string.Empty;
        private int _selectedCellCount;
        private int _selectedRowCount;
        private int _totalRowCount;
        private int _displayedRowCount;
        private int _topLevelGroupCount;
        private string _currentCellDescription = string.Empty;
        private string _savedStatePreview = string.Empty;
        private IReadOnlyDictionary<string, object> _surfaceRowsByKey = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        private IReadOnlyDictionary<string, GridColumnBindingModel> _surfaceColumnsByKey = new Dictionary<string, GridColumnBindingModel>(StringComparer.OrdinalIgnoreCase);
        private ResourceDictionary _themeTokenDictionary;
        private bool _systemThemeSubscriptionActive;
        private object _lastEditSessionRecordsReference;
        private object _lastEditSessionFieldDefinitionsReference;
        private IReadOnlyList<object> _lastEditSessionRecordsSnapshot = Array.Empty<object>();
        private IReadOnlyList<IEditSessionFieldDefinition> _lastEditSessionFieldDefinitionsSnapshot = Array.Empty<IEditSessionFieldDefinition>();
        private bool _suppressViewStateNotifications;
        private bool _suppressColumnFilterRefresh;
        private bool _pendingRebuildColumnBindingsWhileHidden;
        private bool _pendingRefreshRowsViewWhileHidden;
        private bool _pendingRefreshSurfaceRowIndicatorsWhileHidden;
        private bool _pendingVisibleStateFlushScheduled;
        private int _gridUpdateBatchDepth;
        private bool _pendingRebuildColumnBindingsAfterBatch;
        private bool _pendingRefreshRowsViewAfterBatch;
        private bool _pendingRefreshSurfaceRowIndicatorsAfterBatch;
        private bool _pendingApplyRegionLayoutAfterBatch;
        private bool _pendingViewStateChangedAfterBatch;
        private bool _isChangePanelChangedRowsFilterActive;
        private string _pendingCurrentCellRowKeyAfterBatch;
        private string _pendingCurrentCellColumnKeyAfterBatch;
        private GridInteractionMode _autoInteractionMode = GridInteractionMode.Classic;
        private GridInteractionMode _resolvedInteractionMode = GridInteractionMode.Classic;
        private GridDensityMetrics _densityMetrics = GridInteractionConfiguration.ResolveDensityMetrics(GridDensity.Compact);
        private long _lastTouchInteractionTimestamp;
        private bool _isApplyingRegionLayout;
        private bool _workspacePanelTabOverflowLayoutQueued;

        public static readonly DependencyProperty TopCommandContentProperty =
            DependencyProperty.Register(nameof(TopCommandContent), typeof(object), typeof(PhialeGrid), new PropertyMetadata(null, HandleRegionChromeChanged));

        public static readonly DependencyProperty SideToolContentProperty =
            DependencyProperty.Register(nameof(SideToolContent), typeof(object), typeof(PhialeGrid), new PropertyMetadata(null, HandleRegionChromeChanged));

        public static readonly DependencyProperty ChangePanelContentProperty =
            DependencyProperty.Register(nameof(ChangePanelContent), typeof(object), typeof(PhialeGrid), new PropertyMetadata(null, HandleRegionChromeChanged));

        public static readonly DependencyProperty ValidationPanelContentProperty =
            DependencyProperty.Register(nameof(ValidationPanelContent), typeof(object), typeof(PhialeGrid), new PropertyMetadata(null, HandleRegionChromeChanged));

        public static readonly DependencyProperty SummaryDesignerContentProperty =
            DependencyProperty.Register(nameof(SummaryDesignerContent), typeof(object), typeof(PhialeGrid), new PropertyMetadata(null, HandleRegionChromeChanged));

        public static readonly DependencyProperty SideToolRegionTitleProperty =
            DependencyProperty.Register(nameof(SideToolRegionTitle), typeof(string), typeof(PhialeGrid), new PropertyMetadata("Grid options", HandleSideToolRegionTitleChanged));

        public static readonly DependencyProperty SideToolRegionUsesDrawerChromeProperty =
            DependencyProperty.Register(nameof(SideToolRegionUsesDrawerChrome), typeof(bool), typeof(PhialeGrid), new PropertyMetadata(false, HandleRegionChromeChanged));

        public static readonly DependencyProperty CapabilityPolicyProperty =
            DependencyProperty.Register(nameof(CapabilityPolicy), typeof(IGridCapabilityPolicy), typeof(PhialeGrid), new PropertyMetadata(null, HandleCapabilityPolicyChanged));

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(PhialeGrid), new PropertyMetadata(null, HandleItemsSourceChanged));

        public static readonly DependencyProperty ColumnsProperty =
            DependencyProperty.Register(nameof(Columns), typeof(IEnumerable<GridColumnDefinition>), typeof(PhialeGrid), new PropertyMetadata(null, HandleColumnsChanged));

        public static readonly DependencyProperty EditSessionContextProperty =
            DependencyProperty.Register(nameof(EditSessionContext), typeof(IEditSessionContext), typeof(PhialeGrid), new PropertyMetadata(null, HandleEditSessionContextChanged));

        public static readonly DependencyProperty GroupsProperty =
            DependencyProperty.Register(
                nameof(Groups),
                typeof(IEnumerable<GridGroupDescriptor>),
                typeof(PhialeGrid),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, HandleGroupsChanged));

        public static readonly DependencyProperty SortsProperty =
            DependencyProperty.Register(
                nameof(Sorts),
                typeof(IEnumerable<GridSortDescriptor>),
                typeof(PhialeGrid),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, HandleSortsChanged));

        public static readonly DependencyProperty SummariesProperty =
            DependencyProperty.Register(
                nameof(Summaries),
                typeof(IEnumerable<GridSummaryDescriptor>),
                typeof(PhialeGrid),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, HandleSummariesChanged));

        public static readonly DependencyProperty IsGridReadOnlyProperty =
            DependencyProperty.Register(nameof(IsGridReadOnly), typeof(bool), typeof(PhialeGrid), new PropertyMetadata(true, HandleReadOnlyChanged));

        public static readonly DependencyProperty SelectionStateProperty =
            DependencyProperty.Register(nameof(SelectionState), typeof(GridSelectionState), typeof(PhialeGrid), new PropertyMetadata(null));

        public static readonly DependencyProperty ViewportProperty =
            DependencyProperty.Register(nameof(Viewport), typeof(GridViewport), typeof(PhialeGrid), new PropertyMetadata(null));

        public static readonly DependencyProperty LanguageCodeProperty =
            DependencyProperty.Register(nameof(LanguageCode), typeof(string), typeof(PhialeGrid), new PropertyMetadata("en", HandleLocalizationChanged));

        public static readonly DependencyProperty LanguageDirectoryProperty =
            DependencyProperty.Register(nameof(LanguageDirectory), typeof(string), typeof(PhialeGrid), new PropertyMetadata(null, HandleLocalizationChanged));

        public static readonly DependencyProperty LocalizationCatalogProperty =
            DependencyProperty.Register(nameof(LocalizationCatalog), typeof(GridLocalizationCatalog), typeof(PhialeGrid), new PropertyMetadata(GridLocalizationCatalog.Empty));

        public static readonly DependencyProperty IsNightModeProperty =
            DependencyProperty.Register(nameof(IsNightMode), typeof(bool), typeof(PhialeGrid), new PropertyMetadata(false, HandleThemeChanged));

        public static readonly DependencyProperty InteractionModeProperty =
            DependencyProperty.Register(nameof(InteractionMode), typeof(GridInteractionMode), typeof(PhialeGrid), new PropertyMetadata(GridInteractionMode.Classic, HandleInteractionConfigurationChanged));

        public static readonly DependencyProperty DensityProperty =
            DependencyProperty.Register(nameof(Density), typeof(GridDensity), typeof(PhialeGrid), new PropertyMetadata(GridDensity.Compact, HandleInteractionConfigurationChanged));

        public static readonly DependencyProperty SelectCurrentRowProperty =
            DependencyProperty.Register(nameof(SelectCurrentRow), typeof(bool), typeof(PhialeGrid), new PropertyMetadata(true, HandleRowIndicatorConfigurationChanged));

        public static readonly DependencyProperty MultiSelectProperty =
            DependencyProperty.Register(nameof(MultiSelect), typeof(bool), typeof(PhialeGrid), new PropertyMetadata(false, HandleRowIndicatorConfigurationChanged));

        public static readonly DependencyProperty ShowNbProperty =
            DependencyProperty.Register(nameof(ShowNb), typeof(bool), typeof(PhialeGrid), new PropertyMetadata(false, HandleRowIndicatorConfigurationChanged));

        public static readonly DependencyProperty RowNumberingModeProperty =
            DependencyProperty.Register(nameof(RowNumberingMode), typeof(GridRowNumberingMode), typeof(PhialeGrid), new PropertyMetadata(GridRowNumberingMode.Global, HandleRowIndicatorConfigurationChanged));

        public static readonly DependencyProperty EnableCellSelectionProperty =
            DependencyProperty.Register(nameof(EnableCellSelection), typeof(bool), typeof(PhialeGrid), new PropertyMetadata(true, HandleSelectionConfigurationChanged));

        public static readonly DependencyProperty EnableRangeSelectionProperty =
            DependencyProperty.Register(nameof(EnableRangeSelection), typeof(bool), typeof(PhialeGrid), new PropertyMetadata(true, HandleSelectionConfigurationChanged));

        public static readonly DependencyProperty ShowCurrentRecordIndicatorProperty =
            DependencyProperty.Register(nameof(ShowCurrentRecordIndicator), typeof(bool), typeof(PhialeGrid), new PropertyMetadata(true, HandleCurrentRecordIndicatorChanged));

        public static readonly DependencyProperty EditActivationModeProperty =
            DependencyProperty.Register(nameof(EditActivationMode), typeof(GridEditActivationMode), typeof(PhialeGrid), new PropertyMetadata(GridEditActivationMode.DirectInteraction, HandleEditActivationModeChanged));

        public PhialeGrid()
        {
            _internalEditSessionContext = new EditSessionContext<object>(_editSessionDataSource, ResolveRowId);
            _editSessionContext = _internalEditSessionContext;
            InitializeComponent();
            _regionLayoutAdapter = CreateRegionLayoutAdapter();
            AddHandler(UIElement.PreviewMouseRightButtonDownEvent, new MouseButtonEventHandler(HandleHeaderPreviewMouseRightButtonDown), true);
            AddHandler(UIElement.MouseRightButtonDownEvent, new MouseButtonEventHandler(HandleHeaderPreviewMouseRightButtonDown), true);
            AddHandler(FrameworkElement.ContextMenuOpeningEvent, new ContextMenuEventHandler(HandleHeaderContextMenuOpening), true);
            _openColumnMenuCommand = new UiActionCommand(HandleOpenColumnMenu);
            _editSessionContext.StateChanged += HandleEditSessionContextStateChanged;
            _surfaceDataBridge = new SurfaceGridDataBridge(this);
            _surfaceCoordinator.CellValueProvider = _surfaceDataBridge;
            _surfaceCoordinator.EditCellAccessor = _surfaceDataBridge;
            _surfaceCoordinator.EditValidator = new SurfaceGridEditValidator(this);
            _surfaceCoordinator.EditValueParser = ParseEditingValue;
            _surfaceCoordinator.EditSessionContext = _editSessionContext;
            _surfaceCoordinator.EditActivationMode = EditActivationMode;
            _surfaceCoordinator.SnapshotChanged += HandleSurfaceSnapshotChanged;
            _surfaceCoordinator.HeaderActivated += HandleSurfaceHeaderActivated;
            _surfaceCoordinator.ColumnResizeRequested += HandleSurfaceColumnResizeRequested;
            _surfaceCoordinator.ColumnReorderRequested += HandleSurfaceColumnReorderRequested;
            _surfaceCoordinator.ColumnGroupingDragRequested += HandleSurfaceColumnGroupingDragRequested;
            _surfaceCoordinator.RowActionRequested += HandleSurfaceRowActionRequested;
            SurfaceHost.RowDetailContentFactory = _rowDetailContentFactory;
            SurfaceHost.Initialize(_surfaceCoordinator);
            SurfaceColumnHeaderBand.InputSurfaceHost = SurfaceHost;
            SurfaceHost.ViewportScrollChanged += HandleSurfaceViewportScrollChanged;
            SurfaceHost.HostGeometryChanged += HandleSurfaceHostGeometryChanged;
            FilterScrollViewer.ScrollChanged += HandleFilterScrollViewerScrollChanged;
            Loaded += HandleLoaded;
            Unloaded += HandleUnloaded;
            IsVisibleChanged += HandleIsVisibleChanged;
            PreviewTouchDown += HandlePreviewTouchInput;
            PreviewStylusDown += HandlePreviewStylusInput;
            PreviewMouseDown += HandlePreviewMouseInput;
            PreviewKeyDown += HandlePreviewKeyInput;
            _surfaceCoordinator.SetRegionCapabilityPolicy(GridAllowAllCapabilityPolicy.Instance);
            ApplyThemeResources();
            ReloadLocalization();
            ApplyInteractionConfiguration();
            ApplyRegionLayout();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public event EventHandler ViewStateChanged;

        public ICommand OpenColumnMenuCommand => _openColumnMenuCommand;

        public IEditSessionContext EditSessionContext
        {
            get => (IEditSessionContext)GetValue(EditSessionContextProperty) ?? _internalEditSessionContext;
            set => SetValue(EditSessionContextProperty, value);
        }

        public IEnumerable ItemsSource
        {
            get => (IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public IEnumerable<GridColumnDefinition> Columns
        {
            get => (IEnumerable<GridColumnDefinition>)GetValue(ColumnsProperty);
            set => SetValue(ColumnsProperty, value);
        }

        public GridSelectionState SelectionState
        {
            get => (GridSelectionState)GetValue(SelectionStateProperty);
            set => SetValue(SelectionStateProperty, value);
        }

        public bool ShowCurrentRecordIndicator
        {
            get => (bool)GetValue(ShowCurrentRecordIndicatorProperty);
            set => SetValue(ShowCurrentRecordIndicatorProperty, value);
        }

        public GridEditActivationMode EditActivationMode
        {
            get => (GridEditActivationMode)GetValue(EditActivationModeProperty);
            set => SetValue(EditActivationModeProperty, value);
        }

        public IEnumerable<GridGroupDescriptor> Groups
        {
            get => (IEnumerable<GridGroupDescriptor>)GetValue(GroupsProperty);
            set => SetValue(GroupsProperty, value);
        }

        public IEnumerable<GridSortDescriptor> Sorts
        {
            get => (IEnumerable<GridSortDescriptor>)GetValue(SortsProperty);
            set => SetValue(SortsProperty, value);
        }

        public IEnumerable<GridSummaryDescriptor> Summaries
        {
            get => (IEnumerable<GridSummaryDescriptor>)GetValue(SummariesProperty);
            set => SetValue(SummariesProperty, value);
        }

        public bool IsGridReadOnly
        {
            get => (bool)GetValue(IsGridReadOnlyProperty);
            set => SetValue(IsGridReadOnlyProperty, value);
        }

        public GridInteractionMode InteractionMode
        {
            get => (GridInteractionMode)GetValue(InteractionModeProperty);
            set => SetValue(InteractionModeProperty, value);
        }

        public GridDensity Density
        {
            get => (GridDensity)GetValue(DensityProperty);
            set => SetValue(DensityProperty, value);
        }

        public bool SelectCurrentRow
        {
            get => (bool)GetValue(SelectCurrentRowProperty);
            set => SetValue(SelectCurrentRowProperty, value);
        }

        public bool MultiSelect
        {
            get => (bool)GetValue(MultiSelectProperty);
            set => SetValue(MultiSelectProperty, value);
        }

        public bool ShowNb
        {
            get => (bool)GetValue(ShowNbProperty);
            set => SetValue(ShowNbProperty, value);
        }

        public GridRowNumberingMode RowNumberingMode
        {
            get => (GridRowNumberingMode)GetValue(RowNumberingModeProperty);
            set => SetValue(RowNumberingModeProperty, value);
        }

        public bool EnableCellSelection
        {
            get => (bool)GetValue(EnableCellSelectionProperty);
            set => SetValue(EnableCellSelectionProperty, value);
        }

        public bool EnableRangeSelection
        {
            get => (bool)GetValue(EnableRangeSelectionProperty);
            set => SetValue(EnableRangeSelectionProperty, value);
        }

        public GridViewport Viewport
        {
            get => (GridViewport)GetValue(ViewportProperty);
            set => SetValue(ViewportProperty, value);
        }

        public string LanguageCode
        {
            get => (string)GetValue(LanguageCodeProperty);
            set => SetValue(LanguageCodeProperty, value);
        }

        public string LanguageDirectory
        {
            get => (string)GetValue(LanguageDirectoryProperty);
            set => SetValue(LanguageDirectoryProperty, value);
        }

        public GridLocalizationCatalog LocalizationCatalog
        {
            get => (GridLocalizationCatalog)GetValue(LocalizationCatalogProperty);
            private set => SetValue(LocalizationCatalogProperty, value);
        }

        public bool IsNightMode
        {
            get => (bool)GetValue(IsNightModeProperty);
            set => SetValue(IsNightModeProperty, value);
        }

        public IReadOnlyList<GridColumnBindingModel> VisibleColumns => _visibleColumns;

        public IReadOnlyList<GridGroupChipModel> GroupChips => _groupChips;

        public IReadOnlyList<GridSummaryDisplayItem> SummaryItems => _summaryItems;

        public object TopCommandContent
        {
            get => GetValue(TopCommandContentProperty);
            set => SetValue(TopCommandContentProperty, value);
        }

        public object SideToolContent
        {
            get => GetValue(SideToolContentProperty);
            set => SetValue(SideToolContentProperty, value);
        }

        public object ChangePanelContent
        {
            get => GetValue(ChangePanelContentProperty);
            set => SetValue(ChangePanelContentProperty, value);
        }

        public object ValidationPanelContent
        {
            get => GetValue(ValidationPanelContentProperty);
            set => SetValue(ValidationPanelContentProperty, value);
        }

        public object SummaryDesignerContent
        {
            get => GetValue(SummaryDesignerContentProperty);
            set => SetValue(SummaryDesignerContentProperty, value);
        }

        public string SideToolRegionTitle
        {
            get => (string)GetValue(SideToolRegionTitleProperty);
            set => SetValue(SideToolRegionTitleProperty, value);
        }

        public bool SideToolRegionUsesDrawerChrome
        {
            get => (bool)GetValue(SideToolRegionUsesDrawerChromeProperty);
            set => SetValue(SideToolRegionUsesDrawerChromeProperty, value);
        }

        public IGridCapabilityPolicy CapabilityPolicy
        {
            get => (IGridCapabilityPolicy)GetValue(CapabilityPolicyProperty) ?? GridAllowAllCapabilityPolicy.Instance;
            set => SetValue(CapabilityPolicyProperty, value);
        }

        public IGridRowDetailProvider RowDetailProvider
        {
            get => _rowDetailProvider;
            set
            {
                if (ReferenceEquals(_rowDetailProvider, value))
                {
                    return;
                }

                _rowDetailProvider = value;
                _rowDetailExpansionState = GridRowDetailExpansionState.Empty;
                ApplyResolvedRowHeaderMetrics();
                OnPropertyChanged(nameof(ResolvedRowHeaderWidth));
                OnPropertyChanged(nameof(ResolvedRowActionWidth));
                RefreshRowsView();
            }
        }

        public IGridRowDetailContentFactory RowDetailContentFactory
        {
            get => _rowDetailContentFactory;
            set
            {
                if (ReferenceEquals(_rowDetailContentFactory, value))
                {
                    return;
                }

                _rowDetailContentFactory = value;
                if (SurfaceHost != null)
                {
                    SurfaceHost.RowDetailContentFactory = value;
                }
            }
        }

        public IEnumerable RowsView
        {
            get => _rowsView;
            private set
            {
                _rowsView = value ?? Array.Empty<GridDisplayRowModel>();
                ApplyResolvedRowHeaderMetrics();
                OnPropertyChanged(nameof(RowsView));
                OnPropertyChanged(nameof(ResolvedRowHeaderWidth));
            }
        }

        public bool HasRows
        {
            get => _hasRows;
            private set
            {
                _hasRows = value;
                OnPropertyChanged(nameof(HasRows));
            }
        }

        public bool HasNoGroups => _groupDescriptors.Count == 0;

        public bool HasGroups => _groupDescriptors.Count > 0;

        public bool HasHierarchy => _hierarchyController != null && _hierarchyRoots.Count > 0;

        public bool IsFilterRowVisible => !HasHierarchy;

        public string GlobalSearchText => _globalSearchText;

        public bool HasSummaries
        {
            get => _hasSummaries;
            private set
            {
                if (_hasSummaries != value)
                {
                    _hasSummaries = value;
                    OnPropertyChanged(nameof(HasSummaries));
                    ApplyRegionLayout();
                }
            }
        }

        public string GroupingBandLabelText => GetText(GridTextKeys.GroupingBandLabel);

        public string GroupingDropText => GetText(GridTextKeys.GroupingDropHere);

        public string GroupingDragDataFormat => GroupingDragFormat;

        public string GroupingEmptyText => GetText(GridTextKeys.GroupingEmpty);

        public string GroupingExpandAllText => GetText(GridTextKeys.GroupingExpandAll);

        public string GroupingCollapseAllText => GetText(GridTextKeys.GroupingCollapseAll);

        public string ToolsRegionTitleText => string.IsNullOrWhiteSpace(SideToolRegionTitle) ? "Grid options" : SideToolRegionTitle;

        public bool IsRowNumberingGlobal
        {
            get => RowNumberingMode == GridRowNumberingMode.Global;
            set { if (value) RowNumberingMode = GridRowNumberingMode.Global; }
        }

        public bool IsRowNumberingWithinGroup
        {
            get => RowNumberingMode == GridRowNumberingMode.WithinGroup;
            set { if (value) RowNumberingMode = GridRowNumberingMode.WithinGroup; }
        }

        public IReadOnlyList<GridColumnDefinition> ColumnChooserColumns => GetGridOptionsColumnChooserColumns();

        public void ToggleColumnVisibilityFromPanel(string columnId)
        {
            var isCurrentlyVisible = _layoutState?.Columns.Any(c => string.Equals(c.Id, columnId, StringComparison.OrdinalIgnoreCase) && c.IsVisible) ?? false;
            if (isCurrentlyVisible && GetVisibleColumnCount() <= 1)
            {
                return;
            }

            SetColumnVisibility(columnId, !isCurrentlyVisible);
        }

        public string ChangePanelTitleText => "Changes";

        public string ValidationPanelTitleText => "Validation";

        public string SummaryDesignerTitleText => "Summary designer";

        public string SummaryRegionTitleText => "Summaries";

        public string TopCommandStripToggleText => GetChromeState(GridRegionKind.TopCommandRegion).ToggleText;

        public string GroupingRegionToggleText => GetChromeState(GridRegionKind.GroupingRegion).ToggleText;

        public string SummaryBottomRegionToggleText => GetChromeState(GridRegionKind.SummaryBottomRegion).ToggleText;

        public string SideToolRegionToggleText => GetChromeState(GridRegionKind.SideToolRegion).ToggleText;

        public string ChangePanelRegionToggleText => GetChromeState(GridRegionKind.ChangePanelRegion).ToggleText;

        public string ValidationPanelRegionToggleText => GetChromeState(GridRegionKind.ValidationPanelRegion).ToggleText;

        public string SummaryDesignerRegionToggleText => GetChromeState(GridRegionKind.SummaryDesignerRegion).ToggleText;

        public bool CanToggleTopCommandStrip => GetChromeState(GridRegionKind.TopCommandRegion).CanCollapse;

        public bool CanCollapseGroupingRegion => GetChromeState(GridRegionKind.GroupingRegion).CanCollapse;

        public bool CanCollapseSummaryBottomRegion => GetChromeState(GridRegionKind.SummaryBottomRegion).CanCollapse;

        public bool CanCollapseSideToolRegion => GetChromeState(GridRegionKind.SideToolRegion).CanCollapse;

        public bool CanCollapseChangePanelRegion => GetChromeState(GridRegionKind.ChangePanelRegion).CanCollapse;

        public bool CanCollapseValidationPanelRegion => GetChromeState(GridRegionKind.ValidationPanelRegion).CanCollapse;

        public bool CanCollapseSummaryDesignerRegion => GetChromeState(GridRegionKind.SummaryDesignerRegion).CanCollapse;

        public bool CanCloseTopCommandStrip => GetChromeState(GridRegionKind.TopCommandRegion).CanClose;

        public bool CanCloseGroupingRegion => GetChromeState(GridRegionKind.GroupingRegion).CanClose;

        public bool CanCloseSummaryBottomRegion => GetChromeState(GridRegionKind.SummaryBottomRegion).CanClose;

        public bool CanCloseSideToolRegion => GetChromeState(GridRegionKind.SideToolRegion).CanClose;

        public bool CanCloseChangePanelRegion => GetChromeState(GridRegionKind.ChangePanelRegion).CanClose;

        public bool CanCloseValidationPanelRegion => GetChromeState(GridRegionKind.ValidationPanelRegion).CanClose;

        public bool CanCloseSummaryDesignerRegion => GetChromeState(GridRegionKind.SummaryDesignerRegion).CanClose;

        public Visibility ToolsPanelToolsTabVisibility => ResolveWorkspacePanelTabVisibility(GridRegionKind.SideToolRegion, GridRegionKind.SideToolRegion);

        public Visibility ToolsPanelChangesTabVisibility => ResolveWorkspacePanelTabVisibility(GridRegionKind.SideToolRegion, GridRegionKind.ChangePanelRegion);

        public Visibility ToolsPanelValidationTabVisibility => ResolveWorkspacePanelTabVisibility(GridRegionKind.SideToolRegion, GridRegionKind.ValidationPanelRegion);

        public Visibility ToolsPanelSummaryDesignerTabVisibility => ResolveWorkspacePanelTabVisibility(GridRegionKind.SideToolRegion, GridRegionKind.SummaryDesignerRegion);

        public Visibility ChangesPanelToolsTabVisibility => ResolveWorkspacePanelTabVisibility(GridRegionKind.ChangePanelRegion, GridRegionKind.SideToolRegion);

        public Visibility ChangesPanelChangesTabVisibility => ResolveWorkspacePanelTabVisibility(GridRegionKind.ChangePanelRegion, GridRegionKind.ChangePanelRegion);

        public Visibility ChangesPanelValidationTabVisibility => ResolveWorkspacePanelTabVisibility(GridRegionKind.ChangePanelRegion, GridRegionKind.ValidationPanelRegion);

        public Visibility ChangesPanelSummaryDesignerTabVisibility => ResolveWorkspacePanelTabVisibility(GridRegionKind.ChangePanelRegion, GridRegionKind.SummaryDesignerRegion);

        public Visibility ValidationPanelToolsTabVisibility => ResolveWorkspacePanelTabVisibility(GridRegionKind.ValidationPanelRegion, GridRegionKind.SideToolRegion);

        public Visibility ValidationPanelChangesTabVisibility => ResolveWorkspacePanelTabVisibility(GridRegionKind.ValidationPanelRegion, GridRegionKind.ChangePanelRegion);

        public Visibility ValidationPanelValidationTabVisibility => ResolveWorkspacePanelTabVisibility(GridRegionKind.ValidationPanelRegion, GridRegionKind.ValidationPanelRegion);

        public Visibility ValidationPanelSummaryDesignerTabVisibility => ResolveWorkspacePanelTabVisibility(GridRegionKind.ValidationPanelRegion, GridRegionKind.SummaryDesignerRegion);

        public Visibility SummaryDesignerPanelToolsTabVisibility => ResolveWorkspacePanelTabVisibility(GridRegionKind.SummaryDesignerRegion, GridRegionKind.SideToolRegion);

        public Visibility SummaryDesignerPanelChangesTabVisibility => ResolveWorkspacePanelTabVisibility(GridRegionKind.SummaryDesignerRegion, GridRegionKind.ChangePanelRegion);

        public Visibility SummaryDesignerPanelValidationTabVisibility => ResolveWorkspacePanelTabVisibility(GridRegionKind.SummaryDesignerRegion, GridRegionKind.ValidationPanelRegion);

        public Visibility SummaryDesignerPanelSummaryDesignerTabVisibility => ResolveWorkspacePanelTabVisibility(GridRegionKind.SummaryDesignerRegion, GridRegionKind.SummaryDesignerRegion);

        public string FilterLabelText => GetText(GridTextKeys.FilterLabel);

        public string FilterEditText => GetText(GridTextKeys.FilterEdit);

        public string EmptyNoRowsText => GetText(GridTextKeys.EmptyNoRows);

        public string ColumnMenuButtonText => GetText(GridTextKeys.ColumnMenuOpen);

        public string SortAscendingText => GetText(GridTextKeys.SortAscending);

        public string SortDescendingText => GetText(GridTextKeys.SortDescending);

        public string SortAddAscendingText => GetText(GridTextKeys.SortAddAscending);

        public string SortAddDescendingText => GetText(GridTextKeys.SortAddDescending);

        public string SortClearText => GetText(GridTextKeys.SortClear);

        public string GroupByColumnText => GetText(GridTextKeys.GroupingByColumn);

        public string RemoveFromGroupingText => GetText(GridTextKeys.GroupingRemoveColumn);

        public string ToggleColumnGroupingDirectionText => GetText(GridTextKeys.GroupingToggleColumnDirection);

        public string FreezeColumnText => GetText(GridTextKeys.ColumnsFreeze);

        public string UnfreezeColumnText => GetText(GridTextKeys.ColumnsUnfreeze);

        public string HideColumnText => GetText(GridTextKeys.ColumnsHide);

        public string MoveColumnLeftText => GetText(GridTextKeys.ColumnsMoveLeft);

        public string MoveColumnRightText => GetText(GridTextKeys.ColumnsMoveRight);

        public string WidenColumnText => GetText(GridTextKeys.ColumnsWiden);

        public string NarrowColumnText => GetText(GridTextKeys.ColumnsNarrow);

        public string AutoFitColumnText => GetText(GridTextKeys.ColumnsAutoFit);

        public string HierarchyLoadMoreText => GetText(GridTextKeys.HierarchyLoadMore);

        public string SelectionStatusText => string.Format(CultureInfo.CurrentCulture, GetText(GridTextKeys.SelectionStatus), _selectedRowCount, _selectedCellCount);

        public string CurrentCellText => string.Format(CultureInfo.CurrentCulture, GetText(GridTextKeys.SelectionCurrent), _currentCellDescription);

        public string CurrentDataRowId => ResolveCurrentDataRowId() ?? string.Empty;

        public int PendingEditCount => CurrentEditSessionContext?.PendingEditCount ?? 0;

        public IReadOnlyList<string> PendingEditRowIds => OrderRowIdsByCurrentView(CurrentEditSessionContext?.EditedRecordIds ?? Array.Empty<string>());

        public IReadOnlyList<ChangePanelItem> ChangePanelItems => ResolveChangePanelItems();

        public string ChangeSummaryText => string.Format(
            CultureInfo.CurrentCulture,
            PendingEditCount == 1 ? "{0} changed row" : "{0} changed rows",
            PendingEditCount);

        public bool IsChangePanelChangedRowsFilterActive => _isChangePanelChangedRowsFilterActive;

        public string ChangePanelRowsFilterToggleText => _isChangePanelChangedRowsFilterActive
            ? "Show all rows"
            : "Show only changed rows";

        public IReadOnlyList<string> PendingEditRowIdsWithoutValidation => OrderRowIdsByCurrentView(
            (CurrentEditSessionContext?.EditedRecordIds ?? Array.Empty<string>())
                .Where(rowId => !(CurrentEditSessionContext?.InvalidRecordIds?.Contains(rowId) ?? false)));

        public IReadOnlyList<string> ValidationIssueRowIds => OrderRowIdsByCurrentView(CurrentEditSessionContext?.InvalidRecordIds ?? Array.Empty<string>());

        public IReadOnlyList<ValidationIssuePanelItem> ValidationIssueItems => ResolveValidationIssueItems();

        public string ValidationIssueSummaryText => string.Format(
            CultureInfo.CurrentCulture,
            ValidationIssueCount == 1 ? "{0} validation issue" : "{0} validation issues",
            ValidationIssueCount);

        public bool HasPendingEdits => PendingEditCount > 0;

        public bool HasValidationIssues => CurrentEditSessionContext?.HasValidationIssues ?? false;

        public int ValidationIssueCount => ResolveValidationIssueCount();

        public GridInteractionMode ResolvedInteractionMode => _resolvedInteractionMode;

        public bool IsTouchInteractionMode => _resolvedInteractionMode == GridInteractionMode.Touch;

        public GridDensity ResolvedDensity => _densityMetrics.Density;

        public double ResolvedRowHeight => _densityMetrics.RowHeight;

        public double ResolvedDetailRowHeight => _densityMetrics.DetailRowHeight;

        public double ResolvedRowHeaderWidth => ResolvedRowDetailToggleWidth + ResolvedRowStateWidth + ResolvedRowNumbersWidth;

        public double ResolvedRowActionWidth => ResolvedRowDetailToggleWidth;

        public double SurfaceColumnHeaderHeight => ResolveSurfaceViewportState()?.ColumnHeaderHeight ?? _surfaceCoordinator.ColumnHeaderHeight;

        public double SurfaceFilterRowHeight => IsFilterRowVisible
            ? ResolveSurfaceViewportState()?.FilterRowHeight ?? _surfaceCoordinator.FilterRowHeight
            : 0d;

        public double SurfaceTopChromeHeight => SurfaceColumnHeaderHeight + SurfaceFilterRowHeight;

        public double SurfaceVerticalScrollBarGutterWidth => SurfaceHost?.VerticalScrollBarGutterWidth ?? 0d;

        public double SurfaceHorizontalScrollBarGutterHeight => SurfaceHost?.HorizontalScrollBarGutterHeight ?? 0d;

        public double ResolvedRowStateWidth => ResolvedRowIndicatorWidth + ResolvedSelectionCheckboxWidth;

        public double ResolvedRowDetailToggleWidth => _rowDetailProvider == null ? 0d : RowDetailToggleWidth;

        public double ResolvedRowNumbersWidth => ShowNb
            ? ResolveAdaptiveRowMarkerWidth()
            : 0d;

        public double ResolvedRowIndicatorWidth => GridInteractionConfiguration.ResolveRowIndicatorWidth(_densityMetrics.Density);

        public double ResolvedRowMarkerWidth => ResolvedRowNumbersWidth;

        public double ResolvedSelectionCheckboxWidth => MultiSelect
            ? GridInteractionConfiguration.ResolveSelectionCheckboxWidth(_densityMetrics.Density)
            : 0d;

        public GridSelectionMode ResolvedSelectionMode => EnableCellSelection
            ? GridSelectionMode.Cell
            : GridSelectionMode.None;

        public bool AllowsPointerColumnReorder => _resolvedInteractionMode == GridInteractionMode.Classic;

        public bool AllowsPointerColumnResize => _resolvedInteractionMode == GridInteractionMode.Classic;

        public bool AllowsGroupingDrag => _resolvedInteractionMode == GridInteractionMode.Classic;

        public bool ShowsColumnMenuAffordance => _resolvedInteractionMode != GridInteractionMode.Classic;

        public bool ShowsTouchRowSelectors => _resolvedInteractionMode != GridInteractionMode.Classic;

        public bool HasSelectedRows => _selectedRowCount > 0;

        public string EditStatusText => string.Format(CultureInfo.CurrentCulture, GetText(GridTextKeys.EditingStatus), PendingEditCount, ResolveValidationIssueCount());

        public string PendingEditBannerText => GetText(HasValidationIssues ? GridTextKeys.EditingPendingWithValidation : GridTextKeys.EditingPending);

        public string PagingStatusText => string.Format(CultureInfo.CurrentCulture, GetText(GridTextKeys.PagingLocalStatus), _displayedRowCount, _totalRowCount);

        public string SavedStatePreview
        {
            get => _savedStatePreview;
            private set
            {
                if (!string.Equals(_savedStatePreview, value, StringComparison.Ordinal))
                {
                    _savedStatePreview = value ?? string.Empty;
                    OnPropertyChanged(nameof(SavedStatePreview));
                }
            }
        }

        public string GetText(string key)
        {
            return LocalizationCatalog.GetText(LanguageCode, key);
        }

        public void SetGroups(IEnumerable<GridGroupDescriptor> groups)
        {
            _groupDescriptors = (groups ?? Array.Empty<GridGroupDescriptor>()).ToArray();
            _collapseGroupsOnNextRefresh = _groupDescriptors.Count > 0;
            RebuildGroupChips();
            RefreshRowsView();
            SyncGroupsProperty();
            RaiseViewStateChanged();
        }

        public string SaveState()
        {
            var snapshot = BuildStateSnapshot();
            var encoded = GridStateCodec.Encode(snapshot);
            SavedStatePreview = encoded;
            LogDiagnostics($"SaveState exported snapshot. Columns={snapshot.Layout?.Columns?.Count ?? 0}, Groups={snapshot.Groups?.Count ?? 0}, Sorts={snapshot.Sorts?.Count ?? 0}, SearchLength={snapshot.GlobalSearchText?.Length ?? 0}.");
            return encoded;
        }

        internal string DiagnosticsGridIdForTests => GetDiagnosticsGridId();

        public IDisposable BeginGridUpdateBatch(string reason)
        {
            _gridUpdateBatchDepth++;
            LogDiagnostics("Grid update batch started. Reason='" + (reason ?? string.Empty) + "', Depth=" + _gridUpdateBatchDepth + ".");
            var editStateBatch = _editSessionContext?.BeginStateChangeBatch("grid-update-batch:" + (reason ?? string.Empty));
            var surfaceSnapshotBatch = _surfaceCoordinator.BeginSnapshotUpdateBatch("grid-update-batch:" + (reason ?? string.Empty));
            return new GridUpdateBatchScope(this, reason, editStateBatch, surfaceSnapshotBatch);
        }

        public GridViewState ExportViewState()
        {
            return GridViewStateConverter.FromSnapshot(BuildStateSnapshot());
        }

        public void LoadState(string encoded)
        {
            if (string.IsNullOrWhiteSpace(encoded) || _baselineColumns.Count == 0)
            {
                LogDiagnostics($"LoadState skipped. EncodedEmpty={string.IsNullOrWhiteSpace(encoded)}, BaselineColumns={_baselineColumns.Count}.");
                return;
            }

            LogDiagnostics($"LoadState started. BaselineColumns={_baselineColumns.Count}, EncodedLength={encoded.Length}.");
            var snapshot = GridStateCodec.Decode(encoded, _baselineColumns);
            ApplyStateSnapshot(snapshot);
            LogDiagnostics("LoadState finished.");
        }

        public void ApplyViewState(GridViewState state)
        {
            if (state == null || _baselineColumns.Count == 0)
            {
                LogDiagnostics($"ApplyViewState skipped. StateNull={state == null}, BaselineColumns={_baselineColumns.Count}.");
                return;
            }

            LogDiagnostics($"ApplyViewState started. StateColumns={state.Columns?.Count ?? 0}, StateGroups={state.Groups?.Count ?? 0}, StateSorts={state.Sorts?.Count ?? 0}, BaselineColumns={_baselineColumns.Count}.");
            ApplyStateSnapshot(GridViewStateConverter.ToSnapshot(state, _baselineColumns));
            LogDiagnostics("ApplyViewState finished.");
        }

        public void ResetState()
        {
            if (_baselineColumns.Count == 0)
            {
                return;
            }

            ExecuteWithoutViewStateNotifications(() =>
            {
                _layoutState = new GridLayoutState(_baselineColumns);
                _groupDescriptors = Array.Empty<GridGroupDescriptor>();
                _sortDescriptors = Array.Empty<GridSortDescriptor>();
                _summaryDescriptors = (Summaries ?? Array.Empty<GridSummaryDescriptor>()).ToArray();
                _collapseGroupsOnNextRefresh = false;
                SavedStatePreview = string.Empty;
                _globalSearchText = string.Empty;
                _surfaceCoordinator.SetRegionCapabilityPolicy(CapabilityPolicy);
                _surfaceCoordinator.RestoreRegionLayout(new GridRegionLayoutManager().ExportLayout());
                SyncGroupsProperty();
                SyncSortsProperty();
                SyncSummariesProperty();
                RebuildColumnBindings();
                ClearFilters();
                OnPropertyChanged(nameof(GlobalSearchText));
                RefreshRowsView();
                ApplyRegionLayout();
            });
            RaiseViewStateChanged();
        }

        public void SetRegionVisibility(GridRegionKind regionKind, bool isVisible)
        {
            if (IsResolvedRegionVisibilityEqual(regionKind, isVisible))
            {
                LogDiagnostics("SetRegionVisibility skipped because requested visibility is already active. Region=" + regionKind + ", IsVisible=" + isVisible + ".");
                return;
            }

            HandleRegionCommand(_surfaceInputAdapter.CreateRegionStateInput(
                isVisible ? GridRegionCommandKind.Open : GridRegionCommandKind.Close,
                regionKind,
                DateTime.UtcNow));
        }

        public void OpenWorkspacePanel(GridRegionKind regionKind)
        {
            if (ResolveRegionHostKind(regionKind) != GridRegionHostKind.WorkspacePanel)
            {
                throw new InvalidOperationException(regionKind + " is not a workspace panel.");
            }

            EnsureWorkspacePanelOpen(regionKind);
            HandleRegionCommand(_surfaceInputAdapter.CreateRegionStateInput(
                GridRegionCommandKind.Activate,
                regionKind,
                DateTime.UtcNow));
        }

        public void ToggleChangePanelChangedRowsFilter()
        {
            SetChangePanelChangedRowsFilterActive(!_isChangePanelChangedRowsFilterActive);
        }

        public void ClearChangePanelChangedRowsFilter()
        {
            SetChangePanelChangedRowsFilterActive(false);
        }

        private void SetChangePanelChangedRowsFilterActive(bool isActive)
        {
            if (_isChangePanelChangedRowsFilterActive == isActive)
            {
                return;
            }

            var wasActive = _isChangePanelChangedRowsFilterActive;
            _isChangePanelChangedRowsFilterActive = isActive;
            OnPropertyChanged(nameof(IsChangePanelChangedRowsFilterActive));
            OnPropertyChanged(nameof(ChangePanelRowsFilterToggleText));
            RefreshRowsView();
            if (wasActive && !isActive)
            {
                ResetChangePanelChangedRowsFilterViewport();
            }

            RaiseViewStateChanged();
        }

        private void ResetChangePanelChangedRowsFilterViewport()
        {
            _surfaceCoordinator.SetScrollPosition(0d, 0d);
            SyncFilterScrollOffset();
        }

        private void EnsureWorkspacePanelOpen(GridRegionKind regionKind)
        {
            var regionState = ResolveRequiredRegionState(regionKind);
            if (regionState.State == GridRegionState.Open)
            {
                return;
            }

            HandleRegionCommand(_surfaceInputAdapter.CreateRegionStateInput(
                GridRegionCommandKind.Open,
                regionKind,
                DateTime.UtcNow));
        }

        private GridStateSnapshot BuildStateSnapshot()
        {
            return new GridStateSnapshot(
                _layoutState == null ? new GridLayoutSnapshot(_baselineColumns) : _layoutState.CreateSnapshot(),
                _sortDescriptors,
                BuildFilterGroup(),
                _groupDescriptors,
                _summaryDescriptors,
                _surfaceCoordinator.ExportRegionLayout(),
                _globalSearchText,
                SelectCurrentRow,
                MultiSelect,
                ShowNb,
                RowNumberingMode);
        }

        private void ApplyStateSnapshot(GridStateSnapshot snapshot)
        {
            if (snapshot == null)
            {
                LogDiagnostics("ApplyStateSnapshot skipped because snapshot is null.");
                return;
            }

            LogDiagnostics($"ApplyStateSnapshot started. LayoutColumns={snapshot.Layout?.Columns?.Count ?? 0}, Groups={snapshot.Groups?.Count ?? 0}, Sorts={snapshot.Sorts?.Count ?? 0}, SearchLength={snapshot.GlobalSearchText?.Length ?? 0}.");
            var currentSnapshot = BuildStateSnapshot();
            if (AreStateSnapshotsEquivalent(currentSnapshot, snapshot))
            {
                LogDiagnostics("ApplyStateSnapshot skipped because restored state is equivalent to the current grid state. " + GetGridSessionDescription() + ".");
                return;
            }

            ExecuteWithoutViewStateNotifications(() =>
            {
                _layoutState = new GridLayoutState(snapshot.Layout.Columns);
                _sortDescriptors = snapshot.Sorts;
                _groupDescriptors = snapshot.Groups;
                _summaryDescriptors = snapshot.Summaries;
                _collapseGroupsOnNextRefresh = _groupDescriptors.Count > 0;
                _globalSearchText = snapshot.GlobalSearchText ?? string.Empty;
                _surfaceCoordinator.SetRegionCapabilityPolicy(CapabilityPolicy);
                _surfaceCoordinator.RestoreRegionLayout(snapshot.RegionLayout);
                if (snapshot.SelectCurrentRow.HasValue)
                {
                    SelectCurrentRow = snapshot.SelectCurrentRow.Value;
                }

                if (snapshot.MultiSelect.HasValue)
                {
                    MultiSelect = snapshot.MultiSelect.Value;
                }

                if (snapshot.ShowRowNumbers.HasValue)
                {
                    ShowNb = snapshot.ShowRowNumbers.Value;
                }

                if (snapshot.RowNumberingMode.HasValue)
                {
                    RowNumberingMode = snapshot.RowNumberingMode.Value;
                }

                SyncGroupsProperty();
                SyncSortsProperty();
                SyncSummariesProperty();
                RebuildColumnBindings(refreshRows: false);
                ApplyFilterState(snapshot.Filters);
                OnPropertyChanged(nameof(GlobalSearchText));
                RefreshRowsView();
                ApplyRegionLayout();
            });
            LogDiagnostics($"ApplyStateSnapshot finished. VisibleColumns={_visibleColumns.Count}, GroupDescriptors={_groupDescriptors.Count}, SortDescriptors={_sortDescriptors.Count}.");
        }

        private void ApplyRegionLayout()
        {
            var applyCounter = PhialeGridDiagnostics.IncrementGridCounter(GetDiagnosticsGridId(), "ApplyRegionLayout");
            if (ShouldDeferVisualWorkBecauseBatched())
            {
                _pendingApplyRegionLayoutAfterBatch = true;
                LogDiagnostics("ApplyRegionLayout deferred because grid update batch is active. Count=" + applyCounter.Count + ". " + GetGridSessionDescription() + ".");
                return;
            }

            if (_isApplyingRegionLayout)
            {
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            _isApplyingRegionLayout = true;
            try
            {
                PhialeGridDiagnostics.IncrementGridCounter(GetDiagnosticsGridId(), "ApplyRegionLayoutExecuted");
                _surfaceCoordinator.SetRegionCapabilityPolicy(CapabilityPolicy);
                _regionLayoutAdapter.Apply(
                    _surfaceCoordinator.ExportResolvedRegionStates(),
                    BuildRegionRenderSnapshot());

                OnPropertyChanged(nameof(TopCommandStripToggleText));
                OnPropertyChanged(nameof(GroupingRegionToggleText));
                OnPropertyChanged(nameof(SummaryBottomRegionToggleText));
                OnPropertyChanged(nameof(SideToolRegionToggleText));
                OnPropertyChanged(nameof(ChangePanelRegionToggleText));
                OnPropertyChanged(nameof(ValidationPanelRegionToggleText));
                OnPropertyChanged(nameof(SummaryDesignerRegionToggleText));
                OnPropertyChanged(nameof(CanToggleTopCommandStrip));
                OnPropertyChanged(nameof(CanCollapseGroupingRegion));
                OnPropertyChanged(nameof(CanCollapseSummaryBottomRegion));
                OnPropertyChanged(nameof(CanCollapseSideToolRegion));
                OnPropertyChanged(nameof(CanCollapseChangePanelRegion));
                OnPropertyChanged(nameof(CanCollapseValidationPanelRegion));
                OnPropertyChanged(nameof(CanCollapseSummaryDesignerRegion));
                OnPropertyChanged(nameof(CanCloseTopCommandStrip));
                OnPropertyChanged(nameof(CanCloseGroupingRegion));
                OnPropertyChanged(nameof(CanCloseSummaryBottomRegion));
                OnPropertyChanged(nameof(CanCloseSideToolRegion));
                OnPropertyChanged(nameof(CanCloseChangePanelRegion));
                OnPropertyChanged(nameof(CanCloseValidationPanelRegion));
                OnPropertyChanged(nameof(CanCloseSummaryDesignerRegion));
                RaiseWorkspacePanelTabVisibilityChanged();
                QueueWorkspacePanelTabOverflowLayout();
                LogDiagnostics("ApplyRegionLayout finished in " + stopwatch.ElapsedMilliseconds + " ms. Count=" + applyCounter.Count + ". " + GetGridSessionDescription() + ".");
            }
            finally
            {
                _isApplyingRegionLayout = false;
            }
        }

        private void RaiseWorkspacePanelTabVisibilityChanged()
        {
            OnPropertyChanged(nameof(ToolsPanelToolsTabVisibility));
            OnPropertyChanged(nameof(ToolsPanelChangesTabVisibility));
            OnPropertyChanged(nameof(ToolsPanelValidationTabVisibility));
            OnPropertyChanged(nameof(ToolsPanelSummaryDesignerTabVisibility));
            OnPropertyChanged(nameof(ChangesPanelToolsTabVisibility));
            OnPropertyChanged(nameof(ChangesPanelChangesTabVisibility));
            OnPropertyChanged(nameof(ChangesPanelValidationTabVisibility));
            OnPropertyChanged(nameof(ChangesPanelSummaryDesignerTabVisibility));
            OnPropertyChanged(nameof(ValidationPanelToolsTabVisibility));
            OnPropertyChanged(nameof(ValidationPanelChangesTabVisibility));
            OnPropertyChanged(nameof(ValidationPanelValidationTabVisibility));
            OnPropertyChanged(nameof(ValidationPanelSummaryDesignerTabVisibility));
            OnPropertyChanged(nameof(SummaryDesignerPanelToolsTabVisibility));
            OnPropertyChanged(nameof(SummaryDesignerPanelChangesTabVisibility));
            OnPropertyChanged(nameof(SummaryDesignerPanelValidationTabVisibility));
            OnPropertyChanged(nameof(SummaryDesignerPanelSummaryDesignerTabVisibility));
        }

        private void HandleWorkspacePanelTabOverflowLayoutChanged(object sender, SizeChangedEventArgs e)
        {
            QueueWorkspacePanelTabOverflowLayout();
        }

        private void QueueWorkspacePanelTabOverflowLayout()
        {
            if (_workspacePanelTabOverflowLayoutQueued)
            {
                return;
            }

            _workspacePanelTabOverflowLayoutQueued = true;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                _workspacePanelTabOverflowLayoutQueued = false;
                UpdateWorkspacePanelTabOverflows();
            }), DispatcherPriority.Loaded);
        }

        private void UpdateWorkspacePanelTabOverflows()
        {
            UpdateWorkspacePanelTabOverflow(
                GridRegionKind.SideToolRegion,
                SideToolRegionExpandedShell,
                ToolsPanelExpandedTabStrip,
                ToolsPanelOverflowTabButton,
                ToolsPanelToolsTabButton,
                ToolsPanelChangesTabButton,
                ToolsPanelValidationTabButton,
                ToolsPanelSummaryDesignerTabButton);
            UpdateWorkspacePanelTabOverflow(
                GridRegionKind.ChangePanelRegion,
                ChangePanelExpandedShell,
                ChangesPanelExpandedTabStrip,
                ChangesPanelOverflowTabButton,
                ChangesPanelToolsTabButton,
                ChangesPanelChangesTabButton,
                ChangesPanelValidationTabButton,
                ChangesPanelSummaryDesignerTabButton);
            UpdateWorkspacePanelTabOverflow(
                GridRegionKind.ValidationPanelRegion,
                ValidationPanelExpandedShell,
                ValidationPanelExpandedTabStrip,
                ValidationPanelOverflowTabButton,
                ValidationPanelToolsTabButton,
                ValidationPanelChangesTabButton,
                ValidationPanelValidationTabButton,
                ValidationPanelSummaryDesignerTabButton);
            UpdateWorkspacePanelTabOverflow(
                GridRegionKind.SummaryDesignerRegion,
                SummaryDesignerExpandedShell,
                SummaryDesignerExpandedTabStrip,
                SummaryDesignerPanelOverflowTabButton,
                SummaryDesignerPanelToolsTabButton,
                SummaryDesignerPanelChangesTabButton,
                SummaryDesignerPanelValidationTabButton,
                SummaryDesignerPanelSummaryDesignerTabButton);
        }

        private void UpdateWorkspacePanelTabOverflow(
            GridRegionKind hostRegionKind,
            FrameworkElement shell,
            StackPanel tabStrip,
            Button overflowButton,
            params Button[] tabButtons)
        {
            if (shell == null || tabStrip == null || overflowButton == null || tabButtons == null || shell.Visibility != Visibility.Visible)
            {
                return;
            }

            var logicalTabs = tabButtons
                .Where(button => button != null && ResolveWorkspacePanelTabVisibility(hostRegionKind, ResolveRegionKindFromTag(button)) == Visibility.Visible)
                .ToArray();
            foreach (var button in tabButtons.Where(button => button != null))
            {
                button.Visibility = Visibility.Collapsed;
            }

            overflowButton.Visibility = Visibility.Collapsed;
            overflowButton.ContextMenu = null;

            if (logicalTabs.Length == 0)
            {
                return;
            }

            var availableWidth = ResolveWorkspacePanelTabAvailableWidth(shell, tabStrip);
            var tabWidths = logicalTabs.ToDictionary(button => button, ResolveDesiredOuterWidth);
            var allTabsWidth = tabWidths.Values.Sum();
            if (allTabsWidth <= availableWidth)
            {
                foreach (var button in logicalTabs)
                {
                    button.Visibility = Visibility.Visible;
                }

                return;
            }

            var overflowWidth = ResolveDesiredOuterWidth(overflowButton);
            var availableTabsWidth = Math.Max(0d, availableWidth - overflowWidth);
            var visibleTabs = new HashSet<Button>();
            var activeTab = logicalTabs.FirstOrDefault(button => ResolveRegionKindFromTag(button) == hostRegionKind);
            var usedWidth = 0d;

            if (activeTab != null)
            {
                visibleTabs.Add(activeTab);
                usedWidth += tabWidths[activeTab];
            }

            foreach (var button in logicalTabs)
            {
                if (ReferenceEquals(button, activeTab))
                {
                    continue;
                }

                var nextWidth = tabWidths[button];
                if (usedWidth + nextWidth <= availableTabsWidth)
                {
                    visibleTabs.Add(button);
                    usedWidth += nextWidth;
                }
            }

            var overflowTabs = logicalTabs.Where(button => !visibleTabs.Contains(button)).ToArray();
            foreach (var button in logicalTabs)
            {
                button.Visibility = visibleTabs.Contains(button) ? Visibility.Visible : Visibility.Collapsed;
            }

            if (overflowTabs.Length == 0)
            {
                return;
            }

            overflowButton.ContextMenu = BuildWorkspacePanelTabOverflowMenu(overflowTabs);
            overflowButton.Visibility = Visibility.Visible;
        }

        private double ResolveWorkspacePanelTabAvailableWidth(FrameworkElement shell, FrameworkElement tabStrip)
        {
            if (shell.ActualWidth <= 0d)
            {
                return 0d;
            }

            var leftOffset = Math.Max(0d, tabStrip.Margin.Left);
            var rightOffset = Math.Max(0d, tabStrip.Margin.Right);
            return Math.Max(0d, shell.ActualWidth - leftOffset - rightOffset);
        }

        private double ResolveDesiredOuterWidth(FrameworkElement element)
        {
            var previousVisibility = element.Visibility;
            if (previousVisibility != Visibility.Visible)
            {
                element.Visibility = Visibility.Visible;
            }

            element.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            var width = element.DesiredSize.Width + element.Margin.Left + element.Margin.Right;
            element.Visibility = previousVisibility;
            return Math.Max(0d, width);
        }

        private ContextMenu BuildWorkspacePanelTabOverflowMenu(IEnumerable<Button> overflowTabs)
        {
            var menu = new ContextMenu
            {
                Style = TryFindResource("PgColumnContextMenuStyle") as Style,
            };

            foreach (var tab in overflowTabs)
            {
                var menuItem = new MenuItem
                {
                    Header = tab.Content,
                    Tag = tab.Tag,
                    Style = TryFindResource("PgColumnContextMenuItemStyle") as Style,
                };
                menuItem.Click += HandleOpenWorkspacePanelTabClick;
                menu.Items.Add(menuItem);
            }

            return menu;
        }

        private void HandleWorkspacePanelTabOverflowClick(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button button) || button.ContextMenu == null || button.ContextMenu.Items.Count == 0)
            {
                return;
            }

            button.ContextMenu.PlacementTarget = button;
            button.ContextMenu.Placement = PlacementMode.Top;
            button.ContextMenu.IsOpen = true;
            e.Handled = true;
        }

        private Visibility ResolveWorkspacePanelTabVisibility(GridRegionKind hostRegionKind, GridRegionKind targetRegionKind)
        {
            var states = _surfaceCoordinator.ExportResolvedRegionStates();
            var hostState = states.SingleOrDefault(state => state.RegionKind == hostRegionKind);
            var targetState = states.SingleOrDefault(state => state.RegionKind == targetRegionKind);
            if (hostState == null || targetState == null)
            {
                throw new InvalidOperationException("Missing required Core workspace panel region state.");
            }

            if (hostState.HostKind != GridRegionHostKind.WorkspacePanel ||
                targetState.HostKind != GridRegionHostKind.WorkspacePanel)
            {
                throw new InvalidOperationException("Workspace panel tabs can only bind workspace panel regions.");
            }

            return targetState.State != GridRegionState.Closed && targetState.Placement == hostState.Placement
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private bool IsResolvedRegionVisibilityEqual(GridRegionKind regionKind, bool isVisible)
        {
            var regionState = _surfaceCoordinator.ExportResolvedRegionStates()
                .SingleOrDefault(region => region.RegionKind == regionKind);
            if (regionState == null)
            {
                throw new InvalidOperationException("Missing required Core region state for " + regionKind + ".");
            }

            return (regionState.State != GridRegionState.Closed) == isVisible;
        }

        private static bool AreStateSnapshotsEquivalent(GridStateSnapshot left, GridStateSnapshot right)
        {
            if (left == null || right == null)
            {
                return left == right;
            }

            return AreColumnListsEquivalent(left.Layout?.Columns, right.Layout?.Columns) &&
                AreSortDescriptorsEquivalent(left.Sorts, right.Sorts) &&
                AreFilterGroupsEquivalent(left.Filters, right.Filters) &&
                AreGroupDescriptorsEquivalent(left.Groups, right.Groups) &&
                AreSummaryDescriptorsEquivalent(left.Summaries, right.Summaries) &&
                AreRegionLayoutsEquivalent(left.RegionLayout, right.RegionLayout) &&
                string.Equals(left.GlobalSearchText ?? string.Empty, right.GlobalSearchText ?? string.Empty, StringComparison.Ordinal) &&
                Nullable.Equals(left.SelectCurrentRow, right.SelectCurrentRow) &&
                Nullable.Equals(left.MultiSelect, right.MultiSelect) &&
                Nullable.Equals(left.ShowRowNumbers, right.ShowRowNumbers) &&
                Nullable.Equals(left.RowNumberingMode, right.RowNumberingMode);
        }

        private static bool AreColumnListsEquivalent(IReadOnlyList<GridColumnDefinition> left, IReadOnlyList<GridColumnDefinition> right)
        {
            if ((left?.Count ?? 0) != (right?.Count ?? 0))
            {
                return false;
            }

            for (var index = 0; index < (left?.Count ?? 0); index++)
            {
                if (!AreColumnsEquivalent(left[index], right[index]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool AreColumnsEquivalent(GridColumnDefinition left, GridColumnDefinition right)
        {
            if (left == null || right == null)
            {
                return left == right;
            }

            return string.Equals(left.Id, right.Id, StringComparison.Ordinal) &&
                string.Equals(left.Header, right.Header, StringComparison.Ordinal) &&
                left.Width.Equals(right.Width) &&
                left.MinWidth.Equals(right.MinWidth) &&
                left.IsVisible == right.IsVisible &&
                left.IsFrozen == right.IsFrozen &&
                left.IsEditable == right.IsEditable &&
                left.DisplayIndex == right.DisplayIndex &&
                left.ValueType == right.ValueType &&
                left.EditorKind == right.EditorKind &&
                left.EditorItemsMode == right.EditorItemsMode &&
                string.Equals(left.EditMask, right.EditMask, StringComparison.Ordinal) &&
                string.Equals(left.ValueKind, right.ValueKind, StringComparison.Ordinal) &&
                left.EditorItems.SequenceEqual(right.EditorItems, StringComparer.Ordinal);
        }

        private static bool AreSortDescriptorsEquivalent(IReadOnlyList<GridSortDescriptor> left, IReadOnlyList<GridSortDescriptor> right)
        {
            if ((left?.Count ?? 0) != (right?.Count ?? 0))
            {
                return false;
            }

            for (var index = 0; index < (left?.Count ?? 0); index++)
            {
                if (!string.Equals(left[index].ColumnId, right[index].ColumnId, StringComparison.Ordinal) ||
                    left[index].Direction != right[index].Direction)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool AreGroupDescriptorsEquivalent(IReadOnlyList<GridGroupDescriptor> left, IReadOnlyList<GridGroupDescriptor> right)
        {
            if ((left?.Count ?? 0) != (right?.Count ?? 0))
            {
                return false;
            }

            for (var index = 0; index < (left?.Count ?? 0); index++)
            {
                if (!string.Equals(left[index].ColumnId, right[index].ColumnId, StringComparison.Ordinal) ||
                    left[index].Direction != right[index].Direction)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool AreSummaryDescriptorsEquivalent(IReadOnlyList<GridSummaryDescriptor> left, IReadOnlyList<GridSummaryDescriptor> right)
        {
            if ((left?.Count ?? 0) != (right?.Count ?? 0))
            {
                return false;
            }

            for (var index = 0; index < (left?.Count ?? 0); index++)
            {
                if (!string.Equals(left[index].ColumnId, right[index].ColumnId, StringComparison.Ordinal) ||
                    left[index].Type != right[index].Type ||
                    left[index].ValueType != right[index].ValueType)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool AreFilterGroupsEquivalent(GridFilterGroup left, GridFilterGroup right)
        {
            if (left == null || right == null)
            {
                return left == right;
            }

            if (left.LogicalOperator != right.LogicalOperator || left.Filters.Count != right.Filters.Count)
            {
                return false;
            }

            for (var index = 0; index < left.Filters.Count; index++)
            {
                var leftFilter = left.Filters[index];
                var rightFilter = right.Filters[index];
                if (!string.Equals(leftFilter.ColumnId, rightFilter.ColumnId, StringComparison.Ordinal) ||
                    leftFilter.Operator != rightFilter.Operator ||
                    !object.Equals(leftFilter.Value, rightFilter.Value) ||
                    !object.Equals(leftFilter.SecondValue, rightFilter.SecondValue))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool AreRegionLayoutsEquivalent(GridRegionLayoutSnapshot left, GridRegionLayoutSnapshot right)
        {
            if ((left?.Regions?.Count ?? 0) != (right?.Regions?.Count ?? 0))
            {
                return false;
            }

            var rightByKind = right.Regions.ToDictionary(region => region.RegionKind);
            foreach (var leftRegion in left.Regions)
            {
                if (!rightByKind.TryGetValue(leftRegion.RegionKind, out var rightRegion) ||
                    leftRegion.State != rightRegion.State ||
                    leftRegion.IsActive != rightRegion.IsActive ||
                    leftRegion.PlacementOverride != rightRegion.PlacementOverride ||
                    !Nullable.Equals(leftRegion.Size, rightRegion.Size))
                {
                    return false;
                }
            }

            return true;
        }

        private WpfGridRegionLayoutAdapter CreateRegionLayoutAdapter()
        {
            return new WpfGridRegionLayoutAdapter(
                new[]
                {
                    new WpfGridWorkspaceBandBinding(
                        GridRegionKind.TopCommandRegion,
                        TopCommandStripRow,
                        TopCommandStripUtilityRow,
                        RegionLayoutFrame,
                        BottomWorkspaceBandHost,
                        TopCommandBand,
                        TopCommandStripContentHost,
                        ResolveRequiredRegionDefaultSize(GridRegionKind.TopCommandRegion),
                        false),
                    new WpfGridWorkspaceBandBinding(
                        GridRegionKind.GroupingRegion,
                        GroupingRegionRow,
                        GroupingBandUtilityRow,
                        TopWorkspaceBandHost,
                        BottomWorkspaceBandHost,
                        GroupingRegionHost,
                        GroupingBandContentHost,
                        56d,
                        true),
                    new WpfGridWorkspaceBandBinding(
                        GridRegionKind.SummaryBottomRegion,
                        SummaryBottomRegionRow,
                        SummaryBottomRegionSplitterRow,
                        TopWorkspaceBandHost,
                        BottomWorkspaceBandHost,
                        SummaryBottomRegionHost,
                        SummaryBottomRegionContentScrollViewer,
                        56d,
                        true),
                },
                new[]
                {
                    new WpfGridWorkspacePanelBinding(
                        GridRegionKind.SideToolRegion,
                        RegionSurfaceColumn,
                        LeftWorkspacePanelSplitterColumn,
                        LeftWorkspacePanelColumn,
                        SideToolRegionSplitterColumn,
                        SideToolRegionColumn,
                        new FrameworkElement[]
                        {
                            TopWorkspaceBandHost,
                            RegionLayoutFrame,
                            SummaryBottomRegionSplitter,
                            BottomWorkspaceBandHost,
                            BottomStatusStripHost,
                        },
                        SideToolRegionSplitter,
                        SideToolRegionHost,
                        SideToolRegionContentScrollViewer,
                        SideToolRegionCollapsedRail,
                        SideToolRegionExpandedShell,
                        SideToolRegionExpandedShellTransform,
                        ResolveRequiredRegionDefaultSize(GridRegionKind.SideToolRegion)),
                    new WpfGridWorkspacePanelBinding(
                        GridRegionKind.ChangePanelRegion,
                        RegionSurfaceColumn,
                        LeftWorkspacePanelSplitterColumn,
                        LeftWorkspacePanelColumn,
                        SideToolRegionSplitterColumn,
                        SideToolRegionColumn,
                        new FrameworkElement[]
                        {
                            TopWorkspaceBandHost,
                            RegionLayoutFrame,
                            SummaryBottomRegionSplitter,
                            BottomWorkspaceBandHost,
                            BottomStatusStripHost,
                        },
                        ChangePanelRegionSplitter,
                        ChangePanelRegionHost,
                        ChangePanelContentScrollViewer,
                        ChangePanelCollapsedRail,
                        ChangePanelExpandedShell,
                        ChangePanelExpandedShellTransform,
                        ResolveRequiredRegionDefaultSize(GridRegionKind.ChangePanelRegion)),
                    new WpfGridWorkspacePanelBinding(
                        GridRegionKind.ValidationPanelRegion,
                        RegionSurfaceColumn,
                        LeftWorkspacePanelSplitterColumn,
                        LeftWorkspacePanelColumn,
                        SideToolRegionSplitterColumn,
                        SideToolRegionColumn,
                        new FrameworkElement[]
                        {
                            TopWorkspaceBandHost,
                            RegionLayoutFrame,
                            SummaryBottomRegionSplitter,
                            BottomWorkspaceBandHost,
                            BottomStatusStripHost,
                        },
                        ValidationPanelRegionSplitter,
                        ValidationPanelRegionHost,
                        ValidationPanelContentScrollViewer,
                        ValidationPanelCollapsedRail,
                        ValidationPanelExpandedShell,
                        ValidationPanelExpandedShellTransform,
                        ResolveRequiredRegionDefaultSize(GridRegionKind.ValidationPanelRegion)),
                    new WpfGridWorkspacePanelBinding(
                        GridRegionKind.SummaryDesignerRegion,
                        RegionSurfaceColumn,
                        LeftWorkspacePanelSplitterColumn,
                        LeftWorkspacePanelColumn,
                        SideToolRegionSplitterColumn,
                        SideToolRegionColumn,
                        new FrameworkElement[]
                        {
                            TopWorkspaceBandHost,
                            RegionLayoutFrame,
                            SummaryBottomRegionSplitter,
                            BottomWorkspaceBandHost,
                            BottomStatusStripHost,
                        },
                        SummaryDesignerRegionSplitter,
                        SummaryDesignerRegionHost,
                        SummaryDesignerContentScrollViewer,
                        SummaryDesignerCollapsedRail,
                        SummaryDesignerExpandedShell,
                        SummaryDesignerExpandedShellTransform,
                        ResolveRequiredRegionDefaultSize(GridRegionKind.SummaryDesignerRegion)),
                });
        }

        private static double ResolveRequiredRegionDefaultSize(GridRegionKind regionKind)
        {
            var definition = GridRegionDefinitionCatalog.CreateDefault()
                .FirstOrDefault(candidate => candidate.RegionKind == regionKind);
            if (definition == null || !definition.DefaultSize.HasValue)
            {
                throw new InvalidOperationException("Missing required Core default size for " + regionKind + ".");
            }

            return definition.DefaultSize.Value;
        }

        private double ResolveRequiredDimensionResource(string resourceKey)
        {
            if (TryFindResource(resourceKey) is double value)
            {
                return value;
            }

            throw new InvalidOperationException("Missing required grid dimension resource '" + resourceKey + "'.");
        }

        private WpfGridRegionRenderSnapshot BuildRegionRenderSnapshot()
        {
            return new WpfGridRegionRenderSnapshot(
                new Dictionary<GridRegionKind, bool>
                {
                    [GridRegionKind.TopCommandRegion] = WpfGridRegionContentResolver.HasRenderableContent(TopCommandStripContentPresenter),
                    [GridRegionKind.GroupingRegion] = true,
                    [GridRegionKind.SummaryBottomRegion] = HasSummaries,
                    [GridRegionKind.SideToolRegion] = WpfGridRegionContentResolver.HasRenderableContent(SideToolRegionContentPresenter),
                    [GridRegionKind.ChangePanelRegion] = WpfGridRegionContentResolver.HasRenderableContent(ChangePanelContentPresenter),
                    [GridRegionKind.ValidationPanelRegion] = WpfGridRegionContentResolver.HasRenderableContent(ValidationPanelContentPresenter),
                    [GridRegionKind.SummaryDesignerRegion] = WpfGridRegionContentResolver.HasRenderableContent(SummaryDesignerContentPresenter),
                },
                new Dictionary<GridRegionKind, WpfGridRegionRenderDirectives>
                {
                    [GridRegionKind.TopCommandRegion] = new WpfGridRegionRenderDirectives(forceCompactSize: false),
                    [GridRegionKind.GroupingRegion] = new WpfGridRegionRenderDirectives(forceCompactSize: true),
                    [GridRegionKind.SummaryBottomRegion] = new WpfGridRegionRenderDirectives(forceCompactSize: false, autoSizeToContent: true),
                },
                SideToolRegionUsesDrawerChrome);
        }

        private WpfGridRegionChromeState GetChromeState(GridRegionKind regionKind)
        {
            return _regionLayoutAdapter == null
                ? WpfGridRegionChromeState.Hidden
                : _regionLayoutAdapter.GetChromeState(regionKind);
        }

        private void ToggleRegionCollapsed(GridRegionKind regionKind)
        {
            HandleRegionCommand(_surfaceInputAdapter.CreateRegionStateInput(
                GridRegionCommandKind.ToggleCollapse,
                regionKind,
                DateTime.UtcNow));
        }

        private void HideRegion(GridRegionKind regionKind)
        {
            HandleRegionCommand(_surfaceInputAdapter.CreateRegionStateInput(
                GridRegionCommandKind.Close,
                regionKind,
                DateTime.UtcNow));
        }

        private static GridRegionKind ResolveRegionKindFromTag(object sender)
        {
            if (!(sender is FrameworkElement element))
            {
                throw new InvalidOperationException("Region command source must be a FrameworkElement.");
            }

            if (!(element.Tag is string rawRegionKind) ||
                !Enum.TryParse(rawRegionKind, ignoreCase: false, out GridRegionKind regionKind) ||
                !Enum.IsDefined(typeof(GridRegionKind), regionKind))
            {
                throw new InvalidOperationException("Region command source must declare a valid GridRegionKind tag.");
            }

            return regionKind;
        }

        private void HandleRegionToggleButtonClick(object sender, RoutedEventArgs e)
        {
            var regionKind = ResolveRegionKindFromTag(sender);
            if (ResolveRegionHostKind(regionKind) == GridRegionHostKind.WorkspacePanel)
            {
                if (ResolveRequiredRegionState(regionKind).State == GridRegionState.Collapsed)
                {
                    OpenWorkspacePanel(regionKind);
                    return;
                }

                ToggleWorkspacePanelsCollapsed();
                return;
            }

            ToggleRegionCollapsed(regionKind);
        }

        private void HandleRegionCloseButtonClick(object sender, RoutedEventArgs e)
        {
            var regionKind = ResolveRegionKindFromTag(sender);
            if (ResolveRegionHostKind(regionKind) == GridRegionHostKind.WorkspacePanel)
            {
                HideWorkspacePanelAndActivateNext(regionKind);
                return;
            }

            HideRegion(regionKind);
        }

        private void HandleOpenWorkspacePanelTabClick(object sender, RoutedEventArgs e)
        {
            OpenWorkspacePanel(ResolveRegionKindFromTag(sender));
            e.Handled = true;
        }

        private void HideWorkspacePanels()
        {
            foreach (var regionKind in ResolveWorkspacePanelRegionKinds())
            {
                if (!IsResolvedRegionVisibilityEqual(regionKind, false))
                {
                    HideRegion(regionKind);
                }
            }
        }

        private void HideWorkspacePanelAndActivateNext(GridRegionKind regionKind)
        {
            var states = _surfaceCoordinator.ExportResolvedRegionStates();
            var closingState = states.Single(state => state.RegionKind == regionKind);
            HideRegion(regionKind);
            if (!closingState.IsActive)
            {
                return;
            }

            var nextRegion = states
                .Where(state =>
                    state.HostKind == GridRegionHostKind.WorkspacePanel &&
                    state.RegionKind != regionKind &&
                    state.State == GridRegionState.Open)
                .OrderBy(state => state.RegionKind)
                .Select(state => (GridRegionKind?)state.RegionKind)
                .FirstOrDefault();
            if (nextRegion.HasValue)
            {
                HandleRegionCommand(_surfaceInputAdapter.CreateRegionStateInput(
                    GridRegionCommandKind.Activate,
                    nextRegion.Value,
                    DateTime.UtcNow));
            }
        }

        private void ToggleWorkspacePanelsCollapsed()
        {
            var hasOpenPanel = _surfaceCoordinator.ExportResolvedRegionStates()
                .Any(state => state.HostKind == GridRegionHostKind.WorkspacePanel && state.State == GridRegionState.Open);
            foreach (var regionKind in ResolveWorkspacePanelRegionKinds())
            {
                var state = _surfaceCoordinator.ExportResolvedRegionStates().Single(candidate => candidate.RegionKind == regionKind);
                if (hasOpenPanel && state.State == GridRegionState.Open)
                {
                    ToggleRegionCollapsed(regionKind);
                }
                else if (!hasOpenPanel && state.State == GridRegionState.Collapsed)
                {
                    ToggleRegionCollapsed(regionKind);
                }
            }
        }

        private static IReadOnlyList<GridRegionKind> ResolveWorkspacePanelRegionKinds()
        {
            return GridRegionDefinitionCatalog.CreateDefault()
                .Where(region => region.HostKind == GridRegionHostKind.WorkspacePanel)
                .Select(region => region.RegionKind)
                .ToArray();
        }

        private void HandleRegionDragMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!(sender is FrameworkElement source))
            {
                throw new InvalidOperationException("Region drag source must be a FrameworkElement.");
            }

            if (IsRegionDragStartedFromInteractiveChild(e.OriginalSource))
            {
                return;
            }

            _regionDragKind = ResolveRegionKindFromTag(sender);
            _regionDragSource = source;
            _regionDragStartPoint = e.GetPosition(this);
            _isRegionDragArmed = true;
            if (!source.CaptureMouse())
            {
                ClearRegionDragState();
                throw new InvalidOperationException("Region drag source could not capture the mouse.");
            }
        }

        private void HandleRegionDragMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isRegionDragArmed || _regionDragSource == null || e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            var currentPoint = e.GetPosition(this);
            if (Math.Abs(currentPoint.X - _regionDragStartPoint.X) >= RegionDragThreshold ||
                Math.Abs(currentPoint.Y - _regionDragStartPoint.Y) >= RegionDragThreshold)
            {
                BeginRegionDragPreview(_regionDragKind.Value);
                UpdateRegionDragPreview(currentPoint - _regionDragStartPoint, e.GetPosition(RegionLayoutFrame), e.GetPosition(GridRootFrame));
                _regionDragSource.Cursor = Cursors.SizeAll;
            }
        }

        private void HandleRegionDragMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isRegionDragArmed || !_regionDragKind.HasValue)
            {
                ClearRegionDragState();
                return;
            }

            try
            {
                var currentPoint = e.GetPosition(this);
                if (Math.Abs(currentPoint.X - _regionDragStartPoint.X) < RegionDragThreshold &&
                    Math.Abs(currentPoint.Y - _regionDragStartPoint.Y) < RegionDragThreshold)
                {
                    return;
                }

                MoveRegion(_regionDragKind.Value, ResolveRegionPlacementForDrag(_regionDragKind.Value, e.GetPosition(RegionLayoutFrame), e.GetPosition(GridRootFrame)));
            }
            finally
            {
                ClearRegionDragState();
            }
        }

        private void MoveRegion(GridRegionKind regionKind, GridRegionPlacement placement)
        {
            HandleRegionCommand(_surfaceInputAdapter.CreateRegionMoveInput(
                regionKind,
                placement,
                DateTime.UtcNow));
        }

        private GridRegionViewState ResolveRequiredRegionState(GridRegionKind regionKind)
        {
            var regionState = _surfaceCoordinator.ExportResolvedRegionStates()
                .SingleOrDefault(region => region.RegionKind == regionKind);
            if (regionState == null)
            {
                throw new InvalidOperationException("Missing required Core region state for " + regionKind + ".");
            }

            return regionState;
        }

        private GridRegionPlacement ResolveHorizontalRegionPlacement(Point point)
        {
            if (RegionLayoutFrame == null || RegionLayoutFrame.ActualWidth <= 0d)
            {
                throw new InvalidOperationException("Region layout frame must be measured before resolving region placement.");
            }

            return point.X < RegionLayoutFrame.ActualWidth / 2d
                ? GridRegionPlacement.Left
                : GridRegionPlacement.Right;
        }

        private GridRegionPlacement ResolveVerticalRegionPlacement(Point point)
        {
            if (GridRootFrame == null || GridRootFrame.ActualHeight <= 0d)
            {
                throw new InvalidOperationException("Grid root frame must be measured before resolving workspace band placement.");
            }

            return point.Y < GridRootFrame.ActualHeight / 2d
                ? GridRegionPlacement.Top
                : GridRegionPlacement.Bottom;
        }

        private GridRegionPlacement ResolveRegionPlacementForDrag(GridRegionKind regionKind, Point regionFramePoint, Point rootFramePoint)
        {
            switch (ResolveRegionHostKind(regionKind))
            {
                case GridRegionHostKind.WorkspacePanel:
                    return ResolveHorizontalRegionPlacement(regionFramePoint);
                case GridRegionHostKind.WorkspaceBand:
                    return ResolveVerticalRegionPlacement(rootFramePoint);
                default:
                    throw new InvalidOperationException(regionKind + " cannot be moved by workspace drag.");
            }
        }

        private void HandleRegionDragLostMouseCapture(object sender, MouseEventArgs e)
        {
            if (!_isRegionDragArmed)
            {
                return;
            }

            ClearRegionDragState();
        }

        private GridRegionHostKind ResolveRegionHostKind(GridRegionKind regionKind)
        {
            var state = _surfaceCoordinator.ExportResolvedRegionStates()
                .SingleOrDefault(candidate => candidate.RegionKind == regionKind);
            if (state == null)
            {
                throw new InvalidOperationException("Missing resolved region state for " + regionKind + ".");
            }

            return state.HostKind;
        }

        private void ClearRegionDragState()
        {
            EndRegionDragPreview();

            var dragSource = _regionDragSource;
            _regionDragSource = null;
            _regionDragKind = null;
            _isRegionDragArmed = false;

            if (dragSource != null)
            {
                dragSource.Cursor = null;
                if (dragSource.IsMouseCaptured)
                {
                    dragSource.ReleaseMouseCapture();
                }
            }
        }

        private static bool IsRegionDragStartedFromInteractiveChild(object originalSource)
        {
            var dependencyObject = originalSource as DependencyObject;
            while (dependencyObject != null)
            {
                if (dependencyObject is ButtonBase)
                {
                    return true;
                }

                dependencyObject = VisualTreeHelper.GetParent(dependencyObject);
            }

            return false;
        }

        private void BeginRegionDragPreview(GridRegionKind regionKind)
        {
            if (_isRegionDragPreviewActive)
            {
                return;
            }

            var hostKind = ResolveRegionHostKind(regionKind);
            if (hostKind != GridRegionHostKind.WorkspacePanel && hostKind != GridRegionHostKind.WorkspaceBand)
            {
                return;
            }

            _isRegionDragPreviewActive = true;
            _regionDragPreviewHostKind = hostKind;
            _regionDragPreviewKind = regionKind;
            Panel.SetZIndex(RegionDockPreviewOverlay, 10);
            Panel.SetZIndex(WorkspaceBandDockPreviewOverlay, 10);
            ApplyRegionDockPreviewVisibility(hostKind);

            if (hostKind == GridRegionHostKind.WorkspacePanel)
            {
                ResolveWorkspacePanelDragChrome(regionKind, out var host, out var panel, out var expandedShell, out _);
                _regionDragPreviousHostZIndex = Panel.GetZIndex(host);
                _regionDragPreviousHostBackground = host.Background;
                _regionDragPreviousPanelBackground = panel.Background;
                Panel.SetZIndex(host, 20);
                host.Background = Brushes.Transparent;
                panel.Background = Brushes.Transparent;
                expandedShell.BeginAnimation(OpacityProperty, null);
                expandedShell.Opacity = 0.92d;
            }
        }

        private void UpdateRegionDragPreview(Vector dragDelta, Point currentRegionFramePoint)
        {
            UpdateRegionDragPreview(dragDelta, currentRegionFramePoint, currentRegionFramePoint);
        }

        private void UpdateRegionDragPreview(Vector dragDelta, Point currentRegionFramePoint, Point currentRootFramePoint)
        {
            if (!_isRegionDragPreviewActive)
            {
                return;
            }

            if (_regionDragPreviewHostKind == GridRegionHostKind.WorkspacePanel)
            {
                if (!_regionDragPreviewKind.HasValue)
                {
                    throw new InvalidOperationException("Workspace panel drag preview requires a region kind.");
                }

                ResolveWorkspacePanelDragChrome(_regionDragPreviewKind.Value, out _, out _, out _, out var expandedShellTransform);
                expandedShellTransform.BeginAnimation(TranslateTransform.XProperty, null);
                expandedShellTransform.X = dragDelta.X;
                UpdateRegionDockPreview(ResolveHorizontalRegionPlacement(currentRegionFramePoint));
                return;
            }

            if (_regionDragPreviewHostKind == GridRegionHostKind.WorkspaceBand)
            {
                UpdateRegionDockPreview(ResolveVerticalRegionPlacement(currentRootFramePoint));
            }
        }

        private void UpdateRegionDockPreview(GridRegionPlacement placement)
        {
            RegionDockPreviewLeft.Opacity = placement == GridRegionPlacement.Left ? 0.62d : 0.24d;
            RegionDockPreviewRight.Opacity = placement == GridRegionPlacement.Right ? 0.62d : 0.24d;
            RegionDockPreviewTop.Opacity = placement == GridRegionPlacement.Top ? 0.62d : 0.24d;
            RegionDockPreviewBottom.Opacity = placement == GridRegionPlacement.Bottom ? 0.62d : 0.24d;
        }

        private void ApplyRegionDockPreviewVisibility(GridRegionHostKind hostKind)
        {
            var isWorkspacePanel = hostKind == GridRegionHostKind.WorkspacePanel;
            RegionDockPreviewOverlay.Visibility = isWorkspacePanel ? Visibility.Visible : Visibility.Collapsed;
            WorkspaceBandDockPreviewOverlay.Visibility = isWorkspacePanel ? Visibility.Collapsed : Visibility.Visible;
            RegionDockPreviewLeft.Visibility = isWorkspacePanel ? Visibility.Visible : Visibility.Collapsed;
            RegionDockPreviewRight.Visibility = isWorkspacePanel ? Visibility.Visible : Visibility.Collapsed;
            RegionDockPreviewTop.Visibility = isWorkspacePanel ? Visibility.Collapsed : Visibility.Visible;
            RegionDockPreviewBottom.Visibility = isWorkspacePanel ? Visibility.Collapsed : Visibility.Visible;
        }

        private void EndRegionDragPreview()
        {
            if (!_isRegionDragPreviewActive)
            {
                return;
            }

            _isRegionDragPreviewActive = false;
            RegionDockPreviewOverlay.Visibility = Visibility.Collapsed;
            WorkspaceBandDockPreviewOverlay.Visibility = Visibility.Collapsed;
            RegionDockPreviewLeft.Opacity = 0.24d;
            RegionDockPreviewRight.Opacity = 0.24d;
            RegionDockPreviewTop.Opacity = 0.24d;
            RegionDockPreviewBottom.Opacity = 0.24d;
            RegionDockPreviewLeft.Visibility = Visibility.Collapsed;
            RegionDockPreviewRight.Visibility = Visibility.Collapsed;
            RegionDockPreviewTop.Visibility = Visibility.Collapsed;
            RegionDockPreviewBottom.Visibility = Visibility.Collapsed;

            var wasWorkspacePanelDrag = _regionDragPreviewHostKind == GridRegionHostKind.WorkspacePanel;
            var previewKind = _regionDragPreviewKind;
            _regionDragPreviewHostKind = null;
            _regionDragPreviewKind = null;

            if (_regionDragPreviousHostZIndex.HasValue)
            {
                if (!previewKind.HasValue)
                {
                    throw new InvalidOperationException("Workspace panel drag preview requires a region kind.");
                }

                ResolveWorkspacePanelDragChrome(previewKind.Value, out var host, out var panel, out _, out _);
                Panel.SetZIndex(host, _regionDragPreviousHostZIndex.Value);
                host.Background = _regionDragPreviousHostBackground;
                panel.Background = _regionDragPreviousPanelBackground;
            }

            _regionDragPreviousHostZIndex = null;
            _regionDragPreviousHostBackground = null;
            _regionDragPreviousPanelBackground = null;
            if (!wasWorkspacePanelDrag)
            {
                return;
            }

            if (!previewKind.HasValue)
            {
                throw new InvalidOperationException("Workspace panel drag preview requires a region kind.");
            }

            ResolveWorkspacePanelDragChrome(previewKind.Value, out _, out _, out var expandedShell, out var expandedShellTransform);
            expandedShell.BeginAnimation(
                OpacityProperty,
                new DoubleAnimation
                {
                    To = 1d,
                    Duration = TimeSpan.FromMilliseconds(120d),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                },
                HandoffBehavior.SnapshotAndReplace);
            expandedShellTransform.BeginAnimation(
                TranslateTransform.XProperty,
                new DoubleAnimation
                {
                    To = 0d,
                    Duration = TimeSpan.FromMilliseconds(160d),
                    EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.25d }
                },
                HandoffBehavior.SnapshotAndReplace);
        }

        private void ResolveWorkspacePanelDragChrome(
            GridRegionKind regionKind,
            out Grid host,
            out Grid panel,
            out FrameworkElement expandedShell,
            out TranslateTransform expandedShellTransform)
        {
            switch (regionKind)
            {
                case GridRegionKind.SideToolRegion:
                    host = SideToolRegionHost;
                    panel = ToolsPanel;
                    expandedShell = SideToolRegionExpandedShell;
                    expandedShellTransform = SideToolRegionExpandedShellTransform;
                    return;
                case GridRegionKind.ChangePanelRegion:
                    host = ChangePanelRegionHost;
                    panel = ChangesPanel;
                    expandedShell = ChangePanelExpandedShell;
                    expandedShellTransform = ChangePanelExpandedShellTransform;
                    return;
                case GridRegionKind.ValidationPanelRegion:
                    host = ValidationPanelRegionHost;
                    panel = ValidationPanel;
                    expandedShell = ValidationPanelExpandedShell;
                    expandedShellTransform = ValidationPanelExpandedShellTransform;
                    return;
                case GridRegionKind.SummaryDesignerRegion:
                    host = SummaryDesignerRegionHost;
                    panel = SummaryDesignerPanel;
                    expandedShell = SummaryDesignerExpandedShell;
                    expandedShellTransform = SummaryDesignerExpandedShellTransform;
                    return;
                default:
                    throw new InvalidOperationException(regionKind + " is not a workspace panel.");
            }
        }

        private void HandleRegionSplitterDragCompleted(object sender, DragCompletedEventArgs e)
        {
            try
            {
                switch (ResolveRegionKindFromTag(sender))
                {
                    case GridRegionKind.GroupingRegion:
                        PersistRowRegionSize(GridRegionKind.GroupingRegion, GroupingRegionRow);
                        return;
                    case GridRegionKind.SummaryBottomRegion:
                        PersistRowRegionSize(GridRegionKind.SummaryBottomRegion, SummaryBottomRegionRow);
                        return;
                    case GridRegionKind.SideToolRegion:
                        PersistColumnRegionSize(GridRegionKind.SideToolRegion, ResolveWorkspacePanelSizeColumn(GridRegionKind.SideToolRegion));
                        return;
                    case GridRegionKind.ChangePanelRegion:
                        PersistColumnRegionSize(GridRegionKind.ChangePanelRegion, ResolveWorkspacePanelSizeColumn(GridRegionKind.ChangePanelRegion));
                        return;
                    case GridRegionKind.ValidationPanelRegion:
                        PersistColumnRegionSize(GridRegionKind.ValidationPanelRegion, ResolveWorkspacePanelSizeColumn(GridRegionKind.ValidationPanelRegion));
                        return;
                    case GridRegionKind.SummaryDesignerRegion:
                        PersistColumnRegionSize(GridRegionKind.SummaryDesignerRegion, ResolveWorkspacePanelSizeColumn(GridRegionKind.SummaryDesignerRegion));
                        return;
                    default:
                        throw new InvalidOperationException("Unsupported region splitter binding.");
                }
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is ArgumentOutOfRangeException)
            {
                LogDiagnostics("Region splitter resize ignored. " + ex.Message);
                ApplyRegionLayout();
            }
        }

        private void PersistRowRegionSize(GridRegionKind regionKind, RowDefinition row)
        {
            if (row == null)
            {
                throw new ArgumentNullException(nameof(row));
            }

            var requestedSize = ResolvePersistedRegionSize(regionKind, row.Height, row.ActualHeight);
            HandleRegionCommand(_surfaceInputAdapter.CreateRegionResizeInput(
                regionKind,
                requestedSize,
                DateTime.UtcNow));
        }

        private void PersistColumnRegionSize(GridRegionKind regionKind, ColumnDefinition column)
        {
            if (column == null)
            {
                throw new ArgumentNullException(nameof(column));
            }

            var requestedSize = ResolvePersistedRegionSize(regionKind, column.Width, column.ActualWidth);
            HandleRegionCommand(_surfaceInputAdapter.CreateRegionResizeInput(
                regionKind,
                requestedSize,
                DateTime.UtcNow));
        }

        private ColumnDefinition ResolveWorkspacePanelSizeColumn(GridRegionKind regionKind)
        {
            var state = _surfaceCoordinator.ExportResolvedRegionStates()
                .Single(region => region.RegionKind == regionKind);
            switch (state.Placement)
            {
                case GridRegionPlacement.Left:
                    return LeftWorkspacePanelColumn;
                case GridRegionPlacement.Right:
                    return SideToolRegionColumn;
                default:
                    throw new InvalidOperationException("WPF workspace panel size persistence supports Left and Right placements.");
            }
        }

        private void HandleRegionCommand(string commandId)
        {
            var universalArgs = WpfUniversalInputAdapter.CreateCommandEventArgs(commandId, Keyboard.Modifiers);
            HandleRegionCommand(_surfaceInputAdapter.CreateRegionCommandInput(universalArgs, DateTime.UtcNow));
        }

        private void HandleRegionCommand(GridRegionCommandInput input)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            _surfaceCoordinator.ProcessInput(input);
            ClearChangePanelChangedRowsFilterWhenPanelIsNotVisible();
            ApplyRegionLayout();
            RaiseViewStateChanged();
        }

        private void ClearChangePanelChangedRowsFilterWhenPanelIsNotVisible()
        {
            if (!_isChangePanelChangedRowsFilterActive || IsChangePanelVisibleForChangedRowsFilter())
            {
                return;
            }

            ClearChangePanelChangedRowsFilter();
        }

        private bool IsChangePanelVisibleForChangedRowsFilter()
        {
            var state = ResolveRequiredRegionState(GridRegionKind.ChangePanelRegion);
            return state.State == GridRegionState.Open && state.IsActive;
        }

        private static double ResolvePersistedRegionSize(GridRegionKind regionKind, GridLength configuredSize, double actualSize)
        {
            var requestedSize = configuredSize.IsAbsolute ? configuredSize.Value : actualSize;
            var definition = GridRegionDefinitionCatalog.CreateDefault()
                .FirstOrDefault(candidate => candidate.RegionKind == regionKind);
            if (requestedSize > 0d && !double.IsNaN(requestedSize) && !double.IsInfinity(requestedSize))
            {
                return ClampPersistedRegionSize(definition, requestedSize);
            }

            if (definition?.MinSize is double minSize && minSize > 0d)
            {
                return minSize;
            }

            if (definition?.DefaultSize is double defaultSize && defaultSize > 0d)
            {
                return defaultSize;
            }

            throw new InvalidOperationException("Region resize must resolve to a positive pixel size.");
        }

        private static double ClampPersistedRegionSize(GridRegionDefinition definition, double requestedSize)
        {
            var resolved = requestedSize;
            if (definition?.MinSize is double minSize)
            {
                resolved = Math.Max(resolved, minSize);
            }

            if (definition?.MaxSize is double maxSize)
            {
                resolved = Math.Min(resolved, maxSize);
            }

            return resolved;
        }

        private void ExecuteWithoutViewStateNotifications(Action action)
        {
            _suppressViewStateNotifications = true;
            try
            {
                action?.Invoke();
            }
            finally
            {
                _suppressViewStateNotifications = false;
            }
        }

        public void SetColumnVisibility(string columnId, bool isVisible)
        {
            if (_layoutState == null)
            {
                return;
            }

            _layoutState.SetColumnVisibility(columnId, isVisible);
            RebuildColumnBindings();
            RaiseViewStateChanged();
        }

        public void SetColumnFrozen(string columnId, bool frozen)
        {
            if (_layoutState == null)
            {
                return;
            }

            _layoutState.SetFrozen(columnId, frozen);
            NormalizeFrozenColumns();
            RebuildColumnBindings();
            RaiseViewStateChanged();
        }

        public void ShowAllColumns()
        {
            if (_layoutState == null)
            {
                return;
            }

            foreach (var column in _layoutState.Columns)
            {
                _layoutState.SetColumnVisibility(column.Id, true);
            }

            RebuildColumnBindings();
            RaiseViewStateChanged();
        }

        public void AutoFitVisibleColumns()
        {
            if (_layoutState == null)
            {
                return;
            }

            foreach (var column in _layoutState.VisibleColumns)
            {
                AutoFitColumnCore(column.Id);
            }

            RebuildColumnBindings();
            RaiseViewStateChanged();
        }

        public void AutoFitColumn(string columnId)
        {
            if (!AutoFitColumnCore(columnId))
            {
                return;
            }

            RebuildColumnBindings();
            RaiseViewStateChanged();
        }

        public void MoveColumnLeft(string columnId)
        {
            MoveVisibleColumn(columnId, -1);
        }

        public void MoveColumnRight(string columnId)
        {
            MoveVisibleColumn(columnId, 1);
        }

        internal void ReorderColumn(string columnId, string targetColumnId)
        {
            if (_layoutState == null ||
                string.IsNullOrWhiteSpace(columnId) ||
                string.IsNullOrWhiteSpace(targetColumnId) ||
                string.Equals(columnId, targetColumnId, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var targetColumn = _layoutState.VisibleColumns
                .FirstOrDefault(column => string.Equals(column.Id, targetColumnId, StringComparison.OrdinalIgnoreCase));
            if (targetColumn == null)
            {
                return;
            }

            _layoutState.ReorderColumn(columnId, targetColumn.DisplayIndex);
            NormalizeFrozenColumns();
            RebuildColumnBindings();
            RaiseViewStateChanged();
        }

        public void WidenColumn(string columnId)
        {
            AdjustColumnWidth(columnId, ResolveColumnResizeStep());
        }

        public void NarrowColumn(string columnId)
        {
            AdjustColumnWidth(columnId, -ResolveColumnResizeStep());
        }

        internal void SetColumnWidth(string columnId, double width)
        {
            if (_layoutState == null || string.IsNullOrWhiteSpace(columnId) || double.IsNaN(width) || double.IsInfinity(width))
            {
                return;
            }

            var column = TryGetLayoutColumn(columnId);
            if (column == null)
            {
                return;
            }

            var normalizedWidth = Math.Max(column.MinWidth, width);
            _layoutState.ResizeColumn(column.Id, normalizedWidth);
            RebuildColumnBindings();
            RaiseViewStateChanged();
        }

        private bool AutoFitColumnCore(string columnId)
        {
            if (_layoutState == null || string.IsNullOrWhiteSpace(columnId))
            {
                return false;
            }

            var column = TryGetLayoutColumn(columnId);
            if (column == null)
            {
                return false;
            }

            var width = EstimateAutoFitWidth(column);
            _layoutState.ResizeColumn(column.Id, width);
            return true;
        }

        private void MoveVisibleColumn(string columnId, int offset)
        {
            if (_layoutState == null || string.IsNullOrWhiteSpace(columnId) || offset == 0)
            {
                return;
            }

            var orderedVisibleColumns = _layoutState.VisibleColumns
                .OrderBy(column => column.DisplayIndex)
                .ToList();
            var currentVisibleIndex = orderedVisibleColumns.FindIndex(column =>
                string.Equals(column.Id, columnId, StringComparison.OrdinalIgnoreCase));
            if (currentVisibleIndex < 0)
            {
                return;
            }

            var targetVisibleIndex = Math.Max(0, Math.Min(orderedVisibleColumns.Count - 1, currentVisibleIndex + offset));
            if (targetVisibleIndex == currentVisibleIndex)
            {
                return;
            }

            var targetColumn = orderedVisibleColumns[targetVisibleIndex];
            _layoutState.ReorderColumn(columnId, targetColumn.DisplayIndex);
            NormalizeFrozenColumns();
            RebuildColumnBindings();
            RaiseViewStateChanged();
        }

        private void AdjustColumnWidth(string columnId, double delta)
        {
            if (_layoutState == null || string.IsNullOrWhiteSpace(columnId) || Math.Abs(delta) < 0.1d)
            {
                return;
            }

            var column = TryGetLayoutColumn(columnId);
            if (column == null)
            {
                return;
            }

            _layoutState.ResizeColumn(column.Id, Math.Max(column.MinWidth, column.Width + delta));
            RebuildColumnBindings();
            RaiseViewStateChanged();
        }

        private double EstimateAutoFitWidth(GridColumnDefinition column)
        {
            var sourceRows = (_currentFilteredRows.Count == 0
                    ? EnumerateEffectiveItemsSource()
                    : _currentFilteredRows)
                .Take(60)
                .ToArray();
            var samples = sourceRows
                .Select(row => Convert.ToString(ResolveRowValue(row, column.Id), CultureInfo.CurrentCulture) ?? string.Empty)
                .Concat(new[] { column.Header });
            return _layoutState.EstimateAutoFitWidth(column.Id, samples);
        }

        private GridColumnDefinition TryGetLayoutColumn(string columnId)
        {
            return (_layoutState?.Columns ?? Array.Empty<GridColumnDefinition>())
                .FirstOrDefault(column => string.Equals(column.Id, columnId, StringComparison.OrdinalIgnoreCase));
        }

        private bool CanMoveVisibleColumn(string columnId, int offset)
        {
            if (_layoutState == null || string.IsNullOrWhiteSpace(columnId) || offset == 0)
            {
                return false;
            }

            var orderedVisibleColumns = _layoutState.VisibleColumns
                .OrderBy(column => column.DisplayIndex)
                .ToList();
            var currentVisibleIndex = orderedVisibleColumns.FindIndex(column =>
                string.Equals(column.Id, columnId, StringComparison.OrdinalIgnoreCase));
            if (currentVisibleIndex < 0)
            {
                return false;
            }

            var targetVisibleIndex = currentVisibleIndex + offset;
            return targetVisibleIndex >= 0 && targetVisibleIndex < orderedVisibleColumns.Count;
        }

        private bool CanNarrowColumn(string columnId)
        {
            var column = TryGetLayoutColumn(columnId);
            return column != null && column.Width > (column.MinWidth + 0.1d);
        }

        private bool CanAutoFitColumn(string columnId)
        {
            return TryGetLayoutColumn(columnId) != null;
        }

        private double ResolveColumnResizeStep()
        {
            return _resolvedInteractionMode == GridInteractionMode.Classic
                ? ClassicColumnResizeStep
                : TouchColumnResizeStep;
        }

        public string ExportCurrentViewToCsv()
        {
            var exportColumns = BuildCsvExportColumns();
            if (exportColumns.Count == 0)
            {
                return string.Empty;
            }

            var rows = RowsView.Cast<object>()
                .OfType<GridDisplayRowModel>()
                .Where(row => row.SourceRow != null)
                .Select(row => row.SourceRow)
                .ToArray();

            var headerToColumnId = exportColumns.ToDictionary(column => column.Header, column => column.ColumnId, StringComparer.OrdinalIgnoreCase);
            return GridCsvExporter.Export(
                rows,
                exportColumns.Select(column => column.Header).ToArray(),
                (row, header) => ResolveCsvExportValue(row, headerToColumnId[header]),
                new GridCsvOptions { IncludeHeader = true });
        }

        public void CopySelectionToClipboard()
        {
            var snapshot = _surfaceCoordinator.GetCurrentSnapshot();
            if (snapshot == null)
            {
                return;
            }

            var selectedRows = snapshot.Cells
                .Where(cell => cell.IsSelected)
                .GroupBy(cell => cell.RowKey, StringComparer.OrdinalIgnoreCase)
                .Select(group => group
                    .OrderBy(cell => ResolveColumnDisplayIndex(cell.ColumnKey))
                    .Select(cell => cell.DisplayText ?? string.Empty)
                    .Cast<string>()
                    .ToArray())
                .Where(row => row.Length > 0)
                .Cast<IReadOnlyList<string>>()
                .ToArray();

            if (selectedRows.Length == 0 && snapshot.CurrentCell != null)
            {
                var currentCell = snapshot.Cells.FirstOrDefault(cell =>
                    string.Equals(cell.RowKey, snapshot.CurrentCell.RowKey, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(cell.ColumnKey, snapshot.CurrentCell.ColumnKey, StringComparison.OrdinalIgnoreCase));
                if (currentCell != null)
                {
                    selectedRows = new[] { (IReadOnlyList<string>)new[] { currentCell.DisplayText ?? string.Empty } };
                }
            }

            if (selectedRows.Length == 0)
            {
                return;
            }

            Clipboard.SetText(global::PhialeGrid.Core.Clipboard.GridClipboardCodec.Encode(selectedRows));
        }

        public void SelectVisibleRows()
        {
            _surfaceCoordinator.SelectRows(ResolveSelectableSurfaceRowKeys());
        }

        public void ClearSelection()
        {
            _surfaceCoordinator.ClearSelection();
        }

        public bool FocusRow(string rowId, string columnId = null)
        {
            if (string.IsNullOrWhiteSpace(rowId))
            {
                return false;
            }

            if (!TryResolveOrRevealSurfaceRowKey(rowId, out var rowKey))
            {
                return false;
            }

            if (ShouldDeferVisualWorkBecauseBatched() && !_surfaceRowsByKey.ContainsKey(rowKey))
            {
                _pendingCurrentCellRowKeyAfterBatch = rowKey;
                _pendingCurrentCellColumnKeyAfterBatch = columnId;
                return true;
            }

            _surfaceCoordinator.SetCurrentCell(rowKey, columnId);
            return true;
        }

        public bool ScrollRowIntoView(string rowId, GridScrollAlignment alignment = GridScrollAlignment.Nearest)
        {
            if (string.IsNullOrWhiteSpace(rowId))
            {
                return false;
            }

            return TryResolveOrRevealSurfaceRowKey(rowId, out var rowKey) &&
                _surfaceCoordinator.TryScrollRowIntoView(rowKey, alignment);
        }

        public bool ScrollColumnIntoView(string columnId, GridScrollAlignment alignment = GridScrollAlignment.Nearest)
        {
            if (string.IsNullOrWhiteSpace(columnId))
            {
                return false;
            }

            return _surfaceCoordinator.TryScrollColumnIntoView(columnId, alignment);
        }

        public bool ScrollCellIntoView(string rowId, string columnId, GridScrollAlignment alignment = GridScrollAlignment.Nearest, bool setCurrentCell = false)
        {
            if (string.IsNullOrWhiteSpace(rowId) || string.IsNullOrWhiteSpace(columnId))
            {
                return false;
            }

            return TryResolveOrRevealSurfaceRowKey(rowId, out var rowKey) &&
                _surfaceCoordinator.TryScrollCellIntoView(rowKey, columnId, alignment, setCurrentCell);
        }

        public string GetPrimaryValidationColumnId(string rowId)
        {
            if (string.IsNullOrWhiteSpace(rowId))
            {
                return string.Empty;
            }

            return ResolveValidationDetails(rowId)
                .Select(detail => detail.ColumnId)
                .FirstOrDefault(columnId => !string.IsNullOrWhiteSpace(columnId))
                ?? string.Empty;
        }

        public void SetCheckedRows(IEnumerable<string> rowIds)
        {
            _surfaceCoordinator.SetCheckedRows(ResolveSurfaceRowKeys(rowIds));
        }

        public bool SetRowValueForDemo(string rowId, string columnId, object value)
        {
            if (string.IsNullOrWhiteSpace(rowId) || string.IsNullOrWhiteSpace(columnId))
            {
                return false;
            }

            var row = FindCurrentRow(rowId);
            if (row == null)
            {
                return false;
            }

            _editSessionContext.BeginRecordEdit(rowId);
            if (!_editSessionContext.TrySetFieldValue(rowId, columnId, value, Convert.ToString(value, CultureInfo.CurrentCulture)))
            {
                return false;
            }

            _editSessionContext.CompleteRecordEdit(rowId, _editSessionContext.HasRecordChanges(rowId));
            RefreshRowsView();
            return true;
        }

        public void ExpandAllGroups()
        {
            if (!HasGroups)
            {
                return;
            }

            SetAllGroupsExpandedState(_currentGroupIds, true);
            RefreshRowsView();
        }

        public void CollapseAllGroups()
        {
            if (!HasGroups)
            {
                return;
            }

            SetAllGroupsExpandedState(_currentGroupIds, false);
            RefreshRowsView();
        }

        public void CommitEdits()
        {
            if (_surfaceCoordinator.EditState.IsInEditMode)
            {
                var committed = _surfaceCoordinator.CommitEdit();
                if (!committed)
                {
                    RaiseStatusPropertyChanges();
                    return;
                }
            }

            _editSessionContext.CommitPendingChanges();
            RefreshRowsView();
            RaiseStatusPropertyChanges();
        }

        public void CancelEdits()
        {
            if (_surfaceCoordinator.EditState.IsInEditMode)
            {
                _surfaceCoordinator.CancelEdit();
            }

            _editSessionContext.CancelPendingChanges();
            RefreshRowsView();
            RaiseStatusPropertyChanges();
        }

        public bool BeginEditCurrentCell()
        {
            var wasInEditMode = _surfaceCoordinator.EditState.IsInEditMode;
            var universalArgs = WpfUniversalInputAdapter.CreateCommandEventArgs(GridUniversalCommandIds.BeginEdit, Keyboard.Modifiers);
            _surfaceCoordinator.ProcessInput(_surfaceInputAdapter.CreateEditCommandInput(universalArgs, DateTime.UtcNow));
            RaiseStatusPropertyChanges();
            return !wasInEditMode && _surfaceCoordinator.EditState.IsInEditMode;
        }

        public bool PostCurrentEdit()
        {
            var hadEditing = _surfaceCoordinator.EditState.IsInEditMode || _editSessionContext.IsInEditMode;
            var universalArgs = WpfUniversalInputAdapter.CreateCommandEventArgs(GridUniversalCommandIds.PostEdit, Keyboard.Modifiers);
            _surfaceCoordinator.ProcessInput(_surfaceInputAdapter.CreateEditCommandInput(universalArgs, DateTime.UtcNow));
            RefreshRowsView();
            RaiseStatusPropertyChanges();
            return hadEditing && !_surfaceCoordinator.EditState.IsInEditMode;
        }

        public bool CancelCurrentEdit()
        {
            var hadEditing = _surfaceCoordinator.EditState.IsInEditMode || _editSessionContext.IsInEditMode;
            var universalArgs = WpfUniversalInputAdapter.CreateCommandEventArgs(GridUniversalCommandIds.CancelEdit, Keyboard.Modifiers);
            _surfaceCoordinator.ProcessInput(_surfaceInputAdapter.CreateEditCommandInput(universalArgs, DateTime.UtcNow));
            RefreshRowsView();
            RaiseStatusPropertyChanges();
            return hadEditing && !_surfaceCoordinator.EditState.IsInEditMode;
        }

        public void ClearFilters()
        {
            foreach (var column in _visibleColumns)
            {
                if (!string.Equals(column.FilterText, string.Empty, StringComparison.Ordinal))
                {
                    column.FilterText = string.Empty;
                }
            }
        }

        public void SetHierarchySource(IReadOnlyList<GridHierarchyNode<object>> roots, GridHierarchyController<object> controller, string displayColumnId = "ObjectName")
        {
            _hierarchyRoots = roots ?? Array.Empty<GridHierarchyNode<object>>();
            _hierarchyController = controller;
            _hierarchyPresentationMode = GridHierarchyPresentationMode.Tree;
            _masterDetailHeaderPlacementMode = GridMasterDetailHeaderPlacementMode.Inside;
            _hierarchyDisplayColumnId = string.IsNullOrWhiteSpace(displayColumnId) ? "ObjectName" : displayColumnId;
            _masterDetailDisplayColumnId = "ObjectName";
            _masterDetailHeaderMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _masterDetailHeaderColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _masterDetailDetailColumnIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _masterDetailMasterColumnIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _masterDetailFilterStateByPathId.Clear();
            OnPropertyChanged(nameof(HasHierarchy));
            OnPropertyChanged(nameof(IsFilterRowVisible));
            RefreshRowsView();
        }

        public void SetMasterDetailSource(
            IReadOnlyList<GridHierarchyNode<object>> roots,
            GridHierarchyController<object> controller,
            IEnumerable<string> detailColumnIds,
            string masterDisplayColumnId = "Category",
            string detailDisplayColumnId = "ObjectName",
            GridMasterDetailHeaderPlacementMode detailHeaderPlacementMode = GridMasterDetailHeaderPlacementMode.Inside)
        {
            _hierarchyRoots = roots ?? Array.Empty<GridHierarchyNode<object>>();
            _hierarchyController = controller;
            _hierarchyPresentationMode = GridHierarchyPresentationMode.MasterDetail;
            _masterDetailHeaderPlacementMode = detailHeaderPlacementMode;
            _hierarchyDisplayColumnId = string.IsNullOrWhiteSpace(masterDisplayColumnId) ? "Category" : masterDisplayColumnId;
            _masterDetailDisplayColumnId = string.IsNullOrWhiteSpace(detailDisplayColumnId) ? "ObjectName" : detailDisplayColumnId;

            var detailColumnIdSet = new HashSet<string>(
                detailColumnIds ?? Array.Empty<string>(),
                StringComparer.OrdinalIgnoreCase);

            _masterDetailHeaderColumns = detailColumnIdSet.Count == 0
                ? new HashSet<string>(_visibleColumns.Select(column => column.ColumnId), StringComparer.OrdinalIgnoreCase)
                : detailColumnIdSet;

            _masterDetailDetailColumnIds = new HashSet<string>(_masterDetailHeaderColumns, StringComparer.OrdinalIgnoreCase);
            _masterDetailMasterColumnIds = new HashSet<string>(
                (_layoutState?.VisibleColumns ?? Array.Empty<GridColumnDefinition>())
                    .Select(column => column.Id)
                    .Where(columnId => !_masterDetailDetailColumnIds.Contains(columnId)),
                StringComparer.OrdinalIgnoreCase);

            _masterDetailHeaderMap = ResolveMasterDetailColumnDefinitions()
                .ToDictionary(column => column.Id, column => column.Header, StringComparer.OrdinalIgnoreCase);
            if (_masterDetailHeaderMap.Count == 0)
            {
                _masterDetailHeaderMap = _visibleColumns
                    .Where(column => _masterDetailHeaderColumns.Contains(column.ColumnId))
                    .ToDictionary(column => column.ColumnId, column => column.Header, StringComparer.OrdinalIgnoreCase);
            }

            _masterDetailFilterStateByPathId.Clear();
            OnPropertyChanged(nameof(HasHierarchy));
            OnPropertyChanged(nameof(IsFilterRowVisible));
            RebuildColumnBindings();
            RefreshRowsView();
            RaiseViewStateChanged();
        }

        public void ApplyGlobalSearch(string searchText)
        {
            var normalized = string.IsNullOrWhiteSpace(searchText) ? string.Empty : searchText.Trim();
            if (string.Equals(_globalSearchText, normalized, StringComparison.Ordinal))
            {
                return;
            }

            _globalSearchText = normalized;
            OnPropertyChanged(nameof(GlobalSearchText));
            RefreshRowsView();
            RaiseViewStateChanged();
        }

        public void ClearGlobalSearch()
        {
            ApplyGlobalSearch(string.Empty);
        }

        public void ClearHierarchySource()
        {
            _hierarchyRoots = Array.Empty<GridHierarchyNode<object>>();
            _hierarchyController = null;
            _hierarchyPresentationMode = GridHierarchyPresentationMode.Tree;
            _masterDetailHeaderPlacementMode = GridMasterDetailHeaderPlacementMode.Inside;
            _masterDetailHeaderMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _masterDetailHeaderColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _masterDetailDetailColumnIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _masterDetailMasterColumnIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _masterDetailFilterStateByPathId.Clear();
            _masterDetailDisplayColumnId = "ObjectName";
            OnPropertyChanged(nameof(HasHierarchy));
            OnPropertyChanged(nameof(IsFilterRowVisible));
            RebuildColumnBindings();
            RefreshRowsView();
        }

        public async Task ExpandAllHierarchyAsync()
        {
            if (!HasHierarchy)
            {
                return;
            }

            foreach (var root in _hierarchyRoots)
            {
                await _hierarchyController.ExpandAsync(root).ConfigureAwait(true);
            }

            RefreshRowsView();
        }

        public async Task ExpandHierarchyNodeAsync(string pathId)
        {
            if (!HasHierarchy || string.IsNullOrWhiteSpace(pathId))
            {
                return;
            }

            var node = FindHierarchyNode(pathId, _hierarchyRoots);
            if (node == null)
            {
                return;
            }

            await _hierarchyController.ExpandAsync(node).ConfigureAwait(true);
            RefreshRowsView();
        }

        public void CollapseAllHierarchy()
        {
            if (!HasHierarchy)
            {
                return;
            }

            foreach (var root in _hierarchyRoots)
            {
                CollapseHierarchyBranch(root);
            }

            RefreshRowsView();
        }

        public async Task LoadNextHierarchyChildrenPageAsync(string pathId)
        {
            if (!HasHierarchy || string.IsNullOrWhiteSpace(pathId))
            {
                return;
            }

            var node = FindHierarchyNode(pathId, _hierarchyRoots);
            if (node == null)
            {
                return;
            }

            await _hierarchyController.LoadNextChildrenPageAsync(node).ConfigureAwait(true);
            RefreshRowsView();
        }

        private void HandleLoaded(object sender, RoutedEventArgs e)
        {
            LogDiagnostics($"HandleLoaded started. IsVisible={IsVisible}. {GetGridSessionDescription()}.");
            if (!_systemThemeSubscriptionActive)
            {
                SystemParameters.StaticPropertyChanged += HandleSystemParametersChanged;
                _systemThemeSubscriptionActive = true;
            }

            ApplyThemeResources();
            ApplyInteractionConfiguration();
            RebuildColumnBindings(refreshRows: false);
            RefreshRowsView();
            RaiseSurfaceGeometryPropertyChanges();
            LogDiagnostics("HandleLoaded finished.");
        }

        private void HandleUnloaded(object sender, RoutedEventArgs e)
        {
            if (_systemThemeSubscriptionActive)
            {
                SystemParameters.StaticPropertyChanged -= HandleSystemParametersChanged;
                _systemThemeSubscriptionActive = false;
            }

        }

        private void HandleIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!(e.NewValue is bool isVisible) || !isVisible)
            {
                return;
            }

            LogDiagnostics($"HandleIsVisibleChanged entered visible state. PendingRebuild={_pendingRebuildColumnBindingsWhileHidden}, PendingRefresh={_pendingRefreshRowsViewWhileHidden}, PendingIndicators={_pendingRefreshSurfaceRowIndicatorsWhileHidden}. {GetGridSessionDescription()}.");

            SchedulePendingVisibleStateFlush();
        }

        private static void HandleItemsSourceChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
        {
            ((PhialeGrid)dependencyObject).HandleItemsSourceChanged(args.OldValue as IEnumerable, args.NewValue as IEnumerable);
        }

        private static void HandleColumnsChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
        {
            ((PhialeGrid)dependencyObject).HandleColumnsChanged(args.NewValue as IEnumerable<GridColumnDefinition>);
        }

        private static void HandleEditSessionContextChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
        {
            ((PhialeGrid)dependencyObject).HandleEditSessionContextChanged(args.OldValue as IEditSessionContext, args.NewValue as IEditSessionContext);
        }

        private static void HandleGroupsChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
        {
            var grid = (PhialeGrid)dependencyObject;
            if (!grid._isSyncingGroups)
            {
                grid.SetGroups(args.NewValue as IEnumerable<GridGroupDescriptor>);
            }
        }

        private static void HandleRegionChromeChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
        {
            ((PhialeGrid)dependencyObject).ApplyRegionLayout();
        }

        private static void HandleCapabilityPolicyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
        {
            var grid = (PhialeGrid)dependencyObject;
            grid._surfaceCoordinator.SetRegionCapabilityPolicy(args.NewValue as IGridCapabilityPolicy ?? GridAllowAllCapabilityPolicy.Instance);
            grid.ApplyRegionLayout();
        }

        private static void HandleLocalizationChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
        {
            var grid = (PhialeGrid)dependencyObject;
            grid.ReloadLocalization();
            grid.RefreshLocalizedState();
        }

        private static void HandleThemeChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
        {
            ((PhialeGrid)dependencyObject).ApplyThemeResources();
        }

        private static void HandleInteractionConfigurationChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
        {
            ((PhialeGrid)dependencyObject).ApplyInteractionConfiguration();
        }

        private static void HandleRowIndicatorConfigurationChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
        {
            var grid = (PhialeGrid)dependencyObject;
            grid.ApplyResolvedRowHeaderMetrics();
            grid.OnPropertyChanged(nameof(ResolvedRowStateWidth));
            grid.OnPropertyChanged(nameof(ResolvedRowNumbersWidth));
            grid.OnPropertyChanged(nameof(ResolvedRowIndicatorWidth));
            grid.OnPropertyChanged(nameof(ResolvedRowMarkerWidth));
            grid.OnPropertyChanged(nameof(ResolvedSelectionCheckboxWidth));
            grid.OnPropertyChanged(nameof(ResolvedRowHeaderWidth));
            grid.OnPropertyChanged(nameof(IsRowNumberingGlobal));
            grid.OnPropertyChanged(nameof(IsRowNumberingWithinGroup));
            grid._surfaceCoordinator.SelectCurrentRow = grid.SelectCurrentRow;
            grid._surfaceCoordinator.MultiSelect = grid.MultiSelect;
            grid._surfaceCoordinator.ShowRowNumbers = grid.ShowNb;
            grid._surfaceCoordinator.RowNumberingMode = grid.RowNumberingMode;
            if (grid.IsLoaded)
            {
                grid.RefreshRowsView();
            }
        }

        private static void HandleSelectionConfigurationChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
        {
            var grid = (PhialeGrid)dependencyObject;
            grid.OnPropertyChanged(nameof(ResolvedSelectionMode));
            grid._surfaceCoordinator.EnableCellSelection = grid.EnableCellSelection;
            grid._surfaceCoordinator.EnableRangeSelection = grid.EnableRangeSelection && grid.EnableCellSelection;
            grid._surfaceCoordinator.SelectionMode = grid.ResolveSurfaceSelectionMode();
            if (grid.IsLoaded)
            {
                grid.RefreshSelectionCounters();
            }
        }

        private void ApplyThemeResources()
        {
            var themeDictionary = EnsureThemeTokenDictionary();
            var themeUri = ResolveThemeTokenDictionaryUri();
            if (themeDictionary.Source == null || !Uri.Equals(themeDictionary.Source, themeUri))
            {
                themeDictionary.Source = themeUri;
            }
        }

        private void ApplyInteractionConfiguration()
        {
            var resolvedInteractionMode = GridInteractionConfiguration.ResolveInteractionMode(InteractionMode, _autoInteractionMode);
            var densityMetrics = GridInteractionConfiguration.ResolveDensityMetrics(Density);

            var interactionChanged = _resolvedInteractionMode != resolvedInteractionMode;
            var densityChanged = !_densityMetrics.Equals(densityMetrics);

            _resolvedInteractionMode = resolvedInteractionMode;
            _densityMetrics = densityMetrics;

            Resources["PgGridCellPadding"] = densityMetrics.CellPadding;
            Resources["PgGridHeaderPadding"] = densityMetrics.HeaderPadding;
            Resources["PgGridFilterTextPadding"] = densityMetrics.FilterTextPadding;
            Resources["PgGridFilterClearButtonSize"] = densityMetrics.FilterClearButtonSize;
            Resources["PgGridHeaderMenuButtonSize"] = densityMetrics.HeaderMenuButtonSize;
            ApplyResolvedRowHeaderMetrics();

            if (interactionChanged || densityChanged)
            {
                OnPropertyChanged(nameof(ResolvedInteractionMode));
                OnPropertyChanged(nameof(IsTouchInteractionMode));
                OnPropertyChanged(nameof(ResolvedDensity));
                OnPropertyChanged(nameof(ResolvedRowHeight));
                OnPropertyChanged(nameof(ResolvedDetailRowHeight));
                OnPropertyChanged(nameof(ResolvedRowHeaderWidth));
                OnPropertyChanged(nameof(ResolvedRowActionWidth));
                OnPropertyChanged(nameof(ResolvedRowStateWidth));
                OnPropertyChanged(nameof(ResolvedRowNumbersWidth));
                OnPropertyChanged(nameof(ResolvedRowIndicatorWidth));
                OnPropertyChanged(nameof(ResolvedRowMarkerWidth));
                OnPropertyChanged(nameof(ResolvedSelectionCheckboxWidth));
                OnPropertyChanged(nameof(ResolvedSelectionMode));
                OnPropertyChanged(nameof(AllowsPointerColumnReorder));
                OnPropertyChanged(nameof(AllowsPointerColumnResize));
                OnPropertyChanged(nameof(AllowsGroupingDrag));
                OnPropertyChanged(nameof(ShowsColumnMenuAffordance));
                OnPropertyChanged(nameof(ShowsTouchRowSelectors));
            }

            if ((interactionChanged || densityChanged) && IsLoaded)
            {
                RefreshRowsView();
            }

            QueueHeaderVisualRefresh();
        }

        private void HandleEditSessionContextChanged(IEditSessionContext previousContext, IEditSessionContext nextContext)
        {
            if (ReferenceEquals(previousContext, nextContext))
            {
                LogDiagnostics("HandleEditSessionContextChanged skipped because context instance did not change.");
                return;
            }

            if (previousContext != null)
            {
                previousContext.StateChanged -= HandleEditSessionContextStateChanged;
            }
            else
            {
                _internalEditSessionContext.StateChanged -= HandleEditSessionContextStateChanged;
            }

            _editSessionContext = nextContext ?? _internalEditSessionContext;
            _editSessionContext.StateChanged += HandleEditSessionContextStateChanged;
            _surfaceCoordinator.EditSessionContext = _editSessionContext;
            LogDiagnostics($"HandleEditSessionContextChanged applied. UsesExternalContext={UsesExternalEditSessionContext()}, Records={_editSessionContext?.Records?.Count ?? 0}, Fields={_editSessionContext?.FieldDefinitions?.Count ?? 0}.");
            ApplyEditSessionContextBindings();
        }

        private void ApplyEditSessionContextBindings(bool refreshRows = true)
        {
            CaptureEditSessionStructuralSnapshot();
            LogDiagnostics($"ApplyEditSessionContextBindings started. UsesExternalContext={UsesExternalEditSessionContext()}, Records={_editSessionContext?.Records?.Count ?? 0}, Fields={_editSessionContext?.FieldDefinitions?.Count ?? 0}, RefreshRows={refreshRows}.");

            if (UsesExternalEditSessionContext())
            {
                _baselineColumns = BuildColumnsFromEditSessionContext(_editSessionContext);
                _layoutState = _baselineColumns.Count == 0 ? null : new GridLayoutState(_baselineColumns);
                RebuildColumnBindings(refreshRows: false);
                LogDiagnostics($"ApplyEditSessionContextBindings rebuilt columns from external context. BaselineColumns={_baselineColumns.Count}.");
            }

            if (refreshRows && IsLoaded)
            {
                RefreshRowsView();
                LogDiagnostics("ApplyEditSessionContextBindings refreshed rows view because grid is loaded.");
            }
        }

        private void ApplyResolvedRowHeaderMetrics()
        {
            Resources["PgGridRowHeaderWidth"] = ResolvedRowHeaderWidth;
            Resources["PgGridRowDetailsMargin"] = new Thickness(ResolvedRowHeaderWidth, 0d, 0d, 8d);
        }

        private double ResolveAdaptiveRowMarkerWidth()
        {
            if (!ShowNb)
            {
                return 0d;
            }

            var maxNumber = ResolveMaxRowNumberForCurrentSurface();
            return ResolveAdaptiveRowMarkerWidth(maxNumber);
        }

        private double ResolveAdaptiveRowMarkerWidth(int maxNumber)
        {
            if (!ShowNb)
            {
                return 0d;
            }

            var digits = Math.Max(1, maxNumber.ToString(CultureInfo.CurrentCulture).Length);
            return GridInteractionConfiguration.ResolveRowMarkerWidth(_densityMetrics.Density, false, true, digits);
        }

        private int ResolveMaxRowNumberForCurrentSurface()
        {
            var visibleDataRows = RowsView
                .Cast<object>()
                .OfType<GridDisplayRowModel>()
                .Count(IsNumberedDisplayRowModel);
            if (visibleDataRows <= 0 && _totalRowCount > 0)
            {
                visibleDataRows = _totalRowCount;
            }

            if (visibleDataRows <= 0)
            {
                visibleDataRows = _currentFilteredRows?.Count ?? 0;
            }

            if (RowNumberingMode != GridRowNumberingMode.WithinGroup || _groupDescriptors.Count == 0)
            {
                return Math.Max(1, visibleDataRows);
            }

            var largestVisibleGroup = RowsView
                .Cast<object>()
                .OfType<GridGroupHeaderRowModel>()
                .Select(group => group.ItemCount)
                .DefaultIfEmpty(0)
                .Max();
            if (largestVisibleGroup > 0)
            {
                return largestVisibleGroup;
            }

            var topLevelGroupColumnId = _groupDescriptors[0].ColumnId;
            var filteredRows = (_currentFilteredRows ?? Array.Empty<object>()).ToArray();
            if (filteredRows.Length == 0)
            {
                return Math.Max(1, visibleDataRows);
            }

            var maxGroupCount = filteredRows
                .GroupBy(row => Convert.ToString(ResolveRowValue(row, topLevelGroupColumnId), CultureInfo.CurrentCulture) ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.Count())
                .DefaultIfEmpty(visibleDataRows)
                .Max();
            return Math.Max(1, maxGroupCount);
        }

        private static bool IsNumberedDisplayRowModel(GridDisplayRowModel row)
        {
            if (row == null || row.SourceRow == null)
            {
                return false;
            }

            return !row.IsMasterDetailHeader && !row.IsHierarchyLoadMore;
        }

        private void SyncSurfaceRendererSnapshot(IReadOnlyList<object> sourceRows)
        {
            var snapshotCounter = PhialeGridDiagnostics.IncrementGridCounter(GetDiagnosticsGridId(), "SyncSurfaceRendererSnapshot");
            var stopwatch = Stopwatch.StartNew();
            if (_visibleColumns.Count == 0)
            {
                stopwatch.Stop();
                LogDiagnostics($"SyncSurfaceRendererSnapshot skipped because there are no visible columns. Count={snapshotCounter.Count}. {GetGridSessionDescription()}.");
                return;
            }

            var layoutColumns = _visibleColumns
                .Select(column => new global::PhialeGrid.Core.Layout.GridColumnDefinition
                {
                    ColumnKey = column.ColumnId,
                    Header = column.Header,
                    Width = Math.Max(column.MinWidth, column.Width),
                    MinWidth = column.MinWidth,
                    IsVisible = true,
                    IsFrozen = column.Definition.IsFrozen,
                    IsReadOnly = IsGridReadOnly || !CanEditColumn(column),
                    ValueType = column.ValueType,
                    ValueKind = ResolveColumnValueKind(column),
                    EditorKind = column.EditorKind,
                    EditorItems = column.EditorItems,
                    EditorItemsMode = column.EditorItemsMode,
                    EditMask = column.EditMask,
                })
                .ToArray();

            var keyedRows = CreateSurfaceRowsWithCustomDetails(sourceRows ?? Array.Empty<object>());

            _surfaceRowsByKey = keyedRows.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
            _surfaceColumnsByKey = _visibleColumns.ToDictionary(column => column.ColumnId, column => column, StringComparer.OrdinalIgnoreCase);

            var rowDefinitions = keyedRows
                .Select(pair => CreateSurfaceRowDefinition(pair.Key, pair.Value))
                .ToArray();

            using (_surfaceCoordinator.BeginSnapshotUpdateBatch("surface-renderer-snapshot-sync"))
            {
                _surfaceCoordinator.ColumnHeaderHeight = ResolveRequiredDimensionResource("PgGridColumnHeaderHeight");
                _surfaceCoordinator.FilterRowHeight = IsFilterRowVisible
                    ? ResolveRequiredDimensionResource("PgGridFilterRowHeight")
                    : 0d;
                _surfaceCoordinator.RowHeaderWidth = ResolvedRowHeaderWidth;
                _surfaceCoordinator.RowActionWidth = ResolvedRowActionWidth;
                _surfaceCoordinator.RowIndicatorWidth = ResolvedRowIndicatorWidth;
                _surfaceCoordinator.RowMarkerWidth = ResolvedRowMarkerWidth;
                _surfaceCoordinator.SelectionCheckboxWidth = ResolvedSelectionCheckboxWidth;
                _surfaceCoordinator.DataTopInset = 0d;
                _surfaceCoordinator.FrozenColumnCount = layoutColumns.Count(column => column.IsFrozen);
                _surfaceCoordinator.FrozenRowCount = 0;
                _surfaceCoordinator.EnableCellSelection = EnableCellSelection;
                _surfaceCoordinator.EnableRangeSelection = EnableRangeSelection && EnableCellSelection;
                _surfaceCoordinator.SelectionMode = ResolveSurfaceSelectionMode();
                _surfaceCoordinator.ShowCurrentRecordIndicator = ShowCurrentRecordIndicator;
                _surfaceCoordinator.SelectCurrentRow = SelectCurrentRow;
                _surfaceCoordinator.MultiSelect = MultiSelect;
                _surfaceCoordinator.ShowRowNumbers = ShowNb;
                _surfaceCoordinator.RowNumberingMode = RowNumberingMode;
                _surfaceCoordinator.Initialize(layoutColumns, rowDefinitions);
                _surfaceCoordinator.Sorts = BuildEffectiveSorts();
                _surfaceCoordinator.SetStateProjection(_editSessionContext.SurfaceStateProjection);
                var rowIndicatorProjection = BuildRowIndicatorProjection(keyedRows);
                _surfaceCoordinator.SetEditedRows(rowIndicatorProjection.EditedRowKeys);
                _surfaceCoordinator.SetInvalidRows(rowIndicatorProjection.InvalidRowKeys);
                _surfaceCoordinator.SetRowIndicatorToolTips(rowIndicatorProjection.ToolTips);
            }
            stopwatch.Stop();
            LogDiagnostics($"SyncSurfaceRendererSnapshot finished in {stopwatch.ElapsedMilliseconds} ms. Count={snapshotCounter.Count}, SourceRows={sourceRows?.Count ?? 0}, LayoutColumns={layoutColumns.Length}, RowDefinitions={rowDefinitions.Length}. {GetGridSessionDescription()}.");
        }

        private KeyValuePair<string, object>[] CreateSurfaceRowsWithCustomDetails(IReadOnlyList<object> sourceRows)
        {
            var usedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var keyedRows = new List<KeyValuePair<string, object>>();
            if (sourceRows == null || sourceRows.Count == 0)
            {
                return keyedRows.ToArray();
            }

            for (var index = 0; index < sourceRows.Count; index++)
            {
                var row = sourceRows[index];
                var rowKey = CreateSurfaceRowKey(row, index, usedKeys);
                keyedRows.Add(new KeyValuePair<string, object>(rowKey, row));

                if (TryCreateCustomDetailHostRow(rowKey, row, out var detailHost))
                {
                    if (!usedKeys.Add(detailHost.RowKey))
                    {
                        throw new InvalidOperationException("Duplicate row detail key: " + detailHost.RowKey);
                    }

                    keyedRows.Add(new KeyValuePair<string, object>(detailHost.RowKey, detailHost));
                }
            }

            return keyedRows.ToArray();
        }

        private string CreateSurfaceRowKey(object row, int index, ISet<string> usedKeys)
        {
            var baseKey = ResolveSurfaceRowKey(row);
            var candidate = baseKey;
            while (!usedKeys.Add(candidate))
            {
                candidate = baseKey + "#" + index.ToString(CultureInfo.InvariantCulture);
            }

            return candidate;
        }

        private bool TryCreateCustomDetailHostRow(string rowKey, object row, out GridCustomDetailHostRowModel detailHost)
        {
            detailHost = null;
            if (_rowDetailProvider == null)
            {
                return false;
            }

            var record = UnwrapCustomDetailRecord(row);
            if (record == null)
            {
                return false;
            }

            var request = CreateRowDetailRequest(rowKey, record);
            if (!_rowDetailProvider.HasDetail(request))
            {
                return false;
            }

            if (!_rowDetailExpansionState.IsExpanded(rowKey))
            {
                return false;
            }

            var descriptor = _rowDetailProvider.CreateDetail(request);
            if (descriptor == null)
            {
                throw new InvalidOperationException("Row detail provider returned null descriptor for row '" + rowKey + "'.");
            }

            if (!string.Equals(descriptor.OwnerRowKey, rowKey, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Row detail descriptor owner key does not match source row key '" + rowKey + "'.");
            }

            detailHost = new GridCustomDetailHostRowModel(rowKey, descriptor, request.ToContext());
            return true;
        }

        private object UnwrapCustomDetailRecord(object row)
        {
            if (row is GridGroupFlatRow<object> groupedRow)
            {
                return groupedRow.Kind == GridGroupFlatRowKind.DataRow ? groupedRow.Item : null;
            }

            if (row is GridDataRowModel dataRow)
            {
                return dataRow.SourceRow;
            }

            if (row is GridDisplayRowModel)
            {
                return null;
            }

            return row;
        }

        private GridRowDetailRequest CreateRowDetailRequest(string rowKey, object record)
        {
            var values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            var fields = new Dictionary<string, GridRowDetailFieldContext>(StringComparer.OrdinalIgnoreCase);
            foreach (var column in _visibleColumns)
            {
                values[column.ColumnId] = ResolveRowValue(record, column.ColumnId);
                fields[column.ColumnId] = new GridRowDetailFieldContext(
                    column.ColumnId,
                    column.Header,
                    column.ValueType,
                    IsGridReadOnly || !CanEditColumn(column));
            }

            return new GridRowDetailRequest(
                rowKey,
                ResolveRowId(record),
                record,
                values,
                fields);
        }

        private global::PhialeGrid.Core.Layout.GridRowDefinition CreateSurfaceRowDefinition(string rowKey, object row)
        {
            if (row is GridGroupFlatRow<object> groupedRow)
            {
                var hasCustomDetail = groupedRow.Kind == GridGroupFlatRowKind.DataRow &&
                    HasCustomDetailForSurfaceRow(rowKey, row);
                return new global::PhialeGrid.Core.Layout.GridRowDefinition
                {
                    RowKey = rowKey,
                    Height = ResolvedRowHeight,
                    HeaderText = string.Empty,
                    HierarchyLevel = groupedRow.Level,
                    IsHierarchyExpanded = groupedRow.IsExpanded,
                    HasHierarchyChildren = groupedRow.Kind == GridGroupFlatRowKind.GroupHeader,
                    IsGroupHeader = groupedRow.Kind == GridGroupFlatRowKind.GroupHeader,
                    HasDetails = hasCustomDetail,
                    HasDetailsExpanded = hasCustomDetail && _rowDetailExpansionState.IsExpanded(rowKey),
                    IsReadOnly = groupedRow.Kind == GridGroupFlatRowKind.GroupHeader,
                    RepresentsDataRecord = groupedRow.Kind == GridGroupFlatRowKind.DataRow,
                };
            }

            if (row is GridHierarchyNodeRowModel hierarchyRow)
            {
                return new global::PhialeGrid.Core.Layout.GridRowDefinition
                {
                    RowKey = rowKey,
                    Height = ResolvedRowHeight,
                    HeaderText = string.Empty,
                    HierarchyLevel = hierarchyRow.Level,
                    IsHierarchyExpanded = hierarchyRow.Node.IsExpanded,
                    HasHierarchyChildren = hierarchyRow.Node.CanExpand,
                    IsReadOnly = hierarchyRow.Node.CanExpand,
                    RepresentsDataRecord = _hierarchyPresentationMode != GridHierarchyPresentationMode.MasterDetail,
                };
            }

            if (row is GridHierarchyLoadMoreRowModel loadMoreRow)
            {
                return new global::PhialeGrid.Core.Layout.GridRowDefinition
                {
                    RowKey = rowKey,
                    Height = ResolvedRowHeight,
                    HeaderText = string.Empty,
                    HierarchyLevel = loadMoreRow.Level,
                    IsLoadMore = true,
                    IsReadOnly = true,
                    RepresentsDataRecord = false,
                };
            }

            if (row is GridMasterDetailMasterRowModel masterDetailMasterRow)
            {
                return new global::PhialeGrid.Core.Layout.GridRowDefinition
                {
                    RowKey = rowKey,
                    Height = ResolvedRowHeight,
                    HeaderText = string.Empty,
                    HierarchyLevel = masterDetailMasterRow.Level,
                    IsHierarchyExpanded = masterDetailMasterRow.Node.IsExpanded,
                    HasHierarchyChildren = true,
                    IsReadOnly = true,
                    RepresentsDataRecord = false,
                };
            }

            if (row is GridSurfaceMasterDetailDetailsHostRowModel masterDetailDetailsHost)
            {
                return new global::PhialeGrid.Core.Layout.GridRowDefinition
                {
                    RowKey = rowKey,
                    Height = ComputeMasterDetailSurfaceDetailsHeight(masterDetailDetailsHost.MasterRow),
                    HeaderText = string.Empty,
                    HierarchyLevel = masterDetailDetailsHost.MasterRow.Level + 1,
                    HasDetails = true,
                    HasDetailsExpanded = true,
                    IsDetailsHost = true,
                    DetailsPayload = masterDetailDetailsHost.MasterRow,
                    IsReadOnly = true,
                    RepresentsDataRecord = false,
                };
            }

            if (row is GridCustomDetailHostRowModel customDetailHost)
            {
                return new global::PhialeGrid.Core.Layout.GridRowDefinition
                {
                    RowKey = rowKey,
                    Height = customDetailHost.Descriptor.HeightPolicy.Height,
                    HeaderText = string.Empty,
                    HierarchyLevel = 0,
                    HasDetails = true,
                    HasDetailsExpanded = true,
                    IsDetailsHost = true,
                    DetailsPayload = customDetailHost.ToSurfacePayload(),
                    IsReadOnly = true,
                    RepresentsDataRecord = false,
                };
            }

            if (row is GridMasterDetailHeaderRowModel masterDetailHeaderRow)
            {
                return new global::PhialeGrid.Core.Layout.GridRowDefinition
                {
                    RowKey = rowKey,
                    Height = ResolvedRowHeight,
                    HeaderText = string.Empty,
                    HierarchyLevel = masterDetailHeaderRow.Level,
                    IsReadOnly = true,
                    RepresentsDataRecord = false,
                };
            }

            if (row is GridMasterDetailDetailRowModel masterDetailDetailRow)
            {
                return new global::PhialeGrid.Core.Layout.GridRowDefinition
                {
                    RowKey = rowKey,
                    Height = ResolvedRowHeight,
                    HierarchyLevel = masterDetailDetailRow.Level,
                };
            }

            var hasDetail = HasCustomDetailForSurfaceRow(rowKey, row);
            return new global::PhialeGrid.Core.Layout.GridRowDefinition
            {
                RowKey = rowKey,
                Height = ResolvedRowHeight,
                HasDetails = hasDetail,
                HasDetailsExpanded = hasDetail && _rowDetailExpansionState.IsExpanded(rowKey),
            };
        }

        private bool HasCustomDetailForSurfaceRow(string rowKey, object row)
        {
            if (_rowDetailProvider == null)
            {
                return false;
            }

            var record = UnwrapCustomDetailRecord(row);
            if (record == null)
            {
                return false;
            }

            return _rowDetailProvider.HasDetail(CreateRowDetailRequest(rowKey, record));
        }

        private string ResolveSurfaceRowKey(object row)
        {
            if (row is GridGroupFlatRow<object> groupedRow && groupedRow.Kind == GridGroupFlatRowKind.GroupHeader)
            {
                return "group:" + groupedRow.GroupId;
            }

            if (row is GridHierarchyNodeRowModel hierarchyRow)
            {
                return "hierarchy:" + hierarchyRow.Node.PathId;
            }

            if (row is GridHierarchyLoadMoreRowModel loadMoreRow)
            {
                return "hierarchy-loadmore:" + loadMoreRow.ParentNode.PathId;
            }

            if (row is GridMasterDetailMasterRowModel masterDetailMasterRow)
            {
                return "master:" + masterDetailMasterRow.Node.PathId;
            }

            if (row is GridSurfaceMasterDetailDetailsHostRowModel masterDetailDetailsHost)
            {
                return "master-details:" + masterDetailDetailsHost.MasterRow.Node.PathId;
            }

            if (row is GridCustomDetailHostRowModel customDetailHost)
            {
                return customDetailHost.RowKey;
            }

            if (row is GridMasterDetailHeaderRowModel masterDetailHeaderRow)
            {
                return "master-header:" + masterDetailHeaderRow.PathId;
            }

            return ResolveRowId(row);
        }

        private void HandleSurfaceSnapshotChanged(object sender, GridSnapshotChangedEventArgs e)
        {
            var snapshot = e?.Snapshot;
            if (snapshot == null)
            {
                SurfaceColumnHeaderBand?.ClearSnapshot();
                _selectedCellCount = 0;
                _selectedRowCount = 0;
                _currentCellDescription = string.Empty;
                RaiseSurfaceGeometryPropertyChanges();
                RaiseStatusPropertyChanges();
                return;
            }

            UpdateSurfaceSelectionCounters(snapshot);
            _currentCellDescription = BuildSurfaceCurrentCellDescription(snapshot.CurrentCell);
            SurfaceColumnHeaderBand?.RenderSnapshot(snapshot);
            UpdateViewportFromSurfaceSnapshot(snapshot);
            RaiseSurfaceGeometryPropertyChanges();
            RaiseStatusPropertyChanges();
        }

        private void HandleSurfaceHostGeometryChanged(object sender, EventArgs e)
        {
            RaiseSurfaceGeometryPropertyChanges();
        }

        private string BuildSurfaceCurrentCellDescription(GridCurrentCellMarker currentCell)
        {
            if (currentCell == null)
            {
                return _currentCellDescriptionBuilder.BuildDescription(new GridCurrentCellDescriptorRequest
                {
                    Kind = GridCurrentCellDescriptorKind.Empty,
                });
            }

            _surfaceColumnsByKey.TryGetValue(currentCell.ColumnKey, out var column);
            _surfaceRowsByKey.TryGetValue(currentCell.RowKey, out var row);
            if (!IsSelectableSurfaceRow(row))
            {
                return _currentCellDescriptionBuilder.BuildDescription(new GridCurrentCellDescriptorRequest
                {
                    Kind = GridCurrentCellDescriptorKind.Empty,
                });
            }

            var value = ResolveRowValue(row, currentCell.ColumnKey);

            return _currentCellDescriptionBuilder.BuildDescription(new GridCurrentCellDescriptorRequest
            {
                Kind = column == null ? GridCurrentCellDescriptorKind.Empty : GridCurrentCellDescriptorKind.Data,
                Header = column == null ? currentCell.ColumnKey : column.Header,
                Value = value,
                ValueType = column?.ValueType,
            });
        }

        private GridSelectionMode ResolveSurfaceSelectionMode()
        {
            return ResolvedSelectionMode;
        }

        private void UpdateViewportFromSurfaceSnapshot(GridSurfaceSnapshot snapshot)
        {
            var rowHeights = snapshot.ViewportState.Metrics.RowHeights;
            var columnWidths = snapshot.ViewportState.Metrics.ColumnWidths;

            Viewport = new GridViewport(
                snapshot.ViewportState.HorizontalOffset,
                snapshot.ViewportState.VerticalOffset,
                Math.Max(1d, snapshot.ViewportState.ViewportWidth),
                Math.Max(1d, snapshot.ViewportState.ViewportHeight),
                rowHeights != null && rowHeights.Count > 0 ? rowHeights : new[] { ResolvedRowHeight },
                columnWidths != null && columnWidths.Count > 0 ? columnWidths : new[] { 120d },
                snapshot.ViewportState.FrozenColumnCount,
                1);
        }

        private void HandlePreviewTouchInput(object sender, TouchEventArgs e)
        {
            PromoteAutoInteractionMode(GridInputOrigin.Touch);
        }

        private void HandlePreviewStylusInput(object sender, StylusDownEventArgs e)
        {
            PromoteAutoInteractionMode(GridInputOrigin.Stylus);
        }

        private void HandlePreviewMouseInput(object sender, MouseButtonEventArgs e)
        {
            if (InteractionMode != GridInteractionMode.Auto)
            {
                return;
            }

            var currentTimestamp = Stopwatch.GetTimestamp();
            if (_lastTouchInteractionTimestamp != 0L &&
                currentTimestamp - _lastTouchInteractionTimestamp <= AutoTouchPromotionWindowTicks)
            {
                return;
            }

            PromoteAutoInteractionMode(GridInputOrigin.Mouse);
        }

        private void HandlePreviewKeyInput(object sender, KeyEventArgs e)
        {
            PromoteAutoInteractionMode(GridInputOrigin.Keyboard);
        }

        private void PromoteAutoInteractionMode(GridInputOrigin inputOrigin)
        {
            if (InteractionMode != GridInteractionMode.Auto)
            {
                return;
            }

            if (inputOrigin == GridInputOrigin.Touch || inputOrigin == GridInputOrigin.Stylus)
            {
                _lastTouchInteractionTimestamp = Stopwatch.GetTimestamp();
            }

            var nextAutoMode = GridInteractionConfiguration.UpdateAutoInteractionMode(_autoInteractionMode, inputOrigin);
            if (_autoInteractionMode == nextAutoMode)
            {
                return;
            }

            _autoInteractionMode = nextAutoMode;
            ApplyInteractionConfiguration();
        }

        private ResourceDictionary EnsureThemeTokenDictionary()
        {
            if (_themeTokenDictionary != null)
            {
                return _themeTokenDictionary;
            }

            _themeTokenDictionary = Resources.MergedDictionaries
                .FirstOrDefault(dictionary => dictionary.Source != null &&
                    dictionary.Source.OriginalString.IndexOf("ThemeTokens.", StringComparison.OrdinalIgnoreCase) >= 0);

            if (_themeTokenDictionary == null)
            {
                _themeTokenDictionary = new ResourceDictionary { Source = DayThemeTokensUri };
                Resources.MergedDictionaries.Insert(0, _themeTokenDictionary);
            }

            return _themeTokenDictionary;
        }

        private Uri ResolveThemeTokenDictionaryUri()
        {
            if (SystemParameters.HighContrast)
            {
                return HighContrastThemeTokensUri;
            }

            return IsNightMode ? NightThemeTokensUri : DayThemeTokensUri;
        }

        private void HandleSystemParametersChanged(object sender, PropertyChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.PropertyName) || string.Equals(e.PropertyName, nameof(SystemParameters.HighContrast), StringComparison.Ordinal))
            {
                ApplyThemeResources();
            }
        }

        private static void HandleSortsChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
        {
            var grid = (PhialeGrid)dependencyObject;
            if (grid._isSyncingSorts)
            {
                return;
            }

            grid._sortDescriptors = (args.NewValue as IEnumerable<GridSortDescriptor> ?? Array.Empty<GridSortDescriptor>()).ToArray();
            grid.RefreshSortIndicators();
            grid.RefreshRowsView();
        }

        private static void HandleSummariesChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
        {
            var grid = (PhialeGrid)dependencyObject;
            grid._summaryDescriptors = (args.NewValue as IEnumerable<GridSummaryDescriptor> ?? Array.Empty<GridSummaryDescriptor>()).ToArray();
            grid.RefreshRowsView();
        }

        private static void HandleSideToolRegionTitleChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
        {
            var grid = (PhialeGrid)dependencyObject;
            grid.OnPropertyChanged(nameof(ToolsRegionTitleText));
            grid.QueueWorkspacePanelTabOverflowLayout();
        }

        private static void HandleReadOnlyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
        {
            ((PhialeGrid)dependencyObject).RefreshRowsView();
        }

        private static void HandleCurrentRecordIndicatorChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
        {
            var grid = (PhialeGrid)dependencyObject;
            grid._surfaceCoordinator.ShowCurrentRecordIndicator = args.NewValue is bool enabled && enabled;
            if (grid.IsLoaded)
            {
                grid.RefreshRowsView();
            }
        }

        private static void HandleEditActivationModeChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
        {
            var grid = (PhialeGrid)dependencyObject;
            grid._surfaceCoordinator.EditActivationMode = args.NewValue is GridEditActivationMode mode
                ? mode
                : GridEditActivationMode.DirectInteraction;
            grid.RaiseStatusPropertyChanges();
        }

        private void HandleItemsSourceChanged(IEnumerable previousSource, IEnumerable nextSource)
        {
            LogDiagnostics($"HandleItemsSourceChanged invoked. UsesExternalContext={UsesExternalEditSessionContext()}, NextSourceNull={nextSource == null}.");
            if (_observableItemsSource != null)
            {
                _observableItemsSource.CollectionChanged -= HandleObservableItemsChanged;
                _observableItemsSource = null;
            }

            if (nextSource is INotifyCollectionChanged observable)
            {
                _observableItemsSource = observable;
                _observableItemsSource.CollectionChanged += HandleObservableItemsChanged;
            }

            if (!UsesExternalEditSessionContext())
            {
                _editSessionContext.ClearSession();
                _editSessionDataSource.ReplaceFieldDefinitions(BuildEditSessionFieldDefinitions(_baselineColumns));
                _editSessionDataSource.ReplaceSnapshot((nextSource ?? Array.Empty<object>()).Cast<object>());
            }
            RefreshRowsView();
        }

        private void HandleColumnsChanged(IEnumerable<GridColumnDefinition> columns)
        {
            LogDiagnostics("HandleColumnsChanged invoked.");
            _baselineColumns = (columns ?? Array.Empty<GridColumnDefinition>())
                .Where(column => column != null)
                .Select(CloneColumnDefinition)
                .ToArray();

            _layoutState = _baselineColumns.Count == 0 ? null : new GridLayoutState(_baselineColumns);
            if (_summaryDescriptors.Count == 0)
            {
                _summaryDescriptors = (Summaries ?? Array.Empty<GridSummaryDescriptor>()).ToArray();
            }

            if (!UsesExternalEditSessionContext())
            {
                _editSessionDataSource.ReplaceFieldDefinitions(BuildEditSessionFieldDefinitions(_baselineColumns));
            }
            RebuildColumnBindings();
            LogDiagnostics($"HandleColumnsChanged finished. BaselineColumns={_baselineColumns.Count}, UsesExternalContext={UsesExternalEditSessionContext()}.");
        }

        private void HandleObservableItemsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (!UsesExternalEditSessionContext())
            {
                _editSessionDataSource.ReplaceSnapshot((ItemsSource ?? Array.Empty<object>()).Cast<object>());
            }
            RefreshRowsView();
        }

        private void HandleEditSessionContextStateChanged(object sender, EventArgs e)
        {
            if (UsesExternalEditSessionContext())
            {
                var recordsChanged = !ReferenceEquals(_lastEditSessionRecordsReference, _editSessionContext.Records);
                var fieldsChanged = !ReferenceEquals(_lastEditSessionFieldDefinitionsReference, _editSessionContext.FieldDefinitions);
                if (recordsChanged || fieldsChanged)
                {
                    var structuralChange = EvaluateEditSessionStructuralChange();
                    if (structuralChange.HasDataStructureChanged)
                    {
                        LogDiagnostics($"HandleEditSessionContextStateChanged detected data structural content change. RecordsReferenceChanged={recordsChanged}, FieldsReferenceChanged={fieldsChanged}, Reason='{structuralChange.Reason}'.");
                        ApplyEditSessionContextBindings();
                        return;
                    }

                    if (structuralChange.HasColumnPresentationChanged)
                    {
                        LogDiagnostics($"HandleEditSessionContextStateChanged detected edit-session column presentation change without data refresh. RecordsReferenceChanged={recordsChanged}, FieldsReferenceChanged={fieldsChanged}, Reason='{structuralChange.Reason}'.");
                        ApplyEditSessionContextBindings(refreshRows: false);
                        return;
                    }

                    CaptureEditSessionStructuralSnapshot();
                    LogDiagnostics($"HandleEditSessionContextStateChanged skipped structural refresh because external context content is equivalent. RecordsReferenceChanged={recordsChanged}, FieldsReferenceChanged={fieldsChanged}, Reason='{structuralChange.Reason}'.");
                }
            }

            using (_surfaceCoordinator.BeginSnapshotUpdateBatch("edit-session-state-change"))
            {
                _surfaceCoordinator.SetStateProjection(_editSessionContext.SurfaceStateProjection);
                RefreshSurfaceRowIndicators();
            }
            RaiseStatusPropertyChanges();
        }

        private IReadOnlyList<IEditSessionFieldDefinition> BuildEditSessionFieldDefinitions(IEnumerable<GridColumnDefinition> columns)
        {
            var definitions = ObjectEditSessionFieldDefinitionFactory.CreateFromGridColumns(columns ?? Array.Empty<GridColumnDefinition>());
            if (definitions.Count > 0)
            {
                return definitions;
            }

            return ObjectEditSessionFieldDefinitionFactory.CreateFromRecords(EnumerateEffectiveItemsSource());
        }

        private bool UsesExternalEditSessionContext()
        {
            return !ReferenceEquals(_editSessionContext, _internalEditSessionContext);
        }

        private void CaptureEditSessionStructuralSnapshot()
        {
            var records = _editSessionContext?.Records ?? Array.Empty<object>();
            var fields = _editSessionContext?.FieldDefinitions ?? Array.Empty<IEditSessionFieldDefinition>();
            _lastEditSessionRecordsReference = records;
            _lastEditSessionFieldDefinitionsReference = fields;
            _lastEditSessionRecordsSnapshot = records.ToArray();
            _lastEditSessionFieldDefinitionsSnapshot = fields.ToArray();
        }

        private EditSessionStructuralChangeEvaluation EvaluateEditSessionStructuralChange()
        {
            var currentRecords = _editSessionContext?.Records ?? Array.Empty<object>();
            var currentFields = _editSessionContext?.FieldDefinitions ?? Array.Empty<IEditSessionFieldDefinition>();
            if (!AreRecordSequencesStructurallyEquivalent(_lastEditSessionRecordsSnapshot, currentRecords))
            {
                return EditSessionStructuralChangeEvaluation.DataStructureChanged("record sequence changed");
            }

            var fieldComparison = CompareEditSessionFieldDefinitions(_lastEditSessionFieldDefinitionsSnapshot, currentFields);
            if (!fieldComparison.AreDataSchemasEquivalent)
            {
                return EditSessionStructuralChangeEvaluation.DataStructureChanged(fieldComparison.Reason);
            }

            if (!fieldComparison.AreColumnPresentationsEquivalent)
            {
                return EditSessionStructuralChangeEvaluation.ColumnPresentationChanged(fieldComparison.Reason);
            }

            return EditSessionStructuralChangeEvaluation.Equivalent(fieldComparison.Reason);
        }

        private bool AreRecordSequencesStructurallyEquivalent(IReadOnlyList<object> left, IReadOnlyList<object> right)
        {
            if ((left?.Count ?? 0) != (right?.Count ?? 0))
            {
                return false;
            }

            for (var index = 0; index < (left?.Count ?? 0); index++)
            {
                var leftRecord = left[index];
                var rightRecord = right[index];
                if (ReferenceEquals(leftRecord, rightRecord))
                {
                    continue;
                }

                var leftRowId = ResolveRowId(leftRecord);
                var rightRowId = ResolveRowId(rightRecord);
                if (string.IsNullOrWhiteSpace(leftRowId) ||
                    string.IsNullOrWhiteSpace(rightRowId) ||
                    !string.Equals(leftRowId, rightRowId, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        private static EditSessionFieldDefinitionComparison CompareEditSessionFieldDefinitions(IReadOnlyList<IEditSessionFieldDefinition> left, IReadOnlyList<IEditSessionFieldDefinition> right)
        {
            if ((left?.Count ?? 0) != (right?.Count ?? 0))
            {
                return EditSessionFieldDefinitionComparison.DataSchemaChanged("field count changed");
            }

            for (var index = 0; index < (left?.Count ?? 0); index++)
            {
                var comparison = CompareEditSessionFieldDefinition(left[index], right[index]);
                if (!comparison.AreDataSchemasEquivalent || !comparison.AreColumnPresentationsEquivalent)
                {
                    return comparison.WithReason("field[" + index.ToString(CultureInfo.InvariantCulture) + "]: " + comparison.Reason);
                }
            }

            return EditSessionFieldDefinitionComparison.Equivalent("field definitions equivalent");
        }

        private static EditSessionFieldDefinitionComparison CompareEditSessionFieldDefinition(IEditSessionFieldDefinition left, IEditSessionFieldDefinition right)
        {
            if (left == null || right == null)
            {
                return left == right
                    ? EditSessionFieldDefinitionComparison.Equivalent("both fields null")
                    : EditSessionFieldDefinitionComparison.DataSchemaChanged("one field is null");
            }

            if (!string.Equals(left.FieldId, right.FieldId, StringComparison.Ordinal))
            {
                return EditSessionFieldDefinitionComparison.DataSchemaChanged("field id changed");
            }

            if (!string.Equals(left.FieldPath, right.FieldPath, StringComparison.Ordinal))
            {
                return EditSessionFieldDefinitionComparison.DataSchemaChanged("field path changed");
            }

            if (left.ValueType != right.ValueType)
            {
                return EditSessionFieldDefinitionComparison.DataSchemaChanged("value type changed");
            }

            if (!string.Equals(left.ValueKind, right.ValueKind, StringComparison.Ordinal) ||
                left.EditorKind != right.EditorKind ||
                left.EditorItemsMode != right.EditorItemsMode ||
                !string.Equals(left.EditMask, right.EditMask, StringComparison.Ordinal) ||
                !left.EditorItems.SequenceEqual(right.EditorItems, StringComparer.Ordinal))
            {
                return EditSessionFieldDefinitionComparison.DataSchemaChanged("editor schema changed");
            }

            if (left.IsWeakAttribute != right.IsWeakAttribute)
            {
                return EditSessionFieldDefinitionComparison.DataSchemaChanged("weak attribute semantics changed");
            }

            if (!AreValidationConstraintsEquivalent(left.ValidationConstraints, right.ValidationConstraints))
            {
                return EditSessionFieldDefinitionComparison.DataSchemaChanged("validation constraints changed");
            }

            if (!AreFieldGridColumnSchemasEquivalent(left.GridColumnDefinition, right.GridColumnDefinition))
            {
                return EditSessionFieldDefinitionComparison.DataSchemaChanged("grid column schema changed");
            }

            if (!string.Equals(left.DisplayName, right.DisplayName, StringComparison.Ordinal) ||
                left.IsVisibleInGrid != right.IsVisibleInGrid ||
                left.IsVisibleInExpandedDetails != right.IsVisibleInExpandedDetails ||
                !AreColumnsEquivalent(left.GridColumnDefinition, right.GridColumnDefinition))
            {
                return EditSessionFieldDefinitionComparison.ColumnPresentationChanged("grid column presentation changed");
            }

            return EditSessionFieldDefinitionComparison.Equivalent("field equivalent");
        }

        private static bool AreFieldGridColumnSchemasEquivalent(GridColumnDefinition left, GridColumnDefinition right)
        {
            if (left == null || right == null)
            {
                return left == right;
            }

            return string.Equals(left.Id, right.Id, StringComparison.Ordinal) &&
                left.ValueType == right.ValueType &&
                left.EditorKind == right.EditorKind &&
                left.EditorItemsMode == right.EditorItemsMode &&
                string.Equals(left.EditMask, right.EditMask, StringComparison.Ordinal) &&
                string.Equals(left.ValueKind, right.ValueKind, StringComparison.Ordinal) &&
                left.EditorItems.SequenceEqual(right.EditorItems, StringComparer.Ordinal);
        }

        private static bool AreValidationConstraintsEquivalent(GridFieldValidationConstraints left, GridFieldValidationConstraints right)
        {
            if (left == null || right == null)
            {
                return left == right;
            }

            return left.Equals(right);
        }

        private struct EditSessionStructuralChangeEvaluation
        {
            private EditSessionStructuralChangeEvaluation(bool hasDataStructureChanged, bool hasColumnPresentationChanged, string reason)
            {
                HasDataStructureChanged = hasDataStructureChanged;
                HasColumnPresentationChanged = hasColumnPresentationChanged;
                Reason = reason ?? string.Empty;
            }

            public bool HasDataStructureChanged { get; }

            public bool HasColumnPresentationChanged { get; }

            public string Reason { get; }

            public static EditSessionStructuralChangeEvaluation DataStructureChanged(string reason)
            {
                return new EditSessionStructuralChangeEvaluation(true, false, reason);
            }

            public static EditSessionStructuralChangeEvaluation ColumnPresentationChanged(string reason)
            {
                return new EditSessionStructuralChangeEvaluation(false, true, reason);
            }

            public static EditSessionStructuralChangeEvaluation Equivalent(string reason)
            {
                return new EditSessionStructuralChangeEvaluation(false, false, reason);
            }
        }

        private struct EditSessionFieldDefinitionComparison
        {
            private EditSessionFieldDefinitionComparison(bool areDataSchemasEquivalent, bool areColumnPresentationsEquivalent, string reason)
            {
                AreDataSchemasEquivalent = areDataSchemasEquivalent;
                AreColumnPresentationsEquivalent = areColumnPresentationsEquivalent;
                Reason = reason ?? string.Empty;
            }

            public bool AreDataSchemasEquivalent { get; }

            public bool AreColumnPresentationsEquivalent { get; }

            public string Reason { get; }

            public EditSessionFieldDefinitionComparison WithReason(string reason)
            {
                return new EditSessionFieldDefinitionComparison(AreDataSchemasEquivalent, AreColumnPresentationsEquivalent, reason);
            }

            public static EditSessionFieldDefinitionComparison DataSchemaChanged(string reason)
            {
                return new EditSessionFieldDefinitionComparison(false, false, reason);
            }

            public static EditSessionFieldDefinitionComparison ColumnPresentationChanged(string reason)
            {
                return new EditSessionFieldDefinitionComparison(true, false, reason);
            }

            public static EditSessionFieldDefinitionComparison Equivalent(string reason)
            {
                return new EditSessionFieldDefinitionComparison(true, true, reason);
            }
        }

        private IEditSessionContext CurrentEditSessionContext => _editSessionContext ?? _internalEditSessionContext;

        private IEnumerable<object> EnumerateEffectiveItemsSource()
        {
            if (UsesExternalEditSessionContext())
            {
                return _editSessionContext?.Records ?? Array.Empty<object>();
            }

            return (ItemsSource ?? Array.Empty<object>()).Cast<object>();
        }

        private IEditSessionFieldDefinition ResolveEditSessionFieldDefinition(string fieldId)
        {
            return string.IsNullOrWhiteSpace(fieldId)
                ? null
                : (_editSessionContext?.FieldDefinitions ?? Array.Empty<IEditSessionFieldDefinition>())
                    .FirstOrDefault(field => string.Equals(field.FieldId, fieldId, StringComparison.OrdinalIgnoreCase));
        }

        private static IReadOnlyList<GridColumnDefinition> BuildColumnsFromEditSessionContext(IEditSessionContext context)
        {
            if (context == null)
            {
                return Array.Empty<GridColumnDefinition>();
            }

            return context.FieldDefinitions
                .Where(field => field != null)
                .Select((field, index) =>
                {
                    if (field.GridColumnDefinition != null)
                    {
                        return CloneColumnDefinition(field.GridColumnDefinition).WithValidationConstraints(null);
                    }

                    return new GridColumnDefinition(
                        field.FieldId,
                        field.DisplayName,
                        width: 140d,
                        minWidth: 30d,
                        isVisible: field.IsVisibleInGrid,
                        isFrozen: false,
                        isEditable: true,
                        displayIndex: index,
                        valueType: field.ValueType,
                        editorKind: field.EditorKind,
                        editorItems: field.EditorItems,
                        editorItemsMode: field.EditorItemsMode,
                        editMask: field.EditMask,
                        valueKind: field.ValueKind,
                        validationConstraints: null);
                })
                .ToArray();
        }

        private void HandleSurfaceViewportScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            SyncFilterScrollOffset();
            EnsureViewportWindow();
            RaiseSurfaceGeometryPropertyChanges();
        }

        private void HandleFilterScrollViewerScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (_isSyncingFilterScroll || SurfaceHost == null)
            {
                return;
            }

            if (Math.Abs(SurfaceHost.HorizontalOffset - e.HorizontalOffset) < 0.1d)
            {
                return;
            }

            var universalArgs = WpfUniversalInputAdapter.CreateScrollChangedEventArgs(e.HorizontalOffset, SurfaceHost.VerticalOffset);
            _surfaceCoordinator.ProcessInput(_surfaceInputAdapter.CreateScrollChangedInput(universalArgs, DateTime.UtcNow));
        }

        private void ReloadLocalization()
        {
            LocalizationCatalog = LoadCatalog(LanguageDirectory);
        }

        private void RefreshLocalizedState()
        {
            OnPropertyChanged(nameof(GroupingBandLabelText));
            OnPropertyChanged(nameof(GroupingDropText));
            OnPropertyChanged(nameof(GroupingEmptyText));
            OnPropertyChanged(nameof(GroupingExpandAllText));
            OnPropertyChanged(nameof(GroupingCollapseAllText));
            OnPropertyChanged(nameof(FilterLabelText));
            OnPropertyChanged(nameof(FilterEditText));
            OnPropertyChanged(nameof(ColumnMenuButtonText));
            OnPropertyChanged(nameof(SortAscendingText));
            OnPropertyChanged(nameof(SortDescendingText));
            OnPropertyChanged(nameof(SortAddAscendingText));
            OnPropertyChanged(nameof(SortAddDescendingText));
            OnPropertyChanged(nameof(SortClearText));
            OnPropertyChanged(nameof(GroupByColumnText));
            OnPropertyChanged(nameof(RemoveFromGroupingText));
            OnPropertyChanged(nameof(ToggleColumnGroupingDirectionText));
            OnPropertyChanged(nameof(FreezeColumnText));
            OnPropertyChanged(nameof(UnfreezeColumnText));
            OnPropertyChanged(nameof(HideColumnText));
            OnPropertyChanged(nameof(MoveColumnLeftText));
            OnPropertyChanged(nameof(MoveColumnRightText));
            OnPropertyChanged(nameof(WidenColumnText));
            OnPropertyChanged(nameof(NarrowColumnText));
            OnPropertyChanged(nameof(AutoFitColumnText));
            OnPropertyChanged(nameof(HierarchyLoadMoreText));
            OnPropertyChanged(nameof(EmptyNoRowsText));
            OnPropertyChanged(nameof(PendingEditBannerText));
            RaiseStatusPropertyChanges();
            RebuildGroupChips();
            RefreshSortIndicators();
            UpdateSummaryItemsFromCurrentRows();
            if (HasHierarchy)
            {
                RefreshRowsView();
            }
        }

        private bool ShouldDeferVisualWorkBecauseHidden()
        {
            return IsLoaded && !IsVisible;
        }

        private bool ShouldDeferVisualWorkBecauseBatched()
        {
            return _gridUpdateBatchDepth > 0;
        }

        private void CompleteGridUpdateBatch(string reason)
        {
            if (_gridUpdateBatchDepth <= 0)
            {
                throw new InvalidOperationException("Grid update batch completion was requested without a matching active batch.");
            }

            _gridUpdateBatchDepth--;
            LogDiagnostics("Grid update batch completed. Reason='" + (reason ?? string.Empty) + "', RemainingDepth=" + _gridUpdateBatchDepth + ".");
            if (_gridUpdateBatchDepth != 0)
            {
                return;
            }

            FlushPendingGridUpdateBatchWork(reason);
        }

        private void FlushPendingGridUpdateBatchWork(string reason)
        {
            var rebuildColumns = _pendingRebuildColumnBindingsAfterBatch;
            var refreshRows = _pendingRefreshRowsViewAfterBatch;
            var refreshIndicators = _pendingRefreshSurfaceRowIndicatorsAfterBatch;
            var applyRegionLayout = _pendingApplyRegionLayoutAfterBatch;
            var raiseViewStateChanged = _pendingViewStateChangedAfterBatch;

            _pendingRebuildColumnBindingsAfterBatch = false;
            _pendingRefreshRowsViewAfterBatch = false;
            _pendingRefreshSurfaceRowIndicatorsAfterBatch = false;
            _pendingApplyRegionLayoutAfterBatch = false;
            _pendingViewStateChangedAfterBatch = false;
            var pendingCurrentCellRowKey = _pendingCurrentCellRowKeyAfterBatch;
            var pendingCurrentCellColumnKey = _pendingCurrentCellColumnKeyAfterBatch;
            _pendingCurrentCellRowKeyAfterBatch = null;
            _pendingCurrentCellColumnKeyAfterBatch = null;

            ConsumePendingHiddenVisualWorkIfVisible(ref rebuildColumns, ref refreshRows, ref refreshIndicators);

            LogDiagnostics(
                "Grid update batch flush started. Reason='" + (reason ?? string.Empty) +
                "', RebuildColumns=" + rebuildColumns +
                ", RefreshRows=" + refreshRows +
                ", RefreshIndicators=" + refreshIndicators +
                ", ApplyRegionLayout=" + applyRegionLayout +
                ", ViewStateChanged=" + raiseViewStateChanged + ".");

            if (rebuildColumns)
            {
                RebuildColumnBindings(refreshRows);
            }
            else if (refreshRows)
            {
                RefreshRowsView();
            }

            if (!string.IsNullOrWhiteSpace(pendingCurrentCellRowKey))
            {
                _surfaceCoordinator.SetCurrentCell(pendingCurrentCellRowKey, pendingCurrentCellColumnKey);
            }

            if (refreshIndicators && !refreshRows)
            {
                RefreshSurfaceRowIndicators();
            }

            if (applyRegionLayout)
            {
                ApplyRegionLayout();
            }

            if (raiseViewStateChanged)
            {
                RaiseViewStateChangedCore();
            }

            LogDiagnostics("Grid update batch flush completed. Reason='" + (reason ?? string.Empty) + "'.");
        }

        private void SchedulePendingVisibleStateFlush()
        {
            if (_pendingVisibleStateFlushScheduled ||
                (!_pendingRebuildColumnBindingsWhileHidden &&
                 !_pendingRefreshRowsViewWhileHidden &&
                 !_pendingRefreshSurfaceRowIndicatorsWhileHidden))
            {
                return;
            }

            _pendingVisibleStateFlushScheduled = true;
            Dispatcher.BeginInvoke(
                new Action(() =>
                {
                    _pendingVisibleStateFlushScheduled = false;
                    FlushPendingVisibleStateWork();
                }),
                DispatcherPriority.ContextIdle);
            LogDiagnostics("Pending visible-state flush scheduled.");
        }

        private void FlushPendingVisibleStateWork()
        {
            if (!IsVisible)
            {
                LogDiagnostics("Pending visible-state flush skipped because grid is not visible.");
                return;
            }

            var rebuildColumns = false;
            var refreshRows = false;
            var refreshIndicators = false;
            ConsumePendingHiddenVisualWorkIfVisible(ref rebuildColumns, ref refreshRows, ref refreshIndicators);

            LogDiagnostics(
                "Pending visible-state flush started. RebuildColumns=" + rebuildColumns +
                ", RefreshRows=" + refreshRows +
                ", RefreshIndicators=" + refreshIndicators + ".");

            if (rebuildColumns)
            {
                RebuildColumnBindings(refreshRows);
            }
            else if (refreshRows)
            {
                RefreshRowsView();
            }

            if (refreshIndicators && !refreshRows)
            {
                RefreshSurfaceRowIndicators();
            }

            LogDiagnostics("Pending visible-state flush completed.");
        }

        private void ConsumePendingHiddenVisualWorkIfVisible(ref bool rebuildColumns, ref bool refreshRows, ref bool refreshIndicators)
        {
            if (!IsVisible)
            {
                return;
            }

            var consumedRebuildColumns = _pendingRebuildColumnBindingsWhileHidden;
            var consumedRefreshRows = _pendingRefreshRowsViewWhileHidden;
            var consumedRefreshIndicators = _pendingRefreshSurfaceRowIndicatorsWhileHidden;

            if (consumedRebuildColumns)
            {
                rebuildColumns = true;
            }

            if (consumedRefreshRows)
            {
                refreshRows = true;
            }

            if (consumedRefreshIndicators)
            {
                refreshIndicators = true;
            }

            if (consumedRebuildColumns || consumedRefreshRows || consumedRefreshIndicators)
            {
                LogDiagnostics(
                    "Consumed hidden visual work. RebuildColumns=" + consumedRebuildColumns +
                    ", RefreshRows=" + consumedRefreshRows +
                    ", RefreshIndicators=" + consumedRefreshIndicators + ".");
            }

            _pendingRebuildColumnBindingsWhileHidden = false;
            _pendingRefreshRowsViewWhileHidden = false;
            _pendingRefreshSurfaceRowIndicatorsWhileHidden = false;
        }

        private void RebuildColumnBindings(bool refreshRows = true)
        {
            var gridId = GetDiagnosticsGridId();
            var rebuildCounter = PhialeGridDiagnostics.IncrementGridCounter(gridId, "RebuildColumnBindings");
            var stopwatch = Stopwatch.StartNew();
            if (ShouldDeferVisualWorkBecauseBatched())
            {
                _pendingRebuildColumnBindingsAfterBatch = true;
                if (refreshRows)
                {
                    _pendingRefreshRowsViewAfterBatch = true;
                }

                stopwatch.Stop();
                LogDiagnostics($"RebuildColumnBindings deferred because grid update batch is active. Count={rebuildCounter.Count}, RefreshRows={refreshRows}. {GetGridSessionDescription()}.");
                return;
            }

            if (ShouldDeferVisualWorkBecauseHidden())
            {
                _pendingRebuildColumnBindingsWhileHidden = true;
                if (refreshRows)
                {
                    _pendingRefreshRowsViewWhileHidden = true;
                }

                stopwatch.Stop();
                LogDiagnostics($"RebuildColumnBindings deferred because grid is hidden. Count={rebuildCounter.Count}, RefreshRows={refreshRows}. {GetGridSessionDescription()}.");
                return;
            }

            foreach (var column in _visibleColumns)
            {
                column.PropertyChanged -= HandleColumnFilterChanged;
            }

            _visibleColumns = new ObservableCollection<GridColumnBindingModel>(
                GetDisplayedColumns()
                    .OrderBy(column => column.DisplayIndex)
                    .Select(column => new GridColumnBindingModel(column)));

            foreach (var column in _visibleColumns)
            {
                column.PropertyChanged += HandleColumnFilterChanged;
            }

            if (_hierarchyPresentationMode == GridHierarchyPresentationMode.MasterDetail)
            {
                _masterDetailHeaderMap = _visibleColumns
                    .Where(column => _masterDetailHeaderColumns.Contains(column.ColumnId))
                    .ToDictionary(column => column.ColumnId, column => column.Header, StringComparer.OrdinalIgnoreCase);
            }

            RefreshSortIndicators();
            OnPropertyChanged(nameof(VisibleColumns));
            OnPropertyChanged(nameof(ColumnChooserColumns));
            RebuildGroupChips();
            if (refreshRows)
            {
                RefreshRowsView();
            }

            stopwatch.Stop();
            LogDiagnostics($"RebuildColumnBindings finished in {stopwatch.ElapsedMilliseconds} ms. Count={rebuildCounter.Count}. VisibleColumns={_visibleColumns.Count}, RefreshRows={refreshRows}. {GetGridSessionDescription()}.");
        }

        private IEnumerable<GridColumnDefinition> GetDisplayedColumns()
        {
            var visibleColumns = _layoutState?.VisibleColumns ?? Array.Empty<GridColumnDefinition>();
            if (_hierarchyPresentationMode != GridHierarchyPresentationMode.MasterDetail ||
                _masterDetailHeaderPlacementMode != GridMasterDetailHeaderPlacementMode.Inside)
            {
                return visibleColumns;
            }

            return visibleColumns.Where(column => _masterDetailMasterColumnIds.Contains(column.Id));
        }

        private void RefreshRowsView()
        {
            var gridId = GetDiagnosticsGridId();
            var refreshCounter = PhialeGridDiagnostics.IncrementGridCounter(gridId, "RefreshRowsView");
            var stopwatch = Stopwatch.StartNew();
            if (ShouldDeferVisualWorkBecauseBatched())
            {
                _pendingRefreshRowsViewAfterBatch = true;
                stopwatch.Stop();
                LogDiagnostics($"RefreshRowsView deferred because grid update batch is active. Count={refreshCounter.Count}. {GetGridSessionDescription()}.");
                return;
            }

            if (!IsLoaded)
            {
                stopwatch.Stop();
                LogDiagnostics($"RefreshRowsView skipped because grid is not loaded. Count={refreshCounter.Count}. {GetGridSessionDescription()}.");
                return;
            }

            if (ShouldDeferVisualWorkBecauseHidden())
            {
                _pendingRefreshRowsViewWhileHidden = true;
                stopwatch.Stop();
                LogDiagnostics($"RefreshRowsView deferred because grid is hidden. Count={refreshCounter.Count}. {GetGridSessionDescription()}.");
                return;
            }

            PhialeGridDiagnostics.IncrementGridCounter(gridId, "RefreshRowsViewExecuted");
            var sourceRows = ApplyChangePanelChangedRowsFilter(ApplyGlobalSearch(EnumerateEffectiveItemsSource().ToArray()));
            if (sourceRows.Length == 0)
            {
                RowsView = Array.Empty<GridDisplayRowModel>();
                _currentFilteredRows = Array.Empty<object>();
                _currentGroupIds = Array.Empty<string>();
                _currentSummary = GridSummarySet.Empty;
                _virtualizedRows = null;
                _virtualizedGroupedRows = null;
                HasRows = false;
                _displayedRowCount = 0;
                _totalRowCount = 0;
                _topLevelGroupCount = 0;
                UpdateSummaryItems(GridSummarySet.Empty);
                SyncSurfaceRendererSnapshot(Array.Empty<object>());
                RefreshSelectionCounters();
                RaiseStatusPropertyChanges();
                stopwatch.Stop();
                LogDiagnostics($"RefreshRowsView finished in {stopwatch.ElapsedMilliseconds} ms for empty result. Count={refreshCounter.Count}. {GetGridSessionDescription()}.");
                return;
            }

            if (HasHierarchy)
            {
                _virtualizedRows = null;
                _virtualizedGroupedRows = null;
                _currentFilteredRows = Array.Empty<object>();
                _currentGroupIds = Array.Empty<string>();

                var hierarchyRows = BuildHierarchyDisplayRows();
                RowsView = CreateRowsView(hierarchyRows);
                HasRows = hierarchyRows.Count > 0;
                _topLevelGroupCount = _hierarchyRoots.Count;
                _totalRowCount = CountHierarchyLeafRows(_hierarchyRoots);
                _displayedRowCount = hierarchyRows.Count;
                _currentSummary = GridSummarySet.Empty;
                UpdateSummaryItems(GridSummarySet.Empty);
                SyncSurfaceRendererSnapshot(BuildSurfaceHierarchyRows(hierarchyRows));
                RefreshSelectionCounters();
                RaiseStatusPropertyChanges();
                SyncFilterScrollOffset();
                stopwatch.Stop();
                LogDiagnostics($"RefreshRowsView finished in {stopwatch.ElapsedMilliseconds} ms for hierarchy rows. Count={refreshCounter.Count}, DisplayedRows={_displayedRowCount}. {GetGridSessionDescription()}.");
                return;
            }

            if (_groupDescriptors.Count == 0)
            {
                _currentFilteredRows = Array.Empty<object>();
                _currentGroupIds = Array.Empty<string>();
                _virtualizedGroupedRows = null;
                _virtualizedRows = CreateVirtualizedRows(sourceRows);
                var surfaceRows = BuildSurfaceRows(sourceRows);
                RowsView = CreateRowsView(_virtualizedRows);
                HasRows = _virtualizedRows.Count > 0;
                _topLevelGroupCount = 0;
                _totalRowCount = _virtualizedRows.TotalItemCount;
                _displayedRowCount = _virtualizedRows.Count;
                _currentSummary = _virtualizedRows.Summary;
                UpdateSummaryItems(_currentSummary);
                SyncSurfaceRendererSnapshot(surfaceRows);
                RefreshSelectionCounters();
                RaiseStatusPropertyChanges();
                SyncFilterScrollOffset();
                EnsureViewportWindow();
                stopwatch.Stop();
                LogDiagnostics($"RefreshRowsView finished in {stopwatch.ElapsedMilliseconds} ms for flat rows. Count={refreshCounter.Count}, DisplayedRows={_displayedRowCount}. {GetGridSessionDescription()}.");
                return;
            }

            _virtualizedRows = null;
            _currentFilteredRows = Array.Empty<object>();
            var groupedSurfaceResult = BuildGroupedSurfaceResult(sourceRows, _collapseGroupsOnNextRefresh);
            _collapseGroupsOnNextRefresh = false;
            _virtualizedGroupedRows = null;
            var groupedDisplayRows = CreateGroupedDisplayRows(groupedSurfaceResult.Rows);
            RowsView = CreateRowsView(groupedDisplayRows);
            HasRows = groupedDisplayRows.Count > 0;
            _currentGroupIds = groupedSurfaceResult.GroupIds;
            _topLevelGroupCount = groupedSurfaceResult.TopLevelGroupCount;
            _totalRowCount = groupedSurfaceResult.TotalItemCount;
            _displayedRowCount = groupedSurfaceResult.VisibleRowCount;
            _currentSummary = groupedSurfaceResult.Summary;
            UpdateSummaryItems(_currentSummary);
            SyncSurfaceRendererSnapshot(groupedSurfaceResult.Rows);
            RefreshSelectionCounters();
            RaiseStatusPropertyChanges();
            SyncFilterScrollOffset();
            EnsureViewportWindow();
            stopwatch.Stop();
            LogDiagnostics($"RefreshRowsView finished in {stopwatch.ElapsedMilliseconds} ms for grouped rows. Count={refreshCounter.Count}, DisplayedRows={_displayedRowCount}, Groups={_topLevelGroupCount}. {GetGridSessionDescription()}.");
        }

        private object[] ApplyGlobalSearch(IReadOnlyList<object> sourceRows)
        {
            if (string.IsNullOrWhiteSpace(_globalSearchText))
            {
                return sourceRows.ToArray();
            }

            var search = _globalSearchText;
            return sourceRows
                .Where(row => _visibleColumns.Any(column =>
                {
                    var value = ResolveRowValue(row, column.ColumnId);
                    var text = GridValueFormatter.FormatDisplayValue(value);
                    return text.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
                }))
                .ToArray();
        }

        private object[] ApplyChangePanelChangedRowsFilter(IReadOnlyList<object> sourceRows)
        {
            if (!_isChangePanelChangedRowsFilterActive)
            {
                return sourceRows.ToArray();
            }

            var changedRowIds = new HashSet<string>(
                CurrentEditSessionContext?.EditedRecordIds ?? Array.Empty<string>(),
                StringComparer.OrdinalIgnoreCase);
            if (changedRowIds.Count == 0)
            {
                return Array.Empty<object>();
            }

            return sourceRows
                .Where(row => changedRowIds.Contains(ResolveRowId(row)))
                .ToArray();
        }

        private GridFilterGroup BuildFilterGroup()
        {
            var filters = _visibleColumns
                .Where(column => !string.IsNullOrWhiteSpace(column.FilterText))
                .Select(column => new GridFilterDescriptor(column.ColumnId, GridFilterOperator.Contains, column.FilterText))
                .ToArray();

            return new GridFilterGroup(filters, GridLogicalOperator.And);
        }

        private IReadOnlyList<GridSortDescriptor> BuildEffectiveSorts()
        {
            var groupSorts = _groupDescriptors.Select(group => new GridSortDescriptor(group.ColumnId, group.Direction));
            var nonGroupSorts = _sortDescriptors.Where(sort => _groupDescriptors.All(group => !string.Equals(group.ColumnId, sort.ColumnId, StringComparison.OrdinalIgnoreCase)));
            return groupSorts.Concat(nonGroupSorts).ToArray();
        }

        private IReadOnlyList<object> BuildSurfaceRows(IReadOnlyList<object> sourceRows)
        {
            if (sourceRows == null || sourceRows.Count == 0)
            {
                return Array.Empty<object>();
            }

            var queryEngine = new GridQueryEngine<object>(new DelegateGridRowAccessor<object>(ResolveRowValue));
            var query = new GridQueryRequest(
                0,
                sourceRows.Count,
                BuildEffectiveSorts(),
                BuildFilterGroup(),
                Array.Empty<GridGroupDescriptor>(),
                Array.Empty<GridSummaryDescriptor>());
            return queryEngine.Execute(sourceRows, query).Items;
        }

        private IReadOnlyList<object> BuildGroupedSurfaceRows(IReadOnlyList<object> sourceRows)
        {
            return BuildGroupedSurfaceResult(sourceRows, false).Rows.Cast<object>().ToArray();
        }

        private GridGroupedQueryResult<object> BuildGroupedSurfaceResult(IReadOnlyList<object> sourceRows, bool collapseGroupsOnFirstLoad)
        {
            var groupedQueryCounter = PhialeGridDiagnostics.IncrementGridCounter(GetDiagnosticsGridId(), "BuildGroupedSurfaceResult");
            var stopwatch = Stopwatch.StartNew();
            if (sourceRows == null || sourceRows.Count == 0 || _groupDescriptors.Count == 0)
            {
                stopwatch.Stop();
                LogDiagnostics($"BuildGroupedSurfaceResult skipped for empty grouped input. Count={groupedQueryCounter.Count}. {GetGridSessionDescription()}.");
                return new GridGroupedQueryResult<object>(
                    Array.Empty<GridGroupFlatRow<object>>(),
                    0,
                    0,
                    0,
                    Array.Empty<string>(),
                    GridSummarySet.Empty);
            }

            var queryEngine = new GridQueryEngine<object>(new DelegateGridRowAccessor<object>(ResolveRowValue));
            var request = CreateGroupedSurfaceQueryRequest(sourceRows.Count);
            var result = queryEngine.ExecuteGroupedWindow(sourceRows, request);
            if (collapseGroupsOnFirstLoad && result.GroupIds.Count > 0)
            {
                foreach (var groupId in result.GroupIds)
                {
                    _groupExpansionState.SetExpanded(groupId, false);
                }

                result = queryEngine.ExecuteGroupedWindow(sourceRows, CreateGroupedSurfaceQueryRequest(sourceRows.Count));
            }

            stopwatch.Stop();
            LogDiagnostics($"BuildGroupedSurfaceResult finished in {stopwatch.ElapsedMilliseconds} ms. Count={groupedQueryCounter.Count}, Rows={result.Rows.Count}, VisibleRows={result.VisibleRowCount}, TotalItems={result.TotalItemCount}, Groups={result.TopLevelGroupCount}. {GetGridSessionDescription()}.");
            return result;
        }

        private GridGroupedQueryRequest CreateGroupedSurfaceQueryRequest(int sourceRowCount)
        {
            return new GridGroupedQueryRequest(
                0,
                EstimateGroupedSurfaceRowCount(sourceRowCount, _groupDescriptors.Count),
                BuildEffectiveSorts(),
                BuildFilterGroup(),
                _groupDescriptors,
                _summaryDescriptors,
                _groupExpansionState);
        }

        private IList CreateGroupedDisplayRows(IReadOnlyList<GridGroupFlatRow<object>> groupedRows)
        {
            if (groupedRows == null || groupedRows.Count == 0)
            {
                return Array.Empty<GridDisplayRowModel>();
            }

            var displayRows = new GridDisplayRowModel[groupedRows.Count];
            var displayColumnId = ResolveGroupedSurfaceDisplayColumnId();
            for (var index = 0; index < groupedRows.Count; index++)
            {
                var row = groupedRows[index];
                displayRows[index] = row.Kind == GridGroupFlatRowKind.GroupHeader
                    ? (GridDisplayRowModel)new GridGroupHeaderRowModel(row.GroupId, row.GroupColumnId, row.GroupKey, row.GroupItemCount, row.Level, row.IsExpanded, displayColumnId)
                    : new GridDataRowModel(this, row.Item, row.Level);
            }

            return displayRows;
        }

        private static int EstimateGroupedSurfaceRowCount(int sourceRowCount, int groupCount)
        {
            if (sourceRowCount <= 0)
            {
                return 1;
            }

            var multiplier = Math.Max(2, groupCount + 1);
            return (int)Math.Min(int.MaxValue, Math.Max(1L, (long)sourceRowCount * multiplier));
        }

        private string ResolveGroupedSurfaceDisplayColumnId()
        {
            return _visibleColumns.FirstOrDefault()?.ColumnId
                ?? (_groupDescriptors.Count == 0 ? string.Empty : _groupDescriptors[0].ColumnId);
        }

        private static string BuildGroupedSurfaceCaption(GridGroupFlatRow<object> groupedRow)
        {
            var keyText = Convert.ToString(groupedRow.GroupKey, CultureInfo.CurrentCulture) ?? string.Empty;
            return groupedRow.GroupColumnId + ": " + keyText + " (" + groupedRow.GroupItemCount.ToString(CultureInfo.CurrentCulture) + ")";
        }

        private void ApplyFilterState(GridFilterGroup filterGroup)
        {
            var filterCounter = PhialeGridDiagnostics.IncrementGridCounter(GetDiagnosticsGridId(), "ApplyFilterState");
            var stopwatch = Stopwatch.StartNew();
            var byColumn = filterGroup?.Filters.ToDictionary(
                    filter => filter.ColumnId,
                    filter => Convert.ToString(filter.Value, CultureInfo.InvariantCulture) ?? string.Empty,
                    StringComparer.OrdinalIgnoreCase)
                ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            _suppressColumnFilterRefresh = true;
            try
            {
                foreach (var column in _visibleColumns)
                {
                    column.FilterText = byColumn.TryGetValue(column.ColumnId, out var value) ? value : string.Empty;
                }
            }
            finally
            {
                _suppressColumnFilterRefresh = false;
            }
            stopwatch.Stop();
            LogDiagnostics($"ApplyFilterState finished in {stopwatch.ElapsedMilliseconds} ms. Count={filterCounter.Count}, FilterCount={byColumn.Count}. {GetGridSessionDescription()}.");
        }

        internal object ResolveRowValue(object row, string columnId)
        {
            if (row == null || string.IsNullOrWhiteSpace(columnId))
            {
                return null;
            }

            if (row is GridGroupFlatRow<object> groupedRow)
            {
                if (groupedRow.Kind == GridGroupFlatRowKind.GroupHeader)
                {
                    return string.Equals(columnId, ResolveGroupedSurfaceDisplayColumnId(), StringComparison.OrdinalIgnoreCase)
                        ? (object)BuildGroupedSurfaceCaption(groupedRow)
                        : string.Empty;
                }

                row = groupedRow.Item;
            }

            if (row is GridGroupHeaderRowModel groupHeaderRow)
            {
                return groupHeaderRow[columnId];
            }

            if (row is GridHierarchyNodeRowModel hierarchyRow)
            {
                return hierarchyRow[columnId];
            }

            if (row is GridHierarchyLoadMoreRowModel loadMoreRow)
            {
                return loadMoreRow[columnId];
            }

            if (row is GridMasterDetailHeaderRowModel masterDetailHeaderRow)
            {
                return masterDetailHeaderRow[columnId];
            }

            if (row is GridMasterDetailMasterRowModel masterDetailMasterRow)
            {
                return masterDetailMasterRow[columnId];
            }

            if (row is GridSurfaceMasterDetailDetailsHostRowModel)
            {
                return string.Empty;
            }

            if (row is GridMasterDetailDetailRowModel masterDetailDetailRow)
            {
                return masterDetailDetailRow[columnId];
            }

            if (row is GridDataRowModel dataRowModel)
            {
                row = dataRowModel.SourceRow;
            }

            var fieldDefinition = ResolveEditSessionFieldDefinition(columnId);
            if (fieldDefinition != null)
            {
                return fieldDefinition.GetValue(row);
            }

            if (row is IDictionary<string, object> dictionary && dictionary.TryGetValue(columnId, out var value))
            {
                return value;
            }

            var cacheKey = row.GetType().FullName + "|" + columnId;
            if (!_propertyCache.TryGetValue(cacheKey, out var propertyInfo))
            {
                propertyInfo = row.GetType().GetProperty(columnId, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
                _propertyCache[cacheKey] = propertyInfo;
            }

            return propertyInfo == null ? null : propertyInfo.GetValue(row, null);
        }

        private void RebuildGroupChips()
        {
            var lastIndex = _groupDescriptors.Count - 1;
            _groupChips = new ObservableCollection<GridGroupChipModel>(
                _groupDescriptors.Select((group, index) =>
                {
                    var column = _visibleColumns.FirstOrDefault(candidate => string.Equals(candidate.ColumnId, group.ColumnId, StringComparison.OrdinalIgnoreCase));
                    var header = column == null ? group.ColumnId : column.Header;
                    var directionGlyph = group.Direction == GridSortDirection.Ascending ? "↑" : "↓";
                    return new GridGroupChipModel(group.ColumnId, header, directionGlyph, index < lastIndex);
                }));

            OnPropertyChanged(nameof(GroupChips));
            OnPropertyChanged(nameof(HasNoGroups));
            OnPropertyChanged(nameof(HasGroups));
        }

        private void SetAllGroupsExpandedState(IEnumerable<string> groupIds, bool isExpanded)
        {
            foreach (var groupId in groupIds)
            {
                _groupExpansionState.SetExpanded(groupId, isExpanded);
            }
        }

        private void ToggleGroup(string groupId)
        {
            if (string.IsNullOrWhiteSpace(groupId))
            {
                return;
            }

            var nextExpanded = !_groupExpansionState.IsExpanded(groupId);
            _groupExpansionState.SetExpanded(groupId, nextExpanded);
            RefreshRowsView();
        }

        private void HandleSurfaceRowActionRequested(object sender, GridRowActionRequestedEventArgs e)
        {
            if (e == null || string.IsNullOrWhiteSpace(e.RowKey) || !_surfaceRowsByKey.TryGetValue(e.RowKey, out var row))
            {
                return;
            }

            if (e.ActionKind == GridRowActionKind.ToggleHierarchy &&
                row is GridGroupFlatRow<object> groupedRow &&
                groupedRow.Kind == GridGroupFlatRowKind.GroupHeader)
            {
                ToggleGroup(groupedRow.GroupId);
                return;
            }

            if (e.ActionKind == GridRowActionKind.ToggleHierarchy &&
                row is GridHierarchyNodeRowModel hierarchyRow &&
                hierarchyRow.Node.CanExpand)
            {
                ToggleHierarchyNodeAsync(hierarchyRow);
                return;
            }

            if (e.ActionKind == GridRowActionKind.ToggleHierarchy &&
                row is GridMasterDetailMasterRowModel masterDetailRow)
            {
                _ = ToggleHierarchyNodeAsync(masterDetailRow.Node);
                return;
            }

            if (e.ActionKind == GridRowActionKind.ToggleDetails)
            {
                ToggleCustomRowDetail(e.RowKey, row);
                return;
            }

            if (e.ActionKind == GridRowActionKind.LoadMoreHierarchy &&
                row is GridHierarchyLoadMoreRowModel loadMoreRow)
            {
                LoadMoreHierarchyChildrenAsync(loadMoreRow);
            }
        }

        private void ToggleCustomRowDetail(string rowKey, object row)
        {
            if (_rowDetailProvider == null || string.IsNullOrWhiteSpace(rowKey))
            {
                return;
            }

            var record = UnwrapCustomDetailRecord(row);
            if (record == null)
            {
                return;
            }

            var request = CreateRowDetailRequest(rowKey, record);
            if (!_rowDetailProvider.HasDetail(request))
            {
                return;
            }

            _rowDetailExpansionState = _rowDetailExpansionState.Toggle(rowKey);
            RefreshRowsView();
        }

        private async void ToggleHierarchyNodeAsync(GridHierarchyNodeRowModel hierarchyNodeRow)
        {
            if (hierarchyNodeRow == null || !HasHierarchy)
            {
                return;
            }

            await ToggleHierarchyNodeAsync(hierarchyNodeRow.Node).ConfigureAwait(true);
        }

        private async Task ToggleHierarchyNodeAsync(GridHierarchyNode<object> node)
        {
            if (node == null || !HasHierarchy)
            {
                return;
            }

            if (node.IsExpanded)
            {
                _hierarchyController.Collapse(node);
            }
            else
            {
                await _hierarchyController.ExpandAsync(node).ConfigureAwait(true);
            }

            RefreshRowsView();
        }

        private async void LoadMoreHierarchyChildrenAsync(GridHierarchyLoadMoreRowModel loadMoreRow)
        {
            if (loadMoreRow == null || !HasHierarchy)
            {
                return;
            }

            await _hierarchyController.LoadNextChildrenPageAsync(loadMoreRow.ParentNode).ConfigureAwait(true);
            RefreshRowsView();
        }

        private void CollapseHierarchyBranch(GridHierarchyNode<object> node)
        {
            if (node == null)
            {
                return;
            }

            _hierarchyController.Collapse(node);
            foreach (var child in node.Children)
            {
                CollapseHierarchyBranch(child);
            }
        }

        private static GridHierarchyNode<object> FindHierarchyNode(string pathId, IEnumerable<GridHierarchyNode<object>> nodes)
        {
            if (string.IsNullOrWhiteSpace(pathId) || nodes == null)
            {
                return null;
            }

            foreach (var node in nodes)
            {
                if (string.Equals(node.PathId, pathId, StringComparison.OrdinalIgnoreCase))
                {
                    return node;
                }

                var nested = FindHierarchyNode(pathId, node.Children);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }

        private void SyncFilterScrollOffset()
        {
            if (_isSyncingFilterScroll || FilterScrollViewer == null || SurfaceHost == null)
            {
                return;
            }

            _isSyncingFilterScroll = true;
            try
            {
                FilterScrollViewer.ScrollToHorizontalOffset(SurfaceHost.HorizontalOffset);
            }
            finally
            {
                _isSyncingFilterScroll = false;
            }
        }

        private void EnsureColumnFilterVisible(string columnId)
        {
            if (string.IsNullOrWhiteSpace(columnId) || SurfaceHost == null || FilterScrollViewer == null)
            {
                return;
            }

            var viewportWidth = FilterScrollViewer.ViewportWidth > 0d
                ? FilterScrollViewer.ViewportWidth
                : FilterScrollViewer.ActualWidth;
            if (viewportWidth <= 0d)
            {
                return;
            }

            var visibleColumns = _visibleColumns.ToArray();
            var targetIndex = Array.FindIndex(
                visibleColumns,
                candidate => string.Equals(candidate.ColumnId, columnId, StringComparison.OrdinalIgnoreCase));
            if (targetIndex < 0)
            {
                return;
            }

            var targetStart = 0d;
            for (var index = 0; index < targetIndex; index++)
            {
                targetStart += visibleColumns[index].Width;
            }

            var targetWidth = visibleColumns[targetIndex].Width;
            var targetEnd = targetStart + targetWidth;
            var currentOffset = SurfaceHost.HorizontalOffset;
            var desiredOffset = currentOffset;

            if (targetStart < currentOffset)
            {
                desiredOffset = targetStart;
            }
            else if (targetEnd > currentOffset + viewportWidth)
            {
                desiredOffset = targetEnd - viewportWidth;
            }

            desiredOffset = Math.Max(0d, desiredOffset);
            if (Math.Abs(desiredOffset - currentOffset) < 0.1d)
            {
                return;
            }

            var universalArgs = WpfUniversalInputAdapter.CreateScrollChangedEventArgs(desiredOffset, SurfaceHost.VerticalOffset);
            _surfaceCoordinator.ProcessInput(_surfaceInputAdapter.CreateScrollChangedInput(universalArgs, DateTime.UtcNow));
            SyncFilterScrollOffset();
        }

        private GridVirtualizedRowCollection CreateVirtualizedRows(IReadOnlyList<object> sourceRows)
        {
            var displayColumnId = _visibleColumns.FirstOrDefault()?.ColumnId ?? string.Empty;
            return new GridVirtualizedRowCollection(
                this,
                sourceRows,
                BuildFilterGroup(),
                BuildEffectiveSorts(),
                _summaryDescriptors,
                displayColumnId);
        }

        private GridVirtualizedGroupedRowCollection CreateVirtualizedGroupedRows(IReadOnlyList<object> sourceRows, bool collapseGroupsOnFirstLoad)
        {
            var displayColumnId = _visibleColumns.FirstOrDefault()?.ColumnId ?? (_groupDescriptors.Count == 0 ? string.Empty : _groupDescriptors[0].ColumnId);
            return new GridVirtualizedGroupedRowCollection(
                this,
                sourceRows,
                BuildFilterGroup(),
                _sortDescriptors,
                _groupDescriptors,
                _summaryDescriptors,
                _groupExpansionState,
                displayColumnId,
                collapseGroupsOnFirstLoad);
        }

        private IList BuildHierarchyDisplayRows()
        {
            if (_hierarchyPresentationMode == GridHierarchyPresentationMode.MasterDetail &&
                _masterDetailHeaderPlacementMode == GridMasterDetailHeaderPlacementMode.Inside)
            {
                return _hierarchyRoots
                    .Select(root => (GridDisplayRowModel)BuildMasterDetailMasterRow(root))
                    .ToArray();
            }

            var rows = new List<GridDisplayRowModel>();
            foreach (var root in _hierarchyRoots)
            {
                if (_hierarchyPresentationMode == GridHierarchyPresentationMode.MasterDetail)
                {
                    AppendMasterDetailRows(root, rows);
                }
                else
                {
                    AppendHierarchyRows(root, rows);
                }
            }

            return rows;
        }

        private IReadOnlyList<object> BuildSurfaceHierarchyRows(IEnumerable hierarchyRows)
        {
            if (hierarchyRows == null)
            {
                return Array.Empty<object>();
            }

            if (_hierarchyPresentationMode != GridHierarchyPresentationMode.MasterDetail ||
                _masterDetailHeaderPlacementMode != GridMasterDetailHeaderPlacementMode.Inside)
            {
                return hierarchyRows.Cast<object>().ToArray();
            }

            var surfaceRows = new List<object>();
            foreach (var row in hierarchyRows.Cast<object>())
            {
                surfaceRows.Add(row);

                if (row is GridMasterDetailMasterRowModel masterRow && masterRow.IsDetailExpanded)
                {
                    surfaceRows.Add(new GridSurfaceMasterDetailDetailsHostRowModel(masterRow));
                }
            }

            return surfaceRows;
        }

        private IReadOnlyList<GridCsvExportColumnMapping> BuildCsvExportColumns()
        {
            var duplicates = _visibleColumns
                .GroupBy(column => column.Header, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            return _visibleColumns
                .Select(column => new GridCsvExportColumnMapping(
                    duplicates.Contains(column.Header) ? column.Header + " (" + column.ColumnId + ")" : column.Header,
                    column.ColumnId))
                .ToArray();
        }

        private object ResolveCsvExportValue(object row, string columnId)
        {
            var value = ResolveRowValue(row, columnId);
            if (value is DateTime dateTime)
            {
                return dateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }

            return value;
        }

        private void AppendHierarchyRows(GridHierarchyNode<object> node, List<GridDisplayRowModel> rows)
        {
            rows.Add(new GridHierarchyNodeRowModel(this, node, _hierarchyDisplayColumnId));
            if (!node.IsExpanded)
            {
                return;
            }

            foreach (var child in node.Children)
            {
                AppendHierarchyRows(child, rows);
            }

            if (node.HasMoreChildren)
            {
                rows.Add(new GridHierarchyLoadMoreRowModel(node, node.PathId, _hierarchyDisplayColumnId, GetText(GridTextKeys.HierarchyLoadMore)));
            }
        }

        private void AppendMasterDetailRows(GridHierarchyNode<object> node, List<GridDisplayRowModel> rows)
        {
            rows.Add(new GridHierarchyNodeRowModel(this, node, _hierarchyDisplayColumnId));
            if (!node.IsExpanded)
            {
                return;
            }

            if (_masterDetailHeaderMap.Count > 0)
            {
                rows.Add(new GridMasterDetailHeaderRowModel(_masterDetailHeaderMap, node.PathId));
            }

            foreach (var child in node.Children)
            {
                if (child.CanExpand)
                {
                    AppendMasterDetailRows(child, rows);
                    continue;
                }

                rows.Add(new GridMasterDetailDetailRowModel(
                    this,
                    child.Item,
                    _masterDetailDisplayColumnId,
                    _masterDetailHeaderColumns,
                    node.PathId.Count(character => character == '/') + 1,
                    applyDisplayIndent: true));
            }

            if (node.HasMoreChildren)
            {
                rows.Add(new GridHierarchyLoadMoreRowModel(node, node.PathId, _masterDetailDisplayColumnId, GetText(GridTextKeys.HierarchyLoadMore)));
            }
        }

        private GridMasterDetailMasterRowModel BuildMasterDetailMasterRow(GridHierarchyNode<object> node)
        {
            var filters = GetOrCreateMasterDetailFilters(node.PathId);
            var detailRows = CreateMasterDetailDetailRows(node, filters);
            var row = new GridMasterDetailMasterRowModel(
                this,
                node,
                _hierarchyDisplayColumnId,
                CreateMasterDetailColumns(node.PathId, filters),
                _masterDetailDisplayColumnId,
                detailRows);
            row.RowActionWidth = 0d;
            row.ShowRowIndicator = SelectCurrentRow;
            row.RowIndicatorWidth = ResolvedRowIndicatorWidth;
            row.ShowSelectionCheckbox = MultiSelect;
            row.SelectionCheckboxWidth = ResolvedSelectionCheckboxWidth;
            row.ShowRowNumbers = ShowNb;
            row.RowNumberWidth = ShowNb
                ? ResolveAdaptiveRowMarkerWidth(detailRows.Count)
                : 0d;
            return row;
        }

        private IReadOnlyList<GridColumnDefinition> ResolveMasterDetailColumnDefinitions()
        {
            var layoutColumns = _layoutState?.Columns ?? Array.Empty<GridColumnDefinition>();
            var detailColumns = layoutColumns
                .Where(column => _masterDetailDetailColumnIds.Contains(column.Id))
                .OrderBy(column => column.DisplayIndex)
                .ToArray();
            if (detailColumns.Length > 0)
            {
                return detailColumns;
            }

            return _baselineColumns
                .Where(column => _masterDetailDetailColumnIds.Contains(column.Id))
                .OrderBy(column => column.DisplayIndex)
                .ToArray();
        }

        private IReadOnlyList<GridMasterDetailColumnModel> CreateMasterDetailColumns(string pathId, IReadOnlyDictionary<string, string> filters)
        {
            return ResolveMasterDetailColumnDefinitions()
                .Select(column => new GridMasterDetailColumnModel(
                    column.Id,
                    column.Header,
                    Math.Max(column.MinWidth, column.Width),
                    filters.TryGetValue(column.Id, out var filterText) ? filterText : string.Empty,
                    pathId,
                    HandleMasterDetailFilterChanged))
                .ToArray();
        }

        private ObservableCollection<GridMasterDetailDetailRowModel> CreateMasterDetailDetailRows(GridHierarchyNode<object> node, IReadOnlyDictionary<string, string> filters)
        {
            var rows = node.Children
                .Where(child => !child.CanExpand)
                .Select(child => child.Item)
                .Where(item => MatchesMasterDetailFilters(item, filters))
                .Select(item => new GridMasterDetailDetailRowModel(
                    this,
                    item,
                    _masterDetailDisplayColumnId,
                    _masterDetailDetailColumnIds,
                    node.PathId.Count(character => character == '/') + 1,
                    applyDisplayIndent: false))
                .ToArray();

            return new ObservableCollection<GridMasterDetailDetailRowModel>(rows);
        }

        private Dictionary<string, string> GetOrCreateMasterDetailFilters(string pathId)
        {
            if (!_masterDetailFilterStateByPathId.TryGetValue(pathId ?? string.Empty, out var filters))
            {
                filters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                _masterDetailFilterStateByPathId[pathId ?? string.Empty] = filters;
            }

            foreach (var detailColumnId in _masterDetailDetailColumnIds)
            {
                if (!filters.ContainsKey(detailColumnId))
                {
                    filters[detailColumnId] = string.Empty;
                }
            }

            return filters;
        }

        private bool MatchesMasterDetailFilters(object row, IReadOnlyDictionary<string, string> filters)
        {
            if (filters == null || filters.Count == 0)
            {
                return true;
            }

            foreach (var filter in filters)
            {
                if (string.IsNullOrWhiteSpace(filter.Value))
                {
                    continue;
                }

                var text = GridValueFormatter.FormatDisplayValue(ResolveRowValue(row, filter.Key));
                if (text.IndexOf(filter.Value, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    return false;
                }
            }

            return true;
        }

        private void HandleMasterDetailFilterChanged(string pathId, string columnId, string filterText)
        {
            if (string.IsNullOrWhiteSpace(pathId) || string.IsNullOrWhiteSpace(columnId))
            {
                return;
            }

            var filters = GetOrCreateMasterDetailFilters(pathId);
            filters[columnId] = filterText ?? string.Empty;

            var row = RowsView.Cast<object>()
                .OfType<GridMasterDetailMasterRowModel>()
                .FirstOrDefault(candidate => string.Equals(candidate.Node.PathId, pathId, StringComparison.OrdinalIgnoreCase));
            if (row == null)
            {
                return;
            }

            row.ReplaceDetailRows(CreateMasterDetailDetailRows(row.Node, filters));
            if (_hierarchyPresentationMode == GridHierarchyPresentationMode.MasterDetail &&
                _masterDetailHeaderPlacementMode == GridMasterDetailHeaderPlacementMode.Inside)
            {
                SyncSurfaceRendererSnapshot(BuildSurfaceHierarchyRows(RowsView));
            }
        }

        internal bool IsCurrentMasterDetailRow(string pathId, GridMasterDetailDetailRowModel detailRow)
        {
            if (string.IsNullOrWhiteSpace(pathId) ||
                detailRow?.SourceRow == null ||
                !_masterDetailCurrentRowsByPathId.TryGetValue(pathId, out var currentRow) ||
                currentRow == null)
            {
                return false;
            }

            if (ReferenceEquals(currentRow, detailRow.SourceRow))
            {
                return true;
            }

            var currentRowId = ResolveRowId(currentRow);
            var detailRowId = ResolveRowId(detailRow.SourceRow);
            return !string.IsNullOrWhiteSpace(currentRowId) &&
                string.Equals(currentRowId, detailRowId, StringComparison.OrdinalIgnoreCase);
        }

        internal void SetCurrentMasterDetailRow(string pathId, GridMasterDetailDetailRowModel detailRow)
        {
            if (string.IsNullOrWhiteSpace(pathId) || detailRow?.SourceRow == null)
            {
                return;
            }

            _masterDetailCurrentRowsByPathId[pathId] = detailRow.SourceRow;
        }

        private static ICollectionView CreateRowsView(IList source)
        {
            return new GridVirtualizedCollectionView(source);
        }

        private void EnsureViewportWindow()
        {
            if ((_virtualizedRows == null && _virtualizedGroupedRows == null) || SurfaceHost == null)
            {
                return;
            }

            var columnWidths = _visibleColumns.Select(column => Math.Max(column.MinWidth, column.Width)).ToArray();
            if (columnWidths.Length == 0)
            {
                return;
            }

            var viewportHeight = Math.Max(1d, SurfaceHost.ViewportHeight);
            var viewport = new GridViewport(
                Math.Max(0d, SurfaceHost.HorizontalOffset),
                Math.Max(0d, SurfaceHost.VerticalOffset),
                Math.Max(1d, SurfaceHost.ViewportWidth),
                viewportHeight,
                ResolvedRowHeight,
                columnWidths);

            Viewport = viewport;
            _virtualizedRows?.EnsureViewport(viewport, _totalRowCount);
            _virtualizedGroupedRows?.EnsureViewport(viewport);
        }

        private static int CountHierarchyLeafRows(IEnumerable<GridHierarchyNode<object>> roots)
        {
            if (roots == null)
            {
                return 0;
            }

            var total = 0;
            foreach (var root in roots)
            {
                total += CountHierarchyLeafRows(root);
            }

            return total;
        }

        private static int CountHierarchyLeafRows(GridHierarchyNode<object> node)
        {
            if (node == null)
            {
                return 0;
            }

            if (!node.CanExpand)
            {
                return 1;
            }

            var childrenTotal = 0;
            foreach (var child in node.Children)
            {
                childrenTotal += CountHierarchyLeafRows(child);
            }

            return childrenTotal;
        }

        private static T FindDescendant<T>(DependencyObject root)
            where T : DependencyObject
        {
            return FindDescendant<T>(root, _ => true);
        }

        private static T FindAncestor<T>(DependencyObject dependencyObject)
            where T : DependencyObject
        {
            var current = dependencyObject;
            while (current != null)
            {
                if (current is T ancestor)
                {
                    return ancestor;
                }

                current = VisualTreeHelper.GetParent(current);
            }

            return null;
        }

        private static T FindDescendant<T>(DependencyObject root, Func<T, bool> predicate)
            where T : DependencyObject
        {
            if (root == null)
            {
                return null;
            }

            for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
            {
                var child = VisualTreeHelper.GetChild(root, index);
                if (child is T target && predicate(target))
                {
                    return target;
                }

                var nested = FindDescendant<T>(child, predicate);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }

        private void RefreshSortIndicators()
        {
            foreach (var column in _visibleColumns)
            {
                var sort = _sortDescriptors
                    .Select((descriptor, index) => new { descriptor, index })
                    .FirstOrDefault(candidate => string.Equals(candidate.descriptor.ColumnId, column.ColumnId, StringComparison.OrdinalIgnoreCase));
                if (sort != null)
                {
                    column.ApplySort(sort.descriptor.Direction, sort.index);
                    continue;
                }

                var groupSort = _groupDescriptors
                    .Select((descriptor, index) => new { descriptor, index })
                    .FirstOrDefault(candidate => string.Equals(candidate.descriptor.ColumnId, column.ColumnId, StringComparison.OrdinalIgnoreCase));
                if (groupSort != null)
                {
                    column.ApplySort(groupSort.descriptor.Direction, groupSort.index);
                }
                else
                {
                    column.ClearSort();
                }
            }
        }

        private void UpdateSummaryItems(GridSummarySet summary)
        {
            if (_summaryDescriptors.Count == 0)
            {
                _summaryItems = new ObservableCollection<GridSummaryDisplayItem>();
                HasSummaries = false;
                OnPropertyChanged(nameof(SummaryItems));
                return;
            }

            var items = new List<GridSummaryDisplayItem>(_summaryDescriptors.Count);
            foreach (var descriptor in _summaryDescriptors)
            {
                var key = descriptor.ColumnId + ":" + descriptor.Type;
                summary.Values.TryGetValue(key, out var value);
                var column = _visibleColumns.FirstOrDefault(candidate => string.Equals(candidate.ColumnId, descriptor.ColumnId, StringComparison.OrdinalIgnoreCase));
                var header = column == null ? descriptor.ColumnId : column.Header;
                items.Add(new GridSummaryDisplayItem(header, GetSummaryTypeText(descriptor.Type), GridValueFormatter.FormatDisplayValue(value)));
            }

            _summaryItems = new ObservableCollection<GridSummaryDisplayItem>(items);
            HasSummaries = items.Count > 0;
            OnPropertyChanged(nameof(SummaryItems));
        }

        private void UpdateSummaryItemsFromCurrentRows()
        {
            UpdateSummaryItems(_currentSummary);
        }

        private void HandleColumnFilterChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(GridColumnBindingModel.FilterText))
            {
                if (_suppressColumnFilterRefresh)
                {
                    return;
                }

                RefreshRowsView();
                RaiseViewStateChanged();
            }
        }

        private void HandleClearFilterColumnClick(object sender, RoutedEventArgs e)
        {
            if (!((sender as FrameworkElement)?.DataContext is GridColumnBindingModel column))
            {
                return;
            }

            if (!string.IsNullOrEmpty(column.FilterText))
            {
                column.FilterText = string.Empty;
            }
        }

        internal void ApplyColumnSort(string columnId, GridSortDirection direction, bool appendToExistingSorts)
        {
            if (string.IsNullOrWhiteSpace(columnId))
            {
                return;
            }

            var nextSorts = appendToExistingSorts
                ? _sortDescriptors
                    .Where(sort => !string.Equals(sort.ColumnId, columnId, StringComparison.OrdinalIgnoreCase))
                    .ToList()
                : new List<GridSortDescriptor>();

            nextSorts.Add(new GridSortDescriptor(columnId, direction));
            _sortDescriptors = nextSorts.ToArray();
            RefreshSortIndicators();
            SyncSortsProperty();
            RefreshRowsView();
            RaiseViewStateChanged();
        }

        internal void ClearColumnSort(string columnId)
        {
            if (string.IsNullOrWhiteSpace(columnId))
            {
                return;
            }

            _sortDescriptors = _sortDescriptors
                .Where(sort => !string.Equals(sort.ColumnId, columnId, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            RefreshSortIndicators();
            SyncSortsProperty();
            RefreshRowsView();
            RaiseViewStateChanged();
        }

        private void HandleSurfaceHeaderActivated(object sender, GridHeaderActivatedEventArgs e)
        {
            if (e == null ||
                e.HeaderKind != global::PhialeGrid.Core.Surface.GridHeaderKind.ColumnHeader ||
                string.IsNullOrWhiteSpace(e.HeaderKey) ||
                _visibleColumns.All(column => !string.Equals(column.ColumnId, e.HeaderKey, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            if (IsColumnGrouped(e.HeaderKey))
            {
                HandleToggleGroupingDirection(e.HeaderKey);
                return;
            }

            ToggleColumnSort(e.HeaderKey, MapSurfaceSortModifiers(e.Modifiers));
        }

        private void HandleSurfaceColumnResizeRequested(object sender, GridColumnResizeRequestedEventArgs e)
        {
            if (e == null || string.IsNullOrWhiteSpace(e.ColumnKey))
            {
                return;
            }

            SetColumnWidth(e.ColumnKey, e.Width);
        }

        private void HandleSurfaceColumnReorderRequested(object sender, GridColumnReorderRequestedEventArgs e)
        {
            if (e == null ||
                string.IsNullOrWhiteSpace(e.ColumnKey) ||
                string.IsNullOrWhiteSpace(e.TargetColumnKey))
            {
                return;
            }

            ReorderColumn(e.ColumnKey, e.TargetColumnKey);
        }

        private void HandleSurfaceColumnGroupingDragRequested(object sender, GridColumnGroupingDragRequestedEventArgs e)
        {
            if (e == null ||
                !AllowsGroupingDrag ||
                e.PointerKind != GridPointerKind.Mouse ||
                string.IsNullOrWhiteSpace(e.ColumnKey))
            {
                return;
            }

            if (_visibleColumns.All(column => !string.Equals(column.ColumnId, e.ColumnKey, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            try
            {
                var dragData = new DataObject();
                dragData.SetData(GroupingDragFormat, e.ColumnKey);
                DragDrop.DoDragDrop(SurfaceHost, dragData, DragDropEffects.Move);
            }
            catch (InvalidOperationException)
            {
                // DoDragDrop can throw when platform state blocks drag startup; fail gracefully.
            }
        }

        private void ToggleColumnSort(string columnId, UniversalModifierKeys modifiers)
        {
            if (string.IsNullOrWhiteSpace(columnId))
            {
                return;
            }

            _sortDescriptors = _sortInteractionController.ToggleSort(
                _sortDescriptors,
                columnId,
                modifiers).ToArray();

            RefreshSortIndicators();
            SyncSortsProperty();
            RefreshRowsView();
            RaiseViewStateChanged();
        }

        private UniversalModifierKeys MapSurfaceSortModifiers(GridInputModifiers modifiers)
        {
            var universalModifiers = UniversalModifierKeys.None;
            if ((modifiers & GridInputModifiers.Shift) != 0)
            {
                universalModifiers |= UniversalModifierKeys.Shift;
            }

            if ((modifiers & GridInputModifiers.Control) != 0)
            {
                universalModifiers |= UniversalModifierKeys.Control;
            }

            if ((modifiers & GridInputModifiers.Alt) != 0)
            {
                universalModifiers |= UniversalModifierKeys.Alt;
            }

            if ((modifiers & GridInputModifiers.Super) != 0)
            {
                universalModifiers |= UniversalModifierKeys.Windows;
            }

            return universalModifiers;
        }

        internal void AddColumnGrouping(string columnId)
        {
            if (string.IsNullOrWhiteSpace(columnId))
            {
                return;
            }

            _groupDescriptors = _groupingController.ApplyDrop(
                _groupDescriptors,
                new GridGroupingDragPayload(columnId),
                GridGroupingDropTarget.GroupingPanel);

            _collapseGroupsOnNextRefresh = _groupDescriptors.Count > 0;
            RebuildGroupChips();
            RefreshRowsView();
            SyncGroupsProperty();
            QueueHeaderVisualRefresh();
            RaiseViewStateChanged();
        }

        internal void RemoveColumnGrouping(string columnId)
        {
            if (string.IsNullOrWhiteSpace(columnId))
            {
                return;
            }

            _groupDescriptors = _groupingController.ApplyDrop(
                _groupDescriptors,
                new GridGroupingDragPayload(columnId),
                GridGroupingDropTarget.RemoveGrouping);

            _collapseGroupsOnNextRefresh = _groupDescriptors.Count > 0;
            RebuildGroupChips();
            RefreshRowsView();
            SyncGroupsProperty();
            QueueHeaderVisualRefresh();
            RaiseViewStateChanged();
        }

        public bool FocusColumnFilter(string columnId)
        {
            if (string.IsNullOrWhiteSpace(columnId) || FilterItemsControl == null)
            {
                return false;
            }

            var filterBox = FindDescendant<TextBox>(
                FilterItemsControl,
                textBox => textBox.DataContext is GridColumnBindingModel model &&
                           string.Equals(model.ColumnId, columnId, StringComparison.OrdinalIgnoreCase));
            if (filterBox == null)
            {
                return false;
            }

            EnsureColumnFilterVisible(columnId);
            filterBox.Focus();
            filterBox.SelectAll();
            return true;
        }

        private void HandleHeaderContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var headerSource = e.OriginalSource as DependencyObject ?? sender as DependencyObject;
            if (!TryOpenColumnHeaderContextMenu(headerSource, pointerPosition: null))
            {
                return;
            }

            e.Handled = true;
        }

        private void HandleHeaderPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e == null || e.ChangedButton != MouseButton.Right)
            {
                return;
            }

            var headerSource = e.OriginalSource as DependencyObject ?? e.Source as DependencyObject;
            var pointerPosition = SurfaceHost != null
                ? (Point?)e.GetPosition(SurfaceHost)
                : null;
            if (!TryOpenColumnHeaderContextMenu(headerSource, pointerPosition))
            {
                return;
            }

            e.Handled = true;
        }

        private void HandleOpenColumnMenu(object parameter)
        {
            var button = parameter as FrameworkElement;
            var column = button?.DataContext as GridColumnBindingModel;
            if (button == null || column == null)
            {
                return;
            }

            var contextMenu = BuildColumnContextMenu(column, button);
            button.ContextMenu = contextMenu;
            contextMenu.IsOpen = true;
        }

        private void HandleGridOptionsButtonClick(object sender, RoutedEventArgs e)
        {
            var button = sender as FrameworkElement;
            if (button == null)
            {
                return;
            }

            var contextMenu = BuildGridOptionsContextMenu(button);
            button.ContextMenu = contextMenu;
            contextMenu.IsOpen = true;
        }

        private ContextMenu BuildGridOptionsContextMenu(FrameworkElement placementTarget)
        {
            var popupLayout = ResolveColumnContextMenuLayout(placementTarget);
            var contextMenu = new ContextMenu
            {
                PlacementTarget = placementTarget,
                Placement = popupLayout.Placement,
                MaxHeight = popupLayout.MaxHeight,
                Style = TryFindResource("PgGridRegionsContextMenuStyle") as Style,
            };

            contextMenu.Items.Add(CreateGridOptionsSectionHeader("Regions"));
            contextMenu.Items.Add(CreateGridOptionsRegionToggleCheckBox(GridRegionKind.TopCommandRegion, "Command strip"));
            contextMenu.Items.Add(CreateGridOptionsRegionToggleCheckBox(GridRegionKind.GroupingRegion, "Grouping"));
            contextMenu.Items.Add(CreateGridOptionsRegionToggleCheckBox(GridRegionKind.SummaryBottomRegion, SummaryRegionTitleText));
            contextMenu.Items.Add(CreateGridOptionsRegionToggleCheckBox(GridRegionKind.SideToolRegion, ToolsRegionTitleText));
            contextMenu.Items.Add(CreateGridOptionsRegionToggleCheckBox(GridRegionKind.SummaryDesignerRegion, SummaryDesignerTitleText));
            return contextMenu;
        }

        internal ContextMenu CreateGridOptionsContextMenuForPlacementTarget(FrameworkElement placementTarget)
        {
            return BuildGridOptionsContextMenu(placementTarget);
        }

        private ContextMenu BuildColumnContextMenu(GridColumnBindingModel column, FrameworkElement placementTarget)
        {
            var popupLayout = ResolveColumnContextMenuLayout(placementTarget);
            var contextMenu = new ContextMenu
            {
                PlacementTarget = placementTarget,
                Placement = popupLayout.Placement,
                MaxHeight = popupLayout.MaxHeight,
                Style = TryFindResource("PgColumnContextMenuStyle") as Style,
            };

            contextMenu.Items.Add(CreateColumnMenuItem(SortAscendingText, () => ApplyColumnSort(column.ColumnId, GridSortDirection.Ascending, false)));
            contextMenu.Items.Add(CreateColumnMenuItem(SortDescendingText, () => ApplyColumnSort(column.ColumnId, GridSortDirection.Descending, false)));
            contextMenu.Items.Add(CreateColumnMenuSeparator());
            contextMenu.Items.Add(CreateColumnMenuItem(SortAddAscendingText, () => ApplyColumnSort(column.ColumnId, GridSortDirection.Ascending, true)));
            contextMenu.Items.Add(CreateColumnMenuItem(SortAddDescendingText, () => ApplyColumnSort(column.ColumnId, GridSortDirection.Descending, true)));
            contextMenu.Items.Add(CreateColumnMenuItem(
                SortClearText,
                () => ClearColumnSort(column.ColumnId),
                IsColumnSorted(column.ColumnId)));
            contextMenu.Items.Add(CreateColumnMenuSeparator());

            var isGrouped = IsColumnGrouped(column.ColumnId);
            contextMenu.Items.Add(CreateColumnMenuItem(
                isGrouped ? RemoveFromGroupingText : GroupByColumnText,
                () =>
                {
                    if (isGrouped)
                    {
                        RemoveColumnGrouping(column.ColumnId);
                    }
                    else
                    {
                        AddColumnGrouping(column.ColumnId);
                    }
                }));
            contextMenu.Items.Add(CreateColumnMenuItem(
                ToggleColumnGroupingDirectionText,
                () => HandleToggleGroupingDirection(column.ColumnId),
                isGrouped));
            contextMenu.Items.Add(CreateColumnMenuSeparator());
            contextMenu.Items.Add(CreateColumnMenuItem(FilterEditText, () => FocusFilterEditor(column.ColumnId)));
            contextMenu.Items.Add(CreateColumnMenuItem(
                GetText(GridTextKeys.FilterClear),
                () => ClearColumnFilter(column.ColumnId),
                column.HasFilterText));
            contextMenu.Items.Add(CreateColumnMenuSeparator());

            if (!AllowsPointerColumnReorder || !AllowsPointerColumnResize)
            {
                contextMenu.Items.Add(CreateColumnMenuItem(
                    MoveColumnLeftText,
                    () => MoveColumnLeft(column.ColumnId),
                    CanMoveVisibleColumn(column.ColumnId, -1)));
                contextMenu.Items.Add(CreateColumnMenuItem(
                    MoveColumnRightText,
                    () => MoveColumnRight(column.ColumnId),
                    CanMoveVisibleColumn(column.ColumnId, 1)));
                contextMenu.Items.Add(CreateColumnMenuItem(
                    WidenColumnText,
                    () => WidenColumn(column.ColumnId)));
                contextMenu.Items.Add(CreateColumnMenuItem(
                    NarrowColumnText,
                    () => NarrowColumn(column.ColumnId),
                    CanNarrowColumn(column.ColumnId)));
                contextMenu.Items.Add(CreateColumnMenuSeparator());
            }

            contextMenu.Items.Add(CreateColumnMenuItem(
                AutoFitColumnText,
                () => AutoFitColumn(column.ColumnId),
                CanAutoFitColumn(column.ColumnId)));
            contextMenu.Items.Add(CreateColumnMenuSeparator());

            var isFrozen = IsColumnFrozen(column.ColumnId);
            contextMenu.Items.Add(CreateColumnMenuItem(
                isFrozen ? UnfreezeColumnText : FreezeColumnText,
                () => SetColumnFrozen(column.ColumnId, !isFrozen)));
            contextMenu.Items.Add(CreateColumnMenuItem(
                HideColumnText,
                () => SetColumnVisibility(column.ColumnId, false),
                _visibleColumns.Count > 1));
            return contextMenu;
        }

        private static GridPopupLayoutMetrics ResolveColumnContextMenuLayout(FrameworkElement placementTarget)
        {
            var workArea = SystemParameters.WorkArea;
            if (placementTarget == null || !placementTarget.IsLoaded)
            {
                return new GridPopupLayoutMetrics(PlacementMode.Bottom, Math.Max(0d, workArea.Height - 32d));
            }

            var targetTopLeft = TransformPointFromDevice(placementTarget, placementTarget.PointToScreen(new Point(0d, 0d)));
            var targetBottomRight = TransformPointFromDevice(
                placementTarget,
                placementTarget.PointToScreen(new Point(placementTarget.ActualWidth, placementTarget.ActualHeight)));

            var availableAbove = Math.Max(0d, targetTopLeft.Y - workArea.Top);
            var availableBelow = Math.Max(0d, workArea.Bottom - targetBottomRight.Y);

            var metrics = GridPopupLayoutConfiguration.ResolveContextMenuLayout(availableAbove, availableBelow);
            if (metrics.MaxHeight > 0d)
            {
                return metrics;
            }

            return new GridPopupLayoutMetrics(PlacementMode.Bottom, Math.Max(0d, workArea.Height - 32d));
        }

        private static Point TransformPointFromDevice(Visual visual, Point point)
        {
            var source = PresentationSource.FromVisual(visual);
            if (source?.CompositionTarget == null)
            {
                return point;
            }

            return source.CompositionTarget.TransformFromDevice.Transform(point);
        }

        private MenuItem CreateColumnMenuItem(string header, Action callback, bool isEnabled = true)
        {
            var menuItem = new MenuItem
            {
                Header = header,
                IsEnabled = isEnabled,
                Style = TryFindResource("PgColumnContextMenuItemStyle") as Style,
            };
            menuItem.Click += (sender, args) => callback();
            return menuItem;
        }

        private MenuItem CreateGridOptionsColumnsSubmenuItem()
        {
            var menuItem = new MenuItem
            {
                Header = GetText(GridTextKeys.OptionsShowColumns),
                Style = TryFindResource("PgColumnContextMenuItemStyle") as Style,
            };

            var columnItems = new List<MenuItem>();
            var showAllColumnsItem = CreateColumnMenuItem(
                GetText(GridTextKeys.ColumnsShowAll),
                () =>
                {
                    ShowAllColumns();
                    foreach (var columnItem in columnItems)
                    {
                        columnItem.IsChecked = true;
                    }
                });
            showAllColumnsItem.StaysOpenOnClick = true;
            menuItem.Items.Add(showAllColumnsItem);

            foreach (var column in GetGridOptionsColumnChooserColumns())
            {
                var canToggleVisibility = !column.IsVisible || GetVisibleColumnCount() > 1;
                var columnItem = CreateGridOptionsToggleMenuItem(
                    column.Header,
                    column.IsVisible,
                    () => SetColumnVisibility(column.Id, !column.IsVisible),
                    canToggleVisibility);
                columnItem.StaysOpenOnClick = true;
                columnItems.Add(columnItem);
                menuItem.Items.Add(columnItem);
            }

            return menuItem;
        }

        private CheckBox CreateGridOptionsRegionToggleCheckBox(GridRegionKind regionKind, string header)
        {
            var regionState = GetChromeState(regionKind);
            var checkBox = new CheckBox
            {
                Content = header,
                IsChecked = regionState.IsVisible,
                IsEnabled = regionState.CanClose || !regionState.IsVisible,
                Style = TryFindResource("PgGridRegionsContextMenuCheckBoxStyle") as Style,
            };

            checkBox.Click += (sender, args) =>
            {
                SetRegionVisibility(regionKind, checkBox.IsChecked == true);
            };
            return checkBox;
        }

        private IReadOnlyList<GridColumnDefinition> GetGridOptionsColumnChooserColumns()
        {
            if (_layoutState == null)
            {
                return Array.Empty<GridColumnDefinition>();
            }

            return _layoutState.Columns
                .OrderBy(column => column.DisplayIndex)
                .ThenBy(column => column.Header, StringComparer.CurrentCulture)
                .ToArray();
        }

        private int GetVisibleColumnCount()
        {
            return _layoutState?.Columns.Count(column => column.IsVisible) ?? 0;
        }

        private MenuItem CreateGridOptionsToggleMenuItem(string header, bool isChecked, Action callback, bool isEnabled = true, object icon = null)
        {
            return CreateGridOptionsMenuItem(header, isChecked, callback, isEnabled, icon, "toggle");
        }

        private MenuItem CreateGridOptionsRadioMenuItem(string header, bool isChecked, Action callback, bool isEnabled = true)
        {
            var menuItem = CreateGridOptionsMenuItem(header, isChecked, callback, isEnabled, icon: null, stateKind: "radio");
            menuItem.Margin = new Thickness(22, 1, 2, 1);
            return menuItem;
        }

        private MenuItem CreateGridOptionsMenuItem(string header, bool isChecked, Action callback, bool isEnabled, object icon, object stateKind)
        {
            var menuItem = new MenuItem
            {
                Header = header,
                Icon = icon,
                Tag = stateKind,
                IsCheckable = true,
                IsChecked = isChecked,
                IsEnabled = isEnabled,
                Style = TryFindResource("PgGridOptionsContextMenuItemStyle") as Style,
            };
            menuItem.Click += (sender, args) => callback();
            return menuItem;
        }

        private FrameworkElement CreateGridOptionsSectionHeader(string title)
        {
            var textBlock = new TextBlock
            {
                Text = title ?? string.Empty,
                Style = TryFindResource("PgGridRegionsContextMenuHeaderTextStyle") as Style,
            };

            return new Border
            {
                Child = textBlock,
                SnapsToDevicePixels = true,
                IsHitTestVisible = false,
            };
        }

        private object BuildGridOptionsMenuIcon(GridOptionsMenuIconKind kind)
        {
            switch (kind)
            {
                case GridOptionsMenuIconKind.CurrentRowIndicator:
                    return BuildTriangleMenuIcon();
                case GridOptionsMenuIconKind.MultiSelect:
                    return BuildCheckboxMenuIcon();
                case GridOptionsMenuIconKind.ShowRowNumbers:
                    return BuildListMenuIcon();
                case GridOptionsMenuIconKind.GlobalNumbering:
                    return BuildMenuGlyphText("1 2 3");
                case GridOptionsMenuIconKind.WithinGroupNumbering:
                    return BuildMenuGlyphText("1 | 1");
                case GridOptionsMenuIconKind.CellSelection:
                    return BuildCellSelectionMenuIcon();
                case GridOptionsMenuIconKind.RangeSelection:
                    return BuildRangeSelectionMenuIcon();
                default:
                    return BuildMenuGlyphText(string.Empty);
            }
        }

        private FrameworkElement BuildMenuGlyphText(string glyph)
        {
            var textBlock = new TextBlock
            {
                Text = glyph,
                Width = 18,
                FontSize = 10,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
            };
            textBlock.SetResourceReference(TextBlock.ForegroundProperty, "PgMutedTextBrush");
            return textBlock;
        }

        private FrameworkElement BuildTriangleMenuIcon()
        {
            var host = CreateMenuIconHost();
            var triangle = new Polygon
            {
                Width = 8,
                Height = 10,
                Stretch = Stretch.Fill,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Points = new PointCollection
                {
                    new Point(0, 0),
                    new Point(8, 5),
                    new Point(0, 10),
                },
            };
            triangle.SetResourceReference(Shape.FillProperty, "PgMutedTextBrush");
            host.Children.Add(triangle);
            return host;
        }

        private FrameworkElement BuildListMenuIcon()
        {
            var host = CreateMenuIconHost();
            host.Children.Add(BuildListPath("M 4 4 L 16 4 M 4 9 L 16 9 M 4 14 L 16 14"));

            foreach (var top in new[] { 3d, 8d, 13d })
            {
                var dot = new Ellipse
                {
                    Width = 2.5,
                    Height = 2.5,
                    Margin = new Thickness(1.5, top, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                };
                dot.SetResourceReference(Shape.FillProperty, "PgMutedTextBrush");
                host.Children.Add(dot);
            }

            return host;
        }

        private FrameworkElement BuildCheckboxMenuIcon()
        {
            var host = CreateMenuIconHost();
            var box = new Rectangle
            {
                Width = 11,
                Height = 11,
                RadiusX = 1,
                RadiusY = 1,
                StrokeThickness = 1,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            box.SetResourceReference(Shape.StrokeProperty, "PgMutedTextBrush");

            var check = new Path
            {
                Data = Geometry.Parse("M 4.5 9 L 7 11.5 L 12.5 6"),
                StrokeThickness = 1.6,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                StrokeLineJoin = PenLineJoin.Round,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Stretch = Stretch.Fill,
                Margin = new Thickness(0),
                IsHitTestVisible = false,
            };
            check.SetResourceReference(Path.StrokeProperty, "PgMutedTextBrush");

            host.Children.Add(box);
            host.Children.Add(check);
            return host;
        }

        private FrameworkElement BuildCellSelectionMenuIcon()
        {
            var host = CreateMenuIconHost();
            var outline = new Rectangle
            {
                Width = 12,
                Height = 12,
                StrokeThickness = 1,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            outline.SetResourceReference(Shape.StrokeProperty, "PgMutedTextBrush");

            var divider = BuildListPath("M 9 3 L 9 15 M 3 9 L 15 9");
            divider.SetResourceReference(Path.StrokeProperty, "PgMutedTextBrush");

            var activeCell = new Rectangle
            {
                Width = 4,
                Height = 4,
                Margin = new Thickness(3, 3, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
            };
            activeCell.SetResourceReference(Shape.FillProperty, "PgAccentBrush");

            host.Children.Add(outline);
            host.Children.Add(divider);
            host.Children.Add(activeCell);
            return host;
        }

        private FrameworkElement BuildRangeSelectionMenuIcon()
        {
            var host = CreateMenuIconHost();
            var outline = new Rectangle
            {
                Width = 12,
                Height = 12,
                StrokeThickness = 1.2,
                StrokeDashArray = new DoubleCollection { 2, 1 },
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            outline.SetResourceReference(Shape.StrokeProperty, "PgMutedTextBrush");

            var selection = new Rectangle
            {
                Width = 6,
                Height = 6,
                Fill = Brushes.Transparent,
                StrokeThickness = 1,
                Margin = new Thickness(6, 6, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
            };
            selection.SetResourceReference(Shape.StrokeProperty, "PgAccentBrush");

            host.Children.Add(outline);
            host.Children.Add(selection);
            return host;
        }

        private Grid CreateMenuIconHost()
        {
            return new Grid
            {
                Width = 18,
                Height = 18,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                IsHitTestVisible = false,
            };
        }

        private Path BuildListPath(string geometry)
        {
            var path = new Path
            {
                Data = Geometry.Parse(geometry),
                StrokeThickness = 1.3,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Stretch = Stretch.Fill,
                Margin = new Thickness(0),
                IsHitTestVisible = false,
            };
            path.SetResourceReference(Path.StrokeProperty, "PgMutedTextBrush");
            return path;
        }

        private enum GridOptionsMenuIconKind
        {
            CurrentRowIndicator,
            MultiSelect,
            ShowRowNumbers,
            GlobalNumbering,
            WithinGroupNumbering,
            CellSelection,
            RangeSelection,
        }

        private Separator CreateColumnMenuSeparator()
        {
            return new Separator
            {
                Style = TryFindResource("PgColumnContextMenuSeparatorStyle") as Style,
            };
        }

        private bool IsColumnSorted(string columnId)
        {
            return _sortDescriptors.Any(sort => string.Equals(sort.ColumnId, columnId, StringComparison.OrdinalIgnoreCase));
        }

        private bool IsColumnGrouped(string columnId)
        {
            return _groupDescriptors.Any(group => string.Equals(group.ColumnId, columnId, StringComparison.OrdinalIgnoreCase));
        }

        private bool IsColumnFrozen(string columnId)
        {
            return (_layoutState?.Columns ?? Array.Empty<GridColumnDefinition>())
                .Any(column => string.Equals(column.Id, columnId, StringComparison.OrdinalIgnoreCase) && column.IsFrozen);
        }

        private void HandleToggleGroupingDirection(string columnId)
        {
            if (!IsColumnGrouped(columnId))
            {
                return;
            }

            _groupDescriptors = _groupingController.ToggleDirection(_groupDescriptors, columnId);
            _collapseGroupsOnNextRefresh = _groupDescriptors.Count > 0;
            RebuildGroupChips();
            RefreshRowsView();
            SyncGroupsProperty();
            QueueHeaderVisualRefresh();
            RaiseViewStateChanged();
        }

        private void ClearColumnFilter(string columnId)
        {
            var column = _visibleColumns.FirstOrDefault(
                candidate => string.Equals(candidate.ColumnId, columnId, StringComparison.OrdinalIgnoreCase));
            if (column != null && !string.IsNullOrEmpty(column.FilterText))
            {
                column.FilterText = string.Empty;
            }
        }

        private void FocusFilterEditor(string columnId)
        {
            Dispatcher.BeginInvoke(
                new Action(() =>
                {
                    FocusColumnFilter(columnId);
                }),
                DispatcherPriority.Input);
        }

        private void RefreshSelectionCounters()
        {
            var snapshot = _surfaceCoordinator.GetCurrentSnapshot();
            UpdateSurfaceSelectionCounters(snapshot);
            OnPropertyChanged(nameof(HasSelectedRows));
        }

        private void UpdateSurfaceSelectionCounters(GridSurfaceSnapshot snapshot)
        {
            if (snapshot == null)
            {
                _selectedCellCount = 0;
                _selectedRowCount = 0;
                return;
            }

            var cellSelectionRegion = snapshot.SelectionRegions
                .FirstOrDefault(region => region.Unit == global::PhialeGrid.Core.Surface.GridSelectionUnit.Cell &&
                                          region.SelectedKeys != null &&
                                          region.SelectedKeys.Count > 0);

            if (cellSelectionRegion != null)
            {
                _selectedCellCount = cellSelectionRegion.SelectedKeys.Count;
            }
            else
            {
                _selectedCellCount = snapshot.Cells
                    .Where(cell => cell.IsSelected)
                    .Where(cell => _surfaceRowsByKey.TryGetValue(cell.RowKey, out var row) && IsSelectableSurfaceRow(row))
                    .Count();
            }

            var selectedRowsRegion = snapshot.SelectionRegions
                .FirstOrDefault(region => region.Unit == global::PhialeGrid.Core.Surface.GridSelectionUnit.Row &&
                                          region.SelectedKeys != null &&
                                          region.SelectedKeys.Count > 0);

            if (selectedRowsRegion != null)
            {
                _selectedRowCount = selectedRowsRegion.SelectedKeys
                    .Count(rowKey => _surfaceRowsByKey.TryGetValue(rowKey, out var row) && IsSelectableSurfaceRow(row));
                return;
            }

            if (cellSelectionRegion != null)
            {
                _selectedRowCount = ResolveSelectedRowCountFromCellKeys(snapshot, cellSelectionRegion.SelectedKeys);
                return;
            }

            _selectedRowCount = snapshot.Cells
                .Where(cell => cell.IsSelected)
                .Where(cell => _surfaceRowsByKey.TryGetValue(cell.RowKey, out var row) && IsSelectableSurfaceRow(row))
                .Select(cell => cell.RowKey)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();
        }

        private int ResolveSelectedRowCountFromCellKeys(
            GridSurfaceSnapshot snapshot,
            IReadOnlyList<string> selectedCellKeys)
        {
            if (snapshot == null || selectedCellKeys == null || selectedCellKeys.Count == 0)
            {
                return 0;
            }

            var columnSuffixes = snapshot.Columns
                .Select(column => column.ColumnKey)
                .Where(columnKey => !string.IsNullOrWhiteSpace(columnKey))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(columnKey => "_" + columnKey)
                .OrderByDescending(suffix => suffix.Length)
                .ToArray();

            if (columnSuffixes.Length == 0)
            {
                return 0;
            }

            var selectedRows = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var cellKey in selectedCellKeys)
            {
                if (string.IsNullOrWhiteSpace(cellKey))
                {
                    continue;
                }

                foreach (var suffix in columnSuffixes)
                {
                    if (!cellKey.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var rowKey = cellKey.Substring(0, cellKey.Length - suffix.Length);
                    if (_surfaceRowsByKey.TryGetValue(rowKey, out var row) && IsSelectableSurfaceRow(row))
                    {
                        selectedRows.Add(rowKey);
                    }

                    break;
                }
            }

            return selectedRows.Count;
        }

        private static bool IsSelectableRowItem(object rowItem)
        {
            return !(rowItem is GridGroupHeaderRowModel) &&
                   !(rowItem is GridMasterDetailHeaderRowModel) &&
                   !(rowItem is GridSurfaceMasterDetailDetailsHostRowModel) &&
                   !(rowItem is GridHierarchyLoadMoreRowModel) &&
                   !(rowItem is GridLoadingRowModel);
        }

        private IEnumerable<string> ResolveSelectableSurfaceRowKeys()
        {
            return _surfaceRowsByKey
                .Where(entry => IsSelectableSurfaceRow(entry.Value))
                .Select(entry => entry.Key);
        }

        private bool TryResolveOrRevealSurfaceRowKey(string rowId, out string rowKey)
        {
            rowKey = ResolveSurfaceRowKeys(new[] { rowId }).FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(rowKey))
            {
                return true;
            }

            if (!TryExpandGroupsToRevealRow(rowId))
            {
                return false;
            }

            if (ShouldDeferVisualWorkBecauseBatched())
            {
                rowKey = rowId;
                return true;
            }

            rowKey = ResolveSurfaceRowKeys(new[] { rowId }).FirstOrDefault();
            return !string.IsNullOrWhiteSpace(rowKey);
        }

        private IEnumerable<string> ResolveSurfaceRowKeys(IEnumerable<string> rowIds)
        {
            var lookup = new HashSet<string>(
                (rowIds ?? Array.Empty<string>()).Where(rowId => !string.IsNullOrWhiteSpace(rowId)),
                StringComparer.OrdinalIgnoreCase);

            return _surfaceRowsByKey
                .Where(entry => IsSelectableSurfaceRow(entry.Value))
                .Where(entry => lookup.Contains(ResolveRowId(entry.Value)))
                .Select(entry => entry.Key)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private bool TryExpandGroupsToRevealRow(string rowId)
        {
            if (string.IsNullOrWhiteSpace(rowId) || _groupDescriptors.Count == 0)
            {
                return false;
            }

            var row = ResolveFilteredRowForIndicators(rowId);
            if (row == null)
            {
                return false;
            }

            var changed = false;
            foreach (var groupId in BuildContainingGroupIds(row))
            {
                if (_groupExpansionState.IsExpanded(groupId))
                {
                    continue;
                }

                _groupExpansionState.SetExpanded(groupId, true);
                changed = true;
            }

            if (changed)
            {
                RefreshRowsView();
            }

            return changed;
        }

        private int ResolveColumnDisplayIndex(string columnKey)
        {
            if (string.IsNullOrWhiteSpace(columnKey))
            {
                return int.MaxValue;
            }

            var column = _visibleColumns.FirstOrDefault(candidate =>
                string.Equals(candidate.ColumnId, columnKey, StringComparison.OrdinalIgnoreCase));
            return column?.Definition.DisplayIndex ?? int.MaxValue;
        }

        private GridColumnBindingModel ResolveColumnFromHeader(object sender)
        {
            if (sender is FrameworkElement element)
            {
                var boundColumn = element.DataContext as GridColumnBindingModel;
                if (boundColumn != null)
                {
                    return boundColumn;
                }

                if (element.DataContext is GridHeaderSurfaceItem headerDataFromDataContext &&
                    !string.IsNullOrWhiteSpace(headerDataFromDataContext.HeaderKey))
                {
                    return _visibleColumns.FirstOrDefault(candidate =>
                        string.Equals(candidate.ColumnId, headerDataFromDataContext.HeaderKey, StringComparison.OrdinalIgnoreCase));
                }
            }

            if (sender is GridColumnHeaderPresenter headerPresenter &&
                headerPresenter.HeaderData != null &&
                !string.IsNullOrWhiteSpace(headerPresenter.HeaderData.HeaderKey))
            {
                return _visibleColumns.FirstOrDefault(candidate =>
                    string.Equals(candidate.ColumnId, headerPresenter.HeaderData.HeaderKey, StringComparison.OrdinalIgnoreCase));
            }

            return null;
        }

        private bool TryOpenColumnHeaderContextMenu(DependencyObject headerSource, Point? pointerPosition)
        {
            var header = ResolveColumnHeaderPresenter(headerSource, pointerPosition);
            var column = ResolveColumnFromHeader(header);
            if (header == null || column == null)
            {
                return false;
            }

            var contextMenu = BuildColumnContextMenu(column, header);
            header.ContextMenu = contextMenu;
            contextMenu.IsOpen = true;
            return true;
        }

        private GridColumnHeaderPresenter ResolveColumnHeaderPresenter(DependencyObject headerSource, Point? pointerPosition)
        {
            var header = FindAncestor<GridColumnHeaderPresenter>(headerSource) ?? headerSource as GridColumnHeaderPresenter;
            if (header != null)
            {
                return header;
            }

            if (!pointerPosition.HasValue || SurfaceHost?.CurrentSnapshot?.Headers == null)
            {
                return null;
            }

            var columnHeader = SurfaceHost.CurrentSnapshot.Headers.FirstOrDefault(candidate =>
                candidate.Kind == GridHeaderKind.ColumnHeader &&
                pointerPosition.Value.X >= candidate.Bounds.Left &&
                pointerPosition.Value.X <= candidate.Bounds.Right &&
                pointerPosition.Value.Y >= candidate.Bounds.Top &&
                pointerPosition.Value.Y <= candidate.Bounds.Bottom);
            if (columnHeader == null)
            {
                return null;
            }

            return FindDescendant<GridColumnHeaderPresenter>(
                SurfaceHost,
                presenter => presenter.HeaderData != null &&
                             string.Equals(presenter.HeaderData.HeaderKey, columnHeader.HeaderKey, StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsSelectableSurfaceRow(object row)
        {
            if (row == null)
            {
                return false;
            }

            if (row is GridGroupFlatRow<object> groupedRow)
            {
                return groupedRow.Kind == GridGroupFlatRowKind.DataRow;
            }

            return IsSelectableRowItem(row);
        }

        private void QueueHeaderVisualRefresh()
        {
            if (SurfaceHost == null || !IsLoaded)
            {
                return;
            }

            Dispatcher.BeginInvoke(
                new Action(() =>
                {
                    if (SurfaceHost == null)
                    {
                        return;
                    }

                    SurfaceHost.InvalidateMeasure();
                    SurfaceHost.InvalidateArrange();
                    SurfaceHost.InvalidateVisual();
                }),
                DispatcherPriority.Loaded);
        }

        private GridViewportState ResolveSurfaceViewportState()
        {
            return SurfaceHost?.CurrentSnapshot?.ViewportState ?? _surfaceCoordinator.GetCurrentSnapshot()?.ViewportState;
        }

        private void RaiseSurfaceGeometryPropertyChanges()
        {
            OnPropertyChanged(nameof(SurfaceColumnHeaderHeight));
            OnPropertyChanged(nameof(SurfaceFilterRowHeight));
            OnPropertyChanged(nameof(SurfaceTopChromeHeight));
            OnPropertyChanged(nameof(SurfaceVerticalScrollBarGutterWidth));
            OnPropertyChanged(nameof(SurfaceHorizontalScrollBarGutterHeight));
        }

        private void HandleGroupingBandRemoveGroupRequested(object sender, PhialeGroupingBandColumnEventArgs e)
        {
            RemoveColumnGrouping(e.ColumnId);
            e.Handled = true;
        }

        private void HandleGroupingBandToggleDirectionRequested(object sender, PhialeGroupingBandColumnEventArgs e)
        {
            HandleToggleGroupingDirection(e.ColumnId);
            e.Handled = true;
        }

        private void HandleGroupingBandColumnDropped(object sender, PhialeGroupingBandColumnEventArgs e)
        {
            _groupDescriptors = _groupingController.ApplyDrop(
                _groupDescriptors,
                new GridGroupingDragPayload(e.ColumnId),
                GridGroupingDropTarget.GroupingPanel);

            _collapseGroupsOnNextRefresh = _groupDescriptors.Count > 0;
            RebuildGroupChips();
            RefreshRowsView();
            SyncGroupsProperty();
            QueueHeaderVisualRefresh();
            e.Handled = true;
        }

        private void HandleExpandAllGroupsClick(object sender, RoutedEventArgs e)
        {
            ExpandAllGroups();
        }

        private void HandleCollapseAllGroupsClick(object sender, RoutedEventArgs e)
        {
            CollapseAllGroups();
        }

        private void TrackEditedRow(object row, string changedColumnId = null)
        {
            row = UnwrapEditableSurfaceRow(row);
            if (row == null)
            {
                return;
            }

            var rowId = ResolveRowId(row);
            if (!string.IsNullOrWhiteSpace(changedColumnId))
            {
                var value = ResolveRowValue(row, changedColumnId);
                _editSessionContext.TrySetFieldValue(rowId, changedColumnId, value, Convert.ToString(value, CultureInfo.CurrentCulture));
                return;
            }

            var hasChanges = _editSessionContext.HasRecordChanges(rowId);
            _editSessionContext.CompleteRecordEdit(rowId, hasChanges);
            RefreshSurfaceRowIndicators();
            RaiseStatusPropertyChanges();
        }

        private IReadOnlyList<string> ResolveChangedRowIds()
        {
            return _editSessionContext.EditedRecordIds.ToArray();
        }

        private IReadOnlyList<ChangePanelItem> ResolveChangePanelItems()
        {
            var rowIds = PendingEditRowIds;
            if (rowIds.Count == 0)
            {
                return Array.Empty<ChangePanelItem>();
            }

            var items = new List<ChangePanelItem>(rowIds.Count);
            foreach (var rowId in rowIds)
            {
                var row = FindCurrentRow(rowId);
                var description = row == null
                    ? rowId
                    : ResolveIndicatorRecordLabel(row);
                var displayRowText = ResolveChangePanelRowDisplayText(rowId, row);
                items.Add(new ChangePanelItem(rowId, ResolveChangePanelNavigationColumnId(row), displayRowText, description));
            }

            return items;
        }

        private string ResolveChangePanelRowDisplayText(string rowId, object row)
        {
            var rowNumber = ResolveCurrentViewDataRowNumber(rowId);
            if (rowNumber > 0)
            {
                return rowNumber.ToString(CultureInfo.CurrentCulture);
            }

            if (!string.IsNullOrWhiteSpace(rowId))
            {
                return rowId;
            }

            throw new InvalidOperationException("Change panel row display text requires a row id.");
        }

        private int ResolveCurrentViewDataRowNumber(string rowId)
        {
            if (string.IsNullOrWhiteSpace(rowId) || RowsView == null)
            {
                return 0;
            }

            var rowNumber = 0;
            foreach (var row in RowsView.Cast<object>())
            {
                if (!IsSelectableSurfaceRow(row))
                {
                    continue;
                }

                rowNumber++;
                if (string.Equals(ResolveRowId(row), rowId, StringComparison.OrdinalIgnoreCase))
                {
                    return rowNumber;
                }
            }

            return 0;
        }

        private string ResolveChangePanelNavigationColumnId(object row)
        {
            var candidateColumns = new[] { "ObjectName", "Name", "Id" };
            foreach (var columnId in candidateColumns)
            {
                if (HasChangePanelNavigationColumn(columnId))
                {
                    return columnId;
                }
            }

            var firstColumnId = ResolveChangePanelColumns()
                .Select(column => column.Id)
                .FirstOrDefault(columnId => !string.IsNullOrWhiteSpace(columnId));
            if (!string.IsNullOrWhiteSpace(firstColumnId))
            {
                return firstColumnId;
            }

            throw new InvalidOperationException("Change panel row navigation requires at least one column.");
        }

        private bool HasChangePanelNavigationColumn(string columnId)
        {
            return !string.IsNullOrWhiteSpace(columnId) &&
                ResolveChangePanelColumns().Any(column => string.Equals(column.Id, columnId, StringComparison.OrdinalIgnoreCase));
        }

        private IReadOnlyList<GridColumnDefinition> ResolveChangePanelColumns()
        {
            if (_visibleColumns.Count > 0)
            {
                return _visibleColumns
                    .Select(column => column.Definition)
                    .Where(column => column != null)
                    .ToArray();
            }

            if (_layoutState != null && _layoutState.Columns.Count > 0)
            {
                return _layoutState.Columns;
            }

            if (_baselineColumns.Count > 0)
            {
                return _baselineColumns;
            }

            return (_editSessionContext?.FieldDefinitions ?? Array.Empty<IEditSessionFieldDefinition>())
                .Select(field => new GridColumnDefinition(field.FieldId, field.DisplayName))
                .ToArray();
        }

        private bool HasRowValidationIssues(string rowId)
        {
            return !string.IsNullOrWhiteSpace(rowId) &&
                _editSessionContext.InvalidRecordIds.Contains(rowId);
        }

        private IReadOnlyList<string> ResolveValidationIssueRowIds()
        {
            return _editSessionContext.InvalidRecordIds.ToArray();
        }

        private IReadOnlyList<ValidationIssuePanelItem> ResolveValidationIssueItems()
        {
            var rowIds = OrderRowIdsByCurrentView(CurrentEditSessionContext?.InvalidRecordIds ?? Array.Empty<string>());
            if (rowIds.Count == 0)
            {
                return Array.Empty<ValidationIssuePanelItem>();
            }

            var items = new List<ValidationIssuePanelItem>();
            foreach (var rowId in rowIds)
            {
                var details = ResolveValidationDetails(rowId);
                if (details.Count == 0)
                {
                    throw new InvalidOperationException("Validation issue row '" + rowId + "' has no validation details.");
                }

                foreach (var detail in details)
                {
                    items.Add(new ValidationIssuePanelItem(rowId, detail.ColumnId, detail.DisplayName, detail.Message));
                }
            }

            return items;
        }

        private IReadOnlyList<RowValidationDetail> ResolveValidationDetails(string rowId)
        {
            if (string.IsNullOrWhiteSpace(rowId))
            {
                return Array.Empty<RowValidationDetail>();
            }

            return _editSessionContext.GetValidationDetails(rowId)
                .Select(detail => new RowValidationDetail(detail.FieldId, detail.DisplayName, detail.Message))
                .ToArray();
        }

        private string ResolveCurrentDataRowId()
        {
            var currentCell = _surfaceCoordinator.GetCurrentSnapshot()?.CurrentCell;
            if (currentCell == null ||
                !_surfaceRowsByKey.TryGetValue(currentCell.RowKey, out var row) ||
                !IsSelectableSurfaceRow(row))
            {
                return string.Empty;
            }

            return ResolveRowId(row);
        }

        private IReadOnlyList<string> OrderRowIdsByCurrentView(IEnumerable<string> rowIds)
        {
            var remaining = new HashSet<string>(
                (rowIds ?? Array.Empty<string>()).Where(rowId => !string.IsNullOrWhiteSpace(rowId)),
                StringComparer.OrdinalIgnoreCase);
            if (remaining.Count == 0)
            {
                return Array.Empty<string>();
            }

            var ordered = new List<string>(remaining.Count);

            if (RowsView != null)
            {
                foreach (var row in RowsView.Cast<object>())
                {
                    var rowId = ResolveRowId(row);
                    if (remaining.Remove(rowId))
                    {
                        ordered.Add(rowId);
                    }
                }
            }

            foreach (var row in EnumerateEffectiveItemsSource())
            {
                var rowId = ResolveRowId(row);
                if (remaining.Remove(rowId))
                {
                    ordered.Add(rowId);
                }
            }

            if (remaining.Count > 0)
            {
                ordered.AddRange(remaining);
            }

            return ordered;
        }

        private void SyncDirtyRowsWithSnapshots()
        {
            RefreshSurfaceRowIndicators();
        }

        private object ParseEditingValue(string rowKey, string columnKey, string editingText)
        {
            var text = editingText ?? string.Empty;
            var column = ResolveEditingColumn(columnKey);
            var declaredType = column?.ValueType ?? typeof(string);
            var targetType = Nullable.GetUnderlyingType(declaredType) ?? declaredType;

            if (targetType == typeof(string))
            {
                return text;
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                return Nullable.GetUnderlyingType(declaredType) != null ? null : text;
            }

            try
            {
                if (targetType == typeof(DateTime))
                {
                    if (DateTime.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.None, out var currentCultureDate) ||
                        DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out currentCultureDate))
                    {
                        return currentCultureDate;
                    }

                    return text;
                }

                if (targetType == typeof(DateTimeOffset))
                {
                    if (DateTimeOffset.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.None, out var currentCultureOffset) ||
                        DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out currentCultureOffset))
                    {
                        return currentCultureOffset;
                    }

                    return text;
                }

                if (targetType == typeof(TimeSpan))
                {
                    if (TimeSpan.TryParse(text, CultureInfo.CurrentCulture, out var currentCultureTime) ||
                        TimeSpan.TryParse(text, CultureInfo.InvariantCulture, out currentCultureTime))
                    {
                        return currentCultureTime;
                    }

                    return text;
                }

                if (targetType == typeof(int))
                {
                    if (int.TryParse(text, NumberStyles.Integer, CultureInfo.CurrentCulture, out var intValue) ||
                        int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out intValue))
                    {
                        return intValue;
                    }

                    return text;
                }

                if (targetType == typeof(bool))
                {
                    if (bool.TryParse(text, out var boolValue))
                    {
                        return boolValue;
                    }

                    if (string.Equals(text, "1", StringComparison.Ordinal))
                    {
                        return true;
                    }

                    if (string.Equals(text, "0", StringComparison.Ordinal))
                    {
                        return false;
                    }

                    return text;
                }

                if (targetType == typeof(long))
                {
                    if (long.TryParse(text, NumberStyles.Integer, CultureInfo.CurrentCulture, out var longValue) ||
                        long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out longValue))
                    {
                        return longValue;
                    }

                    return text;
                }

                if (targetType == typeof(decimal))
                {
                    if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out var decimalValue) ||
                        decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out decimalValue))
                    {
                        return decimalValue;
                    }

                    return text;
                }

                if (targetType == typeof(double))
                {
                    if (double.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out var doubleValue) ||
                        double.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out doubleValue))
                    {
                        return doubleValue;
                    }

                    return text;
                }

                if (targetType == typeof(float))
                {
                    if (float.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out var floatValue) ||
                        float.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out floatValue))
                    {
                        return floatValue;
                    }

                    return text;
                }

                if (targetType == typeof(short))
                {
                    if (short.TryParse(text, NumberStyles.Integer, CultureInfo.CurrentCulture, out var shortValue) ||
                        short.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out shortValue))
                    {
                        return shortValue;
                    }

                    return text;
                }

                if (targetType.IsEnum)
                {
                    try
                    {
                        return Enum.Parse(targetType, text, true);
                    }
                    catch
                    {
                        return text;
                    }
                }

                return Convert.ChangeType(text, targetType, CultureInfo.CurrentCulture);
            }
            catch
            {
                return text;
            }
        }

        private string ResolveColumnValueKind(GridColumnBindingModel column)
        {
            if (column == null)
            {
                return "Text";
            }

            if (!string.IsNullOrWhiteSpace(column.ValueKind))
            {
                return column.ValueKind;
            }

            var targetType = Nullable.GetUnderlyingType(column.ValueType) ?? column.ValueType ?? typeof(string);
            if (targetType == typeof(bool))
            {
                return "Boolean";
            }

            if (targetType == typeof(DateTime) || targetType == typeof(DateTimeOffset) || targetType == typeof(TimeSpan))
            {
                return "DateTime";
            }

            if (targetType.IsEnum)
            {
                return "Status";
            }

            if (IsNumericColumnType(targetType))
            {
                return "Number";
            }

            if (targetType == typeof(string) && LooksLikeCodeColumn(column.ColumnId, column.Header))
            {
                return "Code";
            }

            if (LooksLikeStatusColumn(column.ColumnId, column.Header))
            {
                return "Status";
            }

            return "Text";
        }

        private static bool LooksLikeStatusColumn(string columnId, string header)
        {
            return ContainsSemanticToken(columnId, "status", "priority", "state") ||
                ContainsSemanticToken(header, "status", "priority", "state");
        }

        private static bool LooksLikeCodeColumn(string columnId, string header)
        {
            return ContainsSemanticToken(columnId, "id", "code", "number", "document") ||
                ContainsSemanticToken(header, "id", "code", "number", "document");
        }

        private static bool ContainsSemanticToken(string text, params string[] candidates)
        {
            if (string.IsNullOrWhiteSpace(text) || candidates == null || candidates.Length == 0)
            {
                return false;
            }

            foreach (var candidate in candidates)
            {
                if (!string.IsNullOrWhiteSpace(candidate) &&
                    text.IndexOf(candidate, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsNumericColumnType(Type type)
        {
            return type == typeof(decimal) ||
                type == typeof(double) ||
                type == typeof(float) ||
                type == typeof(int) ||
                type == typeof(long) ||
                type == typeof(short) ||
                type == typeof(byte) ||
                type == typeof(uint) ||
                type == typeof(ulong) ||
                type == typeof(ushort);
        }

        private GridColumnBindingModel ResolveEditingColumn(string columnKey)
        {
            if (string.IsNullOrWhiteSpace(columnKey))
            {
                return null;
            }

            if (_surfaceColumnsByKey.TryGetValue(columnKey, out var surfaceColumn))
            {
                return surfaceColumn;
            }

            return _visibleColumns.FirstOrDefault(column =>
                string.Equals(column.ColumnId, columnKey, StringComparison.OrdinalIgnoreCase));
        }

        private IReadOnlyList<string> ValidateRow(object row)
        {
            return ValidateRowDetails(row)
                .Select(detail => detail.Message)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }

        private IReadOnlyList<RowValidationDetail> ValidateRowDetails(object row)
        {
            row = UnwrapEditableSurfaceRow(row);
            if (row == null)
            {
                return Array.Empty<RowValidationDetail>();
            }

            var rowId = ResolveRowId(row);
            return _editSessionContext.GetValidationDetails(rowId)
                .Select(detail => new RowValidationDetail(detail.FieldId, detail.DisplayName, detail.Message))
                .ToArray();
        }

        private bool UpdateValidationState(string rowId, object row)
        {
            if (string.IsNullOrWhiteSpace(rowId))
            {
                return false;
            }

            return _editSessionContext.InvalidRecordIds.Contains(rowId);
        }

        private void ClearValidationState(string rowId)
        {
            _ = rowId;
        }

        private IReadOnlyList<GridColumnDefinition> ResolveValidationColumns()
        {
            return _layoutState?.Columns ?? _baselineColumns ?? Array.Empty<GridColumnDefinition>();
        }

        private IReadOnlyList<GridValidationError> ValidateColumnConstraints(string columnId, object value, string editingText)
        {
            var rowId = ResolveCurrentDataRowId();
            if (string.IsNullOrWhiteSpace(rowId))
            {
                return Array.Empty<GridValidationError>();
            }

            return _editSessionContext.ValidateFieldValue(rowId, columnId, value, editingText);
        }

        private static void AddValidationDetail(ICollection<RowValidationDetail> details, string columnId, string displayName, string message)
        {
            if (details == null || string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            if (details.Any(detail =>
                string.Equals(detail.ColumnId, columnId ?? string.Empty, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(detail.Message, message, StringComparison.Ordinal)))
            {
                return;
            }

            details.Add(new RowValidationDetail(columnId, displayName, message));
        }

        private RowIndicatorProjection BuildRowIndicatorProjection(IEnumerable<KeyValuePair<string, object>> keyedRows)
        {
            var editedRowKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var invalidRowKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var toolTipSegments = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            if (keyedRows == null || (!_editSessionContext.HasPendingEdits && !_editSessionContext.HasValidationIssues))
            {
                return RowIndicatorProjection.Empty;
            }

            var visibleDataRowKeysById = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var visibleGroupRowKeysById = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var pair in keyedRows)
            {
                if (IsSelectableSurfaceRow(pair.Value))
                {
                    visibleDataRowKeysById[ResolveRowId(pair.Value)] = pair.Key;
                    continue;
                }

                if (TryResolveGroupId(pair.Value, out var groupId))
                {
                    visibleGroupRowKeysById[groupId] = pair.Key;
                }
            }

            foreach (var row in ResolveRowsForIndicatorProjection())
            {
                var rowId = ResolveRowId(row);
                var validationDetails = ResolveValidationDetails(rowId);
                var fieldChanges = _editSessionContext.GetFieldChanges(rowId);
                if (validationDetails.Count == 0 && fieldChanges.Count == 0)
                {
                    continue;
                }

                var anchorKey = ResolveIndicatorAnchorKey(row, rowId, visibleDataRowKeysById, visibleGroupRowKeysById);
                if (string.IsNullOrWhiteSpace(anchorKey))
                {
                    continue;
                }

                if (validationDetails.Count > 0)
                {
                    invalidRowKeys.Add(anchorKey);
                    AppendToolTipSegment(
                        toolTipSegments,
                        anchorKey,
                        BuildAnchoredIndicatorToolTip(row, BuildValidationToolTip(validationDetails)));
                    continue;
                }

                if (fieldChanges.Count == 0)
                {
                    continue;
                }

                editedRowKeys.Add(anchorKey);
                var editedToolTip = BuildEditedRowToolTip(fieldChanges);
                if (!string.IsNullOrWhiteSpace(editedToolTip))
                {
                    AppendToolTipSegment(
                        toolTipSegments,
                        anchorKey,
                        BuildAnchoredIndicatorToolTip(row, editedToolTip));
                }
            }

            var toolTips = toolTipSegments.ToDictionary(
                entry => entry.Key,
                entry => string.Join(Environment.NewLine + Environment.NewLine, entry.Value),
                StringComparer.OrdinalIgnoreCase);

            return new RowIndicatorProjection(
                editedRowKeys.ToArray(),
                invalidRowKeys.ToArray(),
                toolTips);
        }

        private void RefreshSurfaceRowIndicators()
        {
            var indicatorCounter = PhialeGridDiagnostics.IncrementGridCounter(GetDiagnosticsGridId(), "RefreshSurfaceRowIndicators");
            var stopwatch = Stopwatch.StartNew();
            if (ShouldDeferVisualWorkBecauseBatched())
            {
                _pendingRefreshSurfaceRowIndicatorsAfterBatch = true;
                stopwatch.Stop();
                LogDiagnostics($"RefreshSurfaceRowIndicators deferred because grid update batch is active. Count={indicatorCounter.Count}. {GetGridSessionDescription()}.");
                return;
            }

            if (ShouldDeferVisualWorkBecauseHidden())
            {
                _pendingRefreshSurfaceRowIndicatorsWhileHidden = true;
                stopwatch.Stop();
                LogDiagnostics($"RefreshSurfaceRowIndicators deferred because grid is hidden. Count={indicatorCounter.Count}. {GetGridSessionDescription()}.");
                return;
            }

            if (_surfaceRowsByKey == null || _surfaceRowsByKey.Count == 0)
            {
                stopwatch.Stop();
                LogDiagnostics($"RefreshSurfaceRowIndicators skipped because there are no surface rows. Count={indicatorCounter.Count}. {GetGridSessionDescription()}.");
                return;
            }

            PhialeGridDiagnostics.IncrementGridCounter(GetDiagnosticsGridId(), "RefreshSurfaceRowIndicatorsExecuted");
            var rowIndicatorProjection = BuildRowIndicatorProjection(_surfaceRowsByKey);
            using (_surfaceCoordinator.BeginSnapshotUpdateBatch("surface-row-indicator-refresh"))
            {
                _surfaceCoordinator.SetEditedRows(rowIndicatorProjection.EditedRowKeys);
                _surfaceCoordinator.SetInvalidRows(rowIndicatorProjection.InvalidRowKeys);
                _surfaceCoordinator.SetRowIndicatorToolTips(rowIndicatorProjection.ToolTips);
            }
            stopwatch.Stop();
            LogDiagnostics($"RefreshSurfaceRowIndicators finished in {stopwatch.ElapsedMilliseconds} ms. Count={indicatorCounter.Count}, SurfaceRows={_surfaceRowsByKey.Count}. {GetGridSessionDescription()}.");
        }

        private IReadOnlyList<object> ResolveRowsForIndicatorProjection()
        {
            var sourceRows = ApplyGlobalSearch(EnumerateEffectiveItemsSource().ToArray());
            if (sourceRows.Length == 0)
            {
                return Array.Empty<object>();
            }

            return BuildSurfaceRows(sourceRows);
        }

        private object ResolveFilteredRowForIndicators(string rowId)
        {
            return ResolveRowsForIndicatorProjection()
                .FirstOrDefault(candidate => string.Equals(ResolveRowId(candidate), rowId, StringComparison.OrdinalIgnoreCase));
        }

        private string ResolveIndicatorAnchorKey(
            object row,
            string rowId,
            IReadOnlyDictionary<string, string> visibleDataRowKeysById,
            IReadOnlyDictionary<string, string> visibleGroupRowKeysById)
        {
            if (visibleDataRowKeysById.TryGetValue(rowId, out var directRowKey))
            {
                return directRowKey;
            }

            if (_groupDescriptors.Count == 0)
            {
                return null;
            }

            foreach (var groupId in BuildContainingGroupIds(row))
            {
                if (_groupExpansionState.IsExpanded(groupId))
                {
                    continue;
                }

                if (visibleGroupRowKeysById.TryGetValue(groupId, out var groupRowKey))
                {
                    return groupRowKey;
                }
            }

            return null;
        }

        private IReadOnlyList<string> BuildContainingGroupIds(object row)
        {
            if (row == null || _groupDescriptors.Count == 0)
            {
                return Array.Empty<string>();
            }

            var groupIds = new List<string>(_groupDescriptors.Count);
            string parentId = null;
            foreach (var descriptor in _groupDescriptors)
            {
                var groupId = GridGroupNode<object>.BuildStableId(parentId, descriptor.ColumnId, ResolveRowValue(row, descriptor.ColumnId));
                groupIds.Add(groupId);
                parentId = groupId;
            }

            return groupIds;
        }

        private static bool TryResolveGroupId(object row, out string groupId)
        {
            if (row is GridGroupFlatRow<object> groupedRow && groupedRow.Kind == GridGroupFlatRowKind.GroupHeader)
            {
                groupId = groupedRow.GroupId;
                return true;
            }

            if (row is GridGroupHeaderRowModel headerRow)
            {
                groupId = headerRow.GroupId;
                return true;
            }

            groupId = null;
            return false;
        }

        private string BuildAnchoredIndicatorToolTip(object row, string detailText)
        {
            if (string.IsNullOrWhiteSpace(detailText))
            {
                return string.Empty;
            }

            var label = ResolveIndicatorRecordLabel(row);
            return string.IsNullOrWhiteSpace(label)
                ? detailText
                : label + Environment.NewLine + detailText;
        }

        private string ResolveIndicatorRecordLabel(object row)
        {
            var candidateColumns = new[] { "ObjectName", "Name", "Id" };
            foreach (var columnId in candidateColumns)
            {
                var value = Convert.ToString(ResolveRowValue(row, columnId), CultureInfo.CurrentCulture);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            return ResolveRowId(row);
        }

        private static void AppendToolTipSegment(
            IDictionary<string, List<string>> segments,
            string anchorKey,
            string toolTipSegment)
        {
            if (string.IsNullOrWhiteSpace(anchorKey) || string.IsNullOrWhiteSpace(toolTipSegment))
            {
                return;
            }

            if (!segments.TryGetValue(anchorKey, out var rowSegments))
            {
                rowSegments = new List<string>();
                segments[anchorKey] = rowSegments;
            }

            if (!rowSegments.Contains(toolTipSegment, StringComparer.Ordinal))
            {
                rowSegments.Add(toolTipSegment);
            }
        }

        private string BuildValidationToolTip(IReadOnlyList<RowValidationDetail> details)
        {
            if (details == null || details.Count == 0)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            builder.AppendLine("Validation issues:");

            foreach (var detail in details)
            {
                builder.Append("- ");
                builder.Append(string.IsNullOrWhiteSpace(detail.DisplayName) ? "Record" : detail.DisplayName);
                builder.Append(": ");
                builder.Append(detail.Message);
                builder.AppendLine();
            }

            return builder.ToString().TrimEnd();
        }

        private string BuildEditedRowToolTip(IReadOnlyList<EditSessionFieldChange> fieldChanges)
        {
            if (fieldChanges == null || fieldChanges.Count == 0)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            builder.AppendLine("Edited fields:");
            foreach (var change in fieldChanges)
            {
                builder.Append("- ");
                builder.Append(string.IsNullOrWhiteSpace(change.DisplayName) ? ResolveColumnDisplayName(change.FieldId) : change.DisplayName);
                builder.Append(": ");
                builder.Append(FormatIndicatorValue(change.OriginalValue));
                builder.Append(" -> ");
                builder.AppendLine(FormatIndicatorValue(change.CurrentValue));
            }

            return builder.ToString().TrimEnd();
        }

        private string ResolveColumnDisplayName(string columnId)
        {
            if (string.IsNullOrWhiteSpace(columnId))
            {
                return "Record";
            }

            var visibleColumn = _visibleColumns.FirstOrDefault(column =>
                string.Equals(column.ColumnId, columnId, StringComparison.OrdinalIgnoreCase));
            if (visibleColumn != null)
            {
                return string.IsNullOrWhiteSpace(visibleColumn.Header) ? visibleColumn.ColumnId : visibleColumn.Header;
            }

            return columnId;
        }

        private static string FormatIndicatorValue(object value)
        {
            var text = GridValueFormatter.FormatDisplayValue(value, CultureInfo.CurrentCulture);
            return string.IsNullOrWhiteSpace(text) ? "(empty)" : text;
        }

        private object FindCurrentRow(string rowId)
        {
            return EnumerateEffectiveItemsSource()
                .FirstOrDefault(row => string.Equals(ResolveRowId(row), rowId, StringComparison.OrdinalIgnoreCase));
        }

        private string ResolveRowId(object row)
        {
            if (row is GridGroupFlatRow<object> groupedRow)
            {
                if (groupedRow.Kind == GridGroupFlatRowKind.GroupHeader)
                {
                    return "group:" + groupedRow.GroupId;
                }

                row = groupedRow.Item;
            }

            if (row is GridDataRowModel dataRowModel)
            {
                row = dataRowModel.SourceRow;
            }

            var id = Convert.ToString(ResolveRowValue(row, "Id"), CultureInfo.InvariantCulture);
            if (!string.IsNullOrWhiteSpace(id))
            {
                return id;
            }

            var product = Convert.ToString(ResolveRowValue(row, "Product"), CultureInfo.InvariantCulture);
            if (!string.IsNullOrWhiteSpace(product))
            {
                return product;
            }

            return row.GetType().FullName + ":" + row.GetHashCode().ToString(CultureInfo.InvariantCulture);
        }

        private object CloneRow(object row)
        {
            row = UnwrapEditableSurfaceRow(row);
            if (row == null)
            {
                return null;
            }

            if (row is ICloneable cloneable)
            {
                return cloneable.Clone();
            }

            var type = row.GetType();
            var clone = Activator.CreateInstance(type);
            foreach (var property in GetWritableProperties(type))
            {
                property.SetValue(clone, property.GetValue(row, null), null);
            }

            return clone;
        }

        private static IEnumerable<PropertyInfo> GetWritableProperties(Type type)
        {
            return type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(property => property.CanRead && property.CanWrite);
        }

        private bool CanEditColumn(GridColumnBindingModel column)
        {
            if (column == null || !column.Definition.IsEditable)
            {
                return false;
            }

            var fieldDefinition = ResolveEditSessionFieldDefinition(column.ColumnId);
            if (fieldDefinition != null)
            {
                return column.Definition.IsEditable;
            }

            var firstRow = EnumerateEffectiveItemsSource().FirstOrDefault();
            if (firstRow == null)
            {
                return column.Definition.IsEditable;
            }

            if (firstRow is IDictionary<string, object> dictionary)
            {
                return dictionary.ContainsKey(column.ColumnId);
            }

            var propertyInfo = firstRow.GetType().GetProperty(column.ColumnId, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
            return propertyInfo != null && propertyInfo.CanWrite;
        }

        private void CopyRowValues(object source, object target)
        {
            if (source == null || target == null)
            {
                return;
            }

            foreach (var property in GetWritableProperties(target.GetType()))
            {
                property.SetValue(target, property.GetValue(source, null), null);
            }
        }

        private bool AreRowsEqual(object left, object right)
        {
            left = UnwrapEditableSurfaceRow(left);
            right = UnwrapEditableSurfaceRow(right);
            if (left == null || right == null)
            {
                return Equals(left, right);
            }

            foreach (var property in GetWritableProperties(left.GetType()))
            {
                var leftValue = property.GetValue(left, null);
                var rightValue = property.GetValue(right, null);
                if (!Equals(leftValue, rightValue))
                {
                    return false;
                }
            }

            return true;
        }

        private static object UnwrapEditableSurfaceRow(object row)
        {
            if (row is GridGroupFlatRow<object> groupedRow)
            {
                return groupedRow.Kind == GridGroupFlatRowKind.DataRow ? groupedRow.Item : null;
            }

            if (row is GridSurfaceMasterDetailDetailsHostRowModel)
            {
                return null;
            }

            return row is GridDataRowModel dataRowModel ? dataRowModel.SourceRow : row;
        }

        internal void SetRowValue(object row, string columnId, object value)
        {
            if (row is GridGroupFlatRow<object> groupedRow)
            {
                if (groupedRow.Kind == GridGroupFlatRowKind.GroupHeader)
                {
                    return;
                }

                row = groupedRow.Item;
            }

            if (row is GridSurfaceMasterDetailDetailsHostRowModel)
            {
                return;
            }

            if (row is GridDataRowModel dataRowModel)
            {
                row = dataRowModel.SourceRow;
            }

            var fieldDefinition = ResolveEditSessionFieldDefinition(columnId);
            if (fieldDefinition != null)
            {
                fieldDefinition.SetValue(row, value);
                return;
            }

            var propertyInfo = row.GetType().GetProperty(columnId, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
            if (propertyInfo == null || !propertyInfo.CanWrite)
            {
                return;
            }

            var targetType = Nullable.GetUnderlyingType(propertyInfo.PropertyType) ?? propertyInfo.PropertyType;
            object converted = value;
            if (value != null && targetType != typeof(string) && !targetType.IsInstanceOfType(value))
            {
                if (value is string stringValue && string.IsNullOrWhiteSpace(stringValue) && Nullable.GetUnderlyingType(propertyInfo.PropertyType) != null)
                {
                    converted = null;
                }
                else
                {
                    converted = Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
                }
            }

            propertyInfo.SetValue(row, converted, null);
        }

        private double ComputeMasterDetailSurfaceDetailsHeight(GridMasterDetailMasterRowModel masterRow)
        {
            if (masterRow == null)
            {
                return ResolvedDetailRowHeight;
            }

            var detailRowCount = Math.Max(1, masterRow.DetailRows?.Count ?? 0);
            const int maxVisibleDetailRows = 8;
            var visibleDetailRowCount = Math.Min(detailRowCount, maxVisibleDetailRows);
            const double paddingHeight = 28d;
            const double headerSectionHeight = 28d;
            const double filterSectionHeight = 32d;
            return paddingHeight + headerSectionHeight + filterSectionHeight + (visibleDetailRowCount * ResolvedDetailRowHeight);
        }

        private void NormalizeFrozenColumns()
        {
            if (_layoutState == null)
            {
                return;
            }

            var ordered = _layoutState.Columns
                .OrderByDescending(column => column.IsFrozen)
                .ThenBy(column => column.DisplayIndex)
                .Select((column, index) => column.WithDisplayIndex(index))
                .ToArray();
            _layoutState = new GridLayoutState(ordered);
        }

        private static GridLocalizationCatalog LoadCatalog(string languageDirectory)
        {
            if (!string.IsNullOrWhiteSpace(languageDirectory))
            {
                try
                {
                    return GridLocalizationCatalog.LoadFromDirectory(languageDirectory);
                }
                catch
                {
                }
            }

            try
            {
                return GridLocalizationCatalog.LoadDefault();
            }
            catch
            {
                return GridLocalizationCatalog.Empty;
            }
        }

        private void SyncGroupsProperty()
        {
            _isSyncingGroups = true;
            SetCurrentValue(GroupsProperty, _groupDescriptors.ToArray());
            _isSyncingGroups = false;
        }

        private void SyncSortsProperty()
        {
            _isSyncingSorts = true;
            SetCurrentValue(SortsProperty, _sortDescriptors.ToArray());
            _isSyncingSorts = false;
        }

        private void SyncSummariesProperty()
        {
            SetCurrentValue(SummariesProperty, _summaryDescriptors.ToArray());
        }

        private void RaiseStatusPropertyChanges()
        {
            OnPropertyChanged(nameof(SelectionStatusText));
            OnPropertyChanged(nameof(CurrentCellText));
            OnPropertyChanged(nameof(HasSelectedRows));
            OnPropertyChanged(nameof(EditStatusText));
            OnPropertyChanged(nameof(PendingEditCount));
            OnPropertyChanged(nameof(ChangePanelItems));
            OnPropertyChanged(nameof(ChangeSummaryText));
            OnPropertyChanged(nameof(IsChangePanelChangedRowsFilterActive));
            OnPropertyChanged(nameof(ChangePanelRowsFilterToggleText));
            OnPropertyChanged(nameof(ValidationIssueCount));
            OnPropertyChanged(nameof(ValidationIssueRowIds));
            OnPropertyChanged(nameof(ValidationIssueItems));
            OnPropertyChanged(nameof(ValidationIssueSummaryText));
            OnPropertyChanged(nameof(HasPendingEdits));
            OnPropertyChanged(nameof(HasValidationIssues));
            OnPropertyChanged(nameof(PendingEditBannerText));
            OnPropertyChanged(nameof(PagingStatusText));
            OnPropertyChanged(nameof(HasSummaries));
        }

        private int ResolveValidationIssueCount()
        {
            return ResolveValidationIssueItems().Count;
        }

        private string GetSummaryTypeText(GridSummaryType type)
        {
            switch (type)
            {
                case GridSummaryType.Count:
                    return GetText(GridTextKeys.SummaryCount);
                case GridSummaryType.Sum:
                    return GetText(GridTextKeys.SummarySum);
                case GridSummaryType.Average:
                    return GetText(GridTextKeys.SummaryAverage);
                case GridSummaryType.Min:
                    return GetText(GridTextKeys.SummaryMin);
                case GridSummaryType.Max:
                    return GetText(GridTextKeys.SummaryMax);
                default:
                    return type.ToString();
            }
        }

        private static GridColumnDefinition CloneColumnDefinition(GridColumnDefinition column)
        {
            return new GridColumnDefinition(
                column.Id,
                column.Header,
                column.Width,
                column.MinWidth,
                column.IsVisible,
                column.IsFrozen,
                column.IsEditable,
                column.DisplayIndex,
                column.ValueType,
                column.EditorKind,
                column.EditorItems,
                column.EditMask,
                column.ValueKind,
                column.ValidationConstraints,
                column.EditorItemsMode);
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void RaiseViewStateChanged()
        {
            if (_suppressViewStateNotifications)
            {
                return;
            }

            if (ShouldDeferVisualWorkBecauseBatched())
            {
                _pendingViewStateChangedAfterBatch = true;
                LogDiagnostics("ViewStateChanged deferred because grid update batch is active. " + GetGridSessionDescription() + ".");
                return;
            }

            RaiseViewStateChangedCore();
        }

        private void RaiseViewStateChangedCore()
        {
            ViewStateChanged?.Invoke(this, EventArgs.Empty);
        }

        private void LogDiagnostics(string message)
        {
            PhialeGridDiagnostics.Write("PhialeGrid", $"{GetDiagnosticsGridId()}: {message}");
        }

        private string GetDiagnosticsGridId()
        {
            var name = string.IsNullOrWhiteSpace(Name) ? "<unnamed>" : Name;
            return name + "#" + GetHashCode().ToString("X8");
        }

        private string GetGridSessionDescription()
        {
            return PhialeGridDiagnostics.DescribeGridSession(GetDiagnosticsGridId());
        }

        private sealed class GridSurfaceMasterDetailDetailsHostRowModel
        {
            public GridSurfaceMasterDetailDetailsHostRowModel(GridMasterDetailMasterRowModel masterRow)
            {
                MasterRow = masterRow ?? throw new ArgumentNullException(nameof(masterRow));
            }

            public GridMasterDetailMasterRowModel MasterRow { get; }
        }

        private sealed class GridCustomDetailHostRowModel
        {
            public GridCustomDetailHostRowModel(
                string ownerRowKey,
                GridRowDetailDescriptor descriptor,
                GridRowDetailContext context)
            {
                if (string.IsNullOrWhiteSpace(ownerRowKey))
                {
                    throw new ArgumentException("Owner row key is required.", nameof(ownerRowKey));
                }

                OwnerRowKey = ownerRowKey;
                Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
                Context = context ?? throw new ArgumentNullException(nameof(context));
                RowKey = descriptor.DetailKey;
            }

            public string RowKey { get; }

            public string OwnerRowKey { get; }

            public GridRowDetailDescriptor Descriptor { get; }

            public GridRowDetailContext Context { get; }

            public GridRowDetailSurfacePayload ToSurfacePayload()
            {
                return new GridRowDetailSurfacePayload(
                    Descriptor.DetailKey,
                    OwnerRowKey,
                    Context,
                    Descriptor.ContentDescriptor);
            }
        }

        private sealed class RowValidationDetail
        {
            public RowValidationDetail(string columnId, string displayName, string message)
            {
                ColumnId = columnId ?? string.Empty;
                DisplayName = string.IsNullOrWhiteSpace(displayName) ? ColumnId : displayName;
                Message = message ?? string.Empty;
            }

            public string ColumnId { get; }

            public string DisplayName { get; }

            public string Message { get; }
        }

        public sealed class ValidationIssuePanelItem
        {
            public ValidationIssuePanelItem(string rowId, string columnId, string columnDisplayName, string message)
            {
                if (string.IsNullOrWhiteSpace(rowId))
                {
                    throw new ArgumentException("Validation issue row id is required.", nameof(rowId));
                }

                if (string.IsNullOrWhiteSpace(columnId))
                {
                    throw new ArgumentException("Validation issue column id is required.", nameof(columnId));
                }

                if (string.IsNullOrWhiteSpace(columnDisplayName))
                {
                    throw new ArgumentException("Validation issue column display name is required.", nameof(columnDisplayName));
                }

                if (string.IsNullOrWhiteSpace(message))
                {
                    throw new ArgumentException("Validation issue message is required.", nameof(message));
                }

                RowId = rowId;
                ColumnId = columnId;
                ColumnDisplayName = columnDisplayName;
                Message = message;
                Title = "Row " + RowId + " - " + ColumnDisplayName;
            }

            public string RowId { get; }

            public string ColumnId { get; }

            public string ColumnDisplayName { get; }

            public string Message { get; }

            public string Title { get; }
        }

        public sealed class ChangePanelItem
        {
            public ChangePanelItem(string rowId, string navigationColumnId, string rowDisplayText, string description)
            {
                if (string.IsNullOrWhiteSpace(rowId))
                {
                    throw new ArgumentException("Change panel row id is required.", nameof(rowId));
                }

                if (string.IsNullOrWhiteSpace(navigationColumnId))
                {
                    throw new ArgumentException("Change panel navigation column id is required.", nameof(navigationColumnId));
                }

                if (string.IsNullOrWhiteSpace(rowDisplayText))
                {
                    throw new ArgumentException("Change panel row display text is required.", nameof(rowDisplayText));
                }

                if (string.IsNullOrWhiteSpace(description))
                {
                    throw new ArgumentException("Change panel description is required.", nameof(description));
                }

                RowId = rowId;
                NavigationColumnId = navigationColumnId;
                RowDisplayText = rowDisplayText;
                Description = description;
                Title = "Row " + RowDisplayText;
            }

            public string RowId { get; }

            public string NavigationColumnId { get; }

            public string RowDisplayText { get; }

            public string Description { get; }

            public string Title { get; }
        }

        private sealed class RowIndicatorProjection
        {
            public static readonly RowIndicatorProjection Empty = new RowIndicatorProjection(
                Array.Empty<string>(),
                Array.Empty<string>(),
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));

            public RowIndicatorProjection(
                IReadOnlyCollection<string> editedRowKeys,
                IReadOnlyCollection<string> invalidRowKeys,
                IReadOnlyDictionary<string, string> toolTips)
            {
                EditedRowKeys = editedRowKeys ?? Array.Empty<string>();
                InvalidRowKeys = invalidRowKeys ?? Array.Empty<string>();
                ToolTips = toolTips ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            public IReadOnlyCollection<string> EditedRowKeys { get; }

            public IReadOnlyCollection<string> InvalidRowKeys { get; }

            public IReadOnlyDictionary<string, string> ToolTips { get; }
        }

        private sealed class GridUpdateBatchScope : IDisposable
        {
            private PhialeGrid _owner;
            private readonly string _reason;
            private readonly IDisposable _editStateBatch;
            private readonly IDisposable _surfaceSnapshotBatch;

            public GridUpdateBatchScope(PhialeGrid owner, string reason, IDisposable editStateBatch, IDisposable surfaceSnapshotBatch)
            {
                _owner = owner ?? throw new ArgumentNullException(nameof(owner));
                _reason = reason ?? string.Empty;
                _editStateBatch = editStateBatch;
                _surfaceSnapshotBatch = surfaceSnapshotBatch;
            }

            public void Dispose()
            {
                var owner = _owner;
                if (owner == null)
                {
                    return;
                }

                _owner = null;
                _editStateBatch?.Dispose();
                owner.CompleteGridUpdateBatch(_reason);
                _surfaceSnapshotBatch?.Dispose();
            }
        }

        private sealed class SurfaceGridDataBridge : IGridCellValueProvider, IGridEditCellAccessor
        {
            private readonly PhialeGrid _owner;

            public SurfaceGridDataBridge(PhialeGrid owner)
            {
                _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            }

            public bool TryGetValue(string rowKey, string columnKey, out object value)
            {
                if (string.IsNullOrWhiteSpace(rowKey) || string.IsNullOrWhiteSpace(columnKey) || !_owner._surfaceRowsByKey.TryGetValue(rowKey, out var row))
                {
                    value = null;
                    return false;
                }

                value = _owner.ResolveRowValue(row, columnKey);
                return true;
            }

            public void SetValue(string rowKey, string columnKey, object value)
            {
                if (string.IsNullOrWhiteSpace(rowKey) || string.IsNullOrWhiteSpace(columnKey) || !_owner._surfaceRowsByKey.TryGetValue(rowKey, out var row))
                {
                    return;
                }

                if (row is GridGroupFlatRow<object> groupedRow && groupedRow.Kind == GridGroupFlatRowKind.GroupHeader)
                {
                    return;
                }

                var rowId = _owner.ResolveRowId(row);
                _owner._editSessionContext.TrySetFieldValue(rowId, columnKey, value, Convert.ToString(value, CultureInfo.CurrentCulture));
            }
        }

        private sealed class SurfaceGridEditValidator : IGridEditValidator
        {
            private readonly PhialeGrid _owner;

            public SurfaceGridEditValidator(PhialeGrid owner)
            {
                _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            }

            public IReadOnlyList<GridValidationError> Validate(string rowKey, string columnKey, object parsedValue, string editingText)
            {
                if (string.IsNullOrWhiteSpace(rowKey) ||
                    string.IsNullOrWhiteSpace(columnKey) ||
                    !_owner._surfaceRowsByKey.TryGetValue(rowKey, out var row))
                {
                    return Array.Empty<GridValidationError>();
                }

                var rowId = _owner.ResolveRowId(row);
                return _owner._editSessionContext.ValidateFieldValue(rowId, columnKey, parsedValue ?? editingText, editingText);
            }
        }
    }

    public abstract class GridDisplayRowModel : INotifyPropertyChanged, IDataErrorInfo
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public abstract bool IsGroupHeader { get; }

        public abstract int Level { get; }

        public abstract object SourceRow { get; }

        public virtual bool IsHierarchyBranch => false;

        public virtual bool IsHierarchyLoadMore => false;

        public virtual bool IsMasterDetailMaster => false;

        public virtual bool IsMasterDetailHeader => false;

        public virtual bool IsMasterDetailDetail => false;

        public object this[string columnId]
        {
            get => GetCellValue(columnId);
            set => SetCellValue(columnId, value);
        }

        public virtual string Error => string.Empty;

        string IDataErrorInfo.this[string columnName] => GetValidationError(columnName);

        protected abstract object GetCellValue(string columnId);

        protected virtual void SetCellValue(string columnId, object value)
        {
        }

        protected virtual string GetValidationError(string columnName)
        {
            return string.Empty;
        }

        protected void RaiseCellChanged(string columnId)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(columnId));
        }

        protected void RaiseRowChanged()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        }

        protected void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class GridDataRowModel : GridDisplayRowModel
    {
        private readonly PhialeGrid _owner;
        private readonly object _sourceRow;
        private readonly int _level;

        public GridDataRowModel(PhialeGrid owner, object sourceRow, int level = 0)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _sourceRow = sourceRow ?? throw new ArgumentNullException(nameof(sourceRow));
            _level = level;

            if (_sourceRow is INotifyPropertyChanged notifyingRow)
            {
                PropertyChangedEventManager.AddHandler(notifyingRow, HandleSourceRowPropertyChanged, string.Empty);
            }
        }

        public override bool IsGroupHeader => false;

        public override int Level => _level;

        public override object SourceRow => _sourceRow;

        internal PhialeGrid Owner => _owner;

        public override string Error => _sourceRow is IDataErrorInfo dataErrorInfo ? dataErrorInfo.Error ?? string.Empty : string.Empty;

        protected override object GetCellValue(string columnId)
        {
            return _owner.ResolveRowValue(_sourceRow, columnId);
        }

        protected override void SetCellValue(string columnId, object value)
        {
            _owner.SetRowValue(_sourceRow, columnId, value);
            RaiseCellChanged(columnId);
        }

        protected override string GetValidationError(string columnName)
        {
            if (!(_sourceRow is IDataErrorInfo dataErrorInfo))
            {
                return string.Empty;
            }

            var normalized = NormalizeColumnName(columnName);
            return string.IsNullOrWhiteSpace(normalized) ? string.Empty : dataErrorInfo[normalized] ?? string.Empty;
        }

        private static string NormalizeColumnName(string columnName)
        {
            if (string.IsNullOrWhiteSpace(columnName))
            {
                return string.Empty;
            }

            return columnName.Trim().TrimStart('[').TrimEnd(']');
        }

        private void HandleSourceRowPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(e.PropertyName))
            {
                RaiseRowChanged();
                return;
            }

            RaiseCellChanged(e.PropertyName);
        }
    }

    public sealed class GridHierarchyNodeRowModel : GridDataRowModel
    {
        private readonly string _displayColumnId;

        public GridHierarchyNodeRowModel(PhialeGrid owner, GridHierarchyNode<object> node, string displayColumnId)
            : base(owner, RequireNodeItem(node), node.PathId.Count(character => character == '/'))
        {
            Node = node ?? throw new ArgumentNullException(nameof(node));
            _displayColumnId = displayColumnId ?? string.Empty;
        }

        public GridHierarchyNode<object> Node { get; }

        public override bool IsHierarchyBranch => Node.CanExpand;

        public string Caption
        {
            get
            {
                var glyph = Node.CanExpand ? (Node.IsExpanded ? "▼" : "▶") : "•";
                var indent = new string(' ', Level * 4);
                var baseText = Convert.ToString(base.GetCellValue(_displayColumnId), CultureInfo.CurrentCulture) ?? string.Empty;
                return indent + glyph + " " + baseText;
            }
        }

        protected override object GetCellValue(string columnId)
        {
            if (string.Equals(columnId, _displayColumnId, StringComparison.OrdinalIgnoreCase))
            {
                return Caption;
            }

            return base.GetCellValue(columnId);
        }

        private static object RequireNodeItem(GridHierarchyNode<object> node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            if (node.Item == null)
            {
                throw new ArgumentException("Hierarchy node item cannot be null.", nameof(node));
            }

            return node.Item;
        }
    }

    public sealed class GridGroupHeaderRowModel : GridDisplayRowModel
    {
        private readonly string _displayColumnId;

        public GridGroupHeaderRowModel(string groupId, string groupColumnId, object groupKey, int itemCount, int level, bool isExpanded, string displayColumnId)
        {
            GroupId = groupId ?? throw new ArgumentNullException(nameof(groupId));
            GroupColumnId = groupColumnId ?? throw new ArgumentNullException(nameof(groupColumnId));
            GroupKey = groupKey;
            ItemCount = itemCount;
            Level = level;
            IsExpanded = isExpanded;
            _displayColumnId = displayColumnId ?? throw new ArgumentNullException(nameof(displayColumnId));
        }

        public override bool IsGroupHeader => true;

        public override int Level { get; }

        public override object SourceRow => null;

        public string GroupId { get; }

        public string GroupColumnId { get; }

        public object GroupKey { get; }

        public int ItemCount { get; }

        public bool IsExpanded { get; }

        public string Caption
        {
            get
            {
                var indent = new string(' ', Level * 4);
                var glyph = IsExpanded ? "▼" : "▶";
                var keyText = Convert.ToString(GroupKey, CultureInfo.CurrentCulture) ?? string.Empty;
                return indent + glyph + " " + GroupColumnId + ": " + keyText + " (" + ItemCount.ToString(CultureInfo.CurrentCulture) + ")";
            }
        }

        protected override object GetCellValue(string columnId)
        {
            return string.Equals(columnId, _displayColumnId, StringComparison.OrdinalIgnoreCase)
                ? (object)Caption
                : string.Empty;
        }
    }

    public sealed class GridMasterDetailHeaderRowModel : GridDisplayRowModel
    {
        private readonly IReadOnlyDictionary<string, string> _headerMap;
        private readonly string _pathId;

        public GridMasterDetailHeaderRowModel(IReadOnlyDictionary<string, string> headerMap, string pathId)
        {
            _headerMap = headerMap ?? throw new ArgumentNullException(nameof(headerMap));
            _pathId = pathId ?? string.Empty;
        }

        public override bool IsGroupHeader => false;

        public override bool IsMasterDetailHeader => true;

        public string PathId => _pathId;

        public override int Level => _pathId.Count(character => character == '/') + 1;

        public override object SourceRow => null;

        protected override object GetCellValue(string columnId)
        {
            return _headerMap.TryGetValue(columnId ?? string.Empty, out var header)
                ? (object)header
                : string.Empty;
        }
    }

    public sealed class GridMasterDetailMasterRowModel : GridDataRowModel
    {
        private readonly string _displayColumnId;

        public GridMasterDetailMasterRowModel(
            PhialeGrid owner,
            GridHierarchyNode<object> node,
            string displayColumnId,
            IReadOnlyList<GridMasterDetailColumnModel> detailColumns,
            string detailDisplayColumnId,
            ObservableCollection<GridMasterDetailDetailRowModel> detailRows)
            : base(owner, RequireNodeItem(node), 0)
        {
            Node = node ?? throw new ArgumentNullException(nameof(node));
            _displayColumnId = displayColumnId ?? string.Empty;
            DetailColumns = detailColumns ?? throw new ArgumentNullException(nameof(detailColumns));
            DetailDisplayColumnId = detailDisplayColumnId ?? string.Empty;
            DetailRows = detailRows ?? throw new ArgumentNullException(nameof(detailRows));
        }

        public GridHierarchyNode<object> Node { get; }

        public IReadOnlyList<GridMasterDetailColumnModel> DetailColumns { get; }

        public string DetailDisplayColumnId { get; }

        public ObservableCollection<GridMasterDetailDetailRowModel> DetailRows { get; private set; }

        public double RowActionWidth { get; set; }

        public bool ShowRowIndicator { get; set; }

        public double RowIndicatorWidth { get; set; }

        public bool ShowSelectionCheckbox { get; set; }

        public double SelectionCheckboxWidth { get; set; }

        public bool ShowRowNumbers { get; set; }

        public double RowNumberWidth { get; set; }

        public override bool IsMasterDetailMaster => true;

        public bool IsDetailExpanded => Node.IsExpanded;

        public string Caption
        {
            get
            {
                var glyph = Node.IsExpanded ? "▼" : "▶";
                var baseText = Convert.ToString(base.GetCellValue(_displayColumnId), CultureInfo.CurrentCulture) ?? string.Empty;
                return glyph + " " + baseText;
            }
        }

        protected override object GetCellValue(string columnId)
        {
            if (string.Equals(columnId, _displayColumnId, StringComparison.OrdinalIgnoreCase))
            {
                return Caption;
            }

            if (DetailColumns.Any(column => string.Equals(column.ColumnId, columnId, StringComparison.OrdinalIgnoreCase)))
            {
                return string.Empty;
            }

            return base.GetCellValue(columnId);
        }

        public void ReplaceDetailRows(ObservableCollection<GridMasterDetailDetailRowModel> detailRows)
        {
            DetailRows = detailRows ?? throw new ArgumentNullException(nameof(detailRows));
            RaiseCellChanged(_displayColumnId);
            RaisePropertyChanged(nameof(DetailRows));
        }

        private static object RequireNodeItem(GridHierarchyNode<object> node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            if (node.Item == null)
            {
                throw new ArgumentException("Hierarchy node item cannot be null.", nameof(node));
            }

            return node.Item;
        }
    }

    public sealed class GridMasterDetailDetailRowModel : GridDataRowModel
    {
        private readonly string _displayColumnId;
        private readonly ISet<string> _detailColumns;
        private readonly int _detailLevel;
        private readonly bool _applyDisplayIndent;
        private bool _isMarkerChecked;

        public GridMasterDetailDetailRowModel(
            PhialeGrid owner,
            object sourceRow,
            string displayColumnId,
            ISet<string> detailColumns,
            int detailLevel,
            bool applyDisplayIndent)
            : base(owner, sourceRow, detailLevel)
        {
            _displayColumnId = displayColumnId ?? string.Empty;
            _detailColumns = detailColumns ?? throw new ArgumentNullException(nameof(detailColumns));
            _detailLevel = detailLevel;
            _applyDisplayIndent = applyDisplayIndent;
        }

        public override bool IsMasterDetailDetail => true;

        public bool IsMarkerChecked
        {
            get => _isMarkerChecked;
            set
            {
                if (_isMarkerChecked == value)
                {
                    return;
                }

                _isMarkerChecked = value;
                RaisePropertyChanged(nameof(IsMarkerChecked));
            }
        }

        protected override object GetCellValue(string columnId)
        {
            if (!_detailColumns.Contains(columnId ?? string.Empty))
            {
                return string.Empty;
            }

            var baseValue = base.GetCellValue(columnId);
            if (!string.Equals(columnId, _displayColumnId, StringComparison.OrdinalIgnoreCase))
            {
                return baseValue;
            }

            var text = Convert.ToString(baseValue, CultureInfo.CurrentCulture) ?? string.Empty;
            if (!_applyDisplayIndent || _detailLevel <= 0)
            {
                return text;
            }

            return new string('\u2003', _detailLevel) + text;
        }
    }

    public sealed class GridMasterDetailColumnModel : INotifyPropertyChanged
    {
        private readonly Action<string, string, string> _changeHandler;
        private readonly string _pathId;
        private string _filterText;

        public GridMasterDetailColumnModel(string columnId, string header, double width, string filterText, string pathId, Action<string, string, string> changeHandler)
        {
            ColumnId = columnId ?? throw new ArgumentNullException(nameof(columnId));
            Header = header ?? string.Empty;
            Width = Math.Max(80d, width);
            _filterText = filterText ?? string.Empty;
            _pathId = pathId ?? string.Empty;
            _changeHandler = changeHandler;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public string ColumnId { get; }

        public string Header { get; }

        public double Width { get; }

        public string FilterText
        {
            get => _filterText;
            set
            {
                var next = value ?? string.Empty;
                if (string.Equals(_filterText, next, StringComparison.Ordinal))
                {
                    return;
                }

                _filterText = next;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FilterText)));
                _changeHandler?.Invoke(_pathId, ColumnId, next);
            }
        }
    }

    public sealed class GridHierarchyLoadMoreRowModel : GridDisplayRowModel
    {
        private readonly string _pathId;
        private readonly string _displayColumnId;
        private readonly string _caption;

        public GridHierarchyLoadMoreRowModel(GridHierarchyNode<object> parentNode, string pathId, string displayColumnId, string caption)
        {
            ParentNode = parentNode ?? throw new ArgumentNullException(nameof(parentNode));
            _pathId = pathId ?? throw new ArgumentNullException(nameof(pathId));
            _displayColumnId = displayColumnId ?? string.Empty;
            _caption = caption ?? string.Empty;
        }

        public GridHierarchyNode<object> ParentNode { get; }

        public override bool IsGroupHeader => false;

        public override bool IsHierarchyLoadMore => true;

        public override int Level => _pathId.Count(character => character == '/') + 1;

        public override object SourceRow => null;

        public string Caption
        {
            get
            {
                var indent = new string(' ', Level * 4);
                return indent + "↳ " + _caption;
            }
        }

        protected override object GetCellValue(string columnId)
        {
            return string.Equals(columnId, _displayColumnId, StringComparison.OrdinalIgnoreCase)
                ? (object)Caption
                : string.Empty;
        }
    }

    public sealed class GridLoadingRowModel : GridDisplayRowModel
    {
        private readonly string _displayColumnId;
        private readonly string _loadingText;

        public GridLoadingRowModel(string displayColumnId, string loadingText)
        {
            _displayColumnId = displayColumnId ?? string.Empty;
            _loadingText = loadingText ?? string.Empty;
        }

        public override bool IsGroupHeader => false;

        public override int Level => 0;

        public override object SourceRow => null;

        protected override object GetCellValue(string columnId)
        {
            return string.Equals(columnId, _displayColumnId, StringComparison.OrdinalIgnoreCase)
                ? (object)_loadingText
                : string.Empty;
        }
    }

    public sealed class InMemoryGridQueryDataProvider : IGridQueryDataProvider<object>, IGridGroupedQueryDataProvider<object>
    {
        private readonly IReadOnlyList<object> _rows;
        private readonly DelegateGridRowAccessor<object> _accessor;

        public InMemoryGridQueryDataProvider(IReadOnlyList<object> rows, DelegateGridRowAccessor<object> accessor)
        {
            _rows = rows ?? throw new ArgumentNullException(nameof(rows));
            _accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));
        }

        public Task<GridQueryResult<object>> QueryAsync(GridQueryRequest request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var engine = new GridQueryEngine<object>(_accessor);
            return Task.FromResult(engine.Execute(_rows, request));
        }

        public Task<GridGroupedQueryResult<object>> QueryGroupedAsync(GridGroupedQueryRequest request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var engine = new GridQueryEngine<object>(_accessor);
            return Task.FromResult(engine.ExecuteGroupedWindow(_rows, request));
        }
    }

    public sealed class GridVirtualizedCollectionView : CollectionView, IEditableCollectionView
    {
        private readonly IList _source;
        private readonly INotifyCollectionChanged _observableSource;
        private object _currentEditItem;
        private NewItemPlaceholderPosition _newItemPlaceholderPosition;

        public GridVirtualizedCollectionView(IList source)
            : base(source ?? throw new ArgumentNullException(nameof(source)))
        {
            _source = source;
            _observableSource = source as INotifyCollectionChanged;
            if (_observableSource != null)
            {
                _observableSource.CollectionChanged += HandleSourceCollectionChanged;
            }

            if (_source.Count == 0)
            {
                SetCurrent(null, -1, 0);
            }
            else
            {
                SetCurrent(_source[0], 0, _source.Count);
            }
        }

        public override int Count => _source.Count;

        public override bool IsEmpty => _source.Count == 0;

        public bool CanAddNew => false;

        public bool CanCancelEdit => true;

        public bool CanRemove => !_source.IsReadOnly && !_source.IsFixedSize;

        public object CurrentAddItem => null;

        public object CurrentEditItem => _currentEditItem;

        public bool IsAddingNew => false;

        public bool IsEditingItem => _currentEditItem != null;

        public NewItemPlaceholderPosition NewItemPlaceholderPosition
        {
            get => _newItemPlaceholderPosition;
            set
            {
                if (value != NewItemPlaceholderPosition.None)
                {
                    throw new InvalidOperationException("This view does not support AddNew.");
                }

                _newItemPlaceholderPosition = value;
            }
        }

        public override object GetItemAt(int index)
        {
            return _source[index];
        }

        public override int IndexOf(object item)
        {
            return _source.IndexOf(item);
        }

        public override bool Contains(object item)
        {
            return _source.Contains(item);
        }

        protected override IEnumerator GetEnumerator()
        {
            return _source.GetEnumerator();
        }

        protected override void RefreshOverride()
        {
            ReconcileCurrency();
        }

        public object AddNew()
        {
            throw new InvalidOperationException("This view does not support AddNew.");
        }

        public void CommitNew()
        {
            throw new InvalidOperationException("This view does not support AddNew.");
        }

        public void CancelNew()
        {
            throw new InvalidOperationException("This view does not support AddNew.");
        }

        public void RemoveAt(int index)
        {
            if (!CanRemove)
            {
                throw new InvalidOperationException("This view does not support Remove.");
            }

            _source.RemoveAt(index);
        }

        public void Remove(object item)
        {
            if (!CanRemove)
            {
                throw new InvalidOperationException("This view does not support Remove.");
            }

            _source.Remove(item);
        }

        public void EditItem(object item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (!_source.Contains(item))
            {
                throw new ArgumentException("The supplied item does not belong to this view.", nameof(item));
            }

            if (ReferenceEquals(_currentEditItem, item))
            {
                return;
            }

            if (_currentEditItem != null)
            {
                CommitEdit();
            }

            _currentEditItem = item;
            if (item is IEditableObject editableObject)
            {
                editableObject.BeginEdit();
            }

            OnPropertyChanged(new PropertyChangedEventArgs(nameof(CurrentEditItem)));
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsEditingItem)));
        }

        public void CommitEdit()
        {
            if (_currentEditItem == null)
            {
                return;
            }

            if (_currentEditItem is IEditableObject editableObject)
            {
                editableObject.EndEdit();
            }

            _currentEditItem = null;
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(CurrentEditItem)));
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsEditingItem)));
        }

        public void CancelEdit()
        {
            if (_currentEditItem == null)
            {
                return;
            }

            if (_currentEditItem is IEditableObject editableObject)
            {
                editableObject.CancelEdit();
            }

            _currentEditItem = null;
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(CurrentEditItem)));
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsEditingItem)));
        }

        public override void DetachFromSourceCollection()
        {
            if (_observableSource != null)
            {
                _observableSource.CollectionChanged -= HandleSourceCollectionChanged;
            }

            _currentEditItem = null;

            base.DetachFromSourceCollection();
        }

        private void HandleSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            ReconcileCurrency();
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsEmpty)));
            OnCollectionChanged(e);
        }

        private void ReconcileCurrency()
        {
            if (_source.Count == 0)
            {
                SetCurrent(null, -1, 0);
                return;
            }

            var currentPosition = CurrentPosition;
            if (currentPosition < 0)
            {
                SetCurrent(_source[0], 0, _source.Count);
                return;
            }

            if (currentPosition >= _source.Count)
            {
                var lastIndex = _source.Count - 1;
                SetCurrent(_source[lastIndex], lastIndex, _source.Count);
                return;
            }

            SetCurrent(_source[currentPosition], currentPosition, _source.Count);
        }
    }

    public sealed class GridVirtualizedRowCollection : IList, INotifyCollectionChanged, INotifyPropertyChanged
    {
        private readonly PhialeGrid _owner;
        private readonly VirtualizedGridDataSource<object> _source;
        private readonly string _displayColumnId;
        private readonly GridLoadingRowModel _loadingRow;
        private readonly Dictionary<int, GridDisplayRowModel> _rows = new Dictionary<int, GridDisplayRowModel>();
        private readonly HashSet<int> _pendingPages = new HashSet<int>();
        private readonly object _gate = new object();
        private GridRange _loadedRange;

        public GridVirtualizedRowCollection(
            PhialeGrid owner,
            IReadOnlyList<object> sourceRows,
            GridFilterGroup filterGroup,
            IReadOnlyList<GridSortDescriptor> sorts,
            IReadOnlyList<GridSummaryDescriptor> summaries,
            string displayColumnId)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _displayColumnId = displayColumnId ?? string.Empty;

            var provider = new InMemoryGridQueryDataProvider(sourceRows, new DelegateGridRowAccessor<object>(owner.ResolveRowValue));
            var querySource = new QueryVirtualizedGridDataSource<object>(provider, 120)
            {
                FilterGroup = filterGroup ?? GridFilterGroup.EmptyAnd(),
                Sorts = sorts ?? Array.Empty<GridSortDescriptor>(),
                Summaries = summaries ?? Array.Empty<GridSummaryDescriptor>(),
            };

            _source = new VirtualizedGridDataSource<object>(querySource, 120, 24, 1);
            _loadingRow = new GridLoadingRowModel(_displayColumnId, _owner.GetText(GridTextKeys.LoadingText));
            Count = _source.GetCountAsync().GetAwaiter().GetResult();
            TotalItemCount = querySource.LastTotalCount;
            Summary = querySource.LastSummary;
            PrimeInitialWindow();
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public event PropertyChangedEventHandler PropertyChanged;

        public int Count { get; }

        public int TotalItemCount { get; }

        public GridSummarySet Summary { get; }

        public bool IsSynchronized => false;

        public object SyncRoot => this;

        public bool IsFixedSize => true;

        public bool IsReadOnly => true;

        public object this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                lock (_gate)
                {
                    if (_rows.TryGetValue(index, out var row))
                    {
                        return row;
                    }
                }

                return _loadingRow;
            }
            set => throw new NotSupportedException();
        }

        public IEnumerator GetEnumerator()
        {
            for (var index = 0; index < Count; index++)
            {
                yield return GetLoadedRowOrPlaceholder(index);
            }
        }

        public int Add(object value)
        {
            throw new NotSupportedException();
        }

        public void Clear()
        {
            throw new NotSupportedException();
        }

        public bool Contains(object value)
        {
            return IndexOf(value) >= 0;
        }

        public int IndexOf(object value)
        {
            if (value is GridDataRowModel dataRow)
            {
                lock (_gate)
                {
                    var match = _rows.FirstOrDefault(entry => ReferenceEquals(entry.Value, dataRow));
                    return match.Value == null ? -1 : match.Key;
                }
            }

            return -1;
        }

        public void Insert(int index, object value)
        {
            throw new NotSupportedException();
        }

        public void Remove(object value)
        {
            throw new NotSupportedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        public void CopyTo(Array array, int index)
        {
            foreach (var item in this)
            {
                array.SetValue(item, index++);
            }
        }

        public void EnsureViewport(GridViewport viewport, int totalCount)
        {
            var range = viewport.CalculateVisibleRows(totalCount);
            if (IsCovered(_loadedRange, range))
            {
                return;
            }

            QueueWindowLoad(range.Start, range.Length);
        }

        private void PrimeInitialWindow()
        {
            if (Count == 0)
            {
                return;
            }

            var initialItems = _source.GetVisibleWindowAsync(0, Math.Min(_source.PageSize, Count)).GetAwaiter().GetResult();
            for (var i = 0; i < initialItems.Count; i++)
            {
                _rows[i] = new GridDataRowModel(_owner, initialItems[i]);
            }

            _loadedRange = new GridRange(0, initialItems.Count);
        }

        private async void QueueWindowLoad(int startIndex, int size)
        {
            if (size <= 0)
            {
                return;
            }

            var pageStart = (startIndex / _source.PageSize) * _source.PageSize;
            lock (_gate)
            {
                if (_pendingPages.Contains(pageStart))
                {
                    return;
                }

                _pendingPages.Add(pageStart);
            }

            try
            {
                var items = await _source.GetVisibleWindowAsync(startIndex, size).ConfigureAwait(false);
                await _owner.Dispatcher.InvokeAsync(() =>
                {
                    for (var i = 0; i < items.Count; i++)
                    {
                        var index = startIndex + i;
                        if (index >= Count)
                        {
                            break;
                        }

                        _rows[index] = new GridDataRowModel(_owner, items[i]);
                    }

                    PruneOutsideWindow(startIndex, size);
                    _loadedRange = new GridRange(startIndex, Math.Min(size, Count - startIndex));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
                    CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                });
            }
            finally
            {
                lock (_gate)
                {
                    _pendingPages.Remove(pageStart);
                }
            }
        }

        private GridDisplayRowModel GetLoadedRowOrPlaceholder(int index)
        {
            lock (_gate)
            {
                return _rows.TryGetValue(index, out var row) ? row : _loadingRow;
            }
        }

        private void PruneOutsideWindow(int startIndex, int size)
        {
            var keepStart = Math.Max(0, startIndex - Math.Max(size, _source.PageSize));
            var keepEndExclusive = Math.Min(Count, startIndex + Math.Max(size * 2, _source.PageSize));
            var keysToRemove = _rows.Keys.Where(key => key < keepStart || key >= keepEndExclusive).ToArray();
            foreach (var key in keysToRemove)
            {
                _rows.Remove(key);
            }
        }

        private static bool IsCovered(GridRange loadedRange, GridRange requestedRange)
        {
            if (loadedRange.Length == 0)
            {
                return false;
            }

            return requestedRange.Start >= loadedRange.Start
                && requestedRange.EndExclusive <= loadedRange.EndExclusive;
        }
    }

    public sealed class GridVirtualizedGroupedRowCollection : IList, INotifyCollectionChanged, INotifyPropertyChanged
    {
        private readonly PhialeGrid _owner;
        private readonly GroupedQueryVirtualizedGridDataSource<object> _groupedSource;
        private readonly VirtualizedGridDataSource<GridGroupFlatRow<object>> _source;
        private readonly string _displayColumnId;
        private readonly GridLoadingRowModel _loadingRow;
        private readonly Dictionary<int, GridDisplayRowModel> _rows = new Dictionary<int, GridDisplayRowModel>();
        private readonly HashSet<int> _pendingPages = new HashSet<int>();
        private readonly object _gate = new object();
        private GridRange _loadedRange;

        public GridVirtualizedGroupedRowCollection(
            PhialeGrid owner,
            IReadOnlyList<object> sourceRows,
            GridFilterGroup filterGroup,
            IReadOnlyList<GridSortDescriptor> sorts,
            IReadOnlyList<GridGroupDescriptor> groups,
            IReadOnlyList<GridSummaryDescriptor> summaries,
            GridGroupExpansionState expansionState,
            string displayColumnId,
            bool collapseGroupsOnFirstLoad)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _displayColumnId = displayColumnId ?? string.Empty;

            var provider = new InMemoryGridQueryDataProvider(sourceRows, new DelegateGridRowAccessor<object>(owner.ResolveRowValue));
            _groupedSource = new GroupedQueryVirtualizedGridDataSource<object>(provider, 96)
            {
                FilterGroup = filterGroup ?? GridFilterGroup.EmptyAnd(),
                Sorts = sorts ?? Array.Empty<GridSortDescriptor>(),
                Groups = groups ?? Array.Empty<GridGroupDescriptor>(),
                Summaries = summaries ?? Array.Empty<GridSummaryDescriptor>(),
                ExpansionState = expansionState ?? new GridGroupExpansionState(),
            };

            _source = new VirtualizedGridDataSource<GridGroupFlatRow<object>>(_groupedSource, 96, 24, 1);
            _loadingRow = new GridLoadingRowModel(_displayColumnId, _owner.GetText(GridTextKeys.LoadingText));
            PrimeInitialWindow(collapseGroupsOnFirstLoad);
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public event PropertyChangedEventHandler PropertyChanged;

        public int Count { get; private set; }

        public int TotalItemCount { get; private set; }

        public int TopLevelGroupCount { get; private set; }

        public IReadOnlyList<string> GroupIds { get; private set; } = Array.Empty<string>();

        public GridSummarySet Summary { get; private set; } = GridSummarySet.Empty;

        public bool IsSynchronized => false;

        public object SyncRoot => this;

        public bool IsFixedSize => true;

        public bool IsReadOnly => true;

        public object this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                lock (_gate)
                {
                    if (_rows.TryGetValue(index, out var row))
                    {
                        return row;
                    }
                }

                return _loadingRow;
            }
            set => throw new NotSupportedException();
        }

        public IEnumerator GetEnumerator()
        {
            for (var index = 0; index < Count; index++)
            {
                yield return GetLoadedRowOrPlaceholder(index);
            }
        }

        public int Add(object value)
        {
            throw new NotSupportedException();
        }

        public void Clear()
        {
            throw new NotSupportedException();
        }

        public bool Contains(object value)
        {
            return IndexOf(value) >= 0;
        }

        public int IndexOf(object value)
        {
            if (!(value is GridDisplayRowModel displayRow))
            {
                return -1;
            }

            lock (_gate)
            {
                var match = _rows.FirstOrDefault(entry => ReferenceEquals(entry.Value, displayRow));
                return match.Value == null ? -1 : match.Key;
            }
        }

        public void Insert(int index, object value)
        {
            throw new NotSupportedException();
        }

        public void Remove(object value)
        {
            throw new NotSupportedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        public void CopyTo(Array array, int index)
        {
            foreach (var item in this)
            {
                array.SetValue(item, index++);
            }
        }

        public void EnsureViewport(GridViewport viewport)
        {
            if (Count == 0)
            {
                return;
            }

            var range = viewport.CalculateVisibleRows(Count);
            if (IsCovered(_loadedRange, range))
            {
                return;
            }

            QueueWindowLoad(range.Start, range.Length);
        }

        private void PrimeInitialWindow(bool collapseGroupsOnFirstLoad)
        {
            Count = _source.GetCountAsync().GetAwaiter().GetResult();
            if (collapseGroupsOnFirstLoad && _groupedSource.LastGroupIds.Count > 0)
            {
                foreach (var groupId in _groupedSource.LastGroupIds)
                {
                    _groupedSource.ExpansionState.SetExpanded(groupId, false);
                }

                _groupedSource.Invalidate();
                _source.Invalidate();
                Count = _source.GetCountAsync().GetAwaiter().GetResult();
            }

            UpdateMetadataFromSource();
            if (Count == 0)
            {
                return;
            }

            var initialRows = _source.GetVisibleWindowAsync(0, Math.Min(_source.PageSize, Count)).GetAwaiter().GetResult();
            StoreWindow(0, initialRows);
            _loadedRange = new GridRange(0, initialRows.Count);
        }

        private async void QueueWindowLoad(int startIndex, int size)
        {
            if (size <= 0 || Count == 0)
            {
                return;
            }

            var pageStart = (startIndex / _source.PageSize) * _source.PageSize;
            lock (_gate)
            {
                if (_pendingPages.Contains(pageStart))
                {
                    return;
                }

                _pendingPages.Add(pageStart);
            }

            try
            {
                var rows = await _source.GetVisibleWindowAsync(startIndex, size).ConfigureAwait(false);
                await _owner.Dispatcher.InvokeAsync(() =>
                {
                    StoreWindow(startIndex, rows);
                    PruneOutsideWindow(startIndex, size);
                    _loadedRange = new GridRange(startIndex, Math.Min(size, Count - startIndex));

                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
                    CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                }, DispatcherPriority.Background);
            }
            finally
            {
                lock (_gate)
                {
                    _pendingPages.Remove(pageStart);
                }
            }
        }

        private void StoreWindow(int startIndex, IReadOnlyList<GridGroupFlatRow<object>> rows)
        {
            for (var i = 0; i < rows.Count; i++)
            {
                var index = startIndex + i;
                if (index >= Count)
                {
                    break;
                }

                var row = rows[i];
                _rows[index] = row.Kind == GridGroupFlatRowKind.GroupHeader
                    ? (GridDisplayRowModel)new GridGroupHeaderRowModel(row.GroupId, row.GroupColumnId, row.GroupKey, row.GroupItemCount, row.Level, row.IsExpanded, _displayColumnId)
                    : new GridDataRowModel(_owner, row.Item, row.Level);
            }
        }

        private void UpdateMetadataFromSource()
        {
            TotalItemCount = _groupedSource.LastTotalItemCount;
            TopLevelGroupCount = _groupedSource.LastTopLevelGroupCount;
            GroupIds = _groupedSource.LastGroupIds.ToArray();
            Summary = _groupedSource.LastSummary;
        }

        private void PruneOutsideWindow(int startIndex, int size)
        {
            var keepStart = Math.Max(0, startIndex - Math.Max(size, _source.PageSize));
            var keepEndExclusive = Math.Min(Count, startIndex + Math.Max(size * 2, _source.PageSize));
            var keysToRemove = _rows.Keys.Where(key => key < keepStart || key >= keepEndExclusive).ToArray();
            foreach (var key in keysToRemove)
            {
                _rows.Remove(key);
            }
        }

        private static bool IsCovered(GridRange loadedRange, GridRange requestedRange)
        {
            if (loadedRange.Length == 0)
            {
                return false;
            }

            return requestedRange.Start >= loadedRange.Start
                && requestedRange.EndExclusive <= loadedRange.EndExclusive;
        }

        private GridDisplayRowModel GetLoadedRowOrPlaceholder(int index)
        {
            lock (_gate)
            {
                return _rows.TryGetValue(index, out var row) ? row : _loadingRow;
            }
        }
    }

    public sealed class GridCellBindingConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return string.Empty;
            }

            if (value is string text)
            {
                return text;
            }

            var valueType = parameter as Type;
            if (valueType == typeof(DateTime) && value is DateTime dateTime)
            {
                return dateTime.ToString("yyyy-MM-dd", culture);
            }

            if ((valueType == typeof(decimal) || valueType == typeof(double) || valueType == typeof(float)) && value is IFormattable formattableNumber)
            {
                return formattableNumber.ToString("N2", culture);
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var text = value as string ?? System.Convert.ToString(value, culture);
            var valueType = parameter as Type ?? typeof(string);
            if (string.IsNullOrWhiteSpace(text))
            {
                return valueType == typeof(string) ? string.Empty : null;
            }

            if (valueType == typeof(string))
            {
                return text;
            }

            if (valueType == typeof(DateTime))
            {
                return DateTime.Parse(text, culture);
            }

            return System.Convert.ChangeType(text, valueType, culture);
        }
    }

    public sealed class GridColumnBindingModel : INotifyPropertyChanged
    {
        private string _filterText;
        private string _sortGlyph = string.Empty;
        private string _sortOrderText = string.Empty;
        private ListSortDirection? _sortDirection;
        private double _width;

        public GridColumnBindingModel(GridColumnDefinition definition)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            _width = Math.Max(definition.Width, Math.Max(definition.MinWidth, 120d));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public GridColumnDefinition Definition { get; }

        public string ColumnId => Definition.Id;

        public string Header => Definition.Header;

        public double Width => _width;

        public double MinWidth => Definition.MinWidth;

        public Type ValueType => Definition.ValueType;

        public string ValueKind => Definition.ValueKind;

        public GridFieldValidationConstraints ValidationConstraints => Definition.ValidationConstraints;

        public GridColumnEditorKind EditorKind => Definition.EditorKind;

        public IReadOnlyList<string> EditorItems => Definition.EditorItems;

        public GridEditorItemsMode EditorItemsMode => Definition.EditorItemsMode;

        public string EditMask => Definition.EditMask;

        public string SortGlyph
        {
            get => _sortGlyph;
            private set
            {
                if (!string.Equals(_sortGlyph, value, StringComparison.Ordinal))
                {
                    _sortGlyph = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SortGlyph)));
                }
            }
        }

        public string SortOrderText
        {
            get => _sortOrderText;
            private set
            {
                if (!string.Equals(_sortOrderText, value, StringComparison.Ordinal))
                {
                    _sortOrderText = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SortOrderText)));
                }
            }
        }

        public ListSortDirection? SortDirection
        {
            get => _sortDirection;
            private set
            {
                if (_sortDirection != value)
                {
                    _sortDirection = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SortDirection)));
                }
            }
        }

        public string FilterText
        {
            get => _filterText;
            set
            {
                var normalizedValue = value ?? string.Empty;
                if (!string.Equals(_filterText, normalizedValue, StringComparison.Ordinal))
                {
                    _filterText = normalizedValue;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FilterText)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasFilterText)));
                }
            }
        }

        public bool HasFilterText => !string.IsNullOrWhiteSpace(_filterText);

        public void ApplySort(GridSortDirection direction, int sortIndex)
        {
            SortDirection = direction == GridSortDirection.Ascending ? ListSortDirection.Ascending : ListSortDirection.Descending;
            SortGlyph = direction == GridSortDirection.Ascending ? "▲" : "▼";
            SortOrderText = sortIndex >= 1 ? (sortIndex + 1).ToString(CultureInfo.InvariantCulture) : string.Empty;
        }

        public void ClearSort()
        {
            SortDirection = null;
            SortGlyph = string.Empty;
            SortOrderText = string.Empty;
        }

        public void UpdateWidth(double width)
        {
            var nextWidth = Math.Max(width, Math.Max(Definition.MinWidth, 120d));
            if (Math.Abs(_width - nextWidth) < 0.1d)
            {
                return;
            }

            _width = nextWidth;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Width)));
        }
    }

    public sealed class GridGroupChipModel
    {
        public GridGroupChipModel(string columnId, string displayText)
            : this(columnId, displayText, string.Empty, false)
        {
        }

        public GridGroupChipModel(string columnId, string displayText, string directionGlyph, bool hasFollowingGroup)
        {
            ColumnId = columnId;
            DisplayText = displayText;
            DirectionGlyph = directionGlyph;
            HasFollowingGroup = hasFollowingGroup;
        }

        public string ColumnId { get; }

        public string DisplayText { get; }

        public string DirectionGlyph { get; }

        public bool HasFollowingGroup { get; }
    }

    public sealed class GridSummaryDisplayItem
    {
        public GridSummaryDisplayItem(string columnLabel, string typeLabel, string valueText)
        {
            ColumnLabel = columnLabel ?? throw new ArgumentNullException(nameof(columnLabel));
            TypeLabel = typeLabel ?? throw new ArgumentNullException(nameof(typeLabel));
            ValueText = valueText ?? throw new ArgumentNullException(nameof(valueText));
            Label = string.IsNullOrWhiteSpace(TypeLabel)
                ? ColumnLabel
                : ColumnLabel + " " + TypeLabel;
        }

        public string Label { get; }

        public string ColumnLabel { get; }

        public string TypeLabel { get; }

        public string ValueText { get; }
    }

    internal sealed class GridCsvExportColumnMapping
    {
        public GridCsvExportColumnMapping(string header, string columnId)
        {
            Header = header ?? string.Empty;
            ColumnId = columnId ?? string.Empty;
        }

        public string Header { get; }

        public string ColumnId { get; }
    }

    internal sealed class UiActionCommand : ICommand
    {
        private readonly Action<object> _execute;

        public UiActionCommand(Action<object> execute)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        }

        public event EventHandler CanExecuteChanged
        {
            add { }
            remove { }
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            _execute(parameter);
        }
    }
}
