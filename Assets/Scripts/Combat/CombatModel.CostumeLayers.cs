using System.Collections.Generic;
using UnityEngine;
namespace Emberfall
{
    public sealed partial class CombatModel
    {
        private readonly List<GameObject> baseOuterCostume = new List<GameObject>();
        private FashionData activeWeaponFashion;
        private void CaptureBaseCostume()
        {
            baseOuterCostume.Clear();
            foreach(Transform part in GetComponentsInChildren<Transform>(true))
                if(part.gameObject.activeSelf && CostumeLayers.IsBaseOuter(part.name))baseOuterCostume.Add(part.gameObject);
        }
        private void SetBaseCostumeVisible(bool visible)
        {
            foreach(GameObject part in baseOuterCostume)if(part!=null)part.SetActive(visible);
        }
        private void BuildClassUpgradeGeometry(EquipmentAppearance look)
        {
            int stage=CostumeLayers.UpgradeStage(look.UpgradeRank);
            if(stage==0)return;
            Color metal=look.Accent,cloth=GameBalance.ClassColor(heroClass)*.72f;
            if(heroClass==HeroClass.Arcanist)
            {
                for(int i=0;i<3;i++)Part("Stitched star-chart point",PrimitiveType.Cube,new Vector3((i-1)*.09f,1.28f+(i%2)*.09f,.40f),Vector3.one*.04f,metal,equipmentArmor,VisualSurface.Metal).localRotation=Quaternion.Euler(0,0,45);
                if(stage>=2)CostumeMesh("Astrolabe chest frame",WingSilhouette.Mechanical,equipmentArmor,new Vector3(0,1.32f,.39f),Vector3.one*.23f,metal,VisualSurface.Metal);
                if(stage>=3)for(int side=-1;side<=1;side+=2)
                    Part("Master chart collar fin",PrimitiveType.Cube,new Vector3(side*.22f,1.65f,.13f),new Vector3(.10f,.25f,.18f),cloth,equipmentArmor,VisualSurface.Cloth).localRotation=Quaternion.Euler(-12,0,side*18);
            }
            else if(heroClass==HeroClass.Ranger)
            {
                for(int i=0;i<2;i++)Part("Ranger clasp",PrimitiveType.Cube,new Vector3(-.16f+i*.22f,1.37f+i*.05f,.40f),new Vector3(.09f,.07f,.035f),metal,equipmentArmor,VisualSurface.Metal);
                if(stage>=2)for(int i=0;i<2;i++)Part("Layered ranger arm guard",PrimitiveType.Cube,new Vector3(-.05f,-.13f-i*.12f,.19f),new Vector3(.30f,.10f,.07f),cloth,equipmentLeftShoulder,VisualSurface.Cloth);
                if(stage>=3)CostumeMesh("Master ranger feather crest",WingSilhouette.Feather,equipmentLeftShoulder,new Vector3(-.16f,.06f,.04f),new Vector3(.7f,.45f,.7f),metal,VisualSurface.Metal).localRotation=Quaternion.Euler(0,0,35);
            }
            else if(heroClass==HeroClass.Summoner)
            {
                CostumeMesh("Contract branch ring",WingSilhouette.Mechanical,equipmentArmor,new Vector3(0,1.31f,.41f),Vector3.one*.21f,new Color(.38f,.26f,.14f),VisualSurface.Wood);
                if(stage>=2)for(int side=-1;side<=1;side+=2)
                    Part("Forked contract bough",PrimitiveType.Capsule,new Vector3(side*.22f,1.43f,.31f),new Vector3(.06f,.21f,.06f),metal,equipmentArmor,VisualSurface.Wood).localRotation=Quaternion.Euler(0,0,-side*28);
                if(stage>=3)for(int side=-1;side<=1;side+=2)
                    CostumeMesh("Master contract leaf",WingSilhouette.Feather,side<0?equipmentLeftShoulder:equipmentRightShoulder,new Vector3(side*.17f,.1f,0),new Vector3(.9f,.5f,.9f),cloth,VisualSurface.Foliage).localRotation=Quaternion.Euler(0,0,-side*35);
            }
        }
        private void RefreshWeaponFashion()
        {
            if(activeWeaponFashion==null||fashionWeapon==null)return;
            Transform parent=fashionWeapon.parent;
            fashionWeapon.gameObject.SetActive(false);Destroy(fashionWeapon.gameObject);
            fashionWeapon=NewJoint("Fashion Weapon",parent,Vector3.zero);
            BuildWeaponFashionShape(activeWeaponFashion);
        }
        private void BuildWeaponFashionShape(FashionData fashion)
        {
            Color accent=Color.Lerp(GameBalance.ClassColor(heroClass),GameBalance.RarityColor(fashion.rarity),.55f);
            WingSilhouette style=CostumeRecipes.WingStyle(fashion.rarity);
            if(fashion.rarity==Rarity.Rare)BuildRareWeaponFashion(accent);
            // Small structural ornaments echo the back silhouette; the blade, string and core stay readable.
            if(swordRig!=null)
            {
                for(int side=-1;side<=1;side+=2)
                    CostumeMesh("Fashion sword guard",style,fashionWeapon,WeaponAnchorLocal(WeaponVisualAnchor.SwordGuard)+new Vector3(side*.17f,style==WingSilhouette.Mechanical?.24f:.12f,0),new Vector3(.5f,.28f,.5f),accent,VisualSurface.Metal).localRotation=Quaternion.Euler(0,0,-side*65);
            }
            else if(staffRig!=null)
            {
                CostumeMesh("Fashion focus collar",WingSilhouette.Mechanical,fashionWeapon,WeaponAnchorLocal(WeaponVisualAnchor.StaffCollar),Vector3.one*.27f,accent,VisualSurface.Metal).localRotation=Quaternion.Euler(90,0,0);
                for(int side=-1;side<=1;side+=2)
                    CostumeMesh("Fashion focus fin",style,fashionWeapon,WeaponAnchorLocal(WeaponVisualAnchor.StaffCore)+new Vector3(side*.25f,-.10f,0),new Vector3(.55f,.30f,.55f),accent,style==WingSilhouette.Crystal?VisualSurface.Crystal:VisualSurface.Metal).localRotation=Quaternion.Euler(0,0,-side*30);
            }
            else if(bowRig!=null)
                for(int side=-1;side<=1;side+=2)
                    CostumeMesh("Fashion bow limb crest",style,fashionWeapon,new Vector3(.025f,side*weaponStructure.BowReach*.84f,.10f),new Vector3(.5f,.26f,.5f),accent,style==WingSilhouette.Crystal?VisualSurface.Crystal:VisualSurface.Wood).localRotation=Quaternion.Euler(0,0,side<0?180:0);
            int rank=(int)fashion.rarity;
            if(staffRig!=null&&heroClass==HeroClass.Summoner)
            {
                // Contract staff uses branching wood/leaf forms, distinct from the elementalist's crystal astrolabe.
                for(int side=-1;side<=1;side+=2)
                {
                    Vector3 at=WeaponAnchorLocal(WeaponVisualAnchor.StaffCore)+new Vector3(side*(.17f+rank*.025f),.05f,0);
                    Part("Spirit fashion antler",PrimitiveType.Capsule,at,new Vector3(.06f,.30f+rank*.06f,.06f),new Color(.34f,.22f,.1f),fashionWeapon,VisualSurface.Wood).localRotation=Quaternion.Euler(0,0,-side*28);
                    CostumeMesh("Spirit fashion leaf",WingSilhouette.Feather,fashionWeapon,at,new Vector3(.6f,.27f+rank*.05f,.6f),new Color(.25f,.75f,.5f),VisualSurface.Foliage).localRotation=Quaternion.Euler(0,0,-side*48);
                }
            }
            if(rank>=2)
            {
                int layers=rank==3?3:2;
                for(int side=-1;side<=1;side+=2)for(int layer=0;layer<layers;layer++)
                {
                    if(swordRig!=null)
                    {
                        Vector3 at=WeaponAnchorLocal(WeaponVisualAnchor.SwordGuard)+new Vector3(side*(.13f+layer*.055f),.30f+layer*.20f,0);
                        CostumeMesh("Prismatic oath blade flare",WingSilhouette.Crystal,fashionWeapon,at,new Vector3(.48f,.32f,.38f),layer%2==0?accent:Color.white,VisualSurface.Crystal).localRotation=Quaternion.Euler(0,0,-side*12);
                    }
                    else if(bowRig!=null)
                    {
                        Vector3 at=new Vector3(.035f,side*weaponStructure.BowReach*(.55f+layer*.13f),.17f);
                        CostumeMesh("Rainbow bow flight crest",WingSilhouette.Feather,fashionWeapon,at,new Vector3(.5f,.3f,.5f),layer%2==0?accent:new Color(.2f,.85f,.9f),VisualSurface.Crystal).localRotation=Quaternion.Euler(0,0,side<0?245:65);
                    }
                    else if(staffRig!=null&&heroClass==HeroClass.Arcanist)
                    {
                        Vector3 at=WeaponAnchorLocal(WeaponVisualAnchor.StaffCore)+new Vector3(side*(.21f+layer*.07f),layer*.12f,0);
                        CostumeMesh("Elemental crown prism",WingSilhouette.Crystal,fashionWeapon,at,new Vector3(.45f,.30f,.45f),layer%2==0?accent:new Color(.2f,.85f,1),VisualSurface.Crystal).localRotation=Quaternion.Euler(0,0,-side*(15+layer*12));
                    }
                    else if(staffRig!=null)
                    {
                        Vector3 at=WeaponAnchorLocal(WeaponVisualAnchor.StaffCore)+new Vector3(side*(.22f+layer*.06f),.12f+layer*.12f,.04f);
                        CostumeMesh("Ancestral crown leaf",WingSilhouette.Feather,fashionWeapon,at,new Vector3(.5f,.3f,.5f),layer%2==0?accent:new Color(.45f,1,.7f),VisualSurface.Foliage).localRotation=Quaternion.Euler(0,0,-side*(35+layer*16));
                    }
                }
            }
        }
        private void BuildRareWeaponFashion(Color accent)
        {
            if(swordRig!=null)for(int side=-1;side<=1;side+=2)
                Part("Rare forked guard tine",PrimitiveType.Cube,WeaponAnchorLocal(WeaponVisualAnchor.SwordGuard)+new Vector3(side*.29f,.13f,0),new Vector3(.055f,.29f,.065f),accent,fashionWeapon,VisualSurface.Metal).localRotation=Quaternion.Euler(0,0,-side*27);
            else if(staffRig!=null)
            {
                for(int side=-1;side<=1;side+=2)CostumeMesh("Rare split focus halo",WingSilhouette.Mechanical,fashionWeapon,WeaponAnchorLocal(WeaponVisualAnchor.StaffCore)+new Vector3(side*.13f,0,0),new Vector3(.32f,.39f,.20f),accent,VisualSurface.Metal).localRotation=Quaternion.Euler(0,side*35,0);
            }
            else if(bowRig!=null)for(int side=-1;side<=1;side+=2)
                Part("Rare recurved bow plate",PrimitiveType.Cube,new Vector3(0,side*weaponStructure.BowReach*.75f,.15f),new Vector3(.10f,.31f,.045f),accent,fashionWeapon,VisualSurface.Wood).localRotation=Quaternion.Euler(side*18,0,side*13);
        }
    }
}
