#requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$project = Split-Path -Parent $PSScriptRoot
Push-Location $project
try {
    # Reuse the same production replay and explicit engine boundary as the .NET runner.
    $fixtureJson = python -X utf8 -c "import ast,json;from pathlib import Path;s=ast.parse(Path('Tests/ChapterEntryProductionTests.py').read_text(encoding='utf-8'));out={};[(out.update({n.targets[0].id:ast.literal_eval(n.value)})) for n in s.body if isinstance(n,ast.Assign) and isinstance(n.targets[0],ast.Name) and n.targets[0].id in ('shell','core') and isinstance(n.value,(ast.Constant,ast.List))];print(json.dumps(out))"
    if ($LASTEXITCODE -ne 0) { throw 'Could not read chapter fixture.' }
    $fixture = $fixtureJson | ConvertFrom-Json
    function Read-Member([string]$source, [string]$signature) {
        $a = $source.IndexOf($signature)
        $b = $source.IndexOf('{', $a) + 1
        $depth = 1
        while ($depth -gt 0) {
            if ($source[$b] -eq '{') { $depth++ }
            if ($source[$b] -eq '}') { $depth-- }
            $b++
        }
        return $source.Substring($a, $b - $a)
    }
    $uiSource = Get-Content Assets/Scripts/UI/GameUI.cs -Raw
    $close = Read-Member $uiSource 'private void ClosePanel()'
    $dispatch = Read-Member $uiSource 'else if (session.IsDead)'
    $body = $fixture.shell.Replace('CLOSE', $close).Replace('DEAD_DISPATCH', $dispatch.Substring($dispatch.IndexOf('{')))
    $temporaryFixture = Join-Path ([IO.Path]::GetTempPath()) ('ChapterEntry-' + [Guid]::NewGuid().ToString('N') + '.cs')
    Set-Content -LiteralPath $temporaryFixture -Value $body -Encoding utf8
    $sources = @('Assets/Scripts/Combat/EnemyControlPolicy.cs', 'Assets/Scripts/Core/CastFirstHitReceipt.cs') + @($fixture.core | ForEach-Object { 'Assets/Scripts/Core/' + $_ + '.cs' }) + @(
        'Assets/Scripts/UI/GameUI.Chapter.cs', 'Assets/Scripts/UI/ChapterEntryPresentation.cs',
        'Assets/Scripts/UI/MobilePanelLayout.cs', 'Tests/ProgressionTests.cs', 'Tests/ChapterPresentationTests.cs', $temporaryFixture)
    $references = @((Get-ChildItem (Join-Path $PSHOME 'ref') -Filter '*.dll').FullName)
    Add-Type -Path $sources -ReferencedAssemblies $references -CompilerOptions '/langversion:9.0', '/nowarn:0649,0414,0169'
    $saveRoot = Join-Path ([IO.Path]::GetTempPath()) ('ChapterReplay-' + [Guid]::NewGuid().ToString('N'))
    Write-Host ('PASS: ' + [Emberfall.GameUI]::Verify($saveRoot) + ' chapter UI/core replay assertions (engine boundaries; no rendering)')
    Write-Host ([ChapterPresentationTests]::Run())
}
finally { Pop-Location }
