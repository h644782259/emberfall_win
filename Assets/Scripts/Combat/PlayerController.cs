using System.Collections.Generic;
using UnityEngine;

namespace Emberfall
{
    public sealed partial class PlayerController : MonoBehaviour
    {
        public float Health { get; private set; }
        public float MaxHealth { get; private set; }
        public bool IsDead { get { return Health <= 0; } }
        public HeroClass HeroClass { get; private set; }
        public float DodgeCooldown { get { return dodgeCooldown; } }
        public float BlinkCooldown { get { return dodgeCooldown; } }
        public bool IsJumping { get { return jumping; } }
        public float JumpCooldown { get { return 0f; } }
        public bool TraversalStartedThisFrame { get { return traversalFrame == Time.frameCount; } }
        public float Energy { get { return skillRuntime != null ? skillRuntime.Energy : SkillRuntime.MaximumEnergy; } }
        public float MaxEnergy { get { return SkillRuntime.MaximumEnergy; } }
        public EnemyController AimTarget { get; private set; }
        public Vector3 AimPoint { get { return aimPoint; } }
        public float MovementMultiplier { get { return (slowTime > 0 ? Mathf.Max(.3f, 1f - slowStrength) : 1f) * (charge != null && charge.IsCharging ? .4f : 1f); } }
        internal int CombatEpoch { get; private set; }

        private GameSession session;
        private StatBlock stats;
        private CombatModel model;
        private SkillTargetingController targeting;
        private SkillChargeController charge;
        private bool executingChargedSkill;
        private readonly Dictionary<EnemyController,Renderer[]> aimGeometry = new Dictionary<EnemyController,Renderer[]>();
        private readonly List<EnemyController> staleAimGeometry = new List<EnemyController>();
        private GameUI inputUI;
        internal bool GameplayCancelAllowed { get { return session != null && !session.InputBlocked && (inputUI == null || inputUI.GameplayBackAllowed); } }
        private SkillRuntime skillRuntime;
        private RunChoices ActiveRunBonuses { get { return session != null && session.InDungeon ? session.RunChoices : null; } }
        internal float RunAttackMultiplier { get { return ActiveRunBonuses == null ? 1f : ActiveRunBonuses.AttackMultiplier; } }
        private float CombatAttack { get { return stats.Damage * RunAttackMultiplier; } }

        private float attackCooldown, attackAnimation, hurtTimer, dodgeCooldown, invulnerability, skillFeedbackCooldown;
        private float guardTime, guardPower, guardReduction, guardRadius, guardPulseTimer;
        private int guardRank, mobilityRank, guardCastId;
        private float healingProtectionTime, healingReduction, mobilityTime;
        private float slowTime, slowStrength;
        private bool jumping;
        private float jumpAge, movementSkillLock;
        private Vector3 jumpOrigin;
        private int traversalFrame = -1;
        private float passiveCooldown, passiveTime, passiveReduction, passiveSpeed;
        private Vector3 aimPoint;
        private Vector3 bufferedBlinkDirection;
        private float perfectDodgeCounterTime;
        private bool lastMeleeDamagedEnemy;
        private float blinkBufferTime, perfectDodgeWindow, counterTime, dodgeShockTime, chargedWardTime, pursuitTime, starterRetry;
        private bool perfectDodgeAwarded, suppressBasicUntilReleased;
        private SkillBasicRecoveryClock skillBasicRecovery;
        private float classDodgeTime, burnStrideTime, coreWardTime;
        private readonly MasteryCoreRuntime masteryCore = new MasteryCoreRuntime();
        private int nextCastId;
        private float castDamageRoll = -1;
        private EnemyController focusedEnemy;
        private float focusTime;
        private readonly CombatProcCooldown returningBladeProc = new CombatProcCooldown();
        private readonly CombatProcCooldown venomSpreadProc = new CombatProcCooldown();
        private readonly CombatProcCooldown openingFrostProc = new CombatProcCooldown();
        private readonly OpeningFrostCounter openingFrost = new OpeningFrostCounter();
        private EnemyController openingFrostTarget;
        private int openingFrostIdentity;
        public float CounterRemaining { get { return counterTime; } }
        public float FocusRemaining { get { return FocusTarget == null ? 0 : focusTime; } }
        public EnemyController FocusTarget
        {
            get
            {
                return focusTime > 0 && ValidAimTarget(focusedEnemy) &&
                    CombatFx.Flat(focusedEnemy.transform.position - transform.position).sqrMagnitude <= PlayerUpgradeRules.FocusRange * PlayerUpgradeRules.FocusRange
                    ? focusedEnemy : null;
            }
        }
        internal bool HasMechanic(EquipmentMechanic mechanic) { return session != null && session.Progression.HasMechanic(mechanic); }
        internal ElementalistSpecialization Specialization { get { return session.Progression.Profile.specialization; } }


        public void Initialize(GameSession game, HeroClass heroClass)
        {
            // Re-initializing the same hero cannot reset resources or cooldowns.
            // A different class belongs to a new character and a new controller.
            if (skillRuntime == null) skillRuntime = new SkillRuntime(heroClass);
            else if (skillRuntime.HeroClass != heroClass)
                throw new System.InvalidOperationException("A player controller cannot change class during an adventure.");
            session = game;
            if(session!=null&&session.PracticeActive)skillRuntime.EnergyChanged=session.RecordPracticeEnergy;
            inputUI = game == null ? null : game.GetComponent<GameUI>();
            HeroClass = heroClass;
            gameObject.name = "Hero - " + GameBalance.ClassName(heroClass);
            if (model != null) Destroy(model.gameObject);
            model = CombatModel.Hero(transform, heroClass);
            targeting = GetComponent<SkillTargetingController>();
            if (targeting == null) targeting = gameObject.AddComponent<SkillTargetingController>();
            targeting.Initialize(this,session);
            charge = GetComponent<SkillChargeController>();
            if (charge == null) charge = gameObject.AddComponent<SkillChargeController>();
            charge.Initialize(this,session);
            RefreshStats(true);
            aimPoint = transform.position + Vector3.forward * 5;
        }

        public void RefreshStats(bool heal)
        {
            if (session == null) return;
            float previousMaximum = MaxHealth;
            bool wasDead = previousMaximum > 0 && Health <= 0;
            stats = session.Progression.GetStats();
            int selectedCore=session.Progression.Profile.masteryCore;
            int oldCore=masteryCore.Core,oldTier=masteryCore.Tier;
            masteryCore.Configure(selectedCore,selectedCore>=0&&selectedCore<4?session.Progression.Profile.masteryRanks[selectedCore]:0);
            if(oldCore!=masteryCore.Core||oldTier!=masteryCore.Tier)coreWardTime=0;
            if (model != null) model.ApplyFashion(session.Progression.EquippedFashion(FashionSlot.Wings),
                session.Progression.EquippedFashion(FashionSlot.Weapon));
            if (model != null) model.ApplyEquipment(session.Progression.Equipped(ItemSlot.Weapon),
                session.Progression.Equipped(ItemSlot.Armor), session.Progression.Equipped(ItemSlot.Relic));
            MaxHealth = Mathf.Max(1f, stats.MaxHealth);
            if (heal) Health = MaxHealth;
            // A camp draft is a pure allocation change, including no-op applies.
            // Preserve fractional living HP; other refresh callers retain their legacy floor.
            else if (!wasDead) Health = session.Progression.IsApplyingBuildDraft ? Mathf.Min(Health, MaxHealth) : Mathf.Clamp(Health, 1, MaxHealth);
            if(model!=null)model.SetBlenderPilotOwnerAlive(!IsDead);
            SummonedCompanion.RefreshBuild(this);
        }

        public void Heal(float amount)
        {
            if (IsDead || amount <= 0) return;
            float healed = Mathf.Min(amount,MaxHealth - Health);
            Health += healed;
            session.RecordActualHealing(healed);
            if (healed > .5f)
            {
                session.SpawnFloatingText(transform.position + Vector3.up * 2.4f,"+" + Mathf.CeilToInt(healed),new Color(.42f,1f,.65f));
                CombatFx.Ring(transform.position,1.2f,new Color(.35f,1f,.6f),.5f);
            }
        }

        public void Teleport(Vector3 position)
        {
            SummonedCompanion.RefreshBuild(this);
            CombatEpoch++;
            ClearMobilePinnedTarget();
            perfectDodgeCounterTime = 0;
            CancelCombatPose();
            masteryCore.Reset();coreWardTime=0;
            if (targeting != null) targeting.Cancel();
            if (charge != null) charge.Cancel();
            AimTarget = null;
            aimGeometry.Clear();
            position = WorldTraversal.NearestWalkable(position, .45f);
            transform.position = position;
            if (model != null) model.ResetLocomotion();
            jumping = false;
            jumpAge = movementSkillLock = 0;
            aimPoint = position+transform.forward*5f;
            guardTime = healingProtectionTime = mobilityTime = passiveTime = 0;guardCastId=0;
            blinkBufferTime = perfectDodgeWindow = counterTime = dodgeShockTime = chargedWardTime = pursuitTime = focusTime = 0;
            focusedEnemy = null; classDodgeTime = burnStrideTime = 0;
            perfectDodgeAwarded = true;
            starterRetry = 0;
            openingFrost.Clear(); openingFrostTarget = null; openingFrostIdentity = 0;
            slowTime = slowStrength = 0;
            attackAnimation = 0;
            attackCooldown = .15f;
            invulnerability = .65f;
            SummonedCompanion.TransferPermanentPartners(this);
        }

        // World teardown must retire delayed combat before dropping its loot
        // receipts. Do not move against old terrain or reset skill cooldowns;
        // Teleport later transfers the surviving permanent bodies on new ground.
        internal void RetireCombatForWorldTransition()
        {
            SummonedCompanion.RefreshBuild(this);
            CombatEpoch++;
            ClearMobilePinnedTarget();
            perfectDodgeCounterTime = 0;
            CancelCombatPose();
            if (targeting != null) targeting.Cancel();
            if (charge != null) charge.Cancel();
        }

        // Call once only after a dungeon/new challenge transition has committed.
        // Opening/cancelling a portal and advancing waves never call this method.
        public void ResetCooldownsForDungeonEntry()
        {
            CombatEpoch++;
            ClearMobilePinnedTarget();
            perfectDodgeCounterTime = 0;
            CancelCombatPose();
            masteryCore.Reset();coreWardTime=0; // Retire prior-zone delayed impacts as well as stale aim.
            if (targeting != null) targeting.Cancel();
            if (charge != null) charge.Cancel();
            if (skillRuntime != null) skillRuntime.ResetCooldowns();
            attackCooldown = dodgeCooldown = passiveCooldown = skillFeedbackCooldown = movementSkillLock = 0;
            blinkBufferTime = 0;
            returningBladeProc.Advance(float.MaxValue);
            venomSpreadProc.Advance(float.MaxValue);
            openingFrostProc.Advance(float.MaxValue);
            openingFrost.Clear();
            // Teleport already transfers persistent partners; this extra epoch
            // retires old skill effects without dismissing those same partners.
            SummonedCompanion.TransferPermanentPartners(this);
        }

