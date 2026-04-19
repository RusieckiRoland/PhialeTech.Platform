using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using PhialeTech.Components.Shared.Model;

namespace PhialeTech.Components.Shared.Services
{
    public static class DemoGisDataLoader
    {
        private const string ResourceFileName = "gis-grid-demo-530-regenerated.xml";

        public static IReadOnlyList<DemoGisRecordViewModel> LoadDefaultRecords()
        {
            var assembly = typeof(DemoGisDataLoader).GetTypeInfo().Assembly;
            var resourceName = assembly.GetManifestResourceNames()
                .FirstOrDefault(name => name.EndsWith(ResourceFileName, StringComparison.OrdinalIgnoreCase));

            if (resourceName == null)
            {
                throw new InvalidOperationException("Embedded GIS demo data was not found.");
            }

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    throw new InvalidOperationException("Unable to open embedded GIS demo data.");
                }

                return LoadRecords(stream);
            }
        }

        private static IReadOnlyList<DemoGisRecordViewModel> LoadRecords(Stream stream)
        {
            var document = XDocument.Load(stream, LoadOptions.None);
            return document.Root?
                .Elements("Record")
                .Select(MapRecord)
                .ToArray()
                ?? Array.Empty<DemoGisRecordViewModel>();
        }

        private static DemoGisRecordViewModel MapRecord(XElement element)
        {
            return new DemoGisRecordViewModel(
                ReadText(element, "ObjectId"),
                ReadText(element, "Category"),
                ReadText(element, "ObjectName"),
                ReadText(element, "GeometryType"),
                ReadText(element, "CRS"),
                ReadText(element, "Municipality"),
                ReadText(element, "District"),
                ReadText(element, "Status"),
                ReadDecimal(element, "Area_m2"),
                ReadDecimal(element, "Length_m"),
                ReadDate(element, "LastInspection"),
                ReadText(element, "Source"),
                ReadText(element, "Priority"),
                ReadBoolean(element, "Visible"),
                ReadBoolean(element, "Editable"),
                ReadText(element, "Owner"),
                ReadInt32(element, "ScaleHint"),
                ReadText(element, "Tags"));
        }

        private static string ReadText(XElement element, string childName)
        {
            return (string)element.Element(childName) ?? string.Empty;
        }

        private static decimal ReadDecimal(XElement element, string childName)
        {
            var raw = ReadText(element, childName);
            return decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var value) ? value : 0m;
        }

        private static int ReadInt32(XElement element, string childName)
        {
            var raw = ReadText(element, childName);
            return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : 0;
        }

        private static bool ReadBoolean(XElement element, string childName)
        {
            var raw = ReadText(element, childName);
            return bool.TryParse(raw, out var value) && value;
        }

        private static DateTime ReadDate(XElement element, string childName)
        {
            var raw = ReadText(element, childName);
            return DateTime.TryParseExact(raw, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var value)
                ? value
                : DateTime.MinValue;
        }
    }
}

