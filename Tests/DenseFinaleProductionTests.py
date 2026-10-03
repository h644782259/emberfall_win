#!/usr/bin/env python3
"""Execute production area -> burn settlement -> receipt -> filled mesh -> shared lease together.
Enemy HP/terminal callback and Unity scene are managed boundaries, not PlayMode.
"""
from CastReceiptFixtureSources import include_cast_receipt_source
import os,sys,tempfile,subprocess
from pathlib import Path
r=Path(__file__).resolve().parents[1];dotnet=sys.argv[1] if len(sys.argv)>1 else os.environ.get('DOTNET','dotnet')
def member(s,k):
 a=s.index(k);b=s.index('{',a)+1;n=1
 while n:n+=(s[b]=='{')-(s[b]=='}');b+=1
 return s[a:b]
s=(r/'Tests/FilledVfxAllocationTests.cs').read_text();s='using System;using System.Linq;using System.Collections.Generic;using System.Reflection;using UnityEngine;'+s[s.index('namespace Emberfall'):]
burn=(r/'Tests/BurnFinaleProductionTests.cs').read_text()
s=s.replace(member(s,'public sealed class PlayerController:'),member(burn,'public sealed partial class PlayerController:'))
s=s.replace('public bool HasStarted,ModeFinished,InputBlocked;','public bool HasStarted,ModeFinished,InputBlocked,CombatEnded;public List<EnemyController> Enemies=new List<EnemyController>();public void RecordClassTutorial(HeroClass hero){}')
s=s.replace('public class MonoBehaviour:Component{}','public class MonoBehaviour:Component{public T GetComponentInChildren<T>()where T:Component=>null;}')
s=s.replace('public static float Min(float a,float b)', 'public static int Min(int a,int b)=>Math.Min(a,b);public static float Min(float a,float b)')
s=s.replace('public static class CombatFx{','public static class CombatFx{public static void Ring(Vector3 p,float r,Color c,float d,float w){}'+member((r/'Assets/Scripts/Combat/CombatEffects.cs').read_text(),'internal static void BurnContact('))
# Presentation is outside this damage/lease integration suite; reuse its explicit legacy-fallback boundary.
extra='namespace Emberfall{public enum HeroClass{Arcanist}public enum ElementalistSpecialization{None,Burn,Shatter}'+member(burn,'public class CombatModel:')+''.join(member(burn,k) for k in ['public class EnemyController:', 'public static class ElementalCombatVfx', 'public static class DestructibleProp', 'public static class PlayerUpgradeRules'])+member((r/'Assets/Scripts/Combat/PlayerUpgradeRules.cs').read_text(),'public sealed class RecentCastGate')+'}'
area=member((r/'Assets/Scripts/Combat/PlayerController.cs').read_text(),'internal void ElementalAdvancedArea(')
test='''using System;using System.Linq;using UnityEngine;using Emberfall;
class Test{static int n;static void C(bool b,string m){n++;if(!b)throw new Exception(m);}static GameObject[] Live()=>GameObject.All.Where(o=>!o.Destroyed&&o.activeInHierarchy).ToArray();static void Main(){
foreach(string scenario in new[]{"live-boss","final-kill","boss-first-guards-alive"}){
foreach(var o in GameObject.All.ToArray())UnityEngine.Object.Destroy(o);GameObject.All.Clear();EffectPreferences.ReducedEffects=true;Application.isMobilePlatform=true;Time.deltaTime=0;Time.frameCount++;
var hero=new GameObject("hero").AddComponent<PlayerController>();var game=new GameSession{HasStarted=true,Player=hero};GameSession.Instance=game;
for(int i=0;i<40;i++){var o=new GameObject("enemy");var e=o.AddComponent<EnemyController>();e.StatusEffects=o.AddComponent<EnemyStatusEffects>();o.Call("Awake");e.IsBoss=i==0;e.Health=scenario=="live-boss"?10000:scenario=="boss-first-guards-alive"?(i==0?1:10000):50;e.StatusEffects.Burn(hero,3,30);game.Enemies.Add(e);
e.OnDamage=()=>{if((scenario=="boss-first-guards-alive"&&e.IsBoss&&e.IsDead)||(scenario=="final-kill"&&game.Enemies.All(x=>x.IsDead)))game.ModeFinished=game.CombatEnded=game.InputBlocked=true;};}
FilledSkillVfx.Impact(hero,Vector3.zero,4,FilledVfxKind.Fire,new Color(1,1,1),CombatVisualPriority.Finale,91);
hero.ElementalAdvancedArea(Vector3.zero,4,new CombatDamage(100,false),91,true);
C(Live().Any(o=>o.name.EndsWith("Primary Fire")),"actual dense cashouts retain main at cap12");C(CombatVisualLease.Active<=12,"actual dense cashouts remain bounded");
if(scenario=="live-boss")C(game.Enemies.All(x=>x.Hits.Count==2)&&hero.BurnCashFeedback(out int targets,out _)&&targets==40,"all forty actual cash settlements deliver and aggregate");
if(scenario=="final-kill")C(game.Enemies.All(x=>x.IsDead&&x.Hits.Count==2)&&game.ModeFinished,"forty cashouts include terminal final kill");
if(scenario=="boss-first-guards-alive")C(game.ModeFinished&&game.Enemies.Skip(1).All(x=>!x.IsDead&&x.Hits.Count==0),"boss first ends without damaging remaining guards");
int hits=game.Enemies.Sum(x=>x.Hits.Count);game.ModeFinished=true;Time.unscaledDeltaTime=.15f;
foreach(var o in Live().Where(o=>o.GetComponent<FilledSkillVfx>()!=null).ToArray())o.Call("Update");C(Live().Any(o=>o.name.EndsWith("Finale short tail")),"confirmed last-hit tail survives immediate result transition");
for(int f=0;f<6;f++)foreach(var o in Live().Where(o=>o.GetComponent<FilledSkillVfx>()!=null).ToArray())o.Call("Update");
C(CombatVisualLease.Active==0&&game.Enemies.Sum(x=>x.Hits.Count)==hits,"terminal presentation expires with no residual or extra damage");
}Console.WriteLine("PASS: "+n+" integrated dense actual area/burn-cash/VFX/lease checks, cap12; managed HP/terminal boundary");}}
'''
with tempfile.TemporaryDirectory(prefix='dense-finale-') as tmp:
 p=Path(tmp)
 for f in ['Core/CombatVisualBudget','Core/CombatImpactBatch','Core/ScheduledTickWindow','Core/BurnFinaleReceipts','Core/FilledVfxRecipes','Core/FilledVfxPlacement','Combat/CombatVisualLease','Combat/FilledSkillVfx','Combat/AnchoredImpactMesh','Combat/EnemyStatusEffects','Combat/CombatDamage','Combat/PlayerController.BurnFeedback']:(p/(Path(f).name+'.cs')).write_text((r/('Assets/Scripts/'+f+'.cs')).read_text())
 (p/'Shell.cs').write_text(s+extra);(p/'Player.cs').write_text('using UnityEngine;namespace Emberfall{public sealed partial class PlayerController{'+area+'}}');(p/'Test.cs').write_text(test)
 (p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>');project=p/'Test.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>');include_cast_receipt_source(project)
 for old in [False,True]:
  if old:
   f=p/'FilledSkillVfx.cs';f.write_text(f.read_text().replace('priority:CombatVisualPriority.RealContact);','priority:finale?CombatVisualPriority.Finale:CombatVisualPriority.RealContact);'))
  q=subprocess.run([dotnet,'build',str(project),'--configfile',str(p/'NuGet.Config'),'-v:q'],capture_output=True,text=True);assert q.returncode==0,q.stdout+q.stderr
  q=subprocess.run([dotnet,str(p/'bin/Debug/net8.0/Test.dll')],capture_output=True,text=True)
  if old:assert q.returncode and 'actual dense cashouts retain main at cap12' in q.stderr,q.stdout+q.stderr;print('PASS: compiled old actual cashout priority fails integrated main retention')
  else:print(q.stdout,end='');assert q.returncode==0,q.stderr
