#!/usr/bin/env python3
"""Actual prop combat paths must not mutate real scenery during practice."""
import os,sys,subprocess,tempfile
from pathlib import Path
root=Path(__file__).resolve().parents[1]
f=(root/'Tests/EquipmentCompositionProductionTests.Fixture.cs').read_text().split('namespace Emberfall {')[0]
f=f.replace('public static Vector3 zero=>','public static Vector3 Lerp(Vector3 a,Vector3 b,float t)=>a+(b-a)*t;public static float Angle(Vector3 a,Vector3 b)=>0;public static Vector3 zero=>')
f=f.replace('public static float deltaTime=.016f;','public static float deltaTime=.016f,time;')
f=f.replace('public const float PI=', 'public static float Clamp01(float v)=>Clamp(v,0,1);public const float PI=')
source=(root/'Assets/Scripts/World/DestructibleProp.cs').read_text().split('    internal sealed class DestructibleDebrisBurst')[0]+'}'
with tempfile.TemporaryDirectory(prefix='practice-props-') as d:
 p=Path(d);(p/'Fixture.cs').write_text(f);actual=p/'Actual.cs';actual.write_text(source)
 (p/'Rules.cs').write_text((root/'Assets/Scripts/Core/DestructiblePropRules.cs').read_text());(p/'Test.cs').write_text((root/'Tests/PracticePropIsolationProductionTests.cs').read_text())
 project=p/'Test.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><NoWarn>0649;0169;0414</NoWarn></PropertyGroup></Project>');(p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1');dotnet=sys.argv[1] if len(sys.argv)>1 else 'dotnet'
 def run(args):
  q=subprocess.run([dotnet]+args,env=env,text=True,capture_output=True);print(q.stdout+q.stderr);return q
 assert run(['run','--project',str(project)]).returncode==0
 assert '&&!game.PracticeActive' in source;actual.write_text(source.replace('&&!game.PracticeActive',''))
 assert run(['build',str(project),'--no-restore','-v:q']).returncode==0
 q=run([str(p/'bin/Debug/net8.0/Test.dll')]);assert q.returncode!=0 and 'practice cannot break or release original obstacle' in q.stdout+q.stderr
 print('PASS compiled original missing-scope guard fails actual Impact isolation')
