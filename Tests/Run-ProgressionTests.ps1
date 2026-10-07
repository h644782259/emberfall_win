#requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$projectDirectory = Split-Path -Parent $PSScriptRoot
$testDirectory = Join-Path ([IO.Path]::GetTempPath()) ('EmberfallProgression-' + [Guid]::NewGuid().ToString('N'))
try {
    Add-Type -Path @(
        (Join-Path $projectDirectory 'Assets/Scripts/Core/GameTypes.cs'),
        (Join-Path $projectDirectory 'Assets/Scripts/Core/ProgressionService.cs'),
        (Join-Path $projectDirectory 'Assets/Scripts/Core/CombatBalance.cs'),
        (Join-Path $projectDirectory 'Assets/Scripts/Core/HubTravelRules.cs'),
        (Join-Path $projectDirectory 'Assets/Scripts/Core/MasteryCoreRuntime.cs'),
        (Join-Path $projectDirectory 'Assets/Scripts/Core/CastFirstHitReceipt.cs'),
        (Join-Path $projectDirectory 'Assets/Scripts/Core/TierRewardRules.cs'),
        (Join-Path $projectDirectory 'Assets/Scripts/Core/TierRewardBand.cs'),
        (Join-Path $projectDirectory 'Assets/Scripts/Core/ProgressionGoalState.cs'),
        (Join-Path $projectDirectory 'Assets/Scripts/Core/ChapterProgression.cs'),
        (Join-Path $projectDirectory 'Assets/Scripts/Core/ProgressionService.Chapter.cs'),
        (Join-Path $projectDirectory 'Assets/Scripts/Core/ProgressionService.Reforge.cs'),
        (Join-Path $projectDirectory 'Assets/Scripts/Core/ReforgeQuote.cs'),
        (Join-Path $projectDirectory 'Assets/Scripts/Core/RoomTactics.cs'),
        (Join-Path $PSScriptRoot 'ProgressionTests.cs')
    )
    [ProgressionTests]::Run($testDirectory)
}
finally {
    $resolvedTarget = [IO.Path]::GetFullPath($testDirectory)
    $allowedPrefix = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar + 'EmberfallProgression-'
    if ($resolvedTarget.StartsWith($allowedPrefix, [StringComparison]::OrdinalIgnoreCase) -and (Test-Path -LiteralPath $resolvedTarget)) {
        Remove-Item -LiteralPath $resolvedTarget -Recurse -Force
    }
}
