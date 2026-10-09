using System.Collections.Generic;
using UnityEngine;
namespace Emberfall
{
    // Keep the real contact/root fixed. Certify entire faces, not just a few rays through them.
    internal static class AnchoredImpactMesh
    {
        private const int MaximumDepth=6,MaximumTriangles=4096,MaximumChecks=4096;
        internal static Mesh Create(Mesh source,Transform root,Vector3 offset,Vector3 scale,Quaternion rotation)
        {
            Vector3 origin=CombatFx.Flat(root.position);Vector3[] points=(Vector3[])source.vertices.Clone();
            var sourceUv=source.uv;var original=source.triangles;
            float minX=float.PositiveInfinity,minZ=float.PositiveInfinity,maxX=float.NegativeInfinity,maxZ=float.NegativeInfinity;
            for(int i=0;i<points.Length;i++)
            {
                Vector3 world=root.TransformPoint(offset+rotation*Vector3.Scale(points[i],scale));points[i]=world;
                minX=Mathf.Min(minX,world.x);minZ=Mathf.Min(minZ,world.z);maxX=Mathf.Max(maxX,world.x);maxZ=Mathf.Max(maxZ,world.z);
            }
            // Two exact whole-face certificates cover the complete transformed bounds.
            // Open ground needs no per-vertex rays or recursive face clipping.
            Vector3 cornerA=new Vector3(minX,0,minZ),cornerB=new Vector3(maxX,0,minZ),cornerC=new Vector3(maxX,0,maxZ),cornerD=new Vector3(minX,0,maxZ);
            if(points.Length>0&&original.Length<=MaximumTriangles*3&&CombatSight.VisualTriangle(origin,cornerA,cornerB,cornerC)&&CombatSight.VisualTriangle(origin,cornerA,cornerC,cornerD))
            {
                for(int i=0;i<points.Length;i++)points[i]=root.InverseTransformPoint(points[i]);
                var clearMesh=new Mesh{name=source.name+" / certified open ground",vertices=points,uv=sourceUv,triangles=original};
                clearMesh.RecalculateNormals();clearMesh.RecalculateBounds();return clearMesh;
            }
            for(int i=0;i<points.Length;i++)
            {
                Vector3 world=points[i];Vector3 clipped=CombatSight.BoundaryPoint(CombatSightKind.Area,origin,world);clipped.y=world.y;points[i]=clipped;
            }
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
            int remainingChecks=MaximumChecks;
            for(int i=0;i<original.Length;i+=3)
            {
                int a=original[i],b=original[i+1],c=original[i+2];
                // Share the creation budget across source faces: hidden early faces must not
                // spend every query before later, visible identity features are considered.
                int allowance=System.Math.Min(remainingChecks,System.Math.Max(1,MaximumChecks/System.Math.Max(1,original.Length/3)));
                int remaining=allowance;
                Add(root,origin,points[a],points[b],points[c],sourceUv[a],sourceUv[b],sourceUv[c],0,ref remaining,vertices,uv,triangles);
                remainingChecks-=allowance-remaining;
            }
            var mesh=new Mesh{name=source.name+" / anchored cover clip",vertices=vertices.ToArray(),uv=uv.ToArray(),triangles=triangles.ToArray()};mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        private static void Add(Transform root,Vector3 origin,Vector3 a,Vector3 b,Vector3 c,Vector2 ua,Vector2 ub,Vector2 uc,int depth,ref int remainingChecks,List<Vector3> vertices,List<Vector2> uv,List<int> triangles)
        {
            if(triangles.Count>=MaximumTriangles*3||remainingChecks<=0)return;
            remainingChecks--;
            if(CombatSight.VisualTriangle(origin,a,b,c))
            {
                int first=vertices.Count;vertices.Add(root.InverseTransformPoint(a));vertices.Add(root.InverseTransformPoint(b));vertices.Add(root.InverseTransformPoint(c));uv.Add(ua);uv.Add(ub);uv.Add(uc);
                triangles.Add(first);triangles.Add(first+1);triangles.Add(first+2);return;
            }
            if(depth>=MaximumDepth)return; // Uncertified slivers are omitted, never painted across cover.
            float ab=CombatFx.Flat(a-b).sqrMagnitude,bc=CombatFx.Flat(b-c).sqrMagnitude,ca=CombatFx.Flat(c-a).sqrMagnitude;
            if(bc>ab&&bc>=ca){Vector3 old=a;a=b;b=c;c=old;Vector2 oldUv=ua;ua=ub;ub=uc;uc=oldUv;}
            else if(ca>ab&&ca>bc){Vector3 old=c;c=b;b=a;a=old;Vector2 oldUv=uc;uc=ub;ub=ua;ua=oldUv;}
            Vector3 mid=(a+b)*.5f;Vector2 midUv=new Vector2((ua.x+ub.x)*.5f,(ua.y+ub.y)*.5f);
            Add(root,origin,a,mid,c,ua,midUv,uc,depth+1,ref remainingChecks,vertices,uv,triangles);
            Add(root,origin,mid,b,c,midUv,ub,uc,depth+1,ref remainingChecks,vertices,uv,triangles);
        }
    }
}
