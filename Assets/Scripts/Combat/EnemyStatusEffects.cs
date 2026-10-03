using UnityEngine;

namespace Emberfall
{
    /// <summary>Distinct timed status mechanics; all timers obey combat pause.</summary>
    public sealed class EnemyStatusEffects : MonoBehaviour
    {
        public float MoveMultiplier { get { return slowTime > 0 ? 1 - slowStrength : 1; } }
        public float DamageMultiplier { get { return markTime > 0 ? 1 + markStrength : 1; } }
        public int PoisonStacks { get { return poisonTime > 0 ? poisonStacks : 0; } }
        public bool IsFrozen { get { return frozenTime > 0; } }
        public bool HasFrostMark { get { return frozenTime > 0 || frostMarkTime > 0; } }
        public bool IsMarked { get { return markTime > 0; } }
        public bool IsBurning { get { return burnTime > 0; } }
        public float FrostRemaining {get{return Mathf.Max(frozenTime,frostMarkTime);}}
        // Presentation metadata follows the timer that supplies the observed window.
        private float frozenWindowDuration, frostWindowDuration, poisonWindowDuration, burnWindowDuration;
        public float FrostWindowDuration {get{return frozenTime>=frostMarkTime?frozenWindowDuration:frostWindowDuration;}}
        public float PoisonWindowDuration {get{return poisonWindowDuration;}}
        public float BurnWindowDuration {get{return burnWindowDuration;}}
        public float OwnPoisonOpportunityRemaining(PlayerController source)
        {return source!=null&&poisonSource==source&&sourceEpoch==source.CombatEpoch&&PoisonStacks>=3?poisonTime:0;}
        public float PoisonRemaining {get{return poisonTime;}}
        public float MarkRemaining {get{return markTime;}}
        public float BurnRemaining {get{return burnTime;}}
        public float OwnBurnRemaining(PlayerController source)
        {return source!=null&&burnSource==source&&burnEpoch==source.CombatEpoch?burnTime:0;}
        public bool KnockedDown { get { return downTime > 0; } }
        public bool IsAirborne { get { return airborneTime > 0; } }
        public float AirborneHeight { get { return airborneTime <= 0 ? 0 : Mathf.Sin(Mathf.Clamp01(1f - airborneTime / airborneDuration) * Mathf.PI) * airborneHeight; } }
        public string Summary { get {
            string value = IsAirborne ? "浮空 " : downTime > 0 ? "击倒 " : frozenTime > 0 ? "冻结 " : enemy != null && enemy.IsStunned ? "眩晕 " : "";
            if (frostMarkTime > 0 && frozenTime <= 0) value += "霜痕 ";
            if (burnTime > 0) value += "灼烧 ";
            if (slowTime > 0) value += "减速 ";
            if (PoisonStacks > 0) value += "中毒×" + PoisonStacks + " ";
            if (markTime > 0) value += "锁定易伤 ";
            return value.Trim();
        } }
        private EnemyController enemy;
        private float slowTime, slowStrength, markTime, markStrength, poisonTime, poisonDamage, downTime, frozenTime;
        private float frostMarkTime, burnTime, burnDamage;
        private ScheduledTickWindow poisonSchedule, burnSchedule;
        private int burnEpoch, burnClockFrame=int.MinValue;
        private readonly BurnFinaleReceipts burnFinaleCasts=new BurnFinaleReceipts();
        private PlayerController burnSource;
        private readonly RecentCastGate meteorCasts = new RecentCastGate();
        private readonly RecentCastGate poisonCasts = new RecentCastGate();
        private PlayerController castOwner;
        private int castEpoch;
        private int poisonStacks, sourceEpoch;
        private PlayerController poisonSource;
        private Transform model;
        private CombatModel knockdownModel;
        private bool wasDown;
        private float airborneTime, airborneDuration, airborneHeight, airborneRecovery;

        internal event System.Action VisualStateChanged;
        private void NotifyVisualState(){var changed=VisualStateChanged;if(changed!=null)changed();}

