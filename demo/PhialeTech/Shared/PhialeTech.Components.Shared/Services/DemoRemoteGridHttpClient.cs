using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using PhialeGrid.Core.Query;
using PhialeTech.Components.Shared.Model;

namespace PhialeTech.Components.Shared.Services
{
    public sealed class DemoRemoteGridHttpClient : IDemoRemoteGridClient
    {
        private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private readonly HttpClient _httpClient;
        private readonly Uri _queryUri;

        public DemoRemoteGridHttpClient(HttpClient httpClient, string baseAddress)
        {
            if (httpClient == null)
            {
                throw new ArgumentNullException(nameof(httpClient));
            }

            if (string.IsNullOrWhiteSpace(baseAddress))
            {
                throw new ArgumentException("Base address is required.", nameof(baseAddress));
            }

            _httpClient = httpClient;
            var normalizedBaseAddress = baseAddress.EndsWith("/", StringComparison.Ordinal) ? baseAddress : baseAddress + "/";
            _queryUri = new Uri(new Uri(normalizedBaseAddress, UriKind.Absolute), "api/phialegrid/query");
        }

        public async Task<DemoRemoteQueryResult> QueryAsync(DemoRemoteQueryRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            using (var content = new StringContent(JsonSerializer.Serialize(RemoteQueryRequestPayload.From(request), SerializerOptions), Encoding.UTF8, "application/json"))
            using (var response = await _httpClient.PostAsync(_queryUri, content, cancellationToken).ConfigureAwait(false))
            {
                if (response.StatusCode == HttpStatusCode.Forbidden || response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new DemoRemoteQueryException(
                        DemoRemoteQueryFailureKind.Forbidden,
                        "Remote access denied.",
                        response.StatusCode);
                }

                if (!response.IsSuccessStatusCode)
                {
                    throw new DemoRemoteQueryException(
                        DemoRemoteQueryFailureKind.Unknown,
                        "Remote service returned HTTP " + (int)response.StatusCode + " " + response.ReasonPhrase + ".",
                        response.StatusCode);
                }

                var payload = JsonSerializer.Deserialize<RemoteQueryResponsePayload>(
                    await response.Content.ReadAsStringAsync().ConfigureAwait(false),
                    SerializerOptions);

                if (payload == null)
                {
                    throw new InvalidOperationException("Remote query response payload is empty.");
                }

                return payload.ToResult();
            }
        }

        private sealed class RemoteQueryRequestPayload
        {
            public int Offset { get; set; }

            public int Size { get; set; }

            public int RefreshGeneration { get; set; }

            public RemoteSortPayload[] Sorts { get; set; }

            public RemoteFilterGroupPayload FilterGroup { get; set; }

            public static RemoteQueryRequestPayload From(DemoRemoteQueryRequest request)
            {
                return new RemoteQueryRequestPayload
                {
                    Offset = request.Offset,
                    Size = request.Size,
                    RefreshGeneration = request.RefreshGeneration,
                    Sorts = request.Sorts.Select(sort => new RemoteSortPayload
                    {
                        ColumnId = sort.ColumnId,
                        Direction = sort.Direction.ToString()
                    }).ToArray(),
                    FilterGroup = RemoteFilterGroupPayload.From(request.FilterGroup)
                };
            }
        }

        private sealed class RemoteSortPayload
        {
            public string ColumnId { get; set; }

            public string Direction { get; set; }
        }

        private sealed class RemoteFilterGroupPayload
        {
            public string LogicalOperator { get; set; }

            public RemoteFilterPayload[] Filters { get; set; }

            public static RemoteFilterGroupPayload From(GridFilterGroup filterGroup)
            {
                var group = filterGroup ?? GridFilterGroup.EmptyAnd();
                return new RemoteFilterGroupPayload
                {
                    LogicalOperator = group.LogicalOperator.ToString(),
                    Filters = group.Filters.Select(filter => new RemoteFilterPayload
                    {
                        ColumnId = filter.ColumnId,
                        Operator = filter.Operator.ToString(),
                        Value = filter.Value,
                        SecondValue = filter.SecondValue
                    }).ToArray()
                };
            }
        }

        private sealed class RemoteFilterPayload
        {
            public string ColumnId { get; set; }

            public string Operator { get; set; }

            public object Value { get; set; }

            public object SecondValue { get; set; }
        }

        private sealed class RemoteQueryResponsePayload
        {
            public int Offset { get; set; }

            public int Size { get; set; }

            public int ReturnedCount { get; set; }

            public int TotalCount { get; set; }

            public List<RemoteRecordPayload> Items { get; set; }

            public DemoRemoteQueryResult ToResult()
            {
                return new DemoRemoteQueryResult(
                    Offset,
                    Size,
                    ReturnedCount,
                    TotalCount,
                    (Items ?? new List<RemoteRecordPayload>()).Select(item => item.ToViewModel()).ToArray());
            }
        }

        private sealed class RemoteRecordPayload
        {
            public string Category { get; set; }

            public string ObjectName { get; set; }

            public string ObjectId { get; set; }

            public string GeometryType { get; set; }

            public string Crs { get; set; }

            public string Municipality { get; set; }

            public string District { get; set; }

            public string Status { get; set; }

            public decimal AreaSquareMeters { get; set; }

            public decimal LengthMeters { get; set; }

            public DateTime LastInspection { get; set; }

            public string Source { get; set; }

            public string Priority { get; set; }

            public bool Visible { get; set; }

            public bool EditableFlag { get; set; }

            public string Owner { get; set; }

            public int ScaleHint { get; set; }

            public string Tags { get; set; }

            public DemoGisRecordViewModel ToViewModel()
            {
                return new DemoGisRecordViewModel(
                    ObjectId,
                    Category,
                    ObjectName,
                    GeometryType,
                    Crs,
                    Municipality,
                    District,
                    Status,
                    AreaSquareMeters,
                    LengthMeters,
                    LastInspection,
                    Source,
                    Priority,
                    Visible,
                    EditableFlag,
                    Owner,
                    ScaleHint,
                    Tags);
            }
        }
    }
}
