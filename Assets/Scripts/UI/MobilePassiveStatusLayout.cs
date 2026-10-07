namespace Emberfall
{
    // Read-only passive identities live inside the existing player status card.
    // They are not skill buttons and do not consume space in the combat view.
    public static class MobilePassiveStatusLayout
    {
        public const int Count=2;
        public static int SkillAtIndicator(int index){return index==0?3:index==1?8:-1;}
        public static readonly MobileControlLayout.Area HealthBar=new MobileControlLayout.Area(100,39,95,8);
        public static readonly MobileControlLayout.Area EnergyBar=new MobileControlLayout.Area(100,52,95,5);
        public static MobileControlLayout.Area Indicator(int index)
        {return new MobileControlLayout.Area(202+index*29,36,24,34);}
    }
}
