using UnityEngine;

namespace Emberfall
{
    /// <summary>A pending cast owns one fixed aim and commits its budget only on release.</summary>
    public sealed class SkillChargeController : MonoBehaviour
    {
        public bool IsCharging { get { return SkillIndex >= 0; } }
        public int SkillIndex { get; private set; } = -1;
        public float Progress { get { return IsCharging && duration > 0 ? Mathf.Clamp01(elapsed / duration) : 0; } }
        public bool CancelledThisFrame { get { return cancelledFrame == Time.frameCount; } }
        public bool ConsumedThisFrame { get { return CancelledThisFrame || completedFrame == Time.frameCount; } }
        public Vector3 TargetPoint { get; private set; }
        public EnemyController TargetEnemy { get; private set; }
        internal Vector3 Direction { get; private set; }

        private PlayerController owner;
        private GameSession session;
        private int epoch, cancelledFrame = -1, completedFrame = -1;
        private float duration, elapsed;
        private AdvancedSkillVfx chargeEffect;
        private SkillCastConduit weaponFocus;

        public void Initialize(PlayerController hero, GameSession game) { owner = hero; session = game; }

        public static float Duration(HeroClass hero, int skill)
        { return SkillDamageBudgets.ChargeSeconds(hero, skill); }

        public bool Begin(int skill)
        {
            if (owner == null || session == null || IsCharging || ConsumedThisFrame || !owner.CanBeginSkillTargeting(skill)) return false;
            float chargeTime = Duration(owner.HeroClass, skill);
            if (chargeTime <= 0) return false;
            int rank = session.Progression.Profile.skillRanks[skill];
            Vector3 origin = owner.transform.position;
            bool contract = owner.HeroClass == HeroClass.Summoner && (skill == 4 || skill == 9);
            var targeting = contract ? owner.GetComponent<SkillTargetingController>() : null;
            bool explicitPoint = targeting != null && targeting.ConfirmingContract;
            TargetEnemy = contract && !explicitPoint ? SummonedCompanion.ExplicitFocus(owner) ?? owner.AimTarget : owner.AimTarget;
            Vector3 intendedPoint = contract && !explicitPoint && TargetEnemy != null ? TargetEnemy.transform.position : owner.AimPoint;
            TargetPoint = Vector3.ClampMagnitude(origin + Vector3.ClampMagnitude(CombatFx.Flat(intendedPoint - origin), 9f * GameBalance.SkillRangeMultiplier(rank)), session.ArenaRadius);
            if (contract) TargetPoint = CombatSight.GroundPoint(origin, TargetPoint);
            if (!ValidSnapshotTarget()) TargetEnemy = null;
            Direction = owner.transform.forward;
            epoch = owner.CombatEpoch;
            duration = chargeTime;
            elapsed = 0;
            SkillIndex = skill;
            weaponFocus=SkillCastConduit.BeginCharge(owner,owner.GetComponentInChildren<CombatModel>(),chargeTime+.2f);
            chargeEffect = AdvancedSkillVfx.Rune(owner, origin, 1.35f, GameBalance.ClassColor(owner.HeroClass), chargeTime + .2f, 2, true, skill==6&&owner.HeroClass==HeroClass.Vanguard?4:owner.HeroClass==HeroClass.Vanguard?1:owner.HeroClass==HeroClass.Summoner?3:owner.HeroClass==HeroClass.Arcanist&&skill==4?2:0);
            if (chargeEffect != null) chargeEffect.transform.localScale = Vector3.one * .65f;
            return true;
        }

        public void Cancel()
        {
            if (IsCharging)
            {
                if (CombatReviewEvents.Enabled && owner != null) CombatReviewEvents.Emit("chargecancel",CombatReviewObjectId.Get(owner),skill:SkillIndex);
                if (owner != null) owner.CancelCombatPose();
                cancelledFrame = Time.frameCount;
            }
            SkillIndex = -1; TargetEnemy = null;
            elapsed = duration = 0;
            ClearEffect();
        }

        private void LateUpdate() { Advance(Time.deltaTime); }

        // Kept separate so runtime checks can exercise exact commit boundaries.
        private void Advance(float deltaTime)
        {
            if (!IsCharging) return;
            if (owner == null || owner.IsDead || session == null || session.Player != owner || !session.HasStarted || owner.CombatEpoch != epoch)
            { Cancel(); return; }
            if (session.InputBlocked || deltaTime <= 0 || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime)) return;
            elapsed += deltaTime;
            if(weaponFocus!=null)weaponFocus.ChargeProgress(Progress);
            if (chargeEffect != null) chargeEffect.transform.localScale = Vector3.one * Mathf.Lerp(.65f, 1.2f, Progress);
            if (elapsed < duration) return;
            int skill = SkillIndex;
            SkillIndex = -1;
            // Revalidate the learned rank, budget and cooldown at the actual release.
            // TargetPoint/Direction remain the original snapshot through this call.
            if (!ValidSnapshotTarget()) TargetEnemy = null;
            owner.ExecuteChargedSkill(skill);
            TargetEnemy = null;
            ClearEffect();
            completedFrame = Time.frameCount;
            elapsed = duration = 0;
        }

        private bool ValidSnapshotTarget()
        {
            return TargetEnemy != null && !TargetEnemy.IsDead && TargetEnemy.gameObject.activeInHierarchy &&
                session != null && owner != null && session.Enemies.Contains(TargetEnemy) &&
                CombatFx.Flat(TargetEnemy.transform.position-owner.transform.position).sqrMagnitude <= 14f*14f &&
                CombatSight.Direct(owner.transform.position,TargetEnemy.transform.position);
        }

        private void OnDisable() { Cancel(); }

        private void ClearEffect()
        {
            if(weaponFocus!=null){weaponFocus.Retire();weaponFocus=null;}
            if (chargeEffect == null) return;
            chargeEffect.gameObject.SetActive(false);
            Destroy(chargeEffect.gameObject);
            chargeEffect = null;
        }
    }
}
