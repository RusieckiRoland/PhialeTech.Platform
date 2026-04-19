using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using System;
using System.ComponentModel;
using PhialeTech.Components.Shared.Services;
using PhialeTech.Components.Shared.ViewModels;

namespace PhialeTech.Components.Avalonia;

public partial class MainWindow : Window
{
    private readonly DemoApplicationServices _applicationServices;
    private readonly bool _ownsApplicationServices;
    private readonly DemoShellViewModel _viewModel;
    private WebHostShowcaseView? _webHostShowcaseView;
    private PdfViewerShowcaseView? _pdfViewerShowcaseView;
    private ReportDesignerShowcaseView? _reportDesignerShowcaseView;

    public MainWindow()
        : this(DemoApplicationServices.CreateIsolatedForWindow(), true)
    {
    }

    public MainWindow(DemoApplicationServices applicationServices, bool ownsApplicationServices = false)
    {
        InitializeComponent();
        _applicationServices = applicationServices ?? throw new ArgumentNullException(nameof(applicationServices));
        _ownsApplicationServices = ownsApplicationServices;
        _viewModel = new DemoShellViewModel("Avalonia", definitionManager: _applicationServices.DefinitionManager);
        _viewModel.PropertyChanged += HandleViewModelPropertyChanged;
        DataContext = _viewModel;
        RefreshWebComponentSurface();
        Closed += HandleClosed;
    }

    private void HandleClosed(object? sender, EventArgs e)
    {
        _viewModel.PropertyChanged -= HandleViewModelPropertyChanged;
        _webHostShowcaseView?.Dispose();
        _webHostShowcaseView = null;
        _pdfViewerShowcaseView?.Dispose();
        _pdfViewerShowcaseView = null;
        _reportDesignerShowcaseView?.Dispose();
        _reportDesignerShowcaseView = null;
        WebHostSurfacePresenter.Content = null;

        if (_ownsApplicationServices)
        {
            _applicationServices.Dispose();
        }
    }

    private void HandleViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DemoShellViewModel.SelectedExample))
        {
            RefreshWebComponentSurface();
            return;
        }

        if (e.PropertyName == nameof(DemoShellViewModel.LanguageCode) ||
            e.PropertyName == nameof(DemoShellViewModel.SelectedThemeCode))
        {
            if (_reportDesignerShowcaseView != null)
            {
                _ = _reportDesignerShowcaseView.ApplyEnvironmentAsync(_viewModel.LanguageCode, ResolveReportDesignerTheme());
            }
        }
    }

    private void RefreshWebComponentSurface()
    {
        var shouldShowWebComponents = _viewModel.ShowWebComponentsSurface;
        DefaultDemoSurfaceScrollViewer.IsVisible = !shouldShowWebComponents;
        WebHostSurfaceContainer.IsVisible = shouldShowWebComponents;

        if (!shouldShowWebComponents)
        {
            if (WebHostSurfacePresenter.Content != null)
            {
                WebHostSurfacePresenter.Content = null;
            }

            return;
        }

        if (_viewModel.ShowWebHostSurface)
        {
            if (_webHostShowcaseView == null)
            {
                _webHostShowcaseView = new WebHostShowcaseView();
            }

            if (!ReferenceEquals(WebHostSurfacePresenter.Content, _webHostShowcaseView))
            {
                WebHostSurfacePresenter.Content = _webHostShowcaseView;
            }

            return;
        }

        if (_viewModel.ShowPdfViewerSurface)
        {
            if (_pdfViewerShowcaseView == null)
            {
                _pdfViewerShowcaseView = new PdfViewerShowcaseView();
            }

            if (!ReferenceEquals(WebHostSurfacePresenter.Content, _pdfViewerShowcaseView))
            {
                WebHostSurfacePresenter.Content = _pdfViewerShowcaseView;
            }

            return;
        }

        if (_viewModel.ShowReportDesignerSurface)
        {
            if (_reportDesignerShowcaseView == null)
            {
                _reportDesignerShowcaseView = new ReportDesignerShowcaseView(_viewModel.LanguageCode, ResolveReportDesignerTheme());
            }

            if (!ReferenceEquals(WebHostSurfacePresenter.Content, _reportDesignerShowcaseView))
            {
                WebHostSurfacePresenter.Content = _reportDesignerShowcaseView;
            }

            _ = _reportDesignerShowcaseView.ApplyEnvironmentAsync(_viewModel.LanguageCode, ResolveReportDesignerTheme());
        }
    }

    private string ResolveReportDesignerTheme()
    {
        return NormalizeThemeCode(_viewModel.SelectedThemeCode);
    }

    private static string NormalizeThemeCode(string? themeCode)
    {
        if (string.Equals(themeCode, "night", StringComparison.OrdinalIgnoreCase))
        {
            return "dark";
        }

        if (string.Equals(themeCode, "day", StringComparison.OrdinalIgnoreCase))
        {
            return "light";
        }

        return Application.Current?.ActualThemeVariant == ThemeVariant.Dark ? "dark" : "light";
    }
}
