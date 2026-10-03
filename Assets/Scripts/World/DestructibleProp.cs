using System.Collections.Generic;
using UnityEngine;

namespace Emberfall
{
    // Scenery is deliberately not an EnemyController: it cannot satisfy a wave,
    // trigger enemy on-hit mechanics, grant kill XP, or award persistent loot.
    public sealed class DestructibleProp : MonoBehaviour
    {
        private static readonly List<DestructibleProp> props=new List<DestructibleProp>();
        private DestructiblePropRules rules;
        private Transform visual;
        private Material debrisMaterial;
        private PropRecovery recovery;
        private bool blocksPath,initialized;
        private float radius,hitAt=-10;
        private WorldTraversal.ObstacleHandle obstacle;
        private PlayerController breakOwner;
        private int breakEpoch;
        internal bool WasBrokenBy(PlayerController owner,int epoch){return Broken&&owner!=null&&breakOwner==owner&&breakEpoch==epoch;}
        public bool Broken {get{return rules==null||rules.Broken;}}
        public float Radius {get{return radius;}}
        public static int ActiveCount
        {get{int count=0;foreach(var prop in props)if(prop!=null&&prop.initialized&&prop.gameObject.activeInHierarchy)count++;return count;}}

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry(){props.Clear();}
        internal static bool CanPlace(Vector3 at,float size)
        {
            props.RemoveAll(p=>p==null);
            if(float.IsNaN(at.x)||float.IsNaN(at.y)||float.IsNaN(at.z)||float.IsInfinity(at.x)||float.IsInfinity(at.y)||float.IsInfinity(at.z))return false;
            if(ActiveCount>=DestructiblePropRules.MaximumProps||!WorldTraversal.IsWalkable(at,size))return false;
            foreach(var prop in props)if(prop!=null&&prop.gameObject.activeInHierarchy&&!prop.Broken&&CombatFx.Flat(at-prop.transform.position).magnitude<size+prop.radius+.12f)return false;
            return true;
        }
        internal void Initialize(DestructibleKind kind,int level,float size,bool blocking,PropRecovery reward,Transform model,Material shardMaterial)
        {
            rules=new DestructiblePropRules(kind,level);radius=size;blocksPath=blocking;recovery=reward;visual=model;debrisMaterial=shardMaterial;
            initialized=true;props.Add(this);RegisterObstacle();
        }
        private void RegisterObstacle()
        {if(initialized&&gameObject.activeInHierarchy&&blocksPath&&!Broken&&obstacle==null)obstacle=WorldTraversal.AddDynamicCircle(transform.position,radius);}
        private void ReleaseObstacle()
        {if(obstacle!=null)WorldTraversal.RemoveDynamicObstacle(obstacle);obstacle=null;}
        private void OnEnable(){RegisterObstacle();}
        private void OnDisable(){ReleaseObstacle();}
        private void OnDestroy(){ReleaseObstacle();props.Remove(this);if(rules!=null)rules.Dispose();}
        private void Update()
        {
            if(visual==null||Broken||Time.deltaTime<=0)return;
            float t=Mathf.Clamp01((Time.time-hitAt)/.2f),kick=Mathf.Sin(t*Mathf.PI*3f)*(1f-t);
            visual.localRotation=Quaternion.Euler(kick*4f,0,kick*3f);
        }
        private static bool CanAffect(PlayerController owner)
        {var game=GameSession.Instance;return owner!=null&&game!=null&&game.Player==owner&&!game.PracticeActive&&game.HasStarted&&!owner.IsDead&&!game.InputBlocked;}
        private bool Reachable(Vector3 origin)
        {return WorldTraversal.HasLineOfSightIgnoringObstacle(origin,transform.position,obstacle);}
        internal void Impact(PlayerController owner,int castId,CombatDamage damage)
        {
            if(!CanAffect(owner)||Broken||!gameObject.activeInHierarchy)return;
            if(castId==0)castId=owner.NewCastId();
            long key=unchecked(((long)owner.CombatEpoch+1L)<<32)|(uint)castId;
            PropHitResult result=rules.Hit(key,damage.Amount);if(!result.Applied)return;
            hitAt=Time.time;
            HitFeedback.Spawn(transform.position+Vector3.up*.6f,transform.position-owner.transform.position,result.Broke?1f:.6f,damage.IsCritical,priority:CombatVisualPriority.RealContact);
            if(!result.Broke)return;
            breakOwner=owner;breakEpoch=owner.CombatEpoch;
            ReleaseObstacle();visual.gameObject.SetActive(false);
            DestructibleDebrisBurst.Spawn(transform,debrisMaterial,EffectPreferences.ReducedEffects?2:4);
            GameAudio.Play(SoundCue.Hit);
            if(!rules.TryClaimRecovery()||recovery==PropRecovery.None)return;
            var game=GameSession.Instance;
            // Limited-healing challenges never gain free healing from scenery.
            if(recovery==PropRecovery.Health&&!game.ChallengeRun)
            {float before=owner.Health;owner.Heal(owner.MaxHealth*.03f);if(owner.Health>before)game.LogSystem("打破陶罐 · 恢复少量生命");}
            else {float before=owner.Energy;owner.RestoreSkillEnergy(6);float gained=owner.Energy-before;if(gained>0)game.LogSystem("破坏杂物 · 能量 +"+gained.ToString("0.#"));}
        }
        public static void StrikeArea(PlayerController owner,Vector3 at,float range,CombatDamage damage,int castId)
        {
            if(!CanAffect(owner)||range<=0||damage.Amount<=0)return;
            if(castId==0)castId=owner.NewCastId();
            foreach(var prop in props)
                if(prop!=null&&!prop.Broken&&prop.gameObject.activeInHierarchy&&CombatFx.Flat(prop.transform.position-at).magnitude<=range+prop.radius&&prop.Reachable(at))prop.Impact(owner,castId,damage);
        }
        public static void StrikeCone(PlayerController owner,Vector3 at,Vector3 forward,float range,float arc,CombatDamage damage,int castId)
        {
            if(!CanAffect(owner)||range<=0||damage.Amount<=0)return;
            if(castId==0)castId=owner.NewCastId();
            foreach(var prop in props)
            {
                if(prop==null||prop.Broken||!prop.gameObject.activeInHierarchy)continue;
                Vector3 delta=CombatFx.Flat(prop.transform.position-at);
                if(delta.magnitude<=range+prop.radius&&(delta.sqrMagnitude<.1f||Vector3.Angle(forward,delta)<=arc*.5f)&&prop.Reachable(at))prop.Impact(owner,castId,damage);
            }
        }
        public static void StrikeLine(PlayerController owner,Vector3 from,Vector3 to,float width,CombatDamage damage,int castId)
        {
            if(!CanAffect(owner)||damage.Amount<=0)return;
            if(castId==0)castId=owner.NewCastId();
            foreach(var prop in props)
                if(prop!=null&&!prop.Broken&&prop.gameObject.activeInHierarchy&&CombatFx.SegmentDistance(prop.transform.position,from,to)<=width+prop.radius&&prop.Reachable(from))prop.Impact(owner,castId,damage);
        }
        internal static bool FindProjectileHit(PlayerController owner,Vector3 from,Vector3 to,float width,out DestructibleProp target,out float fraction)
        {
            target=null;fraction=2;
            if(!CanAffect(owner))return false;
            foreach(var prop in props)
            {
                if(prop==null||prop.Broken||!prop.gameObject.activeInHierarchy)continue;
                Vector3 point=prop.transform.position;
                float hit=PropImpactGeometry.EntryFraction(from.x,from.z,to.x,to.z,point.x,point.z,prop.radius+width);
                if(hit<=1&&hit<fraction&&WorldTraversal.HasLineOfSightIgnoringObstacle(from,Vector3.Lerp(from,to,hit),prop.obstacle))
                {target=prop;fraction=hit;}
            }
            return target!=null;
        }
    }

