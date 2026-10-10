using System;
using System.Collections.Generic;
using Emberfall;
using UnityEngine;
public static class ChapterRoomGeometryTests
{
    static int checks;
    static void Check(bool ok,string text){checks++;if(!ok)throw new Exception(text);}
    static void Register(ChapterRoomPlan plan,bool rubble=true)
    {
        WorldTraversal.Reset(ZoneKind.Dungeon);ChapterRoomGeometry.Register(plan);
        if(rubble)for(int side=-1;side<=1;side+=2)WorldTraversal.AddDynamicCircle(new Vector3(side*13.3f,0,2.2f),.7f);
    }
    static void ReplayRoutes()
    {
        foreach(int legacy in new[]{0,1,2,3,4,999999,int.MinValue,int.MaxValue})
            Check(!ChapterRoomGeometry.RedrockSplitRoute(legacy),"legacy seed has no replay format marker");
        for(int raw=0;raw<64;raw++)
        {
            int split=ChapterRoomGeometry.RedrockReplaySeed(raw,true),solid=ChapterRoomGeometry.RedrockReplaySeed(raw,false);
            Check((split&255)==(raw&255)&&(solid&255)==(raw&255),"route flag preserves entire spawn jitter byte including mirror and Forest formation");
            Check(ChapterRoomGeometry.RedrockReplaySeed(split,true)==split,"encoded seed idempotent replay");
            foreach(ChapterNode node in Enum.GetValues(typeof(ChapterNode)))for(int room=0;room<2;room++)
            {
                var old=ChapterRoomGeometry.Plan(node,room,raw);var chosen=ChapterRoomGeometry.Plan(node,room,split);
                if(node==ChapterNode.Redrock&&room==1)continue;
                Check(old.Obstacles.Length==chosen.Obstacles.Length,"replay route scoped to Redrock room two");
                for(int i=0;i<old.Obstacles.Length;i++)Check(Vector3.Distance(old.Obstacles[i].Center,chosen.Obstacles[i].Center)<.001f&&old.Obstacles[i].Size.x==chosen.Obstacles[i].Size.x&&old.Obstacles[i].Size.y==chosen.Obstacles[i].Size.y&&old.Obstacles[i].Radius==chosen.Obstacles[i].Radius,"all other room footprints unchanged");
            }
            var plan=ChapterRoomGeometry.Plan(ChapterNode.Redrock,1,split);
            var again=ChapterRoomGeometry.FromLayout(plan.Layout,split);
            Check(plan.Obstacles.Length==4&&again.Obstacles.Length==4,"saved seed reconstructs split geometry without profile");
            Register(ChapterRoomGeometry.Plan(ChapterNode.Redrock,1,solid));
            Check(!WorldTraversal.HasLineOfSight(plan.SpawnCandidates[1],plan.SpawnCandidates[2]),"continuous wall separates existing ranged branches");
            Register(plan);
            Check(WorldTraversal.HasLineOfSight(plan.SpawnCandidates[1],plan.SpawnCandidates[2]),"SPLIT_CROSSING existing crossfire branches now share sight across passage");
            foreach(float radius in new[]{.45f,.65f,.9f,1.3f})
            {
                var left=new Vector3(-4,0,-1);var right=new Vector3(4,0,-1);
                Register(ChapterRoomGeometry.Plan(ChapterNode.Redrock,1,solid));
                Check(!WorldTraversal.HasLineOfSight(left,right)&&!WorldTraversal.HasGroundPath(left,right,radius),"continuous wall blocks cross passage LOS and motion");
                var detour=WorldTraversal.FindPath(left,right,radius);float length=0;var previous=left;
                foreach(var point in detour){length+=Vector3.Distance(previous,point);previous=point;}
                Check(detour.Count>1&&length>12,"continuous wall requires a substantial real detour");
                Register(plan);
                Check(WorldTraversal.HasLineOfSight(left,right)&&WorldTraversal.HasGroundPath(left,right,radius)&&WorldTraversal.FindPath(left,right,radius).Count==1,"SPLIT_CROSSING opens direct movement and ranged sight for every actor radius");
                foreach(var target in new List<Vector3>(plan.Objectives){plan.Exit})Check(WorldTraversal.IsWalkable(plan.Entrance,radius)&&WorldTraversal.IsWalkable(target,radius)&&WorldTraversal.CanReach(plan.Entrance,target,radius),"split entrance objectives exit safe and reachable");
                for(int x=-12;x<=12;x+=3)for(int z=-12;z<=12;z+=3)
                {var point=new Vector3(x,0,z);if(WorldTraversal.IsWalkable(point,radius))Check(WorldTraversal.CanReach(plan.Entrance,point,radius),"split map has no isolated usable sample");}
                var occupied=new List<Vector3>();
                var desired=new[]{plan.SpawnCandidates[2],plan.SpawnCandidates[1],new Vector3(-2,0,11),new Vector3(2,0,11),plan.SpawnCandidates[4],plan.SpawnCandidates[5]};
                for(int i=0;i<6;i++)
                {
                    Vector3 at,replay;
                    Check(ChapterRoomGeometry.TrySpawnAt(plan,desired[i],occupied,radius,out at),"six crossfire positions resolve at every radius");
                    Check(ChapterRoomGeometry.TrySpawnAt(again,desired[i],occupied,radius,out replay)&&Vector3.Distance(at,replay)<.0001f,"fixed seed reproduces actual spawn positions");
                    Check(WorldTraversal.CanReach(plan.Entrance,at,radius)&&Vector3.Distance(plan.Entrance,at)>=5.5f,"split spawns preserve safe arrival and reachable target");
                    foreach(var other in occupied)Check(Vector3.Distance(at,other)>=Mathf.Max(2.4f,radius*2),"split spawns retain actor clearance");
                    occupied.Add(at);
                }
                for(int i=6;i<10;i++){Vector3 extra;Check(ChapterRoomGeometry.TrySpawn(plan,i,occupied,radius,out extra),"expanded split roster resolves");occupied.Add(extra);}
                Vector3 eleventh;Check(!ChapterRoomGeometry.TrySpawn(plan,10,occupied,radius,out eleventh),"split roster cap is ten");
                // Existing Heroic heat line cannot seal the new passage plus both old side routes.
                float half=ChapterHazardGeometry.HeatHalfWidth;
                WorldTraversal.AddBox((plan.HazardStart+plan.HazardEnd)*.5f,new Vector2(Mathf.Abs(plan.HazardEnd.x-plan.HazardStart.x)+2*half,2*half));
                Check(WorldTraversal.CanReach(plan.Entrance,plan.Objectives[0],radius),"heroic heat envelope preserves a safe route");
            }
        }
    }
    public static string Run()
    {
        checks=0;
        ReplayRoutes();
        foreach(ChapterNode node in Enum.GetValues(typeof(ChapterNode)))for(int room=0;room<2;room++)for(int seed=0;seed<2;seed++)
        {
            var p=ChapterRoomGeometry.Plan(node,room,seed);Register(p);
            Check(ChapterRoomGeometry.FromLayout(p.Layout,seed).Node==node,"chapter layout roundtrip");
            foreach(float radius in new[]{.45f,.65f,.9f,1.3f})
            {
                var previous=p.Entrance;
                foreach(var target in new List<Vector3>(p.Objectives){p.Exit})
                {
                    Check(WorldTraversal.IsWalkable(target,radius)&&WorldTraversal.CanReach(previous,target,radius),node+" room "+room+" target "+target.x+","+target.z+" radius "+radius);
                    var path=WorldTraversal.FindPath(previous,target,radius);
                    for(int i=1;i<path.Count;i++)Check(WorldTraversal.HasGroundPath(path[i-1],path[i],radius),"real routed segment walkable");
                    previous=target;
                }
                for(int x=-12;x<=12;x+=3)for(int z=-12;z<=12;z+=3)
                {var sample=new Vector3(x,0,z);if(WorldTraversal.IsWalkable(sample,radius))Check(WorldTraversal.CanReach(p.Entrance,sample,radius),"no isolated usable region");}
            }
            Check(node==ChapterNode.StarPlatform?WorldTraversal.HasGroundPath(p.Entrance,p.Exit,.65f):!WorldTraversal.HasGroundPath(p.Entrance,p.Exit,.65f),"negative control: forest/mine require bypass, star center stays open");
            if(node==ChapterNode.StarPlatform)
            {
                foreach(var center in new[]{new Vector3(0,0,1),new Vector3(-1,0,1),new Vector3(1,0,2)})for(int angle=0;angle<360;angle+=15)
                {
                    float a=angle*Mathf.PI/180;var anchor=center+new Vector3(Mathf.Sin(a)*2.6f,0,Mathf.Cos(a)*2.6f);
                    Check(WorldTraversal.IsWalkable(anchor,.55f)&&WorldTraversal.HasGroundPath(center,anchor,.15f),"existing near-core boss anchor candidates fit clear star center");
                    Check(WorldTraversal.CanReach(p.Entrance,anchor,.7f),"melee companion can approach anchor candidate");
                }
            }
            else
            {
                foreach(float radius in new[]{.45f,.65f,.9f,1.3f})for(int clipped=0;clipped<2;clipped++)
                {
                    Register(p);
                    if(node==ChapterNode.ForestCourt)WorldTraversal.AddCircle(p.HazardCenter,ArenaPulseRules.Radius);
                    else
                    {
                        Vector3 end=clipped==0?p.HazardEnd:ChapterHazardGeometry.ClipLine(p.HazardStart,p.HazardEnd);
                        if(clipped==1)Check(Vector3.Distance(p.HazardStart,end)<Vector3.Distance(p.HazardStart,p.HazardEnd),"mine wall clips real heat line");
                        // Conservative axis-aligned capsule envelope includes both end caps;
                        // WorldTraversal adds the actor radius to the complete hazard footprint.
                        float half=ChapterHazardGeometry.HeatHalfWidth;
                        WorldTraversal.AddBox((p.HazardStart+end)*.5f,new Vector2(Mathf.Abs(end.x-p.HazardStart.x)+half*2,half*2));
                    }
                    var from=p.Entrance;foreach(var target in new List<Vector3>(p.Objectives){p.Exit})
                    {Check(WorldTraversal.CanReach(from,target,radius),"full/clipped hazard safe detour "+node+" radius "+radius);from=target;}
                }
            }
        }
        foreach(ChapterNode node in Enum.GetValues(typeof(ChapterNode)))for(int room=0;room<2;room++)for(int seed=0;seed<64;seed++)
        {
            var p=ChapterRoomGeometry.Plan(node,room,seed);Register(p);var occupied=new List<Vector3>();
            for(int i=0;i<10;i++)
            {
                float radius=node==ChapterNode.StarPlatform&&i==0?1.3f:.65f;
                Vector3 first,second;Check(ChapterRoomGeometry.TrySpawn(p,i,occupied,radius,out first),"ten bounded safe spawns "+node+" seed "+seed+" index "+i);
                Check(ChapterRoomGeometry.TrySpawn(p,i,occupied,radius,out second)&&Vector3.Distance(first,second)<.0001f,"same seed reproduces spawn");
                Check(Vector3.Distance(first,p.Entrance)>=5.5f&&WorldTraversal.CanReach(p.Entrance,first,.45f),"safe arrival distance and every class can reach enemy");
                bool stance=false;for(int j=0;j<8;j++){float a=j*Mathf.PI/4;var near=first+new Vector3(Mathf.Sin(a),0,Mathf.Cos(a))*1.4f;if(WorldTraversal.CanReach(p.Entrance,near,.7f)&&WorldTraversal.HasGroundPath(near,first,.12f)){stance=true;break;}}
                Check(stance,"large melee companion has attack stance");foreach(var other in occupied)Check(Vector3.Distance(first,other)>=2.4f,"spawns separated");occupied.Add(first);
            }
            Vector3 rejected;Check(!ChapterRoomGeometry.TrySpawn(p,10,occupied,.65f,out rejected),"eleventh spawn rejected");
            Check(!ChapterRoomGeometry.TrySpawn(p,0,occupied,float.NaN,out rejected),"invalid radius rejected");
        }
        foreach(var node in new[]{ChapterNode.ForestCourt,ChapterNode.Redrock})
        {
            var p=ChapterRoomGeometry.Plan(node,0,0);
            WorldTraversal.Reset(ZoneKind.Dungeon);TacticalRoomGeometry.Register(p.Layout);
            Check(WorldTraversal.HasGroundPath(p.Entrance,p.Exit,.65f),"negative control: legacy >=20 dispatch has no chapter obstruction");
            Register(p);Check(!WorldTraversal.HasGroundPath(p.Entrance,p.Exit,.65f),"new chapter geometry actually changes the route");
        }
        var signatures=new HashSet<string>();
        for(int mask=0;mask<8;mask++)
        {
            var pieces=ChapterRoomGeometry.StarMapPieces(mask);string signature="";
            Check(pieces.Length>=15&&pieces.Length<=18,"star map reuses bounded small pieces");
            for(int node=0;node<3;node++)
            {
                int arcs=0,stars=0;bool recovered=(mask&(1<<node))!=0;
                foreach(var piece in pieces)if(piece.Node==node)
                {
                    Check(piece.Repaired==recovered&&piece.Position.magnitude<.9f,"node mask affects only its own fitted sector");
                    if(piece.Star)stars++;else{arcs++;Check(recovered?Math.Abs(piece.Position.z)<.0001f:Math.Abs(piece.Position.z)>.07f,"repair rejoins displaced geometry, not just recoloring");}
                }
                Check(stars==1&&arcs==(recovered?5:4),"repaired sector fills missing arc and retains one star");
                signature+=arcs+":";
            }
            Check(signatures.Add(signature),"every completion mask has a distinct repaired silhouette");
        }
        Check(!ChapterRoomGeometry.IsChapterLayout(25)&&!ChapterRoomGeometry.IsChapterLayout(99)&&!ChapterRoomGeometry.IsChapterLayout(106),"old and unknown layouts not captured");
        return "PASS: "+checks+" chapter production geometry/navigation/spawn checks (managed shims, not gameplay or rendering)";
    }
}
