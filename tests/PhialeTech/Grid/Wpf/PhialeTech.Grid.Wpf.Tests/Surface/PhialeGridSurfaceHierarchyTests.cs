using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using PhialeGrid.Core.Columns;
using PhialeGrid.Core.Hierarchy;
using PhialeTech.PhialeGrid.Wpf.Controls;
using WpfGrid = PhialeTech.PhialeGrid.Wpf.Controls.PhialeGrid;

namespace PhialeGrid.Wpf.Tests.Surface
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public sealed class PhialeGridSurfaceHierarchyTests
    {
        [Test]
        public void HierarchySurface_InitialSnapshotContainsOnlyRootRows()
        {
            var (roots, controller) = CreateHierarchySource();
            var grid = CreateHierarchyGrid();
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 640, height: 320);

            try
            {
                grid.ItemsSource = roots.Select(root => root.Item).ToArray();
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                grid.SetHierarchySource(roots, controller, displayColumnId: "ObjectName");
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);

                Assert.Multiple(() =>
                {
                    Assert.That(surfaceHost.CurrentSnapshot, Is.Not.Null);
                    Assert.That(surfaceHost.CurrentSnapshot.Rows.Count, Is.EqualTo(roots.Count));
                    Assert.That(surfaceHost.CurrentSnapshot.Rows.All(row => row.HierarchyLevel == 0), Is.True);
                    Assert.That(surfaceHost.CurrentSnapshot.Rows.Any(row => row.HasHierarchyChildren), Is.True);
                    Assert.That(surfaceHost.CurrentSnapshot.Cells.Any(cell => cell.DisplayText.Contains("Root Alpha")), Is.True);
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void HierarchySurface_WhenHierarchyTogglePressed_ExpandsAndCollapsesSnapshot()
        {
            var (roots, controller) = CreateHierarchySource();
            var grid = CreateHierarchyGrid();
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 640, height: 320);

            try
            {
                grid.ItemsSource = roots.Select(root => root.Item).ToArray();
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                grid.SetHierarchySource(roots, controller, displayColumnId: "ObjectName");
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var rootRow = surfaceHost.CurrentSnapshot.Rows[0];
                Assert.That(surfaceHost.CurrentSnapshot.Rows.Count, Is.EqualTo(roots.Count));

                GridSurfaceTestHost.ClickPoint(surfaceHost, x: 10, y: rootRow.Bounds.Y + (rootRow.Bounds.Height / 2d));
                GridSurfaceTestHost.FlushDispatcher(grid);

                Assert.Multiple(() =>
                {
                    Assert.That(surfaceHost.CurrentSnapshot.Rows.Count, Is.GreaterThan(roots.Count));
                    Assert.That(surfaceHost.CurrentSnapshot.Rows.Any(row => row.HierarchyLevel > 0), Is.True);
                    Assert.That(surfaceHost.CurrentSnapshot.Cells.Any(cell => cell.DisplayText.Contains("Child A1")), Is.True);
                });

                GridSurfaceTestHost.ClickPoint(surfaceHost, x: 10, y: rootRow.Bounds.Y + (rootRow.Bounds.Height / 2d));
                GridSurfaceTestHost.FlushDispatcher(grid);

                Assert.That(surfaceHost.CurrentSnapshot.Rows.Count, Is.EqualTo(roots.Count));
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void HierarchySurface_WhenHierarchyLoadMoreRowPressed_LoadsNextChildrenPage()
        {
            var (roots, controller) = CreatePagedHierarchySource();
            var grid = CreateHierarchyGrid();
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 640, height: 320);

            try
            {
                grid.ItemsSource = roots.Select(root => root.Item).ToArray();
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                grid.SetHierarchySource(roots, controller, displayColumnId: "ObjectName");
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var rootRow = surfaceHost.CurrentSnapshot.Rows[0];

                GridSurfaceTestHost.ClickPoint(surfaceHost, x: 10, y: rootRow.Bounds.Y + (rootRow.Bounds.Height / 2d));
                GridSurfaceTestHost.FlushDispatcher(grid);

                Assert.Multiple(() =>
                {
                    Assert.That(surfaceHost.CurrentSnapshot.Rows.Any(row => row.RowKey == "hierarchy-loadmore:root-alpha"), Is.True);
                    Assert.That(surfaceHost.CurrentSnapshot.Cells.Any(cell => cell.DisplayText.Contains("Child A1")), Is.True);
                    Assert.That(surfaceHost.CurrentSnapshot.Cells.Any(cell => cell.DisplayText.Contains("Child A2")), Is.False);
                });

                var loadMoreRow = surfaceHost.CurrentSnapshot.Rows.Single(row => row.IsLoadMore);
                GridSurfaceTestHost.ClickPoint(surfaceHost, x: 90, y: loadMoreRow.Bounds.Y + (loadMoreRow.Bounds.Height / 2d));
                GridSurfaceTestHost.FlushDispatcher(grid);

                Assert.Multiple(() =>
                {
                    Assert.That(surfaceHost.CurrentSnapshot.Rows.Any(row => row.RowKey == "hierarchy-loadmore:root-alpha"), Is.False);
                    Assert.That(surfaceHost.CurrentSnapshot.Cells.Any(cell => cell.DisplayText.Contains("Child A2")), Is.True);
                });
            }
            finally
            {
                window.Close();
            }
        }

        private static (IReadOnlyList<GridHierarchyNode<object>> Roots, GridHierarchyController<object> Controller) CreateHierarchySource()
        {
            var rootAlpha = new GridHierarchyNode<object>(
                "root-alpha",
                new Dictionary<string, object> { ["Id"] = "root-alpha", ["ObjectName"] = "Root Alpha", ["Status"] = "Ready" },
                canExpand: true);
            var rootBeta = new GridHierarchyNode<object>(
                "root-beta",
                new Dictionary<string, object> { ["Id"] = "root-beta", ["ObjectName"] = "Root Beta", ["Status"] = "Ready" },
                canExpand: true);

            var provider = new TestHierarchyProvider(new Dictionary<string, IReadOnlyList<GridHierarchyNode<object>>>(StringComparer.OrdinalIgnoreCase)
            {
                [rootAlpha.PathId] = new[]
                {
                    new GridHierarchyNode<object>("child-1", new Dictionary<string, object> { ["Id"] = "child-1", ["ObjectName"] = "Child A1", ["Status"] = "Ok" }, canExpand: false, parentId: rootAlpha.PathId),
                    new GridHierarchyNode<object>("child-2", new Dictionary<string, object> { ["Id"] = "child-2", ["ObjectName"] = "Child A2", ["Status"] = "Ok" }, canExpand: false, parentId: rootAlpha.PathId),
                },
                [rootBeta.PathId] = new[]
                {
                    new GridHierarchyNode<object>("child-3", new Dictionary<string, object> { ["Id"] = "child-3", ["ObjectName"] = "Child B1", ["Status"] = "Ok" }, canExpand: false, parentId: rootBeta.PathId),
                },
            });

            return (new[] { rootAlpha, rootBeta }, new GridHierarchyController<object>(provider));
        }

        private static (IReadOnlyList<GridHierarchyNode<object>> Roots, GridHierarchyController<object> Controller) CreatePagedHierarchySource()
        {
            var rootAlpha = new GridHierarchyNode<object>(
                "root-alpha",
                new Dictionary<string, object> { ["Id"] = "root-alpha", ["ObjectName"] = "Root Alpha", ["Status"] = "Ready" },
                canExpand: true);

            var provider = new TestPagedHierarchyProvider(new Dictionary<string, IReadOnlyList<IReadOnlyList<GridHierarchyNode<object>>>>(StringComparer.OrdinalIgnoreCase)
            {
                [rootAlpha.PathId] = new[]
                {
                    (IReadOnlyList<GridHierarchyNode<object>>)new[]
                    {
                        new GridHierarchyNode<object>("child-1", new Dictionary<string, object> { ["Id"] = "child-1", ["ObjectName"] = "Child A1", ["Status"] = "Ok" }, canExpand: false, parentId: rootAlpha.PathId),
                    },
                    new[]
                    {
                        new GridHierarchyNode<object>("child-2", new Dictionary<string, object> { ["Id"] = "child-2", ["ObjectName"] = "Child A2", ["Status"] = "Ok" }, canExpand: false, parentId: rootAlpha.PathId),
                    },
                },
            });

            return (new[] { rootAlpha }, new GridHierarchyController<object>(provider, pageSize: 1));
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

        private sealed class TestHierarchyProvider : IGridHierarchyProvider<object>
        {
            private readonly IReadOnlyDictionary<string, IReadOnlyList<GridHierarchyNode<object>>> _childrenByPath;

            public TestHierarchyProvider(IReadOnlyDictionary<string, IReadOnlyList<GridHierarchyNode<object>>> childrenByPath)
            {
                _childrenByPath = childrenByPath ?? throw new ArgumentNullException(nameof(childrenByPath));
            }

            public Task<IReadOnlyList<GridHierarchyNode<object>>> LoadChildrenAsync(GridHierarchyNode<object> parent, CancellationToken cancellationToken)
            {
                if (parent == null || !_childrenByPath.TryGetValue(parent.PathId, out var children))
                {
                    return Task.FromResult((IReadOnlyList<GridHierarchyNode<object>>)Array.Empty<GridHierarchyNode<object>>());
                }

                return Task.FromResult(children);
            }
        }

        private sealed class TestPagedHierarchyProvider : IGridHierarchyPagingProvider<object>
        {
            private readonly IReadOnlyDictionary<string, IReadOnlyList<IReadOnlyList<GridHierarchyNode<object>>>> _pagesByPath;

            public TestPagedHierarchyProvider(IReadOnlyDictionary<string, IReadOnlyList<IReadOnlyList<GridHierarchyNode<object>>>> pagesByPath)
            {
                _pagesByPath = pagesByPath ?? throw new ArgumentNullException(nameof(pagesByPath));
            }

            public Task<IReadOnlyList<GridHierarchyNode<object>>> LoadChildrenAsync(GridHierarchyNode<object> parent, CancellationToken cancellationToken)
            {
                if (parent == null ||
                    !_pagesByPath.TryGetValue(parent.PathId, out var pages) ||
                    pages.Count == 0)
                {
                    return Task.FromResult((IReadOnlyList<GridHierarchyNode<object>>)Array.Empty<GridHierarchyNode<object>>());
                }

                return Task.FromResult(pages[0]);
            }

            public Task<GridHierarchyPage<object>> LoadChildrenPageAsync(GridHierarchyNode<object> parent, int offset, int size, CancellationToken cancellationToken)
            {
                if (parent == null ||
                    !_pagesByPath.TryGetValue(parent.PathId, out var pages))
                {
                    return Task.FromResult(new GridHierarchyPage<object>(Array.Empty<GridHierarchyNode<object>>(), hasMore: false));
                }

                var pageIndex = Math.Min(offset, pages.Count - 1);
                var page = pages[pageIndex];
                return Task.FromResult(new GridHierarchyPage<object>(page, hasMore: pageIndex < pages.Count - 1));
            }
        }
    }
}

