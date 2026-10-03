using UnityEngine;
namespace Emberfall
{
    // Enemy-local, depth-tested status channels. No targeting reticle or gameplay timer.
    internal sealed class EnemyStatusVisual : MonoBehaviour
    {
        private EnemyController enemy;
        private EnemyStatusEffects status;
        private CombatModel bodyModel;
        private GameObject[] ice = new GameObject[3];
        private GameObject frost;
        private GameObject[] vulnerability = new GameObject[2];
        private Material iceMaterial, markMaterial;
        internal static void Attach(EnemyController owner)
        {
            var visual=owner.GetComponent<EnemyStatusVisual>()??owner.gameObject.AddComponent<EnemyStatusVisual>();
            visual.Bind(owner);
        }
        private void Bind(EnemyController owner)
        {
            if(status!=null)status.VisualStateChanged-=Sync;
            enemy=owner;status=owner.StatusEffects;
            if(status!=null)status.VisualStateChanged+=Sync;
            Sync();
        }
        private void LateUpdate(){Sync();}
        private void Sync()
        {
            bool alive=enemy!=null&&!enemy.IsDead&&status!=null;
            var game=GameSession.Instance;
            bool selected=alive&&game!=null&&game.Player!=null&&game.Player.AimTarget==enemy;
            int count=EnemyStatusVisualRules.IceCount(alive&&status.IsFrozen,alive&&enemy.IsBoss,selected,alive&&enemy.Tier==EnemyController.ThreatTier.Elite,EffectPreferences.ReducedEffects);
            bool marked=alive&&status.HasFrostMark;
            bool vulnerable=alive&&status.IsMarked;
            if(count>0||marked||vulnerable)EnsureMaterials();
            if(bodyModel==null||!bodyModel.gameObject.activeInHierarchy)
                bodyModel=enemy==null?null:enemy.GetComponentInChildren<CombatModel>();
            Transform bodyAnchor=transform;Vector3 frostPoint=new Vector3(-.6f,.7f,0),vulnerablePoint=new Vector3(.6f,.7f,0);
            if(bodyModel!=null)bodyModel.TryStatusAttachment(out bodyAnchor,out frostPoint,out vulnerablePoint);
            if(bodyAnchor==null)bodyAnchor=transform;
            for(int i=0;i<ice.Length;i++)
            {
                if(i<count&&ice[i]==null)ice[i]=Piece("Frozen ankle crystal",iceMaterial,new Vector3((i-1)*.22f,.3f,.12f),new Vector3(.12f,.5f,.14f),new Vector3(0,0,(i-1)*18));
                Set(ice[i],i<count);
            }
            if(marked&&frost==null)frost=Piece("Frost mark diamond",iceMaterial,frostPoint,new Vector3(.14f,.14f,.06f),new Vector3(0,0,45));
            if(frost!=null)AttachBody(frost,bodyAnchor,frostPoint,new Vector3(.14f,.14f,.06f));
            Set(frost,marked);
            for(int i=0;i<vulnerability.Length;i++)
            {
                if(vulnerable&&vulnerability[i]==null)vulnerability[i]=Piece("Vulnerability slash",markMaterial,vulnerablePoint,new Vector3(.045f,.23f,.045f),new Vector3(0,0,-25));
                if(vulnerability[i]!=null)AttachBody(vulnerability[i],bodyAnchor,vulnerablePoint+new Vector3(i*.12f,0,0),new Vector3(.045f,.23f,.045f));
                Set(vulnerability[i],vulnerable);
            }
            float emphasis=EnemyStatusVisualRules.Emphasis(selected,alive&&enemy.IsBoss,alive&&enemy.Tier==EnemyController.ThreatTier.Elite,EffectPreferences.ReducedEffects);
            if(frost!=null)AttachBody(frost,bodyAnchor,frostPoint,new Vector3(.14f,.14f,.06f)*emphasis);
            if(iceMaterial!=null)iceMaterial.color=new Color(.38f,.83f,1f);
            if(markMaterial!=null)markMaterial.color=new Color(1f,.63f,.22f);
        }
        private static void AttachBody(GameObject piece,Transform anchor,Vector3 point,Vector3 worldSize)
        {
            if(piece.transform.parent!=anchor)piece.transform.SetParent(anchor,false);
            piece.transform.localPosition=point;
            // Preserve a legible small world size across goblin, squash and boss scales.
            float x=(anchor.TransformPoint(Vector3.right)-anchor.position).magnitude;
            float y=(anchor.TransformPoint(Vector3.up)-anchor.position).magnitude;
            float z=(anchor.TransformPoint(Vector3.forward)-anchor.position).magnitude;
            piece.transform.localScale=new Vector3(worldSize.x/Mathf.Max(.01f,x),worldSize.y/Mathf.Max(.01f,y),worldSize.z/Mathf.Max(.01f,z));
        }
        private void EnsureMaterials()
        {
            if(iceMaterial!=null)return;
            // Opaque lit material respects the scene depth buffer and never draws through walls.
            iceMaterial=new Material(Shader.Find("Standard"));markMaterial=new Material(Shader.Find("Standard"));
        }
        private GameObject Piece(string name,Material material,Vector3 at,Vector3 scale,Vector3 rotation)
        {
            var piece=ProceduralVisuals.Create(name,PrimitiveType.Cube,material);
            piece.GetComponent<MeshRenderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            piece.transform.SetParent(transform,false);piece.transform.localPosition=at;
            piece.transform.localScale=scale;piece.transform.localRotation=Quaternion.Euler(rotation.x,rotation.y,rotation.z);
            return piece;
        }
        private static void Set(GameObject obj,bool enabled){if(obj!=null&&obj.activeSelf!=enabled)obj.SetActive(enabled);}
        private void OnDisable(){foreach(var obj in ice)Set(obj,false);Set(frost,false);foreach(var obj in vulnerability)Set(obj,false);}
        private void OnDestroy(){if(status!=null)status.VisualStateChanged-=Sync;if(iceMaterial!=null)Destroy(iceMaterial);if(markMaterial!=null)Destroy(markMaterial);}
    }
}
