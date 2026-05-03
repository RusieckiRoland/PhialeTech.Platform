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

        private void HandleGoToRowClick(object sender, RoutedEventArgs e)
        {
            var rowId = (sender as FrameworkElement)?.DataContext as string;
            if (string.IsNullOrWhiteSpace(rowId))
            {
                throw new InvalidOperationException("Change panel row navigation requires a row id.");
            }

            var grid = FindAncestorGrid(sender as DependencyObject);
            if (grid == null)
            {
                throw new InvalidOperationException("Change panel row navigation requires a PhialeGrid ancestor.");
            }

            if (!grid.ScrollCellIntoView(rowId, "ObjectName", GridScrollAlignment.Start, setCurrentCell: true))
            {
                throw new InvalidOperationException("Change panel row navigation failed for row '" + rowId + "'.");
            }
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
