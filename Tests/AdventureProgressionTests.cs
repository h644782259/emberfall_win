using System;
using System.IO;
using Emberfall;
using UnityEngine;
public static class AdventureProgressionTests
{
    static int checks;
    static void Check(bool ok,string reason){checks++;if(!ok)throw new Exception(reason);}
    static ProgressionService Fresh(string root,HeroClass hero=HeroClass.Arcanist)
    {var p=new ProgressionService(Path.Combine(root,Guid.NewGuid().ToString("N")));Check(p.CreateNewSlot(hero),"new fixture");return p;}
    static string State(ProgressionService p){return JsonUtility.ToJson(p.Profile,true);}
    public static string Run(string root)
    {
        checks=0;
        for(int mode=0;mode<5;mode++)
        {
            var p=Fresh(root);var id=Guid.NewGuid().ToString("N");string before=State(p);
            Func<bool> settle=()=>mode==0?p.TryCompleteDungeonRun(id,5,100,50):p.TryGrantModeReward(id,100,50,4,5);
            Directory.CreateDirectory(p.SaveFilePath+".tmp");Check(!settle()&&State(p)==before,"failed settlement publishes no tier, material or first core");Directory.Delete(p.SaveFilePath+".tmp");
            Check(settle()&&p.HighestAdventureTier==5&&p.HighestUnlockedAdventureTier==6&&p.Profile.pendingFirstClearReward,"every completed mode advances shared milestones");
            Check(mode==0?p.Profile.pendingFashionChest:p.Profile.clearedRuns==0&&!p.Profile.pendingFashionChest&&p.Profile.bestFloor==0,"mode rewards retain independent ordinary counters/chest");
            string settled=State(p);Check(settle()&&State(p)==settled,"duplicate settlement is idempotent");
            Check(p.LoadSlot(p.CurrentSlotId)&&p.HighestAdventureTier==5&&p.Profile.pendingFirstClearReward,"tier and firstcore survive restart");
            Check(settle()&&State(p)==settled,"receipt survives restart");
            Check(p.ClaimFirstClearReward(EquipmentMechanic.FrostEcho)&&!p.Profile.pendingFirstClearReward,"shared firstcore claim");
            Check(!p.ClaimFirstClearReward(EquipmentMechanic.FrostEcho),"firstcore is single claim");
            p.Profile.mechanicMaterials=100;p.Save();
            Check(p.AscendAttachment(EquipmentMechanic.FrostEcho,true),"any mode fifth tier opens attachment ascension");
        }
        {
            var p=Fresh(root);p.Profile.bestFloor=7;p.Profile.highestAdventureTier=0;p.Save();Check(p.HighestAdventureTier==7&&p.LoadSlot(p.CurrentSlotId)&&p.Profile.highestAdventureTier==7,"legacy ordinary tier migrates");
            var q=Fresh(root);Check(q.TryGrantModeReward(Guid.NewGuid().ToString("N"),10,10,1)&&q.HighestAdventureTier==0&&!q.Profile.pendingFirstClearReward,"generic noncompletion rewards cannot advance shared gate");
            Check(!q.TryGrantModeReward(Guid.NewGuid().ToString("N"),10,10,1,101)&&q.HighestAdventureTier==0,"invalid tier rejected");
        }
        VariantRoundTrip(root);GoalsAndTutorial(root);
        return "PASS: "+checks+" shared adventure, preset variant and tutorial persistence checks (managed fixtures)";
    }
    static void VariantRoundTrip(string root)
    {
        var p=Fresh(root);p.Profile.level=50;p.Profile.mechanicMaterials=200;p.Profile.gold=99999;p.Save();Check(p.CollectLoot(p.CreateMechanicItem(EquipmentMechanic.FrostEcho)),"collect legacy variant item");
        var item=p.Profile.inventory.Find(x=>x.mechanic==EquipmentMechanic.FrostEcho);string id=item.id;Check(p.Equip(id),"equip variant");
        Check(p.ToggleMechanicVariant(id,true)&&p.SaveBuildPreset(0,true),"capture unlocked B");
        Check(p.ToggleMechanicVariant(id,true)&&p.SaveBuildPreset(1,true),"capture A");
        p.Profile.level=100;p.Save();Check(p.ReforgeMechanic(id,true),"reforge after capture");
        p.Profile.bestFloor=5;p.Profile.slotUpgradeRanks[(int)ItemSlot.Relic]=6;p.Save();Check(p.AscendMechanic(id,true),"ascend after capture");
        var grown=p.Profile.inventory.Find(x=>x.id==id);int atk=grown.attack,hp=grown.health;
        Check(p.ApplyBuildPreset(0,true)&&p.Profile.inventory.Find(x=>x.id==id).mechanicVariant==1,"restore B");
        Check(p.ApplyBuildPreset(1,true)&&p.Profile.inventory.Find(x=>x.id==id).mechanicVariant==0,"restore A");
        Check(p.ApplyBuildPreset(0,true)&&p.Profile.inventory.Find(x=>x.id==id).mechanicVariant==1,"restore B again");
        grown=p.Profile.inventory.Find(x=>x.id==id);Check(grown.rarity==Rarity.Legendary&&grown.attack==atk&&grown.health==hp&&p.Profile.slotUpgradeRanks[2]==6,"preset preserves current ascension stats and slot upgrades");
        Check(p.LoadSlot(p.CurrentSlotId)&&p.ApplyBuildPreset(1,true)&&p.Profile.inventory.Find(x=>x.id==id).mechanicVariant==0,"variants persist after restart");
        p.Profile.variantKnowledge.Clear();p.Profile.inventory.Find(x=>x.id==id).mechanicVariantUnlocked=false;p.Attachment(EquipmentMechanic.FrostEcho).variantUnlocked=false;p.Save();string before=State(p);
        Check(!p.ApplyBuildPreset(0,true)&&State(p)==before,"invalid locked variant cannot unlock or spend");
        p.Profile.buildPresets[0].equipmentVariants=null;p.Profile.buildPresets[0].mountedAttachments=null;p.Profile.buildPresets[0].attachmentVariants=null;Check(p.ApplyBuildPreset(0,true),"legacy preset keeps current variant");
        p.Profile.variantKnowledge.Add(EquipmentMechanic.FrostEcho);p.Profile.inventory.Find(x=>x.id==id).mechanicVariantUnlocked=true;p.Save();before=State(p);
        Directory.CreateDirectory(p.SaveFilePath+".tmp");Check(!p.ApplyBuildPreset(1,true)&&State(p)==before,"failed apply leaves entire current build intact");Directory.Delete(p.SaveFilePath+".tmp");
    }
    static void GoalsAndTutorial(string root)
    {
        foreach(HeroClass hero in Enum.GetValues(typeof(HeroClass)))
        {
            var p=Fresh(root,hero);p.Profile.tutorialMask=15;p.Save();Check(!p.Profile.classTutorialCompleted,"legacy cast evidence does not count as class mechanic");
            string before=State(p);Directory.CreateDirectory(p.SaveFilePath+".tmp");Check(!p.RecordClassTutorialEvidence(hero)&&State(p)==before,"failed tutorial persistence does not publish completed lesson");Directory.Delete(p.SaveFilePath+".tmp");
            Check(p.RecordClassTutorialEvidence(hero)&&p.LoadSlot(p.CurrentSlotId)&&p.Profile.classTutorialCompleted&&p.ClassTutorialUsable,"early real event is preserved without skill/display gate");
            before=State(p);Check(p.RecordClassTutorialEvidence(hero)&&State(p)==before,"class evidence repeat idempotent");
            Check(!p.RecordTutorialEvidence(4),"generic cast cannot complete class lesson");
        }
        var q=Fresh(root);Check(q.SelectProgressionGoal(ProgressionGoalKind.Tier,null,1),"explicit goal set");
        Check(q.TryGrantModeReward(Guid.NewGuid().ToString("N"),10,10,3,1),"achieve goal");
        Check(q.Profile.progressionGoal==ProgressionGoalKind.Tier&&q.Profile.progressionGoalTier==1&&q.ProgressionGoalStatus(3).Contains("已完成")&&q.ProgressionGoalStatus(3).Contains("本局 +3"),"goal completes without auto advancing, shows gains");
        Check(q.LoadSlot(q.CurrentSlotId)&&q.Profile.progressionGoalTier==1,"selected goal persists");
        Check(q.SelectProgressionGoal(ProgressionGoalKind.SecondPreset)&&q.SaveBuildPreset(1,true)&&!q.ProgressionGoalStatus().Contains("已完成"),"B alone is not two builds");
        Check(q.SaveBuildPreset(0,true)&&q.ProgressionGoalStatus().Contains("已完成"),"both saved builds complete the second preset goal");
        string saved=State(q);Directory.CreateDirectory(q.SaveFilePath+".tmp");Check(!q.SelectProgressionGoal(ProgressionGoalKind.Tier,null,2)&&State(q)==saved,"failed goal switch preserves selection");Directory.Delete(q.SaveFilePath+".tmp");
        Check(!q.SelectProgressionGoal(ProgressionGoalKind.Variant,"missing")&&State(q)==saved,"goal cannot target absent item");
    }
}
