using System;
using System.Collections.Generic;
using System.Linq;
using PhialeTech.Components.Shared.Model;

namespace PhialeTech.Components.Shared.Services
{
    public sealed class DemoPdfBookPlan
    {
        public DemoPdfBookPlan(IReadOnlyList<DemoPdfBookChapterPlan> chapters)
        {
            Chapters = chapters ?? Array.Empty<DemoPdfBookChapterPlan>();
        }

        public IReadOnlyList<DemoPdfBookChapterPlan> Chapters { get; }
    }

    public sealed class DemoPdfBookChapterPlan
    {
        public DemoPdfBookChapterPlan(
            string drawerGroupId,
            string title,
            string description,
            IReadOnlyList<DemoExampleDefinition> examples)
        {
            DrawerGroupId = drawerGroupId ?? string.Empty;
            Title = title ?? string.Empty;
            Description = description ?? string.Empty;
            Examples = examples ?? Array.Empty<DemoExampleDefinition>();
        }

        public string DrawerGroupId { get; }

        public string Title { get; }

        public string Description { get; }

        public IReadOnlyList<DemoExampleDefinition> Examples { get; }
    }

    public static class DemoPdfBookPlanBuilder
    {
        public static DemoPdfBookPlan Build(DemoFeatureCatalog catalog, string languageCode)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            var selectedLanguageCode = string.IsNullOrWhiteSpace(languageCode) ? "en" : languageCode.Trim();
            var drawerGroups = catalog.BuildDrawerGroups(selectedLanguageCode, selectedDrawerGroupId: string.Empty);
            var chapters = drawerGroups
                .Select(group => new DemoPdfBookChapterPlan(
                    group.Id,
                    group.Title,
                    group.Description,
                    catalog.GetExamplesByDrawerGroupId(group.Id)
                        .OrderBy(example => example.SectionOrder)
                        .ThenBy(example => example.DisplayOrder)
                        .ToArray()))
                .Where(chapter => chapter.Examples.Count > 0)
                .ToArray();

            return new DemoPdfBookPlan(chapters);
        }
    }
}

