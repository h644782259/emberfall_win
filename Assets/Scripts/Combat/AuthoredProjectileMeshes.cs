using System.Collections.Generic;
using UnityEngine;
namespace Emberfall
{
    // Decorative identity only: never infer damage element from a tint.
    internal static class AuthoredProjectileMeshes
    {
        internal static bool Enabled=true;
        private static readonly Dictionary<string,Mesh> cache=new Dictionary<string,Mesh>();
        internal static string FriendlyKey(bool arrow,HeroClass hero,bool companion)
        {return arrow?"ArrowBody":companion||hero==HeroClass.Summoner?"ContractBolt":"CasterBolt";}
        internal static Mesh Load(string key)
        {
            if(!Enabled)return null;
            Mesh mesh;if(cache.TryGetValue(key,out mesh))return mesh;
            var data=Resources.Load<TextAsset>("BlenderProjectiles/"+key);
            mesh=data==null?null:AuthoredActorMeshes.Decode(data.bytes,"Authored projectile / "+key);
            cache[key]=mesh;return mesh;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {foreach(var mesh in cache.Values)if(mesh!=null)Object.Destroy(mesh);cache.Clear();Enabled=true;}
    }

    // The area owns the ornament; its lease can retire only this child, never gameplay.
    internal sealed class AuthoredTrapVisual:MonoBehaviour
    {
        private Mesh clipped;
        private Material surface;
        internal static GameObject Create(Transform parent,Color tint,float radius)
        {
            var source=AuthoredProjectileMeshes.Load("TrapCore");if(source==null)return null;
            var obj=new GameObject("Authored trap mechanism");obj.transform.SetParent(parent,false);
            var visual=obj.AddComponent<AuthoredTrapVisual>();
            if(CombatVisualLease.Attach(obj,CombatVisualPriority.Decoration)==null)return null;
            float size=Mathf.Min(.9f,radius*.26f);
            visual.clipped=AnchoredImpactMesh.Create(source,parent,Vector3.up*.06f,Vector3.one*size,Quaternion.identity);
            // Keep this small optional ornament within its original per-instance cap after cover clipping.
            if(visual.clipped.triangles.Length>128*3){Object.Destroy(obj);return null;}
            visual.surface=new Material(Shader.Find("Standard")){color=Color.Lerp(tint,new Color(.65f,.8f,.72f),.35f)};
            visual.surface.EnableKeyword("_EMISSION");visual.surface.SetColor("_EmissionColor",tint*.32f);
            ProceduralVisuals.ApplySurface(visual.surface,VisualSurface.Metal);
            obj.AddComponent<MeshFilter>().sharedMesh=visual.clipped;
            obj.AddComponent<MeshRenderer>().sharedMaterial=visual.surface;
            return obj;
        }
        private void OnDestroy(){if(clipped!=null)Object.Destroy(clipped);if(surface!=null)Object.Destroy(surface);}
    }
}
