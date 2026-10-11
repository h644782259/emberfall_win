using System;
namespace Emberfall
{
    // Presentation holds use the real event schedule; no animation callback owns damage.
    public static class SkillPerformanceTiming
    {
        public static float Duration(HeroClass hero,int skill,int rank)
        {
            if(skill==2&&hero!=HeroClass.Summoner){var field=SkillDamageBudgets.EarlyField(hero,rank);return field.Startup+field.Duration;}
            if(skill==7||skill==9)
            {
                if(hero==HeroClass.Summoner)return skill==7?SummonerDamageRules.MarkFinisherTime:2.4f;
                return SkillDamageBudgets.AdvancedFirstEvent(hero,skill)+(SkillDamageBudgets.AdvancedSteps(hero,skill,rank)-1)*SkillDamageBudgets.AdvancedInterval(hero,skill)+.35f;
            }
            if(hero==HeroClass.Arcanist&&skill==6)return 1.3f;
            return hero==HeroClass.Summoner&&(skill==2||skill==4||skill==6)?1.8f:skill==1&&hero==HeroClass.Arcanist?1.6f:.9f;
        }
    }
}
