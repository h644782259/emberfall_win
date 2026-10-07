using System.Collections.Generic;
using UnityEngine;

namespace Emberfall
{
    // A cast has a finite event budget. It keeps the cast's stats/aim and never
    // follows a newly loaded hero or survives a teleport to another encounter.
    internal sealed class AdvancedSkillSequence : MonoBehaviour
    {
        private PlayerController owner;
        private GameSession session;
        private HeroClass heroClass;
        private int skill, rank, epoch, step, steps;
        private CombatDamage damage;
        private float range, interval, age, nextEvent;
        private int castId;
        private CastFirstHitReceipt castReceipt;
        private Vector3 target, forward, origin;
        private Color color;
        private EnemyController lockedTarget;
        private FilledSkillVfx.ArrowBatchHandle arrowBatch;
        private bool restrictedHealing;
        private AdvancedSkillVfx healingAura;

        public static void Spawn(PlayerController hero, GameSession game, int index, int skillRank, Vector3 aim, Vector3 direction, CombatDamage strength, Color tint, int castId = 0)
        {
            GameObject obj = new GameObject("Skill Sequence " + index);
            AdvancedSkillSequence sequence = obj.AddComponent<AdvancedSkillSequence>();
            sequence.owner = hero; sequence.session = game; sequence.heroClass = hero.HeroClass;
            sequence.skill = index; sequence.rank = skillRank; sequence.epoch = hero.CombatEpoch;
            sequence.damage = strength; sequence.castId = castId==0?hero.NewCastId():castId; sequence.castReceipt=hero.RetainCastReceipt(sequence.castId); sequence.range = GameBalance.SkillRangeMultiplier(skillRank);
            sequence.target = aim; sequence.origin = hero.transform.position; sequence.forward = CombatFx.Flat(direction).normalized;
            sequence.color = tint;
            sequence.restrictedHealing = game.ChallengeRun && game.InDungeon;
            sequence.Configure();
        }

        private void Configure()
        {
            steps=SkillDamageBudgets.AdvancedSteps(heroClass,skill,rank);
            interval=SkillDamageBudgets.AdvancedInterval(heroClass,skill);
            nextEvent=SkillDamageBudgets.AdvancedFirstEvent(heroClass,skill);
            if(skill==6)
            {
                owner.HealingProtection(rank);
                healingAura=AdvancedSkillVfx.Healing(owner,3.2f*range,color,5.3f,rank,()=>this!=null&&gameObject.activeInHierarchy&&step<steps);
                return;
            }
            if(heroClass==HeroClass.Ranger&&skill==9)
            {arrowBatch=FilledSkillVfx.BeginArrowBatch(owner,target,6f*range,color,priority:CombatVisualPriority.ActionBody,castId:castId);return;}
            if(heroClass==HeroClass.Ranger&&skill==7)lockedTarget=Nearest(target,10f*range);
            if(heroClass==HeroClass.Arcanist&&skill==9)
            {
                AdvancedSkillVfx.Rune(owner,target,GameBalance.ArcanistPulseRadius*range,color,nextEvent,rank+1,identity:2);
                CombatFx.Ring(target,GameBalance.ArcanistPulseRadius*range,color,nextEvent,.12f);
                return;
            }
            if (skill >= 6)
                AdvancedSkillVfx.Rune(owner,skill==7 && heroClass==HeroClass.Ranger?origin:target,4.2f*range,color,nextEvent+steps*interval+.5f,rank+1,identity:heroClass==HeroClass.Vanguard?1:heroClass==HeroClass.Summoner?3:heroClass==HeroClass.Arcanist&&skill==4?2:0);
            else AdvancedSkillVfx.Rune(owner,origin,1.6f*range,color,.7f,rank,identity:heroClass==HeroClass.Vanguard?1:heroClass==HeroClass.Summoner?3:heroClass==HeroClass.Arcanist&&skill==4?2:0);
        }

