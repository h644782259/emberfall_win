using UnityEngine;
namespace Emberfall
{
    /// <summary>Shared preview and impact policy. No physics/layer-dependent shortcuts.</summary>
    internal static class CombatSight
    {
        private static bool Finite(Vector3 p)
        {return !float.IsNaN(p.x)&&!float.IsNaN(p.z)&&!float.IsInfinity(p.x)&&!float.IsInfinity(p.z);}
        private static bool Reach(CombatSightKind kind,Vector3 origin,Vector3 target)
        {
            if(!Finite(origin)||!Finite(target))return false;
            // Area silhouettes contain airborne vertices; their height must not
            // reject otherwise visible ground damage or clip tall spell meshes.
            float height=kind==CombatSightKind.Area?Mathf.Abs(WorldTraversal.SurfaceHeight(origin,.05f)-WorldTraversal.SurfaceHeight(target,.05f)):Mathf.Abs(origin.y-target.y);
            if(height>=(kind==CombatSightKind.Melee?.65f:2.5f))return false;
            return CombatSightRules.Allows(kind,WorldTraversal.HasLineOfSight(origin,target),
                kind!=CombatSightKind.Melee||WorldTraversal.HasGroundPath(origin,target,.15f));
        }
        public static void FillAreaBoundary(Vector3[] points,Vector3 center,float radius,CombatSightKind kind=CombatSightKind.Area)
        {
            for(int i=0;i<points.Length;i++)
            {
                float a=i*Mathf.PI*2/points.Length;
                points[i]=BoundaryPoint(kind,center,center+new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius))+new Vector3(0,.12f,0);
            }
        }
        // A swept disk is deliberately conservative: the complete visual footprint, not
        // just its origin, must fit on the damage-visible side of solid cover.
        public static bool VisualFootprint(Vector3 origin,Vector3 point,float extent)
        { return Area(origin,point)&&WorldTraversal.HasClearVisualFootprint(origin,point,extent); }
        public static bool VisualTriangle(Vector3 origin,Vector3 a,Vector3 b,Vector3 c)
        { return WorldTraversal.HasClearVisualTriangle(origin,a,b,c); }
        public static bool Direct(Vector3 origin,Vector3 target){return Reach(CombatSightKind.Direct,origin,target);}
        public static bool Area(Vector3 center,Vector3 target){return Reach(CombatSightKind.Area,center,target);}
        public static bool Chain(Vector3 previous,Vector3 target){return Reach(CombatSightKind.Chain,previous,target);}
        public static bool Melee(Vector3 origin,Vector3 target){return Reach(CombatSightKind.Melee,origin,target);}
        public static Vector3 GroundPoint(Vector3 origin,Vector3 desired) { return BoundaryPoint(CombatSightKind.GroundPlacement,origin,desired); }
        public static Vector3 BoundaryPoint(CombatSightKind kind,Vector3 origin,Vector3 desired)
        {
            desired.y=WorldTraversal.SurfaceHeight(desired,.05f);
            if(!Finite(origin))return Vector3.zero;
            if(!Finite(desired))return origin;
            if(!Reach(kind,origin,origin))return origin;
            if(Reach(kind,origin,desired))return desired;
            float low=0,high=1;
            for(int n=0;n<CombatSightRules.RefinementSteps;n++)
            {float mid=(low+high)*.5f;if(Reach(kind,origin,Vector3.Lerp(origin,desired,mid)))low=mid;else high=mid;}
            return Vector3.Lerp(origin,desired,low);
        }
    }
}
