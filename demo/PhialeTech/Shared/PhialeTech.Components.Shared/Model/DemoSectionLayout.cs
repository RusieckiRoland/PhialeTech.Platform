using System;
using System.Collections.Generic;
using System.Linq;

namespace PhialeTech.Components.Shared.Model
{
    public static class DemoSectionLayout
    {
        public static IReadOnlyList<DemoSectionRowViewModel> BuildRows(IEnumerable<DemoExampleCardViewModel> examples, int columnsPerRow)
        {
            if (examples == null)
            {
                throw new ArgumentNullException(nameof(examples));
            }

            if (columnsPerRow <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(columnsPerRow), columnsPerRow, "Columns per row must be greater than zero.");
            }

            var cards = examples.ToArray();
            if (cards.Length == 0)
            {
                return Array.Empty<DemoSectionRowViewModel>();
            }

            var rows = new List<DemoSectionRowViewModel>();
            for (var index = 0; index < cards.Length; index += columnsPerRow)
            {
                rows.Add(new DemoSectionRowViewModel(cards.Skip(index).Take(columnsPerRow)));
            }

            return rows;
        }
    }
}
