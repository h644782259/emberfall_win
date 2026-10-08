using UnityEngine;
namespace Emberfall
{
 public static partial class WorldBuilder
 {
  private static void BuildTown(Transform parent,WorldResources r,int hub)
  {
   bool quarry=hub==1;
   Material floor=r.Material(quarry?new Color(.34f,.28f,.22f):new Color(.2f,.29f,.35f)),stone=r.Material(quarry?new Color(.47f,.34f,.24f):new Color(.43f,.49f,.56f));
   Material roof=r.Material(quarry?new Color(.4f,.17f,.12f):new Color(.12f,.25f,.4f),false,quarry?VisualSurface.Wood:VisualSurface.Metal);
   Material glow=r.Material(quarry?new Color(.97f,.58f,.23f):new Color(.35f,.76f,1),true);
   Primitive(parent,"Town foundation",PrimitiveType.Cylinder,new Vector3(0,-.8f,0),new Vector3(47,.7f,47),floor);
   Primitive(parent,quarry?"Staggered quarry working street":"Astral crescent plaza",quarry?PrimitiveType.Cube:PrimitiveType.Cylinder,new Vector3(0,-.015f,-3),new Vector3(quarry?19:29,.025f,30),stone);
   HubSettlementPlan.RegisterTownNavigation(hub);
   for(int index=0;index<HubSettlementPlan.BuildingCount;index++)
   {
    Vector3 p=HubSettlementPlan.Building(hub,index);int side=p.x<0?-1:1;
    var building=new GameObject("Town building occlusion group");building.transform.SetParent(parent,false);
    Primitive(building.transform,quarry?"Quarry workshop":"Observatory arcade",PrimitiveType.Cube,p+Vector3.up*1.9f,new Vector3(HubSettlementPlan.BuildingWidth,3.8f,HubSettlementPlan.BuildingWidth),stone,cameraOccluder:true);
    if(quarry) BuildWorkshopRoof(building.transform,r,p,side,roof,stone);
    else Primitive(building.transform,"Astral dome",PrimitiveType.Sphere,p+Vector3.up*4,new Vector3(6.2f,2,6.2f),roof,cameraOccluder:true);
    Primitive(building.transform,"Lit doorway",PrimitiveType.Cube,p+new Vector3(-side*2.78f,1.3f,0),new Vector3(.04f,2.4f,1.6f),glow);
    // Flush footing panels remain inside the building's registered solid footprint.
    for(int j=0;j<4;j++)Primitive(building.transform,"Weathered wall footing",PrimitiveType.Cube,p+new Vector3(-side*2.72f,.16f,-1.95f+j*1.3f),new Vector3(.045f,.3f,1.2f),floor);
    if(quarry)
    {

     Primitive(building.transform,"Timber storage beam",PrimitiveType.Cube,p+new Vector3(-side*2.65f,.65f,1.1f),new Vector3(.18f,.3f,1.8f),roof);
    }
    else
    {
     Primitive(building.transform,"Observatory roof spire",PrimitiveType.Cylinder,p+Vector3.up*5.3f,new Vector3(.13f,.6f,.13f),glow);
     for(int j=-1;j<=1;j+=2)Primitive(building.transform,"Arcade wall pilaster",PrimitiveType.Cylinder,p+new Vector3(-side*2.65f,1.9f,j*1.9f),new Vector3(.2f,1.9f,.2f),roof,cameraOccluder:true);
    }
    BuildingOcclusionGroup.Configure(building.transform,p,new Vector2(HubSettlementPlan.BuildingWidth,HubSettlementPlan.BuildingWidth));
   }
   if(quarry)
   {
    for(int side=-1;side<=1;side+=2)
    {Vector3 pier=HubSettlementPlan.ForgePier(side);Primitive(parent,"Forge gantry footing",PrimitiveType.Cube,pier+Vector3.up*.25f,new Vector3(1.4f,.5f,1.4f),stone);
     Primitive(parent,"Forge gantry pier",PrimitiveType.Cube,pier+Vector3.up*2.8f,new Vector3(.75f,5.6f,.75f),roof,cameraOccluder:true);}
    Primitive(parent,"Forge gantry crossbeam",PrimitiveType.Cube,new Vector3(0,5.8f,2),new Vector3(15,.5f,.5f),roof,cameraOccluder:true);
    // Flat cart rails describe the working route without adding invisible barriers.
    for(int side=-1;side<=1;side+=2)Primitive(parent,"Quarry cart rail",PrimitiveType.Cube,new Vector3(side*1.5f,.022f,1),new Vector3(.09f,.018f,35),roof);
   }
   else
   {
    Vector3 center=HubSettlementPlan.ObservatoryCenter;
    Primitive(parent,"Observatory circular dais",PrimitiveType.Cylinder,center+Vector3.up*.02f,new Vector3(9,.02f,9),floor);
    Crystal(parent,r,center+Vector3.up*2,1.1f,glow);
    Transform orbit=Region(parent,"Town astronomical crown");orbit.localPosition=center+Vector3.up*4.3f;
    GameObject ring=new GameObject("Observatory brass meridian");ring.transform.SetParent(orbit,false);
    ring.AddComponent<MeshFilter>().sharedMesh=CostumeMeshLibrary.Get(WingSilhouette.Mechanical);ring.AddComponent<MeshRenderer>().sharedMaterial=r.Material(new Color(.74f,.56f,.29f),false,VisualSurface.Metal);ring.transform.localScale=Vector3.one*2.5f;ring.transform.localRotation=Quaternion.Euler(25,15,0);
    orbit.gameObject.AddComponent<FashionOrbit>();
   }
   BuildTownGroundDetail(parent,r,hub);Portal(parent,r,new Vector3(0,0,11),glow);BuildHubNpcs(parent,r);
  }
  private static void BuildTownGroundDetail(Transform parent,WorldResources r,int hub)
  {
   Material edge=r.Material(hub==1?new Color(.42f,.36f,.27f):new Color(.48f,.55f,.61f)),trim=r.Material(new Color(.23f,.32f,.33f));
   for(int i=0;i<24;i++)for(int side=-1;side<=1;side+=2)
   {
    float z=-17+i*1.4f;float x=hub==1?3.2f:4.1f+Mathf.Sin(i*.24f)*1.1f;
    Primitive(parent,"Flush road edge stone",PrimitiveType.Cube,new Vector3(side*x,.022f,z),new Vector3(.32f,.025f,.95f),i%3==0?trim:edge);
   }
   if(hub==2)
    for(int i=0;i<16;i++)
    {float a=i*Mathf.PI/8;Vector3 p=new Vector3(Mathf.Cos(a)*20.7f,.01f,Mathf.Sin(a)*20.7f);
     Primitive(parent,"Observatory perimeter mosaic",PrimitiveType.Cube,p,new Vector3(.8f,.018f,.16f),trim).transform.localRotation=Quaternion.Euler(0,-a*Mathf.Rad2Deg,0);}
  }
  private static void BuildWaterBankDetail(Transform parent,WorldResources r,Vector3[] stream)
  {
   Material reed=r.Material(new Color(.31f,.43f,.24f),false,VisualSurface.Foliage),pebble=r.Material(new Color(.43f,.47f,.43f));
   for(int segment=1;segment<stream.Length;segment++)for(int sample=1;sample<=3;sample++)
   {
    Vector3 p=Vector3.Lerp(stream[segment-1],stream[segment],sample/4f);if(Mathf.Abs(p.x)<2.4f)continue;
    Vector3 tangent=(stream[segment]-stream[segment-1]).normalized,normal=new Vector3(-tangent.z,0,tangent.x);
    for(int side=-1;side<=1;side+=2)
    {Vector3 bank=p+normal*(side*1.52f);
     Primitive(parent,"Small bank pebble",PrimitiveType.Sphere,bank+Vector3.up*.045f,new Vector3(.24f,.09f,.16f),pebble);
     Primitive(parent,"Brook reed clump",PrimitiveType.Cube,bank+Vector3.up*.16f,new Vector3(.035f,.32f,.035f),reed).transform.localRotation=Quaternion.Euler(side*12,segment*31,side*8);}
   }
  }
  private static void BuildHubNpcs(Transform parent,WorldResources r)
  {
   for(int index=0;index<2;index++)
   {
    Vector3 p=GameSession.HubNpcPosition(index);Transform npc=Region(parent,index==0?"Camp Merchant":index==1?"Camp Blacksmith":"Star Exchange Steward");
    Transform body=Region(npc,"NPC body pivot");body.localPosition=p;
    Material cloth=r.Material(index==0?new Color(.58f,.35f,.16f):index==1?new Color(.32f,.37f,.44f):new Color(.22f,.48f,.53f),false,VisualSurface.Cloth);
    Material skin=r.Material(new Color(.76f,.57f,.42f),false,VisualSurface.Skin),dark=r.Material(new Color(.12f,.16f,.2f),false,VisualSurface.Cloth);
    Material wood=r.Material(new Color(.32f,.20f,.12f),false,VisualSurface.Wood),metal=r.Material(new Color(.38f,.42f,.47f),false,VisualSurface.Metal),light=r.Material(new Color(.46f,.86f,.9f),true,VisualSurface.Crystal);
    Primitive(body,"NPC tunic",PrimitiveType.Capsule,Vector3.up*.85f,new Vector3(index==1?.84f:index==2?.62f:.72f,.58f,index==1?.58f:.5f),cloth);
    Transform head=Region(body,"NPC head pivot");head.localPosition=Vector3.up*1.66f;
    Primitive(head,"NPC head",PrimitiveType.Sphere,Vector3.zero,Vector3.one*.47f,skin);
    Primitive(head,"NPC nose",PrimitiveType.Sphere,new Vector3(0,-.015f,.225f),new Vector3(.075f,.09f,.08f),skin);
    for(int side=-1;side<=1;side+=2)Primitive(head,"NPC eye",PrimitiveType.Sphere,new Vector3(side*.09f,.035f,.213f),new Vector3(.04f,.04f,.025f),dark);
    Transform left=null,right=null,dial=null;
    for(int side=-1;side<=1;side+=2)
    {
     Primitive(body,"NPC boots",PrimitiveType.Capsule,new Vector3(side*.19f,.29f,0),new Vector3(.21f,.26f,.24f),dark);
     Transform arm=Region(body,"Articulated working sleeve");arm.localPosition=new Vector3(side*.43f,1.22f,0);if(side<0)left=arm;else right=arm;
     Primitive(arm,"NPC sleeve",PrimitiveType.Capsule,new Vector3(0,-.20f,0),new Vector3(index==1?.30f:.23f,.22f,index==1?.29f:.24f),cloth);
     Primitive(arm,"NPC hand",PrimitiveType.Sphere,new Vector3(0,-.45f,.03f),Vector3.one*.2f,skin);
    }
    if(index==0)
    {
     Primitive(head,"Merchant cap",PrimitiveType.Cylinder,Vector3.up*.23f,new Vector3(.92f,.075f,.70f),cloth);
     for(int row=0;row<2;row++)
     {Primitive(npc,"Merchant stocked shelf",PrimitiveType.Cube,p+new Vector3(0,.35f+row*.6f,.65f),new Vector3(1.4f,.1f,.5f),wood);
      for(int item=0;item<3;item++)Primitive(npc,item%2==0?"Potion stock":"Wrapped provision",item%2==0?PrimitiveType.Cylinder:PrimitiveType.Cube,p+new Vector3(-.46f+item*.46f,.53f+row*.6f,.65f),new Vector3(.22f,.28f,.22f),item%2==0?light:cloth);}
     for(int side=-1;side<=1;side+=2)Primitive(npc,"Shelf upright",PrimitiveType.Cube,p+new Vector3(side*.65f,.67f,.65f),new Vector3(.08f,1.3f,.45f),wood);
    }
    else if(index==1)
    {
     Primitive(body,"Smith leather apron",PrimitiveType.Cube,new Vector3(0,.84f,.27f),new Vector3(.62f,.69f,.06f),dark);
     Primitive(npc,"Anvil stump",PrimitiveType.Cylinder,p+new Vector3(0,.3f,.65f),new Vector3(.65f,.3f,.48f),wood);
     Primitive(npc,"Forged anvil face",PrimitiveType.Cube,p+new Vector3(0,.7f,.65f),new Vector3(1.2f,.22f,.48f),metal);
     Primitive(npc,"Anvil tapered horn",PrimitiveType.Capsule,p+new Vector3(.48f,.73f,.65f),new Vector3(.18f,.22f,.17f),metal).transform.localRotation=Quaternion.Euler(0,0,90);
     Primitive(right,"Smith hammer handle",PrimitiveType.Cylinder,new Vector3(0,-.42f,.16f),new Vector3(.06f,.23f,.06f),wood).transform.localRotation=Quaternion.Euler(90,0,0);
     Primitive(right,"Smith hammer head",PrimitiveType.Cube,new Vector3(0,-.42f,.37f),new Vector3(.44f,.23f,.23f),metal);
    }
    else
    {
     Primitive(npc,"Exchange lectern",PrimitiveType.Cube,p+new Vector3(0,.44f,.65f),new Vector3(1.05f,.88f,.48f),wood);
     Primitive(npc,"Star chart table",PrimitiveType.Cylinder,p+new Vector3(0,.95f,.65f),new Vector3(1.35f,.04f,.48f),metal);
     dial=Region(npc,"Turning exchange star chart");dial.localPosition=p+new Vector3(0,1.1f,.65f);dial.localScale=Vector3.one*.5f;
     var ring=dial.gameObject.AddComponent<MeshFilter>();ring.sharedMesh=CostumeMeshLibrary.Get(WingSilhouette.Mechanical);dial.gameObject.AddComponent<MeshRenderer>().sharedMaterial=light;
     for(int i=0;i<5;i++){float a=i*Mathf.PI*.4f;Primitive(dial,"Chart constellation",PrimitiveType.Sphere,new Vector3(Mathf.Cos(a)*.6f,Mathf.Sin(a)*.6f,0),Vector3.one*.09f,light);}
    }
    HubSettlementPlan.RegisterNpcNavigation(index);npc.gameObject.AddComponent<HubNpcIdle>().Initialize(index,body,head,right,left,dial);
   }
  }
 }
}
