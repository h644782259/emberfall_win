#requires -Version 7.0
# Recompiles current scripts for an existing Unity validation player. This is
# native runtime coverage with cached assets; it is not a fresh Unity asset build.
param([string]$Dotnet = "$PSScriptRoot/../Tests/TestResults/Toolchain/dotnet/dotnet.exe",[switch]$LogicOnly,[switch]$ShowPreview)
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
$project=Split-Path -Parent $PSScriptRoot
$basePlayer=Join-Path $project 'Builds/VisualValidation'
if(!(Test-Path -LiteralPath (Join-Path $basePlayer 'Emberfall.exe'))) {throw 'An existing VisualValidation player is required.'}
$output=Join-Path $project ('Tests/TestResults/ManagedPlayer-'+[Guid]::NewGuid().ToString('N'))
$playerRoot=Join-Path $output 'Player'
New-Item -ItemType Directory -Path $playerRoot -Force | Out-Null
Get-ChildItem -LiteralPath $basePlayer | Copy-Item -Destination $playerRoot -Recurse
$managed=Join-Path $playerRoot 'Emberfall_Data/Managed'
$refs=Get-ChildItem -LiteralPath $managed -Filter '*.dll' | Where-Object {$_.Name -ne 'Assembly-CSharp.dll'}
$sources=@(Get-ChildItem -LiteralPath (Join-Path $project 'Assets/Scripts') -Filter '*.cs' -Recurse)+@(Get-ChildItem -LiteralPath (Join-Path $project 'Assets/Tests') -Filter '*.cs' -Recurse)
$sdk=Get-ChildItem -LiteralPath (Join-Path (Split-Path $Dotnet -Parent) 'sdk') -Directory | Select-Object -First 1
$compiler=Join-Path $sdk.FullName 'Roslyn/bincore/csc.dll'
$response=Join-Path $output 'compile.rsp'
$assembly=Join-Path $managed 'Assembly-CSharp.dll'
$args=@('/nostdlib+','/target:library','/langversion:9','/optimize+','/define:UNITY_6000_6;UNITY_6000_6_OR_NEWER;UNITY_STANDALONE;UNITY_STANDALONE_WIN;UNITY_5_3_OR_NEWER;EMBERFALL_VISUAL_VALIDATION',('/out:"'+$assembly+'"'))
$args+=@($refs | ForEach-Object {'/reference:"'+$_.FullName+'"'})
$args+=@($sources | ForEach-Object {'"'+$_.FullName+'"'})
[IO.File]::WriteAllLines($response,$args,[Text.UTF8Encoding]::new($false))
& $Dotnet $compiler ('@'+$response) *> (Join-Path $output 'compile.log')
if($LASTEXITCODE -ne 0){Get-Content -LiteralPath (Join-Path $output 'compile.log');throw 'Current managed-player script compile failed.'}
$windowStyle=if($ShowPreview){'Normal'}else{'Hidden'}
$playerArgs=@('--windows-handoff','--visual-validation-root',('"'+$output+'"'),'-logFile',('"'+(Join-Path $output 'player.log')+'"'),'-screen-fullscreen','0','-force-d3d11')
if($LogicOnly){$playerArgs+='--logic-only'}
$run=Start-Process -FilePath (Join-Path $playerRoot 'Emberfall.exe') -ArgumentList $playerArgs -WindowStyle $windowStyle -PassThru
if(!$run.WaitForExit(210000)){Stop-Process -Id $run.Id;throw "Runtime validation timeout: $output"}
$report=Get-Content -LiteralPath (Join-Path $output 'visual-validation-report.json') -Raw | ConvertFrom-Json
Write-Host "Cached Unity player + current compiled scripts: $output"
if($run.ExitCode -ne 0 -or $report.status -ne 'PASS'){throw "Native managed-player validation failed: $($report.failure)"}
Write-Host "PASS: $($report.assertions) native assertions, $($report.screenshots.Count) framebuffer captures. This is not a fresh Unity asset build."