        public void TakeDamage(float amount) { TakeDamageFrom(amount, "敌方攻击"); }

        public void TakeDamageFrom(float amount, string source)
        {
            CombatImpactBatch.BeginAction();
            try
            {
            if (session == null || IsDead || session.CombatEnded || invulnerability > 0 || !session.HasStarted || amount <= 0 || float.IsNaN(amount) || float.IsInfinity(amount)) return;
            float damage = Mathf.Max(1, amount * CombatBalance.ArmorDamageMultiplier(stats.Armor, session.Progression.Profile.level));
            if (chargedWardTime > 0) damage *= .75f;
            if(coreWardTime>0)damage*=1f-masteryCore.WardReduction;
            if (guardTime > 0)
            {
                damage *= 1f-guardReduction;
                if (HeroClass == HeroClass.Vanguard)
                {
                    AdvancedSkillVfx.Rune(this, transform.position, guardRadius, new Color(1f,.84f,.4f), .55f, guardRank,identity:4);
                    HitArea(transform.position, guardRadius, CombatAttack * guardPower, .5f, .2f,guardCastId);
                }
            }
            // A guard counter can finish a mode reentrantly. Paid victory wins that
            // tie; do not apply the already-in-flight hostile hit after terminal state.
            if(session.CombatEnded)return;
            if (healingProtectionTime > 0) damage *= 1f-healingReduction;
            if (passiveTime > 0) damage *= 1f-passiveReduction;
            if (ActiveRunBonuses != null) damage *= ActiveRunBonuses.IncomingDamageMultiplier(Health / Mathf.Max(1f, MaxHealth));
            damage = Mathf.Max(damage, amount * CombatBalance.MinimumCombinedDamageMultiplier);
            if (session.HasBlessing(RunBlessing.RiskContract)) damage *= 1.15f;
            session.RecordIncomingDamage(source, Mathf.Min(Health, damage));
            float healthBeforeHit = Health;
            Health = Mathf.Max(0,Health - damage);
            if (CombatReviewEvents.Enabled) CombatReviewEvents.Emit("takendamage",CombatReviewObjectId.Get(this),amount:healthBeforeHit-Health,detail:source);
            if(Health>0){float recovery=masteryCore.DamageTaken(Health/MaxHealth);if(recovery>0){Heal(MaxHealth*recovery);session.RecordCombatAction("生机核心");}}
            GameAudio.Play(SoundCue.Hit);
            invulnerability = .2f;
            hurtTimer = .22f;
            session.SpawnFloatingText(transform.position + Vector3.up * 2.5f,"−" + Mathf.CeilToInt(damage),new Color(1f,.4f,.42f));
            CombatFx.Ring(transform.position,.85f,new Color(1f,.27f,.3f),.2f);
            if (Health > 0) TryDefensePassive();
            if (Health <= 0)
            {
                if (CombatReviewEvents.Enabled) CombatReviewEvents.Emit("death",CombatReviewObjectId.Get(this));
                CombatEpoch++;
            ClearMobilePinnedTarget();
                perfectDodgeCounterTime = 0;
                CancelCombatPose();
            masteryCore.Reset();coreWardTime=0;
                if (targeting != null) targeting.Cancel();
                if (charge != null) charge.Cancel();
                if (jumping) transform.position = WorldTraversal.NearestWalkable(transform.position, .45f);
                jumping = false;
                AimTarget = null;
                focusedEnemy = null; focusTime = blinkBufferTime = 0;
                model.SetBlenderPilotOwnerAlive(false); // Restore procedural visuals before the final death pose.
                model.transform.localRotation = Quaternion.Euler(0,0,75f);
                session.OnPlayerDied();
            }

            }
            finally { CombatImpactBatch.EndAction(); }
        }

        public float CooldownRemaining(int slot) { return SkillCooldownRemaining(HotbarSkill(slot)); }
        public float SkillCooldownRemaining(int skillIndex) { return skillRuntime != null ? skillRuntime.Remaining(skillIndex) : 0f; }

        private int HotbarSkill(int slot)
        {
            if (session == null || slot < 0 || slot >= GameBalance.HotbarSize) return -1;
            GameProfile profile = session.Progression.Profile;
            int index = profile.hotbarPage * GameBalance.HotbarSize + slot;
            return profile.equippedSkills != null && index >= 0 && index < profile.equippedSkills.Length ? profile.equippedSkills[index] : -1;
        }

        private void OnApplicationFocus(bool focused) { if (!focused) CancelTransientInput(); }
        private void OnApplicationPause(bool paused) { if (paused) CancelTransientInput(); }
        private void OnDestroy() { SummonedCompanion.RetireOwner(this); }
        private void CancelTransientInput()
        {
            blinkBufferTime = 0;
            suppressBasicUntilReleased = true;
        }

