using System;
using System.Collections.Generic;
namespace Emberfall
{
    /// <summary>Seven normal active skills use two mobile pages; ultimate keeps its fixed slot.</summary>
    public static class MobileSkillPolicy
    {
        public const int ButtonCount=5;
        public const int PageCount=2;
        private static readonly int[] activeSkills={0,1,2,4,5,6,7,9};
        public static int SkillAtButton(int button,int page=0)
        {if(button<0||button>=ButtonCount||page<0||page>=PageCount)return -1;if(button==4)return 9;int index=page*4+button;return index<7?activeSkills[index]:-1;}
        public static int[] DefaultBindings(bool summoner=false){return summoner?new[]{0,1,5,7,2,4,6,-1,9}:new[]{0,1,2,4,5,6,7,-1,9};}
        public static bool ValidBindings(int[] values)
        {
            if(values==null||values.Length!=9||values[8]!=9)return false;
            var seen=new HashSet<int>();
            foreach(int value in values)if(value!=-1&&!IsActiveSkill(value)||!seen.Add(value))return false;
            return true;
        }
        public static int BindingIndex(int button,int page)
        {return button<0||button>=5||page<0||page>=2?-1:button==4?8:page*4+button;}
        public static bool SwapBinding(int[] values,int index,int skill)
        {
            if(!ValidBindings(values)||index<0||index>=8||skill==9)return false;
            int source=Array.IndexOf(values,skill);if(source<0)return false;
            int previous=values[index];values[index]=skill;values[source]=previous;return true;
        }
        public static bool IsActiveSkill(int skill){return skill>=0&&skill<10&&skill!=3&&skill!=8;}
        public struct Candidate
        {
            public float DistanceSquared;public bool Valid,CurrentFocus;
            public Candidate(float distanceSquared,bool valid,bool currentFocus){DistanceSquared=distanceSquared;Valid=valid;CurrentFocus=currentFocus;}
        }
        public static int SelectTarget(IReadOnlyList<Candidate> candidates,float range)
        {
            if(candidates==null||range<=0||float.IsNaN(range)||float.IsInfinity(range))return -1;
            int best=-1;float nearest=range*range;
            for(int i=0;i<candidates.Count;i++)
            {
                var c=candidates[i];
                if(!c.Valid||c.DistanceSquared<0||float.IsNaN(c.DistanceSquared)||float.IsInfinity(c.DistanceSquared)||c.DistanceSquared>range*range)continue;
                if(c.CurrentFocus)return i;
                if(best<0||c.DistanceSquared<nearest){nearest=c.DistanceSquared;best=i;}
            }
            return best;
        }
    }
    /// <summary>Exactly one release per captured touch; moving onto another button never changes the skill.</summary>
    public sealed class MobileSkillTap
    {
        public int Finger {get;private set;}=-1000;
        public int Skill {get;private set;}=-1;
        public bool Active {get{return Finger!=-1000;}}
        public bool Begin(int finger,int skill)
        {if(Active||!MobileSkillPolicy.IsActiveSkill(skill))return false;Finger=finger;Skill=skill;return true;}
        public bool Release(int finger,bool inside,bool cancelled,out int skill)
        {skill=-1;if(finger!=Finger||!Active)return false;int picked=Skill;Cancel();if(!inside||cancelled)return false;skill=picked;return true;}
        public void Cancel(){Finger=-1000;Skill=-1;}
    }
}
