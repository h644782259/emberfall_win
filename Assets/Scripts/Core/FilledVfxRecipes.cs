using System;
using System.Collections.Generic;

namespace Emberfall
{
    public enum FilledVfxKind { Crescent, Ice, Fire, Summon, Charge, Thrust, Sword, Lightning, Arcane, ArrowRain, Vine }
    public sealed class FilledMeshRecipe
    {
        public readonly float[] Positions, Uv;
        public readonly int[] Triangles;
        public FilledMeshRecipe(float[] positions,float[] uv,int[] triangles)
        {Positions=positions;Uv=uv;Triangles=triangles;}
    }
    public struct FilledVfxFrame
    {
        public readonly float Progress, Expansion, Opacity;
        public FilledVfxFrame(float progress,float expansion,float opacity)
        {Progress=progress;Expansion=expansion;Opacity=opacity;}
    }
    public static class FilledVfxRecipes
    {
        public const int MaximumParts=14, ReducedParts=7, DesktopEffects=20, MobileEffects=12;
        public static FilledVfxFrame Sample(FilledVfxKind kind,float age,float life)
        {
            if(!Finite(age)||!Finite(life)||life<=0||age<0)return new FilledVfxFrame(0,0,0);
            float t=Math.Max(0,Math.Min(1,age/life));
            float expansion=kind==FilledVfxKind.Charge?.55f+t*.45f:.42f+.72f*(1-(float)Math.Pow(1-Math.Min(1,t*2.8f),3));
            float opacity=kind==FilledVfxKind.Charge?Math.Min(1,t*8)*Math.Min(1,(1-t)*8):Math.Min(1,(1-t)*3.5f);
            return new FilledVfxFrame(t,expansion,Math.Max(0,opacity));
        }
        // Reusable closed surface adds a luminous body beneath ribbons and shards.
        public static FilledMeshRecipe EnergyCore()
        {
            const int rings=12,sides=24;int count=(rings-1)*sides+2;
            float[] p=new float[count*3],uv=new float[count*2];int[] triangles=new int[(rings-1)*sides*6];int cursor=0;
            for(int y=1;y<rings;y++)for(int x=0;x<sides;x++)
            {
                float v=y/(float)rings,u=x/(float)sides;double phi=v*Math.PI,theta=u*Math.PI*2;
                float radius=(float)Math.Sin(phi)*(.46f+.04f*(float)Math.Cos(theta*6+phi*2));
                int k=(y-1)*sides+x;p[k*3]=radius*(float)Math.Cos(theta);p[k*3+1]=.5f+.5f*(float)Math.Cos(phi);p[k*3+2]=radius*(float)Math.Sin(theta);uv[k*2]=u;uv[k*2+1]=1-v;
                if(y==rings-1)continue;
                int next=(y-1)*sides+(x+1)%sides,b=k+sides;
                triangles[cursor++]=k;triangles[cursor++]=next;triangles[cursor++]=b;
                triangles[cursor++]=next;triangles[cursor++]=next+sides;triangles[cursor++]=b;
            }
            int top=count-2,bottom=count-1;p[top*3+1]=1;uv[top*2]=uv[bottom*2]=.5f;uv[top*2+1]=1;
            for(int x=0;x<sides;x++)
            {
                triangles[cursor++]=top;triangles[cursor++]=(x+1)%sides;triangles[cursor++]=x;
                triangles[cursor++]=bottom;triangles[cursor++]=(rings-2)*sides+x;triangles[cursor++]=(rings-2)*sides+(x+1)%sides;
            }
            return new FilledMeshRecipe(p,uv,triangles);
        }
        // Closed diamond-section blade, with a broad convex cutting face and tapered tips.
        public static FilledMeshRecipe Crescent(int segments=36)
        {
            segments=Math.Max(12,Math.Min(64,segments));int count=(segments+1)*4;
            float[] vertices=new float[count*3],uv=new float[count*2];int[] triangles=new int[segments*24+12];
            for(int i=0;i<=segments;i++)
            {
                float t=(float)i/segments,a=(-108+216*t)*(float)Math.PI/180;
                float taper=.015f+(float)Math.Pow(Math.Sin(t*Math.PI),.7);
                for(int j=0;j<4;j++)
                {
                    float r=j==0?1.04f:j==2?1-.37f*taper:1-.15f*taper;
                    float y=(j==1?.09f:j==3?-.075f:0)*taper+.055f*(float)Math.Sin(a);
                    int k=i*4+j;vertices[k*3]=(float)Math.Sin(a)*r;vertices[k*3+1]=y;vertices[k*3+2]=(float)Math.Cos(a)*r;
                    uv[k*2]=t;uv[k*2+1]=j==0?1:j==2?0:.6f;
                    if(i==segments)continue;
                    int p=(i*4+j)*6,n=i*4+(j+1)%4;
                    triangles[p]=k;triangles[p+1]=k+4;triangles[p+2]=n;triangles[p+3]=n;triangles[p+4]=k+4;triangles[p+5]=n+4;
                }
            }
            int end=segments*24,last=segments*4;
            int[] caps={0,1,2,0,2,3,last,last+2,last+1,last,last+3,last+2};Array.Copy(caps,0,triangles,end,caps.Length);
            return new FilledMeshRecipe(vertices,uv,triangles);
        }
        public static FilledMeshRecipe Crystal()
        {
            const int sides=6;float[] p=new float[sides*6*3],uv=new float[sides*6*2];int[] tr=new int[sides*6];
            for(int i=0;i<sides;i++)
            {
                double a=i*Math.PI*2/sides,b=(i+1)*Math.PI*2/sides;
                float ax=(float)Math.Cos(a)*.5f,az=(float)Math.Sin(a)*.5f,bx=(float)Math.Cos(b)*.5f,bz=(float)Math.Sin(b)*.5f;
                float[] face={ax,.16f,az,.12f,1,.07f,bx,.16f,bz,ax,.16f,az,bx,.16f,bz,0,0,0};
                Array.Copy(face,0,p,i*18,18);
                for(int j=0;j<6;j++){int k=i*6+j;tr[k]=k;uv[k*2]=j%3*.5f;uv[k*2+1]=j==1?1:0;}
            }
            return new FilledMeshRecipe(p,uv,tr);
        }
        // Filled, curved flame volume. Its taper is authored rather than a sphere billboard.
        public static FilledMeshRecipe Flame(int rings=8,int sides=12)
        {
            rings=Math.Max(4,Math.Min(12,rings));sides=Math.Max(6,Math.Min(20,sides));
            int surface=(rings+1)*sides;
            float[] p=new float[(surface+2)*3],uv=new float[(surface+2)*2];int[] tr=new int[(rings+1)*sides*6];
            for(int i=0;i<=rings;i++)for(int j=0;j<sides;j++)
            {
                float t=(float)i/rings,a=(float)(j*Math.PI*2/sides),r=.006f+.46f*(float)Math.Pow(Math.Sin(t*Math.PI),.65)*(1-.5f*t);
                int k=i*sides+j;p[k*3]=(float)Math.Cos(a)*r+.26f*t*t;p[k*3+1]=t;p[k*3+2]=(float)Math.Sin(a)*r+.11f*(float)Math.Sin(t*5)*t;
                uv[k*2]=(float)j/sides;uv[k*2+1]=t;
                if(i==rings)continue;int n=i*sides+(j+1)%sides,o=k*6;
                tr[o]=k;tr[o+1]=k+sides;tr[o+2]=n;tr[o+3]=n;tr[o+4]=k+sides;tr[o+5]=n+sides;
            }
            p[(surface+1)*3]=.26f;p[(surface+1)*3+1]=1;p[(surface+1)*3+2]=.11f*(float)Math.Sin(5);
            uv[(surface+1)*2+1]=1;
            for(int j=0;j<sides;j++)
            {
                int o=rings*sides*6+j*6,n=(j+1)%sides;
                tr[o]=surface;tr[o+1]=j;tr[o+2]=n;
                tr[o+3]=surface+1;tr[o+4]=rings*sides+n;tr[o+5]=rings*sides+j;
            }
            return new FilledMeshRecipe(p,uv,tr);
        }
        // Distinct closed volumes; disconnected sub-volumes share one mesh/renderer budget slot.
        public static FilledMeshRecipe Sword()
        {
            var b=new VolumeBuilder();
            int tip=b.Vertex(0,0,0),start=b.Count;
            foreach(float y in new[]{.2f,.94f})
            {b.Vertex(-.22f,y,0);b.Vertex(0,y,.075f);b.Vertex(.22f,y,0);b.Vertex(0,y,-.075f);}
            for(int i=0;i<4;i++)
            {int n=(i+1)%4;b.Triangle(tip,start+n,start+i);b.Quad(start+i,start+n,start+4+n,start+4+i);}
            b.Quad(start+4,start+5,start+6,start+7);
            b.Beam(-.42f,.98f,0,.42f,.98f,0,.065f);
            b.Beam(0,1.02f,0,0,1.3f,0,.055f);
            return b.Build();
        }
        public static FilledMeshRecipe Lightning()
        {
            var b=new VolumeBuilder();
            float[] x={0,-.16f,.13f,-.2f,.1f,0},y={0,.24f,.46f,.73f,1.02f,1.4f};
            for(int i=0;i<5;i++)b.Beam(x[i],y[i],0,x[i+1],y[i+1],0,.038f);
            b.Beam(-.2f,.73f,0,-.52f,.58f,.12f,.024f);
            b.Beam(-.52f,.58f,.12f,-.42f,.3f,.2f,.018f);
            b.Beam(.13f,.46f,0,.46f,.31f,-.12f,.024f);
            return b.Build();
        }
        public static FilledMeshRecipe Arcane()
        {
            var b=new VolumeBuilder();
            // A cubical lattice with diagonals, not a cluster of crystal spikes.
            for(int axis=0;axis<3;axis++)for(int a=-1;a<=1;a+=2)for(int c=-1;c<=1;c+=2)
            {
                float[] from={a*.4f,c*.4f,.4f},to={a*.4f,c*.4f,-.4f};
                b.Beam(from[axis],from[(axis+1)%3]+.5f,from[(axis+2)%3],to[axis],to[(axis+1)%3]+.5f,to[(axis+2)%3],.032f);
            }
            b.Beam(-.4f,.1f,-.4f,.4f,.9f,.4f,.042f);
            b.Beam(.4f,.1f,-.4f,-.4f,.9f,.4f,.042f);
            return b.Build();
        }
        public static FilledMeshRecipe ArcaneShard()
        {
            var b=new VolumeBuilder();b.Beam(-.28f,0,0,.28f,.38f,0,.032f);b.Beam(.28f,.38f,0,.28f,.7f,.18f,.028f);return b.Build();
        }
        public static FilledMeshRecipe Rupture()
        {
            var b=new VolumeBuilder();
            for(int arm=0;arm<3;arm++)
            {
                double a=arm*Math.PI*2/3;float dx=(float)Math.Cos(a),dz=(float)Math.Sin(a);
                float px=0,pz=0;
                for(int i=1;i<=3;i++)
                {float x=dx*i*.28f-dz*(i%2==0?-.09f:.09f),z=dz*i*.28f+dx*(i%2==0?-.09f:.09f);b.Beam(px,.025f,pz,x,.025f,z,.027f);px=x;pz=z;}
            }
            return b.Build();
        }
        public static FilledMeshRecipe Arrow()
        {
            var b=new VolumeBuilder();b.Beam(0,.08f,0,0,1.7f,0,.022f);
            b.Beam(-.13f,.25f,0,0,0,0,.026f);b.Beam(0,0,0,.13f,.25f,0,.026f);
            b.Beam(0,1.38f,0,-.14f,1.64f,0,.018f);b.Beam(0,1.38f,0,.14f,1.64f,0,.018f);return b.Build();
        }
        public static FilledMeshRecipe Vine()
        {
            var b=new VolumeBuilder();float x=0,z=0;
            for(int i=1;i<=7;i++){float nx=(float)Math.Sin(i*.9f)*.55f,nz=i*.25f;b.Beam(x,.04f,z,nx,.04f,nz,.035f);if(i%2!=0)b.Beam(nx,.04f,nz,nx+(i%4==1?.3f:-.3f),.14f,nz+.18f,.024f);x=nx;z=nz;}
            return b.Build();
        }
        private sealed class VolumeBuilder
        {
            private readonly List<float> positions=new List<float>(),uv=new List<float>();
            private readonly List<int> indices=new List<int>();
            public int Count=>positions.Count/3;
            public int Vertex(float x,float y,float z){int i=Count;positions.Add(x);positions.Add(y);positions.Add(z);uv.Add((x+1)*.5f);uv.Add(Math.Max(0,Math.Min(1,y)));return i;}
            public void Triangle(int a,int b,int c){indices.Add(a);indices.Add(b);indices.Add(c);}
            public void Quad(int a,int b,int c,int d){Triangle(a,b,c);Triangle(a,c,d);}
            public void Beam(float ax,float ay,float az,float bx,float by,float bz,float width)
            {
                float dx=bx-ax,dy=by-ay,dz=bz-az,length=(float)Math.Sqrt(dx*dx+dy*dy+dz*dz);dx/=length;dy/=length;dz/=length;
                float ux=-dy,uy=dx,uz=0;
                if(Math.Abs(dz)>.95f){ux=1;uy=uz=0;}
                float ul=(float)Math.Sqrt(ux*ux+uy*uy+uz*uz);ux=ux/ul*width;uy=uy/ul*width;uz=uz/ul*width;
                float vx=dy*uz-dz*uy,vy=dz*ux-dx*uz,vz=dx*uy-dy*ux;int o=Count;
                foreach(int end in new[]{0,1})foreach(int corner in new[]{0,1,2,3})
                {float u=corner==0||corner==3?-1:1,v=corner<2?-1:1;Vertex((end==0?ax:bx)+ux*u+vx*v,(end==0?ay:by)+uy*u+vy*v,(end==0?az:bz)+uz*u+vz*v);}
                Quad(o,o+3,o+2,o+1);Quad(o+4,o+5,o+6,o+7);
                for(int i=0;i<4;i++){int n=(i+1)%4;Quad(o+i,o+n,o+4+n,o+4+i);}
            }
            public FilledMeshRecipe Build(){return new FilledMeshRecipe(positions.ToArray(),uv.ToArray(),indices.ToArray());}
        }
        private static bool Finite(float n){return !float.IsNaN(n)&&!float.IsInfinity(n);}
    }
}
