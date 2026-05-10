using System.Linq;
using NUnit.Framework;
using PhialeGrid.Core.Clipboard;
using PhialeGrid.Core.Export;
using PhialeGis.Library.Tests.Grid.Support;

namespace PhialeGis.Library.Tests.Grid
{
    public class GridClipboardAndCsvTests
    {
        [Test]
        public void ClipboardCodec_RoundTrip()
        {
            var encoded = GridClipboardCodec.Encode(new[]
            {
                new[] { "A", "B" },
                new[] { "1", "2" },
            });

            var decoded = GridClipboardCodec.Decode(encoded);
            Assert.That(decoded.Count, Is.EqualTo(2));
            Assert.That(decoded[1][1], Is.EqualTo("2"));
        }

        [Test]
        public void CsvExportImport_RoundTripBasicData()
        {
            var rows = new[]
            {
                new TestRow { Id = "1", Name = "Alice", City = "Warsaw" },
                new TestRow { Id = "2", Name = "Bob", City = "Berlin" },
            };

            var csv = GridCsvExporter.Export(rows, new[] { "Id", "Name", "City" }, (row, col) =>
            {
                switch (col)
                {
                    case "Id": return row.Id;
                    case "Name": return row.Name;
                    case "City": return row.City;
                    default: return null;
                }
            });

            var imported = GridCsvImporter.Import(csv);
            Assert.That(imported.Count, Is.EqualTo(2));
            Assert.That(imported[0]["Name"], Is.EqualTo("Alice"));
            Assert.That(imported[1]["City"], Is.EqualTo("Berlin"));
            Assert.That(csv.Split('\n').First(), Is.EqualTo("Id,Name,City"));
        }
    }
}

