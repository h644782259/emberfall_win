using System.Collections.Generic;
using UnityEngine;

namespace Emberfall
{
    /// <summary>Shared ground rules for direct movement, leaps and creature routes.</summary>
    public static partial class WorldTraversal
    {
        public sealed class ObstacleHandle { internal ObstacleHandle(){} }
        private struct Obstacle { public Vector2 Center, Half; public float Radius,Height; public ObstacleHandle Handle; }
        // Immutable-in-use search order; private and never returned or written after initialization.
        private static readonly int[] NeighborX = { -1, 0, 1, -1, 1, -1, 0, 1 };
        private static readonly int[] NeighborY = { -1, -1, -1, 0, 0, 1, 1, 1 };
        private static readonly List<Obstacle> obstacles = new List<Obstacle>();
        private static readonly Dictionary<int, bool[]> grids = new Dictionary<int, bool[]>();
        private static Vector3[] river;
        private static float riverHalfWidth;
        private static Rect bridge;
        private static float arena = 22;
        private static int revision;
        public static int Revision {get{return revision;}}
        private const float Cell = .7f;
        private const int Side = 67;
        private const float GridOrigin = -23.1f;
        public static void Reset(ZoneKind zone)
        {
            obstacles.Clear(); grids.Clear(); river = null; arena = zone == ZoneKind.Dungeon ? 18 : 22; revision++;
        }
        public static void AddCircle(Vector3 center, float radius)
        {
            obstacles.Add(new Obstacle { Center = new Vector2(center.x, center.z), Radius = radius }); grids.Clear(); revision++;
        }
        public static void AddBox(Vector3 center, Vector2 size)
        {
            obstacles.Add(new Obstacle { Center = new Vector2(center.x, center.z), Half = size * .5f }); grids.Clear(); revision++;
        }
        public static ObstacleHandle AddDynamicCircle(Vector3 center,float radius)
        {
            if(!Finite(center.x)||!Finite(center.z)||!Finite(radius)||radius<=0)return null;
            var handle=new ObstacleHandle();
            obstacles.Add(new Obstacle{Center=new Vector2(center.x,center.z),Radius=radius,Handle=handle});
            grids.Clear();revision++;return handle;
        }
        public static ObstacleHandle AddDynamicBox(Vector3 center,Vector2 size)
        {
            if(!Finite(center.x)||!Finite(center.z)||!Finite(size.x)||!Finite(size.y)||size.x<=0||size.y<=0)return null;
            var handle=new ObstacleHandle();
            obstacles.Add(new Obstacle{Center=new Vector2(center.x,center.z),Half=size*.5f,Handle=handle});
            grids.Clear();revision++;return handle;
        }
        public static bool RemoveDynamicObstacle(ObstacleHandle handle)
        {
            if(handle==null)return false;
            for(int i=0;i<obstacles.Count;i++)if(object.ReferenceEquals(obstacles[i].Handle,handle))
            {obstacles.RemoveAt(i);grids.Clear();revision++;return true;}
            return false;
        }
        public static bool HasLineOfSightIgnoringObstacle(Vector3 from,Vector3 to,ObstacleHandle handle)
        {return ClearSegment(from,to,.04f,true,handle);}
        public static void SetRiver(Vector3[] centerline, float width, Rect crossing)
        {
            river = centerline; riverHalfWidth = width * .5f; bridge = crossing; grids.Clear(); revision++;
        }
        public static bool IsWalkable(Vector3 point, float radius = .45f)
        {
            if (!ClearOfSolids(point, radius)) return false;
            Vector2 p = new Vector2(point.x, point.z);
            if (river == null || (p.x >= bridge.xMin + radius && p.x <= bridge.xMax - radius && p.y >= bridge.yMin && p.y <= bridge.yMax)) return true;
            for (int i = 1; i < river.Length; i++)
                if (CombatFx.SegmentDistance(point, river[i - 1], river[i]) < riverHalfWidth + radius) return false;
            return true;
        }
        // Navigation map classification uses the same geometry as movement. Bridges
        // remain ground; solid obstacles are never mislabelled as water.
        public static bool IsOpenWater(Vector3 point, float radius = .16f)
        { return ClearOfSolids(point, radius) && !IsWalkable(point, radius); }
        private static bool ClearOfSolids(Vector3 point, float radius, ObstacleHandle ignored=null)
        {
            Vector2 p = new Vector2(point.x, point.z);
            if (p.sqrMagnitude > (arena - radius) * (arena - radius)) return false;
            foreach (Obstacle obstacle in obstacles)
            {
                if(ignored!=null&&object.ReferenceEquals(obstacle.Handle,ignored))continue;
                Vector2 delta = p - obstacle.Center;
                if (obstacle.Radius > 0) { if (delta.sqrMagnitude < (obstacle.Radius + radius) * (obstacle.Radius + radius)) return false; }
                else
                {
                    Vector2 nearest = new Vector2(Mathf.Max(0, Mathf.Abs(delta.x) - obstacle.Half.x), Mathf.Max(0, Mathf.Abs(delta.y) - obstacle.Half.y));
                    if (nearest.sqrMagnitude <= radius * radius) return false;
                }
            }
            return true;
        }
        // The union of these three fans is the convex hull of origin and the face.
        // Every ray to every point of the face (also under radial animation shrink)
        // lies in that hull. Exact distances use the same .04 LOS clearance and the
        // same registered solids; unlike a constant swept disk, no face-width radius
        // is imposed at the real impact origin. Water is intentionally not a solid.
        public static bool HasClearVisualTriangle(Vector3 origin,Vector3 a,Vector3 b,Vector3 c)
        {
            const double clearance=.04f;
            double limit=arena-clearance;
            if(!VisualInsideArena(origin,limit)||!VisualInsideArena(a,limit)||!VisualInsideArena(b,limit)||!VisualInsideArena(c,limit))return false;
            foreach(Obstacle obstacle in obstacles)
                if(!VisualFanClear(origin,a,b,obstacle,clearance)||!VisualFanClear(origin,b,c,obstacle,clearance)||!VisualFanClear(origin,c,a,obstacle,clearance))return false;
            return true;
        }
        private static bool VisualInsideArena(Vector3 p,double limit)
        {return Finite(p.x)&&Finite(p.z)&&(double)p.x*p.x+(double)p.z*p.z<=limit*limit;}
        private static bool VisualFanClear(Vector3 a,Vector3 b,Vector3 c,Obstacle obstacle,double clearance)
        {
            Vector3 center=new Vector3(obstacle.Center.x,0,obstacle.Center.y);
            if(obstacle.Radius>0)
            {double radius=obstacle.Radius+clearance;return VisualPointTriangleDistance(center,a,b,c)>=radius*radius;}
            // Distance to the actual rectangle, not its expanded AABB, preserves
            // legal near-corner rays while respecting its round .04 clearance.
            double x0=(double)obstacle.Center.x-obstacle.Half.x,x1=(double)obstacle.Center.x+obstacle.Half.x;
            double z0=(double)obstacle.Center.y-obstacle.Half.y,z1=(double)obstacle.Center.y+obstacle.Half.y;
            if(VisualInsideBox(a,x0,x1,z0,z1)||VisualInsideBox(b,x0,x1,z0,z1)||VisualInsideBox(c,x0,x1,z0,z1))return false;
            Vector3 p=new Vector3((float)x0,0,(float)z0),q=new Vector3((float)x1,0,(float)z0),r=new Vector3((float)x1,0,(float)z1),t=new Vector3((float)x0,0,(float)z1);
            double distance=System.Math.Min(System.Math.Min(VisualPointTriangleDistance(p,a,b,c),VisualPointTriangleDistance(q,a,b,c)),System.Math.Min(VisualPointTriangleDistance(r,a,b,c),VisualPointTriangleDistance(t,a,b,c)));
            distance=System.Math.Min(distance,VisualEdgeBoxDistance(a,b,p,q,r,t));
            distance=System.Math.Min(distance,VisualEdgeBoxDistance(b,c,p,q,r,t));
            distance=System.Math.Min(distance,VisualEdgeBoxDistance(c,a,p,q,r,t));
            return distance>clearance*clearance;
        }
        private static bool VisualInsideBox(Vector3 p,double x0,double x1,double z0,double z1)
        {return p.x>=x0&&p.x<=x1&&p.z>=z0&&p.z<=z1;}
        private static double VisualEdgeBoxDistance(Vector3 a,Vector3 b,Vector3 p,Vector3 q,Vector3 r,Vector3 t)
        {return System.Math.Min(System.Math.Min(VisualSegmentsDistance(a,b,p,q),VisualSegmentsDistance(a,b,q,r)),System.Math.Min(VisualSegmentsDistance(a,b,r,t),VisualSegmentsDistance(a,b,t,p)));}
        private static double VisualCross(Vector3 a,Vector3 b,Vector3 p)
        {return ((double)b.x-a.x)*((double)p.z-a.z)-((double)b.z-a.z)*((double)p.x-a.x);}
        private static double VisualPointSegmentDistance(Vector3 p,Vector3 a,Vector3 b)
        {
            double dx=(double)b.x-a.x,dz=(double)b.z-a.z,len=dx*dx+dz*dz;
            double t=len==0?0:System.Math.Max(0,System.Math.Min(1,(((double)p.x-a.x)*dx+((double)p.z-a.z)*dz)/len));
            double x=(double)p.x-a.x-t*dx,z=(double)p.z-a.z-t*dz;return x*x+z*z;
        }
        private static double VisualPointTriangleDistance(Vector3 p,Vector3 a,Vector3 b,Vector3 c)
        {
            double x=VisualCross(a,b,p),y=VisualCross(b,c,p),z=VisualCross(c,a,p);
            if(VisualCross(a,b,c)!=0&&((x>=0&&y>=0&&z>=0)||(x<=0&&y<=0&&z<=0)))return 0;
            return System.Math.Min(VisualPointSegmentDistance(p,a,b),System.Math.Min(VisualPointSegmentDistance(p,b,c),VisualPointSegmentDistance(p,c,a)));
        }
        private static double VisualSegmentsDistance(Vector3 a,Vector3 b,Vector3 c,Vector3 d)
        {
            double x=VisualCross(a,b,c),y=VisualCross(a,b,d),z=VisualCross(c,d,a),w=VisualCross(c,d,b);
            if(((x>0&&y<0)||(x<0&&y>0))&&((z>0&&w<0)||(z<0&&w>0)))return 0;
            return System.Math.Min(System.Math.Min(VisualPointSegmentDistance(a,c,d),VisualPointSegmentDistance(b,c,d)),System.Math.Min(VisualPointSegmentDistance(c,a,b),VisualPointSegmentDistance(d,a,b)));
        }
        // Exact solid clearance for the complete telegraphed sweep capsule.
        // Sampling alone can miss a narrow pillar tangent between sample points.
        public static bool HasClearSweepCapsule(Vector3 from,Vector3 to,float radius)
        {
            if(!Finite(radius)||radius<=0||!VisualInsideArena(from,arena-radius)||!VisualInsideArena(to,arena-radius))return false;
            foreach(var obstacle in obstacles)if(!VisualFanClear(from,to,to,obstacle,radius))return false;
            return river==null||HasGroundPath(from,to,radius); // Keep the established river/bridge policy.
        }
        public static bool HasClearVisualFootprint(Vector3 origin, Vector3 point, float radius)
        { return ClearSegment(origin, point, Mathf.Max(.04f,radius), true); }
        public static bool HasLineOfSight(Vector3 from, Vector3 to) { return ClearSegment(from, to, .04f, true); }
        public static bool HasGroundPath(Vector3 from, Vector3 to, float radius = .45f) { return ClearSegment(from, to, radius, false); }
        public static bool CanLeap(Vector3 from, Vector3 to, float radius = .45f)
        {
            return IsWalkable(from, radius) && IsWalkable(to, radius) && ClearSegment(from, to, radius, true);
        }
        // Water may be crossed, but solids and arena edges stop the entire path.
        // Keep the furthest safe landing before that stop rather than reject a
        // useful shorter blink just because the requested endpoint is unsafe.
        public static bool TryResolveBlink(Vector3 from, Vector3 direction, float distance, float radius, float bound, out Vector3 landing)
        {
            landing = CombatFx.Flat(from);
            direction = CombatFx.Flat(direction);
            if (!Finite(landing.x) || !Finite(landing.z) || !Finite(direction.x) || !Finite(direction.z) ||
                !Finite(distance) || !Finite(radius) || !Finite(bound) || distance <= 0 || radius <= 0 || bound <= radius ||
                direction.sqrMagnitude < .0001f || !IsWalkable(landing, radius)) return false;
            Vector3 origin = landing;
            direction.Normalize();
            float safeDistance = PlayerUpgradeRules.FindSafeBlinkDistance(distance,
                travelled =>
                {
                    Vector3 point = origin + direction * travelled;
                    return point.sqrMagnitude <= bound * bound && ClearOfSolids(point, radius);
                },
                travelled => IsWalkable(origin + direction * travelled, radius));
            landing = origin + direction * safeDistance;
            return safeDistance >= .35f;
        }

