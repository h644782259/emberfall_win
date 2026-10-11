using UnityEngine;
namespace Emberfall
{
    // A small focus at the actual hand/weapon bridges character motion and the emitted field.
    internal sealed class SkillCastConduit : MonoBehaviour
    {
        private PlayerController owner;private GameSession session;private CombatModel model;
        private int epoch;private float age,life;private ParticleSystem particles;
        internal static void Begin(PlayerController hero,CombatModel body,int skill,int rank,int castId)
        {
            if(hero==null||body==null)return;
            float duration=SkillPerformanceTiming.Duration(hero.HeroClass,skill,rank);
            float cadence=skill==2?SkillDamageBudgets.EarlyField(hero.HeroClass,rank).Interval:SkillDamageBudgets.AdvancedInterval(hero.HeroClass,skill);
            body.BeginSkillPerformance(skill,duration,cadence,castId);
            Create(hero,body,duration);
        }
        internal static SkillCastConduit BeginCharge(PlayerController hero,CombatModel body,float duration)
        {return hero==null||body==null?null:Create(hero,body,duration);}
        private static SkillCastConduit Create(PlayerController hero,CombatModel body,float duration)
        {
            var previous=hero.GetComponentInChildren<SkillCastConduit>();if(previous!=null){previous.gameObject.SetActive(false);Destroy(previous.gameObject);}
            if(hero.HeroClass==HeroClass.Vanguard)return null; // Its real blade ribbon owns the release link.
            var root=new GameObject("Weapon focus / casting conduit");root.transform.SetParent(hero.transform,false);
            if(CombatVisualLease.Attach(root,CombatVisualPriority.Decoration)==null)return null;
            var link=root.AddComponent<SkillCastConduit>();link.owner=hero;link.session=GameSession.Instance;link.model=body;link.epoch=hero.CombatEpoch;link.life=duration;
            link.particles=ElementalCombatVfx.Create(root.transform,"Weapon focus motes",hero.HeroClass==HeroClass.Summoner?ElementalCombatVfx.Element.Poison:ElementalCombatVfx.Element.Lightning,EffectPreferences.ReducedEffects?8:20,.13f);
            if(link.particles!=null){var main=link.particles.main;main.simulationSpace=ParticleSystemSimulationSpace.World;main.startLifetime=.4f;main.startSize=.12f;link.particles.useAutoRandomSeed=false;link.particles.randomSeed=751;link.particles.Play();}
            link.Follow();return link;
        }
        internal void ChargeProgress(float progress)
        {
            if(particles==null)return;var main=particles.main;main.startSize=Mathf.Lerp(.07f,.2f,progress);
            var emission=particles.emission;emission.rateOverTime=Mathf.Lerp(EffectPreferences.ReducedEffects?5:10,EffectPreferences.ReducedEffects?12:32,progress);Follow();
        }
        internal void Retire(){gameObject.SetActive(false);Destroy(gameObject);}
        private void Follow()
        {Vector3 anchor;if(model.TryGetWeaponVisualAnchor(owner.HeroClass==HeroClass.Ranger?WeaponVisualAnchor.BowArrowRest:WeaponVisualAnchor.StaffCore,out anchor))transform.position=anchor;}
        private void LateUpdate()
        {
            if(owner==null||owner.IsDead||model==null||session==null||session!=GameSession.Instance||!session.HasStarted||session.Player!=owner||owner.CombatEpoch!=epoch||session.CombatEffectsEnded){Destroy(gameObject);return;}
            if(session.InputBlocked)return;age+=Time.deltaTime;if(age>=life){Destroy(gameObject);return;}Follow();
        }
    }
}
