param([Parameter(Mandatory = $true)][string]$ProjectDirectory)
$ErrorActionPreference = 'Stop'
function Invoke-RepositoryGit([string[]]$GitArguments) {
    $output = & git -C $ProjectDirectory @GitArguments
    if ($LASTEXITCODE -ne 0) { throw "Git update failed: git $($GitArguments -join ' '). Build cancelled." }
    return $output
}
if (-not (Get-Command git -ErrorAction SilentlyContinue)) { throw 'Install Git for Windows before updating and building.' }
$branch = Invoke-RepositoryGit @('branch', '--show-current')
if ($branch -ne 'main') { throw 'Automatic installation requires the main branch. Build cancelled.' }
$changes = @(Invoke-RepositoryGit @('status', '--porcelain', '--untracked-files=normal'))
# Unity can update its editor version during a build. Git still refuses any
# conflicting incoming changes; this file is never discarded or overwritten.
$codeChanges = @($changes | Where-Object { $_.Substring(3) -ne 'ProjectSettings/ProjectVersion.txt' })
if ($codeChanges.Count) { throw "Uncommitted source changes found. Commit or preserve them before retrying.`n$($codeChanges -join "`n")" }
$previousPrompt = $env:GIT_TERMINAL_PROMPT
$previousSsh = $env:GIT_SSH_COMMAND
try {
    $env:GIT_TERMINAL_PROMPT = '0'
    $env:GIT_SSH_COMMAND = 'ssh -o BatchMode=yes'
    Invoke-RepositoryGit @('pull', '--ff-only', 'origin', 'main') | ForEach-Object { Write-Host $_ }
    $local = Invoke-RepositoryGit @('rev-parse', 'HEAD')
    $remote = Invoke-RepositoryGit @('rev-parse', 'refs/remotes/origin/main')
    if ($local -ne $remote) { throw 'Local main differs from origin/main. Build cancelled.' }
    Write-Host "Updated main: $local"
} finally {
    $env:GIT_TERMINAL_PROMPT = $previousPrompt
    $env:GIT_SSH_COMMAND = $previousSsh
}
