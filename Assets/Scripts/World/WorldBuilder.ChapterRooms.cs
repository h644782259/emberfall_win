using UnityEngine;
namespace Emberfall
{
    public static partial class WorldBuilder
    {
        private static void BuildChapterRoom(Transform parent,WorldResources r,ChapterRoomPlan plan)
        {
            bool forest=plan.Node==ChapterNode.ForestCourt,mine=plan.Node==ChapterNode.Redrock;
            ApplyChapterAtmosphere(parent,plan);
            Material ground=r.Material(forest?new Color(.19f,.30f,.23f):mine?new Color(.35f,.25f,.19f):new Color(.22f,.29f,.38f));
            Material structure=r.Material(forest?new Color(.29f,.24f,.16f):mine?new Color(.43f,.29f,.20f):new Color(.37f,.45f,.55f),false,forest?VisualSurface.Wood:VisualSurface.Stone);
            Material trim=r.Material(new Color(.62f,.46f,.25f),false,VisualSurface.Metal);
            Primitive(parent,"Chapter arena foundation",PrimitiveType.Cylinder,new Vector3(0,-.7f,0),new Vector3(36, .7f,36),ground);
            ChapterRoomGeometry.Register(plan);
            foreach(var obstacle in plan.Obstacles)
            {
                // Primitive radii are .5 and cylinder half-height is 1; these dimensions
                // match the shared ground footprint, including the low solid base.
                Vector3 scale=obstacle.Radius>0?new Vector3(obstacle.Radius*2,obstacle.Height*.5f,obstacle.Radius*2):new Vector3(obstacle.Size.x,obstacle.Height,obstacle.Size.y);
                Primitive(parent,forest?"Root island solid":mine?"Forked mine retaining block":"Star spoke pedestal",obstacle.Radius>0?PrimitiveType.Cylinder:PrimitiveType.Cube,obstacle.Center+Vector3.up*obstacle.Height*.5f,scale,structure,cameraOccluder:true);
                if(forest)
                {
                    BuildBranchTree(parent,r,obstacle.Center+Vector3.up*obstacle.Height,Mathf.Clamp(obstacle.Radius*.48f,.75f,1.65f),197+Mathf.RoundToInt(obstacle.Center.x*13+obstacle.Center.z*31));
                }
                else if(!mine)
                {
                    Crystal(parent,r,obstacle.Center+Vector3.up*(obstacle.Height+.45f),.5f,r.Material(new Color(.31f,.72f,.84f),true));
                    Ring(parent,r,"Spoke star dial",obstacle.Center+Vector3.up*(obstacle.Height+.03f),.72f,.06f,trim,false);
                }
            }
            if(forest)
            {
                Material roots=r.Material(plan.Room==0?new Color(.24f,.21f,.15f):new Color(.28f,.52f,.36f),plan.Room>0,VisualSurface.Wood);
                for(int i=0;i<8;i++)
                {
                    float a=i*Mathf.PI/4;Vector3 ray=new Vector3(Mathf.Sin(a),0,Mathf.Cos(a));
                    // Branches stay on the existing solid island, never new traversal blockers.
                    Ribbon(parent,r,plan.Room==0?"Dormant forest root vein":"Restored forest root vein",new[]{ray*.4f,ray*1.8f+new Vector3(ray.z,0,-ray.x)*.25f,ray*3.15f},.16f,1.265f,roots);
                }
                for(int i=0;i<24;i++)
                {float a=i*Mathf.PI/12;Primitive(parent,"Forest outer trail marker",PrimitiveType.Cube,new Vector3(Mathf.Sin(a)*10.8f,.015f,Mathf.Cos(a)*10.8f),new Vector3(.35f,.02f,.18f),trim);}
            }
            else if(mine)
            {
                for(int side=-1;side<=1;side+=2)for(int rail=-1;rail<=1;rail+=2)
                    Primitive(parent,"Forked mine flush rail",PrimitiveType.Cube,new Vector3(side*7+rail*.42f,.02f,-1),new Vector3(.07f,.02f,21),trim);
                int mirror=(plan.Seed&1)==0?1:-1;
                Ribbon(parent,r,"Mine hunt-side feeder conduit",new[]{new Vector3(mirror*8,0,7),new Vector3(mirror*6,0,9),new Vector3(0,0,11)},.13f,.03f,r.Material(new Color(.48f,.35f,.20f),false,VisualSurface.Metal));
                // Supports sit entirely on the already-solid central retaining spine.
                for(int z=-5;z<=3;z+=8)Primitive(parent,"Mine hoist column",PrimitiveType.Cube,new Vector3(0,3,z),new Vector3(.5f,3,.5f),trim,cameraOccluder:true);
                Primitive(parent,"Mine hoist suspended beam",PrimitiveType.Cube,new Vector3(0,4.55f,-1),new Vector3(.5f,.35f,9),trim,cameraOccluder:true);
            }
            else
            {
                for(int i=0;i<8;i++)
                {
                    float a=i*Mathf.PI/4;var tile=Primitive(parent,"Open star ritual inset",PrimitiveType.Cube,new Vector3(Mathf.Sin(a)*4.8f,.021f,1+Mathf.Cos(a)*4.8f),new Vector3(.18f,.012f,.85f),trim);
                    tile.transform.localRotation=Quaternion.Euler(0,i*45,0);
                }
                Ring(parent,r,"Clear central star arena",new Vector3(0,.02f,1),5.8f,.045f,trim,false);
                for(int i=0;i<3;i++)
                {float a=(30+i*120)*Mathf.Deg2Rad;Vector3 ray=new Vector3(Mathf.Sin(a),0,Mathf.Cos(a));Ribbon(parent,r,"Three star spokes",new[]{ray*6.5f,ray*13.4f},.32f,.024f,trim);}
            }
            Primitive(parent,"Chapter entrance threshold",PrimitiveType.Cube,plan.Entrance+Vector3.up*.025f,new Vector3(3.4f,.035f,.8f),trim);
            BuildPortalFocus(parent,r,plan.Exit);
        }

