using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PhialeGrid.Core;

namespace PhialeTech.PhialeGrid.Wpf.Controls
{
    /// <summary>
    /// Thin content control hosted by the grid workspace panel for validation issues.
    /// </summary>
    public partial class PhialeValidationPanel : UserControl
    {
        public PhialeValidationPanel()
        {
            InitializeComponent();
        }

        private void HandleGoToCellClick(object sender, RoutedEventArgs e)
        {
            var rowId = (sender as FrameworkElement)?.DataContext as string;
            if (string.IsNullOrWhiteSpace(rowId))
            {
                throw new InvalidOperationException("Validation panel cell navigation requires a row id.");
            }

            var grid = FindAncestorGrid(sender as DependencyObject);
            if (grid == null)
            {
                throw new InvalidOperationException("Validation panel cell navigation requires a PhialeGrid ancestor.");
            }

            var columnId = grid.GetPrimaryValidationColumnId(rowId);
            if (string.IsNullOrWhiteSpace(columnId))
            {
                throw new InvalidOperationException("Validation panel cell navigation requires a validation column for row '" + rowId + "'.");
            }

            if (!grid.ScrollCellIntoView(rowId, columnId, GridScrollAlignment.Start, setCurrentCell: true))
            {
                throw new InvalidOperationException("Validation panel cell navigation failed for row '" + rowId + "' and column '" + columnId + "'.");
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
