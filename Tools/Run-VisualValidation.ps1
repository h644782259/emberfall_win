param(
    [string]$UnityPath = 'C:\Program Files\Unity\Hub\Editor\6000.6.3f1\Editor\Unity.exe',
    [switch]$ShowPreview,
    [switch]$UserFixes,
    [switch]$ButtonStyles,
    [switch]$AdventureTypography
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$project = Split-Path -Parent $PSScriptRoot
if (-not (Test-Path -LiteralPath $UnityPath)) { throw 'Specify an installed Unity editor with -UnityPath.' }
$logs = Join-Path $project 'Logs'
New-Item -ItemType Directory -Force -Path $logs | Out-Null
$buildLog = Join-Path $logs 'visual-validation-build.log'
$build = Start-Process -FilePath $UnityPath -ArgumentList @('-batchmode','-quit','-projectPath',('"' + $project + '"'),'-executeMethod','Emberfall.Editor.VisualValidationBuild.Build','-logFile',('"' + $buildLog + '"')) -WindowStyle Hidden -PassThru -Wait
if ($build.ExitCode -ne 0) { throw "Visual validation build failed. See $buildLog" }
$output = Join-Path $project ('Tests\TestResults\Visual-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $output | Out-Null
$player = Join-Path $project 'Builds\VisualValidation\Emberfall.exe'
$playerLog = Join-Path $output 'player.log'
$windowStyle = if ($ShowPreview) { 'Normal' } else { 'Hidden' }
$extraArgs=if($AdventureTypography){@('--adventure-typography')}elseif($ButtonStyles){@('--button-styles')}elseif($UserFixes){@('--user-fixes')}else{@()}
$run = Start-Process -FilePath $player -ArgumentList (@('--visual-validation-root',('"' + $output + '"'),'-logFile',('"' + $playerLog + '"'),'-screen-fullscreen','0')+$extraArgs) -WindowStyle $windowStyle -PassThru
if (-not $run.WaitForExit(210000)) { Stop-Process -Id $run.Id; throw "Visual validation timed out. See $playerLog" }
$reportPath = Join-Path $output 'visual-validation-report.json'
if (-not (Test-Path -LiteralPath $reportPath)) { throw "No visual report was written. See $playerLog" }
$report = Get-Content -LiteralPath $reportPath -Raw | ConvertFrom-Json
if ($run.ExitCode -ne 0 -or $report.status -ne 'PASS') { throw "Visual validation failed: $($report.failure). See $reportPath. If the hidden window has a blank framebuffer, run again with -ShowPreview for a visible review window." }
Write-Host "PASS: $($report.assertions) checks; $($report.screenshots.Count) actual full-frame captures."
Write-Host "Inspect the PNG files in $output"
