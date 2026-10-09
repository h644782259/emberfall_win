using System;
using Emberfall;

public static class BossAttackPolicyTests
{
    private static int assertions;
    private static void Check(bool condition, string message)
    {
        assertions++;
        if (!condition) throw new Exception("FAILED: " + message);
    }
    private static bool Near(float a, float b) { return Math.Abs(a - b) < .0001f; }

    public static string Run()
    {
        assertions = 0;
        Check(BossAttackPolicy.Select(0) == BossAttackPolicy.Move.Slam, "overlapping target selects slam");
        Check(BossAttackPolicy.Select(3.5f) == BossAttackPolicy.Move.Slam, "old melee gate remains close range");
        Check(BossAttackPolicy.Select(BossAttackPolicy.CloseRange) == BossAttackPolicy.Move.Slam, "close boundary is inclusive");
        Check(BossAttackPolicy.Select(BossAttackPolicy.CloseRange + .001f) == BossAttackPolicy.Move.Charge, "charge begins beyond close range");
        Check(BossAttackPolicy.Select(6) == BossAttackPolicy.Move.Charge, "midrange charge starts without entering 3.5m gate");
        Check(BossAttackPolicy.Select(BossAttackPolicy.ChargeRange) == BossAttackPolicy.Move.Charge, "charge boundary is inclusive");
        Check(BossAttackPolicy.Select(BossAttackPolicy.ChargeRange + .001f) == BossAttackPolicy.Move.Fan, "far range starts fan");
        Check(BossAttackPolicy.Select(15) == BossAttackPolicy.Move.Fan, "far fan starts without entering melee range");
        Check(BossAttackPolicy.CanEngage(0) && BossAttackPolicy.CanEngage(16), "legal engagement range includes both edges");
        Check(!BossAttackPolicy.CanEngage(-.01f) && !BossAttackPolicy.CanEngage(16.001f), "invalid or too-far targets cannot engage");
        Check(!BossAttackPolicy.CanEngage(float.NaN) && !BossAttackPolicy.CanEngage(float.PositiveInfinity), "nonfinite distances cannot engage");
        Check(!BossAttackPolicy.IsEnraged(100, 100), "full health has no combo");
        Check(!BossAttackPolicy.IsEnraged(50.01f, 100), "above half health has no combo");
        Check(BossAttackPolicy.IsEnraged(50, 100) && BossAttackPolicy.IsEnraged(.01f, 100), "half health and below enable combo");
        Check(!BossAttackPolicy.IsEnraged(0, 100) && !BossAttackPolicy.IsEnraged(-1, 100), "dead boss cannot enter phase two");
        Check(!BossAttackPolicy.IsEnraged(1, 0) && !BossAttackPolicy.IsEnraged(1, float.PositiveInfinity), "invalid max health cannot enable combo");
        Check(!BossAttackPolicy.IsEnraged(float.NaN, 100) && !BossAttackPolicy.IsEnraged(float.PositiveInfinity, 100), "invalid current health cannot enable combo");
        Check(BossAttackPolicy.FollowUp(BossAttackPolicy.Move.Slam, 2) == BossAttackPolicy.Move.Fan, "slam combo changes to ranged pressure");
        Check(BossAttackPolicy.FollowUp(BossAttackPolicy.Move.Slam, 10) == BossAttackPolicy.Move.Fan, "slam cannot queue another unreachable melee attack");
        Check(BossAttackPolicy.FollowUp(BossAttackPolicy.Move.Charge, 2) == BossAttackPolicy.Move.Slam, "charge closes into slam");
        Check(BossAttackPolicy.FollowUp(BossAttackPolicy.Move.Charge, 12) == BossAttackPolicy.Move.Fan, "escaped charge target gets reachable follow-up");
        Check(BossAttackPolicy.FollowUp(BossAttackPolicy.Move.Fan, 6) == BossAttackPolicy.Move.Charge, "approaching a fan can trigger charge follow-up");
        Check(BossAttackPolicy.ComboGap >= .3f, "combo has a real inter-move gap");
        foreach (BossAttackPolicy.Move move in Enum.GetValues(typeof(BossAttackPolicy.Move)))
        {
            Check(BossAttackPolicy.Windup(move, false) >= 1.6f, move + " first attack warning stays readable");
            Check(BossAttackPolicy.Windup(move, true) >= 1.5f, move + " combo keeps an independent readable warning");
            Check(BossAttackPolicy.Windup(move, true) < BossAttackPolicy.Windup(move, false), move + " combo has distinct but fair pacing");
        }
        Check(BossAttackPolicy.Recovery(true) >= BossAttackPolicy.Recovery(false), "full combo gives a punish window");
        Check(Near(BossAttackPolicy.ChargeContactDelay(1.3f, 0, .2f), .2f), "near-front threat arrives after remaining windup");
        Check(Near(BossAttackPolicy.ChargeContactDelay(3.5f, 0, 0), .2f), "charge considers time to actual swept contact");
        Check(Near(BossAttackPolicy.ChargeContactDelay(3.5f, 0, .8f), 1f), "early warning alone is not an imminent dodge");
        Check(BossAttackPolicy.ChargeContactDelay(3.5f, 1.2f, 0) > .2f, "grazing edge makes contact later than centerline");
        Check(float.IsPositiveInfinity(BossAttackPolicy.ChargeContactDelay(3, 1.3f, 0)), "outside charge width can never award");
        Check(float.IsPositiveInfinity(BossAttackPolicy.ChargeContactDelay(3, -2, 0)), "other side outside width can never award");
        Check(float.IsPositiveInfinity(BossAttackPolicy.ChargeContactDelay(float.NaN, 0, 0)), "nonfinite geometry cannot award");
        Check(float.IsPositiveInfinity(BossAttackPolicy.ChargeContactDelay(3, 0, float.PositiveInfinity)), "nonfinite timing cannot award");
        Check(BossAttackPolicy.IsPerfectDodgeTiming(.2f, .22f), "imminent timing is eligible");
        Check(BossAttackPolicy.IsPerfectDodgeTiming(0, .22f), "active threat at contact is eligible");
        Check(!BossAttackPolicy.IsPerfectDodgeTiming(.23f, .22f), "early dodge is ineligible");
        Check(!BossAttackPolicy.IsPerfectDodgeTiming(.31f, 100), "caller cannot inflate perfect-dodge timing window");
        Check(!BossAttackPolicy.IsPerfectDodgeTiming(-.01f, .22f), "past attack cannot award");
        Check(!BossAttackPolicy.IsPerfectDodgeTiming(.1f, 0), "empty window cannot award");
        Check(!BossAttackPolicy.IsPerfectDodgeTiming(float.NaN, .22f) && !BossAttackPolicy.IsPerfectDodgeTiming(float.PositiveInfinity, .22f), "invalid impact time cannot award");
        Check(!BossAttackPolicy.IsPerfectDodgeTiming(.1f, float.NaN) && !BossAttackPolicy.IsPerfectDodgeTiming(.1f, float.PositiveInfinity), "invalid configured window cannot award");
        Check(BossAttackPolicy.Select(3, BossAttackPolicy.Move.Slam, 1)==BossAttackPolicy.Move.Charge, "recent slam avoids distance-locked repeat");
        Check(BossAttackPolicy.Select(6, BossAttackPolicy.Move.Charge, 1)==BossAttackPolicy.Move.Fan, "midrange charge varies next attack");
        Check(BossAttackPolicy.ShouldAdvance(12, BossAttackPolicy.Move.Fan, 1), "far fan prompts real repositioning");
        Check(!BossAttackPolicy.ShouldAdvance(6, BossAttackPolicy.Move.Fan, 1), "close enough can attack instead of forced advance");
        for (int i = 0; i <= 160; i++)
        {
            float distance = i / 10f;
            BossAttackPolicy.Move move = BossAttackPolicy.Select(distance);
            Check(BossAttackPolicy.CanEngage(distance), "every tenth-meter target in range engages " + distance);
            Check(move != BossAttackPolicy.Move.Slam || distance <= BossAttackPolicy.SlamRadius, "selected slam can reach target " + distance);
        }
        return "PASS: " + assertions + " boss policy and imminent-dodge timing assertions";
    }
}
