#!/usr/bin/env python3
"""Production Slash dispatch and endpoint ribbon identity with controlled Unity doubles."""
from CastReceiptFixtureSources import include_cast_receipt_source
import os,sys,tempfile,subprocess
from pathlib import Path
root=Path(__file__).resolve().parents[1];dotnet=sys.argv[1] if len(sys.argv)>1 else os.environ.get('DOTNET','dotnet')
source=(root/'Assets/Scripts/Combat/CombatEffects.cs').read_text()
start=source.index('        public static void Slash(');end=source.index('        public static Material NewGlow()',start)
with tempfile.TemporaryDirectory(prefix='weapon-swing-identity-') as temp:
 p=Path(temp)
 for file in ['Assets/Scripts/Combat/WeaponVisualLinks.cs','Assets/Scripts/Core/WeaponStructure.cs','Assets/Scripts/Core/CombatVisualBudget.cs','Tests/WeaponVisualLinkTests.cs']:(p/Path(file).name).write_text((root/file).read_text())
 fixture=(root/'Tests/FilledVfxAllocationTests.cs').read_text();fixture=fixture[fixture.index('namespace Emberfall'):]
 (p/'UnityFixture.cs').write_text('using System;using System.Collections.Generic;using System.Linq;using System.Reflection;using UnityEngine;\n'+fixture.replace('public static class CombatFx','public static partial class CombatFx'))
 dispatch=p/'Dispatch.cs';dispatch.write_text('using UnityEngine;namespace Emberfall {public static partial class CombatFx {'+source[start:end]+'}public static class FilledSkillVfx {public static int Crescents;public static void Crescent(PlayerController p,Vector3 at,Vector3 f,float r,Color c,int side,CombatVisualPriority priority){Crescents++;}public static void BurnContact(PlayerController owner,Vector3 point,bool finale){}}}')
 (p/'Program.cs').write_text('''using System;using System.Linq;using Emberfall;using UnityEngine;
System.Console.WriteLine(WeaponVisualLinkTests.Run());
Time.deltaTime=.02f;var go=new GameObject("attacker");var owner=go.AddComponent<PlayerController>();var model=go.AddComponent<CombatModel>();model.Root=new Vector3(1,1,0);model.Tip=new Vector3(1,2,0);GameSession.Instance=new GameSession{Player=owner,HasStarted=true};
int before=GameObject.All.Count(x=>x.GetComponent<WeaponSlashRibbon>()!=null);
for(int i=0;i<30;i++)CombatFx.Slash(Vector3.zero,Vector3.forward,.5f,new Color(1,1,1));
if(GameObject.All.Count(x=>x.GetComponent<WeaponSlashRibbon>()!=null)!=before||FilledSkillVfx.Crescents!=30)throw new Exception("remote slash ticks never attach a hand-held sword ribbon");
CombatFx.WeaponSlash(owner,model,Vector3.zero,Vector3.forward,2,new Color(1,1,1));
if(GameObject.All.Count(x=>x.GetComponent<WeaponSlashRibbon>()!=null)!=before+1)throw new Exception("explicit physical swing claims its single hand-held ribbon");
model.WeaponActionId++;CombatFx.WeaponSlash(owner,model,Vector3.zero,Vector3.forward,2,new Color(1,1,1));
if(GameObject.All.Count(x=>x.GetComponent<WeaponSlashRibbon>()!=null)!=before+2)throw new Exception("a new physical swing gets its own identity");
System.Console.WriteLine("PASS: production decorative/physical Slash dispatch and per-swing identity");''')
 project=p/'Test.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>');include_cast_receipt_source(project);(p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1',DOTNET_CLI_TELEMETRY_OPTOUT='1')
 subprocess.run([dotnet,'restore',str(project),'--configfile',str(p/'NuGet.Config'),'-v:q'],env=env,check=True)
 command=[dotnet,'run','--project',str(project),'--no-restore'];subprocess.run(command,env=env,check=True)
 mutations=[(p/'WeaponVisualLinks.cs','if(!model.TryClaimSwordRibbon(model.WeaponActionId))return;','','one physical swing can create only one endpoint ribbon'),
            (p/'WeaponVisualLinks.cs','if(model.WeaponActionId!=action||!model.SwordActionActive)sampling=false;','','action change stops sampling while the existing afterimage fades'),
            (dispatch,'if(game!=null)FilledSkillVfx.Crescent(game.Player,center,forward,radius,color,1,CombatVisualPriority.Decoration);','if(game!=null){FilledSkillVfx.Crescent(game.Player,center,forward,radius,color,1,CombatVisualPriority.Decoration);WeaponSlashRibbon.Spawn(game.Player,game.Player.gameObject.GetComponent<CombatModel>(),color);}','remote slash ticks never attach a hand-held sword ribbon')]
 for path,before,after,expected in mutations:
  original=path.read_text()
  if original.count(before)!=1:raise AssertionError('mutation must match exactly once')
  path.write_text(original.replace(before,after))
  subprocess.run([dotnet,'build',str(project),'--no-restore','-v:q'],env=env,check=True,stdout=subprocess.DEVNULL)
  result=subprocess.run(command+['--no-build'],env=env,capture_output=True,text=True);path.write_text(original)
  if result.returncode==0 or 'System.Exception: '+expected not in result.stdout+result.stderr:raise AssertionError(result.stdout+result.stderr)
 print('PASS: 3 compiled duplicate-swing, stale-sampling and decorative-Slash negative controls')
