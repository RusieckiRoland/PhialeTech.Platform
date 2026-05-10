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
            Assert.That(code, Does.Contain("BuildGroupedSurfaceResult(sourceRows"));
            Assert.That(code, Does.Contain("CreateGroupedDisplayRows(groupedSurfaceResult.Rows)"));
            Assert.That(code, Does.Contain("RowsView = CreateRowsView(groupedDisplayRows);"));
            Assert.That(code, Does.Not.Contain("RowsView = CreateRowsView(_virtualizedGroupedRows);"));
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
        public void WpfHost_CancelAndCommitEdits_ResolveChangesThroughCoreEditSession()
        {
            var code = File.ReadAllText(GetCodePath());

            Assert.That(code, Does.Contain("ResolveChangedRowIds()"));
            Assert.That(code, Does.Contain("_editSessionContext.HasRecordChanges(rowId)"));
            Assert.That(code, Does.Contain("_editSessionContext.CompleteRecordEdit(rowId, hasChanges);"));
            Assert.That(code, Does.Contain("_editSessionContext.EditedRecordIds.ToArray()"));
            Assert.That(code, Does.Contain("private void TrackEditedRow("));
            Assert.That(code, Does.Not.Contain("Dispatcher.BeginInvoke(new Action(() => TrackEditedRow(row))"));
        }

        [Test]
        public void WpfHost_UsesCoreControllers_ForSortHeaderInteractionsAndCurrentCellDescription()
        {
            var code = File.ReadAllText(GetCodePath());
            var coreCoordinatorCode = File.ReadAllText(GetCoreSurfaceCoordinatorCodePath());

            Assert.Multiple(() =>
            {
                Assert.That(coreCoordinatorCode, Does.Contain("HeaderDragThreshold"));
                Assert.That(coreCoordinatorCode, Does.Contain("HasExceededColumnHeaderDragThreshold"));
                Assert.That(code, Does.Contain("GridSortInteractionController"));
                Assert.That(code, Does.Contain("GridCurrentCellDescriptionBuilder"));
                Assert.That(code, Does.Contain("SurfaceHost.Initialize(_surfaceCoordinator);"));
                Assert.That(code, Does.Contain("GridSurfaceUniversalInputAdapter"));
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

        private static string GetCoreSurfaceCoordinatorCodePath()
        {
            return Path.Combine(GetRepoRoot(), "src", "PhialeTech", "Products", "Grid", "Core", "PhialeGrid.Core", "GridSurfaceCoordinator.cs");
        }

        private static string GetRepoRoot()
        {
            return RepositoryPaths.GetRepositoryRoot();
        }
    }
}

