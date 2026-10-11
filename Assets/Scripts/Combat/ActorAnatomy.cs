using System.Collections.Generic;
using UnityEngine;
namespace Emberfall
{
    // Continuous cloth is skinned to the existing gameplay rig. Shared anatomical
    // meshes change presentation only; collision and weapon sockets stay authoritative.
    internal static class ActorAnatomy
    {
        internal static bool Enabled=true;
        private static readonly Dictionary<string,Mesh> meshes=new Dictionary<string,Mesh>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset(){foreach(var mesh in meshes.Values)if(mesh!=null)Object.Destroy(mesh);meshes.Clear();Enabled=true;}
        static Vector4 Ring(float y,float x,float z,float shift=0){return new Vector4(y,x,z,shift);}
        internal static void Apply(GameObject obj,string name,bool treant)
        {
            if(!Enabled||treant)return;
            string key=null;Vector4[] rings=null;
            switch(name)
            {
                case "Breastplate":key="torso";rings=new[]{Ring(1,.24f,.27f),Ring(.8f,.43f,.42f),Ring(.45f,.5f,.49f),Ring(.1f,.45f,.46f),Ring(-.45f,.34f,.37f),Ring(-.8f,.35f,.36f),Ring(-1,.39f,.38f)};break;
                case "Pauldrons":case "Shoulder shell":case "Guardian layered pauldron":key="shoulder";rings=new[]{Ring(.5f,.06f,.06f),Ring(.38f,.29f,.31f),Ring(.1f,.47f,.46f),Ring(-.20f,.5f,.49f),Ring(-.40f,.46f,.44f),Ring(-.5f,.37f,.36f)};break;
                case "Helmet":case "Leather Cap":key="helmet";rings=new[]{Ring(.5f,.03f,.03f),Ring(.4f,.24f,.25f),Ring(.16f,.40f,.41f),Ring(-.12f,.48f,.48f),Ring(-.5f,.49f,.49f)};break;
                case "Guardian chest plate":key="breast-shell";rings=new[]{Ring(.5f,.31f,.33f),Ring(.32f,.47f,.47f),Ring(-.08f,.5f,.5f),Ring(-.3f,.43f,.46f),Ring(-.5f,.31f,.34f)};break;
                case "Slime Body":key="gel-body";rings=new[]{Ring(.5f,.025f,.025f),Ring(.39f,.23f,.21f),Ring(.18f,.39f,.35f),Ring(-.10f,.49f,.45f),Ring(-.33f,.5f,.48f),Ring(-.46f,.45f,.43f),Ring(-.5f,.28f,.27f)};break;
                case "Spirit Core":key="spirit";rings=new[]{Ring(.5f,.005f,.005f,-.07f),Ring(.26f,.24f,.27f,-.03f),Ring(-.04f,.46f,.45f),Ring(-.24f,.35f,.35f),Ring(-.5f,.015f,.015f,.04f)};break;
                case "Arcane Crystal":case "Focus crystal":case "Spirit Crown":key="cut-gem";rings=new[]{Ring(.5f,.005f,.005f),Ring(.2f,.48f,.48f),Ring(-.15f,.4f,.4f),Ring(-.5f,.02f,.02f)};break;
                case "Head":key="face";rings=new[]{Ring(.5f,.03f,.03f),Ring(.43f,.31f,.33f),Ring(.27f,.45f,.46f),Ring(.08f,.49f,.48f),Ring(-.13f,.43f,.44f,.025f),Ring(-.32f,.31f,.36f,.035f),Ring(-.46f,.15f,.20f,.02f),Ring(-.5f,.02f,.04f)};break;
                case "Glove":key="hand";rings=new[]{Ring(.5f,.27f,.30f),Ring(.30f,.41f,.38f),Ring(-.08f,.48f,.38f),Ring(-.32f,.42f,.30f),Ring(-.5f,.27f,.21f)};break;
                case "Boot":key="boot";rings=new[]{Ring(.5f,.33f,.27f,-.16f),Ring(.18f,.35f,.32f,-.12f),Ring(-.12f,.46f,.47f),Ring(-.35f,.5f,.5f),Ring(-.5f,.49f,.49f)};break;
                case "Forearm Bracer":key="bracer";rings=new[]{Ring(1,.48f,.46f),Ring(.6f,.5f,.5f),Ring(-.25f,.43f,.44f),Ring(-1,.34f,.37f)};break;
                case "Knee Guard":key="knee";rings=new[]{Ring(.5f,.22f,.18f),Ring(.29f,.45f,.40f),Ring(0,.5f,.5f,.03f),Ring(-.28f,.35f,.34f),Ring(-.5f,.11f,.14f)};break;
                case "Cuirass":key="cuirass";rings=new[]{Ring(.5f,.31f,.25f),Ring(.28f,.5f,.46f),Ring(-.08f,.46f,.5f),Ring(-.5f,.34f,.35f)};break;
                case "Overlapping Armor":key="lamella";rings=new[]{Ring(.5f,.43f,.38f),Ring(.1f,.49f,.47f),Ring(-.28f,.5f,.5f),Ring(-.5f,.45f,.41f)};break;
            }
            if(key==null)return;Mesh mesh;
            if(!meshes.TryGetValue(key,out mesh)){mesh=key=="hand"?Hand(rings):Loft(key,key=="cut-gem"?rings:SmoothProfile(rings),key=="cut-gem"?8:24);meshes.Add(key,mesh);}
            obj.GetComponent<MeshFilter>().sharedMesh=mesh;
        }
        static Vector4[] SmoothProfile(Vector4[] source)
        {
            var result=new List<Vector4>();
            for(int r=0;r<source.Length-1;r++)for(int i=0;i<3;i++)
            {
                float t=i/3f;Vector4 a=source[Mathf.Max(0,r-1)],b=source[r],c=source[r+1],d=source[Mathf.Min(source.Length-1,r+2)];
                Vector4 v=.5f*((2*b)+(-a+c)*t+(2*a-5*b+4*c-d)*t*t+(-a+3*b-3*c+d)*t*t*t);
                v.x=Mathf.Lerp(b.x,c.x,t);v.y=Mathf.Clamp(v.y,.002f,.5f);v.z=Mathf.Clamp(v.z,.002f,.5f);result.Add(v);
            }
            result.Add(source[source.Length-1]);return result.ToArray();
        }
        static Mesh Hand(Vector4[] palmRings)
        {
            Mesh palm=Loft("palm",palmRings,16);
            Mesh finger=Loft("finger",new[]{Ring(.12f,.075f,.10f),Ring(0,.085f,.105f),Ring(-.16f,.067f,.08f),Ring(-.24f,.025f,.04f)},10);
            var parts=new List<CombineInstance>{new CombineInstance{mesh=palm,transform=Matrix4x4.identity}};
            for(int i=0;i<4;i++)parts.Add(new CombineInstance{mesh=finger,transform=Matrix4x4.TRS(new Vector3(-.27f+i*.18f,-.39f+(i==0||i==3?.055f:0),.03f),Quaternion.Euler(-18,0,(i-1.5f)*-4),Vector3.one)});
            parts.Add(new CombineInstance{mesh=finger,transform=Matrix4x4.TRS(new Vector3(.43f,.03f,.12f),Quaternion.Euler(-32,0,47),new Vector3(1.2f,1.15f,1.1f))});
            var mesh=new Mesh{name="Anatomical hand and fingers"};mesh.CombineMeshes(parts.ToArray(),true,true);mesh.RecalculateBounds();Object.Destroy(palm);Object.Destroy(finger);return mesh;
        }
        static Mesh Loft(string name,Vector4[] rings,int sides)
        {
            int width=sides+1;var vertices=new Vector3[rings.Length*width];var uv=new Vector2[vertices.Length];var indices=new List<int>();
            for(int r=0;r<rings.Length;r++)for(int c=0;c<=sides;c++)
            {
                float a=c*Mathf.PI*2/sides;Vector4 ring=rings[r];float x=Mathf.Cos(a)*ring.y,z=Mathf.Sin(a)*ring.z+ring.w;
                if(name=="face"&&Mathf.Sin(a)>0)
                {
                    // Brow ridge, cheek plane and a gentle muzzle make a face, not a ball.
                    float brow=Mathf.Exp(-Mathf.Pow((ring.x-.08f)*10,2))*Mathf.Exp(-Mathf.Pow((Mathf.Abs(x)-.23f)*6,2));
                    z+=brow*.035f+Mathf.Exp(-x*x*90-Mathf.Pow((ring.x+.06f)*9,2))*.045f;
                }
                if(name=="torso"||name=="sleeve"||name=="trousers")
                {float folds=Mathf.Sin(a*7+ring.x*5)*.009f*Mathf.Clamp01(1-Mathf.Abs(ring.x));x+=Mathf.Cos(a)*folds;z+=Mathf.Sin(a)*folds;}
                int i=r*width+c;vertices[i]=new Vector3(x,ring.x,z);uv[i]=new Vector2(c/(float)sides,r/(float)(rings.Length-1));
                if(r<rings.Length-1&&c<sides){int b=i+width;indices.Add(i);indices.Add(i+1);indices.Add(b);indices.Add(i+1);indices.Add(b+1);indices.Add(b);}
            }
            for(int c=1;c<sides-1;c++){indices.Add(0);indices.Add(c+1);indices.Add(c);int b=(rings.Length-1)*width;indices.Add(b);indices.Add(b+c);indices.Add(b+c+1);}
            var mesh=new Mesh{name="Anatomical "+name,vertices=vertices,uv=uv,triangles=indices.ToArray()};mesh.RecalculateNormals();
            var normals=mesh.normals;for(int r=0;r<rings.Length;r++){int a=r*width,b=a+sides;normals[a]=normals[b]=(normals[a]+normals[b]).normalized;}mesh.normals=normals;mesh.RecalculateTangents();mesh.RecalculateBounds();return mesh;
        }
        internal static void Sleeve(Transform shoulder,Transform elbow)
        { SkinLimb(shoulder,elbow,"sleeve",.30f,.56f,shoulder.Find("Sleeve")); }
        internal static void Trousers(Transform hip,Transform knee)
        { SkinLimb(hip,knee,"trousers",.37f,.68f,hip.Find("Trouser")); }
        static void SkinLimb(Transform upper,Transform lower,string key,float joint,float length,Transform original)
        {
            if(!Enabled||original==null)return;
            var source=original.GetComponent<Renderer>();if(source==null)return;
            Mesh mesh;
            if(!meshes.TryGetValue(key,out mesh))
            {
                const int rows=17;var rings=new Vector4[rows];
                for(int r=0;r<rows;r++)
                {
                    float t=r/(float)(rows-1),y=-length*t;
                    float radius=key=="sleeve"?Mathf.Lerp(.119f,.083f,t)+Mathf.Sin(t*Mathf.PI)*.009f:Mathf.Lerp(.125f,.077f,t)+Mathf.Sin(t*Mathf.PI)*.014f;
                    rings[r]=Ring(y,radius,radius*(key=="sleeve"?1:1.07f));
                }
                mesh=Loft(key,rings,16);var weights=new BoneWeight[mesh.vertexCount];var v=mesh.vertices;
                for(int i=0;i<weights.Length;i++){float blend=Mathf.SmoothStep(0,1,Mathf.InverseLerp(joint-.075f,joint+.075f,-v[i].y));weights[i]=new BoneWeight{boneIndex0=0,boneIndex1=1,weight0=1-blend,weight1=blend};}
                mesh.boneWeights=weights;mesh.bindposes=new[]{Matrix4x4.identity,Matrix4x4.Translate(new Vector3(0,joint,0))};meshes.Add(key,mesh);
            }
            var obj=new GameObject("Continuous articulated "+key);obj.transform.SetParent(upper,false);
            var renderer=obj.AddComponent<SkinnedMeshRenderer>();renderer.sharedMesh=mesh;renderer.sharedMaterial=source.sharedMaterial;renderer.bones=new[]{upper,lower};renderer.rootBone=upper;renderer.quality=SkinQuality.Bone2;renderer.localBounds=new Bounds(Vector3.zero,Vector3.one*1.6f);source.enabled=false;
        }
    }
}
