using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using PhialeGrid.Core.Columns;
using PhialeGrid.Core.Summaries;
using PhialeTech.ActiveLayerSelector;
using PhialeTech.Components.Shared.Model;
using PhialeTech.Components.Shared.Services;

namespace PhialeTech.GisStudio.Mock.Wpf.ViewModels;

public sealed class MainShellViewModel : ViewModelBase
{
    private static readonly IReadOnlyList<string> ScaleOptions = new[]
    {
        "1:1000",
        "1:2500",
        "1:5000",
        "1:10000",
        "1:25000"
    };

    private static readonly IReadOnlyList<string> PaperFormats = new[]
    {
        "A4 pion",
        "A4 poziom",
        "A3 poziom",
        "A2 poziom"
    };

    private static readonly IReadOnlyList<string> CatalogEntries = new[]
    {
        "WMS / Ortofotomapa wojewódzka",
        "PostGIS / Sieć wodna i armatura",
        "SHP / Ewidencja budynków",
        "GML / Zmiany z ośrodka",
        "GeoTIFF / Model wysokościowy"
    };

    private static readonly IReadOnlyList<string> RecentActions = new[]
    {
        "Identyfikacja obiektów na warstwie aktywnej",
        "Walidacja geometrii po imporcie GML",
        "Eksport zestawienia zmian do PDF",
        "Przekazanie lokalizacji do procesu OPS"
    };

    private static readonly string[] StatusOptions =
    {
        "Active",
        "Verified",
        "NeedsReview",
        "UnderMaintenance",
        "Planned",
        "Retired"
    };

    private static readonly string[] PriorityOptions =
    {
        "Critical",
        "High",
        "Medium",
        "Low"
    };

    private readonly IReadOnlyList<DemoGisRecordViewModel> _gridRecords;
    private readonly IReadOnlyList<GridColumnDefinition> _gridColumns;
    private readonly IReadOnlyList<GridSummaryDescriptor> _gridSummaries;
    private readonly IActiveLayerSelectorState _activeLayerSelectorState;
    private readonly ObservableCollection<MapDocumentViewModel> _mapDocuments = new();
    private bool _isAppDrawerOpen = true;
    private MapDocumentViewModel? _selectedMapDocument;
    private int _nextMapDocumentIndex = 2;
    private string _selectedWorkspace = "home";
    private string _selectedThemeCode = "day";
    private string _selectedInspectorTab = "style";
    private string _selectedScale = "1:5000";
    private string _selectedPaperFormat = "A3 poziom";
    private string _definitionQueryText = "status <> 'archiwalny'";
    private string _drawModeText = "Select";
    private bool _isInspectorOpen = true;
    private bool _showLabels = true;
    private bool _useScaleVisibility = true;
    private bool _lockLayerEditing;
    private bool _includeLegend = true;
    private bool _includeNorthArrow = true;
    private bool _includeOverviewMap;
    private double _layerOpacity = 84d;

    public MainShellViewModel()
    {
        _gridRecords = DemoGisDataLoader.LoadDefaultRecords();
        _gridColumns = BuildGridColumns();
        _gridSummaries = BuildGridSummaries();
        _activeLayerSelectorState = DemoActiveLayerSelectorFactory.CreateDefaultState();
        _activeLayerSelectorState.StateChanged += HandleActiveLayerStateChanged;

        var initialDocument = new MapDocumentViewModel("Mapa główna", false);
        _mapDocuments.Add(initialDocument);
        _selectedMapDocument = initialDocument;
    }

    public string AppTitle => "PhialeGIS Studio";

    public string WorkspaceLabel => "workspace: operations / central region";

    public string ViewportStatusTitle => "Dokument: Region północny / zmiany operacyjne";

    public string SelectedSectionTitle =>
        _selectedWorkspace switch
        {
            "studio" => "Studio GIS",
            "catalog" => "Katalog Danych",
            "business" => "Procesy Biznesowe",
            "reports" => "Raporty i Eksport",
            _ => "Start Workspace"
        };

    public string SelectedSectionSubtitle =>
        _selectedWorkspace switch
        {
            "studio" => "Lekki shell desktopowy dla map, analizy i danych przestrzennych.",
            "catalog" => "Źródła, połączenia i zasilanie danych są osobnym obszarem roboczym.",
            "business" => "Przejście z GIS do zwykłych kart operacyjnych, formularzy i kolejek pracy.",
            "reports" => "Zestawienia, wydruki i eksporty powinny być dostępne jako osobny widok roboczy.",
            _ => "Punkt wejścia do map, danych, procesów i raportów w jednym środowisku."
        };

