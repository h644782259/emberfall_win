using UnityEngine;
namespace Emberfall
{
    public sealed partial class CombatModel
    {
        private Transform CostumeMesh(string name,WingSilhouette recipe,Transform parent,Vector3 at,Vector3 scale,Color color,VisualSurface surface)
        {
            Transform root=NewJoint(name,parent,at);root.localScale=scale;
            root.gameObject.AddComponent<MeshFilter>().sharedMesh=CostumeMeshLibrary.Get(recipe);
            root.gameObject.AddComponent<MeshRenderer>().sharedMaterial=Mat(color,surface);
            return root;
        }
        private void BuildFashionWingShape(FashionData wings,Color color)
        {
            fashionWings.localPosition += RearSilhouette.WingOffset(heroClass);
            fashionWings.localScale=Vector3.one*(wings.rarity==Rarity.Legendary?1.75f:wings.rarity==Rarity.Epic?1.45f:1f);
            WingSilhouette style=CostumeRecipes.WingStyle(wings.rarity);
            if(style==WingSilhouette.Mechanical)
            {
                Transform orbit=NewJoint("Mechanical star-ring orbit",fashionWings,Vector3.zero);
                CostumeMesh("Bronze outer astrolabe",style,orbit,Vector3.zero,Vector3.one*1.2f,new Color(.71f,.48f,.23f),VisualSurface.Metal);
                CostumeMesh("Inclined inner astrolabe",style,orbit,Vector3.zero,new Vector3(.91f,.91f,.91f),color,VisualSurface.Metal).localRotation=Quaternion.Euler(28,15,0);
                for(int i=0;i<8;i++)
                {
                    float a=i*Mathf.PI/4;Vector3 axis=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0);
                    Part("Star-ring radial vane",PrimitiveType.Cube,axis*.96f,new Vector3(.15f,.3f,.075f),color,orbit,VisualSurface.Metal).localRotation=Quaternion.Euler(0,0,i*45-90);
                    Part("Star-ring focus crystal",PrimitiveType.Sphere,axis*.78f,Vector3.one*.09f,Color.white,orbit,VisualSurface.Crystal);
                }
                orbit.gameObject.AddComponent<FashionOrbit>();
                for(int side=-1;side<=1;side+=2)for(int feather=0;feather<4;feather++)
                {
                    var vane=CostumeMesh("Layered legendary crystal flight",WingSilhouette.Crystal,fashionWings,new Vector3(side*(.45f+feather*.23f),.18f-feather*.12f,-.12f-feather*.08f),new Vector3(1.1f,1.3f-feather*.08f,.7f),Color.Lerp(color,Color.white,feather*.12f),VisualSurface.Crystal);
                    vane.localRotation=Quaternion.Euler(14,side*18,-side*(45+feather*13));
                }
            }
            else for(int side=-1;side<=1;side+=2)
            {
                int count=style==WingSilhouette.Crystal?5:5+(wings.rarity==Rarity.Rare?1:0);
                for(int i=0;i<count;i++)
                {
                    var feather=CostumeMesh(style==WingSilhouette.Crystal?"Faceted wing crystal":"Swept flight feather",style,fashionWings,
                        new Vector3(side*(.18f+i*.18f),.18f-i*.12f,-i*.025f),
                        new Vector3(1,1.05f-i*.065f,1),i%2==0?color:Color.Lerp(color,Color.white,.28f),style==WingSilhouette.Crystal?VisualSurface.Crystal:VisualSurface.Cloth);
                    feather.localRotation=Quaternion.Euler(12,side*RearSilhouette.WingYaw(heroClass),-side*(RearSilhouette.WingSpread(heroClass)+i*12));
                }
                if(wings.rarity==Rarity.Epic)for(int feather=0;feather<3;feather++)
                {
                    var under=CostumeMesh("Inner crystal flight layer",WingSilhouette.Crystal,fashionWings,new Vector3(side*(.23f+feather*.16f),-.12f-feather*.13f,-.18f),new Vector3(.6f,.8f-feather*.08f,.6f),color*.72f,VisualSurface.Crystal);
                    under.localRotation=Quaternion.Euler(20,side*12,-side*(62+feather*15));
                }
                Part("Wing scapular support",PrimitiveType.Capsule,new Vector3(side*.3f,.08f,0),new Vector3(.14f,.45f,.13f),color,fashionWings,style==WingSilhouette.Crystal?VisualSurface.Crystal:VisualSurface.Cloth).localRotation=Quaternion.Euler(0,0,-side*52);
            }
            Part("Wing clasp",PrimitiveType.Sphere,Vector3.zero,new Vector3(.2f,.23f,.12f),Color.white,fashionWings,VisualSurface.Crystal);
        }
        private void BuildClassCostume()
        {
            Color accent=GameBalance.ClassColor(heroClass),leather=new Color(.25f,.17f,.10f);
            if(body!=null)body.GetComponent<Renderer>().sharedMaterial=Mat(accent*.62f,heroClass==HeroClass.Vanguard?VisualSurface.Metal:VisualSurface.Cloth);
            if(heroClass==HeroClass.Arcanist)
                for(int side=-1;side<=1;side+=2)
                    Part("Scholar rear high collar",PrimitiveType.Cube,new Vector3(side*.21f,.49f,-.18f),new Vector3(.12f,.29f,.22f),accent*.65f,spine,VisualSurface.Cloth).localRotation=Quaternion.Euler(-12,0,side*15);
            if(heroClass==HeroClass.Vanguard)return;
            RemovePart(leftArm.Find("Pauldrons"));RemovePart(rightArm.Find("Pauldrons"));
            if(heroClass==HeroClass.Arcanist)
            {
                for(int side=-1;side<=1;side+=2)
                    Part("Long robe front panel",PrimitiveType.Cube,new Vector3(side*.18f,-.1f,.31f),new Vector3(.18f,.92f,.055f),accent*.8f,spine,VisualSurface.Cloth).localRotation=Quaternion.Euler(-5,0,side*4);
            }
            else if(heroClass==HeroClass.Ranger)
            {
                Part("Single leather shoulder",PrimitiveType.Sphere,new Vector3(0,.025f,0),new Vector3(.42f,.18f,.40f),leather,leftArm,VisualSurface.Cloth);
                Part("Diagonal ranger sash",PrimitiveType.Cube,new Vector3(.03f,.21f,.32f),new Vector3(.16f,.64f,.05f),accent*.68f,spine,VisualSurface.Cloth).localRotation=Quaternion.Euler(0,0,-34);
            }
            else
            {
                // Keep the existing antler crown but remove the mage's ankle-length robe.
                RemovePart(spine.Find("Layered Robe"));
                foreach(Transform part in spine)if(part.name=="Embroidered Stole"||part.name=="Robe Hem")RemovePart(part);
                for(int side=-1;side<=1;side+=2)for(int i=0;i<3;i++)
                    CostumeMesh("Leaf ritual mantle",WingSilhouette.Feather,spine,new Vector3(side*(.24f+i*.12f),.56f-i*.07f,.05f),new Vector3(1.2f,.42f,1.2f),accent*.65f,VisualSurface.Foliage).localRotation=Quaternion.Euler(15,0,side*(125+i*15));
                Part("Bound spirit totem",PrimitiveType.Cube,new Vector3(0,.14f,.35f),new Vector3(.18f,.38f,.12f),leather,spine,VisualSurface.Wood);
                Part("Totem moonstone",PrimitiveType.Sphere,new Vector3(0,.23f,.43f),Vector3.one*.15f,accent,spine,VisualSurface.Crystal);
            }
        }
        private void BuildClassEquipmentArmor(EquipmentAppearance look)
        {
            equipmentArmor=GearRoot("Equipped class costume",spine);equipmentArmor.localPosition=Vector3.down*1.12f;
            float width=CostumeRecipes.ChestWidth(heroClass,look.Tier);
            Color cloth=GameBalance.ClassColor(heroClass)*.72f;
            if(heroClass==HeroClass.Arcanist)
                Tapered("Equipped robe skirt",equipmentArmor,new Vector3(0,.4f,0),.48f,.30f,.62f,Vector3.zero,cloth,20);
            if(heroClass==HeroClass.Arcanist)
            {
                for(int side=-1;side<=1;side+=2)
                {
                    Part("Embroidered robe panel",PrimitiveType.Cube,new Vector3(side*width*.30f,.99f,.35f),new Vector3(width*.38f,.99f,.06f),cloth,equipmentArmor,VisualSurface.Cloth).localRotation=Quaternion.Euler(-5,0,side*4);
                    Part("Robe metal seam",PrimitiveType.Cube,new Vector3(side*width*.47f,1.05f,.39f),new Vector3(.028f,.9f,.02f),look.Accent,equipmentArmor,VisualSurface.Metal);
                }
            }
            else if(heroClass==HeroClass.Ranger)
                Part("Asymmetric leather chest wrap",PrimitiveType.Cube,new Vector3(-.08f,1.33f,.33f),new Vector3(width,.42f,.08f),cloth,equipmentArmor,VisualSurface.Cloth).localRotation=Quaternion.Euler(0,0,-17);
            else
            {
                Part("Spirit ritual bib",PrimitiveType.Cube,new Vector3(0,1.34f,.35f),new Vector3(width,.33f,.09f),cloth,equipmentArmor,VisualSurface.Cloth);
                CostumeMesh("Contract chest crystal",WingSilhouette.Crystal,equipmentArmor,new Vector3(0,1.1f,.44f),new Vector3(.65f,.37f,.6f),look.Glow,VisualSurface.Crystal);
            }
            for(int side=-1;side<=1;side+=2)
            {
                if(heroClass==HeroClass.Ranger&&side==1)continue;
                Transform shoulder=GearRoot("Equipped class shoulder",side<0?leftArm:rightArm);
                if(side<0)equipmentLeftShoulder=shoulder;else equipmentRightShoulder=shoulder;
                if(heroClass==HeroClass.Summoner)
                    for(int i=0;i<3;i++)CostumeMesh("Tiered leaf shoulder",WingSilhouette.Feather,shoulder,new Vector3(side*i*.09f,.08f,0),new Vector3(1,.38f+look.Tier*.025f,1),cloth,VisualSurface.Foliage).localRotation=Quaternion.Euler(0,0,side*(105+i*14));
                else Part("Tailored shoulder mantle",PrimitiveType.Sphere,new Vector3(0,0,.04f),new Vector3(heroClass==HeroClass.Ranger?.4f:.25f,.15f,.35f),cloth,shoulder,VisualSurface.Cloth);
            }
            Part("Costume fastening",PrimitiveType.Sphere,new Vector3(0,1.49f,.42f),Vector3.one*(.1f+look.Tier*.017f),look.Accent,equipmentArmor,VisualSurface.Metal);
            BuildClassTierGeometry(look);
            BuildClassUpgradeGeometry(look);
        }
        // Item level selects construction, independently of rarity pigment and upgrade insignia.
        // Every piece belongs to a replaced equipment root; no extra chest layer or relic overlap.
        private void BuildClassTierGeometry(EquipmentAppearance look)
        {
            Color cloth=GameBalance.ClassColor(heroClass)*.66f,lining=GameBalance.ClassColor(heroClass)*.9f;
            Color wood=new Color(.30f,.20f,.12f);int tier=look.Tier;
            if(heroClass==HeroClass.Arcanist)
            {
                for(int side=-1;side<=1;side+=2)
                {
                    Part("Scholar split side lining",PrimitiveType.Cube,new Vector3(side*.31f,.63f,.15f),new Vector3(.16f,.92f,.055f),lining,equipmentArmor,VisualSurface.Cloth).localRotation=Quaternion.Euler(0,side*32,side*5);
                    if(tier>=2)Part("Raised scholar collar",PrimitiveType.Cube,new Vector3(side*.23f,1.57f,.06f),new Vector3(.12f,.25f,.27f),cloth,equipmentArmor,VisualSurface.Cloth).localRotation=Quaternion.Euler(-10,0,side*13);
                    if(tier>=3)Part("Split scholar rear stole",PrimitiveType.Cube,new Vector3(side*.22f,.81f,-.30f),new Vector3(.23f,1.13f,.05f),cloth,equipmentArmor,VisualSurface.Cloth).localRotation=Quaternion.Euler(7,0,-side*7);
                    if(tier>=4)Part("Arch scholar star ray",PrimitiveType.Cube,new Vector3(side*.26f,1.40f,.39f),new Vector3(.055f,.29f,.055f),look.Metal,equipmentArmor,VisualSurface.Metal).localRotation=Quaternion.Euler(0,0,side*42);
                }
                if(tier>=3)CostumeMesh("Scholar orbit yoke",WingSilhouette.Mechanical,equipmentArmor,new Vector3(0,1.46f,.31f),new Vector3(.37f,.23f,.20f),look.Metal,VisualSurface.Metal);
                if(tier>=4)Part("Arch scholar under-robe hem",PrimitiveType.Cube,new Vector3(0,.26f,-.06f),new Vector3(.54f,.30f,.18f),lining,equipmentArmor,VisualSurface.Cloth);
            }
            else if(heroClass==HeroClass.Ranger)
            {
                Part("Hunter quiver harness",PrimitiveType.Cube,new Vector3(.29f,1.39f,-.28f),new Vector3(.09f,.64f,.08f),wood,equipmentArmor,VisualSurface.Cloth).localRotation=Quaternion.Euler(0,0,-17);
                if(tier>=2)
                {
                    for(int i=0;i<2;i++)Part("Hunter quiver retaining hoop",PrimitiveType.Cube,new Vector3(.34f,1.22f+i*.31f,-.36f),new Vector3(.31f,.07f,.30f),wood,equipmentArmor,VisualSurface.Wood);
                    Part("Hunter offside utility flap",PrimitiveType.Cube,new Vector3(-.30f,.89f,.14f),new Vector3(.17f,.34f,.08f),cloth,equipmentArmor,VisualSurface.Cloth).localRotation=Quaternion.Euler(0,0,-13);
                }
                if(tier>=3)for(int i=0;i<3;i++)Part("Hunter layered hip tab",PrimitiveType.Cube,new Vector3(-.31f-i*.035f,.99f-i*.12f,.20f),new Vector3(.21f,.20f,.045f),i%2==0?lining:cloth,equipmentArmor,VisualSurface.Cloth).localRotation=Quaternion.Euler(0,0,-15-i*7);
                if(tier>=4)
                {
                    Part("Master hunter raised left guard",PrimitiveType.Cube,new Vector3(-.10f,.12f,-.05f),new Vector3(.35f,.14f,.32f),wood,equipmentLeftShoulder,VisualSurface.Wood).localRotation=Quaternion.Euler(0,0,14);
                    for(int i=0;i<2;i++)Part("Master hunter quiver rail",PrimitiveType.Cube,new Vector3(.22f+i*.24f,1.48f,-.36f),new Vector3(.035f,.53f,.04f),look.Metal,equipmentArmor,VisualSurface.Metal);
                }
            }
            else if(heroClass==HeroClass.Summoner)
            {
                for(int side=-1;side<=1;side+=2)
                {
                    Part("Split contract shawl tail",PrimitiveType.Cube,new Vector3(side*.29f,1.24f,-.17f),new Vector3(.23f,.57f,.055f),cloth,equipmentArmor,VisualSurface.Cloth).localRotation=Quaternion.Euler(12,0,-side*18);
                    if(tier>=2)Part("Hanging contract tablet",PrimitiveType.Cube,new Vector3(side*.27f,1.23f,.36f),new Vector3(.13f,.28f,.06f),wood,equipmentArmor,VisualSurface.Wood).localRotation=Quaternion.Euler(0,0,side*9);
                    if(tier>=3)CostumeMesh("Divided contract shoulder fan",WingSilhouette.Feather,side<0?equipmentLeftShoulder:equipmentRightShoulder,new Vector3(side*.16f,.13f,-.09f),new Vector3(.8f,.45f,.9f),lining,VisualSurface.Foliage).localRotation=Quaternion.Euler(12,0,side*105);
                }
                if(tier>=3)
                {
                    equipmentHead=GearRoot("Equipped contract crown forks",headRig);
                    for(int side=-1;side<=1;side+=2)
                    {
                        Part("Contract crown outer fork",PrimitiveType.Capsule,new Vector3(side*.36f,.49f,-.10f),new Vector3(.055f,.19f,.06f),wood,equipmentHead,VisualSurface.Wood).localRotation=Quaternion.Euler(-12,0,side*39);
                        if(tier>=4)Part("Elder contract crown branch",PrimitiveType.Capsule,new Vector3(side*.46f,.60f,-.10f),new Vector3(.045f,.17f,.05f),wood,equipmentHead,VisualSurface.Wood).localRotation=Quaternion.Euler(0,0,-side*28);
                    }
                }
                if(tier>=4)for(int side=-1;side<=1;side+=2)Part("Elder contract split clasp",PrimitiveType.Cube,new Vector3(side*.16f,1.50f,.34f),new Vector3(.12f,.19f,.06f),look.Metal,equipmentArmor,VisualSurface.Metal).localRotation=Quaternion.Euler(0,0,side*24);
            }
        }
    }
}
