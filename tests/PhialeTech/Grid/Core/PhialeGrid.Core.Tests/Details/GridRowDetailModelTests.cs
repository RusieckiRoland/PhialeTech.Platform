using System;
using System.Collections.Generic;
using NUnit.Framework;
using PhialeGrid.Core.Details;

namespace PhialeGrid.Core.Tests.Details
{
    [TestFixture]
    public class GridRowDetailModelTests
    {
        [Test]
        public void FixedHeightPolicy_RejectsInvalidHeights()
        {
            Assert.Multiple(() =>
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => GridRowDetailHeightPolicy.Fixed(0));
                Assert.Throws<ArgumentOutOfRangeException>(() => GridRowDetailHeightPolicy.Fixed(-1));
                Assert.Throws<ArgumentOutOfRangeException>(() => GridRowDetailHeightPolicy.Fixed(double.NaN));
                Assert.Throws<ArgumentOutOfRangeException>(() => GridRowDetailHeightPolicy.Fixed(double.PositiveInfinity));
            });
        }

        [Test]
        public void Descriptor_RequiresExplicitOwnerKeyHeightAndContentDescriptor()
        {
            var policy = GridRowDetailHeightPolicy.Fixed(84);
            Assert.Multiple(() =>
            {
                Assert.Throws<ArgumentException>(() => new GridRowDetailDescriptor("", "row-1", policy, new object()));
                Assert.Throws<ArgumentException>(() => new GridRowDetailDescriptor("detail-1", "", policy, new object()));
                Assert.Throws<ArgumentNullException>(() => new GridRowDetailDescriptor("detail-1", "row-1", null, new object()));
                Assert.Throws<ArgumentNullException>(() => new GridRowDetailDescriptor("detail-1", "row-1", policy, null));
            });
        }

        [Test]
        public void ExpansionState_ToggleExpandCollapse_IsImmutableAndFailFast()
        {
            var initial = GridRowDetailExpansionState.Empty;
            var expanded = initial.Toggle("row-1");
            var collapsed = expanded.Toggle("row-1");

            Assert.Multiple(() =>
            {
                Assert.That(initial.IsExpanded("row-1"), Is.False);
                Assert.That(expanded.IsExpanded("row-1"), Is.True);
                Assert.That(collapsed.IsExpanded("row-1"), Is.False);
                Assert.Throws<ArgumentException>(() => initial.Toggle(" "));
            });
        }

        [Test]
        public void Context_ExposesRecordAndFieldValuesWithoutPlatformTypes()
        {
            var record = new TestRecord { Id = "row-1", Name = "Alpha" };
            var values = new Dictionary<string, object> { ["Name"] = "Alpha" };
            var fields = new Dictionary<string, GridRowDetailFieldContext>
            {
                ["Name"] = new GridRowDetailFieldContext("Name", "Object name", typeof(string), true),
            };

            var context = new GridRowDetailContext("row-1", "record-1", record, values, fields);

            Assert.Multiple(() =>
            {
                Assert.That(context.RowKey, Is.EqualTo("row-1"));
                Assert.That(context.RecordKey, Is.EqualTo("record-1"));
                Assert.That(context.Record, Is.SameAs(record));
                Assert.That(context.Values["Name"], Is.EqualTo("Alpha"));
                Assert.That(context.Fields["Name"].DisplayName, Is.EqualTo("Object name"));
            });
        }

        private sealed class TestRecord
        {
            public string Id { get; set; }

            public string Name { get; set; }
        }
    }
}
