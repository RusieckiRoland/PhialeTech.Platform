param(
  [string]$Root,                                # opcjonalny; domyślnie folder skryptu
  [string]$OutFile,
  [switch]$IncludeDesigner,
  [string[]]$ExtraExcludeFolders = @()
)

# --- Ustal Root: folder skryptu -> w przeciwnym razie bieżący katalog
if ([string]::IsNullOrWhiteSpace($Root)) {
  if ($PSScriptRoot -and (Test-Path -LiteralPath $PSScriptRoot)) {
    $Root = $PSScriptRoot
  } else {
    $Root = (Get-Location).Path
  }
}

# --- Domyślna nazwa wyjściowa
if ([string]::IsNullOrWhiteSpace($OutFile)) {
  $leaf = Split-Path -Path $Root -Leaf
  $OutFile = "cs_dump_{0}_{1}.txt" -f $leaf, (Get-Date -Format "yyyyMMdd_HHmmss")
}

# --- Wykluczenia katalogów
$defaultExclude = @("bin","obj",".git",".vs","packages","node_modules")
$excludeFolders = ($defaultExclude + $ExtraExcludeFolders) | Sort-Object -Unique
$excludeRegex = "(\\|/)({0})(\\|/)" -f ($excludeFolders -join "|")

# --- Przygotowanie nagłówka pliku
"# CS DUMP" | Out-File -FilePath $OutFile -Encoding utf8
"# Root: $Root" | Out-File -FilePath $OutFile -Encoding utf8 -Append
"# Excluded folders: $($excludeFolders -join ', ')" | Out-File -FilePath $OutFile -Encoding utf8 -Append
"" | Out-File -FilePath $OutFile -Encoding utf8 -Append

# --- Zbiór plików
$rootItem = Get-Item -LiteralPath $Root -ErrorAction Stop
$rootPath = $rootItem.FullName.TrimEnd('\','/')
$rootUri  = [Uri]($rootPath + [IO.Path]::DirectorySeparatorChar)

$files = Get-ChildItem -LiteralPath $rootPath -Recurse -File -Filter *.cs -ErrorAction SilentlyContinue |
  Where-Object { $_.FullName -notmatch $excludeRegex } |
  Sort-Object FullName

if (-not $IncludeDesigner) {
  $files = $files | Where-Object {
    $name = $_.Name
    $name -notmatch '\.designer\.cs$' -and
    $name -notmatch '\.g\.cs$'       -and
    $name -notmatch '\.g\.i\.cs$'    -and
    $name -ne 'AssemblyInfo.cs'
  }
}

foreach ($f in $files) {
  $fileUri  = [Uri]$f.FullName
  $relPath  = $rootUri.MakeRelativeUri($fileUri).ToString().Replace('/', [IO.Path]::DirectorySeparatorChar)
  $hash     = (Get-FileHash -Algorithm SHA256 -LiteralPath $f.FullName).Hash
  $modified = $f.LastWriteTime.ToString("s")

  "===== BEGIN FILE: $relPath =====" | Out-File -FilePath $OutFile -Encoding utf8 -Append
  "# Length: $($f.Length) bytes | Modified: $modified | SHA256: $hash" | Out-File -FilePath $OutFile -Encoding utf8 -Append
  "" | Out-File -FilePath $OutFile -Encoding utf8 -Append

  Get-Content -LiteralPath $f.FullName -Raw -Encoding UTF8 |
    Out-File -FilePath $OutFile -Encoding utf8 -Append

  "`r`n===== END FILE: $relPath =====`r`n" | Out-File -FilePath $OutFile -Encoding utf8 -Append
}

Write-Host "Done. Output:" (Resolve-Path $OutFile).Path
