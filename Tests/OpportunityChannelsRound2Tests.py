#!/usr/bin/env python3
"""Production window observations and result channel; explicit managed engine edges."""
import os,sys,tempfile,subprocess
from pathlib import Path
root=Path(__file__).resolve().parents[1];dotnet=sys.argv[1] if len(sys.argv)>1 else 'dotnet'
ns={'__file__':str(root/'Tests/ShatterAvailabilityTests.py')};exec((root/'Tests/ShatterAvailabilityTests.py').read_text().split('with tempfile.TemporaryDirectory')[0],ns)
test=r'''
using System;using Emberfall;using UnityEngine;
class Program{
 static int n;static void C(bool b,string s){n++;if(!b)throw new Exception(s);}
 static void Main(){
 var p=new PlayerController();p.session.Player=p;p.session.Progression.Profile.skillRanks[1]=1;p.aimPoint=new Vector3(0,0,8);var e=new EnemyController(8,true);p.session.Enemies.Add(e);p.AimTarget=e;
 C(p.SkillOpportunityWindow(1).Actionable,"actual ready footprint");p.Energy=0;var w=p.SkillOpportunityWindow(1);C(w.Window&&!w.Actionable&&w.Remaining==4&&w.BlockReason=="缺能","energy preserves clock without execution");e.StatusEffects.FrostTime=.2f;C(p.SkillOpportunityWindow(1).Remaining==.2f,"real decreasing clock");p.Energy=100;
 CombatSight.Wall=5;C(p.SkillOpportunityWindow(1).Window&&!p.SkillOpportunityWindow(1).Actionable,"wall keeps intended status clock blocked");CombatSight.Wall=float.PositiveInfinity;
 e.IsDead=true;C(!p.SkillOpportunityWindow(1).Window,"dead target clears window");e.IsDead=false;e.StatusEffects.HasFrostMark=false;C(!p.SkillOpportunityWindow(1).Window,"expired status clears clock");e.StatusEffects.HasFrostMark=true;
 var other=new EnemyController(2,false);p.session.Enemies.Add(other);p.AimTarget=other;p.aimPoint=other.transform.position;C(!p.SkillOpportunityWindow(1).Window,"new aim does not retain prior target clock");p.AimTarget=e;p.aimPoint=e.transform.position;
 p.session.InputBlocked=true;C(!p.SkillOpportunityWindow(1).Window,"pause hides windows");p.session.InputBlocked=false;p.session.Player=new PlayerController();C(!p.SkillOpportunityWindow(1).Window,"owner replacement clears windows");p.session.Player=p;
 p.Specialization=ElementalistSpecialization.Burn;p.session.Progression.Profile.skillRanks[9]=1;e.StatusEffects.IsBurning=true;p.Energy=0;C(p.SkillOpportunityWindow(1).Kind==CombatOpportunityKind.Reignite&&p.SkillOpportunityWindow(9).Kind==CombatOpportunityKind.BurnFinale,"burn windows distinct even blocked");e.StatusEffects.OwnedBurn=false;C(!p.SkillOpportunityWindow(9).Window,"foreign burn not own payoff");
 p.HeroClass=HeroClass.Ranger;p.session.Progression.Profile.skillRanks[0]=1;e.StatusEffects.PoisonStacks=3;C(p.SkillOpportunityWindow(0).Remaining==2&&!p.SkillOpportunityWindow(0).Actionable,"poison ownership clock survives shortage");
 p.HeroClass=HeroClass.Summoner;p.session.Progression.Profile.skillRanks[2]=1;SummonedCompanion.Opportunity=3.25f;C(p.SkillOpportunityWindow(2).Remaining==3.25f,"contract actual remaining");
 p.Energy=100;p.AimTarget=null;C(p.SkillOpportunityWindow(2).Window&&!p.SkillOpportunityWindow(2).Actionable&&p.SkillOpportunityWindow(2).BlockReason=="无目标"&&p.SkillOpportunityWindow(2).Remaining==3.25f,"summoner no target preserves actual window but is not actionable");p.AimTarget=e;C(p.SkillOpportunityWindow(2).Actionable,"summoner living target restores actionability");p.HeroClass=HeroClass.Vanguard;p.CounterOpportunityRemaining=.8f;p.masteryCore.Configure(0,10);p.masteryCore.SkillHit(1);p.attackCooldown=.2f;C(p.BasicOpportunityWindow().Window&&!p.BasicOpportunityWindow().Actionable&&p.BasicOpportunityWindow(true).Kind==CombatOpportunityKind.MasteryCombo,"counter and mastery share blocked model");p.attackCooldown=0;C(p.BasicOpportunityWindow(true).BlockReason=="距离不足","out of reach keeps basic clock blocked");p.AimTarget=null;p.attackCooldown=0;C(p.BasicOpportunityWindow(true).BlockReason=="无目标","no legal target keeps combo blocked");p.masteryCore.Advance(6);C(!p.BasicOpportunityWindow(true).Window,"combo real expiry");
 var channel=new CombatResultChannel();object owner=new object(),target=new object();var hit=new CombatOpportunityState(CombatOpportunityKind.BurnCash,2,3,receipt:7);var timer=new CombatOpportunityState(CombatOpportunityKind.Shatter,1.2f,blockReason:"缺能");
 for(int i=0;i<100;i++){C(channel.Observe(hit,owner,1,target,false)=="兑燃 ×3","result display");C(timer.Caption=="碎冰 1.2"&&!timer.Actionable,"repeated result never replaces window");}
 C(channel.Observe(hit,owner,1,target,true)==""&&channel.Observe(hit,owner,1,target,false)=="","pause does not replay old result");hit=new CombatOpportunityState(CombatOpportunityKind.BurnCash,2,1,receipt:8);C(channel.Observe(hit,owner,1,target,false)!="","new receipt after pause");target=new object();C(channel.Observe(hit,owner,1,target,false)==""&&channel.Observe(hit,owner,1,target,false)=="","target switch never replays old receipt");C(channel.Observe(hit,owner,2,null,false)=="","death/epoch clears result");
 p.HeroClass=HeroClass.Arcanist;p.Specialization=ElementalistSpecialization.Shatter;p.AimTarget=e;p.aimPoint=e.transform.position;p.Energy=0;var ui=new GameUI(p.session);ui.Read();C(ui.Read()=="目标霜痕","result cannot mask persistent HUD opportunity");
 Console.WriteLine("PASS "+n+" production window/result-channel assertions; managed only");}}
'''
with tempfile.TemporaryDirectory(prefix='opportunity-channels-') as tmp:
 p=Path(tmp)
 for f in ['Core/GameTypes','Core/CombatBalance','Combat/MobileSkillPolicy','UI/CombatOpportunityPresentation','Core/CombatOpportunityState','Core/CombatResultChannel','Core/MasteryCoreRuntime','Core/CastFirstHitReceipt','Combat/PlayerController.Opportunities','Combat/PlayerController.OpportunityWindows']:(p/(Path(f).name+'.cs')).write_text((root/('Assets/Scripts/'+f+'.cs')).read_text())
 fixture=(root/'Tests/ShatterAvailabilityTests.cs').read_text().replace('HasStarted=true,InputBlocked;','HasStarted=true,InputBlocked,CombatEnded;').replace('private int burnFeedbackCast;','private int burnFeedbackCast=7;public MasteryCoreRuntime masteryCore=new MasteryCoreRuntime();public string MobilePinnedActionReason(int skill)=>PinAllowed?"":"被遮挡";').replace('targets=0;remaining=0;return false;','targets=3;remaining=2;return true;')
 (p/'Fixture.cs').write_text(fixture);ui=p/'UI.cs';ui.write_text('namespace Emberfall{public partial class GameUI{'+ns['extract']((root/'Assets/Scripts/UI/GameUI.CombatOpportunities.cs').read_text(),'private string CurrentCombatOpportunity(')+'}}');(p/'Player.cs').write_text('using UnityEngine;namespace Emberfall{public sealed partial class PlayerController{'+ns['methods']+'}public class SkillTargetingController{'+ns['preview']+'}}');(p/'Tests.cs').write_text(test)
 (p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>');proj=p/'Test.csproj';proj.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>');env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1');original=ui.read_text();windowFile=p/'PlayerController.OpportunityWindows.cs';originalWindow=windowFile.read_text()
 for mode in ['current','old-result-masks-window','old-ready-only','window-all-actionable']:
  windowFile.write_text(originalWindow.replace('return new CombatOpportunityState(kind,remaining,blockReason:OpportunityBlockReason(skill),duration:duration);','return default;') if mode=='old-ready-only' else originalWindow.replace('blockReason:OpportunityBlockReason(skill)','blockReason:""') if mode=='window-all-actionable' else originalWindow)
  ui.write_text(original if mode!='old-result-masks-window' else original.replace('if(hero.HeroClass==HeroClass.Vanguard)','var result=hero.LatestCombatResult();if(result.Kind!=CombatOpportunityKind.None)return result.Caption;\n            if(hero.HeroClass==HeroClass.Vanguard)'))
  q=subprocess.run([dotnet,'build',str(proj),'--configfile',str(p/'NuGet.Config'),'-v:q'],env=env,capture_output=True,text=True);print(mode,'BUILD',q.stdout,q.stderr);assert q.returncode==0
  q=subprocess.run([dotnet,str(p/'bin/Debug/net8.0/Test.dll')],env=env,capture_output=True,text=True);print(mode,'RUN',q.stdout,q.stderr)
  if mode=='current':assert q.returncode==0
  else:
   oracle='result cannot mask persistent HUD opportunity' if mode=='old-result-masks-window' else 'energy preserves clock without execution'
   assert q.returncode!=0 and oracle in q.stderr;print('PASS compiled negative control:',mode)
print('PASS no Unity rendering/input/physics claim')
