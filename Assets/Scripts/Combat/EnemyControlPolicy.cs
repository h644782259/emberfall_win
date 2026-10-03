using System;

namespace Emberfall
{
    public enum EnemyControlTier { Normal, Elite, Boss }

    /// <summary>Per-target bounded control budget. Stun and knockback cannot stack
    /// indefinitely; qualifying skills get one interruption attempt per cast/target.</summary>
    public sealed class EnemyControlPolicy
    {
        public EnemyControlTier Tier { get; private set; }
        public float ControlRecovery { get; private set; }
        public float InterruptRecovery { get; private set; }
        public float KnockbackRecovery { get; private set; }
        private int interruptedWindup;
        private readonly CastFirstHitHistory standaloneCasts=new CastFirstHitHistory();
        public EnemyControlPolicy(EnemyControlTier tier) { Tier = tier; }
        public float MaximumImpulse { get { return Tier == EnemyControlTier.Boss ? 1.8f : Tier == EnemyControlTier.Elite ? 6f : 11f; } }
        public bool CanInterruptWindup { get { return InterruptRecovery <= 0; } }

        public void Advance(float delta)
        {
            if (!Finite(delta) || delta <= 0) return;
            ControlRecovery = Math.Max(0, ControlRecovery - delta);
            InterruptRecovery = Math.Max(0, InterruptRecovery - delta);
            KnockbackRecovery = Math.Max(0, KnockbackRecovery - delta);
        }

        public float ApplyStun(float requested)
        {
            // Boss hard interruption is exclusively a real qualifying player skill
            // during the visible windup, never a DoT/pet/pulse's repeated raw stun.
            if (!Finite(requested) || requested <= 0 || ControlRecovery > 0 || Tier == EnemyControlTier.Boss) return 0;
            float duration = Math.Min(Tier == EnemyControlTier.Elite ? .8f : 1.5f,
                requested * (Tier == EnemyControlTier.Elite ? .55f : 1f));
            ControlRecovery = duration + (Tier == EnemyControlTier.Elite ? 1.1f : .45f);
            return duration;
        }

        public float ApplyKnockback(float requested)
        {
            if (!Finite(requested) || requested <= 0 || KnockbackRecovery > 0) return 0;
            float factor = Tier == EnemyControlTier.Boss ? .12f : Tier == EnemyControlTier.Elite ? .55f : 1f;
            KnockbackRecovery = Tier == EnemyControlTier.Boss ? 1f : Tier == EnemyControlTier.Elite ? .35f : .12f;
            return Math.Min(MaximumImpulse, requested * 7f * factor);
        }

        public bool TryInterrupt(int castId, int windupId, bool preparing, bool eligibleSkill, out float stagger)
        { return TryInterrupt(standaloneCasts.Get(castId),windupId,preparing,eligibleSkill,out stagger); }
        public bool TryInterrupt(CastFirstHitReceipt cast, int windupId, bool preparing, bool eligibleSkill, out float stagger)
        {
            stagger = 0;
            if (!eligibleSkill || cast==null || !cast.FirstInterruptTarget(this)) return false;
            // Consume first eligible contact even outside a windup or during recovery.
            if (!preparing || windupId <= 0 || windupId == interruptedWindup || InterruptRecovery > 0) return false;
            interruptedWindup = windupId;
            stagger = Tier == EnemyControlTier.Boss ? .45f : Tier == EnemyControlTier.Elite ? .65f : .9f;
            InterruptRecovery = Tier == EnemyControlTier.Boss ? 5f : Tier == EnemyControlTier.Elite ? 2.5f : 1.4f;
            ControlRecovery = Math.Max(ControlRecovery, stagger + (Tier == EnemyControlTier.Boss ? 2.5f : .65f));
            return true;
        }

        public float MaximumPullSpeed { get { return Tier == EnemyControlTier.Boss ? .6f : Tier == EnemyControlTier.Elite ? 2.5f : 5f; } }
        public float PullDistance(float requestedDistance, float deltaTime)
        {
            if (!Finite(requestedDistance) || !Finite(deltaTime) || requestedDistance <= 0 || deltaTime <= 0) return 0;
            float factor = Tier == EnemyControlTier.Boss ? .12f : Tier == EnemyControlTier.Elite ? .55f : 1f;
            return Math.Min(requestedDistance * factor, MaximumPullSpeed * Math.Min(.1f, deltaTime));
        }

        public void ResetCastOwner() { standaloneCasts.Clear(); }
        public static bool IsInterruptSkill(HeroClass hero, int skill)
        {
            return hero == HeroClass.Vanguard && skill == 1 ||
                hero == HeroClass.Arcanist && (skill == 0 || skill == 1) ||
                hero == HeroClass.Ranger && skill == 1 ||
                hero == HeroClass.Summoner && skill == 0;
        }
        private static bool Finite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
    }
}
