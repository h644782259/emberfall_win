using UnityEngine;
namespace Emberfall
{
    // One producer per cast. Footprints share a single tick and never multiply
    // damage when the rider circles, stops or doubles back over the same enemy.
    internal sealed class FlameRide : MonoBehaviour
    {
        private const float BurnRadius=2.2f;
        private static Mesh tongueMesh;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetArt(){if(tongueMesh!=null)Destroy(tongueMesh);tongueMesh=null;}
        private PlayerController owner;
        private GameSession session;
        private int epoch,castId;
        private float duration,age,nextTick,attack;
        private CastFirstHitReceipt receipt;
        private readonly Vector3[] footprints=new Vector3[3];
        private int footprintCount,cursor;
        private Transform mount;
        private Material flame,core;
        internal static void Spawn(PlayerController player,GameSession game,float seconds,float power,int cast)
        {
            var ride=new GameObject("Fire path riding flame").AddComponent<FlameRide>();
            ride.owner=player;ride.session=game;ride.epoch=player.CombatEpoch;
            ride.duration=seconds;ride.attack=power;ride.castId=cast;ride.receipt=player.RetainCastReceipt(cast);
            ride.flame=CombatFx.NewGlow();ride.flame.color=new Color(1f,.27f,.045f,.9f);
            ride.core=CombatFx.NewGlow();ride.core.color=new Color(1f,.85f,.24f,.95f);
            ride.mount=new GameObject("Flame surf mount").transform;ride.mount.SetParent(ride.transform,false);
            if(tongueMesh==null)tongueMesh=AuthoredSpellBases.Load("Flame");
            for(int i=0;i<7;i++)
            {
                var part=ProceduralVisuals.Create(i==0?"Riding flame core":"Trailing flame tongue",PrimitiveType.Sphere,i==0?ride.core:ride.flame).transform;
                part.SetParent(ride.mount,false);
                part.localPosition=i==0?new Vector3(0,.14f,0):new Vector3((i%3-1)*.3f,.12f,-.35f-(i/3)*.45f);
                part.localScale=i==0?new Vector3(1.15f,.22f,1.8f):new Vector3(.42f,.7f,.6f);
                if(i>0&&tongueMesh!=null){part.GetComponent<MeshFilter>().sharedMesh=tongueMesh;part.localRotation=Quaternion.Euler(55,0,0);}
            }
        }
        private bool Current {get{return owner!=null&&!owner.IsDead&&session!=null&&session.Player==owner&&session.HasStarted&&!session.CombatEffectsEnded&&owner.CombatEpoch==epoch;}}
        private void Update()
        {
            if(!Current){Destroy(gameObject);return;}
            transform.SetPositionAndRotation(owner.transform.position,owner.transform.rotation);
            if(session.InputBlocked||Time.deltaTime<=0)return;
            // No late burst after a stalled frame. At most one tick per update.
            if(age>=duration){Destroy(gameObject);return;}
            if(age>=nextTick)
            {
                nextTick=age+SkillDamageBudgets.FlameRideInterval;
                footprints[cursor]=owner.transform.position;cursor=(cursor+1)%footprints.Length;
                footprintCount=Mathf.Min(footprints.Length,footprintCount+1);
                CombatFx.Ring(owner.transform.position,BurnRadius,new Color(1f,.4f,.1f),1.5f,.18f);
                for(int i=session.Enemies.Count-1;i>=0&&Current;i--)
                {
                    var enemy=session.Enemies[i];if(enemy==null||enemy.IsDead)continue;
                    for(int p=0;p<footprintCount;p++)
                    {
                        Vector3 delta=CombatFx.Flat(enemy.transform.position-footprints[p]);
                        float radius=BurnRadius+(enemy.IsBoss?.85f:.4f)+enemy.HitFootprintBonus;
                        if(delta.sqrMagnitude>radius*radius||!CombatSight.Area(footprints[p],enemy.transform.position))continue;
                        owner.RegisterSkillHit(castId);
                        enemy.TakeDamage(attack*SkillDamageBudgets.FlameRideTick,delta.normalized,0,0,practiceCastId:castId);
                        ElementalCombatVfx.OnEnemy(enemy,ElementalCombatVfx.Element.Fire,.6f);
                        break;
                    }
                }
            }
            age+=Time.deltaTime;
            mount.localScale=new Vector3(1f+Mathf.Sin(age*12f)*.08f,1f,1f);
        }
        private void OnDestroy(){receipt?.Release();if(flame!=null)Destroy(flame);if(core!=null)Destroy(core);}
    }
}
