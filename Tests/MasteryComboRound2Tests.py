#!/usr/bin/env python3
"""Production core/payoff tests; Unity/game edges are explicit shims."""
from CastReceiptFixtureSources import include_cast_receipt_source
import os,sys,tempfile,subprocess
from pathlib import Path
root=Path(__file__).resolve().parents[1]
def member(s,k):
 a=s.index(k);b=s.index('{',a)+1;n=1
 while n:n+=(s[b]=='{')-(s[b]=='}');b+=1
 return s[a:b]
shell=r'''
using System;using System.Collections.Generic;using UnityEngine;
namespace UnityEngine {
 public struct Vector3 {public static Vector3 zero=>default;public static Vector3 up=>default;public float magnitude=>0;public Vector3 normalized=>this;public static Vector3 operator+(Vector3 a,Vector3 b)=>a;public static Vector3 operator-(Vector3 a,Vector3 b)=>a;public static Vector3 operator*(Vector3 a,float b)=>a;}
 public struct Color{public Color(float r,float g,float b){}} public class Transform{public Vector3 position;}
}
namespace Emberfall {
 public enum MasteryType{Offense,Vitality,Guard,Technique} public static class MasteryProgressionRules{public const int InitialInvestment=10,EnhancedInvestment=20;}
 public class EnemyController {public bool IsDead,IsBoss,Ignore;public float Health=1000,HitFootprintBonus;public int Hits;public bool LastCritical,LastImpact;public Transform transform=new Transform();public void TakeDamage(float n,Vector3 d,float knockback=0,float stun=0,bool impact=true,bool critical=false,int practiceCastId=0){if(Ignore||IsDead)return;Health-=n;Hits++;LastCritical=critical;LastImpact=impact;IsDead=Health<=0;}}
 public class GameSession{public PlayerController Player;public bool HasStarted=true,InputBlocked,CombatEnded;public int Records,Feedback;public List<EnemyController> Enemies=new List<EnemyController>();public void RecordCombatAction(string s){Records++;}public void SpawnMechanismText(Vector3 at,string s,Color c){Feedback++;}}
 public struct CombatDamage{public float Amount;public bool IsCritical;public CombatDamage(float a){Amount=a;IsCritical=false;}}
 public class ProjectileVolleyBudget<T>{public CombatDamage Apply(T e,CombatDamage d,bool area)=>d;}
 public static class CombatImpactBatch{public static void Begin(){}public static void End(){}}
 public static class DestructibleProp{public static void StrikeArea(PlayerController p,Vector3 a,float r,CombatDamage d,int id){}}
 public static class CombatFx{public static Vector3 Flat(Vector3 p)=>p;}
 public static class CombatSight{public static bool Area(Vector3 a,Vector3 b)=>true;}
 public static class FilledSkillVfx{public static void ConfirmFinale(PlayerController p,int id){}}
 public sealed partial class PlayerController {
 readonly CastFirstHitRegistry fixtureCasts=new CastFirstHitRegistry();CastFirstHitReceipt liveCast;public void PrimeCast(int castId){liveCast=fixtureCasts.Issue(castId);}internal CastFirstHitReceipt CaptureCastReceipt(int castId)=>fixtureCasts.Find(castId);
 public MasteryCoreRuntime masteryCore=new MasteryCoreRuntime();public GameSession session;public bool IsDead;public float CombatAttack=100;int id;
 public PlayerController(){session=new GameSession{Player=this};masteryCore.Configure(0,10);}
 bool ValidAimTarget(EnemyController e)=>e!=null&&!e.IsDead;int NewCastId(){int castId=++id;PrimeCast(castId);return castId;}void ApplySpellDodgeBoon(EnemyController e){}
 public void Pay(EnemyController e)=>SettleMasteryCombo(e);
 }
}
class Program{
 static int n;static void C(bool v,string s){n++;if(!v)throw new Exception(s);}static bool N(float a,float b)=>Math.Abs(a-b)<.001f;
 static void Main(){
 foreach(int rank in new[]{10,20}){
 var c=new Emberfall.MasteryCoreRuntime();c.Configure(0,rank);float power=rank==10?.6f:1f,cd=rank==10?6:4;
 for(int i=0;i<100;i++)C(c.BasicHit()==0,"basic spam cannot arm");
 c.SkillSpent(100);C(c.ComboRemaining==0,"empty skill does not arm");c.SkillHit(1);C(N(c.ComboRemaining,6),"ready six seconds");for(int i=0;i<100;i++)c.SkillHit(1);
 C(N(c.BasicHit(),power),"new coefficient");C(c.BasicHit()==0&&c.ComboRemaining==0,"one charge only");
 c.SkillHit(2);c.Advance(cd);c.SkillHit(2);c.SkillHit(1);C(c.BasicHit()==0,"lingering and cooldown-seen casts never rearm");
 c.SkillHit(3);c.Advance(5.999f);C(N(c.BasicHit(),power),"window last instant");c.Advance(cd-.001f);c.SkillHit(4);C(c.BasicHit()==0,"ICD boundary");c.Advance(.002f);c.SkillHit(4);C(c.BasicHit()==0,"discard cooldown first hit");
 c.SkillHit(5);c.Advance(6);C(c.BasicHit()==0,"six-second expiry");c.SkillHit(6);c.SkillHit(7);C(N(c.BasicHit(),power)&&c.BasicHit()==0,"skill spam cannot stock charges");c.Advance(cd);c.SkillHit(6);C(c.BasicHit()==0,"older multihit cannot recycle");
 c.Reset();c.SkillHit(1);C(N(c.BasicHit(),power),"room reset");c.Advance(cd);c.SkillHit(2);c.Configure(1,rank);C(c.BasicHit()==0&&c.ComboRemaining==0,"core swap clears readiness");C(N(c.DamageTaken(.4f),rank==10?.03f:.05f),"vitality unchanged");
 c.Configure(2,rank);c.SkillHit(100);C(c.BasicHit()==0&&N(c.PerfectDodge(),rank==10?2:3),"guard isolated unchanged");c.Configure(3,rank);c.SkillHit(101);C(c.BasicHit()==0&&N(c.SkillSpent(rank==10?60:45).Energy,rank==10?8:12),"technique isolated unchanged");}
 var h=new Emberfall.PlayerController();var e=new Emberfall.EnemyController();h.masteryCore.SkillHit(1);h.Pay(e);C(N(e.Health,940)&&e.Hits==1&&!e.LastCritical&&!e.LastImpact,"separate noncritical low-impact payoff");C(h.session.Records==1&&h.session.Feedback==1,"one actual payoff feedback");h.Pay(e);C(e.Hits==1,"no recursive payoff");
 h.masteryCore.Advance(6);h.masteryCore.SkillHit(2);e.IsDead=true;h.Pay(e);C(h.MasteryComboReady,"dead target cannot waste ready charge");e.IsDead=false;e.Ignore=true;h.Pay(e);C(h.session.Feedback==1,"no false feedback on ignored damage");
 h.masteryCore.Reset();h.session.Enemies.Add(e);e.Ignore=false;h.HitArea(Vector3.zero,2,new Emberfall.CombatDamage(1));C(!h.MasteryComboReady,"passive area cannot arm combo");h.PrimeCast(200);h.HitArea(Vector3.zero,2,new Emberfall.CombatDamage(1),castId:200);C(h.MasteryComboReady,"actual skill area arms combo");h.session.InputBlocked=true;C(!h.MasteryComboReady,"blocked feedback hidden");h.session.InputBlocked=false;h.IsDead=true;C(!h.MasteryComboReady,"dead feedback hidden");
 Console.WriteLine("PASS "+n+" actual core/payoff/area assertions (managed shims, not Unity)");}}
'''
player=(root/'Assets/Scripts/Combat/PlayerController.cs').read_text()
with tempfile.TemporaryDirectory(prefix='core-round2-') as tmp:
 p=Path(tmp);core=p/'Core.cs';core.write_text((root/'Assets/Scripts/Core/MasteryCoreRuntime.cs').read_text());(p/'Pay.cs').write_text((root/'Assets/Scripts/Combat/PlayerController.MasteryCombo.cs').read_text());area=p/'Area.cs';area.write_text('using UnityEngine;namespace Emberfall{public sealed partial class PlayerController{'+member(player,'internal void HitArea(')+member(player,'internal void RegisterSkillHit(')+'}}');(p/'Tests.cs').write_text(shell)
 (p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>');proj=p/'Test.csproj';proj.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>');include_cast_receipt_source(proj)
 env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1');dotnet=sys.argv[1] if len(sys.argv)>1 else 'dotnet';originals={core:core.read_text(),area:area.read_text()}
 for mode in ['current','baseline-coefficient','baseline-reaction']:
  core.write_text(originals[core].replace('return Tier == 1 ? .60f : 1.00f;','return Tier == 1 ? .20f : .35f;') if mode=='baseline-coefficient' else originals[core]);area.write_text(originals[area].replace('if(confirmedSkillCast)RegisterSkillHit(castId);','RegisterSkillHit(castId);') if mode=='baseline-reaction' else originals[area])
  q=subprocess.run([dotnet,'build',str(proj),'--configfile',str(p/'NuGet.Config'),'-v:q'],env=env,capture_output=True,text=True);print(mode,'BUILD',q.stdout,q.stderr);assert q.returncode==0
  q=subprocess.run([dotnet,str(p/'bin/Debug/net8.0/Test.dll')],env=env,capture_output=True,text=True);print(mode,'RUN',q.stdout,q.stderr)
  if mode=='current':assert q.returncode==0
  else:assert q.returncode!=0 and {'baseline-coefficient':'new coefficient','baseline-reaction':'passive area cannot arm combo'}[mode] in q.stderr;print('PASS compiled baseline failure detected:',mode)
assert 'hero.BasicOpportunityWindow(true)'  in (root/'Assets/Scripts/UI/MobileControls.Feedback.cs').read_text()
assert 'hero.BasicOpportunityWindow(true)'  in (root/'Assets/Scripts/UI/GameUI.CombatOpportunities.cs').read_text()
print('PASS desktop/mobile ready source hooks (no rendered UI claim)')
