using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using PhialeTech.GisStudio.Mock.Wpf.ViewModels;

namespace PhialeTech.GisStudio.Mock.Wpf;

public partial class MainWindow : Window
{
    private const double RailWidth = 56d;
    private const double NavigationDrawerWidth = 240d;
    private const double PushDrawerClosedWidth = 0d;
    private const double OverlayDrawerOpenX = 0d;
    private const double OverlayDrawerClosedX = -NavigationDrawerWidth;
    private static readonly Uri ThemeDayUri = new(
        "pack://application:,,,/PhialeTech.Styles.Wpf;component/Themes/ThemeTokens.Day.xaml",
        UriKind.Absolute);

    private static readonly Uri ThemeNightUri = new(
        "pack://application:,,,/PhialeTech.Styles.Wpf;component/Themes/ThemeTokens.Night.xaml",
        UriKind.Absolute);

    private static readonly Uri MockThemeDayUri = new(
        "pack://application:,,,/PhialeTech.Styles.Wpf;component/Themes/GisStudio.Mock.Theme.Day.xaml",
        UriKind.Absolute);

    private static readonly Uri MockThemeNightUri = new(
        "pack://application:,,,/PhialeTech.Styles.Wpf;component/Themes/GisStudio.Mock.Theme.Night.xaml",
        UriKind.Absolute);

