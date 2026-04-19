param(
  [string]$Root,                                # optional; defaults to script folder or current dir
  [string]$OutFile,                             # optional; auto-generated if empty
  [ValidateSet('cs','md','csxaml')]
  [string]$Kind,                                # optional; if empty, you'll be prompted (cs/md/csxaml)
  [switch]$IncludeDesigner,                     # applies to cs and csxaml (only for .cs files)
  [string[]]$ExtraExcludeFolders = @()
)

# --- Ask for file kind if not provided (default: csxaml)
if ([string]::IsNullOrWhiteSpace($Kind)) {
  $answer = Read-Host "Which file type to dump? (cs/md/csxaml) [csxaml]"
  if ([string]::IsNullOrWhiteSpace($answer)) { $answer = 'csxaml' }
  if ($answer -notin @('cs','md','csxaml')) {
    Write-Error "Invalid choice '$answer'. Use 'cs', 'md', or 'csxaml'."
    exit 1
  }
  $Kind = $answer
}

# --- Resolve Root: prefer script folder, otherwise current directory
if ([string]::IsNullOrWhiteSpace($Root)) {
  if ($PSScriptRoot -and (Test-Path -LiteralPath $PSScriptRoot)) {
    $Root = $PSScriptRoot
  } else {
    $Root = (Get-Location).Path
  }
}

# --- Default output name
if ([string]::IsNullOrWhiteSpace($OutFile)) {
  $leaf = Split-Path -Path $Root -Leaf
  $stamp = Get-Date -Format "yyyyMMdd_HHmmss"
  $OutFile = "{0}_dump_{1}_{2}.txt" -f $Kind, $leaf, $stamp
}

# --- Excluded folders
$defaultExclude = @("bin","obj",".git",".vs","packages","node_modules")
$excludeFolders = ($defaultExclude + $ExtraExcludeFolders) | Sort-Object -Unique
$excludeRegex = "(\\|/)({0})(\\|/)" -f ($excludeFolders -join "|")

# --- Header
"# $($Kind.ToUpper()) DUMP" | Out-File -FilePath $OutFile -Encoding utf8
"# Root: $Root" | Out-File -FilePath $OutFile -Encoding utf8 -Append
"# Excluded folders: $($excludeFolders -join ', ')" | Out-File -FilePath $OutFile -Encoding utf8 -Append
"" | Out-File -FilePath $OutFile -Encoding utf8 -Append

# --- Prepare paths
$rootItem = Get-Item -LiteralPath $Root -ErrorAction Stop
$rootPath = $rootItem.FullName.TrimEnd('\','/')
$rootUri  = [Uri]($rootPath + [IO.Path]::DirectorySeparatorChar)

# --- Collect files based on kind
$files = @()

switch ($Kind) {
  'md' {
    $files = Get-ChildItem -Path $rootPath -Recurse -File -Filter '*.md' -ErrorAction SilentlyContinue |
      Where-Object { $_.FullName -notmatch $excludeRegex }
  }
  'cs' {
    $files = Get-ChildItem -Path $rootPath -Recurse -File -Filter '*.cs' -ErrorAction SilentlyContinue |
      Where-Object { $_.FullName -notmatch $excludeRegex }
  }
  'csxaml' {
    # Using two provider-side filters and concatenating results (robust across PS versions)
    $cs   = Get-ChildItem -Path $rootPath -Recurse -File -Filter '*.cs'   -ErrorAction SilentlyContinue
    $xaml = Get-ChildItem -Path $rootPath -Recurse -File -Filter '*.xaml' -ErrorAction SilentlyContinue
    $files = @($cs + $xaml) | Where-Object { $_.FullName -notmatch $excludeRegex }
  }
}

# --- Exclude designer/auto-generated only for .cs files (applies to 'cs' and 'csxaml')
if (($Kind -eq 'cs' -or $Kind -eq 'csxaml') -and -not $IncludeDesigner) {
  $files = $files | Where-Object {
    if ($_.Extension -ieq '.cs') {
      $name = $_.Name
      ($name -notmatch '\.designer\.cs$') -and
      ($name -notmatch '\.g\.cs$')       -and
      ($name -notmatch '\.g\.i\.cs$')    -and
      ($name -ne 'AssemblyInfo.cs')
    } else {
      $true  # do not filter out .xaml here
    }
  }
}

# --- Sort files
$files = $files | Sort-Object FullName -Unique

# --- Dump contents
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
