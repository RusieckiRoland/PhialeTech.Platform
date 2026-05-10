using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PhialeGrid.Core.Columns;

namespace PhialeTech.PhialeGrid.Wpf.Controls
{
    public partial class PhialeToolsPanel : UserControl
    {
        public PhialeToolsPanel()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            DataContext = FindAncestor<PhialeGrid>(this);
        }

        private void HandleShowColumnsClick(object sender, RoutedEventArgs e)
        {
            if (DataContext is PhialeGrid grid)
            {
                ColumnsItemsControl.ItemsSource = grid.ColumnChooserColumns;
            }

            // Disable outer ScrollViewer so this control gets bounded height,
            // allowing the ColumnsView Grid's Row="*" to constrain the inner scroll.
            var outerScroll = FindAncestor<ScrollViewer>(this);
            if (outerScroll != null)
                outerScroll.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;

            MainView.Visibility = Visibility.Collapsed;
            ColumnsView.Visibility = Visibility.Visible;
        }

        private void HandleBackFromColumnsClick(object sender, RoutedEventArgs e)
        {
            ColumnsView.Visibility = Visibility.Collapsed;
            MainView.Visibility = Visibility.Visible;

            var outerScroll = FindAncestor<ScrollViewer>(this);
            if (outerScroll != null)
                outerScroll.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        }

        private void HandleColumnVisibilityToggleClick(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox &&
                checkBox.DataContext is GridColumnDefinition column &&
                DataContext is PhialeGrid grid)
            {
                grid.ToggleColumnVisibilityFromPanel(column.Id);
                e.Handled = true;
            }
        }

        private static T FindAncestor<T>(DependencyObject child) where T : DependencyObject
        {
            var parent = VisualTreeHelper.GetParent(child);
            while (parent != null)
            {
                if (parent is T found)
                {
                    return found;
                }

                parent = VisualTreeHelper.GetParent(parent);
            }

            return null;
        }
    }
}
