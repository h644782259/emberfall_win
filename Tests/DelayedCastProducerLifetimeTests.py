#!/usr/bin/env python3
"""Actual producer acquisition statements and complete OnDisable bodies with engine-only shims."""
import os,sys,tempfile,subprocess,re
from pathlib import Path
root=Path(__file__).resolve().parents[1];dotnet=sys.argv[1] if len(sys.argv)>1 else 'dotnet'
ns={'__file__':str(root/'Tests/DelayedCastFirstHitTests.py')};exec((root/'Tests/DelayedCastFirstHitTests.py').read_text().split('with tempfile.TemporaryDirectory')[0],ns)
member=ns['member'];fixture=ns['fixture'].replace('Console.WriteLine("PASS "+n+', 'LifetimeTests.Run();Console.WriteLine("PASS "+n+')
parts=[]
specs=[('ProjectileLifetime','CombatEffects','class CombatProjectile','projectile','player','projectile.castReceipt=player.RetainCastReceipt(projectile.castId);'),('AreaLifetime','CombatEffects','class CombatArea','area','player','area.castReceipt=player.RetainCastReceipt(area.castId);'),('SequenceLifetime','AdvancedSkillSequence','class AdvancedSkillSequence','sequence','hero','sequence.castReceipt=hero.RetainCastReceipt(sequence.castId);'),('SummonerLifetime','SummonerSpell','class SummonerSpell','spell','player','spell.castReceipt=player.RetainCastReceipt(propCast);')]
for cls,file,start,var,owner,acquire in specs:
 source=(root/('Assets/Scripts/Combat/'+file+'.cs')).read_text();source=source[source.index(start):];assert acquire in source
 parts.append('public class '+cls+':IStop {private CastFirstHitReceipt castReceipt;private int castId,step,steps;private Dummy pendingTargets=new Dummy(),pendingTickTargets=new Dummy(),healingAura;private Batch arrowBatch;private Action hostileEnded;public '+cls+'(PlayerController '+owner+',int id){var '+var+'=this;castId=id;int propCast=id;'+acquire+'}public void Stop(){OnDisable();}'+member(source,'private void OnDisable(')+'}')
extra='''using System;using Emberfall;
public interface IStop{void Stop();}public class Dummy{public void Clear(){}public void Stop(){}}public struct Batch{public bool IsValid=>false;public void Retire(){}}
'''+''.join(parts)+'''
public static class LifetimeTests{
 static void C(bool b,string why){if(!b)throw new Exception(why);}
 public static void Run(){
 foreach(var factory in new Func<PlayerController,int,IStop>[] {(p,id)=>new ProjectileLifetime(p,id),(p,id)=>new AreaLifetime(p,id),(p,id)=>new SequenceLifetime(p,id),(p,id)=>new SummonerLifetime(p,id)}){
 var owner=new PlayerController();int id=owner.Issue();var producer=factory(owner,id);owner.Issue();GC.Collect();C(owner.CaptureCastReceipt(id)!=null,"production capture strongly retains old producer cast after GC/new cast");producer.Stop();C(owner.CaptureCastReceipt(id)==null,"production OnDisable retires final lease without GC");producer.Stop();C(owner.CaptureCastReceipt(id)==null,"repeated disable idempotent");}
 var hero=new PlayerController();int cast=hero.Issue();var parent=new SequenceLifetime(hero,cast);hero.Issue();var child=new AreaLifetime(hero,cast);parent.Stop();C(hero.CaptureCastReceipt(cast)!=null,"child keeps same-cast receipt after parent sequence ends");child.Stop();C(hero.CaptureCastReceipt(cast)==null,"last child releases cast");
 Console.WriteLine("PASS 14 production producer acquisition/OnDisable lifetime assertions; engine construction is a shim");}}
'''
with tempfile.TemporaryDirectory(prefix='cast-producer-') as tmp:
 p=Path(tmp)
 for f in ['Core/CastFirstHitReceipt','Core/MasteryCoreRuntime','Combat/EnemyControlPolicy','Combat/PlayerController.CastReceipts']:(p/(Path(f).name+'.cs')).write_text((root/('Assets/Scripts/'+f+'.cs')).read_text())
 (p/'Fixture.cs').write_text(fixture);(p/'Lifetimes.cs').write_text(extra);(p/'Hooks.cs').write_text('using UnityEngine;namespace Emberfall{public sealed partial class PlayerController{'+member(ns['player'],'internal int NewCastId(')+member(ns['player'],'internal void RegisterSkillHit(')+'}public partial class EnemyController{'+member(ns['enemy'],'internal bool TrySkillInterrupt(')+'}}');(p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>');proj=p/'Test.csproj';proj.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><NoWarn>0649;0414;0219</NoWarn></PropertyGroup></Project>');env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1');file=p/'Lifetimes.cs'
 for mode in ['current','missing-producer-release']:
  file.write_text(extra if mode=='current' else extra.replace('castReceipt?.Release();castReceipt=null;','castReceipt=null;'))
  q=subprocess.run([dotnet,'build',str(proj),'--configfile',str(p/'NuGet.Config'),'-v:q'],env=env,capture_output=True,text=True);print(mode,'BUILD',q.stdout,q.stderr);assert q.returncode==0
  q=subprocess.run([dotnet,str(p/'bin/Debug/net8.0/Test.dll')],env=env,capture_output=True,text=True);print(mode,'RUN',q.stdout,q.stderr)
  if mode=='current':assert q.returncode==0
  else:assert q.returncode and 'production OnDisable retires final lease without GC' in q.stderr;print('PASS compiled missing-release control fails actual lifetime boundary')
