"""Exercise level and dungeon milestone receipts against the actual save service."""
from pathlib import Path
import importlib.util,os,subprocess,tempfile
root=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('cv',root/'Tools/cloud-validation.py');cv=importlib.util.module_from_spec(spec);spec.loader.exec_module(cv)
program=r'''using System;using System.IO;using System.Linq;using Emberfall;
class Program{static int n;static void C(bool b,string s){n++;if(!b)throw new Exception(s);}static void Main(string[] args){
 var p=new ProgressionService(args[0]);C(p.CreateNewSlot(HeroClass.Ranger),"create fixture");
 p.Profile.level=59;p.Profile.highestAdventureTier=5;p.Save();
 foreach(string prefix in new[]{"level/","dungeon/"})foreach(int level in new[]{60,70,80,90}){var id=prefix+level;C(ProgressionService.Achievements.Count(a=>a.Id==id)==1,"each intermediate milestone has unique stable ID");C(!p.ClaimAchievement(id),"cannot claim before required level");}
 p.Profile.level=100;p.Profile.highestAdventureTier=10;p.Save();C(p.ClaimAchievement("level/50")&&p.ClaimAchievement("level/100"),"old milestone IDs still work");
 foreach(string prefix in new[]{"level/","dungeon/"})foreach(int level in new[]{60,70,80,90}){
 var id=prefix+level;int gold=p.Profile.gold;Directory.CreateDirectory(p.SaveFilePath+".tmp");C(!p.ClaimAchievement(id)&&p.Profile.gold==gold&&!p.AchievementClaimed(id),"failed write does not consume claim");Directory.Delete(p.SaveFilePath+".tmp");C(p.ClaimAchievement(id),"existing high-level save can claim newly added milestone");gold=p.Profile.gold;C(!p.ClaimAchievement(id)&&p.Profile.gold==gold,"same milestone grants once");}
 var reload=new ProgressionService(args[0]);C(reload.LoadSlot(p.CurrentSlotId),"reload milestone receipts");foreach(string prefix in new[]{"level/","dungeon/"})foreach(int level in new[]{60,70,80,90})C(reload.AchievementClaimed(prefix+level)&&!reload.ClaimAchievement(prefix+level),"reloading cannot duplicate intermediate rewards");C(reload.AchievementClaimed("level/50")&&reload.AchievementClaimed("level/100"),"old receipts remain claimed");
 for(int node=0;node<3;node++){
 var chapter=(ChapterNode)node;int unlock=30+node*10;
 p.Profile.level=unlock-1;C(!ChapterProgression.IsUnlocked(p.Profile,chapter),"chapter level gate holds below threshold");
 p.Profile.level=unlock;C(ChapterProgression.IsUnlocked(p.Profile,chapter),"chapter opens exactly at 30/40/50 without old chain gating");
 C(ChapterProgression.LevelTier(unlock+9)==unlock/10&&ChapterProgression.LevelTier(unlock+10)==unlock/10+1,"ten-level band boundaries");
 ChapterRunReceipt receipt;C(!p.TryBeginChapterNode(chapter,ChapterDifficulty.Normal,1,out receipt),"manual obsolete tier is rejected");
 C(p.TryBeginChapterNode(chapter,ChapterProgression.LevelDifficulty(chapter,p.Profile.level),ChapterProgression.LevelTier(p.Profile.level),out receipt),"level-derived entry accepted");
 C(ProgressionService.Achievements.Single(a=>a.Id=="chapter-unlock/"+node).Progress(p.Profile)==unlock,"unlock achievement follows same gate");
 C(ProgressionService.Achievements.Single(a=>a.Id=="chapter-band/"+node+"/"+unlock).Progress(p.Profile)==0,"old manual tiers do not fabricate level clears");
 p.Profile.chapterBestLevels[node]=unlock;C(ProgressionService.Achievements.Single(a=>a.Id=="chapter-band/"+node+"/"+unlock).Progress(p.Profile)==unlock,"level-clear achievement reads actual new records");
 }
 C(AdventureRewardRules.EnemyEquipmentChance(false,false)==8&&AdventureRewardRules.EnemyEquipmentChance(false,true)==35&&AdventureRewardRules.EnemyEquipmentChance(true,true)==100,"enemy equipment sources follow intended rates");

 var smith=new ProgressionService(Path.Combine(args[0],"smith"));C(smith.CreateNewSlot(HeroClass.Arcanist),"smith fixture");smith.Profile.level=100;smith.Profile.mechanicMaterials=200;smith.Profile.chapterBestLevels[0]=50;
 smith.Profile.attachments.Add(new MechanicAttachment{id=Guid.NewGuid().ToString("N"),mechanic=EquipmentMechanic.CinderTrail,rarity=Rarity.Epic});smith.Save();

 C(smith.AttachmentAscensionLock(EquipmentMechanic.CinderTrail,true)!="","rank0 cannot ascend despite level100");
 float initialPower=smith.MechanicPowerMultiplier(EquipmentMechanic.CinderTrail);smith.Attachment(EquipmentMechanic.CinderTrail).mounted=true;
 for(int rank=1;rank<=9;rank++){
 C(smith.UpgradeAttachment(EquipmentMechanic.CinderTrail,true)&&smith.Attachment(EquipmentMechanic.CinderTrail).upgradeRank==rank,"consecutive rank upgrades");
 int count=smith.Attachment(EquipmentMechanic.CinderTrail).ascensionRank;
 C(Math.Abs(smith.MechanicPowerMultiplier(EquipmentMechanic.CinderTrail)-BuildCatalog.AscensionPower(count))<.0001f,"upgrade does not alter mechanic strength");
 if(rank%3==0){int before=smith.Profile.mechanicMaterials;Directory.CreateDirectory(smith.SaveFilePath+".tmp");C(!smith.AscendAttachment(EquipmentMechanic.CinderTrail,true)&&smith.Profile.mechanicMaterials==before&&smith.Attachment(EquipmentMechanic.CinderTrail).ascensionRank==count,"failed save cannot consume ascension");Directory.Delete(smith.SaveFilePath+".tmp");C(smith.AscendAttachment(EquipmentMechanic.CinderTrail,true)&&smith.Attachment(EquipmentMechanic.CinderTrail).ascensionRank==rank/3&&smith.Profile.mechanicMaterials==before-24,"ascend at 3 6 9");C(smith.Attachment(EquipmentMechanic.CinderTrail).rarity==Rarity.Epic,"ascension does not silently upgrade numeric rarity");}
 C(!smith.AscendAttachment(EquipmentMechanic.CinderTrail,true),"cannot repeat ascension before next threshold");
 }
 int costBefore=smith.Profile.mechanicMaterials;C(!smith.UpgradeAttachment(EquipmentMechanic.CinderTrail,true)&&smith.Profile.mechanicMaterials==costBefore,"max9 no additional payment");
 C(smith.ToggleAttachmentVariant(EquipmentMechanic.CinderTrail,true)&&smith.Profile.mechanicMaterials==costBefore,"unlocked switching has no shard cost");
 var smithReload=new ProgressionService(Path.Combine(args[0],"smith"));C(smithReload.LoadSlot(smith.CurrentSlotId)&&smithReload.Attachment(EquipmentMechanic.CinderTrail).ascensionRank==3,"all ascensions survive reload");
 C(smith.Profile.equippedSkills[9]==GameBalance.HotbarPotion&&smith.Profile.hotbarKeys[9]==98,"new character potion defaults to B");
 var baseStats=smith.GetStats();smith.Profile.fashions.Add(new FashionData{id="testwing",slot=FashionSlot.Wings,rarity=Rarity.Legendary});var wingsStats=smith.GetStats();C(Math.Abs(wingsStats.MoveSpeed/baseStats.MoveSpeed-1.08f)<.0001f,"wing adds actual8percent speed");smith.Profile.fashions.Add(new FashionData{id="weakwing",slot=FashionSlot.Wings,rarity=Rarity.Rare});C(Math.Abs(smith.GetStats().MoveSpeed-wingsStats.MoveSpeed)<.0001f,"fashion collection does not stack same slot");
 var gear=smith.Equipped(ItemSlot.Weapon);smith.Profile.refinementStones=100;smith.Save();var cap=smith.PreviewRefinementLimit(gear.id);int oldAttack=gear.attack,oldRank=gear.upgradeLevel;
 Directory.CreateDirectory(smith.SaveFilePath+".tmp");C(!smith.RefineEquipment(gear.id,true)&&smith.Profile.refinementStones==100&&smith.Profile.inventory.Find(x=>x.id==gear.id).attack==oldAttack,"refine rollback on disk failure");Directory.Delete(smith.SaveFilePath+".tmp");
 int attempts=0;while(smith.RefinementLockReason(gear.id,true)==""&&attempts++<100){oldAttack=smith.Profile.inventory.Find(x=>x.id==gear.id).attack;C(smith.RefineEquipment(gear.id,true),"refine commits");var changed=smith.Profile.inventory.Find(x=>x.id==gear.id);C(changed.attack>=oldAttack&&changed.attack<=cap.attack&&changed.upgradeLevel==oldRank,"refine nondecreasing capped and retains upgrade rank");}
 C(attempts<100&&smith.RefinementLockReason(gear.id,true)=="数值已满","refine reaches cap");costBefore=smith.Profile.refinementStones;C(!smith.RefineEquipment(gear.id,true)&&smith.Profile.refinementStones==costBefore,"max refinement spends no stone");C(smith.Profile.refinementCount>0&&smith.Profile.refinementMaxCount==1,"refinement achievement evidence recorded");
 var refineReload=new ProgressionService(Path.Combine(args[0],"smith"));C(refineReload.LoadSlot(smith.CurrentSlotId)&&refineReload.Profile.inventory.Find(x=>x.id==gear.id).attack==cap.attack,"refined values survive reload");
 var dungeon=new ProgressionService(Path.Combine(args[0],"chapter-drops"));dungeon.NewGame(HeroClass.Arcanist);dungeon.Profile.level=60;dungeon.Save();
 for(int node=0;node<3;node++){
 ChapterRunReceipt receipt;var chapter=(ChapterNode)node;C(dungeon.TryBeginChapterNode(chapter,ChapterProgression.AvailableDifficulty(dungeon.Profile,chapter),ChapterProgression.AvailableTier(dungeon.Profile,chapter),out receipt),"chapter begins");
 for(int room=0;room<(node==2?1:2);room++)for(int enemy=0;enemy<(node==2?3:6);enemy++)C(dungeon.RegisterChapterEnemy(receipt,room,enemy,node==2&&enemy==0),"register actual room enemy");
 int stones=dungeon.Profile.refinementStones;C(dungeon.TryCompleteChapterNode(receipt),"complete actual chapter");C(dungeon.Profile.refinementStones-stones==ProgressionService.ChapterRefinementStones(chapter,receipt.Tier),"chapter stone reward matches preview");C(dungeon.TryCompleteChapterNode(receipt)&&dungeon.Profile.refinementStones-stones==ProgressionService.ChapterRefinementStones(chapter,receipt.Tier),"repeat settlement cannot duplicate stones");C(dungeon.OpenDungeonChest()!=null,"open chapter chest");var chest=dungeon.LastChestReward;C(node==2?chest.gemMechanic!=EquipmentMechanic.None&&chest.gemRarity>=Rarity.Epic:chest.gemMechanic==EquipmentMechanic.None,"gem exclusive chapter and minimum epic");dungeon.AcknowledgeChestReward();
 }
 for(int i=0;i<100;i++)C(dungeon.RollLoot(60,true,6).mechanic==EquipmentMechanic.None,"ordinary enemy drop cannot bypass exclusive gem source");

 for(int tier=1;tier<=10;tier++)for(int roll=0;roll<100;roll++)foreach(bool boss in new[]{false,true})C(TierRewardRules.DropRarity(boss,tier,roll)<=AdventureRewardRules.MinimumRarity(0),"enemy rarity never exceeds clear guarantee");
 var choose=new ProgressionService(Path.Combine(args[0],"choose"));choose.NewGame(HeroClass.Ranger);choose.PrepareDungeonChest();Directory.CreateDirectory(choose.SaveFilePath+".tmp");C(choose.OpenChosenDungeonChest(2)==null&&choose.SelectedRewardChest==2,"failed write retains chosen chest in session");Directory.Delete(choose.SaveFilePath+".tmp");C(choose.OpenChosenDungeonChest(0)==null&&choose.SelectedRewardChest==2,"cannot switch failed frozen choice");choose.Save();C(string.IsNullOrEmpty(choose.LastError),"persist frozen choice");var resumed=new ProgressionService(Path.Combine(args[0],"choose"));C(resumed.LoadSlot(choose.CurrentSlotId)&&resumed.SelectedRewardChest==2,"selected chest survives restart");C(resumed.OpenChosenDungeonChest(1)==null,"restart cannot change chosen box");C(resumed.OpenChosenDungeonChest(2)!=null&&resumed.LastChestReward.selectedChest==2,"chosen box opens on retry");int paid=resumed.Profile.gold;C(resumed.OpenChosenDungeonChest(0)==null&&resumed.Profile.gold==paid,"remaining boxes cannot grant again");

 var statsService=new ProgressionService(Path.Combine(args[0],"stats"));statsService.NewGame(HeroClass.Ranger);var plain=statsService.GetStats();
 for(int i=0;i<9;i++){
 statsService.Profile.attachments.Clear();var gem=new MechanicAttachment{id="attribute-test",mechanic=(EquipmentMechanic)((int)EquipmentMechanic.WeaponPower+i),rarity=Rarity.Epic,mounted=true,ascensionRank=0};statsService.Profile.attachments.Add(gem);var v=statsService.GetStats();
 bool changed=i==0?v.Damage>plain.Damage:i==1?v.CritChance>plain.CritChance:i==2?v.CritDamageBonus>plain.CritDamageBonus:i==3?v.MaxHealth>plain.MaxHealth:i==4?v.Armor>plain.Armor:i==5?v.DamageReduction>plain.DamageReduction:i==6?v.EnergyRecovery>plain.EnergyRecovery:i==7?v.CooldownReduction>plain.CooldownReduction:v.MoveSpeed>plain.MoveSpeed;
 C(changed,"each equipment gem grants its advertised stat");gem.mounted=false;v=statsService.GetStats();C(v.Damage==plain.Damage&&v.CritChance==plain.CritChance&&v.MaxHealth==plain.MaxHealth&&v.MoveSpeed==plain.MoveSpeed&&v.EnergyRecovery==plain.EnergyRecovery&&v.CooldownReduction==plain.CooldownReduction&&v.DamageReduction==plain.DamageReduction,"unmounted gem grants nothing");
 gem.mounted=true;gem.ascensionRank=3;v=statsService.GetStats();C(i<3?v.GemHealthyDamage>.14f:i<6?v.GemLowHealthGuard>.14f:v.GemLowEnergyRecovery>.59f,"ascension improves slot-specific conditional mechanic");
 }
 statsService.Profile.attachments.Clear();statsService.Profile.attachments.Add(new MechanicAttachment{id="off-class",mechanic=EquipmentMechanic.CinderTrail,mounted=true,upgradeRank=9,ascensionRank=3});C(statsService.GetStats().Damage==plain.Damage,"off-class gem inactive");

 var batch=new ProgressionService(Path.Combine(args[0],"claim-all"));batch.NewGame(HeroClass.Ranger);batch.Profile.level=100;batch.Profile.highestAdventureTier=10;batch.Save();
 int ready=batch.ClaimableAchievements,initialGold=batch.Profile.gold,initialShards=batch.Profile.mechanicMaterials;
 C(ready>2,"batch has multiple eligible rewards");Directory.CreateDirectory(batch.SaveFilePath+".tmp");
 C(!batch.ClaimAllAchievements()&&batch.Profile.gold==initialGold&&batch.Profile.mechanicMaterials==initialShards&&batch.ClaimableAchievements==ready,"batch write failure is all or nothing");Directory.Delete(batch.SaveFilePath+".tmp");
 C(batch.ClaimAllAchievements()&&batch.ClaimableAchievements==0,"claim all collects each eligible receipt");int allGold=batch.Profile.gold;
 C(!batch.ClaimAllAchievements()&&batch.Profile.gold==allGold,"repeat batch cannot duplicate rewards");
 var batchReload=new ProgressionService(Path.Combine(args[0],"claim-all"));C(batchReload.LoadSlot(batch.CurrentSlotId)&&batchReload.ClaimableAchievements==0&&batchReload.Profile.gold==allGold,"batch durable across restart");
 var trade=new ProgressionService(Path.Combine(args[0],"trades"));trade.NewGame(HeroClass.Arcanist);trade.Profile.clearedRuns=1;trade.Profile.pendingFirstClearReward=true;trade.Profile.attachments.Add(new MechanicAttachment{id="ownedlegend",mechanic=EquipmentMechanic.CinderTrail,rarity=Rarity.Legendary,upgradeRank=6,ascensionRank=2,mounted=false});trade.Save();
 var firstQuote=trade.PrepareMerchantPurchase(EquipmentMechanic.CinderTrail,true,Rarity.Epic);C(firstQuote!=null,"owned gem no longer deadlocks first clear");int shardsBefore=trade.Profile.mechanicMaterials;
 Directory.CreateDirectory(trade.SaveFilePath+".tmp");C(!trade.BuyAtMerchant(firstQuote,true)&&!trade.Profile.firstClearRewardClaimed&&trade.Profile.mechanicMaterials==shardsBefore,"first claim rolls back on save failure");Directory.Delete(trade.SaveFilePath+".tmp");
 C(trade.BuyAtMerchant(firstQuote,true)&&trade.Profile.firstClearRewardClaimed&&trade.Profile.mechanicMaterials==shardsBefore+3,"duplicate first claim converts exactly once");
 C(trade.Attachment(EquipmentMechanic.CinderTrail).rarity==Rarity.Legendary&&trade.Attachment(EquipmentMechanic.CinderTrail).upgradeRank==6,"free duplicate never downgrades owned gem");
 C(!trade.BuyAtMerchant(firstQuote,true)&&trade.Profile.mechanicMaterials==shardsBefore+3,"stale first-clear quote cannot grant twice");
 var firstAttribute=new ProgressionService(Path.Combine(args[0],"first-attribute"));firstAttribute.NewGame(HeroClass.Ranger);firstAttribute.Profile.clearedRuns=1;firstAttribute.Profile.pendingFirstClearReward=true;firstAttribute.Save();
 var attributeQuote=firstAttribute.PrepareMerchantPurchase(EquipmentMechanic.WeaponPower,true,Rarity.Epic);C(attributeQuote!=null&&firstAttribute.BuyAtMerchant(attributeQuote,true)&&firstAttribute.Attachment(EquipmentMechanic.WeaponPower).rarity==Rarity.Epic&&firstAttribute.Profile.firstClearRewardClaimed,"attribute gem can be selected as first-clear reward");
 trade.Attachment(EquipmentMechanic.CinderTrail).mounted=true;C(!trade.SellGem(EquipmentMechanic.CinderTrail,true),"mounted gems protected from sale");trade.Attachment(EquipmentMechanic.CinderTrail).mounted=false;
 var legacy=trade.Equipped(ItemSlot.Weapon);legacy.mechanic=EquipmentMechanic.CinderTrail;trade.Save();trade.Attachment(EquipmentMechanic.CinderTrail).mounted=false;trade.Save();
 int saleGold=trade.Profile.gold,saleValue=trade.GemSellValue(EquipmentMechanic.CinderTrail);Directory.CreateDirectory(trade.SaveFilePath+".tmp");
 C(!trade.SellGem(EquipmentMechanic.CinderTrail,true)&&trade.Attachment(EquipmentMechanic.CinderTrail)!=null&&trade.Profile.gold==saleGold,"failed sale keeps gem and balance");Directory.Delete(trade.SaveFilePath+".tmp");
 C(trade.SellGem(EquipmentMechanic.CinderTrail,true)&&trade.Attachment(EquipmentMechanic.CinderTrail)==null&&trade.Profile.gold==saleGold+saleValue,"gem sale atomic receipt");
 C(!trade.SellGem(EquipmentMechanic.CinderTrail,true)&&trade.Profile.gold==saleGold+saleValue,"gem cannot sell twice");
 var soldReload=new ProgressionService(Path.Combine(args[0],"trades"));C(soldReload.LoadSlot(trade.CurrentSlotId)&&soldReload.Attachment(EquipmentMechanic.CinderTrail)==null,"legacy equipment cannot resurrect sold gem after reload");

 foreach(HeroClass hero in Enum.GetValues(typeof(HeroClass)))foreach(var mechanic in BuildCatalog.GemsFor(hero))
 {
  var form=new ProgressionService(Path.Combine(args[0],"forms-"+hero+"-"+mechanic));form.NewGame(hero);form.Profile.level=100;form.Profile.mechanicMaterials=9999;
  form.Profile.attachments.Clear();form.Profile.attachments.Add(new MechanicAttachment{id="form",mechanic=mechanic,rarity=Rarity.Common,ascensionRank=0,upgradeRank=0,mounted=true});form.Save();
  C(BuildCatalog.HasMechanicVariant(mechanic),"every gem supports forms");
  C(!form.ToggleAttachmentVariant(mechanic,true),"unascended gem cannot switch form");
  if(!BuildCatalog.IsAttributeGem(mechanic))C(!form.HasMechanic(mechanic),"unascended gem grants stats without combat mechanism");
  for(int asc=1;asc<=3;asc++){
   for(int step=0;step<3;step++)C(form.UpgradeAttachment(mechanic,true),"three actual upgrades before ascension");
   C(form.AscendAttachment(mechanic,true)&&form.Attachment(mechanic).variantUnlocked,"every gem ascension unlocks form");
   C(form.ToggleAttachmentVariant(mechanic,true)&&form.Attachment(mechanic).variant==1,"form B selectable");
   C(BuildCatalog.GemFormDescription(mechanic,0,asc)!=BuildCatalog.GemFormDescription(mechanic,1,asc),"form descriptions have distinct effects");
   if(BuildCatalog.IsAttributeGem(mechanic)){
    var stats=form.GetStats();var slot=BuildCatalog.MechanicSlot(mechanic);
    C(slot==ItemSlot.Weapon?stats.GemLowHealthDamage>0&&stats.GemHealthyDamage==0:slot==ItemSlot.Armor?stats.GemHealthyGuard>0&&stats.GemLowHealthGuard==0:stats.GemHighEnergyRecovery>0&&stats.GemLowEnergyRecovery==0,"B selects distinct runtime trigger");
   }else C(form.HasMechanic(mechanic),"ascended mechanism active");
   C(form.ToggleAttachmentVariant(mechanic,true)&&form.Attachment(mechanic).variant==0,"switch back A free");
  }
  var loaded=new ProgressionService(Path.Combine(args[0],"forms-"+hero+"-"+mechanic));C(loaded.LoadSlot(form.CurrentSlotId)&&loaded.Attachment(mechanic).ascensionRank==3&&loaded.Attachment(mechanic).variantUnlocked,"all gem forms survive reload");
 }
 var levels=new GameProfile{level=100,chapterBestTiers=new int[3]};
 for(int node=0;node<3;node++){
  var chapter=(ChapterNode)node;int first=ChapterProgression.UnlockLevel(chapter)/10;
  C(ChapterProgression.AvailableTier(levels,chapter)==10,"chapter difficulty follows character level without prior clears");
  levels.chapterBestTiers[node]=first;C(ChapterProgression.AvailableTier(levels,chapter)==10,"chapter clear does not lower level-derived difficulty");
  levels.level=first*10;C(ChapterProgression.AvailableTier(levels,chapter)==first,"character level still limits selection");levels.level=100;
 }
 var goalService=new ProgressionService(Path.Combine(args[0],"finished-goal"));goalService.NewGame(HeroClass.Vanguard);goalService.Profile.automaticGrowth=true;goalService.Profile.classTutorialCompleted=true;goalService.Profile.attachments.Add(new MechanicAttachment{id="goal-gem",mechanic=EquipmentMechanic.ReturningBlade,upgradeRank=3,ascensionRank=1,variantUnlocked=true});goalService.Profile.chapterBestLevels[0]=100;
 C(goalService.SelectedProgressionGoal().RequiredAdventureTier==0&&goalService.SelectedProgressionGoal().Title=="成长目标已完成","Lv100 chapter clear advances goal without obsolete reward receipt");
 goalService.Profile.chapterBestLevels[0]=0;goalService.Profile.adventureBestTiers[2]=10;C(goalService.SelectedProgressionGoal().RequiredAdventureTier==0,"independent dungeon record counts for goal");
 goalService.Save();var goalReload=new ProgressionService(Path.Combine(args[0],"finished-goal"));C(goalReload.LoadSlot(goalService.CurrentSlotId)&&goalReload.SelectedProgressionGoal().RequiredAdventureTier==0,"completed goal stays complete after reload");
 for(int hero=0;hero<4;hero++)C(GameBalance.SkillEnergyCost((HeroClass)hero,9)==0,"all ultimate skills cost zero energy");
 var affixService=new ProgressionService(Path.Combine(args[0],"attack-affix"));affixService.NewGame(HeroClass.Ranger);
 var affixItem=affixService.Equipped(ItemSlot.Weapon);C(affixItem!=null,"starter weapon fixture");
 affixItem.attackPercent=0;float plainDamage=affixService.GetStats().Damage;affixItem.attackPercent=.12f;
 C(affixService.GetStats().Damage>plainDamage,"attack percent changes actual equipped damage");
 affixService.Save();var affixLoaded=new ProgressionService(Path.Combine(args[0],"attack-affix"));C(affixLoaded.LoadSlot(affixService.CurrentSlotId)&&Math.Abs(affixLoaded.Equipped(ItemSlot.Weapon).attackPercent-.12f)<.0001,"attack percent survives save reload");
 Console.WriteLine("PASS "+n+" achievement boundary/persistence assertions");}}
'''
with tempfile.TemporaryDirectory(prefix='achievement-milestones-') as tmp:
 p=Path(tmp);names=['SkillRuntime','GameTypes','ProgressionService','ProgressionService.Attachments','ProgressionService.AutomaticGrowth','ProgressionService.Trading','CombatBalance','SkillDamageBudgets','HubTravelRules','MasteryCoreRuntime','TierRewardRules','TierRewardBand','ProgressionGoalState','ChapterProgression','ProgressionService.Chapter','ProgressionService.Reforge','ReforgeQuote','RoomTactics','CombatImpactBatch','SafeSaveFlow']
 sources=[root/('Assets/Scripts/Core/'+name+'.cs') for name in names]+[root/'Tests/ProgressionTests.cs'];project=cv.write_project(p/'project',sources,program)
 sdk='/Applications/Unity/Hub/Editor/6000.6.4f1/Unity.app/Contents/Resources/Scripting/DotNetSdk/dotnet'
 subprocess.run([sdk,'run','--project',str(project),'--',str(p/'saves')],check=True,env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1'))
