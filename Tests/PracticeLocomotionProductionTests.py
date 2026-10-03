#!/usr/bin/env python3
"""Actual practice traversal -> enemy adapter -> locomotion state, managed boundaries."""
import os,sys,subprocess,tempfile
from pathlib import Path
root=Path(__file__).resolve().parents[1]
def member(s,key):
 a=s.index(key);b=s.index('{',a)+1;n=1
 while n:n+=(s[b]=='{')-(s[b]=='}');b+=1
 return s[a:b]
enemy=(root/'Assets/Scripts/Combat/EnemyController.cs').read_text()
practiceBranch=member(member(enemy,'private void Update()'),'if(session.PracticeActive)')
session=(root/'Assets/Scripts/Core/GameSession.Practice.cs').read_text()
motion=(root/'Assets/Scripts/Combat/CombatModel.Motion.cs').read_text()
with tempfile.TemporaryDirectory(prefix='practice-locomotion-') as d:
 p=Path(d)
 actual='using UnityEngine;namespace Emberfall{public partial class EnemyController{'+'public void Tick(){'+practiceBranch+'}'+member(enemy,'private void AnimateModel(')+'}public partial class GameSession{'+member(session,'public bool MovePracticeTarget(')+'}public partial class CombatModel{'+member(motion,'public void SetLocomotion(')+'}}'
 source=p/'Actual.cs';source.write_text(actual)
 for name in ['LocomotionPoseState','CombatImpactBatch','CampPracticeRecord']:(p/(name+'.cs')).write_text((root/'Assets/Scripts/Core'/(name+'.cs')).read_text())
 (p/'Test.cs').write_text((root/'Tests/PracticeLocomotionProductionTests.cs').read_text())
 project=p/'Test.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><NoWarn>0649;0169;0414</NoWarn></PropertyGroup></Project>')
 (p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1');dotnet=sys.argv[1] if len(sys.argv)>1 else 'dotnet'
 def run(args):
  q=subprocess.run([dotnet]+args,env=env,text=True,capture_output=True);print(q.stdout+q.stderr);return q
 assert run(['run','--project',str(project)]).returncode==0
 for old,new,message in [
 ('walkingDisplacement=CombatFx.Flat(transform.position-beforeWalking);','walkingDisplacement=Vector3.zero;','actual accepted walking advances phase'),
 ('knockVelocity=Vector3.Lerp','walkingDisplacement+=knockVelocity*Time.deltaTime;knockVelocity=Vector3.Lerp','knockback excluded from active gait'),
 ('walkingDisplacement=CombatFx.Flat(transform.position-beforeWalking);','walkingDisplacement=new Vector3(.25f,0,0);','frozen active gait stops'),
 ]:
  assert old in actual;source.write_text(actual.replace(old,new))
  assert run(['build',str(project),'--no-restore','-v:q']).returncode==0
  q=run([str(p/'bin/Debug/net8.0/Test.dll')]);assert q.returncode!=0 and message in q.stdout+q.stderr
  print('PASS compiled negative:',message);source.write_text(actual)
