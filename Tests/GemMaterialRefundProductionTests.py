"""Exercise level and dungeon milestone receipts against the actual save service."""
from pathlib import Path
import importlib.util,os,subprocess,tempfile
root=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('cv',root/'Tools/cloud-validation.py');cv=importlib.util.module_from_spec(spec);spec.loader.exec_module(cv)
program=r'''using System;using System.IO;using System.Linq;using Emberfall;
class Program{static int n;static void C(bool b,string s){n++;if(!b)throw new Exception(s);}static void Main(string[] args){
 var trade=new ProgressionService(Path.Combine(args[0],"trades"));trade.NewGame(HeroClass.Arcanist);trade.Profile.clearedRuns=1;trade.Profile.pendingFirstClearReward=true;trade.Profile.attachments.Add(new MechanicAttachment{id="ownedlegend",mechanic=EquipmentMechanic.CinderTrail,rarity=Rarity.Legendary,upgradeRank=6,ascensionRank=2,mounted=false});trade.Save();
 var firstQuote=trade.PrepareMerchantPurchase(EquipmentMechanic.CinderTrail,true,Rarity.Epic);C(firstQuote!=null,"owned gem no longer deadlocks first clear");int shardsBefore=trade.Profile.mechanicMaterials;
 Directory.CreateDirectory(trade.SaveFilePath+".tmp");C(!trade.BuyAtMerchant(firstQuote,true)&&!trade.Profile.firstClearRewardClaimed&&trade.Profile.mechanicMaterials==shardsBefore,"first claim rolls back on save failure");Directory.Delete(trade.SaveFilePath+".tmp");
 C(trade.BuyAtMerchant(firstQuote,true)&&trade.Profile.firstClearRewardClaimed&&trade.Profile.mechanicMaterials==shardsBefore+3,"duplicate first claim converts exactly once");
 C(trade.Attachment(EquipmentMechanic.CinderTrail).rarity==Rarity.Legendary&&trade.Attachment(EquipmentMechanic.CinderTrail).upgradeRank==6,"free duplicate never downgrades owned gem");
 C(!trade.BuyAtMerchant(firstQuote,true)&&trade.Profile.mechanicMaterials==shardsBefore+3,"stale first-clear quote cannot grant twice");
 var firstAttribute=new ProgressionService(Path.Combine(args[0],"first-attribute"));firstAttribute.NewGame(HeroClass.Ranger);firstAttribute.Profile.clearedRuns=1;firstAttribute.Profile.pendingFirstClearReward=true;firstAttribute.Save();
 var attributeQuote=firstAttribute.PrepareMerchantPurchase(EquipmentMechanic.WeaponPower,true,Rarity.Epic);C(attributeQuote!=null&&firstAttribute.BuyAtMerchant(attributeQuote,true)&&firstAttribute.Attachment(EquipmentMechanic.WeaponPower).rarity==Rarity.Epic&&firstAttribute.Profile.firstClearRewardClaimed,"attribute gem can be selected as first-clear reward");
 int zeroGold=firstAttribute.Profile.gold,zeroMaterials=firstAttribute.Profile.mechanicMaterials;
 C(firstAttribute.GemSellValue(EquipmentMechanic.WeaponPower)==0&&firstAttribute.SellGem(EquipmentMechanic.WeaponPower,true)&&firstAttribute.Profile.gold==zeroGold&&firstAttribute.Profile.mechanicMaterials==zeroMaterials,"uninvested gem returns no currency");
 trade.Attachment(EquipmentMechanic.CinderTrail).mounted=true;C(!trade.SellGem(EquipmentMechanic.CinderTrail,true),"mounted gems protected from sale");trade.Attachment(EquipmentMechanic.CinderTrail).mounted=false;
 var legacy=trade.Equipped(ItemSlot.Weapon);legacy.mechanic=EquipmentMechanic.CinderTrail;trade.Save();trade.Attachment(EquipmentMechanic.CinderTrail).mounted=false;trade.Save();
 C(trade.GemSellValue(EquipmentMechanic.CinderTrail)==trade.Attachment(EquipmentMechanic.CinderTrail).upgradeRank*ProgressionService.AttachmentUpgradeCost+Math.Max(0,trade.Attachment(EquipmentMechanic.CinderTrail).ascensionRank)*ProgressionService.AscensionCost,"refund equals rank and ascension investment");
 int uncappedMaterials=trade.Profile.mechanicMaterials;trade.Profile.mechanicMaterials=999999;C(!trade.SellGem(EquipmentMechanic.CinderTrail,true)&&trade.Attachment(EquipmentMechanic.CinderTrail)!=null,"material cap cannot destroy gem");trade.Profile.mechanicMaterials=uncappedMaterials;
 int saleMaterials=trade.Profile.mechanicMaterials;int saleGold=trade.Profile.gold,saleValue=trade.GemSellValue(EquipmentMechanic.CinderTrail);Directory.CreateDirectory(trade.SaveFilePath+".tmp");
 C(!trade.SellGem(EquipmentMechanic.CinderTrail,true)&&trade.Attachment(EquipmentMechanic.CinderTrail)!=null&&trade.Profile.gold==saleGold&&trade.Profile.mechanicMaterials==saleMaterials,"failed sale keeps gem and balance");Directory.Delete(trade.SaveFilePath+".tmp");
 C(trade.SellGem(EquipmentMechanic.CinderTrail,true)&&trade.Attachment(EquipmentMechanic.CinderTrail)==null&&trade.Profile.gold==saleGold&&trade.Profile.mechanicMaterials==saleMaterials+saleValue,"gem sale atomic receipt");
 C(!trade.SellGem(EquipmentMechanic.CinderTrail,true)&&trade.Profile.gold==saleGold&&trade.Profile.mechanicMaterials==saleMaterials+saleValue,"gem cannot sell twice");
 var soldReload=new ProgressionService(Path.Combine(args[0],"trades"));C(soldReload.LoadSlot(trade.CurrentSlotId)&&soldReload.Attachment(EquipmentMechanic.CinderTrail)==null,"legacy equipment cannot resurrect sold gem after reload");

 Console.WriteLine("PASS "+n+" gem material refund assertions");}}
'''
with tempfile.TemporaryDirectory(prefix='achievement-milestones-') as tmp:
 p=Path(tmp);names=['SkillRuntime','GameTypes','ProgressionService','ProgressionService.Attachments','ProgressionService.AutomaticGrowth','ProgressionService.Trading','CombatBalance','SkillDamageBudgets','HubTravelRules','MasteryCoreRuntime','TierRewardRules','TierRewardBand','ProgressionGoalState','ChapterProgression','ProgressionService.Chapter','ProgressionService.Reforge','ReforgeQuote','RoomTactics','CombatImpactBatch','SafeSaveFlow']
 sources=[root/('Assets/Scripts/Core/'+name+'.cs') for name in names]+[root/'Tests/ProgressionTests.cs'];project=cv.write_project(p/'project',sources,program)
 sdk='/Applications/Unity/Hub/Editor/6000.6.4f1/Unity.app/Contents/Resources/Scripting/DotNetSdk/dotnet'
 subprocess.run([sdk,'run','--project',str(project),'--',str(p/'saves')],check=True,env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1'))
