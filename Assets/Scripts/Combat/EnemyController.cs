using UnityEngine;

namespace Emberfall
{
    public sealed class EnemyController : MonoBehaviour
    {
        public enum ThreatTier { Normal, Elite, Boss }
        public ThreatTier Tier { get; private set; }
        public bool IsAggro { get { return aggro; } }
        public bool IsStunned { get { return stunTime > 0; } }
        public float ControlStunRemaining { get { return stunTime; } }
        public EnemyStatusEffects StatusEffects { get; private set; }
        public float Health { get; private set; }
        public float MaxHealth { get; private set; }
        public bool IsDead { get { return Health <= 0; } }
        public EnemyKind Kind { get; private set; }
        public bool IsBoss { get; private set; }
        public bool IsEnraged { get { return IsBoss && BossAttackPolicy.IsEnraged(Health, MaxHealth); } }
        public bool IsPreparingAttack { get { return preparing || largeBoss != null && largeBoss.State.Interruptible; } }
        public bool CanBeSkillInterrupted { get { return IsPreparingAttack && controlPolicy != null && controlPolicy.CanInterruptWindup; } }
        public float ControlRecoveryRemaining { get { return controlPolicy == null ? 0 : controlPolicy.InterruptRecovery; } }
        public float AttackWindupRemaining { get { return largeBoss != null && largeBoss.State.Interruptible ? largeBoss.State.Remaining : preparing ? windup : 0; } }
        private GuardArmorVisual guardArmorVisual;
        public bool GuardArmorClosed {get{return GuardArmorRules.Closed(Kind==EnemyKind.Guardian,IsBoss,preparing);}}
        public float NavigationRadius { get { return largeBoss != null ? 1.25f : IsBoss ? .9f : Kind == EnemyKind.Guardian ? .6f : .45f; } }
        public float HitFootprintBonus { get { return largeBoss != null ? .45f : 0f; } }
        public float ProjectileHitRadius { get { return largeBoss != null ? 1.3f : IsBoss ? 1.05f : .6f; } }
        internal float AttackDamage { get { return damage; } }
        public string DisplayName { get; private set; }
        public string TraitDescription { get { return EscapePostDescription+(session!=null&&(session.IsRoomSupplier(this)||session.IsChapterSupplier(this))?"护援者：6米内可见同伴减伤30%；引开、遮挡或击杀可解除。" : largeBoss != null ? "大型远征首领：70%与35%生命召唤供能锚；青色符号表示可打断，红色扫射期间摧毁锚点。断能后核心暴露6秒，受到伤害增加35%。" : IsBoss ? "首领：危险边界始终橙红；青色符号表示可用控制技能打断，无符号时处于霸体恢复。打断后5秒免疫再次打断，击退大幅衰减。近身震地、中距冲锋、远距弹幕。" : Kind == EnemyKind.Slime ? "跳扑近身，黏液命中使你暂时减速。" : Kind == EnemyKind.Goblin ? "绕侧接近，近身后快速出刀并侧移。" : Kind == EnemyKind.Wisp ? "保持远距离游走，发射双重灵弹。" : "正面石甲减伤35%；重击蓄力时护甲失效。"); } }

        private enum AttackType { Melee, Bolt, Slam, Charge, Fan }
        private GameSession session;
        private ThreatAdmissionPolicy threatAdmission;
        private int threatMember;
        internal void ConfigureThreatAdmission(ThreatAdmissionPolicy policy,int member)
        {threatAdmission=policy;threatMember=member;}
        private bool AdmitThreatAttack()
        {return threatAdmission==null||threatAdmission.Request(threatMember,Time.time);}
        private CombatModel model;
        private LargeExpeditionBoss largeBoss;
        private float speed, damage, attackCooldown, windup, stunTime, hurtTime, attackAnimation, patrolPhase;
        private int attackNumber, repeatedMove, arenaBossPattern, pullBudgetFrame = -1;
        private float pullUsedThisFrame;
        private BossAttackPolicy.Move previousMove;
        private Vector3 walkingDisplacement;
        private readonly BossAdvanceBudget advanceBudget = new BossAdvanceBudget();
        private Vector3 origin, targetPoint, knockVelocity, chargeDirection;
        private float chargeTime, totalWindup, comboDelay;
        private int comboRemaining;
        private Vector3 attackOrigin, attackForward, chargeEnd;
        private EnemyAttackTelegraph telegraph;
        private bool dodgeRegistered, dodgePending;
        private Vector3 dodgeOrigin;
        private PlayerController dodgePlayer;
        private int dodgeEpoch;
        private float flinchUntil, nextImpactTime, nextFlinchAllowed;
        private EnemyControlPolicy controlPolicy;
        private PlayerController controlOwner;
        private int controlOwnerEpoch;
        private SummonedCompanion companionTarget;
        private float sidestepTime;
        private Vector3 sidestepDirection;
        private bool preparing, aggro, deathReported, chargeHit, activeChargePose;
        private AttackType attackType;
        private GameObject warning;
        private Transform healthRoot, healthFill;
        private Material healthBackgroundMaterial, healthFillMaterial;
        private readonly WorldTraversal.Route route = new WorldTraversal.Route();

        public void Initialize(GameSession game, EnemyKind kind, int level, bool boss = false)
        {
            session = game;
            Kind = kind;
            IsBoss = boss;
            Tier = boss ? ThreatTier.Boss : game.InDungeon ? ThreatTier.Elite : ThreatTier.Normal;
            controlPolicy = new EnemyControlPolicy((EnemyControlTier)(int)Tier);
            level = Mathf.Max(1,level);
            DisplayName = (Tier == ThreatTier.Boss ? "首领 · " : Tier == ThreatTier.Elite ? "精英 · " : "普通 · ") +
                (boss ? "星蚀巨像" : new[] { "森林史莱姆", "盗宝哥布林", "幽光魔灵", "遗迹守卫" }[(int)kind]);
            gameObject.name = DisplayName;
            float[] moveSpeed = { 2.05f, 3.1f, 2.5f, 2.1f };
            int challengeTier = game.InDungeon ? game.DungeonTier : 1;
            MaxHealth = CombatBalance.EnemyHealth(level, challengeTier, boss, kind);
            damage = CombatBalance.EnemyDamage(level, challengeTier, boss);
            if(game.ChapterActive)
            {MaxHealth*=ChapterDefinition.HealthMultiplier(game.ActiveChapterDifficulty);damage*=ChapterDefinition.DamageMultiplier(game.ActiveChapterDifficulty);}
            Health = MaxHealth;
            speed = boss ? 2.35f : moveSpeed[(int)kind];
            transform.position = WorldTraversal.NearestWalkable(transform.position, NavigationRadius);
            origin = transform.position;
            patrolPhase = Random.value * Mathf.PI * 2f;
            attackCooldown = Random.Range(.5f,1.2f);
            model = CombatModel.Enemy(transform,kind,boss);
            StatusEffects = gameObject.AddComponent<EnemyStatusEffects>();
            EnemyStatusVisual.Attach(this);
            BuildHealthBar();
            guardArmorVisual=GuardArmorVisual.Attach(this);
        }

