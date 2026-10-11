using UnityEngine;
namespace Emberfall
{
    public sealed partial class CombatModel
    {
        private float performanceRemaining,performanceBeatAge,performanceCadence;
        private int performanceSkill,performanceCast;
        internal void BeginSkillPerformance(int skill,float duration,float cadence,int castId)
        {
            if(!isHero||duration<1.2f)return;
            performanceCast=castId;performanceSkill=skill;performanceRemaining=duration;
            performanceBeatAge=0;performanceCadence=Mathf.Max(.3f,cadence);
        }
        internal void SkillPerformanceBeat(int skill,int castId)
        {if(performanceCast==castId&&(skill<0||performanceSkill==skill)&&performanceRemaining>0)performanceBeatAge=0;}
        private bool SampleSkillPerformance(ref float t,ref bool acting,float dt)
        {
            if(performanceRemaining<=0||pilotCharging)return false;
            performanceRemaining=Mathf.Max(0,performanceRemaining-dt);performanceBeatAge+=dt;
            if(performanceRemaining<=0)return false;
            if(actionBasic&&actionDuration>0&&actionAge/actionDuration<.75f)return false;
            // Pull/draw, decisive release, then settle back to the guiding pose.
            float phase=Mathf.Clamp01(performanceBeatAge/performanceCadence);
            t=phase<.25f?Mathf.Lerp(.52f,.64f,phase/.25f):phase<.7f?Mathf.Lerp(.64f,.25f,Mathf.SmoothStep(0,1,(phase-.25f)/.45f)):Mathf.Lerp(.25f,.44f,(phase-.7f)/.3f);
            if(heroClass==HeroClass.Arcanist&&performanceSkill==7)t=.39f+Mathf.Sin(performanceBeatAge*5)*.06f;
            acting=true;return true;
        }
    }
}
