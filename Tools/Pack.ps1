param([string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot))
$ErrorActionPreference = 'Stop'
$resolvedRoot = (Resolve-Path -LiteralPath $ProjectRoot).Path
$packagePath = Join-Path $resolvedRoot 'Packages\com.vass.depth-anything'
$manifest = Get-Content -LiteralPath (Join-Path $packagePath 'package.json') -Raw | ConvertFrom-Json
$stage = Join-Path $resolvedRoot ('Builds\PackageStaging-' + [guid]::NewGuid().ToString('N'))
$release = Join-Path $resolvedRoot 'Releases'
New-Item -ItemType Directory -Path $stage,$release -Force | Out-Null
Copy-Item -LiteralPath $packagePath -Destination (Join-Path $stage 'package') -Recurse
$archive = Join-Path $release ($manifest.name + '-' + $manifest.version + '.tgz')
if (Test-Path -LiteralPath $archive) { throw "Release already exists: $archive. Move it aside or bump the version before repacking." }
$tar = Join-Path $env:SystemRoot 'System32\tar.exe'
if (-not (Test-Path -LiteralPath $tar)) { $tar = (Get-Command tar.exe -ErrorAction Stop).Source }
Push-Location -LiteralPath $stage
try {
    # A relative archive path also works with Git's GNU tar, which treats D: as a remote host.
    & $tar -czf (Join-Path '..\..\Releases' ($manifest.name + '-' + $manifest.version + '.tgz')) package
    if ($LASTEXITCODE -ne 0) { throw 'tar failed.' }
} finally { Pop-Location }
Get-FileHash -LiteralPath $archive -Algorithm SHA256
Write-Output "Install from tarball in Unity Package Manager: $archive"
