using System;
namespace Emberfall
{
    public enum WeaponVisualAnchor
    {
        SwordPommel, SwordGrip, SwordGuard, SwordRoot, SwordTip,
        StaffBottom, StaffGrip, StaffCollar, StaffCore, StaffTop,
        BowGrip, BowUpperTip, BowLowerTip, BowNock, BowArrowRest
    }

    // Rig-local dimensions, independent of damage, range and safe projectile origins.
    // Cylinder scale.y is HALF its length; every staff shaft ends inside its collar.
    public struct WeaponStructure
    {
        public readonly int Tier;
        public readonly float SwordRoot, SwordTip, StaffBottom, StaffCollar, StaffCore,
            StaffCoreDiameter, BowReach;
        public WeaponStructure(int tier,bool compactStaff=false)
        {
            Tier = Math.Max(0, Math.Min(4, tier));
            float growth = 1f + Math.Max(0, Tier - 1) * .13f;
            SwordRoot = Tier == 0 ? .2f : .21f;
            SwordTip = SwordRoot + (Tier == 0 ? 1.1f : 1.08f * growth);
            StaffBottom = compactStaff?-.82f:-.94f;
            StaffCollar = compactStaff?.78f:1.02f + Math.Max(0,Tier-2)*.085f;
            StaffCore = StaffCollar + (compactStaff?.24f:.31f);
            StaffCoreDiameter = Tier == 0 ? .34f : .24f + Tier * .065f;
            BowReach = Tier == 0 ? .58f : .59f * growth;
        }
        public float StaffShaftCenter { get { return (StaffBottom + StaffCollar) * .5f; } }
        public float StaffShaftHalfLength { get { return (StaffCollar - StaffBottom) * .5f; } }
        public float StaffTop { get { return StaffCore + StaffCoreDiameter * .5f; } }
    }
}
