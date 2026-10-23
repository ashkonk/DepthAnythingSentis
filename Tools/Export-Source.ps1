param([string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot))
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path -LiteralPath $ProjectRoot).Path
$manifest = Get-Content -LiteralPath (Join-Path $root 'Packages/com.vass.depth-anything/package.json') -Raw | ConvertFrom-Json
$stage = Join-Path $root ('Builds\SourceStaging-' + [guid]::NewGuid().ToString('N'))
$release = Join-Path $root 'Releases'
New-Item -ItemType Directory -Path $stage,$release -Force | Out-Null
foreach ($name in @('Assets','Packages','ProjectSettings','Documentation','Tools','README.md','LICENSE','.gitignore','.gitattributes')) {
    Copy-Item -LiteralPath (Join-Path $root $name) -Destination (Join-Path $stage $name) -Recurse
}
$zip = Join-Path $release ('DepthAnythingUnity-source-' + $manifest.version + '.zip')
if (Test-Path -LiteralPath $zip) { throw "Source release already exists: $zip" }
Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory($stage, $zip, [System.IO.Compression.CompressionLevel]::Optimal, $false)
Get-FileHash -LiteralPath $zip -Algorithm SHA256
Write-Output "Source project (without Library, builds or machine-local state): $zip"
