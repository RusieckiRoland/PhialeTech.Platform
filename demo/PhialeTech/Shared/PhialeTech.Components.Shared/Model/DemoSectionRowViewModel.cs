using System.Collections.Generic;
using System.Linq;

namespace PhialeTech.Components.Shared.Model
{
    public sealed class DemoSectionRowViewModel
    {
        public DemoSectionRowViewModel(IEnumerable<DemoExampleCardViewModel> examples)
        {
            Examples = examples.ToArray();
        }

        public IReadOnlyList<DemoExampleCardViewModel> Examples { get; }
    }
}

