using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private void SelectInventoryTab(int tab)
        {
            if(mobileInventoryTab==tab)return;
            mobileInventoryTab=tab;mobileInventoryDetail=false;inventoryComparisonOpen=false;inventoryPopupItem=null;
            CancelMobileScroll();BlockUITransition();
        }
        private float inventoryActionUntil=-1;
        private bool inventoryComparisonOpen;
        private Vector2 inventoryComparisonScroll;
        private bool InventoryAction(Rect rect,string caption,bool enabled,string reason=null)
        {bool pressed=Button(rect,caption,jade,enabled,reason);if(!pressed||Time.unscaledTime<inventoryActionUntil)return false;inventoryActionUntil=Time.unscaledTime+.25f;return true;}

        // Compact visuals keep their full hit rectangle; no nested button frame.
        private bool QuietAction(Rect hit,string caption,bool enabled=true,string hint=null,bool selected=false)
        {
            float u=MobileControls.Active?TouchRatio:1f;
            bool prior=GUI.enabled;enabled=enabled&&prior;
            Color ink=enabled?(selected?gold:jade):muted;
            Text(hit,caption,Mathf.RoundToInt(12*u),ink,selected,false,TextAnchor.MiddleCenter);
            if(selected)Fill(new Rect(hit.x+8*u,hit.yMax-3*u,Mathf.Max(0,hit.width-16*u),2*u),gold);
            if(hit.Contains(Mouse)&&!string.IsNullOrEmpty(hint))tooltip=hint;
            GUI.enabled=enabled;bool clicked=GUI.Button(hit,GUIContent.none,invisibleButton);GUI.enabled=prior;
            if(clicked)GameAudio.Play(SoundCue.UI);
            return clicked;
        }
        private bool QuietInventoryAction(Rect hit,string caption,bool enabled,string reason=null)
        {bool pressed=QuietAction(hit,caption,enabled,reason);if(!pressed||Time.unscaledTime<inventoryActionUntil)return false;inventoryActionUntil=Time.unscaledTime+.25f;return true;}

        private bool DrawInventorySortIcon(Rect hit)
        {
            float u=MobileControls.Active?TouchRatio:1f;
            float x=hit.center.x-10*u,y=hit.center.y-7*u;
            for(int row=0;row<3;row++)Fill(new Rect(x,y+row*6*u,(20-row*5)*u,2*u),jade);
            Text(new Rect(x+14*u,y+3*u,10*u,18*u),"↓",Mathf.RoundToInt(12*u),jade,true);
            return QuietAction(hit,"",true,"排序 · 当前："+MobileInventorySortLabel);
        }

        private bool DrawInventoryLock(Rect hit,bool locked)
        {
            bool click=GUI.Button(hit,GUIContent.none,invisibleButton)&&Time.unscaledTime>=inventoryActionUntil;
            if(click)inventoryActionUntil=Time.unscaledTime+.25f;
            float size=Mathf.Min(18* (MobileControls.Active?TouchRatio:1),hit.width*.45f);
            DrawIcon(new Rect(hit.center.x-size*.5f,hit.center.y-size*.5f,size,size),UIIconAtlas.EquipmentLock(locked),Color.white);
            return click;
        }

        private void DrawInventoryComparison(Rect area,ItemData candidate,float u)
        {
            var p=session.Progression;var current=p.Equipped(candidate.slot);var next=p.PreviewEquippedItem(candidate);
            Fill(area,card);Fill(new Rect(area.x,area.y,area.width,u),jade*.4f);
            Text(new Rect(area.x+8*u,area.y+4*u,area.width-60*u,26*u),"属性对比 · "+candidate.name,Mathf.RoundToInt(12*u),pale,true);
            if(Button(new Rect(area.xMax-48*u,area.y,44*u,44*u),"×",jade)){inventoryComparisonOpen=false;return;}
            Rect body=new Rect(area.x+6*u,area.y+46*u,area.width-12*u,Mathf.Max(20*u,area.height-50*u));
            string comparison="当前 → 候选"+(IsEquipped(candidate)?"（已穿戴）":"")+"\n攻击 "+(current==null?0:current.attack)+" → "+next.attack+"   防御 "+(current==null?0:current.defense)+" → "+next.defense+"\n生命 "+(current==null?0:current.health)+" → "+next.health+"\n"+EquipmentComparisonPresentation.Changes(current,candidate,p.Profile.heroClass)+"\n"+EquipmentComparisonPresentation.Description(candidate,p.Profile.heroClass);
            float h=Style(Mathf.RoundToInt(12*u),false,true).CalcHeight(new GUIContent(comparison),body.width-18*u)+12*u;
            inventoryComparisonScroll=BeginTouchScroll("inventory-comparison",body,inventoryComparisonScroll,new Rect(0,0,body.width-18*u,Mathf.Max(body.height,h)));
            Text(new Rect(0,0,body.width-18*u,h),comparison,Mathf.RoundToInt(12*u),pale,false,true);
            EndTouchScroll();
        }

        private Rect inventoryPopupAnchor,inventoryPopupRect;
        private string inventoryPopupItem;
        private bool inventoryPopupCompare;
        private int inventoryPopupOpened=-1,inventoryPopupDismissed=-1;
        private void PrepareInventoryPopupInput()
        {
            if(!inventoryComparisonOpen)return;
            var e=Event.current;
            if(e.type==EventType.ScrollWheel || e.type==EventType.MouseDown&&!inventoryPopupRect.Contains(Mouse))
            {inventoryComparisonOpen=false;inventoryPopupDismissed=Time.frameCount;CancelMobileScroll();BlockUITransition();e.Use();}
        }
        private void OpenInventoryPopup(string id,Rect anchor)
        {
            inventoryPopupItem=id;inventoryPopupAnchor=anchor;inventoryPopupCompare=false;
            inventoryComparisonOpen=true;inventoryPopupOpened=Time.frameCount;inventoryComparisonScroll=Vector2.zero;
            if(!id.StartsWith("@")){selectedItem=id;ReviewEquipment(session.Progression.Profile.inventory.Find(v=>v!=null&&v.id==id));}
            CancelMobileScroll();
        }
        private void DrawInventoryIcon(Rect tile,ItemData item,float u)
        {
            Color rarity=GameBalance.RarityColor(item.rarity);
            Fill(tile,card);Border(tile,rarity);
            DrawIcon(new Rect(tile.x+6*u,tile.y+5*u,tile.width-12*u,tile.height-17*u),UIIconAtlas.EquipmentCardIcon(item.slot),rarity);
            // Counted pips encode rarity without relying on color alone.
            for(int pip=0;pip<=(int)item.rarity;pip++)Fill(new Rect(tile.x+3*u+pip*5*u,tile.y+3*u,3*u,3*u),pale);
            Text(new Rect(tile.x+2*u,tile.yMax-15*u,tile.width-4*u,14*u),"L"+item.level,Mathf.RoundToInt(9*u),pale,true,false,TextAnchor.MiddleRight);
            if(item.locked)DrawIcon(new Rect(tile.xMax-14*u,tile.y+2*u,12*u,12*u),UIIconAtlas.EquipmentLock(true),Color.white);
            if(IsEquipped(item))Text(new Rect(tile.x+2*u,tile.yMax-16*u,14*u,14*u),"✓",Mathf.RoundToInt(10*u),jade,true);
            if(tile.Contains(Mouse))tooltip=item.name+" · "+GameBalance.RarityName(item.rarity)+" · Lv."+item.level;
        }
        private void DrawEquipmentIconGrid(Rect viewport,ref Vector2 scroll,float u)
        {
            var full=new MobilePanelLayout.Area(viewport.x/u,viewport.y/u,viewport.width/u,viewport.height/u);
            string[] filters={"全部","武器","护甲","饰品"};
            for(int index=0;index<filters.Length;index++)
            {
                var area=InventoryGridGeometry.FilterButton(full,index);
                Rect hit=new Rect(area.X*u,area.Y*u,area.Width*u,area.Height*u);
                bool selected=inventoryFilter==index-1;
                if(selected)Fill(hit,new Color(gold.r,gold.g,gold.b,.08f));
                if(QuietAction(hit,filters[index],!inventoryComparisonOpen,null,selected))
                {inventoryFilter=index-1;scroll=Vector2.zero;RebuildBagItems();ResolveSelectedItem();CancelMobileScroll();BlockUITransition();}
            }
            viewport.width-=InventoryGridGeometry.FilterRailWidth*u;
            float available=viewport.width/u-18;var geometry=new InventoryGridGeometry(available);
            float contentHeight=Mathf.Max(viewport.height/u,((bagItems.Count+geometry.Columns-1)/geometry.Columns)*InventoryGridGeometry.RowHeight+4);
            bool previous=GUI.enabled;GUI.enabled=previous&&!inventoryComparisonOpen&&inventoryPopupDismissed!=Time.frameCount;
            Vector2 before=scroll;
            scroll=BeginTouchScroll("inventory-icon-grid",viewport,scroll,new Rect(0,0,available*u,contentHeight*u));
            string chosen=null;Rect anchor=default;
            for(int index=0;index<bagItems.Count;index++)
            {
                var cell=geometry.Tile(index);Rect tile=new Rect(cell.X*u,cell.Y*u,cell.Width*u,cell.Height*u);
                if(tile.yMax<scroll.y||tile.y>scroll.y+viewport.height)continue;
                var item=bagItems[index];DrawInventoryIcon(tile,item,u);
                if(GUI.Button(tile,GUIContent.none,invisibleButton)){chosen=item.id;anchor=new Rect(viewport.x+tile.x,viewport.y+tile.y-scroll.y,tile.width,tile.height);}
            }
            if(bagItems.Count==0)Text(new Rect(8*u,12*u,available*u-16*u,48*u),"这个分类暂无装备",Mathf.RoundToInt(14*u),muted);
            EndTouchScroll();GUI.enabled=previous;
            if(scroll!=before)inventoryComparisonOpen=false;
            if(chosen!=null)OpenInventoryPopup(chosen,anchor);
            DrawInventoryPopup(viewport,u);
        }
        private void DrawInventoryPopup(Rect bounds,float u)
        {
            if(!inventoryComparisonOpen)return;
            bool potion=inventoryPopupItem=="@potion",fashion=inventoryPopupItem!=null&&inventoryPopupItem.StartsWith("@fashion:");
            var appearance=fashion?session.Progression.Profile.fashions.Find(v=>v!=null&&v.id==inventoryPopupItem.Substring(9)):null;
            var item=potion||fashion?null:session.Progression.Profile.inventory.Find(v=>v!=null&&v.id==(inventoryPopupItem??selectedItem));
            if(!potion&&!fashion&&item==null||fashion&&appearance==null){inventoryComparisonOpen=false;return;}
            var area=InventoryGridGeometry.Popup(new MobilePanelLayout.Area(bounds.x/u,bounds.y/u,bounds.width/u,bounds.height/u),new MobilePanelLayout.Area(inventoryPopupAnchor.x/u,inventoryPopupAnchor.y/u,inventoryPopupAnchor.width/u,inventoryPopupAnchor.height/u),inventoryPopupCompare);
            Rect r=inventoryPopupRect=new Rect(area.X*u,area.Y*u,area.Width*u,area.Height*u);
            Fill(r,new Color(.025f,.055f,.075f,.99f));Border(r,jade);
            Text(new Rect(r.x+8*u,r.y+4*u,r.width-56*u,36*u),potion?"生命药剂 × "+session.Progression.Profile.potions:fashion?ProgressionService.FashionName(appearance.slot,appearance.rarity,session.Progression.Profile.heroClass):item.name,Mathf.RoundToInt(12*u),pale,true,true);
            bool prior=GUI.enabled;GUI.enabled=prior&&Time.frameCount!=inventoryPopupOpened;
            if(QuietAction(new Rect(r.xMax-44*u,r.y,44*u,44*u),"×")){inventoryComparisonOpen=false;BlockUITransition();}
            string info=potion?"恢复50%生命":fashion?"时装 · "+GameBalance.RarityName(appearance.rarity)+" · "+(appearance.slot==FashionSlot.Wings?"翅膀":"武器外观"):GameBalance.RarityName(item.rarity)+" · Lv."+item.level+" · 评分 "+EquipmentPreviewScore(item).ToString("0.#");
            Text(new Rect(r.x+8*u,r.y+42*u,r.width-16*u,22*u),info,Mathf.RoundToInt(10*u),muted);
            float actionWidth=(r.width-60*u)*.5f;
            if(potion)
            {
                bool usable=session.Progression.Profile.potions>0&&session.Player!=null&&session.Player.Health<session.Player.MaxHealth-.5f;
                if(QuietInventoryAction(new Rect(r.x+8*u,r.y+68*u,r.width-16*u,44*u),"使用",usable,usable?null:"暂无药剂或已满血")){session.DrinkPotion();inventoryPopupOpened=Time.frameCount;}
            }
            else if(fashion)
            {
                var current=session.Progression.EquippedFashion(appearance.slot);bool worn=current!=null&&current.id==appearance.id;
                if(QuietInventoryAction(new Rect(r.x+8*u,r.y+68*u,r.width-16*u,44*u),worn?"卸下":"穿戴",true))
                {MobileFashionResult(worn?session.Progression.UnequipFashion(appearance.slot):session.Progression.EquipFashion(appearance.id),worn?"已卸下外观":"外观已穿戴");inventoryPopupOpened=Time.frameCount;}
            }
            else
            {
                if(QuietInventoryAction(new Rect(r.x+6*u,r.y+68*u,actionWidth,44*u),IsEquipped(item)?"已穿戴":"穿戴",!IsEquipped(item)&&item.level<=session.Progression.Profile.level))
                {MobileInventoryResult(session.Progression.Equip(item.id),"装备已穿戴");inventoryPopupOpened=Time.frameCount;}
                if(QuietAction(new Rect(r.x+8*u+actionWidth,r.y+68*u,actionWidth,44*u),"对比",true,null,inventoryPopupCompare)){inventoryPopupCompare=!inventoryPopupCompare;inventoryComparisonScroll=Vector2.zero;}
                if(DrawInventoryLock(new Rect(r.xMax-48*u,r.y+68*u,44*u,44*u),item.locked))
                {MobileInventoryResult(session.Progression.SetItemLocked(item.id,!item.locked),item.locked?"已解锁":"已锁定");inventoryPopupOpened=Time.frameCount;}
            }
            if(inventoryPopupCompare&&!potion&&!fashion)
            {
                var p=session.Progression;var current=p.Equipped(item.slot);var next=p.PreviewEquippedItem(item);
                string text="当前 → 候选\n攻击 "+(current==null?0:current.attack)+" → "+next.attack+"\n防御 "+(current==null?0:current.defense)+" → "+next.defense+"\n生命 "+(current==null?0:current.health)+" → "+next.health+"\n"+EquipmentComparisonPresentation.Changes(current,item,p.Profile.heroClass)+"\n"+EquipmentComparisonPresentation.Description(item,p.Profile.heroClass);
                Rect body=new Rect(r.x+6*u,r.y+116*u,r.width-12*u,Mathf.Max(24*u,r.height-120*u));float h=Style(Mathf.RoundToInt(11*u),false,true).CalcHeight(new GUIContent(text),body.width-18*u)+8*u;
                inventoryComparisonScroll=BeginTouchScroll("inventory-popup-comparison",body,inventoryComparisonScroll,new Rect(0,0,body.width-18*u,Mathf.Max(body.height,h)));
                Text(new Rect(0,0,body.width-18*u,h),text,Mathf.RoundToInt(11*u),pale,false,true);EndTouchScroll();
            }
            GUI.enabled=prior;
        }
        private bool DrawMobileEquipmentGrid(MobilePanelLayout.Area viewport,bool wide)
        {DrawEquipmentIconGrid(MobilePanelRect(viewport),ref mobileInventoryListScroll,TouchRatio);return false;}
    }
}
