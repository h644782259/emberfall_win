#!/usr/bin/env python3
"""Real factory + authored bytes + AnimateHero + recovery + equipment. Managed TRS."""
from pathlib import Path
import tempfile,subprocess,sys,os
root=Path(__file__).resolve().parents[1];dotnet=sys.argv[1] if len(sys.argv)>1 else 'dotnet'
with tempfile.TemporaryDirectory(prefix='vanguard-recovery-') as temp:
 out=Path(temp)
 subprocess.run([sys.executable,str(root/'ArtSource/VanguardActions/export_actions.py'),str(out),dotnet],check=True)
 p=out/'export'
 (p/'CombatImpactBatch.cs').write_text((root/'Assets/Scripts/Core/CombatImpactBatch.cs').read_text())
 (p/'CampPracticeRecord.cs').write_text((root/'Assets/Scripts/Core/CampPracticeRecord.cs').read_text())
 ns={'__file__':str(root/'Tests/EquipmentCompositionProductionTests.py')};argv=sys.argv;sys.argv=['x',dotnet];exec((root/'Tests/EquipmentCompositionProductionTests.py').read_text().split('with tempfile.TemporaryDirectory')[0],ns);sys.argv=argv;extract=ns['extract']
 recovery=(root/'Assets/Scripts/Combat/CombatModel.Recovery.cs').read_text();model=(root/'Assets/Scripts/Combat/CombatModel.cs').read_text()
 motion=p/'MotionExport.cs';s=motion.read_text().replace('private void AdvanceVisualMotion(float dt){throw new Exception("clock must be frozen");}','').replace('private void ApplyVisualRecovery(float dt){}','')
 s=s[:-2]+'''private Transform[] recoveryJoints;private Quaternion[] recoveryRotations;private float recoveryAge=1,previousVisualYaw;private bool visualYawReady,recoveryCancellation,pilotCharging;private int visualMotionFrame=-1;
'''+''.join(extract(recovery,k) for k in ['private void BeginVisualRecovery(','private void ApplyVisualRecovery(','private void AdvanceVisualMotion('])+''.join(extract(model,k) for k in ['public void PlayAction(','public void CancelAction(','private void CommitActionPose('])+r'''
public void IntegrationCheck(){
 int checks=0;Action<bool,string> check=(ok,label)=>{checks++;if(!ok)throw new Exception(label);};
 Func<Quaternion,Quaternion,bool> same=(a,b)=>Math.Abs(System.Numerics.Quaternion.Dot(a.q,b.q))>.999999f;
 ConfigureVanguardArt();check(vanguardArt!=null,"actual binary selected on real factory");isolatedPreview=false;Time.time=4;Time.deltaTime=0;Time.frameCount=3;
 var bodyBefore=spine;var library=vanguardArt;
 // Real synchronous attack commit uses the original .52 contact phase with new art loaded.
 PlayAction(0,true,.46f);check(Math.Abs(actionAge/actionDuration-.52f)<.00001f,"committed contact phase unchanged");
 var committed=spine.localRotation;var arm=rightArm.localRotation;var grip=swordRig;Vector3 committedTip;check(TryGetWeaponVisualAnchor(WeaponVisualAnchor.SwordTip,out committedTip),"real committed sword tip available");
 var expected=Quaternion.Euler(0,0,0)*Pose(Vector3.zero,new Vector3(-8,WeaponSwingSide*-29f,-7),new Vector3(10,WeaponSwingSide*34f,9),.52f);
 expected*=Quaternion.Euler(vanguardArt.Sample(VanguardArtPose.Basic,1,.52f));
 check(same(committed,expected),"contact includes real authored torso offset");
 CancelAction();check(actionDuration==0&&actionAge==0,"cancel clears gameplay action");AnimateHero(0,0,false,0);
 check(same(spine.localRotation,committed)&&same(rightArm.localRotation,arm),"cancel at zero delta preserves complete authored contact pose");Vector3 cancelTip;check(TryGetWeaponVisualAnchor(WeaponVisualAnchor.SwordTip,out cancelTip)&&(cancelTip-committedTip).magnitude<.0001f,"cancellation preserves actual world-space sword contact tip");
 ApplyVisualRecovery(.06f);check(recoveryAge==.06f,"real recovery clock advances");AnimateHero(0,0,false,0);check(same(spine.localRotation,Quaternion.Slerp(Quaternion.identity,committed,.5f)),"actual half recovery blends authored contact into idle");
 ApplyVisualRecovery(.06f);AnimateHero(0,0,false,0);check(!same(spine.localRotation,committed),"recovery releases complete authored pose to idle");
 // Cancellation/replacement takes old head/cloth continuity but commits the new attack now.
 PlayAction(0,false);var skillArm=rightArm.localRotation;CancelAction();AnimateHero(0,0,false,0);PlayAction(0,true,.46f);
 check(Math.Abs(actionAge/actionDuration-.52f)<.00001f&&!recoveryCancellation,"new attack is not trapped in cancellation recovery");
 check(!same(rightArm.localRotation,skillArm),"replacement attack commits its own right arm");
 for(int upgrade=0;upgrade<4;upgrade++){
  var item=new ItemData{id="real-upgrade",slot=ItemSlot.Weapon,level=60,rarity=Rarity.Epic,upgradeLevel=upgrade};
  ApplyEquipment(item,null,null);PlayAction(0,true,.46f);
  check(spine==bodyBefore&&swordRig==grip&&vanguardArt==library,"equipment keeps body joints and selected library");
  check(equipmentWeapon!=null&&equipmentWeapon.parent==swordRig,"real equipment builder retains animated wrist binding");
  CancelAction();AnimateHero(0,0,false,0);ApplyVisualRecovery(.12f);
 }
 ApplyEquipment(null,null,null);check(spine==bodyBefore&&vanguardArt==library,"unequip keeps full motion identity");
 // Counter variant uses a real thrust contact, then the same cancellation/recovery ownership.
 PlayAction(-2,true,.46f);
 check(Math.Abs(actionAge/actionDuration-.52f)<.00001f,"counter thrust keeps existing basic contact clock");
 check(same(rightArm.localRotation,Pose(new Vector3(-18,0,12),new Vector3(-60,-8,12),new Vector3(-92,0,4),.52f)),"counter commits narrow thrust arm rather than cleave");
 Vector3 thrustTip;check(TryGetWeaponVisualAnchor(WeaponVisualAnchor.SwordTip,out thrustTip),"counter actual sword tip available");
 CancelAction();AnimateHero(0,0,false,0);Vector3 heldTip;
 check(TryGetWeaponVisualAnchor(WeaponVisualAnchor.SwordTip,out heldTip)&&(heldTip-thrustTip).magnitude<.0001f,"counter cancellation preserves world contact tip");
 ApplyVisualRecovery(.12f);AnimateHero(0,0,false,0);
 AnimateHero(0,0,false,0);var beforeDeath=spine.localRotation;pilotOwnerDead=true;var death=new DeathSessionProbe();death.OnPlayerDied();check(Time.timeScale==0,"actual OnPlayerDied pauses scaled clock");
 check(death.PreserveCalls==1&&death.SaveCalls==1,"death reaches explicit successful persistence boundary once");
 Time.timeScale=1;var failedDeath=new DeathSessionProbe(false);failedDeath.OnPlayerDied();
 check(failedDeath.IsDead&&Time.timeScale==0,"failed persistence still freezes actual death clock");
 check(failedDeath.PreserveCalls==1&&failedDeath.SaveCalls==0&&failedDeath.Notification=="fixture preserve failure","failed preservation skips Save and keeps its error");
 failedDeath.OnPlayerDied();check(failedDeath.PreserveCalls==1&&failedDeath.SaveCalls==0,"repeated death callback does not retry or pay twice");Time.deltaTime=0;
 var frozen=Time.time;SampleVanguardDeath();var terminal=spine.localRotation;
 check(same(terminal,beforeDeath*Quaternion.Euler(vanguardArt.Sample(VanguardArtPose.Death,1,1))),"death terminal pose reached while scaled time frozen");
 for(int i=0;i<30;i++){SampleVanguardDeath();check(Time.time==frozen&&same(spine.localRotation,terminal),"paused death terminal never accumulates or advances gameplay time");}
 pilotOwnerDead=false;SampleVanguardDeath();check(vanguardDeathRest==null,"revive clears terminal snapshot");
 Console.WriteLine("PASS "+checks+" real factory/authored/recovery/cancel/equipment/paused-death integration assertions; managed numerical TRS, not Unity");
}
}}
'''
 motion.write_text(s)
 import re
 types=p/'Types.cs';types.write_text(types.read_text()+'namespace Emberfall {'+re.search(r'public enum EnemyKind\s*\{[^}]*\}',(root/'Assets/Scripts/Core/GameTypes.cs').read_text()).group(0)+'}')
 (p/'CombatBalance.cs').write_text((root/'Assets/Scripts/Core/CombatBalance.cs').read_text())
 session=(root/'Assets/Scripts/Core/GameSession.cs').read_text()
 (p/'ApplicationPauseState.cs').write_text((root/'Assets/Scripts/Core/ApplicationPauseState.cs').read_text())
 # Presentation/recovery suite: persistence is an explicit configurable boundary.
 # Real writes/receipts are tested by DeathLootPersistenceProductionTests; real death and pause methods remain below.
 pause='using UnityEngine;namespace Emberfall {enum ExpeditionModeFailure{PlayerDefeated}enum RoomFailureReason{Death}enum SoundCue{Death}static class GameAudio{public static void Play(SoundCue cue){}}class DeathSessionProbe {public bool RoomBranchChoiceOpen=>false;public bool PracticeActive=>false;public CampPracticeRecord PracticeRecord=>throw new System.InvalidOperationException("ordinary death cannot access practice result");public void EndPractice(string reason){throw new System.InvalidOperationException("ordinary death recovery probe cannot exit practice");}private readonly bool preserveResult;public int PreserveCalls;public int SaveCalls=>Progression.Saves;public string Notification;public DeathSessionProbe(bool preserve=true){preserveResult=preserve;}private bool PreserveWorldLoot(){PreserveCalls++;if(!preserveResult)Progression.LastError="fixture preserve failure";return preserveResult;}public bool IsDead,Paused,uiBlocking,DungeonSelectionOpen,ModeFinished,ChapterActive;public bool HasStarted=true;private ApplicationPauseState pauseState=new ApplicationPauseState();class Mode{public void Fail(ExpeditionModeFailure f){}}class Chain{public void Fail(RoomFailureReason f){}}class Choice{public bool AwaitingChoice;public void Cancel(){}}class ProfileData{public int gold=100;}class Progress{public string LastError;public int Saves;public ProfileData Profile=new ProfileData();public void AddGold(int n){Profile.gold+=n;}public void Save(){Saves++;}}private Mode ModeRun;private Chain RoomChainRun;private Choice pendingRoomChoice=new Choice(),RunChoices=new Choice();private Progress Progression=new Progress();private object LastRunSummary;private object BuildRunSummary(bool won){return null;}private void FailChapter(string s){}private void RecordRecapGoldLoss(int n){}private void Notify(string s){Notification=s;}'+extract(session,'public void OnPlayerDied(')+extract(session,'private void UpdateTimeScale(')+'}}'
 (p/'ActualDeathPause.cs').write_text(pause)
 (p/'SkillDamageBudgets.cs').write_text((root/'Assets/Scripts/Core/SkillDamageBudgets.cs').read_text())
 # Execute the shared catalog helper used by the actual skill budget, not a copied coefficient.
 (p/'BuildCatalogDamage.cs').write_text('namespace Emberfall{public static class BuildCatalog{'+extract((root/'Assets/Scripts/Core/GameTypes.cs').read_text(),'public static float CinderTrailTickMultiplier(')+'}}')
 fixture=p/'Fixture.cs';s=fixture.read_text().replace('public static int frameCount;','public static int frameCount;public static float timeScale=1;').replace('public new string name=>gameObject.name;','public Vector3 eulerAngles=>new Vector3(0,0,0);public new string name=>gameObject.name;').replace('public static class Mathf{','public static class Mathf{public static int FloorToInt(float f)=>(int)Math.Floor(f);public static float DeltaAngle(float a,float b){float d=Repeat(b-a,360);return d>180?d-360:d;}');fixture.write_text(s)
 (p/'Exporter.cs').write_text('using UnityEngine;using Emberfall;class Entry{static void Main(){CombatModel.Hero(new GameObject("test").transform,HeroClass.Vanguard).IntegrationCheck();}}')
 cmd=[dotnet,'run','--project',str(p/'Export.csproj')];env=dict(os.environ,DOTNET_CLI_HOME=str(out/'cli'))
 subprocess.run(cmd,env=env,check=True)
 # Require the integrated tests to reject losing either new-art or recovery ownership.
 original=motion.read_text()
 for label,before,after in [('skip-authored','ApplyAuthoredVanguardPose(acting,t,hurt);',''),('skip-recovery','ApplyVisualRecovery(dt);',''),('skip-counter-thrust','if(actionBasic && actionSkill == -2)','if(false)')]:
  assert before in original;motion.write_text(original.replace(before,after));r=subprocess.run(cmd,env=env,capture_output=True,text=True)
  assert r.returncode!=0 and 'Unhandled exception. System.Exception' in r.stderr,(label,r.stdout,r.stderr)
  print('PASS compiled integration negative control:',label)
 motion.write_text(original)

 art=p/'CombatModel.VanguardArt.cs';normal=art.read_text()
 art.write_text(normal.replace('private Quaternion[] vanguardDeathRest;','private Quaternion[] vanguardDeathRest;private float oldDeathStart;').replace('vanguardDeathRest=new Quaternion','oldDeathStart=Time.time;vanguardDeathRest=new Quaternion').replace('VanguardLayer(VanguardArtPose.Death,1,1);','VanguardLayer(VanguardArtPose.Death,Mathf.Clamp01((Time.time-oldDeathStart)/.35f),1);'))
 r=subprocess.run(cmd,env=env,capture_output=True,text=True)
 assert r.returncode!=0 and 'death terminal pose reached while scaled time frozen' in r.stderr,(r.stdout,r.stderr)
 print('PASS compiled old scaled-time death control rejected during pause')
 art.write_text(normal)
