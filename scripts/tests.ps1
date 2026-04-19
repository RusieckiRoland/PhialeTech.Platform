param(
    [ValidateSet("all", "unit", "integration", "migration")]
    [string]$Suite = "all",
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug"
)

$project = Join-Path $PSScriptRoot "..\PhialeGis.Library.Tests\PhialeGis.Library.Tests.csproj"
$project = (Resolve-Path $project).Path

$args = @(
    "test",
    $project,
    "-c", $Configuration
)

$dotnetCmd = Get-Command dotnet -ErrorAction SilentlyContinue
$dotnet = if ($dotnetCmd) { $dotnetCmd.Source } else { $null }
if (-not $dotnet) {
    $fallback = "C:\Program Files\dotnet\dotnet.exe"
    if (Test-Path $fallback) {
        $dotnet = $fallback
    }
}

if (-not $dotnet) {
    throw "dotnet was not found in PATH and fallback path does not exist."
}

switch ($Suite) {
    "unit" {
        $args += @("--filter", "TestCategory=Unit")
    }
    "integration" {
        $args += @("--filter", "TestCategory=Integration")
    }
    "migration" {
        $args += @("--filter", "TestCategory=MigrationGuard")
    }
}

& $dotnet @args
exit $LASTEXITCODE
