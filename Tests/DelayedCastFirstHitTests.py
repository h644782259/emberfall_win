#!/usr/bin/env python3
"""Actual actor registry/RegisterSkillHit/Enemy.TrySkillInterrupt plus bounded receipt policy."""
import os,sys,tempfile,subprocess
from pathlib import Path
root=Path(__file__).resolve().parents[1];dotnet=sys.argv[1] if len(sys.argv)>1 else 'dotnet'
def member(s,k):
 a=s.index(k);b=s.index('{',a)+1;n=1
 while n:n+=(s[b]=='{')-(s[b]=='}');b+=1
 return s[a:b]
player=(root/'Assets/Scripts/Combat/PlayerController.cs').read_text();enemy=(root/'Assets/Scripts/Combat/EnemyController.cs').read_text()
fixture=r'''
using System;using System.Collections.Generic;using Emberfall;using UnityEngine;
namespace UnityEngine{public struct Vector3{public static Vector3 up=>default;public static Vector3 operator+(Vector3 a,Vector3 b)=>a;public static Vector3 operator*(Vector3 a,float b)=>a;}public struct Color{public Color(float a,float b,float c){}}public static class Mathf{public static float Max(float a,float b)=>Math.Max(a,b);}public class Transform{public Vector3 position;}public class GameObject{public bool activeInHierarchy=true;}}
namespace Emberfall{
 public enum HeroClass{Vanguard,Arcanist,Ranger,Summoner}public enum MasteryType{Offense,Vitality,Guard,Technique}public static class MasteryProgressionRules{public const int InitialInvestment=10,EnhancedInvestment=20;}
 public class GameSession{public PlayerController Player;public bool HasStarted=true,InputBlocked,CombatEnded;public int Interrupts;public void SpawnMechanismText(Vector3 p,string s,Color c){Interrupts++;}}
 public static class WorldTraversal{public static bool HasLineOfSight(Vector3 a,Vector3 b)=>true;}
 public sealed partial class PlayerController{public int CombatEpoch=1;private int nextCastId=100;public bool IsDead;public HeroClass HeroClass=HeroClass.Arcanist;public Transform transform=new Transform();public GameSession session;public MasteryCoreRuntime masteryCore=new MasteryCoreRuntime();public PlayerController(){session=new GameSession{Player=this};masteryCore.Configure(0,10);}public int Issue()=>NewCastId();public void Hit(int id)=>RegisterSkillHit(id);public void Teleport(){CombatEpoch++;masteryCore.Reset();}public void Retire(){CombatEpoch++;}}
 public class Boss{public class Status{public bool Interruptible;}public Status State=new Status();public void InterruptWindup(){}}
 public partial class EnemyController{public bool IsDead,IsBoss=true,enabled=true,IsPreparingAttack;public GameObject gameObject=new GameObject();public Transform transform=new Transform();public GameSession session;public EnemyControlPolicy controlPolicy=new EnemyControlPolicy(EnemyControlTier.Boss);private PlayerController controlOwner;private int controlOwnerEpoch;public int attackNumber;float stunTime,attackCooldown;Boss largeBoss;void CancelAttack(bool interrupt){IsPreparingAttack=false;}public EnemyController(PlayerController source){session=source.session;}public void Windup(int id){attackNumber=id;IsPreparingAttack=true;}}
}
class Program{
 static int n;static void C(bool v,string text){n++;if(!v)throw new Exception(text);}
 static void Main(){
 var h=new PlayerController();int delayed=h.Issue();var keepDelayed=h.RetainCastReceipt(delayed);int quick=h.Issue();var keepQuick=h.RetainCastReceipt(quick);var boss=new EnemyController(h);
 C(delayed==101&&quick==102,"real actor issued delayed101 and quick102");C(!boss.TrySkillInterrupt(h,0,quick),"102 nova first contact outside warning consumes its target qualification");h.Hit(quick);C(h.masteryCore.BasicHit()==.6f,"102 core first hit pays");h.masteryCore.Advance(6);boss.Windup(1);C(boss.TrySkillInterrupt(h,1,delayed),"101 delayed meteor first contact can interrupt after102");h.Hit(delayed);C(h.masteryCore.BasicHit()==.6f,"101 reverse first contact can arm core after102");
 boss.controlPolicy.Advance(6);boss.Windup(2);C(!boss.TrySkillInterrupt(h,1,delayed)&&!boss.TrySkillInterrupt(h,0,quick),"same cast ticks cannot claim later warning");h.masteryCore.Advance(6);h.Hit(delayed);C(h.masteryCore.BasicHit()==0,"same delayed cast cannot rearm after cooldown");
 var other=new EnemyController(h);other.Windup(1);C(other.TrySkillInterrupt(h,1,delayed),"same cast first contact on another enemy qualifies independently");
 int active=h.Issue();var activeLease=h.RetainCastReceipt(active);h.Hit(active);C(h.masteryCore.BasicHit()==.6f,"new core cast pays");int during=h.Issue();var duringLease=h.RetainCastReceipt(during);h.Hit(during);h.masteryCore.Advance(6);h.Hit(during);C(h.masteryCore.BasicHit()==0,"first contact during cooldown consumed before gate");
 h.Retire();boss.controlPolicy.Advance(6);boss.Windup(3);C(!boss.TrySkillInterrupt(h,1,active),"epoch-only world retirement rejects old cast");h.Hit(during);C(h.masteryCore.BasicHit()==0,"old core epoch cannot mint current ticket");C(!activeLease.Active&&!keepDelayed.Active,"scope invalidates all old leases");
 h.Teleport();h.IsDead=true;int dead=h.Issue();h.Hit(dead);C(h.masteryCore.BasicHit()==0,"dead source cannot arm");h.IsDead=false;h.session.Player=new PlayerController();boss.Windup(4);C(!boss.TrySkillInterrupt(h,1,dead),"retired owner cannot interrupt");h.Hit(dead);C(h.masteryCore.BasicHit()==0,"retired owner cannot arm core");
 var ledger=new CastFirstHitRegistry();ledger.SetEpoch(1);var oldest=ledger.Issue(101);for(int i=102;i<30102;i++){var t=ledger.Issue(i);C(t!=null,"retired burst admissions reclaimed without GC or LRU");t.Release();}C(ledger.Count<=2&&ledger.Find(101)==oldest,"long active receipt retained across30000 later casts");C(oldest.FirstCoreHit()&&!oldest.FirstCoreHit(),"long active cast has exactly one first core hit");var policy=new EnemyControlPolicy(EnemyControlTier.Boss);float stagger;C(policy.TryInterrupt(oldest,1,true,true,out stagger),"long active cast first target remains eligible");policy.Advance(6);C(!policy.TryInterrupt(oldest,2,true,true,out stagger),"long cast repeated target never requalifies under pressure");
 var held=new List<CastFirstHitReceipt>();var full=new CastFirstHitRegistry();full.SetEpoch(1);for(int i=1;i<=CastFirstHitRegistry.MaximumLiveCasts;i++)held.Add(full.Issue(i));C(full.Issue(999)==null&&full.Count==256,"live registry hard capacity fail closed");C(full.Find(1)==held[0]&&held[0].FirstCoreHit()&&!held[0].FirstCoreHit(),"capacity never evicts or recreates active old receipt");held[1].Release();C(full.Issue(1000)!=null&&full.Find(2)==null&&full.Issue(2)==null,"retired cast ID cannot be reissued");
 var targetReceipt=held[0];for(int i=0;i<256;i++)C(targetReceipt.FirstInterruptTarget(new object()),"bounded per-target admission");C(!targetReceipt.FirstInterruptTarget(new object()),"per-cast target receipt cap never evicts old targets");
 full.SetEpoch(2);C(!held[0].Active&&!held[0].FirstCoreHit(),"reset invalidates strong stale leases");
 var actorStress=new PlayerController();int actorOld=actorStress.Issue();var actorLease=actorStress.RetainCastReceipt(actorOld);for(int i=0;i<30000;i++)actorStress.Issue();C(actorStress.CaptureCastReceipt(actorOld)==actorLease,"actual actor latest issuance releases while old producer stays live under30000 casts");actorStress.Hit(actorOld);C(actorStress.masteryCore.BasicHit()==.6f,"actual actor old retained cast still first-hits after pressure");
 // Existing pure-rule integer APIs remain order independent and bounded too.
 var pure=new MasteryCoreRuntime();pure.Configure(0,10);pure.SkillHit(102);pure.BasicHit();pure.Advance(6);pure.SkillHit(101);C(pure.BasicHit()==.6f,"legacy rule adapter supports reverse first hit");
 Console.WriteLine("PASS "+n+" production cast/actor/interrupt/core bounded-lifetime assertions; managed physics/session boundary");}}
'''
with tempfile.TemporaryDirectory(prefix='delayed-cast-') as tmp:
 p=Path(tmp)
 for f in ['Core/CastFirstHitReceipt','Core/MasteryCoreRuntime','Combat/EnemyControlPolicy','Combat/PlayerController.CastReceipts']:(p/(Path(f).name+'.cs')).write_text((root/('Assets/Scripts/'+f+'.cs')).read_text())
 (p/'Fixture.cs').write_text(fixture);(p/'PlayerHooks.cs').write_text('namespace Emberfall{public sealed partial class PlayerController{'+member(player,'internal int NewCastId(')+member(player,'internal void RegisterSkillHit(')+'}}');(p/'EnemyHook.cs').write_text('using UnityEngine;namespace Emberfall{public partial class EnemyController{'+member(enemy,'internal bool TrySkillInterrupt(')+'}}')
 (p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>');proj=p/'Test.csproj';proj.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><NoWarn>0649;0414</NoWarn></PropertyGroup></Project>');env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1');policy=p/'EnemyControlPolicy.cs';core=p/'MasteryCoreRuntime.cs';origPolicy=policy.read_text();origCore=core.read_text()
 for mode in ['current','old-newest-interrupt','old-newest-core']:
  policy.write_text(origPolicy if mode!='old-newest-interrupt' else origPolicy.replace('private int interruptedWindup;','private int interruptedWindup,newestControlCast;').replace('if (!eligibleSkill || cast==null || !cast.FirstInterruptTarget(this)) return false;','if (!eligibleSkill || cast==null || cast.Id<=newestControlCast) return false;newestControlCast=cast.Id;'))
  core.write_text(origCore if mode!='old-newest-core' else origCore.replace('private readonly CastFirstHitHistory standaloneCasts','private int lastCast;private readonly CastFirstHitHistory standaloneCasts').replace('cast==null||!cast.FirstCoreHit()','cast==null||cast.Id<=lastCast').replace('// First impact during cooldown','lastCast=cast.Id;\n            // First impact during cooldown'))
  q=subprocess.run([dotnet,'build',str(proj),'--configfile',str(p/'NuGet.Config'),'-v:q'],env=env,capture_output=True,text=True);print(mode,'BUILD',q.stdout,q.stderr);assert q.returncode==0
  q=subprocess.run([dotnet,str(p/'bin/Debug/net8.0/Test.dll')],env=env,capture_output=True,text=True);print(mode,'RUN',q.stdout,q.stderr)
  if mode=='current':assert q.returncode==0
  else:
   oracle='101 delayed meteor first contact can interrupt after102' if mode=='old-newest-interrupt' else '101 reverse first contact can arm core after102'
   assert q.returncode!=0 and oracle in q.stderr;print('PASS compiled exact old monotonic gate rejected:',mode)
