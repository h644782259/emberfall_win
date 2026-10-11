using UnityEngine;
namespace Emberfall
{
    // Cosmetic after-current of a confirmed chain hit; never applies damage or control.
    internal sealed class ChainElectrifiedVisual : MonoBehaviour
    {
        private const float Duration=2.2f;
        private EnemyController target;
        private PlayerController owner;
        private GameSession session;
        private int epoch,seed;
        private float age,radius=.66f;
        private Material material,volumeMaterial;
        private Transform[] branches;private Renderer[] branchRenderers;private MaterialPropertyBlock block;
        private LineRenderer[] arcs,cores;
        private readonly Vector3[] points=new Vector3[17];
        internal static void Attach(PlayerController source,EnemyController enemy)
        {
            if(source==null||enemy==null||enemy.IsDead)return;
            var existing=enemy.GetComponentInChildren<ChainElectrifiedVisual>();
            if(existing!=null&&existing.owner==source&&existing.epoch==source.CombatEpoch)
            {existing.age=0;return;}
            var root=new GameObject("Chain lightning body current");
            root.transform.SetParent(enemy.transform,false);
            if(CombatVisualLease.Attach(root,CombatVisualPriority.SustainedBackground)==null)return;
            var visual=root.AddComponent<ChainElectrifiedVisual>();
            visual.target=enemy;visual.owner=source;visual.session=GameSession.Instance;
            visual.epoch=source.CombatEpoch;
#if UNITY_6000_6_OR_NEWER
            visual.seed=enemy.GetEntityId().GetHashCode()&1023;
#else
            visual.seed=enemy.GetHashCode()&1023;
#endif
            var model=enemy.GetComponentInChildren<CombatModel>();
            Transform anchor;Vector3 flank,other;
            if(model!=null&&model.TryStatusAttachment(out anchor,out flank,out other)&&anchor!=null)
            {root.transform.SetParent(anchor,false);visual.radius=Mathf.Max(.66f,Mathf.Abs(flank.x)*.94f);}
            else root.transform.localPosition=Vector3.up*.8f;
            visual.material=CombatFx.NewGlow();
            int count=EffectPreferences.ReducedEffects?2:3;
            visual.arcs=new LineRenderer[count];visual.cores=new LineRenderer[count];
            visual.branches=new Transform[count];visual.branchRenderers=new Renderer[count];visual.block=new MaterialPropertyBlock();
            visual.volumeMaterial=new Material(Resources.Load<Shader>("SpellEnergySurface"));
            for(int i=0;i<count;i++)
            {
                var branch=new GameObject("Branching electric volume");branch.transform.SetParent(root.transform,false);
                branch.AddComponent<MeshFilter>().sharedMesh=AuthoredSpellBases.Load("Lightning");
                visual.branchRenderers[i]=branch.AddComponent<MeshRenderer>();visual.branchRenderers[i].sharedMaterial=visual.volumeMaterial;
                visual.branchRenderers[i].shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                branch.transform.localScale=new Vector3(.35f,1.25f,.35f);visual.branches[i]=branch.transform;
                visual.arcs[i]=visual.CreateArc("Blue body arc",.055f);
                if(!EffectPreferences.ReducedEffects)visual.cores[i]=visual.CreateArc("White electric core",.019f);
            }
            visual.Draw();
        }
        private LineRenderer CreateArc(string name,float width)
        {
            var child=new GameObject(name);child.transform.SetParent(transform,false);
            var line=child.AddComponent<LineRenderer>();line.useWorldSpace=false;
            line.positionCount=points.Length;line.widthMultiplier=width;
            line.sharedMaterial=material;line.numCapVertices=2;line.numCornerVertices=1;
            line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows=false;return line;
        }
        private void LateUpdate()
        {
            if(target==null||target.IsDead||!target.gameObject.activeInHierarchy||owner==null||owner.IsDead||
                owner.CombatEpoch!=epoch||session==null||GameSession.Instance!=session||session.Player!=owner||
                !session.HasStarted||session.CombatEnded||session.ModeFinished)
            {Retire();return;}
            if(session.InputBlocked||Time.deltaTime<=0)return;
            age+=Time.deltaTime;if(age>=Duration){Retire();return;}Draw();
        }
        private void Draw()
        {
            // Deterministic flicker does not consume the combat random sequence.
            float tick=Mathf.Floor(age/.055f),fade=Mathf.Clamp01((Duration-age)/.4f);
            for(int i=0;i<arcs.Length;i++)
            {
                float pulse=.65f+.35f*Mathf.Sin(age*33f+i*2.1f);
                float side=i*Mathf.PI*2/arcs.Length+tick*.37f;
                branches[i].localPosition=new Vector3(Mathf.Cos(side)*radius*.72f,-.6f,Mathf.Sin(side)*radius*.72f);
                branches[i].localRotation=Quaternion.Euler(0,side*Mathf.Rad2Deg,Mathf.Sin(tick+i)*16);
                block.SetColor("_Tint",new Color(.25f,.65f,1f));block.SetFloat("_Opacity",fade*pulse);block.SetFloat("_Phase",age+i);branchRenderers[i].SetPropertyBlock(block);
                for(int j=0;j<points.Length;j++)
                {
                    float t=j/(float)(points.Length-1);
                    float jitter=Mathf.Sin(seed+tick*7.13f+j*12.7f+i*19.3f);
                    float angle=t*Mathf.PI*.85f+i*Mathf.PI*2/arcs.Length+tick*.37f+jitter*.3f;
                    float r=radius*(1f+.25f*jitter);
                    points[j]=new Vector3(Mathf.Cos(angle)*r,(t-.5f)*2f+jitter*.16f,Mathf.Sin(angle)*r);
                }
                arcs[i].SetPositions(points);arcs[i].startColor=arcs[i].endColor=new Color(.13f,.65f,1f,fade*pulse*.4f);
                if(cores[i]!=null){cores[i].SetPositions(points);cores[i].startColor=cores[i].endColor=new Color(.85f,.97f,1f,fade*pulse);}
            }
        }
        private void Retire(){gameObject.SetActive(false);Destroy(gameObject);}
        private void OnDestroy(){if(material!=null)Destroy(material);if(volumeMaterial!=null)Destroy(volumeMaterial);}
    }
}
