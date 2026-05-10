using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PhialeGrid.Core;
using PhialeGrid.Core.Editing;
using PhialeGrid.Core.Interaction;
using PhialeGrid.Core.Layout;
using PhialeGrid.Core.Rendering;
using PhialeGrid.Core.Surface;

namespace PhialeGrid.Wpf.Tests.Surface
{
    [TestFixture]
    public sealed class GridSurfaceCoordinatorScrollBehaviorTests
    {
        [Test]
        public void TryScrollCellIntoView_WhenTargetIsOutsideViewport_UsesNearestOffsetOnBothAxes()
        {
            var coordinator = CreateScrollableCoordinator();

            var result = coordinator.TryScrollCellIntoView("row-5", "col-5");
            var snapshot = coordinator.GetCurrentSnapshot();

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.True);
                Assert.That(snapshot.ViewportState.HorizontalOffset, Is.EqualTo(260d).Within(0.1d));
                Assert.That(snapshot.ViewportState.VerticalOffset, Is.EqualTo(110d).Within(0.1d));
            });
        }

        [Test]
        public void TryScrollCellIntoView_WhenTargetIsAlreadyVisible_DoesNotMoveViewport()
        {
            var coordinator = CreateScrollableCoordinator();

            var result = coordinator.TryScrollCellIntoView("row-3", "col-3");
            var snapshot = coordinator.GetCurrentSnapshot();

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.True);
                Assert.That(snapshot.ViewportState.HorizontalOffset, Is.EqualTo(0d).Within(0.1d));
                Assert.That(snapshot.ViewportState.VerticalOffset, Is.EqualTo(0d).Within(0.1d));
            });
        }

        [Test]
        public void TryScrollCellIntoView_WhenTargetIsFrozen_DoesNotScroll()
        {
            var coordinator = CreateScrollableCoordinator();
            coordinator.SetScrollPosition(75d, 40d);

            var result = coordinator.TryScrollCellIntoView("row-1", "col-1");
            var snapshot = coordinator.GetCurrentSnapshot();

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.True);
                Assert.That(snapshot.ViewportState.HorizontalOffset, Is.EqualTo(75d).Within(0.1d));
                Assert.That(snapshot.ViewportState.VerticalOffset, Is.EqualTo(40d).Within(0.1d));
            });
        }

        [Test]
        public void TryScrollColumnIntoView_WithExplicitAlignment_FollowsContract()
        {
            var startCoordinator = CreateScrollableCoordinator();
            var centerCoordinator = CreateScrollableCoordinator();
            var endCoordinator = CreateScrollableCoordinator();

            var startResult = startCoordinator.TryScrollColumnIntoView("col-4", GridScrollAlignment.Start);
            var centerResult = centerCoordinator.TryScrollColumnIntoView("col-4", GridScrollAlignment.Center);
            var endResult = endCoordinator.TryScrollColumnIntoView("col-4", GridScrollAlignment.End);

            Assert.Multiple(() =>
            {
                Assert.That(startResult, Is.True);
                Assert.That(centerResult, Is.True);
                Assert.That(endResult, Is.True);
                Assert.That(startCoordinator.GetCurrentSnapshot().ViewportState.HorizontalOffset, Is.EqualTo(140d).Within(0.1d));
                Assert.That(centerCoordinator.GetCurrentSnapshot().ViewportState.HorizontalOffset, Is.EqualTo(130d).Within(0.1d));
                Assert.That(endCoordinator.GetCurrentSnapshot().ViewportState.HorizontalOffset, Is.EqualTo(120d).Within(0.1d));
            });
        }

        [Test]
        public void TryScrollCellIntoView_WhenCurrentCellRequested_DoesNotRewriteExistingSelection()
        {
            var coordinator = CreateScrollableCoordinator();
            coordinator.SelectionMode = GridSelectionMode.Row;
            coordinator.SelectRows(new[] { "row-2" });

            var result = coordinator.TryScrollCellIntoView("row-5", "col-5", setCurrentCell: true);
            var snapshot = coordinator.GetCurrentSnapshot();

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.True);
                Assert.That(snapshot.CurrentCell, Is.Not.Null);
                Assert.That(snapshot.CurrentCell.RowKey, Is.EqualTo("row-5"));
                Assert.That(snapshot.CurrentCell.ColumnKey, Is.EqualTo("col-5"));
                Assert.That(snapshot.Rows.Single(row => row.RowKey == "row-2").IsSelected, Is.True);
                Assert.That(snapshot.Rows.Single(row => row.RowKey == "row-5").IsSelected, Is.False);
            });
        }

        [Test]
        public void TryScrollCellIntoView_WhenTargetIsMissingFromCurrentProjection_ReturnsFalse()
        {
            var coordinator = CreateScrollableCoordinator();

            Assert.Multiple(() =>
            {
                Assert.That(coordinator.TryScrollRowIntoView("missing-row"), Is.False);
                Assert.That(coordinator.TryScrollColumnIntoView("missing-col"), Is.False);
                Assert.That(coordinator.TryScrollCellIntoView("missing-row", "col-5"), Is.False);
                Assert.That(coordinator.TryScrollCellIntoView("row-5", "missing-col"), Is.False);
            });
        }

        [Test]
        public void StartEditingCell_WhenCellStartsNearViewportEdge_CentersEditedCellHorizontally()
        {
            var coordinator = CreateEditableScrollableCoordinator();

            coordinator.SetCurrentCell("row-1", "col-2");
            coordinator.ProcessInput(new GridKeyInput(DateTime.UtcNow, GridKey.F2));

            var snapshot = coordinator.GetCurrentSnapshot();

            Assert.Multiple(() =>
            {
                Assert.That(snapshot.ViewportState.IsInEditMode, Is.True);
                Assert.That(snapshot.CurrentCell, Is.Not.Null);
                Assert.That(snapshot.CurrentCell.ColumnKey, Is.EqualTo("col-2"));
                Assert.That(snapshot.ViewportState.HorizontalOffset, Is.EqualTo(100d).Within(0.1d));
                Assert.That(snapshot.ViewportState.VerticalOffset, Is.EqualTo(0d).Within(0.1d));
            });
        }

        [Test]
        public void StartEditingCell_WhenCellIsAlreadyVisible_DoesNotShiftViewportUnderPointer()
        {
            var coordinator = CreateEditableVisibleCoordinator();

            coordinator.SetCurrentCell("row-1", "col-2");
            coordinator.ProcessInput(new GridKeyInput(DateTime.UtcNow, GridKey.F2));

            var snapshot = coordinator.GetCurrentSnapshot();

            Assert.Multiple(() =>
            {
                Assert.That(snapshot.ViewportState.IsInEditMode, Is.True);
                Assert.That(snapshot.CurrentCell, Is.Not.Null);
                Assert.That(snapshot.CurrentCell.ColumnKey, Is.EqualTo("col-2"));
                Assert.That(snapshot.ViewportState.HorizontalOffset, Is.EqualTo(0d).Within(0.1d));
                Assert.That(snapshot.ViewportState.VerticalOffset, Is.EqualTo(0d).Within(0.1d));
            });
        }

        [Test]
        public void Snapshot_WhenRowsAreEditedOrInvalid_ExposesViewportTrackMarkers()
        {
            var coordinator = CreateScrollableCoordinator();
            coordinator.SetEditedRows(new[] { "row-4" });
            coordinator.SetInvalidRows(new[] { "row-5" });
            coordinator.SetRowIndicatorToolTips(new System.Collections.Generic.Dictionary<string, string>
            {
                ["row-4"] = "Edited row",
                ["row-5"] = "Invalid row",
            });

            var markers = coordinator.GetCurrentSnapshot().ViewportState.VerticalTrackMarkers;

            Assert.Multiple(() =>
            {
                Assert.That(markers.Count, Is.EqualTo(2));
                Assert.That(markers.Any(marker => marker.TargetKey == "row-4" && marker.Kind == GridViewportTrackMarkerKind.PendingEdit), Is.True);
                Assert.That(markers.Any(marker => marker.TargetKey == "row-5" && marker.Kind == GridViewportTrackMarkerKind.ValidationError), Is.True);
            });
        }

        private static GridSurfaceCoordinator CreateScrollableCoordinator()
        {
            var coordinator = new GridSurfaceCoordinator
            {
                FrozenColumnCount = 1,
                FrozenRowCount = 1,
                SelectionMode = GridSelectionMode.Cell,
                EnableCellSelection = true,
            };

            coordinator.Initialize(
                new[]
                {
                    new GridColumnDefinition { ColumnKey = "col-1", Header = "Col 1", Width = 100, IsFrozen = true },
                    new GridColumnDefinition { ColumnKey = "col-2", Header = "Col 2", Width = 80 },
                    new GridColumnDefinition { ColumnKey = "col-3", Header = "Col 3", Width = 60 },
                    new GridColumnDefinition { ColumnKey = "col-4", Header = "Col 4", Width = 120 },
                    new GridColumnDefinition { ColumnKey = "col-5", Header = "Col 5", Width = 150 },
                },
                new[]
                {
                    new GridRowDefinition { RowKey = "row-1", Height = 20 },
                    new GridRowDefinition { RowKey = "row-2", Height = 30 },
                    new GridRowDefinition { RowKey = "row-3", Height = 40 },
                    new GridRowDefinition { RowKey = "row-4", Height = 50 },
                    new GridRowDefinition { RowKey = "row-5", Height = 60 },
                });

            coordinator.SetViewportSize(280d, 120d);
            return coordinator;
        }

        private static GridSurfaceCoordinator CreateEditableScrollableCoordinator()
        {
            var accessor = new TestEditableCellAccessor();
            accessor.Add("row-1", "col-1", "Alpha");
            accessor.Add("row-1", "col-2", "This is a wider editable cell");
            accessor.Add("row-1", "col-3", "Omega");

            var coordinator = new GridSurfaceCoordinator
            {
                CellValueProvider = accessor,
                EditCellAccessor = accessor,
                SelectionMode = GridSelectionMode.Cell,
                EnableCellSelection = true,
            };

            coordinator.Initialize(
                new[]
                {
                    new GridColumnDefinition { ColumnKey = "col-1", Header = "Col 1", Width = 100 },
                    new GridColumnDefinition { ColumnKey = "col-2", Header = "Col 2", Width = 180, IsReadOnly = false },
                    new GridColumnDefinition { ColumnKey = "col-3", Header = "Col 3", Width = 100 },
                },
                new[]
                {
                    new GridRowDefinition { RowKey = "row-1", Height = 24 },
                });

            coordinator.SetViewportSize(180d, 120d);
            return coordinator;
        }

        private static GridSurfaceCoordinator CreateEditableVisibleCoordinator()
        {
            var accessor = new TestEditableCellAccessor();
            accessor.Add("row-1", "col-1", "Alpha");
            accessor.Add("row-1", "col-2", "Visible editable cell");
            accessor.Add("row-1", "col-3", "Omega");

            var coordinator = new GridSurfaceCoordinator
            {
                CellValueProvider = accessor,
                EditCellAccessor = accessor,
                SelectionMode = GridSelectionMode.Cell,
                EnableCellSelection = true,
            };

            coordinator.Initialize(
                new[]
                {
                    new GridColumnDefinition { ColumnKey = "col-1", Header = "Col 1", Width = 100 },
                    new GridColumnDefinition { ColumnKey = "col-2", Header = "Col 2", Width = 120 },
                    new GridColumnDefinition { ColumnKey = "col-3", Header = "Col 3", Width = 100 },
                },
                new[]
                {
                    new GridRowDefinition { RowKey = "row-1", Height = 24 },
                });

            coordinator.SetViewportSize(320d, 120d);
            return coordinator;
        }

        private sealed class TestEditableCellAccessor : IGridEditCellAccessor, IGridCellValueProvider
        {
            private readonly Dictionary<string, object> _values = new(StringComparer.OrdinalIgnoreCase);

            public TestEditableCellAccessor Add(string rowKey, string columnKey, object value)
            {
                _values[rowKey + "_" + columnKey] = value;
                return this;
            }

            public bool TryGetValue(string rowKey, string columnKey, out object value)
            {
                return _values.TryGetValue(rowKey + "_" + columnKey, out value);
            }

            public void SetValue(string rowKey, string columnKey, object value)
            {
                _values[rowKey + "_" + columnKey] = value;
            }
        }
    }
}

