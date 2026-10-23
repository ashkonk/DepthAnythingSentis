param([string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot))
$ErrorActionPreference = 'Stop'
$resolvedRoot = (Resolve-Path -LiteralPath $ProjectRoot).Path
$manifestPath = Join-Path $resolvedRoot 'Packages\manifest.json'
$sampleSource = Join-Path $resolvedRoot 'Packages\com.vass.depth-anything\Samples~\BodyPix'
$sampleTarget = Join-Path $resolvedRoot 'Assets\BodyPixDemo'
if (-not (Test-Path -LiteralPath $sampleSource)) { throw 'Run this script against the included example project.' }
if (Test-Path -LiteralPath $sampleTarget) { throw 'Assets/BodyPixDemo already exists. Use Package Manager to manage the existing sample.' }
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
$manifest.dependencies | Add-Member -NotePropertyName 'jp.keijiro.bodypix' -NotePropertyValue '5.0.0' -Force
$registries = @()
if ($manifest.PSObject.Properties.Name -contains 'scopedRegistries') { $registries = @($manifest.scopedRegistries) }
$registry = $registries | Where-Object { $_.url -eq 'https://registry.npmjs.org' } | Select-Object -First 1
if ($null -eq $registry) {
    $registries += [pscustomobject]@{ name='Keijiro'; url='https://registry.npmjs.org'; scopes=@('jp.keijiro.bodypix','jp.keijiro.klak.nnutils') }
} else {
    $registry.scopes = @(@($registry.scopes) + @('jp.keijiro.bodypix','jp.keijiro.klak.nnutils') | Select-Object -Unique)
}
$manifest | Add-Member -NotePropertyName 'scopedRegistries' -NotePropertyValue $registries -Force
# Copy only into a new folder, preserving the references in the supplied sample scene.
Copy-Item -LiteralPath $sampleSource -Destination $sampleTarget -Recurse
$manifest | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $manifestPath -Encoding UTF8
Write-Output 'Open the project in Unity, allow package resolution, then open Assets/BodyPixDemo/BodyPixDemo.unity.'
