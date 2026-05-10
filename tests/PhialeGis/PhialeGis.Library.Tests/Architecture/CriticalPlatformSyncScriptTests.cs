using System;
using System.IO;
using NUnit.Framework;

namespace PhialeGis.Library.Tests.Architecture;

[TestFixture]
public sealed class CriticalPlatformSyncScriptTests
{
    [Test]
    public void SyncCriticalPlatformScript_MustCopyGitIgnore_WhileKeepingOnlyAdrUnderDocs()
    {
        string scriptPath = Path.Combine(ResolveRepositoryRoot(), "scripts", "Sync-CriticalPlatform.ps1");
        string script = File.ReadAllText(scriptPath);

        Assert.That(script, Does.Not.Contain("if ($normalized -ieq '.gitignore')"),
            ".gitignore must not be excluded because the target is wiped before files are copied back.");

        Assert.That(script, Does.Contain("if ($normalized -like 'Docs\\*')"),
            "Docs filtering rule must remain explicit.");
        Assert.That(script, Does.Contain("return $normalized -ieq 'Docs\\Adr' -or $normalized -like 'Docs\\Adr\\*'"),
            "Only the ADR subtree should be copied from Docs.");
    }

    private static string ResolveRepositoryRoot()
    {
        string current = TestContext.CurrentContext.TestDirectory;
        while (!string.IsNullOrWhiteSpace(current))
        {
            if (File.Exists(Path.Combine(current, "PhialeGis.Library.sln")))
            {
                return current;
            }

            DirectoryInfo parent = Directory.GetParent(current);
            current = parent == null ? null : parent.FullName;
        }

        throw new InvalidOperationException("Could not resolve repository root from test directory.");
    }
}