        public void ConfigurePracticeTarget()
        { if(session==null||!session.PracticeActive)return;if(!session.PracticeRecord.UsesEnemyAI)MaxHealth=Health=1000000;DisplayName=session.PracticeRecord.HasSupplier&&Kind==EnemyKind.Wisp?"试招供能者 · 减伤30%":DisplayName+" · 试招"; }

        public void ConfigureArenaBoss(int pattern)
        {
            // A preference only: health/damage, navigation, windup and ordinary
            // pattern zero are unchanged. Call after spawning a Guardian boss.
            arenaBossPattern = IsBoss && pattern >= 1 && pattern <= 3 ? pattern : 0;
        }

        internal void ConfigureLargeExpedition(LargeExpeditionBoss encounter)
        {
            if (!IsBoss || largeBoss != null || encounter == null) return;
            largeBoss = encounter;
            DisplayName = "大型首领 · 星环执政官"; gameObject.name = DisplayName;
            if (model != null) { model.gameObject.SetActive(false); Destroy(model.gameObject); }
            model = CombatModel.LargeExpedition(transform);
            transform.position = WorldTraversal.NearestWalkable(transform.position, NavigationRadius);
            if (healthRoot != null) healthRoot.localPosition = Vector3.up * 4f;
        }
        internal void BeginLargeBossMechanic()
        {
            CancelAttack(); attackNumber++; aggro = true; knockVelocity = Vector3.zero;
        }

        internal void ApplyPull(Vector3 displacement)
        {
            if (session == null || !session.HasStarted || session.InputBlocked || IsDead || chargeTime > 0 || largeBoss != null && largeBoss.State.OwnsAttacks ||
                controlPolicy == null || !FinitePoint(displacement) || Time.deltaTime <= 0) return;
            if (pullBudgetFrame != Time.frameCount) { pullBudgetFrame = Time.frameCount; pullUsedThisFrame = 0; }
            Vector3 flat = CombatFx.Flat(displacement);
            float budget = controlPolicy.MaximumPullSpeed * Mathf.Min(.1f, Time.deltaTime);
            float distance = Mathf.Min(controlPolicy.PullDistance(flat.magnitude, Time.deltaTime), Mathf.Max(0, budget - pullUsedThisFrame));
            if (distance <= 0 || flat.sqrMagnitude < .000001f) return;
            pullUsedThisFrame += distance;
            transform.position = WorldTraversal.Move(transform.position, flat.normalized * distance, NavigationRadius);
        }

        private void BuildHealthBar()
        {
            GameObject root = new GameObject("Enemy Health");
            root.transform.SetParent(transform,false);
            root.transform.localPosition = Vector3.up * (IsBoss ? 4.2f : Kind == EnemyKind.Slime ? 1.45f : 2.6f);
            healthRoot = root.transform;
            float width = IsBoss ? 2.1f : 1.05f;
            healthBackgroundMaterial = new Material(Shader.Find("Unlit/Color"));
            healthBackgroundMaterial.color = new Color(.12f,.12f,.19f);
            healthFillMaterial = new Material(Shader.Find("Unlit/Color"));
            healthFillMaterial.color = ThreatColor();
            Transform background = HealthQuad("Background",healthBackgroundMaterial);
            background.localScale = new Vector3(width+.06f,.14f,1);
            healthFill = HealthQuad("Health",healthFillMaterial);
            healthFill.localPosition = new Vector3(0,0,-.012f);
            healthFill.localScale = new Vector3(width,.095f,1);
        }

        private Transform HealthQuad(string title, Material material)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            obj.name = title;
            Destroy(obj.GetComponent<Collider>());
            obj.transform.SetParent(healthRoot,false);
            obj.GetComponent<Renderer>().sharedMaterial = material;
            return obj.transform;
        }

