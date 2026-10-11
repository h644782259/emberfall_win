using System.Collections.Generic;
using UnityEngine;
namespace Emberfall
{
    public static partial class WorldBuilder
    {
        static readonly Dictionary<int,Mesh[]> naturalTrees=new Dictionary<int,Mesh[]>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetNaturalTrees(){foreach(var pair in naturalTrees)foreach(var mesh in pair.Value)if(mesh!=null)Object.Destroy(mesh);naturalTrees.Clear();}
        static bool BuildNaturalTree(Transform parent,WorldResources r,Vector3 p,float size,int seed)
        {
            if(!NaturalWorldMaterial.Enabled||!SurfaceTextureLibrary.Enabled)return false;
            int variant=(seed%3+3)%3;Mesh[] meshes;
            if(!naturalTrees.TryGetValue(variant,out meshes))
            {
                var bark=new DetailBatch();var leaf=new DetailBatch();var random=new System.Random(18943+variant);
                bark.Branch(Vector3.zero,new Vector3(.06f,1.6f,0),.18f,.12f);
                bark.Branch(new Vector3(.06f,1.6f,0),new Vector3(.10f,2.9f,.05f),.12f,.035f);
                for(int i=0;i<5;i++)
                {
                    float a=i*2.39996f+variant*.3f;Vector3 direction=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
                    float height=1.65f+i*.28f;Vector3 fork=direction*(.72f+i%2*.12f)+Vector3.up*height;
                    bark.Branch(new Vector3(.06f,height-.55f,0),fork,.065f,.025f);
                    for(int branch=0;branch<3;branch++)
                    {
                        float turn=a+(branch-1)*.8f;Vector3 spread=new Vector3(Mathf.Cos(turn),0,Mathf.Sin(turn));
                        Vector3 tip=fork+spread*.46f+Vector3.up*.26f;bark.Branch(fork,tip,.023f,.009f);
                        for(int j=0;j<26;j++)
                        {
                            Vector3 at=tip+new Vector3(Next(random,-.42f,.42f),Next(random,-.12f,.20f),Next(random,-.34f,.34f));
                            float shade=Next(random,.80f,1.15f);
                            leaf.Leaf(at,Next(random,0,6.28f),Next(random,.24f,.40f),new Color(shade,shade,shade,.8f));
                        }
                    }
                    Vector3 rootTip=direction*.56f;bark.Branch(rootTip+Vector3.up*.025f,Vector3.up*.32f,.045f,.10f);
                }
                meshes=new[]{bark.Mesh("Shared tapered tree wood"),leaf.Mesh("Shared individual canopy leaves")};naturalTrees.Add(variant,meshes);
            }
            Transform root=Region(parent,"Natural branching woodland tree");root.localPosition=p;root.localScale=Vector3.one*size;root.localRotation=Quaternion.Euler(0,seed*31,0);
            for(int i=0;i<2;i++)
            {
                var go=new GameObject(i==0?"Tapered trunk roots and twigs":"Open individual leaf canopy");go.transform.SetParent(root,false);
                go.AddComponent<MeshFilter>().sharedMesh=meshes[i];var renderer=go.AddComponent<MeshRenderer>();
                renderer.sharedMaterial=r.Material(i==0?new Color(.34f,.25f,.17f):new Color(.21f,.38f,.23f),false,i==0?VisualSurface.Wood:VisualSurface.Foliage);
                if(i==1&&renderer.sharedMaterial.HasProperty("_VertexTint")){renderer.sharedMaterial.SetFloat("_VertexTint",1);renderer.sharedMaterial.SetFloat("_Wind",1);renderer.sharedMaterial.SetFloat("_Cull",0);}
            }
            CameraOcclusionSurface.MarkHierarchy(root.gameObject);return true;
        }
        // Fixed seeded decoration, merged into three renderers. Traversal remains authored.
        static void BuildNaturalGroundDetail(Transform parent,WorldResources r,bool dungeon,int hub)
        {
            if(!NaturalWorldMaterial.Enabled)return;
            var fern=new DetailBatch();var litter=new DetailBatch();var gravel=new DetailBatch();
            var random=new System.Random(59117+(dungeon?97:hub*31));
            int count=dungeon?300:hub==0?850:360;float extent=WorldTraversal.ArenaRadius-1;
            for(int i=0;i<count;i++)
            {
                Vector3 p=new Vector3(Next(random,-extent,extent),.055f,Next(random,-extent,extent));
                if(!WorldTraversal.IsWalkable(p,.35f))continue;
                // Main approaches, shop fronts, combat center and bridge stay easy to read.
                if(Mathf.Abs(p.x)<3.3f||(!dungeon&&p.z<-8&&Mathf.Abs(p.x)<9))continue;
                if(dungeon&&p.sqrMagnitude<85)continue;
                float size=Next(random,.055f,.15f);
                gravel.Pebble(p,new Vector3(size,size*.42f,size*.72f),Next(random,0,6.28f));
                if(!dungeon&&hub==0)
                {
                    litter.Leaf(p+new Vector3(.2f,.005f,0),Next(random,0,6.28f),Next(random,.07f,.16f),new Color(.74f,.66f,.47f,0));
                    if(p.x<-7&&i%3==0)
                    {
                        for(int frond=0;frond<5;frond++)
                        {
                            float a=frond*1.2566f+i;Vector3 direction=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
                            for(int leaf=1;leaf<=5;leaf++)
                            {
                                float t=leaf/6f;Vector3 at=p+direction*(t*.46f)+Vector3.up*(Mathf.Sin(t*Mathf.PI)*.32f);
                                fern.Leaf(at,a+.65f,.13f*(1-t*.55f),new Color(.72f,.90f,.66f,t));
                                fern.Leaf(at,a-.65f,.13f*(1-t*.55f),new Color(.72f,.90f,.66f,t));
                            }
                        }
                    }
                }
            }
            gravel.Build(parent,r,"Merged natural gravel",r.Material(new Color(.46f,.44f,.38f),false,VisualSurface.Stone),false);
            Material foliage=r.Material(new Color(.27f,.39f,.22f),false,VisualSurface.Foliage);
            if(foliage.HasProperty("_VertexTint")){foliage.SetFloat("_VertexTint",1);foliage.SetFloat("_Wind",1);foliage.SetFloat("_Cull",0);}
            fern.Build(parent,r,"Merged woodland fern fronds",foliage,true);
            Material leaves=r.Material(new Color(.43f,.35f,.20f),false,VisualSurface.Foliage);
            if(leaves.HasProperty("_VertexTint")){leaves.SetFloat("_VertexTint",1);leaves.SetFloat("_Cull",0);}
            litter.Build(parent,r,"Merged forest leaf litter",leaves,true);
        }
        static float Next(System.Random random,float min,float max){return min+(float)random.NextDouble()*(max-min);}
        sealed class DetailBatch
        {
            readonly List<Vector3> vertices=new List<Vector3>();readonly List<int> triangles=new List<int>();readonly List<Color> colors=new List<Color>();
            void Vertex(Vector3 point,Color color){vertices.Add(point);colors.Add(color);}
            void Face(int a,int b,int c){triangles.Add(a);triangles.Add(b);triangles.Add(c);}
            internal void Branch(Vector3 from,Vector3 to,float radius,float tipRadius)
            {
                Vector3 axis=(to-from).normalized;Vector3 tangent=Vector3.Cross(axis,Mathf.Abs(axis.y)>.95f?Vector3.forward:Vector3.up).normalized;Vector3 bitangent=Vector3.Cross(axis,tangent);
                int n=vertices.Count;const int sides=10;
                for(int ring=0;ring<2;ring++)for(int i=0;i<sides;i++)
                {float a=i*Mathf.PI*2/sides;Vertex((ring==0?from:to)+(tangent*Mathf.Cos(a)+bitangent*Mathf.Sin(a))*(ring==0?radius:tipRadius),Color.white);}
                for(int i=0;i<sides;i++){int next=(i+1)%sides;Face(n+i,n+next,n+sides+i);Face(n+next,n+sides+next,n+sides+i);}
            }
            internal Mesh Mesh(string name)
            {
                var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.SetColors(colors);mesh.RecalculateNormals();mesh.RecalculateBounds();
                return mesh;
            }
            internal void Leaf(Vector3 p,float angle,float length,Color color)
            {
                int n=vertices.Count;Vector3 forward=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*length;
                Vector3 side=new Vector3(-forward.z,0,forward.x)*.4f;
                Vertex(p,color);Vertex(p+forward*.45f+side,color);Vertex(p+forward*.5f+Vector3.up*.025f,color);
                Vertex(p+forward*.45f-side,color);Vertex(p+forward,color);
                Face(n,n+1,n+2);Face(n,n+2,n+3);Face(n+1,n+4,n+2);Face(n+2,n+4,n+3);
            }
            internal void Pebble(Vector3 p,Vector3 scale,float angle)
            {
                int n=vertices.Count;Vertex(p+Vector3.up*scale.y,Color.white);Vertex(p-Vector3.up*.02f,Color.white);
                for(int i=0;i<6;i++){float a=angle+i*Mathf.PI/3;Vertex(p+new Vector3(Mathf.Cos(a)*scale.x,0,Mathf.Sin(a)*scale.z),Color.white);}
                for(int i=0;i<6;i++){int a=n+2+i,b=n+2+(i+1)%6;Face(n,b,a);Face(n+1,a,b);}
            }
            internal void Build(Transform parent,WorldResources r,string name,Material material,bool vegetation)
            {
                if(vertices.Count==0)return;
                GameObject go=Geometry(parent,r,name,vertices,triangles,material);Mesh mesh=go.GetComponent<MeshFilter>().sharedMesh;mesh.SetColors(colors);
                if(vegetation)
                {
                    var normals=new Vector3[vertices.Count];for(int i=0;i<normals.Length;i++)normals[i]=Vector3.up;mesh.normals=normals;
                    go.GetComponent<MeshRenderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                }
            }
        }
    }
}
