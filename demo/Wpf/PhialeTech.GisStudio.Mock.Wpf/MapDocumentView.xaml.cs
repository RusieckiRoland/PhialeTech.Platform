using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using PhialeTech.GisStudio.Mock.Wpf.ViewModels;

namespace PhialeTech.GisStudio.Mock.Wpf;

public partial class MapDocumentView : UserControl
{
    private const double InspectorOpenX = 0d;
    private const double InspectorClosedX = 360d;
    private INotifyPropertyChanged? _notifier;

    public MapDocumentView()
    {
        InitializeComponent();

        var gridLanguageDirectory = Path.Combine(AppContext.BaseDirectory, "PhialeGrid.Localization", "Languages");
        CompactAttributeGrid.LanguageDirectory = gridLanguageDirectory;
        FullAttributeGrid.LanguageDirectory = gridLanguageDirectory;

        Loaded += HandleLoaded;
        DataContextChanged += HandleDataContextChanged;
    }

    private void HandleLoaded(object sender, RoutedEventArgs e)
    {
        ApplyDrawerState(animated: false);
    }

    private void HandleDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_notifier != null)
        {
            _notifier.PropertyChanged -= HandleViewModelPropertyChanged;
        }

        _notifier = e.NewValue as INotifyPropertyChanged;
        if (_notifier != null)
        {
            _notifier.PropertyChanged += HandleViewModelPropertyChanged;
        }

        ApplyDrawerState(animated: false);
    }

    private void HandleViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MapDocumentViewModel.IsInspectorOpen))
        {
            ApplyDrawerState(animated: true);
        }
    }

    private void ApplyDrawerState(bool animated)
    {
        if (DataContext is not MapDocumentViewModel viewModel)
        {
            return;
        }

        ApplyTransform(
            InspectorDrawerTransform,
            viewModel.IsInspectorOpen ? InspectorOpenX : InspectorClosedX,
            animated);
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
}
