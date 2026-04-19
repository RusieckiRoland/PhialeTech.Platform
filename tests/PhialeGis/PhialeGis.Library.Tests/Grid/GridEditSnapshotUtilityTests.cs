using System;
using System.Collections.Generic;
using NUnit.Framework;
using PhialeGrid.Core.Editing;
using PhialeTech.Components.Shared.Model;

namespace PhialeGis.Library.Tests.Grid
{
    public class GridEditSnapshotUtilityTests
    {
        [Test]
        public void ResolveChangedRowIds_ReturnsChangedRows_EvenWhenDirtySetIsNotProvided()
        {
            var row1 = CreateRow("GIS-1", "Valve A");
            var row2 = CreateRow("GIS-2", "Valve B");

            var snapshots = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["GIS-1"] = row1.Clone(),
                ["GIS-2"] = row2.Clone(),
            };

            row1.ObjectName = "Valve A Updated";
            row2.Owner = "Updated owner";

            var currentRows = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["GIS-1"] = row1,
                ["GIS-2"] = row2,
            };

            var changed = GridEditSnapshotUtility.ResolveChangedRowIds(
                snapshots,
                rowId => currentRows.TryGetValue(rowId, out var row) ? row : null,
                AreRowsEqual);

            Assert.That(changed, Is.EquivalentTo(new[] { "GIS-1", "GIS-2" }));
        }

        [Test]
        public void ResolveChangedRowIds_IgnoresRowsThatAlreadyMatchSnapshot()
        {
            var row = CreateRow("GIS-1", "Valve A");
            var snapshots = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["GIS-1"] = row.Clone(),
            };

            var changed = GridEditSnapshotUtility.ResolveChangedRowIds(
                snapshots,
                _ => row,
                AreRowsEqual);

            Assert.That(changed, Is.Empty);
        }

        private static DemoGisRecordViewModel CreateRow(string id, string objectName)
        {
            return new DemoGisRecordViewModel(
                id,
                "Valve",
                objectName,
                "Point",
                "EPSG:2180",
                "Wroclaw",
                "Srodmiescie",
                "Active",
                0m,
                0m,
                new DateTime(2025, 1, 2),
                "Survey",
                "Medium",
                true,
                true,
                "Operations",
                500,
                "network");
        }

        private static bool AreRowsEqual(object left, object right)
        {
            if (left is not DemoGisRecordViewModel leftRow || right is not DemoGisRecordViewModel rightRow)
            {
                return Equals(left, right);
            }

            return leftRow.ObjectId == rightRow.ObjectId
                && leftRow.ObjectName == rightRow.ObjectName
                && leftRow.Status == rightRow.Status
                && leftRow.Priority == rightRow.Priority
                && leftRow.AreaSquareMeters == rightRow.AreaSquareMeters
                && leftRow.LengthMeters == rightRow.LengthMeters
                && leftRow.LastInspection == rightRow.LastInspection
                && leftRow.Owner == rightRow.Owner
                && leftRow.ScaleHint == rightRow.ScaleHint;
        }
    }
}
