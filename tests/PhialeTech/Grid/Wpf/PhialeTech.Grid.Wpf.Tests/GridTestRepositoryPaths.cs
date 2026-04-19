using System;
using System.IO;
using NUnit.Framework;

namespace PhialeGrid.Wpf.Tests
{
    internal static class GridTestRepositoryPaths
    {
        private static readonly Lazy<string> RepositoryRootPath = new Lazy<string>(FindRepositoryRoot);

        public static string RepositoryRoot => RepositoryRootPath.Value;

        public static string GridLanguagesDirectory =>
            Path.Combine(RepositoryRoot, "src", "PhialeTech", "Products", "Grid", "Localization", "PhialeGrid.Localization", "Languages");

        public static string GridMockServerJsDirectory =>
            Path.Combine(RepositoryRoot, "src", "PhialeTech", "Products", "Grid", "MockServer", "Js");

        private static string FindRepositoryRoot()
        {
            var current = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (current != null)
            {
                if (File.Exists(Path.Combine(current.FullName, "PhialeTech.Components.sln")))
                {
                    return current.FullName;
                }

                current = current.Parent;
            }

            throw new DirectoryNotFoundException("Could not locate repository root from test output directory.");
        }
    }
}