    public string SelectedThemeCode
    {
        get => _selectedThemeCode;
        set
        {
            var normalized = value == "night" ? "night" : "day";
            if (!SetProperty(ref _selectedThemeCode, normalized))
            {
                return;
            }

            OnPropertyChanged(nameof(IsDayThemeSelected));
            OnPropertyChanged(nameof(IsNightThemeSelected));
            OnPropertyChanged(nameof(UseNightTheme));
            ApplyThemeToDocuments();
        }
    }

    public bool UseNightTheme => _selectedThemeCode == "night";

    public bool IsDayThemeSelected
    {
        get => _selectedThemeCode == "day";
        set
        {
            if (!value)
            {
                OnPropertyChanged();
                return;
            }

            SelectedThemeCode = "day";
        }
    }

    public bool IsNightThemeSelected
    {
        get => _selectedThemeCode == "night";
        set
        {
            if (!value)
            {
                OnPropertyChanged();
                return;
            }

            SelectedThemeCode = "night";
        }
    }

    public bool IsHomeSelected
    {
        get => _selectedWorkspace == "home";
        set => SelectWorkspace(value, "home");
    }

    public bool IsAppDrawerOpen
    {
        get => _isAppDrawerOpen;
        set
        {
            if (!SetProperty(ref _isAppDrawerOpen, value))
            {
                return;
            }

            OnPropertyChanged(nameof(AppDrawerButtonText));
            OnPropertyChanged(nameof(IsPushDrawerOpen));
            OnPropertyChanged(nameof(IsOverlayDrawerOpen));
        }
    }

    public string AppDrawerButtonText => _isAppDrawerOpen ? "Zwin" : "Menu";

    public WorkspaceKind SelectedWorkspaceKind =>
        _selectedWorkspace == "studio"
            ? WorkspaceKind.Map
            : WorkspaceKind.App;

    public NavigationDrawerMode CurrentNavigationDrawerMode =>
        SelectedWorkspaceKind == WorkspaceKind.Map
            ? NavigationDrawerMode.Overlay
            : NavigationDrawerMode.Push;

    public bool IsPushDrawerMode => CurrentNavigationDrawerMode == NavigationDrawerMode.Push;

    public bool IsOverlayDrawerMode => CurrentNavigationDrawerMode == NavigationDrawerMode.Overlay;

    public bool IsPushDrawerOpen => IsPushDrawerMode && _isAppDrawerOpen;

    public bool IsOverlayDrawerOpen => IsOverlayDrawerMode && _isAppDrawerOpen;

    public bool IsStudioSelected
    {
        get => _selectedWorkspace == "studio";
        set => SelectWorkspace(value, "studio");
    }

    public bool IsCatalogSelected
    {
        get => _selectedWorkspace == "catalog";
        set => SelectWorkspace(value, "catalog");
    }

    public bool IsBusinessSelected
    {
        get => _selectedWorkspace == "business";
        set => SelectWorkspace(value, "business");
    }

    public bool IsReportsSelected
    {
        get => _selectedWorkspace == "reports";
        set => SelectWorkspace(value, "reports");
    }

    public ObservableCollection<MapDocumentViewModel> MapDocuments => _mapDocuments;

    public string AppDrawerTitle =>
        _selectedWorkspace switch
        {
            "studio" => "Mapy i dokumenty",
            "catalog" => "Połączenia i źródła",
            "business" => "Kolejki i sprawy",
            "reports" => "Wydruki i analizy",
            _ => "Główne workspace"
        };

    public string AppDrawerSubtitle =>
        _selectedWorkspace switch
        {
            "studio" => "Mapa, dokumenty i edycja pracują jako główny obszar produkcyjny.",
            "catalog" => "Tu użytkownik zarządza zasilaniem WMS, SHP, GML, PostGIS i GeoTIFF.",
            "business" => "Widok procesów operacyjnych, formularzy i przejść do mapowego kontekstu.",
            "reports" => "Eksporty PDF, raporty dzienne, wydruki i statusy publikacji.",
            _ => "Startowy hub do map, danych, procesów i raportów."
        };

    public MapDocumentViewModel? SelectedMapDocument
    {
        get => _selectedMapDocument;
        set
        {
            if (!SetProperty(ref _selectedMapDocument, value))
            {
                return;
            }

            OnPropertyChanged(nameof(HasSelectedMapDocument));
            OnPropertyChanged(nameof(HasNoSelectedMapDocument));
        }
    }

    public bool HasSelectedMapDocument => _selectedMapDocument != null;

