using System.Collections.Generic;
using UnityEngine;
namespace Emberfall
{
    public static partial class WorldBuilder
    {
        static void BuildMeadowDetail(Transform parent,WorldResources resources)
        {
            var vertices=new List<Vector3>();var triangles=new List<int>();var random=new System.Random(83723);
            for(int i=0;i<520;i++)
            {
                Vector3 p=new Vector3((float)random.NextDouble()*62-31,.035f,(float)random.NextDouble()*62-31);
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
            if(material.HasProperty("_AtlasRegion"))material.SetVector("_AtlasRegion",new Vector4(1,1,0,0));
        }
        private sealed class GroundSubdivision { public List<Vector3> Vertices; public List<int> Triangles; }
        private static readonly Dictionary<string,GroundSubdivision> groundSubdivisions=new Dictionary<string,GroundSubdivision>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetGroundSubdivisions(){groundSubdivisions.Clear();}
        static void SubdivideGround(ref List<Vector3> vertices,ref List<int> triangles)
        {
            // Keep CPU geometry only; each world still owns and releases its GPU mesh.
            var keyBuilder=new System.Text.StringBuilder();
            foreach(var v in vertices)
            {keyBuilder.Append(v.x.ToString("R",System.Globalization.CultureInfo.InvariantCulture)).Append(',').Append(v.y.ToString("R",System.Globalization.CultureInfo.InvariantCulture)).Append(',').Append(v.z.ToString("R",System.Globalization.CultureInfo.InvariantCulture)).Append(';');}
            foreach(var index in triangles)keyBuilder.Append(index).Append(',');
            string key=keyBuilder.ToString();GroundSubdivision cached;
            if(groundSubdivisions.TryGetValue(key,out cached)){vertices=cached.Vertices;triangles=cached.Triangles;return;}
            var result=new List<Vector3>();var indices=new List<int>();
            for(int i=0;i<triangles.Count;i+=3)Split(vertices[triangles[i]],vertices[triangles[i+1]],vertices[triangles[i+2]],0,result,indices);
            if(groundSubdivisions.Count>=96)groundSubdivisions.Clear();
            groundSubdivisions.Add(key,new GroundSubdivision{Vertices=result,Triangles=indices});
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
