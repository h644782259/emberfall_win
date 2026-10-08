"""Production chest transactions and mounted-gem presentation, with managed save boundaries."""
from pathlib import Path
import importlib.util,tempfile,subprocess,os,sys
root=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('cv',root/'Tools/cloud-validation.py');cv=importlib.util.module_from_spec(spec);spec.loader.exec_module(cv)
def member(path,key):
 s=(root/path).read_text();a=s.index(key);b=s.index('{',a)+1;depth=1
 while depth:depth+=(s[b]=='{')-(s[b]=='}');b+=1
 return s[a:b]
ready=member('Assets/Scripts/Core/GameSession.DungeonRewards.cs','public bool DungeonResultsReady')
ui=member('Assets/Scripts/UI/GameUI.Rewards.cs','private void EnsurePendingChestPanel()')
fixture=r'''
using System;using System.IO;using Emberfall;
class Session {public bool HasStarted=true,InDungeon=true,DungeonCleared=true,DungeonRewardPending,Paused,IsDead;public ProgressionService Progression;public void SetUIBlocking(bool b){} READY}
class Flow {
public enum Panel{None,Chests,Summary,Other}public Panel panel;public Session session;public string chestQualificationId,mobileChestError;public int resets;struct V{public static V zero=>new V();}V mobileChestScroll,mobileChestArtScroll;void ResetChestReveal(){resets++;chestQualificationId=session.Progression.Profile.pendingChestQualificationId;}ENSURE
public void Tick()=>EnsurePendingChestPanel();
}
class Program{
sealed class ZeroRoll:Random{public override int Next(int max)=>0;}
static void C(bool v,string why){if(!v)throw new Exception(why);}
static void Main(string[] args){
var p=new ProgressionService(args[0]);C(p.CreateNewSlot(HeroClass.Arcanist),"create");var s=new Session{Progression=p};var ui=new Flow{session=s};string previous=null;
for(int tier=1;tier<=4;tier++){
s.DungeonRewardPending=true;C(!s.DungeonResultsReady,"unsettled cannot show result");string id=Guid.NewGuid().ToString("N");C(p.TryCompleteDungeonRun(id,tier,150,0,true),p.LastError);s.DungeonRewardPending=false;
C(!s.DungeonResultsReady,"unopened chest precedes result");ui.panel=tier==1?Flow.Panel.Summary:Flow.Panel.Chests;ui.Tick();C(ui.panel==Flow.Panel.Chests&&ui.resets==tier,"new qualification resets retained chest or preempts recap");ui.Tick();C(ui.resets==tier,"same qualification does not reset animation");
Directory.CreateDirectory(p.SaveFilePath+".tmp");C(p.OpenDungeonChest()==null&&p.Profile.pendingFashionChest,"failed save retains eligibility");Directory.Delete(p.SaveFilePath+".tmp");C(p.OpenDungeonChest()!=null,p.LastError);C(p.LastChestReward.Id!=previous,"fresh receipt each stage");previous=p.LastChestReward.Id;
C(!s.DungeonResultsReady,"unacknowledged reward precedes result");int gold=p.Profile.gold;C(p.OpenDungeonChest()==null&&p.Profile.gold==gold,"repeat cannot grant twice");C(p.AcknowledgeChestReward(),p.LastError);C(s.DungeonResultsReady,"result available after receipt acknowledgement");
}
var m=EquipmentMechanic.FrostEcho;var item=new ItemData{slot=BuildCatalog.MechanicSlot(m),mechanic=m};p.Profile.attachments.Add(new MechanicAttachment{id="gem",mechanic=m,mounted=true});p.Save();
C(EquipmentComparisonPresentation.Description(item,p).Contains(BuildCatalog.GemName(m)),"mounted gem displayed");C(p.SetAttachmentMounted(m,false,true),p.LastError);C(!p.HasMechanic(m),"unmounted disables combat effect");C(EquipmentComparisonPresentation.Description(item,p).Contains("未镶嵌"),"unmounted removes old item mechanism text");C(p.LoadSlot(p.CurrentSlotId),"reload");C(EquipmentComparisonPresentation.Description(item,p).Contains("未镶嵌"),"unmounted survives reload");

for(int level=1;level<=100;level++){int expected=level<10?1:level/10*10;C(ProgressionService.EquipmentGenerationLevel(level)==expected,"ten-level sets");C(p.RollLoot(level,false).level==expected,"actual drops use set level");}
for(int roll=0;roll<100;roll++)C(ProgressionService.RollFashionRarity(roll)==(roll<40?(Rarity?)Rarity.Legendary:null),"fashion chance remains 40 percent and only legendary");
p.Profile.fashions.Add(new FashionData{id="fashion-1-0",slot=FashionSlot.Weapon,rarity=Rarity.Common});p.Profile.fashions.Add(new FashionData{id="fashion-1-1",slot=FashionSlot.Weapon,rarity=Rarity.Rare,appearanceTier=0});p.Profile.fashionQualityRevision=0;p.Profile.weaponFashionId="fashion-1-0";p.Save();C(p.LoadSlot(p.CurrentSlotId),"migrate fashion");
C(p.EquippedFashion(FashionSlot.Weapon).rarity==Rarity.Legendary&&p.Profile.weaponFashionId=="fashion-1-0","quality upgrade retains equipped identity");C(p.Profile.fashions.Exists(f=>f.id=="fashion-1-1"&&f.rarity==Rarity.Legendary&&f.AppearanceRarity==Rarity.Rare),"migration preserves collected appearances");p.Save();C(p.LoadSlot(p.CurrentSlotId),"migration persists");C(p.EquippedFashion(FashionSlot.Weapon).AppearanceRarity==Rarity.Common,"appearance survives second load");
p.Profile.level=11;p.Profile.gold=999999;p.Profile.slotUpgradeRanks=new int[3];var equipped=p.Equipped(ItemSlot.Weapon);for(int rank=0;rank<11;rank++)C(p.UpgradeAtSmith(p.PrepareSmithUpgrade(ItemSlot.Weapon,true),true),p.LastError);C(p.SlotUpgradeRank(ItemSlot.Weapon)==11&&p.PrepareSmithUpgrade(ItemSlot.Weapon,true)==null,"rank exceeds old 10 cap but stops at player level");C(!p.Upgrade(equipped.id),"direct upgrade cannot bypass level cap");p.Profile.level=12;C(p.UpgradeAtSmith(p.PrepareSmithUpgrade(ItemSlot.Weapon,true),true),"level up unlocks next rank");C(CombatBalance.UpgradeValue(100,100)==600,"full level cap uses full linear growth");
p.Profile.inventory.RemoveAll(i=>i.id!=p.Profile.weaponId&&i.id!=p.Profile.armorId&&i.id!=p.Profile.relicId);p.Profile.slotUpgradeRanks=new int[3];equipped=p.Equipped(ItemSlot.Weapon);equipped.attack=100;equipped.baseAttack=100;equipped.upgradeBaseInitialized=true;equipped.upgradeLevel=0;
p.Profile.inventory.Add(new ItemData{id="weak",slot=ItemSlot.Weapon,level=1,rarity=Rarity.Common,attack=1});p.Profile.inventory.Add(new ItemData{id="locked",slot=ItemSlot.Weapon,level=1,rarity=Rarity.Common,attack=1,locked=true});p.Profile.inventory.Add(new ItemData{id="strong",slot=ItemSlot.Weapon,level=1,rarity=Rarity.Common,attack=9999});p.Profile.inventory.Add(new ItemData{id="planned",slot=ItemSlot.Weapon,level=1,rarity=Rarity.Common,attack=1});p.Profile.buildPresets=new[]{new BuildPreset{populated=true,heroClass=p.Profile.heroClass,weaponId="planned"},new BuildPreset()};
var quote=p.PrepareMerchantBulkSale(true);C(quote!=null&&quote.Count==1,"bulk sale filters worn locked improved and planned gear");int beforeGold=p.Profile.gold;Directory.CreateDirectory(p.SaveFilePath+".tmp");C(!p.SellAtMerchant(quote,true)&&p.Profile.inventory.Find(i=>i.id=="weak")!=null&&p.Profile.gold==beforeGold,"failed bulk save removes nothing");Directory.Delete(p.SaveFilePath+".tmp");C(p.SellAtMerchant(quote,true)&&p.Profile.inventory.Find(i=>i.id=="weak")==null&&p.Profile.gold==beforeGold+quote.Gold,"bulk sale commits atomically");C(!p.SellAtMerchant(quote,true),"stale quote cannot sell twice");

p.Profile.fashions.Add(new FashionData{id="fashion-0-3",slot=FashionSlot.Wings,rarity=Rarity.Legendary});if(!p.Profile.fashions.Exists(f=>f.id=="fashion-1-3"))p.Profile.fashions.Add(new FashionData{id="fashion-1-3",slot=FashionSlot.Weapon,rarity=Rarity.Legendary});typeof(ProgressionService).GetField("random",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(p,new ZeroRoll());C(p.PrepareDungeonChest(),"prepare duplicate");C(p.OpenDungeonChest()!=null,p.LastError);C(p.LastChestReward.Duplicate&&p.LastChestReward.duplicateGold==800&&p.LastChestReward.duplicateThreads==8&&p.LastChestReward.Rarity==Rarity.Legendary,"legendary duplicate currency");C(p.AcknowledgeChestReward(),"ack duplicate");C(p.LoadSlot(p.CurrentSlotId),"reload legendary receipt");
Console.WriteLine("PASS four consecutive stage rewards, save failure/retry, no duplicate grants, chest-before-result ordering, UI qualification reset mounted gem detail/reload, legendary fashion migration, level-bound upgrades and atomic bulk sale");}}
'''.replace('READY',ready).replace('ENSURE',ui)
with tempfile.TemporaryDirectory(prefix='dungeon-reward-sequence-') as d:
 p=Path(d);(p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 names=['SkillRuntime','GameTypes','ProgressionService','ProgressionService.Attachments','ProgressionService.AutomaticGrowth','CombatBalance','SkillDamageBudgets','HubTravelRules','MasteryCoreRuntime','TierRewardRules','TierRewardBand','ProgressionGoalState','ChapterProgression','ProgressionService.Chapter','ProgressionService.Reforge','ReforgeQuote','RoomTactics','CombatImpactBatch','SafeSaveFlow']
 sources=[root/('Assets/Scripts/Core/'+n+'.cs') for n in names]+[root/'Tests/ProgressionTests.cs',root/'Assets/Scripts/UI/EquipmentComparisonPresentation.cs']
 fixture=fixture.replace('Vector2.zero','V.zero')
 project=cv.write_project(p/'project',sources,fixture)
 subprocess.run([sys.argv[1],'run','--project',str(project),'--',str(p/'saves')],check=True,env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1'))
