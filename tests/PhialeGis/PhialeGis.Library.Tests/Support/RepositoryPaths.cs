using System;
using System.IO;
using NUnit.Framework;

namespace PhialeGis.Library.Tests.Support;

internal static class RepositoryPaths
{
    public static string GetRepositoryRoot()
    {
        string current = TestContext.CurrentContext.TestDirectory;
        while (!string.IsNullOrWhiteSpace(current))
        {
            if (File.Exists(Path.Combine(current, "PhialeGis.Library.sln")))
            {
                return current;
            }

            DirectoryInfo parent = Directory.GetParent(current);
            if (parent is null)
            {
                break;
            }

            current = parent.FullName;
        }

        throw new InvalidOperationException("Could not resolve repository root.");
    }
}
