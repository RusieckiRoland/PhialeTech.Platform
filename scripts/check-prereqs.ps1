$ErrorActionPreference = "Stop"

function Write-Status($name, $ok, $details) {
    $state = if ($ok) { "OK" } else { "MISSING" }
    Write-Host ("[{0}] {1} - {2}" -f $state, $name, $details)
}

$dotnetPath = "C:\Program Files\dotnet\dotnet.exe"
$hasDotnet = Test-Path $dotnetPath
Write-Status ".NET SDK host" $hasDotnet $dotnetPath

if ($hasDotnet) {
    $sdks = & $dotnetPath --list-sdks
    $runtimes = & $dotnetPath --list-runtimes

    $hasSdk8 = [bool]($sdks -match "^8\.")
    $hasRuntime6 = [bool]($runtimes -match "Microsoft.NETCore.App 6\.")

    Write-Status ".NET SDK 8.x" $hasSdk8 (($sdks -join "; "))
    Write-Status ".NET Runtime 6.x (tests)" $hasRuntime6 (($runtimes -join "; "))
}

$webAssetsNupkg = Join-Path $PSScriptRoot "..\.nuget\local-feed\PhialeGis.WebAssets.1.0.0.nupkg"
$hasWebAssets = Test-Path $webAssetsNupkg
Write-Status "Local package PhialeGis.WebAssets" $hasWebAssets $webAssetsNupkg

$uwpTargets = "C:\Program Files\dotnet\sdk\8.0.418\Microsoft\WindowsXaml\v17.0\Microsoft.Windows.UI.Xaml.CSharp.targets"
$hasUwpTargets = Test-Path $uwpTargets
Write-Status "UWP XAML build targets (required for UWP projects)" $hasUwpTargets $uwpTargets

$vsCodeDir = Join-Path $PSScriptRoot "..\.vscode"
$hasVsCodeTasks = Test-Path (Join-Path $vsCodeDir "tasks.json")
$hasVsCodeExt = Test-Path (Join-Path $vsCodeDir "extensions.json")
Write-Status "VS Code tasks" $hasVsCodeTasks (Join-Path $vsCodeDir "tasks.json")
Write-Status "VS Code extension recommendations" $hasVsCodeExt (Join-Path $vsCodeDir "extensions.json")

if (-not $hasDotnet -or -not $hasWebAssets -or -not $hasUwpTargets) {
    exit 1
}
