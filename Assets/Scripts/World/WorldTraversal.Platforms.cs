using UnityEngine;
namespace Emberfall
{
    public static partial class WorldTraversal
    {
        public static ObstacleHandle AddPlatform(Vector3 center,Vector2 size,float height)
        {
            if(!Finite(height)||height<.2f||height>1.1f||size.x<1.2f||size.y<1.2f)return null;
            var handle=AddDynamicBox(center,size);
            if(handle==null)return null;
            int last=obstacles.Count-1;var obstacle=obstacles[last];obstacle.Height=height;obstacles[last]=obstacle;return handle;
        }
        private static bool ContainsTop(Obstacle o,Vector3 point,float radius)
        {return o.Height>0&&Mathf.Abs(point.x-o.Center.x)<=o.Half.x-radius&&Mathf.Abs(point.z-o.Center.y)<=o.Half.y-radius;}
        public static float SurfaceHeight(Vector3 point,float radius=.45f)
        {
            float height=0;
            foreach(var o in obstacles)if(ContainsTop(o,point,radius))height=Mathf.Max(height,o.Height);
            return height;
        }
        private static bool ClearAtHeight(Vector3 point,float radius,float height)
        {
            if(!Finite(point.x)||!Finite(point.z)||!Finite(height)||CombatFx.Flat(point).magnitude>arena-radius)return false;
            foreach(var o in obstacles)
            {
                if(o.Height>0&&height>=o.Height-.015f)continue;
                Vector2 d=new Vector2(point.x-o.Center.x,point.z-o.Center.y);
                if(o.Radius>0){if(d.sqrMagnitude<(o.Radius+radius)*(o.Radius+radius))return false;}
                else {var nearest=new Vector2(Mathf.Max(0,Mathf.Abs(d.x)-o.Half.x),Mathf.Max(0,Mathf.Abs(d.y)-o.Half.y));if(nearest.sqrMagnitude<=radius*radius)return false;}
            }
            return true;
        }
        public static bool CanStand(Vector3 point,float radius=.45f)
        {
            float height=SurfaceHeight(point,radius);
            return Mathf.Abs(point.y-height)<.035f&&ClearAtHeight(point,radius,height)&&(height>0||IsWalkable(point,radius));
        }
        public static bool TryResolvePlatformJump(Vector3 from,Vector3 direction,float distance,float radius,out Vector3 landing)
        {
            landing=from;direction=CombatFx.Flat(direction).normalized;
            if(!CanStand(from,radius)||direction.sqrMagnitude<.01f)return false;
            // Try the intended distance first; shorter safe points allow landing on
            // a narrow box without requiring pixel-perfect movement input.
            for(float d=distance;d>=.5f;d-=.15f)
            {
                var to=CombatFx.Flat(from)+direction*d;to.y=SurfaceHeight(to,radius);
                if(to.y<=0&&from.y<=0)continue;
                if(!CanStand(to,radius))continue;
                int samples=Mathf.Max(24,Mathf.CeilToInt(d/.08f));bool clear=true;
                for(int i=0;i<=samples;i++)
                {float t=i/(float)samples;var p=Vector3.Lerp(from,to,t);float y=p.y+Mathf.Sin(t*Mathf.PI)*1.65f;if(!ClearAtHeight(p,radius,y)){clear=false;break;}}
                if(clear){landing=to;return true;}
            }
            return false;
        }
        private static Vector3 MoveOnPlatform(Vector3 from,Vector3 delta,float radius)
        {
            delta=CombatFx.Flat(delta);int steps=Mathf.Max(1,Mathf.CeilToInt(delta.magnitude/.12f));var step=delta/steps;
            for(int i=0;i<steps;i++)
            {
                var next=from+step;float top=SurfaceHeight(next,radius);
                // Ground actors cannot walk onto a raised top. Leaving its rim is
                // blocked until jumping down, so a stationary actor never sinks.
                if(Mathf.Abs(top-from.y)>.035f||!ClearAtHeight(next,radius,top))break;
                next.y=top;from=next;
            }
            return from;
        }
        public static bool HeightAttackAllowed(Vector3 from,Vector3 to,bool ranged)
        {return Mathf.Abs(from.y-to.y)<(ranged?2.5f:.65f);}
    }
}
