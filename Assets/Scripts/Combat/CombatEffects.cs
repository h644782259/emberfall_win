using System.Collections.Generic;
using UnityEngine;

namespace Emberfall
{
    internal static class CombatFx
    {
        public static GameObject Ring(Vector3 center, float radius, Color color, float lifetime, float width = .1f, bool expand = true, bool respectCover = false)
        {
            if (expand && FadingCombatEffect.ActiveCount >= (Application.isMobilePlatform ? 32 : 48)) return null;
            GameObject obj = new GameObject("Combat Ring");
            obj.transform.position = center + Vector3.up * .065f;
            if(expand&&CombatVisualLease.Attach(obj,CombatVisualPriority.Decoration)==null)return null;
            LineRenderer line = obj.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = 64;
            for (int i = 0; i < 64; i++)
            {
                float angle = i * Mathf.PI * 2f / 64f;
                line.SetPosition(i, new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle)));
            }
            // Player areas are light rims, not opaque floor coverage. Enemy threat
            // boundaries use their own higher-priority renderer and remain distinct.
            if (radius >= 2.5f) color.a = Mathf.Min(color.a, Application.isMobilePlatform ? .48f : .66f);
            line.widthMultiplier = Mathf.Min(width, radius >= 2.5f ? .12f : width);
            line.sortingOrder = 5;
            line.numCornerVertices = 4;
            line.numCapVertices = 4;
            line.sharedMaterial = NewGlow();
            line.startColor = line.endColor = color;
            obj.AddComponent<FadingCombatEffect>().Setup(line, color, radius, lifetime, expand, respectCover: respectCover);
            return obj;
        }

        // Remote/area/pet ornament only: it never claims the player's current sword swing.
        public static void Slash(Vector3 center, Vector3 forward, float radius, Color color)
        {
            var game=GameSession.Instance;
            if(game!=null)FilledSkillVfx.Crescent(game.Player,center,forward,radius,color,1,CombatVisualPriority.Decoration);
        }
        public static void WeaponSlash(PlayerController owner,CombatModel model,Vector3 center,Vector3 forward,float radius,Color color)
        {
            if(owner==null||model==null||!model.SwordActionActive)return;
            FilledSkillVfx.Crescent(owner,center,forward,radius,color,model.WeaponSwingSide,CombatVisualPriority.ActionBody);
            WeaponSlashRibbon.Spawn(owner,model,color);
        }

        internal static void BurnContact(PlayerController owner,Vector3 point,bool finale=false)
        {FilledSkillVfx.BurnContact(owner,point,finale);}

        public static Material NewGlow()
        {
            Material material = new Material(Shader.Find("Sprites/Default"));
            material.enableInstancing = true;
            return material;
        }

        public static float SegmentDistance(Vector3 point, Vector3 a, Vector3 b)
        {
            point.y = a.y = b.y = 0;
            Vector3 direction = b - a;
            float fraction = direction.sqrMagnitude < .0001f ? 0 : Mathf.Clamp01(Vector3.Dot(point - a, direction) / direction.sqrMagnitude);
            return Vector3.Distance(point, a + direction * fraction);
        }

        public static Vector3 Flat(Vector3 vector) { vector.y = 0; return vector; }
    }

    internal sealed class FadingCombatEffect : MonoBehaviour
    {
        private LineRenderer line;
        private GameSession session;
        private PlayerController playerGeneration;
        private int epoch;
        private bool combatBound;
        private Color color;
        private float radius, lifetime, age;
        private bool expand, slash, registered, respectCover;
        private readonly Vector3[] boundary=new Vector3[64];
        private int boundaryRevision=-1;private Vector3 boundaryCenter;
        public static int ActiveCount { get; private set; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCount() { ActiveCount = 0; }
        public void Setup(LineRenderer renderer, Color value, float size, float duration, bool growing, bool ribbon = false, bool respectCover = false)
        {
            session = GameSession.Instance;
            playerGeneration = session != null ? session.Player : null;
            epoch = playerGeneration != null ? playerGeneration.CombatEpoch : -1;
            combatBound = session != null && session.HasStarted && playerGeneration != null;
            line = renderer; color = value; radius = size; lifetime = Mathf.Max(.05f, duration); expand = growing; slash = ribbon;
            color.a *= growing ? Mathf.Lerp(.4f,1f,EffectPreferences.EffectsScale) : 1f;
            this.respectCover=respectCover; registered = true; ActiveCount++;
            transform.localScale = Vector3.one * (expand ? radius * .4f : radius);
            RefreshBoundary();
        }
        private void LateUpdate(){RefreshBoundary();}
        private void Update()
        {
            // Standalone rings/forks have no world parent to retire them. Check the
            // originating adventure before scaled time: title/death can freeze it.
            if (combatBound && (session == null || GameSession.Instance != session || !session.HasStarted ||
                session.IsDead || session.ModeFinished || playerGeneration == null ||
                session.Player != playerGeneration || playerGeneration.CombatEpoch != epoch))
            {
                gameObject.SetActive(false); // Return both the ring and optional bolt leases now.
                Destroy(gameObject);
                return;
            }
            float dt = combatBound ? Time.deltaTime : Time.unscaledDeltaTime;
            age += dt;
            float fraction = age / lifetime;
            if (fraction >= 1) { Destroy(gameObject); return; }
            if (expand) transform.localScale = Vector3.one * radius * Mathf.Lerp(slash ? .75f : .4f, 1f, 1f-Mathf.Pow(1f-Mathf.Clamp01(fraction*1.7f),3f));
            if (slash) transform.Rotate(0,dt*100f*(1f-fraction),0,Space.Self);
            Color faded = color;
            faded.a *= Mathf.Clamp01((1f - fraction) * 2f);
            line.startColor = line.endColor = faded;
        }
        private void RefreshBoundary()
        {
            if(!respectCover||line==null)return;
            if(boundaryRevision==WorldTraversal.Revision&&(boundaryCenter-transform.position).sqrMagnitude<.000001f)return;
            boundaryRevision=WorldTraversal.Revision;boundaryCenter=transform.position;line.useWorldSpace=true;
            CombatSight.FillAreaBoundary(boundary,transform.position,radius);line.SetPositions(boundary);
        }
        private void Release(){if(!registered)return;registered=false;ActiveCount=Mathf.Max(0,ActiveCount-1);}
        private void OnDisable(){Release();}
        private void OnEnable(){if(line==null||registered)return;if(expand&&ActiveCount>=(Application.isMobilePlatform?32:48)){gameObject.SetActive(false);return;}registered=true;ActiveCount++;}
        private void OnDestroy() { Release(); if (line != null && line.sharedMaterial != null) Destroy(line.sharedMaterial); }
    }

    internal sealed class CombatProjectile : MonoBehaviour
    {
        private CastFirstHitReceipt castReceipt;
        private static readonly List<CombatProjectile> hostileProjectiles = new List<CombatProjectile>();
        private Vector3 dodgeOrigin;
        private float dodgeDeadline;
        private int dodgeEpoch;
        private bool pendingDodge;
        private int skillIndex = -1, castId;
        private ProjectileVolleyBudget<EnemyController> volley;
        private string damageSource = "敌方弹幕";
        private System.Action hostileEnded;
        private GameSession session;
        private PlayerController owner;
        private PlayerController playerGeneration;
        private SummonedCompanion companionSource;
        private bool empoweredCompanionShot;
        private Vector3 direction;
        private float speed, radius, age, lifetime, explosionRadius;
        private CombatDamage damage, explosionDamage;
        private bool hostile, pierce, basicAttack, energyAwarded, arrowShape;
        private EnemyController homingTarget;
        private EnemyController basicAimTarget;
        private EnemyController impactMarkTarget;
        private float impactMarkStrength;
        private string terminationReason = "disposed";
        private bool bodyHeightFlight;
        private bool concentrated;
        private EnemyController concentratedTarget;
        private float launchHeight, impactHeight, aimedDistance, distanceTravelled;
        private int epoch;
        private readonly HashSet<EnemyController> hitTargets = new HashSet<EnemyController>();
        private Material bodyMaterial, trailMaterial;
        private Transform visualBody;
        private TrailRenderer visualTrail;
        private Color color;

        public static void Friendly(PlayerController player, GameSession game, Vector3 at, Vector3 forward, CombatDamage amount, Color tint, bool piercing = false, bool arrow = false, bool basic = false, float size = 1f, float velocity = 0f, EnemyController tracking = null, CombatDamage blastDamage = default(CombatDamage), float blastRadius = 0f, int skillIndex = -1, int castId = 0, SummonedCompanion companionSource = null, ProjectileVolleyBudget<EnemyController> volley = null, EnemyController markTarget = null, float markStrength = 0, EnemyController concentratedTarget = null, bool concentrated = false)
        {
            CombatProjectile projectile = Make(at, forward, tint, arrow, AuthoredProjectileMeshes.FriendlyKey(arrow,player.HeroClass,companionSource!=null));
            projectile.concentrated = concentrated;
            projectile.concentratedTarget = concentratedTarget;
            projectile.owner = player;
            projectile.playerGeneration = player;
            projectile.session = game;
            projectile.epoch = player.CombatEpoch;
            projectile.damage = amount;
            projectile.volley = volley;
            projectile.impactMarkTarget = markTarget; projectile.impactMarkStrength = markStrength;
            projectile.speed = arrow ? 20f : 16f;
            projectile.lifetime = 1.15f;
            projectile.radius = piercing ? .38f : .22f;
            projectile.pierce = piercing;
            projectile.basicAttack = basic;
            projectile.homingTarget = tracking; projectile.companionSource = companionSource;
            projectile.empoweredCompanionShot=companionSource!=null&&companionSource.EmpoweredAttackActive;
            projectile.arrowShape = arrow;
            projectile.explosionDamage = blastDamage;
            projectile.explosionRadius = blastRadius;
            projectile.skillIndex = skillIndex; projectile.castId = castId==0?player.NewCastId():castId;projectile.castReceipt=player.RetainCastReceipt(projectile.castId);
            projectile.transform.localScale *= size;
            projectile.radius *= Mathf.Min(2f,size);
            if(concentrated)
            {
                projectile.radius = ConcentratedVenomRules.Radius;
                // Body/trail only: the simulation root, hit radius and speed retain their budgets.
                projectile.visualBody.localScale=new Vector3(.68f,1.12f,.68f);
                projectile.visualBody.gameObject.name="Concentrated venom arrow";
                projectile.visualTrail.startWidth=.045f;
                projectile.visualTrail.endWidth=0;
                projectile.visualTrail.time=EffectPreferences.ReducedEffects?.06f:.1f;
            }
            projectile.BindVisualOrigin(companionSource==null?player:null);
            if (velocity > 0) projectile.speed = velocity;
            if (tracking != null) projectile.lifetime = 2.5f;
            if (CombatReviewEvents.Enabled) CombatReviewEvents.Emit("projectilelaunch",CombatReviewObjectId.Get(player),tracking==null?"0":CombatReviewObjectId.Get(tracking),skill:skillIndex,detail:CombatReviewObjectId.Get(projectile).ToString());
        }

        internal static bool CanLaunchFromMuzzle(PlayerController player,Vector3 muzzle,CombatDamage amount,int castId)
        {
            if(player==null||player.IsDead)return false;
            if(CombatSight.Direct(player.transform.position,muzzle))return true;
            DestructibleProp prop;float fraction;
            if(DestructibleProp.FindProjectileHit(player,player.transform.position,muzzle,.22f,out prop,out fraction))prop.Impact(player,castId,amount);
            return false;
        }

        // Ordinary shots keep their original range. A mage locks only the target
        // chosen at cast time; an arrow gets a very short, bounded correction.
        public static void BasicShot(PlayerController player,GameSession game,Vector3 muzzle,Vector3 target,CombatDamage amount,Color tint,bool arrow,EnemyController selected)
        {
            int attackId=player.NewCastId();
            if (!CanLaunchFromMuzzle(player,muzzle,amount,attackId))
            {if (CombatReviewEvents.Enabled) CombatReviewEvents.Emit("basicmiss",CombatReviewObjectId.Get(player),detail:"blocked_muzzle;prop_may_have_been_hit");CombatFx.Ring(player.transform.position,.4f,tint,.15f);return;}
            Vector3 direction=CombatFx.Flat(target-muzzle);
            if(direction.sqrMagnitude<.0001f) direction=player.transform.forward;
            CombatProjectile projectile=Make(muzzle,direction,tint,arrow,AuthoredProjectileMeshes.FriendlyKey(arrow,player.HeroClass,false));
            projectile.owner=player;
            projectile.playerGeneration=player;
            projectile.session=game;
            projectile.epoch=player.CombatEpoch;
            projectile.damage=amount;
            projectile.speed=arrow?20f:16f;
            projectile.lifetime=1.15f;
            projectile.radius=.22f;
            projectile.basicAttack=true;
            projectile.castId=attackId;projectile.castReceipt=player.RetainCastReceipt(attackId);
            projectile.arrowShape=arrow;
            projectile.basicAimTarget=selected;
            projectile.homingTarget=arrow?null:selected;
            projectile.bodyHeightFlight=true;
            projectile.launchHeight=muzzle.y;
            projectile.impactHeight=target.y;
            projectile.aimedDistance=Mathf.Max(.25f,CombatFx.Flat(target-muzzle).magnitude);
            projectile.transform.position=muzzle;
            projectile.AlignBodyFlight();
            projectile.BindVisualOrigin(player);
            if (CombatReviewEvents.Enabled) CombatReviewEvents.Emit("projectilelaunch",CombatReviewObjectId.Get(player),selected==null?"0":CombatReviewObjectId.Get(selected),detail:CombatReviewObjectId.Get(projectile).ToString());
        }

        public static void Hostile(GameSession game, Vector3 at, Vector3 forward, float amount, float velocity = 8f, string sourceName = "敌方弹幕", System.Action onEnded = null)
        {
            CombatProjectile projectile = Make(at, forward, new Color(1f,.31f,.48f), false,"HostileBolt");
            projectile.session = game;
            projectile.playerGeneration = game.Player;
            projectile.epoch = game.Player.CombatEpoch;
            projectile.hostile = true;
            projectile.damageSource = sourceName;
            projectile.hostileEnded = onEnded;
            projectile.damage = amount;
            projectile.speed = velocity;
            projectile.lifetime = 3f;
            projectile.radius = .32f;
            projectile.BindVisualOrigin(null);
            hostileProjectiles.Add(projectile);
        }

        internal static void RegisterDodge(PlayerController player, Vector3 origin, Vector3 destination)
        {
            foreach (CombatProjectile projectile in hostileProjectiles)
            {
                if (projectile == null || projectile.playerGeneration != player || projectile.epoch != player.CombatEpoch || projectile.speed <= 0) continue;
                Vector3 end = projectile.transform.position + projectile.direction * projectile.speed * .18f;
                float hitRadius = projectile.radius + .46f;
                if (CombatFx.SegmentDistance(origin, projectile.transform.position, end) >= hitRadius ||
                    CombatFx.SegmentDistance(destination, projectile.transform.position, end) <= hitRadius ||
                    !CombatSight.Direct(projectile.transform.position, origin)) continue;
                projectile.pendingDodge = true;
                projectile.dodgeOrigin = origin;
                projectile.dodgeDeadline = projectile.age + .25f;
                projectile.dodgeEpoch = player.CombatEpoch;
            }
        }

        private static CombatProjectile Make(Vector3 at, Vector3 forward, Color tint, bool arrow,string identity)
        {
            Shader detail=Resources.Load<Shader>("FilledSpell");
            Material surface = new Material(detail!=null?detail:Shader.Find("Standard")) { color = tint };
            if(detail!=null){surface.SetFloat("_Element",arrow?0:5);surface.SetFloat("_Seed",at.x*.17f+at.z*.23f);}
            ProceduralVisuals.ApplySurface(surface,arrow ? VisualSurface.Metal : VisualSurface.Crystal);
            GameObject obj = new GameObject("Projectile simulation root");
            GameObject body = ProceduralVisuals.Create(arrow ? "Spectral Arrow" : "Arcane Bolt",arrow ? PrimitiveType.Capsule : PrimitiveType.Sphere,surface);
            Mesh authored=AuthoredProjectileMeshes.Load(identity);
            if(authored!=null)body.GetComponent<MeshFilter>().sharedMesh=authored;
            body.transform.SetParent(obj.transform,false);
            obj.transform.position = new Vector3(at.x, 1f, at.z);
            Vector3 normalized = CombatFx.Flat(forward).normalized;
            if (normalized.sqrMagnitude < .1f) normalized = Vector3.forward;
            obj.transform.rotation = Quaternion.LookRotation(normalized) * (arrow ? Quaternion.Euler(90,0,0) : Quaternion.identity);
            obj.transform.localScale = arrow ? new Vector3(.1f,.43f,.1f) : Vector3.one * .27f;
            CombatProjectile projectile = obj.AddComponent<CombatProjectile>();
            projectile.direction = normalized;
            projectile.color = tint;
            projectile.bodyMaterial = surface;
            projectile.visualBody=body.transform;
            TrailRenderer trail = body.AddComponent<TrailRenderer>();projectile.visualTrail=trail;trail.emitting=false;
            projectile.trailMaterial = CombatFx.NewGlow();
            trail.sharedMaterial = projectile.trailMaterial;
            trail.time = EffectPreferences.ReducedEffects ? .08f : .16f;
            trail.numCapVertices = 4; trail.numCornerVertices = 4;
            trail.startWidth = arrow ? .11f : .22f;
            trail.endWidth = 0;
            trail.startColor = Color.Lerp(tint,Color.white,.65f);
            trail.endColor = new Color(tint.r,tint.g,tint.b,0);
            trail.minVertexDistance = .035f;
            return projectile;
        }

        private void Update()
        {
            CombatImpactBatch.BeginAction();
            try
            {
            if (session == null || session.Player == null || session.Player != playerGeneration || !session.HasStarted || session.IsDead || session.CombatEffectsEnded || hostile && session.CombatEnded || session.Player.CombatEpoch != epoch || (!hostile && owner == null))
            { terminationReason = "retired"; Destroy(gameObject); return; }
            if (session.InputBlocked) return;
            float dt = Time.deltaTime;
            if (dt <= 0) return;
            age += dt;
            if (age > lifetime) { terminationReason = "expired"; Destroy(gameObject); return; }
            if (homingTarget != null && !homingTarget.IsDead && homingTarget.gameObject.activeInHierarchy)
            {
                Vector3 towards = CombatFx.Flat(homingTarget.transform.position-transform.position).normalized;
                if (towards.sqrMagnitude > .01f) direction = Vector3.Slerp(direction,towards,Mathf.Min(1,dt*8f)).normalized;
                transform.rotation = Quaternion.LookRotation(direction) * (arrowShape ? Quaternion.Euler(90,0,0) : Quaternion.identity);
            }
            else if(basicAttack && arrowShape && age<=.18f && basicAimTarget!=null && !basicAimTarget.IsDead && basicAimTarget.gameObject.activeInHierarchy)
            {
                Vector3 towards=CombatFx.Flat(basicAimTarget.transform.position-transform.position).normalized;
                if(towards.sqrMagnitude>.01f && Vector3.Angle(direction,towards)<=18f)
                    direction=Vector3.RotateTowards(direction,towards,70f*Mathf.Deg2Rad*dt,0).normalized;
            }
            if(concentrated && age<=ConcentratedVenomRules.SteeringSeconds && concentratedTarget!=null && !concentratedTarget.IsDead && concentratedTarget.gameObject.activeInHierarchy)
            {
                Vector3 towards=CombatFx.Flat(concentratedTarget.transform.position-transform.position).normalized;
                if(towards.sqrMagnitude>.01f && Vector3.Angle(direction,towards)<=ConcentratedVenomRules.SteeringDegrees)
                    direction=Vector3.RotateTowards(direction,towards,ConcentratedVenomRules.DegreesPerSecond*Mathf.Deg2Rad*dt,0).normalized;
                transform.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(90,0,0);
            }
            Vector3 previous = transform.position;
            Vector3 nextPosition = previous + direction * speed * dt;
            bool terrainHit = !CombatSight.Direct(previous, nextPosition);
            if (terrainHit)
            {
                float clear = 0, blocked = 1;
                for (int sample = 0; sample < 10; sample++)
                {
                    float fraction = (clear + blocked) * .5f;
                    if (CombatSight.Direct(previous, Vector3.Lerp(previous, nextPosition, fraction))) clear = fraction;
                    else blocked = fraction;
                }
                nextPosition = Vector3.Lerp(previous, nextPosition, clear);
            }
            transform.position = nextPosition;
            if(bodyHeightFlight)
            {
                if(basicAimTarget!=null && !basicAimTarget.IsDead && basicAimTarget.gameObject.activeInHierarchy) impactHeight=owner.EnemyBodyPoint(basicAimTarget).y;
                distanceTravelled += CombatFx.Flat(nextPosition - previous).magnitude;
                Vector3 point=transform.position;
                point.y=Mathf.Lerp(launchHeight,impactHeight,Mathf.Clamp01(distanceTravelled/aimedDistance));
                transform.position=point;
                AlignBodyFlight();
            }
            if (hostile)
            {
                bool strikesPlayer = CombatFx.SegmentDistance(session.Player.transform.position, previous, transform.position) < radius + .46f && CombatSight.Direct(previous, session.Player.transform.position);
                Vector3 segment = CombatFx.Flat(transform.position - previous);
                float playerFraction = !strikesPlayer ? 1f : segment.sqrMagnitude < .00001f ? 0 : Mathf.Clamp01(Vector3.Dot(CombatFx.Flat(session.Player.transform.position - previous), segment) / segment.sqrMagnitude);
                if (SummonedCompanion.HitHostileProjectile(previous, transform.position, damage.Amount, playerFraction))
                { CombatFx.Ring(transform.position, .65f, color, .18f); Destroy(gameObject); return; }
                if (pendingDodge)
                {
                    if (age > dodgeDeadline || session.Player.CombatEpoch != dodgeEpoch) pendingDodge = false;
                    else if (!strikesPlayer && CombatFx.SegmentDistance(dodgeOrigin, previous, transform.position) < radius + .46f &&
                        CombatSight.Direct(previous, dodgeOrigin))
                    { pendingDodge = false; session.Player.NotifyPerfectDodge(); }
                }
                if (strikesPlayer)
                {
                    session.Player.TakeDamageFrom(damage.Amount, damageSource);
                    CombatFx.Ring(transform.position, .65f, color, .18f);
                    Destroy(gameObject);
                    return;
                }
            }
            else
            {
                DestructibleProp hitProp;float propFraction;
                DestructibleProp.FindProjectileHit(owner,previous,transform.position,radius,out hitProp,out propFraction);
                EnemyController firstIntercept = null;
                if(concentrated)
                {
                    float firstFraction=float.PositiveInfinity;
                    foreach(var candidate in session.Enemies)
                    {
                        if(candidate==null || candidate.IsDead || !candidate.gameObject.activeInHierarchy)continue;
                        float extent=candidate.ProjectileHitRadius+radius;
                        if(CombatFx.SegmentDistance(candidate.transform.position,previous,transform.position)>extent || !CombatSight.Direct(previous,candidate.transform.position))continue;
                        float fraction=PropImpactGeometry.EntryFraction(previous.x,previous.z,transform.position.x,transform.position.z,candidate.transform.position.x,candidate.transform.position.z,extent);
                        if(hitProp!=null && fraction>propFraction)continue;
                        if(fraction<firstFraction){firstFraction=fraction;firstIntercept=candidate;}
                    }
                }
                for (int i = session.Enemies.Count - 1; i >= 0; i--)
                {
                    EnemyController enemy = session.Enemies[i];
                    if (enemy == null || enemy.IsDead || hitTargets.Contains(enemy) || concentrated && enemy!=firstIntercept) continue;
                    float hitRadius = enemy.ProjectileHitRadius;
                    if(hitProp!=null&&PropImpactGeometry.EntryFraction(previous.x,previous.z,transform.position.x,transform.position.z,enemy.transform.position.x,enemy.transform.position.z,hitRadius+radius)>propFraction)continue;
                    if (CombatFx.SegmentDistance(enemy.transform.position, previous, transform.position) > hitRadius + radius) continue;
                    if (!CombatSight.Direct(previous, enemy.transform.position)) continue;
                    hitTargets.Add(enemy);
                    Vector3 hitPosition = enemy.transform.position;
                    CombatDamage impact=volley==null?damage:volley.Apply(enemy,damage,false);
                    float healthBefore = enemy.Health;
                    if (LockedImpactMarkPolicy.ShouldApply(impactMarkTarget, enemy, !enemy.IsDead, impact.Amount, impactMarkStrength) && enemy.StatusEffects != null)
                        enemy.StatusEffects.Mark(4f, impactMarkStrength);
                    if(impact.Amount>0){if(!basicAttack&&companionSource==null)owner.RegisterSkillHit(castId);enemy.TakeDamage(owner.ResolveSkillImpact(enemy, skillIndex, castId, impact.Amount, impact.IsCritical, impact.CriticalMultiplier), direction, .18f, critical:impact.IsCritical,practiceCastId:!basicAttack&&companionSource==null?castId:0);}
                    if(concentrated&&enemy.Health<healthBefore)VenomSkillVfx.Contact(owner,owner.EnemyBodyPoint(enemy),false);
                    if (CombatReviewEvents.Enabled) CombatReviewEvents.Emit("projectilehit",CombatReviewObjectId.Get(owner),CombatReviewObjectId.Get(enemy),Mathf.Max(0,healthBefore-enemy.Health),skillIndex,CombatReviewObjectId.Get(this).ToString());
                    if (companionSource != null) {companionSource.OnConfirmedHit(enemy);companionSource.RecordEmpoweredHit(enemy,Mathf.Max(0,healthBefore-enemy.Health),empoweredCompanionShot);}
                    if(!concentrated)CombatFx.Ring(hitPosition, .7f, color, .2f);
                    if (basicAttack && !energyAwarded)
                    {
                        energyAwarded = true;
                        owner.OnBasicAttackHitTarget(hitPosition, enemy, true);
                    }
                    if(explosionDamage.Amount>0)
                    {
                        owner.HitArea(hitPosition,explosionRadius,explosionDamage,.15f,.15f,castId,volley);
                        CombatFx.Ring(hitPosition,explosionRadius,color,.3f,.11f);
                    }
                    if (!pierce) { Destroy(gameObject); return; }
                    i=Mathf.Min(i,session.Enemies.Count);
                }
                if(hitProp!=null)
                {
                    Vector3 point=hitProp.transform.position;
                    hitProp.Impact(owner,castId,damage);
                    if(explosionDamage.Amount>0)owner.HitArea(point,explosionRadius,explosionDamage,.15f,.15f,castId,volley);
                    CombatFx.Ring(point,.45f,color,.2f,.04f);Destroy(gameObject);return;
                }
            }
            if (terrainHit) { terminationReason = "terrain"; CombatFx.Ring(transform.position, .4f, color, .15f); Destroy(gameObject); return; }
            float bound = session.ArenaRadius + 3f;
            if (Mathf.Abs(transform.position.x) > bound || Mathf.Abs(transform.position.z) > bound) Destroy(gameObject);

            }
            finally { CombatImpactBatch.EndAction(); }
        }

        private void OnDisable()
        {castReceipt?.Release();castReceipt=null;var ended=hostileEnded;hostileEnded=null;if(ended!=null)ended();}

        private void OnDestroy()
        {
            OnDisable();
            if (!hostile && owner != null && CombatReviewEvents.Enabled) CombatReviewEvents.Emit("projectileend",CombatReviewObjectId.Get(owner),skill:skillIndex,detail:CombatReviewObjectId.Get(this)+":"+terminationReason+":hits="+hitTargets.Count);
            hostileProjectiles.Remove(this);
            if (bodyMaterial != null) Destroy(bodyMaterial);
            if (trailMaterial != null) Destroy(trailMaterial);
        }

        private void BindVisualOrigin(PlayerController source)
        {
            var model=source!=null?source.GetComponentInChildren<CombatModel>():null;
            ProjectileVisualBridge.Bind(visualBody,transform,model,arrowShape,visualTrail);
        }

        private void AlignBodyFlight()
        {
            Vector3 visibleDirection=direction;
            if(distanceTravelled<aimedDistance) visibleDirection.y=(impactHeight-launchHeight)/aimedDistance;
            transform.rotation=Quaternion.LookRotation(visibleDirection.normalized)*(arrowShape?Quaternion.Euler(90,0,0):Quaternion.identity);
        }
    }

    internal sealed class CombatArea : MonoBehaviour
    {
        private CastFirstHitReceipt castReceipt;
        private RunMechanismEvidence.Instance mechanismInstance;
        private void RecordMechanismHealthLoss(float amount)
        {if(IsCurrentCast&&mechanismInstance!=null)mechanismInstance.Record(session.Player,owner.CombatEpoch,amount);}
        private PlayerController owner;
        private GameSession session;
        private float radius, stun, delay, duration, interval, age, nextTick, pullStrength;
        private CombatDamage damage, finalDamage;
        private bool follow, meteor, finished;
        private int statusSkill = -1, statusRank = 1, castId;
        private int epoch;
        private Color color;
        private GameObject marker, fallingOrb, trapCore;
        private Material orbMaterial;
        private bool fireVisual, poisonVisual, lightningVisual, solidImpactSpawned;
        private SkillVisualRecipe visualRecipe;
        private FilledSkillVfx.ArrowBatchHandle arrowRainVisual;
        private readonly ScheduledImpactBatch<EnemyController> pendingTickTargets = new ScheduledImpactBatch<EnemyController>();
        private bool IsCurrentCast { get { return owner != null && session != null && session.Player == owner && !owner.IsDead && session.HasStarted && !session.CombatEffectsEnded && owner.CombatEpoch == epoch; } }

        public static void Spawn(PlayerController player, GameSession game, Vector3 at, float size, CombatDamage amount, float disable,
            float startup, float activeTime, float tickInterval, Color tint, bool followPlayer = false, bool fallingMeteor = false, float pulling = 0f, CombatDamage finisher = default(CombatDamage), int statusSkill = -1, int statusRank = 1, int castId = 0, SkillVisualRecipe visual = SkillVisualRecipe.Neutral, int trackedMechanic = -1)
        {
            GameObject obj = new GameObject("Skill Area");
            obj.transform.position = new Vector3(at.x,0,at.z);
            CombatArea area = obj.AddComponent<CombatArea>();
            area.owner = player; area.session = game; area.epoch = player.CombatEpoch;
            area.radius = size; area.damage = activeTime > 0 ? amount.WithoutCritical() : amount; area.stun = disable;
            area.delay = startup; area.duration = activeTime; area.interval = Mathf.Max(.1f,tickInterval);
            area.color = tint; area.follow = followPlayer; area.meteor = fallingMeteor;
            area.pullStrength = pulling; area.finalDamage = finisher;
            area.statusSkill = statusSkill; area.statusRank = statusRank; area.castId = castId==0?player.NewCastId():castId;area.castReceipt=player.RetainCastReceipt(area.castId);
            area.nextTick = startup;
            if(area.IsCurrentCast&&game.InDungeon&&trackedMechanic>=0)area.mechanismInstance=game.MechanismEvidence.Register(player,area.epoch,trackedMechanic);
            if (startup > 0) FilledSkillVfx.Charge(obj.transform, player, obj.transform.position, size, tint, startup);
            area.marker = CombatFx.Ring(at, size, tint, startup + activeTime + .2f, .075f, false, respectCover:true);
            area.visualRecipe = visual;
            area.fireVisual = visual == SkillVisualRecipe.Fire;
            area.poisonVisual = visual == SkillVisualRecipe.Poison;
            area.lightningVisual = visual == SkillVisualRecipe.Lightning;
            if(visual==SkillVisualRecipe.Neutral && player.HeroClass==HeroClass.Ranger && statusSkill==1 && startup>0)
                area.trapCore=AuthoredTrapVisual.Create(obj.transform,tint,size);
            if (fallingMeteor)
            {
                area.orbMaterial = new Material(Shader.Find("Standard")) { color = new Color(.64f,.19f,.075f) };
                ProceduralVisuals.ApplySurface(area.orbMaterial,VisualSurface.Crystal);
                area.orbMaterial.SetColor("_EmissionColor",new Color(.85f,.21f,.035f));
                area.fallingOrb = ProceduralVisuals.Create("Falling Meteor",PrimitiveType.Sphere,area.orbMaterial);
                area.fallingOrb.GetComponent<MeshFilter>().sharedMesh=AuthoredProjectileMeshes.Load("MeteorRock")??ProceduralVisuals.WeatheredRock;
                area.fallingOrb.transform.SetParent(obj.transform, false);
                area.fallingOrb.transform.localPosition = Vector3.up * 9f;
                area.fallingOrb.transform.localScale = Vector3.one * 1.1f;

            }
        }

        private void Update()
        {
            if (!IsCurrentCast)
            { Retire(); return; }
            if (session.InputBlocked || Time.deltaTime <= 0) return;
            age += Time.deltaTime;
            if (follow) transform.position = owner.transform.position;
            if (marker != null) marker.transform.position = transform.position + Vector3.up * .065f;
            if(pullStrength>0)
            {
                for(int i=0;i<session.Enemies.Count;i++)
                {
                    EnemyController enemy=session.Enemies[i];
                    if(enemy==null || enemy.IsDead) continue;
                    Vector3 delta=CombatFx.Flat(transform.position-enemy.transform.position);
                    if(delta.magnitude<radius+1f && delta.magnitude>.55f && CombatSight.Area(transform.position,enemy.transform.position))
                        enemy.ApplyPull(delta.normalized * Mathf.Min(delta.magnitude - .55f, pullStrength * Time.deltaTime));
                }
            }
            if(trapCore!=null && age>=delay){Destroy(trapCore);trapCore=null;}
            if (fallingOrb != null)
            {
                float fallProgress=Mathf.Clamp01(age/Mathf.Max(.01f,delay));
                fallingOrb.transform.localPosition = Vector3.up * Mathf.Lerp(9f,.5f,fallProgress*fallProgress);
                fallingOrb.transform.Rotate(Time.deltaTime*120f,Time.deltaTime*70f,0,Space.Self);
                if (age >= delay) { Destroy(fallingOrb); fallingOrb = null; }
            }
            bool arrowVisualEmitted=false;
            CombatImpactBatch.Begin();try
            {
            for (int tick = 0; tick < ScheduledTickWindow.MaximumCatchUp; tick++)
            {
                if (!IsCurrentCast) { Retire(); return; }
                if (session.InputBlocked) return;
                if (!pendingTickTargets.Pending)
                {
                    if (ScheduledTickWindow.Collect(ref nextTick, age, delay + duration, interval, 1) == 0) break;
                    pendingTickTargets.Begin(session.Enemies, true);
                    if(visualRecipe==SkillVisualRecipe.ArrowRain&&!arrowVisualEmitted)
                    {
                        arrowVisualEmitted=true;
                        if(arrowRainVisual.IsValid)arrowRainVisual.ArrowBeat(transform.position,radius,false);
                        else arrowRainVisual=FilledSkillVfx.ArrowRain(owner,transform.position,radius,color,priority:CombatVisualPriority.SustainedBackground);
                    }
                    if (!solidImpactSpawned)
                    {
                        solidImpactSpawned = true;
                        if (fireVisual || poisonVisual || lightningVisual)
                            ElementalCombatVfx.Area(transform, radius, fireVisual ? ElementalCombatVfx.Element.Fire :
                                poisonVisual ? ElementalCombatVfx.Element.Poison : ElementalCombatVfx.Element.Lightning);
                        if(poisonVisual)FilledSkillVfx.PoisonVines(owner,transform.position,radius,color);
                        if (fireVisual) FilledSkillVfx.Impact(owner, transform.position, radius, FilledVfxKind.Fire, new Color(1f,.43f,.12f),CombatVisualPriority.ActionBody);
                        else if (visualRecipe == SkillVisualRecipe.Ice)
                            FilledSkillVfx.Impact(owner, transform.position, radius, FilledVfxKind.Ice, new Color(.2f,.75f,1f),CombatVisualPriority.ActionBody);
                        else if (visualRecipe == SkillVisualRecipe.Spirit || visualRecipe == SkillVisualRecipe.Arcane || visualRecipe == SkillVisualRecipe.Lightning || visualRecipe == SkillVisualRecipe.Steel)
                            FilledSkillVfx.Impact(owner, transform.position, radius, SkillVisualRecipes.Filled(visualRecipe), color,CombatVisualPriority.ActionBody);
                    }
                    if (tick == 0)
                    { DestructibleProp.StrikeArea(owner,transform.position,radius,damage,castId); if(visualRecipe!=SkillVisualRecipe.ArrowRain)CombatFx.Ring(transform.position,radius,color,.42f,.15f); }
                    if (tick == 0 && lightningVisual)
                        for (int bolt = 0; bolt < 3; bolt++)
                        {
                            Vector2 point = Random.insideUnitCircle * radius * .85f;
                            Vector3 end = transform.position + new Vector3(point.x, .15f, point.y);
                            ElementalCombatVfx.Lightning(transform.position + Vector3.up * Random.Range(2.1f, 3.5f), end);
                        }
                    if (tick == 0 && !meteor && !follow && visualRecipe!=SkillVisualRecipe.ArrowRain)
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            Vector2 offset = Random.insideUnitCircle * radius * .65f;
                            CombatFx.Slash(transform.position + new Vector3(offset.x, .4f, offset.y),Vector3.forward,.4f,color);
                        }
                    }
                }
                while (pendingTickTargets.Pending)
                {
                    if (!IsCurrentCast) { Retire(); return; }
                    if (session.InputBlocked) return;
                    EnemyController enemy;
                    if (!pendingTickTargets.TryTake(out enemy)) break;
                    if (enemy == null || enemy.IsDead || !session.Enemies.Contains(enemy)) continue;
                    Vector3 delta = CombatFx.Flat(enemy.transform.position - transform.position);
                    if (delta.magnitude <= radius + (enemy.IsBoss ? .85f : .4f) + enemy.HitFootprintBonus && CombatSight.Area(transform.position,enemy.transform.position))
                    {
                        if (fireVisual)
                            ElementalCombatVfx.OnEnemy(enemy, ElementalCombatVfx.Element.Fire, 2.5f);
                        if (poisonVisual)
                            ElementalCombatVfx.OnEnemy(enemy, ElementalCombatVfx.Element.Poison, 2f);
                        enemy.TakeDamage(owner.ResolveSkillImpact(enemy, statusSkill, castId, damage.Amount, damage.IsCritical, damage.CriticalMultiplier),delta.normalized,.3f,stun,critical:damage.IsCritical,actualHealthLoss:mechanismInstance==null?(System.Action<float>)null:RecordMechanismHealthLoss,practiceCastId:castId);
                        owner.RegisterSkillHit(castId);
                        if (tick == 0 && lightningVisual)
                            ElementalCombatVfx.Lightning(transform.position + Vector3.up * 2f,
                                enemy.transform.position + Vector3.up * (enemy.IsBoss ? 2f : 1f));
                        if (enemy.StatusEffects != null && statusSkill == 0 && owner.HeroClass == HeroClass.Arcanist)
                            owner.ApplyNovaStatus(enemy, statusRank);
                        if (enemy.StatusEffects != null && ((statusSkill == 5 && owner.HeroClass == HeroClass.Ranger) || (statusSkill == 1 && owner.HeroClass == HeroClass.Summoner)))
                        {
                            enemy.StatusEffects.Slow(3f, .3f + statusRank * .07f);
                            enemy.StatusEffects.Poison(owner, SummonerDamageRules.ThornPoisonDuration(statusRank), damage.Amount * SummonerDamageRules.ThornPoisonFraction);
                        }
                    }
                }
            }
            }
            finally { CombatImpactBatch.End(); }
            if (!IsCurrentCast) { Retire(); return; }
            if (session.InputBlocked) return;
            bool ticksDrained = !pendingTickTargets.Pending && ScheduledTickWindow.Drained(nextTick, delay + duration);
            if (!finished && finalDamage.Amount>0 && age>=delay+duration && ticksDrained)
            {
                finished=true;
                if(visualRecipe==SkillVisualRecipe.ArrowRain)FilledSkillVfx.ArrowRain(owner,transform.position,radius*1.1f,color,true,CombatVisualPriority.Finale,castId);
                else AdvancedSkillVfx.Rune(owner,transform.position,radius*1.1f,color,.65f,3);
                owner.HitArea(transform.position,radius*1.1f,finalDamage,.7f,.65f,castId);
            }
            if (age > delay + duration + .1f && ticksDrained) Retire();
        }

        private void Retire() { pendingTickTargets.Clear(); Destroy(gameObject); }
        private void OnDisable() { arrowRainVisual.Retire();arrowRainVisual=default;castReceipt?.Release();castReceipt=null;pendingTickTargets.Clear(); }

        private void OnDestroy()
        {
            OnDisable();
            pendingTickTargets.Clear();
            if (marker != null) Destroy(marker);
            if (orbMaterial != null) Destroy(orbMaterial);
        }
    }
}
