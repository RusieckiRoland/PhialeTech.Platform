using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;

namespace PhialeGrid.MockServer.Tests
{
    [TestFixture]
    public sealed class MockServerEndpointsTests
    {
        private WebApplicationFactory<Program> _factory;

        [SetUp]
        public void SetUp()
        {
            _factory = new WebApplicationFactory<Program>();
        }

        [TearDown]
        public void TearDown()
        {
            _factory.Dispose();
        }

        [Test]
        public async System.Threading.Tasks.Task HealthEndpoint_ShouldReturnOk()
        {
            using var client = _factory.CreateClient();

            using var response = await client.GetAsync("/api/phialegrid/health");
            var payload = await response.Content.ReadFromJsonAsync<JsonElement>();

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(payload.GetProperty("status").GetString(), Is.EqualTo("ok"));
            Assert.That(payload.GetProperty("service").GetString(), Is.EqualTo("PhialeGrid.MockServer"));
        }

        [Test]
        public async System.Threading.Tasks.Task SchemaEndpoint_ShouldReturnExpectedColumnsAndTotalCount()
        {
            using var client = _factory.CreateClient();

            using var response = await client.GetAsync("/api/phialegrid/schema");
            var payload = await response.Content.ReadFromJsonAsync<JsonElement>();

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(payload.GetProperty("totalRecordCount").GetInt32(), Is.EqualTo(530));
            Assert.That(payload.GetProperty("columns").EnumerateArray().Count(), Is.EqualTo(12));
            Assert.That(payload.GetProperty("columns").EnumerateArray().First().GetProperty("id").GetString(), Is.EqualTo("Category"));
        }

        [Test]
        public async System.Threading.Tasks.Task QueryEndpoint_ShouldReturnPagedSortedFilteredRowsAndSummary()
        {
            using var client = _factory.CreateClient();
            var request = new
            {
                offset = 0,
                size = 5,
                sorts = new[]
                {
                    new
                    {
                        columnId = "AreaSquareMeters",
                        direction = "Descending",
                    },
                },
                filterGroup = new
                {
                    logicalOperator = "And",
                    filters = new[]
                    {
                        new
                        {
                            columnId = "Municipality",
                            @operator = "Equals",
                            value = "Wroclaw",
                        },
                    },
                },
                summaries = new[]
                {
                    new
                    {
                        columnId = "AreaSquareMeters",
                        type = "Sum",
                    },
                },
            };

            using var response = await client.PostAsJsonAsync("/api/phialegrid/query", request);
            var payload = await response.Content.ReadFromJsonAsync<JsonElement>();

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(payload.GetProperty("returnedCount").GetInt32(), Is.EqualTo(5));
            Assert.That(payload.GetProperty("totalCount").GetInt32(), Is.GreaterThan(5));

            var items = payload.GetProperty("items").EnumerateArray().ToArray();
            Assert.That(items[0].GetProperty("municipality").GetString(), Is.EqualTo("Wroclaw"));
            Assert.That(items[0].GetProperty("areaSquareMeters").GetDecimal(), Is.GreaterThanOrEqualTo(items[1].GetProperty("areaSquareMeters").GetDecimal()));
            Assert.That(payload.GetProperty("summary").GetProperty("AreaSquareMeters:Sum").GetDecimal(), Is.GreaterThan(0m));
        }

        [Test]
        public async System.Threading.Tasks.Task GroupedQueryEndpoint_ShouldRespectCollapsedGroupsAndReturnHeadersOnly()
        {
            using var client = _factory.CreateClient();
            var previewRequest = new
            {
                offset = 0,
                size = 20,
                groups = new[]
                {
                    new
                    {
                        columnId = "Category",
                        direction = "Ascending",
                    },
                },
            };

            using var previewResponse = await client.PostAsJsonAsync("/api/phialegrid/grouped-query", previewRequest);
            var previewPayload = await previewResponse.Content.ReadFromJsonAsync<JsonElement>();
            var firstGroupId = previewPayload.GetProperty("groupIds").EnumerateArray().First().GetString();

            var collapsedRequest = new
            {
                offset = 0,
                size = 20,
                groups = new[]
                {
                    new
                    {
                        columnId = "Category",
                        direction = "Ascending",
                    },
                },
                collapsedGroupIds = new[]
                {
                    firstGroupId,
                },
            };

            using var collapsedResponse = await client.PostAsJsonAsync("/api/phialegrid/grouped-query", collapsedRequest);
            var collapsedPayload = await collapsedResponse.Content.ReadFromJsonAsync<JsonElement>();
            var rows = collapsedPayload.GetProperty("rows").EnumerateArray().ToArray();

            Assert.That(collapsedResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(rows[0].GetProperty("kind").GetString(), Is.EqualTo("GroupHeader"));
            Assert.That(rows[0].GetProperty("groupId").GetString(), Is.EqualTo(firstGroupId));
            Assert.That(rows[0].GetProperty("isExpanded").GetBoolean(), Is.False);
            Assert.That(rows.Any(row => row.GetProperty("kind").GetString() == "DataRow"), Is.True);
        }

        [Test]
        public async System.Threading.Tasks.Task QueryEndpoint_ShouldReturnBadRequestForUnsupportedCustomSummary()
        {
            using var client = _factory.CreateClient();
            var request = new
            {
                offset = 0,
                size = 10,
                summaries = new[]
                {
                    new
                    {
                        columnId = "AreaSquareMeters",
                        type = "Custom",
                    },
                },
            };

            using var response = await client.PostAsJsonAsync("/api/phialegrid/query", request);
            var payload = await response.Content.ReadFromJsonAsync<JsonElement>();

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(payload.GetProperty("error").GetString(), Does.Contain("Custom summaries"));
        }
    }
}

