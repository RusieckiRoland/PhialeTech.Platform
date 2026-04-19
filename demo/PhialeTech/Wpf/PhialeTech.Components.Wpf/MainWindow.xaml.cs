using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;
using PhialeGrid.Core;
using PhialeGrid.Core.Regions;
using PhialeGrid.Core.State;
using PhialeTech.ComponentHost.State;
using PhialeTech.Components.Shared.Model;
using PhialeTech.Components.Shared.Services;
using PhialeTech.Yaml.Library;
using PhialeTech.Components.Shared.ViewModels;
using PhialeTech.MonacoEditor.Abstractions;
using PhialeTech.MonacoEditor.Wpf.Controls;
using PhialeTech.PhialeGrid.Wpf.Controls;
using PhialeTech.PhialeGrid.Wpf.Diagnostics;
using PhialeTech.PhialeGrid.Wpf.State;
using PhialeTech.WebHost.Wpf;
using PhialeTech.YamlApp.Abstractions.Enums;
using PhialeTech.YamlApp.Abstractions.Results;
using PhialeTech.YamlApp.Core.Normalization;
using PhialeTech.YamlApp.Core.Resolved;
using PhialeTech.YamlApp.Definitions.Documents;
using PhialeTech.YamlApp.Definitions.Fields;
using PhialeTech.YamlApp.Infrastructure.Loading;
using PhialeTech.YamlApp.Runtime.Model;
using PhialeTech.YamlApp.Runtime.Services;
using PhialeTech.YamlApp.Wpf.Document;

namespace PhialeTech.Components.Wpf
{
    public partial class MainWindow : Window
    {
        public static readonly DependencyProperty SelectedInputDensityModeProperty =
            DependencyProperty.Register(
                nameof(SelectedInputDensityMode),
                typeof(DensityMode),
                typeof(MainWindow),
                new PropertyMetadata(DensityMode.Normal, OnYamlDocumentPresentationPropertyChanged));

        public static readonly DependencyProperty SelectedInputInteractionModeProperty =
            DependencyProperty.Register(
                nameof(SelectedInputInteractionMode),
                typeof(InteractionMode),
                typeof(MainWindow),
                new PropertyMetadata(InteractionMode.Classic, OnYamlDocumentPresentationPropertyChanged));

        public static readonly DependencyProperty YamlDocumentRuntimeStateProperty =
            DependencyProperty.Register(
                nameof(YamlDocumentRuntimeState),
                typeof(RuntimeDocumentState),
                typeof(MainWindow),
                new PropertyMetadata(null));

        public static readonly DependencyProperty YamlDocumentLastResultTextProperty =
            DependencyProperty.Register(
                nameof(YamlDocumentLastResultText),
                typeof(string),
                typeof(MainWindow),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty YamlGeneratedFormSourceTextProperty =
            DependencyProperty.Register(
                nameof(YamlGeneratedFormSourceText),
                typeof(string),
                typeof(MainWindow),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty IsYamlGeneratedFormVisibleProperty =
            DependencyProperty.Register(
                nameof(IsYamlGeneratedFormVisible),
                typeof(bool),
                typeof(MainWindow),
                new PropertyMetadata(false));

        public static readonly DependencyProperty YamlGeneratedFormDiagnosticsTextProperty =
            DependencyProperty.Register(
                nameof(YamlGeneratedFormDiagnosticsText),
                typeof(string),
                typeof(MainWindow),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty HasYamlGeneratedFormDiagnosticsProperty =
            DependencyProperty.Register(
                nameof(HasYamlGeneratedFormDiagnostics),
                typeof(bool),
                typeof(MainWindow),
                new PropertyMetadata(false));

        private enum ScenarioStatusArea
        {
            Selection,
            Editing,
            Constraint,
        }

        private const int WmLButtonDown = 0x0201;
        private readonly DemoShellViewModel _viewModel;
        private readonly GridNamedViewCatalog _namedViewCatalog = new GridNamedViewCatalog();
        private static readonly Uri DemoThemeDayUri = new Uri("pack://application:,,,/PhialeTech.Styles.Wpf;component/Themes/Demo.Theme.Day.xaml", UriKind.Absolute);
        private static readonly Uri DemoThemeNightUri = new Uri("pack://application:,,,/PhialeTech.Styles.Wpf;component/Themes/Demo.Theme.Night.xaml", UriKind.Absolute);
        private const string SelectionCurrentRowId = "DZ-KRA-STA-0001";
        private const string SelectionMultiCurrentRowId = "BLD-WRO-FAB-0002";
        private static readonly string[] MultiSelectScenarioRowIds = { "DZ-KRA-STA-0001", "BLD-WRO-FAB-0002", "WAT-POZ-JEZ-0004" };
        private const string EditingCurrentRowId = "RD-GDA-OLI-0003";
        private const string EditingEditedRowId = "DZ-KRA-STA-0001";
        private const string EditingInvalidRowId = "BLD-WRO-FAB-0002";
        private readonly DemoApplicationServices _applicationServices;
        private readonly bool _ownsApplicationServices;
        private ApplicationStateRegistration _gridStateRegistration;
        private PhialeGridViewStateComponent _gridStateComponent;
        private string _registeredGridStateKey = string.Empty;
        private int _gridActivationSequence;
        private EventHandler _pendingFirstRenderProbe;
        private WebHostShowcaseView _webHostShowcaseView;
        private PdfViewerShowcaseView _pdfViewerShowcaseView;
        private ReportDesignerShowcaseView _reportDesignerShowcaseView;
        private MonacoEditorShowcaseView _monacoEditorShowcaseView;
        private PhialeMonacoEditor _yamlGeneratedFormMonacoEditor;
        private IWebDemoFocusModeSource _activeWebDemoFocusModeSource;
        private bool _isWebDemoChromeCollapsed;
        private bool _webHostPrewarmQueued;
        private WebHostLoadTrace _webHostLoadTrace;
        private int _webComponentsClickTimestamp = -1;
        private bool _isUpdatingYamlGeneratedFormEditor;

        public IReadOnlyList<DensityMode> InputDensityOptions { get; } =
            new[] { DensityMode.Compact, DensityMode.Normal, DensityMode.Comfortable };

        public IReadOnlyList<InteractionMode> InputInteractionOptions { get; } =
            new[] { InteractionMode.Classic, InteractionMode.Touch };

        public DensityMode SelectedInputDensityMode
        {
            get => (DensityMode)GetValue(SelectedInputDensityModeProperty);
            set => SetValue(SelectedInputDensityModeProperty, value);
        }

        public InteractionMode SelectedInputInteractionMode
        {
            get => (InteractionMode)GetValue(SelectedInputInteractionModeProperty);
            set => SetValue(SelectedInputInteractionModeProperty, value);
        }

        public RuntimeDocumentState YamlDocumentRuntimeState
        {
            get => (RuntimeDocumentState)GetValue(YamlDocumentRuntimeStateProperty);
            set => SetValue(YamlDocumentRuntimeStateProperty, value);
        }

        public string YamlDocumentLastResultText
        {
            get => (string)GetValue(YamlDocumentLastResultTextProperty);
            set => SetValue(YamlDocumentLastResultTextProperty, value);
        }

        public string YamlGeneratedFormSourceText
        {
            get => (string)GetValue(YamlGeneratedFormSourceTextProperty);
            set => SetValue(YamlGeneratedFormSourceTextProperty, value);
        }

        public bool IsYamlGeneratedFormVisible
        {
            get => (bool)GetValue(IsYamlGeneratedFormVisibleProperty);
            set => SetValue(IsYamlGeneratedFormVisibleProperty, value);
        }

        public string YamlGeneratedFormDiagnosticsText
        {
            get => (string)GetValue(YamlGeneratedFormDiagnosticsTextProperty);
            set => SetValue(YamlGeneratedFormDiagnosticsTextProperty, value);
        }

        public bool HasYamlGeneratedFormDiagnostics
        {
            get => (bool)GetValue(HasYamlGeneratedFormDiagnosticsProperty);
            set => SetValue(HasYamlGeneratedFormDiagnosticsProperty, value);
        }

        public MainWindow()
            : this(DemoApplicationServices.CreateIsolatedForWindow(), true)
        {
        }

        public MainWindow(DemoApplicationServices applicationServices, bool ownsApplicationServices = false)
        {
            InitializeComponent();
            _applicationServices = applicationServices ?? throw new ArgumentNullException(nameof(applicationServices));
            _ownsApplicationServices = ownsApplicationServices;
            AddHandler(UIElement.PreviewMouseDownEvent, new MouseButtonEventHandler(DebugObserveWindowPreviewMouseDown), true);
            SourceInitialized += HandleSourceInitialized;
            _viewModel = new DemoShellViewModel(
                "Wpf",
                remoteGridClient: CreateRemoteGridClient(),
                definitionManager: _applicationServices.DefinitionManager);
            _viewModel.PropertyChanged += HandleViewModelPropertyChanged;
            DataContext = _viewModel;
            var gridLanguageDirectory = Path.Combine(AppContext.BaseDirectory, "PhialeGrid.Localization", "Languages");
            DemoGrid.LanguageDirectory = gridLanguageDirectory;
            ApplicationStateDemoGrid.LanguageDirectory = gridLanguageDirectory;
            DemoActiveLayerSelector.LanguageDirectory = Path.Combine(AppContext.BaseDirectory, "PhialeTech.ActiveLayerSelector", "Languages");
            ApplySelectedTheme();
            Loaded += HandleWindowLoaded;
            Closed += HandleWindowClosed;
            if (ExampleTabControl != null)
            {
                ExampleTabControl.SelectionChanged += HandleExampleTabSelectionChanged;
            }

            RebuildYamlDocumentRuntimeState();
        }

        private void HandleWindowLoaded(object sender, RoutedEventArgs e)
        {
            LogGridStateLifecycle("Window loaded. Scheduling grid state registration refresh.");
            if (_viewModel.ShowSelectionTools || _viewModel.ShowEditingTools || _viewModel.ShowConstraintTools)
            {
                Dispatcher.BeginInvoke(new Action(ActivateCurrentExampleStateAndScenario), DispatcherPriority.DataBind);
                return;
            }

            QueueGridStateRegistrationRefresh();
            QueueWebHostPrewarm();
        }

        private void HandleWindowClosed(object sender, EventArgs e)
        {
            SaveAndDetachGridStateRegistration();
            _webHostShowcaseView?.Dispose();
            _webHostShowcaseView = null;
            _pdfViewerShowcaseView?.Dispose();
            _pdfViewerShowcaseView = null;
            _reportDesignerShowcaseView?.Dispose();
            _reportDesignerShowcaseView = null;
            _monacoEditorShowcaseView?.Dispose();
            _monacoEditorShowcaseView = null;
            if (_yamlGeneratedFormMonacoEditor != null)
            {
                _yamlGeneratedFormMonacoEditor.ContentChanged -= HandleYamlGeneratedFormMonacoContentChanged;
                _yamlGeneratedFormMonacoEditor.Dispose();
                _yamlGeneratedFormMonacoEditor = null;
            }
            if (ExampleTabControl != null)
            {
                ExampleTabControl.SelectionChanged -= HandleExampleTabSelectionChanged;
            }
            if (_ownsApplicationServices)
            {
                _applicationServices.Dispose();
            }
        }

        private void QueueGridStateRegistrationRefresh()
        {
            LogGridStateLifecycle("QueueGridStateRegistrationRefresh requested.");
            Dispatcher.BeginInvoke(new Action(() => AttachGridStateRegistrationForCurrentExample()), DispatcherPriority.DataBind);
        }

        private bool AttachGridStateRegistrationForCurrentExample()
        {
            var activeGrid = GetCurrentStateGrid();
            if (activeGrid == null)
            {
                LogGridStateLifecycle("AttachGridStateRegistrationForCurrentExample skipped because there is no active grid.");
                return false;
            }

            var stateKey = ResolveCurrentGridStateKey();
            LogGridStateLifecycle($"AttachGridStateRegistrationForCurrentExample started. ActiveGrid={DescribeGrid(activeGrid)}, StateKey='{stateKey}', RegisteredKey='{_registeredGridStateKey}'.");
            if (string.IsNullOrWhiteSpace(stateKey))
            {
                SaveAndDetachGridStateRegistration();
                SyncToolbarStateFromViewState(activeGrid.ExportViewState());
                _viewModel.MarkStateCleared();
                LogGridStateLifecycle("No state key resolved. Registration detached and toolbar synced from current view state.");
                return false;
            }

            if (string.Equals(_registeredGridStateKey, stateKey, StringComparison.Ordinal))
            {
                LogGridStateLifecycle("AttachGridStateRegistrationForCurrentExample skipped because the requested state key is already registered.");
                return _gridStateRegistration?.RestoredFromStore == true;
            }

            SaveAndDetachGridStateRegistration();

            _gridStateComponent = new PhialeGridViewStateComponent(activeGrid);
            LogGridStateLifecycle("Registering grid state component.");
            _gridStateRegistration = _applicationServices.ApplicationStateManager.Register(stateKey, _gridStateComponent);
            _registeredGridStateKey = stateKey;
            SyncToolbarStateFromViewState(activeGrid.ExportViewState());
            _viewModel.GridSearchText = activeGrid.GlobalSearchText;
            LogGridStateLifecycle($"Grid state registration completed. RestoredFromStore={_gridStateRegistration.RestoredFromStore}. CurrentSearch='{activeGrid.GlobalSearchText}'.");

            if (_gridStateRegistration.RestoredFromStore)
            {
                _viewModel.MarkStateSaved();
            }
            else
            {
                _viewModel.MarkStateCleared();
            }

            return _gridStateRegistration.RestoredFromStore;
        }

        private void SaveAndDetachGridStateRegistration()
        {
            if (!string.IsNullOrWhiteSpace(_registeredGridStateKey))
            {
                try
                {
                    LogGridStateLifecycle($"Saving registered grid state before detach. StateKey='{_registeredGridStateKey}'.");
                    _applicationServices.ApplicationStateManager.SaveRegisteredState(_registeredGridStateKey);
                }
                catch (InvalidOperationException)
                {
                    LogGridStateLifecycle($"SaveRegisteredState threw InvalidOperationException for key '{_registeredGridStateKey}'.");
                }
            }

            _gridStateRegistration?.Dispose();
            _gridStateRegistration = null;
            _gridStateComponent?.Dispose();
            _gridStateComponent = null;
            if (!string.IsNullOrWhiteSpace(_registeredGridStateKey))
            {
                LogGridStateLifecycle($"Detached grid state registration for key '{_registeredGridStateKey}'.");
            }
            _registeredGridStateKey = string.Empty;
        }

        private PhialeTech.PhialeGrid.Wpf.Controls.PhialeGrid GetCurrentStateGrid()
        {
            if (_viewModel.ShowApplicationStateManagerSurface)
            {
                return ApplicationStateDemoGrid;
            }

            if (_viewModel.ShowGridSurface)
            {
                return DemoGrid;
            }

            return null;
        }

        private string ResolveCurrentGridStateKey()
        {
            if (_viewModel.SelectedExample == null || GetCurrentStateGrid() == null)
            {
                return string.Empty;
            }

            return DemoApplicationStateKeys.ForGridExample(_viewModel.SelectedExample.Id);
        }

        private void DebugObserveWindowPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e == null || e.ChangedButton != MouseButton.Left)
            {
                return;
            }

            var originalSource = e.OriginalSource as DependencyObject;
            var source = e.Source as DependencyObject;
            var directlyOver = Mouse.DirectlyOver as DependencyObject;

            _ = originalSource;
            _ = source;
            _ = directlyOver;
            _ = e.Handled;
            _ = e.RoutedEvent;
        }

        private void HandleSourceInitialized(object sender, EventArgs e)
        {
            if (PresentationSource.FromVisual(this) is HwndSource hwndSource)
            {
                hwndSource.AddHook(DebugObserveWindowWndProc);
            }
        }

        private IntPtr DebugObserveWindowWndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg != WmLButtonDown)
            {
                return IntPtr.Zero;
            }

