"""Actual full cast commitment, actual five-step sequence, Heal, potion and legal companion predicates.
Reuse mobile managed engine shell only. Non-healing spell paths and visual emission are explicit boundaries.
"""
from pathlib import Path
import os,sys,subprocess
root=Path(__file__).resolve().parents[1]
def member(path,signature):
 s=(root/'Assets/Scripts'/path).read_text();a=s.index(signature);b=s.index('{',a)+1;d=1
 while d:d+=(s[b]=='{')-(s[b]=='}');b+=1
 return s[a:b]
def adapt(f):
 start=f.index('public static class MobilePinnedTargetProductionTests');end=f.index('namespace Emberfall{internal static class ReturningCounterRules',start);f=f[:start]+f[end:]
 f=f.replace('public static void Destroy(Object o){}','public bool destroyed;public static void Destroy(Object o){o.destroyed=true;}')
 f=f.replace('public bool activeInHierarchy=true;','public GameObject(string name=""){}public T AddComponent<T>()where T:MonoBehaviour,new(){var c=new T();c.gameObject=this;return c;}public bool activeInHierarchy=true;')
 f=f.replace('public static float Clamp(float v,','public const float PI=(float)Math.PI;public static float Sin(float v)=>(float)Math.Sin(v);public static float Cos(float v)=>(float)Math.Cos(v);public static int CeilToInt(float v)=>(int)Math.Ceiling(v);public static float Clamp(float v,')
 f=f.replace('public static Vector3 forward=>','public static Vector3 right=>new Vector3(1,0,0);public static Vector3 Lerp(Vector3 a,Vector3 b,float t)=>a+(b-a)*t;public static Vector3 forward=>')
 f=f.replace('public class Mastery{','public class Mastery{public int Core,Tier;public void Configure(int core,int tier){Core=core;Tier=tier;}')
 f=f.replace('public class Model{','public class Model{public void ApplyFashion(object a,object b){}public void ApplyEquipment(object a,object b,object c){}public void SetBlenderPilotOwnerAlive(bool b){}')
 f=f.replace('public class Stats{public float Damage=10;}','public class Stats{public float Damage=10,MaxHealth=1000;}')
 f=f.replace('public static class AdvancedSkillSequence{public static void Spawn(params object[] a){}}','')
 start=f.index(' public static class SummonedCompanion{');end=f.index('\n',start);f=f[:start]+f[end:]
 f=f.replace('public class GameSession{','public partial class GameSession{public bool IsDead,CombatEnded;public float Healed;public void RecordActualHealing(float h){Healed+=h;}public void SpawnFloatingText(params object[] a){}')
 f=f.replace('public bool TrySpendHealingCharge()=>true;','')
 f=f.replace('public class FakeProgression{','public class FakeProgression{public bool IsApplyingBuildDraft=>false;public float RefreshedMaxHealth=1000;public PlayerController.Stats GetStats()=>new PlayerController.Stats{MaxHealth=RefreshedMaxHealth};public object EquippedFashion(FashionSlot slot)=>null;public object Equipped(ItemSlot slot)=>null;public int Potions=3;public string LastError="none";public bool UsePotion(){if(Potions==0)return false;Potions--;return true;}')
 # The shared mobile shell now declares Beam for F6; do not inject a duplicate.
 assert f.count('public static void Beam(params object[] a){}')==1
 f=f.replace('public static AdvancedSkillVfx Rune(params object[] a)=>new AdvancedSkillVfx();','public static AdvancedSkillVfx Rune(PlayerController owner,Vector3 at,float r,Color c,float duration,int rank,bool follows=false,int identity=0)=>new AdvancedSkillVfx();')
 # Real SummonerSpell dispatch uses this same Spawn call for slot six. All other spells remain boundary recorders.
 f=f.replace('player.RecordEmission(point,target);','if(skill==6)AdvancedSkillSequence.Spawn(player,game,skill,rank,point,player.transform.forward,new CombatDamage(),new Color(),castId);else player.RecordEmission(point,target);')
 f=f.replace('public class AdvancedSkillVfx:MonoBehaviour{','public class AdvancedSkillVfx:MonoBehaviour{public static AdvancedSkillVfx Healing(PlayerController h,float r,Color c,float life,int detail,System.Func<bool> active)=>new AdvancedSkillVfx();public void Stop(){}')
 return f

