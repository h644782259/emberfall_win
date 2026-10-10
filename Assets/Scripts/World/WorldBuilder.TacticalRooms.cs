using UnityEngine;
namespace Emberfall
{
    public static partial class WorldBuilder
    {
        private static void BuildTacticalRoom(Transform parent,WorldResources r,int layout)
        {
            // Shared entrance/exit shell, without old room interiors.
            BuildLinkedRoom(parent,r,5);
            TacticalRoomGeometry.Register(layout);
            Material stone=r.Material(new Color(.33f,.38f,.42f));
            foreach(var wall in TacticalRoomGeometry.Walls(layout))
                Primitive(parent,"Tactical sight-blocking wall",PrimitiveType.Cube,wall.Position+Vector3.up*.9f,new Vector3(wall.Size.x,1.8f,wall.Size.y),stone,cameraOccluder:true);
            if(TacticalRoomGeometry.Flooded(layout))
            {
                Rect bridge=TacticalRoomGeometry.Bridge(layout);
                BuildWaterSurface(parent,r,"Flooded crossing",TacticalRoomGeometry.River(),3.2f,.06f,WaterEnvironment.Tactical);
                Primitive(parent,"Offset wooden bridge",PrimitiveType.Cube,new Vector3((bridge.xMin+bridge.xMax)*.5f,.09f,0),new Vector3(5.4f,.1f,6),r.Material(new Color(.42f,.28f,.16f),false,VisualSurface.Wood));
                BuildBridgeWaterContact(parent,r,new Rect((bridge.xMin+bridge.xMax)*.5f-2.7f,-3,5.4f,6),.14f);
            }
        }
        public static GameObject MakeRoomObjective(Vector3 position,bool chapterSeal=false)
        {
            GameObject root=new GameObject("Room capture boundary");root.transform.position=position;
            WorldResources r=root.AddComponent<WorldResources>();Material gold=r.Material(chapterSeal?new Color(.18f,.25f,.27f):new Color(1,.8f,.25f),!chapterSeal);
            Ring(root.transform,r,"Stand inside",position+Vector3.up*.08f,RoomTacticalRegion.CaptureRadius,.09f,gold,false);
            // Lift the existing objective crown above feet; the exact capture boundary stays unchanged.
            Crystal(root.transform,r,position+Vector3.up*.85f,.45f,gold);
            return root;
        }
        public static GameObject MakeDungeonReturnMarker(Vector3 position,float radius=2.5f)
        {
            var root=new GameObject("Cleared dungeon return portal");root.transform.position=position;
            var resources=root.AddComponent<WorldResources>();var glow=resources.Material(new Color(.32f,.91f,.77f),true);
            Ring(root.transform,resources,"Return interaction",position+Vector3.up*.09f,radius,.10f,glow,false);
            Label(root.transform,"Return to camp","返回营地\n靠近传送点交互",position+Vector3.up*4.3f,.08f,new Color(.55f,1f,.85f),false);
            return root;
        }
        public static GameObject MakeSideEventCrystal(Vector3 position)
        {
            var root=MakeLootBeacon(position,new Color(.33f,.85f,1));
            Label(root.transform,"Optional crystal terms","晶核支线\n靠近后开启挑战",position+Vector3.up*2.1f,.065f,new Color(.65f,.95f,1),false);
            return root;
        }
        public static GameObject MakeRoomContestMarker(Transform enemy,float footprint)
        {
            var root=new GameObject("Contesting objective: double gold ring");
            root.transform.SetParent(enemy,false);
            var r=root.AddComponent<WorldResources>();
            var gold=r.Material(new Color(1f,.82f,.18f),true);
            Ring(root.transform,r,"Contest inner",enemy.position+Vector3.up*.08f,footprint+.10f,.055f,gold,false);
            Ring(root.transform,r,"Contest outer",enemy.position+Vector3.up*.08f,footprint+.23f,.055f,gold,false);
            return root;
        }
    }
}
