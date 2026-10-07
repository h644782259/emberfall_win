// One real service/session journey per class and fresh/legacy start. Unity scene/math,
// player movement, AI placement and JsonUtility are the existing explicit host boundaries.
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using UnityEngine;
namespace Emberfall
{
 public sealed partial class GameSession
 {
  static int journeyChecks,journeyCases,journeyChapters;
  static string journeyContext;
  static void JourneyCheck(bool ok,string why){journeyChecks++;if(!ok)throw new Exception(journeyContext+": "+why);}
  static string JourneyState(ProgressionService p)=>JsonUtility.ToJson(p.Profile,true);
  static string JourneyFiles(ProgressionService p)
  {
   return string.Join("|",new[]{p.SaveFilePath,p.SaveFilePath+".bak",p.SaveFilePath+".delete-pending"}.Select(f=>File.Exists(f)?f+":"+File.GetLastWriteTimeUtc(f).Ticks+":"+Convert.ToBase64String(File.ReadAllBytes(f)):Directory.Exists(f)?f+":directory":f+":missing"));
  }
  static long JourneyTotalXp(ProgressionService p){long xp=p.Profile.xp;for(int level=1;level<p.Profile.level;level++)xp+=GameBalance.XpToNext(level);return xp;}
  static void JourneyBudget(ProgressionService p,string stage)
  {
   JourneyCheck(p.Profile.skillPoints>=0&&p.Profile.skillPoints+p.Profile.skillRanks.Sum()+p.Profile.masteryRanks.Sum()==GameBalance.SkillPointBudget(p.Profile.level),stage+" shared point budget conserved");
   JourneyCheck(p.Profile.inventory.Select(i=>i.id).Distinct().Count()==p.Profile.inventory.Count,stage+" inventory stable IDs unique");
   JourneyCheck(p.Profile.gold>=0&&p.Profile.mechanicMaterials>=0,stage+" currencies nonnegative");
  }
  static void JourneyStage(GameSession s,string stage)
  {
   JourneyBudget(s.Progression,stage);var p=s.Progression.Profile;
   Console.WriteLine("JOURNEY "+JsonUtility.ToJson(new {scenario=journeyContext,stage,level=p.level,xp=p.xp,totalXp=JourneyTotalXp(s.Progression),gold=p.gold,materials=p.mechanicMaterials,points=p.skillPoints,spent=p.skillRanks.Sum()+p.masteryRanks.Sum(),items=p.inventory.Count,coreClaimed=p.firstClearRewardClaimed,corePending=p.pendingFirstClearReward,chapterMask=p.chapterCompletedMask,sequence=p.chapterRewardSequence},false).Replace("\n","").Replace("\r",""));
  }
  static void JourneyAtomicFailure(ProgressionService p,Func<bool> operation,string label)
  {
   string state=JourneyState(p),files=JourneyFiles(p),tmp=p.SaveFilePath+".tmp";var original=p.Profile;int events=0;Action changed=()=>events++;
   JourneyCheck(!File.Exists(tmp)&&!Directory.Exists(tmp),label+" isolated temp path clear");p.Changed+=changed;Directory.CreateDirectory(tmp);
   try
   {
    JourneyCheck(!operation(),label+" actual save refusal reported");
    JourneyCheck(ReferenceEquals(p.Profile,original)&&JourneyState(p)==state&&JourneyFiles(p)==files&&events==0,label+" failed transaction publishes nothing");
    JourneyCheck(!string.IsNullOrEmpty(p.LastError),label+" failure remains visible");
   }
   finally{p.Changed-=changed;Directory.Delete(tmp);}
  }
  static void JourneyNoMutation(ProgressionService p,Func<bool> operation,bool expected,string label)
  {
   string state=JourneyState(p),files=JourneyFiles(p);int events=0;Action changed=()=>events++;p.Changed+=changed;
   try{JourneyCheck(operation()==expected,label+" result");JourneyCheck(JourneyState(p)==state&&JourneyFiles(p)==files&&events==0,label+" no duplicate state write or event");}
   finally{p.Changed-=changed;}
  }
  static void JourneyReturn(GameSession s,bool fail)
  {
   var p=s.Progression;var receipt=s.Receipt;var run=s.ChapterRun;var result=s.ChapterResult;var oldWorld=s.world;int epoch=s.Player.CombatEpoch,retired=s.Player.Retirements,builds=WorldBuilder.Builds;
   JourneyCheck(s.ChapterFinished&&Time.timeScale==0,"terminal chapter frozen before return");
   if(fail)
   {
    string state=JourneyState(p),files=JourneyFiles(p);Directory.CreateDirectory(p.SaveFilePath+".tmp");
    try
    {
     new GameUI(s).Return();
     JourneyCheck(s.Receipt==receipt&&s.ChapterRun==run&&s.ChapterResult==result&&s.world==oldWorld&&oldWorld.activeInHierarchy&&s.Player.CombatEpoch==epoch&&s.Player.Retirements==retired&&WorldBuilder.Builds==builds&&Time.timeScale==0,"failed UI return preserves run world epoch and frozen clock");
     JourneyCheck(JourneyState(p)==state&&JourneyFiles(p)==files,"failed UI return retains earned build and disk");
    }
    finally{Directory.Delete(p.SaveFilePath+".tmp");}
   }
   new GameUI(s).Return();
   JourneyCheck(!s.ChapterActive&&s.Receipt==null&&s.ChapterRun==null&&s.ChapterResult==null&&!s.InDungeon&&!s.InputBlocked&&Time.timeScale==1,"successful UI return clears chapter and resumes camp");
   JourneyCheck(!oldWorld.activeInHierarchy&&s.world!=oldWorld&&s.Player.CombatEpoch==epoch+1&&s.Player.Retirements==retired+1,"successful UI return retires old world exactly once");
   JourneyCheck(s.chapterEnemies.Count==0&&s.transientObjects.Count==0&&!s.RunChoices.AwaitingChoice&&!s.pendingRoomChoice.AwaitingChoice&&p.ChapterTotalExperience==0,"return clears transient chapter and XP ownership");
  }
  static void JourneyChapter(GameSession s,ChapterNode node,bool failSettlement=false,bool failReturn=false)
  {
   journeyChapters++;var p=s.Progression;int entry=p.Profile.level,startGold=p.Profile.gold,startMaterials=p.Profile.mechanicMaterials,startKills=p.Profile.kills;long beforeXp=JourneyTotalXp(p),sequence=p.Profile.chapterRewardSequence;
   s.SelectedChapterNode=node;s.SelectedChapterDifficulty=ChapterDifficulty.Normal;s.SelectedChapterTier=1;s.SelectedChapterTactic=-1;s.SelectedChapterLimitedHealing=false;
   JourneyCheck(s.ConfirmChapterEnter(),"reachable normal chapter entry "+node);var receipt=s.Receipt;
   JourneyCheck(p.ChapterExperienceEntryLevel==entry,"chapter freezes actual entry level");
   int deaths=0,earnedXp=0,earnedGold=0,rooms=0;bool injected=false;
   while(!s.ChapterFinished)
   {
    JourneyCheck(++rooms<=3,"bounded chapter room count");
    foreach(var enemy in s.Enemies)enemy.transform.position=new Vector3(99,0,99); // Controlled AI position, not a navigation/playability claim.
    if(s.ChapterRun.Objective==RoomObjective.Purify)
    {
     foreach(int seal in new[]{1,0}){s.Player.transform.position=s.chapterPlan.Objectives[seal];for(int i=0;i<12;i++)s.Tick();}
     JourneyCheck(s.ChapterRun.Seals==2&&s.Enemies.Count==6,"objective-first seals preserve all enemies");
    }
    else if(s.ChapterRun.Objective==RoomObjective.Escape)
    {
     s.Player.transform.position=s.ChapterObjectivePoint;for(int i=0;i<16;i++)s.Tick();
     JourneyCheck(s.Enemies.Count==6,"objective-first exit does not invent kill rewards");
    }
    else
    {
     int count=s.ChapterRun.Objective==RoomObjective.Hunt?1:s.Enemies.Count;
     for(int i=0;i<count;i++)
     {
      var enemy=s.Enemies[0];bool last=failSettlement&&node==ChapterNode.StarPlatform&&s.Enemies.Count==1;
      string disk=JourneyFiles(p);var profile=p.Profile;int goldBefore=p.Profile.gold;long xpBefore=JourneyTotalXp(p);int pendingMask=p.Profile.chapterCompletedMask;
      if(last){Directory.CreateDirectory(p.SaveFilePath+".tmp");injected=true;}
      s.OnEnemyKilled(enemy);deaths++;earnedXp+=s.LastEnemyExperience;earnedGold+=p.Profile.gold-goldBefore;
      int share=(enemy.IsBoss?100+entry*12:22+entry*2)*3/10;
      JourneyCheck(s.LastEnemyExperience==share,"registered kill retains fixed entry share");
      JourneyNoMutation(p,()=>{s.OnEnemyKilled(enemy);return true;},true,"same enemy callback cannot pay twice");
      if(last)
      {
       JourneyCheck(s.ChapterRewardPending&&s.ChapterFinished&&!s.ChapterResult.Saved&&s.ChapterRun.RewardClaimed==false,"failed completion stays pending");
       JourneyCheck(ReferenceEquals(p.Profile,profile)&&JourneyTotalXp(p)==xpBefore+share&&p.Profile.chapterRewardSequence==sequence&&p.Profile.chapterCompletedMask==pendingMask&&p.Profile.mechanicMaterials==startMaterials&&!p.Profile.pendingFirstClearReward&&JourneyFiles(p)==disk,"failed final kill save retains live earnings but no completion publication");
       var oldWorld=s.world;int epoch=s.Player.CombatEpoch;new GameUI(s).Return();
       JourneyCheck(s.ChapterRewardPending&&s.Receipt==receipt&&s.world==oldWorld&&oldWorld.activeInHierarchy&&s.Player.CombatEpoch==epoch&&Time.timeScale==0,"return cannot discard unsaved chapter receipt");
       Directory.Delete(p.SaveFilePath+".tmp");injected=false;
       JourneyCheck(s.TrySettleChapterReward()&&s.ChapterResult.Saved&&Time.timeScale==0,"same pending receipt retries without advancing terminal clock");
      }
     }
    }
    if(!s.ChapterFinished)
    {
     JourneyCheck(s.ChapterRun.DoorUnlocked,"objective opens chapter exit");s.Player.transform.position=s.chapterPlan.Exit;
     JourneyCheck(s.EnterNextChapterRoom(),"actual chapter room transition");
    }
   }
   JourneyCheck(!injected&&s.ChapterRun.RewardClaimed&&s.ChapterResult.Saved,"chapter reward is durable");
   int normal=22+entry*2,boss=100+entry*12;
   int completion=node==ChapterNode.StarPlatform?boss+2*normal-(boss*3/10+2*(normal*3/10)):12*normal-12*(normal*3/10);
   JourneyCheck(JourneyTotalXp(p)==beforeXp+earnedXp+completion,"objective completion never redistributes skipped enemy shares");
   JourneyCheck(p.Profile.gold==startGold+earnedGold&&p.Profile.kills==startKills+deaths,"only registered deaths grant gold and kills");
   JourneyCheck(p.Profile.mechanicMaterials==startMaterials+receipt.Materials&&p.Profile.chapterRewardSequence==sequence+1,"chapter material and receipt paid exactly once");
   JourneyNoMutation(p,()=>s.TrySettleChapterReward(),true,"session settlement replay");
   JourneyNoMutation(p,()=>p.TryCompleteChapterNode(receipt),true,"service receipt replay");
   JourneyBudget(p,"chapter "+node);JourneyReturn(s,failReturn);
   JourneyNoMutation(p,()=>p.TryCompleteChapterNode(receipt),true,"durable receipt replay after camp cleanup");
   JourneyStage(s,"chapter-"+node);
  }
  static void JourneyReload(GameSession s)
  {
   var p=s.Progression;string state=JourneyState(p),slot=p.CurrentSlotId;var oldWorld=s.world;var oldPlayer=s.Player;int epoch=oldPlayer.CombatEpoch,builds=WorldBuilder.Builds;
   string primary=p.SaveFilePath,backup=primary+".bak";byte[] primaryBytes=File.ReadAllBytes(primary),backupBytes=File.ReadAllBytes(backup);var primaryTime=File.GetLastWriteTimeUtc(primary);var backupTime=File.GetLastWriteTimeUtc(backup);
   s.Paused=true;s.UpdateTimeScale();
   File.WriteAllText(primary,"{broken-primary");File.WriteAllText(backup,"{broken-backup");string corruptFiles=JourneyFiles(p);
   try
   {
    JourneyCheck(!s.LoadSaveFromPause(slot,false),"actual same-slot reload rejects both corrupt documents");
    JourneyCheck(s.Progression==p&&JourneyState(p)==state&&JourneyFiles(p)==corruptFiles&&s.world==oldWorld&&oldWorld.activeInHierarchy&&s.Player==oldPlayer&&oldPlayer.CombatEpoch==epoch&&WorldBuilder.Builds==builds&&s.Paused&&Time.timeScale==0,"failed load staging preserves built character and paused world");
   }
   finally
   {
    File.WriteAllBytes(primary,primaryBytes);File.WriteAllBytes(backup,backupBytes);File.SetLastWriteTimeUtc(primary,primaryTime);File.SetLastWriteTimeUtc(backup,backupTime);
   }
   JourneyCheck(s.LoadSaveFromPause(slot,false),"same saved role reload retries through actual session transition");
   JourneyCheck(s.Progression!=p&&JourneyState(s.Progression)==state&&s.Progression.CurrentSlotId==slot,"retried load publishes freshly staged exact character");
   JourneyCheck(!oldWorld.activeInHierarchy&&!oldPlayer.gameObject.activeInHierarchy&&s.Player!=oldPlayer&&s.world!=oldWorld&&!s.Paused&&!s.ChapterActive&&Time.timeScale==1,"successful staged reload alone retires old generation and resumes camp");
   JourneyStage(s,"actual-session-reload-retried");
  }
  public static string VerifyIntegratedJourney(string root)
  {
   journeyChecks=journeyCases=journeyChapters=0;
   foreach(HeroClass hero in Enum.GetValues(typeof(HeroClass)))foreach(bool legacy in new[]{false,true})
   {
    journeyContext=hero+(legacy?"-legacy-L1":"-fresh-L1");journeyCases++;
    var s=new GameSession{Progression=new ProgressionService(Path.Combine(root,journeyContext))};var p=s.Progression;
    JourneyCheck(p.CreateNewSlot(hero),"new isolated real save");string slot=p.CurrentSlotId;
    if(legacy)
    {
     var document=JsonNode.Parse(File.ReadAllText(p.SaveFilePath));var profile=document["profile"];profile["skillRanks"]=new JsonArray(Enumerable.Range(0,3).Select(i=>(JsonNode)JsonValue.Create(0)).ToArray());profile["skillPoints"]=0;
     string oldSave=document.ToJsonString(new System.Text.Json.JsonSerializerOptions{WriteIndented=true});File.WriteAllText(p.SaveFilePath,oldSave);File.WriteAllText(p.SaveFilePath+".bak",oldSave);
     p=new ProgressionService(p.SaveDirectory);JourneyCheck(p.LoadSlot(slot),"legacy level-one save loads through real migration");s.Progression=p;
    }
    JourneyCheck(p.Profile.level==1&&p.Profile.skillRanks[0]==1&&p.Profile.skillRanks.Sum()==1&&p.Profile.skillPoints==0,"fresh and legacy start with exactly one allocated first skill");
    JourneyCheck(p.Profile.chapterCompletedMask==0&&!p.Profile.pendingFirstClearReward&&!p.Profile.firstClearRewardClaimed,"start has no fabricated unlocks or claim");
    JourneyStage(s,"start");JourneyChapter(s,ChapterNode.ForestCourt,false,true);
    JourneyCheck(p.Profile.level==3&&p.Profile.xp==54&&!p.Profile.pendingFirstClearReward,"objective-first forest exact minimum XP and no core");
    JourneyChapter(s,ChapterNode.Redrock);
    JourneyCheck(p.Profile.level==5&&p.Profile.xp==32&&!p.Profile.pendingFirstClearReward,"required hunt only exact XP and no early core");
    JourneyChapter(s,ChapterNode.StarPlatform,true,true);
    JourneyCheck(p.Profile.level==6&&p.Profile.xp==76&&p.Profile.mechanicMaterials==7&&p.Profile.pendingFirstClearReward&&!p.Profile.firstClearRewardClaimed,"first star exact earnings and shared entitlement");
    var mechanic=BuildCatalog.MechanicsFor(hero)[0];var ids=p.Profile.inventory.Select(i=>i.id).ToArray();int gold=p.Profile.gold,materials=p.Profile.mechanicMaterials;
    JourneyAtomicFailure(p,()=>p.ClaimFirstClearReward(mechanic),"first shared core claim");
    JourneyCheck(p.ClaimFirstClearReward(mechanic),"same first-core claim retries");
    var attachment=p.Attachment(mechanic);JourneyCheck(attachment!=null&&attachment.mounted&&attachment.rarity==Rarity.Epic&&p.Profile.inventory.Count==ids.Length,"first entitlement grants independent attachment without an inventory item");
    // Exercise retained legacy equipment investments with an actual mechanism-drop factory.
    var core=p.CreateMechanicItem(mechanic);JourneyCheck(p.CollectLoot(core),"legacy mechanism drop remains receivable");string coreId=core.id;
    JourneyCheck(core.level==ProgressionService.EquipmentGenerationLevel(6)&&core.mechanic==mechanic&&core.rarity==Rarity.Epic&&core.locked&&p.Profile.inventory.Count==ids.Length+1&&p.Profile.gold==gold&&p.Profile.mechanicMaterials==materials&&p.Profile.firstClearRewardClaimed&&!p.Profile.pendingFirstClearReward,"one first core retains reward identity and no currency charge");
    JourneyNoMutation(p,()=>p.ClaimFirstClearReward(mechanic),false,"shared first core cannot be claimed twice");JourneyStage(s,"core-claimed");
    JourneyBuild(s,coreId);JourneyReload(s);p=s.Progression;
    var grown=p.Profile.inventory.Single(i=>i.id==coreId);int grownLevel=grown.level,grownRank=p.SlotUpgradeRank(grown.slot),grownGold=p.Profile.gold;
    JourneyChapter(s,ChapterNode.ForestCourt,false,true);
    grown=p.Profile.inventory.Single(i=>i.id==coreId);
    JourneyCheck(grown.level==grownLevel&&p.SlotUpgradeRank(grown.slot)==grownRank&&p.Equipped(grown.slot).id==coreId&&p.Profile.gold==grownGold&&p.Profile.firstClearRewardClaimed&&!p.Profile.pendingFirstClearReward,"post-build reward and failed-return retry preserve grown core and currencies");
    string state=JourneyState(p);JourneyCheck(p.LoadSlot(slot)&&JourneyState(p)==state,"same final slot reload keeps complete journey state");
    JourneyNoMutation(p,()=>p.ClaimFirstClearReward(mechanic),false,"reload cannot reopen first core");
    // Another actual legacy-mode completion must share the same entitlement flag.
    JourneyCheck(p.TryGrantModeReward(Guid.NewGuid().ToString("N"),0,0,0,1),"old-mode reward entry remains usable after chapter/build chain");
    JourneyCheck(p.Profile.firstClearRewardClaimed&&!p.Profile.pendingFirstClearReward&&p.Profile.inventory.Count==ids.Length+1,"later legacy mode never duplicates shared chapter core");
    JourneyNoMutation(p,()=>p.ClaimFirstClearReward(mechanic),false,"legacy completion cannot reopen shared core");JourneyStage(s,"finished");
   }
   return "PASS: "+journeyChecks+" integrated production journey assertions across "+journeyCases+" fresh/legacy class journeys and "+journeyChapters+" actual chapter attempts; real temp filesystem, managed JsonUtility/scene boundaries";
  }
 }
}
