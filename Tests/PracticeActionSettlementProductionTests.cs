using System;using System.Collections.Generic;using UnityEngine;
namespace Emberfall {
 public enum ThreatTier{Normal,Elite,Boss}public enum RunBlessing{RiskContract}
 public partial class GameSession {
  public bool PracticeActive=true,ModeFinished,InDungeon,DungeonCleared,uiBlocking,RoomBranchChoiceOpen,DungeonSelectionOpen;public State RunChoices=new State(),pauseState=new State();public class State{public bool AwaitingChoice,BackgroundPaused;}
  public CampPracticeRecord PracticeRecord=new CampPracticeRecord(CampPracticeScenario.GuardAndWispPressure,10,"actual sequence");
  public bool HasBlessing(RunBlessing b)=>false;
  public float RoomSupportMultiplier(EnemyController e)=>1;public float ChapterSupportMultiplier(EnemyController e)=>1;
  public void SpawnCombatDamage(params object[] a){}public void SpawnFloatingText(params object[] a){}
 }
 public class PlayerStats{public float Armor,Damage=100;}
 public class Opening{public bool RecordHit(params object[] a)=>false;}
 public class Targeting{public void Cancel(){}}
 public class GuardArmorVisual{public void RecordImpact(bool a,bool b){}}
 public class BossState{public float IncomingMultiplier=1;public bool OwnsAttacks;}
 public class LargeBoss{public BossState State=new BossState();public void StopEncounter(){}}
 public enum CombatVisualPriority{RealContact}
 public static class HitFeedback{public static void Spawn(Vector3 at,Vector3 d,float strength,bool critical,CombatVisualPriority priority){}}
 public static class FilledSkillVfx{public static void ConfirmFinale(PlayerController p,int c){}}
 public partial class EnemyController{
  public GameSession session;public EnemyKind Kind=EnemyKind.Goblin;public ThreatTier Tier;public void BeginDeath(){}
  LargeBoss largeBoss;GuardArmorVisual guardArmorVisual;Model model=new Model();bool preparing,aggro,deathReported;
  float hurtTime,nextImpactTime,nextFlinchAllowed,flinchUntil,chargeTime,stunTime;Vector3 knockVelocity;
  EnemyControlPolicy controlPolicy=new EnemyControlPolicy(EnemyControlTier.Normal);
  static bool FinitePoint(Vector3 v)=>true;void CancelAttack(bool hit=false){}
 }
 public partial class SequenceTerminalProbe {
  PlayerController owner;GameSession session;int epoch,step,steps=3,skill=9,rank=1;float age,nextEvent,interval=.05f,range=1;HeroClass heroClass=HeroClass.Vanguard;Vector3 target;
  public int Calls;public bool Retired;object gameObject=>this;void Destroy(object o){Retired=true;}
  public SequenceTerminalProbe(PlayerController p,GameSession s){owner=p;session=s;epoch=p.CombatEpoch;}
  public void Tick(){Update();}void Pull(Vector3 t,float r,float s){}void Healing(){throw new Exception("unexpected heal dispatch");}void Ranger(){throw new Exception("unexpected ranger dispatch");}void Arcanist(){throw new Exception("unexpected caster dispatch");}
  void Vanguard(){Calls++;if(session.Enemies.Count>0)session.Enemies[0].TakeDamage(100,Vector3.zero);session.RecordCombatAction("sequence current event tail");}
 }
 public partial class CompanionTerminalProbe {
  PlayerController Owner;GameSession session;int epoch;public bool Dismissed;public int Steps;
  public CompanionTerminalProbe(PlayerController p,GameSession s){Owner=p;session=s;epoch=p.CombatEpoch;}void Dismiss(){Dismissed=true;}public void Tick(){Update();}
 }
 public partial class PlayerController {
  public float Health=100,MaxHealth=100;
  PlayerStats stats=new PlayerStats();float invulnerability,chargedWardTime,guardTime,guardReduction,guardRadius=3,guardPower=1,healingProtectionTime,healingReduction,passiveTime,passiveReduction,hurtTimer,focusTime,blinkBufferTime,dodgeShockTime;
  int guardRank,guardCastId,openingFrostIdentity;bool jumping,rangerVault;EnemyController focusedEnemy,openingFrostTarget;Targeting targeting;Opening openingFrost=new Opening();CombatProcCooldown openingFrostProc=new CombatProcCooldown();
  void SettleMasteryCombo(EnemyController e){}void ApplySpellDodgeBoon(EnemyController e){}void Heal(float f){Health=Math.Min(MaxHealth,Health+f);}void TryDefensePassive(){}void CancelCombatPose(){}
  static PlayerController Practice(){var p=Make(true);p.skillRuntime.TryConsume(0,1);p.skillRuntime.RestoreEnergy(100-p.Energy-1);p.skillRuntime.EnergyChanged=p.session.PracticeRecord.Energy;p.session.PracticeRecord.Advance(2);return p;}
  static void PracticeSettlementTests(){
   var p=Practice();var target=p.Enemy(0,1);target.Health=1;p.counterTime=3;p.perfectDodgeCounterTime=3;float before=p.Energy;p.BasicAttack();var r=p.session.PracticeRecord;
   Check(r.Finished&&r.ObjectiveCompleted&&r.Survived&&r.Elapsed==2,"last counter seals at synchronous action boundary without elapsed drift");
   Check(r.EnergyRestored>0&&Math.Abs(r.EnergyRestored-(p.Energy-before))<.001f,"last basic records actual clipped energy refund");
   Check(r.Mechanisms.ContainsKey("普攻回能")&&r.Mechanisms.ContainsKey("剑卫反击"),"last lethal counter records complete outer action mechanisms");
   Check(p.session.InputBlocked,"terminal practice blocks subsequent player input");var after=r.FrozenCopy();r.Energy(99);r.Mechanism("late");r.Advance(8);Check(r.EnergyRestored==after.EnergyRestored&&!r.Mechanisms.ContainsKey("late")&&r.Elapsed==2,"finished record rejects unrelated future receipts");
   float hp=p.Health;p.invulnerability=0;p.TakeDamageFrom(500,"future");Check(p.Health==hp,"future hostile action rejected after settlement");var later=p.Enemy(0,1);float eh=later.Health;later.TakeDamage(100,Vector3.zero);Check(later.Health==eh,"future enemy damage rejected after settlement");
   foreach(bool lethal in new[]{false,true}){
    p=Practice();p.Health=lethal?1:100;p.guardTime=1;target=p.Enemy(0,1);target.Health=1;p.TakeDamageFrom(10,"guard incoming");r=p.session.PracticeRecord;
    Check(target.IsDead&&r.ObjectiveCompleted&&r.ActualDamage==1,"actual guard HitArea and enemy health mutation clears targets");
    Check(r.DamageTaken>0&&r.Finished&&r.Elapsed==2,"same-stack incoming damage settles after guard last kill");
    Check(r.Survived==!lethal&&p.IsDead==lethal,"same-action death takes priority over target-clear survival");
    if(lethal)Check(!r.ComparableConditions(after),"same-action death cannot compare despite cleared target fact");
    Check(lethal?r.EndReason.Contains("倒下"):r.EndReason=="目标全部击败","terminal reason matches actual survival");
   }
   p=Practice();target=p.Enemy(0,1);target.Health=1;var sequence=new SequenceTerminalProbe(p,p.session);Time.deltaTime=1;sequence.Tick();Check(sequence.Calls==1&&p.session.PracticeRecord.Mechanisms["sequence current event tail"]==1,"one sequence event clears targets without catchup-event receipts");sequence.Tick();Check(sequence.Calls==1&&sequence.Retired,"subsequent sequence frame retires after seal");
   foreach(bool dungeon in new[]{false,true}){var game=new GameSession{PracticeActive=false,ModeFinished=!dungeon,InDungeon=dungeon,DungeonCleared=dungeon};var ordinary=new PlayerController();game.Player=ordinary;var pet=new CompanionTerminalProbe(ordinary,game);pet.Tick();Check(!pet.Dismissed&&pet.Steps==0,"ordinary finished-mode companion is retained without another attack");}
   var tempPet=new CompanionTerminalProbe(p,p.session);tempPet.Tick();Check(!tempPet.Dismissed&&tempPet.Steps==0,"finished practice companion stops without lifetime mutation before host cleanup");
   p=Practice();target=p.Enemy(0,1);target.Health=1;p.counterTime=p.perfectDodgeCounterTime=3;p.session.ThrowOnMechanism=true;bool threw=false;try{p.BasicAttack();}catch(InvalidOperationException){threw=true;}Check(threw&&p.session.PracticeRecord.Finished&&p.session.PracticeRecord.Mechanisms.ContainsKey("剑卫反击"),"real BasicAttack finally seals lethal receipts after presentation exception");
   var open=new CampPracticeRecord(CampPracticeScenario.GuardAndWispPressure,10,"nested");open.Advance(3);CombatImpactBatch.BeginAction();try{CombatImpactBatch.Begin();try{open.Defeat("Guardian",false,true);Check(!open.Finished,"nested target batch cannot seal outer action early");CombatImpactBatch.Resolve(()=>open.Mechanism("impact callback"));}finally{CombatImpactBatch.End();}Check(open.Mechanisms.ContainsKey("impact callback")&&!open.Finished,"ordinary impact resolution precedes practice settlement");open.Energy(4);}finally{CombatImpactBatch.EndAction();}Check(open.Finished&&open.EnergyRestored==4&&open.Elapsed==3,"outer action seals nested receipts once");
   var exceptional=new CampPracticeRecord(CampPracticeScenario.GuardAndWispPressure,10,"exception");try{CombatImpactBatch.BeginAction();try{exceptional.Defeat("Guardian",false,true);throw new InvalidOperationException();}finally{CombatImpactBatch.EndAction();}}catch(InvalidOperationException){}Check(exceptional.Finished,"finally seals action after presentation exception");
   var fresh=new CampPracticeRecord(CampPracticeScenario.GuardAndWispPressure,10,"fresh");fresh.Defeat("Guardian",false,true);Check(fresh.Finished,"exception does not leak action scope into next run");
   Console.WriteLine("PASS "+n+" real practice combat settlement assertions; managed engine/presentation boundary, not Unity");
  }
 }
}
