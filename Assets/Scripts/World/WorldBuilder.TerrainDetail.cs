using System.Collections.Generic;
using UnityEngine;
namespace Emberfall
{
    public static partial class WorldBuilder
    {
        static void BuildMeadowDetail(Transform parent,WorldResources resources)
        {
            var vertices=new List<Vector3>();var triangles=new List<int>();var random=new System.Random(83723);
            for(int i=0;i<240;i++)
            {
                Vector3 p=new Vector3((float)random.NextDouble()*40-20,.035f,(float)random.NextDouble()*38-19);
                if(Mathf.Abs(p.x)<4||p.z>6&&p.x>-7||!WorldTraversal.IsWalkable(p,.3f))continue;
                for(int blade=0;blade<4;blade++)
                {
                    float a=blade*2.4f+i;Vector3 side=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*.075f;
                    Vector3 tip=p+new Vector3(.07f,.22f+(i%3)*.06f,.04f);
                    int n=vertices.Count;vertices.Add(p-side);vertices.Add(tip);vertices.Add(p+side);
                    triangles.Add(n);triangles.Add(n+1);triangles.Add(n+2);
                }
            }
            Material grass=resources.Material(new Color(.27f,.40f,.24f),false,VisualSurface.Foliage);
            if(grass.HasProperty("_Wind")){grass.SetFloat("_Wind",1f);grass.SetFloat("_Cull",0);}
            Geometry(parent,resources,"Breezy meadow grass tufts",vertices,triangles,grass);
        }
        static void ApplyMeadowTexture(Material material)
        {
            if(!SurfaceTextureLibrary.Enabled)return;
            var texture=Resources.Load<Texture2D>("WorldArt/GroundMeadow");
            if(texture==null||!material.HasProperty("_ColorTexture"))return;
            material.mainTexture=texture;material.SetFloat("_ColorTexture",.85f);material.SetFloat("_GrainScale",.4f);
        }
        static void SubdivideGround(ref List<Vector3> vertices,ref List<int> triangles)
        {
            var result=new List<Vector3>();var indices=new List<int>();
            for(int i=0;i<triangles.Count;i+=3)Split(vertices[triangles[i]],vertices[triangles[i+1]],vertices[triangles[i+2]],0,result,indices);
            vertices=result;triangles=indices;
        }
        static void Split(Vector3 a,Vector3 b,Vector3 c,int depth,List<Vector3> vertices,List<int> triangles)
        {
            float ab=(a-b).sqrMagnitude,bc=(b-c).sqrMagnitude,ca=(c-a).sqrMagnitude;
            if(depth>=15||Mathf.Max(ab,Mathf.Max(bc,ca))<=.81f)
            {int n=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(c);triangles.Add(n);triangles.Add(n+1);triangles.Add(n+2);return;}
            if(ab>=bc&&ab>=ca){Vector3 m=(a+b)*.5f;Split(a,m,c,depth+1,vertices,triangles);Split(m,b,c,depth+1,vertices,triangles);}
            else if(bc>=ca){Vector3 m=(b+c)*.5f;Split(a,b,m,depth+1,vertices,triangles);Split(a,m,c,depth+1,vertices,triangles);}
            else {Vector3 m=(c+a)*.5f;Split(a,b,m,depth+1,vertices,triangles);Split(m,b,c,depth+1,vertices,triangles);}
        }
    }
}
