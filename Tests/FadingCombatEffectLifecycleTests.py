#!/usr/bin/env python3
"""Execute the production fading component with explicit managed Unity lifecycle substitutes."""
from CastReceiptFixtureSources import include_cast_receipt_source
import os
from pathlib import Path
import subprocess
import tempfile
root = Path(__file__).resolve().parents[1]
dotnet = os.environ.get('DOTNET', 'dotnet')
source = (root / 'Assets/Scripts/Combat/CombatEffects.cs').read_text()
start = source.index('    internal sealed class FadingCombatEffect')
end = source.index('    internal sealed class CombatProjectile', start)
with tempfile.TemporaryDirectory(prefix='emberfall-fading-') as temp:
    temp = Path(temp)
    (temp / 'Effect.cs').write_text('using UnityEngine;\nnamespace Emberfall\n{\n' + source[start:end] + '\n}\n')
    for rel in ['Tests/FadingCombatEffectLifecycleTests.cs', 'Assets/Scripts/Combat/DecorationLease.cs', 'Assets/Scripts/Core/DecorationBudget.cs', 'Assets/Scripts/Combat/CombatVisualLease.cs', 'Assets/Scripts/Core/CombatVisualBudget.cs']:
        (temp / Path(rel).name).write_text((root / rel).read_text())
    (temp / 'Program.cs').write_text('System.Console.WriteLine(FadingCombatEffectLifecycleTests.Run());')
    (temp / 'Validation.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework></PropertyGroup></Project>');include_cast_receipt_source(temp / 'Validation.csproj')
    (temp / 'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
    subprocess.run([dotnet, 'restore', str(temp / 'Validation.csproj'), '--configfile', str(temp / 'NuGet.Config')], check=True)
    subprocess.run([dotnet, 'run', '--project', str(temp / 'Validation.csproj'), '--no-restore', '-c', 'Release'], check=True)
