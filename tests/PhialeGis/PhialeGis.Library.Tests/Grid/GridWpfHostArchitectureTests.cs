using System.IO;
using NUnit.Framework;
using PhialeGis.Library.Tests.Support;

namespace PhialeGis.Library.Tests.Grid
{
    public class GridWpfHostArchitectureTests
    {
        [Test]
        public void WpfHost_UsesFlatGroupRows_InsteadOfCollectionViewGrouping()
        {
            var code = File.ReadAllText(GetCodePath());

            Assert.That(code, Does.Contain("QueryVirtualizedGridDataSource<object>"));
            Assert.That(code, Does.Contain("GroupedQueryVirtualizedGridDataSource<object>"));
            Assert.That(code, Does.Contain("GridVirtualizedGroupedRowCollection"));
            Assert.That(code, Does.Contain("GridVirtualizedCollectionView"));
            Assert.That(code, Does.Contain("new GridViewport("));
            Assert.That(code, Does.Not.Contain("GroupDescriptions.Add"));
            Assert.That(code, Does.Not.Contain("CollectionViewSource.GetDefaultView(filteredRows)"));
            Assert.That(code, Does.Not.Contain("BuildDisplayRows("));
            Assert.That(code, Does.Not.Contain("GridGroupRowFlattener"));
            Assert.That(code, Does.Not.Contain("BuildGroupedView(filteredRows"));
            Assert.That(code, Does.Contain("RowsView = CreateRowsView(_virtualizedRows);"));
            Assert.That(code, Does.Contain("RowsView = CreateRowsView(_virtualizedGroupedRows);"));
        }

        [Test]
        public void WpfHost_VirtualizedCollections_DoNotTriggerLoadsDuringCollectionViewEnumeration()
        {
            var code = File.ReadAllText(GetCodePath());

            Assert.That(code, Does.Contain("yield return GetLoadedRowOrPlaceholder(index);"));
            Assert.That(code, Does.Not.Contain("yield return this[index];"));
            Assert.That(code, Does.Not.Contain("QueuePageLoad(index);"));
            Assert.That(code, Does.Not.Contain("QueueWindowLoad(index, 12);"));
        }

        [Test]
        public void WpfHost_CancelAndCommitEdits_ResolveChangesFromSnapshots()
        {
            var code = File.ReadAllText(GetCodePath());

            Assert.That(code, Does.Contain("ResolveChangedRowIds()"));
            Assert.That(code, Does.Contain("GridEditSnapshotUtility.ResolveChangedRowIds"));
            Assert.That(code, Does.Contain("TrackEditedRow(row);"));
            Assert.That(code, Does.Not.Contain("Dispatcher.BeginInvoke(new Action(() => TrackEditedRow(row))"));
        }

        [Test]
        public void WpfHost_UsesCoreControllers_ForSortHeaderDragAndCurrentCellDescription()
        {
            var code = File.ReadAllText(GetCodePath());

            Assert.Multiple(() =>
            {
                Assert.That(code, Does.Contain("GridHeaderDragController"));
                Assert.That(code, Does.Contain("GridSortInteractionController"));
                Assert.That(code, Does.Contain("GridCurrentCellDescriptionBuilder"));
                Assert.That(code, Does.Contain("SurfaceHost.Initialize(_surfaceCoordinator);"));
                Assert.That(code, Does.Not.Contain("WpfUniversalInputAdapter."));
                Assert.That(code, Does.Not.Contain("var keepExisting = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;"));
                Assert.That(code, Does.Not.Contain("_dragStartPoint"));
                Assert.That(code, Does.Not.Contain("_dragColumn"));
                Assert.That(code, Does.Not.Contain("header.Header + \": \" +"));
            });
        }

        private static string GetCodePath()
        {
            return Path.Combine(GetRepoRoot(), "src", "PhialeTech", "Products", "Grid", "Platforms", "Wpf", "PhialeGrid.Wpf", "PhialeGrid.cs");
        }

        private static string GetRepoRoot()
        {
            return RepositoryPaths.GetRepositoryRoot();
        }
    }
}
