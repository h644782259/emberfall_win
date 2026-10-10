using System.Collections.Generic;
using UnityEngine;
namespace Emberfall
{
    public static class EscapeRoomFormation
    {
        public static EscapeRole Role(int index)
        {return index==0?EscapeRole.GateSupplier:index==1||index==3?EscapeRole.GateGuard:index==2||index==4?EscapeRole.Pursuer:EscapeRole.SideFlanker;}
        public static Vector3 Desired(int seed,int index)
        {
            int side=RoomTactics.Mirror(seed);
            switch(index){case 0:return new Vector3(0,0,7);case 1:return new Vector3(-2,0,11);case 3:return new Vector3(2,0,11);
                case 2:return new Vector3(-side*7,0,3);case 4:return new Vector3(side*7,0,6);default:return new Vector3(side*11,0,10);}
        }
        public static bool TrySpawn(int seed,int index,List<Vector3> occupied,out Vector3 point)
        {
            if(index>=6)return TacticalRoomGeometry.TrySpawn(seed,1,index,occupied,out point);
            Vector3 desired=Desired(seed,index);
            for(int attempt=0;attempt<40;attempt++)
            {
                Vector3 probe=desired+(attempt==0?Vector3.zero:new Vector3(Mathf.Sin(attempt*2.39996f),0,Mathf.Cos(attempt*2.39996f))*(.35f+attempt*.08f));
                probe=WorldTraversal.NearestWalkable(probe,.65f);
                if(probe.magnitude>16.35f||Vector3.Distance(probe,TacticalRoomGeometry.Entrance)<5.5f||!WorldTraversal.CanReach(TacticalRoomGeometry.Entrance,probe,.65f))continue;
                if(Role(index)==EscapeRole.GateGuard&&!RoomTacticalRegion.Contests(CombatFx.Flat(probe-new Vector3(0,0,11)).sqrMagnitude,.6f,true))continue;
                bool crowded=false;foreach(var other in occupied)if(Vector3.Distance(other,probe)<2.4f){crowded=true;break;}
                if(crowded)continue;point=probe;return true;
            }
            point=Vector3.zero;return false;
        }
    }
}
