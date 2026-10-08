namespace Emberfall
{
    public static class ProgressionHudHint
    {
        public static bool TutorialUsable(GameProfile profile,bool mobile,bool companionAvailable)
        {
            if(profile==null)return false;
            if(profile.heroClass==HeroClass.Vanguard)return true;
            if(profile.heroClass==HeroClass.Summoner)return companionAvailable;
            if(profile.heroClass==HeroClass.Ranger)return ActiveSkill(profile,mobile,0);
            return ActiveSkill(profile,mobile,1)&&(profile.specialization==ElementalistSpecialization.Burn||ActiveSkill(profile,mobile,0));
        }
        private static bool ActiveSkill(GameProfile profile,bool mobile,int skill)
        {return profile.skillRanks!=null&&profile.skillRanks.Length>skill&&profile.skillRanks[skill]>0&&(mobile||profile.equippedSkills!=null&&System.Array.IndexOf(profile.equippedSkills,skill)>=0);}
        private static string CompactStep(string text)
        {
            if(string.IsNullOrEmpty(text))return text;
            var parts=new System.Collections.Generic.List<string>();
            foreach(string part in text.Split(new[]{" · "},System.StringSplitOptions.None))
                if(part!="营地已到达"&&!part.StartsWith("仍缺")&&!part.StartsWith("仍需"))parts.Add(part);
            return string.Join(" · ",parts);
        }
        // A room win blocker always owns the HUD. Explicit goals suppress fallback tutorials.
        public static bool TryGet(ProgressionService p,ProgressionAttention attention,bool roomPriority,bool inCamp,bool tutorialUsable,out string title,out string step)
        {
            title=step=null;if(p==null||roomPriority)return false;
            bool selected=p.Profile.progressionGoal!=ProgressionGoalKind.None;
            ProgressionGoalState goal=selected?p.SelectedProgressionGoal(inCamp):null;
            if(inCamp)
            {
                if(goal!=null&&goal.CanAct&&(!goal.Done||goal.Action==ProgressionGoalAction.Equip))
                {title=goal.ActionLabel;step=goal.Title+" · 右上目标";return true;}
                if(attention!=null&&attention.FirstClearClaimable)
                {title="领取首通核心";step="商人 → 机制兑换 · 选择本职业核心";return true;}

                if(attention!=null&&attention.Skills)
                {foreach(int skill in attention.LearnableSkills){title="学习"+GameBalance.SkillName(p.Profile.heroClass,skill);step="职业技能中有可用点数 · 本次只需完成这一步";return true;}}
            }
            if(selected)
            {
                title=goal.Title+(goal.Done?" · 已完成":"");
                step=p.Profile.progressionGoal==ProgressionGoalKind.ClassTutorial&&!tutorialUsable&&!goal.Done?"先学习并装入职业循环所需技能":CompactStep(goal.Step);
                return true;
            }
            if(tutorialUsable&&!p.Profile.classTutorialCompleted)
            {title="职业练习";step=p.ClassTutorialText;return true;}
            return false;
        }
    }
}
