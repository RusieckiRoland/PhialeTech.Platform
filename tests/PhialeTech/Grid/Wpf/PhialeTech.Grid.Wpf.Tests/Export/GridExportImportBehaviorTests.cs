using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using NUnit.Framework;
using PhialeTech.Components.Shared.Services;
using PhialeTech.Components.Shared.ViewModels;
using PhialeTech.Components.Wpf;
using PhialeTech.PhialeGrid.Wpf.Controls;
using WpfGrid = PhialeTech.PhialeGrid.Wpf.Controls.PhialeGrid;

namespace PhialeGrid.Wpf.Tests.Export
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public sealed class GridExportImportBehaviorTests
    {
        [Test]
        public void ExportCurrentViewToCsv_ShouldFollowVisibleColumnsAndFilteredRows()
        {
            var viewModel = new DemoShellViewModel("Wpf");
            var grid = CreateBoundDemoGrid(viewModel);
            var window = CreateHostWindow(grid);

            try
            {
                window.Show();
                FlushDispatcher(grid.Dispatcher);

                viewModel.SelectExample("export-import");
                FlushDispatcher(grid.Dispatcher);

                grid.ApplyGlobalSearch("Gdansk");
                grid.SetColumnVisibility("Owner", false);
                FlushDispatcher(grid.Dispatcher);

                var expectedRows = grid.RowsView.Cast<object>()
                    .OfType<GridDataRowModel>()
                    .Select(row => (PhialeTech.Components.Shared.Model.DemoGisRecordViewModel)row.SourceRow)
                    .ToArray();
                var expectedHeader = string.Join(",", grid.VisibleColumns.Select(column => column.Header));

                var csv = grid.ExportCurrentViewToCsv();
                var importedRows = DemoGisCsvTransferService.Import(csv, viewModel.GridColumns);

                Assert.Multiple(() =>
                {
                    Assert.That(expectedRows.Length, Is.GreaterThan(0));
                    Assert.That(csv, Does.StartWith(expectedHeader));
                    Assert.That(csv, Does.Not.Contain("Owner"));
                    Assert.That(importedRows.Count, Is.EqualTo(expectedRows.Length));
                    Assert.That(importedRows.All(row => string.Equals(row.Municipality, "Gdansk", StringComparison.OrdinalIgnoreCase)), Is.True);
                    Assert.That(importedRows[0].ObjectId, Is.EqualTo(expectedRows[0].ObjectId));
                    Assert.That(importedRows[0].ObjectName, Is.EqualTo(expectedRows[0].ObjectName));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void MainWindow_ExportImportAndRestoreHandlers_ShouldDriveLiveDemoState()
        {
            var window = new MainWindow();

            try
            {
                window.Show();
                FlushDispatcher(window.Dispatcher);

                var viewModel = (DemoShellViewModel)window.DataContext;
                viewModel.SelectExample("export-import");
                FlushDispatcher(window.Dispatcher);

                var grid = (WpfGrid)window.FindName("DemoGrid");
                Assert.That(grid, Is.Not.Null);

                InvokeHandler(window, "HandleExportCsvClick");
                FlushDispatcher(window.Dispatcher);

                Assert.Multiple(() =>
                {
                    Assert.That(viewModel.TransferStatusText, Does.Contain("Exported"));
                    Assert.That(viewModel.TransferPreviewText, Does.Contain("Category"));
                    Assert.That(viewModel.TransferPreviewText.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).Length, Is.GreaterThan(10));
                });

                InvokeHandler(window, "HandleImportSampleCsvClick");
                FlushDispatcher(window.Dispatcher);

                var importedRows = grid.RowsView.Cast<object>().OfType<GridDataRowModel>().ToArray();
                Assert.Multiple(() =>
                {
                    Assert.That(viewModel.TransferStatusText, Does.Contain("Imported"));
                    Assert.That(viewModel.GridRecords.Count, Is.EqualTo(12));
                    Assert.That(importedRows.Length, Is.EqualTo(12));
                    Assert.That(importedRows[0].SourceRow, Is.Not.Null);
                });

                InvokeHandler(window, "HandleRestoreSourceClick");
                FlushDispatcher(window.Dispatcher);

                var restoredRows = grid.RowsView.Cast<object>().OfType<GridDataRowModel>().ToArray();

                Assert.Multiple(() =>
                {
                    Assert.That(viewModel.TransferStatusText, Does.Contain("Restored"));
                    Assert.That(viewModel.GridRecords.Count, Is.EqualTo(530));
                    Assert.That(restoredRows.Length, Is.GreaterThan(0));
                    Assert.That(((PhialeTech.Components.Shared.Model.DemoGisRecordViewModel)restoredRows[0].SourceRow).ObjectId, Is.EqualTo(viewModel.GridRecords[0].ObjectId));
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

        private static Window CreateHostWindow(WpfGrid grid)
        {
            return new Window
            {
                Width = 1280,
                Height = 720,
                Content = grid,
                ShowInTaskbar = false,
                WindowStyle = WindowStyle.None
            };
        }

        private static void InvokeHandler(Window window, string handlerName)
        {
            var method = window.GetType().GetMethod(handlerName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, handlerName + " was not found.");
            method.Invoke(window, new object[] { window, new RoutedEventArgs(Button.ClickEvent) });
        }

        private static void FlushDispatcher(Dispatcher dispatcher)
        {
            dispatcher.Invoke(() => { }, DispatcherPriority.Background);
            dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        }
    }
}



