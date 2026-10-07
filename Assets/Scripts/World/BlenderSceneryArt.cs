using UnityEngine;
namespace Emberfall
{
    // Scenery-only rollout. Does not opt in the experimental hero or touch game rules.
    // Set false before world construction for the original procedural art fallback.
    public static class BlenderSceneryArt
    {
        public static bool Enabled=true;
        public static GameObject CreatePilotProp(string name,Transform parent,Vector3 position)
        {
            if(!Enabled)return null;
            GameObject source=Resources.Load<GameObject>("BlenderPilot/"+name);
            if(!HasSafeGeometry(source))return null;
            GameObject instance=Object.Instantiate(source,parent,false);
            instance.transform.localPosition=position;
            if(!BlenderPilotArt.ApplyMaterial(instance))return Reject(instance);
            return instance;
        }
        public static GameObject Create(string name,Transform parent,Vector3 position,WorldResources resources)
        {
            if(!Enabled||resources==null)return null;
            GameObject source=Resources.Load<GameObject>("BlenderScenery/"+name);
            if(!HasSafeGeometry(source))return null;
            GameObject instance=Object.Instantiate(source,parent,false);
            // Scenery meshes are authored in metres; FBX's root unit conversion
            // may otherwise multiply the pot/rubble dimensions by 100.
            instance.transform.localScale=Vector3.one;
            instance.transform.localPosition=position;
            foreach(MeshRenderer renderer in instance.GetComponentsInChildren<MeshRenderer>(true))
            {
                if(!renderer.enabled||!LocallyActive(renderer.transform,instance.transform))continue;
                Material[] slots=renderer.sharedMaterials;
                for(int i=0;i<slots.Length;i++)
                {
                    if(slots[i]==null)return Reject(instance);
                    string key=slots[i].name;
                    if(key=="Clay")slots[i]=resources.Material(new Color(.49f,.27f,.18f),false,VisualSurface.Stone);
                    else if(key=="Ochre")slots[i]=resources.Material(new Color(.64f,.43f,.22f),false,VisualSurface.Stone);
                    else if(key=="Stone")slots[i]=resources.Material(new Color(.35f,.4f,.43f),false,VisualSurface.Stone);
                    else if(key=="Bark")slots[i]=resources.Material(new Color(.27f,.20f,.14f),false,VisualSurface.Wood);
                    else if(key=="Leaf")slots[i]=resources.Material(new Color(.18f,.36f,.26f),false,VisualSurface.Foliage);
                    else return Reject(instance);
                    if(slots[i]==null||slots[i].shader==null||!slots[i].shader.isSupported)return Reject(instance);
                }
                renderer.sharedMaterials=slots;
            }
            return instance;
        }
        // Inspect resource-local activation, not isVisible/activeInHierarchy on a prefab asset.
        // Inactive optional children are allowed, but every enabled visible mesh must draw.
        private static bool LocallyActive(Transform item,Transform root)
        {
            for(var current=item;current!=null;current=current.parent)
            {
                if(!current.gameObject.activeSelf)return false;
                if(current==root)return true;
            }
            return false;
        }
        private static bool HasSafeGeometry(GameObject source)
        {
            if(source==null||!source.activeSelf||source.GetComponentsInChildren<Collider>(true).Length!=0)return false;
            int drawable=0;
            foreach(MeshRenderer renderer in source.GetComponentsInChildren<MeshRenderer>(true))
            {
                if(!renderer.enabled||!LocallyActive(renderer.transform,source.transform))continue;
                var filter=renderer.GetComponent<MeshFilter>();
                Mesh mesh=filter==null?null:filter.sharedMesh;
                if(mesh==null||mesh.vertexCount<3||mesh.subMeshCount<1)return false;
                Material[] slots=renderer.sharedMaterials;
                if(slots==null||slots.Length<mesh.subMeshCount)return false;
                foreach(var slot in slots)if(slot==null)return false;
                for(int sub=0;sub<mesh.subMeshCount;sub++)
                {
                    // Metadata APIs also work when FBX CPU Read/Write is disabled.
                    uint indices=mesh.GetIndexCount(sub);
                    var descriptor=mesh.GetSubMesh(sub);
                    if(mesh.GetTopology(sub)!=MeshTopology.Triangles||indices<3||indices%3!=0||
                        descriptor.firstVertex<0||descriptor.vertexCount<3||
                        (long)descriptor.firstVertex+descriptor.vertexCount>mesh.vertexCount)return false;
                }
                drawable++;
            }
            // Reject detached visible filters too; an inactive optional placeholder is harmless.
            foreach(var filter in source.GetComponentsInChildren<MeshFilter>(true))
                if(LocallyActive(filter.transform,source.transform)&&filter.GetComponent<MeshRenderer>()==null)return false;
            return drawable>0;
        }
        private static GameObject Reject(GameObject instance)
        { instance.SetActive(false);Object.Destroy(instance);return null; }
    }
}
