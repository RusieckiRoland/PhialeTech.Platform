using System.Collections.Generic;
using System.Linq;

namespace PhialeTech.Components.Shared.Model
{
    public sealed class DemoSectionViewModel
    {
        public const int DefaultColumnsPerRow = 3;

        public DemoSectionViewModel(string key, string title, IEnumerable<DemoExampleCardViewModel> examples)
        {
            Key = key;
            Title = title;
            Examples = examples.ToArray();
            Rows = DemoSectionLayout.BuildRows(Examples, DefaultColumnsPerRow);
        }

        public string Key { get; }

        public string Title { get; }

        public IReadOnlyList<DemoExampleCardViewModel> Examples { get; }

        public IReadOnlyList<DemoSectionRowViewModel> Rows { get; }
    }
}

