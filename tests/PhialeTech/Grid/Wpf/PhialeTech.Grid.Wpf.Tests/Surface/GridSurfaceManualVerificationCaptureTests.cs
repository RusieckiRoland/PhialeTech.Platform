using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Data;
using NUnit.Framework;
using PhialeGrid.Core.Columns;
using PhialeGrid.Core.Hierarchy;
using PhialeGrid.Core.Query;
using PhialeTech.Components.Shared.ViewModels;
using PhialeTech.PhialeGrid.Wpf.Controls;
using WpfGrid = PhialeTech.PhialeGrid.Wpf.Controls.PhialeGrid;

namespace PhialeGrid.Wpf.Tests.Surface
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    [Explicit("Captures live WPF screenshots for manual surface verification.")]
    [NonParallelizable]
    public sealed class GridSurfaceManualVerificationCaptureTests
    {
        private static readonly string OutputDirectory =
            Path.Combine(TestContext.CurrentContext.WorkDirectory, "artifacts", "surface-manual-verification");

        [Test]
        public void CaptureSurfaceVerificationScreens()
        {
            CaptureGroupedSurface();
            CaptureHierarchySurface();
            CaptureMultiSortSurface();
            CaptureTouchAndSummariesSurface();
            CaptureEditingSurface();

            var pngFiles = Directory.GetFiles(OutputDirectory, "*.png", SearchOption.TopDirectoryOnly);
            Assert.That(pngFiles.Length, Is.GreaterThanOrEqualTo(5));
        }

        private static void CaptureGroupedSurface()
        {
            var grid = CreateGroupedGrid();
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 760, height: 360);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var firstRow = surfaceHost.CurrentSnapshot.Rows[0];
                GridSurfaceTestHost.ClickPointViaRoutedUi(surfaceHost, 10d, firstRow.Bounds.Y + (firstRow.Bounds.Height / 2d));
                GridSurfaceTestHost.FlushDispatcher(grid);

                GridSurfaceTestHost.SaveElementScreenshot(grid, Path.Combine(OutputDirectory, "01-grouping-expanded.png"));
            }
            finally
            {
                window.Close();
            }
        }

        private static void CaptureHierarchySurface()
        {
            var grid = CreateHierarchyGrid();
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 760, height: 360);

            try
            {
                var (roots, controller) = CreatePagedHierarchySource();
                grid.ItemsSource = roots.Select(root => root.Item).ToArray();
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                grid.SetHierarchySource(roots, controller, displayColumnId: "ObjectName");
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var rootRow = surfaceHost.CurrentSnapshot.Rows[0];
                GridSurfaceTestHost.ClickPointViaRoutedUi(surfaceHost, 10d, rootRow.Bounds.Y + (rootRow.Bounds.Height / 2d));
                GridSurfaceTestHost.FlushDispatcher(grid);

                var loadMoreRow = surfaceHost.CurrentSnapshot.Rows.Single(row => row.IsLoadMore);
                GridSurfaceTestHost.ClickPointViaRoutedUi(surfaceHost, 90d, loadMoreRow.Bounds.Y + (loadMoreRow.Bounds.Height / 2d));
                GridSurfaceTestHost.FlushDispatcher(grid);

                GridSurfaceTestHost.SaveElementScreenshot(grid, Path.Combine(OutputDirectory, "02-hierarchy-load-more.png"));
            }
            finally
            {
                window.Close();
            }
        }

        private static void CaptureMultiSortSurface()
        {
            var grid = CreateThreeColumnGrid();
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 760, height: 360);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                grid.Sorts = new[]
                {
                    new GridSortDescriptor("Name", GridSortDirection.Ascending),
                    new GridSortDescriptor("City", GridSortDirection.Ascending),
                    new GridSortDescriptor("Status", GridSortDirection.Ascending),
                };
                GridSurfaceTestHost.FlushDispatcher(grid);

                GridSurfaceTestHost.SaveElementScreenshot(grid, Path.Combine(OutputDirectory, "03-multisort-ordinal.png"));
            }
            finally
            {
                window.Close();
            }
        }

        private static void CaptureTouchAndSummariesSurface()
        {
            var viewModel = new DemoShellViewModel("Wpf");
            var grid = CreateBoundDemoGrid(viewModel);
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 1200, height: 760);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                viewModel.SelectExample("summaries");
                grid.InteractionMode = GridInteractionMode.Touch;
                grid.Density = GridDensity.Touch;
                GridSurfaceTestHost.FlushDispatcher(grid);

                var municipalityColumn = grid.VisibleColumns.First(column => column.ColumnId == "Municipality");
                municipalityColumn.FilterText = "Wroclaw";
                grid.SetGroups(new[] { new GridGroupDescriptor("Category", GridSortDirection.Ascending) });
                GridSurfaceTestHost.FlushDispatcher(grid);

                GridSurfaceTestHost.SaveElementScreenshot(grid, Path.Combine(OutputDirectory, "04-touch-density-summaries.png"));
            }
            finally
            {
                window.Close();
            }
        }

        private static void CaptureEditingSurface()
        {
            var grid = CreateEditableGrid();
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 760, height: 360);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var nameCell = surfaceHost.CurrentSnapshot.Cells.Single(cell => cell.RowKey == "row-1" && cell.ColumnKey == "Name");

                GridSurfaceTestHost.DoubleClickPointViaRoutedUi(surfaceHost, nameCell.Bounds.X + 10d, nameCell.Bounds.Y + (nameCell.Bounds.Height / 2d));
                GridSurfaceTestHost.FlushDispatcher(grid);
                GridSurfaceTestHost.SendKeyViaRoutedUi(surfaceHost, "DELETE");
                GridSurfaceTestHost.SendTextViaRoutedUi(surfaceHost, "Zed");
                GridSurfaceTestHost.FlushDispatcher(grid);

                GridSurfaceTestHost.SaveElementScreenshot(grid, Path.Combine(OutputDirectory, "05-editing-live-editor.png"));
            }
            finally
            {
                window.Close();
            }
        }

        private static WpfGrid CreateThreeColumnGrid()
        {
            return new WpfGrid
            {
                Width = 720,
                Height = 320,
                IsGridReadOnly = true,
                LanguageDirectory = global::PhialeGrid.Wpf.Tests.GridTestRepositoryPaths.GridLanguagesDirectory,
                Columns = new[]
                {
                    new GridColumnDefinition("Name", "Name", width: 160, displayIndex: 0),
                    new GridColumnDefinition("City", "City", width: 160, displayIndex: 1),
                    new GridColumnDefinition("Status", "Status", width: 160, displayIndex: 2),
                },
                ItemsSource = new[]
                {
                    new SurfaceRow { Id = "row-1", Name = "Alpha", City = "Warsaw", Status = "Ready" },
                    new SurfaceRow { Id = "row-2", Name = "Beta", City = "Gdansk", Status = "Draft" },
                    new SurfaceRow { Id = "row-3", Name = "Gamma", City = "Krakow", Status = "Ready" },
                },
            };
        }

        private static WpfGrid CreateGroupedGrid()
        {
            return new WpfGrid
            {
                Width = 640,
                Height = 320,
                IsGridReadOnly = true,
                LanguageDirectory = global::PhialeGrid.Wpf.Tests.GridTestRepositoryPaths.GridLanguagesDirectory,
                Columns = new[]
                {
                    new GridColumnDefinition("Name", "Name", width: 140, displayIndex: 0),
                    new GridColumnDefinition("City", "City", width: 140, displayIndex: 1),
                },
                Groups = new[] { new GridGroupDescriptor("City") },
                ItemsSource = new[]
                {
                    new SurfaceRow { Id = "row-1", Name = "Alpha One", City = "Alpha" },
                    new SurfaceRow { Id = "row-2", Name = "Alpha Two", City = "Alpha" },
                    new SurfaceRow { Id = "row-3", Name = "Beta One", City = "Beta" },
                },
            };
        }

        private static WpfGrid CreateEditableGrid()
        {
            return new WpfGrid
            {
                Width = 640,
                Height = 320,
                IsGridReadOnly = false,
                LanguageDirectory = global::PhialeGrid.Wpf.Tests.GridTestRepositoryPaths.GridLanguagesDirectory,
                Columns = new[]
                {
                    new GridColumnDefinition("Name", "Name", width: 160, displayIndex: 0),
                    new GridColumnDefinition("City", "City", width: 160, displayIndex: 1),
                },
                ItemsSource = new[]
                {
                    new SurfaceRow { Id = "row-1", Name = "Alpha", City = "Warsaw" },
                    new SurfaceRow { Id = "row-2", Name = "Beta", City = "Gdansk" },
                },
            };
        }

        private static WpfGrid CreateHierarchyGrid()
        {
            return new WpfGrid
            {
                Width = 640,
                Height = 320,
                IsGridReadOnly = true,
                LanguageDirectory = global::PhialeGrid.Wpf.Tests.GridTestRepositoryPaths.GridLanguagesDirectory,
                Columns = new[]
                {
                    new GridColumnDefinition("ObjectName", "Object Name", width: 180, displayIndex: 0),
                    new GridColumnDefinition("Status", "Status", width: 120, displayIndex: 1),
                },
                ItemsSource = Array.Empty<object>(),
            };
        }

        private static (GridHierarchyNode<object>[] Roots, GridHierarchyController<object> Controller) CreatePagedHierarchySource()
        {
            var rootAlpha = new GridHierarchyNode<object>(
                "root-alpha",
                new System.Collections.Generic.Dictionary<string, object> { ["Id"] = "root-alpha", ["ObjectName"] = "Root Alpha", ["Status"] = "Ready" },
                canExpand: true);

            var provider = new TestPagedHierarchyProvider(new System.Collections.Generic.Dictionary<string, System.Collections.Generic.IReadOnlyList<System.Collections.Generic.IReadOnlyList<GridHierarchyNode<object>>>>(StringComparer.OrdinalIgnoreCase)
            {
                [rootAlpha.PathId] = new[]
                {
                    (System.Collections.Generic.IReadOnlyList<GridHierarchyNode<object>>)new[]
                    {
                        new GridHierarchyNode<object>("child-1", new System.Collections.Generic.Dictionary<string, object> { ["Id"] = "child-1", ["ObjectName"] = "Child A1", ["Status"] = "Ok" }, canExpand: false, parentId: rootAlpha.PathId),
                    },
                    new[]
                    {
                        new GridHierarchyNode<object>("child-2", new System.Collections.Generic.Dictionary<string, object> { ["Id"] = "child-2", ["ObjectName"] = "Child A2", ["Status"] = "Ok" }, canExpand: false, parentId: rootAlpha.PathId),
                    },
                },
            });

            return (new[] { rootAlpha }, new GridHierarchyController<object>(provider, pageSize: 1));
        }

        private static WpfGrid CreateBoundDemoGrid(DemoShellViewModel viewModel)
        {
            var grid = new WpfGrid
            {
                Width = 1200,
                Height = 720,
                DataContext = viewModel,
                LanguageDirectory = global::PhialeGrid.Wpf.Tests.GridTestRepositoryPaths.GridLanguagesDirectory,
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

        private sealed class SurfaceRow
        {
            public string Id { get; set; }

            public string Name { get; set; }

            public string City { get; set; }

            public string Status { get; set; }
        }

        private sealed class TestPagedHierarchyProvider : IGridHierarchyPagingProvider<object>
        {
            private readonly System.Collections.Generic.IReadOnlyDictionary<string, System.Collections.Generic.IReadOnlyList<System.Collections.Generic.IReadOnlyList<GridHierarchyNode<object>>>> _pagesByPath;

            public TestPagedHierarchyProvider(System.Collections.Generic.IReadOnlyDictionary<string, System.Collections.Generic.IReadOnlyList<System.Collections.Generic.IReadOnlyList<GridHierarchyNode<object>>>> pagesByPath)
            {
                _pagesByPath = pagesByPath;
            }

            public System.Threading.Tasks.Task<System.Collections.Generic.IReadOnlyList<GridHierarchyNode<object>>> LoadChildrenAsync(GridHierarchyNode<object> parent, System.Threading.CancellationToken cancellationToken)
            {
                if (parent == null || !_pagesByPath.TryGetValue(parent.PathId, out var pages) || pages.Count == 0)
                {
                    return System.Threading.Tasks.Task.FromResult((System.Collections.Generic.IReadOnlyList<GridHierarchyNode<object>>)Array.Empty<GridHierarchyNode<object>>());
                }

                return System.Threading.Tasks.Task.FromResult(pages[0]);
            }

            public System.Threading.Tasks.Task<GridHierarchyPage<object>> LoadChildrenPageAsync(GridHierarchyNode<object> parent, int offset, int size, System.Threading.CancellationToken cancellationToken)
            {
                if (parent == null || !_pagesByPath.TryGetValue(parent.PathId, out var pages))
                {
                    return System.Threading.Tasks.Task.FromResult(new GridHierarchyPage<object>(Array.Empty<GridHierarchyNode<object>>(), hasMore: false));
                }

                var pageIndex = Math.Min(offset, pages.Count - 1);
                var page = pages[pageIndex];
                return System.Threading.Tasks.Task.FromResult(new GridHierarchyPage<object>(page, hasMore: pageIndex < pages.Count - 1));
            }
        }
    }
}