    private readonly MainShellViewModel _viewModel = new();
    private readonly Dictionary<MapDocumentViewModel, DetachedMapWindow> _detachedWindows = new();
    private bool _closeDrawerAfterNavigationSelection;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _viewModel;
        _viewModel.PropertyChanged += HandleViewModelPropertyChanged;
        Loaded += (_, _) => ApplyAppDrawerState(animated: false);
        StudioGrid.LanguageDirectory = Path.Combine(AppContext.BaseDirectory, "PhialeGrid.Localization", "Languages");
        StudioLayerSelector.LanguageDirectory = Path.Combine(AppContext.BaseDirectory, "PhialeTech.ActiveLayerSelector", "Languages");
        ApplyTheme(_viewModel.SelectedThemeCode);
    }

    private void HandleViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainShellViewModel.SelectedThemeCode))
        {
            ApplyTheme(_viewModel.SelectedThemeCode);
        }
        else if (e.PropertyName == nameof(MainShellViewModel.IsAppDrawerOpen))
        {
            ApplyAppDrawerState(animated: true);
        }
        else if (e.PropertyName == nameof(MainShellViewModel.CurrentNavigationDrawerMode))
        {
            ApplyAppDrawerState(animated: false);
        }
    }

    private void ApplyTheme(string themeCode)
    {
        ApplyApplicationTheme(themeCode);

        var isNight = string.Equals(themeCode, "night", StringComparison.OrdinalIgnoreCase);
        StudioGrid.IsNightMode = isNight;
        StudioLayerSelector.IsNightMode = isNight;
    }

    private void HandleCreateMapClick(object sender, RoutedEventArgs e)
    {
        _viewModel.IsStudioSelected = true;
        _viewModel.CreateMapDocument();
    }

    private void HandleDuplicateMapClick(object sender, RoutedEventArgs e)
    {
        _viewModel.DuplicateSelectedMapDocument();
    }

    private void HandleDetachMapClick(object sender, RoutedEventArgs e)
    {
        var document = _viewModel.SelectedMapDocument;
        if (document == null)
        {
            return;
        }

        if (_detachedWindows.TryGetValue(document, out var existingWindow))
        {
            if (document.IsViewportDetached)
            {
                existingWindow.Close();
            }
            else
            {
                existingWindow.Activate();
            }

            return;
        }

        _viewModel.DetachSelectedMapDocument();

        var window = new DetachedMapWindow
        {
            DataContext = document,
            Owner = this
        };

        window.Closed += (_, _) =>
        {
            _detachedWindows.Remove(document);
            if (document.IsViewportDetached)
            {
                _viewModel.AttachMapDocument(document);
            }
        };

        _detachedWindows[document] = window;
        window.Show();
        window.Activate();
    }

    internal void CloseOverlayNavigationDrawer()
    {
        _viewModel.CloseNavigationDrawerIfOverlay();
    }

    private void HandleNavigationItemClick(object sender, RoutedEventArgs e)
    {
        if (sender is not ToggleButton)
        {
            return;
        }

        if (_closeDrawerAfterNavigationSelection || _viewModel.IsOverlayDrawerOpen)
        {
            _viewModel.CloseNavigationDrawer();
        }

        _closeDrawerAfterNavigationSelection = false;
    }

    private void HandleNavigationItemPointerDown(object sender, MouseButtonEventArgs e)
    {
        _closeDrawerAfterNavigationSelection = _viewModel.IsOverlayDrawerOpen;
    }

    private void HandleWindowPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape || !_viewModel.IsOverlayDrawerOpen)
        {
            return;
        }

        _viewModel.CloseNavigationDrawer();
        e.Handled = true;
    }

    private void ApplyAppDrawerState(bool animated)
    {
        LeftRailHost.Width = RailWidth;

        var pushTarget = _viewModel.IsPushDrawerOpen
            ? NavigationDrawerWidth
            : PushDrawerClosedWidth;

        ApplyWidth(PushDrawerHost, pushTarget, animated && _viewModel.IsPushDrawerMode);
        PushDrawerHost.IsHitTestVisible = _viewModel.IsPushDrawerOpen;

        var overlayTarget = _viewModel.IsOverlayDrawerOpen
            ? OverlayDrawerOpenX
            : OverlayDrawerClosedX;

        ApplyTransform(OverlayDrawerTransform, overlayTarget, animated && _viewModel.IsOverlayDrawerMode);
        OverlayDrawerHost.IsHitTestVisible = _viewModel.IsOverlayDrawerOpen;
        OverlayDrawerLayer.IsHitTestVisible = _viewModel.IsOverlayDrawerOpen;
    }

    private static void ApplyWidth(FrameworkElement element, double target, bool animated)
    {
        if (!animated)
        {
            element.Width = target;
            return;
        }

        var animation = new DoubleAnimation
        {
            To = target,
            Duration = TimeSpan.FromMilliseconds(180),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        element.BeginAnimation(FrameworkElement.WidthProperty, animation, HandoffBehavior.SnapshotAndReplace);
    }

    private static void ApplyTransform(TranslateTransform transform, double target, bool animated)
    {
        if (!animated)
        {
            transform.X = target;
            return;
        }

        var animation = new DoubleAnimation
        {
            To = target,
            Duration = TimeSpan.FromMilliseconds(180),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        transform.BeginAnimation(TranslateTransform.XProperty, animation, HandoffBehavior.SnapshotAndReplace);
    }

    private static void ApplyApplicationTheme(string themeCode)
    {
        var appResources = Application.Current.Resources;
        var isNight = string.Equals(themeCode, "night", StringComparison.OrdinalIgnoreCase);
        var targetUri = isNight
            ? ThemeNightUri
            : ThemeDayUri;
        var mockThemeUri = isNight
            ? MockThemeNightUri
            : MockThemeDayUri;

        var existingTheme = appResources.MergedDictionaries.FirstOrDefault(dictionary =>
            dictionary.Source != null &&
            dictionary.Source.ToString().IndexOf("/Themes/ThemeTokens.", StringComparison.OrdinalIgnoreCase) >= 0);

        var existingMockTheme = appResources.MergedDictionaries.FirstOrDefault(dictionary =>
            dictionary.Source != null &&
            dictionary.Source.ToString().IndexOf("/Themes/GisStudio.Mock.Theme.", StringComparison.OrdinalIgnoreCase) >= 0);

        if (existingTheme == null)
        {
            appResources.MergedDictionaries.Insert(0, new ResourceDictionary { Source = targetUri });
        }
        else if (existingTheme.Source == null ||
                 !string.Equals(existingTheme.Source.ToString(), targetUri.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            existingTheme.Source = targetUri;
        }

        if (existingMockTheme == null)
        {
            appResources.MergedDictionaries.Add(new ResourceDictionary { Source = mockThemeUri });
        }
        else if (existingMockTheme.Source == null ||
                 !string.Equals(existingMockTheme.Source.ToString(), mockThemeUri.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            existingMockTheme.Source = mockThemeUri;
        }
    }
}
