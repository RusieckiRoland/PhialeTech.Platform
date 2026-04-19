using System;
using System.Collections.Generic;
using System.Linq;
using PhialeGrid.Core.Query;
using PhialeGrid.MockServer.Contracts;
using PhialeTech.Components.Shared.Model;

namespace PhialeGrid.MockServer.Services
{
    public sealed class DemoGisGridQueryService
    {
        private readonly IReadOnlyList<DemoGisRecordViewModel> _records;
        private readonly IReadOnlyList<PhialeGrid.Core.Columns.GridColumnDefinition> _columns;
        private readonly GridQueryEngine<DemoGisRecordViewModel> _engine;
        private readonly GridRequestMapper _mapper;

        public DemoGisGridQueryService()
            : this(DemoGisGridDefinition.Records, DemoGisGridDefinition.Columns, DemoGisGridDefinition.Accessor, DemoGisGridDefinition.Schema)
        {
        }

        public DemoGisGridQueryService(
            IReadOnlyList<DemoGisRecordViewModel> records,
            IReadOnlyList<PhialeGrid.Core.Columns.GridColumnDefinition> columns,
            IGridRowAccessor<DemoGisRecordViewModel> accessor,
            GridQuerySchema schema)
        {
            _records = records ?? throw new ArgumentNullException(nameof(records));
            _columns = columns ?? throw new ArgumentNullException(nameof(columns));
            _engine = new GridQueryEngine<DemoGisRecordViewModel>(accessor ?? throw new ArgumentNullException(nameof(accessor)), schema ?? throw new ArgumentNullException(nameof(schema)));
            _mapper = new GridRequestMapper(schema);
        }

        public GridSchemaHttpResponse GetSchema()
        {
            return new GridSchemaHttpResponse
            {
                TotalRecordCount = _records.Count,
                Columns = _columns.Select(column => new GridColumnHttpDescriptor
                {
                    Id = column.Id,
                    Header = column.Header,
                    ValueType = column.ValueType.Name,
                    Width = column.Width,
                    IsEditable = column.IsEditable,
                    IsVisible = column.IsVisible,
                }).ToArray(),
            };
        }

        public GridQueryHttpResponse Execute(GridQueryHttpRequest request)
        {
            var mappedRequest = _mapper.Map(request);
            var result = _engine.Execute(_records, mappedRequest);

            return new GridQueryHttpResponse
            {
                Offset = mappedRequest.Offset,
                Size = mappedRequest.Size,
                ReturnedCount = result.Items.Count,
                TotalCount = result.TotalCount,
                Summary = result.Summary.Values.ToDictionary(pair => pair.Key, pair => pair.Value),
                Items = result.Items.Select(ToDto).ToArray(),
            };
        }

        public GridGroupedQueryHttpResponse ExecuteGrouped(GridGroupedQueryHttpRequest request)
        {
            var mappedRequest = _mapper.Map(request);
            var result = _engine.ExecuteGroupedWindow(_records, mappedRequest);

            return new GridGroupedQueryHttpResponse
            {
                Offset = mappedRequest.Offset,
                Size = mappedRequest.Size,
                ReturnedRowCount = result.Rows.Count,
                VisibleRowCount = result.VisibleRowCount,
                TotalItemCount = result.TotalItemCount,
                TopLevelGroupCount = result.TopLevelGroupCount,
                GroupIds = result.GroupIds.ToArray(),
                Summary = result.Summary.Values.ToDictionary(pair => pair.Key, pair => pair.Value),
                Rows = result.Rows.Select(ToGroupedRowDto).ToArray(),
            };
        }

        private static GridGroupedRowHttpDto ToGroupedRowDto(GridGroupFlatRow<DemoGisRecordViewModel> row)
        {
            return new GridGroupedRowHttpDto
            {
                Kind = row.Kind.ToString(),
                Level = row.Level,
                GroupId = row.GroupId,
                GroupColumnId = row.GroupColumnId,
                GroupKey = row.GroupKey,
                GroupItemCount = row.GroupItemCount,
                IsExpanded = row.IsExpanded,
                Item = row.Item == null ? null : ToDto(row.Item),
            };
        }

        private static DemoGisRecordHttpDto ToDto(DemoGisRecordViewModel row)
        {
            return new DemoGisRecordHttpDto
            {
                Id = row.Id,
                Category = row.Category,
                ObjectId = row.ObjectId,
                ObjectName = row.ObjectName,
                GeometryType = row.GeometryType,
                Crs = row.Crs,
                Municipality = row.Municipality,
                District = row.District,
                Status = row.Status,
                AreaSquareMeters = row.AreaSquareMeters,
                LengthMeters = row.LengthMeters,
                LastInspection = row.LastInspection,
                Source = row.Source,
                Priority = row.Priority,
                Visible = row.Visible,
                EditableFlag = row.EditableFlag,
                Owner = row.Owner,
                ScaleHint = row.ScaleHint,
                Tags = row.Tags,
            };
        }
    }
}
