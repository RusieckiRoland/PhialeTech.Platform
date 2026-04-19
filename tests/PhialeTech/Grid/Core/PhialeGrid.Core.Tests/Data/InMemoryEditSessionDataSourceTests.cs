using System;
using NUnit.Framework;
using PhialeGrid.Core.Data;
using PhialeGrid.Core.Editing;

namespace PhialeGrid.Core.Tests.Data
{
    [TestFixture]
    public sealed class InMemoryEditSessionDataSourceTests
    {
        [Test]
        public void ReplaceData_ShouldRaiseSingleVersionChanged_AndUpdateSnapshotAndFields()
        {
            var dataSource = new InMemoryEditSessionDataSource<TestRecord>();
            var versionChangedCount = 0;
            dataSource.VersionChanged += (sender, args) => versionChangedCount++;

            var fieldDefinitions = new IEditSessionFieldDefinition[]
            {
                new EditSessionFieldDefinition<TestRecord>(
                    "Name",
                    "Name",
                    typeof(string),
                    getter: record => record.Name,
                    setter: (record, value) => record.Name = value as string ?? string.Empty)
            };

            dataSource.ReplaceData(
                new[]
                {
                    new TestRecord { Id = "1", Name = "Alpha" },
                },
                fieldDefinitions);

            Assert.Multiple(() =>
            {
                Assert.That(versionChangedCount, Is.EqualTo(1));
                Assert.That(dataSource.GetSnapshot().Count, Is.EqualTo(1));
                Assert.That(dataSource.GetFieldDefinitions().Count, Is.EqualTo(1));
                Assert.That(dataSource.GetSnapshot()[0].Name, Is.EqualTo("Alpha"));
                Assert.That(dataSource.GetFieldDefinitions()[0].FieldId, Is.EqualTo("Name"));
            });
        }

        private sealed class TestRecord
        {
            public string Id { get; set; }

            public string Name { get; set; }
        }
    }
}