        // A blocked movement skill still casts: use the last safe point, including the origin.
        public static Vector3 ResolveSkillLanding(Vector3 from, Vector3 direction, float distance, float radius, float bound)
        {
            Vector3 landing;
            if(from.y>.05f)
            {return TryResolvePlatformJump(from,direction,distance,radius,out landing)?landing:from;}
            TryResolveBlink(from,direction,distance,radius,bound,out landing);
            return landing;
        }

        private static bool Finite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }

        private static bool ClearSegment(Vector3 from, Vector3 to, float radius, bool ignoreWater, ObstacleHandle ignored=null)
        {
            int samples = Mathf.Max(1, Mathf.CeilToInt(CombatFx.Flat(to - from).magnitude / .18f));
            for (int i = 0; i <= samples; i++)
            {
                Vector3 p = Vector3.Lerp(from, to, i / (float)samples);
                if(ignoreWater&&(from.y>.05f||to.y>.05f)){if(!ClearAtHeight(p,radius,p.y+.9f))return false;}
                else if (ignoreWater ? !ClearOfSolids(p, radius, ignored) : !IsWalkable(p, radius)) return false;
            }
            return true;
        }
        public static Vector3 Move(Vector3 from, Vector3 delta, float radius = .45f)
        {
            if(from.y>.05f&&CanStand(from,radius))return MoveOnPlatform(from,delta,radius);
            from = CombatFx.Flat(from); delta = CombatFx.Flat(delta);
            if (!IsWalkable(from, radius)) from = NearestWalkable(from, radius);
            int steps = Mathf.Max(1, Mathf.CeilToInt(delta.magnitude / .16f));
            Vector3 step = delta / steps;
            for (int i = 0; i < steps; i++)
            {
                Vector3 next = from + step;
                if (IsWalkable(next, radius)) { from = next; continue; }
                Vector3 alongX = from + new Vector3(step.x, 0, 0);
                if (IsWalkable(alongX, radius)) from = alongX;
                Vector3 alongZ = from + new Vector3(0, 0, step.z);
                if (IsWalkable(alongZ, radius)) from = alongZ;
            }
            return from;
        }
        // Search outward in distance order; never fall back to an obstructed origin.
        public static bool TryReturnPortalPosition(Vector3 from,out Vector3 position,float clearance=2.65f)
        {
            position=Vector3.zero;
            from=NearestWalkable(from,.45f);
            if(IsWalkable(position,clearance)&&CanReach(from,position,.45f))return true;
            for(float distance=.25f;distance<=arena-clearance;distance+=.25f)
            {
                int samples=Mathf.Max(32,Mathf.CeilToInt(distance*2*Mathf.PI/.5f));
                for(int i=0;i<samples;i++)
                {
                    float angle=i*2*Mathf.PI/samples;
                    Vector3 candidate=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*distance;
                    if(!IsWalkable(candidate,clearance)||!CanReach(from,candidate,.45f))continue;
                    position=candidate;return true;
                }
            }
            return false;
        }

