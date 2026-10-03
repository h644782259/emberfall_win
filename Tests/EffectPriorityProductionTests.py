#!/usr/bin/env python3
"""Real mesh effects, HitFeedback, global lease and decorative Slash; managed engine boundaries."""
from CastReceiptFixtureSources import include_cast_receipt_source
import os,sys,subprocess,tempfile
from pathlib import Path
r=Path(__file__).resolve().parents[1];dotnet=sys.argv[1] if len(sys.argv)>1 else os.environ.get('DOTNET','dotnet')
def member(s,key):
 a=s.index(key);b=s.index('{',a)+1;n=1
 while n:n+=(s[b]=='{')-(s[b]=='}');b+=1
 return s[a:b]
s=(r/'Tests/FilledVfxAllocationTests.cs').read_text();shell='using System;using System.Linq;using System.Collections.Generic;using System.Reflection;using UnityEngine;'+s[s.index('namespace Emberfall'):]
shell=shell.replace('public static class CombatFx{','public static partial class CombatFx{').replace('public static bool ReducedEffects;','public static bool ReducedEffects,CameraShake;').replace('public void RecalculateNormals(){}','public void MarkDynamic(){}public void RecalculateNormals(){}').replace('public struct Color{','public partial struct Color{').replace('public static class Mathf\n','public static partial class Mathf\n').replace('public static Vector3 forward=>','public static Vector3 right=>new Vector3(1,0,0);public static Vector3 forward=>')
shell=shell.replace('public Vector3 right=>localRotation.Rotate','public Vector3 forward=>localRotation.Rotate(Vector3.forward);public Vector3 up=>localRotation.Rotate(Vector3.up);public Vector3 right=>localRotation.Rotate')
shell=shell.replace('public sealed class PlayerController:MonoBehaviour{','public sealed partial class PlayerController:MonoBehaviour{private GameSession session=>GameSession.Instance;')
effects=(r/'Assets/Scripts/Combat/CombatEffects.cs').read_text();slash=member(effects,'public static void Slash(')
with tempfile.TemporaryDirectory(prefix='effect-priority-') as temp:
 p=Path(temp)
 for f in ['Core/CombatVisualBudget','Core/FilledVfxRecipes','Core/FilledVfxPlacement','Combat/CombatVisualLease','Combat/FilledSkillVfx','Combat/AnchoredImpactMesh','Combat/HitFeedback','Combat/PlayerController.BurnFeedback']:(p/(Path(f).name+'.cs')).write_text((r/('Assets/Scripts/'+f+'.cs')).read_text())
 (p/'Shell.cs').write_text(shell);(p/'Test.cs').write_text((r/'Tests/EffectPriorityProductionTests.cs').read_text());(p/'Slash.cs').write_text('using UnityEngine;namespace Emberfall{public static partial class CombatFx{'+slash+member(effects,'internal static void BurnContact(')+'}}')
 project=p/'Tests.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>');include_cast_receipt_source(project);cfg=p/'NuGet.Config';cfg.write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1');subprocess.run([dotnet,'restore',str(project),'--configfile',str(cfg),'-v:q'],env=env,check=True)
 cmd=[dotnet,'run','--project',str(project),'--no-restore'];subprocess.run(cmd,env=env,check=True)
 for name,a,b,error in [('FilledSkillVfx.cs','priority:CombatVisualPriority.RealContact);','priority:finale?CombatVisualPriority.Finale:CombatVisualPriority.RealContact);','dense cash contacts cannot evict their own finale main'),('FilledSkillVfx.cs','if(!confirmedFinale)','if(true)','only confirmed finale survives result; skips and owner exits retire immediately'),('Slash.cs','CombatVisualPriority.Decoration','CombatVisualPriority.ActionBody','decorative Slash cannot displace sustained background'),('HitFeedback.cs','if(CombatVisualLease.Attach(obj,priority)==null)return;','','real contact evicts decoration under the global cap'),('FilledSkillVfx.cs','if(priority==CombatVisualPriority.Finale)\n                fx.Add(rupture','if(false)\n                fx.Add(rupture','element finale retains its minimum short tail')]:
  path=p/name;old=path.read_text();assert old.count(a)==1;path.write_text(old.replace(a,b));subprocess.run([dotnet,'build',str(project),'--no-restore','-v:q'],env=env,check=True,stdout=subprocess.DEVNULL);q=subprocess.run(cmd+['--no-build'],env=env,capture_output=True,text=True);path.write_text(old)
  assert q.returncode and 'System.Exception: '+error in q.stdout+q.stderr,q.stdout+q.stderr
 print('PASS: 5 compiled exact-negative priority/contact/finale controls')