        private static void ApplyChapterAtmosphere(Transform parent,ChapterRoomPlan plan)
        {
            bool forest=plan.Node==ChapterNode.ForestCourt,mine=plan.Node==ChapterNode.Redrock;
            // Reuse the two world lights. Clearer ambient ground and restrained fog keep tactical rings legible.
            RenderSettings.fogColor=forest?new Color(.10f,.19f,.16f):mine?new Color(.20f,.13f,.10f):new Color(.08f,.12f,.23f);
            RenderSettings.fogDensity=forest?(plan.Room==0?.009f:.006f):mine?.011f:.006f;
            RenderSettings.ambientSkyColor=forest?new Color(.43f,.55f,.46f):mine?new Color(.51f,.39f,.31f):new Color(.40f,.49f,.66f);
            RenderSettings.ambientEquatorColor=forest?new Color(.27f,.36f,.29f):mine?new Color(.31f,.24f,.20f):new Color(.25f,.31f,.43f);
            RenderSettings.ambientGroundColor=forest?new Color(.18f,.24f,.19f):mine?new Color(.23f,.17f,.14f):new Color(.17f,.21f,.31f);
            foreach(var light in parent.GetComponentsInChildren<Light>())if(light.type==LightType.Directional)
            {
                bool key=light.gameObject.name=="Sun";light.intensity=key?1.15f:.32f;
                light.color=key?(forest?new Color(.91f,1,.86f):mine?new Color(1,.78f,.61f):new Color(.76f,.86f,1)):new Color(.49f,.62f,.78f);
                if(key)light.transform.localRotation=Quaternion.Euler(forest?52:mine?42:62,mine?-55:-35,0);
            }
        }

        public static void ApplyChapterLandmark(GameObject world,int mask)
        {
            if(world==null)return;
            Transform dial=null;
            foreach(Transform part in world.GetComponentsInChildren<Transform>(true))if(part.name=="Turning exchange star chart"){dial=part;break;}
            if(dial==null)return;
            string stateName="Chapter star map state "+(mask&7);
            Transform previous=null;
            foreach(Transform child in dial)
            {
                if(child.name==stateName&&child.gameObject.activeSelf)return;
                if(child.name.StartsWith("Chapter star map state ")&&child.gameObject.activeSelf)previous=child;
                else if(child.name=="Chart constellation")child.gameObject.SetActive(false);
            }
            if(previous!=null){previous.gameObject.SetActive(false);Object.Destroy(previous.gameObject);}
            var baseRenderer=dial.GetComponent<Renderer>();if(baseRenderer!=null)baseRenderer.enabled=false;
            var root=new GameObject(stateName);root.transform.SetParent(dial,false);
            WorldResources owned=root.AddComponent<WorldResources>();
            Material bronze=owned.Material(new Color(.60f,.44f,.23f),false,VisualSurface.Metal),dim=owned.Material(new Color(.25f,.29f,.31f),false,VisualSurface.Stone),star=owned.Material(new Color(.38f,.78f,.83f),true);
            foreach(var piece in ChapterRoomGeometry.StarMapPieces(mask))
            {
                if(piece.Star)
                    Primitive(root.transform,piece.Repaired?"Recovered chapter star":"Missing chapter star",PrimitiveType.Sphere,piece.Position,Vector3.one*(piece.Repaired?.15f:.08f),piece.Repaired?star:dim);
                else
                {
                    GameObject tile=Primitive(root.transform,piece.Repaired?"Joined star chart arc":"Broken star chart arc",PrimitiveType.Cube,piece.Position,new Vector3(.27f,.08f,.055f),piece.Repaired?bronze:dim);
                    tile.transform.localRotation=Quaternion.Euler(0,0,piece.Angle);
                }
            }
        }
    }
}
