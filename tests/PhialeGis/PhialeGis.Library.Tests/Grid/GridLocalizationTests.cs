using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;
using PhialeGis.Library.Tests.Support;
using PhialeGrid.Localization;

namespace PhialeGis.Library.Tests.Grid
{
    public class GridLocalizationTests
    {
        [Test]
        public void LocalizationCatalog_LoadsEnglishAndPolishTexts()
        {
            var catalog = GridLocalizationCatalog.LoadFromDirectory(GetLanguageDirectory());

            Assert.That(catalog.AvailableLanguageCodes, Does.Contain("en"));
            Assert.That(catalog.AvailableLanguageCodes, Does.Contain("pl"));
            Assert.That(catalog.GetText("en", GridTextKeys.GroupingBandLabel), Is.EqualTo("Group by:"));
            Assert.That(catalog.GetText("en", GridTextKeys.GroupingDropHere), Is.EqualTo("- - Drop column here to group - -"));
            Assert.That(catalog.GetText("pl", GridTextKeys.GroupingDropHere), Is.EqualTo("- - Upuść kolumnę tutaj, aby grupować - -"));
            Assert.That(catalog.GetText("en", GridTextKeys.GroupingExpandAll), Is.EqualTo("Expand all groups"));
            Assert.That(catalog.GetText("pl", GridTextKeys.GroupingCollapseAll), Is.EqualTo("Zwiń wszystkie grupy"));
            Assert.That(catalog.GetText("en", GridTextKeys.EditingPending), Does.Contain("Unsaved edits detected"));
            Assert.That(catalog.GetText("pl", GridTextKeys.EditingPendingWithValidation), Does.Contain("niezapisane zmiany"));
        }

        [Test]
        public void LocalizationCatalog_FallsBackToEnglish_WhenLanguageIsMissing()
        {
            var catalog = GridLocalizationCatalog.LoadFromDirectory(GetLanguageDirectory());

            var text = catalog.GetText("de-DE", GridTextKeys.FilterContains);

            Assert.That(text, Is.EqualTo("Contains"));
        }

        [Test]
        public void LocalizationCatalog_ReturnsKey_WhenTranslationIsMissingEverywhere()
        {
            var catalog = GridLocalizationCatalog.LoadFromDirectory(GetLanguageDirectory());

            var text = catalog.GetText("pl", "custom.missing_key");

            Assert.That(text, Is.EqualTo("custom.missing_key"));
        }

        [Test]
        public void LocalizationCatalog_LoadsAdditionalLanguageFromExternalFile()
        {
            var temporaryDirectory = Path.Combine(Path.GetTempPath(), "PhialeGrid.Localization.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(temporaryDirectory);

            try
            {
                File.Copy(Path.Combine(GetLanguageDirectory(), "en.lang"), Path.Combine(temporaryDirectory, "en.lang"));
                File.Copy(Path.Combine(GetLanguageDirectory(), "pl.lang"), Path.Combine(temporaryDirectory, "pl.lang"));
                File.WriteAllText(
                    Path.Combine(temporaryDirectory, "de.lang"),
                    "grouping.drop_here=Ziehen Sie eine Spalte hierher, um danach zu gruppieren" + Environment.NewLine +
                    "filter.contains=Enthaelt");

                var catalog = GridLocalizationCatalog.LoadFromDirectory(temporaryDirectory);

                Assert.That(catalog.AvailableLanguageCodes, Does.Contain("de"));
                Assert.That(catalog.GetText("de", GridTextKeys.GroupingDropHere), Is.EqualTo("Ziehen Sie eine Spalte hierher, um danach zu gruppieren"));
                Assert.That(catalog.GetText("de", GridTextKeys.SelectionCopy), Is.EqualTo("Copy"));
            }
            finally
            {
                if (Directory.Exists(temporaryDirectory))
                {
                    Directory.Delete(temporaryDirectory, true);
                }
            }
        }

        [Test]
        public void EnglishAndPolishLanguageFiles_ExposeTheSameKeys()
        {
            var catalog = GridLocalizationCatalog.LoadFromDirectory(GetLanguageDirectory());
            var englishKeys = new HashSet<string>(catalog.GetLanguage("en").Keys, StringComparer.Ordinal);
            var polishKeys = new HashSet<string>(catalog.GetLanguage("pl").Keys, StringComparer.Ordinal);

            Assert.That(englishKeys.SetEquals(polishKeys), Is.True);
        }

        [TestCase("src/PhialeTech/Products/Grid/Platforms/Wpf/PhialeGrid.Wpf/PhialeGrid.Wpf.csproj")]
        [TestCase("src/PhialeTech/Products/Grid/Platforms/Avalonia/PhialeGrid.Avalonia/PhialeGrid.Avalonia.csproj")]
        [TestCase("src/PhialeTech/Products/Grid/Platforms/WinUI3/PhialeGrid.WinUI/PhialeGrid.WinUI.csproj")]
        public void GridUiHosts_ReferenceSharedLocalizationProject(string relativeProjectPath)
        {
            var projectPath = Path.Combine(GetRepoRoot(), relativeProjectPath.Replace('/', Path.DirectorySeparatorChar));
            var document = XDocument.Load(projectPath);
            var projectReferences = document.Root
                .Elements()
                .Where(x => x.Name.LocalName == "ItemGroup")
                .Elements()
                .Where(x => x.Name.LocalName == "ProjectReference")
                .Select(x => (string)x.Attribute("Include"))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToArray();
            var contentIncludes = document.Root
                .Elements()
                .Where(x => x.Name.LocalName == "ItemGroup")
                .Elements()
                .Where(x => x.Name.LocalName == "Content")
                .Select(x => (string)x.Attribute("Include"))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToArray();

            Assert.That(projectReferences.Any(x => x.Contains("PhialeGrid.Localization.csproj", StringComparison.Ordinal)), Is.True);
            Assert.That(contentIncludes.Any(x => x.Contains("PhialeGrid.Localization\\Languages\\*.lang", StringComparison.Ordinal)), Is.True);
        }

        private static string GetRepoRoot()
        {
            return RepositoryPaths.GetRepositoryRoot();
        }

        private static string GetLanguageDirectory()
        {
            return Path.Combine(GetRepoRoot(), "src", "PhialeTech", "Products", "Grid", "Localization", "PhialeGrid.Localization", "Languages");
        }
    }
}

