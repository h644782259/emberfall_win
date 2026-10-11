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
        public static void AddJumpPlatform(Vector3 center,float radius,float height)
        {
            obstacles.Add(new Obstacle { Center=new Vector2(center.x,center.z), Radius=radius, Height=height });
            grids.Clear();revision++;
        }
        private static bool ContainsTop(Obstacle o,Vector3 point,float radius)
        {
            if(o.Height<=0)return false;
            if(o.Radius>0)
            {float r=Mathf.Max(0,o.Radius-.12f);return new Vector2(point.x-o.Center.x,point.z-o.Center.y).sqrMagnitude<=r*r;}
            return Mathf.Abs(point.x-o.Center.x)<=o.Half.x-radius&&Mathf.Abs(point.z-o.Center.y)<=o.Half.y-radius;
        }
        public static float SurfaceHeight(Vector3 point,float radius=.45f)
        {
            float height=WorldTerrain.Height(point);
            foreach(var o in obstacles)if(ContainsTop(o,point,radius))height=Mathf.Max(height,WorldTerrain.Height(point)+o.Height);
            return height;
        }
        private static bool ClearAtHeight(Vector3 point,float radius,float height)
        {
            if(!Finite(point.x)||!Finite(point.z)||!Finite(height)||CombatFx.Flat(point).magnitude>arena-radius)return false;
            foreach(var o in obstacles)
            {
                if(o.Height>0&&height>=WorldTerrain.Height(point)+o.Height-.015f)continue;
                Vector2 d=new Vector2(point.x-o.Center.x,point.z-o.Center.y);
                if(o.Radius>0){float clearance=o.Height>0?Mathf.Min(radius,.12f):radius;if(d.sqrMagnitude<(o.Radius+clearance)*(o.Radius+clearance))return false;}
                else {var nearest=new Vector2(Mathf.Max(0,Mathf.Abs(d.x)-o.Half.x),Mathf.Max(0,Mathf.Abs(d.y)-o.Half.y));if(nearest.sqrMagnitude<=radius*radius)return false;}
            }
            return true;
        }
        public static bool CanStand(Vector3 point,float radius=.45f)
        {
            float height=StandingHeight(point,radius,point.y);
            return Mathf.Abs(point.y-height)<.035f&&ClearAtHeight(point,radius,height)&&(height>0||IsWalkable(point,radius));
        }
        public static Vector3 JumpPosition(Vector3 from,Vector3 to,float progress)
        {
            float t=Mathf.Clamp01(progress);
            // Rise clear of the starting ledge before moving across its side.
            float travel=t;
            return Vector3.Lerp(from,to,travel)+Vector3.up*(Mathf.Sin(t*Mathf.PI)*1.65f);
        }
        public static bool TryResolvePlatformJump(Vector3 from,Vector3 direction,float distance,float radius,out Vector3 landing)
        {
            landing=from;direction=CombatFx.Flat(direction).normalized;
            if(!CanStand(from,radius)||direction.sqrMagnitude<.01f)return false;
            // Try the intended distance first; shorter safe points allow landing on
            // a narrow box without requiring pixel-perfect movement input.
            for(int pass=0;pass<(from.y<=WorldTerrain.Height(from)+.05f?2:1);pass++)
            for(float d=distance;d>=.05f;d-=.05f)
            {
                var to=CombatFx.Flat(from)+direction*d;to.y=SurfaceHeight(to,radius);
                if(from.y<=WorldTerrain.Height(from)+.05f&&(pass==0?to.y<=WorldTerrain.Height(to):to.y>WorldTerrain.Height(to)))continue;
                if(!CanStand(to,radius))continue;
                int samples=Mathf.Max(24,Mathf.CeilToInt(d/.08f));bool clear=true;
                for(int i=0;i<=samples;i++)
                {float t=i/(float)samples;var p=JumpPosition(from,to,t);if(!ClearAtHeight(p,radius,p.y)){clear=false;break;}}
                if(clear){landing=to;return true;}
            }
            return false;
        }
        private static float StandingHeight(Vector3 point,float radius,float ceiling)
        {
            float height=WorldTerrain.Height(point);
            foreach(var o in obstacles)
            {
                if(o.Height<=0||WorldTerrain.Height(point)+o.Height>ceiling+.035f)continue;
                Vector2 d=new Vector2(point.x-o.Center.x,point.z-o.Center.y);
                float clearance=o.Radius>0?Mathf.Min(radius,.12f):radius;
                bool overlap;
                if(o.Radius>0)overlap=d.sqrMagnitude<(o.Radius+clearance)*(o.Radius+clearance);
                else {var nearest=new Vector2(Mathf.Max(0,Mathf.Abs(d.x)-o.Half.x),Mathf.Max(0,Mathf.Abs(d.y)-o.Half.y));overlap=nearest.sqrMagnitude<=radius*radius;}
                if(overlap)height=Mathf.Max(height,WorldTerrain.Height(point)+o.Height);
            }
            return height;
        }
        // Keep horizontal control while gravity settles the player onto the next lower surface.
        public static Vector3 MovePlayer(Vector3 from,Vector3 delta,float deltaTime,ref float fallSpeed,float radius=.45f)
        {
            if(!Finite(deltaTime)||deltaTime<=0)return from;
            if(from.y<=WorldTerrain.Height(from)+.035f){fallSpeed=0;return Move(from,delta,radius);}
            delta=CombatFx.Flat(delta);
            int steps=Mathf.Max(1,Mathf.Max(Mathf.CeilToInt(delta.magnitude/.08f),Mathf.CeilToInt(deltaTime/.02f)));
            float dt=deltaTime/steps;
            for(int i=0;i<steps;i++)
            {
                Vector3 next=from+delta/steps;
                if(!ClearAtHeight(next,radius,from.y)||IsOpenWater(next,radius))next=from;
                float support=StandingHeight(next,radius,from.y);
                if(from.y<=support+.001f){next.y=support;fallSpeed=0;}
                else
                {
                    float oldSpeed=fallSpeed;
                    fallSpeed+=18f*dt;
                    next.y=Mathf.Max(support,from.y-(oldSpeed+fallSpeed)*.5f*dt);
                    if(next.y<=support+.001f)fallSpeed=0;
                }
                from=next;
            }
            return from;
        }
        private static Vector3 MoveOnPlatform(Vector3 from,Vector3 delta,float radius)
        {
            float speed=0;
            return MovePlayer(from,delta,1f/60f,ref speed,radius);
        }
        public static bool HeightAttackAllowed(Vector3 from,Vector3 to,bool ranged)
        {return Mathf.Abs(from.y-to.y)<(ranged?2.5f:.65f);}
    }
}
