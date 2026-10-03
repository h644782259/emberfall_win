#!/usr/bin/env python3
"""The old broad compatibility must fail an independent real-cast usefulness oracle."""
from CastReceiptFixtureSources import include_cast_receipt_source
import os
from pathlib import Path
import subprocess
import sys
import tempfile
root=Path(__file__).resolve().parents[1]
dotnet=sys.argv[1] if len(sys.argv)>1 else os.environ.get('DOTNET','dotnet')
source=(root/'Assets/Scripts/Core/RunChoices.cs').read_text()
start=source.index('            if (blessing == RunBlessing.FlowingEssence || blessing == RunBlessing.QuickRecovery)')
end=source.index('            if ((int)blessing >= (int)RunBlessing.KeenSight',start)
with tempfile.TemporaryDirectory(prefix='blessing-usability-negative-') as folder:
    folder=Path(folder)
    sources=['Core/GameTypes','Core/CombatBalance','Core/RunChoices.Rooms','Core/SkillRuntime','Core/SkillDamageBudgets','Combat/EnemyControlPolicy']
    for name in sources:(folder/(name.split('/')[-1]+'.cs')).write_text((root/('Assets/Scripts/'+name+'.cs')).read_text())
    for name in ['SkillRuntimeTests','RoomBlessingUsabilityTests']:(folder/(name+'.cs')).write_text((root/('Tests/'+name+'.cs')).read_text())
    (folder/'RunChoices.cs').write_text(source[:start]+source[end:])
    (folder/'Program.cs').write_text('System.Console.WriteLine(RoomBlessingUsabilityTests.Run());')
    project=folder/'Validation.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>');include_cast_receipt_source(project)
    config=folder/'NuGet.Config';config.write_text('<configuration><packageSources><clear /></packageSources></configuration>')
    env=dict(os.environ,DOTNET_CLI_HOME=str(folder/'cli'),DOTNET_CLI_TELEMETRY_OPTOUT='1')
    subprocess.run([dotnet,'restore',str(project),'--configfile',str(config),'--verbosity','quiet'],env=env,check=True)
    result=subprocess.run([dotnet,'run','--project',str(project),'--no-restore','-c','Release'],env=env,capture_output=True,text=True)
    assert result.returncode!=0 and 'independent cast effects require two useful offers: frozen review reproduction Vanguard seed=17 / BattleFervor,FlowingEssence,QuickRecovery' in result.stdout+result.stderr,result.stdout+result.stderr
    print('PASS: frozen Vanguard seed 17 offer fails the independent production-cast oracle when the broad resource eligibility is restored (not a compilation failure).')
