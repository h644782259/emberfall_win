using UnityEngine;
namespace Emberfall
{
    // Presentation only. Called at the existing melee release; the 1.15s wake never deals damage.
    internal sealed class BlenderSkillVfx : MonoBehaviour
    {
        private sealed class Part { internal Transform Transform; internal Renderer Renderer; internal Vector3 At,Scale; internal float Phase,Delay; internal int Motion; internal bool Core; }
        private static Mesh crescent,core,shard,fault;
        private static Material material;
        private readonly System.Collections.Generic.List<Part> parts=new System.Collections.Generic.List<Part>();
        private MaterialPropertyBlock block;
        private void Awake() { block = new MaterialPropertyBlock(); }
        private PlayerController owner; private GameSession session; private int epoch; private float age,range; private bool shock;
        private const float Life=1.15f;
        private static Mesh Load(string name)
        { var data=Resources.Load<TextAsset>("BlenderVfx/"+name);return data==null?null:AuthoredActorMeshes.Decode(data.bytes,"Blender VFX / "+name); }

        internal static bool TryPlay(PlayerController hero,float range,bool groundShock)
        {
            if(hero==null||hero.IsDead||float.IsNaN(range)||float.IsInfinity(range)||range<=0)return false;
            if(crescent==null)crescent=Load("Crescent");if(core==null)core=Load("CrescentCore");if(shard==null)shard=Load("Shard");if(fault==null)fault=Load("Fault");
            if(crescent==null||core==null||shard==null||fault==null)return false; // Original presentation remains available when imports are missing.
            if(material==null){var shader=Resources.Load<Shader>("FilledSpell");if(shader==null||!shader.isSupported)return false;material=new Material(shader);material.renderQueue=3070;}
            // A rotating silhouette needs a certified complete footprint. Near cover retain the original clipped presentation.
            float footprint=(groundShock?4.8f:3.4f)*range;
            if(!CombatSight.VisualFootprint(hero.transform.position,hero.transform.position,footprint))return false;
            var root=new GameObject(groundShock?"Blender ground shock wake":"Blender whirlwind blades");root.transform.position=hero.transform.position;root.transform.rotation=Quaternion.LookRotation(hero.transform.forward);
            if(CombatVisualLease.Attach(root,CombatVisualPriority.ActionBody)==null)return true;
            var fx=root.AddComponent<BlenderSkillVfx>();fx.owner=hero;fx.session=GameSession.Instance;fx.epoch=hero.CombatEpoch;fx.range=range;fx.shock=groundShock;
            int cap=EffectPreferences.ReducedEffects?5:Application.isMobilePlatform?8:12;
            if(!groundShock)
            {
                // Blade silhouette and hot core precede optional motes on every quality tier.
                for(int i=0;i<3;i++)fx.Add(crescent,new Vector3(0,.56f+i*.24f,0),Vector3.one,0,i*Mathf.PI*2/3,0,false);
                for(int i=0;i<2&&fx.parts.Count<cap;i++)fx.Add(core,new Vector3(0,.63f+i*.16f,0),Vector3.one,0,i*Mathf.PI*2/3+.04f,0,true);
                for(int i=0;fx.parts.Count<cap;i++)fx.Add(shard,Vector3.zero,new Vector3(.025f,.13f,.04f),2,i*2.399f,0,false);
            }
            else
            {
                // Contact is visible at age zero; delayed crests are a secondary wake only.
                fx.Add(fault,new Vector3(0,.07f,2.5f),new Vector3(2,1,2),3,0,0,true);
                for(int j=0;j<3;j++)fx.Add(shard,new Vector3((j%2==0?1:-1)*(.12f+j*.1f),.015f,.5f+j*.68f),new Vector3(.10f+j*.027f,.75f,.24f),1,0,j*.055f,false);
                fx.Add(fault,new Vector3(0,.045f,1.3f),Vector3.one,3,0,0,true);
                for(int j=0;fx.parts.Count<cap;j++)fx.Add(shard,new Vector3(0,.04f,2.5f),new Vector3(.07f,.18f,.10f),2,j*Mathf.PI*2/7,0,false);
            }
            fx.Sample();return true;
        }
        private void Add(Mesh mesh,Vector3 at,Vector3 scale,int motion,float phase,float delay,bool core)
        {
            var go=new GameObject(core?"Hot contact core":"Authored skill surface");go.transform.SetParent(transform,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
            parts.Add(new Part{Transform=go.transform,Renderer=renderer,At=at,Scale=scale,Motion=motion,Phase=phase,Delay=delay,Core=core});
        }
        private void Update()
        {
            if(owner==null||owner.IsDead||owner.CombatEpoch!=epoch||session==null||GameSession.Instance!=session||session.Player!=owner||!session.HasStarted||session.ModeFinished){Retire();return;}
            if(session.InputBlocked||Time.deltaTime<=0)return;
            age+=Time.deltaTime;if(age>=Life){Retire();return;}Sample();
        }
        private void Sample()
        {
            foreach(var p in parts)
            {
                float t=age-p.Delay;p.Transform.gameObject.SetActive(t>=0);if(t<0)continue;
                Vector3 at=p.At,scale=p.Scale;float yaw=0,opacity=Mathf.Clamp01((Life-age)/.38f);
                if(p.Motion==0){float r=1.1f+Mathf.Min(1,age/.48f)*2.3f;scale=new Vector3(r,r*1.3f,r);if(p.Core)scale*=.92f;yaw=(age*9+p.Phase)*Mathf.Rad2Deg;}
                else if(p.Motion==1){scale.y=.12f+1.25f*Mathf.Sin(Mathf.Min(1,t/.45f)*Mathf.PI);opacity*=Mathf.Max(0,1-t/.6f);}
                else if(p.Motion==2){float a=p.Phase+age*2,r=shock?.45f+age*1.3f:Mathf.Min(3.25f,1.3f+age*2);at+=new Vector3(Mathf.Sin(a)*r,Mathf.Sin(age/Life*Mathf.PI)*(shock?.8f:.45f),Mathf.Cos(a)*r);}
                else opacity*=Mathf.Max(0,1-t/.6f);
                p.Transform.localPosition=at*range;p.Transform.localScale=scale*range;p.Transform.localRotation=Quaternion.Euler(0,yaw,0);
                Color tint=p.Core?new Color(1.35f,1.12f,.65f):new Color(.95f,.48f,.12f);tint.a=opacity*(p.Core?.9f:.72f)*Mathf.Lerp(.55f,1,EffectPreferences.EffectsScale);
                block.SetFloat("_Sculpted",p.Core?0:1);block.SetFloat("_Element",0);block.SetFloat("_ImpactLight",Mathf.Clamp01(1-age/.14f)*.35f);
                block.SetColor("_Color",tint);block.SetFloat("_Opacity",opacity);block.SetFloat("_Progress",age/Life);block.SetFloat("_Style",.35f);p.Renderer.SetPropertyBlock(block);
            }
        }
        private void Retire(){gameObject.SetActive(false);Destroy(gameObject);}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset(){if(crescent!=null)Destroy(crescent);if(core!=null)Destroy(core);if(shard!=null)Destroy(shard);if(fault!=null)Destroy(fault);crescent=core=shard=fault=null;if(material!=null)Destroy(material);material=null;}
    }
}
