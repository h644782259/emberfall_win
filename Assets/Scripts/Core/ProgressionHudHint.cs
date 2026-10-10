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
            var goal=p.SelectedProgressionGoal(inCamp);
            title=goal.Title;step=goal.Step;
            return true;
        }
    }
}
