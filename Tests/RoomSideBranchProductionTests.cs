using System;
using Emberfall;
using UnityEngine;
public static class RoomSideBranchProductionTests
{
    private static int checks,started;
    private static void Check(bool ok,string reason){checks++;if(!ok)throw new Exception(reason);}
    private static void Complete(GameSession session)
    {
        var plan=session.RoomChainRun.Room;
        foreach(var enemy in session.Enemies)session.Kill(enemy);
        if(plan.Objective==RoomObjective.Purify)
            for(int seal=0;seal<2;seal++)
            {session.Player.Teleport(session.Markers[seal].transform.position);for(int tick=0;tick<12;tick++)session.Tick();}
        else if(plan.Objective==RoomObjective.Escape)
        {session.Player.Teleport(new Vector3(0,0,11));for(int tick=0;tick<16;tick++)session.Tick();}
    }
    private static void Side(GameSession session,int seed)
    {
        if(session.RoomChainRun.Room.Index!=RoomTactics.EventRoom(seed))return;
        var room=session.RoomChainRun.Room;var branch=session.RoomChainRun.SelectedBranch;
        int enemies=session.Enemies.Count;
        session.Player.Teleport(new Vector3(-RoomTactics.Mirror(seed)*12,0,-6));
        Check(session.StartSideEvent(),"reachable optional crystal starts both enemies");started++;
        Check(session.Enemies.Count==enemies+2,"side encounter creates exactly two enemies");
        var guard=session.Enemies[enemies];var wisp=session.Enemies[enemies+1];
        Check(Vector3.Distance(guard.transform.position,wisp.transform.position)>=2.5f,"reserved side spawn respects pair clearance");
        Check(Vector3.Distance(guard.transform.position,session.Player.transform.position)>=5.5f&&Vector3.Distance(wisp.transform.position,session.Player.transform.position)>=5.5f,"side enemies retain player safety distance");
        Check(WorldTraversal.CanReach(TacticalRoomGeometry.Entrance,guard.transform.position,.65f)&&WorldTraversal.CanReach(TacticalRoomGeometry.Entrance,wisp.transform.position,.5f),"side enemies reachable with actual traversal");
        Check(!session.StartSideEvent()&&session.Enemies.Count==enemies+2,"repeated crystal click creates no duplicate enemies");
        Check(ReferenceEquals(room,session.RoomChainRun.Room)&&branch==session.RoomChainRun.SelectedBranch&&!session.RoomBranchChoiceOpen,"side start cannot advance room or select branch");
    }
    public static void Main()
    {
        for(int seed=0;seed<54;seed++)foreach(int action in new[]{0,1,2})
        {
            var session=new GameSession(seed);
            Check(session.DungeonTier==15&&session.ChallengeRun,"tier fifteen limited-healing entry");
            for(int room=0;room<2;room++)
            {
                if(action==1)Side(session,seed);
                Complete(session);
                if(action==2)Side(session,seed);
                session.Tick();session.Tick();
                if(room==0)Check(session.ConfirmBlessingForTest(0),"first-room blessing remains available");
                session.Player.Teleport(new Vector3(0,0,14));session.EnterNextRoom();
                if(room==0)Check(!session.SideStillActiveForTest,"completed room transition abandons side state");
            }
            session.CancelRoomBranchChoice();session.EnterNextRoom();
            Check(session.ConfirmRoomBranch(RoomBranch.Seal)&&!session.RoomChainRun.Failed,"direct or optional-event path enters seal corridor after cancel/reopen");
            Check(!session.SideStillActiveForTest,"confirmed branch transition abandons second-room side state");
            Check(session.RoomChainRun.Room.Index==2&&session.Enemies.Count==4,"third-room seal roster remains four");
            if(action!=0)Side(session,seed);
        }
        Console.WriteLine("PASS: "+checks+" side/branch assertions; 162 tier-15 limited-healing paths, "+started+" optional encounters; managed geometry and scene boundaries, not Unity");
    }
}
namespace Emberfall
{
    public sealed partial class GameSession
    {
        public bool ConfirmBlessingForTest(int index)=>ConfirmRoomInterlude(index);
        public bool SideStillActiveForTest=>sideEventRun!=null||sideEventStarted||sideEventEnemies.Count!=0;
    }
}
