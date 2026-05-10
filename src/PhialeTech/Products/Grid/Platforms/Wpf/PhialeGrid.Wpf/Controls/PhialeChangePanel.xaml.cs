using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PhialeGrid.Core;

namespace PhialeTech.PhialeGrid.Wpf.Controls
{
    /// <summary>
    /// Thin content control hosted by the grid workspace panel for pending row changes.
    /// </summary>
    public partial class PhialeChangePanel : UserControl
    {
        public PhialeChangePanel()
        {
            InitializeComponent();
        }

        private void HandleChangePanelLoaded(object sender, RoutedEventArgs e)
        {
            var grid = FindAncestorGrid(this);
            if (grid == null)
            {
                throw new InvalidOperationException("Change panel requires a PhialeGrid ancestor.");
            }

            DataContext = grid;
        }

        private void HandleGoToRowClick(object sender, RoutedEventArgs e)
        {
            var change = (sender as FrameworkElement)?.DataContext as PhialeGrid.ChangePanelItem;
            if (change == null)
            {
                throw new InvalidOperationException("Change panel row navigation requires a change panel item.");
            }

            var grid = FindAncestorGrid(sender as DependencyObject);
            if (grid == null)
            {
                throw new InvalidOperationException("Change panel row navigation requires a PhialeGrid ancestor.");
            }

            if (string.IsNullOrWhiteSpace(change.RowId) || string.IsNullOrWhiteSpace(change.NavigationColumnId))
            {
                throw new InvalidOperationException("Change panel row navigation requires a row id and column id.");
            }

            if (!grid.ScrollCellIntoView(change.RowId, change.NavigationColumnId, GridScrollAlignment.Start, setCurrentCell: true))
            {
                throw new InvalidOperationException("Change panel row navigation failed for row '" + change.RowId + "' and column '" + change.NavigationColumnId + "'.");
            }
        }

        private void HandleChangedRowsFilterToggleClick(object sender, RoutedEventArgs e)
        {
            var grid = FindAncestorGrid(sender as DependencyObject);
            if (grid == null)
            {
                throw new InvalidOperationException("Change panel changed rows filter requires a PhialeGrid ancestor.");
            }

            grid.ToggleChangePanelChangedRowsFilter();
        }

        private static PhialeGrid FindAncestorGrid(DependencyObject source)
        {
            var current = source;
            while (current != null)
            {
                var grid = current as PhialeGrid;
                if (grid != null)
                {
                    return grid;
                }

                current = VisualTreeHelper.GetParent(current);
            }

            return null;
        }
    }
}
