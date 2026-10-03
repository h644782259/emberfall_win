// Managed scene doubles only. The companion script compiles the actual Chapter host,
// ChangeZone/SaveBeforeLeaving/OnEnemyKilled and real progression/filesystem transactions.
using System;using System.Collections;using System.Collections.Generic;using System.IO;using Emberfall;using UnityEngine;
namespace UnityEngine
{
 public class Object {public static void Destroy(Object o){if(o is GameObject g)g.SetActive(false);}}
 public class MonoBehaviour:Object {public GameObject gameObject;public Transform transform=>gameObject.transform;public MonoBehaviour(){gameObject=new GameObject();}}
 public class GameObject:Object {public string name;public bool activeInHierarchy=true;public readonly Transform transform;readonly Dictionary<Type,object> components=new Dictionary<Type,object>();public GameObject(string n=""){name=n;transform=new Transform{gameObject=this};}public void SetActive(bool a){activeInHierarchy=a;}public T AddComponent<T>()where T:new(){var v=new T();if(v is MonoBehaviour m)m.gameObject=this;components[typeof(T)]=v;return v;}public T GetComponent<T>()where T:new(){return components.TryGetValue(typeof(T),out var v)?(T)v:new T();}}
 public class Transform {public Vector3 position;public GameObject gameObject;public void SetParent(Transform p,bool world){}}
 public struct Vector3 {public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}public static Vector3 up=>new Vector3(0,1,0);public static Vector3 zero=>new Vector3();public float magnitude=>(float)Math.Sqrt(sqrMagnitude);public float sqrMagnitude=>x*x+y*y+z*z;public static float Distance(Vector3 a,Vector3 b)=>(float)Math.Sqrt((a-b).sqrMagnitude);public static Vector3 operator-(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);public static Vector3 operator+(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);public static Vector3 operator*(Vector3 a,float b)=>new Vector3(a.x*b,a.y*b,a.z*b);}
 public struct Vector2 {public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}}
 public static class Mathf {public static int FloorToInt(float value)=>(int)Math.Floor(value);public const float PI=(float)Math.PI;public static float Sin(float v)=>(float)Math.Sin(v);public static float Cos(float v)=>(float)Math.Cos(v);public static int Min(int a,int b)=>Math.Min(a,b);public static float Max(float a,float b)=>Math.Max(a,b);public static int Max(int a,int b)=>Math.Max(a,b);public static int Clamp(int v,int a,int b)=>Math.Max(a,Math.Min(b,v));public static float Clamp(float v,float a,float b)=>Math.Max(a,Math.Min(b,v));public static int RoundToInt(float v)=>(int)Math.Round(v);}
 public static class Random {public static int Range(int a,int b)=>a+1;public static float value=>.9f;}
 public static class Time {public static int frameCount;public static float deltaTime=.25f,time;}
 public class Camera:MonoBehaviour {public static Camera main=new Camera();public T GetComponent<T>()where T:new()=>new T();}
}
namespace Emberfall
{
 public sealed class PlayerController:MonoBehaviour {public int CombatEpoch=1,CooldownResets,Retirements;public float Health=100,MaxHealth=100;public void RetireCombatForWorldTransition(){CombatEpoch++;Retirements++;}public void Teleport(Vector3 p){transform.position=p;}public void RefreshStats(bool full){}public void ResetCooldownsForDungeonEntry(){CooldownResets++;}public void Heal(float n){Health=Math.Min(MaxHealth,Health+n);}}
 public sealed partial class EnemyController:MonoBehaviour {public bool IsBoss,IsDead;public EscapeRole PostRole;public bool isActiveAndEnabled=>gameObject.activeInHierarchy;public EnemyKind Kind;public float NavigationRadius=>IsBoss?1.3f:.65f;public int PostCalls;public void Initialize(GameSession s,EnemyKind k,int l,bool b){Kind=k;IsBoss=b;ApplySpawnStats(s,l,b);}public void ConfigureThreatAdmission(ThreatAdmissionPolicy policy,int member){}public void ConfigureEscapePost(EscapeRole role,Vector3 p){PostRole=role;PostCalls++;}public void ConfigureMobileSupport(EnemyController supplier,EnemyController first,EnemyController second){}public void BeginDeath(){IsDead=true;}}
 public static class WorldTraversal {public static bool SpawnBlocked,PathBlocked,Occluded;public static int Rays;public static void AddCircle(Vector3 p,float r){}public static void AddBox(Vector3 p,Vector2 s){}public static bool IsWalkable(Vector3 p,float r)=>!SpawnBlocked;public static Vector3 NearestWalkable(Vector3 p,float r)=>p;public static bool CanReach(Vector3 a,Vector3 b,float r)=>!PathBlocked;public static bool HasLineOfSight(Vector3 a,Vector3 b){Rays++;return !Occluded;}}
 public static class WorldBuilder {public static bool ThrowBuild;public static int Builds;public static GameObject Build(ZoneKind z,int layout=0,int tier=0,int hub=0,int chapterSeed=0){Builds++;if(ThrowBuild)throw new InvalidOperationException("fixture build failed");return new GameObject("World");}public static GameObject MakeLootBeacon(Vector3 p,Color c)=>new GameObject();public static GameObject MakeRoomObjective(Vector3 p,bool chapterSeal=false){var g=new GameObject();g.transform.position=p;return g;}public static void ApplyChapterLandmark(GameObject w,int mask){}}
 // Visual components are separately executed by TacticalLiveVisualTests; host keeps scene boundary doubles.
 public static class LargeBossShutdownVisual {public static GameSession Owner;public static bool Active;public static bool IsPresenting(GameSession s)=>Active&&ReferenceEquals(Owner,s);public static void Skip(GameSession s){if(ReferenceEquals(Owner,s))Active=false;}}
 public static class TacticalCaptureVisual {public static void AttachChapter(GameObject o,GameSession s,int index){}public static void Attach(GameObject objective,GameSession session){}}
 public static class TacticalEnemyVisual {public static void Attach(EnemyController enemy,GameSession session){}}
 public static class TacticalRoomGeometry {public static Vector3 Entrance=>new Vector3(0,0,-12);}
 public static class CombatFx {public static Vector3 Flat(Vector3 p)=>new Vector3(p.x,0,p.z);public static void Ring(Vector3 p,float r,Color c,float a,float b){}}
 public static class LargeExpeditionBoss {public static int Configures;public static void ConfigureChapter(EnemyController e,int t,int s,ChapterDifficulty d){Configures++;}}
 public static class ChapterHazards {public static void Configure(GameSession g,ChapterNode n,ChapterDifficulty d,int r,int s,ChapterRoomPlan p){}}
 public class AdventureCamera {public void Snap(){}}
 public enum SoundCue {Victory,Death}public static class GameAudio {public static void Play(SoundCue s){}}
 public static class MobileControls {public static bool Active;}
 public class FakeChoices {public void Reset(){}public void Cancel(){}}
 public sealed partial class GameSession:MonoBehaviour
 {
  // Practice is outside these chapter lifecycle tests.
  private bool retryingRoomChain=false;private int roomRetrySeed=0;public bool RoomBranchChoiceOpen=>false;public bool PracticeActive=>false;public CampPracticeRecord PracticeRecord=>throw new System.InvalidOperationException("ordinary chapter cannot access practice result");public void EndPractice(string reason=""){}
  public ProgressionService Progression;public PlayerController Player=new PlayerController();public bool HasStarted=true,InDungeon,IsDead,Paused,BackgroundPaused,IsInCamp=true;
  public bool InputBlocked=>Paused||BackgroundPaused||IsDead||ChapterFinished;public bool CombatEnded=>ChapterFinished||DungeonCleared;
  public int CurrentHub,DungeonTier=1,DungeonWave,DungeonLayout,DungeonEntryLevel=2,SelectedDungeonTier=1,HealingCharges;public int MaximumDungeonTier=>100;
  public bool DungeonCleared,ChallengeRun;public string LastRunSummary,Notice;public List<EnemyController> Enemies=new List<EnemyController>();
  ExpeditionModeState ModeRun;RoomChainState RoomChainRun;bool loadingSaveSnapshot,changingZone;object waveRoutine;GameObject world=new GameObject("Camp");readonly List<GameObject> transientObjects=new List<GameObject>();
  int runSeed,wavePopulation,recapGoldLost;float nextReinforcementAt,lastDamageAmount,lastInterruptAt,respawnTimer,runDamageTaken,runHealingReceived;string lastDamageSource;bool objectiveHealedThisWave,DungeonSelectionOpen,uiBlocking;Queue<object> reinforcementQueue=new Queue<object>();Dictionary<string,int> combatActions=new Dictionary<string,int>();public RunMechanismEvidence MechanismEvidence=new RunMechanismEvidence();RunChoices RunChoices=new RunChoices();FakeChoices pendingRoomChoice=new FakeChoices();
  public int LastEnemyExperience;public int OldWaveCalls,OldBuildCalls;public bool DungeonRewardPending=>false;public bool ModeRewardPending=>ChapterRewardPending;public bool WorldAlive=>world.activeInHierarchy;
  public void Notify(string s){Notice=s;}void SuspendInputs(){}void UpdateTimeScale(){}void AbandonSideEvent(){}void RetireWorldLootReceipts(int epoch){}string BuildRunSummary(bool success,string failure=null)=>"summary";
  void RecordRecapGoldLoss(int amount){recapGoldLost+=amount;}
  void ClearDungeonSettlement(){}void ResetArenaMode(bool dungeon){ModeRun=null;RoomChainRun=null;}bool TrySettleSideEventRewards()=>true;bool TrySettleDungeonReward()=>true;bool TrySettleArenaReward()=>TrySettleChapterReward();bool PreserveWorldLoot()=>true;
  void StopCoroutine(object routine){}void BeginRoomChainScene(){OldBuildCalls++;}void BeginArenaScene(){OldBuildCalls++;}void SpawnDungeonWave(){OldBuildCalls++;}void BuildSideEvent(){}void SpawnWildernessEnemy(){}
  void RecordArenaDefeat(EnemyController e){}void RecordRoomDefeat(EnemyController e){}void OnExpeditionEnemyKilled(EnemyController e){}public void LogSystem(string s){}void SpawnFloatingText(Vector3 p,string s,Color c){if(s.Contains(" XP  +"))LastEnemyExperience=int.Parse(s.Substring(1,s.IndexOf(" XP")-1));}void DeliverEnemyLoot(ItemData item,Vector3 p){}void FinalizeRoomChain(){}void FinalizeArenaResult(){}void TrySpawnReinforcements(){}object StartCoroutine(IEnumerator x){OldWaveCalls++;return x;}IEnumerator NextWave(){yield break;}
  public Vector3 FixtureObjective=>ChapterNextObjectivePoint;public void Tick()=>TickChapterRun();public void Camp(){ChangeZone(false);}public void FailForTest(){FailChapter("dead");}public ChapterRunReceipt Receipt=>chapterReceipt;
  public static string VerifyChapterResult(string folder)
  {
   int n=0;Action<bool,string> check=(ok,why)=>{n++;if(!ok)throw new Exception(why);};
   foreach(int first in new[]{0,1})
   {
    var hud=new GameSession{Progression=new ProgressionService(Path.Combine(folder,"seal-hud-"+first))};check(hud.Progression.CreateNewSlot(HeroClass.Vanguard)&&hud.ConfirmChapterEnter(),"seal HUD real host entry");
    foreach(var enemy in hud.Enemies)enemy.transform.position=new Vector3(99,0,99);
    hud.Player.transform.position=hud.chapterPlan.Objectives[first];for(int i=0;i<6;i++)hud.Tick();
    check(hud.ChapterSealView(first).Occupied&&!hud.ChapterSealView(1-first).Occupied&&hud.ChapterSealView(first).Seconds==1.5f,"HUD occupancy follows actual player on either ring");
    hud.Player.transform.position=hud.chapterPlan.Objectives[1-first];for(int i=0;i<6;i++)hud.Tick();
    hud.Enemies[0].transform.position=hud.chapterPlan.Objectives[1-first];hud.Tick();
    check(hud.ChapterSealView(1-first).Contested&&!hud.ChapterSealView(first).Contested&&hud.ChapterSealView(1-first).Seconds==1.5f,"HUD contest uses actual enemy region and pauses only occupied contested ring");
    hud.Enemies[0].transform.position=new Vector3(99,0,99);
    hud.Player.transform.position=hud.chapterPlan.Objectives[first];for(int i=0;i<6;i++)hud.Tick();
    check(hud.ChapterSealView(first).Complete&&hud.ChapterSealView(1-first).Seconds==1.5f,"HUD return preserves other half while chosen seal completes");
    hud.Player.transform.position=hud.chapterPlan.Objectives[1-first];for(int i=0;i<6;i++)hud.Tick();
    check(hud.ChapterSealView(0).Complete&&hud.ChapterSealView(1).Complete&&hud.ChapterRun.DoorUnlocked,"HUD both orders complete real host rings");
   }
   Func<string,GameSession> create=name=>{var game=new GameSession{Progression=new ProgressionService(Path.Combine(folder,name))};check(game.Progression.CreateNewSlot(HeroClass.Vanguard),"result fixture real save");game.FixtureUnlock();game.SelectedChapterNode=ChapterNode.StarPlatform;check(game.ConfirmChapterEnter(),"result fixture direct boss entry");return game;};
   var s=create("boss-first");Time.frameCount=10;LargeBossShutdownVisual.Owner=s;LargeBossShutdownVisual.Active=true;
   int xp=s.Progression.Profile.xp,gold=s.Progression.Profile.gold;var fake=new EnemyController();s.Enemies.Add(fake);s.OnEnemyKilled(fake);check(s.Progression.Profile.xp==xp&&s.Progression.Profile.gold==gold&&s.Enemies.Contains(fake),"unregistered chapter enemy cannot award or advance even when listed");s.Enemies.Remove(fake);
   var firstBoss=s.Enemies[0];s.OnEnemyKilled(firstBoss);check(s.LastEnemyExperience==33,"profile level one boss receives frozen 33 XP even at dungeon level two");xp=s.Progression.Profile.xp;gold=s.Progression.Profile.gold;s.OnEnemyKilled(firstBoss);check(s.Progression.Profile.xp==xp&&s.Progression.Profile.gold==gold,"repeated registered kill callback cannot pay twice");check(!s.ChapterFinished,"boss death alone does not settle live guards");
   Time.frameCount++;foreach(var e in new List<EnemyController>(s.Enemies))s.OnEnemyKilled(e);
   check(s.ChapterFinished&&s.ChapterRun.RewardClaimed&&s.ChapterResult.Saved,"reward is durably settled before presentation ends");
   check(!s.ChapterResultReady,"ACTIVE_BOSS_EXIT must keep opaque result hidden");
   LargeBossShutdownVisual.Active=false;check(s.ChapterResultReady,"finished boss exit allows result without a new guard-death delay");
   int paid=s.Progression.Profile.mechanicMaterials;s.TrySettleChapterReward();check(s.Progression.Profile.mechanicMaterials==paid,"presentation does not duplicate settled reward");
   s=create("boss-last");Time.frameCount=20;var boss=s.Enemies[0];s.OnEnemyKilled(s.Enemies[2]);s.OnEnemyKilled(s.Enemies[1]);s.OnEnemyKilled(boss);
   check(!s.ChapterResultReady,"boss death frame waits for visual lifecycle admission");Time.frameCount++;LargeBossShutdownVisual.Owner=s;LargeBossShutdownVisual.Active=true;check(!s.ChapterResultReady,"boss-last waits for actual visual rather than scaled combat time");s.ContinueChapterResult();check(s.ChapterResultReady&&!LargeBossShutdownVisual.Active,"explicit continue skips only presentation");
   s=create("failed-write");Directory.CreateDirectory(s.Progression.SaveFilePath+".tmp");foreach(var e in new List<EnemyController>(s.Enemies))s.OnEnemyKilled(e);
   check(s.ChapterRewardPending&&!s.ChapterResult.Saved&&s.ChapterResult.Materials==0&&s.ChapterResult.UnlockedNode==-1,"failed write publishes no saved reward or unlock snapshot");Directory.Delete(s.Progression.SaveFilePath+".tmp");check(s.TrySettleChapterReward()&&s.ChapterResult.Saved&&s.ChapterResult.SharedAfter==1,"same pending receipt retry captures committed shared tier");
   s=new GameSession{Progression=new ProgressionService(Path.Combine(folder,"failed-seal"))};check(s.Progression.CreateNewSlot(HeroClass.Vanguard)&&s.ConfirmChapterEnter(),"failure fixture enters forest");foreach(var e in s.Enemies)e.transform.position=new Vector3(99,0,99);var killed=s.Enemies[0];s.OnEnemyKilled(killed);check(s.LastEnemyExperience==7,"profile level one ordinary enemy receives frozen 7 XP");s.Player.transform.position=s.chapterPlan.Objectives[1];for(int i=0;i<5;i++)s.Tick();s.lastDamageSource="guard slam";s.lastDamageAmount=17;s.FailChapter("fixture generation path #4");
   check(s.ChapterResult.Failed&&s.ChapterResult.FirstSealSeconds==0&&s.ChapterResult.SecondSealSeconds==1.25f,"failure snapshot preserves independently chosen seal progress");check(s.ChapterResult.LastHit=="guard slam"&&s.ChapterResult.LastHitAmount==17&&s.ChapterResult.Failure.Contains("#4"),"failure snapshot retains actual last hit and specific path evidence");check(!s.ChapterResult.Saved&&s.ChapterResult.UnlockedNode==-1,"failure never invents saved unlock");
   s=new GameSession{Progression=new ProgressionService(Path.Combine(folder,"actual-death"))};check(s.Progression.CreateNewSlot(HeroClass.Vanguard)&&s.ConfirmChapterEnter(),"actual death enters forest");foreach(var enemy in s.Enemies)enemy.transform.position=new Vector3(99,0,99);s.OnEnemyKilled(s.Enemies[0]);s.Player.transform.position=s.chapterPlan.Objectives[1];for(int i=0;i<5;i++)s.Tick();s.lastDamageSource="guardian final slam";s.lastDamageAmount=23;int deathGold=s.Progression.Profile.gold;
   Directory.CreateDirectory(s.Progression.SaveFilePath+".tmp");s.OnPlayerDied();check(s.ChapterResult!=null,"DEATH_RESULT_CAPTURE must exist before XP budget cancellation");
   check(s.IsDead&&s.ChapterFinished&&s.ChapterResult.Failed&&s.ChapterResult.KillExperience==7&&s.Progression.ChapterKillExperienceEarned==0,"real death retains earned XP before canceling budget");
   check(s.ChapterResult.FirstSealSeconds==0&&s.ChapterResult.SecondSealSeconds==1.25f&&s.ChapterResult.LastHit=="guardian final slam"&&s.ChapterResult.LastHitAmount==23,"real death retains actual independent seal and last-hit evidence");
   string deathCopy=ChapterEntryPresentation.Result(s.ChapterResult);check(deathCopy.Contains("角色倒下")&&deathCopy.Contains("guardian final slam")&&deathCopy.Contains("本次击杀经验 +7")&&deathCopy.Contains("二 1.3s")&&!deathCopy.Contains("奖励已保存"),"actual death UI copy includes reason partial seal last hit and earned XP despite save failure");
   int remainingGold=s.Progression.Profile.gold;s.OnPlayerDied();check(s.Progression.Profile.gold==remainingGold&&remainingGold==deathGold,"duplicate death never removes earned gold");Directory.Delete(s.Progression.SaveFilePath+".tmp");
   var failedRun=s.ChapterRun;var failedReceipt=s.Receipt;int failedEpoch=s.Player.CombatEpoch,failedSeed=s.ChapterSeed,earnedXp=s.Progression.Profile.xp;
   s.Progression.Profile.gold++;int retained=s.Progression.Profile.gold;Directory.CreateDirectory(s.Progression.SaveFilePath+".tmp");
   check(!s.RetryFailedChapter()&&ReferenceEquals(s.ChapterRun,failedRun)&&s.IsDead&&s.Player.CombatEpoch==failedEpoch,"RETRY_SAVE atomic preflight preserves failed world and death state");
   Directory.Delete(s.Progression.SaveFilePath+".tmp");Time.frameCount++;
   check(s.RetryFailedChapter()&&!s.IsDead&&!s.ChapterFinished&&s.ChapterRoomIndex==0&&s.ChapterSeed==failedSeed&&s.Enemies.Count==6&&s.ChapterRun.Seals==0,"RETRY_RESET rebuilds node start with same seed and new enemies");
   check(!ReferenceEquals(s.Receipt,failedReceipt)&&s.Receipt.Id!=failedReceipt.Id&&!s.Progression.TryCompleteChapterNode(failedReceipt),"RETRY_RECEIPT rejects canceled old completion");
   check(s.Progression.Profile.gold==retained&&s.Progression.Profile.xp==earnedXp&&!s.RetryFailedChapter(),"RETRY_DUPLICATE preserves earned rewards and refuses active retry");
   s.OnEnemyKilled(killed);check(s.Progression.Profile.gold==retained,"RETRY_STALE old enemy callback cannot reward new run");
   s.OnPlayerDied();check(s.SaveBeforeLeaving(),"failure exit preflight saves earned progress");
   var reloaded=new ProgressionService(Path.Combine(folder,"actual-death"));check(reloaded.LoadSlot(s.Progression.CurrentSlotId)&&reloaded.Profile.gold==retained&&reloaded.Profile.xp==earnedXp&&reloaded.Profile.chapterRewardSequence==0&&!reloaded.TryCompleteChapterNode(s.Receipt),"RETRY_RESTART failed attempt grants no completion across save reload");
   s.Respawn();check(!s.IsDead&&!s.InDungeon&&!s.ChapterActive&&!s.CanRetryChapter,"RETRY_CAMP real respawn clears retry and terminal gates");
   var ordinary=new GameSession{Progression=new ProgressionService(Path.Combine(folder,"ordinary-death"))};check(ordinary.Progression.CreateNewSlot(HeroClass.Vanguard),"ordinary death profile");int ordinaryGold=ordinary.Progression.Profile.gold;ordinary.OnPlayerDied();check(ordinary.IsDead&&ordinary.ChapterResult==null&&ordinary.Progression.Profile.gold==ordinaryGold,"nonchapter death retains earned gold and no invented chapter result");
   s=create("retry-boss");s.OnPlayerDied();var canceledBoss=s.Receipt;long beforeSequence=s.Progression.Profile.chapterRewardSequence;Time.frameCount++;
   WorldTraversal.SpawnBlocked=true;check(!s.RetryFailedChapter()&&s.ChapterRun.Failed&&!s.CanRetryChapter&&!s.RunChoices.Active.GetEnumerator().MoveNext(),"RETRY_GENERATION failed build remains actionable next frame without tactic or repeat admission");WorldTraversal.SpawnBlocked=false;Time.frameCount++;
   check(s.RetryFailedChapter(),"retry generation failure recovers on next attempt");foreach(var enemy in new List<EnemyController>(s.Enemies))s.OnEnemyKilled(enemy);
   int paidMaterials=s.Progression.Profile.mechanicMaterials;check(s.ChapterRun.RewardClaimed&&s.Progression.Profile.chapterRewardSequence==beforeSequence+1&&!s.Progression.TryCompleteChapterNode(canceledBoss)&&s.TrySettleChapterReward()&&s.Progression.Profile.mechanicMaterials==paidMaterials,"RETRY_COMPLETION new run pays once and rejects failed run receipt");
   s=new GameSession{Progression=new ProgressionService(Path.Combine(folder,"revisit"))};check(s.Progression.CreateNewSlot(HeroClass.Vanguard),"revisit slot");s.FixtureUnlock();s.SelectedChapterTactic=2;s.SelectedChapterDifficulty=ChapterDifficulty.Hard;s.SelectedChapterLimitedHealing=true;
   check(s.ConfirmChapterEnter()&&s.RunChoices.Has(RunBlessing.IronSkin)&&!s.ForestMobileLineup,"C tactic applies after entry reset; first Hard lineup A");
   int sameSeed=s.ChapterSeed;s.FailForTest();s.SelectedChapterNode=ChapterNode.Redrock;s.SelectedChapterDifficulty=ChapterDifficulty.Heroic;s.SelectedChapterTier=55;s.SelectedChapterTactic=0;s.SelectedChapterLimitedHealing=false;Time.frameCount++;
   check(s.RetryFailedChapter()&&s.ActiveChapterNode==ChapterNode.ForestCourt&&s.ActiveChapterDifficulty==ChapterDifficulty.Hard&&s.DungeonTier==1&&s.ChapterSeed==sameSeed&&!s.ForestMobileLineup&&s.RunChoices.Has(RunBlessing.IronSkin)&&!s.RunChoices.Has(RunBlessing.DodgeShock)&&s.ChallengeRun&&s.HealingCharges==3,"RETRY_CONDITIONS immutable admitted options preserve seed roster tier healing and tactic");
   s.SelectedChapterNode=ChapterNode.ForestCourt;s.SelectedChapterDifficulty=ChapterDifficulty.Hard;s.SelectedChapterTier=1;s.SelectedChapterTactic=2;
   foreach(var enemy in s.Enemies)enemy.transform.position=new Vector3(99,0,99);
   for(int seal=0;seal<2;seal++){s.Player.transform.position=s.chapterPlan.Objectives[seal];for(int i=0;i<12;i++)s.Tick();}
   s.Player.transform.position=s.chapterPlan.Exit;check(s.EnterNextChapterRoom()&&s.RunChoices.Has(RunBlessing.IronSkin),"C tactic survives actual room retirement");s.FailForTest();check(!s.RunChoices.Has(RunBlessing.IronSkin),"C failure clears tactic");s.Camp();
   check(s.SelectedForestLineupB&&s.ConfirmChapterEnter()&&s.ForestMobileLineup&&s.Enemies.Count==6,"D repeated Hard attempt selects B independently of mirror");
   check(s.Enemies[1].PostRole==EscapeRole.GateGuard&&s.Enemies[4].PostRole==EscapeRole.GateGuard,"D B has two altar guards");
   check(s.Enemies[2].Kind==EnemyKind.Goblin&&s.Enemies[5].Kind==EnemyKind.Goblin&&s.Enemies[3].Kind==EnemyKind.Slime,"D B preserves exact six enemy kinds");
   s.FailForTest();s.Camp();check(!s.SelectedForestLineupB&&s.ConfirmChapterEnter()&&!s.ForestMobileLineup,"D third Hard attempt returns A");s.Camp();check(!s.RunChoices.Has(RunBlessing.IronSkin),"C camp clears tactic");
   foreach(HeroClass hero in Enum.GetValues(typeof(HeroClass)))foreach(bool mobile in new[]{false,true})for(int skill=0;skill<10;skill++)
   {
    var p=new GameProfile{heroClass=hero,chapterCompletedMask=7,equippedSkills=new[]{-1,-1,-1,-1,-1,-1,-1,-1,-1,-1}};p.skillRanks[skill]=1;
    bool eligible=mobile&&!GameBalance.IsPassive(skill)&&EnemyControlPolicy.IsInterruptSkill(hero,skill);
    check(RunChoices.ChapterTactic(p,mobile,1)==(eligible?RunBlessing.InterruptFlow:RunBlessing.SwiftHands),"C exact reachable interrupt eligibility");
    var choices=new RunChoices();check(choices.ChooseChapterTactic(p,ChapterNode.ForestCourt,mobile,0)&&!choices.ChooseChapterTactic(p,ChapterNode.ForestCourt,mobile,2),"C at most one tactic");
    p.chapterCompletedMask=3;choices.Reset();check(!choices.ChooseChapterTactic(p,ChapterNode.ForestCourt,mobile,0),"C first story has no tactic");
   }
   s=new GameSession{Progression=new ProgressionService(Path.Combine(folder,"mechanism-result"))};check(s.Progression.CreateNewSlot(HeroClass.Arcanist),"mechanism result save");s.FixtureUnlock();s.SelectedChapterNode=ChapterNode.StarPlatform;check(s.ConfirmChapterEnter(),"mechanism result enters");
   for(int i=0;i<6;i++){var token=s.MechanismEvidence.Register(s.Player,s.Player.CombatEpoch,0);if(i<4)token.Record(s.Player,s.Player.CombatEpoch,1);}
   foreach(var enemy in new List<EnemyController>(s.Enemies))s.OnEnemyKilled(enemy);
   check(s.ChapterResult.EmberCreated==6&&s.ChapterResult.EmberEffective==4&&ChapterEntryPresentation.Result(s.ChapterResult).Contains("生成 6 / 生效 4"),"E chapter structured snapshot and result use distinct effective instances");
   for(int alive=1;alive<=2;alive++)
   {
    s=new GameSession{Progression=new ProgressionService(Path.Combine(folder,"forest-mastery-"+alive))};check(s.Progression.CreateNewSlot(HeroClass.Vanguard),"forest mastery slot");s.FixtureUnlock();s.SelectedChapterDifficulty=ChapterDifficulty.Hard;check(s.ConfirmChapterEnter(),"forest mastery enters Hard");
    while(s.Enemies.Count>alive)s.OnEnemyKilled(s.Enemies[s.Enemies.Count-1]);foreach(var enemy in s.Enemies)enemy.transform.position=new Vector3(99,0,99);
    for(int seal=0;seal<2;seal++){s.Player.transform.position=s.chapterPlan.Objectives[seal];for(int tick=0;tick<12;tick++)s.Tick();}
    check(s.chapterReceipt.MasteryEvidence==(alive==2?1:0),"F forest real seal completion requires at least two living first-room enemies");
    s.FailForTest();check(s.Progression.Profile.chapterMasteryMask==0,"F forest failure publishes no staged mastery");
   }
   for(int otherKills=0;otherKills<=1;otherKills++)
   {
    s=new GameSession{Progression=new ProgressionService(Path.Combine(folder,"red-mastery-"+otherKills))};check(s.Progression.CreateNewSlot(HeroClass.Vanguard),"red mastery slot");s.FixtureUnlock();s.SelectedChapterNode=ChapterNode.Redrock;s.SelectedChapterDifficulty=ChapterDifficulty.Heroic;check(s.ConfirmChapterEnter(),"red mastery enters Heroic");
    if(otherKills==1)s.OnEnemyKilled(s.Enemies[5]);s.OnEnemyKilled(s.Enemies[0]);check(s.chapterReceipt.MasteryEvidence==(otherKills==0?2:0)&&s.ChapterRun.DoorUnlocked,"F designated hunt death with other five alive stages mastery without blocking bypass");
   }
   s=new GameSession{Progression=new ProgressionService(Path.Combine(folder,"star-mastery-owner"))};check(s.Progression.CreateNewSlot(HeroClass.Vanguard),"star mastery slot");s.FixtureUnlock();s.SelectedChapterNode=ChapterNode.StarPlatform;s.SelectedChapterDifficulty=ChapterDifficulty.Hard;check(s.ConfirmChapterEnter(),"star mastery enters");
   var realBoss=s.Enemies[0];s.RecordChapterInterrupt(s.Enemies[1]);s.RecordChapterAnchorExposure(new EnemyController{IsBoss=true});check(s.chapterReceipt.MasteryEvidence==0,"F guards and unregistered boss cannot earn star mastery");
   s.RecordChapterInterrupt(realBoss);s.RecordChapterAnchorExposure(realBoss);check(s.chapterReceipt.MasteryEvidence==12,"F current registered boss admits actual counter evidence");s.Player.RetireCombatForWorldTransition();s.chapterReceipt.MasteryEvidence=0;s.RecordChapterAnchorExposure(realBoss);check(s.chapterReceipt.MasteryEvidence==0,"F stale epoch boss cannot earn mastery");
   for(int entryFailure=0;entryFailure<2;entryFailure++)for(int tactic=0;tactic<3;tactic++)
   {
    s=new GameSession{Progression=new ProgressionService(Path.Combine(folder,"failed-tactic-entry-"+entryFailure+"-"+tactic))};
    check(s.Progression.CreateNewSlot(HeroClass.Vanguard),"failed tactic entry slot");s.FixtureUnlock();s.SelectedChapterDifficulty=ChapterDifficulty.Hard;s.SelectedChapterTactic=tactic;
    WorldTraversal.SpawnBlocked=entryFailure==0;WorldTraversal.PathBlocked=entryFailure==1;
    try
    {
     check(s.ConfirmChapterEnter()&&s.InDungeon&&s.ChapterFinished&&s.ChapterRun.Failed&&s.ChapterResult.Failed,"failed chapter entry remains available for result presentation");
     bool activeTactic=false;foreach(var blessing in s.RunChoices.Active)activeTactic=true;
     check(!activeTactic&&!s.RunChoices.AwaitingChoice&&s.RunChoices.CompletedWave==0,"C failed spawn or unreachable entry never reapplies selected tactic after failure reset");
    }
    finally {WorldTraversal.SpawnBlocked=WorldTraversal.PathBlocked=false;}
    s.Camp();check(!s.ChapterActive&&!s.InDungeon,"failed tactic entry can return cleanly to camp");
   }
   var firstRoute=new GameSession{Progression=new ProgressionService(Path.Combine(folder,"red-first-route"))};
   check(firstRoute.Progression.CreateNewSlot(HeroClass.Vanguard),"first Redrock save");firstRoute.Progression.Profile.chapterCompletedMask=1;firstRoute.Progression.Save();firstRoute.SelectedChapterNode=ChapterNode.Redrock;
   foreach(var difficulty in new[]{ChapterDifficulty.Hard,ChapterDifficulty.Heroic})
   {firstRoute.SelectedChapterDifficulty=difficulty;check(!firstRoute.ConfirmChapterEnter()&&firstRoute.redrockRouteOwner==null,"first-clear Hard Heroic rejected without consuming route history");}
   firstRoute.SelectedChapterDifficulty=ChapterDifficulty.Normal;
   check(firstRoute.ConfirmChapterEnter()&&!ChapterRoomGeometry.RedrockSplitRoute(firstRoute.ChapterSeed),"first clear Normal retains legacy geometry seed");firstRoute.FailForTest();firstRoute.Camp();
   var route=new GameSession{Progression=new ProgressionService(Path.Combine(folder,"red-route"))};
   check(route.Progression.CreateNewSlot(HeroClass.Vanguard),"route save created");route.FixtureUnlock();route.SelectedChapterNode=ChapterNode.Redrock;
   route.SelectedChapterDifficulty=ChapterDifficulty.Normal;
   check(route.ConfirmChapterEnter()&&!ChapterRoomGeometry.RedrockSplitRoute(route.ChapterSeed),"Normal replay keeps original route");route.Camp();
   for(int admission=0;admission<8;admission++)
   {
    route.SelectedChapterDifficulty=admission%2==0?ChapterDifficulty.Hard:ChapterDifficulty.Heroic;
    // Failed filesystem save, followed by failed spawn, must not advance the admitted sequence.
    Directory.CreateDirectory(route.Progression.SaveFilePath+".tmp");
    check(!route.ConfirmChapterEnter(),"route save rejection cannot enter");Directory.Delete(route.Progression.SaveFilePath+".tmp");
    string ownerBefore=route.redrockRouteOwner;bool splitBefore=route.previousRedrockSplit;
    WorldTraversal.PathBlocked=true;
    try{check(route.ConfirmChapterEnter()&&route.ChapterRun.Failed,"route unreachable entry captured");}
    finally{WorldTraversal.PathBlocked=false;}
    check(route.redrockRouteOwner==ownerBefore&&route.previousRedrockSplit==splitBefore,"REPLAY_ADMISSION alternates only successfully admitted eligible runs");
    route.Camp();
    WorldBuilder.ThrowBuild=true;
    try{route.ConfirmChapterEnter();check(route.ChapterRun.Failed,"route build exception captured");}
    finally{WorldBuilder.ThrowBuild=false;}
    route.Camp();
    WorldTraversal.SpawnBlocked=true;
    try{check(route.ConfirmChapterEnter()&&route.ChapterRun.Failed,"route failed spawn captured");}
    finally{WorldTraversal.SpawnBlocked=false;}
    check(route.redrockRouteOwner==ownerBefore&&route.previousRedrockSplit==splitBefore,"REPLAY_ADMISSION alternates only successfully admitted eligible runs");
    route.Camp();
    check(route.ConfirmChapterEnter()&&ChapterRoomGeometry.RedrockSplitRoute(route.ChapterSeed)==(admission%2==0),"REPLAY_ADMISSION alternates only successfully admitted eligible runs");
    check(route.chapterPlan.Obstacles.Length==3&&route.Enemies.Count==6,"replay first room unchanged and capped");
    route.OnEnemyKilled(route.Enemies[0]);route.Player.transform.position=route.chapterPlan.Exit;
    check(route.EnterNextChapterRoom()&&route.chapterPlan.Obstacles.Length==(admission%2==0?4:3)&&route.Enemies.Count==6,"room two uses seed route within six enemy cap");
    check(route.ChapterRun.Objective==RoomObjective.Escape&&!route.ChapterRun.DoorUnlocked,"replay retains escape objective");
    int wisps=0,guards=0,goblins=0,slimes=0;foreach(var enemy in route.Enemies){if(enemy.Kind==EnemyKind.Wisp)wisps++;if(enemy.Kind==EnemyKind.Guardian)guards++;if(enemy.Kind==EnemyKind.Goblin)goblins++;if(enemy.Kind==EnemyKind.Slime)slimes++;}
    check(wisps==2&&guards==2&&goblins==1&&slimes==1,"replay preserves crossfire roster types");route.FailForTest();route.Camp();
   }
   route.SelectedChapterDifficulty=ChapterDifficulty.Normal;
   check(route.ConfirmChapterEnter()&&!ChapterRoomGeometry.RedrockSplitRoute(route.ChapterSeed),"Normal between eligible runs keeps legacy route");route.FailForTest();route.Camp();route.SelectedChapterDifficulty=ChapterDifficulty.Hard;
   // Reloading the same owner keeps host history; a new host intentionally resets it.
   check(route.Progression.LoadSlot(route.Progression.CurrentSlotId),"route same save reload");
   check(route.ConfirmChapterEnter()&&ChapterRoomGeometry.RedrockSplitRoute(route.ChapterSeed),"same owner session keeps alternating history");route.FailForTest();route.Camp();
   check(route.Progression.LoadSlot(route.Progression.CurrentSlotId),"route reload after split");
   check(route.ConfirmChapterEnter()&&!ChapterRoomGeometry.RedrockSplitRoute(route.ChapterSeed),"same save reload does not reset split history");route.FailForTest();route.Camp();
   var restarted=new GameSession{Progression=route.Progression,SelectedChapterNode=ChapterNode.Redrock,SelectedChapterDifficulty=ChapterDifficulty.Hard};
   check(restarted.ConfirmChapterEnter()&&ChapterRoomGeometry.RedrockSplitRoute(restarted.ChapterSeed),"new host restarts bounded route history");restarted.FailForTest();restarted.Camp();
   var oldOwner=route.Progression;
   route.Progression=new ProgressionService(Path.Combine(folder,"red-route-other-owner"));check(route.Progression.CreateNewSlot(HeroClass.Vanguard),"second route owner save");route.FixtureUnlock();
   check(route.ConfirmChapterEnter()&&ChapterRoomGeometry.RedrockSplitRoute(route.ChapterSeed),"different owner starts split instead of inheriting route");route.FailForTest();route.Camp();
   var otherOwner=route.Progression;
   route.Progression=oldOwner;
   check(route.ConfirmChapterEnter()&&ChapterRoomGeometry.RedrockSplitRoute(route.ChapterSeed),"bounded last-owner policy resets after another owner admitted");route.FailForTest();route.Camp();
   foreach(var node in new[]{ChapterNode.Redrock,ChapterNode.ForestCourt})
   {
    route.Progression=otherOwner;route.SelectedChapterNode=node;route.SelectedChapterDifficulty=node==ChapterNode.Redrock?ChapterDifficulty.Normal:ChapterDifficulty.Hard;
    check(route.ConfirmChapterEnter(),"other owner noneligible visit admitted");route.FailForTest();route.Camp();
    check(route.redrockRouteOwner==oldOwner.SaveFilePath,"Normal or other chapter owner visit does not replace eligible Redrock history");
    route.Progression=oldOwner;route.SelectedChapterNode=ChapterNode.Redrock;route.SelectedChapterDifficulty=ChapterDifficulty.Hard;
    check(route.ConfirmChapterEnter()&&ChapterRoomGeometry.RedrockSplitRoute(route.ChapterSeed)==(node==ChapterNode.ForestCourt),"eligible owner resumes alternation after ignored visit");route.FailForTest();route.Camp();
   }
   return "PASS: "+n+" actual chapter result persistence, independent seal evidence and boss presentation-gate checks";
  }
  public void FixtureUnlock(){Progression.Profile.chapterCompletedMask=7;Progression.Profile.chapterHighestDifficulties=new[]{3,3,3};Progression.Save();}
 }
}
public static class ChapterHostProductionTests
{
 static int checks;static void Check(bool v,string text){checks++;if(!v)throw new Exception(text);}
 static GameSession Fresh(string path){var s=new GameSession{Progression=new ProgressionService(path)};Check(s.Progression.CreateNewSlot(HeroClass.Vanguard),"fresh fixture save");return s;}
 static void Capture(GameSession s){s.Player.transform.position=s.FixtureObjective;for(int i=0;i<16;i++)s.Tick();}
 static void Exit(GameSession s){s.Player.transform.position=new Vector3(0,0,14);Check(s.EnterNextChapterRoom(),"real chapter exit succeeds");}
 static void Formation(GameSession s)
 {
  Check(s.Enemies.Count==6&&s.Enemies.FindAll(e=>e.Kind==EnemyKind.Wisp).Count==2&&s.Enemies.FindAll(e=>e.Kind==EnemyKind.Guardian).Count==2,"crossfire roster uses two wisps and two guardians within six-enemy cap");
  Check(s.Enemies[0].Kind==EnemyKind.Wisp&&s.Enemies[0].PostRole==EscapeRole.GateSupplier&&s.Enemies[1].PostRole==EscapeRole.SideFlanker,"hunt target remains index zero while second caster holds its lane");
  Check(s.Enemies[0].transform.position.x*s.Enemies[1].transform.position.x<0,"production crossfire spawn candidates occupy opposite fork lanes");
  for(int i=2;i<4;i++)Check(s.Enemies[i].PostRole==EscapeRole.GateGuard&&Vector3.Distance(s.Enemies[i].transform.position,new Vector3(0,0,11))<3,"both real guardian posts protect the exit capture ring");
  Check(s.Enemies[4].PostRole==EscapeRole.Pursuer&&s.Enemies[5].PostRole==EscapeRole.SideFlanker,"remaining melee retain pursuit and side-lane decisions");
 }
 public static string Run(string folder)
 {
  checks=0;var s=Fresh(Path.Combine(folder,"entry"));var oldWorldBuilds=WorldBuilder.Builds;int oldEpoch=s.Player.CombatEpoch;
  s.SelectedChapterDifficulty=ChapterDifficulty.Hard;Check(!s.ConfirmChapterEnter()&&!s.InDungeon&&s.WorldAlive&&s.Player.CombatEpoch==oldEpoch,"locked difficulty cannot destroy camp");s.SelectedChapterDifficulty=ChapterDifficulty.Normal;
  s.Progression.Profile.gold++;Directory.CreateDirectory(s.Progression.SaveFilePath+".tmp");Check(!s.ConfirmChapterEnter()&&!s.ChapterActive&&WorldBuilder.Builds==oldWorldBuilds&&s.Player.CombatEpoch==oldEpoch,"entry save failure leaves old world and epoch intact");Directory.Delete(s.Progression.SaveFilePath+".tmp");
  Check(s.ConfirmChapterEnter()&&s.Enemies.Count==6&&s.OldBuildCalls==0&&s.Player.CooldownResets==1,"chapter enters six-enemy host without old mode spawning");var first=s.ChapterRun;var stale=s.Enemies[0];Check(s.ChapterSupportMultiplier(s.Enemies[1])==1,"normal forest never grants support");Check(Math.Abs(stale.MaxHealth-CombatBalance.EnemyHealth(s.DungeonEntryLevel,1,false,EnemyKind.Wisp))<.001f,"normal chapter stats retain tier baseline");
  var originalPositions=s.Enemies.ConvertAll(e=>e.transform.position);foreach(var e in s.Enemies)e.transform.position=new Vector3(99,0,99);WorldTraversal.Rays=0;s.Tick();Check(WorldTraversal.Rays==0,"far capture contestants skip LOS query");for(int i=0;i<s.Enemies.Count;i++)s.Enemies[i].transform.position=originalPositions[i];
  foreach(var enemy in s.Enemies)enemy.transform.position=new Vector3(0,0,-8);
  Capture(s);Check(s.ChapterRun.Seals==1&&!s.ChapterRun.DoorUnlocked,"first seal alone cannot exit");Capture(s);Check(s.ChapterRun.DoorUnlocked,"two actual host captures open first exit");
  s.Player.transform.position=new Vector3(0,0,14);oldEpoch=s.Player.CombatEpoch;s.Progression.Profile.gold++;Directory.CreateDirectory(s.Progression.SaveFilePath+".tmp");Check(!s.EnterNextChapterRoom()&&s.ChapterRoomIndex==0&&s.Player.CombatEpoch==oldEpoch&&stale.gameObject.activeInHierarchy,"room save failure preserves living old room");Directory.Delete(s.Progression.SaveFilePath+".tmp");
  Exit(s);Check(s.ChapterRoomIndex==1&&s.Enemies.Count==6&&s.Player.CombatEpoch==oldEpoch+1&&s.Player.CooldownResets==1&&!stale.gameObject.activeInHierarchy,"second room retires epoch/enemies while retaining cooldowns");int gold=s.Progression.Profile.gold;s.OnEnemyKilled(stale);Check(s.Progression.Profile.gold==gold&&!s.ChapterRun.DoorUnlocked,"stale previous-room kill cannot award or advance new room");
  foreach(var enemy in new List<EnemyController>(s.Enemies))s.OnEnemyKilled(enemy);Check(s.OldWaveCalls==0&&!s.ChapterFinished,"chapter clear never schedules legacy NextWave or skips capture");Capture(s);Check(s.ChapterRun.DoorUnlocked&&!s.ChapterFinished,"second capture still requires actual exit");
  s.Player.transform.position=new Vector3(0,0,14);s.Progression.Profile.gold++;s.Progression.Save();
  Exit(s);Check(s.ChapterFinished&&!s.ChapterRewardPending&&s.Progression.Profile.chapterCompletedMask==1&&s.Progression.Profile.highestAdventureTier==0,"forest durable node completion changes story only");int paid=s.Progression.Profile.mechanicMaterials;Check(s.TrySettleChapterReward()&&s.Progression.Profile.mechanicMaterials==paid&&!s.EnterNextChapterRoom(),"repeat finish/exit cannot double pay");
  Check(!s.Progression.Profile.pendingFirstClearReward&&!s.ChapterResult.FirstCoreAvailable,"FOREST_CORE_BOUNDARY forest host completion must not enable shared core");
  s.Camp();Check(!s.ChapterActive&&!s.InDungeon,"return to camp clears chapter state");
  s.FixtureUnlock();s.SelectedChapterNode=ChapterNode.Redrock;s.SelectedChapterDifficulty=ChapterDifficulty.Hard;Check(s.ConfirmChapterEnter(),"hard redrock entry");Check(s.Enemies.TrueForAll(e=>e.PostCalls==1),"hard redrock configures existing guard roles once");Formation(s);Check(Math.Abs(s.Enemies[0].MaxHealth-CombatBalance.EnemyHealth(s.DungeonEntryLevel,1,false,EnemyKind.Wisp)*1.2f)<.001f&&Math.Abs(s.Enemies[0].damage-CombatBalance.EnemyDamage(s.DungeonEntryLevel,1,false)*1.15f)<.001f,"chapter difficulty multiplies already tier-scaled stats exactly once");s.OnEnemyKilled(s.Enemies[0]);Check(s.ChapterRun.DoorUnlocked&&s.Enemies.Count==5,"hunt prioritizes designated target rather than all enemies");Exit(s);Formation(s);s.FailForTest();var abandoned=s.Receipt;s.Camp();Check(!s.Progression.TryCompleteChapterNode(abandoned),"failed and abandoned attempt receipt is invalid");
  s.SelectedChapterNode=ChapterNode.StarPlatform;s.SelectedChapterDifficulty=ChapterDifficulty.Normal;Check(s.ConfirmChapterEnter()&&s.Enemies.Count==3&&s.ChapterRoomIndex==0&&!s.ChapterRun.DoorUnlocked&&s.ChapterObjectiveStatus.Contains("1 / 1"),"star directly enters its single boss room");Check(s.Enemies.Count==3&&LargeExpeditionBoss.Configures==1,"star creates one anchor boss plus two guards");
  s.OnEnemyKilled(s.Enemies[0]);Check(!s.ChapterFinished,"boss kill alone does not ignore guards");s.OnEnemyKilled(s.Enemies[0]);
  // Kill callback's normal enemy save is allowed to fail; chapter receipt must remain retryable.
  Directory.CreateDirectory(s.Progression.SaveFilePath+".tmp");s.OnEnemyKilled(s.Enemies[0]);Check(s.ChapterFinished&&s.ChapterRewardPending&&s.Progression.Profile.highestAdventureTier==0,"real completion save failure publishes no shared tier and remains pending");Directory.Delete(s.Progression.SaveFilePath+".tmp");Check(s.TrySettleChapterReward()&&!s.ChapterRewardPending&&s.Progression.Profile.highestAdventureTier==1,"same terminal receipt can retry durable star tier completion");Check(s.Progression.Profile.pendingFirstClearReward&&!s.Progression.Profile.pendingFashionChest,"first successful star uses shared one-time core entitlement without relic chest");
  s.Camp();s.SelectedChapterNode=ChapterNode.ForestCourt;s.SelectedChapterDifficulty=ChapterDifficulty.Hard;Check(s.ConfirmChapterEnter(),"hard forest entry");var supplier=s.Enemies[0];var friend=s.Enemies[1];friend.transform.position=supplier.transform.position+new Vector3(1,0,0);Check(s.ChapterSupportMultiplier(friend)==.7f&&s.ChapterSupportMultiplier(supplier)==1,"hard forest gives actual support to others only");WorldTraversal.Occluded=true;Check(s.ChapterSupportMultiplier(friend)==1,"wall severs chapter support");WorldTraversal.Occluded=false;friend.transform.position=new Vector3(99,0,99);WorldTraversal.Rays=0;Check(s.ChapterSupportMultiplier(friend)==1&&WorldTraversal.Rays==0,"distant support skips LOS query");s.Camp();
  WorldTraversal.SpawnBlocked=true;Check(s.ConfirmChapterEnter()&&s.ChapterFinished&&s.ChapterRun.Failed,"unavailable spawn fails safely without legacy fallback enemies");WorldTraversal.SpawnBlocked=false;s.Camp();
  var state=new ChapterCombatRun(ChapterNode.ForestCourt,ChapterDifficulty.Normal,42);Check(!state.Register(0,-1,0),"unbound room cannot accept spawn");state.BindRoom(7);
  for(int i=0;i<6;i++)Check(state.Register(0,7,i)&&!state.Register(0,7,i),"registration is bounded and idempotent");
  Check(!state.Register(0,7,6)&&!state.Defeat(0,6,0)&&!state.Defeat(1,7,0),"cap and stale epoch/room rejected");
  state.Advance(200,true,true,false);Check(state.Progress==.25f,"large frame cannot skip objective interaction");state.Advance(float.NaN,true,true,false);state.Advance(1,false,true,false);state.Advance(1,true,true,true);Check(state.Progress==.25f,"pause contest invalid delta retain progress");
  Check(!state.Exit(true,false)&&!state.FinishBoss(),"no early room exit or boss completion");state.Fail();Check(!state.Defeat(0,7,0)&&!state.Exit(true,false),"failed state rejects late callbacks");
  s.SelectedChapterDifficulty=ChapterDifficulty.Heroic;Check(s.ConfirmChapterEnter(),"heroic fixture entry");Check(Math.Abs(s.Enemies[0].MaxHealth-CombatBalance.EnemyHealth(s.DungeonEntryLevel,1,false,EnemyKind.Wisp)*1.35f)<.001f&&Math.Abs(s.Enemies[0].damage-CombatBalance.EnemyDamage(s.DungeonEntryLevel,1,false)*1.25f)<.001f,"heroic difficulty uses stated stat multipliers");s.Camp();
  s.SelectedChapterNode=ChapterNode.Redrock;Check(s.ConfirmChapterEnter(),"heroic redrock entry");Formation(s);s.OnEnemyKilled(s.Enemies[0]);Exit(s);Formation(s);s.FailForTest();s.Camp();
  // Independent real Redrock -> Star progression, without FixtureUnlock's synthetic Star-complete mask.
  var boundary=Fresh(Path.Combine(folder,"core-boundary"));boundary.Progression.Profile.chapterCompletedMask=1;boundary.Progression.Profile.chapterHighestDifficulties=new[]{1,0,0};boundary.Progression.Save();Check(!boundary.Progression.Profile.pendingFirstClearReward,"forest migration retains no core entitlement");
  boundary.SelectedChapterNode=ChapterNode.Redrock;Check(boundary.ConfirmChapterEnter(),"core boundary enters redrock");boundary.OnEnemyKilled(boundary.Enemies[0]);Exit(boundary);foreach(var enemy in new List<EnemyController>(boundary.Enemies))boundary.OnEnemyKilled(enemy);Capture(boundary);Exit(boundary);
  Check(boundary.ChapterResult.Saved&&!boundary.ChapterResult.FirstCoreAvailable&&!boundary.Progression.Profile.pendingFirstClearReward,"REDROCK_CORE_BOUNDARY actual redrock completion grants no shared core");boundary.Camp();Check(boundary.Progression.LoadSlot(boundary.Progression.CurrentSlotId)&&!boundary.Progression.Profile.pendingFirstClearReward,"redrock save reload cannot manufacture core entitlement");
  boundary.SelectedChapterNode=ChapterNode.StarPlatform;Check(boundary.ConfirmChapterEnter(),"core boundary enters star after earlier nodes");foreach(var enemy in new List<EnemyController>(boundary.Enemies))boundary.OnEnemyKilled(enemy);
  Check(boundary.ChapterResult.Saved&&boundary.ChapterResult.FirstCoreAvailable&&boundary.Progression.Profile.pendingFirstClearReward,"STAR_CORE_BOUNDARY only actual saved star completion newly enables shared core");int coreMaterials=boundary.Progression.Profile.mechanicMaterials;Check(boundary.TrySettleChapterReward()&&boundary.Progression.Profile.mechanicMaterials==coreMaterials,"star retry cannot repeat materials or entitlement");boundary.Camp();Check(boundary.Progression.LoadSlot(boundary.Progression.CurrentSlotId)&&boundary.Progression.Profile.pendingFirstClearReward,"star entitlement survives real save reload");
  return "PASS: "+checks+" actual chapter host/save/transition/kill assertions (managed scene doubles, no Unity gameplay)";
 }
}
