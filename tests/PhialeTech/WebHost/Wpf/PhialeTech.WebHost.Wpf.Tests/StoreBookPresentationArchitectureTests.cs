using System.IO;
using NUnit.Framework;

namespace PhialeTech.WebHost.Wpf.Tests
{
    public sealed class StoreBookPresentationArchitectureTests
    {
        [Test]
        public void StoryBookBranding_IsUpdated_ToStoreBook_AndPhialeTechComponentShowcase()
        {
            var english = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "demo", "PhialeTech", "Shared", "PhialeTech.Components.Shared", "Languages", "en.lang"));
            var polish = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "demo", "PhialeTech", "Shared", "PhialeTech.Components.Shared", "Languages", "pl.lang"));

            Assert.That(english, Does.Contain("App.Title=PhialeTech Store book"));
            Assert.That(english, Does.Contain("App.Subtitle=PhialeTech component showcase"));
            Assert.That(english, Does.Not.Contain("GIS component showcase"));

            Assert.That(polish, Does.Contain("App.Title=PhialeTech Store book"));
            Assert.That(polish, Does.Contain("App.Subtitle=Pakiet komponentów PhialeTech"));
            Assert.That(polish, Does.Not.Contain("Prezentacja komponentów GIS"));
        }

        [Test]
        public void DemoStyles_DefineAnImplicitStyle_ForPhialeWebComponentScrollHost()
        {
            var code = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "src", "PhialeTech", "Shared", "PhialeTech.Styles.Wpf", "Themes", "Demo.Controls.xaml"));

            Assert.That(code, Does.Contain("xmlns:webHost=\"clr-namespace:PhialeTech.WebHost.Wpf.Controls;assembly=PhialeTech.WebHost.Wpf\""));
            Assert.That(code, Does.Contain("TargetType=\"{x:Type webHost:PhialeWebComponentScrollHost}\""));
            Assert.That(code, Does.Contain("Setter Property=\"Background\""));
            Assert.That(code, Does.Contain("Setter Property=\"BorderBrush\""));
            Assert.That(code, Does.Contain("Setter Property=\"BorderThickness\""));
            Assert.That(code, Does.Contain("Setter Property=\"CornerRadius\""));
        }

        [Test]
        public void ScrollHost_ExposesStyleDrivenChromeProperties()
        {
            var code = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "src", "PhialeTech", "Shared", "Platforms", "Wpf", "PhialeTech.WebHost.Wpf", "Controls", "PhialeWebComponentScrollHost.cs"));

            Assert.That(code, Does.Contain("CornerRadiusProperty"));
            Assert.That(code, Does.Contain("nameof(CornerRadius)"));
            Assert.That(code, Does.Contain("SetBinding(Border.BackgroundProperty"));
            Assert.That(code, Does.Contain("SetBinding(Border.BorderBrushProperty"));
            Assert.That(code, Does.Contain("SetBinding(Border.BorderThicknessProperty"));
            Assert.That(code, Does.Contain("SetBinding(Border.CornerRadiusProperty"));
        }

        [Test]
        public void WebComponents_HasAnExplanationTab_ForTheScrollHost()
        {
            var xaml = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "src", "PhialeTech", "Shared", "PhialeTech.Styles.Wpf", "Themes.Linked", "Demo", "PhialeTech.Components.Wpf.MainWindow.xaml"));
            var viewModel = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "demo", "PhialeTech", "Shared", "PhialeTech.Components.Shared", "ViewModels", "DemoShellViewModel.cs"));

            Assert.That(xaml, Does.Contain("Header=\"{Binding ExplanationTabText}\""));
            Assert.That(xaml, Does.Contain("Visibility=\"{Binding ShowWebComponentsExplanationTab"));
            Assert.That(viewModel, Does.Contain("public string ExplanationTabText => Localize(DemoTextKeys.ShellExplanationTab);"));
            Assert.That(viewModel, Does.Contain("public bool ShowWebComponentsExplanationTab => IsWebComponentsExample;"));
            Assert.That(viewModel, Does.Contain("WebComponentsScrollHostTitle"));
            Assert.That(viewModel, Does.Contain("WebComponentsScrollHostDescription"));
        }

        [Test]
        public void WebComponents_OverviewIncludes_ASeparateScrollHostComponentCard_AndDetailSurface()
        {
            var featureCatalog = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "demo", "PhialeTech", "Shared", "PhialeTech.Components.Shared", "Services", "DemoFeatureCatalog.cs"));
            var textKeys = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "demo", "PhialeTech", "Shared", "PhialeTech.Components.Shared", "Localization", "DemoTextKeys.cs"));
            var english = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "demo", "PhialeTech", "Shared", "PhialeTech.Components.Shared", "Languages", "en.lang"));
            var polish = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "demo", "PhialeTech", "Shared", "PhialeTech.Components.Shared", "Languages", "pl.lang"));
            var viewModel = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "demo", "PhialeTech", "Shared", "PhialeTech.Components.Shared", "ViewModels", "DemoShellViewModel.cs"));
            var xaml = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "src", "PhialeTech", "Shared", "PhialeTech.Styles.Wpf", "Themes.Linked", "Demo", "PhialeTech.Components.Wpf.MainWindow.xaml"));
            var snippets = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "demo", "PhialeTech", "Shared", "PhialeTech.Components.Shared", "Services", "DemoCodeSnippetBuilder.cs"));

            Assert.That(featureCatalog, Does.Contain("new DemoExampleDefinition(\"web-component-scroll-host\", \"web-components\""));
            Assert.That(textKeys, Does.Contain("ExampleWebComponentScrollHostTitle"));
            Assert.That(textKeys, Does.Contain("ExampleWebComponentScrollHostDescription"));
            Assert.That(english, Does.Contain("Example.WebComponentScrollHost.Title="));
            Assert.That(english, Does.Contain("Example.WebComponentScrollHost.Description="));
            Assert.That(polish, Does.Contain("Example.WebComponentScrollHost.Title="));
            Assert.That(polish, Does.Contain("Example.WebComponentScrollHost.Description="));
            Assert.That(viewModel, Does.Contain("public bool IsWebComponentScrollHostExample => SelectedExample != null && string.Equals(SelectedExample.Id, \"web-component-scroll-host\", StringComparison.OrdinalIgnoreCase);"));
            Assert.That(xaml, Does.Contain("Visibility=\"{Binding IsWebComponentScrollHostExample, Converter={StaticResource BooleanToVisibilityConverter}}\""));
            Assert.That(snippets, Does.Contain("string.Equals(exampleId, \"web-component-scroll-host\", StringComparison.OrdinalIgnoreCase)"));
        }

        private static string GetRepositoryRoot()
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "src")))
            {
                directory = directory.Parent;
            }

            if (directory == null)
            {
                throw new DirectoryNotFoundException("Repository root could not be located from the test output directory.");
            }

            return directory.FullName;
        }
    }
}