    public bool HasNoSelectedMapDocument => _selectedMapDocument == null;

    public bool IsInspectorOpen
    {
        get => _isInspectorOpen;
        set
        {
            if (!SetProperty(ref _isInspectorOpen, value))
            {
                return;
            }

            OnPropertyChanged(nameof(InspectorToggleText));
            OnPropertyChanged(nameof(IsInspectorClosed));
        }
    }

    public bool IsInspectorClosed => !_isInspectorOpen;

    public string InspectorToggleText => _isInspectorOpen ? "Zwiń panel" : "Otwórz panel";

    public bool IsStyleInspectorSelected
    {
        get => _selectedInspectorTab == "style";
        set => SelectInspectorTab(value, "style");
    }

    public bool IsDataInspectorSelected
    {
        get => _selectedInspectorTab == "data";
        set => SelectInspectorTab(value, "data");
    }

    public bool IsPrintInspectorSelected
    {
        get => _selectedInspectorTab == "print";
        set => SelectInspectorTab(value, "print");
    }

    public string SelectedScale
    {
        get => _selectedScale;
        set
        {
            if (!SetProperty(ref _selectedScale, value ?? "1:5000"))
            {
                return;
            }

            OnPropertyChanged(nameof(PrintSummaryText));
        }
    }

    public string SelectedPaperFormat
    {
        get => _selectedPaperFormat;
        set
        {
            if (!SetProperty(ref _selectedPaperFormat, value ?? "A3 poziom"))
            {
                return;
            }

            OnPropertyChanged(nameof(PrintSummaryText));
        }
    }

    public string DefinitionQueryText
    {
        get => _definitionQueryText;
        set => SetProperty(ref _definitionQueryText, value ?? string.Empty);
    }

    public string DrawModeText
    {
        get => _drawModeText;
        set => SetProperty(ref _drawModeText, value ?? string.Empty);
    }

    public bool ShowLabels
    {
        get => _showLabels;
        set => SetProperty(ref _showLabels, value);
    }

    public bool UseScaleVisibility
    {
        get => _useScaleVisibility;
        set => SetProperty(ref _useScaleVisibility, value);
    }

    public bool LockLayerEditing
    {
        get => _lockLayerEditing;
        set => SetProperty(ref _lockLayerEditing, value);
    }

    public bool IncludeLegend
    {
        get => _includeLegend;
        set
        {
            if (!SetProperty(ref _includeLegend, value))
            {
                return;
            }

            OnPropertyChanged(nameof(PrintSummaryText));
        }
    }

    public bool IncludeNorthArrow
    {
        get => _includeNorthArrow;
        set => SetProperty(ref _includeNorthArrow, value);
    }

    public bool IncludeOverviewMap
    {
        get => _includeOverviewMap;
        set => SetProperty(ref _includeOverviewMap, value);
    }

    public double LayerOpacity
    {
        get => _layerOpacity;
        set
        {
            var normalized = Math.Max(0d, Math.Min(100d, value));
            if (!SetProperty(ref _layerOpacity, normalized))
            {
                return;
            }

            OnPropertyChanged(nameof(LayerOpacityText));
        }
    }

    public string LayerOpacityText => $"{Math.Round(_layerOpacity):0}%";

    public bool IsGridReadOnly => false;

    public IReadOnlyList<GridColumnDefinition> GridColumns => _gridColumns;

    public IReadOnlyList<GridSummaryDescriptor> GridSummaries => _gridSummaries;

    public IReadOnlyList<DemoGisRecordViewModel> GridRecords => _gridRecords;

    public IActiveLayerSelectorState ActiveLayerSelectorState => _activeLayerSelectorState;

    public IReadOnlyList<string> AvailableScales => ScaleOptions;

    public IReadOnlyList<string> AvailablePaperFormats => PaperFormats;

    public IReadOnlyList<string> CatalogItems => CatalogEntries;

    public IReadOnlyList<string> RecentActionItems => RecentActions;

    public string ActiveLayerName => ActiveLayerItem?.Name ?? "Brak warstwy aktywnej";

    public string ActiveLayerPath => ActiveLayerItem?.TreePath ?? "Workspace / brak aktywnej warstwy";

    public string ActiveLayerSource => ActiveLayerItem?.LayerType ?? "Brak źródła";

    public string ActiveGeometryType => ActiveLayerItem?.GeometryType ?? "Nieznany";

    public string ActiveLayerCapabilitiesText =>
        $"{VisibleLayerCount} widoczne • {EditableLayerCount} edytowalne • {SnappableLayerCount} snappowalne";

