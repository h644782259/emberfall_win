using UnityEngine;
namespace Emberfall
{
    public static partial class WorldBuilder
    {
        private static void BuildClimbableProps(Transform parent,WorldResources r)
        {
            for(int side=-1;side<=1;side+=2)
            {
                Vector3 point=WorldTraversal.NearestWalkable(new Vector3(side*6,0,-15),1.7f);
                Vector2 size=side<0?new Vector2(2.4f,1.8f):new Vector2(2.6f,2.2f);float height=side<0?.65f:.95f;
                if(!WorldTraversal.IsWalkable(point,1.7f))continue;
                var handle=WorldTraversal.AddPlatform(point,size,height);if(handle==null)continue;
                GameObject obj=Primitive(parent,side<0?"Climbable supply crate":"Climbable stone terrace",PrimitiveType.Cube,point+Vector3.up*(height*.5f),new Vector3(size.x,height,size.y),r.Material(side<0?new Color(.42f,.29f,.17f):new Color(.43f,.48f,.47f),false,side<0?VisualSurface.Wood:VisualSurface.Stone));
                obj.AddComponent<BoxCollider>();
                Label(parent,"CLIMBABLE","跳跃可站立",point+Vector3.up*(height+.12f),.09f,new Color(.6f,.86f,.75f),true);
            }
        }
        private static void BuildTravelStation(Transform parent,WorldResources r,int hub)
        {
            Vector3 p=new Vector3(0,0,-14);
            Primitive(parent,"Travel station ground rune",PrimitiveType.Cylinder,p+Vector3.up*.035f,new Vector3(2.8f,.025f,2.8f),r.Material(new Color(.25f,.75f,.8f),true));
            Label(parent,"TRAVEL STATION",HubTravelRules.Name(hub)+" · M旅行",p+Vector3.up*.12f,.09f,new Color(.7f,.94f,.9f),true);
        }
    }
}
