using System;
using System.Text.Json;
using NUnit.Framework;
using PhialeGrid.Core.Query;
using PhialeGrid.Core.Summaries;
using PhialeGrid.MockServer.Contracts;
using PhialeGrid.MockServer.Services;

namespace PhialeGrid.MockServer.Tests
{
    [TestFixture]
    public sealed class GridRequestMapperTests
    {
        private GridRequestMapper _mapper;

        [SetUp]
        public void SetUp()
        {
            _mapper = new GridRequestMapper(DemoGisGridDefinition.Schema);
        }

        [Test]
        public void Map_ShouldConvertDecimalAndDateFilterValuesUsingSchema()
        {
            var request = new GridQueryHttpRequest
            {
                Offset = 10,
                Size = 25,
                Sorts =
                {
                    new GridSortHttpDescriptor
                    {
                        ColumnId = "LastInspection",
                        Direction = "Descending",
                    },
                },
                FilterGroup = new GridFilterGroupHttpDescriptor
                {
                    LogicalOperator = "And",
                    Filters =
                    {
                        new GridFilterHttpDescriptor
                        {
                            ColumnId = "AreaSquareMeters",
                            Operator = "GreaterThan",
                            Value = JsonDocument.Parse("100.50").RootElement.Clone(),
                        },
                        new GridFilterHttpDescriptor
                        {
                            ColumnId = "LastInspection",
                            Operator = "Between",
                            Value = JsonDocument.Parse("\"2024-01-01T00:00:00Z\"").RootElement.Clone(),
                            SecondValue = JsonDocument.Parse("\"2024-12-31T00:00:00Z\"").RootElement.Clone(),
                        },
                    },
                },
                Summaries =
                {
                    new GridSummaryHttpDescriptor
                    {
                        ColumnId = "AreaSquareMeters",
                        Type = "Sum",
                    },
                },
            };

            var mapped = _mapper.Map(request);

            Assert.That(mapped.Offset, Is.EqualTo(10));
            Assert.That(mapped.Size, Is.EqualTo(25));
            Assert.That(mapped.Sorts[0].Direction, Is.EqualTo(GridSortDirection.Descending));
            Assert.That(mapped.FilterGroup.Filters[0].Value, Is.TypeOf<decimal>());
            Assert.That(mapped.FilterGroup.Filters[0].Value, Is.EqualTo(100.50m));
            Assert.That(mapped.FilterGroup.Filters[1].Value, Is.TypeOf<DateTime>());
            Assert.That(mapped.FilterGroup.Filters[1].SecondValue, Is.TypeOf<DateTime>());
            Assert.That(mapped.Summaries[0].Type, Is.EqualTo(GridSummaryType.Sum));
            Assert.That(mapped.Summaries[0].ValueType, Is.EqualTo(typeof(decimal)));
        }

        [Test]
        public void MapGrouped_ShouldConvertCollapsedGroupIdsIntoExpansionState()
        {
            const string collapsedGroupId = "Category:Road";
            var request = new GridGroupedQueryHttpRequest
            {
                Size = 30,
                Groups =
                {
                    new GridGroupHttpDescriptor
                    {
                        ColumnId = "Category",
                        Direction = "Ascending",
                    },
                },
                CollapsedGroupIds =
                {
                    collapsedGroupId,
                },
            };

            var mapped = _mapper.Map(request);

            Assert.That(mapped.Groups.Count, Is.EqualTo(1));
            Assert.That(mapped.ExpansionState.IsExpanded(collapsedGroupId), Is.False);
        }

        [Test]
        public void Map_ShouldRejectUnsupportedCustomSummary()
        {
            var request = new GridQueryHttpRequest
            {
                Size = 10,
                Summaries =
                {
                    new GridSummaryHttpDescriptor
                    {
                        ColumnId = "AreaSquareMeters",
                        Type = "Custom",
                    },
                },
            };

            var exception = Assert.Throws<InvalidOperationException>(() => _mapper.Map(request));

            Assert.That(exception.Message, Does.Contain("Custom summaries"));
        }
    }
}

