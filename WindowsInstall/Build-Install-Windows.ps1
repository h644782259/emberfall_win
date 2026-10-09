# Run with powershell.exe -NoProfile -ExecutionPolicy Bypass -File <this script>
[CmdletBinding()]
param(
    [string]$InstallDirectory = (Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'Programs\Emberfall'),
    [string]$PackageDirectory,
    [string]$UnityPath,
    [switch]$SkipDesktopShortcut
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$toolsRoot = Join-Path $projectRoot 'Tools'

function Get-ExternalDirectory([string]$Path) {
    if ($Path -notmatch '^[A-Za-z]:[\\/]') { throw "Use an absolute local path: $Path" }
    $full = [IO.Path]::GetFullPath($Path).TrimEnd('\', '/')
    if ($full -eq [IO.Path]::GetPathRoot($full).TrimEnd('\', '/')) {
        throw 'Choose a dedicated folder, not a drive root.'
    }
    if ($full.Equals($projectRoot, [StringComparison]::OrdinalIgnoreCase) -or
        $full.StartsWith($projectRoot + '\', [StringComparison]::OrdinalIgnoreCase)) {
        throw "Installation and packages must be outside the source repository: $full"
    }
    return $full
}

try {
    foreach ($required in @('Assets', 'Packages', 'ProjectSettings', 'Tools\Build-Windows.ps1', 'Tools\Package-Release.ps1')) {
        if (-not (Test-Path -LiteralPath (Join-Path $projectRoot $required))) {
            throw "Missing project files: $required. Obtain the complete Windows repository."
        }
    }
    $InstallDirectory = Get-ExternalDirectory $InstallDirectory
    if (-not $PackageDirectory) { $PackageDirectory = Join-Path $InstallDirectory 'Installer' }
    $PackageDirectory = Get-ExternalDirectory $PackageDirectory
    if (Get-Process -Name Emberfall -ErrorAction SilentlyContinue) {
        throw 'Close Emberfall before building and installing.'
    }
    if (-not (Test-Path -LiteralPath ([IO.Path]::GetPathRoot($InstallDirectory)))) {
        throw "The installation drive does not exist: $InstallDirectory"
    }

    Write-Host '[1/5] Building current local Windows source...'
    & (Join-Path $toolsRoot 'Build-Windows.ps1') -UnityPath $UnityPath
    $buildRoot = Join-Path $projectRoot 'Builds\Windows'

    Write-Host '[2/5] Packaging outside the repository...'
    & (Join-Path $toolsRoot 'Package-Release.ps1') -BuildDirectory $buildRoot -OutputDirectory $PackageDirectory

    Write-Host '[3/5] Installing...'
    $installerPath = Join-Path $PackageDirectory 'Emberfall-Setup.exe'
    $process = Start-Process -FilePath $installerPath -ArgumentList @('--install-dir', ('"' + $InstallDirectory + '"')) -WindowStyle Hidden -Wait -PassThru
    if ($process.ExitCode -ne 0) { throw "Installation failed (exit $($process.ExitCode))." }

    Write-Host '[4/5] Verifying installed files...'
    $count = 0
    foreach ($file in Get-ChildItem -LiteralPath $buildRoot -Recurse -File) {
        $relative = $file.FullName.Substring($buildRoot.Length + 1)
        $target = Join-Path $InstallDirectory $relative
        if ((Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash -ne
            (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash) {
            throw "Installed file differs from the fresh build: $relative"
        }
        $count++
    }

    Write-Host '[5/5] Creating desktop shortcut...'
    $gamePath = Join-Path $InstallDirectory 'Emberfall.exe'
    if (-not $SkipDesktopShortcut) {
        $desktop = [Environment]::GetFolderPath('Desktop')
        if (-not $desktop) { throw 'Desktop folder is unavailable.' }
        $shellObject = New-Object -ComObject WScript.Shell
        $shortcut = $shellObject.CreateShortcut((Join-Path $desktop 'Emberfall.lnk'))
        $shortcut.TargetPath = $gamePath
        $shortcut.WorkingDirectory = $InstallDirectory
        $shortcut.IconLocation = $gamePath + ',0'
        $shortcut.Save()
    }
    Write-Host "Done. Verified $count files."
    Write-Host "Game: $gamePath"
    Write-Host "Installer: $installerPath"
}
catch {
    Write-Error $_
    exit 1
}
