using UnityEngine;
namespace Emberfall
{
 public static partial class WorldBuilder
 {
  // Three genuinely different traversal arrangements, all within the existing
  // navigation grid. Heights are decorative; reachable surfaces remain planar.
  private static void BuildChallengeArena(Transform parent,WorldResources r,int mode)
  {
   if(mode==2)
   {
    RenderSettings.ambientSkyColor=new Color(.48f,.52f,.65f);
    RenderSettings.ambientEquatorColor=new Color(.32f,.35f,.46f);
    RenderSettings.ambientGroundColor=new Color(.2f,.22f,.3f);
    RenderSettings.fogColor=new Color(.13f,.15f,.23f);
    RenderSettings.fogDensity=.009f;
   }
   Material stone=r.Material(mode==0?new Color(.18f,.29f,.25f):mode==1?new Color(.27f,.19f,.17f):new Color(.29f,.33f,.43f));
   Material edge=r.Material(mode==0?new Color(.32f,.42f,.31f):mode==1?new Color(.43f,.29f,.21f):new Color(.43f,.44f,.56f));
   Material glow=r.Material(mode==0?new Color(.3f,.77f,.56f):mode==1?new Color(1,.34f,.08f):new Color(.56f,.45f,1),true);
   Primitive(parent,"Challenge island foundation",PrimitiveType.Cylinder,new Vector3(0,-.8f,0),new Vector3(39,.7f,39),stone);
   Vector2[] rim=new Vector2[32];for(int n=0;n<rim.Length;n++){float a=n*Mathf.PI*2/rim.Length;rim[n]=new Vector2(Mathf.Cos(a)*19,Mathf.Sin(a)*19);}
   Surface(parent,r,"Challenge playable surface",rim,0,stone);Skirt(parent,r,"Challenge outer cliffs",rim,-2.4f,edge);
   if(mode==0)
   {
    // Four angled approaches around a protected circular objective courtyard.
    Primitive(parent,"Verdant seal court",PrimitiveType.Cylinder,new Vector3(0,.018f,0),new Vector3(7.2f,.016f,7.2f),edge);
    for(int side=-1;side<=1;side+=2)
    {Tree(parent,r,new Vector3(side*8,0,6),1.35f,2+side);Tree(parent,r,new Vector3(side*9,0,-4),1.1f,5+side);
     Rock(parent,r,new Vector3(side*5.7f,0,1.4f),1.1f,8+side);Pillar(parent,r,new Vector3(side*4.8f,0,-5.3f),1.8f,false);}
    for(int k=0;k<8;k++){float a=k*Mathf.PI/4;Primitive(parent,"Seal boundary crystal",PrimitiveType.Cube,new Vector3(Mathf.Cos(a)*3.45f,.07f,Mathf.Sin(a)*3.45f),new Vector3(.38f,.04f,.38f),glow);}
   }
   else if(mode==1)
   {
    // A molten channel forces crossing through the central bridge; offset cover
    // divides north/south encounters into deliberate approaches.
    Vector3[] channel={new Vector3(-19,0,0),new Vector3(0,0,0),new Vector3(19,0,0)};
    WorldTraversal.SetRiver(channel,3.1f,new Rect(-3.1f,-3,6.2f,6));
    Ribbon(parent,r,"Molten fracture",channel,3.1f,.02f,glow);
    Primitive(parent,"Basalt crossing",PrimitiveType.Cube,new Vector3(0,.05f,0),new Vector3(6.2f,.045f,5.8f),edge);
    for(int side=-1;side<=1;side+=2)
    {Vector3 p=new Vector3(side*7,0,side*7);
     var cover=new GameObject("Quarry solid cover group");cover.transform.SetParent(parent,false);
     Primitive(cover.transform,"Offset quarry cover",PrimitiveType.Cube,p+Vector3.up*.8f,new Vector3(5,1.6f,1.2f),edge);
     Primitive(cover.transform,"Quarry opaque footing",PrimitiveType.Cube,p+Vector3.up*.12f,new Vector3(5,.24f,1.2f),edge);
     BuildingOcclusionGroup.Configure(cover.transform,p,new Vector2(5,1.2f));WorldTraversal.AddBox(p,new Vector2(5,1.2f));
     Crystal(parent,r,new Vector3(side*13,1,side*7),.9f,glow);}
   }
   else
   {
    // Alternating pillars create three duelling lanes and safe pockets around
    // the central open ring, rather than the original cathedral corridors.
    for(int k=0;k<3;k++)
    {float a=k*Mathf.PI*2/3;Vector3 p=new Vector3(Mathf.Sin(a)*10,0,Mathf.Cos(a)*10);
     Primitive(parent,"Duel ring floor",PrimitiveType.Cylinder,p+Vector3.up*.018f,new Vector3(9,.016f,9),edge);
     for(int side=-1;side<=1;side+=2)Pillar(parent,r,p+new Vector3(side*3.5f,0,1.4f),2.2f,true);}
    Crystal(parent,r,new Vector3(0,2.5f,18.5f),1.5f,glow);
   }
   PointLight(parent,new Vector3(-8,3,7),glow.color,1.1f,12);
  }
  private static void BuildBreakablePockets(Transform parent,int layout)
  {
   int level=GameSession.Instance==null||GameSession.Instance.Progression==null?1:GameSession.Instance.Progression.Profile.level;
   for(int side=-1;side<=1;side+=2)
   {
    DestructiblePropFactory.Create(parent,new Vector3(side*12.5f,0,-7),DestructibleKind.Crate,level,false,PropRecovery.Energy);
    DestructiblePropFactory.Create(parent,new Vector3(side*11.5f,0,7.5f),DestructibleKind.Pot,level,false,PropRecovery.Health);
    DestructiblePropFactory.Create(parent,new Vector3(side*13.3f,0,2.2f),DestructibleKind.Rubble,level,true,PropRecovery.None);
    if(layout%2==0)DestructiblePropFactory.Create(parent,new Vector3(side*9.3f,0,11),DestructibleKind.Crate,level,false,PropRecovery.Energy);
   }
  }
 }
}