        private void Update()
        {
            if (owner == null || owner.IsDead || session == null || session.Player != owner || !session.HasStarted || session.CombatEnded || owner.CombatEpoch != epoch)
            { Destroy(gameObject); return; }
            if (session.InputBlocked || Time.deltaTime <= 0) return;
            age += Time.deltaTime;
            if (heroClass == HeroClass.Arcanist && skill == 7) Pull(target,5.2f*range,rank==3?6f:4f);
            // Limit catch-up to three events per frame after a frame-time spike.
            int catchup = 0;
            while (step < steps && age >= nextEvent && catchup++ < 3)
            {
                if (owner == null || owner.IsDead || owner.CombatEpoch != epoch || session.CombatEnded) { Destroy(gameObject); return; }
                if (session.InputBlocked) return;
                CombatImpactBatch.BeginAction();
                try
                {
                if (skill==6) Healing();
                else if (heroClass == HeroClass.Vanguard) Vanguard();
                else if (heroClass == HeroClass.Arcanist) Arcanist();
                else Ranger();
                step++; nextEvent += interval;
                }
                finally { CombatImpactBatch.EndAction(); }
            }
            if (step >= steps) Destroy(gameObject);
        }

        private void Healing()
        {
            float total=rank==3?.55f:rank==2?.42f:.3f;
            float selfTotal=restrictedHealing?(rank==3?.8f:rank==2?.7f:.6f):total;
            float healthBefore=owner.Health;
            owner.Heal(owner.MaxHealth*selfTotal/5f);
            if (heroClass == HeroClass.Summoner) SummonedCompanion.HealAll(owner, total / 5f);
            if(owner.Health>healthBefore)FilledSkillVfx.HealingPulse(owner,color);
            if(rank==3 && step==steps-1) owner.RestoreSkillEnergy(8f);
        }

        private void Vanguard()
        {
            switch (skill)
            {
                case 5: // Dash damages the whole traversed lane once, not only its endpoint.
                    Vector3 start = owner.transform.position;
                    owner.SkillDash(forward,7f*range,.5f);
                    HitLine(start,owner.transform.position,1.3f*range,damage*SkillDamageBudgets.AdvancedImpact(heroClass,skill,rank,step),1.3f,.55f+rank*.15f);
                    AdvancedSkillVfx.Rune(owner,owner.transform.position,2f*range,color,.65f,rank);
                    if (rank >= 2) owner.HitArea(owner.transform.position,2.7f*range,damage*SkillDamageBudgets.AdvancedAuxiliary(heroClass,skill,rank),.8f,.5f,castId:castId);
                    if (rank == 3) SpawnTail(start,2.5f*range,.6f);
                    break;
                case 7: // A travelling fault with perpendicular fissures.
                    Vector3 fault = Clamp(origin+forward*(2f+step*1.8f)*range);
                    Vector3 tangent = Vector3.Cross(Vector3.up,forward);
                    AdvancedSkillVfx.Beam(owner,fault-tangent*2.7f*range,fault+tangent*2.7f*range,new Color(1f,.65f,.23f),.65f,.28f);
                    AdvancedSkillVfx.FallingBlade(owner,fault,color,.5f);
                    owner.HitArea(fault,2.6f*range,damage*SkillDamageBudgets.AdvancedImpact(heroClass,skill,rank,step),.7f,.5f,castId:castId);
                    LaunchArea(fault, 2.6f * range, .6f + rank * .12f, 1f + rank * .2f);
                    if (rank==3 && step==steps-1) Burst(fault,4f*range,damage*SkillDamageBudgets.AdvancedAuxiliary(heroClass,skill,rank),color,3,SkillVisualRecipe.Steel);
                    break;
                case 9: // The main judgment lands first; the unchanged sword budget follows.
                    if (step == 0) GameAudio.Play(SoundCue.Judgment);
                    if (CombatReviewEvents.Enabled) CombatReviewEvents.Emit("skillimpact",CombatReviewObjectId.Get(owner),skill:skill,detail:step==0?"judgment":"sword-array");
                    if (step > 0)
                    {
                        float radius = (2.4f+(step-1)*.55f)*range;
                        AdvancedSkillVfx.FallingBlade(owner,target+Circle((step-1)*2.1f,1.8f*range),color,.85f);
                        CombatFx.Ring(target,radius,color,.5f,.18f);
                        owner.HitArea(target,radius,damage*SkillDamageBudgets.AdvancedImpact(heroClass,skill,rank,step),.1f,.2f,castId:castId);
                    }
                    else
                    {
                        AdvancedSkillVfx.FallingBlade(owner,target,new Color(1f,.95f,.63f),2.1f);
                        Burst(target,6.2f*range,damage*SkillDamageBudgets.AdvancedImpact(heroClass,skill,rank,step),color,3,SkillVisualRecipe.Steel);
                    }
                    if (rank==3 && step==steps-1) SpawnTail(target,5.2f*range,.25f);
                    break;
            }
        }