    internal sealed class DestructibleDebrisBurst : MonoBehaviour
    {
        private const int MaximumPieces=32;
        private static int activePieces;
        private Transform[] pieces;
        private Vector3[] velocity,baseScale;
        private float age;
        private bool released;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCount(){activePieces=0;}
        public static void Spawn(Transform parent,Material material,int wanted)
        {
            int count=Mathf.Min(wanted,MaximumPieces-activePieces);if(count<=0)return;
            GameObject root=new GameObject("Bounded breakable debris");root.transform.SetParent(parent,false);
            var burst=root.AddComponent<DestructibleDebrisBurst>();
            burst.pieces=new Transform[count];burst.velocity=new Vector3[count];burst.baseScale=new Vector3[count];
            activePieces+=count;
            for(int i=0;i<count;i++)
            {
                GameObject shard=ProceduralVisuals.Create("Bevelled fragment",PrimitiveType.Cube,material);
                shard.transform.SetParent(root.transform,false);shard.transform.localPosition=Vector3.up*.45f;
                burst.baseScale[i]=new Vector3(.15f+(i%2)*.05f,.12f,.22f);shard.transform.localScale=burst.baseScale[i];
                shard.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                float angle=i*2.39996f+.3f;
                burst.velocity[i]=new Vector3(Mathf.Cos(angle)*1.6f,1.4f+i*.15f,Mathf.Sin(angle)*1.6f);burst.pieces[i]=shard.transform;
            }
        }
        private void Update()
        {
            if(Time.deltaTime<=0||pieces==null)return;float dt=Time.deltaTime;age+=dt;
            if(age>=1.05f){Destroy(gameObject);return;}
            for(int i=0;i<pieces.Length;i++)
            {
                velocity[i]+=Vector3.down*7f*dt;Vector3 p=pieces[i].localPosition+velocity[i]*dt;
                if(p.y<.05f){p.y=.05f;velocity[i]=new Vector3(velocity[i].x*.65f,Mathf.Abs(velocity[i].y)*.18f,velocity[i].z*.65f);}
                pieces[i].localPosition=p;pieces[i].Rotate(dt*130f,dt*70f,dt*110f,Space.Self);
                pieces[i].localScale=baseScale[i]*Mathf.Clamp01((1.05f-age)/.3f);
            }
        }
        private void OnEnable()
        {
            if(!released||pieces==null)return;
            if(activePieces+pieces.Length>MaximumPieces){Destroy(gameObject);return;}
            released=false;activePieces+=pieces.Length;
        }
        private void Release(){if(released)return;released=true;activePieces=Mathf.Max(0,activePieces-(pieces==null?0:pieces.Length));}
        private void OnDisable(){Release();}
        private void OnDestroy(){Release();}
    }
}
