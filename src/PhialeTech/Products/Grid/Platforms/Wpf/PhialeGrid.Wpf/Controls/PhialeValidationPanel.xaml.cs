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

        private void HandleValidationPanelLoaded(object sender, RoutedEventArgs e)
        {
            var grid = FindAncestorGrid(this);
            if (grid == null)
            {
                throw new InvalidOperationException("Validation panel requires a PhialeGrid ancestor.");
            }

            DataContext = grid;
        }

        private void HandleGoToCellClick(object sender, RoutedEventArgs e)
        {
            var issue = (sender as FrameworkElement)?.DataContext as PhialeGrid.ValidationIssuePanelItem;
            if (issue == null)
            {
                throw new InvalidOperationException("Validation panel cell navigation requires a validation issue item.");
            }

            var grid = FindAncestorGrid(sender as DependencyObject);
            if (grid == null)
            {
                throw new InvalidOperationException("Validation panel cell navigation requires a PhialeGrid ancestor.");
            }

            if (string.IsNullOrWhiteSpace(issue.RowId) || string.IsNullOrWhiteSpace(issue.ColumnId))
            {
                throw new InvalidOperationException("Validation panel cell navigation requires a row id and column id.");
            }

            if (!grid.ScrollCellIntoView(issue.RowId, issue.ColumnId, GridScrollAlignment.Start, setCurrentCell: true))
            {
                throw new InvalidOperationException("Validation panel cell navigation failed for row '" + issue.RowId + "' and column '" + issue.ColumnId + "'.");
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
