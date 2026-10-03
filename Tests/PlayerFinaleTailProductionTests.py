#!/usr/bin/env python3
"""Real Ranger final event / generic Burst / area finisher / HitArea + full visual lease lifecycle.
Unity and enemy HP terminal callback are explicit managed boundaries; no rendered-frame claim.
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
s=s.replace('sealed class PlayerController','sealed partial class PlayerController').replace('public bool HasStarted,ModeFinished,InputBlocked;','public bool HasStarted,ModeFinished,InputBlocked;public List<EnemyController> Enemies=new List<EnemyController>();public int Rewards;')
s=s.replace('public static class CombatFx{','public static class CombatFx{public static void Ring(Vector3 p,float r,Color c,float d,float w){}')
sequence=(r/'Assets/Scripts/Combat/AdvancedSkillSequence.cs').read_text();ranger=sequence[sequence.index('        private void Ranger()'):];a=ranger.index('                case 9:')+len('                case 9:');b=ranger.index('                    break;',a);event=ranger[a:b]
hit=member((r/'Assets/Scripts/Combat/PlayerController.cs').read_text(),'internal void HitArea(')
area=member((r/'Assets/Scripts/Combat/CombatEffects.cs').read_text(),'if (!finished && finalDamage.Amount>0 && age>=delay+duration && ticksDrained)')
parts=(r/'Tests/CombatReadabilityVisualTests.cs').read_text()
# Keep complete production branch bodies unchanged; fixture supplies only omitted engine/scheduler context.
extra='namespace Emberfall{'+''.join(member(parts,k) for k in ['public enum ElementalistSpecialization','public enum HeroClass','public static class CombatBalance','public static class CombatProjectile'])+'''
public class EnemyController:MonoBehaviour {public bool IsDead,IsBoss,IgnoreDamage;public float HitFootprintBonus,Health=10000;public int Hits;public Action OnDamage;
public void TakeDamage(float damage,Vector3 direction,float knockback=0,float stun=0,bool critical=false,int practiceCastId=0){if(IsDead||IgnoreDamage||GameSession.Instance.ModeFinished)return;float old=Health;Health=Math.Max(0,Health-damage);Hits++;IsDead=Health<=0;if(Health<old)OnDamage?.Invoke();}}
public class ProjectileVolleyBudget<T>{public CombatDamage Apply(T e,CombatDamage damage,bool area)=>damage;}
public static class DestructibleProp{public static void StrikeArea(PlayerController owner,Vector3 at,float radius,CombatDamage damage,int castId){}}
// Healing is outside these non-healing finale routes; a non-null use is a fixture failure.
public class AdvancedSkillVfx{public static void Rune(params object[] values){}public void Stop(){throw new Exception("unexpected healing anchor in finale fixture");}}
public sealed partial class PlayerController{GameSession session=>GameSession.Instance;int id;int NewCastId()=>++id;void RegisterSkillHit(int castId){}void ApplySpellDodgeBoon(EnemyController enemy){}
'''+hit+'''}
public class FinaleProducer {
private CastFirstHitReceipt castReceipt; // Neutral lifetime boundary; dedicated delayed-hit suite tests live receipts.
private AdvancedSkillVfx healingAura; // Actual OnDisable dependency; always null for tested non-healing routes.
PlayerController owner;GameSession session;FilledSkillVfx.ArrowBatchHandle arrowBatch;int step=8,steps=9,rank=1,skill=9,castId=73;HeroClass heroClass=HeroClass.Ranger;Vector3 target,forward=Vector3.forward;float range=1;Color color=new Color(1,1,1);CombatDamage damage=new CombatDamage(10,false);
public FinaleProducer(PlayerController hero){owner=hero;session=GameSession.Instance;arrowBatch=FilledSkillVfx.BeginArrowBatch(owner,target,6f*range,color,priority:CombatVisualPriority.ActionBody,castId:castId);}
public void ArrowFinal(){'''+event+'''}
public void EarlyBeat(){step=0;ArrowFinal();step=8;}
public void CompleteFinal(){ArrowFinal();step++;OnDisable();}
public void Generic(bool steel){Burst(Vector3.zero,4,new CombatDamage(85,false),color,3,steel?SkillVisualRecipe.Steel:SkillVisualRecipe.Arcane);}
'''+member(sequence,'private void Burst(')+'''
private static Vector3 Circle(float angle,float radius)=>new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*radius;
public void Cancel(){OnDisable();}
'''+member(sequence,'private void OnDisable()')+'''}
public class AreaFinalFixture {PlayerController owner;Vector3 position;public Transform transform;float radius=4,age=10,delay=0,duration=2;bool finished;int castId=73;SkillVisualRecipe visualRecipe=SkillVisualRecipe.ArrowRain;Color color=new Color(1,1,1);CombatDamage finalDamage=new CombatDamage(85,false);
public AreaFinalFixture(PlayerController hero){owner=hero;transform=new GameObject("area").transform;}
public void Finish(){bool ticksDrained=true;'''+area+'''}}
}'''
test='''using System;using System.Linq;using Emberfall;using UnityEngine;
class Test {static int n;static void C(bool ok,string m){n++;if(!ok)throw new Exception(m);}static GameObject[] Fx()=>GameObject.All.Where(o=>!o.Destroyed&&o.activeInHierarchy&&o.GetComponent<FilledSkillVfx>()!=null).ToArray();
static void Tick(){foreach(var fx in Fx())fx.Call("Update");}
static void Main(){foreach(string kind in new[]{"arrow-sequence","arrow-field","steel-burst","arcane-burst"})foreach(string outcome in new[]{"live-boss","final-kill","boss-first-guards","empty","invulnerable","skip","epoch","owner","dead"}){
foreach(var o in GameObject.All.ToArray())UnityEngine.Object.Destroy(o);GameObject.All.Clear();EffectPreferences.ReducedEffects=true;Application.isMobilePlatform=true;Time.deltaTime=0;Time.unscaledDeltaTime=.1f;CombatProjectile.Count=0;
var hero=new GameObject("hero").AddComponent<PlayerController>();var game=new GameSession{Player=hero,HasStarted=true};GameSession.Instance=game;
for(int i=0;i<12;i++)FilledSkillVfx.Charge(null,hero,Vector3.zero,3,new Color(1,1,1),8);
if(outcome!="empty")for(int i=0;i<(outcome=="boss-first-guards"?3:1);i++){var e=new GameObject("enemy").AddComponent<EnemyController>();game.Enemies.Add(e);e.IsBoss=i==(outcome=="boss-first-guards"?2:0);e.Health=outcome=="live-boss"?10000:1;e.IgnoreDamage=outcome=="invulnerable";e.OnDamage=()=>{if(e.IsDead&&(e.IsBoss||game.Enemies.All(x=>x.IsDead))){game.ModeFinished=game.InputBlocked=true;game.Rewards++;}};}
if(kind=="arrow-field")new AreaFinalFixture(hero).Finish();else {var producer=new FinaleProducer(hero);if(kind=="arrow-sequence")producer.CompleteFinal();else producer.Generic(kind=="steel-burst");}
C(CombatVisualLease.Active<=12,"all finale producers respect cap12");if(outcome=="final-kill")C(game.ModeFinished&&game.Rewards==1,"actual producer final damage ends battle exactly once");
if(outcome=="boss-first-guards")C(game.ModeFinished&&game.Enemies.Take(2).All(e=>e.Hits==0),"boss-first finale leaves remaining guards undamaged");
int hits=game.Enemies.Sum(e=>e.Hits),rewards=game.Rewards,shots=CombatProjectile.Count;game.ModeFinished=game.InputBlocked=true;
if(outcome=="dead")hero.IsDead=true;if(outcome=="skip")FilledSkillVfx.SkipFinales(game);if(outcome=="epoch")hero.CombatEpoch++;if(outcome=="owner")game.Player=new GameObject("replacement").AddComponent<PlayerController>();
Tick();bool expected=outcome!="empty"&&outcome!="invulnerable"&&outcome!="skip"&&outcome!="epoch"&&outcome!="owner"&&outcome!="dead";
C((Fx().Length>0)==expected,"confirmed producer finale survives terminal boundary only with real contact and valid owner");
for(int i=0;i<9;i++)Tick();C(Fx().Length==0&&CombatVisualLease.Active==0,"all terminal tails retire within bounded unscaled time");C(game.Enemies.Sum(e=>e.Hits)==hits&&game.Rewards==rewards&&CombatProjectile.Count==shots,"terminal tail emits no damage rewards or projectiles");}
foreach(var o in GameObject.All.ToArray())UnityEngine.Object.Destroy(o);GameObject.All.Clear();
var earlyHero=new GameObject("hero").AddComponent<PlayerController>();var earlyGame=new GameSession{Player=earlyHero,HasStarted=true};GameSession.Instance=earlyGame;
var earlyEnemy=new GameObject("enemy").AddComponent<EnemyController>();earlyGame.Enemies.Add(earlyEnemy);var earlyProducer=new FinaleProducer(earlyHero);earlyProducer.EarlyBeat();C(earlyEnemy.Hits==1,"actual early arrow beat hits before final registration");earlyGame.Enemies.Clear();earlyProducer.CompleteFinal();earlyGame.ModeFinished=true;Tick();C(Fx().Length==0,"earlier arrow contact cannot confirm empty final beat");
Console.WriteLine("PASS: "+n+" actual finale producers/HitArea/visual lifecycle assertions at cap12; managed HP/terminal boundary");}}
'''
with tempfile.TemporaryDirectory(prefix='player-finale-tail-') as tmp:
 p=Path(tmp)
 (p/'BuildCatalogDamage.cs').write_text('namespace Emberfall{public static class BuildCatalog{'+member((r/'Assets/Scripts/Core/GameTypes.cs').read_text(),'public static float CinderTrailTickMultiplier(')+'}}')
 for f in ['Core/CombatImpactBatch','Core/CombatVisualBudget','Core/FilledVfxRecipes','Core/FilledVfxPlacement','Core/SkillVisualRecipe','Core/SkillDamageBudgets','Combat/CombatDamage','Combat/CombatVisualLease','Combat/FilledSkillVfx','Combat/AnchoredImpactMesh']:(p/(Path(f).name+'.cs')).write_text((r/('Assets/Scripts/'+f+'.cs')).read_text())
 (p/'Shell.cs').write_text(s);(p/'Producers.cs').write_text('using System;using UnityEngine;'+extra);(p/'Tests.cs').write_text(test)
 (p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>');project=p/'Test.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><NoWarn>0649;0414</NoWarn></PropertyGroup></Project>');include_cast_receipt_source(project)
 env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1')
 original=(p/'Producers.cs').read_text()
 for mode in ['current','old-unconfirmed','old-empty-confirmed']:
  code=original
  if mode=='old-unconfirmed':code=code.replace('if(enemy.Health<healthBeforeFinale)FilledSkillVfx.ConfirmFinale(this,castId);','')
  if mode=='old-empty-confirmed':code=code.replace('public void Finish(){bool ticksDrained=true;','public void Finish(){bool ticksDrained=true;').replace('owner.HitArea(transform.position,radius*1.1f,finalDamage,.7f,.65f,castId);','owner.HitArea(transform.position,radius*1.1f,finalDamage,.7f,.65f,castId);FilledSkillVfx.ConfirmFinale(owner,castId);')
  (p/'Producers.cs').write_text(code)
  q=subprocess.run([dotnet,'build',str(project),'--configfile',str(p/'NuGet.Config'),'-v:q'],env=env,capture_output=True,text=True);assert q.returncode==0,q.stdout+q.stderr
  q=subprocess.run([dotnet,str(p/'bin/Debug/net8.0/Test.dll')],env=env,capture_output=True,text=True)
  if mode=='current':print(q.stdout,end='');assert q.returncode==0,q.stderr
  else:assert q.returncode and 'confirmed producer finale survives terminal boundary only with real contact and valid owner' in q.stderr,q.stdout+q.stderr;print('PASS: compiled negative',mode)