        private void Awake() { enemy = GetComponent<EnemyController>(); }
        public void Slow(float duration, float strength)
        {
            slowTime = Mathf.Max(slowTime, duration);
            slowStrength = Mathf.Max(slowStrength, Mathf.Clamp(strength * (enemy.IsBoss ? .35f : 1), 0, .8f));
            enemy.Provoke();
        }
        public void Freeze(float duration)
        {
            if (enemy == null || enemy.IsDead || duration <= 0 || float.IsNaN(duration) || float.IsInfinity(duration)) return;
            float granted = enemy.ApplyControl(duration);
            if (!enemy.IsBoss && granted <= 0) granted = Mathf.Min(duration, enemy.ControlStunRemaining);
            if(granted>=frozenTime)frozenWindowDuration=granted;
            frozenTime = Mathf.Max(frozenTime, granted);
            // Boss armor blocks hard freeze, not the frost-mark/shatter opportunity.
            if (enemy.IsBoss) {if(duration+2f>=frostMarkTime)frostWindowDuration=duration+2f;frostMarkTime = Mathf.Max(frostMarkTime, duration + 2f);}
            Slow(duration + 2, .4f);
            NotifyVisualState();
        }
        public void FrostMark(float duration)
        {
            if (enemy == null || enemy.IsDead || duration <= 0) return;
            if(duration>=frostMarkTime)frostWindowDuration=duration;
            frostMarkTime = Mathf.Max(frostMarkTime, duration);
            Slow(duration, .15f);
            NotifyVisualState();
        }

        public bool TryShatter(PlayerController source, int castId)
        {
            if (source == null || source.IsDead || enemy == null || enemy.IsDead) return false;
            PrepareCastOwner(source);
            if (!meteorCasts.TryEnterEligible(castId, HasFrostMark)) return false;
            return ConsumeFrost();
        }

        public void Knockdown(float duration)
        {
            if (enemy == null || enemy.IsDead || duration <= 0 || float.IsNaN(duration) || float.IsInfinity(duration)) return;
            float granted = enemy.ApplyControl(duration);
            if (!enemy.IsBoss && granted <= 0) granted = Mathf.Min(duration, enemy.ControlStunRemaining);
            downTime = Mathf.Max(downTime, granted);
        }
        public void Knockup(float duration, float height)
        {
            if (enemy == null || enemy.IsDead || enemy.IsBoss || duration <= 0 || float.IsNaN(duration) || float.IsInfinity(duration) || IsAirborne || airborneRecovery > 0) return;
            float granted = enemy.ApplyControl(Mathf.Clamp(duration, .3f, 1.2f));
            if (granted <= 0) granted = Mathf.Min(1.2f, enemy.ControlStunRemaining);
            if (granted <= 0) return;
            airborneDuration = granted;
            airborneTime = airborneDuration;
            airborneHeight = Mathf.Clamp(height, .25f, 2f) * (enemy.IsBoss ? .18f : 1f);
            airborneRecovery = airborneDuration + (enemy.IsBoss ? 2.5f : enemy.Tier == EnemyController.ThreatTier.Elite ? 1.1f : .45f);
            enemy.Provoke();
        }
        public void Mark(float duration, float vulnerability)
        {
            markTime = Mathf.Max(markTime, duration);
            markStrength = Mathf.Max(markStrength, Mathf.Clamp(vulnerability, 0, .3f));
            NotifyVisualState();
            enemy.Provoke();
        }
        public void Poison(PlayerController source, float duration, float damagePerTick)
        {
            if (source == null || source.IsDead || enemy == null || enemy.IsDead || !FinitePositive(duration) || !FinitePositive(damagePerTick)) return;
            if (poisonSchedule == null || poisonSchedule.Complete || poisonSource != source || sourceEpoch != source.CombatEpoch)
            { poisonWindowDuration=duration;poisonStacks = 0; poisonDamage = 0; poisonSchedule = new ScheduledTickWindow(duration,StatusTickRates.Poison,StatusTickRates.Poison); }
            else {if(duration>=poisonSchedule.Remaining)poisonWindowDuration=duration;poisonSchedule.Refresh(duration);}
            poisonTime = poisonSchedule.Remaining;
            poisonStacks = Mathf.Min(3, poisonStacks + 1);
            poisonDamage = Mathf.Max(poisonDamage, damagePerTick);
            poisonSource = source; sourceEpoch = source.CombatEpoch;
            enemy.Provoke();
            ElementalCombatVfx.OnEnemy(enemy, ElementalCombatVfx.Element.Poison, duration);
        }
        private void PrepareCastOwner(PlayerController source)
        {
            if (castOwner == source && castEpoch == source.CombatEpoch) return;
            castOwner = source; castEpoch = source.CombatEpoch;
            meteorCasts.Clear(); poisonCasts.Clear();burnFinaleCasts.Clear();
        }

