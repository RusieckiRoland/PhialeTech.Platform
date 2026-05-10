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
    public static class DemoGisMasterDetailBuilder
    {
        private static readonly IReadOnlyDictionary<string, string> CategoryDescriptions =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["AddressPoint"] = "Address points captured for municipal dispatch and planning workflows.",
                ["Building"] = "Building footprints and structures tracked for cadastral review.",
                ["Hydrant"] = "Hydrant assets monitored for inspection status and operational readiness.",
                ["Parcel"] = "Parcel boundaries used in cadastre, ownership and zoning scenarios.",
                ["PowerLine"] = "Power line geometries with maintenance metadata and operational state.",
                ["Road"] = "Road centerlines used for routing, district maintenance and closures.",
                ["SewerPipe"] = "Sewer pipe segments with condition and intervention status.",
                ["StreetLight"] = "Street light features scheduled for maintenance and ownership review.",
                ["Tree"] = "Tree inventory records with municipality coverage and inspection cadence.",
                ["WaterPipe"] = "Water pipe segments monitored for repairs, length and pressure planning.",
            };

        public static DemoGisHierarchyDefinition Build(DemoFeatureCatalog catalog, int pageSize = 8)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            var records = catalog.GetGisRecords();
            var groupedByCategory = records
                .GroupBy(record => record.Category, StringComparer.OrdinalIgnoreCase)
                .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var childrenByCategory = groupedByCategory.ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<DemoGisRecordViewModel>)group
                    .OrderBy(record => record.ObjectName, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(record => record.ObjectId, StringComparer.OrdinalIgnoreCase)
                    .Select(record => (DemoGisRecordViewModel)record.Clone())
                    .ToArray(),
                StringComparer.OrdinalIgnoreCase);

            var roots = groupedByCategory
                .Select(group => new GridHierarchyNode<object>(
                    BuildCategoryNodeId(group.Key),
                    BuildCategoryMasterRow(group.Key, group.ToArray()),
                    canExpand: true))
                .ToArray();

            var provider = new CategoryHierarchyProvider(childrenByCategory);
            return new DemoGisHierarchyDefinition(roots, new GridHierarchyController<object>(provider, pageSize: pageSize));
        }

        private static Dictionary<string, object> BuildCategoryMasterRow(string category, IReadOnlyList<DemoGisRecordViewModel> records)
        {
            var municipalities = records
                .Select(record => record.Municipality)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();

            var description = CategoryDescriptions.TryGetValue(category ?? string.Empty, out var mapped)
                ? mapped
                : "Operational GIS records available for master-detail inspection.";

            return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["Category"] = category ?? string.Empty,
                ["Description"] = description + " " + records.Count.ToString(CultureInfo.InvariantCulture) + " features across " + municipalities.ToString(CultureInfo.InvariantCulture) + " municipalities.",
                ["ObjectName"] = string.Empty,
                ["ObjectId"] = string.Empty,
                ["GeometryType"] = string.Empty,
                ["Status"] = string.Empty,
            };
        }

        private static string BuildCategoryNodeId(string category)
        {
            if (string.IsNullOrWhiteSpace(category))
            {
                return "category";
            }

            return category
                .Trim()
                .ToLowerInvariant()
                .Replace(" ", "-");
        }

        private sealed class CategoryHierarchyProvider : IGridHierarchyPagingProvider<object>
        {
            private readonly IReadOnlyDictionary<string, IReadOnlyList<DemoGisRecordViewModel>> _childrenByCategory;

            public CategoryHierarchyProvider(IReadOnlyDictionary<string, IReadOnlyList<DemoGisRecordViewModel>> childrenByCategory)
            {
                _childrenByCategory = childrenByCategory ?? throw new ArgumentNullException(nameof(childrenByCategory));
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
                var category = ExtractCategory(parent);
                if (!_childrenByCategory.TryGetValue(category, out var records))
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

            private static string ExtractCategory(GridHierarchyNode<object> parent)
            {
                if (parent?.Item is IReadOnlyDictionary<string, object> dictionary &&
                    dictionary.TryGetValue("Category", out var category))
                {
                    return Convert.ToString(category, CultureInfo.InvariantCulture) ?? string.Empty;
                }

                if (parent?.Item is IDictionary<string, object> mutableDictionary &&
                    mutableDictionary.TryGetValue("Category", out var mutableCategory))
                {
                    return Convert.ToString(mutableCategory, CultureInfo.InvariantCulture) ?? string.Empty;
                }

                return string.Empty;
            }
        }
    }
}

