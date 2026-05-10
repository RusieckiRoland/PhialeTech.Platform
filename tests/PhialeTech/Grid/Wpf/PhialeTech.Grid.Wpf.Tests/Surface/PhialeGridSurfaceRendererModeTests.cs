using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using PhialeGrid.Core;
using PhialeGrid.Core.Columns;
using PhialeGrid.Core.Data;
using PhialeGrid.Core.Editing;
using PhialeGrid.Core.Query;
using PhialeGrid.Core.Regions;
using PhialeGrid.Core.Surface;
using PhialeGrid.Core.Validation;
using PhialeTech.PhialeGrid.Wpf.Controls;
using PhialeTech.PhialeGrid.Wpf.Diagnostics;
using PhialeTech.PhialeGrid.Wpf.Surface.Presenters;
using PhialeTech.PhialeGrid.Wpf.Surface;
using UniversalInput.Contracts;
using WpfGrid = PhialeTech.PhialeGrid.Wpf.Controls.PhialeGrid;

namespace PhialeGrid.Wpf.Tests.Surface
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public sealed class PhialeGridSurfaceRendererModeTests
    {
        [Test]
        public void SurfaceRuntime_BuildsSnapshotByDefault()
        {
            var grid = CreateGrid();
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 640, height: 320);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);

                Assert.Multiple(() =>
                {
                    Assert.That(surfaceHost.Visibility, Is.EqualTo(System.Windows.Visibility.Visible));
                    Assert.That(surfaceHost.CurrentSnapshot, Is.Not.Null);
                    Assert.That(surfaceHost.CurrentSnapshot.Cells.Count, Is.GreaterThan(0));
                    Assert.That(surfaceHost.CurrentSnapshot.Columns.Select(column => column.ColumnKey), Is.EqualTo(new[] { "Name", "City" }));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void SurfaceRuntime_WhenGroupedHeaderPressed_CollapsesAndExpandsSnapshot()
        {
            var grid = CreateGroupedGrid();
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 640, height: 320);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var firstRow = surfaceHost.CurrentSnapshot.Rows[0];
                Assert.That(surfaceHost.CurrentSnapshot.Rows.Count, Is.EqualTo(2));

                GridSurfaceTestHost.ClickPoint(surfaceHost, x: 10, y: firstRow.Bounds.Y + (firstRow.Bounds.Height / 2d));
                GridSurfaceTestHost.FlushDispatcher(grid);
                Assert.That(surfaceHost.CurrentSnapshot.Rows.Count, Is.EqualTo(4));

                GridSurfaceTestHost.ClickPoint(surfaceHost, x: 10, y: firstRow.Bounds.Y + (firstRow.Bounds.Height / 2d));
                GridSurfaceTestHost.FlushDispatcher(grid);
                Assert.That(surfaceHost.CurrentSnapshot.Rows.Count, Is.EqualTo(2));
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void SurfaceRuntime_WhenGroupedCurrentCellBecomesHiddenAfterCollapse_ClearsCurrentCell()
        {
            var grid = CreateGroupedGrid();
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 640, height: 320);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var firstGroupRow = surfaceHost.CurrentSnapshot.Rows[0];

                GridSurfaceTestHost.ClickPoint(surfaceHost, x: 10, y: firstGroupRow.Bounds.Y + (firstGroupRow.Bounds.Height / 2d));
                GridSurfaceTestHost.FlushDispatcher(grid);

                var alphaNameCell = surfaceHost.CurrentSnapshot.Cells.Single(cell => cell.RowKey == "row-1" && cell.ColumnKey == "Name");
                GridSurfaceTestHost.ClickBoundsCenter(surfaceHost, alphaNameCell.Bounds);
                GridSurfaceTestHost.FlushDispatcher(grid);
                Assert.That(surfaceHost.CurrentSnapshot.CurrentCell?.RowKey, Is.EqualTo("row-1"));

                GridSurfaceTestHost.ClickPoint(surfaceHost, x: 10, y: firstGroupRow.Bounds.Y + (firstGroupRow.Bounds.Height / 2d));
                GridSurfaceTestHost.FlushDispatcher(grid);

                Assert.That(surfaceHost.CurrentSnapshot.CurrentCell, Is.Null);
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void SurfaceRuntime_WhenGroupedRowCellPressed_TogglesGroupExpansion()
        {
            var grid = CreateGroupedGrid();
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 640, height: 320);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var firstRow = surfaceHost.CurrentSnapshot.Rows[0];
                var groupCell = surfaceHost.CurrentSnapshot.Cells.Single(cell => cell.RowKey == firstRow.RowKey && cell.ColumnKey == "Name");

                GridSurfaceTestHost.ClickBoundsCenter(surfaceHost, groupCell.Bounds);
                GridSurfaceTestHost.FlushDispatcher(grid);

                var updatedSnapshot = surfaceHost.CurrentSnapshot;
                Assert.Multiple(() =>
                {
                    Assert.That(updatedSnapshot.Rows.Count, Is.EqualTo(4));
                    Assert.That(updatedSnapshot.CurrentCell, Is.Null);
                    Assert.That(updatedSnapshot.Overlays.Any(overlay => overlay.Kind == GridOverlayKind.CurrentRecord && overlay.TargetKey == firstRow.RowKey), Is.False);
                    Assert.That(grid.HasSelectedRows, Is.False);
                    Assert.That(grid.SelectionStatusText, Does.Contain("Selected rows: 0"));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void SurfaceRuntime_WhenPointerPressed_UpdatesCurrentCellAndStatusText()
        {
            var grid = CreateGrid();
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 640, height: 320);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var nameCell = surfaceHost.CurrentSnapshot.Cells.Single(cell => cell.RowKey == "row-1" && cell.ColumnKey == "Name");

                GridSurfaceTestHost.ClickBoundsCenter(surfaceHost, nameCell.Bounds);
                GridSurfaceTestHost.FlushDispatcher(grid);

                Assert.Multiple(() =>
                {
                    Assert.That(surfaceHost.CurrentSnapshot.CurrentCell, Is.Not.Null);
                    Assert.That(surfaceHost.CurrentSnapshot.CurrentCell.RowKey, Is.EqualTo("row-1"));
                    Assert.That(surfaceHost.CurrentSnapshot.CurrentCell.ColumnKey, Is.EqualTo("Name"));
                    Assert.That(surfaceHost.CurrentSnapshot.Rows.Single(row => row.RowKey == "row-1").IsSelected, Is.False);
                    Assert.That(surfaceHost.CurrentSnapshot.Headers.Single(header => header.Kind == GridHeaderKind.RowHeader && header.HeaderKey == "row-1").IsSelected, Is.False);
                    Assert.That(surfaceHost.CurrentSnapshot.Overlays.Any(overlay => overlay.Kind == GridOverlayKind.CurrentRecord && overlay.TargetKey == "row-1"), Is.True);
                    Assert.That(grid.CurrentCellText, Does.Contain("Name"));
                    Assert.That(grid.CurrentCellText, Does.Contain("Alpha"));
                    Assert.That(grid.SelectionStatusText, Does.Contain("1"));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void SurfaceRuntime_WhenGlobalSearchChanges_RefreshesSnapshotRows()
        {
            var grid = CreateGrid();
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 640, height: 320);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var initialRows = surfaceHost.CurrentSnapshot.Rows.Count;

                grid.ApplyGlobalSearch("Gdansk");
                GridSurfaceTestHost.FlushDispatcher(grid);

                Assert.Multiple(() =>
                {
                    Assert.That(surfaceHost.CurrentSnapshot.Rows.Count, Is.LessThan(initialRows));
                    Assert.That(surfaceHost.CurrentSnapshot.Rows.Single().RowKey, Is.EqualTo("row-2"));
                    Assert.That(surfaceHost.CurrentSnapshot.Cells.All(cell => !string.Equals(cell.RowKey, "row-1", StringComparison.OrdinalIgnoreCase)), Is.True);
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void SurfaceRuntime_WhenScrollCellIntoViewIsRequested_ScrollsToTargetAndKeepsCurrentCell()
        {
            var grid = CreateScrollableGrid();
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 320, height: 160);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                var result = grid.ScrollCellIntoView("row-5", "Scale", GridScrollAlignment.Nearest, setCurrentCell: true);
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);

                Assert.Multiple(() =>
                {
                    Assert.That(result, Is.True);
                    Assert.That(surfaceHost.CurrentSnapshot.ViewportState.HorizontalOffset, Is.GreaterThan(0d));
                    Assert.That(surfaceHost.CurrentSnapshot.ViewportState.VerticalOffset, Is.GreaterThan(0d));
                    Assert.That(surfaceHost.CurrentSnapshot.CurrentCell, Is.Not.Null);
                    Assert.That(surfaceHost.CurrentSnapshot.CurrentCell.RowKey, Is.EqualTo("row-5"));
                    Assert.That(surfaceHost.CurrentSnapshot.CurrentCell.ColumnKey, Is.EqualTo("Scale"));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void SurfaceRuntime_WhenScrollCellIntoViewTargetsFilteredOutRow_ReturnsFalse()
        {
            var grid = CreateScrollableGrid();
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 320, height: 160);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                grid.ApplyGlobalSearch("Gdansk");
                GridSurfaceTestHost.FlushDispatcher(grid);

                var result = grid.ScrollCellIntoView("row-1", "Scale");
                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);

                Assert.Multiple(() =>
                {
                    Assert.That(result, Is.False);
                    Assert.That(surfaceHost.CurrentSnapshot.Rows.All(row => !string.Equals(row.RowKey, "row-1", StringComparison.OrdinalIgnoreCase)), Is.True);
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void SurfaceRuntime_WhenGroupedHiddenInvalidRowExists_AnchorsMarkerToVisibleGroupHeader()
        {
            var grid = CreateGroupedScrollableGrid();
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 320, height: 180);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                Assert.That(grid.SetRowValueForDemo("row-5", "Owner", string.Empty), Is.True);
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var marker = surfaceHost.CurrentSnapshot.ViewportState.VerticalTrackMarkers
                    .FirstOrDefault(candidate => candidate.Kind == GridViewportTrackMarkerKind.ValidationError);

                Assert.Multiple(() =>
                {
                    Assert.That(marker, Is.Not.Null);
                    Assert.That(marker.TargetKey, Does.StartWith("group:"));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void SurfaceRuntime_WhenGridMutationsAreBatched_PerformsSingleRowsRefresh()
        {
            var grid = CreateGroupedScrollableGrid();
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 320, height: 180);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                PhialeGridDiagnostics.BeginGridSession(grid.DiagnosticsGridIdForTests, "batched grid mutation test", true);

                using (grid.BeginGridUpdateBatch("component-grid-mutation"))
                {
                    Assert.That(grid.SetRowValueForDemo("row-5", "Owner", string.Empty), Is.True);
                    Assert.That(grid.SetRowValueForDemo("row-4", "Name", "Zeta edited"), Is.True);
                    Assert.That(grid.FocusRow("row-5", "Owner"), Is.True);
                }

                GridSurfaceTestHost.FlushDispatcher(grid);
                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var refreshRowsCount = PhialeGridDiagnostics.GetGridCounter(grid.DiagnosticsGridIdForTests, "RefreshRowsViewExecuted");
                var snapshotCount = PhialeGridDiagnostics.GetGridCounter(grid.DiagnosticsGridIdForTests, "SyncSurfaceRendererSnapshot");
                var indicatorCount = PhialeGridDiagnostics.GetGridCounter(grid.DiagnosticsGridIdForTests, "RefreshSurfaceRowIndicatorsExecuted");

                Assert.Multiple(() =>
                {
                    Assert.That(refreshRowsCount, Is.EqualTo(1));
                    Assert.That(snapshotCount, Is.EqualTo(1));
                    Assert.That(indicatorCount, Is.EqualTo(0));
                    Assert.That(surfaceHost.CurrentSnapshot.CurrentCell, Is.Not.Null);
                    Assert.That(surfaceHost.CurrentSnapshot.CurrentCell.RowKey, Is.EqualTo("row-5"));
                    Assert.That(surfaceHost.CurrentSnapshot.CurrentCell.ColumnKey, Is.EqualTo("Owner"));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void SurfaceRuntime_WhenHostModelChangesAreBatched_PerformsSingleRowsRefresh()
        {
            var grid = CreateScrollableGrid();
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 320, height: 180);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                PhialeGridDiagnostics.BeginGridSession(grid.DiagnosticsGridIdForTests, "batched host model update test", true);

                using (grid.BeginGridUpdateBatch("host-model-update"))
                {
                    grid.Columns = new[]
                    {
                        new GridColumnDefinition("Name", "Name", width: 140, displayIndex: 0),
                        new GridColumnDefinition("City", "City", width: 140, displayIndex: 1),
                        new GridColumnDefinition("Owner", "Owner", width: 160, displayIndex: 2),
                    };
                    grid.ItemsSource = new[]
                    {
                        new SurfaceRow { Id = "row-a", Name = "Atlas", City = "Warsaw", Status = "Ready", Owner = "A", Scale = "100" },
                        new SurfaceRow { Id = "row-b", Name = "Beryl", City = "Warsaw", Status = "Draft", Owner = "B", Scale = "200" },
                        new SurfaceRow { Id = "row-c", Name = "Cobalt", City = "Gdansk", Status = "Ready", Owner = "C", Scale = "300" },
                    };
                    grid.Groups = new[] { new GridGroupDescriptor("City") };
                    grid.Sorts = new[] { new GridSortDescriptor("Name", GridSortDirection.Ascending) };
                }

                GridSurfaceTestHost.FlushDispatcher(grid);
                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var refreshRowsCount = PhialeGridDiagnostics.GetGridCounter(grid.DiagnosticsGridIdForTests, "RefreshRowsViewExecuted");
                var snapshotCount = PhialeGridDiagnostics.GetGridCounter(grid.DiagnosticsGridIdForTests, "SyncSurfaceRendererSnapshot");
                var groupedQueryCount = PhialeGridDiagnostics.GetGridCounter(grid.DiagnosticsGridIdForTests, "BuildGroupedSurfaceResult");

                var groupRows = surfaceHost.CurrentSnapshot.Rows
                    .Where(row => row.RowKey.StartsWith("group:", StringComparison.OrdinalIgnoreCase))
                    .ToArray();

                Assert.Multiple(() =>
                {
                    Assert.That(refreshRowsCount, Is.EqualTo(1));
                    Assert.That(snapshotCount, Is.EqualTo(1));
                    Assert.That(groupedQueryCount, Is.EqualTo(1));
                    Assert.That(surfaceHost.CurrentSnapshot.Columns.Select(column => column.ColumnKey), Is.EquivalentTo(new[] { "Name", "City", "Owner" }));
                    Assert.That(groupRows, Has.Length.EqualTo(2));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void SurfaceRuntime_WhenExternalEditSessionRaisesEquivalentStructuralState_DoesNotRebuildRows()
        {
            var columns = new[]
            {
                new GridColumnDefinition("Name", "Name", width: 140, displayIndex: 0),
                new GridColumnDefinition("City", "City", width: 140, displayIndex: 1),
                new GridColumnDefinition("Owner", "Owner", width: 160, displayIndex: 2),
            };
            var rows = new object[]
            {
                new SurfaceRow { Id = "row-a", Name = "Atlas", City = "Warsaw", Status = "Ready", Owner = "A", Scale = "100" },
                new SurfaceRow { Id = "row-b", Name = "Beryl", City = "Gdansk", Status = "Draft", Owner = "B", Scale = "200" },
            };
            var fields = ObjectEditSessionFieldDefinitionFactory.CreateFromGridColumns(columns);
            var dataSource = new InMemoryEditSessionDataSource<object>(rows, fields);
            var editContext = new EditSessionContext<object>(dataSource, row => ((SurfaceRow)row).Id);
            var grid = new WpfGrid
            {
                Width = 320,
                Height = 160,
                IsGridReadOnly = false,
                LanguageDirectory = global::PhialeGrid.Wpf.Tests.GridTestRepositoryPaths.GridLanguagesDirectory,
                EditSessionContext = editContext,
            };
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 320, height: 180);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                PhialeGridDiagnostics.BeginGridSession(grid.DiagnosticsGridIdForTests, "equivalent external edit context state test", true);

                using (grid.BeginGridUpdateBatch("equivalent-external-edit-context"))
                {
                    dataSource.ReplaceData(rows, fields);
                }

                GridSurfaceTestHost.FlushDispatcher(grid);

                var refreshRowsCount = PhialeGridDiagnostics.GetGridCounter(grid.DiagnosticsGridIdForTests, "RefreshRowsViewExecuted");
                var snapshotCount = PhialeGridDiagnostics.GetGridCounter(grid.DiagnosticsGridIdForTests, "SyncSurfaceRendererSnapshot");

                Assert.Multiple(() =>
                {
                    Assert.That(refreshRowsCount, Is.EqualTo(0));
                    Assert.That(snapshotCount, Is.EqualTo(0));
                });
            }
            finally
            {
                window.Close();
                editContext.Dispose();
            }
        }

        [Test]
        public void SurfaceRuntime_WhenExternalEditSessionReplacesRecordsWithSameRowIds_DoesNotRebuildRows()
        {
            var columns = new[]
            {
                new GridColumnDefinition("Name", "Name", width: 140, displayIndex: 0),
                new GridColumnDefinition("City", "City", width: 140, displayIndex: 1),
                new GridColumnDefinition("Owner", "Owner", width: 160, displayIndex: 2),
            };
            var rows = new object[]
            {
                new SurfaceRow { Id = "row-a", Name = "Atlas", City = "Warsaw", Status = "Ready", Owner = "A", Scale = "100" },
                new SurfaceRow { Id = "row-b", Name = "Beryl", City = "Gdansk", Status = "Draft", Owner = "B", Scale = "200" },
            };
            var replacementRows = new object[]
            {
                new SurfaceRow { Id = "row-a", Name = "Atlas", City = "Warsaw", Status = "Ready", Owner = "A", Scale = "100" },
                new SurfaceRow { Id = "row-b", Name = "Beryl", City = "Gdansk", Status = "Draft", Owner = "B", Scale = "200" },
            };
            var fields = ObjectEditSessionFieldDefinitionFactory.CreateFromGridColumns(columns);
            var dataSource = new InMemoryEditSessionDataSource<object>(rows, fields);
            var editContext = new EditSessionContext<object>(dataSource, row => ((SurfaceRow)row).Id);
            var grid = new WpfGrid
            {
                Width = 320,
                Height = 160,
                IsGridReadOnly = false,
                LanguageDirectory = global::PhialeGrid.Wpf.Tests.GridTestRepositoryPaths.GridLanguagesDirectory,
                EditSessionContext = editContext,
            };
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 320, height: 180);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                PhialeGridDiagnostics.BeginGridSession(grid.DiagnosticsGridIdForTests, "same row ids external edit context state test", true);

                using (grid.BeginGridUpdateBatch("same-row-ids-external-edit-context"))
                {
                    dataSource.ReplaceData(replacementRows, fields);
                }

                GridSurfaceTestHost.FlushDispatcher(grid);

                var refreshRowsCount = PhialeGridDiagnostics.GetGridCounter(grid.DiagnosticsGridIdForTests, "RefreshRowsViewExecuted");
                var snapshotCount = PhialeGridDiagnostics.GetGridCounter(grid.DiagnosticsGridIdForTests, "SyncSurfaceRendererSnapshot");

                Assert.Multiple(() =>
                {
                    Assert.That(refreshRowsCount, Is.EqualTo(0));
                    Assert.That(snapshotCount, Is.EqualTo(0));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void SurfaceRuntime_WhenExternalEditSessionChangesColumnPresentationOnly_DoesNotRebuildRows()
        {
            var columns = new[]
            {
                new GridColumnDefinition("Name", "Name", width: 140, displayIndex: 0),
                new GridColumnDefinition("City", "City", width: 140, displayIndex: 1),
                new GridColumnDefinition("Owner", "Owner", width: 160, displayIndex: 2),
            };
            var presentationColumns = new[]
            {
                new GridColumnDefinition("Name", "Name", width: 180, displayIndex: 2),
                new GridColumnDefinition("City", "City", width: 120, isVisible: false, displayIndex: 0),
                new GridColumnDefinition("Owner", "Owner", width: 220, displayIndex: 1),
            };
            var rows = new object[]
            {
                new SurfaceRow { Id = "row-a", Name = "Atlas", City = "Warsaw", Status = "Ready", Owner = "A", Scale = "100" },
                new SurfaceRow { Id = "row-b", Name = "Beryl", City = "Gdansk", Status = "Draft", Owner = "B", Scale = "200" },
            };
            var fields = ObjectEditSessionFieldDefinitionFactory.CreateFromGridColumns(columns);
            var presentationFields = ObjectEditSessionFieldDefinitionFactory.CreateFromGridColumns(presentationColumns);
            var dataSource = new InMemoryEditSessionDataSource<object>(rows, fields);
            var editContext = new EditSessionContext<object>(dataSource, row => ((SurfaceRow)row).Id);
            var grid = new WpfGrid
            {
                Width = 320,
                Height = 160,
                IsGridReadOnly = false,
                LanguageDirectory = global::PhialeGrid.Wpf.Tests.GridTestRepositoryPaths.GridLanguagesDirectory,
                EditSessionContext = editContext,
            };
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 320, height: 180);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                PhialeGridDiagnostics.BeginGridSession(grid.DiagnosticsGridIdForTests, "presentation-only external edit context state test", true);

                using (grid.BeginGridUpdateBatch("presentation-only-external-edit-context"))
                {
                    dataSource.ReplaceData(rows, presentationFields);
                }

                GridSurfaceTestHost.FlushDispatcher(grid);

                var refreshRowsCount = PhialeGridDiagnostics.GetGridCounter(grid.DiagnosticsGridIdForTests, "RefreshRowsViewExecuted");
                var snapshotCount = PhialeGridDiagnostics.GetGridCounter(grid.DiagnosticsGridIdForTests, "SyncSurfaceRendererSnapshot");

                Assert.Multiple(() =>
                {
                    Assert.That(refreshRowsCount, Is.EqualTo(0));
                    Assert.That(snapshotCount, Is.EqualTo(0));
                });
            }
            finally
            {
                window.Close();
                editContext.Dispose();
            }
        }

        [Test]
        public void SurfaceRuntime_WhenHiddenGridBecomesVisibleBeforeHostBatch_DoesNotFlushIntermediateState()
        {
            var grid = CreateScrollableGrid();
            grid.Visibility = System.Windows.Visibility.Collapsed;
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 320, height: 180);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                PhialeGridDiagnostics.BeginGridSession(grid.DiagnosticsGridIdForTests, "visible host batch test", true);

                grid.Visibility = System.Windows.Visibility.Visible;
                using (grid.BeginGridUpdateBatch("visible-host-batch"))
                {
                    grid.Columns = new[]
                    {
                        new GridColumnDefinition("Name", "Name", width: 140, displayIndex: 0),
                        new GridColumnDefinition("City", "City", width: 140, displayIndex: 1),
                    };
                    grid.ItemsSource = new[]
                    {
                        new SurfaceRow { Id = "row-a", Name = "Atlas", City = "Warsaw", Status = "Ready", Owner = "A", Scale = "100" },
                        new SurfaceRow { Id = "row-b", Name = "Beryl", City = "Gdansk", Status = "Draft", Owner = "B", Scale = "200" },
                    };
                }

                GridSurfaceTestHost.FlushDispatcher(grid);
                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var refreshRowsCount = PhialeGridDiagnostics.GetGridCounter(grid.DiagnosticsGridIdForTests, "RefreshRowsViewExecuted");
                var snapshotCount = PhialeGridDiagnostics.GetGridCounter(grid.DiagnosticsGridIdForTests, "SyncSurfaceRendererSnapshot");

                Assert.Multiple(() =>
                {
                    Assert.That(refreshRowsCount, Is.EqualTo(1));
                    Assert.That(snapshotCount, Is.EqualTo(1));
                    Assert.That(surfaceHost.CurrentSnapshot.Columns.Select(column => column.ColumnKey), Is.EqualTo(new[] { "Name", "City" }));
                    Assert.That(surfaceHost.CurrentSnapshot.Rows.Select(row => row.RowKey), Is.EqualTo(new[] { "row-a", "row-b" }));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void SurfaceRuntime_WhenRegionVisibilityChangesAreBatched_AppliesRegionLayoutOnce()
        {
            var grid = CreateGrid();
            grid.SideToolContent = new System.Windows.Controls.Border
            {
                Width = 160,
                Height = 120,
            };
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 640, height: 320);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                PhialeGridDiagnostics.BeginGridSession(grid.DiagnosticsGridIdForTests, "batched region layout test", true);
                var viewStateChangedCount = 0;
                grid.ViewStateChanged += (_, __) => viewStateChangedCount++;

                using (grid.BeginGridUpdateBatch("region-visibility-update"))
                {
                    grid.SetRegionVisibility(GridRegionKind.SideToolRegion, true);
                    grid.SetRegionVisibility(GridRegionKind.GroupingRegion, false);
                    grid.SetRegionVisibility(GridRegionKind.SideToolRegion, true);
                    grid.SetRegionVisibility(GridRegionKind.GroupingRegion, false);

                    Assert.That(
                        PhialeGridDiagnostics.GetGridCounter(grid.DiagnosticsGridIdForTests, "ApplyRegionLayoutExecuted"),
                        Is.EqualTo(0));
                    Assert.That(viewStateChangedCount, Is.EqualTo(0));
                }

                GridSurfaceTestHost.FlushDispatcher(grid);
                var applyCount = PhialeGridDiagnostics.GetGridCounter(grid.DiagnosticsGridIdForTests, "ApplyRegionLayoutExecuted");
                var exported = grid.ExportViewState();
                var sideToolState = exported.RegionLayout.Single(region => region.RegionKind == GridRegionKind.SideToolRegion);
                var groupingState = exported.RegionLayout.Single(region => region.RegionKind == GridRegionKind.GroupingRegion);

                Assert.Multiple(() =>
                {
                    Assert.That(applyCount, Is.EqualTo(1));
                    Assert.That(viewStateChangedCount, Is.EqualTo(1));
                    Assert.That(sideToolState.State, Is.EqualTo(GridRegionState.Open));
                    Assert.That(groupingState.State, Is.EqualTo(GridRegionState.Closed));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void SurfaceRuntime_WhenEquivalentViewStateIsApplied_SkipsRowsAndRegionRefresh()
        {
            var grid = CreateGroupedScrollableGrid();
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 480, height: 240);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                var state = grid.ExportViewState();
                PhialeGridDiagnostics.BeginGridSession(grid.DiagnosticsGridIdForTests, "equivalent view state test", true);

                grid.ApplyViewState(state);
                GridSurfaceTestHost.FlushDispatcher(grid);

                var refreshRowsCount = PhialeGridDiagnostics.GetGridCounter(grid.DiagnosticsGridIdForTests, "RefreshRowsViewExecuted");
                var snapshotCount = PhialeGridDiagnostics.GetGridCounter(grid.DiagnosticsGridIdForTests, "SyncSurfaceRendererSnapshot");
                var regionLayoutCount = PhialeGridDiagnostics.GetGridCounter(grid.DiagnosticsGridIdForTests, "ApplyRegionLayoutExecuted");

                Assert.Multiple(() =>
                {
                    Assert.That(refreshRowsCount, Is.EqualTo(0));
                    Assert.That(snapshotCount, Is.EqualTo(0));
                    Assert.That(regionLayoutCount, Is.EqualTo(0));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void SurfaceRuntime_WhenGroupedCollapsedScrollCellTargetsHiddenRow_ExpandsOwningGroupAndRevealsCell()
        {
            var grid = CreateGroupedScrollableGrid();
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 320, height: 180);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                var result = grid.ScrollCellIntoView("row-5", "Owner", GridScrollAlignment.Start, setCurrentCell: true);
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);

                Assert.Multiple(() =>
                {
                    Assert.That(result, Is.True);
                    Assert.That(surfaceHost.CurrentSnapshot.Rows.Any(row => row.RowKey == "row-5"), Is.True);
                    Assert.That(surfaceHost.CurrentSnapshot.CurrentCell, Is.Not.Null);
                    Assert.That(surfaceHost.CurrentSnapshot.CurrentCell.RowKey, Is.EqualTo("row-5"));
                    Assert.That(surfaceHost.CurrentSnapshot.CurrentCell.ColumnKey, Is.EqualTo("Owner"));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void SurfaceRuntime_WhenColumnVisibilityChanges_RefreshesSnapshotColumns()
        {
            var grid = CreateGrid();
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 640, height: 320);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                Assert.That(surfaceHost.CurrentSnapshot.Columns.Any(column => column.ColumnKey == "City"), Is.True);

                grid.SetColumnVisibility("City", false);
                GridSurfaceTestHost.FlushDispatcher(grid);

                Assert.Multiple(() =>
                {
                    Assert.That(surfaceHost.CurrentSnapshot.Columns.Any(column => column.ColumnKey == "City"), Is.False);
                    Assert.That(surfaceHost.CurrentSnapshot.Cells.Any(cell => cell.ColumnKey == "City"), Is.False);
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void SurfaceRuntime_WhenColumnFilterChanges_RefreshesSnapshotRowsUsingFilterPipeline()
        {
            var grid = CreateGrid();
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 640, height: 320);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                grid.VisibleColumns.First(column => column.ColumnId == "City").FilterText = "Gdansk";
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                Assert.Multiple(() =>
                {
                    Assert.That(surfaceHost.CurrentSnapshot.Rows.Count, Is.EqualTo(1));
                    Assert.That(surfaceHost.CurrentSnapshot.Rows[0].RowKey, Is.EqualTo("row-2"));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void SurfaceRuntime_WhenTouchInteractionModeAndCellPressed_SelectsTouchedCellAndKeepsRowSelectionIndependent()
        {
            var grid = CreateGrid();
            grid.InteractionMode = GridInteractionMode.Touch;
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 640, height: 320);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var nameCell = surfaceHost.CurrentSnapshot.Cells.Single(cell => cell.RowKey == "row-1" && cell.ColumnKey == "Name");
                GridSurfaceTestHost.ClickBoundsCenter(surfaceHost, nameCell.Bounds);
                GridSurfaceTestHost.FlushDispatcher(grid);

                var updatedSnapshot = surfaceHost.CurrentSnapshot;

                Assert.Multiple(() =>
                {
                    Assert.That(updatedSnapshot.CurrentCell, Is.Not.Null);
                    Assert.That(updatedSnapshot.CurrentCell.RowKey, Is.EqualTo("row-1"));
                    Assert.That(updatedSnapshot.CurrentCell.ColumnKey, Is.EqualTo("Name"));
                    Assert.That(updatedSnapshot.Rows.Single(row => row.RowKey == "row-1").IsSelected, Is.False);
                    Assert.That(updatedSnapshot.Cells.Single(cell => cell.RowKey == "row-1" && cell.ColumnKey == "Name").IsSelected, Is.True);
                    Assert.That(updatedSnapshot.Cells.Where(cell => cell.RowKey == "row-1" && cell.ColumnKey != "Name").All(cell => !cell.IsSelected), Is.True);
                    Assert.That(updatedSnapshot.Headers.Single(header => header.Kind == GridHeaderKind.RowHeader && header.HeaderKey == "row-1").IsSelected, Is.False);
                    Assert.That(grid.SelectionStatusText, Does.Contain("1"));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void SurfaceRuntime_WhenSelectVisibleRowsAndClearSelection_KeepsSnapshotInSync()
        {
            var grid = CreateGrid();
            grid.InteractionMode = GridInteractionMode.Touch;
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 640, height: 320);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);

                grid.SelectVisibleRows();
                GridSurfaceTestHost.FlushDispatcher(grid);

                Assert.Multiple(() =>
                {
                    Assert.That(surfaceHost.CurrentSnapshot.Rows.All(row => !row.IsSelected), Is.True);
                    Assert.That(surfaceHost.CurrentSnapshot.Cells.All(cell => cell.IsSelected), Is.True);
                    Assert.That(grid.HasSelectedRows, Is.True);
                });

                grid.ClearSelection();
                GridSurfaceTestHost.FlushDispatcher(grid);

                Assert.Multiple(() =>
                {
                    Assert.That(surfaceHost.CurrentSnapshot.Rows.All(row => !row.IsSelected), Is.True);
                    Assert.That(surfaceHost.CurrentSnapshot.Cells.All(cell => !cell.IsSelected), Is.True);
                    Assert.That(grid.HasSelectedRows, Is.False);
                    Assert.That(grid.SelectionStatusText, Does.Contain("0"));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void SurfaceRuntime_WhenClassicModeRowHeaderPressed_SelectsWholeRow()
        {
            var grid = CreateGrid();
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 640, height: 320);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var rowHeader = surfaceHost.CurrentSnapshot.Headers.Single(header => header.Kind == GridHeaderKind.RowHeader && header.HeaderKey == "row-1");
                GridSurfaceTestHost.ClickBoundsCenter(surfaceHost, rowHeader.Bounds);
                GridSurfaceTestHost.FlushDispatcher(grid);

                Assert.Multiple(() =>
                {
                    Assert.That(surfaceHost.CurrentSnapshot.Cells.Where(cell => cell.RowKey == "row-1").All(cell => cell.IsSelected), Is.True);
                    Assert.That(surfaceHost.CurrentSnapshot.Headers.Single(header => header.Kind == GridHeaderKind.RowHeader && header.HeaderKey == "row-1").IsSelected, Is.False);
                    Assert.That(grid.SelectionStatusText, Does.Contain("1"));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void SurfaceRuntime_WhenColumnHeaderClicked_AppliesAscendingSortAndRefreshesSnapshot()
        {
            var grid = CreateGrid();
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 640, height: 320);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var cityHeader = surfaceHost.CurrentSnapshot.Headers.Single(header => header.Kind == GridHeaderKind.ColumnHeader && header.HeaderKey == "City");
                GridSurfaceTestHost.ClickBoundsCenter(surfaceHost, cityHeader.Bounds);
                GridSurfaceTestHost.FlushDispatcher(grid);

                Assert.Multiple(() =>
                {
                    Assert.That(grid.Sorts, Has.Exactly(1).Matches<GridSortDescriptor>(sort =>
                        sort.ColumnId == "City" && sort.Direction == GridSortDirection.Ascending));
                    Assert.That(surfaceHost.CurrentSnapshot.Rows[0].RowKey, Is.EqualTo("row-2"));
                    Assert.That(surfaceHost.CurrentSnapshot.Columns.Single(column => column.ColumnKey == "City").SortPriority, Is.EqualTo(0));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void SurfaceRuntime_WhenShiftClickingSecondColumnHeader_AppendsSortDescriptor()
        {
            var grid = CreateGrid();
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 640, height: 320);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var nameHeader = surfaceHost.CurrentSnapshot.Headers.Single(header => header.Kind == GridHeaderKind.ColumnHeader && header.HeaderKey == "Name");
                var cityHeader = surfaceHost.CurrentSnapshot.Headers.Single(header => header.Kind == GridHeaderKind.ColumnHeader && header.HeaderKey == "City");

                GridSurfaceTestHost.ClickBoundsCenter(surfaceHost, nameHeader.Bounds);
                GridSurfaceTestHost.ClickBoundsCenter(surfaceHost, cityHeader.Bounds, UniversalModifierKeys.Shift);
                GridSurfaceTestHost.FlushDispatcher(grid);

                var nameColumn = surfaceHost.CurrentSnapshot.Columns.Single(column => column.ColumnKey == "Name");
                var cityColumn = surfaceHost.CurrentSnapshot.Columns.Single(column => column.ColumnKey == "City");

                Assert.Multiple(() =>
                {
                    Assert.That(grid.Sorts.Count, Is.EqualTo(2));
                    Assert.That(nameColumn.SortPriority, Is.EqualTo(0));
                    Assert.That(cityColumn.SortPriority, Is.EqualTo(1));
                    Assert.That(nameColumn.SortDirection, Is.True);
                    Assert.That(cityColumn.SortDirection, Is.True);
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void SurfaceRuntime_WhenCurrentRecordIndicatorDisabled_DoesNotRenderRightSideMarker()
        {
            var grid = CreateGrid();
            grid.ShowCurrentRecordIndicator = false;
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 640, height: 320);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var nameCell = surfaceHost.CurrentSnapshot.Cells.Single(cell => cell.RowKey == "row-1" && cell.ColumnKey == "Name");

                GridSurfaceTestHost.ClickBoundsCenter(surfaceHost, nameCell.Bounds);
                GridSurfaceTestHost.FlushDispatcher(grid);

                Assert.That(surfaceHost.CurrentSnapshot.Overlays.Any(overlay => overlay.Kind == GridOverlayKind.CurrentRecord), Is.False);
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void SurfaceRuntime_WhenClickingSecondColumnHeaderWithoutShift_ReplacesPreviousSortDescriptor()
        {
            var grid = CreateGrid();
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 640, height: 320);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var nameHeader = surfaceHost.CurrentSnapshot.Headers.Single(header => header.Kind == GridHeaderKind.ColumnHeader && header.HeaderKey == "Name");
                var cityHeader = surfaceHost.CurrentSnapshot.Headers.Single(header => header.Kind == GridHeaderKind.ColumnHeader && header.HeaderKey == "City");

                GridSurfaceTestHost.ClickBoundsCenter(surfaceHost, nameHeader.Bounds);
                GridSurfaceTestHost.ClickBoundsCenter(surfaceHost, cityHeader.Bounds);
                GridSurfaceTestHost.FlushDispatcher(grid);
                var sorts = grid.Sorts.ToArray();

                Assert.Multiple(() =>
                {
                    Assert.That(sorts.Length, Is.EqualTo(1));
                    Assert.That(sorts[0].ColumnId, Is.EqualTo("City"));
                    Assert.That(sorts[0].Direction, Is.EqualTo(GridSortDirection.Ascending));
                    Assert.That(surfaceHost.CurrentSnapshot.Rows[0].RowKey, Is.EqualTo("row-2"));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void SurfaceRuntime_WhenTouchModeAndShiftClickingSecondColumnHeader_AppendsSortDescriptor()
        {
            var grid = CreateGrid();
            grid.InteractionMode = GridInteractionMode.Touch;
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 640, height: 320);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var nameHeader = surfaceHost.CurrentSnapshot.Headers.Single(header => header.Kind == GridHeaderKind.ColumnHeader && header.HeaderKey == "Name");
                var cityHeader = surfaceHost.CurrentSnapshot.Headers.Single(header => header.Kind == GridHeaderKind.ColumnHeader && header.HeaderKey == "City");

                GridSurfaceTestHost.ClickBoundsCenter(surfaceHost, nameHeader.Bounds);
                GridSurfaceTestHost.ClickBoundsCenter(surfaceHost, cityHeader.Bounds, UniversalModifierKeys.Shift);
                GridSurfaceTestHost.FlushDispatcher(grid);

                Assert.That(grid.Sorts.Count, Is.EqualTo(2));
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void SurfaceRuntime_WhenGroupedColumnHeaderClicked_TogglesGroupDirection()
        {
            var grid = CreateGroupedGrid();
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 640, height: 320);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var cityHeader = surfaceHost.CurrentSnapshot.Headers.Single(header => header.Kind == GridHeaderKind.ColumnHeader && header.HeaderKey == "City");

                Assert.That(grid.Groups.Single().Direction, Is.EqualTo(GridSortDirection.Ascending));

                GridSurfaceTestHost.ClickBoundsCenter(surfaceHost, cityHeader.Bounds);
                GridSurfaceTestHost.FlushDispatcher(grid);
                Assert.That(grid.Groups.Single().Direction, Is.EqualTo(GridSortDirection.Descending));

                GridSurfaceTestHost.ClickBoundsCenter(surfaceHost, cityHeader.Bounds);
                GridSurfaceTestHost.FlushDispatcher(grid);
                Assert.That(grid.Groups.Single().Direction, Is.EqualTo(GridSortDirection.Ascending));
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void SurfaceRuntime_WhenMultiSortApplied_RendersSortOrderOrdinalOnVisibleHeaders()
        {
            var grid = CreateThreeColumnGrid();
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 720, height: 320);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var nameHeader = surfaceHost.CurrentSnapshot.Headers.Single(header => header.Kind == GridHeaderKind.ColumnHeader && header.HeaderKey == "Name");
                var cityHeader = surfaceHost.CurrentSnapshot.Headers.Single(header => header.Kind == GridHeaderKind.ColumnHeader && header.HeaderKey == "City");
                var statusHeader = surfaceHost.CurrentSnapshot.Headers.Single(header => header.Kind == GridHeaderKind.ColumnHeader && header.HeaderKey == "Status");

                GridSurfaceTestHost.ClickBoundsCenter(surfaceHost, nameHeader.Bounds);
                GridSurfaceTestHost.ClickBoundsCenter(surfaceHost, cityHeader.Bounds, UniversalModifierKeys.Shift);
                GridSurfaceTestHost.ClickBoundsCenter(surfaceHost, statusHeader.Bounds, UniversalModifierKeys.Shift);
                GridSurfaceTestHost.FlushDispatcher(grid);

                var cityPresenter = GridSurfaceTestHost.FindElementByAutomationId<GridColumnHeaderPresenter>(surfaceHost, "surface.column-header.City");
                var statusPresenter = GridSurfaceTestHost.FindElementByAutomationId<GridColumnHeaderPresenter>(surfaceHost, "surface.column-header.Status");

                Assert.Multiple(() =>
                {
                    Assert.That(cityPresenter, Is.Not.Null);
                    Assert.That(statusPresenter, Is.Not.Null);
                    Assert.That(GridSurfaceTestHost.ReadVisibleText(cityPresenter), Does.Contain("2"));
                    Assert.That(GridSurfaceTestHost.ReadVisibleText(statusPresenter), Does.Contain("3"));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void SurfaceRuntime_WhenColumnResizeHandleDragged_UpdatesColumnWidthAndSnapshot()
        {
            var grid = CreateGrid();
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 640, height: 320);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var nameHeader = surfaceHost.CurrentSnapshot.Headers.Single(header => header.Kind == GridHeaderKind.ColumnHeader && header.HeaderKey == "Name");
                var originalWidth = grid.VisibleColumns.First(column => column.ColumnId == "Name").Width;
                var pressX = nameHeader.Bounds.X + nameHeader.Bounds.Width - 2d;
                var centerY = nameHeader.Bounds.Y + (nameHeader.Bounds.Height / 2d);

                GridSurfaceTestHost.DragPoint(surfaceHost, pressX, centerY, pressX + 48d, centerY);
                GridSurfaceTestHost.FlushDispatcher(grid);

                var updatedWidth = grid.VisibleColumns.First(column => column.ColumnId == "Name").Width;
                var snapshotColumn = surfaceHost.CurrentSnapshot.Columns.Single(column => column.ColumnKey == "Name");

                Assert.Multiple(() =>
                {
                    Assert.That(updatedWidth, Is.GreaterThan(originalWidth));
                    Assert.That(snapshotColumn.Width, Is.EqualTo(updatedWidth).Within(1d));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void SurfaceRuntime_WhenColumnHeaderDragged_ReordersColumnsAndPreservesSortState()
        {
            var grid = CreateGrid();
            grid.Sorts = new[] { new GridSortDescriptor("City", GridSortDirection.Descending) };
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 640, height: 320);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var nameHeader = surfaceHost.CurrentSnapshot.Headers.Single(header => header.Kind == GridHeaderKind.ColumnHeader && header.HeaderKey == "Name");
                var cityHeader = surfaceHost.CurrentSnapshot.Headers.Single(header => header.Kind == GridHeaderKind.ColumnHeader && header.HeaderKey == "City");

                GridSurfaceTestHost.DragPoint(
                    surfaceHost,
                    nameHeader.Bounds.X + (nameHeader.Bounds.Width / 2d),
                    nameHeader.Bounds.Y + (nameHeader.Bounds.Height / 2d),
                    cityHeader.Bounds.X + (cityHeader.Bounds.Width * 0.75d),
                    cityHeader.Bounds.Y + (cityHeader.Bounds.Height / 2d));
                GridSurfaceTestHost.FlushDispatcher(grid);

                Assert.Multiple(() =>
                {
                    Assert.That(grid.VisibleColumns.Select(column => column.ColumnId).Take(2), Is.EqualTo(new[] { "City", "Name" }));
                    Assert.That(grid.Sorts.Single().ColumnId, Is.EqualTo("City"));
                    Assert.That(grid.VisibleColumns.First(column => column.ColumnId == "City").Width, Is.EqualTo(140d).Within(1d));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void SurfaceRuntime_WhenEditingCancelledThroughEscape_RestoresOriginalValue()
        {
            var grid = CreateGrid();
            grid.IsGridReadOnly = false;
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 640, height: 320);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var nameCell = surfaceHost.CurrentSnapshot.Cells.Single(cell => cell.RowKey == "row-1" && cell.ColumnKey == "Name");
                GridSurfaceTestHost.DoubleClickPoint(surfaceHost, nameCell.Bounds.X + 10d, nameCell.Bounds.Y + (nameCell.Bounds.Height / 2d));
                GridSurfaceTestHost.SendText(surfaceHost, "Q");
                GridSurfaceTestHost.SendKey(surfaceHost, "ESCAPE");
                GridSurfaceTestHost.FlushDispatcher(grid);

                var firstRow = ((SurfaceRow[])grid.ItemsSource)[0];
                Assert.Multiple(() =>
                {
                    Assert.That(firstRow.Name, Is.EqualTo("Alpha"));
                    Assert.That(surfaceHost.CurrentSnapshot.ViewportState.IsInEditMode, Is.False);
                    Assert.That(surfaceHost.CurrentSnapshot.Cells.Single(cell => cell.RowKey == "row-1" && cell.ColumnKey == "Name").DisplayText, Is.EqualTo("Alpha"));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void SurfaceRuntime_WhenValueCommittedThroughEnter_MarksPendingEditUntilGridCommit()
        {
            var grid = CreateGrid();
            grid.IsGridReadOnly = false;
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 640, height: 320);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var nameCell = surfaceHost.CurrentSnapshot.Cells.Single(cell => cell.RowKey == "row-1" && cell.ColumnKey == "Name");
                GridSurfaceTestHost.ClickBoundsCenter(surfaceHost, nameCell.Bounds);
                GridSurfaceTestHost.SendText(surfaceHost, "Z");
                GridSurfaceTestHost.SendText(surfaceHost, "e");
                GridSurfaceTestHost.SendText(surfaceHost, "d");
                GridSurfaceTestHost.SendKey(surfaceHost, "ENTER");
                GridSurfaceTestHost.FlushDispatcher(grid);

                Assert.Multiple(() =>
                {
                    Assert.That(surfaceHost.CurrentSnapshot.ViewportState.IsInEditMode, Is.False);
                    Assert.That(grid.HasPendingEdits, Is.True);
                    Assert.That(grid.PendingEditCount, Is.EqualTo(1));
                    Assert.That(grid.HasValidationIssues, Is.False);
                    Assert.That(
                        surfaceHost.CurrentSnapshot.Headers.Any(
                            header => header.Kind == GridHeaderKind.RowHeader &&
                                      (header.RowIndicatorState == GridRowIndicatorState.Edited ||
                                       header.RowIndicatorState == GridRowIndicatorState.CurrentAndEdited)),
                        Is.True);
                });

                grid.CommitEdits();
                GridSurfaceTestHost.FlushDispatcher(grid);

                Assert.Multiple(() =>
                {
                    Assert.That(grid.HasPendingEdits, Is.False);
                    Assert.That(grid.PendingEditCount, Is.EqualTo(0));
                    Assert.That(
                        surfaceHost.CurrentSnapshot.Headers.Any(
                            header => header.Kind == GridHeaderKind.RowHeader &&
                                      (header.RowIndicatorState == GridRowIndicatorState.Edited ||
                                       header.RowIndicatorState == GridRowIndicatorState.CurrentAndEdited)),
                        Is.False);
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void SurfaceRuntime_WhenCommittedPendingEditCancelledThroughGridApi_RestoresOriginalValue()
        {
            var grid = CreateGrid();
            grid.IsGridReadOnly = false;
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 640, height: 320);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var nameCell = surfaceHost.CurrentSnapshot.Cells.Single(cell => cell.RowKey == "row-1" && cell.ColumnKey == "Name");
                GridSurfaceTestHost.ClickBoundsCenter(surfaceHost, nameCell.Bounds);
                GridSurfaceTestHost.SendText(surfaceHost, "Q");
                GridSurfaceTestHost.SendKey(surfaceHost, "ENTER");
                GridSurfaceTestHost.FlushDispatcher(grid);

                Assert.That(grid.HasPendingEdits, Is.True);
                Assert.That(GetTrackedOriginalName(grid, "row-1"), Is.EqualTo("Alpha"));

                grid.CancelEdits();
                GridSurfaceTestHost.FlushDispatcher(grid);

                var firstRow = ((SurfaceRow[])grid.ItemsSource)[0];
                Assert.Multiple(() =>
                {
                    Assert.That(firstRow.Name, Is.EqualTo("Alpha"));
                    Assert.That(surfaceHost.CurrentSnapshot.Cells.Single(cell => cell.RowKey == "row-1" && cell.ColumnKey == "Name").DisplayText, Is.EqualTo("Alpha"));
                    Assert.That(grid.HasPendingEdits, Is.False);
                    Assert.That(grid.PendingEditCount, Is.EqualTo(0));
                    Assert.That(grid.HasValidationIssues, Is.False);
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void SurfaceRuntime_WhenValidationFails_KeepsEditorOpenAndPreservesSourceValue()
        {
            var grid = CreateGrid();
            grid.IsGridReadOnly = false;
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 640, height: 320);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var nameCell = surfaceHost.CurrentSnapshot.Cells.Single(cell => cell.RowKey == "row-1" && cell.ColumnKey == "Name");
                GridSurfaceTestHost.DoubleClickPoint(surfaceHost, nameCell.Bounds.X + 10d, nameCell.Bounds.Y + (nameCell.Bounds.Height / 2d));
                GridSurfaceTestHost.SendKey(surfaceHost, "DELETE");
                GridSurfaceTestHost.SendKey(surfaceHost, "ENTER");
                GridSurfaceTestHost.FlushDispatcher(grid);

                var firstRow = ((SurfaceRow[])grid.ItemsSource)[0];
                var editedCell = surfaceHost.CurrentSnapshot.Cells.Single(cell => cell.RowKey == "row-1" && cell.ColumnKey == "Name");
                Assert.Multiple(() =>
                {
                    Assert.That(firstRow.Name, Is.EqualTo("Alpha"));
                    Assert.That(surfaceHost.CurrentSnapshot.ViewportState.IsInEditMode, Is.True);
                    Assert.That(editedCell.IsEditing, Is.True);
                    Assert.That(editedCell.HasValidationError, Is.True);
                    Assert.That(editedCell.ValidationError, Is.Not.Empty);
                    Assert.That(grid.HasPendingEdits, Is.False);
                    Assert.That(grid.PendingEditCount, Is.EqualTo(0));
                    Assert.That(grid.HasValidationIssues, Is.True);
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void SurfaceRuntime_WhenDeclarativeConstraintFailsAndThenIsFixed_UpdatesCellAndRowValidationState()
        {
            var grid = CreateConstrainedGrid();
            grid.IsGridReadOnly = false;
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 640, height: 320);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var nameCell = surfaceHost.CurrentSnapshot.Cells.Single(cell => cell.RowKey == "row-1" && cell.ColumnKey == "Name");
                GridSurfaceTestHost.DoubleClickPoint(surfaceHost, nameCell.Bounds.X + 10d, nameCell.Bounds.Y + (nameCell.Bounds.Height / 2d));
                GridSurfaceTestHost.SendKey(surfaceHost, "DELETE");
                GridSurfaceTestHost.SendKey(surfaceHost, "ENTER");
                GridSurfaceTestHost.FlushDispatcher(grid);

                var invalidCell = surfaceHost.CurrentSnapshot.Cells.Single(cell => cell.RowKey == "row-1" && cell.ColumnKey == "Name");
                Assert.Multiple(() =>
                {
                    Assert.That(invalidCell.HasValidationError, Is.True);
                    Assert.That(invalidCell.ValidationError, Does.Contain("required").IgnoreCase);
                    Assert.That(grid.HasValidationIssues, Is.True);
                });

                foreach (var character in "Gamma")
                {
                    GridSurfaceTestHost.SendText(surfaceHost, character.ToString());
                }
                GridSurfaceTestHost.SendKey(surfaceHost, "ENTER");
                GridSurfaceTestHost.FlushDispatcher(grid);

                var fixedCell = surfaceHost.CurrentSnapshot.Cells.Single(cell => cell.RowKey == "row-1" && cell.ColumnKey == "Name");
                Assert.Multiple(() =>
                {
                    Assert.That(fixedCell.HasValidationError, Is.False);
                    Assert.That(grid.HasValidationIssues, Is.False);
                    Assert.That(((ConstraintRow[])grid.ItemsSource)[0].Name, Is.EqualTo("Gamma"));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void SurfaceRuntime_WhenDeclarativeConstraintIsCorrectedWhileTyping_ClearsLiveValidationBeforeCommit()
        {
            var grid = CreateConstrainedGrid();
            grid.IsGridReadOnly = false;
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 640, height: 320);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var nameCell = surfaceHost.CurrentSnapshot.Cells.Single(cell => cell.RowKey == "row-1" && cell.ColumnKey == "Name");
                GridSurfaceTestHost.DoubleClickPoint(surfaceHost, nameCell.Bounds.X + 10d, nameCell.Bounds.Y + (nameCell.Bounds.Height / 2d));
                GridSurfaceTestHost.SendKey(surfaceHost, "DELETE");
                GridSurfaceTestHost.FlushDispatcher(grid);

                var invalidCell = surfaceHost.CurrentSnapshot.Cells.Single(cell => cell.RowKey == "row-1" && cell.ColumnKey == "Name");
                Assert.That(invalidCell.HasValidationError, Is.True);

                foreach (var character in "Gam")
                {
                    GridSurfaceTestHost.SendText(surfaceHost, character.ToString());
                }
                GridSurfaceTestHost.FlushDispatcher(grid);

                var correctedCell = surfaceHost.CurrentSnapshot.Cells.Single(cell => cell.RowKey == "row-1" && cell.ColumnKey == "Name");
                Assert.Multiple(() =>
                {
                    Assert.That(correctedCell.IsEditing, Is.True);
                    Assert.That(correctedCell.HasValidationError, Is.False);
                    Assert.That(((ConstraintRow[])grid.ItemsSource)[0].Name, Is.EqualTo("Alpha"));
                });
            }
            finally
            {
                window.Close();
            }
        }

        private static WpfGrid CreateGrid()
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
                ItemsSource = new[]
                {
                    new SurfaceRow { Id = "row-1", Name = "Alpha", City = "Warsaw" },
                    new SurfaceRow { Id = "row-2", Name = "Beta", City = "Gdansk" },
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

        private static WpfGrid CreateConstrainedGrid()
        {
            return new WpfGrid
            {
                Width = 640,
                Height = 320,
                IsGridReadOnly = true,
                LanguageDirectory = global::PhialeGrid.Wpf.Tests.GridTestRepositoryPaths.GridLanguagesDirectory,
                Columns = new[]
                {
                    new GridColumnDefinition("Name", "Name", width: 160, displayIndex: 0, valueType: typeof(string), validationConstraints: new TextValidationConstraints(required: true, minLength: 3)),
                    new GridColumnDefinition("Scale", "Scale", width: 140, displayIndex: 1, valueType: typeof(int), validationConstraints: new IntegerValidationConstraints(required: true, minValue: 100, maxValue: 1000)),
                },
                ItemsSource = new[]
                {
                    new ConstraintRow { Id = "row-1", Name = "Alpha", Scale = 200 },
                    new ConstraintRow { Id = "row-2", Name = "Beta", Scale = 300 },
                },
            };
        }

        private static WpfGrid CreateScrollableGrid()
        {
            return new WpfGrid
            {
                Width = 320,
                Height = 160,
                IsGridReadOnly = true,
                LanguageDirectory = global::PhialeGrid.Wpf.Tests.GridTestRepositoryPaths.GridLanguagesDirectory,
                Columns = new[]
                {
                    new GridColumnDefinition("Name", "Name", width: 140, displayIndex: 0),
                    new GridColumnDefinition("City", "City", width: 140, displayIndex: 1),
                    new GridColumnDefinition("Status", "Status", width: 140, displayIndex: 2),
                    new GridColumnDefinition("Owner", "Owner", width: 160, displayIndex: 3),
                    new GridColumnDefinition("Scale", "Scale", width: 180, displayIndex: 4),
                },
                ItemsSource = new[]
                {
                    new SurfaceRow { Id = "row-1", Name = "Alpha", City = "Warsaw", Status = "Ready", Owner = "A", Scale = "100" },
                    new SurfaceRow { Id = "row-2", Name = "Beta", City = "Gdansk", Status = "Draft", Owner = "B", Scale = "200" },
                    new SurfaceRow { Id = "row-3", Name = "Gamma", City = "Krakow", Status = "Ready", Owner = "C", Scale = "300" },
                    new SurfaceRow { Id = "row-4", Name = "Delta", City = "Lodz", Status = "Ready", Owner = "D", Scale = "400" },
                    new SurfaceRow { Id = "row-5", Name = "Epsilon", City = "Poznan", Status = "Draft", Owner = "E", Scale = "500" },
                },
            };
        }

        private static WpfGrid CreateGroupedScrollableGrid()
        {
            var grid = CreateScrollableGrid();
            grid.IsGridReadOnly = false;
            grid.Groups = new[] { new GridGroupDescriptor("City") };
            return grid;
        }

        private static string GetTrackedOriginalName(WpfGrid grid, string rowId)
        {
            return grid.EditSessionContext?
                .GetFieldChanges(rowId)
                .FirstOrDefault(change => string.Equals(change.FieldId, "Name", StringComparison.OrdinalIgnoreCase))
                ?.OriginalValue as string;
        }

        private sealed class SurfaceRow : IDataErrorInfo
        {
            public string Id { get; set; }

            public string Name { get; set; }

            public string City { get; set; }

            public string Status { get; set; }

            public string Owner { get; set; }

            public string Scale { get; set; }

            public string Error => string.Empty;

            public string this[string columnName]
            {
                get
                {
                    if (string.Equals(columnName, nameof(Name), StringComparison.OrdinalIgnoreCase) &&
                        string.IsNullOrWhiteSpace(Name))
                    {
                        return "Name is required.";
                    }

                    if (string.Equals(columnName, nameof(Owner), StringComparison.OrdinalIgnoreCase) &&
                        string.IsNullOrWhiteSpace(Owner))
                    {
                        return "Owner is required.";
                    }

                    return string.Empty;
                }
            }
        }

        private sealed class ConstraintRow
        {
            public string Id { get; set; }

            public string Name { get; set; }

            public int Scale { get; set; }
        }
    }
}