    public string DrawerHeadline => $"Warstwa aktywna: {ActiveLayerName}";

    public string DataStatusText => $"{_gridRecords.Count} rekordów • {VisibleLayerCount} warstw widocznych";

    public string PrintSummaryText =>
        $"{SelectedPaperFormat} • skala {SelectedScale} • legenda {(IncludeLegend ? "on" : "off")}";

    public int VisibleLayerCount => _activeLayerSelectorState.Items.Count(item => item.IsVisible);

    public int EditableLayerCount => _activeLayerSelectorState.Items.Count(item => item.IsEditable);

    public int SnappableLayerCount => _activeLayerSelectorState.Items.Count(item => item.IsSnappable);

    public string RecordSummaryText =>
        $"Tabela atrybutów zsynchronizowana z warstwą \"{ActiveLayerName}\"";

    public string SelectedSectionCode => _selectedWorkspace;

    public MapDocumentViewModel CreateMapDocument()
    {
        var document = new MapDocumentViewModel($"Mapa {_nextMapDocumentIndex++}", UseNightTheme);
        _mapDocuments.Add(document);
        SelectedMapDocument = document;
        return document;
    }

    public MapDocumentViewModel? DuplicateSelectedMapDocument()
    {
        if (SelectedMapDocument == null)
        {
            return null;
        }

        var document = SelectedMapDocument.CreateDuplicate($"{SelectedMapDocument.Title} kopia");
        document.IsNightMode = UseNightTheme;
        _mapDocuments.Add(document);
        SelectedMapDocument = document;
        return document;
    }

    public MapDocumentViewModel? DetachSelectedMapDocument()
    {
        if (SelectedMapDocument == null)
        {
            return null;
        }

        SelectedMapDocument.IsViewportDetached = true;
        return SelectedMapDocument;
    }

    public void AttachMapDocument(MapDocumentViewModel? document)
    {
        if (document == null)
        {
            return;
        }

        document.IsNightMode = UseNightTheme;
        document.IsViewportDetached = false;
        SelectedMapDocument = document;
    }

    public void CloseNavigationDrawer()
    {
        IsAppDrawerOpen = false;
    }

    public void CloseNavigationDrawerIfOverlay()
    {
        if (IsOverlayDrawerOpen)
        {
            IsAppDrawerOpen = false;
        }
    }

    private ActiveLayerSelectorItemState? ActiveLayerItem =>
        _activeLayerSelectorState.Items.FirstOrDefault(item =>
            string.Equals(item.LayerId, _activeLayerSelectorState.ActiveLayerId, StringComparison.Ordinal));

    private IReadOnlyList<GridColumnDefinition> BuildGridColumns()
    {
        return new[]
        {
            new GridColumnDefinition("Category", "Kategoria", width: 150d, displayIndex: 0, valueType: typeof(string), isEditable: false),
            new GridColumnDefinition("ObjectName", "Obiekt", width: 230d, displayIndex: 1, valueType: typeof(string), isEditable: true),
            new GridColumnDefinition("ObjectId", "Id", width: 170d, displayIndex: 2, valueType: typeof(string), isEditable: false),
            new GridColumnDefinition("GeometryType", "Geometria", width: 120d, displayIndex: 3, valueType: typeof(string), isEditable: false),
            new GridColumnDefinition("Municipality", "Gmina", width: 140d, displayIndex: 4, valueType: typeof(string), isEditable: false),
            new GridColumnDefinition("District", "Obręb", width: 150d, displayIndex: 5, valueType: typeof(string), isEditable: false),
            new GridColumnDefinition("Status", "Status", width: 150d, displayIndex: 6, valueType: typeof(string), isEditable: true, editorKind: GridColumnEditorKind.Combo, editorItems: StatusOptions),
            new GridColumnDefinition("Priority", "Priorytet", width: 120d, displayIndex: 7, valueType: typeof(string), isEditable: true, editorKind: GridColumnEditorKind.Combo, editorItems: PriorityOptions),
            new GridColumnDefinition("AreaSquareMeters", "Powierzchnia", width: 135d, displayIndex: 8, valueType: typeof(decimal), isEditable: false),
            new GridColumnDefinition("LengthMeters", "Długość", width: 130d, displayIndex: 9, valueType: typeof(decimal), isEditable: false),
            new GridColumnDefinition("LastInspection", "Inspekcja", width: 140d, displayIndex: 10, valueType: typeof(DateTime), isEditable: true, editorKind: GridColumnEditorKind.DatePicker),
            new GridColumnDefinition("Owner", "Właściciel", width: 160d, displayIndex: 11, valueType: typeof(string), isEditable: true, editorKind: GridColumnEditorKind.Autocomplete, editorItems: BuildOwnerOptions()),
            new GridColumnDefinition("ScaleHint", "Skala", width: 110d, displayIndex: 12, valueType: typeof(int), isEditable: true, editorKind: GridColumnEditorKind.MaskedText, editMask: "^[0-9]{0,6}$")
        };
    }