        public void TakeDamage(float amount, Vector3 direction, float knockback = 0f, float stun = 0f, bool impact = true, bool critical = false, System.Action<float> actualHealthLoss = null, int practiceCastId = 0)
        {
            if (session == null || !AdventureResultPolicy.AcceptsDamage(session.HasStarted,session.CombatEnded) || IsDead || amount <= 0 || float.IsNaN(amount) || float.IsInfinity(amount)) return;
            amount *= session.RoomSupportMultiplier(this)*session.ChapterSupportMultiplier(this);
            amount *= StatusEffects == null ? 1 : StatusEffects.DamageMultiplier;
            if (largeBoss != null) amount *= largeBoss.State.IncomingMultiplier;
            float armorMultiplier=GuardArmorRules.Multiplier(Kind==EnemyKind.Guardian,IsBoss,preparing,CombatFx.Flat(direction).sqrMagnitude,Vector3.Dot(transform.forward,-CombatFx.Flat(direction).normalized));
            amount*=armorMultiplier;
            float previousHealth = Health;
            Health = Mathf.Max(0,Health-amount);
            if(session.PracticeActive)session.PracticeRecord.ConfirmedHealthLoss(previousHealth-Health,practiceCastId);
            if(actualHealthLoss!=null&&Health<previousHealth)actualHealthLoss(previousHealth-Health);
            if(guardArmorVisual!=null&&Health<previousHealth)guardArmorVisual.RecordImpact(armorMultiplier<1,preparing);
            if (CombatReviewEvents.Enabled) CombatReviewEvents.Emit("damage","0",CombatReviewObjectId.Get(this),previousHealth-Health,detail:"enemy_health_loss");
            aggro = true;
            hurtTime = .15f;
            if (impact && (critical || Time.time >= nextImpactTime))
            {
                nextImpactTime = Time.time + .10f;
                Vector3 push = CombatFx.Flat(direction).normalized;
                if (push.sqrMagnitude < .01f) push = -transform.forward;
                float strength = Mathf.Clamp(amount / Mathf.Max(1f, session.Progression.GetStats().Damage), critical ? 1.6f : .55f, critical ? 2.5f : 2f);
                model.Recoil(push, strength * (IsBoss ? .5f : 1f));
                if (Time.time >= nextFlinchAllowed)
                {
                    flinchUntil = Time.time + (IsBoss ? .018f : Mathf.Lerp(.035f, .065f, strength / 2f));
                    nextFlinchAllowed = Time.time + (IsBoss ? .8f : Tier == ThreatTier.Elite ? .35f : .22f);
                }
                HitFeedback.Spawn(transform.position + Vector3.up * (IsBoss ? 2f : Kind == EnemyKind.Slime ? .65f : 1.25f), push, strength, critical, priority:CombatVisualPriority.RealContact);
                GameAudio.Play(critical ? SoundCue.CriticalHit : SoundCue.Hit);
            }
            float impulse = chargeTime > 0 || largeBoss != null && largeBoss.State.OwnsAttacks ? 0 : controlPolicy.ApplyKnockback(knockback);
            if (FinitePoint(direction)) knockVelocity = Vector3.ClampMagnitude(knockVelocity + CombatFx.Flat(direction).normalized * impulse, controlPolicy.MaximumImpulse);
            float grantedStun = controlPolicy.ApplyStun(stun);
            stunTime = Mathf.Max(stunTime, grantedStun);
            if (grantedStun > 0 && stun >= .45f) CancelAttack(true);
            session.SpawnCombatDamage(transform.position+Vector3.up*(IsBoss?3.6f:1.9f),Mathf.CeilToInt(amount).ToString(),critical);
            if (Health <= 0 && !deathReported)
            {
                if (CombatReviewEvents.Enabled) CombatReviewEvents.Emit("enemydeath","0",CombatReviewObjectId.Get(this));
                deathReported = true;
                if (largeBoss != null) largeBoss.StopEncounter();
                CancelAttack();
                CombatFx.Ring(transform.position,IsBoss?2.5f:1.1f,new Color(1f,.77f,.35f),.45f,.13f);
                session.OnEnemyKilled(this);
            }
        }

        internal float ApplyControl(float duration)
        {
            if (IsDead || duration <= 0 || controlPolicy == null) return 0;
            aggro = true;
            float granted = controlPolicy.ApplyStun(duration);
            stunTime = Mathf.Max(stunTime, granted);
            if (granted > 0 && duration >= .45f) CancelAttack(true);
            return granted;
        }

        internal bool IsLargeBossCounterWindow {get{return largeBoss!=null&&largeBoss.State.Interruptible;}}
        internal bool TrySkillInterrupt(PlayerController source, int skill, int castId)
        {
            if (source == null || source.IsDead || session == null || source != session.Player || !session.HasStarted ||
                session.InputBlocked || IsDead || !enabled || !gameObject.activeInHierarchy || controlPolicy == null ||
                !EnemyControlPolicy.IsInterruptSkill(source.HeroClass, skill) ||
                !WorldTraversal.HasLineOfSight(source.transform.position, transform.position)) return false;
            if (controlOwner != source || controlOwnerEpoch != source.CombatEpoch)
            {
                controlOwner = source; controlOwnerEpoch = source.CombatEpoch;
                controlPolicy.ResetCastOwner();
            }
            float stagger;
            if (!controlPolicy.TryInterrupt(source.CaptureCastReceipt(castId), attackNumber, IsPreparingAttack,
                EnemyControlPolicy.IsInterruptSkill(source.HeroClass, skill), out stagger)) return false;
            stunTime = Mathf.Max(stunTime, stagger);
            bool specialWindup = largeBoss != null && largeBoss.State.Interruptible;
            CancelAttack(true);
            if (specialWindup) largeBoss.InterruptWindup();
            attackCooldown = Mathf.Max(attackCooldown, IsBoss ? 1.3f : .8f);
            session.SpawnMechanismText(transform.position + Vector3.up * (IsBoss ? 3.2f : 2f),
                "打断！", new Color(.35f, 1f, .85f));
            return true;
        }

        internal void Provoke() { if (!IsDead) aggro = true; }

        internal void BeginDeath()
        {
            if (healthRoot != null) healthRoot.gameObject.SetActive(false);
            if(model!=null&&model.TryBeginLargeBossShutdown()){gameObject.SetActive(false);Destroy(gameObject);return;}
            if (model != null && GetComponent<EnemyDeathDissolve>() == null)
                gameObject.AddComponent<EnemyDeathDissolve>().Initialize(model, Kind == EnemyKind.Slime, IsBoss);
            else if (model == null) Destroy(gameObject);
        }

