using System.IO;
using NUnit.Framework;
using PhialeGis.Library.Tests.Support;

namespace PhialeGis.Library.Tests.Interactions
{
    [TestFixture]
    [Category("Architecture")]
    public sealed class ViewportStatusBarPlatformTemplateTests
    {
        [Test]
        public void WpfTemplate_ContainsStatusBarAndTakeoverParts()
        {
            var text = ReadRepositoryFile("src/PhialeGis/Platforms/Wpf/PhialeGis.Library.WpfUi/Themes/Generic.xaml");

            StringAssert.Contains("PART_StatusPrimaryText", text);
            StringAssert.Contains("PART_StatusCoordinateText", text);
            StringAssert.Contains("PART_StatusSnapText", text);
            StringAssert.Contains("PART_TakeoverButton", text);
            StringAssert.Contains("PART_UndoButton", text);
            StringAssert.Contains("PART_FinishButton", text);
            StringAssert.Contains("PART_CancelButton", text);
        }

        [Test]
        public void WinUiTemplate_ContainsStatusBarAndTakeoverParts()
        {
            var text = ReadRepositoryFile("src/PhialeGis/Platforms/WinUI3/PhialeGis.Library.WinUi/Themes/Generic.xaml");

            StringAssert.Contains("PART_StatusPrimaryText", text);
            StringAssert.Contains("PART_StatusCoordinateText", text);
            StringAssert.Contains("PART_StatusSnapText", text);
            StringAssert.Contains("PART_TakeoverButton", text);
            StringAssert.Contains("PART_UndoButton", text);
            StringAssert.Contains("PART_FinishButton", text);
            StringAssert.Contains("PART_CancelButton", text);
        }

        [Test]
        public void AvaloniaTemplate_ContainsStatusBarAndTakeoverParts()
        {
            var text = ReadRepositoryFile("src/PhialeGis/Platforms/Avalonia/PhialeGis.Library.AvaloniaUi/Themes/Generic.axaml");

            StringAssert.Contains("PART_StatusPrimaryText", text);
            StringAssert.Contains("PART_StatusCoordinateText", text);
            StringAssert.Contains("PART_StatusSnapText", text);
            StringAssert.Contains("PART_TakeoverButton", text);
            StringAssert.Contains("PART_UndoButton", text);
            StringAssert.Contains("PART_FinishButton", text);
            StringAssert.Contains("PART_CancelButton", text);
        }

        private static string ReadRepositoryFile(string relativePath)
        {
            var root = RepositoryPaths.GetRepositoryRoot();
            return File.ReadAllText(Path.Combine(root, relativePath));
        }
    }
}
