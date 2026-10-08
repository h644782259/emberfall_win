using UnityEngine;
namespace Emberfall
{
    internal static class VenomSkillVfx
    {
        internal static void Contact(PlayerController owner,Vector3 point,bool consumed)
        {
            if(owner==null||owner.IsDead)return;
            var root=new GameObject(consumed?"Three poison seeds consumed":"Venom arrow contact");
            root.transform.position=point;
            if(CombatVisualLease.Attach(root,consumed?CombatVisualPriority.RealContact:CombatVisualPriority.Decoration)==null)return;
            root.AddComponent<VenomContactVisual>().Initialize(owner,consumed);
        }
    }
    // A body-local sized mark, not an expanding ground radius or damage-area preview.
    internal sealed class VenomContactVisual:MonoBehaviour
    {
        private PlayerController owner;private int epoch;private float age,duration;
        private Material material;private Transform[] pieces;
        internal void Initialize(PlayerController player,bool consumed)
        {
            owner=player;epoch=player.CombatEpoch;duration=consumed?.32f:.12f;
            material=new Material(Shader.Find("Standard")){color=consumed?new Color(.7f,1f,.27f):new Color(.35f,.76f,.28f)};
            pieces=new Transform[consumed?3:1];
            for(int i=0;i<pieces.Length;i++)
            {
                var part=ProceduralVisuals.Create(consumed?"Consumed poison seed":"Physical venom nick",PrimitiveType.Cube,material);
                part.transform.SetParent(transform,false);
                part.transform.localPosition=consumed?new Vector3((i-1)*.12f,(i==1?.09f:0),0):Vector3.zero;
                part.transform.localScale=consumed?new Vector3(.08f,.13f,.08f):new Vector3(.04f,.17f,.04f);
                part.transform.localRotation=Quaternion.Euler(0,0,consumed?45:-25);
                part.GetComponent<MeshRenderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                pieces[i]=part.transform;
            }
        }
        private void Update()
        {
            var game=GameSession.Instance;
            if(owner==null||owner.IsDead||owner.CombatEpoch!=epoch||game==null||game.Player!=owner||!game.HasStarted||game.CombatEffectsEnded){Destroy(gameObject);return;}
            if(game.InputBlocked)return;
            age+=Time.deltaTime;if(age>=duration){Destroy(gameObject);return;}
            // Shrinking inward communicates expenditure of three stacks, never outward splash.
            transform.localScale=Vector3.one*Mathf.Max(.1f,1f-age/duration);
        }
        private void OnDestroy(){if(material!=null)Destroy(material);}
    }
}
