using System;
using System.Collections.Generic;
using UnityEngine;
namespace Emberfall
{
    public struct ChapterObstacle
    {
        public readonly Vector3 Center;
        public readonly Vector2 Size;
        public readonly float Radius,Height;
        public ChapterObstacle(Vector3 center,float radius,float height){Center=center;Radius=radius;Height=height;Size=new Vector2(0,0);}
        public ChapterObstacle(Vector3 center,Vector2 size,float height){Center=center;Size=size;Height=height;Radius=0;}
    }
    public sealed class ChapterRoomPlan
    {
        public readonly ChapterNode Node;
        public readonly int Room,Seed,Layout;
        public readonly Vector3 Entrance=new Vector3(0,0,-12),Exit=new Vector3(0,0,14);
        public readonly Vector3[] Objectives,SpawnCandidates;
        public readonly ChapterObstacle[] Obstacles;
        public readonly Vector3 HazardCenter,HazardStart,HazardEnd;
        internal ChapterRoomPlan(ChapterNode node,int room,int seed,ChapterObstacle[] obstacles,Vector3[] objectives,Vector3[] spawns,int mirror)
        {
            Node=node;Room=room;Seed=seed;Layout=100+(int)node*2+(node==ChapterNode.StarPlatform?1:room);
            Obstacles=obstacles;Objectives=objectives;SpawnCandidates=spawns;
            HazardCenter=new Vector3(-mirror*7,0,-5);
            // Cross the inner, short approach to the mirrored hunt target; outer rail stays a safe detour.
            HazardStart=new Vector3(mirror*2.7f,0,-4);HazardEnd=new Vector3(mirror*5.8f,0,-4);
        }
    }
    public struct ChapterStarMapPiece
    {
        public readonly int Node;
        public readonly bool Star,Repaired;
        public readonly Vector3 Position;
        public readonly float Angle;
        public ChapterStarMapPiece(int node,bool star,bool repaired,Vector3 position,float angle)
        {Node=node;Star=star;Repaired=repaired;Position=position;Angle=angle;}
    }
    public static class ChapterRoomGeometry
    {
        public const int MaximumEnemies=6;
        // Versioned replay marker: old generated seeds (0..999999) stay legacy.
        // Bit 20 is outside old generated seeds and the low-byte spawn jitter.
        // Mirror bit 0 and Forest formation bit 1 remain independent.
        private const int RedrockRouteBit=1<<20;
        public static int RedrockReplaySeed(int seed,bool split)
        {return (((seed&0x00ffffff)|0x24000000)&~RedrockRouteBit)|(split?RedrockRouteBit:0);}
        public static bool RedrockSplitRoute(int seed)
        {return (seed&0x7f000000)==0x24000000&&(seed&RedrockRouteBit)!=0;}

        public static ChapterStarMapPiece[] StarMapPieces(int mask)
        {
            var pieces=new List<ChapterStarMapPiece>();mask&=7;
            for(int node=0;node<3;node++)
            {
                bool repaired=(mask&(1<<node))!=0;
                for(int segment=0;segment<5;segment++)
                {
                    if(!repaired&&segment==2)continue;
                    float degrees=node*120+segment*20+10,a=degrees*Mathf.PI/180;
                    pieces.Add(new ChapterStarMapPiece(node,false,repaired,
                        new Vector3(Mathf.Cos(a)*.76f,Mathf.Sin(a)*.76f,repaired?0:segment%2==0?.10f:-.08f),
                        degrees+90+(repaired?0:segment%2==0?12:-12)));
                }
                float starAngle=(node*120+50)*Mathf.PI/180;
                pieces.Add(new ChapterStarMapPiece(node,true,repaired,new Vector3(Mathf.Cos(starAngle)*.45f,Mathf.Sin(starAngle)*.45f,0),0));
            }
            return pieces.ToArray();
        }

        // Roster choice is bit 1; mirror remains independent bit 0. Counts/kinds are unchanged.
        public static Vector3 ForestMobileSpawn(int seed,int index)
        {
            if(index<0||index>=6)throw new ArgumentOutOfRangeException(nameof(index));
            int mirror=(seed&1)==0?1:-1;
            var points=new[]{new Vector3(-6,0,7),new Vector3(-8,0,1),new Vector3(-5,0,4.5f),
                new Vector3(5,0,-4),new Vector3(8,0,1),new Vector3(-8.5f,0,6.5f)};
            Vector3 point=points[index];point.x*=mirror;return point;
        }

