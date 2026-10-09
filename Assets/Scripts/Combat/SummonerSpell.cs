using UnityEngine;

namespace Emberfall
{
    internal sealed class SummonerSpell : MonoBehaviour
    {
        private CastFirstHitReceipt castReceipt;
        private PlayerController owner;
        private GameSession session;
        private int rank, epoch, castId;
        private float damage, age, nextTick;
        private CombatDamage finisherDamage;
        private readonly ScheduledImpactBatch<EnemyController> pendingTargets = new ScheduledImpactBatch<EnemyController>();
        private bool pendingFinisher;
        private bool IsCurrentCast { get { return owner != null && session != null && session.Player == owner && !owner.IsDead && session.HasStarted && !session.CombatEffectsEnded && owner.CombatEpoch == epoch; } }
        private float Radius { get { return 4.4f * GameBalance.SkillRangeMultiplier(rank); } }
        public static void Cast(PlayerController player, GameSession game, int skill, int rank, Vector3 target, float damage, EnemyController commandTarget = null, bool preserveTargetPoint = false, int castId = 0)
        {
            int propCast=castId==0?player.NewCastId():castId;
            float range = GameBalance.SkillRangeMultiplier(rank);
            Color color = GameBalance.ClassColor(HeroClass.Summoner);
            if (skill == 2 || skill == 4 || skill == 9)
            {
                var form = skill == 2 ? SummonedCompanion.Kind.Wolf : skill == 4 ? SummonedCompanion.Kind.Spirit : SummonedCompanion.Kind.Treant;
                Vector3 position = CombatSight.GroundPoint(player.transform.position,target);
                SummonedCompanion partner = SummonedCompanion.CastContract(player, game, form, rank, position, damage,
                    game.Progression.Profile.summonerRoute == SummonerRoute.Pack, preserveTargetPoint ? commandTarget : commandTarget != null ? commandTarget : player.AimTarget, preserveTargetPoint);
                if (partner != null) FilledSkillVfx.Impact(player,partner.transform.position,1.8f,FilledVfxKind.Summon,color,CombatVisualPriority.ActionBody);
            }
            else if (skill == 0)
            {
                if(!FilledSkillVfx.IdentityContact(player,player.transform.position,player.transform.forward,1.6f*range,color,3)) CombatFx.Slash(player.transform.position, player.transform.forward, 5f * range, color);
                CombatDamage impact = player.RollDirectDamage(damage * SummonerDamageRules.ImpulseCoefficient);
                DestructibleProp.StrikeCone(player,player.transform.position,player.transform.forward,5f*range,110,impact,propCast);
                foreach (var enemy in game.Enemies.ToArray())
                {
                    if (enemy == null || enemy.IsDead) continue;
                    Vector3 delta = CombatFx.Flat(enemy.transform.position - player.transform.position);
                    if (delta.magnitude <= 5f * range && (delta.sqrMagnitude < .1f || Vector3.Angle(player.transform.forward, delta) < 55) && CombatSight.Melee(player.transform.position,enemy.transform.position))
                    {
                        float contactHealth=enemy.Health;
                        enemy.TakeDamage(impact.Amount, delta.normalized, 1.25f + rank * .2f, .2f, critical: impact.IsCritical,practiceCastId:propCast);
                        if(enemy.Health<contactHealth)FilledSkillVfx.IdentityContact(player,enemy.transform.position,player.transform.forward,.6f,color,3,CombatVisualPriority.RealContact);
                        player.RegisterSkillHit(propCast);
                    }
                }
            }
            else if (skill == 1)
                CombatArea.Spawn(player, game, target, 3.2f * range, damage * SummonerDamageRules.ThornTickCoefficient, 0, SummonerDamageRules.ThornStartup, SummonerDamageRules.ThornDuration(rank), SummonerDamageRules.ThornInterval, color, statusSkill: 1, statusRank: rank,castId:propCast,visual:SkillVisualRecipe.Poison);
            else if(skill==5)
                CombatProjectile.Friendly(player,game,player.transform.position,player.transform.forward,player.RollDirectDamage(damage*2.6f),color,true,false,false,range,22f,castId:propCast);
            else if(skill==6)
                CombatArea.Spawn(player,game,target,4f*range,player.RollDirectDamage(damage*3.2f),.8f,.2f,0,1,color,castId:propCast,visual:SkillVisualRecipe.Spirit);
            else if (skill == 7)
            {
                var obj = new GameObject("引力印记"); obj.transform.position = target;
                var spell = obj.AddComponent<SummonerSpell>();
                spell.owner = player; spell.session = game; spell.rank = rank; spell.damage = damage; spell.epoch = player.CombatEpoch; spell.castId=propCast;spell.castReceipt=player.RetainCastReceipt(propCast);
                spell.finisherDamage = player.RollDirectDamage(damage * SummonerDamageRules.MarkFinisherCoefficient);
                AdvancedSkillVfx.Rune(player, target, 4.4f * range, color, 3.3f, rank + 1);
            }
        }
        private void Update()
        {
            if (!IsCurrentCast) { Retire(); return; }
            if (session.InputBlocked || Time.deltaTime <= 0) return;
            age += Time.deltaTime;
            foreach (var enemy in session.Enemies)
            {
                if (enemy == null || enemy.IsDead) continue;
                Vector3 delta = CombatFx.Flat(transform.position - enemy.transform.position);
                if (delta.magnitude > Radius || !CombatSight.Area(transform.position,enemy.transform.position)) continue;
                enemy.Provoke();
                if (delta.magnitude > .6f)
                    enemy.ApplyPull(delta.normalized * Mathf.Min(delta.magnitude - .6f, Time.deltaTime * (3.5f + rank)));
            }
            for (int tick=0;tick<ScheduledTickWindow.MaximumCatchUp;tick++)
            {
                if (!IsCurrentCast) { Retire(); return; }
                if (session.InputBlocked) return;
                if (!pendingTargets.Pending)
                {
                    pendingFinisher = ScheduledTickWindow.Collect(ref nextTick, age, SummonerDamageRules.MarkLastTick, SummonerDamageRules.MarkInterval, 1) == 0;
                    if (pendingFinisher && (age < SummonerDamageRules.MarkFinisherTime || !ScheduledTickWindow.Drained(nextTick,SummonerDamageRules.MarkLastTick))) break;
                    pendingTargets.Begin(session.Enemies);
                    if (!pendingFinisher && tick == 0)
                        DestructibleProp.StrikeArea(owner,transform.position,Radius,damage*SummonerDamageRules.MarkTickCoefficient,castId);
                }
                while (pendingTargets.Pending)
                {
                    if (!IsCurrentCast) { Retire(); return; }
                    if (session.InputBlocked) return;
                    EnemyController enemy;
                    if (!pendingTargets.TryTake(out enemy)) break;
                    if (enemy == null || enemy.IsDead || !session.Enemies.Contains(enemy)) continue;
                    Vector3 delta = CombatFx.Flat(transform.position - enemy.transform.position);
                    if (delta.magnitude > Radius || !CombatSight.Area(transform.position,enemy.transform.position)) continue;
                    if (pendingFinisher)
                    {
                        enemy.TakeDamage(finisherDamage.Amount, -delta.normalized, .3f, .25f, critical: finisherDamage.IsCritical,practiceCastId:castId);
                        owner.RegisterSkillHit(castId);
                        if (!enemy.IsDead) enemy.StatusEffects.Knockup(.7f + rank * .1f, 1.2f + rank * .2f);
                    }
                    else
                    {
                        enemy.TakeDamage(damage*SummonerDamageRules.MarkTickCoefficient,Vector3.zero,impact:false,practiceCastId:castId);
                        owner.RegisterSkillHit(castId);
                    }
                }
                if (pendingFinisher)
                {
                    DestructibleProp.StrikeArea(owner,transform.position,Radius,finisherDamage,castId);
                    FilledSkillVfx.Impact(owner,transform.position,Radius,FilledVfxKind.Summon,GameBalance.ClassColor(HeroClass.Summoner),CombatVisualPriority.SustainedBackground);
                    CombatFx.Ring(transform.position,Radius,GameBalance.ClassColor(HeroClass.Summoner),.5f,.22f);
                    Retire();return;
                }
            }
        }
        private void Retire() { pendingTargets.Clear(); Destroy(gameObject); }
        private void OnDisable() { castReceipt?.Release();castReceipt=null;pendingTargets.Clear(); }
        private void OnDestroy() { OnDisable(); }
    }
}
