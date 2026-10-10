"""Actual supply, fashion transactions and preview invalidation against the save service."""
from pathlib import Path
import importlib.util, tempfile, subprocess, os, sys
root=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('cv',root/'Tools/cloud-validation.py');cv=importlib.util.module_from_spec(spec);spec.loader.exec_module(cv)
program=r'''using System;using System.IO;using Emberfall;
class Program{
static int n;static void C(bool v,string s){n++;if(!v)throw new Exception(s);}
static ProgressionService P(string dir){var p=new ProgressionService(dir);C(p.CreateNewSlot(HeroClass.Arcanist),"create slot");return p;}
static void Main(string[] args){
var p=P(Path.Combine(args[0],"shop"));p.Profile.gold=1000;p.Save();
C(p.PrepareRefinementPurchase(false)==null&&p.PrepareRefinementPurchase(true,0)==null&&p.PrepareRefinementPurchase(true,100)==null,"purchase gates and quantities");
var q=p.PrepareRefinementPurchase(true,5);C(q!=null,"prepare five");int g=p.Profile.gold,s=p.Profile.refinementStones;
Directory.CreateDirectory(p.SaveFilePath+".tmp");C(!p.BuyAtMerchant(q,true)&&p.Profile.gold==g&&p.Profile.refinementStones==s,"purchase write failure rolls back");Directory.Delete(p.SaveFilePath+".tmp");
C(p.BuyAtMerchant(q,true)&&p.Profile.gold==g-250&&p.Profile.refinementStones==s+5,"five stones cost 250");C(!p.BuyAtMerchant(q,true)&&p.Profile.gold==g-250,"quote cannot buy twice");
q=p.PrepareRefinementPurchase(true);p.Profile.refinementStones++;C(!p.BuyAtMerchant(q,true),"stone quote expires on balance change");
p.Profile.gold=49;C(p.PrepareRefinementPurchase(true)==null,"insufficient gold");p.Profile.gold=1000;p.Profile.refinementStones=999999;C(p.PrepareRefinementPurchase(true)==null,"stone cap");
int[] tiers={1,5,10,20,40,100},counts={2,3,4,5,6,6};for(int i=0;i<tiers.Length;i++)C(ProgressionService.DungeonRefinementStones(tiers[i])==counts[i],"tier supply");
var d=P(Path.Combine(args[0],"dungeon"));string id=Guid.NewGuid().ToString("N");s=d.Profile.refinementStones;
Directory.CreateDirectory(d.SaveFilePath+".tmp");C(!d.TryCompleteDungeonRun(id,10,150,0)&&d.Profile.refinementStones==s,"clear failure rolls back stones");Directory.Delete(d.SaveFilePath+".tmp");
C(d.TryCompleteDungeonRun(id,10,150,0)&&d.Profile.refinementStones==s+4,"ordinary dungeon fixed drop");C(d.GetRewardPresentation(id).RefinementStones==4,"clear receipt includes stones");C(d.TryCompleteDungeonRun(id,10,150,0)&&d.Profile.refinementStones==s+4,"clear receipt deduplication");C(d.LoadSlot(d.CurrentSlotId)&&d.Profile.refinementStones==s+4,"clear persists");
for(int mode=-1;mode<=3;mode++){var m=P(Path.Combine(args[0],"mode"+mode));id=Guid.NewGuid().ToString("N");C(m.TryGrantModeReward(id,0,0,0,1,mode)&&m.Profile.refinementStones==2,"every dungeon mode fixed drop");C(m.TryGrantModeReward(id,0,0,0,1,mode)&&m.Profile.refinementStones==2,"mode deduplicates");}
var f=P(Path.Combine(args[0],"fashion"));f.Profile.fashions.Add(new FashionData{id="fashion-0-0",slot=FashionSlot.Wings,rarity=Rarity.Legendary,appearanceTier=0});f.Profile.fashions.Add(new FashionData{id="fashion-1-3",slot=FashionSlot.Weapon,rarity=Rarity.Legendary,appearanceTier=3});f.Profile.fashionThreads=100;f.Save();
string wing="fashion-0-0",weapon="fashion-1-3";C(f.PrepareFashionService(wing,false,false)==null,"fashion requires smith");
var before=f.GetStats();var old=f.Profile.fashions.Find(x=>x.id==wing);var appearance=new CollectionPreviewAppearance(f.Profile.heroClass,null,null,null,old,null);var preview=f.PreviewFashionUpgrade(wing);
C(preview.upgradeRank==1&&preview.VisualRarity==Rarity.Rare&&old.upgradeRank==0,"preview changes visual tier without mutation");var fq=f.PrepareFashionService(wing,false,true);int threads=f.Profile.fashionThreads;
Directory.CreateDirectory(f.SaveFilePath+".tmp");C(!f.ApplyFashionService(fq,true)&&f.Profile.fashionThreads==threads&&f.Profile.fashions.Find(x=>x.id==wing).upgradeRank==0,"fashion upgrade write rollback");Directory.Delete(f.SaveFilePath+".tmp");
for(int rank=1;rank<=3;rank++){fq=f.PrepareFashionService(wing,false,true);C(fq!=null&&fq.Threads==6*rank&&f.ApplyFashionService(fq,true),"wing upgrades to each stage");C(!f.ApplyFashionService(fq,true),"stale fashion quote rejected");}
C(f.Profile.fashionThreads==64&&f.Profile.fashions.Find(x=>x.id==wing).upgradeRank==3,"total investment 36 threads");var upgraded=f.Profile.fashions.Find(x=>x.id==wing);C(!appearance.Equals(new CollectionPreviewAppearance(f.Profile.heroClass,null,null,null,upgraded,null)),"appearance snapshot detects rank");
var after=f.GetStats();C(after.MaxHealth>before.MaxHealth&&after.Armor>before.Armor&&after.MoveSpeed>before.MoveSpeed&&after.DamageReduction>before.DamageReduction,"wing live stats and new mitigation affix");C(ProgressionService.FashionBonus(upgraded).Contains("受到伤害减免"),"wing added affix description");C(f.PrepareFashionService(wing,false,true)==null,"rank cap");
for(int rank=1;rank<=3;rank++){fq=f.PrepareFashionService(weapon,false,true);C(f.ApplyFashionService(fq,true),"weapon upgrades");}
C(f.GetStats().Damage>before.Damage&&f.GetStats().CritDamageBonus>before.CritDamageBonus,"weapon live damage and new critical affix");C(f.LoadSlot(f.CurrentSlotId)&&f.Profile.fashions.Find(x=>x.id==wing).upgradeRank==3,"fashion rank persists reload");
C(f.EquipFashion(wing)&&f.PrepareFashionService(wing,true,true)==null,"worn fashion protected");C(f.UnequipFashion(FashionSlot.Wings),"unequip for dismantle");fq=f.PrepareFashionService(wing,true,true);C(fq.Threads==38,"base dismantle value plus full upgrade investment");threads=f.Profile.fashionThreads;
Directory.CreateDirectory(f.SaveFilePath+".tmp");C(!f.ApplyFashionService(fq,true)&&f.Profile.fashions.Exists(x=>x.id==wing)&&f.Profile.fashionThreads==threads,"dismantle write rollback");Directory.Delete(f.SaveFilePath+".tmp");
C(f.ApplyFashionService(fq,true)&&!f.Profile.fashions.Exists(x=>x.id==wing)&&f.Profile.fashionThreads==threads+38,"dismantle atomic receipt");C(!f.ApplyFashionService(fq,true),"dismantle cannot repeat");C(f.LoadSlot(f.CurrentSlotId)&&!f.Profile.fashions.Exists(x=>x.id==wing),"dismantle persists");
Console.WriteLine("PASS "+n+" refinement supply and fashion growth assertions");}}
'''
with tempfile.TemporaryDirectory(prefix='fashion-refinement-') as tmp:
 p=Path(tmp);(p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 names=['SkillRuntime','GameTypes','ProgressionService','ProgressionService.Attachments','ProgressionService.AutomaticGrowth','ProgressionService.Trading','CombatBalance','SkillDamageBudgets','HubTravelRules','MasteryCoreRuntime','TierRewardRules','TierRewardBand','ProgressionGoalState','ChapterProgression','ProgressionService.Chapter','ProgressionService.Reforge','ReforgeQuote','RoomTactics','CombatImpactBatch','SafeSaveFlow']
 sources=[root/('Assets/Scripts/Core/'+name+'.cs') for name in names]+[root/'Tests/ProgressionTests.cs',root/'Assets/Scripts/UI/CollectionPreviewState.cs']
 project=cv.write_project(p/'project',sources,program)
 subprocess.run([sys.argv[1] if len(sys.argv)>1 else '/Applications/Unity/Hub/Editor/6000.6.4f1/Unity.app/Contents/Resources/Scripting/DotNetSdk/dotnet','run','--project',str(project),'--',str(p/'saves')],check=True,env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1'))
