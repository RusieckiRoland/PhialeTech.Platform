using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using PhialeGrid.Core.Surface;

namespace PhialeTech.PhialeGrid.Wpf.Surface.Presenters
{
    /// <summary>
    /// Presenter dla overlayów surface gridu.
    /// </summary>
    public sealed class GridOverlayPresenter : ContentControl
    {
        public GridOverlayPresenter()
        {
            SetValue(ClipToBoundsProperty, true);
            SetResourceReference(StyleProperty, "PgGridSurfaceOverlayPresenterStyle");
            SetValue(HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch);
            SetValue(VerticalContentAlignmentProperty, VerticalAlignment.Stretch);
            IsHitTestVisible = false;
        }

        public GridOverlaySurfaceItem OverlayData
        {
            get { return (GridOverlaySurfaceItem)GetValue(OverlayDataProperty); }
            set { SetValue(OverlayDataProperty, value); }
        }

        public static readonly DependencyProperty OverlayDataProperty =
            DependencyProperty.Register(
                nameof(OverlayData),
                typeof(GridOverlaySurfaceItem),
                typeof(GridOverlayPresenter),
                new PropertyMetadata(null, OnOverlayDataChanged));

        public GridBounds Bounds
        {
            get { return (GridBounds)GetValue(BoundsProperty); }
            set { SetValue(BoundsProperty, value); }
        }

        public static readonly DependencyProperty BoundsProperty =
            DependencyProperty.Register(
                nameof(Bounds),
                typeof(GridBounds),
                typeof(GridOverlayPresenter),
                new PropertyMetadata(GridBounds.Empty, OnBoundsChanged));

        private static void OnOverlayDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var presenter = (GridOverlayPresenter)d;
            var overlay = (GridOverlaySurfaceItem)e.NewValue;

            if (overlay == null)
            {
                presenter.Content = null;
                presenter.Background = Brushes.Transparent;
                presenter.BorderThickness = new Thickness(0);
                presenter.Opacity = 1d;
                return;
            }

            presenter.Opacity = 1d;
            switch (overlay.Kind)
            {
                case GridOverlayKind.Selection:
                    presenter.Content = null;
                    presenter.Background = new SolidColorBrush(Color.FromArgb(50, 0, 0, 255));
                    presenter.BorderBrush = Brushes.Blue;
                    presenter.BorderThickness = new Thickness(2);
                    break;
                case GridOverlayKind.CurrentCell:
                    presenter.Content = null;
                    presenter.Background = Brushes.Transparent;
                    presenter.BorderBrush = Brushes.Green;
                    presenter.BorderThickness = new Thickness(2);
                    break;
                case GridOverlayKind.CurrentRecord:
                    presenter.ApplyCurrentRecordIndicator();
                    break;
                case GridOverlayKind.RowHighlight:
                    presenter.Content = null;
                    presenter.Background = Brushes.Transparent;
                    presenter.BorderBrush = Brushes.Transparent;
                    presenter.BorderThickness = new Thickness(0);
                    break;
                case GridOverlayKind.Validation:
                    presenter.Content = null;
                    presenter.Background = Brushes.Transparent;
                    presenter.BorderBrush = Brushes.OrangeRed;
                    presenter.BorderThickness = new Thickness(2);
                    break;
                default:
                    presenter.Content = null;
                    presenter.Background = Brushes.Transparent;
                    presenter.BorderBrush = Brushes.Transparent;
                    presenter.BorderThickness = new Thickness(0);
                    break;
            }
        }

        private void ApplyCurrentRecordIndicator()
        {
            var host = new Grid
            {
                IsHitTestVisible = false,
            };

            var bar = new Border
            {
                Width = 3,
                HorizontalAlignment = HorizontalAlignment.Right,
            };
            bar.SetResourceReference(Border.BackgroundProperty, "PgAccentBrush");

            var triangle = new Path
            {
                Data = Geometry.Parse("M 0 0 L 8 6 L 0 12 Z"),
                Width = 8,
                Height = 12,
                Margin = new Thickness(0, 0, 3, 0),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Stretch = Stretch.Fill,
            };
            triangle.SetResourceReference(Path.FillProperty, "PgAccentBrush");

            host.Children.Add(bar);
            host.Children.Add(triangle);

            Content = host;
            Background = Brushes.Transparent;
            BorderBrush = Brushes.Transparent;
            BorderThickness = new Thickness(0);
        }

        private static void OnBoundsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var presenter = (GridOverlayPresenter)d;
            var bounds = (GridBounds)e.NewValue;

            Canvas.SetLeft(presenter, bounds.X);
            Canvas.SetTop(presenter, bounds.Y);
            presenter.Width = bounds.Width;
            presenter.Height = bounds.Height;
        }
    }
}
