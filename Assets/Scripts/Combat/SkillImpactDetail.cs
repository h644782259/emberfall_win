using UnityEngine;
namespace Emberfall
{
    // Presentation only: release-time bodies, never an extra damage/targeting path.
    internal sealed class SkillImpactDetail : MonoBehaviour
    {
        private static Material material;
        private GameSession session; private PlayerController player; private int epoch;
        private EnemyController source;private bool chargeBody;
        private float age,life; private bool poison;
        private Transform[] fragments; private Renderer[] renderers; private Vector3[] positions;
        private Mesh[] ownedMeshes;
        private MaterialPropertyBlock block;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset(){if(material!=null)Destroy(material);material=null;}
        private static SkillImpactDetail Create(EnemyController enemy,Vector3 at,float lifetime,int count,bool slime)
        {
            var game=GameSession.Instance;if(game==null||game.Player==null||enemy==null)return null;
            var root=new GameObject("Enemy skill / sculpted body");root.transform.position=at;
            if(CombatVisualLease.Attach(root,CombatVisualPriority.ActionBody)==null)return null;
            if(material==null){var shader=Resources.Load<Shader>("FilledSpell");material=new Material(shader!=null?shader:Shader.Find("Sprites/Default"));material.renderQueue=3050;}
            var fx=root.AddComponent<SkillImpactDetail>();fx.session=game;fx.player=game.Player;fx.epoch=game.Player.CombatEpoch;fx.source=enemy;fx.life=lifetime;fx.poison=slime;fx.block=new MaterialPropertyBlock();
            fx.fragments=new Transform[count];fx.renderers=new Renderer[count];fx.positions=new Vector3[count];fx.ownedMeshes=new Mesh[count];return fx;
        }
        internal static void EnemyRelease(EnemyController enemy,Vector3 at,float radius,bool slam,bool slime)
        {
            int count=EffectPreferences.ReducedEffects?3:slam?8:5;
            var fx=Create(enemy,at,slam?.65f:.4f,count,slime);if(fx==null)return;
            Mesh shard=AuthoredSpellBases.Load(slime?"Flame":"Crystal"),baseMesh=AuthoredSpellBases.Load("Rupture");
            for(int i=0;i<count;i++)
            {
                float angle=i*2.399963f;Vector3 offset=i==0?Vector3.zero:new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*radius*(.18f+.045f*i);
                if(!CombatSight.Direct(enemy.transform.position,at+offset))continue;
                Vector3 scale=i==0&&!slime?new Vector3(radius*.6f,.25f,radius*.6f):slime?new Vector3(.26f,.65f+.1f*i,.26f):new Vector3(.2f,(slam?1.25f:.65f)+.12f*i,.22f);
                fx.Add(i,i==0&&!slime?baseMesh:shard,offset+Vector3.up*.06f,scale,Quaternion.Euler(0,i*57,0),true);
            }
            fx.Present(0);
        }
        internal static void ChargeRelease(EnemyController enemy,float duration)
        {
            var fx=Create(enemy,enemy.transform.position,Mathf.Max(.12f,duration),EffectPreferences.ReducedEffects?2:4,false);if(fx==null)return;
            fx.chargeBody=true;fx.transform.rotation=enemy.transform.rotation;
            Mesh flame=AuthoredSpellBases.Load("Flame");
            for(int i=0;i<fx.fragments.Length;i++)
                fx.Add(i,flame,new Vector3((i%2==0?-1:1)*.32f,.55f,-.2f-i*.15f),new Vector3(.28f,.85f,.28f),Quaternion.Euler(-65,0,0),false);
            fx.Present(0);
        }
        private void Add(int i,Mesh mesh,Vector3 offset,Vector3 scale,Quaternion rotation,bool ground)
        {
            var part=new GameObject(poison?"Raised venom tongue":"Raised impact facet");part.transform.SetParent(transform,false);
            var filter=part.AddComponent<MeshFilter>();var renderer=part.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;
            if(mesh!=null)
            {
                if(ground){ownedMeshes[i]=AnchoredImpactMesh.Create(mesh,transform,offset,scale,rotation);filter.sharedMesh=ownedMeshes[i];}
                else {filter.sharedMesh=mesh;part.transform.localPosition=offset;part.transform.localScale=scale;part.transform.localRotation=rotation;}
            }
            else
            {
                // Retain the previous primitive fallback when an authored resource is unavailable.
                Destroy(part);part=ProceduralVisuals.Create("Impact fallback",poison?PrimitiveType.Sphere:PrimitiveType.Cube,material);part.transform.SetParent(transform,false);
                part.transform.localPosition=offset;part.transform.localScale=scale;part.transform.localRotation=rotation;renderer=part.GetComponent<MeshRenderer>();
            }
            fragments[i]=part.transform;renderers[i]=renderer;positions[i]=part.transform.localPosition;
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
        }
        private void Update()
        {
            if(session==null||session!=GameSession.Instance||player==null||session.Player!=player||player.CombatEpoch!=epoch||session.CombatEffectsEnded||chargeBody&&(source==null||source.IsDead||source.IsStunned||!source.enabled)){Destroy(gameObject);return;}
            if(session.InputBlocked)return;
            age+=Time.deltaTime;if(age>=life){Destroy(gameObject);return;}
            if(chargeBody){transform.position=source.transform.position;transform.rotation=source.transform.rotation;}
            Present(age/life);
        }
        private void Present(float t)
        {
            for(int i=0;i<fragments.Length;i++)
            {
                if(fragments[i]==null)continue;
                // Clipped ground meshes only move vertically, preserving certified XZ faces.
                fragments[i].localPosition=positions[i]+Vector3.up*Mathf.Sin(t*Mathf.PI)*(chargeBody?.07f:poison?.32f:i==0?.02f:.45f+i*.05f);
                block.SetColor("_Color",poison?Color.Lerp(new Color(.12f,.43f,.06f),new Color(.52f,.9f,.16f),i/(float)fragments.Length):Color.Lerp(new Color(.55f,.08f,.025f),new Color(1,.48f,.1f),i/(float)fragments.Length));
                block.SetFloat("_Element",poison?4:1);block.SetFloat("_Sculpted",1);block.SetFloat("_ImpactLight",Mathf.Max(0,1-t*5)*.28f);
                block.SetFloat("_Seed",i*.37f);block.SetFloat("_Progress",t);block.SetFloat("_Style",1);block.SetFloat("_Opacity",Mathf.Clamp01((1-t)*1.8f));
                renderers[i].SetPropertyBlock(block);
            }
        }
        private void OnDestroy(){if(ownedMeshes!=null)foreach(var mesh in ownedMeshes)if(mesh!=null)Destroy(mesh);}
    }
}
