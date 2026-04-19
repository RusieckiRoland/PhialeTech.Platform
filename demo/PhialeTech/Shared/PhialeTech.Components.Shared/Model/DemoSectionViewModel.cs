using System.Collections.Generic;
using System.Linq;

namespace PhialeTech.Components.Shared.Model
{
    public sealed class DemoSectionViewModel
    {
        public DemoSectionViewModel(string key, string title, IEnumerable<DemoExampleCardViewModel> examples)
        {
            Key = key;
            Title = title;
            Examples = examples.ToArray();
        }

        public string Key { get; }

        public string Title { get; }

        public IReadOnlyList<DemoExampleCardViewModel> Examples { get; }
    }
}
