#!/usr/bin/env python3
"""Production Update walking/aim tail -> FaceAim -> BasicAttack visual commit -> sampler.
Input/aim/traversal/Unity are managed boundaries; BasicAttack is deliberately truncated
before damage/effect dispatch. Never claims the full Update or gameplay damage ran.
"""
import os,sys,tempfile,subprocess
from pathlib import Path
r=Path(__file__).resolve().parents[1];dotnet=sys.argv[1] if len(sys.argv)>1 else 'dotnet'
ns={'__file__':str(r/'Tests/BlenderPilotAdapterProductionTests.py')};exec((r/'Tests/BlenderPilotAdapterProductionTests.py').read_text().split('with tempfile.TemporaryDirectory')[0],ns)
member=ns['member'];model=ns['model'];player=(r/'Assets/Scripts/Combat/PlayerController.cs').read_text();motion=(r/'Assets/Scripts/Combat/CombatModel.Motion.cs').read_text()
update=member(player,'private void Update(')
walk=update[update.index('                Vector3 walkingStart'):update.index('\n            }\n            // Readiness')]
tail=update[update.index('            Vector3 beforeBoundary'):update.index('\n            if (charge != null && charge.IsCharging) model.AnimateCharge')]
assert tail.index('model.SetLocomotion(')<tail.index('aimPoint =')<tail.index('FaceAim();')<tail.index('BasicAttack();')<tail.index('model.Animate(')
basic=member(player,'private void BasicAttack(');basic=basic[:basic.index('            Color color =')]+'}finally{CombatImpactBatch.EndAction();}}'
assert basic.index('FaceAim();')<basic.index('model.PlayAction(')
fixture=(r/'Tests/BlenderPilotAdapterProductionFixture.cs').read_text();fixture=fixture.replace(member(fixture,'static void Main()'),'static void Main(){PilotFacingFixture.Run();}')
fixture=fixture.replace(member(fixture,'public class Motion{'),'''public class Motion{public float Landing,Side,Forward,Phase,Speed;public int Advances;private LocomotionPoseState actual=new LocomotionPoseState();public void Advance(float x,float z,float dt,float speed,bool walk,bool air,float jump){Advances++;actual.Advance(x,z,dt,speed,walk,air,jump);Landing=actual.Landing;Side=actual.Side;Forward=actual.Forward;Phase=actual.Phase;Speed=actual.Speed;}public void Reset(){actual.Reset();Landing=Side=Forward=Phase=Speed=0;}}''')
fixture=fixture.replace('public class GameSession{','public partial class GameSession{').replace('public class Profile{','public class Profile:GameProfile{').replace('public class PlayerStats{','public class PlayerStats{public float MoveSpeed=6;').replace('public class CancelState{','public class CancelState{public bool IsCharging,ConsumedThisFrame,Blocked;public bool TickInput()=>false;public void Begin(int skill){}')
fixture=fixture.replace('private object AimTarget,focusedEnemy;', 'private bool ReturningCounterVariant=>false;private bool ReturningCounterReady=>false;private object AimTarget,focusedEnemy;') # Optional mechanism disabled; ReturningCounterProductionTests owns it.
fixture=fixture.replace('public class RunBonus{','public class RunBonus{public float AttackSpeedMultiplier=1;').replace('public enum SoundCue{Hit}','public enum SoundCue{Hit,Attack}')
fixture=fixture.replace('public static class CombatFx{','public static class CombatFx{public static Vector3 Flat(Vector3 p)=>new Vector3(p.x,0,p.z);')
fixture=fixture.replace('public static class WorldTraversal{','public static class WorldTraversal{public static float SurfaceHeight(Vector3 p,float max=0)=>0;public static Vector3? Accepted;public static Vector3 Move(Vector3 p,Vector3 delta,float radius)=>p+(Accepted??delta);')
fixture=fixture.replace('private object AimTarget,focusedEnemy;','private EnemyController AimTarget;private object focusedEnemy;')
fixture=fixture.replace('public static Vector3 zero=>','public static Vector3 operator -(Vector3 a,Vector3 b)=>a+b*(-1);public static Vector3 ClampMagnitude(Vector3 v,float max)=>v.magnitude>max?v.normalized*max:v;public static Vector3 zero=>')
fixture=fixture.replace('public static Quaternion Inverse(', 'public static Quaternion LookRotation(Vector3 v)=>Euler(0,(float)(Math.Atan2(v.x,v.z)*180/Math.PI),0);public static Quaternion RotateTowards(Quaternion a,Quaternion b,float max)=>b;public static Quaternion Inverse(')
with tempfile.TemporaryDirectory(prefix='pilot-facing-') as tmp:
 p=Path(tmp)
 (p/'BuildCatalogDamage.cs').write_text('namespace Emberfall{public static class BuildCatalog{'+member((r/'Assets/Scripts/Core/GameTypes.cs').read_text(),'public static float CinderTrailTickMultiplier(')+'}}')
 for f in ['Combat/BlenderPilotVisual','Combat/CombatModel.BlenderPilot','Combat/CombatModel.WeaponRig','Core/RendererGroupCache','Core/BlenderPilotPosePolicy','Core/BasicActionTimeline','Core/SkillDamageBudgets','Core/CombatImpactBatch','Core/CombatBalance','Core/WeaponStructure','Core/LocomotionPoseState']:(p/(Path(f).name+'.cs')).write_text((r/('Assets/Scripts/'+f+'.cs')).read_text())
 for name in ['BlenderPilotLayerProductionFixture.cs','BlenderPilotReadinessTests.cs','PilotFacingCommitProductionFixture.cs']:(p/name).write_text((r/'Tests'/name).read_text())
 (p/'Fixture.cs').write_text(fixture)
 types=''.join(member((r/'Assets/Scripts/Core/GameTypes.cs').read_text(),s) for s in ['public enum EnemyKind','public enum FashionSlot','public enum ItemSlot','public enum Rarity','public enum EquipmentMechanic','public class ItemData'])
 (p/'Methods.cs').write_text('using UnityEngine;namespace Emberfall{'+types+'public sealed partial class CombatModel{'+ns['methods']+member(motion,'public void SetLocomotion(')+member(motion,'public void ResetLocomotion(')+'}}')
 (p/'Player.cs').write_text('using System;using UnityEngine;namespace Emberfall{public partial class PlayerController{'+member(player,'public void TakeDamageFrom(')+member(player,'internal void CancelCombatPose(')+member(player,'public void RefreshStats(')+member(player,'private void FaceAim(')+basic+'public void FacingFrame(Vector3 movement,float dt){bool mobile=false;float movementBonus=0;Vector3 walkingDisplacement;'+walk+tail+'}}}')
 (p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>');(p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>')
 for name,old,new in [('production',None,None),('missing-raw-facing','locomotion.Speed<=.05f || PilotMovingBasicFacing()','true'),('stale-basis','owner.InverseTransformDirection(pilotAcceptedWalkingWorld)','pilotAcceptedWalkingWorld')]:
  target=p/'CombatModel.BlenderPilot.cs';original=target.read_text()
  if old:assert old in original;target.write_text(original.replace(old,new))
  run=subprocess.run([dotnet,'build',str(p/'Test.csproj'),'--configfile',str(p/'NuGet.Config'),'-v:q'],capture_output=True,text=True);assert run.returncode==0,run.stdout+run.stderr
  run=subprocess.run([dotnet,str(p/'bin/Debug/net8.0/Test.dll')],capture_output=True,text=True);target.write_text(original)
  if old:assert run.returncode and 'current-facing moving basic rejects' in run.stderr,run.stdout+run.stderr;print('PASS: compiled '+name+' fails current-facing commit assertion')
  else:print(run.stdout,end='');assert run.returncode==0,run.stderr
