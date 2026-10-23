param(
    [string]$Unity = 'C:\Program Files\Unity\Hub\Editor\6000.6.3f1\Editor\Unity.exe',
    [switch]$BodyPix
)
$ErrorActionPreference = 'Stop'
$project = (Resolve-Path -LiteralPath (Split-Path -Parent $PSScriptRoot)).Path
$variant = if ($BodyPix) { 'BodyPix' } else { 'Basic' }
$method = if ($BodyPix) { 'DepthPackageTools.BuildBodyPix' } else { 'DepthPackageTools.BuildBasic' }
$name = if ($BodyPix) { 'BodyPixDemo.exe' } else { 'DepthDemo.exe' }
$log = Join-Path $project ('Validation\build-' + $variant.ToLowerInvariant() + '.log')
New-Item -ItemType Directory -Path (Join-Path $project 'Validation') -Force | Out-Null
$editorArguments = "-batchmode -quit -projectPath `"$project`" -force-d3d11 -executeMethod $method -logFile `"$log`""
$build = Start-Process -FilePath $Unity -ArgumentList $editorArguments -PassThru -Wait -WindowStyle Hidden
if ($build.ExitCode -ne 0) { throw "Unity build failed; inspect $log" }
$output = Join-Path $project ('Validation\' + $variant.ToLowerInvariant())
$executable = Join-Path $project ("Builds\$variant\$name")
$arguments = "-screen-fullscreen 0 -screen-width 1440 -screen-height 900 -depthSmoke `"$output`" -logFile `"$output-player.log`""
$player = Start-Process -FilePath $executable -ArgumentList $arguments -PassThru -WindowStyle Hidden
if (-not $player.WaitForExit(180000)) {
    # This script owns this exact process. Never terminate another Editor/player.
    Stop-Process -Id $player.Id
    throw 'Smoke player timed out after three minutes.'
}
Get-Content -LiteralPath (Join-Path $output 'results.txt')
if ($player.ExitCode -ne 0) { throw 'Smoke validation failed.' }
