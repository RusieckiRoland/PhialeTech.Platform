using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using NUnit.Framework;
using PhialeTech.Components.Shared.ViewModels;
using PhialeTech.PhialeGrid.Wpf.Controls;
using PhialeTech.PhialeGrid.Wpf.Surface;
using PhialeGrid.Wpf.Tests.Surface;
using WpfGrid = PhialeTech.PhialeGrid.Wpf.Controls.PhialeGrid;

namespace PhialeGrid.Wpf.Tests.Advanced
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public sealed class GridAdvancedUiBehaviorTests
    {
        [Test]
        public void GlobalSearch_AppliesAcrossVisibleColumns_AndClearRestoresRows()
        {
            var viewModel = new DemoShellViewModel("Wpf");
            var grid = CreateBoundDemoGrid(viewModel);
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 1280, height: 720);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                viewModel.SelectExample("filtering");
                GridSurfaceTestHost.FlushDispatcher(grid);

                var initialCount = grid.RowsView.Cast<object>().Count();

                grid.ApplyGlobalSearch("Wroclaw");
                GridSurfaceTestHost.FlushDispatcher(grid);

                var filteredRows = grid.RowsView.Cast<GridDataRowModel>().ToArray();

                grid.ClearGlobalSearch();
                GridSurfaceTestHost.FlushDispatcher(grid);

                var restoredCount = grid.RowsView.Cast<object>().Count();

                Assert.Multiple(() =>
                {
                    Assert.That(filteredRows.Length, Is.LessThan(initialCount));
                    Assert.That(filteredRows.Length, Is.GreaterThan(0));
                    Assert.That(filteredRows.All(row => row["Municipality"].ToString().IndexOf("Wroclaw", System.StringComparison.OrdinalIgnoreCase) >= 0), Is.True);
                    Assert.That(restoredCount, Is.EqualTo(initialCount));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void PersonalizationState_RestoresSavedSearchAndColumnVisibility()
        {
            var viewModel = new DemoShellViewModel("Wpf");
            var grid = CreateBoundDemoGrid(viewModel);
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 1280, height: 720);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                viewModel.SelectExample("personalization");
                GridSurfaceTestHost.FlushDispatcher(grid);

                grid.ApplyGlobalSearch("Gdansk");
                grid.SetColumnVisibility("Owner", false);
                GridSurfaceTestHost.FlushDispatcher(grid);

                var savedState = grid.SaveState();
                var savedCount = grid.RowsView.Cast<object>().Count();

                grid.ClearGlobalSearch();
                grid.ShowAllColumns();
                GridSurfaceTestHost.FlushDispatcher(grid);

                grid.LoadState(savedState);
                GridSurfaceTestHost.FlushDispatcher(grid);

                var restoredRows = grid.RowsView.Cast<GridDataRowModel>().ToArray();
                Assert.Multiple(() =>
                {
                    Assert.That(grid.VisibleColumns.Any(column => column.ColumnId == "Owner"), Is.False);
                    Assert.That(grid.GlobalSearchText, Is.EqualTo("Gdansk"));
                    Assert.That(restoredRows.Length, Is.EqualTo(savedCount));
                    Assert.That(restoredRows.All(row => row["Municipality"].ToString().IndexOf("Gdansk", System.StringComparison.OrdinalIgnoreCase) >= 0), Is.True);
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void Theme_WhenNightModeIsEnabled_ShouldUpdateGridPalette()
        {
            var viewModel = new DemoShellViewModel("Wpf");
            var grid = CreateBoundDemoGrid(viewModel);
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 1280, height: 720);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var surfacePanel = GridSurfaceTestHost.FindDescendant<GridSurfacePanel>(surfaceHost);
                Assert.That(surfacePanel, Is.Not.Null);

                grid.IsNightMode = true;
                GridSurfaceTestHost.FlushDispatcher(grid);

                var backgroundBrush = grid.Resources["PgGridBackgroundBrush"] as SolidColorBrush;
                var primaryTextBrush = grid.Resources["PgPrimaryTextBrush"] as SolidColorBrush;

                Assert.Multiple(() =>
                {
                    Assert.That(backgroundBrush, Is.Not.Null);
                    Assert.That(primaryTextBrush, Is.Not.Null);
                    Assert.That(backgroundBrush.Color, Is.EqualTo((Color)ColorConverter.ConvertFromString("#171C25")));
                    Assert.That(primaryTextBrush.Color, Is.EqualTo((Color)ColorConverter.ConvertFromString("#F4F6FA")));
                    Assert.That(surfacePanel.Background, Is.InstanceOf<SolidColorBrush>());
                    Assert.That(((SolidColorBrush)surfacePanel.Background).Color, Is.EqualTo((Color)ColorConverter.ConvertFromString("#171C25")));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void Theme_WhenNightModeIsEnabled_ShouldStyleScrollBarsAndSystemFallbackBrushes()
        {
            var viewModel = new DemoShellViewModel("Wpf");
            viewModel.SelectExample("grouping");

            var grid = CreateBoundDemoGrid(viewModel);
            grid.Width = 760;
            grid.Height = 420;

            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 1280, height: 720);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);

                grid.IsNightMode = true;
                GridSurfaceTestHost.FlushDispatcher(grid);
                surfaceHost.UpdateLayout();
                GridSurfaceTestHost.FlushDispatcher(grid);

                var trackBrush = grid.Resources["PgScrollTrackBrush"] as SolidColorBrush;
                var fallbackBrush = grid.Resources[SystemColors.ControlLightBrushKey] as SolidColorBrush;
                var highlightBrush = grid.Resources[SystemColors.HighlightBrushKey] as SolidColorBrush;
                var scrollBars = GridSurfaceTestHost.FindVisualChildren<ScrollBar>(surfaceHost)
                    .Where(scrollBar => scrollBar.Visibility == Visibility.Visible)
                    .ToArray();

                Assert.Multiple(() =>
                {
                    Assert.That(trackBrush, Is.Not.Null);
                    Assert.That(fallbackBrush, Is.Not.Null);
                    Assert.That(highlightBrush, Is.Not.Null);
                    Assert.That(fallbackBrush.Color, Is.EqualTo((Color)ColorConverter.ConvertFromString("#232B38")));
                    Assert.That(highlightBrush.Color, Is.EqualTo((Color)ColorConverter.ConvertFromString("#274E7A")));
                    Assert.That(scrollBars.Length, Is.GreaterThanOrEqualTo(1));
                    Assert.That(scrollBars.All(scrollBar => scrollBar.Background is SolidColorBrush brush && brush.Color == trackBrush.Color), Is.True);
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void Theme_WhenDayModeIsEnabled_ShouldUseAlignedHeaderAndFallbackPalette()
        {
            var viewModel = new DemoShellViewModel("Wpf");
            var grid = CreateBoundDemoGrid(viewModel);
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 1280, height: 720);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                grid.IsNightMode = false;
                GridSurfaceTestHost.FlushDispatcher(grid);

                var headerBrush = grid.Resources["PgHeaderBackgroundBrush"] as SolidColorBrush;
                var rowStateBrush = grid.Resources["PgRowIndicatorColumnBackgroundBrush"] as SolidColorBrush;
                var controlDarkBrush = grid.Resources[SystemColors.ControlDarkDarkBrushKey] as SolidColorBrush;

                Assert.Multiple(() =>
                {
                    Assert.That(headerBrush, Is.Not.Null);
                    Assert.That(rowStateBrush, Is.Not.Null);
                    Assert.That(controlDarkBrush, Is.Not.Null);
                    Assert.That(headerBrush.Color, Is.EqualTo((Color)ColorConverter.ConvertFromString("#DDECFB")));
                    Assert.That(rowStateBrush.Color, Is.EqualTo((Color)ColorConverter.ConvertFromString("#F0F3F7")));
                    Assert.That(controlDarkBrush.Color, Is.EqualTo((Color)ColorConverter.ConvertFromString("#D3D9E1")));
                });
            }
            finally
            {
                window.Close();
            }
        }

        private static WpfGrid CreateBoundDemoGrid(DemoShellViewModel viewModel)
        {
            var grid = new WpfGrid
            {
                Width = 1280,
                Height = 720,
                DataContext = viewModel,
                LanguageDirectory = global::PhialeGrid.Wpf.Tests.GridTestRepositoryPaths.GridLanguagesDirectory
            };

            BindingOperations.SetBinding(grid, WpfGrid.ItemsSourceProperty, new Binding(nameof(DemoShellViewModel.GridRecords)));
            BindingOperations.SetBinding(grid, WpfGrid.ColumnsProperty, new Binding(nameof(DemoShellViewModel.GridColumns)));
            BindingOperations.SetBinding(grid, WpfGrid.EditSessionContextProperty, new Binding(nameof(DemoShellViewModel.GridEditSessionContext)));
            BindingOperations.SetBinding(grid, WpfGrid.GroupsProperty, new Binding(nameof(DemoShellViewModel.GridGroups)) { Mode = BindingMode.TwoWay });
            BindingOperations.SetBinding(grid, WpfGrid.SortsProperty, new Binding(nameof(DemoShellViewModel.GridSorts)) { Mode = BindingMode.TwoWay });
            BindingOperations.SetBinding(grid, WpfGrid.SummariesProperty, new Binding(nameof(DemoShellViewModel.GridSummaries)));
            BindingOperations.SetBinding(grid, WpfGrid.IsGridReadOnlyProperty, new Binding(nameof(DemoShellViewModel.IsGridReadOnly)));
            return grid;
        }

    }
}



