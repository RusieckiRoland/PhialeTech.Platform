using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
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
using PhialeTech.ComponentHost.Abstractions.Presentation;
using PhialeTech.ComponentHost.Presentation;
using PhialeTech.ComponentHost.State;
using PhialeTech.Components.Shared.Model;
using PhialeTech.Components.Shared.Services;
using PhialeTech.Yaml.Library;
using PhialeTech.Components.Shared.ViewModels;
using PhialeTech.Components.Wpf.Hosting;
using PhialeTech.MonacoEditor.Abstractions;
using PhialeTech.MonacoEditor.Wpf.Controls;
using PhialeTech.PhialeGrid.Wpf.Controls;
using PhialeTech.PhialeGrid.Wpf.Diagnostics;
using PhialeTech.PhialeGrid.Wpf.State;
using PhialeTech.ComponentHost.Wpf.Hosting;
using PhialeTech.ComponentHost.Wpf.Services;
using PhialeTech.Shell.Abstractions.Presentation;
using PhialeTech.Shell.Wpf.Controls;
using PhialeTech.Shell.Wpf.Input;
using PhialeTech.WebHost.Wpf;
using PhialeTech.YamlApp.Abstractions.Enums;
using PhialeTech.YamlApp.Abstractions.Results;
using PhialeTech.YamlApp.Core.Normalization;
using PhialeTech.YamlApp.Core.Resolved;
using PhialeTech.YamlApp.Definitions.Documents;
using PhialeTech.YamlApp.Definitions.Fields;
using PhialeTech.YamlApp.Infrastructure.Loading;
using PhialeTech.YamlApp.Runtime.Model;
using PhialeTech.YamlApp.Wpf.Controls.Buttons;
using PhialeTech.YamlApp.Runtime.Services;
using PhialeTech.YamlApp.Wpf.Document;

namespace PhialeTech.Components.Wpf
{
    public partial class MainWindow : PhialeWindow
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

        public static readonly DependencyProperty YamlPrimitiveLastCommandTextProperty =
            DependencyProperty.Register(
                nameof(YamlPrimitiveLastCommandText),
                typeof(string),
                typeof(MainWindow),
                new PropertyMetadata("No command invoked yet."));

        public static readonly DependencyProperty HostedModalLastResultTextProperty =
            DependencyProperty.Register(
                nameof(HostedModalLastResultText),
                typeof(string),
                typeof(MainWindow),
                new PropertyMetadata("No hosted modal opened yet."));

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
        private readonly HostedSurfaceManager _hostedSurfaceManager;
        private ApplicationStateRegistration _gridStateRegistration;
        private PhialeGridViewStateComponent _gridStateComponent;
        private string _registeredGridStateKey = string.Empty;
        private int _gridActivationSequence;
        private EventHandler _pendingFirstRenderProbe;
        private WebHostShowcaseView _webHostShowcaseView;
        private PdfViewerShowcaseView _pdfViewerShowcaseView;
        private ReportDesignerShowcaseView _reportDesignerShowcaseView;
        private MonacoEditorShowcaseView _monacoEditorShowcaseView;
        private PhialeMonacoEditor _yamlActionsMonacoEditor;
        private PhialeMonacoEditor _yamlDocumentMonacoEditor;
        private IWebDemoFocusModeSource _activeWebDemoFocusModeSource;
        private bool _isWebDemoChromeCollapsed;
        private bool _webHostPrewarmQueued;
        private WebHostLoadTrace _webHostLoadTrace;
        private int _webComponentsClickTimestamp = -1;
        private bool _isUpdatingYamlGeneratedFormEditor;
        private readonly IReadOnlyList<YamlLibraryFormTemplateOption> _yamlLibraryFormTemplates;
        private readonly IReadOnlyList<YamlDocumentInheritanceOption> _yamlDocumentInheritanceOptions;
        private bool _isApplyingYamlActionTemplateSelection;
        private bool _isApplyingYamlDocumentInheritanceSelection;
        private readonly HashSet<PhialeMonacoEditor> _diagnosticMonacoEditors = new HashSet<PhialeMonacoEditor>();

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

        public string YamlPrimitiveLastCommandText
        {
            get => (string)GetValue(YamlPrimitiveLastCommandTextProperty);
            set => SetValue(YamlPrimitiveLastCommandTextProperty, value);
        }