        public static Vector3 NearestWalkable(Vector3 point, float radius = .45f)
        {
            point = Vector3.ClampMagnitude(CombatFx.Flat(point), arena - radius - .02f);
            if (IsWalkable(point, radius)) return point;
            for (float distance = .2f; distance <= arena * 2; distance += .25f)
                for (int i = 0; i < 32; i++)
                {
                    float angle = i * Mathf.PI / 16;
                    Vector3 probe = point + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * distance;
                    if (IsWalkable(probe, radius)) return probe;
                }
            return Vector3.zero;
        }

        public sealed class Route
        {
            private List<Vector3> waypoints;
            private int next, builtRevision = -1;
            private Vector3 goal;
            private float refresh;
            public Vector3 Direction(Vector3 from, Vector3 destination, float radius = .45f)
            {
                destination = NearestWalkable(destination, radius);
                if (ClearSegment(from, destination, radius, false)) return CombatFx.Flat(destination - from).normalized;
                if (waypoints == null || builtRevision != revision || Time.time >= refresh || (destination - goal).sqrMagnitude > 6.25f)
                {
                    waypoints = FindPath(from, destination, radius); next = 0; goal = destination; builtRevision = revision; refresh = Time.time + .85f;
                }
                while (next < waypoints.Count && CombatFx.Flat(waypoints[next] - from).sqrMagnitude < .22f)
                {
                    if (next + 1 >= waypoints.Count) { if (CombatFx.Flat(waypoints[next] - from).sqrMagnitude < .01f) next++; break; }
                    if (!ClearSegment(from, waypoints[next + 1], radius, false)) break;
                    next++;
                }
                if (next >= waypoints.Count) return Vector3.zero;
                if (!ClearSegment(from, waypoints[next], radius, false))
                {
                    waypoints = FindPath(from, destination, radius); next = 0;
                    if (waypoints.Count == 0) return Vector3.zero;
                }
                if (next + 1 < waypoints.Count && ClearSegment(from, waypoints[next + 1], radius, false)) next++;
                return CombatFx.Flat(waypoints[next] - from).normalized;
            }
        }
        public static bool CanReach(Vector3 from,Vector3 target,float radius=.65f)
        { return IsWalkable(from,radius) && IsWalkable(target,radius) && FindPath(from,target,radius).Count>0; }
        private static Vector3 Point(int index) { return new Vector3(GridOrigin + index % Side * Cell, 0, GridOrigin + index / Side * Cell); }
        private static int NearestNode(Vector3 point, bool[] grid, float radius)
        {
            int cx = Mathf.Clamp(Mathf.RoundToInt((point.x - GridOrigin) / Cell), 0, Side - 1);
            int cy = Mathf.Clamp(Mathf.RoundToInt((point.z - GridOrigin) / Cell), 0, Side - 1);
            int best = -1; float distance = float.MaxValue;
            for (int y = Mathf.Max(0, cy - 2); y <= Mathf.Min(Side - 1, cy + 2); y++)
                for (int x = Mathf.Max(0, cx - 2); x <= Mathf.Min(Side - 1, cx + 2); x++)
                {
                    int node = y * Side + x;
                    float d = (Point(node) - point).sqrMagnitude;
                    if (grid[node] && d < distance && ClearSegment(point, Point(node), radius, false)) { best = node; distance = d; }
                }
            return best;
        }
        public static List<Vector3> FindPath(Vector3 from, Vector3 target, float radius = .45f)
        {
            from = NearestWalkable(from, radius); target = NearestWalkable(target, radius);
            var path = new List<Vector3>();
            if (ClearSegment(from, target, radius, false)) { path.Add(target); return path; }
            int key = Mathf.CeilToInt(radius * 20);
            float gridRadius = key / 20f;
            bool[] grid;
            if (!grids.TryGetValue(key, out grid))
            {
                grid = new bool[Side * Side];
                for (int i = 0; i < grid.Length; i++) grid[i] = IsWalkable(Point(i), gridRadius);
                grids[key] = grid;
            }
            int start = NearestNode(from, grid, radius), end = NearestNode(target, grid, radius);
            if (start < 0 || end < 0) return path;
            float[] cost = new float[grid.Length]; int[] parent = new int[grid.Length]; bool[] closed = new bool[grid.Length];
            for (int i = 0; i < cost.Length; i++) { cost[i] = float.MaxValue; parent[i] = -1; }
            var queue = new Heap(); cost[start] = 0; queue.Add(start, 0);
            while (queue.Count > 0)
            {
                int node = queue.Pop(); if (closed[node]) continue; closed[node] = true;
                if (node == end)
                {
                    for (int n = end; n >= 0; n = parent[n]) { path.Add(Point(n)); if (n == start) break; }
                    path.Reverse(); path.Add(target); return path;
                }
                int x = node % Side, y = node / Side;
                for (int i = 0; i < 8; i++)
                {
                    int nx = x + NeighborX[i], ny = y + NeighborY[i];
                    if (nx < 0 || ny < 0 || nx >= Side || ny >= Side) continue;
                    int candidate = ny * Side + nx;
                    if (!grid[candidate] || closed[candidate]) continue;
                    bool diagonal = NeighborX[i] != 0 && NeighborY[i] != 0;
                    if (diagonal && (!grid[y * Side + nx] || !grid[ny * Side + x])) continue;
                    if (!ClearSegment(Point(node), Point(candidate), radius, false)) continue;
                    float nextCost = cost[node] + (diagonal ? 1.414214f : 1f);
                    if (nextCost >= cost[candidate]) continue;
                    cost[candidate] = nextCost; parent[candidate] = node;
                    queue.Add(candidate, nextCost + Vector3.Distance(Point(candidate), Point(end)) / Cell);
                }
            }
            return path;
        }
        private sealed class Heap
        {
            private struct Entry { public int Node; public float Score; }
            private readonly List<Entry> items = new List<Entry>();
            public int Count { get { return items.Count; } }
            public void Add(int node, float score)
            {
                var value = new Entry { Node = node, Score = score }; items.Add(value); int i = items.Count - 1;
                while (i > 0) { int p = (i - 1) / 2; if (items[p].Score <= score) break; items[i] = items[p]; i = p; }
                items[i] = value;
            }
            public int Pop()
            {
                int result = items[0].Node; Entry tail = items[items.Count - 1]; items.RemoveAt(items.Count - 1);
                if (items.Count == 0) return result;
                int i = 0;
                while (i * 2 + 1 < items.Count)
                {
                    int child = i * 2 + 1;
                    if (child + 1 < items.Count && items[child + 1].Score < items[child].Score) child++;
                    if (items[child].Score >= tail.Score) break;
                    items[i] = items[child]; i = child;
                }
                items[i] = tail; return result;
            }
        }
    }
}
