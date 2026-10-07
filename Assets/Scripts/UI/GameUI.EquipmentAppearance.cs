using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private bool equipmentAppearanceOpen,equipmentAppearanceCandidate,equipmentAppearanceDetail;
        private string equipmentAppearanceItem;
        private Vector2 equipmentAppearanceScroll;
        private void DrawMobileEquipmentAppearance(Rect viewport,ItemData item,float u)
        {
            if(equipmentAppearanceItem!=item.id)equipmentAppearanceScroll=Vector2.zero;
            // Controls, mannequin and override explanation belong to the clipped body.
            // Short landscape panels scroll this content; the fixed footer owns its area.
            float contentHeight=Mathf.Max(viewport.height,410*u);
            float contentWidth=Mathf.Max(1,viewport.width-18*u);
            equipmentAppearanceScroll=BeginTouchScroll("equipment-appearance",viewport,equipmentAppearanceScroll,
                new Rect(0,0,contentWidth,contentHeight));
            DrawEquipmentAppearanceDetail(new Rect(0,0,contentWidth,contentHeight),item,u);
            EndTouchScroll();
        }
        private bool DrawEquipmentAppearanceDetail(Rect area,ItemData item,float u)
        {
            if(!equipmentAppearanceOpen||item==null)return false;
            if(equipmentAppearanceItem!=item.id){equipmentAppearanceItem=item.id;equipmentAppearanceCandidate=false;}
            float gap=8*u,half=(area.width-gap)*.5f;
            if(TabButton(new Rect(area.x,area.y,half,44*u), "当前装备", !(equipmentAppearanceCandidate))){equipmentAppearanceCandidate=false;BlockUITransition();}
            if(TabButton(new Rect(area.x+half+gap,area.y,half,44*u), "候选装备", equipmentAppearanceCandidate)){equipmentAppearanceCandidate=true;BlockUITransition();}
            if(ToggleButton(new Rect(area.x,area.y+50*u,half,40*u), equipmentAppearanceDetail?"展示观看":"战斗观看", equipmentAppearanceDetail)){equipmentAppearanceDetail=!equipmentAppearanceDetail;BlockUITransition();}
            if(NavigationButton(new Rect(area.x+half+gap,area.y+50*u,half,40*u), "返回属性", jade)){equipmentAppearanceOpen=false;ReleaseCollectionModel();BlockUITransition();return true;}
            var p=session.Progression;
            ItemData weapon=p.Equipped(ItemSlot.Weapon),armor=p.Equipped(ItemSlot.Armor),relic=p.Equipped(ItemSlot.Relic);
            // PreviewEquippedItem returns a detached copy with persistent slot upgrades.
            ItemData candidate=p.PreviewEquippedItem(item);
            if(equipmentAppearanceCandidate)
            {
                if(item.slot==ItemSlot.Weapon)weapon=candidate;
                else if(item.slot==ItemSlot.Armor)armor=candidate;
                else if(item.slot==ItemSlot.Relic)relic=candidate;
            }
            var wings=p.EquippedFashion(FashionSlot.Wings);var fashionWeapon=p.EquippedFashion(FashionSlot.Weapon);
            if(collectionModel==null)collectionModel=new CollectionModelPreview();
            collectionModel.SetComposition(CollectionPreviewComposition.Full);collectionModel.SetYaw(20);
            collectionModel.SetEquipmentFraming(true,equipmentAppearanceDetail);
            collectionModel.SetEquipmentHighlight((int)item.slot);
            if(Button(new Rect(area.x,area.y+96*u,half,40*u),"短移动（表现）",jade)){collectionModel.Play(CollectionPreviewAction.Move);BlockUITransition();}
            if(Button(new Rect(area.x+half+gap,area.y+96*u,half,40*u),"普攻（表现）",jade)){collectionModel.Play(CollectionPreviewAction.Attack);BlockUITransition();}
            Rect viewport=new Rect(area.x,area.y+142*u,area.width,Mathf.Max(48*u,area.height-234*u));
            collectionModel.SetViewport(viewport.width*Mathf.Abs(GUI.matrix.m00),viewport.height*Mathf.Abs(GUI.matrix.m11),MobileControls.Active);
            Texture image=collectionModel.Render(p.Profile.heroClass,weapon,armor,relic,wings,fashionWeapon);
            Fill(viewport,new Color(.035f,.06f,.09f));if(image!=null)GUI.DrawTexture(viewport,image,ScaleMode.ScaleToFit,false);
            string note=(equipmentAppearanceCandidate?"候选":"当前")+" · "+(equipmentAppearanceDetail?"展示观看":"战斗观看")+" · "+
                (fashionWeapon!=null?"兵装外观覆盖武器":"部位强化 +"+p.SlotUpgradeRank(item.slot));
            Text(new Rect(area.x,viewport.yMax+4*u,area.width,30*u),note,Mathf.RoundToInt(11*u),muted,false,true);
            bool worn=IsEquipped(item),canEquip=item.level<=p.Profile.level&&!worn;
            if(Button(new Rect(area.x,viewport.yMax+40*u,area.width,40*u),worn?"已穿戴":canEquip?"穿戴预览装备":"需要 "+item.level+" 级",jade,canEquip))Feedback(p.Equip(item.id),"装备已穿戴 · 挂件沿用");
            return true;
        }
    }
}
