#!/usr/bin/env python3
"""Exercise production resolver and query bodies without copying selection algorithms."""
import os,sys,tempfile,subprocess
from pathlib import Path
root=Path(__file__).resolve().parents[1];dotnet=sys.argv[1] if len(sys.argv)>1 else os.environ.get('DOTNET','dotnet')
def extract(source,signature):
 start=source.index(signature);brace=source.index('{',start);depth=1;end=brace+1
 while depth:
  depth+=(source[end]=='{')-(source[end]=='}');end+=1
 return source[start:end]
source=(root/'Assets/Scripts/Combat/PlayerController.cs').read_text()
methods='\n'.join(extract(source,signature) for signature in ['internal void ResolveMobileSkillAim(','internal void PrepareMobileSkillAim(','private static bool ValidAimTarget(','private bool CanUseMovementSkill(','internal bool SkillTargetingReady(','internal Vector3 ResolveSkillGroundTarget(','public bool CanShatterNow(','internal EnemyController CurrentOpportunityTarget'])
target=(root/'Assets/Scripts/Combat/SkillTargetingController.cs').read_text()
preview='public bool CanBeginThisFrame=>true;public int skill=-1; public bool IsTargeting=>skill>=0; public UnityEngine.Vector3 TargetPoint;'+extract(target,'public int TargetedSkillIndex')+extract(target,'public enum Shape')+extract(target,'public struct Preview')+extract(target,'public static Preview Describe(')
with tempfile.TemporaryDirectory(prefix='shatter-availability-') as temporary:
 path=Path(temporary)
 for relative in ['Assets/Scripts/Combat/PlayerController.SkillAvailability.cs','Assets/Scripts/Core/GameTypes.cs','Assets/Scripts/Core/CombatBalance.cs','Assets/Scripts/Combat/MobileSkillPolicy.cs','Tests/ShatterAvailabilityTests.cs','Assets/Scripts/UI/CombatOpportunityPresentation.cs','Assets/Scripts/Core/CombatOpportunityState.cs','Assets/Scripts/Combat/PlayerController.Opportunities.cs']:(path/Path(relative).name).write_text((root/relative).read_text())
 uiSource=(root/'Assets/Scripts/UI/GameUI.CombatOpportunities.cs').read_text();ui=path/'UI.cs';ui.write_text('namespace Emberfall{public partial class GameUI{'+extract(uiSource,'private string CurrentCombatOpportunity(')+'}}')
 host=path/'Player.cs';host.write_text('using UnityEngine;namespace Emberfall{public sealed partial class PlayerController{'+methods+'}public class SkillTargetingController{'+preview+'}}')
 (path/'Program.cs').write_text('System.Console.WriteLine(ShatterAvailabilityTests.Run());')
 project=path/'Test.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>');config=path/'NuGet.Config';config.write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 subprocess.run([dotnet,'restore',str(project),'--configfile',str(config)],check=True)
 command=[dotnet,'run','--project',str(project),'--no-restore'];subprocess.run(command,check=True)
 original=host.read_text();body=extract(original,'public bool CanShatterNow(')
 host.write_text(original.replace(body,'public bool CanShatterNow(int skill=1){foreach(var enemy in session.Enemies)if(enemy.StatusEffects.HasFrostMark&&SkillTargetingReady(skill))return true;return false;}'))
 result=subprocess.run(command,capture_output=True,text=True)
 assert result.returncode and 'real release footprint distinguishes 8/10/13m' in result.stdout+result.stderr,result.stdout+result.stderr
 host.write_text(original)
 uiOriginal=ui.read_text();assert 'burnRoute?ready:hero.CanShatterNow(1)' in uiOriginal
 ui.write_text(uiOriginal.replace('burnRoute?ready:hero.CanShatterNow(1)','ready').replace('                ready=ready&&hero.SkillOpportunity(1).Actionable;',''))
 result=subprocess.run(command,capture_output=True,text=True)
 assert result.returncode and 'actual HUD must not call out unreachable marked target as available' in result.stdout+result.stderr,result.stdout+result.stderr
 ui.write_text(uiOriginal)
 opportunity=path/'PlayerController.Opportunities.cs';good=opportunity.read_text()
 for before,after,oracle in [
  ('status.FrostRemaining','(status.HasFrostMark?4f:0)','typed frost expiry is not a synthetic timer'),
  ('||!SkillTargetingReady(skill)','', 'typed no-energy skill remains unavailable despite frost')]:
  assert before in good;opportunity.write_text(good.replace(before,after))
  subprocess.run([dotnet,'build',str(project),'--no-restore','-v:q'],check=True,stdout=subprocess.DEVNULL)
  failed=subprocess.run([dotnet,str(path/'bin/Debug/net8.0/Test.dll')],capture_output=True,text=True)
  assert failed.returncode and 'System.Exception: '+oracle in failed.stdout+failed.stderr,failed.stdout+failed.stderr
  opportunity.write_text(good)
 print('PASS: compiled fabricated-expiry and readiness-bypass controls fail exact typed opportunity assertions')
 print('PASS: old HUD cooldown-only connection fails actual HUD replay')
 print('PASS: old frost-presence-plus-ready shortcut fails actual production query geometry test')
assert 'ResolveSkillGroundTarget(' in extract(source,'private void CastSkillCore(')
assert 'SkillTargetingReady(skill)' in extract(source,'internal bool CanBeginSkillTargeting(')
assert 'ResolveMobileSkillAim(' in extract(source,'internal void PrepareMobileSkillAim(')
print('PASS: release/preparation/availability share production query resolvers; not Unity physics or GPU acceptance')