        public string HostedModalLastResultText
        {
            get => (string)GetValue(HostedModalLastResultTextProperty);
            set => SetValue(HostedModalLastResultTextProperty, value);
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

        public WpfHostedSurfaceService HostedSurfaceService { get; }

        public MainWindow()
            : this(DemoApplicationServices.CreateIsolatedForWindow(), true)
        {
        }

        public MainWindow(DemoApplicationServices applicationServices, bool ownsApplicationServices = false)
        {
            InitializeComponent();
            _yamlLibraryFormTemplates = LoadYamlLibraryFormTemplates();
            _yamlDocumentInheritanceOptions = BuildYamlDocumentInheritanceOptions();
            _applicationServices = applicationServices ?? throw new ArgumentNullException(nameof(applicationServices));
            _ownsApplicationServices = ownsApplicationServices;
            PhialeWebHostDiagnostics.Sink = (source, message) => MonacoInputTrace.Write("webhost", source, message);
            MonacoInputTrace.Write("app", "MainWindow", "constructed log=" + MonacoInputTrace.CurrentLogFilePath);
            AddHandler(UIElement.PreviewMouseDownEvent, new MouseButtonEventHandler(DebugObserveWindowPreviewMouseDown), true);
            AddHandler(UIElement.PreviewKeyDownEvent, new KeyEventHandler(HandleWindowPreviewKeyDownDiagnostic), true);
            AddHandler(UIElement.PreviewKeyUpEvent, new KeyEventHandler(HandleWindowPreviewKeyUpDiagnostic), true);
            AddHandler(UIElement.PreviewTextInputEvent, new TextCompositionEventHandler(HandleWindowPreviewTextInputDiagnostic), true);
            AddHandler(Keyboard.GotKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(HandleWindowGotKeyboardFocusDiagnostic), true);
            AddHandler(Keyboard.LostKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(HandleWindowLostKeyboardFocusDiagnostic), true);
            SourceInitialized += HandleSourceInitialized;
            _viewModel = new DemoShellViewModel(
                "Wpf",
                remoteGridClient: CreateRemoteGridClient(),
                definitionManager: _applicationServices.DefinitionManager);
            _hostedSurfaceManager = new HostedSurfaceManager(new DemoHostedShellCoordinator(_viewModel));
            var hostedSurfaceRegistry = new WpfHostedSurfaceFactoryRegistry();
            hostedSurfaceRegistry.Register(new DemoViewHostedSurfaceFactory());
            hostedSurfaceRegistry.Register(new DemoYamlHostedSurfaceFactory());
            HostedSurfaceService = new WpfHostedSurfaceService(_hostedSurfaceManager, hostedSurfaceRegistry);
            HostedSurfaceService.SessionChanged += HandleHostedSurfaceSessionChanged;
            AddHandler(PhialeTitleBar.CommandInvokedEvent, new EventHandler<ShellCommandInvokedRoutedEventArgs>(HandleShellCommandInvoked));
            _viewModel.PropertyChanged += HandleViewModelPropertyChanged;
            DataContext = _viewModel;
            var gridLanguageDirectory = Path.Combine(AppContext.BaseDirectory, "PhialeGrid.Localization", "Languages");
            DemoGrid.LanguageDirectory = gridLanguageDirectory;
            ApplicationStateDemoGrid.LanguageDirectory = gridLanguageDirectory;
            DemoActiveLayerSelector.LanguageDirectory = Path.Combine(AppContext.BaseDirectory, "PhialeTech.ActiveLayerSelector", "Languages");
            ApplySelectedTheme();
            UpdateApplicationShellState();
            InitializeYamlActionTemplatePicker();
            InitializeYamlDocumentInheritancePicker();
            Loaded += HandleWindowLoaded;
            Closed += HandleWindowClosed;
            if (ExampleTabControl != null)
            {
                ExampleTabControl.SelectionChanged += HandleExampleTabSelectionChanged;
            }

            ResetYamlGenerateFormState();
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
            DisposeYamlGeneratedFormMonacoEditor(ref _yamlActionsMonacoEditor);
            DisposeYamlGeneratedFormMonacoEditor(ref _yamlDocumentMonacoEditor);
            if (ExampleTabControl != null)
            {
                ExampleTabControl.SelectionChanged -= HandleExampleTabSelectionChanged;
            }
            if (_ownsApplicationServices)
            {
                _applicationServices.Dispose();
            }

            if (HostedSurfaceService != null)
            {
                HostedSurfaceService.SessionChanged -= HandleHostedSurfaceSessionChanged;
            }

            HostedSurfaceService?.Dispose();
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
                    _viewModel.AppTitle,
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
            if (e.PropertyName == nameof(DemoShellViewModel.SelectedThemeCode) ||
                e.PropertyName == nameof(DemoShellViewModel.LanguageCode) ||
                e.PropertyName == nameof(DemoShellViewModel.SelectedExample) ||
                e.PropertyName == nameof(DemoShellViewModel.WorkspaceOverviewTitle) ||
                e.PropertyName == nameof(DemoShellViewModel.WorkspaceOverviewSubtitle))
            {
                UpdateApplicationShellState();
            }

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
                if (_yamlActionsMonacoEditor != null)
                {
                    await _yamlActionsMonacoEditor.SetThemeAsync(ResolveReportDesignerTheme()).ConfigureAwait(true);
                }
                if (_yamlDocumentMonacoEditor != null)
                {
                    await _yamlDocumentMonacoEditor.SetThemeAsync(ResolveReportDesignerTheme()).ConfigureAwait(true);
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

            if (_viewModel.IsYamlDocumentExample || _viewModel.IsYamlActionsExample)
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
                    MonacoInputTrace.Write("monaco.showcase", "MainWindow", "showcase view created");
                }

                if (!ReferenceEquals(WebHostSamplePresenter.Content, _monacoEditorShowcaseView))
                {
                    WebHostSamplePresenter.Content = _monacoEditorShowcaseView;
                }

                _ = _monacoEditorShowcaseView.ApplyEnvironmentAsync(_viewModel.LanguageCode, ResolveReportDesignerTheme());
                AttachWebDemoFocusModeSource(_monacoEditorShowcaseView);
                return;
            }

            if (_viewModel.IsWebComponentScrollHostExample)
            {
                WebHostSamplePresenter.Content = null;
                AttachWebDemoFocusModeSource(null);
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

        private void UpdateApplicationShellState()
        {
            if (_viewModel == null)
            {
                return;
            }

            var titleBarCommands = new List<ApplicationShellCommandItem>();
            if (_viewModel.IsDetailVisible && !IsHostedSurfaceSessionOpen())
            {
                titleBarCommands.Add(new ApplicationShellCommandItem("shell.overview", _viewModel.BackToOverviewText, ApplicationShellCommandPlacement.Leading));
            }

            ShellState = new ApplicationShellState(
                _viewModel.AppTitle,
                _viewModel.AppSubtitle,
                _viewModel.IsOverviewVisible ? _viewModel.WorkspaceOverviewTitle : _viewModel.SelectedExampleTitle,
                _viewModel.IsOverviewVisible ? _viewModel.WorkspaceOverviewSubtitle : _viewModel.SelectedExampleDescription,
                navigationItems: Array.Empty<ApplicationShellNavigationItem>(),
                titleBarCommands: titleBarCommands,
                statusItems: BuildApplicationShellStatusItems());
        }

        private bool IsHostedSurfaceSessionOpen()
        {
            return HostedSurfaceService != null && HostedSurfaceService.CurrentSession != null;
        }

        private IReadOnlyList<ApplicationShellStatusItem> BuildApplicationShellStatusItems()
        {
            return new[]
            {
                new ApplicationShellStatusItem("shell.platform", "Platform", "Wpf"),
                new ApplicationShellStatusItem("shell.language", _viewModel.LanguageLabelText, (_viewModel.LanguageCode ?? string.Empty).ToUpperInvariant()),
                new ApplicationShellStatusItem("shell.theme", _viewModel.ThemeLabelText, ResolveThemeDisplayName()),
            };
        }

        private string ResolveThemeDisplayName()
        {
            if (_viewModel == null || _viewModel.ThemeOptions == null)
            {
                return string.Empty;
            }

            var selectedTheme = _viewModel.ThemeOptions.FirstOrDefault(option =>
                option != null &&
                string.Equals(option.Code, _viewModel.SelectedThemeCode, StringComparison.OrdinalIgnoreCase));

            return selectedTheme == null
                ? _viewModel.SelectedThemeCode ?? string.Empty
                : selectedTheme.DisplayName ?? string.Empty;
        }

        private void HandleShellCommandInvoked(object sender, ShellCommandInvokedRoutedEventArgs e)
        {
            if (e == null || e.Command == null)
            {
                return;
            }

            if (string.Equals(e.Command.CommandId, "shell.overview", StringComparison.OrdinalIgnoreCase))
            {
                if (_viewModel != null && _viewModel.ShowOverviewCommand != null && _viewModel.ShowOverviewCommand.CanExecute(null))
                {
                    _viewModel.ShowOverviewCommand.Execute(null);
                    e.Handled = true;
                }

                return;
            }

            throw new InvalidOperationException("Unsupported shell command: " + e.Command.CommandId);
        }

        private void HandleHostedSurfaceSessionChanged(object sender, EventArgs e)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.InvokeAsync(UpdateApplicationShellState);
                return;
            }

            UpdateApplicationShellState();
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

            // YAML document demos are generated on demand from Monaco content.
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

        private void HandleYamlPrimitiveButtonInvoked(object sender, YamlButtonInvokedEventArgs e)
        {
            if (e?.Command == null)
            {
                return;
            }

            YamlPrimitiveLastCommandText = string.Format(
                "Last command: {0}{1}Source: UniversalInput -> Core controller -> YamlButton routed event",
                e.CommandId,
                Environment.NewLine);
        }

        private async void HandleHostedModalButtonInvoked(object sender, YamlButtonInvokedEventArgs e)
        {
            if (e?.Command == null || HostedSurfaceService == null)
            {
                return;
            }

            try
            {
                var result = await HostedSurfaceService.ShowAsync(BuildHostedSurfaceRequest(e.CommandId)).ConfigureAwait(true);
                HostedModalLastResultText = FormatHostedModalResult(result);
            }
            catch (Exception ex)
            {
                HostedModalLastResultText = "Hosted modal failed: " + ex.Message;
            }
        }

        private async void HandleShowGeneratedYamlAsDialogClick(object sender, RoutedEventArgs e)
        {
            await ShowGeneratedYamlAsDialogAsync(HostedEntranceStyle.Directional).ConfigureAwait(true);
        }

        private async void HandleShowGeneratedYamlAsSoftDialogClick(object sender, RoutedEventArgs e)
        {
            await ShowGeneratedYamlAsDialogAsync(HostedEntranceStyle.Materialize).ConfigureAwait(true);
        }

        private async Task ShowGeneratedYamlAsDialogAsync(HostedEntranceStyle entranceStyle)
        {
            if (HostedSurfaceService == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(YamlGeneratedFormSourceText))
            {
                HostedModalLastResultText = "Generated form YAML is empty.";
                return;
            }

            try
            {
                var request = new HostedSurfaceRequest
                {
                    SurfaceKind = HostedSurfaceKind.YamlDocument,
                    ContentKey = "demo.yaml.generated-preview",
                    PresentationMode = HostedPresentationMode.CompactModal,
                    EntranceStyle = entranceStyle,
                    Size = HostedPresentationSize.Large,
                    Placement = HostedSheetPlacement.Center,
                    Title = entranceStyle == HostedEntranceStyle.Materialize ? "Generated Yaml form (soft dialog)" : "Generated Yaml form",
                    CanDismiss = true,
                    Payload = YamlGeneratedFormSourceText,
                };

                var result = await HostedSurfaceService.ShowAsync(request).ConfigureAwait(true);
                HostedModalLastResultText = FormatHostedModalResult(result);
                YamlDocumentLastResultText = FormatHostedModalResult(result);
            }
            catch (Exception ex)
            {
                HostedModalLastResultText = "Hosted modal failed: " + ex.Message;
                SetYamlGeneratedFormDiagnostics("Nie udalo sie pokazac formularza jako dialog." + Environment.NewLine + ex.Message);
            }
        }

        private PhialeMonacoEditor EnsureYamlGeneratedFormMonacoEditor()
        {
            if (_viewModel != null && _viewModel.IsYamlActionsExample)
            {
                if (_yamlActionsMonacoEditor == null)
                {
                    _yamlActionsMonacoEditor = CreateYamlGeneratedFormMonacoEditor("yaml.actions");
                }

                AttachYamlGeneratedFormEditorToActivePresenter(_yamlActionsMonacoEditor);
                return _yamlActionsMonacoEditor;
            }

            if (_yamlDocumentMonacoEditor == null)
            {
                _yamlDocumentMonacoEditor = CreateYamlGeneratedFormMonacoEditor("yaml.document");
            }

            AttachYamlGeneratedFormEditorToActivePresenter(_yamlDocumentMonacoEditor);
            return _yamlDocumentMonacoEditor;
        }

        private PhialeMonacoEditor CreateYamlGeneratedFormMonacoEditor(string traceName)
        {
            var editor = new PhialeMonacoEditor(
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
            editor.ContentChanged += HandleYamlGeneratedFormMonacoContentChanged;
            AttachMonacoEditorDiagnostics(editor, traceName);
            MonacoInputTrace.Write(traceName, "MainWindow", "editor created");
            return editor;
        }

        private void InitializeYamlActionTemplatePicker()
        {
            if (YamlActionTemplateComboBox == null)
            {
                return;
            }

            YamlActionTemplateComboBox.ItemsSource = _yamlLibraryFormTemplates;
            if (_yamlLibraryFormTemplates.Count == 0)
            {
                return;
            }

            SetSelectedYamlActionTemplate(GetSelectedOrDefaultYamlActionTemplate());
        }

        private void InitializeYamlDocumentInheritancePicker()
        {
            if (YamlDocumentInheritanceComboBox == null)
            {
                return;
            }

            YamlDocumentInheritanceComboBox.ItemsSource = _yamlDocumentInheritanceOptions;
            if (_yamlDocumentInheritanceOptions.Count == 0)
            {
                return;
            }

            SetSelectedYamlDocumentInheritanceOption(GetSelectedOrDefaultYamlDocumentInheritanceOption());
        }

        private async Task EnsureYamlGeneratedFormMonacoReadyAsync()
        {
            var editor = EnsureYamlGeneratedFormMonacoEditor();
            if (editor == null)
            {
                return;
            }

            MonacoInputTrace.Write(GetActiveMonacoScenario(), "MainWindow", "EnsureYamlGeneratedFormMonacoReadyAsync start");
            await editor.InitializeAsync().ConfigureAwait(true);
            MonacoInputTrace.Write(GetActiveMonacoScenario(), "MainWindow", "InitializeAsync completed");
            await editor.SetThemeAsync(ResolveReportDesignerTheme()).ConfigureAwait(true);
            MonacoInputTrace.Write(GetActiveMonacoScenario(), "MainWindow", "SetThemeAsync completed theme=" + ResolveReportDesignerTheme());
            await editor.SetLanguageAsync("yaml").ConfigureAwait(true);
            MonacoInputTrace.Write(GetActiveMonacoScenario(), "MainWindow", "SetLanguageAsync completed yaml");

            _isUpdatingYamlGeneratedFormEditor = true;
            try
            {
                await editor.SetValueAsync(YamlGeneratedFormSourceText ?? string.Empty).ConfigureAwait(true);
                MonacoInputTrace.Write(GetActiveMonacoScenario(), "MainWindow", "SetValueAsync completed length=" + (YamlGeneratedFormSourceText ?? string.Empty).Length);
            }
            finally
            {
                _isUpdatingYamlGeneratedFormEditor = false;
            }

            AttachYamlGeneratedFormEditorToActivePresenter(editor);
            MonacoInputTrace.Write(GetActiveMonacoScenario(), "MainWindow", "AttachYamlGeneratedFormEditorToActivePresenter completed");
        }

        private void HandleYamlGeneratedFormMonacoContentChanged(object sender, MonacoEditorContentChangedEventArgs e)
        {
            if (_isUpdatingYamlGeneratedFormEditor)
            {
                return;
            }

            MonacoInputTrace.Write(GetActiveMonacoScenario(), "MainWindow", "ContentChanged length=" + (e?.Value ?? string.Empty).Length + " snippet=" + MonacoInputTrace.SafeSnippet(e?.Value));
            YamlGeneratedFormSourceText = e?.Value ?? string.Empty;
        }

        private async void HandleYamlActionTemplateSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isApplyingYamlActionTemplateSelection)
            {
                return;
            }

            var template = YamlActionTemplateComboBox == null
                ? null
                : YamlActionTemplateComboBox.SelectedItem as YamlLibraryFormTemplateOption;
            if (template == null)
            {
                return;
            }

            await ApplyYamlActionTemplateAsync(template).ConfigureAwait(true);
        }

        private async void HandleYamlDocumentInheritanceSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isApplyingYamlDocumentInheritanceSelection)
            {
                return;
            }

            var option = YamlDocumentInheritanceComboBox == null
                ? null
                : YamlDocumentInheritanceComboBox.SelectedItem as YamlDocumentInheritanceOption;
            if (option == null)
            {
                return;
            }

            await ApplyYamlDocumentInheritanceOptionAsync(option).ConfigureAwait(true);
        }

        private void SetYamlGeneratedFormDiagnostics(string text)
        {
            var diagnosticText = string.IsNullOrWhiteSpace(text)
                ? "Input log file: " + MonacoInputTrace.CurrentLogFilePath
                : text + Environment.NewLine + "Input log file: " + MonacoInputTrace.CurrentLogFilePath;
            YamlGeneratedFormDiagnosticsText = diagnosticText;
            HasYamlGeneratedFormDiagnostics = !string.IsNullOrWhiteSpace(YamlGeneratedFormDiagnosticsText);
        }

        private void ClearYamlGeneratedFormDiagnostics()
        {
            SetYamlGeneratedFormDiagnostics(string.Empty);
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
                return GetSelectedOrDefaultYamlActionTemplate()?.YamlText ?? BuildYamlActionsDemoSample();
            }

            if (_viewModel != null && _viewModel.IsYamlDocumentExample)
            {
                return BuildYamlDocumentInheritanceSample(GetSelectedOrDefaultYamlDocumentInheritanceOption());
            }

            return BuildDefaultYamlGeneratedFormSample();
        }

        private string BuildDefaultYamlEditorResultHint()
        {
            if (_viewModel != null && _viewModel.IsYamlActionsExample)
            {
                return "Wybierz shell formularza z biblioteki albo wklej wlasny YAML, kliknij Generate form, a wynik po kliknieciu akcji pojawi sie tutaj.";
            }

            if (_viewModel != null && _viewModel.IsYamlDocumentExample)
            {
                return "Ten przyklad pokazuje formularz dziedziczacy gotowy shell akcji. Zmien pola albo layout, kliknij Generate form i sprawdz wynik.";
            }

            return "Wklej YAML, kliknij Generate form, a wynik po OK albo Anuluj pojawi sie tutaj.";
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
                "      chromeMode: Blended",
                "      shared: true",
                "      sticky: true",
                "    - id: leftTools",
                "      placement: Left",
                "      horizontalAlignment: Stretch",
                "      shared: false",
                "    - id: footerPrimary",
                "      placement: Bottom",
                "      horizontalAlignment: Right",
                "      chromeMode: Explicit",
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
                "      iconKey: help",
                "      area: headerActions",
                "      slot: Start",
                "      order: 10",
                "    - id: docs",
                "      semantic: Secondary",
                "      caption: Documentation",
                "      iconKey: document",
                "      area: rightHelp",
                "      slot: Start",
                "      order: 10",
                "    - id: history",
                "      semantic: Secondary",
                "      caption: History",
                "      iconKey: history",
                "      area: leftTools",
                "      slot: Start",
                "      order: 10",
                "    - id: validate",
                "      semantic: Apply",
                "      caption: Validate",
                "      iconKey: validate",
                "      area: headerActions",
                "      slot: End",
                "      order: 20",
                "    - id: cancel",
                "      semantic: Cancel",
                "      caption: Cancel",
                "      iconKey: cancel",
                "      area: footerPrimary",
                "      slot: End",
                "      order: 20",
                "    - id: save",
                "      semantic: Ok",
                "      caption: Save document",
                "      iconKey: save",
                "      area: footerPrimary",
                "      isPrimary: true",
                "      slot: End",
                "      order: 10",
            });
        }

        private static string BuildYamlDocumentInheritanceSample(YamlDocumentInheritanceOption option)
        {
            var shell = option ?? BuildYamlDocumentInheritanceOptions().First();
            var documentId = string.IsNullOrWhiteSpace(shell.SampleDocumentId) ? "review-request" : shell.SampleDocumentId;
            var extendsId = string.IsNullOrWhiteSpace(shell.BaseDocumentId) ? "review-sticky-header-footer" : shell.BaseDocumentId;
            var documentName = string.IsNullOrWhiteSpace(shell.SampleDisplayName) ? "Review request document" : shell.SampleDisplayName;

            return string.Join(Environment.NewLine, new[]
            {
                "namespace: application.forms",
                "imports:",
                "  - domain.person",
                "  - application.forms.actionShells",
                "",
                "document:",
                "  id: " + documentId,
                "  kind: Form",
                "  extends: " + extendsId,
                "  name: " + documentName,
                "  topRegionChrome: Merged",
                "  bottomRegionChrome: Merged",
                "  header:",
                "    title: Review request",
                "    subtitle: Customer verification",
                "    description: Validate personal details and notes before completing the review workflow.",
                "    status: Draft",
                "    context: Internal form",
                "  footer:",
                "    note: Fields marked with * are required.",
                "    status: Draft saved locally",
                "    source: Demo YAML runtime",
                "  fields:",
                "    - id: firstName",
                "      extends: firstName",
                "    - id: lastName",
                "      extends: lastName",
                "    - id: notes",
                "      extends: notes",
                "  layout:",
                "    type: Column",
                "    items:",
                "      - type: Container",
                "        caption: Reviewer",
                "        showBorder: true",
                "        variant: Compact",
                "        items:",
                "          - type: Row",
                "            items:",
                "              - fieldRef: firstName",
                "              - fieldRef: lastName",
                "      - type: Container",
                "        caption: Review notes",
                "        showBorder: true",
                "        items:",
                "          - fieldRef: notes",
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

        private static HostedSurfaceRequest BuildHostedSurfaceRequest(string commandId)
        {
            switch (commandId)
            {
                case "demo.modal.view.compact":
                    return new HostedSurfaceRequest
                    {
                        SurfaceKind = HostedSurfaceKind.View,
                        ContentKey = "demo.view.hosted-modal",
                        PresentationMode = HostedPresentationMode.CompactModal,
                        Size = HostedPresentationSize.Medium,
                        Placement = HostedSheetPlacement.Center,
                        Title = "Hosted WPF view",
                        CanDismiss = true,
                    };
                case "demo.modal.view.sheet":
                    return new HostedSurfaceRequest
                    {
                        SurfaceKind = HostedSurfaceKind.View,
                        ContentKey = "demo.view.hosted-modal",
                        PresentationMode = HostedPresentationMode.OverlaySheet,
                        Size = HostedPresentationSize.Large,
                        Placement = HostedSheetPlacement.Right,
                        Title = "Workflow overlay view",
                        CanDismiss = true,
                    };
                case "demo.modal.yaml.compact":
                    return new HostedSurfaceRequest
                    {
                        SurfaceKind = HostedSurfaceKind.YamlDocument,
                        ContentKey = "demo.yaml.hosted-modal",
                        PresentationMode = HostedPresentationMode.CompactModal,
                        Size = HostedPresentationSize.Medium,
                        Placement = HostedSheetPlacement.Center,
                        Title = "YamlApp compact modal",
                        CanDismiss = true,
                    };
                case "demo.modal.yaml.sheet":
                    return new HostedSurfaceRequest
                    {
                        SurfaceKind = HostedSurfaceKind.YamlDocument,
                        ContentKey = "demo.yaml.hosted-modal",
                        PresentationMode = HostedPresentationMode.OverlaySheet,
                        Size = HostedPresentationSize.Large,
                        Placement = HostedSheetPlacement.Right,
                        Title = "YamlApp overlay sheet",
                        CanDismiss = true,
                    };
                default:
                    throw new InvalidOperationException("Unknown hosted modal command: " + commandId);
            }
        }

        private static string FormatHostedModalResult(IHostedSurfaceResult result)
        {
            if (result == null)
            {
                return "Hosted modal closed without a result.";
            }

            return string.Format(
                "Hosted modal result: {0}{1}Command: {2}{1}Session: {3}",
                result.Outcome,
                Environment.NewLine,
                string.IsNullOrWhiteSpace(result.CommandId) ? "n/a" : result.CommandId,
                string.IsNullOrWhiteSpace(result.SessionId) ? "n/a" : result.SessionId);
        }

        private void AttachYamlGeneratedFormEditorToActivePresenter(PhialeMonacoEditor editor)
        {
            if (editor == null)
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
                    YamlActionsEditorPresenter.Content = editor;
                    MonacoInputTrace.Write("yaml.actions", "MainWindow", "editor attached to YamlActionsEditorPresenter");
                }

                return;
            }

            if (YamlGeneratedFormEditorPresenter != null)
            {
                YamlGeneratedFormEditorPresenter.Content = editor;
                MonacoInputTrace.Write("yaml.document", "MainWindow", "editor attached to YamlGeneratedFormEditorPresenter");
            }
        }

        private void AttachMonacoEditorDiagnostics(PhialeMonacoEditor editor, string traceName)
        {
            if (editor == null || !_diagnosticMonacoEditors.Add(editor))
            {
                return;
            }

            editor.ReadyStateChanged += (_, args) => MonacoInputTrace.Write(traceName, "PhialeMonacoEditor", "ReadyStateChanged initialized=" + args.IsInitialized + " ready=" + args.IsReady);
            editor.ErrorOccurred += (_, args) => MonacoInputTrace.Write(traceName, "PhialeMonacoEditor", "ErrorOccurred message=" + args.Message + " detail=" + args.Detail);
            editor.Loaded += (_, __) => MonacoInputTrace.Write(traceName, "PhialeMonacoEditor", "Loaded");
            editor.Unloaded += (_, __) => MonacoInputTrace.Write(traceName, "PhialeMonacoEditor", "Unloaded");
            editor.PreviewMouseDown += (_, args) => MonacoInputTrace.Write(traceName, "PhialeMonacoEditor", "PreviewMouseDown button=" + args.ChangedButton);
            editor.PreviewKeyDown += (_, args) => MonacoInputTrace.Write(traceName, "PhialeMonacoEditor", "PreviewKeyDown key=" + args.Key + " handled=" + args.Handled);
            editor.PreviewKeyUp += (_, args) => MonacoInputTrace.Write(traceName, "PhialeMonacoEditor", "PreviewKeyUp key=" + args.Key + " handled=" + args.Handled);
            editor.PreviewTextInput += (_, args) => MonacoInputTrace.Write(traceName, "PhialeMonacoEditor", "PreviewTextInput text=" + MonacoInputTrace.SafeSnippet(args.Text) + " handled=" + args.Handled);
            editor.GotKeyboardFocus += (_, args) => MonacoInputTrace.Write(traceName, "PhialeMonacoEditor", "GotKeyboardFocus original=" + DescribeElement(args.OriginalSource as DependencyObject));
            editor.LostKeyboardFocus += (_, args) => MonacoInputTrace.Write(traceName, "PhialeMonacoEditor", "LostKeyboardFocus original=" + DescribeElement(args.OriginalSource as DependencyObject));
            editor.IsKeyboardFocusWithinChanged += (_, __) => MonacoInputTrace.Write(traceName, "PhialeMonacoEditor", "IsKeyboardFocusWithin=" + editor.IsKeyboardFocusWithin);

            var hostField = typeof(PhialeMonacoEditor).GetField("_host", BindingFlags.Instance | BindingFlags.NonPublic);
            var hostElement = hostField?.GetValue(editor) as UIElement;
            if (hostElement == null)
            {
                MonacoInputTrace.Write(traceName, "PhialeMonacoEditor", "host reflection failed");
                return;
            }

            MonacoInputTrace.Write(traceName, "PhialeMonacoEditor", "host type=" + hostElement.GetType().FullName);
            hostElement.PreviewMouseDown += (_, args) => MonacoInputTrace.Write(traceName, "HostElement", "PreviewMouseDown button=" + args.ChangedButton);
            hostElement.PreviewKeyDown += (_, args) => MonacoInputTrace.Write(traceName, "HostElement", "PreviewKeyDown key=" + args.Key + " handled=" + args.Handled);
            hostElement.PreviewKeyUp += (_, args) => MonacoInputTrace.Write(traceName, "HostElement", "PreviewKeyUp key=" + args.Key + " handled=" + args.Handled);
            hostElement.PreviewTextInput += (_, args) => MonacoInputTrace.Write(traceName, "HostElement", "PreviewTextInput text=" + MonacoInputTrace.SafeSnippet(args.Text) + " handled=" + args.Handled);
            hostElement.GotKeyboardFocus += (_, args) => MonacoInputTrace.Write(traceName, "HostElement", "GotKeyboardFocus original=" + DescribeElement(args.OriginalSource as DependencyObject));
            hostElement.LostKeyboardFocus += (_, args) => MonacoInputTrace.Write(traceName, "HostElement", "LostKeyboardFocus original=" + DescribeElement(args.OriginalSource as DependencyObject));
            hostElement.IsKeyboardFocusWithinChanged += (_, __) => MonacoInputTrace.Write(traceName, "HostElement", "IsKeyboardFocusWithin=" + hostElement.IsKeyboardFocusWithin);
        }

        private void HandleWindowPreviewKeyDownDiagnostic(object sender, KeyEventArgs e)
        {
            if (!ShouldLogMonacoInput())
            {
                return;
            }

            MonacoInputTrace.Write(GetActiveMonacoScenario(), "MainWindow", "PreviewKeyDown key=" + e.Key + " handled=" + e.Handled + " original=" + DescribeElement(e.OriginalSource as DependencyObject));
        }

        private void HandleWindowPreviewKeyUpDiagnostic(object sender, KeyEventArgs e)
        {
            if (!ShouldLogMonacoInput())
            {
                return;
            }

            MonacoInputTrace.Write(GetActiveMonacoScenario(), "MainWindow", "PreviewKeyUp key=" + e.Key + " handled=" + e.Handled + " original=" + DescribeElement(e.OriginalSource as DependencyObject));
        }

        private void HandleWindowPreviewTextInputDiagnostic(object sender, TextCompositionEventArgs e)
        {
            if (!ShouldLogMonacoInput())
            {
                return;
            }

            MonacoInputTrace.Write(GetActiveMonacoScenario(), "MainWindow", "PreviewTextInput text=" + MonacoInputTrace.SafeSnippet(e.Text) + " handled=" + e.Handled + " original=" + DescribeElement(e.OriginalSource as DependencyObject));
        }

        private void HandleWindowGotKeyboardFocusDiagnostic(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (!ShouldLogMonacoInput())
            {
                return;
            }

            MonacoInputTrace.Write(GetActiveMonacoScenario(), "MainWindow", "GotKeyboardFocus original=" + DescribeElement(e.OriginalSource as DependencyObject) + " new=" + DescribeElement(e.NewFocus as DependencyObject));
        }

        private void HandleWindowLostKeyboardFocusDiagnostic(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (!ShouldLogMonacoInput())
            {
                return;
            }

            MonacoInputTrace.Write(GetActiveMonacoScenario(), "MainWindow", "LostKeyboardFocus original=" + DescribeElement(e.OriginalSource as DependencyObject) + " old=" + DescribeElement(e.OldFocus as DependencyObject));
        }

        private bool ShouldLogMonacoInput()
        {
            return (_viewModel != null && (_viewModel.ShowMonacoEditorSurface || _viewModel.IsYamlDocumentExample || _viewModel.IsYamlActionsExample));
        }

        private string GetActiveMonacoScenario()
        {
            if (_viewModel == null)
            {
                return "main.unknown";
            }

            if (_viewModel.ShowMonacoEditorSurface)
            {
                return "monaco.showcase";
            }

            if (_viewModel.IsYamlDocumentExample)
            {
                return "yaml.document";
            }

            if (_viewModel.IsYamlActionsExample)
            {
                return "yaml.actions";
            }

            return "main.other";
        }

        private static string DescribeElement(DependencyObject element)
        {
            if (element == null)
            {
                return "null";
            }

            if (element is FrameworkElement frameworkElement)
            {
                return frameworkElement.GetType().Name + "#" + (frameworkElement.Name ?? string.Empty);
            }

            return element.GetType().Name;
        }


        private YamlLibraryFormTemplateOption GetSelectedOrDefaultYamlActionTemplate()
        {
            if (YamlActionTemplateComboBox != null && YamlActionTemplateComboBox.SelectedItem is YamlLibraryFormTemplateOption selected)
            {
                return selected;
            }

            return _yamlLibraryFormTemplates.FirstOrDefault();
        }

        private YamlDocumentInheritanceOption GetSelectedOrDefaultYamlDocumentInheritanceOption()
        {
            if (YamlDocumentInheritanceComboBox != null && YamlDocumentInheritanceComboBox.SelectedItem is YamlDocumentInheritanceOption selected)
            {
                return selected;
            }

            return _yamlDocumentInheritanceOptions.FirstOrDefault();
        }

        private void SetSelectedYamlActionTemplate(YamlLibraryFormTemplateOption template)
        {
            if (YamlActionTemplateComboBox == null || template == null)
            {
                return;
            }

            _isApplyingYamlActionTemplateSelection = true;
            try
            {
                YamlActionTemplateComboBox.SelectedItem = template;
            }
            finally
            {
                _isApplyingYamlActionTemplateSelection = false;
            }
        }

        private void SetSelectedYamlDocumentInheritanceOption(YamlDocumentInheritanceOption option)
        {
            if (YamlDocumentInheritanceComboBox == null || option == null)
            {
                return;
            }

            _isApplyingYamlDocumentInheritanceSelection = true;
            try
            {
                YamlDocumentInheritanceComboBox.SelectedItem = option;
            }
            finally
            {
                _isApplyingYamlDocumentInheritanceSelection = false;
            }
        }

        private async Task ApplyYamlActionTemplateAsync(YamlLibraryFormTemplateOption template)
        {
            if (template == null)
            {
                return;
            }

            YamlGeneratedFormSourceText = template.YamlText ?? string.Empty;
            IsYamlGeneratedFormVisible = false;
            YamlDocumentRuntimeState = null;
            ClearYamlGeneratedFormDiagnostics();
            YamlDocumentLastResultText = BuildDefaultYamlEditorResultHint();

            if (_yamlActionsMonacoEditor == null)
            {
                return;
            }

            _isUpdatingYamlGeneratedFormEditor = true;
            try
            {
                await _yamlActionsMonacoEditor.SetValueAsync(YamlGeneratedFormSourceText).ConfigureAwait(true);
            }
            finally
            {
                _isUpdatingYamlGeneratedFormEditor = false;
            }
        }

        private async Task ApplyYamlDocumentInheritanceOptionAsync(YamlDocumentInheritanceOption option)
        {
            if (option == null)
            {
                return;
            }

            YamlGeneratedFormSourceText = BuildYamlDocumentInheritanceSample(option);
            IsYamlGeneratedFormVisible = false;
            YamlDocumentRuntimeState = null;
            ClearYamlGeneratedFormDiagnostics();
            YamlDocumentLastResultText = BuildDefaultYamlEditorResultHint();

            if (_yamlDocumentMonacoEditor == null)
            {
                return;
            }

            _isUpdatingYamlGeneratedFormEditor = true;
            try
            {
                await _yamlDocumentMonacoEditor.SetValueAsync(YamlGeneratedFormSourceText).ConfigureAwait(true);
            }
            finally
            {
                _isUpdatingYamlGeneratedFormEditor = false;
            }
        }

        private void DisposeYamlGeneratedFormMonacoEditor(ref PhialeMonacoEditor editor)
        {
            if (editor == null)
            {
                return;
            }

            editor.ContentChanged -= HandleYamlGeneratedFormMonacoContentChanged;
            editor.Dispose();
            editor = null;
        }

        private static IReadOnlyList<YamlLibraryFormTemplateOption> LoadYamlLibraryFormTemplates()
        {
            var assembly = typeof(YamlLibraryMarker).Assembly;
            var compiler = new YamlComposedDocumentCompiler();
            var templates = new List<YamlLibraryFormTemplateOption>();

            foreach (var resourceName in assembly.GetManifestResourceNames()
                .Where(name =>
                    name.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase) &&
                    name.IndexOf(".Definitions.application.forms.actionShells.", StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase))
            {
                var yaml = ReadEmbeddedResourceText(assembly, resourceName);
                if (string.IsNullOrWhiteSpace(yaml))
                {
                    continue;
                }

                var compiled = compiler.Compile(yaml, new[] { assembly }, "en");
                if (!compiled.Success || !(compiled.Definition is YamlFormDocumentDefinition formDefinition))
                {
                    continue;
                }

                var displayName = string.IsNullOrWhiteSpace(formDefinition.Name)
                    ? formDefinition.Id
                    : formDefinition.Name;
                templates.Add(new YamlLibraryFormTemplateOption(resourceName, formDefinition.Id, displayName, yaml));
            }

            return templates
                .OrderBy(template => template.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static IReadOnlyList<YamlDocumentInheritanceOption> BuildYamlDocumentInheritanceOptions()
        {
            return new[]
            {
                new YamlDocumentInheritanceOption(
                    "header-confirm-bar",
                    "Header actions inheritance",
                    "review-request-header",
                    "Review request inheriting top actions"),
                new YamlDocumentInheritanceOption(
                    "confirm-footer-right",
                    "Footer actions inheritance",
                    "review-request-footer",
                    "Review request inheriting footer actions"),
                new YamlDocumentInheritanceOption(
                    "review-sticky-header-footer",
                    "Header and footer inheritance",
                    "review-request-full",
                    "Review request inheriting sticky header and footer")
            };
        }

        private static string ReadEmbeddedResourceText(Assembly assembly, string resourceName)
        {
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    return string.Empty;
                }

                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
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

        private sealed class YamlLibraryFormTemplateOption
        {
            public YamlLibraryFormTemplateOption(string resourceName, string documentId, string displayName, string yamlText)
            {
                ResourceName = resourceName ?? string.Empty;
                DocumentId = documentId ?? string.Empty;
                DisplayName = string.IsNullOrWhiteSpace(displayName) ? DocumentId : displayName;
                YamlText = yamlText ?? string.Empty;
            }

            public string ResourceName { get; }

            public string DocumentId { get; }

            public string DisplayName { get; }

            public string YamlText { get; }
        }

        private sealed class YamlDocumentInheritanceOption
        {
            public YamlDocumentInheritanceOption(string baseDocumentId, string displayName, string sampleDocumentId, string sampleDisplayName)
            {
                BaseDocumentId = baseDocumentId ?? string.Empty;
                DisplayName = string.IsNullOrWhiteSpace(displayName) ? BaseDocumentId : displayName;
                SampleDocumentId = sampleDocumentId ?? string.Empty;
                SampleDisplayName = sampleDisplayName ?? string.Empty;
            }

            public string BaseDocumentId { get; }

            public string DisplayName { get; }

            public string SampleDocumentId { get; }

            public string SampleDisplayName { get; }
        }
    }
}