def add_production(p):
 sequence=(root/'Assets/Scripts/Combat/AdvancedSkillSequence.cs').read_text();fields=sequence[sequence.index('        private PlayerController owner;'):sequence.index('        public static void Spawn(')]
 methods=''.join(member('Combat/AdvancedSkillSequence.cs',s) for s in ['public static void Spawn(','private void Configure(','private void Update(','private void Healing('])
 (p/'HealingSequence.cs').write_text('using UnityEngine;namespace Emberfall{internal sealed partial class AdvancedSkillSequence:MonoBehaviour{'+fields+methods+'private void Pull(params object[] a){}private void Vanguard(){}private void Arcanist(){}private void Ranger(){}private EnemyController Nearest(Vector3 a,float b)=>null;private static Vector3 Circle(float a,float r)=>new Vector3(Mathf.Cos(a)*r,0,Mathf.Sin(a)*r);public static AdvancedSkillSequence Last;public void Tick(float dt){Time.deltaTime=dt;if(!gameObject.destroyed)Update();}}}')
 file=p/'HealingSequence.cs';s=file.read_text().replace('sequence.Configure();','sequence.Configure();Last=sequence;');file.write_text(s)
 methods=''.join(member('Combat/PlayerController.cs',s) for s in ['public void RefreshStats(','public void Heal(','internal void HealingProtection(','internal void RestoreSkillEnergy('])
 (p/'HealingOwner.cs').write_text('using UnityEngine;namespace Emberfall{public sealed partial class PlayerController{public float coreWardTime,healingProtectionTime,healingReduction;public void CastHeal(){CastSkillCore(6);}'+methods+'}}')
 methods=''.join(member('Combat/SummonedCompanion.cs',s) for s in ['public bool IsAlive','public static bool HasHealingTarget(','public static void HealAll('])
 (p/'HealingCompanions.cs').write_text('using UnityEngine;using System.Collections.Generic;namespace Emberfall{public class SummonedCompanion:MonoBehaviour{public static void RefreshBuild(PlayerController owner){}public static List<SummonedCompanion> active=new List<SummonedCompanion>();public PlayerController Owner;public GameSession session;public int epoch;public float Health,MaxHealth=100;public static EnemyController Team;public static int Commands;public static EnemyController ExplicitFocus(PlayerController p)=>Team;'+methods+'}}')
 methods=''.join(member(path,s) for path,s in [('Core/GameSession.Expedition.cs','public bool TrySpendHealingCharge('),('Core/GameSession.cs','public void DrinkPotion(')])
 # These fixtures are formal encounters. Any accidental practice-state access
 # must fail, rather than silently granting a permissive practice record.
 practice_boundary='public sealed class PracticeBoundary{public bool Started=>throw new System.InvalidOperationException("formal healing fixture entered practice state");public bool Finished=>throw new System.InvalidOperationException("formal healing fixture entered practice state");}public PracticeBoundary PracticeRecord=>throw new System.InvalidOperationException("formal healing fixture entered practice state");'
 (p/'HealingSession.cs').write_text('using UnityEngine;namespace Emberfall{public partial class GameSession{'+practice_boundary+methods+'}}')
 (p/'HealingTests.cs').write_text((root/'Tests/RestrictedHealingProductionTests.cs').read_text())
 (p/'SequenceBoundary.cs').write_text('using UnityEngine;namespace Emberfall{public enum CombatVisualPriority{ActionBody,RealContact}public class FilledSkillVfx{public static void HealingPulse(PlayerController h,Color c,Transform recipient=null){}public static bool IdentityContact(PlayerController owner,Vector3 at,Vector3 forward,float r,Color c,int identity,CombatVisualPriority priority=CombatVisualPriority.RealContact)=>false;public struct ArrowBatchHandle{}public static ArrowBatchHandle BeginArrowBatch(PlayerController p,Vector3 t,float r,Color c,CombatVisualPriority priority=CombatVisualPriority.ActionBody,int castId=0)=>default;}}')

# Reuse construction/compilation setup, not the old test body or mutations.
harness=(root/'Tests/MobilePinnedTargetProductionTests.py').read_text().split(' env=dict')[0]
harness=harness.replace("(r/'Tests/MobilePinnedTargetProductionTests.cs').read_text()","adapt((r/'Tests/MobilePinnedTargetProductionTests.cs').read_text())")
harness=harness.replace('MobilePinnedTargetProductionTests.Run()','RestrictedHealingProductionTests.Run()')
harness+='''\n add_production(p)
 env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1')
 subprocess.run([dotnet,'run','--project',str(project)],env=env,check=True)
 for name,before,after,oracle in [('HealingSequence.cs','owner.MaxHealth*selfTotal/5f','owner.MaxHealth*total/5f','five actual one-second self budgets'),('PlayerController.SkillAvailability.cs','return !(skill==6','return !(false&&skill==6','full self and full legal companions reject rank one'),('HealingSequence.cs','SummonedCompanion.HealAll(owner, total / 5f)','SummonedCompanion.HealAll(owner, selfTotal / 5f)','companion old budget unchanged'),('HealingSequence.cs','owner.IsDead || ',' ','lifecycle retires sequence death'),('HealingSequence.cs','owner.MaxHealth*selfTotal/5f','releaseMaxHealth*selfTotal/5f','noncapped pulse uses refreshed current max'),('HealingSequence.cs','game.ChallengeRun && game.InDungeon','game.ChallengeRun','wounded challenge outside dungeon retains ordinary thirty percent'),('HealingSequence.cs','{ Destroy(gameObject); return; }','{ if(rank==3)owner.RestoreSkillEnergy(8f); Destroy(gameObject); return; }','cancelled rank three never emits final energy refund')]:
  file=p/name;original=file.read_text();assert before in original;mutated=original.replace(before,after)
  if after=='releaseMaxHealth*selfTotal/5f':mutated=mutated.replace('private bool restrictedHealing;','private bool restrictedHealing;private float releaseMaxHealth;').replace('sequence.Configure();','sequence.releaseMaxHealth=hero.MaxHealth;sequence.Configure();')
  file.write_text(mutated);failed=subprocess.run([dotnet,'run','--project',str(project)],env=env,capture_output=True,text=True);file.write_text(original)
  assert failed.returncode and oracle in failed.stdout+failed.stderr,failed.stdout+failed.stderr
  print('PASS compiled negative control: '+oracle);print(failed.stdout+failed.stderr)
'''
exec(compile(harness,__file__,'exec'))