        public bool BeginMeteorImpact(PlayerController source, int castId)
        {
            if (source == null || source.IsDead || enemy == null || enemy.IsDead) return false;
            PrepareCastOwner(source);
            return meteorCasts.TryEnter(castId);
        }

        public bool ConsumeFrost()
        {
            if (!HasFrostMark) return false;
            frozenTime = frostMarkTime = 0;
            NotifyVisualState();
            return true;
        }

        public bool ConsumePoison(PlayerController source, int castId, out float storedDamage)
        {
            storedDamage = 0;
            if (source == null || PoisonStacks < 3 || poisonSource != source || sourceEpoch != source.CombatEpoch) return false;
            PrepareCastOwner(source);
            if (!poisonCasts.TryEnter(castId)) return false;
            storedDamage = poisonDamage * poisonStacks * PlayerUpgradeRules.PoisonDetonationTicks;
            poisonTime = poisonDamage = 0; poisonStacks = 0; if (poisonSchedule != null) poisonSchedule.Clear();
            ElementalCombatVfx.ClearPoison(enemy);
            return true;
        }

        public void Burn(PlayerController source, float duration, float totalDamage)
        {
            if (source == null || source.IsDead || enemy == null || enemy.IsDead || !FinitePositive(duration) || !FinitePositive(totalDamage)) return;
            bool refresh = burnSchedule != null && !burnSchedule.Complete && burnSource == source && burnEpoch == source.CombatEpoch;
            float previousRemaining = refresh ? burnSchedule.Remaining : 0;
            float previousBudget = refresh ? burnDamage * previousRemaining : 0;
            if (burnSchedule == null || burnSchedule.Complete || burnSource != source || burnEpoch != source.CombatEpoch)
            { burnWindowDuration=duration;burnDamage = 0; burnSchedule = new ScheduledTickWindow(duration,StatusTickRates.Burn,StatusTickRates.Burn);burnClockFrame=Time.frameCount; }
            else {if(duration>=burnSchedule.Remaining)burnWindowDuration=duration;burnSchedule.Refresh(duration);}
            burnTime = burnSchedule.Remaining;
            burnDamage = Mathf.Max(burnDamage, totalDamage / duration);
            burnSource = source; burnEpoch = source.CombatEpoch;
            if (refresh && (burnTime > previousRemaining + .0001f || burnDamage * burnTime > previousBudget + .0001f))
            {
                var game = GameSession.Instance;
                if (game != null) game.RecordClassTutorial(HeroClass.Arcanist);
            }
            enemy.Provoke();
            ElementalCombatVfx.OnEnemy(enemy, ElementalCombatVfx.Element.Fire, duration);
        }
        internal BurnFinalePlan BeginBurnFinale(PlayerController source,int castId)
        {
            var game=GameSession.Instance;
            if(source==null||source.IsDead||enemy==null||enemy.IsDead||
                game==null||game.InputBlocked||!ValidSource(source,source.CombatEpoch))return null;
            int expectedEpoch=source.CombatEpoch;
            PrepareCastOwner(source);
            if(!burnFinaleCasts.TryEnter(castId))return null;
            AdvanceBurnClock(Time.deltaTime);
            // Existing due events retain their original strength and individual damage/defence callbacks.
            // No future event is claimed until this finite backlog has been paid.
            while(burnSchedule!=null&&burnSchedule.HasDueTicks&&!enemy.IsDead&&ValidSource(source,expectedEpoch))
            {
                if(!burnSchedule.TryTakeDueTick(true))break;
                enemy.TakeDamage(burnDamage*StatusTickRates.Burn,Vector3.zero,impact:false);
            }
            if(enemy.IsDead||!ValidSource(source,expectedEpoch))return null;
            bool hadOwnBurn=burnSource==source&&burnEpoch==source.CombatEpoch&&burnSchedule!=null&&!burnSchedule.Complete&&burnSchedule.Remaining>0;
            return new BurnFinalePlan(this,source,expectedEpoch,hadOwnBurn);
        }
        internal sealed class BurnFinalePlan
        {
            internal readonly EnemyStatusEffects Status;internal readonly PlayerController Source;internal readonly int Epoch;internal readonly bool HadOwnBurn;internal bool Used;
            internal BurnFinalePlan(EnemyStatusEffects status,PlayerController source,int epoch,bool hadOwnBurn)
            {Status=status;Source=source;Epoch=epoch;HadOwnBurn=hadOwnBurn;}
        }
        internal BurnFinaleSettlement CompleteBurnFinale(BurnFinalePlan plan,float refreshedTotalDamage)
        {
            if(plan==null||plan.Status!=this||plan.Used)return null;plan.Used=true;
            if(enemy==null||enemy.IsDead||!FinitePositive(refreshedTotalDamage)||!ValidSource(plan.Source,plan.Epoch))return null;
            // Same-impact boons have now run. Merge them before claiming this impact's future events.
            var source=plan.Source;int expectedEpoch=plan.Epoch;
            Burn(source,3f,refreshedTotalDamage);
            if(!plan.HadOwnBurn)return null;
            float perTick=burnDamage*StatusTickRates.Burn;
            int claimed=burnSchedule.ClaimFutureTicks(3f);
            burnTime=burnSchedule.Remaining;
            if(burnSchedule.Complete)
            {burnTime=burnDamage=0;burnSource=null;ElementalCombatVfx.ClearFire(enemy);}
            return claimed>0?new BurnFinaleSettlement(this,source,expectedEpoch,claimed,perTick*claimed):null;
        }
        internal sealed class BurnFinaleSettlement
        {
            private readonly EnemyStatusEffects status;private readonly PlayerController source;private readonly int epoch;private readonly float amount;private bool applied;
            internal int Ticks {get;private set;}
            internal BurnFinaleSettlement(EnemyStatusEffects status,PlayerController source,int epoch,int ticks,float amount)
            {this.status=status;this.source=source;this.epoch=epoch;this.amount=amount;Ticks=ticks;}
            internal bool Apply()
            {
                if(applied)return false;applied=true;
                if(status==null||status.enemy==null||status.enemy.IsDead||!status.ValidSource(source,epoch))return false;
                // Already-claimed DOT budget: never re-enter spell-hit/crit/resource hooks.
                float before=status.enemy.Health;
                status.enemy.TakeDamage(amount,Vector3.zero,impact:false);
                return status.enemy.Health<before;
            }
        }
        internal BurnFinaleSettlement PrepareBurnFinale(PlayerController source,int castId,float refreshedTotalDamage)
        {return !FinitePositive(refreshedTotalDamage)?null:CompleteBurnFinale(BeginBurnFinale(source,castId),refreshedTotalDamage);}
        internal int ResolveBurnFinale(PlayerController source,int castId,float refreshedTotalDamage)
        {var settlement=PrepareBurnFinale(source,castId,refreshedTotalDamage);if(settlement==null)return 0;return settlement.Apply()?settlement.Ticks:0;}
        private void AdvanceBurnClock(float delta)
        {
            if(burnSchedule==null||burnSchedule.Complete)return;
            if(!ValidSource(burnSource,burnEpoch))
            {burnSchedule.Clear();burnTime=burnDamage=0;burnSource=null;return;}
            if(burnClockFrame==Time.frameCount)return;
            burnClockFrame=Time.frameCount;
            if(FinitePositive(delta)){burnSchedule.Elapse(delta,true);burnTime=burnSchedule.Remaining;}
        }
        private static bool FinitePositive(float value)
        { return value > 0 && !float.IsNaN(value) && !float.IsInfinity(value); }
        private bool ValidSource(PlayerController source,int epoch)
        { var game=GameSession.Instance;return source!=null&&!source.IsDead&&source.CombatEpoch==epoch&&game!=null&&game.HasStarted&&game.Player==source&&!game.CombatEnded; }
        private void Update()
        {
            if (enemy == null || enemy.IsDead) return;
            var game=GameSession.Instance;
            if(game==null||!game.HasStarted||game.InputBlocked)return;
            float dt = Time.deltaTime;
            if (!FinitePositive(dt)) return;
            slowTime = Mathf.Max(0, slowTime - dt);
            markTime = Mathf.Max(0, markTime - dt);
            downTime = Mathf.Max(0, downTime - dt);
            frozenTime = Mathf.Max(0, frozenTime - dt);
            frostMarkTime = Mathf.Max(0, frostMarkTime - dt);
            NotifyVisualState();
            // Both clocks see this frame before a damage callback can open a
            // choice menu. Unclaimed events stay queued until combat resumes.
            AdvanceBurnClock(dt);
            if (poisonSchedule != null && !poisonSchedule.Complete)
            {
                if (!ValidSource(poisonSource,sourceEpoch))
                { poisonSchedule.Clear(); poisonTime=poisonDamage=0; poisonStacks=0; poisonSource=null; }
                else { poisonSchedule.Elapse(dt,true); poisonTime=poisonSchedule.Remaining; }
            }
            for(int i=0;i<ScheduledTickWindow.MaximumCatchUp&&!enemy.IsDead&&!game.InputBlocked&&!game.CombatEnded&&burnSchedule!=null&&burnSchedule.TryTakeDueTick(true);i++)
                enemy.TakeDamage(burnDamage*StatusTickRates.Burn,Vector3.zero,impact:false);
            if(burnSchedule!=null&&burnSchedule.Complete){burnTime=burnDamage=0;burnSource=null;}
            airborneTime = Mathf.Max(0, airborneTime - dt);
            airborneRecovery = Mathf.Max(0, airborneRecovery - dt);
            if (slowTime <= 0) slowStrength = 0;
            if (markTime <= 0) markStrength = 0;
            int poisonDue=0;
            for(;poisonDue<ScheduledTickWindow.MaximumCatchUp&&!enemy.IsDead&&!game.InputBlocked&&!game.CombatEnded&&poisonSchedule!=null&&poisonSchedule.TryTakeDueTick(true);poisonDue++)
                enemy.TakeDamage(poisonDamage*poisonStacks,Vector3.zero,impact:false);
            if(poisonDue>0&&!enemy.IsDead)CombatFx.Ring(enemy.transform.position,.6f,new Color(.55f,1f,.28f),.35f,.06f);
            if(poisonSchedule!=null&&poisonSchedule.Complete){poisonTime=poisonDamage=0;poisonStacks=0;poisonSource=null;}

        }
        private void LateUpdate()
        {
            if (enemy == null || enemy.IsDead) return;
            if (model == null) { CombatModel found = GetComponentInChildren<CombatModel>(); if (found != null) {model = found.transform;knockdownModel=found;} }
            if (model == null) return;
            var game=GameSession.Instance;
            float dt=game!=null&&game.HasStarted&&!game.InputBlocked?Time.deltaTime:0;
            if(knockdownModel!=null&&knockdownModel.TryAnimateKnockdown(downTime,IsAirborne,dt)){wasDown=false;return;}
            if (downTime > 0 && !enemy.IsBoss) { model.localRotation = Quaternion.Euler(0, 0, 72); wasDown = true; }
            else if (wasDown) { model.localRotation = Quaternion.identity; wasDown = false; }
        }
    }
}