        public static bool IsChapterLayout(int layout){return layout>=100&&layout<=105;}
        public static ChapterRoomPlan Plan(ChapterNode node,int room,int seed)
        {
            if((int)node<0||(int)node>2)throw new ArgumentOutOfRangeException(nameof(node));
            if(room<0||room>1)throw new ArgumentOutOfRangeException(nameof(room));
            int mirror=(seed&1)==0?1:-1;
            var obstacles=new List<ChapterObstacle>();
            if(node==ChapterNode.ForestCourt)
            {
                obstacles.Add(new ChapterObstacle(Vector3.zero,3.4f,1.25f));
                obstacles.Add(new ChapterObstacle(new Vector3(-mirror*8,0,4),1.05f,2.3f));
                obstacles.Add(new ChapterObstacle(new Vector3(mirror*8,0,-4),1.05f,2.3f));
            }
            else if(node==ChapterNode.Redrock)
            {
                if(room==1&&RedrockSplitRoute(seed))
                {
                    // Four-metre cross passage; both hoist supports remain on solid wall.
                    obstacles.Add(new ChapterObstacle(new Vector3(0,0,-5),new Vector2(4,4),1.8f));
                    obstacles.Add(new ChapterObstacle(new Vector3(0,0,3),new Vector2(4,4),1.8f));
                }
                else obstacles.Add(new ChapterObstacle(new Vector3(0,0,-1),new Vector2(4,12),1.8f));
                obstacles.Add(new ChapterObstacle(new Vector3(-mirror*8,0,3),new Vector2(5,1.4f),1.45f));
                obstacles.Add(new ChapterObstacle(new Vector3(mirror*8,0,-4),new Vector2(5,1.4f),1.45f));
            }
            else
                for(int spoke=0;spoke<3;spoke++)for(int step=0;step<2;step++)
                {
                    float angle=(30+spoke*120)*(Mathf.PI/180f),distance=9+step*3;
                    obstacles.Add(new ChapterObstacle(new Vector3(Mathf.Sin(angle)*distance,0,Mathf.Cos(angle)*distance),1.05f,1.6f+step*.4f));
                }
            Vector3[] objectives=node==ChapterNode.ForestCourt&&room==0?new[]{new Vector3(-mirror*8,0,1),new Vector3(mirror*8,0,1)}:
                node==ChapterNode.Redrock&&room==0?new[]{new Vector3(mirror*8,0,7)}:
                node==ChapterNode.StarPlatform?new[]{new Vector3(0,0,1)}:new[]{new Vector3(0,0,11)};
            Vector3[] spawns={node==ChapterNode.StarPlatform?new Vector3(0,0,1):node==ChapterNode.Redrock&&room==0?objectives[0]:new Vector3(0,0,8),
                new Vector3(-mirror*8,0,-1),new Vector3(mirror*8,0,2),new Vector3(-mirror*7,0,9),new Vector3(mirror*10,0,7),new Vector3(mirror*5,0,-4)};
            if(node==ChapterNode.ForestCourt&&room==0)
            {
                spawns[0]=new Vector3(-mirror*5.8f,0,3); // Supplier sees the left seal guard within the real six-metre range.
                spawns[1]=new Vector3(-mirror*8,0,1);
                spawns[4]=new Vector3(mirror*8,0,3); // Opposite guard has no cross-island supply LOS.
            }
            return new ChapterRoomPlan(node,room,seed,obstacles.ToArray(),objectives,spawns,mirror);
        }
        public static ChapterRoomPlan FromLayout(int layout,int seed)
        {if(!IsChapterLayout(layout))throw new ArgumentOutOfRangeException(nameof(layout));return Plan((ChapterNode)((layout-100)/2),(layout-100)%2,seed);}
        public static void Register(ChapterRoomPlan plan)
        {
            foreach(var obstacle in plan.Obstacles)
                if(obstacle.Radius>0)WorldTraversal.AddCircle(obstacle.Center,obstacle.Radius);
                else WorldTraversal.AddBox(obstacle.Center,obstacle.Size);
        }
        public static bool TrySpawn(ChapterRoomPlan plan,int index,List<Vector3> occupied,float radius,out Vector3 point)
        {
            point=Vector3.zero;
            if(plan==null||index<0||index>=10)return false;
            if(index>=MaximumEnemies){float angle=index*2.39996f+((plan.Layout-100)%2)*.5f;return TrySpawnAt(plan,new Vector3(Mathf.Sin(angle)*10,0,Mathf.Cos(angle)*9+2),occupied,radius,out point);}
            return TrySpawnAt(plan,plan.SpawnCandidates[index],occupied,radius,out point);
        }
        // Formation hosts can request a role position while retaining the exact shared
        // arrival, reachability, radius and spacing checks used by normal room spawns.
        public static bool TrySpawnAt(ChapterRoomPlan plan,Vector3 desired,List<Vector3> occupied,float radius,out Vector3 point)
        {
            point=Vector3.zero;
            if(plan==null||float.IsNaN(radius)||float.IsInfinity(radius)||radius<=0||radius>1.3f||
                float.IsNaN(desired.x)||float.IsNaN(desired.y)||float.IsNaN(desired.z)||
                float.IsInfinity(desired.x)||float.IsInfinity(desired.y)||float.IsInfinity(desired.z))return false;
            for(int attempt=0;attempt<64;attempt++)
            {
                float a=(attempt*137.50776f+(plan.Seed&255))*(Mathf.PI/180f);
                Vector3 candidate=attempt==0?desired:desired+new Vector3(Mathf.Sin(a),0,Mathf.Cos(a))*(.35f+attempt*.11f);
                if(candidate.magnitude>16.25f||Vector3.Distance(candidate,plan.Entrance)<5.5f||!WorldTraversal.IsWalkable(candidate,radius)||!WorldTraversal.CanReach(plan.Entrance,candidate,radius))continue;
                bool crowded=false;if(occupied!=null)foreach(var other in occupied)if(Vector3.Distance(candidate,other)<Mathf.Max(2.4f,radius*2)){crowded=true;break;}
                if(crowded)continue;point=candidate;return true;
            }
            return false;
        }
    }
}
