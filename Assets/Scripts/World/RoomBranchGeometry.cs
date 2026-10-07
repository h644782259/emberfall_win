using UnityEngine;
namespace Emberfall{public static class RoomBranchGeometry{
        public static bool TrySpawn(RoomChainPlan plan,int index,System.Collections.Generic.List<Vector3> occupied,out Vector3 position)
        {
            int mirror=RoomTactics.Mirror(plan.Seed);
            // The outer supply escort clears the authored blocking rubble at (+/-13.3, 2.2).
            // Four sentries guard the two seals; supply corridor splits a rear caster line from its forward escorts.
            Vector3[] slots=plan.Branch==RoomBranch.Seal?
                new[]{new Vector3(-8,0,-4),new Vector3(-10,0,-6),new Vector3(8,0,7),new Vector3(10,0,9)}:
                new[]{new Vector3(8,0,9),new Vector3(7,0,2),new Vector3(10,0,2),new Vector3(12.3f,0,3.6f),new Vector3(5,0,9),new Vector3(11,0,10)};
            position=Vector3.zero;if(index<0||index>=slots.Length)return false;
            var candidate=slots[index];candidate.x*=mirror;
            if(!WorldTraversal.IsWalkable(candidate,.65f)||!WorldTraversal.CanReach(TacticalRoomGeometry.Entrance,candidate,.65f))return false;
            foreach(var other in occupied)if(Vector3.Distance(other,candidate)<2.4f)return false;
            position=candidate;return true;
        }
}}
