using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PhialeGrid.Core.Hierarchy;
using PhialeTech.Components.Shared.Model;

namespace PhialeTech.Components.Shared.Services
{
    public static class DemoGisHierarchyBuilder
    {
        public static DemoGisHierarchyDefinition Build(DemoFeatureCatalog catalog, int pageSize = 12)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            var records = catalog.GetGisRecords();
            var groupedByMunicipality = records
                .GroupBy(record => record.Municipality, StringComparer.OrdinalIgnoreCase)
                .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var childrenByMunicipality = groupedByMunicipality.ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<DemoGisRecordViewModel>)group
                    .OrderBy(record => record.District, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(record => record.ObjectName, StringComparer.OrdinalIgnoreCase)
                    .Select(record => (DemoGisRecordViewModel)record.Clone())
                    .ToArray(),
                StringComparer.OrdinalIgnoreCase);

            var roots = groupedByMunicipality
                .Select(group =>
                {
                    var summaryRow = BuildMunicipalitySummary(group.Key, group.ToArray());
                    return new GridHierarchyNode<object>(BuildMunicipalityNodeId(group.Key), summaryRow, canExpand: true);
                })
                .ToArray();

            var provider = new MunicipalityHierarchyProvider(childrenByMunicipality);
            return new DemoGisHierarchyDefinition(roots, new GridHierarchyController<object>(provider, pageSize: pageSize));
        }

        private static DemoGisRecordViewModel BuildMunicipalitySummary(string municipality, IReadOnlyList<DemoGisRecordViewModel> records)
        {
            var totalArea = records.Sum(record => record.AreaSquareMeters);
            var totalLength = records.Sum(record => record.LengthMeters);
            var latestInspection = records.Max(record => record.LastInspection);
            var districtCount = records.Select(record => record.District).Distinct(StringComparer.OrdinalIgnoreCase).Count();

            return new DemoGisRecordViewModel(
                "MUN-" + BuildMunicipalityNodeId(municipality),
                "Municipality",
                municipality,
                "Group",
                "EPSG:2180",
                municipality,
                districtCount.ToString(CultureInfo.InvariantCulture) + " districts",
                "Summary",
                totalArea,
                totalLength,
                latestInspection,
                "PhialeTech Demo",
                "Overview",
                true,
                false,
                "PhialeTech Demo",
                1000,
                "hierarchy");
        }

        private static string BuildMunicipalityNodeId(string municipality)
        {
            if (string.IsNullOrWhiteSpace(municipality))
            {
                return "municipality";
            }

            return municipality
                .Trim()
                .ToLowerInvariant()
                .Replace(" ", "-");
        }

        private sealed class MunicipalityHierarchyProvider : IGridHierarchyPagingProvider<object>
        {
            private readonly IReadOnlyDictionary<string, IReadOnlyList<DemoGisRecordViewModel>> _childrenByMunicipality;

            public MunicipalityHierarchyProvider(IReadOnlyDictionary<string, IReadOnlyList<DemoGisRecordViewModel>> childrenByMunicipality)
            {
                _childrenByMunicipality = childrenByMunicipality ?? throw new ArgumentNullException(nameof(childrenByMunicipality));
            }

            public Task<IReadOnlyList<GridHierarchyNode<object>>> LoadChildrenAsync(GridHierarchyNode<object> parent, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return Task.FromResult<IReadOnlyList<GridHierarchyNode<object>>>(BuildChildNodes(parent, 0, int.MaxValue).Items);
            }

            public Task<GridHierarchyPage<object>> LoadChildrenPageAsync(GridHierarchyNode<object> parent, int offset, int size, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var page = BuildChildNodes(parent, offset, size);
                return Task.FromResult(new GridHierarchyPage<object>(page.Items, page.HasMore));
            }

            private (IReadOnlyList<GridHierarchyNode<object>> Items, bool HasMore) BuildChildNodes(GridHierarchyNode<object> parent, int offset, int size)
            {
                var municipality = ExtractMunicipality(parent);
                if (!_childrenByMunicipality.TryGetValue(municipality, out var records))
                {
                    return (Array.Empty<GridHierarchyNode<object>>(), false);
                }

                var safeOffset = Math.Max(0, offset);
                var safeSize = Math.Max(1, size);
                var slice = records.Skip(safeOffset).Take(safeSize).ToArray();
                var nodes = slice
                    .Select(record => new GridHierarchyNode<object>(record.ObjectId, record, canExpand: false, parent.PathId))
                    .ToArray();

                return (nodes, safeOffset + nodes.Length < records.Count);
            }

            private static string ExtractMunicipality(GridHierarchyNode<object> parent)
            {
                if (parent?.Item is DemoGisRecordViewModel record)
                {
                    return record.Municipality;
                }

                return string.Empty;
            }
        }
    }
}
