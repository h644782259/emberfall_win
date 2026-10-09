using UnityEngine;
namespace Emberfall
{
    // Presentation only: release-time fragments, never an extra damage/targeting path.
    internal sealed class SkillImpactDetail : MonoBehaviour
    {
        private static Material material;
        private GameSession session; private PlayerController player; private int epoch;
        private float age,life; private bool poison;
        private Transform[] fragments; private Renderer[] renderers; private Vector3[] positions;
        private MaterialPropertyBlock block;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset(){if(material!=null)Destroy(material);material=null;}
        internal static void EnemyRelease(EnemyController enemy,Vector3 at,float radius,bool slam,bool slime)
        {
            var game=GameSession.Instance;if(game==null||game.Player==null||enemy==null)return;
            var root=new GameObject("Enemy impact / lit fragments");root.transform.position=at;
            if(CombatVisualLease.Attach(root,CombatVisualPriority.Decoration)==null)return;
            if(material==null){var shader=Resources.Load<Shader>("FilledSpell");material=new Material(shader!=null?shader:Shader.Find("Sprites/Default"));}
            var fx=root.AddComponent<SkillImpactDetail>();fx.session=game;fx.player=game.Player;fx.epoch=game.Player.CombatEpoch;fx.life=slam?.65f:.4f;fx.poison=slime;fx.block=new MaterialPropertyBlock();
            int count=EffectPreferences.ReducedEffects?3:slam?8:5;
            fx.fragments=new Transform[count];fx.renderers=new Renderer[count];fx.positions=new Vector3[count];
            for(int i=0;i<count;i++)
            {
                float angle=i*2.399963f;Vector3 offset=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*radius*(.18f+.055f*i);
                // Do not ornament a side of cover that this attack cannot reach.
                if(!CombatSight.Direct(enemy.transform.position,at+offset))continue;
                var part=ProceduralVisuals.Create(slime?"Venom droplet":"Impact shard",slime?PrimitiveType.Sphere:PrimitiveType.Cube,material);
                part.transform.SetParent(root.transform,false);part.transform.localPosition=offset+Vector3.up*.12f;
                part.transform.localRotation=Quaternion.Euler(i*31,i*57,25+i*13);
                part.transform.localScale=slime?Vector3.one*(.12f+.02f*i):new Vector3(.08f,.23f+.045f*i,.11f);
                fx.fragments[i]=part.transform;fx.renderers[i]=part.GetComponent<Renderer>();fx.positions[i]=part.transform.localPosition;
                fx.renderers[i].shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                fx.renderers[i].receiveShadows=false;
            }
            fx.Present(0);
        }
        private void Update()
        {
            if(session==null||session!=GameSession.Instance||player==null||session.Player!=player||player.CombatEpoch!=epoch||session.CombatEffectsEnded){Destroy(gameObject);return;}
            if(session.InputBlocked)return;
            age+=Time.deltaTime;if(age>=life){Destroy(gameObject);return;}Present(age/life);
        }
        private void Present(float t)
        {
            for(int i=0;i<fragments.Length;i++)
            {
                if(fragments[i]==null)continue;
                // Vertical wake keeps the certified horizontal attack footprint unchanged.
                fragments[i].localPosition=positions[i]+Vector3.up*Mathf.Sin(t*Mathf.PI)*(.35f+i*.07f);
                fragments[i].Rotate(0,Time.deltaTime*(35+i*9),Time.deltaTime*45,Space.Self);
                block.SetColor("_Color",poison?new Color(.38f,.85f,.13f,.7f):Color.Lerp(new Color(.7f,.13f,.035f),new Color(1,.69f,.22f),i/(float)fragments.Length));
                block.SetFloat("_Element",poison?4:1);block.SetFloat("_Seed",i*.37f);block.SetFloat("_Progress",t);block.SetFloat("_Style",1);block.SetFloat("_Opacity",1-t);
                renderers[i].SetPropertyBlock(block);
            }
        }
    }
}
