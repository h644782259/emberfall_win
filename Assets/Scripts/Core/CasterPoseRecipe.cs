namespace Emberfall
{
    public enum CasterPoseFamily { Directional, Ground, SelfGuard, Contract }
    public static class CasterPoseRecipe
    {
        public static CasterPoseFamily For(HeroClass hero,int skill)
        {
            if(hero==HeroClass.Vanguard&&(skill==4||skill==6||skill==8))return CasterPoseFamily.SelfGuard;
            if(hero==HeroClass.Summoner)
            {
                if(skill==2||skill==4||skill==6||skill==9)return CasterPoseFamily.Contract;
                if(skill==1||skill==6||skill==7)return CasterPoseFamily.Ground;
            }
            else if(hero==HeroClass.Arcanist&&(skill==0||skill==1||skill==2||skill==5||skill==6||skill==7||skill==9))return CasterPoseFamily.Ground;
            return CasterPoseFamily.Directional;
        }
    }
}
