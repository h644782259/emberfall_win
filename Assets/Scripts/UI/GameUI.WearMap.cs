using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private CollectionModelPreview wearModel;
        private bool inventoryFashionOpen;
        private void DrawCurrentWear(Rect area,float u)
        {
            var p=session.Progression;
            if(wearModel==null)wearModel=new CollectionModelPreview();
            Rect viewport=new Rect(area.x+38*u,area.y,area.width-38*u,Mathf.Max(64*u,area.height-48*u));
            wearModel.SetComposition(CollectionPreviewComposition.Full);wearModel.SetYaw(inventoryFashionOpen&&collectionTrial!=null&&collectionTrial.slot==FashionSlot.Wings?160:20);
            wearModel.SetViewport(viewport.width*Mathf.Abs(GUI.matrix.m00),viewport.height*Mathf.Abs(GUI.matrix.m11),MobileControls.Active);
            Texture current=wearModel.Render(p.Profile.heroClass,p.Equipped(ItemSlot.Weapon),p.Equipped(ItemSlot.Armor),p.Equipped(ItemSlot.Relic),inventoryFashionOpen&&collectionTrial!=null&&collectionTrial.slot==FashionSlot.Wings?collectionTrial:p.EquippedFashion(FashionSlot.Wings),inventoryFashionOpen&&collectionTrial!=null&&collectionTrial.slot==FashionSlot.Weapon?collectionTrial:p.EquippedFashion(FashionSlot.Weapon));
            if(current!=null)GUI.DrawTexture(viewport,current,ScaleMode.ScaleToFit,false);
            else Text(viewport,"角色预览正在恢复",Mathf.RoundToInt(11*u),muted,false,true);
            if(inventoryFashionOpen&&collectionTrial!=null)Text(new Rect(viewport.x,viewport.y,viewport.width,20*u),"试穿 · 未保存",Mathf.RoundToInt(11*u),gold,true);
            for(int slot=0;slot<3;slot++)
            {
                var item=p.Equipped((ItemSlot)slot);Rect r=new Rect(area.x,area.y+slot*48*u,44*u,44*u);
                if(Button(r,"",jade,item!=null))
                {selectedItem=item.id;mobileInventoryTab=0;inventoryComparisonOpen=true;mobileInventoryDetailScroll=Vector2.zero;}
                DrawIcon(new Rect(r.x+6*u,r.y+3*u,30*u,28*u),UIIconAtlas.EquipmentCardIcon((ItemSlot)slot),item==null?muted:GameBalance.RarityColor(item.rarity));
                Text(new Rect(r.x,r.y+29*u,r.width,14*u),GameBalance.SlotName((ItemSlot)slot),Mathf.RoundToInt(9*u),jade,true,false,TextAnchor.MiddleCenter);
                if(r.Contains(Mouse)&&item!=null)tooltip=item.name+" · 已穿戴";
            }
            if(Button(new Rect(area.x,area.yMax-44*u,area.width,40*u),"时装穿戴",gold))
            {if(MobileControls.Active){inventoryFashionOpen=!inventoryFashionOpen;collectionTrial=null;mobileInventoryDetail=false;}else panel=Panel.Fashion;BlockUITransition();}
        }
        private void DrawBagFashion(MobilePanelLayout.Area area)
        {
            var p=session.Progression;float u=TouchRatio;
            for(int slot=0;slot<2;slot++)
                if(TabButton(MobilePanelRect(new MobilePanelLayout.Area(area.X+slot*90,area.Y,84,36)),slot==0?"翅膀":"武器外观",mobileFashionSlot==slot))
                {mobileFashionSlot=slot;mobileFashionScroll=Vector2.zero;collectionTrial=null;}
            if(Button(MobilePanelRect(new MobilePanelLayout.Area(area.XMax-94,area.Y,94,36)),"返回装备",jade))
            {inventoryFashionOpen=false;collectionTrial=null;return;}
            var body=new MobilePanelLayout.Area(area.X,area.Y+42,area.Width,area.Height-42);
            float width=body.Width-18,cell=(width-8)*.5f;int trial=-1;string equip=null;
            mobileFashionScroll=BeginTouchScroll("bag-fashion",MobilePanelRect(body),mobileFashionScroll,new Rect(0,0,width*u,Mathf.Max(body.Height,282)*u));
            for(int rank=0;rank<4;rank++)
            {
                var slot=(FashionSlot)mobileFashionSlot;var rarity=(Rarity)rank;
                string id="fashion-"+mobileFashionSlot+"-"+rank;
                bool owned=p.Profile.fashions.Exists(f=>f!=null&&f.id==id);
                var worn=p.EquippedFashion(slot);bool current=worn!=null&&worn.id==id;
                float x=(rank%2)*(cell+8),y=(rank/2)*116;
                Fill(TouchRect(x,y,cell,110),card);
                Text(TouchRect(x+6,y+5,cell-12,25),ProgressionService.FashionName(slot,rarity),TouchFont(13),GameBalance.RarityColor(rarity),true);
                Text(TouchRect(x+6,y+31,cell-12,23),current?"穿戴中":owned?"已拥有":"未解锁 · 可试穿",TouchFont(11),owned?jade:muted);
                if(Button(TouchRect(x+4,y+59,cell*.5f-6,46),"试穿",jade))trial=rank;
                if(Button(TouchRect(x+cell*.5f+2,y+59,cell*.5f-6,46),current?"已穿":"穿戴",gold,owned&&!current))equip=id;
            }
            bool remove=Button(TouchRect(4,234,width-8,44),"卸下此部位时装",muted,p.EquippedFashion((FashionSlot)mobileFashionSlot)!=null);
            EndTouchScroll();
            if(trial>=0)TrialFashion((FashionSlot)mobileFashionSlot,(Rarity)trial);
            if(equip!=null){MobileFashionResult(p.EquipFashion(equip),"外观已穿戴");collectionTrial=null;}
            if(remove){MobileFashionResult(p.UnequipFashion((FashionSlot)mobileFashionSlot),"已卸下外观，收藏属性保留");collectionTrial=null;}
        }

        private void DrawBagSupplies(MobilePanelLayout.Area area)
        {
            var p=session.Progression;
            float cell=Mathf.Min(138,area.Width-18);
            DrawIcon(MobilePanelRect(new MobilePanelLayout.Area(area.X+6,area.Y+4,26,26)),UIIconAtlas.Utility("potion"),jade);
            Text(MobilePanelRect(new MobilePanelLayout.Area(area.X+38,area.Y+4,cell-42,20)),"药剂 × "+p.Profile.potions,TouchFont(12),pale,true);
            string reason=p.Profile.potions<=0?"暂无药剂":session.Player==null?"无法使用":session.Player.Health>=session.Player.MaxHealth-.5f?"已满血":"恢复50%生命";
            Text(MobilePanelRect(new MobilePanelLayout.Area(area.X+6,area.Y+27,cell-12,17)),reason,TouchFont(10),muted);
            if(InventoryAction(MobilePanelRect(new MobilePanelLayout.Area(area.X+4,area.Y+44,cell-8,44)),"使用",p.Profile.potions>0&&session.Player!=null&&session.Player.Health<session.Player.MaxHealth-.5f,reason))session.DrinkPotion();

        }

        // A body-location map is independent of the selected bag candidate.
        // Every slot opens the existing real composed model with worn equipment.
        private string DrawWearMap(Rect area,float u)
        {
            float cx=area.center.x,top=area.y;
            Color body=new Color(.22f,.37f,.43f);
            Fill(new Rect(cx-9*u,top+6*u,18*u,18*u),body);
            Fill(new Rect(cx-16*u,top+28*u,32*u,52*u),body);
            Fill(new Rect(cx-32*u,top+31*u,12*u,53*u),body);
            Fill(new Rect(cx+20*u,top+31*u,12*u,53*u),body);
            Fill(new Rect(cx-16*u,top+84*u,12*u,39*u),body);
            Fill(new Rect(cx+4*u,top+84*u,12*u,39*u),body);
            string chosen=null;
            for(int slot=0;slot<3;slot++)
            {
                var item=session.Progression.Equipped((ItemSlot)slot);
                float x=slot==0?area.x:slot==1?cx-24*u:area.xMax-48*u;
                float y=top+(slot==1?35:73)*u;
                Rect icon=new Rect(x,y,48*u,48*u);
                if(Button(icon,"",jade,item!=null))chosen=item.id;
                DrawIcon(new Rect(x+6*u,y+4*u,36*u,36*u),UIIconAtlas.EquipmentCardIcon((ItemSlot)slot),item==null?muted:GameBalance.RarityColor(item.rarity));
                Text(new Rect(x-7*u,y+48*u,62*u,20*u),GameBalance.SlotName((ItemSlot)slot),Mathf.RoundToInt(11*u),item==null?muted:jade,true,false,TextAnchor.MiddleCenter);
            }
            return chosen;
        }
    }
}
