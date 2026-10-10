using System;
using System.Collections.Generic;
using Emberfall;
using UnityEngine;
public static class ChapterFormationGeometryTests
{
    public static string Run()
    {
        int n=0;Action<bool,string> check=(ok,msg)=>{n++;if(!ok)throw new Exception(msg);};
        foreach(int seed in new[]{0,1,2,3,17,64,255,256,int.MinValue,int.MaxValue})for(int room=0;room<2;room++)
        {
            var p=ChapterRoomGeometry.Plan(ChapterNode.Redrock,room,seed);
            WorldTraversal.Reset(ZoneKind.Dungeon);ChapterRoomGeometry.Register(p);
            for(int side=-1;side<=1;side+=2)WorldTraversal.AddDynamicCircle(new Vector3(side*13.3f,0,2.2f),.7f);
            var occupied=new List<Vector3>();
            var desired=new[]{p.SpawnCandidates[room==0?0:2],p.SpawnCandidates[1],new Vector3(-2,0,11),new Vector3(2,0,11),p.SpawnCandidates[4],p.SpawnCandidates[5]};
            for(int i=0;i<desired.Length;i++)
            {
                Vector3 at,again;
                check(ChapterRoomGeometry.TrySpawnAt(p,desired[i],occupied,.65f,out at),"all six formation positions resolve with real navigation");
                check(ChapterRoomGeometry.TrySpawnAt(p,desired[i],occupied,.65f,out again)&&Vector3.Distance(at,again)<.0001f,"requested formation remains deterministic");
                check(WorldTraversal.CanReach(p.Entrance,at,.65f)&&Vector3.Distance(p.Entrance,at)>=5.5f,"role placement keeps safe arrival and route");
                if(i==1)check(at.x*occupied[0].x<0,"two ranged attackers occupy opposite branches");
                if(i==2||i==3)check(Vector3.Distance(at,new Vector3(0,0,11))<3,"two guardian placements actually guard exit region");
                foreach(var other in occupied)check(Vector3.Distance(at,other)>=2.4f,"role placement cannot stack actors");
                occupied.Add(at);
            }
            Vector3 first,second;
            check(ChapterRoomGeometry.TrySpawn(p,0,new List<Vector3>(),.65f,out first)&&ChapterRoomGeometry.TrySpawnAt(p,p.SpawnCandidates[0],new List<Vector3>(),.65f,out second)&&Vector3.Distance(first,second)<.0001f,"indexed API delegates without changing old placement");
            for(int i=6;i<10;i++){check(ChapterRoomGeometry.TrySpawn(p,i,occupied,.65f,out first),"additional roster positions resolve");foreach(var other in occupied)check(Vector3.Distance(first,other)>=2.4f,"additional actors preserve spacing");occupied.Add(first);}
            check(!ChapterRoomGeometry.TrySpawn(p,10,occupied,.65f,out first),"indexed roster capped at ten");
            check(!ChapterRoomGeometry.TrySpawnAt(p,new Vector3(float.NaN,0,0),occupied,.65f,out first),"invalid requested point rejected");
        }
        return "PASS: "+n+" chapter formation production navigation assertions (managed shims, not combat AI playback)";
    }
}
