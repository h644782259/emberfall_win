using System;
using Emberfall;
using UnityEngine;
public static class DestructibleTraversalTests
{
    private static int checks;
    private static void Check(bool v,string m){checks++;if(!v)throw new Exception(m);}
    public static string Run()
    {
        checks=0;WorldTraversal.Reset(ZoneKind.Dungeon);
        var first=WorldTraversal.AddDynamicCircle(Vector3.zero,.8f);var second=WorldTraversal.AddDynamicCircle(new Vector3(3,0,0),.8f);
        Check(!WorldTraversal.IsWalkable(Vector3.zero),"intact optional blocker stops movement");
        Check(WorldTraversal.HasLineOfSightIgnoringObstacle(new Vector3(-3,0,0),Vector3.zero,first),"own prop may be hit through its own navigation envelope");
        Check(!WorldTraversal.HasLineOfSightIgnoringObstacle(new Vector3(-3,0,0),new Vector3(5,0,0),first),"ignoring own prop does not ignore another obstacle");
        Check(WorldTraversal.RemoveDynamicObstacle(first)&&WorldTraversal.IsWalkable(Vector3.zero),"break removes own blocker");
        Check(!WorldTraversal.RemoveDynamicObstacle(first)&&!WorldTraversal.IsWalkable(new Vector3(3,0,0)),"duplicate removal cannot remove neighbour");
        WorldTraversal.Reset(ZoneKind.Dungeon);var nextRoom=WorldTraversal.AddDynamicCircle(Vector3.zero,.8f);
        Check(!WorldTraversal.RemoveDynamicObstacle(second)&&!WorldTraversal.IsWalkable(Vector3.zero),"old room deferred destroy cannot remove new handle");
        Check(WorldTraversal.RemoveDynamicObstacle(nextRoom),"new room owns independent token");
        Check(WorldTraversal.AddDynamicCircle(new Vector3(float.NaN,0,0),1)==null&&WorldTraversal.AddDynamicBox(Vector3.zero,new Vector2(-1,1))==null,"invalid dynamic bounds rejected");
        var wall=WorldTraversal.AddDynamicBox(Vector3.zero,new Vector2(1,6));
        var from=new Vector3(-3,0,0);var to=new Vector3(3,0,0);
        Check(!WorldTraversal.HasGroundPath(from,to)&&WorldTraversal.FindPath(from,to).Count>1,"pathfinder routes around intact optional rubble");
        Check(WorldTraversal.RemoveDynamicObstacle(wall)&&WorldTraversal.HasGroundPath(from,to)&&WorldTraversal.FindPath(from,to).Count==1,"cleared shortcut immediately usable");
        WorldTraversal.Reset(ZoneKind.Wilderness);
        WorldTraversal.SetRiver(new[]{new Vector3(-8,0,0),new Vector3(8,0,0)},2,new Rect(-1,-2,2,4));
        Check(WorldTraversal.IsOpenWater(new Vector3(3,0,0)),"map classifies actual river water");
        Check(!WorldTraversal.IsOpenWater(Vector3.zero)&&WorldTraversal.IsWalkable(Vector3.zero),"bridge remains dry ground on map");
        Check(!WorldTraversal.IsOpenWater(new Vector3(30,0,0)),"outside arena is not water");
        int beforeObstacle=WorldTraversal.Revision;
        var bridge=WorldTraversal.AddDynamicBox(Vector3.zero,new Vector2(.6f,.6f));
        Check(WorldTraversal.Revision!=beforeObstacle,"new obstacle invalidates terrain map");
        Check(!WorldTraversal.IsOpenWater(Vector3.zero),"solid blocker is not water");
        int blockedRevision=WorldTraversal.Revision;
        Check(!WorldTraversal.HasGroundPath(new Vector3(0,0,-3),new Vector3(0,0,3)),"dynamic bridge test fixture blocks crossing");
        WorldTraversal.RemoveDynamicObstacle(bridge);
        Check(WorldTraversal.Revision!=blockedRevision,"broken obstacle invalidates terrain map");
        Check(WorldTraversal.HasGroundPath(new Vector3(0,0,-3),new Vector3(0,0,3))&&!WorldTraversal.IsWalkable(new Vector3(3,0,0)),"removal preserves underlying river and bridge rules");
        int oldRevision=WorldTraversal.Revision;
        WorldTraversal.Reset(ZoneKind.Wilderness);
        Check(WorldTraversal.Revision!=oldRevision&&!WorldTraversal.IsOpenWater(new Vector3(3,0,0)),"new town without river replaces old map");
        WorldTraversal.Reset(ZoneKind.Dungeon);Vector3 portal;
        Check(WorldTraversal.TryReturnPortalPosition(new Vector3(0,0,-8),out portal)&&portal.sqrMagnitude<.001f,"clear center remains portal origin");
        WorldTraversal.AddBox(Vector3.zero,new Vector2(2,2));
        Check(WorldTraversal.TryReturnPortalPosition(new Vector3(0,0,-8),out portal),"center obstacle finds nearby portal");
        Check(WorldTraversal.IsWalkable(portal,2.65f)&&WorldTraversal.CanReach(new Vector3(0,0,-8),portal,.45f),"whole portal footprint clear and reachable");
        Check(portal.magnitude<=4.25f,"search selects nearby clearance instead of entrance");
        WorldTraversal.Reset(ZoneKind.Dungeon);WorldTraversal.AddBox(new Vector3(0,0,1),new Vector2(40,1));
        Check(WorldTraversal.TryReturnPortalPosition(new Vector3(0,0,-8),out portal)&&portal.z<0,"portal remains on reachable side of arena partition");
        WorldTraversal.Reset(ZoneKind.Dungeon);WorldTraversal.AddBox(Vector3.zero,new Vector2(40,40));
        Check(!WorldTraversal.TryReturnPortalPosition(new Vector3(0,0,-8),out portal),"no valid point never returns obstructed zero as success");
        return "PASS: "+checks+" dynamic traversal handle assertions";
    }
}
// Managed dependencies only. The tested production file is WorldTraversal.cs;
// blink is deliberately unsupported here and cannot silently pass via a fake implementation.
namespace Emberfall
{
    public enum ZoneKind{Wilderness,Dungeon}
    public static class PlayerUpgradeRules{public static float FindSafeBlinkDistance(float d,Func<float,bool>a,Func<float,bool>b){throw new NotSupportedException("Blink is outside this fixture");}}
    public static class CombatFx
    {
        public static Vector3 Flat(Vector3 v){v.y=0;return v;}
        public static float SegmentDistance(Vector3 p,Vector3 a,Vector3 b){p.y=a.y=b.y=0;var d=b-a;float t=d.sqrMagnitude<.0001f?0:Mathf.Clamp(Vector3.Dot(p-a,d)/d.sqrMagnitude,0,1);return Vector3.Distance(p,a+d*t);}
    }
}
namespace UnityEngine
{
    public struct Vector2
    {
        public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}public float sqrMagnitude=>x*x+y*y;
        public static Vector2 operator -(Vector2 a,Vector2 b)=>new Vector2(a.x-b.x,a.y-b.y);public static Vector2 operator *(Vector2 a,float b)=>new Vector2(a.x*b,a.y*b);
    }
    public struct Vector3
    {
        public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}public static Vector3 zero=>new Vector3();public static Vector3 up=>new Vector3(0,1,0);
        public float sqrMagnitude=>x*x+y*y+z*z;public float magnitude=>(float)Math.Sqrt(sqrMagnitude);public Vector3 normalized=>magnitude>1e-6f?this/magnitude:zero;
        public void Normalize(){this=normalized;}public static Vector3 operator +(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);
        public static Vector3 operator -(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);public static Vector3 operator *(Vector3 a,float b)=>new Vector3(a.x*b,a.y*b,a.z*b);
        public static Vector3 operator /(Vector3 a,float b)=>a*(1/b);public static float Dot(Vector3 a,Vector3 b)=>a.x*b.x+a.y*b.y+a.z*b.z;
        public static float Distance(Vector3 a,Vector3 b)=>(a-b).magnitude;public static Vector3 Lerp(Vector3 a,Vector3 b,float t)=>a+(b-a)*t;
        public static Vector3 ClampMagnitude(Vector3 a,float m)=>a.magnitude>m?a.normalized*m:a;
    }
    public struct Rect{public float xMin,xMax,yMin,yMax;public Rect(float x,float y,float w,float h){xMin=x;xMax=x+w;yMin=y;yMax=y+h;}}
    public static class Time{public static float time=0;}
    public static class Mathf
    {
        public const float PI=(float)Math.PI;public static float Sin(float x)=>(float)Math.Sin(x);public static float Cos(float x)=>(float)Math.Cos(x);
        public static float Abs(float x)=>Math.Abs(x);public static float Min(float x,float y)=>Math.Min(x,y);public static int Min(int x,int y)=>Math.Min(x,y);
        public static float Max(float x,float y)=>Math.Max(x,y);public static int Max(int x,int y)=>Math.Max(x,y);
        public static float Clamp01(float x)=>Clamp(x,0,1);public static float Clamp(float x,float a,float b)=>Math.Min(b,Math.Max(a,x));public static int Clamp(int x,int a,int b)=>Math.Min(b,Math.Max(a,x));
        public static int CeilToInt(float x)=>(int)Math.Ceiling(x);public static int RoundToInt(float x)=>(int)Math.Round(x);
    }
}
