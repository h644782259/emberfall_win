using System;
using System.Collections.Generic;

namespace Emberfall
{
    public static class CompanionRules
    {
        public const float CooperationWindow = 1.5f;
        public const float CooperationCooldown = 3f;
        public const float CooperationDamage = .35f;
        public const float AreaDamageTaken = .55f;
        public const float RecallDamageTaken = .5f;
        public const float RecallDuration = 3f;
        public const float DodgeProtectionDuration = 3f;
        public const float CommandOpportunityDuration = 16f;
        public const float EmpoweredCommandMultiplier = 1.5f, WolfCommandCoefficient = 1.2f, WolfCommandRecovery = .6f, CommandReadyDelay = .1f;
        public const float RestThreatRadius = 16f; // Covers the boss's full ranged engagement distance.
        public static bool CoordinatedTarget(bool playerFocusMatches, bool livingCommandMatches, bool explicitFocusMatches = false)
        { return playerFocusMatches || livingCommandMatches || explicitFocusMatches; }
        public static bool CanTransfer(bool permanent, float health, bool active)
        { return false; }
        public static bool ShouldCreatePartner(bool livingPartner) { return !livingPartner; }
        public static bool PermanentPartner(bool foundation, int form, bool packRoute) { return false; }
        public static int PackReinforcements(int currentCount, int rank, bool twinContract)
        { return Math.Max(0, Math.Min(NormalCapacity(twinContract) - Math.Max(0, currentCount), rank >= 3 ? 3 : 2)); }
        public static float PackLifetime(int rank) { return 8f + Math.Max(1, Math.Min(3, rank)) * 2f; }
        public static float RefreshPackLifetime(float remaining,int rank)
        {return Math.Max(float.IsNaN(remaining)||float.IsInfinity(remaining)?0:remaining,PackLifetime(rank));}
        public static float PreserveRecastHealth(float current,float maximum)
        {return float.IsNaN(current)||float.IsInfinity(current)||float.IsNaN(maximum)||float.IsInfinity(maximum)?0:Math.Max(0,Math.Min(current,maximum));}
        // Persistent bodies follow the current investment, rather than retaining
        // the rank of the cast that first created them. Timed casts keep theirs.
        public static int EffectiveRank(int form, bool foundation, bool permanent, int castRank, int wolfRank, int spiritRank)
        { return Math.Max(0, Math.Min(3, foundation ? wolfRank : permanent && form == 1 ? spiritRank : castRank)); }
        public static float ActiveCommandMultiplier(int rank, bool empowered)
        { return CommandMultiplier(rank) * (empowered ? EmpoweredCommandMultiplier : 1f); }
        public static int NormalCapacity(bool twinContract) { return twinContract ? 2 : 4; }
        public static float DamageMultiplier(bool twinContract) { return twinContract ? 1.6f : 1f; }
        public static float HealthMultiplier(bool twinContract) { return twinContract ? 1.2f : 1f; }
        public static float RankPower(int rank) { return rank <= 0 ? .55f : 1f + (Math.Min(3, rank) - 1) * .3f; }
        public static float CommandMultiplier(int rank) { return 1.25f + Math.Max(0, Math.Min(3, rank)) * .1f; }
        public static float CommandDuration(bool empowered){return empowered?4f:3f;}
        public static float AttackCoefficient(int form){return form==2?2.25f:form==3?.85f:form==1?.72f:.65f;}
        public static float AttackInterval(int form){return form==2?2.2f:form==3?1.5f:form==1?1.2f:.85f;}
        public static float ContractLifetime(int form,int rank,bool permanent)
        {rank=Math.Max(1,Math.Min(3,rank));return form==2?24+(rank-1)*6:(form==0?14:12)+(rank-1)*4;}
        public static float HealthFraction(int form, int rank, bool foundation)
        {
            rank = Math.Max(0, Math.Min(3, rank));
            if (form == 2) return 1.2f + Math.Max(0, rank - 1) * .12f;
            return foundation ? .45f + rank * .1f : .42f + rank * .08f;
        }
        public static float DamageTaken(float amount, bool areaAttack, bool recalling, bool dodgeProtected = false)
        {
            if (float.IsNaN(amount) || float.IsInfinity(amount) || amount <= 0) return 0;
            return amount * (areaAttack ? AreaDamageTaken : 1f) * (recalling || dodgeProtected ? RecallDamageTaken : 1f);
        }
    }

    public sealed class CompanionCommandOpportunity
    {
        private float commandUntil=-1, protectionUntil=-1;
        public void Grant(float combatTime)
        {
            if(!Valid(combatTime))return;
            commandUntil=Math.Max(commandUntil,combatTime+CompanionRules.CommandOpportunityDuration);
            protectionUntil=Math.Max(protectionUntil,combatTime+CompanionRules.DodgeProtectionDuration);
        }
        public bool IsProtected(float combatTime){return Valid(combatTime)&&combatTime<protectionUntil;}
        public float Remaining(float combatTime){return Valid(combatTime)?Math.Max(0,commandUntil-combatTime):0;}
        public bool TryConsume(float combatTime)
        {if(Remaining(combatTime)<=0)return false;commandUntil=-1;return true;}
        private static bool Valid(float value){return value>=0&&!float.IsNaN(value)&&!float.IsInfinity(value);}
    }

    // Only confirmed impacts enter this tracker. A second pet of the same type,
    // a different target, launches, and expired marks cannot trigger cooperation.
    public sealed class CompanionCooperationTracker<T> where T : class
    {
        private sealed class Hits
        {
            public readonly float[] Times = { float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity };
            public float Last;
        }
        private readonly Dictionary<T, Hits> targets = new Dictionary<T, Hits>();
        private readonly List<T> expired = new List<T>();
        private float nextProc, latestTime;
        // Observation only: practice must not age a live mark or cooldown while
        // its original actor is suspended and the shared game clock keeps running.
        public bool HasPending(float combatTime)
        {
            if (combatTime < nextProc) return true;
            foreach (var pair in targets)
                if (combatTime - pair.Value.Last <= CompanionRules.CooperationWindow) return true;
            return false;
        }
        public void Clear() { targets.Clear(); nextProc = latestTime = 0; }
        public bool RegisterHit(T target, int form, float combatTime)
        {
            if (target == null || form < 0 || form > 3 || float.IsNaN(combatTime) || float.IsInfinity(combatTime) || combatTime < latestTime) return false;
            latestTime = combatTime;
            expired.Clear();
            foreach (var pair in targets)
                if (combatTime - pair.Value.Last > CompanionRules.CooperationWindow) expired.Add(pair.Key);
            foreach (T stale in expired) targets.Remove(stale);
            Hits hits;
            if (!targets.TryGetValue(target, out hits)) { hits = new Hits(); targets.Add(target, hits); }
            bool partnerHit = false;
            for (int kind = 0; kind < hits.Times.Length; kind++)
                if (kind != form && combatTime - hits.Times[kind] <= CompanionRules.CooperationWindow) partnerHit = true;
            hits.Times[form] = hits.Last = combatTime;
            if (!partnerHit || combatTime < nextProc) return false;
            targets.Remove(target);
            nextProc = combatTime + CompanionRules.CooperationCooldown;
            return true;
        }
    }
}