    private IReadOnlyList<GridSummaryDescriptor> BuildGridSummaries()
    {
        return new[]
        {
            new GridSummaryDescriptor("ObjectId", GridSummaryType.Count, typeof(string)),
            new GridSummaryDescriptor("AreaSquareMeters", GridSummaryType.Sum, typeof(decimal)),
            new GridSummaryDescriptor("LengthMeters", GridSummaryType.Sum, typeof(decimal))
        };
    }

    private IReadOnlyList<string> BuildOwnerOptions()
    {
        return _gridRecords
            .Select(record => record.Owner)
            .Where(owner => !string.IsNullOrWhiteSpace(owner))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(owner => owner, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private void HandleActiveLayerStateChanged(object? sender, EventArgs e)
    {
        RaiseLayerInspectorChanges();
    }

    private void RaiseLayerInspectorChanges()
    {
        OnPropertyChanged(nameof(ActiveLayerName));
        OnPropertyChanged(nameof(ActiveLayerPath));
        OnPropertyChanged(nameof(ActiveLayerSource));
        OnPropertyChanged(nameof(ActiveGeometryType));
        OnPropertyChanged(nameof(ActiveLayerCapabilitiesText));
        OnPropertyChanged(nameof(DrawerHeadline));
        OnPropertyChanged(nameof(DataStatusText));
        OnPropertyChanged(nameof(RecordSummaryText));
        OnPropertyChanged(nameof(VisibleLayerCount));
        OnPropertyChanged(nameof(EditableLayerCount));
        OnPropertyChanged(nameof(SnappableLayerCount));
    }

    private void ApplyThemeToDocuments()
    {
        foreach (var document in _mapDocuments)
        {
            document.IsNightMode = UseNightTheme;
        }
    }

    private void SelectWorkspace(bool isSelected, string workspaceCode)
    {
        if (!isSelected)
        {
            OnPropertyChanged(nameof(IsHomeSelected));
            OnPropertyChanged(nameof(IsStudioSelected));
            OnPropertyChanged(nameof(IsCatalogSelected));
            OnPropertyChanged(nameof(IsBusinessSelected));
            OnPropertyChanged(nameof(IsReportsSelected));
            return;
        }

        if (!SetProperty(ref _selectedWorkspace, workspaceCode, nameof(SelectedSectionCode)))
        {
            return;
        }

        OnPropertyChanged(nameof(IsHomeSelected));
        OnPropertyChanged(nameof(IsStudioSelected));
        OnPropertyChanged(nameof(IsCatalogSelected));
        OnPropertyChanged(nameof(IsBusinessSelected));
        OnPropertyChanged(nameof(IsReportsSelected));
        OnPropertyChanged(nameof(SelectedSectionTitle));
        OnPropertyChanged(nameof(SelectedSectionSubtitle));
        OnPropertyChanged(nameof(AppDrawerTitle));
        OnPropertyChanged(nameof(AppDrawerSubtitle));
        OnPropertyChanged(nameof(SelectedWorkspaceKind));
        OnPropertyChanged(nameof(CurrentNavigationDrawerMode));
        OnPropertyChanged(nameof(IsPushDrawerMode));
        OnPropertyChanged(nameof(IsOverlayDrawerMode));
        OnPropertyChanged(nameof(IsPushDrawerOpen));
        OnPropertyChanged(nameof(IsOverlayDrawerOpen));
    }

    private void SelectInspectorTab(bool isSelected, string tabCode)
    {
        if (!isSelected)
        {
            OnPropertyChanged(nameof(IsStyleInspectorSelected));
            OnPropertyChanged(nameof(IsDataInspectorSelected));
            OnPropertyChanged(nameof(IsPrintInspectorSelected));
            return;
        }

        if (!SetProperty(ref _selectedInspectorTab, tabCode))
        {
            return;
        }

        OnPropertyChanged(nameof(IsStyleInspectorSelected));
        OnPropertyChanged(nameof(IsDataInspectorSelected));
        OnPropertyChanged(nameof(IsPrintInspectorSelected));
    }
}