        private void Update()
        {
            if(threatAdmission!=null&&session!=null&&(session.InputBlocked||session.Paused))threatAdmission.Withdraw(threatMember);
            if (session == null || session.Player == null || IsDead || !session.HasStarted || session.Paused || session.IsDead) return;
            if (largeBoss != null && (session.InputBlocked || largeBoss.State.Phase == LargeBossPhase.Finished)) return;
            if (session.InputBlocked) return;
            if(session.PracticeActive)
            {
                walkingDisplacement=Vector3.zero;
                Vector3 beforeWalking=transform.position;
                if(session.MovePracticeTarget(this))
                {
                    // Record only traversal-accepted active walking before knockback.
                    // This feeds the ordinary SetLocomotion adapter without changing movement.
                    walkingDisplacement=CombatFx.Flat(transform.position-beforeWalking);
                    transform.position=WorldTraversal.Move(transform.position,knockVelocity*Time.deltaTime,NavigationRadius);
                    knockVelocity=Vector3.Lerp(knockVelocity,Vector3.zero,Mathf.Min(1,Time.deltaTime*12f));
                    stunTime=Mathf.Max(0,stunTime-Time.deltaTime);hurtTime=Mathf.Max(0,hurtTime-Time.deltaTime);
                    controlPolicy.Advance(Time.deltaTime);
                    float acceptedSpeed=Time.deltaTime>0?walkingDisplacement.magnitude/Time.deltaTime:0;
                    AnimateModel(acceptedSpeed/Mathf.Max(.1f,speed),0,hurtTime>0);
                    return;
                }
            }
            float dt = Time.deltaTime;
            if (dt <= 0) return;
            walkingDisplacement = Vector3.zero;
            controlPolicy.Advance(dt);
            if (largeBoss != null && largeBoss.Tick(dt))
            {
                stunTime = Mathf.Max(0, stunTime - dt); hurtTime = Mathf.Max(0, hurtTime - dt);
                AnimateModel(0, largeBoss.State.Interruptible ? .95f : 0, hurtTime > 0);
                return;
            }
            if (telegraph != null) telegraph.SetInterruptible(CanBeSkillInterrupted);
            hurtTime = Mathf.Max(0,hurtTime-dt);
            attackAnimation = Mathf.Max(0,attackAnimation-dt*3f);
            attackCooldown = Mathf.Max(0,attackCooldown-dt);
            stunTime = Mathf.Max(0,stunTime-dt);
            // A charging boss follows the warned straight corridor instead of sliding sideways.
            if (chargeTime <= 0) transform.position = WorldTraversal.Move(transform.position, knockVelocity * dt, NavigationRadius);
            knockVelocity = Vector3.Lerp(knockVelocity,Vector3.zero,Mathf.Min(1,dt*12f));
            if (!preparing && chargeTime <= 0)
                companionTarget = aggro || Tier != ThreatTier.Normal ? SummonedCompanion.ThreatTarget(this, session.Player.transform.position) : null;
            if (companionTarget != null && !companionTarget.IsAlive) companionTarget = null;
            if (preparing && CombatFx.Flat(transform.position - attackOrigin).sqrMagnitude > .0001f) CreateWarning();
            Vector3 combatTargetPosition = companionTarget != null ? companionTarget.transform.position : session.Player.transform.position;
            Vector3 delta = CombatFx.Flat(combatTargetPosition-transform.position);
            float distance = delta.magnitude;
            float effectiveSpeed = speed * (StatusEffects == null ? 1 : StatusEffects.MoveMultiplier);
            // Ordinary wildlife stays neutral regardless of proximity. Damage/control
            // explicitly provokes retaliation; only elites and bosses acquire on sight.
            if (Tier != ThreatTier.Normal && (session.InDungeon || distance < (IsBoss?15f:9f))) aggro = true;
            if (!session.InDungeon && distance > 17f) aggro = false;
            healthFillMaterial.color = ThreatColor();
            if (stunTime > 0 || Time.time < flinchUntil)
            {
                AnimateModel(0,attackAnimation,hurtTime>0);
                ClampPosition();
                return;
            }
            if (RegroupMobileSupport(dt,effectiveSpeed)) return;
            if (ReturnToEscapePost(dt,effectiveSpeed,combatTargetPosition)) return;
            if (sidestepTime > 0)
            {
                sidestepTime -= dt;
                transform.position = WalkForAnimation(sidestepDirection * effectiveSpeed * 1.6f * dt);
                AnimateModel(1, attackAnimation, hurtTime > 0);
                ClampPosition(); return;
            }
            if (chargeTime > 0)
            {
                Vector3 previous = transform.position;
                Vector3 next = Vector3.MoveTowards(previous, chargeEnd, BossAttackPolicy.ChargeSpeed * dt);
                // Never use sliding movement here: the visible corridor is the entire attack path.
                if (WorldTraversal.HasGroundPath(previous, next, NavigationRadius)) transform.position = next;
                else chargeEnd = previous;
                chargeTime = CombatFx.Flat(chargeEnd - transform.position).magnitude / BossAttackPolicy.ChargeSpeed;
                if (!chargeHit && CombatFx.SegmentDistance(combatTargetPosition,previous,transform.position) < BossAttackPolicy.ChargeHalfWidth && WorldTraversal.HasGroundPath(transform.position, combatTargetPosition, .12f))
                {
                    DamageTarget(damage*1.35f);
                    chargeHit = true;
                    dodgePending = false;
                }
                ConfirmChargeDodge(previous, transform.position);
                AnimateModel(1,.6f,hurtTime>0);
                if (chargeTime <= .001f)
                {
                    chargeTime = 0;
                    dodgePending = false;
                    FinishAttack();
                }
            }
            else if (preparing)
            {
                windup -= dt;
                if (telegraph != null) telegraph.SetProgress(1f - windup / Mathf.Max(.01f, totalWindup));
                if (windup <= 0) ResolveAttack();
                AnimateModel(0,preparing?.95f:attackAnimation,hurtTime>0);
            }
            else if (comboDelay > 0)
            {
                comboDelay = Mathf.Max(0, comboDelay - dt);
                AnimateModel(0, attackAnimation, hurtTime > 0);
                if (comboDelay <= 0) BeginComboAttack(distance, combatTargetPosition);
            }
            else if (aggro)
            {
                if (delta.sqrMagnitude>.01f) transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(delta),dt*9f);
                bool approach = IsBoss && advanceBudget.Advance(dt, distance, BossAttackPolicy.PreferredApproach(arenaBossPattern, distance, previousMove, repeatedMove, CanUseBossAttack(BossAttackPolicy.Move.Slam, combatTargetPosition)));
                float range = IsBoss ? (approach ? BossAttackPolicy.ChargeRange : BossAttackPolicy.EngageRange) : Kind==EnemyKind.Wisp ? 7.5f : Kind==EnemyKind.Guardian ? 2.5f : 1.8f;
                if (IsBoss && arenaBossPattern == 1 && !advanceBudget.FallbackActive) range = Mathf.Min(range, BossAttackPolicy.CloseRange);
                bool attackPath = IsBoss ? CanUseBossAttack(SelectBossMove(distance, combatTargetPosition), combatTargetPosition) : Kind == EnemyKind.Wisp ? WorldTraversal.HasLineOfSight(transform.position, combatTargetPosition) : WorldTraversal.HasGroundPath(transform.position, combatTargetPosition, .12f);
                bool ready = distance <= range && attackCooldown <= 0 && attackPath;
                if (!ready && threatAdmission != null) threatAdmission.Withdraw(threatMember);
                if (ready && BeginAttack()) { }
                else if (Kind==EnemyKind.Wisp && distance<4.5f && attackPath)
                {
                    transform.position = WalkForAnimation(-delta.normalized * effectiveSpeed * dt);
                    AnimateModel(.7f,attackAnimation,hurtTime>0);
                }
                else if (distance > range*.82f || !attackPath)
                {
                    bool directGround = WorldTraversal.HasGroundPath(transform.position, combatTargetPosition, NavigationRadius);
                    Vector3 step = route.Direction(transform.position, combatTargetPosition, NavigationRadius) + Separation() * (directGround ? 1f : .15f);
                    if (directGround && Kind == EnemyKind.Goblin && distance > 2.5f && distance < 9f)
                        step += Vector3.Cross(Vector3.up, delta.normalized) * Mathf.Sin(patrolPhase + Time.time * .8f) * .8f;
                    transform.position = WalkForAnimation(Vector3.ClampMagnitude(step, 1.2f) * effectiveSpeed * dt);
                    AnimateModel(1,attackAnimation,hurtTime>0);
                }
                else AnimateModel(0,attackAnimation,hurtTime>0);
            }
            else
            {
                Vector3 patrol = origin + new Vector3(Mathf.Sin(Time.time*.28f+patrolPhase),0,Mathf.Cos(Time.time*.28f+patrolPhase)) * 1.4f;
                Vector3 toPatrol = CombatFx.Flat(patrol-transform.position);
                Vector3 patrolDirection = route.Direction(transform.position, patrol, NavigationRadius);
                transform.position = WalkForAnimation(patrolDirection * Mathf.Min(1, toPatrol.magnitude) * effectiveSpeed * .22f * dt);
                if(toPatrol.sqrMagnitude>.1f) transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(toPatrol),dt*2f);
                AnimateModel(.2f,0,false);
            }
            ClampPosition();
        }

        private Vector3 WalkForAnimation(Vector3 displacement)
        {
            Vector3 next = WorldTraversal.Move(transform.position,displacement,NavigationRadius);
            // Only this explicit three-member roster is tethered. Knockback still uses authoritative traversal directly.
            if(mobileSupplier!=null&&!mobileSupplier.IsDead&&mobileSupplier.isActiveAndEnabled&&
                (CombatFx.Flat(next-mobileSupplier.transform.position).sqrMagnitude>27.04f||!WorldTraversal.HasLineOfSight(next,mobileSupplier.transform.position)))
                next=WorldTraversal.Move(transform.position,route.Direction(transform.position,mobileSupplier.transform.position,NavigationRadius)*displacement.magnitude,NavigationRadius);
            Vector3 actual=CombatFx.Flat(next-transform.position);
            walkingDisplacement += actual;
            if(escapePost!=null&&!preparing&&chargeTime<=0&&actual.sqrMagnitude>.000001f)
                escapeChaseMovement+=Mathf.Max(0,Time.deltaTime);
            return next;
        }
        private void AnimateModel(float speedHint,float attack,bool hurt)
        {
            model.SetLocomotion(transform.InverseTransformDirection(walkingDisplacement),Time.deltaTime,Mathf.Max(.1f,speed),true);
            model.SetEnemyAttackPose(EnemyActionPose.Select(preparing,activeChargePose,attackAnimation),
                preparing?1-windup/Mathf.Max(.01f,totalWindup):1-attackAnimation);
            model.Animate(speedHint,attack,hurt);
        }

        private Vector3 Separation()
        {
            Vector3 force=Vector3.zero;
            for (int i=0;i<session.Enemies.Count;i++)
            {
                EnemyController enemy=session.Enemies[i];
                if(enemy==null || enemy==this || enemy.IsDead) continue;
                Vector3 away=CombatFx.Flat(transform.position-enemy.transform.position);
                float distance=away.magnitude;
                if(distance>.01f && distance<1.35f) force+=away.normalized*(1.35f-distance)*1.3f;
            }
            return Vector3.ClampMagnitude(force,.9f);
        }

        private Color ThreatColor()
        {
            if (Tier == ThreatTier.Boss) return new Color(1f, .3f, .25f);
            if (Tier == ThreatTier.Elite) return new Color(1f, .72f, .25f);
            return aggro ? new Color(1f, .46f, .27f) : new Color(.4f, .87f, .6f);
        }

        private static AttackType ToAttack(BossAttackPolicy.Move move)
        {
            return move == BossAttackPolicy.Move.Slam ? AttackType.Slam : move == BossAttackPolicy.Move.Charge ? AttackType.Charge : AttackType.Fan;
        }

        private static BossAttackPolicy.Move ToMove(AttackType type)
        {
            return type == AttackType.Slam ? BossAttackPolicy.Move.Slam : type == AttackType.Charge ? BossAttackPolicy.Move.Charge : BossAttackPolicy.Move.Fan;
        }

        private bool CanUseBossAttack(BossAttackPolicy.Move move, Vector3 target)
        {
            if (!BossAttackPolicy.InRange(move, CombatFx.Flat(target - transform.position).magnitude)) return false;
            if (move == BossAttackPolicy.Move.Fan) return WorldTraversal.HasLineOfSight(transform.position, target);
            if (!WorldTraversal.HasGroundPath(transform.position, target, .12f)) return false;
            if (move != BossAttackPolicy.Move.Charge) return true;
            Vector3 forward = CombatFx.Flat(target - transform.position).normalized;
            Vector3 end = ClipPath(transform.position, target + forward, true);
            return CombatFx.SegmentDistance(target, transform.position, end) < BossAttackPolicy.ChargeHalfWidth;
        }

        private BossAttackPolicy.Move SelectBossMove(float distance, Vector3 target)
        {
            BossAttackPolicy.Move fallback = BossAttackPolicy.Select(distance, previousMove, repeatedMove);
            BossAttackPolicy.Move preferred = ArenaBossPatternPolicy.Preferred(arenaBossPattern, distance, previousMove, repeatedMove);
            BossAttackPolicy.Move selected = preferred != fallback && !CanUseBossAttack(preferred, target) ? fallback : preferred;
            return BossAttackPolicy.LegalFallback(selected, distance, advanceBudget.FallbackActive,
                CanUseBossAttack(BossAttackPolicy.Move.Charge, target), CanUseBossAttack(BossAttackPolicy.Move.Fan, target));
        }

        private bool BeginAttack()
        {
            Vector3 target = companionTarget != null ? companionTarget.transform.position : session.Player.transform.position;
            comboRemaining = IsEnraged ? 1 : 0;
            AttackType next = IsBoss ? ToAttack(SelectBossMove(CombatFx.Flat(target - transform.position).magnitude, target)) : Kind == EnemyKind.Wisp ? AttackType.Bolt : Kind == EnemyKind.Guardian ? AttackType.Slam : AttackType.Melee;
            return PrepareAttack(next, false, target);
        }

        private void BeginComboAttack(float distance, Vector3 target)
        {
            BossAttackPolicy.Move next = BossAttackPolicy.FollowUp(ToMove(attackType), distance);
            comboRemaining = 0;
            if (!aggro || !BossAttackPolicy.CanEngage(distance) || !CanUseBossAttack(next, target))
            {
                attackCooldown = BossAttackPolicy.Recovery(true);
                return;
            }
            PrepareAttack(ToAttack(next), true, target);
        }

        private bool PrepareAttack(AttackType type, bool followUp, Vector3 target)
        {
            if (!AdmitThreatAttack()) return false;
            advanceBudget.Reset();
            attackNumber++;
            if (IsBoss) { BossAttackPolicy.Move move = ToMove(type); repeatedMove = repeatedMove > 0 && previousMove == move ? repeatedMove+1 : 1; previousMove = move; }
            attackType = type;
            preparing = true;
            dodgeRegistered = dodgePending = false;
            chargeHit = false;
            dodgePlayer = null;
            targetPoint = type == AttackType.Slam ? transform.position : target;
            windup = IsBoss ? BossAttackPolicy.Windup(ToMove(type), followUp) : Kind == EnemyKind.Wisp ? .72f : Kind == EnemyKind.Guardian ? .85f : Kind == EnemyKind.Slime ? .6f : .48f;
            totalWindup = windup;
            CreateWarning();
            if (IsBoss) session.SpawnMechanismText(transform.position + Vector3.up * 3.1f,
                CanBeSkillInterrupted ? "青色符号 · 可打断" : "无打断符号 · 霸体恢复", CanBeSkillInterrupted ? new Color(.35f, 1f, .85f) : new Color(1f, .48f, .25f));
            return true;
        }

        private float ImpactRadius { get { return attackType == AttackType.Slam ? (IsBoss ? BossAttackPolicy.SlamRadius : 3f) : 1.55f; } }

        private void CreateWarning()
        {
            ClearWarning();
            attackOrigin = transform.position;
            attackForward = CombatFx.Flat(targetPoint - attackOrigin).normalized;
            if (attackForward.sqrMagnitude < .01f) attackForward = transform.forward;
            if (attackType == AttackType.Charge)
            {
                chargeDirection = attackForward;
                float length = Mathf.Clamp(CombatFx.Flat(targetPoint - attackOrigin).magnitude + 1f, 2.2f, 9.5f);
                chargeEnd = ClipPath(attackOrigin, attackOrigin + chargeDirection * length, true);
                telegraph = EnemyAttackTelegraph.Charge(attackOrigin, chargeEnd, BossAttackPolicy.ChargeHalfWidth);
            }
            else if (attackType == AttackType.Fan || attackType == AttackType.Bolt)
            {
                int count = attackType == AttackType.Fan ? 5 : 2;
                var directions = new Vector3[count];
                var lengths = new float[count];
                Vector3 muzzle = attackOrigin + attackForward * (attackType == AttackType.Fan ? 1f : .7f);
                float range = attackType == AttackType.Fan ? 21f : 22.5f;
                for (int i = 0; i < count; i++)
                {
                    float angle = attackType == AttackType.Fan ? (i - 2) * 17f : i == 0 ? -7f : 7f;
                    directions[i] = Quaternion.Euler(0, angle, 0) * attackForward;
                    lengths[i] = WorldTraversal.HasLineOfSight(attackOrigin, muzzle) ? CombatFx.Flat(ClipPath(muzzle, muzzle + directions[i] * range, false) - muzzle).magnitude : 0;
                }
                telegraph = EnemyAttackTelegraph.Fan(muzzle, directions, lengths, .78f);
            }
            else telegraph = EnemyAttackTelegraph.Circle(targetPoint, ImpactRadius, transform);
            warning = telegraph.gameObject;
            telegraph.SetInterruptible(CanBeSkillInterrupted);
            telegraph.SetProgress(1f - windup / Mathf.Max(.01f, totalWindup));
        }

        private Vector3 ClipPath(Vector3 start, Vector3 end, bool ground)
        {
            bool clear = ground ? WorldTraversal.HasGroundPath(start, end, NavigationRadius) : WorldTraversal.HasLineOfSight(start, end);
            if (clear) return end;
            float low = 0, high = 1;
            for (int i = 0; i < 12; i++)
            {
                float middle = (low + high) * .5f;
                Vector3 point = Vector3.Lerp(start, end, middle);
                if (ground ? WorldTraversal.HasGroundPath(start, point, NavigationRadius) : WorldTraversal.HasLineOfSight(start, point)) low = middle;
                else high = middle;
            }
            return Vector3.Lerp(start, end, low);
        }

        private void ResolveAttack()
        {
            if (!preparing || IsDead || session == null || !session.HasStarted) return;
            preparing = false;
            ClearWarning();
            attackAnimation = 1f;
            if (attackType == AttackType.Charge)
            {
                chargeHit = false;
                chargeTime = CombatFx.Flat(chargeEnd - transform.position).magnitude / BossAttackPolicy.ChargeSpeed;
                // Own the whole swept attack, including its final damage frame.
                // The short contact/recovery animation is not the charge lifetime.
                activeChargePose = chargeTime > .001f;
                if (chargeTime <= .001f) { dodgePending = false; FinishAttack(); }
                return;
            }
            if (attackType == AttackType.Bolt || attackType == AttackType.Fan)
            {
                Vector3 muzzle = transform.position + attackForward * (attackType == AttackType.Fan ? 1f : .7f);
                if (WorldTraversal.HasLineOfSight(transform.position, muzzle))
                {
                    if (attackType == AttackType.Fan)
                    {
                        for (int i = -2; i <= 2; i++) CombatProjectile.Hostile(session, muzzle, Quaternion.Euler(0, i * 17, 0) * attackForward, damage, 7f, sourceName: DisplayName);
                    }
                    else
                    {
                        System.Action volleyEnded=threatAdmission==null?null:threatAdmission.LaunchVolley(threatMember,2);
                        CombatProjectile.Hostile(session, muzzle, Quaternion.Euler(0, -7, 0) * attackForward, damage * .75f, 7.5f, sourceName: DisplayName,onEnded:volleyEnded);
                        CombatProjectile.Hostile(session, muzzle, Quaternion.Euler(0, 7, 0) * attackForward, damage * .75f, 7.5f, sourceName: DisplayName,onEnded:volleyEnded);
                    }
                }
            }
            else
            {
                if (attackType == AttackType.Melee && Kind == EnemyKind.Slime)
                    transform.position = WorldTraversal.Move(transform.position, Vector3.ClampMagnitude(CombatFx.Flat(targetPoint - transform.position), 1.25f), NavigationRadius);
                CombatFx.Ring(targetPoint, ImpactRadius, new Color(1f,.45f,.25f), .32f, .15f);
                ConfirmImpactDodge();
                if (attackType == AttackType.Slam)
                {
                    if (InsideImpact(session.Player.transform.position)) session.Player.TakeDamageFrom(damage * 1.4f, DisplayName);
                    foreach (SummonedCompanion ally in SummonedCompanion.Snapshot(session.Player))
                        if (ally != null && ally.IsAlive && InsideImpact(ally.transform.position)) ally.TakeDamage(damage * 1.4f, areaAttack: true);
                }
                Vector3 victim = companionTarget != null && companionTarget.IsAlive ? companionTarget.transform.position : session.Player.transform.position;
                if (attackType != AttackType.Slam && InsideImpact(victim))
                {
                    float previousHealth = session.Player.Health;
                    DamageTarget(damage * (attackType == AttackType.Slam ? 1.4f : 1f));
                    if (Kind == EnemyKind.Slime && companionTarget == null && session.Player.Health < previousHealth) session.Player.ApplySlow(1.8f, .35f);
                }
                if (Kind == EnemyKind.Goblin)
                {
                    sidestepTime = .28f;
                    sidestepDirection = Vector3.Cross(Vector3.up, transform.forward) * (attackNumber % 2 == 0 ? 1f : -1f);
                }
            }
            dodgePending = false;
            FinishAttack();
        }

        private void FinishAttack()
        {
            if(threatAdmission!=null)threatAdmission.Finish(threatMember);
            activeChargePose = false;
            if (IsBoss && attackType == AttackType.Charge && !chargeHit) { comboRemaining = 0; attackCooldown = 2.1f; return; }
            if (IsBoss && comboRemaining > 0) { comboDelay = BossAttackPolicy.ComboGap; attackCooldown = 0; }
            else attackCooldown = IsBoss ? BossAttackPolicy.Recovery(IsEnraged) : Kind == EnemyKind.Wisp ? 1.55f : 1.3f;
        }

        private bool InsideImpact(Vector3 point)
        {
            return EnemyImpactRegion.Contains(transform.position,targetPoint,point,ImpactRadius);
        }

        /// <summary>Register only a successful blink out of an imminent actual hit. Resolution confirms the reward.</summary>
        public bool TryRegisterPerfectDodge(Vector3 origin, Vector3 destination, float timingWindow = .22f)
        {
            if (session == null || session.Player == null || session.Player.IsDead || IsDead || !enabled || !gameObject.activeInHierarchy || !session.HasStarted || session.Paused || session.IsDead || dodgeRegistered || stunTime > 0 || Time.time < flinchUntil || timingWindow <= 0 || float.IsNaN(timingWindow) || float.IsInfinity(timingWindow)) return false;
            if (attackType != AttackType.Slam && companionTarget != null && companionTarget.IsAlive) return false;
            if (!FinitePoint(origin) || !FinitePoint(destination) || CombatFx.Flat(destination - origin).sqrMagnitude < .01f) return false;
            timingWindow = Mathf.Min(.3f, timingWindow);
            if (attackType == AttackType.Charge)
            {
                if ((!preparing && chargeTime <= 0) || chargeHit) return false;
                if (CombatFx.SegmentDistance(origin, transform.position, chargeEnd) >= BossAttackPolicy.ChargeHalfWidth || CombatFx.SegmentDistance(destination, transform.position, chargeEnd) < BossAttackPolicy.ChargeHalfWidth + .05f) return false;
                Vector3 relative = CombatFx.Flat(origin - transform.position);
                float along = Vector3.Dot(relative, chargeDirection);
                float lateral = Vector3.Cross(relative, chargeDirection).magnitude;
                float untilContact = BossAttackPolicy.ChargeContactDelay(along, lateral, preparing ? windup : 0);
                if (!BossAttackPolicy.IsPerfectDodgeTiming(untilContact, timingWindow) || !WorldTraversal.HasGroundPath(transform.position, origin, .12f)) return false;
            }
            else
            {
                if (!preparing || !BossAttackPolicy.IsPerfectDodgeTiming(windup, timingWindow) || (attackType != AttackType.Melee && attackType != AttackType.Slam) || !InsideImpact(origin) || InsideImpact(destination)) return false;
            }
            dodgeRegistered = dodgePending = true;
            dodgeOrigin = origin;
            dodgePlayer = session.Player;
            dodgeEpoch = dodgePlayer.CombatEpoch;
            return true;
        }

        private static bool FinitePoint(Vector3 point)
        {
            return !float.IsNaN(point.x) && !float.IsInfinity(point.x) && !float.IsNaN(point.z) && !float.IsInfinity(point.z);
        }

        private bool PendingDodgeIsValid()
        {
            return dodgePending && dodgePlayer != null && session.Player == dodgePlayer && !dodgePlayer.IsDead && dodgePlayer.CombatEpoch == dodgeEpoch && (attackType == AttackType.Slam || companionTarget == null || !companionTarget.IsAlive);
        }

        private void ConfirmImpactDodge()
        {
            if (PendingDodgeIsValid() && InsideImpact(dodgeOrigin) && !InsideImpact(dodgePlayer.transform.position))
                dodgePlayer.NotifyPerfectDodge();
            dodgePending = false;
        }

        private void ConfirmChargeDodge(Vector3 previous, Vector3 current)
        {
            if (!PendingDodgeIsValid() || chargeHit) return;
            if (CombatFx.SegmentDistance(dodgeOrigin, previous, current) >= BossAttackPolicy.ChargeHalfWidth || !WorldTraversal.HasGroundPath(previous, dodgeOrigin, .12f)) return;
            if (CombatFx.SegmentDistance(dodgePlayer.transform.position, previous, chargeEnd) >= BossAttackPolicy.ChargeHalfWidth)
                dodgePlayer.NotifyPerfectDodge();
            dodgePending = false;
        }

        private void DamageTarget(float amount)
        {
            Vector3 victim = companionTarget != null && companionTarget.IsAlive ? companionTarget.transform.position : session.Player.transform.position;
            if (!WorldTraversal.HasGroundPath(transform.position, victim, .12f)) return;
            if (companionTarget != null && companionTarget.IsAlive) companionTarget.TakeDamage(amount);
            else session.Player.TakeDamageFrom(amount, DisplayName);
        }

        private void ClearWarning()
        {
            if (warning != null) Destroy(warning);
            warning = null;
            telegraph = null;
        }

        private void CancelAttack(bool interrupted = false)
        {
            if(threatAdmission!=null){threatAdmission.Withdraw(threatMember);if(preparing||IsDead)threatAdmission.Release(threatMember);}
            bool activeAttack = preparing || chargeTime > 0 || largeBoss != null && largeBoss.State.Interruptible;
            preparing = false;
            activeChargePose = false;
            windup = chargeTime = comboDelay = sidestepTime = 0;
            comboRemaining = 0;
            StopAllCoroutines();
            CancelInvoke();
            dodgePending = false;
            dodgePlayer = null;
            attackCooldown = Mathf.Max(attackCooldown,.55f);
            ClearWarning();
            if (interrupted && activeAttack && session != null && !IsDead) session.OnEnemyInterrupted(this);
        }

        private void ClampPosition()
        {
            transform.position = WorldTraversal.Move(transform.position, Vector3.zero, NavigationRadius);
        }

        private EnemyController mobileSupplier,mobileFirst,mobileSecond;
        internal void ConfigureMobileSupport(EnemyController supplier,EnemyController first,EnemyController second)
        {mobileSupplier=supplier;mobileFirst=first;mobileSecond=second;}
        private bool RegroupMobileSupport(float dt,float speed)
        {
            if(mobileSupplier!=null&&!mobileSupplier.IsDead&&mobileSupplier.isActiveAndEnabled&&(aggro||mobileSupplier.IsAggro))
            {aggro=true;mobileSupplier.Provoke();}
            bool firstAlive=mobileFirst!=null&&!mobileFirst.IsDead&&mobileFirst.isActiveAndEnabled;
            bool secondAlive=mobileSecond!=null&&!mobileSecond.IsDead&&mobileSecond.isActiveAndEnabled;
            if((firstAlive||secondAlive)&&(aggro||firstAlive&&mobileFirst.IsAggro||secondAlive&&mobileSecond.IsAggro))
            {aggro=true;if(firstAlive)mobileFirst.Provoke();if(secondAlive)mobileSecond.Provoke();}
            if(preparing||chargeTime>0)return false;
            EnemyController partner=mobileFirst!=null&&!mobileFirst.IsDead&&mobileFirst.isActiveAndEnabled?mobileFirst:
                mobileSecond!=null&&!mobileSecond.IsDead&&mobileSecond.isActiveAndEnabled?mobileSecond:null;
            if(partner==null)return false;
            // The ranged provider follows the melee contest group rather than staying at its normal 7.5m range.
            Vector3 point=partner.transform.position;
            if(mobileSecond!=null&&mobileSecond!=partner&&!mobileSecond.IsDead&&mobileSecond.isActiveAndEnabled)
                point=(point+mobileSecond.transform.position)*.5f;
            if(CombatFx.Flat(transform.position-point).sqrMagnitude<=4f&&WorldTraversal.HasLineOfSight(transform.position,point))return false;
            Vector3 direction=route.Direction(transform.position,point,NavigationRadius);
            transform.position=WalkForAnimation(direction*speed*dt);
            AnimateModel(1,attackAnimation,hurtTime>0);ClampPosition();return true;
        }

        private string EscapePostDescription
        {get{return escapePost==null?"":escapePost.Role==EscapeRole.GateGuard?"守门：短追后返岗；可引离金环。 ":escapePost.Role==EscapeRole.Pursuer?"追击：持续追踪入侵者。 ":escapePost.Role==EscapeRole.GateSupplier?"北门供能：留守门组。 ":"侧线：守住侧路，远离后返回。 ";}}
        private EscapePostPolicy escapePost;
        private Vector3 escapePostPosition;
        private float escapeChaseMovement;
        internal void ConfigureEscapePost(EscapeRole role,Vector3 position)
        {escapePost=new EscapePostPolicy(role);escapePostPosition=position;}
        private bool ReturnToEscapePost(float dt,float speed,Vector3 target)
        {
            float moved=escapeChaseMovement;escapeChaseMovement=0;
            if(escapePost==null||!escapePost.ReturnToPost(dt,CombatFx.Flat(transform.position-escapePostPosition).magnitude,
                CombatFx.Flat(target-escapePostPosition).magnitude,moved,preparing||chargeTime>0))return false;
            CancelAttack();companionTarget=null;
            Vector3 towards=CombatFx.Flat(escapePostPosition-transform.position);
            if(towards.magnitude>.18f)
            {
                Vector3 step=route.Direction(transform.position,escapePostPosition,NavigationRadius);
                transform.position=WalkForAnimation(step*Mathf.Min(towards.magnitude,speed*dt));
                if(step.sqrMagnitude>.01f)transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(step),dt*7);
            }
            AnimateModel(towards.magnitude>.18f?1:0,0,hurtTime>0);ClampPosition();return true;
        }
        private void LateUpdate()
        {
            if(healthRoot==null) return;
            healthRoot.gameObject.SetActive(!IsDead && (aggro || Health<MaxHealth || IsBoss));
            Camera camera=Camera.main;
            if(camera!=null) healthRoot.rotation=camera.transform.rotation;
            float fraction=Mathf.Clamp01(Health/Mathf.Max(1,MaxHealth));
            float width=IsBoss?2.1f:1.05f;
            healthFill.localScale=new Vector3(width*fraction,.095f,1);
            healthFill.localPosition=new Vector3((fraction-1)*width*.5f,0,-.012f);
        }

        private void OnDisable() { if(threatAdmission!=null)threatAdmission.Release(threatMember); CancelAttack(); if (largeBoss != null) largeBoss.StopEncounter(); }

        private void OnDestroy()
        {
            if(threatAdmission!=null)threatAdmission.Release(threatMember);
            if(warning!=null) Destroy(warning);
            if(healthBackgroundMaterial!=null) Destroy(healthBackgroundMaterial);
            if(healthFillMaterial!=null) Destroy(healthFillMaterial);
        }
    }
}