        private void Arcanist()
        {
            switch (skill)
            {
                case 4:
                    ChainLightning(6+(rank-1)*2);
                    break;
                case 7:
                    if(step<steps-1)
                    {
                        float a=step*.95f;
                        AdvancedSkillVfx.Beam(owner,target+Circle(a,4.3f*range)+Vector3.up*2,target+Vector3.up*.4f,new Color(.74f,.42f,1f),.45f,.17f);
                        owner.HitArea(target,4.8f*range,damage*SkillDamageBudgets.AdvancedImpact(heroClass,skill,rank,step),0,.12f,castId:castId);
                    }
                    else Burst(target,5.3f*range,damage*SkillDamageBudgets.AdvancedImpact(heroClass,skill,rank,step),new Color(.84f,.6f,1f),3,SkillVisualRecipe.Arcane);
                    break;
                case 9:
                    Color element = owner.Specialization==ElementalistSpecialization.Burn?new Color(1f,.5f,.28f):owner.Specialization==ElementalistSpecialization.Shatter?new Color(.5f,.92f,1f):step%3==0?new Color(1f,.5f,.28f):step%3==1?new Color(.5f,.92f,1f):new Color(.77f,.48f,1f);
                    if(step<steps-1)
                    {
                        Vector3 axis = Circle(step*.65f,6f*range);
                        AdvancedSkillVfx.Beam(owner,target-axis+Vector3.up,target+axis+Vector3.up,element,.65f,.34f);
                        AdvancedSkillVfx.Beam(owner,target+Vector3.up*10f,target,element,.65f,.24f);
                        FilledSkillVfx.Impact(owner,target,GameBalance.ArcanistPulseRadius*range,SkillVisualRecipes.Filled(SkillVisualRecipes.Ultimate(owner.Specialization,step,false)),element,CombatVisualPriority.ActionBody);
                        owner.ElementalAdvancedArea(target,GameBalance.ArcanistPulseRadius*range,damage*SkillDamageBudgets.AdvancedImpact(heroClass,skill,rank,step),castId,false);
                    }
                    else
                    {
                        AdvancedSkillVfx.Rune(owner,target,GameBalance.ArcanistFinaleRadius*range,new Color(.92f,.83f,1f),.8f,3);
                        FilledSkillVfx.Impact(owner,target,GameBalance.ArcanistFinaleRadius*range,SkillVisualRecipes.Filled(SkillVisualRecipes.Ultimate(owner.Specialization,step,true)),owner.Specialization==ElementalistSpecialization.Burn?new Color(1f,.43f,.12f):new Color(.2f,.75f,1f),CombatVisualPriority.Finale,castId);
                        owner.ElementalAdvancedArea(target,GameBalance.ArcanistFinaleRadius*range,damage*SkillDamageBudgets.AdvancedImpact(heroClass,skill,rank,step),castId,true);
                        if(rank==3) SpawnTail(target,GameBalance.ArcanistPulseRadius*range,.3f,3f);
                    }
                    break;
            }
        }

