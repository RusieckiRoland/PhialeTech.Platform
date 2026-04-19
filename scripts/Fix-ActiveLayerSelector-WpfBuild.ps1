param(
    [string]$RepoRoot = "D:\transfuzja\PhialeGis.Library\PhialeGis.Library",
    [string]$ProjectPath = "src\PhialeTech\Products\ActiveLayerSelector\Platforms\Wpf\PhialeTech.ActiveLayerSelector.Wpf",
    [switch]$KillBuildProcesses,
    [switch]$Rebuild
)

$ErrorActionPreference = "Stop"

$projectFullPath = Join-Path $RepoRoot $ProjectPath
$binPath = Join-Path $projectFullPath "bin"
$objPath = Join-Path $projectFullPath "obj"
$csproj = Join-Path $projectFullPath "PhialeTech.ActiveLayerSelector.Wpf.csproj"

Write-Host "RepoRoot: $RepoRoot"
Write-Host "Project : $projectFullPath"

if (-not (Test-Path $projectFullPath)) {
    throw "Project path not found: $projectFullPath"
}

if ($KillBuildProcesses) {
    Write-Host "Stopping build-related processes..."
    $names = @("devenv", "MSBuild", "dotnet", "VBCSCompiler")
    foreach ($name in $names) {
        Get-Process -Name $name -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
    }

    Start-Sleep -Seconds 2
}

function Remove-FolderSafe([string]$path) {
    if (Test-Path $path) {
        Write-Host "Removing: $path"
        attrib -r -s -h "$path\*" /s /d 2>$null | Out-Null
        Remove-Item $path -Recurse -Force -ErrorAction Stop
    }
    else {
        Write-Host "Skip missing: $path"
    }
}

Remove-FolderSafe $binPath
Remove-FolderSafe $objPath

Write-Host "Cleaning stray wpftmp artifacts under project..."
Get-ChildItem -Path $projectFullPath -Recurse -Force -ErrorAction SilentlyContinue |
    Where-Object {
        $_.Name -like "*_wpftmp*" -or
        $_.Name -like "*.GeneratedMSBuildEditorConfig.editorconfig"
    } |
    Remove-Item -Recurse -Force -ErrorAction SilentlyContinue

if ($Rebuild) {
    if (-not (Test-Path $csproj)) {
        throw "Project file not found: $csproj"
    }

    Write-Host "Restoring..."
    dotnet restore $csproj

    Write-Host "Building..."
    dotnet build $csproj -c Debug -f net8.0-windows10.0.19041.0
}

Write-Host "Done."
