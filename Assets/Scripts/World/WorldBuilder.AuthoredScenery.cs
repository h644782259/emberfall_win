using UnityEngine;
namespace Emberfall
{
    public static partial class WorldBuilder
    {
        // Three stable profiles of the same authored, open-canopy tree. Existing trunk
        // traversal registration stays in Tree; limbs and leaves are visual-only.
        private static void BuildBranchTree(Transform parent, WorldResources r, Vector3 p, float size, int seed)
        {
            GameObject authored=BlenderSceneryArt.Create("OpenCanopyTree",parent,p,r);
            if(authored!=null)
            {
                authored.transform.localScale=Vector3.one*size;
                authored.transform.localRotation=Quaternion.Euler(0,seed*31f,0);
                CameraOcclusionSurface.MarkHierarchy(authored);
                return;
            }
            int variant=((seed%3)+3)%3;
            Transform root=Region(parent,"Branching open canopy tree");root.localPosition=p;
            root.localScale=Vector3.one*size;root.localRotation=Quaternion.Euler(0,seed*31f,0);
            Material bark=r.Material(new Color(.27f,.20f,.14f),false,VisualSurface.Wood);
            Material leaf=r.Material(new Color(.15f+variant*.025f,.33f,.24f),false,VisualSurface.Foliage);
            if(leaf.HasProperty("_Wind"))leaf.SetFloat("_Wind",1f);
            TreeLimb(root,"Tree bole",Vector3.zero,new Vector3(.08f,2.65f,0),.17f,bark);
            for(int i=0;i<5;i++)
            {
                float angle=(i*137+variant*19)*Mathf.Deg2Rad;
                Vector3 radial=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                float level=1.65f+i*.30f;
                Vector3 start=new Vector3(.05f,level-.55f,0),fork=radial*(.66f+(i%2)*.12f)+Vector3.up*level;
                TreeLimb(root,"Ascending branch",start,fork,.06f,bark);
                TreeLimb(root,"Exposed branch fork",fork, fork+radial*.25f+Vector3.up*.34f,.033f,bark);
                var go=new GameObject("Open canopy leaf fan");go.transform.SetParent(root,false);
                go.transform.localPosition=fork+Vector3.up*.25f;
                go.transform.localRotation=Quaternion.Euler(0,-i*137,0);
                Mesh mesh=LeafFan(variant,i);r.Own(mesh);
                go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=leaf;
            }
            CameraOcclusionSurface.MarkHierarchy(root.gameObject);
        }
        private static void TreeLimb(Transform root,string name,Vector3 a,Vector3 b,float radius,Material material)
        {
            Vector3 direction=b-a;
            GameObject limb=Primitive(root,name,PrimitiveType.Cylinder,(a+b)*.5f,new Vector3(radius*2,direction.magnitude*.5f,radius*2),material);
            limb.transform.localRotation=Quaternion.FromToRotation(Vector3.up,direction.normalized);
        }
        private static Mesh LeafFan(int variant,int layer)
        {
            // Broad scalloped leaf mass, shallow convex upper and lower faces. Open
            // spacing between five fans exposes the fork structure, unlike rock crowns.
            const int n=12;Vector3[] v=new Vector3[n+2];Vector2[] uv=new Vector2[n+2];int[] t=new int[n*6];
            v[0]=new Vector3(0,.24f,0);v[n+1]=new Vector3(0,-.09f,0);
            uv[0]=uv[n+1]=new Vector2(.5f,.5f);
            for(int i=0;i<n;i++)
            {
                float a=i*Mathf.PI*2/n,radius=(i%2==0?1:.78f)*(1-layer*.055f);
                v[i+1]=new Vector3(Mathf.Cos(a)*radius*(.79f+variant*.06f),0,Mathf.Sin(a)*radius*.60f);
                uv[i+1]=new Vector2(v[i+1].x*.5f+.5f,v[i+1].z*.5f+.5f);
                int next=(i+1)%n+1,j=i*6;t[j]=0;t[j+1]=next;t[j+2]=i+1;t[j+3]=n+1;t[j+4]=i+1;t[j+5]=next;
            }
            Mesh mesh=new Mesh{name="Authored scalloped leaf fan",vertices=v,uv=uv,triangles=t};mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        private static void BuildWorkshopRoof(Transform root,WorldResources r,Vector3 p,int side,Material roof,Material stone)
        {
            // Eaves overhang visually; the existing 5.6m building footprint remains authoritative.
            for(int slope=-1;slope<=1;slope+=2)
            {
                var panel=Primitive(root,"Pitched workshop roof module",PrimitiveType.Cube,p+new Vector3(slope*1.52f,4.26f,0),new Vector3(3.4f,.18f,6.2f),roof,cameraOccluder:true);
                panel.transform.localRotation=Quaternion.Euler(0,0,-slope*22);
                Primitive(root,"Workshop projecting eave",PrimitiveType.Cube,p+new Vector3(slope*3.10f,3.64f,0),new Vector3(.16f,.24f,6.28f),roof,cameraOccluder:true);
            }
            Primitive(root,"Workshop ridge cap",PrimitiveType.Cube,p+Vector3.up*4.94f,new Vector3(.23f,.20f,6.3f),roof,cameraOccluder:true);
            Vector3 chimney=p+new Vector3(side*1.5f,4.3f,1.2f);
            Primitive(root,"Kiln chimney",PrimitiveType.Cube,chimney,new Vector3(.8f,2,.8f),stone,cameraOccluder:true);
            Primitive(root,"Kiln chimney cap",PrimitiveType.Cube,chimney+Vector3.up*1.02f,new Vector3(1.02f,.17f,1.02f),roof,cameraOccluder:true);
        }
    }
}