        private void Ranger()
        {
            switch(skill)
            {
                case 4:
                    // Slot 4 now releases through PlayerController.BeginRangerVault.
                    break;
                case 5:
                    AdvancedSkillVfx.Rune(owner,target,3.7f*range,new Color(.55f,.95f,.3f),4.3f+rank,rank+1);
                    var field=SkillDamageBudgets.ToxicField(rank);
                    CombatArea.Spawn(owner,session,target,3.7f*range,damage*field.TickCoefficient,0,field.Startup,field.Duration,field.Interval,new Color(.51f,.88f,.32f),false,false,rank==3?2.8f:0,damage*field.FinisherCoefficient,5,rank,castId:castId,visual:SkillVisualRecipe.Poison);
                    break;
                case 7:
                    EnemyController mark=lockedTarget != null && !lockedTarget.IsDead ? lockedTarget : null;
                    Vector3 fireDirection=mark!=null?CombatFx.Flat(mark.transform.position-owner.transform.position).normalized:forward;
                    Vector3 side=Vector3.Cross(Vector3.up,fireDirection)*(step%2==0?-.55f:.55f);
                    if (CombatSight.Direct(owner.transform.position, owner.transform.position + side + fireDirection))
                        CombatProjectile.Friendly(owner,session,owner.transform.position+side+fireDirection,fireDirection,damage*SkillDamageBudgets.AdvancedImpact(heroClass,skill,rank,step),color,rank>=2,true,false,range,24f*range,mark,castId:castId,markTarget:lockedTarget,markStrength:.08f+rank*.04f);
                    if(step%4==0) AdvancedSkillVfx.Beam(owner,owner.transform.position+side+Vector3.up,owner.transform.position+fireDirection*7f+side+Vector3.up,color,.2f,.07f);
                    break;
                case 9:
                    if(step<steps-1)
                    {
                        Vector3 rainAt=target+Circle(step*2.4f,1.8f*range);
                        if(arrowBatch.IsValid)arrowBatch.ArrowBeat(rainAt,3.4f*range,false);
                        owner.HitArea(rainAt,3.4f*range,damage*SkillDamageBudgets.AdvancedImpact(heroClass,skill,rank,step),0,.12f,castId:castId);
                    }
                    else
                    {
                        if(!arrowBatch.IsValid)arrowBatch=FilledSkillVfx.BeginArrowBatch(owner,target,6f*range,color,true,CombatVisualPriority.Finale,castId);
                        if(arrowBatch.IsValid)arrowBatch.ArrowBeat(target,6f*range,true);
                        owner.HitArea(target,6f*range,damage*SkillDamageBudgets.AdvancedImpact(heroClass,skill,rank,step),1.1f,.7f,castId:castId);
                        if(rank==3) for(int i=0;i<SkillDamageBudgets.RadialArrowCount(heroClass,skill,rank);i++) CombatProjectile.Friendly(owner,session,target,Circle(i*Mathf.PI/6,1),damage*SkillDamageBudgets.RadialArrowCoefficient,color,true,true,false,1.5f,22f,castId:castId);
                    }
                    break;
            }
        }

        private void SpawnTail(Vector3 at,float radius,float stun,float pull=0)
        {
            var tail=SkillDamageBudgets.AdvancedTail(heroClass,skill,rank);
            CombatArea.Spawn(owner,session,at,radius,damage*tail.TickCoefficient,stun,tail.Startup,tail.Duration,tail.Interval,color,false,false,pull,castId:castId,visual:heroClass==HeroClass.Vanguard?SkillVisualRecipe.Steel:heroClass==HeroClass.Arcanist?SkillVisualRecipes.Ultimate(owner.Specialization,step,true):heroClass==HeroClass.Summoner?SkillVisualRecipe.Spirit:SkillVisualRecipe.Neutral);
        }

        private void LaunchArea(Vector3 center, float radius, float duration, float height)
        {
            foreach (EnemyController enemy in session.Enemies)
                if (enemy != null && !enemy.IsDead && enemy.StatusEffects != null && CombatFx.Flat(enemy.transform.position - center).magnitude <= radius && CombatSight.Area(center,enemy.transform.position))
                    enemy.StatusEffects.Knockup(duration, height);
        }

        private void ChainLightning(int maximumTargets)
        {
            HashSet<EnemyController> struck = new HashSet<EnemyController>();
            Vector3 previous = owner.transform.position;
            Vector3 search = target;
            for(int i=0;i<maximumTargets;i++)
            {
                EnemyController nearest=null;
                float best=(i==0?6f:7f)*range;
                for(int j=0;j<session.Enemies.Count;j++)
                {
                    EnemyController enemy=session.Enemies[j];
                    if(enemy==null || enemy.IsDead || struck.Contains(enemy)) continue;
                    float distance=CombatFx.Flat(enemy.transform.position-search).magnitude;
                    if(distance<best && CombatSight.Chain(previous,enemy.transform.position)) { best=distance; nearest=enemy; }
                }
                if(nearest==null) break;
                struck.Add(nearest);
                Vector3 position=nearest.transform.position;
                float endpointHealth=nearest.Health;
                ElementalCombatVfx.Lightning(previous + Vector3.up * 1.15f, position + Vector3.up * 1.15f);
                owner.RegisterSkillHit(castId);
                owner.ApplySpellDodgeBoon(nearest);
                nearest.TakeDamage(damage.Amount*SkillDamageBudgets.AdvancedImpact(heroClass,skill,rank,step),forward,.05f,.35f+rank*.15f, critical:damage.IsCritical,practiceCastId:castId);
                if(nearest.Health<endpointHealth) {
                if(!FilledSkillVfx.IdentityContact(owner,position,Vector3.forward,.85f,new Color(.7f,.85f,1f),2,CombatVisualPriority.RealContact)) AdvancedSkillVfx.Beam(owner,previous+Vector3.up*1.1f,position+Vector3.up*1.1f,new Color(.7f,.85f,1f),.55f,.17f);
                }
                if(rank==3) owner.HitArea(position,1.8f*range,damage*SkillDamageBudgets.AdvancedAuxiliary(heroClass,skill,rank),0,.1f,castId:castId);
                previous=search=position;
            }
            if(struck.Count==0)
            {
                AdvancedSkillVfx.Beam(owner,previous+Vector3.up,target+Vector3.up,new Color(.7f,.85f,1f),.45f,.18f);
                ElementalCombatVfx.Lightning(previous + Vector3.up, target + Vector3.up);
            }
        }

