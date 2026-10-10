using System;
using System.IO;
using System.Linq;
using Emberfall;
public static class MainlineGoalTests
{
    static int checks;
    static void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
    public static string Run(string root)
    {
        foreach(HeroClass hero in Enum.GetValues(typeof(HeroClass)))
        {
            var p=new ProgressionService(Path.Combine(root,"main-"+hero));p.NewGame(hero);
            foreach(ProgressionGoalKind legacy in Enum.GetValues(typeof(ProgressionGoalKind)))
            {
                p.Profile.automaticGrowth=false;p.Profile.progressionGoal=legacy;
                p.Profile.progressionGoalMechanic=BuildCatalog.MechanicsFor(hero).First();
                p.Profile.progressionGoalItemId="sold-gem";
                var goal=p.SelectedProgressionGoal(true);
                Check(goal.Identity=="main/level/30"&&goal.ItemId==null&&goal.MaterialCost==0&&goal.Action==ProgressionGoalAction.None,"every legacy goal becomes main progression without a gem gate");
                string title,step;var attention=new ProgressionAttention{FirstClearClaimable=true};attention.LearnableSkills.Add(0);
                Check(ProgressionHudHint.TryGet(p,attention,false,true,false,out title,out step)&&title==goal.Title&&step==goal.Step,"main HUD resists merchant and tutorial interruptions");
                Check(!ProgressionHudHint.TryGet(p,attention,true,true,true,out title,out step),"combat objective takes precedence");
            }
            for(int chapter=0;chapter<3;chapter++)
            {
                p.Profile.chapterCompletedMask=(1<<chapter)-1;p.Profile.level=30+10*chapter;
                var goal=p.SelectedProgressionGoal(true);
                Check(goal.Identity=="main/chapter/"+chapter&&goal.Title.Contains(ChapterDefinition.Get((ChapterNode)chapter).Name)&&!goal.Done,"uncompleted chapter is the main target");
                p.Profile.chapterCompletedMask|=1<<chapter;
                Check(!p.SelectedProgressionGoal(true).Identity.Contains("gem"),"chapter completion advances without collecting or ascending gems");
            }
            p.Profile.level=100;p.Profile.chapterCompletedMask=7;p.Profile.highestAdventureTier=9;
            var final=p.SelectedProgressionGoal(true);Check(final.RequiredAdventureTier==10&&!final.Done,"last dungeon target follows completed story");
            p.Profile.highestAdventureTier=10;Check(p.SelectedProgressionGoal(true).Done,"main progression completes without gems");
            Check(p.Profile.attachments.Count==0,"goal computation does not fabricate gems");
            Check(ProgressionService.Achievements.Any(a=>a.Id.StartsWith("ascend/"))&&ProgressionService.Achievements.Any(a=>a.Id.StartsWith("core/")),"gem-specific progress remains in achievements");
            p.Profile.buildPresets=new[]{new BuildPreset{populated=true,weaponId="missing",mountedAttachments=new[]{BuildCatalog.MechanicsFor(hero).First()}}};
            p.Save();Check(p.LastError.Length==0&&p.LoadSlot(p.CurrentSlotId)&&p.SelectedProgressionGoal(true).Done,"legacy preset and goal data do not affect save or mainline after reload");
        }
        foreach(string retired in new[]{"SaveBuildPreset","ApplyBuildPreset","HasBuildPreset","QuotePresetReplacement","ReplacePresetEquipment","PresetReferences","BulkSalePresetImpact"})
            Check(typeof(ProgressionService).GetMethod(retired)==null,"retired preset API removed: "+retired);
        return "PASS "+checks+" mainline, legacy data, HUD precedence and preset retirement assertions";
    }
}
