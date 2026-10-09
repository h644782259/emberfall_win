param([string]$UnityPath)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
if (-not $UnityPath) {
    $editorRoot = Join-Path ${env:ProgramFiles} 'Unity\Hub\Editor'
    if (Test-Path -LiteralPath $editorRoot) {
        $editor = Get-ChildItem -LiteralPath $editorRoot -Directory |
            Where-Object { $_.Name -match '^(6000\.|2022\.3\.)' } |
            Sort-Object { [version]($_.Name -replace 'f','.') } -Descending |
            Select-Object -First 1
        if ($editor) { $UnityPath = Join-Path $editor.FullName 'Editor\Unity.exe' }
    }
}
if (-not $UnityPath -or -not (Test-Path -LiteralPath $UnityPath)) {
    throw 'Unity 6 editor not found. Install it in Unity Hub, or pass -UnityPath "D:\Unity\Editor\Unity.exe". Windows Build Support (Mono) is required.'
}
$UnityPath = (Resolve-Path -LiteralPath $UnityPath).Path
$logDirectory = Join-Path $projectRoot 'Logs'
New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null
$logFile = Join-Path $logDirectory 'windows-build.log'
if (Test-Path -LiteralPath $logFile) { Remove-Item -LiteralPath $logFile }
$arguments = @('-batchmode', '-quit', '-projectPath', ('"' + $projectRoot + '"'), '-executeMethod', 'Emberfall.Editor.ProjectTools.BuildWindows', '-logFile', ('"' + $logFile + '"'))
Write-Host "Building with $UnityPath"
$process = Start-Process -FilePath $UnityPath -ArgumentList $arguments -WindowStyle Hidden -Wait -PassThru
if ($process.ExitCode -ne 0) {
    if (Test-Path -LiteralPath $logFile) { Get-Content -LiteralPath $logFile -Tail 60 }
    throw "Unity build failed (exit $($process.ExitCode)). Log: $logFile"
}
$gamePath = Join-Path $projectRoot 'Builds\Windows\Emberfall.exe'
if (-not (Test-Path -LiteralPath $gamePath)) { throw "Unity exited without producing the game. See $logFile" }
if (-not (Test-Path -LiteralPath $logFile) -or
    -not (Select-String -LiteralPath $logFile -SimpleMatch 'Emberfall build ready:' -Quiet)) {
    throw "Unity did not confirm a successful fresh build. Refusing to package existing files. See $logFile"
}
Write-Host "Game ready: $gamePath"
Write-Host 'Distribute the entire Builds\Windows folder, including the _Data directory.'