            var screenPoint = new Point((short)(lParam.ToInt32() & 0xFFFF), (short)((lParam.ToInt32() >> 16) & 0xFFFF));
            var elementUnderMouse = InputHitTest(screenPoint) as DependencyObject;

            _ = elementUnderMouse;
            _ = handled;
            _ = hwnd;
            _ = wParam;
            _ = lParam;

            return IntPtr.Zero;
        }

        private static IDemoRemoteGridClient CreateRemoteGridClient()
        {
            var baseAddress = Environment.GetEnvironmentVariable("PHIALEGRID_REMOTE_BASE_URL");
            if (string.IsNullOrWhiteSpace(baseAddress))
            {
                baseAddress = "http://127.0.0.1:5080/";
            }

            return new DemoRemoteGridHttpClient(new HttpClient(), baseAddress);
        }

        private void HandleCopySelectionClick(object sender, RoutedEventArgs e)
        {
            DemoGrid?.CopySelectionToClipboard();
        }

        private void HandleSelectVisibleRowsClick(object sender, RoutedEventArgs e)
        {
            DemoGrid?.SelectVisibleRows();
        }

        private void HandleClearSelectionClick(object sender, RoutedEventArgs e)
        {
            DemoGrid?.ClearSelection();
        }

        private void HandleShowSingleCurrentRowScenarioClick(object sender, RoutedEventArgs e)
        {
            ApplySelectionScenarioToGrid(enableMultiSelect: false);
        }

        private void HandleShowMultiSelectScenarioClick(object sender, RoutedEventArgs e)
        {
            ApplySelectionScenarioToGrid(enableMultiSelect: true);
        }

        private void HandleDesignFoundationsComponentClick(object sender, RoutedEventArgs e)
        {
            _viewModel.SelectDrawerGroup("foundations");
        }

        private void RefreshDemoGridRegionOptions()
        {
            if (DemoGrid == null)
            {
                return;
            }

            var hideGroupingRegion = _viewModel?.ShowEditingTools == true;
            var hideSummaryBottomRegion = _viewModel?.ShowEditingTools == true;

            DemoGrid.SetRegionVisibility(GridRegionKind.SideToolRegion, true);
            DemoGrid.SetRegionVisibility(GridRegionKind.GroupingRegion, !hideGroupingRegion);
            DemoGrid.SetRegionVisibility(GridRegionKind.SummaryBottomRegion, !hideSummaryBottomRegion);
            EnsureSummaryDesignerRegionVisible();
        }

        private void EnsureSummaryDesignerRegionVisible()
        {
            if (DemoGrid == null || _viewModel == null || !_viewModel.ShowSummaryDesignerTools)
            {
                return;
            }

            DemoGrid.SetRegionVisibility(GridRegionKind.SideToolRegion, true);
        }

        private void HandleSaveFoundationsPdfClick(object sender, RoutedEventArgs e)
        {
            if (FoundationsSurfaceContentPanel == null || !_viewModel.ShowFoundationsSurface)
            {
                return;
            }

            var dialog = new SaveFileDialog
            {
                Title = _viewModel.SaveFoundationsPdfText,
                Filter = "PDF (*.pdf)|*.pdf",
                DefaultExt = ".pdf",
                AddExtension = true,
                OverwritePrompt = true,
                FileName = BuildFoundationsPdfFileName()
            };

            if (dialog.ShowDialog(this) != true)
            {
                return;
            }

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                FoundationsSurfaceScrollViewer?.UpdateLayout();
                FoundationsSurfaceContentPanel.UpdateLayout();
                WpfVisualPdfExporter.ExportFrameworkElementToPdf(
                    FoundationsSurfaceContentPanel,
                    dialog.FileName,
                    _viewModel.SelectedExampleTitle);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
                    _viewModel.FoundationsExportErrorMessage + Environment.NewLine + Environment.NewLine + ex.Message,
                    _viewModel.FoundationsExportErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void HandleArchitectureComponentClick(object sender, RoutedEventArgs e)
        {
            _viewModel.SelectDrawerGroup("architecture");
        }

        private static string BuildFoundationsPdfFileName()
        {
            return "design-foundations-" + DateTime.Now.ToString("yyyy-MM-dd-HHmm") + ".pdf";
        }

        private async void HandleSaveDemoBookPdfClick(object sender, RoutedEventArgs e)
        {
            if (!_viewModel.ShowLicenseSurface)
            {
                return;
            }

            var dialog = new SaveFileDialog
            {
                Title = _viewModel.SaveDemoBookPdfText,
                Filter = "PDF (*.pdf)|*.pdf",
                DefaultExt = ".pdf",
                AddExtension = true,
                OverwritePrompt = true,
                FileName = BuildDemoBookPdfFileName()
            };

            if (dialog.ShowDialog(this) != true)
            {
                return;
            }

            var previousExampleId = _viewModel.SelectedExample?.Id;
            var previousTabIndex = _viewModel.SelectedTabIndex;
            var wasOverviewVisible = _viewModel.SelectedExample == null;

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                var captures = await CaptureDemoBookAsync().ConfigureAwait(true);
                new WpfDemoBookPdfExporter().Export(
                    dialog.FileName,
                    "PhialeTech Demo",
                    captures);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
                    _viewModel.DemoBookExportErrorMessage + Environment.NewLine + Environment.NewLine + ex.Message,
                    _viewModel.DemoBookExportErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                await RestoreDemoSelectionAsync(wasOverviewVisible, previousExampleId, previousTabIndex).ConfigureAwait(true);
                Mouse.OverrideCursor = null;
            }
        }

        private void HandleGridComponentClick(object sender, RoutedEventArgs e)
        {
            _viewModel.SelectDrawerGroup("grid");
        }

        private void HandleActiveLayerSelectorComponentClick(object sender, RoutedEventArgs e)
        {
            _viewModel.SelectDrawerGroup("active-layer-selector");
        }

        private void HandleWebComponentsComponentClick(object sender, RoutedEventArgs e)
        {
            StartWebHostLoadTrace("drawer click");
            LogWebComponentsInputLag();
            TraceNextRender("First render after WebComponents click");
            _viewModel.SelectDrawerGroup("web-components");
            _webHostLoadTrace?.Mark("SelectDrawerGroup returned");
        }

        private void HandleWebComponentsComponentPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _webComponentsClickTimestamp = e?.Timestamp ?? -1;
        }

        private void HandleCommitEditsClick(object sender, RoutedEventArgs e)
        {
            DemoGrid?.CommitEdits();
        }

        private void HandleAddDemoRecordClick(object sender, RoutedEventArgs e)
        {
            if (DemoGrid == null || _viewModel == null)
            {
                return;
            }

            IReadOnlyList<DemoGisRecordViewModel> source = _viewModel.GridRecords ?? Array.Empty<DemoGisRecordViewModel>();
            var template = source.FirstOrDefault();
            if (template == null)
            {
                return;
            }

            var record = new DemoGisRecordViewModel(
                Guid.NewGuid().ToString("N"),
                template.Category,
                "Nowy obiekt roboczy",
                template.GeometryType,
                template.Crs,
                template.Municipality,
                template.District,
                "Planned",
                template.AreaSquareMeters,
                template.LengthMeters,
                DateTime.Today,
                template.Source,
                "Medium",
                true,
                true,
                "UX Draft",
                Math.Max(500, template.ScaleHint),
                template.Tags);

            var records = new[] { record }
                .Concat(source.Select(item => (DemoGisRecordViewModel)item.Clone()))
                .ToArray();
            _viewModel.ReplaceGridRecords(records);
            Dispatcher.BeginInvoke(new Action(() =>
            {
                DemoGrid.FocusRow(record.ObjectId, "ObjectName");
                DemoGrid.ScrollRowIntoView(record.ObjectId, GridScrollAlignment.Start);
            }), DispatcherPriority.DataBind);
        }

        private void HandleBeginCurrentEditClick(object sender, RoutedEventArgs e)
        {
            DemoGrid?.BeginEditCurrentCell();
        }

        private void HandlePostCurrentEditClick(object sender, RoutedEventArgs e)
        {
            DemoGrid?.PostCurrentEdit();
        }

        private void HandleCancelCurrentEditClick(object sender, RoutedEventArgs e)
        {
            DemoGrid?.CancelCurrentEdit();
        }

        private void HandleCancelEditsClick(object sender, RoutedEventArgs e)
        {
            DemoGrid?.CancelEdits();
        }

        private void HandleScrollEditedRowDemoClick(object sender, RoutedEventArgs e)
        {
            if (DemoGrid == null || !_viewModel.ShowEditingTools)
            {
                return;
            }

            EnsureEditingNavigationColumnsVisible();
            var targetRowId = ResolveNextNavigationRowId(DemoGrid.PendingEditRowIdsWithoutValidation);
            var result = !string.IsNullOrWhiteSpace(targetRowId) &&
                DemoGrid.ScrollCellIntoView(targetRowId, "ObjectName", GridScrollAlignment.Start, setCurrentCell: true);
            UpdateScenarioStatus(
                ScenarioStatusArea.Editing,
                result
                    ? "Scroll row demo: the grid moved to the next edited row and made its edited Object name cell current."
                    : "Scroll row demo found no edited row without validation issues in the current grid state.");
        }

        private void HandleScrollErrorCellDemoClick(object sender, RoutedEventArgs e)
        {
            if (DemoGrid == null || !_viewModel.ShowEditingTools)
            {
                return;
            }

            EnsureEditingNavigationColumnsVisible();
            var targetRowId = ResolveNextNavigationRowId(DemoGrid.ValidationIssueRowIds);
            var targetColumnId = string.IsNullOrWhiteSpace(targetRowId)
                ? string.Empty
                : DemoGrid.GetPrimaryValidationColumnId(targetRowId);
            if (string.IsNullOrWhiteSpace(targetColumnId))
            {
                targetColumnId = "Owner";
            }

            var result = !string.IsNullOrWhiteSpace(targetRowId) &&
                DemoGrid.ScrollCellIntoView(targetRowId, targetColumnId, GridScrollAlignment.Start, setCurrentCell: true);
            if (!result && DemoGrid.HasGroups && !string.IsNullOrWhiteSpace(targetRowId))
            {
                DemoGrid.ExpandAllGroups();
                result = DemoGrid.ScrollCellIntoView(targetRowId, targetColumnId, GridScrollAlignment.Start, setCurrentCell: true);
            }

            UpdateScenarioStatus(
                ScenarioStatusArea.Editing,
                result
                    ? "Scroll cell demo: the grid moved to the next invalid cell in the current grid state."
                    : "Scroll cell demo found no invalid row to navigate to in the current grid state.");
        }

        private void HandleScrollFarColumnDemoClick(object sender, RoutedEventArgs e)
        {
            if (DemoGrid == null || !_viewModel.ShowEditingTools)
            {
                return;
            }

            EnsureEditingNavigationColumnsVisible();
            var targetRowId = ResolveLastColumnTargetRowId();
            var result = !string.IsNullOrWhiteSpace(targetRowId) &&
                DemoGrid.ScrollCellIntoView(targetRowId, "ScaleHint", GridScrollAlignment.End, setCurrentCell: true);
            UpdateScenarioStatus(
                ScenarioStatusArea.Editing,
                result
                    ? "Scroll column demo: the grid kept the current navigation context and revealed the last editable column."
                    : "Scroll column demo could not reveal the last column because there is no row to anchor the navigation.");
        }

        private void EnsureEditingNavigationColumnsVisible()
        {
            DemoGrid.SetColumnVisibility("Owner", true);
            DemoGrid.SetColumnVisibility("ScaleHint", true);
        }

        private string ResolveNextNavigationRowId(IReadOnlyList<string> candidateRowIds)
        {
            if (candidateRowIds == null || candidateRowIds.Count == 0)
            {
                return string.Empty;
            }

            var currentRowId = DemoGrid.CurrentDataRowId;
            if (string.IsNullOrWhiteSpace(currentRowId))
            {
                return candidateRowIds[0];
            }

            var currentIndex = candidateRowIds
                .Select((rowId, index) => new { rowId, index })
                .FirstOrDefault(entry => string.Equals(entry.rowId, currentRowId, StringComparison.OrdinalIgnoreCase))
                ?.index ?? -1;

            if (currentIndex < 0)
            {
                return candidateRowIds[0];
            }

            return candidateRowIds[(currentIndex + 1) % candidateRowIds.Count];
        }

        private string ResolveLastColumnTargetRowId()
        {
            if (!string.IsNullOrWhiteSpace(DemoGrid.CurrentDataRowId))
            {
                return DemoGrid.CurrentDataRowId;
            }

            var editedRowId = DemoGrid.PendingEditRowIdsWithoutValidation.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(editedRowId))
            {
                return editedRowId;
            }

            return DemoGrid.ValidationIssueRowIds.FirstOrDefault() ?? string.Empty;
        }

        private void HandleShowRowStateBaselineScenarioClick(object sender, RoutedEventArgs e)
        {
            ApplyEditingScenarioToGrid(GridEditingScenario.Baseline);
        }

        private void HandleShowEditedPriorityScenarioClick(object sender, RoutedEventArgs e)
        {
            ApplyEditingScenarioToGrid(GridEditingScenario.EditedWinsOverCurrent);
        }

        private void HandleShowInvalidPriorityScenarioClick(object sender, RoutedEventArgs e)
        {
            ApplyEditingScenarioToGrid(GridEditingScenario.InvalidWinsOverEditedAndCurrent);
        }

        private void HandleResetRowStateScenarioClick(object sender, RoutedEventArgs e)
        {
            ResetEditingScenarioToCleanCurrentRow();
        }

        private void HandleSaveStateClick(object sender, RoutedEventArgs e)
        {
            var activeGrid = GetCurrentStateGrid();
            if (activeGrid == null || string.IsNullOrWhiteSpace(_registeredGridStateKey))
            {
                return;
            }

            LogGridStateLifecycle($"Manual SaveRegisteredState requested. Grid={DescribeGrid(activeGrid)}, StateKey='{_registeredGridStateKey}'.");
            _applicationServices.ApplicationStateManager.SaveRegisteredState(_registeredGridStateKey);
            SyncToolbarStateFromViewState(activeGrid.ExportViewState());
            _viewModel.GridSearchText = activeGrid.GlobalSearchText;
            _viewModel.MarkStateSaved();
        }

        private void HandleRestoreStateClick(object sender, RoutedEventArgs e)
        {
            var activeGrid = GetCurrentStateGrid();
            if (activeGrid == null || string.IsNullOrWhiteSpace(_registeredGridStateKey))
            {
                return;
            }

            LogGridStateLifecycle($"Manual TryRestoreRegisteredState requested. Grid={DescribeGrid(activeGrid)}, StateKey='{_registeredGridStateKey}'.");
            if (!_applicationServices.ApplicationStateManager.TryRestoreRegisteredState(_registeredGridStateKey))
            {
                LogGridStateLifecycle($"Manual restore found no saved state for key '{_registeredGridStateKey}'.");
                return;
            }

            SyncToolbarStateFromViewState(activeGrid.ExportViewState());
            _viewModel.GridSearchText = activeGrid.GlobalSearchText;
            _viewModel.MarkStateSaved();
            LogGridStateLifecycle($"Manual restore succeeded. Grid={DescribeGrid(activeGrid)}, Search='{activeGrid.GlobalSearchText}'.");
        }

        private void HandleResetStateClick(object sender, RoutedEventArgs e)
        {
            var activeGrid = GetCurrentStateGrid();
            if (activeGrid == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(_registeredGridStateKey))
            {
                _applicationServices.ApplicationStateManager.Delete(_registeredGridStateKey);
            }

            activeGrid.ResetState();
            SyncToolbarStateFromViewState(activeGrid.ExportViewState());
            _viewModel.GridSearchText = activeGrid.GlobalSearchText;
            _viewModel.MarkStateCleared();
        }

        private void HandleApplySearchClick(object sender, RoutedEventArgs e)
        {
            DemoGrid?.ApplyGlobalSearch(_viewModel.GridSearchText);
        }

        private void HandleClearSearchClick(object sender, RoutedEventArgs e)
        {
            _viewModel.GridSearchText = string.Empty;
            DemoGrid?.ClearGlobalSearch();
        }

        private void HandleFocusMunicipalityFilterClick(object sender, RoutedEventArgs e)
        {
            DemoGrid?.FocusColumnFilter("Municipality");
        }

        private void HandleFocusOwnerFilterClick(object sender, RoutedEventArgs e)
        {
            DemoGrid?.FocusColumnFilter("Owner");
        }

        private void HandleClearColumnFiltersClick(object sender, RoutedEventArgs e)
        {
            DemoGrid?.ClearFilters();
        }

        private async void HandlePreviousRemotePageClick(object sender, RoutedEventArgs e)
        {
            await _viewModel.LoadPreviousRemotePageAsync();
        }

        private async void HandleNextRemotePageClick(object sender, RoutedEventArgs e)
        {
            await _viewModel.LoadNextRemotePageAsync();
        }

        private async void HandleRefreshRemoteClick(object sender, RoutedEventArgs e)
        {
            await _viewModel.RefreshRemotePageAsync();
        }

        private async void HandleExpandHierarchyClick(object sender, RoutedEventArgs e)
        {
            if (DemoGrid != null)
            {
                await DemoGrid.ExpandAllHierarchyAsync();
            }
        }

        private void HandleCollapseHierarchyClick(object sender, RoutedEventArgs e)
        {
            DemoGrid?.CollapseAllHierarchy();
        }

        private void HandleToggleMasterDetailPlacementClick(object sender, RoutedEventArgs e)
        {
            _viewModel.ToggleMasterDetailPlacement();
        }

        private void HandleSaveViewClick(object sender, RoutedEventArgs e)
        {
            if (DemoGrid == null || string.IsNullOrWhiteSpace(_viewModel.PendingViewName))
            {
                return;
            }

            _namedViewCatalog.Save(new GridNamedViewDefinition(
                _viewModel.PendingViewName.Trim(),
                DemoGrid.SaveState()));

            _viewModel.SetSavedViewNames(_namedViewCatalog.Names);
            _viewModel.SelectedSavedViewName = _viewModel.PendingViewName.Trim();
            _viewModel.PendingViewName = string.Empty;
        }

        private void HandleApplyViewClick(object sender, RoutedEventArgs e)
        {
            if (DemoGrid == null || string.IsNullOrWhiteSpace(_viewModel.SelectedSavedViewName))
            {
                return;
            }

            if (!_namedViewCatalog.TryGet(_viewModel.SelectedSavedViewName, out var namedView))
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(namedView.GridState))
            {
                DemoGrid.LoadState(namedView.GridState);
                SyncToolbarStateFromSnapshot(namedView.GridState);
            }

            _viewModel.GridSearchText = DemoGrid.GlobalSearchText;
        }

        private void HandleDeleteViewClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_viewModel.SelectedSavedViewName))
            {
                return;
            }

            _namedViewCatalog.Remove(_viewModel.SelectedSavedViewName);
            _viewModel.SetSavedViewNames(_namedViewCatalog.Names);
        }

        private void HandleAddSummaryClick(object sender, RoutedEventArgs e)
        {
            _viewModel.AddSelectedSummary();
        }

        private void HandleRemoveSummaryClick(object sender, RoutedEventArgs e)
        {
            if (!(sender is FrameworkElement frameworkElement) || !(frameworkElement.Tag is DemoConfiguredSummaryViewModel summary))
            {
                return;
            }

            _viewModel.RemoveSummary(summary.ColumnId, summary.Type);
        }

        private void HandleResetSummariesClick(object sender, RoutedEventArgs e)
        {
            _viewModel.ResetSummaries();
        }

        private void HandleExportCsvClick(object sender, RoutedEventArgs e)
        {
            if (DemoGrid == null)
            {
                return;
            }

            var csv = DemoGrid.ExportCurrentViewToCsv();
            var exportedRowCount = DemoGrid.RowsView.Cast<object>()
                .OfType<GridDisplayRowModel>()
                .Count(row => row.SourceRow != null);
            _viewModel.MarkTransferExported(exportedRowCount, DemoGrid.VisibleColumns.Count, csv);
        }

        private void HandleImportSampleCsvClick(object sender, RoutedEventArgs e)
        {
            if (DemoGrid == null)
            {
                return;
            }

            var sampleCsv = _viewModel.BuildSampleImportCsv();
            var importedRecords = DemoGisCsvTransferService.Import(sampleCsv, _viewModel.GridColumns);
            DemoGrid.ClearGlobalSearch();
            DemoGrid.ResetState();
            _viewModel.GridSearchText = DemoGrid.GlobalSearchText;
            _viewModel.ReplaceGridRecords(importedRecords);
            _viewModel.MarkTransferImported(importedRecords.Count, sampleCsv);
        }

        private void HandleRestoreSourceClick(object sender, RoutedEventArgs e)
        {
            if (DemoGrid == null)
            {
                return;
            }

            DemoGrid.ClearGlobalSearch();
            DemoGrid.ResetState();
            _viewModel.GridSearchText = DemoGrid.GlobalSearchText;
            _viewModel.RestoreDefaultGridRecords();
            _viewModel.MarkTransferRestored(_viewModel.GridRecords.Count);
        }

        private async void HandleViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DemoShellViewModel.SelectedThemeCode))
            {
                ApplySelectedTheme();
                if (_reportDesignerShowcaseView != null)
                {
                    await _reportDesignerShowcaseView.ApplyEnvironmentAsync(_viewModel.LanguageCode, ResolveReportDesignerTheme()).ConfigureAwait(true);
                }
                if (_monacoEditorShowcaseView != null)
                {
                    await _monacoEditorShowcaseView.ApplyEnvironmentAsync(_viewModel.LanguageCode, ResolveReportDesignerTheme()).ConfigureAwait(true);
                }
                if (_yamlGeneratedFormMonacoEditor != null)
                {
                    await _yamlGeneratedFormMonacoEditor.SetThemeAsync(ResolveReportDesignerTheme()).ConfigureAwait(true);
                }
                return;
            }

            if (e.PropertyName == nameof(DemoShellViewModel.LanguageCode))
            {
                if (_reportDesignerShowcaseView != null)
                {
                    await _reportDesignerShowcaseView.ApplyEnvironmentAsync(_viewModel.LanguageCode, ResolveReportDesignerTheme()).ConfigureAwait(true);
                }
                if (_monacoEditorShowcaseView != null)
                {
                    await _monacoEditorShowcaseView.ApplyEnvironmentAsync(_viewModel.LanguageCode, ResolveReportDesignerTheme()).ConfigureAwait(true);
                }
                return;
            }

            if (e.PropertyName == nameof(DemoShellViewModel.GridHierarchyRoots) ||
                e.PropertyName == nameof(DemoShellViewModel.GridHierarchyController) ||
                e.PropertyName == nameof(DemoShellViewModel.IsMasterDetailOutside))
            {
                if (_viewModel.ShowHierarchyTools)
                {
                    _ = Dispatcher.BeginInvoke(new Action(ApplyHierarchyScenarioToGrid), System.Windows.Threading.DispatcherPriority.Loaded);
                }

                return;
            }

            if (e.PropertyName != nameof(DemoShellViewModel.SelectedExample))
            {
                return;
            }

            if (_viewModel.IsYamlDocumentExample)
            {
                RebuildYamlDocumentRuntimeState();
            }
            else if (_viewModel.IsYamlGenerateFormExample || _viewModel.IsYamlActionsExample)
            {
                ResetYamlGenerateFormState();
            }

            BeginDiagnosticsSessionsForSelectedExample();
            QueueFirstRenderProbeForCurrentExample();
            LogGridStateLifecycle($"SelectedExample changed to '{_viewModel.SelectedExample?.Id ?? "<null>"}'. ShowEditingTools={_viewModel.ShowEditingTools}, ShowConstraintTools={_viewModel.ShowConstraintTools}, ShowSelectionTools={_viewModel.ShowSelectionTools}.");
            RefreshDemoGridRegionOptions();

            if (_viewModel.ShowWebHostSurface)
            {
                EnsureWebHostLoadTrace();
                _webHostLoadTrace.Mark("SelectedExample changed to web-host");
            }

            if (_webHostLoadTrace != null)
            {
                _webHostLoadTrace.Mark("SaveAndDetachGridStateRegistration begin");
            }
            SaveAndDetachGridStateRegistration();
            if (_webHostLoadTrace != null)
            {
                _webHostLoadTrace.Mark("SaveAndDetachGridStateRegistration completed");
            }

            _viewModel.MarkStateCleared();
            RefreshWebComponentSurface();

            if (_viewModel.ShowActiveLayerSelectorSurface)
            {
                return;
            }

            if (_viewModel.ShowWebComponentsSurface)
            {
                _webHostLoadTrace?.Mark("Skipping grid cleanup for web-components");
                return;
            }

            DemoGrid?.CancelEdits();
            DemoGrid?.SetCheckedRows(Array.Empty<string>());
            UpdateScenarioStatus(ScenarioStatusArea.Selection, string.Empty);
            UpdateScenarioStatus(ScenarioStatusArea.Editing, string.Empty);
            UpdateScenarioStatus(ScenarioStatusArea.Constraint, string.Empty);
            DemoGrid?.ClearHierarchySource();
            DemoGrid?.ClearGlobalSearch();
            _viewModel.GridSearchText = string.Empty;

            if (_viewModel.ShowHierarchyTools)
            {
                _ = Dispatcher.BeginInvoke(new Action(ApplyHierarchyScenarioToGrid), System.Windows.Threading.DispatcherPriority.Loaded);
            }

            if (_viewModel.ShowSelectionTools || _viewModel.ShowEditingTools || _viewModel.ShowConstraintTools)
            {
                LogGridStateLifecycle("Scheduling example activation with state restore.");
                _ = Dispatcher.BeginInvoke(
                    new Action(ActivateCurrentExampleStateAndScenario),
                    DispatcherPriority.DataBind);
            }

            if (_viewModel.ShowFilteringTools)
            {
                _ = Dispatcher.BeginInvoke(
                    new Action(() => DemoGrid?.FocusColumnFilter("Municipality")),
                    DispatcherPriority.Loaded);
            }

            if (_viewModel.ShowRemoteTools)
            {
                await _viewModel.LoadCurrentRemotePageAsync();
            }
            if (!_viewModel.ShowSelectionTools && !_viewModel.ShowEditingTools && !_viewModel.ShowConstraintTools)
            {
                QueueGridStateRegistrationRefresh();
            }
        }

        private void ActivateCurrentExampleStateAndScenario()
        {
            LogGridStateLifecycle("ActivateCurrentExampleStateAndScenario started.");
            var restoredFromStore = AttachGridStateRegistrationForCurrentExample();
            EnsureSummaryDesignerRegionVisible();

            if (_viewModel.ShowEditingTools)
            {
                if (restoredFromStore)
                {
                    LogGridStateLifecycle("Baseline editing scenario layout reset skipped because persisted state was restored. Reapplying demo row-state markers.");
                    ApplyEditingScenarioMarkersAfterRestore();
                }
                else
                {
                    LogGridStateLifecycle("Running baseline editing scenario because persisted state was not restored.");
                    ApplyEditingScenarioToGrid(GridEditingScenario.Baseline);
                }

                return;
            }

            if (_viewModel.ShowSelectionTools)
            {
                if (restoredFromStore)
                {
                    LogGridStateLifecycle("Baseline selection scenario skipped because persisted state was restored.");
                }
                else
                {
                    LogGridStateLifecycle("Running baseline selection scenario because persisted state was not restored.");
                    ApplySelectionScenarioToGrid(enableMultiSelect: false);
                }

                return;
            }

            if (_viewModel.ShowConstraintTools)
            {
                if (restoredFromStore)
                {
                    LogGridStateLifecycle("Baseline constraint scenario skipped because persisted state was restored.");
                }
                else
                {
                    LogGridStateLifecycle("Running baseline constraint scenario because persisted state was not restored.");
                    ApplyConstraintScenarioToGrid();
                }
            }
        }

        private void BeginDiagnosticsSessionsForSelectedExample()
        {
            var activationId = Interlocked.Increment(ref _gridActivationSequence);
            var currentGrid = GetCurrentStateGrid();
            var reason = $"SelectedExample='{_viewModel?.SelectedExample?.Id ?? "<null>"}', Activation={activationId}";

            if (DemoGrid != null)
            {
                PhialeGridDiagnostics.BeginGridSession(DescribeGrid(DemoGrid), reason, ReferenceEquals(currentGrid, DemoGrid));
            }

            if (ApplicationStateDemoGrid != null)
            {
                PhialeGridDiagnostics.BeginGridSession(DescribeGrid(ApplicationStateDemoGrid), reason, ReferenceEquals(currentGrid, ApplicationStateDemoGrid));
            }
        }

        private void QueueFirstRenderProbeForCurrentExample()
        {
            if (_pendingFirstRenderProbe != null)
            {
                CompositionTarget.Rendering -= _pendingFirstRenderProbe;
                _pendingFirstRenderProbe = null;
            }

            var exampleId = _viewModel?.SelectedExample?.Id ?? "<null>";
            var activation = _gridActivationSequence;
            _pendingFirstRenderProbe = (sender, args) =>
            {
                CompositionTarget.Rendering -= _pendingFirstRenderProbe;
                _pendingFirstRenderProbe = null;
                var activeGrid = GetCurrentStateGrid();
                LogGridStateLifecycle($"First render checkpoint reached for example '{exampleId}', activation={activation}, activeGrid={DescribeGrid(activeGrid)}.");
            };

            LogGridStateLifecycle($"First render probe armed for example '{exampleId}', activation={activation}.");
            CompositionTarget.Rendering += _pendingFirstRenderProbe;
        }

        private void LogGridStateLifecycle(string message)
        {
            PhialeGridDiagnostics.Write("Demo.MainWindow", $"{message} CurrentExample='{_viewModel?.SelectedExample?.Id ?? "<null>"}'. LogPath='{PhialeGridDiagnostics.GetLogFilePath()}'.");
        }

        private static string DescribeGrid(PhialeTech.PhialeGrid.Wpf.Controls.PhialeGrid grid)
        {
            if (grid == null)
            {
                return "<null>";
            }

            var name = string.IsNullOrWhiteSpace(grid.Name) ? "<unnamed>" : grid.Name;
            return name + "#" + grid.GetHashCode().ToString("X8");
        }

        private void RefreshWebComponentSurface()
        {
            if (_viewModel.ShowWebHostSurface)
            {
                EnsureWebHostLoadTrace();
                _webHostLoadTrace.Mark("RefreshWebHostSurface invoked");
            }

            EnsureWebComponentSurfacePrepared();

            if (WebHostSamplePresenter == null)
            {
                return;
            }

            if (!_viewModel.ShowWebComponentsSurface)
            {
                WebHostSamplePresenter.Content = null;
                AttachWebDemoFocusModeSource(null);
            }
        }

        private void QueueWebHostPrewarm()
        {
            if (_webHostPrewarmQueued)
            {
                return;
            }

            _webHostPrewarmQueued = true;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                WpfWebComponentHostFactory.WarmUpBrowserRuntime();
            }), DispatcherPriority.ApplicationIdle);
        }

        private void EnsureWebComponentSurfacePrepared()
        {
            if (WebHostSamplePresenter == null)
            {
                return;
            }

            if (_viewModel.ShowWebHostSurface)
            {
                if (_webHostShowcaseView == null)
                {
                    EnsureWebHostLoadTrace();
                    _webHostLoadTrace.Mark("Creating WebHostShowcaseView");
                    _webHostShowcaseView = new WebHostShowcaseView();
                }

                _webHostShowcaseView.AttachLoadTrace(_webHostLoadTrace);

                if (!ReferenceEquals(WebHostSamplePresenter.Content, _webHostShowcaseView))
                {
                    EnsureWebHostLoadTrace();
                    _webHostLoadTrace.Mark("Assigning WebHostShowcaseView to presenter");
                    WebHostSamplePresenter.Content = _webHostShowcaseView;
                    TraceNextRender("First render after WebHost presenter assignment");
                    Dispatcher.BeginInvoke(
                        new Action(() =>
                        {
                            if (_viewModel.ShowWebHostSurface)
                            {
                                EnsureWebHostLoadTrace();
                                _webHostLoadTrace.Mark("WebHost presenter rendered");
                            }
                        }),
                        DispatcherPriority.Render);
                }

                AttachWebDemoFocusModeSource(_webHostShowcaseView);

                return;
            }

            if (_viewModel.ShowPdfViewerSurface)
            {
                if (_pdfViewerShowcaseView == null)
                {
                    _pdfViewerShowcaseView = new PdfViewerShowcaseView();
                }

                if (!ReferenceEquals(WebHostSamplePresenter.Content, _pdfViewerShowcaseView))
                {
                    WebHostSamplePresenter.Content = _pdfViewerShowcaseView;
                }

                AttachWebDemoFocusModeSource(_pdfViewerShowcaseView);

                return;
            }

            if (_viewModel.ShowReportDesignerSurface)
            {
                if (_reportDesignerShowcaseView == null)
                {
                    _reportDesignerShowcaseView = new ReportDesignerShowcaseView(_viewModel.LanguageCode, ResolveReportDesignerTheme());
                }

                if (!ReferenceEquals(WebHostSamplePresenter.Content, _reportDesignerShowcaseView))
                {
                    WebHostSamplePresenter.Content = _reportDesignerShowcaseView;
                }

                _ = _reportDesignerShowcaseView.ApplyEnvironmentAsync(_viewModel.LanguageCode, ResolveReportDesignerTheme());
                AttachWebDemoFocusModeSource(_reportDesignerShowcaseView);
                return;
            }

            if (_viewModel.ShowMonacoEditorSurface)
            {
                if (_monacoEditorShowcaseView == null)
                {
                    _monacoEditorShowcaseView = new MonacoEditorShowcaseView(_viewModel.LanguageCode, ResolveReportDesignerTheme());
                }

                if (!ReferenceEquals(WebHostSamplePresenter.Content, _monacoEditorShowcaseView))
                {
                    WebHostSamplePresenter.Content = _monacoEditorShowcaseView;
                }

                _ = _monacoEditorShowcaseView.ApplyEnvironmentAsync(_viewModel.LanguageCode, ResolveReportDesignerTheme());
                AttachWebDemoFocusModeSource(_monacoEditorShowcaseView);
                return;
            }

            AttachWebDemoFocusModeSource(null);
        }

        private void StartWebHostLoadTrace(string source)
        {
            _webHostLoadTrace = new WebHostLoadTrace("WebHostLoad");
            _webHostLoadTrace.Mark("Load trace source: " + source);
            _webHostShowcaseView?.AttachLoadTrace(_webHostLoadTrace);
        }

        private void EnsureWebHostLoadTrace()
        {
            if (_webHostLoadTrace == null)
            {
                StartWebHostLoadTrace("auto");
            }
        }

        private void LogWebComponentsInputLag()
        {
            if (_webHostLoadTrace == null || _webComponentsClickTimestamp < 0)
            {
                return;
            }

            var delay = unchecked((uint)Environment.TickCount - (uint)_webComponentsClickTimestamp);
            _webHostLoadTrace.Mark("Input-to-click-handler delay: " + delay + " ms");
            _webComponentsClickTimestamp = -1;
        }

        private void TraceNextRender(string label)
        {
            if (_webHostLoadTrace == null)
            {
                return;
            }

            EventHandler handler = null;
            handler = (sender, args) =>
            {
                CompositionTarget.Rendering -= handler;
                _webHostLoadTrace?.Mark(label);
            };

            CompositionTarget.Rendering += handler;
        }

        private void AttachWebDemoFocusModeSource(IWebDemoFocusModeSource source)
        {
            if (ReferenceEquals(_activeWebDemoFocusModeSource, source))
            {
                ApplyWebDemoFocusChrome(source?.IsFocusMode == true);
                return;
            }

            if (_activeWebDemoFocusModeSource != null)
            {
                _activeWebDemoFocusModeSource.FocusModeChanged -= HandleWebDemoFocusModeChanged;
            }

            _activeWebDemoFocusModeSource = source;

            if (_activeWebDemoFocusModeSource != null)
            {
                _activeWebDemoFocusModeSource.FocusModeChanged += HandleWebDemoFocusModeChanged;
            }

            ApplyWebDemoFocusChrome(_activeWebDemoFocusModeSource?.IsFocusMode == true);
        }

        private void HandleWebDemoFocusModeChanged(object sender, WebDemoFocusModeChangedEventArgs e)
        {
            ApplyWebDemoFocusChrome(e.IsFocusMode);
        }

        private void ApplyWebDemoFocusChrome(bool isFocusMode)
        {
            _isWebDemoChromeCollapsed = isFocusMode;

            if (ExampleHeaderPanel != null)
            {
                ExampleHeaderPanel.Visibility = isFocusMode ? Visibility.Collapsed : Visibility.Visible;
            }

            UpdateFocusModeActionButtons();
            ApplyExampleTabHeaderVisibility();
        }

        private void HandleExampleTabSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateFocusModeActionButtons();
        }

        private async void HandleFocusModePrimaryActionClick(object sender, RoutedEventArgs e)
        {
            if (_activeWebDemoFocusModeSource == null || !_activeWebDemoFocusModeSource.ShowPrimaryFocusAction)
            {
                return;
            }

            await _activeWebDemoFocusModeSource.ExecutePrimaryFocusActionAsync().ConfigureAwait(true);
        }

        private void HandleFocusModeExitClick(object sender, RoutedEventArgs e)
        {
            _activeWebDemoFocusModeSource?.ExitFocusMode();
        }

        private void UpdateFocusModeActionButtons()
        {
            if (FocusModeActionsPanel == null)
            {
                return;
            }

            var showActions = _isWebDemoChromeCollapsed && _activeWebDemoFocusModeSource != null;
            FocusModeActionsPanel.Visibility = showActions ? Visibility.Visible : Visibility.Collapsed;

            if (FocusPrimaryActionButton != null)
            {
                FocusPrimaryActionButton.Visibility = showActions && _activeWebDemoFocusModeSource.ShowPrimaryFocusAction
                    ? Visibility.Visible
                    : Visibility.Collapsed;
                FocusPrimaryActionButton.Content = _activeWebDemoFocusModeSource?.PrimaryFocusActionText ?? "Action";
            }

            if (FocusExitButton != null)
            {
                FocusExitButton.Visibility = showActions ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void ApplyExampleTabHeaderVisibility()
        {
            if (ExampleTabControl == null)
            {
                return;
            }

            ExampleTabControl.ApplyTemplate();
            var headerPanel = FindDescendant<TabPanel>(ExampleTabControl);
            if (headerPanel != null)
            {
                headerPanel.Visibility = _isWebDemoChromeCollapsed ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        private static T FindDescendant<T>(DependencyObject root) where T : DependencyObject
        {
            if (root == null)
            {
                return null;
            }

            for (int index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
            {
                var child = VisualTreeHelper.GetChild(root, index);
                if (child is T match)
                {
                    return match;
                }

                var nested = FindDescendant<T>(child);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }

        private async Task<IReadOnlyList<WpfDemoBookChapterCapture>> CaptureDemoBookAsync()
        {
            var plan = DemoPdfBookPlanBuilder.Build(new DemoFeatureCatalog(), _viewModel.LanguageCode);
            var capturedChapters = new List<WpfDemoBookChapterCapture>();

            _activeWebDemoFocusModeSource?.ExitFocusMode();

            foreach (var chapter in plan.Chapters)
            {
                var capturedExamples = new List<WpfDemoBookExampleCapture>();
                foreach (var example in chapter.Examples)
                {
                    _viewModel.SelectExample(example.Id);
                    _viewModel.SelectedTabIndex = 0;
                    RefreshWebComponentSurface();
                    await WaitForDemoSurfaceAsync(_viewModel.ShowWebComponentsSurface).ConfigureAwait(true);

                    if (DemoTabExportGrid == null)
                    {
                        throw new InvalidOperationException("The demo export surface is not available.");
                    }

                    DemoTabExportGrid.UpdateLayout();
                    capturedExamples.Add(new WpfDemoBookExampleCapture(
                        _viewModel.SelectedExampleTitle,
                        _viewModel.SelectedExampleDescription,
                        CaptureVisibleBitmap(DemoTabExportGrid),
                        DemoTabExportGrid.ActualWidth,
                        DemoTabExportGrid.ActualHeight));
                }

                capturedChapters.Add(new WpfDemoBookChapterCapture(chapter.Title, chapter.Description, capturedExamples));
            }

            return capturedChapters;
        }

        private async Task RestoreDemoSelectionAsync(bool wasOverviewVisible, string previousExampleId, int previousTabIndex)
        {
            if (wasOverviewVisible)
            {
                _viewModel.ShowOverview();
            }
            else if (!string.IsNullOrWhiteSpace(previousExampleId))
            {
                _viewModel.SelectExample(previousExampleId);
                _viewModel.SelectedTabIndex = previousTabIndex;
            }

            RefreshWebComponentSurface();
            await WaitForDemoSurfaceAsync(_viewModel.ShowWebComponentsSurface).ConfigureAwait(true);
        }

        private async Task WaitForDemoSurfaceAsync(bool includeWebDelay)
        {
            UpdateLayout();
            DemoTabExportGrid?.UpdateLayout();
            await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Background);
            await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Render);
            await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            if (includeWebDelay)
            {
                await Task.Delay(220).ConfigureAwait(true);
                await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Render);
                await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            }

            UpdateLayout();
            DemoTabExportGrid?.UpdateLayout();
        }

        private static BitmapSource CaptureVisibleBitmap(FrameworkElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            element.UpdateLayout();
            var width = Math.Max(1d, element.ActualWidth);
            var height = Math.Max(1d, element.ActualHeight);
            const double dpi = 144d;
            var scale = dpi / 96d;
            var pixelWidth = Math.Max(1, (int)Math.Ceiling(width * scale));
            var pixelHeight = Math.Max(1, (int)Math.Ceiling(height * scale));

            var drawingVisual = new DrawingVisual();
            using (var drawingContext = drawingVisual.RenderOpen())
            {
                var visualBrush = new VisualBrush(element)
                {
                    Stretch = Stretch.None,
                    AlignmentX = AlignmentX.Left,
                    AlignmentY = AlignmentY.Top
                };

                drawingContext.DrawRectangle(visualBrush, null, new Rect(0d, 0d, width, height));
            }

            var bitmap = new RenderTargetBitmap(pixelWidth, pixelHeight, dpi, dpi, PixelFormats.Pbgra32);
            bitmap.Render(drawingVisual);
            bitmap.Freeze();
            return bitmap;
        }

        private static string BuildDemoBookPdfFileName()
        {
            return "phialetech-demo-book-" + DateTime.Now.ToString("yyyy-MM-dd-HHmm") + ".pdf";
        }

        private void SyncToolbarStateFromSnapshot(string encoded)
        {
            var snapshot = GridStateCodec.Decode(encoded, _viewModel.GridColumns);
            foreach (var column in snapshot.Layout.Columns)
            {
                _viewModel.SetColumnChooserVisibility(column.Id, column.IsVisible);
            }
        }

        private void SyncToolbarStateFromViewState(GridViewState state)
        {
            if (state?.Columns == null)
            {
                _viewModel.SetAllColumnChooserVisibility(true);
                return;
            }

            foreach (var column in state.Columns)
            {
                _viewModel.SetColumnChooserVisibility(column.ColumnId, column.IsVisible);
            }
        }

        private void ApplyHierarchyScenarioToGrid()
        {
            if (DemoGrid == null || !_viewModel.ShowHierarchyTools || _viewModel.GridHierarchyController == null || _viewModel.GridHierarchyRoots.Count == 0)
            {
                return;
            }

            if (_viewModel.IsMasterDetailExample)
            {
                DemoGrid.SetMasterDetailSource(
                    _viewModel.GridHierarchyRoots,
                    _viewModel.GridHierarchyController,
                    new[] { "ObjectName", "ObjectId", "GeometryType", "Status" },
                    masterDisplayColumnId: "Category",
                    detailDisplayColumnId: "ObjectName",
                    detailHeaderPlacementMode: _viewModel.IsMasterDetailOutside
                        ? GridMasterDetailHeaderPlacementMode.Outside
                        : GridMasterDetailHeaderPlacementMode.Inside);
                return;
            }

            DemoGrid.SetHierarchySource(_viewModel.GridHierarchyRoots, _viewModel.GridHierarchyController);
        }

        private void ApplySelectionScenarioToGrid(bool enableMultiSelect)
        {
            if (DemoGrid == null || !_viewModel.ShowSelectionTools)
            {
                return;
            }

            DemoGrid.CancelEdits();
            DemoGrid.SelectCurrentRow = true;
            DemoGrid.MultiSelect = enableMultiSelect;
            DemoGrid.SetCheckedRows(Array.Empty<string>());
            DemoGrid.ClearSelection();

            if (enableMultiSelect)
            {
                DemoGrid.SetCheckedRows(MultiSelectScenarioRowIds);
                DemoGrid.FocusRow(SelectionMultiCurrentRowId, "ObjectName");
                UpdateScenarioStatus(
                    ScenarioStatusArea.Selection,
                    "MultiSelect on: the far-left row-state column stays visible, checkboxes appear in a separate next column, and the current row remains highlighted among checked rows.");
                return;
            }

            DemoGrid.FocusRow(SelectionCurrentRowId, "ObjectName");
            UpdateScenarioStatus(
                ScenarioStatusArea.Selection,
                "MultiSelect off: only the dedicated row-state column is visible. The current row is highlighted across the grid and marked with the current-row triangle.");
        }

        private void ApplyEditingScenarioToGrid(GridEditingScenario scenario)
        {
            if (DemoGrid == null || !_viewModel.ShowEditingTools)
            {
                return;
            }

            LogGridStateLifecycle($"ApplyEditingScenarioToGrid started. Scenario={scenario}.");
            ResetEditingScenarioState();

            switch (scenario)
            {
                case GridEditingScenario.EditedWinsOverCurrent:
                    DemoGrid.SetRowValueForDemo(EditingEditedRowId, "ObjectName", "Parcel Stare Miasto 1001 (edited)");
                    DemoGrid.FocusRow(EditingEditedRowId, "ObjectName");
                    UpdateScenarioStatus(
                        ScenarioStatusArea.Editing,
                        "Current + Edited: the focused row is dirty, so the left state column shows the composite current-plus-edited marker.");
                    break;

                case GridEditingScenario.InvalidWinsOverEditedAndCurrent:
                    DemoGrid.SetRowValueForDemo(EditingInvalidRowId, "ObjectName", "Building Fabryczna 2 (edited)");
                    DemoGrid.SetRowValueForDemo(EditingInvalidRowId, "Owner", string.Empty);
                    DemoGrid.FocusRow(EditingInvalidRowId, "Owner");
                    UpdateScenarioStatus(
                        ScenarioStatusArea.Editing,
                        "Current + Error: this row is current and invalid, so the left state column shows the composite current-plus-error marker while the tooltip explains the failing field.");
                    break;

                default:
                    DemoGrid.SetRowValueForDemo(EditingEditedRowId, "ObjectName", "Parcel Stare Miasto 1001 (edited)");
                    DemoGrid.SetRowValueForDemo(EditingInvalidRowId, "Owner", string.Empty);
                    DemoGrid.FocusRow(EditingCurrentRowId, "ObjectName");
                    UpdateScenarioStatus(
                        ScenarioStatusArea.Editing,
                        "Baseline row-state demo: one normal row, one current row, one edited row with a pencil, and one invalid row with the red error icon are all visible at once. The vertical track rail also marks edited and invalid records.");
                    break;
            }
            LogGridStateLifecycle($"ApplyEditingScenarioToGrid finished. Scenario={scenario}.");
        }

        private void ApplyEditingScenarioMarkersAfterRestore()
        {
            if (DemoGrid == null || !_viewModel.ShowEditingTools)
            {
                return;
            }

            DemoGrid.CancelEdits();
            DemoGrid.SetRowValueForDemo(EditingEditedRowId, "ObjectName", "Parcel Stare Miasto 1001 (edited)");
            DemoGrid.SetRowValueForDemo(EditingInvalidRowId, "Owner", string.Empty);
            UpdateScenarioStatus(
                ScenarioStatusArea.Editing,
                "Baseline row-state demo markers were reapplied after persisted layout restore, so edited and invalid records stay visible without resetting the saved grid view.");
        }

        private void ResetEditingScenarioToCleanCurrentRow()
        {
            if (DemoGrid == null || !_viewModel.ShowEditingTools)
            {
                return;
            }

            ResetEditingScenarioState();
            DemoGrid.FocusRow(EditingCurrentRowId, "ObjectName");
            UpdateScenarioStatus(
                ScenarioStatusArea.Editing,
                "Row-state demo reset: the grid is back to a clean current row with no edited or invalid markers.");
        }

        private void ResetEditingScenarioState()
        {
            LogGridStateLifecycle("ResetEditingScenarioState started.");
            DemoGrid.CancelEdits();
            DemoGrid.SelectCurrentRow = true;
            DemoGrid.MultiSelect = false;
            DemoGrid.SetCheckedRows(Array.Empty<string>());
            DemoGrid.ClearSelection();
            LogGridStateLifecycle("ResetEditingScenarioState finished.");
        }

        private void ApplyConstraintScenarioToGrid()
        {
            if (DemoGrid == null || !_viewModel.ShowConstraintTools)
            {
                return;
            }

            DemoGrid.CancelEdits();
            DemoGrid.SelectCurrentRow = true;
            DemoGrid.MultiSelect = false;
            DemoGrid.SetCheckedRows(Array.Empty<string>());
            DemoGrid.ClearSelection();
            DemoGrid.FocusRow(EditingEditedRowId, "ObjectName");
            UpdateScenarioStatus(
                ScenarioStatusArea.Constraint,
                "Constraints demo: edit Object name, Object ID, Status, Last inspection, Owner, Budget, Completion %, Scale hint or the boolean flags. Validation reacts while typing, so try 'AB', 'bad-id', an empty Status, '2019-01-01', '12.345', '120.0' or '50'.");
        }

        private void UpdateScenarioStatus(ScenarioStatusArea area, string message)
        {
            var normalized = message ?? string.Empty;
            switch (area)
            {
                case ScenarioStatusArea.Selection:
                    _viewModel.SelectionScenarioStatusText = normalized;
                    break;
                case ScenarioStatusArea.Editing:
                    _viewModel.EditingScenarioStatusText = normalized;
                    break;
                case ScenarioStatusArea.Constraint:
                    _viewModel.ConstraintScenarioStatusText = normalized;
                    break;
            }
        }

        private void ApplySelectedTheme()
        {
            var useNight = ResolveNightMode(_viewModel.SelectedThemeCode);
            ApplyApplicationThemeDictionary(useNight);
            ApplyDemoThemeDictionary(useNight);
            Dispatcher.BeginInvoke(new Action(() =>
            {
                InvalidateVisual();
                UpdateLayout();
            }), DispatcherPriority.Render);
            if (DemoGrid != null)
            {
                DemoGrid.IsNightMode = useNight;
            }

            if (ApplicationStateDemoGrid != null)
            {
                ApplicationStateDemoGrid.IsNightMode = useNight;
            }

            if (DemoActiveLayerSelector != null)
            {
                DemoActiveLayerSelector.IsNightMode = useNight;
            }
        }

        private static bool ResolveNightMode(string selectedThemeCode)
        {
            if (string.Equals(selectedThemeCode, "night", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(selectedThemeCode, "day", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return IsSystemNightMode();
        }

        private string ResolveReportDesignerTheme()
        {
            return ResolveNightMode(_viewModel.SelectedThemeCode) ? "dark" : "light";
        }

        private static bool IsSystemNightMode()
        {
            try
            {
                using (var personalize = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    if (personalize == null)
                    {
                        return false;
                    }

                    var value = personalize.GetValue("AppsUseLightTheme");
                    if (value is int intValue)
                    {
                        return intValue == 0;
                    }

                    if (value is long longValue)
                    {
                        return longValue == 0;
                    }
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        private static void OnYamlDocumentPresentationPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var window = d as MainWindow;
            if (window == null)
            {
                return;
            }

            if (window._viewModel != null && window._viewModel.IsYamlDocumentSurfaceExample)
            {
                window.RebuildYamlDocumentRuntimeState();
            }
        }

        private void ResetYamlGenerateFormState()
        {
            YamlGeneratedFormSourceText = BuildDefaultYamlEditorSample();
            IsYamlGeneratedFormVisible = false;
            YamlDocumentRuntimeState = null;
            ClearYamlGeneratedFormDiagnostics();
            YamlDocumentLastResultText = BuildDefaultYamlEditorResultHint();
            _ = EnsureYamlGeneratedFormMonacoReadyAsync();
        }

        private void RebuildYamlDocumentRuntimeState()
        {
            try
            {
                var resolvedDocument = BuildYamlDocumentDefinition();
                var runtimeState = new RuntimeDocumentStateFactory().Create(resolvedDocument);

                if (_viewModel == null || !_viewModel.IsYamlActionsExample)
                {
                    var customerName = runtimeState.GetField("customerName");
                    customerName?.SetValidation("required", "Customer is required.");

                    var ticketCode = runtimeState.GetField("ticketCode");
                    ticketCode?.LoadValue("OPS-2401");

                    var contactEmail = runtimeState.GetField("contactEmail");
                    contactEmail?.LoadValue("roland@example.com");

                    var ownerName = runtimeState.GetField("ownerName");
                    ownerName?.LoadValue("Field Team Alpha");

                    var summary = runtimeState.GetField("summary");
                    if (summary != null)
                    {
                        summary.LoadValue("Initial triage note.");
                        summary.SetValue("Bridge inspection package for the north district.");
                    }

                    var resolutionNotes = runtimeState.GetField("resolutionNotes");
                    resolutionNotes?.LoadValue("Coordinate maintenance and safety teams before publishing the task.");
                }

                YamlDocumentRuntimeState = runtimeState;
                YamlDocumentLastResultText = "Try OK or Cancel to see the document action result here.";
            }
            catch (Exception ex)
            {
                YamlDocumentRuntimeState = null;
                YamlDocumentLastResultText = "Document demo initialization failed." + Environment.NewLine + ex.Message;
            }
        }

        private void HandleGenerateYamlFormClick(object sender, RoutedEventArgs e)
        {
            ClearYamlGeneratedFormDiagnostics();

            var compiler = new YamlComposedDocumentCompiler();
            var imported = compiler.Compile(
                YamlGeneratedFormSourceText,
                new[] { typeof(YamlLibraryMarker).Assembly },
                _viewModel?.LanguageCode);
            if (!imported.Success)
            {
                YamlDocumentRuntimeState = null;
                IsYamlGeneratedFormVisible = false;
                SetYamlGeneratedFormDiagnostics(BuildYamlDiagnosticsText("Nie udalo sie skompilowac YAML.", imported.Diagnostics));
                return;
            }

            var normalized = new YamlDocumentDefinitionNormalizer().Normalize(imported.Definition);
            if (!normalized.Success)
            {
                YamlDocumentRuntimeState = null;
                IsYamlGeneratedFormVisible = false;
                SetYamlGeneratedFormDiagnostics(BuildYamlDiagnosticsText("Nie udalo sie znormalizowac definicji formularza.", normalized.Diagnostics));
                return;
            }

            try
            {
                var resolvedForm = normalized.ResolvedDocument as ResolvedFormDocumentDefinition;
                if (resolvedForm == null)
                {
                    throw new InvalidOperationException("Only form documents can be rendered in the generated form demo.");
                }

                YamlDocumentRuntimeState = new RuntimeDocumentStateFactory().Create(resolvedForm);
                IsYamlGeneratedFormVisible = true;
                ClearYamlGeneratedFormDiagnostics();
                YamlDocumentLastResultText = "Formatka zostala wygenerowana. Kliknij OK albo Anuluj, aby zobaczyc wynik.";
            }
            catch (Exception ex)
            {
                YamlDocumentRuntimeState = null;
                IsYamlGeneratedFormVisible = false;
                SetYamlGeneratedFormDiagnostics("Nie udalo sie wygenerowac runtime formularza." + Environment.NewLine + ex.Message);
            }
        }

        private void HandleYamlDocumentActionInvoked(object sender, YamlDocumentActionInvokedEventArgs e)
        {
            if (e?.DocumentState == null || e.ActionState?.Action == null)
            {
                return;
            }

            var result = BuildDocumentActionResult(e.DocumentState, e.ActionState);
            YamlDocumentLastResultText = FormatDocumentActionResult(result, e.ActionState.Id);

            if (IsYamlGeneratedFormVisible)
            {
                IsYamlGeneratedFormVisible = false;
                YamlDocumentRuntimeState = null;
            }
        }

        private void EnsureYamlGeneratedFormMonacoEditor()
        {
            if (_yamlGeneratedFormMonacoEditor != null)
            {
                AttachYamlGeneratedFormEditorToActivePresenter();
                return;
            }

            _yamlGeneratedFormMonacoEditor = new PhialeMonacoEditor(
                new WpfWebComponentHostFactory(),
                new MonacoEditorOptions
                {
                    InitialTheme = ResolveReportDesignerTheme(),
                    InitialLanguage = "yaml",
                    InitialValue = YamlGeneratedFormSourceText ?? string.Empty,
                })
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            _yamlGeneratedFormMonacoEditor.ContentChanged += HandleYamlGeneratedFormMonacoContentChanged;
            AttachYamlGeneratedFormEditorToActivePresenter();
        }

        private async Task EnsureYamlGeneratedFormMonacoReadyAsync()
        {
            EnsureYamlGeneratedFormMonacoEditor();
            if (_yamlGeneratedFormMonacoEditor == null)
            {
                return;
            }

            await _yamlGeneratedFormMonacoEditor.InitializeAsync().ConfigureAwait(true);
            await _yamlGeneratedFormMonacoEditor.SetThemeAsync(ResolveReportDesignerTheme()).ConfigureAwait(true);
            await _yamlGeneratedFormMonacoEditor.SetLanguageAsync("yaml").ConfigureAwait(true);

            _isUpdatingYamlGeneratedFormEditor = true;
            try
            {
                await _yamlGeneratedFormMonacoEditor.SetValueAsync(YamlGeneratedFormSourceText ?? string.Empty).ConfigureAwait(true);
            }
            finally
            {
                _isUpdatingYamlGeneratedFormEditor = false;
            }

            AttachYamlGeneratedFormEditorToActivePresenter();
        }

        private void HandleYamlGeneratedFormMonacoContentChanged(object sender, MonacoEditorContentChangedEventArgs e)
        {
            if (_isUpdatingYamlGeneratedFormEditor)
            {
                return;
            }

            YamlGeneratedFormSourceText = e?.Value ?? string.Empty;
        }

        private ResolvedFormDocumentDefinition BuildYamlDocumentDefinition()
        {
            var interactionMode = SelectedInputInteractionMode;
            var densityMode = SelectedInputDensityMode;
            const FieldChromeMode chromeMode = FieldChromeMode.Framed;
            const CaptionPlacement captionPlacement = CaptionPlacement.Top;

            var customerNameDefinition = new YamlStringFieldDefinition
            {
                Id = "customerName",
                Name = "Customer",
                CaptionKey = "Customer",
                PlaceholderKey = "Type customer name",
                WidthHint = FieldWidthHint.Fill,
                IsRequired = true,
                ShowLabel = true,
                ShowPlaceholder = true,
                Visible = true,
                Enabled = true,
                ShowOldValueRestoreButton = false,
            };

            var ticketCodeDefinition = new YamlStringFieldDefinition
            {
                Id = "ticketCode",
                Name = "Ticket code",
                CaptionKey = "Ticket code",
                PlaceholderKey = "OPS-2401",
                WidthHint = FieldWidthHint.Short,
                ShowLabel = true,
                ShowPlaceholder = true,
                Visible = true,
                Enabled = true,
            };

            var contactEmailDefinition = new YamlStringFieldDefinition
            {
                Id = "contactEmail",
                Name = "Contact email",
                CaptionKey = "Contact email",
                PlaceholderKey = "name@example.com",
                WidthHint = FieldWidthHint.Long,
                ShowLabel = true,
                ShowPlaceholder = true,
                Visible = true,
                Enabled = true,
            };

            var ownerNameDefinition = new YamlStringFieldDefinition
            {
                Id = "ownerName",
                Name = "Owner",
                CaptionKey = "Owner",
                PlaceholderKey = "Field team",
                WidthHint = FieldWidthHint.Medium,
                ShowLabel = true,
                ShowPlaceholder = true,
                Visible = true,
                Enabled = true,
            };

            var summaryDefinition = new YamlStringFieldDefinition
            {
                Id = "summary",
                Name = "Operational summary",
                CaptionKey = "Operational summary",
                PlaceholderKey = "Describe the current request",
                WidthHint = FieldWidthHint.Fill,
                ShowLabel = true,
                ShowPlaceholder = true,
                Visible = true,
                Enabled = true,
                ShowOldValueRestoreButton = true,
            };

            var resolutionNotesDefinition = new YamlStringFieldDefinition
            {
                Id = "resolutionNotes",
                Name = "Resolution notes",
                CaptionKey = "Resolution notes",
                PlaceholderKey = "Add internal delivery notes",
                WidthHint = FieldWidthHint.Fill,
                ShowLabel = true,
                ShowPlaceholder = true,
                Visible = true,
                Enabled = true,
            };

            var fields = new List<ResolvedFieldDefinition>
            {
                BuildResolvedField(customerNameDefinition, FieldWidthHint.Fill, interactionMode, densityMode, chromeMode, captionPlacement),
                BuildResolvedField(ticketCodeDefinition, FieldWidthHint.Short, interactionMode, densityMode, chromeMode, captionPlacement),
                BuildResolvedField(contactEmailDefinition, FieldWidthHint.Long, interactionMode, densityMode, chromeMode, captionPlacement),
                BuildResolvedField(ownerNameDefinition, FieldWidthHint.Medium, interactionMode, densityMode, chromeMode, captionPlacement),
                BuildResolvedField(summaryDefinition, FieldWidthHint.Fill, interactionMode, densityMode, chromeMode, captionPlacement, showOldValueRestoreButton: true),
                BuildResolvedField(resolutionNotesDefinition, FieldWidthHint.Fill, interactionMode, densityMode, chromeMode, captionPlacement),
            };

            var fieldMap = fields.ToDictionary(field => field.Id, StringComparer.OrdinalIgnoreCase);

            var layout = new ResolvedLayoutDefinition(
                id: "root",
                name: "Root",
                width: null,
                widthHint: FieldWidthHint.Fill,
                visible: true,
                enabled: true,
                showOldValueRestoreButton: false,
                validationTrigger: ValidationTrigger.OnChange,
                interactionMode: interactionMode,
                densityMode: densityMode,
                fieldChromeMode: chromeMode,
                captionPlacement: captionPlacement,
                items: new ResolvedLayoutItemDefinition[]
                {
                    new ResolvedContainerDefinition(
                        id: "primaryIdentity",
                        name: "Primary identity",
                        width: null,
                        widthHint: FieldWidthHint.Fill,
                        visible: true,
                        enabled: true,
                        showOldValueRestoreButton: false,
                        validationTrigger: ValidationTrigger.OnChange,
                        interactionMode: interactionMode,
                        densityMode: densityMode,
                        fieldChromeMode: chromeMode,
                        captionPlacement: captionPlacement,
                        captionKey: "Primary identity",
                        showBorder: true,
                        items: new ResolvedLayoutItemDefinition[]
                        {
                            new ResolvedRowDefinition(
                                id: "identityRow",
                                name: "Identity row",
                                width: null,
                                widthHint: FieldWidthHint.Fill,
                                visible: true,
                                enabled: true,
                                showOldValueRestoreButton: false,
                                validationTrigger: ValidationTrigger.OnChange,
                                interactionMode: interactionMode,
                                densityMode: densityMode,
                                fieldChromeMode: chromeMode,
                                captionPlacement: captionPlacement,
                                items: new ResolvedLayoutItemDefinition[]
                                {
                                    new ResolvedFieldReferenceDefinition("customerNameRef", "Customer ref", null, FieldWidthHint.Fill, true, true, false, ValidationTrigger.OnChange, interactionMode, densityMode, chromeMode, captionPlacement, fieldMap["customerName"]),
                                    new ResolvedFieldReferenceDefinition("ticketCodeRef", "Ticket code ref", null, FieldWidthHint.Short, true, true, false, ValidationTrigger.OnChange, interactionMode, densityMode, chromeMode, captionPlacement, fieldMap["ticketCode"]),
                                })
                        }),
                    new ResolvedContainerDefinition(
                        id: "communication",
                        name: "Communication",
                        width: null,
                        widthHint: FieldWidthHint.Fill,
                        visible: true,
                        enabled: true,
                        showOldValueRestoreButton: false,
                        validationTrigger: ValidationTrigger.OnChange,
                        interactionMode: interactionMode,
                        densityMode: densityMode,
                        fieldChromeMode: chromeMode,
                        captionPlacement: captionPlacement,
                        captionKey: "Communication",
                        showBorder: true,
                        items: new ResolvedLayoutItemDefinition[]
                        {
                            new ResolvedRowDefinition(
                                id: "communicationRow",
                                name: "Communication row",
                                width: null,
                                widthHint: FieldWidthHint.Fill,
                                visible: true,
                                enabled: true,
                                showOldValueRestoreButton: false,
                                validationTrigger: ValidationTrigger.OnChange,
                                interactionMode: interactionMode,
                                densityMode: densityMode,
                                fieldChromeMode: chromeMode,
                                captionPlacement: captionPlacement,
                                items: new ResolvedLayoutItemDefinition[]
                                {
                                    new ResolvedFieldReferenceDefinition("contactEmailRef", "Contact email ref", null, FieldWidthHint.Long, true, true, false, ValidationTrigger.OnChange, interactionMode, densityMode, chromeMode, captionPlacement, fieldMap["contactEmail"]),
                                    new ResolvedFieldReferenceDefinition("ownerNameRef", "Owner ref", null, FieldWidthHint.Medium, true, true, false, ValidationTrigger.OnChange, interactionMode, densityMode, chromeMode, captionPlacement, fieldMap["ownerName"]),
                                })
                        }),
                    new ResolvedContainerDefinition(
                        id: "summaryContainer",
                        name: "Operational summary",
                        width: null,
                        widthHint: FieldWidthHint.Fill,
                        visible: true,
                        enabled: true,
                        showOldValueRestoreButton: false,
                        validationTrigger: ValidationTrigger.OnChange,
                        interactionMode: interactionMode,
                        densityMode: densityMode,
                        fieldChromeMode: chromeMode,
                        captionPlacement: captionPlacement,
                        captionKey: "Operational summary",
                        showBorder: true,
                        items: new ResolvedLayoutItemDefinition[]
                        {
                            new ResolvedFieldReferenceDefinition("summaryRef", "Summary ref", null, FieldWidthHint.Fill, true, true, false, ValidationTrigger.OnChange, interactionMode, densityMode, chromeMode, captionPlacement, fieldMap["summary"]),
                            new ResolvedFieldReferenceDefinition("resolutionNotesRef", "Resolution notes ref", null, FieldWidthHint.Fill, true, true, false, ValidationTrigger.OnChange, interactionMode, densityMode, chromeMode, captionPlacement, fieldMap["resolutionNotes"]),
                        }),
                });

            var actions = new List<ResolvedDocumentActionDefinition>
            {
                new ResolvedDocumentActionDefinition(
                    new YamlDocumentActionDefinition
                    {
                        Id = "save",
                        Name = "Save document",
                        CaptionKey = "Save document",
                        Enabled = true,
                        Visible = true,
                        Semantic = ActionSemantic.Ok,
                    },
                    visible: true,
                    enabled: true),
                new ResolvedDocumentActionDefinition(
                    new YamlDocumentActionDefinition
                    {
                        Id = "cancel",
                        Name = "Cancel",
                        CaptionKey = "Cancel",
                        Enabled = true,
                        Visible = true,
                        Semantic = ActionSemantic.Cancel,
                    },
                    visible: true,
                    enabled: true),
            };

            return new ResolvedFormDocumentDefinition(
                id: "ops-intake",
                name: "Operational intake document",
                kind: DocumentKind.Form,
                width: null,
                widthHint: FieldWidthHint.Fill,
                visible: true,
                enabled: true,
                showOldValueRestoreButton: false,
                validationTrigger: ValidationTrigger.OnChange,
                interactionMode: interactionMode,
                densityMode: densityMode,
                fieldChromeMode: chromeMode,
                captionPlacement: captionPlacement,
                layout: layout,
                actionAreas: Array.Empty<ResolvedActionAreaDefinition>(),
                fields: fields,
                actions: actions,
                fieldMap: fieldMap);
        }

        private static ResolvedFieldDefinition BuildResolvedField(
            YamlStringFieldDefinition definition,
            FieldWidthHint widthHint,
            InteractionMode interactionMode,
            DensityMode densityMode,
            FieldChromeMode chromeMode,
            CaptionPlacement captionPlacement,
            bool showOldValueRestoreButton = false)
        {
            return new ResolvedFieldDefinition(
                definition,
                width: null,
                widthHint: widthHint,
                visible: true,
                enabled: true,
                showOldValueRestoreButton: showOldValueRestoreButton,
                validationTrigger: ValidationTrigger.OnChange,
                interactionMode: interactionMode,
                densityMode: densityMode,
                fieldChromeMode: chromeMode,
                captionPlacement: captionPlacement);
        }

        private void SetYamlGeneratedFormDiagnostics(string text)
        {
            YamlGeneratedFormDiagnosticsText = text ?? string.Empty;
            HasYamlGeneratedFormDiagnostics = !string.IsNullOrWhiteSpace(YamlGeneratedFormDiagnosticsText);
        }

        private void ClearYamlGeneratedFormDiagnostics()
        {
            YamlGeneratedFormDiagnosticsText = string.Empty;
            HasYamlGeneratedFormDiagnostics = false;
        }

        private static string BuildYamlDiagnosticsText(string header, IReadOnlyList<string> diagnostics)
        {
            var lines = new List<string> { header };
            if (diagnostics != null && diagnostics.Count > 0)
            {
                lines.AddRange(diagnostics);
            }

            return string.Join(Environment.NewLine, lines);
        }

        private static string BuildDefaultYamlGeneratedFormSample()
        {
            return string.Join(Environment.NewLine, new[]
            {
                "namespace: application.forms",
                "imports:",
                "  - domain.person",
                "",
                "documents:",
                "  yaml-generated-form:",
                "    kind: Form",
                "    name: YAML generated form",
                "    interactionMode: Classic",
                "    densityMode: Normal",
                "    fieldChromeMode: Framed",
                "    actionAreas:",
                "      - id: footerPrimary",
                "        placement: Bottom",
                "        horizontalAlignment: Right",
                "        shared: true",
                "    fields:",
                "      - id: firstName",
                "        extends: firstName",
                "      - id: lastName",
                "        extends: lastName",
                "      - id: age",
                "        extends: age",
                "      - id: notes",
                "        extends: notes",
                "    layout:",
                "      type: Column",
                "      items:",
                "        - type: Row",
                "          items:",
                "            - fieldRef: firstName",
                "            - fieldRef: lastName",
                "            - fieldRef: age",
                "        - fieldRef: notes",
                "    actions:",
                "      - id: ok",
                "        semantic: Ok",
                "        captionKey: actions.ok.caption",
                "        area: footerPrimary",
                "      - id: cancel",
                "        semantic: Cancel",
                "        captionKey: actions.cancel.caption",
                "        area: footerPrimary",
            });
        }

        private string BuildDefaultYamlEditorSample()
        {
            if (_viewModel != null && _viewModel.IsYamlActionsExample)
            {
                return BuildYamlActionsDemoSample();
            }

            return BuildDefaultYamlGeneratedFormSample();
        }

        private string BuildDefaultYamlEditorResultHint()
        {
            return _viewModel != null && _viewModel.IsYamlActionsExample
                ? "Wklej YAML definiujacy formatke z akcjami, kliknij Generate form, a wynik po kliknieciu akcji pojawi sie tutaj."
                : "Wklej YAML, kliknij Generate form, a wynik po OK albo Anuluj pojawi sie tutaj.";
        }

        private static string BuildYamlActionsDemoSample()
        {
            return string.Join(Environment.NewLine, new[]
            {
                "namespace: demo.actions",
                "imports:",
                "  - medium",
                "",
                "document:",
                "  id: action-review",
                "  kind: Form",
                "  name: YAML actions demo",
                "  interactionMode: Classic",
                "  densityMode: Normal",
                "  fieldChromeMode: Framed",
                "  actionAreas:",
                "    - id: headerActions",
                "      placement: Top",
                "      horizontalAlignment: Stretch",
                "      shared: true",
                "      sticky: true",
                "    - id: leftTools",
                "      placement: Left",
                "      horizontalAlignment: Stretch",
                "      shared: false",
                "    - id: footerPrimary",
                "      placement: Bottom",
                "      horizontalAlignment: Right",
                "      shared: true",
                "      sticky: true",
                "    - id: rightHelp",
                "      placement: Right",
                "      horizontalAlignment: Stretch",
                "      shared: false",
                "  fields:",
                "    - id: reviewTitle",
                "      extends: limited50Text",
                "      caption: Review title",
                "      placeholder: Action rendering driven by YAML",
                "      widthHint: Medium",
                "    - id: reviewNotes",
                "      extends: notesText",
                "      caption: Notes",
                "      placeholder: Compare area grouping, ordering and primary actions",
                "      widthHint: Fill",
                "  layout:",
                "    type: Column",
                "    items:",
                "      - type: Container",
                "        caption: Action rendering examples",
                "        showBorder: true",
                "        items:",
                "          - fieldRef: reviewTitle",
                "          - fieldRef: reviewNotes",
                "  actions:",
                "    - id: help",
                "      semantic: Help",
                "      caption: Help",
                "      area: headerActions",
                "      slot: Start",
                "      order: 10",
                "    - id: docs",
                "      semantic: Secondary",
                "      caption: Documentation",
                "      area: rightHelp",
                "      slot: Start",
                "      order: 10",
                "    - id: history",
                "      semantic: Secondary",
                "      caption: History",
                "      area: leftTools",
                "      slot: Start",
                "      order: 10",
                "    - id: validate",
                "      semantic: Apply",
                "      caption: Validate",
                "      area: headerActions",
                "      slot: End",
                "      order: 20",
                "    - id: cancel",
                "      semantic: Cancel",
                "      caption: Cancel",
                "      area: footerPrimary",
                "      slot: End",
                "      order: 20",
                "    - id: save",
                "      semantic: Ok",
                "      caption: Save document",
                "      area: footerPrimary",
                "      isPrimary: true",
                "      slot: End",
                "      order: 10",
            });
        }

        private ResolvedFormDocumentDefinition BuildYamlDocumentDefinitionFromYaml(string yaml)
        {
            var importer = new YamlDocumentDefinitionImporter();
            var imported = importer.Import(yaml);
            if (!imported.Success)
            {
                throw new InvalidOperationException(string.Join(Environment.NewLine, imported.Diagnostics));
            }

            var normalized = new YamlDocumentDefinitionNormalizer().Normalize(imported.Definition);
            if (!normalized.Success)
            {
                throw new InvalidOperationException(string.Join(Environment.NewLine, normalized.Diagnostics));
            }

            var resolvedForm = normalized.ResolvedDocument as ResolvedFormDocumentDefinition;
            if (resolvedForm == null)
            {
                throw new InvalidOperationException("Only form documents can be rendered in the YAML actions demo.");
            }

            return resolvedForm;
        }

        private static DynamicDocumentDialogResult BuildDocumentActionResult(RuntimeDocumentState documentState, RuntimeActionState actionState)
        {
            var mapper = new RuntimeDocumentJsonMapper();
            var json = mapper.ToJson(documentState);
            var actionKind = actionState.Action == null ? DocumentActionKind.Custom : actionState.Action.ActionKind;

            return actionKind == DocumentActionKind.Cancel
                ? DynamicDocumentDialogResult.Cancelled(documentState.Id, json)
                : DynamicDocumentDialogResult.Confirmed(documentState.Id, json);
        }

        private static string FormatDocumentActionResult(DynamicDocumentDialogResult result, string actionId)
        {
            var lines = new List<string>
            {
                "Action: " + (string.IsNullOrWhiteSpace(actionId) ? "unknown" : actionId),
                "Result: " + (result != null && result.IsCancelled ? "Cancelled" : "Confirmed"),
            };

            if (!string.IsNullOrWhiteSpace(result?.Json))
            {
                lines.Add(result.Json);
            }

            return string.Join(Environment.NewLine, lines);
        }

        private void AttachYamlGeneratedFormEditorToActivePresenter()
        {
            if (_yamlGeneratedFormMonacoEditor == null)
            {
                return;
            }

            if (YamlGeneratedFormEditorPresenter != null)
            {
                YamlGeneratedFormEditorPresenter.Content = null;
            }

            if (YamlActionsEditorPresenter != null)
            {
                YamlActionsEditorPresenter.Content = null;
            }

            if (_viewModel != null && _viewModel.IsYamlActionsExample)
            {
                if (YamlActionsEditorPresenter != null)
                {
                    YamlActionsEditorPresenter.Content = _yamlGeneratedFormMonacoEditor;
                }

                return;
            }

            if (YamlGeneratedFormEditorPresenter != null)
            {
                YamlGeneratedFormEditorPresenter.Content = _yamlGeneratedFormMonacoEditor;
            }
        }

        private static void ApplyApplicationThemeDictionary(bool isNight)
        {
            var resources = Application.Current?.Resources;
            if (resources == null)
            {
                return;
            }

            var targetUri = isNight ? DemoThemeNightUri : DemoThemeDayUri;
            var existingTheme = resources.MergedDictionaries.FirstOrDefault(dictionary =>
                dictionary.Source != null &&
                dictionary.Source.ToString().IndexOf("/Themes/Demo.Theme.", StringComparison.OrdinalIgnoreCase) >= 0);

            if (existingTheme != null)
            {
                if (existingTheme.Source != null &&
                    string.Equals(existingTheme.Source.ToString(), targetUri.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                var index = resources.MergedDictionaries.IndexOf(existingTheme);
                resources.MergedDictionaries.RemoveAt(index);
                resources.MergedDictionaries.Insert(index, new ResourceDictionary { Source = targetUri });
                return;
            }

            resources.MergedDictionaries.Insert(0, new ResourceDictionary { Source = targetUri });
        }

        private void ApplyDemoThemeDictionary(bool isNight)
        {
            var targetUri = isNight ? DemoThemeNightUri : DemoThemeDayUri;
            var existingTheme = Resources.MergedDictionaries.FirstOrDefault(dictionary =>
                dictionary.Source != null &&
                dictionary.Source.ToString().IndexOf("/Themes/Demo.Theme.", StringComparison.OrdinalIgnoreCase) >= 0);

            if (existingTheme != null)
            {
                if (existingTheme.Source != null &&
                    string.Equals(existingTheme.Source.ToString(), targetUri.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                var index = Resources.MergedDictionaries.IndexOf(existingTheme);
                Resources.MergedDictionaries.RemoveAt(index);
                Resources.MergedDictionaries.Insert(index, new ResourceDictionary { Source = targetUri });
                return;
            }

            Resources.MergedDictionaries.Insert(0, new ResourceDictionary { Source = targetUri });
        }

        private enum GridEditingScenario
        {
            Baseline,
            EditedWinsOverCurrent,
            InvalidWinsOverEditedAndCurrent,
        }
    }
}