        private void Update()
        {
            if (session == null || model == null) return;
            // Paused dialogs/panels own Back; their frozen charge is not gameplay input.
            if (GameplayCancelAllowed && charge != null && charge.IsCharging && (Input.GetKeyDown(KeyCode.Escape) || AdventureCamera.CancelSkillRequested)) charge.Cancel();
            if (session.InputBlocked)
            {
                blinkBufferTime = 0;
                if (targeting != null) targeting.Cancel();
            }
            if (session.BackgroundPaused) suppressBasicUntilReleased = true;
            float dt = Time.deltaTime;
            if (dt <= 0) return;
            attackCooldown = Mathf.Max(0,attackCooldown - dt);
            skillBasicRecovery.Advance(dt);
            attackAnimation = Mathf.Max(0,attackAnimation - dt * 4f);
            hurtTimer = Mathf.Max(0,hurtTimer - dt);
            dodgeCooldown = Mathf.Max(0,dodgeCooldown - dt);
            movementSkillLock = Mathf.Max(0, movementSkillLock - dt);
            invulnerability = Mathf.Max(0,invulnerability - dt);
            skillFeedbackCooldown = Mathf.Max(0,skillFeedbackCooldown - dt);
            perfectDodgeWindow = Mathf.Max(0, perfectDodgeWindow - dt);
            counterTime = Mathf.Max(0, counterTime - dt);
            perfectDodgeCounterTime = Mathf.Max(0, perfectDodgeCounterTime - dt);
            classDodgeTime = Mathf.Max(0, classDodgeTime - dt); coreWardTime = Mathf.Max(0, coreWardTime - dt); masteryCore.Advance(dt); burnStrideTime = Mathf.Max(0, burnStrideTime - dt);
            dodgeShockTime = Mathf.Max(0, dodgeShockTime - dt);
            chargedWardTime = Mathf.Max(0, chargedWardTime - dt);
            pursuitTime = Mathf.Max(0, pursuitTime - dt);
            focusTime = Mathf.Max(0, focusTime - dt);
            returningBladeProc.Advance(dt); venomSpreadProc.Advance(dt); openingFrostProc.Advance(dt);
            if (FocusTarget == null) { focusedEnemy = null; focusTime = 0; }
            if (IsDead) return;
            skillRuntime.Advance(dt);
            if (ActiveRunBonuses != null) skillRuntime.RestoreEnergy(dt * ActiveRunBonuses.ExtraEnergyPerSecond);
            slowTime = Mathf.Max(0, slowTime - dt);
            if (slowTime <= 0) slowStrength = 0;
            guardTime = Mathf.Max(0, guardTime - dt);
            healingProtectionTime = Mathf.Max(0,healingProtectionTime-dt);
            mobilityTime = Mathf.Max(0,mobilityTime-dt);
            passiveTime = Mathf.Max(0,passiveTime-dt);
            passiveCooldown = Mathf.Max(0,passiveCooldown-dt);
            if (HeroClass == HeroClass.Arcanist && guardTime > 0)
            {
                guardPulseTimer -= dt;
                if (guardPulseTimer <= 0)
                {
                    guardPulseTimer = guardRank==3?1f:guardRank==2?1.2f:1.5f;
                    if (Specialization == ElementalistSpecialization.Burn)
                        CombatArea.Spawn(this, session, transform.position, guardRadius, CombatAttack * .18f, 0, 0, 1.5f, .5f, new Color(1f,.5f,.25f),castId:guardCastId,visual:SkillVisualRecipe.Fire);
                    else
                    {
                        ControlArea(transform.position,guardRadius,.25f);
                        foreach (EnemyController enemy in session.Enemies)
                            if (ValidAimTarget(enemy) && CombatFx.Flat(enemy.transform.position-transform.position).sqrMagnitude <= guardRadius*guardRadius && CombatSight.Area(transform.position,enemy.transform.position))
                                enemy.StatusEffects.FrostMark(5f);
                    }
                    CombatFx.Ring(transform.position,guardRadius,Specialization==ElementalistSpecialization.Burn?new Color(1f,.55f,.25f):new Color(.56f,.93f,1f),.4f,.12f);
                }
            }
            model.transform.localRotation = Quaternion.identity;
            if (session.InputBlocked)
            {
                model.SetLocomotion(Vector3.zero,dt,stats.MoveSpeed,false,jumping,jumpAge/.55f);
                model.Animate(0,attackAnimation,hurtTimer > 0);
                return;
            }
            Vector3 walkingDisplacement = Vector3.zero;
            MaintainStarterCompanion(dt);
            bool mobile = MobileControls.Active;
            Vector2 moveInput = mobile ? MobileControls.Move : new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            Vector3 movement = mobile ? new Vector3(moveInput.x, 0, moveInput.y) :
                AdventureCamera.CameraRelativeMovement(moveInput, Camera.main == null ? null : Camera.main.transform);
            movement = Vector3.ClampMagnitude(movement,1);
            bool wantsJump = mobile ? MobileControls.ConsumeJump() : Input.GetKeyDown(KeyCode.Space);
            if (wantsJump) TryJump();
            bool wantsBlink = mobile ? MobileControls.ConsumeDodge() : Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift);
            if (wantsBlink) TryBlink(movement);
            else if (blinkBufferTime > 0) TryBlinkCore(bufferedBlinkDirection, false);
            if (jumping)
            {
                AdvanceJump(dt);
                if (!jumping && !wantsBlink && blinkBufferTime > 0) TryBlinkCore(bufferedBlinkDirection, false);
            }
            else if (!TraversalStartedThisFrame)
            {
                float movementBonus = passiveTime>0?passiveSpeed:0;
                if (mobilityTime>0) movementBonus += .1f+mobilityRank*.05f;
                if (pursuitTime > 0) movementBonus += .2f;
                if (burnStrideTime > 0) movementBonus += .2f;
                Vector3 walkingStart = transform.position;
                transform.position = WorldTraversal.Move(transform.position, movement * stats.MoveSpeed * (1f+movementBonus) * MovementMultiplier * dt, .45f);
                walkingDisplacement = CombatFx.Flat(transform.position - walkingStart);
            }
            // Readiness and landing are checked before expiring an older input,
            // so a buffer at the exact cooldown boundary is not lost to frame order.
            if (!wantsBlink) blinkBufferTime = Mathf.Max(0, blinkBufferTime - dt);
            Vector3 beforeBoundary = transform.position;
            Vector3 bounded = transform.position;
            float bound = Mathf.Max(1,session.ArenaRadius - .65f);
            float airborneHeight = jumping ? bounded.y : 0;
            bounded.y = 0;
            bounded = Vector3.ClampMagnitude(bounded,bound);
            bounded.y = airborneHeight;
            transform.position = bounded;
            if (walkingDisplacement.sqrMagnitude > 0) walkingDisplacement += CombatFx.Flat(bounded-beforeBoundary);
            // Commit actual walking before any attack samples its final weapon pose.
            model.SetLocomotion(transform.InverseTransformDirection(walkingDisplacement),dt,stats.MoveSpeed,!TraversalStartedThisFrame,jumping,jumpAge/.55f);
            // Mouse selection uses this frame's final position. Walking only turns
            // the model; it never overwrites the independent mouse aim point.
            if ((charge == null || !charge.IsCharging) && (mobile || !session.PointerOverUI))
                aimPoint = mobile ? ResolveMobileAim(movement) : ResolveAim(Camera.main,Input.mousePosition);
            bool attackHeld = mobile ? MobileControls.AttackHeld : Input.GetMouseButton(0) || Input.GetKey(KeyCode.J);
            if (!attackHeld) suppressBasicUntilReleased = false;
            bool wantsBasic = !suppressBasicUntilReleased && attackHeld && (mobile || !session.PointerOverUI);
            if (movement.sqrMagnitude > .01f && !wantsBasic && (charge == null || !charge.IsCharging))
                transform.rotation = Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(movement),720f*dt);
            if (!TraversalStartedThisFrame && !mobile && !session.PointerOverUI)
            {
                GameProfile profile = session.Progression.Profile;
                for (int slot=0;slot<GameBalance.HotbarSize;slot++)
                    if (profile.hotbarKeys != null && slot < profile.hotbarKeys.Length && Input.GetKeyDown((KeyCode)profile.hotbarKeys[slot]))
                    {
                        int skill = HotbarSkill(slot);
                        if (skill == GameBalance.HotbarPotion) { session.UseHotbarConsumable(); break; }
                        if (skill >= 0 && skill < GameBalance.SkillCount && targeting != null) targeting.Begin(skill);
                    }
            }
            bool suppressBasic = targeting != null && targeting.TickInput();
            if (!TraversalStartedThisFrame && wantsBasic && !suppressBasic && (charge == null || (!charge.IsCharging && !charge.ConsumedThisFrame)))
            {
                FaceAim();
                if (attackCooldown <= 0) BasicAttack();
            }
            model.Animate(movement.magnitude,attackAnimation,hurtTimer > 0);
            if (charge != null && charge.IsCharging) model.AnimateCharge(charge.Progress,charge.SkillIndex);
        }

        // Kept independent of Input so runtime validation can project a known body
        // point, move the player and check that the firing direction is recalculated.
        private void ApplyAim(Vector2 screenPosition)
        {
            if (charge != null && charge.IsCharging) return;
            aimPoint = ResolveAim(Camera.main,screenPosition);
            FaceAim();
        }

        private Vector3 ResolveAim(Camera camera,Vector2 screenPosition)
        {
            AimTarget = null;
            if (camera == null || !camera.pixelRect.Contains(screenPosition)) return aimPoint;
            Vector3 freeAim = aimPoint;
            Ray ray = camera.ScreenPointToRay(screenPosition);
            float enter;
            if (new Plane(Vector3.up,Vector3.zero).Raycast(ray,out enter)) freeAim = ray.GetPoint(enter);
            if (session == null) return freeAim;
            float margin = Mathf.Clamp(camera.pixelHeight/90f,6f,12f);
            float bestScore = float.PositiveInfinity;
            for (int i=0;i<session.Enemies.Count;i++)
            {
                EnemyController enemy = session.Enemies[i];
                if (!ValidAimTarget(enemy) || CombatFx.Flat(enemy.transform.position-transform.position).sqrMagnitude>18f*18f) continue;
                Bounds bounds = EnemyAimBounds(enemy);
                Rect projected;
                if (!ProjectedBounds(camera,bounds,out projected)) continue;
                bool direct = projected.Contains(screenPosition);
                Rect assisted = Rect.MinMaxRect(projected.xMin-margin,projected.yMin-margin,projected.xMax+margin,projected.yMax+margin);
                if (!assisted.Contains(screenPosition)) continue;
                Vector3 bodyScreen = camera.WorldToScreenPoint(EnemyBodyPoint(enemy));
                float bodyDistance = Vector2.Distance(screenPosition,new Vector2(bodyScreen.x,bodyScreen.y));
                float score = (direct?0f:10000f)+bodyDistance+bodyScreen.z*.02f;
                if (score<bestScore) { bestScore=score; AimTarget=enemy; }
            }
            PruneAimGeometry();
            return AimTarget != null ? CombatFx.Flat(AimTarget.transform.position) : freeAim;
        }

        private Vector3 ResolveMobileAim(Vector3 movement)
        {
            var pinned=MobilePinnedTarget;
            if(pinned!=null){AimTarget=pinned;return CombatFx.Flat(pinned.transform.position);}
            AimTarget = null;
            Vector3 direction = movement.sqrMagnitude > .01f ? movement.normalized : transform.forward;
            float nearest = 14f;
            for (int i = 0; i < session.Enemies.Count; i++)
            {
                EnemyController enemy = session.Enemies[i];
                if (!ValidAimTarget(enemy)) continue;
                Vector3 delta = CombatFx.Flat(enemy.transform.position - transform.position);
                float distance = delta.magnitude;
                if (distance < nearest && Vector3.Angle(direction, delta) <= 75f && CombatSight.Direct(transform.position,enemy.transform.position))
                { nearest = distance; AimTarget = enemy; }
            }
            return AimTarget != null ? CombatFx.Flat(AimTarget.transform.position) : transform.position + direction * 8f;
        }

        // Called once for a mobile skill tap before the existing cast/charge path.
        // Selection only changes aim; it never spends energy or starts a cooldown.
        private readonly List<MobileSkillPolicy.Candidate> mobileAimCandidates=new List<MobileSkillPolicy.Candidate>();
        internal void PrepareMobileSkillAim(int skill)
        {EnemyController enemy;Vector3 point;ResolveMobileSkillAim(skill,out enemy,out point);AimTarget=enemy;aimPoint=point;}
        internal void ResolveMobileSkillAim(int skill,out EnemyController enemy,out Vector3 point)
        {
            var preview=SkillTargetingController.Describe(HeroClass,skill,session.Progression.Profile.skillRanks[skill]);
            if(preview.shape==SkillTargetingController.Shape.Self){enemy=null;point=transform.position;return;}
            if(HeroClass==HeroClass.Summoner&&(skill==2||skill==4||skill==9))
            {var team=SummonedCompanion.ExplicitFocus(this);if(team!=null){enemy=team;point=CombatFx.Flat(team.transform.position);return;}}
            var pinned=MobilePinnedTarget;
            if(pinned!=null&&MobilePinAppliesToSkill(skill)){enemy=pinned;point=CombatFx.Flat(pinned.transform.position);return;}
            float range=preview.distance>0?preview.distance:14f;
            mobileAimCandidates.Clear();
            foreach(var candidate in session.Enemies)
            {
                bool valid=ValidAimTarget(candidate);
                float distance=valid?CombatFx.Flat(candidate.transform.position-transform.position).sqrMagnitude:float.PositiveInfinity;
                bool visible=valid&&CombatSight.Direct(transform.position,candidate.transform.position);
                mobileAimCandidates.Add(new MobileSkillPolicy.Candidate(distance,visible,candidate==AimTarget||candidate==FocusTarget));
            }
            int chosen=MobileSkillPolicy.SelectTarget(mobileAimCandidates,range);
            enemy=chosen<0?null:session.Enemies[chosen];
            point=enemy!=null?CombatFx.Flat(enemy.transform.position):CombatFx.Flat(transform.position+transform.forward*Mathf.Min(8f,range));
            point=CombatSight.GroundPoint(transform.position,Vector3.ClampMagnitude(point,Mathf.Max(1,session.ArenaRadius-.65f)));
        }

        private static bool ProjectedBounds(Camera camera,Bounds bounds,out Rect screenBounds)
        {
            Vector3 min=bounds.min,max=bounds.max;
            float left=float.PositiveInfinity,bottom=float.PositiveInfinity,right=float.NegativeInfinity,top=float.NegativeInfinity;
            for(int corner=0;corner<8;corner++)
            {
                Vector3 screen=camera.WorldToScreenPoint(new Vector3((corner&1)==0?min.x:max.x,(corner&2)==0?min.y:max.y,(corner&4)==0?min.z:max.z));
                if(screen.z<=camera.nearClipPlane) { screenBounds=new Rect(); return false; }
                left=Mathf.Min(left,screen.x); right=Mathf.Max(right,screen.x);
                bottom=Mathf.Min(bottom,screen.y); top=Mathf.Max(top,screen.y);
            }
            screenBounds=Rect.MinMaxRect(left,bottom,right,top);
            return camera.pixelRect.Overlaps(screenBounds);
        }

        private Bounds EnemyAimBounds(EnemyController enemy)
        {
            Renderer[] renderers;
            if(!aimGeometry.TryGetValue(enemy,out renderers))
            {
                CombatModel enemyModel=enemy.GetComponentInChildren<CombatModel>();
                renderers=enemyModel!=null?enemyModel.GetComponentsInChildren<Renderer>():new Renderer[0];
                aimGeometry.Add(enemy,renderers);
            }
            Bounds bounds=new Bounds(enemy.transform.position+Vector3.up,Vector3.one);
            bool found=false;
            for(int i=0;i<renderers.Length;i++)
                if(renderers[i]!=null && renderers[i].enabled)
                {
                    if(!found) { bounds=renderers[i].bounds; found=true; }
                    else bounds.Encapsulate(renderers[i].bounds);
                }
            return bounds;
        }

        internal Vector3 EnemyBodyPoint(EnemyController enemy)
        {
            if(enemy==null) return aimPoint+Vector3.up;
            Bounds bounds=EnemyAimBounds(enemy);
            Vector3 at=enemy.transform.position;
            at.y=Mathf.Clamp(bounds.center.y,at.y+.35f,at.y+3f);
            return at;
        }

        private static bool ValidAimTarget(EnemyController enemy)
        {
            return enemy!=null && !enemy.IsDead && enemy.gameObject.activeInHierarchy;
        }

        private void PruneAimGeometry()
        {
            if(aimGeometry.Count<=session.Enemies.Count+4) return;
            staleAimGeometry.Clear();
            foreach(KeyValuePair<EnemyController,Renderer[]> pair in aimGeometry)
                if(!ValidAimTarget(pair.Key)) staleAimGeometry.Add(pair.Key);
            for(int i=0;i<staleAimGeometry.Count;i++) aimGeometry.Remove(staleAimGeometry[i]);
        }

        private void FaceAim()
        {
            if(!ValidAimTarget(AimTarget)) AimTarget=null;
            if(AimTarget!=null) aimPoint=CombatFx.Flat(AimTarget.transform.position);
            Vector3 forward=CombatFx.Flat(aimPoint-transform.position);
            if(forward.sqrMagnitude>.0001f) transform.rotation=Quaternion.LookRotation(forward.normalized);
        }

        private EnemyController MagicConeTarget()
        {
            Vector3 direction=CombatFx.Flat(aimPoint-transform.position).normalized;
            if(direction.sqrMagnitude<.01f) direction=transform.forward;
            EnemyController selected=null;
            float nearest=14f;
            for(int i=0;i<session.Enemies.Count;i++)
            {
                EnemyController enemy=session.Enemies[i];
                if(!ValidAimTarget(enemy)) continue;
                Vector3 delta=CombatFx.Flat(enemy.transform.position-transform.position);
                float distance=delta.magnitude;
                if(distance<=nearest && Vector3.Angle(direction,delta)<=35f && CombatSight.Direct(transform.position,enemy.transform.position)) { selected=enemy; nearest=distance; }
            }
            return selected;
        }

        internal CombatDamage RollDirectDamage(float amount)
        {
            RunChoices bonus = ActiveRunBonuses;
            return CombatDamage.Roll(amount, bonus == null ? stats.CritChance : bonus.CritChance(stats.CritChance),
                castDamageRoll >= 0 ? castDamageRoll : Random.value, bonus == null ? 1.65f : bonus.CriticalMultiplier);
        }

        private CombatDamage Damage(float multiplier)
        {
            return RollDirectDamage(CombatAttack * multiplier);
        }

        private void BasicAttack()
        {
            CombatImpactBatch.BeginAction();
            try
            {
            if (TraversalStartedThisFrame || skillBasicRecovery.Blocked) return;
            if(!MobilePinnedActionAllowed(-1,true))return;
            if (charge != null && (charge.IsCharging || charge.ConsumedThisFrame)) return;
            if ((HeroClass==HeroClass.Arcanist || HeroClass==HeroClass.Summoner) && !ValidAimTarget(AimTarget)) AimTarget=MagicConeTarget();
            FaceAim();
            GameAudio.Play(SoundCue.Attack);
            if (CombatReviewEvents.Enabled) CombatReviewEvents.Emit("attack",CombatReviewObjectId.Get(this));
            attackCooldown = SkillDamageBudgets.BasicInterval(HeroClass);
            if (mobilityTime > 0) attackCooldown *= .8f;
            if (ActiveRunBonuses != null) attackCooldown /= ActiveRunBonuses.AttackSpeedMultiplier;
            attackCooldown = Mathf.Max(.18f, attackCooldown);
            if(ReturningCounterReady) model.PlayAction(-2,true,attackCooldown);
            else model.PlayAction(-1,true,attackCooldown);
            attackAnimation = 1f;
            Color color = GameBalance.ClassColor(HeroClass);
            if (HeroClass == HeroClass.Vanguard)
            {
                bool thrust = ReturningCounterReady;
                if (thrust)
                {
                    if (ValidAimTarget(AimTarget))
                    {
                        Vector3 landing;
                        if(ReturningCounterRules.Predict(transform.position,AimTarget.transform.position,AimTarget.IsBoss,AimTarget.HitFootprintBonus,out landing).Length==0) transform.position=landing;
                    }
                    AdvancedSkillVfx.Beam(this,transform.position+Vector3.up,transform.position+Vector3.up+transform.forward*2.8f,color,.18f,.12f);
                }
                else CombatFx.WeaponSlash(this,model,transform.position,transform.forward,2.3f,color);
                float previousCounter = counterTime;
                bool wasCounter = previousCounter > 0;
                bool wasDodgeCounter = perfectDodgeCounterTime > 0;
                float power = (wasCounter ? 1.75f : SkillDamageBudgets.BasicCoefficient(HeroClass)) * (HasMechanic(EquipmentMechanic.ReturningBlade) && !ReturningCounterVariant ? .92f : 1f);
                counterTime = 0; // Consume only the captured bonus; on-hit procs may grant a NEW one.
                bool hit = Melee(2.8f, 110f, Damage(power), .3f, .12f, basic: true, counterThrust: thrust);
                counterTime = PlayerUpgradeRules.CounterAfterAttack(previousCounter, counterTime, hit);
                if (hit && wasCounter)
                {
                    if (wasDodgeCounter && lastMeleeDamagedEnemy) session.RecordClassTutorial(HeroClass.Vanguard);
                    perfectDodgeCounterTime = 0;
                    session.RecordCombatAction("剑卫反击");
                    session.RecordCombatAction("职业能力");
                    session.SpawnMechanismText(transform.position + Vector3.up * 2.5f, "反击！", new Color(1f, .85f, .35f));
                }
            }
            else
            {
                bool ranger = HeroClass == HeroClass.Ranger;
                Vector3 target=ValidAimTarget(AimTarget)?EnemyBodyPoint(AimTarget):new Vector3(aimPoint.x,1.15f,aimPoint.z);
                float distance=CombatFx.Flat(target-transform.position).magnitude;
                Vector3 muzzle=transform.position+Vector3.up*1.15f+transform.forward*Mathf.Min(.55f,distance*.3f);
                CombatProjectile.BasicShot(this,session,muzzle,target,Damage(SkillDamageBudgets.BasicCoefficient(HeroClass)*(mobilityTime>0?1f+.12f*mobilityRank:1f)),color,ranger,AimTarget);
            }

            }
            finally { CombatImpactBatch.EndAction(); }
        }

        private bool Melee(float range, float arc, CombatDamage damage, float knockback, float stun, float knockdown = 0, bool basic = false, int skillIndex = -1, int castId = 0, bool counterThrust = false)
        {
            CombatImpactBatch.Begin();
            try
            {
            if(castId==0)castId=NewCastId();
            lastMeleeDamagedEnemy = false;
            if(counterThrust) DestructibleProp.StrikeLine(this,transform.position,transform.position+transform.forward*range,.35f,damage,castId);
            else DestructibleProp.StrikeCone(this,transform.position,transform.forward,range,arc,damage,castId);
            bool hit = false;
            EnemyController firstHit = null;
            Vector3 firstHitPosition = transform.position;
            for (int i = session.Enemies.Count - 1; i >= 0; i--)
            {
                EnemyController enemy = session.Enemies[i];
                if (enemy == null || enemy.IsDead) continue;
                Vector3 delta = CombatFx.Flat(enemy.transform.position-transform.position);
                if (delta.magnitude <= range + (enemy.IsBoss ? .5f : 0) + enemy.HitFootprintBonus &&
                    (counterThrust ? (Vector3.Dot(delta,transform.forward) >= 0 &&
                        (delta-transform.forward*Vector3.Dot(delta,transform.forward)).magnitude <= .35f+enemy.HitFootprintBonus)
                        : (delta.sqrMagnitude < .36f || Vector3.Angle(transform.forward,delta) <= arc*.5f)) &&
                    CombatSight.Melee(transform.position, enemy.transform.position))
                {
                    if (!hit) { firstHit = enemy; firstHitPosition = enemy.transform.position; }
                    hit = true;
                    enemy.TrySkillInterrupt(this, skillIndex, castId);
                    if(!basic&&damage.Amount>0)RegisterSkillHit(castId);
                    float healthBefore = enemy.Health;
                    enemy.TakeDamage(damage.Amount,delta.normalized,knockback,stun,critical:damage.IsCritical,practiceCastId:basic?0:castId);
                    if (enemy.Health < healthBefore) lastMeleeDamagedEnemy = true;
                    if (knockdown > 0 && enemy.StatusEffects != null) enemy.StatusEffects.Knockdown(knockdown);
                }
            }
            if (CombatReviewEvents.Enabled && !lastMeleeDamagedEnemy) CombatReviewEvents.Emit(basic ? "basicmiss" : "spellmiss",CombatReviewObjectId.Get(this),skill:skillIndex,detail:"melee_release_no_enemy_damage;props_not_counted");
            if (hit && basic) OnBasicAttackHitTarget(firstHitPosition, firstHit, true);
            return hit;

            }
            finally {CombatImpactBatch.End();}
        }

        // Legacy energy-validation entry point has no confirmed target and does
        // not report a tutorial action. Real hits use the explicit hit contract.
        internal void OnBasicAttackHit(Vector3 position) { if (!IsDead) skillRuntime.RestoreEnergy(SkillDamageBudgets.BasicEnergyOnHit); }

        internal void OnBasicAttackHitTarget(Vector3 position, EnemyController enemy, bool confirmedLivingHit)
        {
            if (IsDead || enemy == null || !confirmedLivingHit) return;
            // Both callers capture a living target before damage, so legitimate
            // killing blows count while attempts against already-dead targets do not.
            skillRuntime.RestoreEnergy(SkillDamageBudgets.BasicEnergyOnHit + session.Progression.Profile.masteryRanks[3] * .1f);
            session.RecordCombatAction("普攻回能");
            if (HeroClass == HeroClass.Arcanist)
            {
                if (openingFrostTarget != enemy) { openingFrostTarget = enemy; openingFrostIdentity++; }
                if (openingFrost.RecordHit(openingFrostIdentity, ValidAimTarget(enemy), session.Progression.Profile.skillRanks[0] > 0) &&
                    enemy.StatusEffects != null && openingFrostProc.TryTrigger(PlayerUpgradeRules.BasicFrostProcCooldown))
                {
                    bool burning = Specialization == ElementalistSpecialization.Burn;
                    if (burning) enemy.StatusEffects.Burn(this, 2f, CombatAttack * .35f);
                    else if (session.Progression.Profile.skillRanks[0] > 0) enemy.StatusEffects.FrostMark(PlayerUpgradeRules.BasicFrostMarkDuration);
                    else enemy.StatusEffects.Freeze(.35f);
                    CombatFx.Ring(position, 1f, burning ? new Color(1f, .46f, .08f) : new Color(.5f, .9f, 1f), .35f, .12f);
                    session.SpawnMechanismText(position + Vector3.up * 2f, burning ? "灼触" : "霜触", burning ? new Color(1f, .55f, .18f) : new Color(.55f, .95f, 1f));
                    session.RecordCombatAction("职业能力");
                }
            }
            SettleMasteryCombo(enemy);
            if (ValidAimTarget(enemy) && classDodgeTime > 0)
            {
                classDodgeTime = 0;
                if (HeroClass == HeroClass.Arcanist && enemy.StatusEffects != null)
                { if (Specialization == ElementalistSpecialization.Burn) enemy.StatusEffects.Burn(this, 2f, CombatAttack * .5f); else enemy.StatusEffects.FrostMark(4f); }
                if (HeroClass == HeroClass.Ranger) { enemy.TakeDamage(CombatAttack * .75f, Vector3.zero); if (!enemy.IsDead) enemy.StatusEffects.Mark(3f,.12f); }
            }
            if (HeroClass == HeroClass.Ranger && ValidAimTarget(enemy) && enemy.StatusEffects != null)
                enemy.StatusEffects.Poison(this, PlayerUpgradeRules.BasicPoisonDuration, CombatAttack * PlayerUpgradeRules.BasicPoisonCoefficient);
            if (HeroClass == HeroClass.Summoner && ValidAimTarget(enemy))
            {
                focusedEnemy = enemy; focusTime = PlayerUpgradeRules.FocusDuration;
                session.RecordCombatAction("召唤集火");
                session.RecordCombatAction("职业能力");
            }
            if (dodgeShockTime > 0)
            {
                dodgeShockTime = 0;
                HitArea(position, 2.5f, CombatAttack * .7f, .35f, .15f);
                CombatFx.Ring(position, 2.5f, new Color(.5f, .8f, 1f), .3f, .14f);
                session.RecordCombatAction("闪避震荡");
            }
            if (HeroClass == HeroClass.Vanguard && HasMechanic(EquipmentMechanic.ReturningBlade) && !ReturningCounterVariant)
            {
                EnemyController bounce = NearestOtherEnemy(position, enemy, 4f);
                if (bounce != null && returningBladeProc.TryTrigger(1.5f))
                {
                    bounce.TakeDamage(CombatAttack * 1.1f, CombatFx.Flat(bounce.transform.position - position).normalized, .12f);
                    AdvancedSkillVfx.Beam(this, position + Vector3.up, bounce.transform.position + Vector3.up, GameBalance.ClassColor(HeroClass), .25f, .14f);
                    // A returned blade strengthens a deliberate next heavy attack,
                    // while a kill carries the blade to a fresh target.
                    if(counterTime<=1.8f)counterWindowDuration=1.8f;
                    counterTime = Mathf.Max(counterTime, 1.8f);
                    if (bounce.IsDead)
                    {
                        EnemyController next = NearestOtherEnemy(bounce.transform.position, enemy, 4f);
                        if (next != null) next.TakeDamage(CombatAttack * .65f, Vector3.zero);
                    }
                    session.RecordCombatAction("回旋刃");
                }
            }
        }

        private EnemyController NearestOtherEnemy(Vector3 point, EnemyController excluded, float radius)
        {
            EnemyController chosen = null;
            float best = radius * radius;
            foreach (EnemyController enemy in session.Enemies)
            {
                if (!ValidAimTarget(enemy) || enemy == excluded) continue;
                float distance = CombatFx.Flat(enemy.transform.position - point).sqrMagnitude;
                if (distance < best && CombatSight.Direct(point, enemy.transform.position)) { chosen = enemy; best = distance; }
            }
            return chosen;
        }

        internal float ResolveSkillImpact(EnemyController enemy, int skill, int castId, float baseDamage, bool critical = false, float criticalMultiplier = 1.65f)
        {
            if (enemy == null || enemy.IsDead || enemy.StatusEffects == null) return baseDamage;
            EnemyStatusEffects status = enemy.StatusEffects;
            enemy.TrySkillInterrupt(this, skill, castId);
            float uncriticalDamage = critical ? baseDamage / Mathf.Clamp(criticalMultiplier, 1f, 2.25f) : baseDamage;
            if (skill >= 0) ApplySpellDodgeBoon(enemy);
            if (HeroClass == HeroClass.Arcanist && skill == 1)
            {
                float direct = baseDamage * PlayerUpgradeRules.MeteorDirectMultiplier(Specialization, HasMechanic(EquipmentMechanic.CinderTrail));
                if (Specialization == ElementalistSpecialization.Burn && status.BeginMeteorImpact(this, castId))
                {
                    status.Burn(this, 3f, uncriticalDamage * .9f);
                    session.RecordCombatAction("陨星灼烧");
                }
                else if (Specialization != ElementalistSpecialization.Burn && status.TryShatter(this, castId))
                {
                    direct += uncriticalDamage * PlayerUpgradeRules.ShatterMultiplier(Specialization);
                    session.SpawnMechanismText(enemy.transform.position + Vector3.up * 2f, "碎冰！", new Color(.55f, .95f, 1f));
                    CombatFx.Ring(enemy.transform.position, 1.5f, new Color(.55f, .95f, 1f), .4f, .16f);
                    session.RecordCombatAction("碎冰连招");
                    session.RecordClassTutorial(HeroClass.Arcanist);
                }
                return direct;
            }
            if (HeroClass == HeroClass.Ranger && skill == 0)
            {
                float bonus;
                if (status.ConsumePoison(this, castId, out bonus))
                {
                    VenomSkillVfx.Contact(this,EnemyBodyPoint(enemy),true);
                    bool spread = HasMechanic(EquipmentMechanic.VenomSpread) && !ConcentratedVenom;
                    baseDamage += bonus * (spread ? .8f : 1f);
                    session.SpawnMechanismText(enemy.transform.position + Vector3.up * 2f, "三毒引爆！", new Color(.6f, 1f, .3f));
                    session.RecordCombatAction(ConcentratedVenom ? "收束毒爆" : "毒层引爆");
                    session.RecordClassTutorial(HeroClass.Ranger);
                    session.RecordCombatAction("职业能力");
                    if (spread && venomSpreadProc.TryTrigger(2f))
                    {
                        int remaining = 2;
                        foreach (EnemyController nearby in session.Enemies.ToArray())
                            if (ValidAimTarget(nearby) && nearby != enemy && nearby.StatusEffects != null &&
                                CombatFx.Flat(nearby.transform.position - enemy.transform.position).sqrMagnitude <= 3.5f * 3.5f &&
                                CombatSight.Direct(enemy.transform.position, nearby.transform.position))
                            {
                                nearby.StatusEffects.Poison(this, 4f, CombatAttack * .14f);
                                if (--remaining == 0) break;
                            }
                        session.RecordCombatAction("毒种蔓延");
                    }
                }
            }
            return baseDamage;
        }

        internal void ApplySpellDodgeBoon(EnemyController enemy)
        {
            if (HeroClass != HeroClass.Arcanist || classDodgeTime <= 0 || !ValidAimTarget(enemy) || enemy.StatusEffects == null) return;
            classDodgeTime = 0;
            if (Specialization == ElementalistSpecialization.Burn) enemy.StatusEffects.Burn(this, 2f, CombatAttack * .5f);
            else enemy.StatusEffects.FrostMark(4f);
        }

        private bool ReturningCounterReady { get { return HeroClass==HeroClass.Vanguard && ReturningCounterVariant && counterTime>0 && perfectDodgeCounterTime>0; } }

        private bool ReturningCounterVariant
        {
            get
            {
                if (!HasMechanic(EquipmentMechanic.ReturningBlade)) return false;
                ItemData item = session.Progression.Equipped(ItemSlot.Weapon);
                return item != null && item.mechanicVariantUnlocked && item.mechanicVariant == 1;
            }
        }

        internal int MechanicVariant(EquipmentMechanic mechanic)
        {
            ItemData item = session.Progression.Equipped(BuildCatalog.MechanicSlot(mechanic));
            return item != null && item.mechanic == mechanic ? item.mechanicVariant : 0;
        }
        internal int NewCastId() { return IssueCastId(); }
        internal void RegisterSkillHit(int castId){if(session!=null&&session.Player==this&&session.HasStarted&&!session.InputBlocked&&!session.CombatEnded&&!IsDead)masteryCore.SkillHit(CaptureCastReceipt(castId));}
        internal void ElementalAdvancedArea(Vector3 at, float radius, CombatDamage direct, int castId, bool final)
        {
            CombatImpactBatch.Begin();
            try
            {
            int impactEpoch=CombatEpoch;
            DestructibleProp.StrikeArea(this,at,radius,direct,castId);
            foreach (EnemyController enemy in session.Enemies.ToArray())
            {
                if (!ValidAimTarget(enemy) || CombatFx.Flat(enemy.transform.position-at).magnitude > radius + (enemy.IsBoss?.85f:.4f) + enemy.HitFootprintBonus || !CombatSight.Area(at,enemy.transform.position)) continue;
                EnemyStatusEffects status = enemy.StatusEffects;
                var burnPlan=Specialization==ElementalistSpecialization.Burn&&final?status.BeginBurnFinale(this,castId):null;
                EnemyStatusEffects.BurnFinaleSettlement burnSettlement=null;
                if(IsDead||CombatEpoch!=impactEpoch||session.Player!=this||!session.HasStarted||session.CombatEnded)return;
                if(!ValidAimTarget(enemy))continue; // A due old burn may have killed it before the direct hit.
                RegisterSkillHit(castId);
                ApplySpellDodgeBoon(enemy);
                float amount = direct.Amount;
                if (Specialization == ElementalistSpecialization.Burn)
                { amount *= .65f;if(final)burnSettlement=status.CompleteBurnFinale(burnPlan,direct.WithoutCritical().Amount*.55f);else status.Burn(this, 3f, direct.WithoutCritical().Amount * .55f); }
                else if (Specialization == ElementalistSpecialization.Shatter)
                {
                    if (!final) status.FrostMark(4f);
                    else if (status.TryShatter(this, castId)) amount += direct.WithoutCritical().Amount * .6f;
                }
                float healthBeforeFinale=enemy.Health;
                enemy.TakeDamage(amount, Vector3.zero, 0, final?.3f:0, critical:direct.IsCritical,practiceCastId:castId);
                if(burnSettlement!=null&&burnSettlement.Apply())RecordBurnCash(castId,impactEpoch,enemy.transform.position);
                if(final&&enemy.Health<healthBeforeFinale)FilledSkillVfx.ConfirmFinale(this,castId);
            }

            }
            finally {CombatImpactBatch.End();}
        }

        internal void ApplyNovaStatus(EnemyController enemy, int rank)
        {
            if (!ValidAimTarget(enemy) || enemy.StatusEffects == null) return;
            if (Specialization == ElementalistSpecialization.Burn) enemy.StatusEffects.Slow(3.5f, .4f);
            else enemy.StatusEffects.Freeze(PlayerUpgradeRules.NovaFreezeDuration(rank));
        }

        public void NotifyPerfectDodge()
        {
            if (IsDead || session == null || !session.HasStarted || perfectDodgeAwarded || perfectDodgeWindow <= 0) return;
            perfectDodgeAwarded = true;
            float previousEnergy = Energy;
            skillRuntime.RestoreEnergy(PlayerUpgradeRules.PerfectDodgeEnergy);
            if (HeroClass == HeroClass.Vanguard) counterWindowDuration = counterTime = perfectDodgeCounterTime = ReturningCounterVariant ? 3f : PlayerUpgradeRules.CounterWindow;
            else classDodgeTime = 3f;
            if (HeroClass == HeroClass.Summoner) { SummonedCompanion.OnPerfectDodge(this); }
            float ward=masteryCore.PerfectDodge();if(ward>0){coreWardTime=ward;session.RecordCombatAction("守御核心");}
            string reward = HeroClass == HeroClass.Vanguard ? "反击" : HeroClass == HeroClass.Ranger ? "精准箭" : HeroClass == HeroClass.Summoner ? "护契·协同" : Specialization == ElementalistSpecialization.Burn ? "余烬" : "霜痕";
            session.SpawnMechanismText(transform.position + Vector3.up * 2.5f, "完美闪避 +" + (Energy-previousEnergy).ToString("0.#") + "能量 · " + reward, new Color(.65f, .95f, 1f));
            session.RecordCombatAction("完美闪避");
        }

        public void OnMarkedEnemyKilled()
        {
            if (session != null && !IsDead && session.HasBlessing(RunBlessing.MarkedPursuit)) pursuitTime = 2.5f;
        }

        internal void OnStarterCompanionDefeated() { starterRetry = 12f; }

        private void MaintainStarterCompanion(float dt)
        {
            if (HeroClass != HeroClass.Summoner || !session.HasStarted) return;
            SummonedCompanion.EnforceCapacity(this);
            starterRetry = Mathf.Max(0, starterRetry - dt);
            if (starterRetry > 0 || SummonedCompanion.HasStarter(this)) return;
            SummonedCompanion.SummonStarter(this, session, stats.Damage);
            starterRetry = 12f;
        }

        internal void RestoreSkillEnergy(float amount) { skillRuntime.RestoreEnergy(amount); }

        internal void HealingProtection(int rank)
        {
            if (rank < 2) return;
            healingProtectionTime = 5.2f;
            healingReduction = rank==3?.25f:.18f;
        }

        internal void MobilityBuff(int rank) { mobilityTime=4f+rank; mobilityRank=rank; }

        internal void ControlArea(Vector3 at,float radius,float duration)
        {
            for(int i=0;i<session.Enemies.Count;i++)
            {
                EnemyController enemy=session.Enemies[i];
                if(enemy!=null && !enemy.IsDead && CombatFx.Flat(enemy.transform.position-at).magnitude<radius && CombatSight.Area(at,enemy.transform.position))
                    enemy.ApplyControl(duration);
            }
        }

        private void TryDefensePassive()
        {
            int rank=session.Progression.Profile.skillRanks[8];
            if(rank<=0 || passiveCooldown>0 || (HeroClass==HeroClass.Vanguard && Health>MaxHealth*.35f)) return;
            passiveCooldown=rank==3?30f:rank==2?38f:45f;
            passiveTime=2f+rank;
            passiveReduction=0;
            passiveSpeed=0;
            float radius=3f*GameBalance.SkillRangeMultiplier(rank);
            Color tint=GameBalance.ClassColor(HeroClass);
            if(HeroClass==HeroClass.Vanguard)
            {
                passiveReduction=.2f+rank*.1f;
                if(rank==3) HitArea(transform.position,radius,CombatAttack*1.6f,.9f,.65f);
            }
            else if(HeroClass==HeroClass.Arcanist || HeroClass==HeroClass.Summoner)
            {
                passiveReduction=(HeroClass==HeroClass.Summoner?.25f:.3f)+rank*.1f;
                skillRuntime.RestoreEnergy(2f+rank*2f);
                if(rank==3) ControlArea(transform.position,radius,1.5f);
            }
            else
            {
                invulnerability=Mathf.Max(invulnerability,.1f+rank*.15f);
                passiveSpeed=.1f+rank*.05f;
                if(rank==3) skillRuntime.RestoreEnergy(SkillDamageBudgets.BasicEnergyOnHit);
            }
            AdvancedSkillVfx.Protection(this,transform.position,radius,tint,passiveTime,rank,()=>passiveTime>0,passive:true);
            session.SpawnMechanismText(transform.position+Vector3.up*2.7f,GameBalance.SkillName(HeroClass,8),tint);
        }

        internal void HitArea(Vector3 at, float radius, CombatDamage damage, float knockback = 0, float stun = 0, int castId = 0, ProjectileVolleyBudget<EnemyController> volley = null)
        {
            CombatImpactBatch.Begin();
            try
            {
            bool confirmedSkillCast=castId>0;
            if(castId==0)castId=NewCastId();
            DestructibleProp.StrikeArea(this,at,radius,damage,castId);
            for (int i = session.Enemies.Count - 1; i >= 0; i--)
            {
                EnemyController enemy = session.Enemies[i];
                if (enemy == null || enemy.IsDead) continue;
                Vector3 delta = CombatFx.Flat(enemy.transform.position - at);
                if (delta.magnitude <= radius + (enemy.IsBoss ? .85f : .4f) + enemy.HitFootprintBonus && CombatSight.Area(at,enemy.transform.position))
                {
                    var impact=volley==null?damage:volley.Apply(enemy,damage,true);if(impact.Amount<=0)continue;
                    if(confirmedSkillCast)RegisterSkillHit(castId);ApplySpellDodgeBoon(enemy);
                    float healthBeforeFinale=enemy.Health;
                    enemy.TakeDamage(impact.Amount,delta.normalized,knockback,stun,critical:impact.IsCritical,practiceCastId:castId);
                    // Confirm after the real mutation even if its death callback just finished the mode.
                    if(enemy.Health<healthBeforeFinale)FilledSkillVfx.ConfirmFinale(this,castId);
                }
            }

            }
            finally {CombatImpactBatch.End();}
        }

        internal void SkillDash(Vector3 direction, float distance, float protection)
        {
            if (jumping) return;
            Vector3 flat = CombatFx.Flat(direction).normalized;
            Vector3 previous = transform.position;
            Vector3 destination = Vector3.ClampMagnitude(CombatFx.Flat(previous) + flat * distance,session.ArenaRadius-.65f);
            if (!WorldTraversal.CanLeap(previous, destination, .45f)) { TraversalFailure(); return; }
            transform.position = destination;
            invulnerability = Mathf.Max(invulnerability,protection);
            AdvancedSkillVfx.Beam(this,previous+Vector3.up,transform.position+Vector3.up,GameBalance.ClassColor(HeroClass),.55f,.35f);
        }

        internal bool TryJump()
        {
            if (session == null || IsDead || !session.HasStarted || session.InputBlocked || jumping || TraversalStartedThisFrame || movementSkillLock > 0 || (charge != null && charge.IsCharging)) return false;
            Vector3 origin = CombatFx.Flat(transform.position);
            if (!WorldTraversal.IsWalkable(origin, .45f)) return false;
            jumpOrigin = origin;
            jumpAge = 0;
            jumping = true;
            traversalFrame = Time.frameCount;
            GameAudio.Play(SoundCue.Dodge);
            return true;
        }

        internal void CancelCombatPose() { skillBasicRecovery.Clear(); if (model != null) model.CancelAction(); }
        private float counterWindowDuration;
        internal float CounterOpportunityDuration {get{return counterWindowDuration;}}
        internal float CounterOpportunityRemaining { get { return IsDead ? 0 : counterTime; } }
        internal EnemyController CurrentOpportunityTarget { get { return ValidAimTarget(AimTarget) ? AimTarget : null; } }

        internal bool TryBlink(Vector3 direction) { return TryBlinkCore(direction, true); }

        private bool TryBlinkCore(Vector3 direction, bool allowBuffer)
        {
            if (session == null || IsDead || !session.HasStarted || session.InputBlocked) { blinkBufferTime = 0; return false; }
            if (jumping || dodgeCooldown > 0 || TraversalStartedThisFrame || movementSkillLock > 0)
            {
                if (allowBuffer)
                {
                    if (PlayerUpgradeRules.CanBufferDodge(dodgeCooldown, movementSkillLock, jumping ? Mathf.Max(0, .55f - jumpAge) : 0))
                    { blinkBufferTime = PlayerUpgradeRules.DodgeBufferWindow; bufferedBlinkDirection = direction; }
                    else if (skillFeedbackCooldown <= 0)
                    {
                        session.ReportControlFailure("dodge",dodgeCooldown>0?"冷却":jumping?"空中":"位移中");
                        session.Notify(dodgeCooldown > 0 ? "闪避冷却中（" + dodgeCooldown.ToString("0.0") + " 秒）" : jumping ? "落地后才能闪避。" : "位移结束后才能闪避。");
                        skillFeedbackCooldown = .5f;
                    }
                }
                return false;
            }
            Vector3 forward = CombatFx.Flat(direction);
            if (forward.sqrMagnitude < .01f) forward = transform.forward;
            Vector3 origin = CombatFx.Flat(transform.position);
            Vector3 destination;
            blinkBufferTime = 0;
            if (!WorldTraversal.TryResolveBlink(origin, forward, 4.8f, .45f, session.ArenaRadius - .65f, out destination))
            { TraversalFailure(); return false; }
            if (charge != null) charge.Cancel();
            CancelCombatPose();
            model.ResetLocomotion();
            transform.position = destination;
            dodgeCooldown = 2.1f;
            invulnerability = Mathf.Max(invulnerability, .38f);
            traversalFrame = Time.frameCount;
            perfectDodgeWindow = .55f; perfectDodgeAwarded = false;
            foreach (EnemyController enemy in session.Enemies)
                if (ValidAimTarget(enemy)) enemy.TryRegisterPerfectDodge(origin, destination);
            CombatProjectile.RegisterDodge(this, origin, destination);
            if (session.HasBlessing(RunBlessing.DodgeShock)) dodgeShockTime = 2f;
            session.RecordCombatAction("成功闪避");
            Color color = GameBalance.ClassColor(HeroClass);
            AdvancedSkillVfx.Rune(this, origin, .9f, color, .3f, 1);
            AdvancedSkillVfx.Rune(this, destination, 1.1f, color, .38f, 1);
            AdvancedSkillVfx.Beam(this, origin + Vector3.up, destination + Vector3.up, color, .28f, .14f);
            GameAudio.Play(SoundCue.Dodge);
            return true;
        }

        private void AdvanceJump(float deltaTime)
        {
            if (!jumping || deltaTime <= 0 || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime)) return;
            jumpAge += deltaTime;
            float progress = Mathf.Clamp01(jumpAge / .55f);
            transform.position = jumpOrigin + Vector3.up * (Mathf.Sin(progress * Mathf.PI) * 1.65f);
            if (progress >= 1f)
            {
                jumping = false;
                transform.position = jumpOrigin;
            }
        }

        private void TraversalFailure()
        {
            if (skillFeedbackCooldown > 0) return;
            session.ReportControlFailure("dodge","无落点");
            session.Notify("前方有障碍或没有安全落点，请走桥或调整方向。");
            skillFeedbackCooldown = .8f;
        }

        private bool CanUseMovementSkill(int skill, int rank)
        {
            bool forwardDash = HeroClass == HeroClass.Vanguard && skill == 5;
            bool retreat = HeroClass == HeroClass.Ranger && skill == 4;
            if (!forwardDash && !retreat) return true;
            Vector3 direction = CombatFx.Flat((ValidAimTarget(AimTarget) ? AimTarget.transform.position : aimPoint) - transform.position);
            if (direction.sqrMagnitude < .0001f) direction = transform.forward;
            direction.Normalize();
            if (retreat) direction = -direction;
            float distance = (forwardDash ? 7f : 5f) * GameBalance.SkillRangeMultiplier(rank);
            Vector3 end = Vector3.ClampMagnitude(CombatFx.Flat(transform.position) + direction * distance, session.ArenaRadius - .65f);
            return WorldTraversal.CanLeap(transform.position, end, .45f);
        }

        internal bool SkillTargetingReady(int skill)
        {
            if(session==null||IsDead||!session.HasStarted||session.InputBlocked||jumping||skill<0||skill>=GameBalance.SkillCount||GameBalance.IsPassive(skill)||
                charge!=null&&(charge.IsCharging||charge.ConsumedThisFrame))return false;
            int rank=session.Progression.Profile.skillRanks[skill];
            return rank>0&&skillRuntime.Remaining(skill)<=0&&Energy>=GameBalance.SkillEnergyCost(HeroClass,skill)&&CanUseMovementSkill(skill,rank);
        }
        internal Vector3 ResolveSkillGroundTarget(Vector3 point,float range,bool fixedPoint=false)
        {
            Vector3 target=fixedPoint?point:transform.position+Vector3.ClampMagnitude(CombatFx.Flat(point-transform.position),9f*range);
            return CombatSight.GroundPoint(transform.position,Vector3.ClampMagnitude(target,session.ArenaRadius));
        }
        public bool CanShatterNow(int skill=1)
        {
            if(HeroClass!=HeroClass.Arcanist||Specialization==ElementalistSpecialization.Burn||skill!=1||!SkillTargetingReady(skill))return false;
            if(!MobilePinnedActionAllowed(skill,false))return false;
            return ElementalOpportunityRemaining(skill,CombatOpportunityKind.Shatter)>0;
        }

        internal bool CanBeginSkillTargeting(int skill)
        {
            if(SkillTargetingReady(skill))return true;
            if(session==null || IsDead || !session.HasStarted || session.InputBlocked || skill<0 || skill>=GameBalance.SkillCount || GameBalance.IsPassive(skill)) return false;
            if (jumping) { session.ReportControlFailure("skill"+skill,"空中"); return false; }
            if(charge != null && (charge.IsCharging || charge.ConsumedThisFrame)) { session.ReportControlFailure("skill"+skill,"施法中"); return false; }
            int rank=session.Progression.Profile.skillRanks[skill];
            string failure=null;
            if(rank<=0) failure="按 K 学习这个技能后再施放。";
            else if(skillRuntime.Remaining(skill)>0) failure=GameBalance.SkillName(HeroClass,skill)+" 冷却中（"+skillRuntime.Remaining(skill).ToString("0.0")+" 秒）";
            else if(Energy<GameBalance.SkillEnergyCost(HeroClass,skill)) failure="能量不足：普攻命中回复 8 点，持续回复每秒 4 点。";
            else if(!CanUseMovementSkill(skill,rank)) failure="前方有障碍或没有安全落点，请走桥或调整方向。";
            if(failure==null) return true;
            session.ReportControlFailure("skill"+skill,rank<=0?"未学":skillRuntime.Remaining(skill)>0?"冷却":Energy<GameBalance.SkillEnergyCost(HeroClass,skill)?"缺能":"无落点");
            if (CombatReviewEvents.Enabled && Energy<GameBalance.SkillEnergyCost(HeroClass,skill)) CombatReviewEvents.Emit("noenergy",CombatReviewObjectId.Get(this),skill:skill);
            if(skillFeedbackCooldown<=0) { session.Notify(failure); skillFeedbackCooldown=.8f; }
            return false;
        }

        internal bool CastImmediateSkill(int skill)
        {
            if (CombatReviewEvents.Enabled) CombatReviewEvents.Emit("skillattempt",CombatReviewObjectId.Get(this),skill:skill);
            if(SkillTargetingController.RequiresConfirmation(HeroClass,skill) || !CanBeginSkillTargeting(skill) || !MobilePinnedActionAllowed(skill,true)) return false;
            // Mouse aim is resolved independently of movement. Re-read a selected
            // living target's position here so immediate directional casts face it.
            FaceAim();
            if (SkillChargeController.Duration(HeroClass, skill) > 0) return charge.Begin(skill);
            CastSkill(skill);
            return true;
        }

        internal bool ConfirmTargetedSkill(int skill,Vector3 worldPoint)
        {
            if (CombatReviewEvents.Enabled) CombatReviewEvents.Emit("skillattempt",CombatReviewObjectId.Get(this),skill:skill);
            if(!SkillTargetingController.RequiresConfirmation(HeroClass,skill) || !CanBeginSkillTargeting(skill) || !MobilePinnedActionAllowed(skill,true)) return false;
            EnemyController selected=AimTarget;
            aimPoint=CombatSight.GroundPoint(transform.position,worldPoint);
            // Ground spell placement normally owns a point. A selected summon
            // contract also owns that enemy until charge snapshots it; clearing
            // here would lose mobile autoaim before Begin can capture identity.
            AimTarget=HeroClass==HeroClass.Summoner&&skill==9&&ValidAimTarget(selected)&&
                CombatSight.Direct(transform.position,selected.transform.position)&&
                CombatFx.Flat(selected.transform.position-aimPoint).sqrMagnitude<=1f?selected:null;
            FaceAim();
            if (SkillChargeController.Duration(HeroClass, skill) > 0) return charge.Begin(skill);
            CastSkill(skill);
            return true;
        }

        internal bool ExecuteChargedSkill(int skill)
        {
            if (charge == null || !CanBeginSkillTargeting(skill)) return false;
            AimTarget = null;
            aimPoint = charge.TargetPoint;
            transform.rotation = Quaternion.LookRotation(charge.Direction);
            executingChargedSkill = true;
            try { CastSkill(skill); }
            finally { executingChargedSkill = false; }
            return true;
        }

        public void ApplySlow(float duration, float strength)
        {
            if (IsDead || duration <= 0 || strength <= 0 || float.IsNaN(duration) || float.IsInfinity(duration) || float.IsNaN(strength) || float.IsInfinity(strength)) return;
            slowTime = Mathf.Max(slowTime, duration);
            slowStrength = Mathf.Max(slowStrength, Mathf.Clamp(strength, 0, .7f));
            CombatFx.Ring(transform.position, .9f, new Color(.42f, .85f, .3f), .3f, .08f);
        }

        private void CastSkill(int slot)
        {
            castDamageRoll = Random.value;
            try { CastSkillCore(slot); }
            finally { castDamageRoll = -1; }
        }

        private void CastSkillCore(int slot)
        {
            CombatImpactBatch.BeginAction();
            try
            {
            if (slot < 0 || slot >= GameBalance.SkillCount || GameBalance.IsPassive(slot)) return;
            int rank = session.Progression.Profile.skillRanks[slot];
            if (rank <= 0)
            {
                if (skillFeedbackCooldown <= 0)
                {
                    session.Notify("按 K 打开技能面板，升级后消耗技能点学习技能。");
                    skillFeedbackCooldown = 2f;
                }
                return;
            }
            if (!CanUseMovementSkill(slot, rank)) { TraversalFailure(); return; }
            // Limited healing rank one has no defensive benefit: do not pay for an empty heal.
            if (slot == 6 && rank == 1 && session.ChallengeRun && session.InDungeon && Health >= MaxHealth
                && (HeroClass != HeroClass.Summoner || !SummonedCompanion.HasHealingTarget(this)))
            { session.Notify("生命已满，无需使用治疗技能。"); return; }
            if (slot == 6 && skillRuntime.Remaining(slot) <= 0 && Energy >= GameBalance.SkillEnergyCost(HeroClass, slot) && !session.TrySpendHealingCharge()) return;
            if (!skillRuntime.TryConsume(slot, rank, ActiveRunBonuses == null ? 1f : ActiveRunBonuses.CooldownMultiplier))
            {
                if (skillFeedbackCooldown <= 0)
                {
                    float remaining = skillRuntime.Remaining(slot);
                    session.Notify(remaining > 0 ? GameBalance.SkillName(HeroClass,slot) + " 冷却中（" + remaining.ToString("0.0") + " 秒）" : "能量不足：需要 " + GameBalance.SkillEnergyCost(HeroClass,slot) + " 点；普攻命中回复 8 点，持续回复每秒 4 点。");
                    skillFeedbackCooldown = .8f;
                }
                return;
            }
            MasteryResourceProc resourceProc=masteryCore.SkillSpent(GameBalance.SkillEnergyCost(HeroClass,slot));
            if(resourceProc.Energy>0){skillRuntime.RestoreEnergy(resourceProc.Energy);skillRuntime.ReduceCooldowns(resourceProc.CooldownReduction);session.RecordCombatAction("循能核心");}
            int castId = NewCastId();HoldCastReceipt(ref skillCastReceipt,castId);
            session.RecordPracticeCast(castId,slot);
            session.RecordCombatAction("职业能力");
            if (executingChargedSkill && session.HasBlessing(RunBlessing.ChargedWard)) chargedWardTime = Mathf.Max(chargedWardTime, 2f);
            GameAudio.Play(SoundCue.Cast);
            if (CombatReviewEvents.Enabled) CombatReviewEvents.Emit("skillrelease",CombatReviewObjectId.Get(this),skill:slot);
            if ((HeroClass == HeroClass.Vanguard && slot == 5) || (HeroClass == HeroClass.Ranger && slot == 4)) movementSkillLock = .15f;
            skillBasicRecovery.Begin(HeroClass, slot, executingChargedSkill);
            if (executingChargedSkill) model.ReleaseCharge(slot);
            else model.PlayAction(slot,false);
            attackAnimation = 1;
            float power = 1f + (rank-1)*.3f;
            float range = GameBalance.SkillRangeMultiplier(rank);
            Color color = GameBalance.ClassColor(HeroClass);
            Vector3 target = ResolveSkillGroundTarget(executingChargedSkill ? charge.TargetPoint : aimPoint,range,executingChargedSkill);
            if (HeroClass == HeroClass.Summoner)
            {
                if (slot == 5)
                {
                    guardTime = 6f + (rank - 1) * 2f;
                    guardRank = rank;guardCastId=castId;HoldCastReceipt(ref guardCastReceipt,castId); guardReduction = .25f + rank * .1f;
                    AdvancedSkillVfx.Protection(this, transform.position, 2.8f * range, color, guardTime, rank + 1, ()=>guardTime>0);
                }
                else
                {
                    SummonerSpell.Cast(this, session, slot, rank, target, (slot == 2 || slot == 4 || slot == 9 ? stats.Damage : CombatAttack) * power, executingChargedSkill ? charge.TargetEnemy : AimTarget, executingChargedSkill,castId);
                    if (slot == 0)
                        foreach (EnemyController enemy in session.Enemies.ToArray())
                        {
                            if (!ValidAimTarget(enemy)) continue;
                            Vector3 offset = CombatFx.Flat(enemy.transform.position - transform.position);
                            if (offset.magnitude <= 5f * range && (offset.sqrMagnitude < .1f || Vector3.Angle(transform.forward, offset) < 55f) && CombatSight.Melee(transform.position,enemy.transform.position))
                                enemy.TrySkillInterrupt(this, slot, castId);
                        }
                }
                return;
            }
            if (slot >= 3)
            {
                if (HeroClass == HeroClass.Vanguard && slot == 4)
                {
                    guardTime = 6f+(rank-1)*2f; guardPower = 1.2f * power;
                    guardReduction=.55f+rank*.05f; guardRadius=3.2f*range; guardRank=rank;guardCastId=castId;HoldCastReceipt(ref guardCastReceipt,castId);
                    AdvancedSkillVfx.Protection(this,transform.position,2.1f*range,new Color(1f,.84f,.4f),guardTime,rank,()=>guardTime>0);
                }
                else if (HeroClass == HeroClass.Arcanist && slot == 5)
                {
                    guardTime=6f+(rank-1)*2f; guardRank=rank;guardCastId=castId;HoldCastReceipt(ref guardCastReceipt,castId); guardReduction=.25f+rank*.1f;
                    guardRadius=2.8f*range; guardPulseTimer=0;
                    if (Specialization == ElementalistSpecialization.Burn) { guardReduction=.3f; burnStrideTime=guardTime; }
                    AdvancedSkillVfx.Protection(this,transform.position,guardRadius,Specialization==ElementalistSpecialization.Burn?new Color(1f,.55f,.25f):new Color(.55f,.92f,1f),guardTime,rank+1,()=>guardTime>0);
                }
                else AdvancedSkillSequence.Spawn(this,session,slot,rank,target,transform.forward,Damage(power * SkillDamageBudgets.AdvancedScale(HeroClass,slot)),color,castId);
                return;
            }
            if(rank>=2) AdvancedSkillVfx.Rune(this,slot==0?transform.position:target,3.1f*range,color,.8f,rank);
            if (HeroClass == HeroClass.Vanguard)
            {
                if (slot == 0)
                {
                    if(!BlenderSkillVfx.TryPlay(this,range,false)) CombatFx.Ring(transform.position,3.4f*range,color,.45f,.2f);
                    Melee(3.4f*range,360,Damage(SkillDamageBudgets.OpeningImpact(HeroClass,slot,rank)),.75f,.3f,skillIndex:slot,castId:castId);
                    if(rank>=2) CombatArea.Spawn(this,session,transform.position,3.4f*range,Damage(SkillDamageBudgets.OpeningImpact(HeroClass,slot,rank,1)),.25f,.18f,rank==3?.22f:0,.22f,color,true,false,rank==3?5f:0,rank==3?Damage(SkillDamageBudgets.OpeningImpact(HeroClass,slot,rank,2)):0,castId:castId,visual:SkillVisualRecipe.Steel);
                }
                else if (slot == 1)
                {
                    if(!BlenderSkillVfx.TryPlay(this,range,true)) CombatFx.WeaponSlash(this,model,transform.position,transform.forward,4.8f*range,new Color(1f,.85f,.4f));
                    else WeaponSlashRibbon.Spawn(this,model,new Color(1f,.85f,.4f));
                    Melee(4.8f*range,90+(rank-1)*10,Damage(SkillDamageBudgets.OpeningImpact(HeroClass,slot,rank)),1.9f,1.3f+(rank-1)*.3f,1.3f+(rank-1)*.3f,skillIndex:slot,castId:castId);
                    CombatFx.Ring(transform.position+transform.forward*2.5f*range,2.1f*range,color,.4f,.16f);
                    if(rank>=2) CombatArea.Spawn(this,session,CombatSight.GroundPoint(transform.position,transform.position+transform.forward*3f*range),2.3f*range,Damage(SkillDamageBudgets.OpeningImpact(HeroClass,slot,rank,1)),.6f,.25f,0,1,color,false,false,0,rank==3?Damage(SkillDamageBudgets.OpeningImpact(HeroClass,slot,rank,2)):0,castId:castId,visual:SkillVisualRecipe.Steel);
                }
                else
                {
                    invulnerability = Mathf.Max(invulnerability,.5f);
                    PeriodicSkillBudget field=SkillDamageBudgets.EarlyField(HeroClass,rank);
                    CombatArea.Spawn(this,session,transform.position,4.1f*range,Damage(field.TickCoefficient),.14f,field.Startup,field.Duration,field.Interval,color,true,false,rank==3?2.5f:0,Damage(field.FinisherCoefficient),castId:castId,visual:SkillVisualRecipe.Steel);
                }
            }
            else if (HeroClass == HeroClass.Arcanist)
            {
                if (slot == 0)
                {
                    bool frostEcho = HasMechanic(EquipmentMechanic.FrostEcho);
                    bool wideEcho = MechanicVariant(EquipmentMechanic.FrostEcho) == 1;
                    float novaPower = frostEcho ? BuildCatalog.FrostEchoOpeningMultiplier(wideEcho) : 1f;
                    CombatArea.Spawn(this,session,transform.position,3.7f*range,Damage(SkillDamageBudgets.OpeningImpact(HeroClass,slot,rank)*novaPower),0,0,0,1f,new Color(.51f,.92f,1f),statusSkill:0,statusRank:rank,castId:castId,visual:SkillVisualRecipe.Ice);
                    if (frostEcho)
                    {
                        CombatArea.Spawn(this,session,transform.position,3.7f*range*BuildCatalog.FrostEchoRadiusMultiplier(wideEcho),Damage(BuildCatalog.FrostEchoCoefficient(wideEcho)*power),0,.7f,0,1f,new Color(.51f,.92f,1f),statusSkill:0,statusRank:rank,castId:castId,visual:SkillVisualRecipe.Ice,trackedMechanic:1);
                        session.RecordCombatAction("霜环回响");
                    }
                    if(rank>=2) CombatArea.Spawn(this,session,transform.position,3.7f*range,Damage(SkillDamageBudgets.OpeningImpact(HeroClass,slot,rank,1)),0,.5f,0,1f,new Color(.51f,.92f,1f),statusSkill:0,statusRank:rank,castId:castId,visual:SkillVisualRecipe.Ice);
                    if(rank==3)
                    {
                        var shards=new ProjectileVolleyBudget<EnemyController>(CombatAttack,1.8f);
                        for(int i=0;i<8;i++)
                        {
                        Vector3 shard=Quaternion.Euler(0,i*45f,0)*Vector3.forward;
                        var shardDamage=Damage(SkillDamageBudgets.OpeningImpact(HeroClass,slot,rank,2));
                        Vector3 muzzle=transform.position+shard*.5f;
                        if(CombatProjectile.CanLaunchFromMuzzle(this,muzzle,shardDamage,castId))CombatProjectile.Friendly(this,session,muzzle,shard,shardDamage,new Color(.51f,.92f,1f),true,false,false,range,16f*range,castId:castId,volley:shards);
                        }
                    }
                }
                else if (slot == 1)
                {
                    CombatArea.Spawn(this,session,target,3f*range,Damage(SkillDamageBudgets.OpeningImpact(HeroClass,slot,rank)),.7f,.7f,0,1f,new Color(1f,.59f,.28f),false,true,statusSkill:1,statusRank:rank,castId:castId,visual:SkillVisualRecipe.Fire);
                    if(rank>=2) CombatArea.Spawn(this,session,target,3f*range,Damage(SkillDamageBudgets.OpeningImpact(HeroClass,slot,rank,1)),.3f,1.1f,0,1,new Color(1f,.59f,.28f),false,true,statusSkill:1,statusRank:rank,castId:castId,visual:SkillVisualRecipe.Fire);
                    if(rank==3){var field=SkillDamageBudgets.MeteorAftermath(rank);CombatArea.Spawn(this,session,target,3.2f*range,Damage(field.TickCoefficient),.1f,field.Startup,field.Duration,field.Interval,new Color(1f,.43f,.22f),castId:castId,visual:SkillVisualRecipe.Fire);}
                    if (HasMechanic(EquipmentMechanic.CinderTrail))
                    {
                        // Four ticks total 40% of the base first meteor impact.
                        CombatArea.Spawn(this,session,target,3f*range*BuildCatalog.CinderTrailRadiusMultiplier(MechanicVariant(EquipmentMechanic.CinderTrail)==1),CombatAttack*SkillDamageBudgets.MeteorTrailTick(rank,MechanicVariant(EquipmentMechanic.CinderTrail)==1),0,1.2f,1.5f,.5f,new Color(1f,.43f,.22f),castId:castId,visual:SkillVisualRecipe.Fire,trackedMechanic:0);
                        session.RecordCombatAction("余烬地带");
                    }
                }
                else {var field=SkillDamageBudgets.EarlyField(HeroClass,rank);CombatArea.Spawn(this,session,target,3.9f*range,Damage(field.TickCoefficient),.22f,field.Startup,field.Duration,field.Interval,new Color(.65f,.5f,1f),false,false,rank==3?3.5f:0,Damage(field.FinisherCoefficient),castId:castId,visual:SkillVisualRecipe.Lightning);}
            }
            else
            {
                if (slot == 0)
                {
                    if (ConcentratedVenom)
                    {
                        CastConcentratedVenom(rank, range, color, castId);
                        return;
                    }
                    int arrows = 5+(rank-1)*2;
                    var volley=new ProjectileVolleyBudget<EnemyController>(CombatAttack,SkillDamageBudgets.FanTargetCap(rank));
                    for (int i=0;i<arrows;i++)
                    {
                        Vector3 dir=Quaternion.Euler(0,Mathf.Lerp(-25,25,i/(float)(arrows-1)),0)*transform.forward;
                        var arrowDamage=Damage(SkillDamageBudgets.OpeningImpact(HeroClass,slot,rank));
                        Vector3 muzzle=transform.position+dir*.6f;
                        if(CombatProjectile.CanLaunchFromMuzzle(this,muzzle,arrowDamage,castId))CombatProjectile.Friendly(this,session,muzzle,dir,arrowDamage,color,true,true,false,range,20f*range,null,rank==3?Damage(SkillDamageBudgets.OpeningImpact(HeroClass,slot,rank,1)):0,1.25f*range,skillIndex:0,castId:castId,volley:volley);
                    }
                }
                else if (slot == 1)
                {
                    CombatArea.Spawn(this,session,target,3f*range,Damage(SkillDamageBudgets.OpeningImpact(HeroClass,slot,rank)),2.3f+(rank-1)*.4f,.4f,0,1f,color,false,false,rank==3?5f:0,statusSkill:1,statusRank:rank,castId:castId,visual:SkillVisualRecipe.Neutral);
                    if(rank>=2) CombatArea.Spawn(this,session,target,3f*range,Damage(SkillDamageBudgets.OpeningImpact(HeroClass,slot,rank,1)),.5f,.8f,0,1,color,castId:castId,visual:SkillVisualRecipe.Neutral);
                }
                else {var field=SkillDamageBudgets.EarlyField(HeroClass,rank);CombatArea.Spawn(this,session,target,4.3f*range,Damage(field.TickCoefficient),.08f,field.Startup,field.Duration,field.Interval,new Color(.7f,1f,.59f),false,false,0,Damage(field.FinisherCoefficient),castId:castId,visual:SkillVisualRecipe.ArrowRain);}
            }

            }
            finally { CombatImpactBatch.EndAction(); }
        }

    }
}
