using System;
using System.Collections.Generic;
using System.Linq;
using PhialeGrid.Core.Columns;
using PhialeGrid.Core.Summaries;
using PhialeTech.ActiveLayerSelector;
using PhialeTech.Components.Shared.Model;
using PhialeTech.Components.Shared.Services;

namespace PhialeTech.GisStudio.Mock.Wpf.ViewModels;

public sealed class MapDocumentViewModel : ViewModelBase
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

    private static readonly IReadOnlyList<string> ExplorerItems = new[]
    {
        "Warstwy robocze",
        "Zakładki widoku",
        "Wyniki identyfikacji",
        "Wydruki i layouty",
        "Połączenia danych"
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
    private string _selectedInspectorTab = "style";
    private string _selectedScale = "1:5000";
    private string _selectedPaperFormat = "A3 poziom";
    private string _definitionQueryText = "status <> 'archiwalny'";
    private string _drawModeText = "Select";
    private string _title;
    private bool _isNightMode;
    private bool _isViewportDetached;
    private bool _isExplorerOpen = true;
    private bool _isInspectorOpen = true;
    private bool _isDslOpen = true;
    private bool _isAttributeTableMaximized;
    private bool _showLabels = true;
    private bool _useScaleVisibility = true;
    private bool _lockLayerEditing;
    private bool _includeLegend = true;
    private bool _includeNorthArrow = true;
    private bool _includeOverviewMap;
    private double _layerOpacity = 84d;

    public MapDocumentViewModel(string title, bool isNightMode)
        : this(
            title,
            DemoGisDataLoader.LoadDefaultRecords(),
            DemoActiveLayerSelectorFactory.CreateDefaultState(),
            isNightMode)
    {
    }

    private MapDocumentViewModel(
        string title,
        IReadOnlyList<DemoGisRecordViewModel> gridRecords,
        IActiveLayerSelectorState activeLayerSelectorState,
        bool isNightMode)
    {
        _title = string.IsNullOrWhiteSpace(title) ? "Mapa" : title;
        _gridRecords = gridRecords;
        _gridColumns = BuildGridColumns();
        _gridSummaries = BuildGridSummaries();
        _activeLayerSelectorState = activeLayerSelectorState;
        _activeLayerSelectorState.StateChanged += HandleActiveLayerStateChanged;
        _isNightMode = isNightMode;
    }

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, string.IsNullOrWhiteSpace(value) ? "Mapa" : value);
    }

    public bool IsNightMode
    {
        get => _isNightMode;
        set => SetProperty(ref _isNightMode, value);
    }

    public bool IsViewportDetached
    {
        get => _isViewportDetached;
        set
        {
            if (!SetProperty(ref _isViewportDetached, value))
            {
                return;
            }

            OnPropertyChanged(nameof(IsViewportAttached));
            OnPropertyChanged(nameof(IsDetachedDataWorkspaceVisible));
            OnPropertyChanged(nameof(IsDslVisibleInMain));
            OnPropertyChanged(nameof(DetachedStateText));
            OnPropertyChanged(nameof(ViewportWindowButtonText));
        }
    }

    public bool IsViewportAttached => !_isViewportDetached;

    public bool IsDetachedDataWorkspaceVisible => _isViewportDetached;

    public bool IsExplorerOpen
    {
        get => _isExplorerOpen;
        set
        {
            if (!SetProperty(ref _isExplorerOpen, value))
            {
                return;
            }

            OnPropertyChanged(nameof(IsExplorerClosed));
        }
    }

    public bool IsExplorerClosed => !_isExplorerOpen;

    public bool IsInspectorOpen
    {
        get => _isInspectorOpen;
        set
        {
            if (!SetProperty(ref _isInspectorOpen, value))
            {
                return;
            }

            OnPropertyChanged(nameof(IsInspectorClosed));
        }
    }

    public bool IsInspectorClosed => !_isInspectorOpen;

    public bool IsDslOpen
    {
        get => _isDslOpen;
        set
        {
            if (!SetProperty(ref _isDslOpen, value))
            {
                return;
            }

            OnPropertyChanged(nameof(IsDslClosed));
        }
    }

    public bool IsDslClosed => !_isDslOpen;

    public bool IsDslVisibleInMain => _isDslOpen && !_isViewportDetached;

    public bool IsAttributeTableMaximized
    {
        get => _isAttributeTableMaximized;
        set
        {
            if (!SetProperty(ref _isAttributeTableMaximized, value))
            {
                return;
            }

            OnPropertyChanged(nameof(IsDocumentLayoutVisible));
            OnPropertyChanged(nameof(IsAttributeTableOnlyVisible));
        }
    }

    public bool IsDocumentLayoutVisible => !_isAttributeTableMaximized;

    public bool IsAttributeTableOnlyVisible => _isAttributeTableMaximized;

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

    public IReadOnlyList<string> ExplorerItemsList => ExplorerItems;

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

    public string DocumentBadgeText => $"{Title} • {ActiveLayerName}";

    public string DetachedStateText =>
        _isViewportDetached
            ? "Viewport mapy jest wypięty do osobnego okna. To okno zostaje przeznaczone na dane i inspektory."
            : "Viewport pracuje wewnątrz głównego shellu.";

    public string ViewportWindowButtonText => _isViewportDetached ? "Przywróć" : "Wypnij";

    public string ObjectInspectorTitle => $"Obiekt / selekcja • {ActiveLayerName}";

    public string ObjectInspectorText =>
        $"Aktywna geometria: {ActiveGeometryType}. Tu trafia lista wybranych obiektów, atrybuty, relacje i akcje biznesowe.";

    public string LayerPropertiesTitle => $"Layer Properties • {ActiveLayerName}";

    public string LayerPropertiesText =>
        $"Źródło: {ActiveLayerSource}. Zakres skali, definicja warstwy, etykiety i parametry widoczności pozostają po stronie danych.";

    public int VisibleLayerCount => _activeLayerSelectorState.Items.Count(item => item.IsVisible);

    public int EditableLayerCount => _activeLayerSelectorState.Items.Count(item => item.IsEditable);

    public int SnappableLayerCount => _activeLayerSelectorState.Items.Count(item => item.IsSnappable);

    public string RecordSummaryText =>
        $"Tabela atrybutów zsynchronizowana z warstwą \"{ActiveLayerName}\"";

    public string DslCaptionText =>
        $"dsl://{Title.ToLowerInvariant().Replace(' ', '-')}/transform";

    public MapDocumentViewModel CreateDuplicate(string title)
    {
        var records = _gridRecords
            .Select(record => (DemoGisRecordViewModel)record.Clone())
            .ToArray();

        var state = new DemoActiveLayerSelectorState(_activeLayerSelectorState.Items.Select(CloneLayerItem).ToArray());
        state.SetActiveLayer(_activeLayerSelectorState.ActiveLayerId);

        return new MapDocumentViewModel(title, records, state, IsNightMode)
        {
            IsViewportDetached = IsViewportDetached,
            IsExplorerOpen = IsExplorerOpen,
            IsInspectorOpen = IsInspectorOpen,
            IsDslOpen = IsDslOpen,
            ShowLabels = ShowLabels,
            UseScaleVisibility = UseScaleVisibility,
            LockLayerEditing = LockLayerEditing,
            IncludeLegend = IncludeLegend,
            IncludeNorthArrow = IncludeNorthArrow,
            IncludeOverviewMap = IncludeOverviewMap,
            LayerOpacity = LayerOpacity,
            SelectedScale = SelectedScale,
            SelectedPaperFormat = SelectedPaperFormat,
            DefinitionQueryText = DefinitionQueryText,
            DrawModeText = DrawModeText,
        };
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
        OnPropertyChanged(nameof(DocumentBadgeText));
        OnPropertyChanged(nameof(ObjectInspectorTitle));
        OnPropertyChanged(nameof(ObjectInspectorText));
        OnPropertyChanged(nameof(LayerPropertiesTitle));
        OnPropertyChanged(nameof(LayerPropertiesText));
        OnPropertyChanged(nameof(RecordSummaryText));
        OnPropertyChanged(nameof(VisibleLayerCount));
        OnPropertyChanged(nameof(EditableLayerCount));
        OnPropertyChanged(nameof(SnappableLayerCount));
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

    private static ActiveLayerSelectorItemState CloneLayerItem(ActiveLayerSelectorItemState item)
    {
        return new ActiveLayerSelectorItemState
        {
            LayerId = item.LayerId,
            Name = item.Name,
            TreePath = item.TreePath,
            LayerType = item.LayerType,
            GeometryType = item.GeometryType,
            IsActive = item.IsActive,
            IsVisible = item.IsVisible,
            IsSelectable = item.IsSelectable,
            IsEditable = item.IsEditable,
            IsSnappable = item.IsSnappable,
            CanBecomeActive = item.CanBecomeActive,
            CanToggleVisible = item.CanToggleVisible,
            CanToggleSelectable = item.CanToggleSelectable,
            CanToggleEditable = item.CanToggleEditable,
            CanToggleSnappable = item.CanToggleSnappable
        };
    }
}
