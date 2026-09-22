param(
    [string]$GameDir = 'C:\Program Files (x86)\Steam\steamapps\common\Timberborn',
    [string]$OutputDir = (Join-Path $PSScriptRoot 'dist')
)
$ErrorActionPreference = 'Stop'
if (!(Test-Path -LiteralPath (Join-Path $GameDir 'Timberborn_Data\Managed\Timberborn.BuildingsNavigation.dll'))) {
    throw 'Timberborn game assemblies were not found. Supply -GameDir.'
}
dotnet build (Join-Path $PSScriptRoot 'source\PersistentWorkAreas.csproj') -c Release "-p:GameDir=$GameDir" -v minimal
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
$dll = Join-Path $PSScriptRoot 'source\bin\Release\netstandard2.1\PersistentWorkAreas.dll'
$assets = Join-Path $PSScriptRoot 'packaging\PersistentWorkAreas\version-1.1'
dotnet run --project (Join-Path $PSScriptRoot 'tests\Checks.csproj') -c Release -- $GameDir $dll $assets
if ($LASTEXITCODE -ne 0) { throw 'Validation failed.' }
$modVersion = (Get-Content -Raw -LiteralPath (Join-Path $assets 'manifest.json') | ConvertFrom-Json).Version
$package = Join-Path $OutputDir 'PersistentWorkAreas'
$version = Join-Path $package 'version-1.1'
# Start clean so files left over from an earlier build never reach the zip.
if (Test-Path -LiteralPath $package) { Remove-Item -LiteralPath $package -Recurse -Force }
New-Item -ItemType Directory -Force $version | Out-Null
Copy-Item -Path (Join-Path $assets '*') -Destination $version -Recurse -Force
Copy-Item -LiteralPath $dll -Destination $version -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'README.md') -Destination $package -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'VALIDATION.md') -Destination $package -Force
$package = (Get-Item -LiteralPath $package).FullName
$zip = Join-Path (Split-Path -Parent $package) "PersistentWorkAreas-v$modVersion.zip"
if (Test-Path -LiteralPath $zip) { Remove-Item -LiteralPath $zip -Force }
# Compress-Archive in Windows PowerShell writes '\' into entry names, which macOS and
# Linux extract as flat file names. Write standard '/' entry names instead.
Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem
$archive = [System.IO.Compression.ZipFile]::Open($zip, 'Create')
try {
    Get-ChildItem -LiteralPath $package -Recurse -File | ForEach-Object {
        $entry = 'PersistentWorkAreas/' + $_.FullName.Substring($package.Length + 1).Replace('\', '/')
        [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $_.FullName, $entry) | Out-Null
    }
}
finally { $archive.Dispose() }
Get-FileHash -LiteralPath $zip -Algorithm SHA256
