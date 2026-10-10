"""Actual supply, fashion transactions and preview invalidation against the save service."""
from pathlib import Path
import importlib.util, tempfile, subprocess, os, sys
root=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('cv',root/'Tools/cloud-validation.py');cv=importlib.util.module_from_spec(spec);spec.loader.exec_module(cv)
program=r'''using System;using System.IO;using Emberfall;
class ScriptedRoll:Random{int call,a,b;public ScriptedRoll(int a,int b){this.a=a;this.b=b;}public override int Next(int max){if(max==100)return call++==0?0:call==2?a:b;return 0;}public override int Next(int min,int max){return min;}}
class Program{static int n;static void C(bool b,string m){n++;if(!b)throw new Exception(m);}static void Main(){
var method=typeof(ProgressionService).GetMethod("RollRandomEquipmentAffixes",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
int[] distribution=new int[4];for(int a=0;a<100;a++)for(int b=0;b<100;b++){var gear=new ItemData{rarity=Rarity.Legendary};method.Invoke(null,new object[]{gear,3,new ScriptedRoll(a,b)});int amount=(gear.criticalChance>0?1:0)+(gear.criticalDamageBonus>0?1:0)+(gear.attackPercent>0?1:0);distribution[amount]++;}
C(distribution[0]==0&&distribution[1]==5500&&distribution[2]==3375&&distribution[3]==1125,"legendary exact count distribution 55 / 33.75 / 11.25");
var p=new ProgressionService(Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N")));C(p.CreateNewSlot(HeroClass.Arcanist),"new save");
p.Profile.gold=4999;C(p.PrepareAffixReforgePurchase(true)==null,"insufficient gold");p.Profile.gold=20000;
C(p.PrepareAffixReforgePurchase(false)==null,"merchant required");var q=p.PrepareAffixReforgePurchase(true);
Directory.CreateDirectory(p.SaveFilePath+".tmp");C(!p.BuyAtMerchant(q,true)&&p.Profile.gold==20000&&p.Profile.affixReforgeStones==0,"failed purchase rollback");Directory.Delete(p.SaveFilePath+".tmp");
C(p.BuyAtMerchant(q,true)&&p.Profile.gold==15000&&p.Profile.affixReforgeStones==1,"price 5000 and one stone");C(!p.BuyAtMerchant(q,true),"stale quote");
p.Profile.affixReforgeStones=999999;C(p.PrepareAffixReforgePurchase(true)==null,"capacity");p.Profile.affixReforgeStones=200;
var item=p.Profile.inventory[0];item.rarity=Rarity.Legendary;int attack=item.attack,defense=item.defense,hp=item.health,rank=item.upgradeLevel;string id=item.id;item.locked=true;
C(!p.ReforgeAffixes(id,false)&&p.Profile.affixReforgeStones==200,"smith required");
int old=p.Profile.affixReforgeCount;float crit=item.criticalChance;
Directory.CreateDirectory(p.SaveFilePath+".tmp");C(!p.ReforgeAffixes(id,true)&&p.Profile.affixReforgeStones==200&&p.Profile.affixReforgeCount==old&&p.Profile.inventory[0].criticalChance==crit,"failed reforge rollback");Directory.Delete(p.SaveFilePath+".tmp");
int masks=0;for(int i=0;i<100;i++){
C(p.ReforgeAffixes(id,true),"reforge");item=p.Profile.inventory.Find(x=>x.id==id);
int count=(item.criticalChance>0?1:0)+(item.criticalDamageBonus>0?1:0)+(item.attackPercent>0?1:0);masks|=1<<count;
C(count>=1&&count<=3,"distinct affix count");C(item.criticalChance<=.08f&&item.criticalDamageBonus<=.20f&&item.attackPercent<=.12f,"quality ranges");
C(item.attack==attack&&item.defense==defense&&item.health==hp&&item.upgradeLevel==rank&&item.locked,"base rank lock preserved");}
C(masks==14&&p.Profile.affixReforgeStones==100,"all count outcomes and consumption");
var reload=new ProgressionService(p.SaveDirectory);C(reload.LoadSlot(p.CurrentSlotId)&&reload.Profile.affixReforgeStones==100&&reload.Profile.affixReforgeCount==old+100,"reload");
item=p.Profile.inventory.Find(x=>x.id==id);item.rarity=Rarity.Common;C(!p.ReforgeAffixes(id,true)&&p.Profile.affixReforgeStones==100,"common rejection");
p.Profile.pendingChestRulesRevision=3;p.Profile.pendingChestMode=2;
for(int tier=1;tier<=10;tier++){int hits=0;for(int r=0;r<100;r++){p.Profile.pendingChestTier=tier;var chest=ProgressionService.BuildSingleChestRoll(p.Profile,r,0,0,false,"reforge-test");if(chest.primaryKind==7){hits++;C(chest.primaryCount==1&&chest.primaryRarity==Rarity.Legendary,"legendary stone");}}C(hits==5,"five percent stone");}
// Quantity purchases commit once, protect limits and reject stale confirmations.
p.Profile.gold=100000;p.Profile.affixReforgeStones=0;p.Profile.potions=0;p.Profile.refinementStones=0;
C(p.PrepareAffixReforgePurchase(true,0)==null&&p.PrepareAffixReforgePurchase(true,int.MaxValue)==null,"invalid stone quantity");
q=p.PrepareAffixReforgePurchase(true,3);C(q!=null&&q.Quantity==3,"three reroll stones quote");
Directory.CreateDirectory(p.SaveFilePath+".tmp");C(!p.BuyAtMerchant(q,true)&&p.Profile.gold==100000&&p.Profile.affixReforgeStones==0,"batch stone write rollback");Directory.Delete(p.SaveFilePath+".tmp");
C(p.BuyAtMerchant(q,true)&&p.Profile.gold==85000&&p.Profile.affixReforgeStones==3,"batch stone cost and quantity");C(!p.BuyAtMerchant(q,true),"batch cannot replay");
q=p.PrepareRefinementPurchase(true,100);C(q!=null&&p.BuyAtMerchant(q,true)&&p.Profile.refinementStones==100&&p.Profile.gold==80000,"material quantities beyond fixed five and 99");
q=p.PrepareMerchantPurchase(EquipmentMechanic.None,true,Rarity.Common,7);C(q!=null,"seven potions quote");int money=p.Profile.gold;
Directory.CreateDirectory(p.SaveFilePath+".tmp");C(!p.BuyAtMerchant(q,true)&&p.Profile.gold==money&&p.Profile.potions==0,"batch potion rollback");Directory.Delete(p.SaveFilePath+".tmp");
C(p.BuyAtMerchant(q,true)&&p.Profile.potions==7&&p.Profile.gold==money-7*ProgressionService.PotionPrice,"batch potion exact total");
C(p.PrepareMerchantPurchase(EquipmentMechanic.None,true,Rarity.Common,93)==null,"potion capacity");
C(p.PrepareMerchantPurchase(BuildCatalog.GemsFor(p.Profile.heroClass)[0],true,Rarity.Epic,2)==null,"unique gem quantity");
q=p.PrepareAffixReforgePurchase(true,2);p.Profile.gold--;C(!p.BuyAtMerchant(q,true)&&p.Profile.affixReforgeStones==3,"changed balance rejects quantity quote");
item=p.Profile.inventory.Find(x=>x.id==id);item.rarity=Rarity.Legendary;
var rq=p.PrepareAffixReforge(id,true);C(rq!=null&&p.Profile.affixReforgeStones==3,"preview and cancellation spend nothing");
item.criticalChance+=.001f;C(!p.ApplyAffixReforge(rq,true)&&p.Profile.affixReforgeStones==3,"modified item invalidates confirmation");
rq=p.PrepareAffixReforge(id,true);Directory.CreateDirectory(p.SaveFilePath+".tmp");C(!p.ApplyAffixReforge(rq,true)&&p.Profile.affixReforgeStones==3,"confirmed reroll rollback");Directory.Delete(p.SaveFilePath+".tmp");
C(p.ApplyAffixReforge(rq,true)&&p.Profile.affixReforgeStones==2&&!p.ApplyAffixReforge(rq,true),"confirmed reroll once only");
// Enumerate every fresh roll in every source; preview predicates match reachable rewards.
foreach(bool chapter in new[]{false,true})for(int mode=chapter?0:-1;mode<=(chapter?2:3);mode++){
p.Profile.pendingChestChapterSource=chapter;p.Profile.pendingChestMode=mode;p.Profile.pendingChestGemSource=chapter&&mode==2;p.Profile.legendaryEquipmentMisses=0;
int fashions=0,reforges=0,legends=0,gems=0;
for(int roll=0;roll<100;roll++){
var chest=ProgressionService.BuildSingleChestRoll(p.Profile,roll,0,0,false,"split-pool");
if(chest.primaryKind==2)fashions++;if(chest.primaryKind==7)reforges++;if(chest.primaryKind==4)gems++;if(chest.primaryKind==1&&chest.primaryRarity==Rarity.Legendary)legends++;
}
C(fashions==(AdventureRewardRules.ChestFashion(mode,chapter)?15:0),"fashion confined to expedition");
C(reforges==(AdventureRewardRules.ChestAffixReforge(mode,chapter)?5:0),"reroll confined to arena and Redrock");
C(legends==4&&gems==(chapter&&mode==2?35:0),"legendary probability and gem source preserved");
p.Profile.legendaryEquipmentMisses=29;for(int roll=0;roll<100;roll++){var chest=ProgressionService.BuildSingleChestRoll(p.Profile,roll,0,0,false,"pity");C(chest.primaryKind==1&&chest.primaryRarity==Rarity.Legendary,"all sources keep 30 chest pity");}
}
Directory.Delete(p.SaveDirectory,true);Console.WriteLine("PASS "+n+" affix reforge assertions");}}
'''
with tempfile.TemporaryDirectory(prefix='fashion-refinement-') as tmp:
 p=Path(tmp);(p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 names=['SkillRuntime','GameTypes','ProgressionService','ProgressionService.Attachments','ProgressionService.AutomaticGrowth','ProgressionService.Trading','CombatBalance','SkillDamageBudgets','HubTravelRules','MasteryCoreRuntime','TierRewardRules','TierRewardBand','ProgressionGoalState','ChapterProgression','ProgressionService.Chapter','ProgressionService.Reforge','ReforgeQuote','RoomTactics','CombatImpactBatch','SafeSaveFlow']
 sources=[root/('Assets/Scripts/Core/'+name+'.cs') for name in names]+[root/'Tests/ProgressionTests.cs',root/'Assets/Scripts/UI/CollectionPreviewState.cs']
 project=cv.write_project(p/'project',sources,program)
 subprocess.run([sys.argv[1] if len(sys.argv)>1 else '/Applications/Unity/Hub/Editor/6000.6.4f1/Unity.app/Contents/Resources/Scripting/DotNetSdk/dotnet','run','--project',str(project),'--',str(p/'saves')],check=True,env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1'))
