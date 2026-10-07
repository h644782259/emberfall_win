using UnityEngine;
namespace Emberfall
{
    // The same authored centers drive the visible buildings and production traversal.
    public static class HubSettlementPlan
    {
        public const int BuildingCount=6;
        public const float BuildingWidth=5.5f;
        public static Vector3 Building(int hub,int index)
        {
            int side=index<3?-1:1,row=index%3;
            if(hub==1)return new Vector3(side*13,0,-12+row*10+(side>0?4:0));
            return new Vector3(side*(row==1?18:row==0?15:12),0,row==0?-10:row==1?0:13);
        }
        // Service clearings beside, rather than across, the caravan through-road.
        public static Vector3 Npc(int index){return index==0?new Vector3(-6,0,-16):index==1?new Vector3(6,0,-16):new Vector3(-6,0,-4);}
        public static Vector3 ForgePier(int side){return new Vector3(side*7,0,2);}
        public static Vector3 ObservatoryCenter {get{return new Vector3(0,0,3);}}
        public static void RegisterTownNavigation(int hub)
        {
            for(int index=0;index<BuildingCount;index++)WorldTraversal.AddBox(Building(hub,index),new Vector2(BuildingWidth,BuildingWidth));
            if(hub==1)for(int side=-1;side<=1;side+=2)WorldTraversal.AddBox(ForgePier(side),new Vector2(1.4f,1.4f));
            else WorldTraversal.AddCircle(ObservatoryCenter,1.1f);
        }
        public static void RegisterNpcNavigation(int index)
        {
            Vector3 p=Npc(index);WorldTraversal.AddCircle(p,.43f);
            WorldTraversal.AddBox(p+new Vector3(0,0,.65f),new Vector2(1.4f,.5f));
        }
    }
}