        private EnemyController Nearest(Vector3 at,float maximumDistance)
        {
            EnemyController nearest=null;
            float best=maximumDistance;
            for(int i=0;i<session.Enemies.Count;i++)
            {
                EnemyController enemy=session.Enemies[i];
                if(enemy==null || enemy.IsDead) continue;
                float distance=CombatFx.Flat(enemy.transform.position-at).magnitude;
                if(distance<best && CombatSight.Direct(owner.transform.position,enemy.transform.position)) { nearest=enemy; best=distance; }
            }
            return nearest;
        }

        private void Pull(Vector3 at,float radius,float strength)
        {
            for(int i=0;i<session.Enemies.Count;i++)
            {
                EnemyController enemy=session.Enemies[i];
                if(enemy==null || enemy.IsDead) continue;
                Vector3 delta=CombatFx.Flat(at-enemy.transform.position);
                if(delta.magnitude<radius && delta.magnitude>.6f && CombatSight.Area(at,enemy.transform.position))
                    enemy.ApplyPull(delta.normalized * Mathf.Min(delta.magnitude - .6f, strength * Time.deltaTime));
            }
        }

        private void HitLine(Vector3 a,Vector3 b,float width,CombatDamage amount,float knockback,float stun)
        {
            CombatImpactBatch.Begin();try
            {
            DestructibleProp.StrikeLine(owner,a,b,width,amount,castId);
            for(int i=session.Enemies.Count-1;i>=0;i--)
            {
                EnemyController enemy=session.Enemies[i];
                if(enemy!=null && !enemy.IsDead && CombatFx.SegmentDistance(enemy.transform.position,a,b)<=width+(enemy.IsBoss?.8f:.4f) && CombatSight.Melee(a,enemy.transform.position))
                {owner.RegisterSkillHit(castId);enemy.TakeDamage(amount.Amount,forward,knockback,stun, critical:amount.IsCritical,practiceCastId:castId);}
            }
                    }
            finally { CombatImpactBatch.End(); }
        }

        private void Burst(Vector3 at,float radius,CombatDamage amount,Color tint,int detail,SkillVisualRecipe visual)
        {
            if (visual == SkillVisualRecipe.Steel) FilledSkillVfx.Crescent(owner,at,forward,radius,tint,priority:CombatVisualPriority.Finale,castId:castId);
            else if (visual != SkillVisualRecipe.Neutral) FilledSkillVfx.Impact(owner,at,radius,SkillVisualRecipes.Filled(visual),tint,CombatVisualPriority.Finale,castId);
            CombatFx.Ring(at,radius,tint,.6f,.25f);
            owner.HitArea(at,radius,amount,1.1f,.7f,castId:castId);
        }

        private void OnDestroy(){OnDisable();}
        private void OnDisable()
        {castReceipt?.Release();castReceipt=null;if(healingAura!=null){healingAura.Stop();healingAura=null;}if(step<steps&&arrowBatch.IsValid)arrowBatch.Retire();}
        private Vector3 Clamp(Vector3 point) { return CombatSight.GroundPoint(origin,Vector3.ClampMagnitude(CombatFx.Flat(point),session.ArenaRadius-.7f)); }
        private static Vector3 Circle(float angle,float radius) { return new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*radius; }
    }
}
