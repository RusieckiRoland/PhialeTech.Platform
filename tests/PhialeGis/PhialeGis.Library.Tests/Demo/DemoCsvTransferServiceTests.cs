using System.Linq;
using NUnit.Framework;
using PhialeTech.Components.Shared.Services;
using PhialeTech.Components.Shared.ViewModels;

namespace PhialeGis.Library.Tests.Demo
{
    [TestFixture]
    public sealed class DemoCsvTransferServiceTests
    {
        [Test]
        public void CsvTransferService_ShouldRoundTripLocalizedCatalogRows()
        {
            var viewModel = new DemoShellViewModel("Wpf");
            viewModel.LanguageCode = "pl";
            viewModel.SelectExample("export-import");

            var sourceRows = viewModel.GridRecords.Take(6).ToArray();
            var csv = DemoGisCsvTransferService.Export(sourceRows, viewModel.GridColumns);
            var importedRows = DemoGisCsvTransferService.Import(csv, viewModel.GridColumns);

            Assert.Multiple(() =>
            {
                Assert.That(csv, Does.Contain("Kategoria"));
                Assert.That(csv, Does.Contain("Nazwa obiektu"));
                Assert.That(importedRows.Count, Is.EqualTo(sourceRows.Length));
                Assert.That(importedRows[0].ObjectId, Is.EqualTo(sourceRows[0].ObjectId));
                Assert.That(importedRows[0].ObjectName, Is.EqualTo(sourceRows[0].ObjectName));
                Assert.That(importedRows[0].Municipality, Is.EqualTo(sourceRows[0].Municipality));
                Assert.That(importedRows[0].AreaSquareMeters, Is.EqualTo(sourceRows[0].AreaSquareMeters));
                Assert.That(importedRows[0].LastInspection, Is.EqualTo(sourceRows[0].LastInspection.Date));
            });
        }

        [Test]
        public void ViewModel_ShouldExposeExportImportScenario_WithTransferSurface()
        {
            var viewModel = new DemoShellViewModel("Wpf");

            viewModel.SelectExample("export-import");

            Assert.Multiple(() =>
            {
                Assert.That(viewModel.ShowTransferTools, Is.True);
                Assert.That(viewModel.ExportCsvText, Is.Not.Empty);
                Assert.That(viewModel.ImportSampleCsvText, Is.Not.Empty);
                Assert.That(viewModel.RestoreDataText, Is.Not.Empty);
                Assert.That(viewModel.TransferStatusText, Does.Contain("Export"));
                Assert.That(viewModel.TransferPreviewText, Is.Empty);
            });
        }

        [Test]
        public void FeatureCatalog_ShouldRenderExportImportCodeSamplesWithRealCsvHostWiring()
        {
            var catalog = new DemoFeatureCatalog();

            var files = catalog.GetCodeFiles("Wpf", "export-import");

            Assert.Multiple(() =>
            {
                Assert.That(files.Any(file => file.Text.Contains("DemoGrid.ExportCurrentViewToCsv()")), Is.True);
                Assert.That(files.Any(file => file.Text.Contains("BuildSampleImportCsv")), Is.True);
                Assert.That(files.Any(file => file.Text.Contains("DemoGisCsvTransferService.Import")), Is.True);
                Assert.That(files.Any(file => file.Text.Contains("TransferPreviewText")), Is.True);
                Assert.That(files.Any(file => file.Text.Contains("HandleRestoreSourceClick")), Is.True);
            });
        }
    }
}
