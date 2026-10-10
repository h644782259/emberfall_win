using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private void SelectInventoryTab(int tab)
        {
            if(mobileInventoryTab==tab)return;
            mobileInventoryTab=tab;mobileInventoryDetail=false;inventoryComparisonOpen=false;inventoryPopupItem=null;
            CancelMobileScroll();
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

        private bool InventoryPictogramAction(Rect hit,string caption,Texture2D icon,bool enabled=true,bool selected=false,bool throttle=false)
        {
            float u=MobileControls.Active?TouchRatio:1f;
            bool prior=GUI.enabled;enabled=enabled&&prior;
            Color ink=enabled?(selected?gold:jade):muted;
            Fill(new Rect(hit.x+2*u,hit.y+3*u,hit.width-4*u,hit.height-6*u),new Color(ink.r,ink.g,ink.b,selected?.28f:.14f));
            Border(new Rect(hit.x+2*u,hit.y+3*u,hit.width-4*u,hit.height-6*u),new Color(ink.r,ink.g,ink.b,.65f));
            float labelWidth=Style(Mathf.RoundToInt(14*u)).CalcSize(new GUIContent(caption)).x;
            float iconSize=Mathf.Min(22*u,hit.width*.25f),gap=6*u;
            float total=Mathf.Min(hit.width-8*u,iconSize+gap+labelWidth),left=hit.center.x-total*.5f;
            DrawIcon(new Rect(left,hit.center.y-iconSize*.5f,iconSize,iconSize),icon,enabled?Color.white:muted);
            Text(new Rect(left+iconSize+gap,hit.y,Mathf.Max(0,total-iconSize-gap),hit.height),caption,Mathf.RoundToInt(14*u),ink,true,false,TextAnchor.MiddleLeft);
            GUI.enabled=enabled;bool clicked=GUI.Button(hit,GUIContent.none,invisibleButton);GUI.enabled=prior;
            if(!clicked||throttle&&Time.unscaledTime<inventoryActionUntil)return false;
            if(throttle)inventoryActionUntil=Time.unscaledTime+.25f;
            GameAudio.Play(SoundCue.UI);return true;
        }

        private bool DrawInventorySortIcon(Rect hit)
        {
            float u=MobileControls.Active?TouchRatio:1f;
            float x=hit.center.x-10*u,y=hit.center.y-7*u;
            for(int row=0;row<5;row++)Fill(new Rect(x,y+row*6*u,(20-row*5)*u,2*u),jade);
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
            if(PopupCloseButton(new Rect(area.xMax-48*u,area.y,44*u,44*u))){inventoryComparisonOpen=false;return;}
            Rect body=new Rect(area.x+6*u,area.y+46*u,area.width-12*u,Mathf.Max(20*u,area.height-50*u));
            string comparison="当前 → 候选"+(IsEquipped(candidate)?"（已穿戴）":"")+"\n攻击 "+(current==null?0:current.attack)+" → "+next.attack+"   防御 "+(current==null?0:current.defense)+" → "+next.defense+"\n生命 "+(current==null?0:current.health)+" → "+next.health+"\n"+EquipmentComparisonPresentation.Changes(current,candidate,p)+"\n"+EquipmentComparisonPresentation.Description(candidate,p);
            float h=Style(Mathf.RoundToInt(12*u),false,true).CalcHeight(new GUIContent(comparison),body.width-18*u)+12*u;
            inventoryComparisonScroll=BeginTouchScroll("inventory-comparison",body,inventoryComparisonScroll,new Rect(0,0,body.width-18*u,Mathf.Max(body.height,h)));
            Text(new Rect(0,0,body.width-18*u,h),comparison,Mathf.RoundToInt(12*u),pale,false,true);
            EndTouchScroll();
        }

        private Rect inventoryPopupAnchor,inventoryPopupRect;
        private string inventoryPopupItem;
        private bool inventoryPopupCompare;
        private Vector2 inventoryDetailScroll;
        private int inventoryPopupOpened=-1,inventoryPopupDismissed=-1;
        private void PrepareInventoryPopupInput()
        {
            if(!inventoryComparisonOpen)return;
            if(!MobileControls.Active)
            {
                if(!inventoryPopupRect.Contains(Mouse)&&!inventoryPopupAnchor.Contains(Mouse))inventoryComparisonOpen=false;
                return;
            }
            var e=Event.current;
            if(e.type==EventType.ScrollWheel || e.type==EventType.MouseDown&&!inventoryPopupRect.Contains(Mouse))
            {inventoryComparisonOpen=false;inventoryPopupDismissed=Time.frameCount;CancelMobileScroll();BlockUITransition();e.Use();}
        }
        private void OpenInventoryPopup(string id,Rect anchor)
        {
            inventoryDetailScroll=Vector2.zero;inventoryPopupItem=id;inventoryPopupAnchor=anchor;inventoryPopupCompare=!id.StartsWith("@");
            inventoryComparisonOpen=true;inventoryPopupOpened=Time.frameCount;inventoryComparisonScroll=Vector2.zero;
            if(!id.StartsWith("@")){selectedItem=id;ReviewEquipment(session.Progression.Profile.inventory.Find(v=>v!=null&&v.id==id));}
            CancelMobileScroll();
        }
        private string inventoryLastClick;
        private float inventoryLastClickAt=-10;
        private bool DesktopInventoryGesture(Rect local,Rect anchor,string id)
        {
            if(MobileControls.Active)return false;
            if(local.Contains(Event.current.mousePosition)&&Event.current.type==EventType.Repaint)
            {
                if(!inventoryComparisonOpen||inventoryPopupItem!=id)OpenInventoryPopup(id,anchor);
                else inventoryPopupAnchor=anchor;
            }
            return true;
        }
        private void DesktopInventoryClick(string id)
        {
            bool twice=inventoryLastClick==id&&Time.unscaledTime-inventoryLastClickAt<.4f;
            inventoryLastClick=id;inventoryLastClickAt=Time.unscaledTime;
            if(!twice)return;
            inventoryLastClick=null;
            var p=session.Progression;
            if(id=="@potion")session.DrinkPotion();
            else if(id.StartsWith("@fashion:"))MobileFashionResult(p.EquipFashion(id.Substring(9)),"外观已穿戴");
            else MobileInventoryResult(p.Equip(id),"装备已穿戴");
        }
        private Rect DesktopInventorySheet(float w,float h)
        {
            float x=inventoryPopupAnchor.xMax+12;
            if(x+w>width-12)x=inventoryPopupAnchor.x-w-12;
            return new Rect(Mathf.Clamp(x,12,Mathf.Max(12,width-w-12)),Mathf.Clamp(inventoryPopupAnchor.y,12,Mathf.Max(12,height-h-12)),w,h);
        }
        private void DrawInventoryIcon(Rect tile,ItemData item,float u)
        {
            Color rarity=GameBalance.RarityColor(item.rarity);
            Fill(tile,card);Border(tile,rarity);
            DrawIcon(new Rect(tile.x+6*u,tile.y+5*u,tile.width-12*u,tile.height-17*u),UIIconAtlas.EquipmentCardIcon(item.slot,item.level,item.rarity,session.Progression.Profile.heroClass),rarity);
            // Counted pips encode rarity without relying on color alone.
            for(int pip=0;pip<=(int)item.rarity;pip++)Fill(new Rect(tile.x+3*u+pip*5*u,tile.y+3*u,3*u,3*u),pale);
            Text(new Rect(tile.x+2*u,tile.yMax-15*u,tile.width-4*u,14*u),"Lv"+item.level,Mathf.RoundToInt(9*u),item.level>session.Progression.Profile.level?new Color(1f,.35f,.3f):pale,true,false,TextAnchor.MiddleRight);
            if(item.locked)DrawIcon(new Rect(tile.xMax-14*u,tile.y+2*u,12*u,12*u),UIIconAtlas.EquipmentLock(true),Color.white);
            if(IsEquipped(item))DrawIcon(new Rect(tile.x+2*u,tile.yMax-16*u,14*u,14*u),UIIconAtlas.Utility("confirm"),jade);
            if(UnreviewedEquipmentUpgrade(item))DrawIcon(new Rect(tile.xMax-18*u,tile.yMax-31*u,18*u,18*u),UIIconAtlas.EquipmentUpgradeArrow(),new Color(.25f,1f,.4f));
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
                if(QuietAction(hit,filters[index],(!MobileControls.Active||!inventoryComparisonOpen),null,selected))
                {inventoryFilter=index-1;scroll=Vector2.zero;RebuildBagItems();ResolveSelectedItem();CancelMobileScroll();}
            }
            viewport.width-=InventoryGridGeometry.FilterRailWidth*u;
            float available=viewport.width/u-18;var geometry=new InventoryGridGeometry(available,MobileControls.Active?60:44);
            float contentHeight=Mathf.Max(viewport.height/u,((bagItems.Count+geometry.Columns-1)/geometry.Columns)*geometry.Stride+4);
            bool previous=GUI.enabled;GUI.enabled=previous&&(!MobileControls.Active||!inventoryComparisonOpen)&&inventoryPopupDismissed!=Time.frameCount;
            Vector2 before=scroll;
            scroll=BeginTouchScroll("inventory-icon-grid",viewport,scroll,new Rect(0,0,available*u,contentHeight*u));
            string chosen=null;Rect anchor=default;
            for(int index=0;index<bagItems.Count;index++)
            {
                var cell=geometry.Tile(index);Rect tile=new Rect(cell.X*u,cell.Y*u,cell.Width*u,cell.Height*u);
                if(tile.yMax<scroll.y||tile.y>scroll.y+viewport.height)continue;
                var item=bagItems[index];DrawInventoryIcon(tile,item,u);
                Rect screenTile=new Rect(viewport.x+tile.x,viewport.y+tile.y-scroll.y,tile.width,tile.height);
                DesktopInventoryGesture(tile,screenTile,item.id);
                if(GUI.Button(tile,GUIContent.none,invisibleButton)){if(MobileControls.Active){chosen=item.id;anchor=screenTile;}else DesktopInventoryClick(item.id);}
            }
            if(bagItems.Count==0)Text(new Rect(8*u,12*u,available*u-16*u,48*u),"这个分类暂无装备",Mathf.RoundToInt(14*u),muted);
            EndTouchScroll();GUI.enabled=previous;
            if(scroll!=before)inventoryComparisonOpen=false;
            if(chosen!=null)OpenInventoryPopup(chosen,anchor);
            DrawInventoryPopup(viewport,u);
        }
        private void DrawEquipmentSheet(ItemData item,float u)
        {
            var p=session.Progression;var next=p.PreviewEquippedItem(item);
            inventoryPopupCompare=true;
            bool compactCompare=height/u<400;
            float sheetWidth=Mathf.Min(compactCompare?620:600,(width/u)-40),sheetHeight=Mathf.Min(480,(height/u)-32);
            Rect r=inventoryPopupRect=MobileControls.Active?new Rect((width-sheetWidth*u)*.5f,(height-sheetHeight*u)*.5f,sheetWidth*u,sheetHeight*u):DesktopInventorySheet(sheetWidth,sheetHeight);
            Fill(r,new Color(.025f,.055f,.075f,.995f));Border(r,jade);
            bool prior=GUI.enabled;GUI.enabled=prior&&Time.frameCount!=inventoryPopupOpened;
            Text(new Rect(r.x+12*u,r.y+4*u,46*u,36*u),"Lv"+item.level,Mathf.RoundToInt(13*u),item.level>session.Progression.Profile.level?new Color(1f,.35f,.3f):gold,true,false,TextAnchor.MiddleLeft);
            Text(new Rect(r.x+60*u,r.y+4*u,r.width-140*u,36*u),ItemTitle(next),Mathf.RoundToInt(15*u),pale,true,false,TextAnchor.MiddleLeft);
            if(DrawInventoryLock(new Rect(r.xMax-80*u,r.y+4*u,36*u,36*u),item.locked))session.Progression.SetItemLocked(item.id,!item.locked);
            DrawDetailTag(new Rect(r.x+12*u,r.y+40*u,70*u,22*u),GameBalance.RarityName(item.rarity),GameBalance.RarityColor(item.rarity),u);
            DrawDetailTag(new Rect(r.x+90*u,r.y+40*u,70*u,22*u),GameBalance.SlotName(item.slot),jade,u);
            if(PopupCloseButton(new Rect(r.xMax-44*u,r.y,44*u,44*u))){inventoryComparisonOpen=false;GUI.enabled=prior;return;}
            float bodyY=r.y+66*u,rowHeight=(sheetHeight<360?22:28)*u;
            string[] labels={"评分","攻击","防御","生命","暴击率","暴击伤害","攻击加成"};
            float inner=r.width-24*u,statsWidth=compactCompare?(inner-12*u)*.52f:inner,labelWidth=96*u,valueWidth=(statsWidth-labelWidth)*.5f;
            var detail=ActualEquipmentPreview(next);
            inventoryDetailScroll=BeginTouchScroll("equipment-detail",new Rect(r.x+12*u,bodyY,inner,r.yMax-bodyY-60*u),inventoryDetailScroll,new Rect(0,0,inner-8*u,DrawRewardDetailRows(detail,inner/u-8,u,false)*u));
            DrawRewardDetailRows(detail,inner/u-8,u,true);
            EndTouchScroll();
            float mechanismY=compactCompare?r.y+66*u:bodyY+(inventoryPopupCompare?7:4)*rowHeight+8*u;
            float mechanismX=compactCompare?r.x+24*u+statsWidth:r.x+12*u,mechanismWidth=compactCompare?inner-statsWidth-12*u:inner;
            bool worn=IsEquipped(item);float buttonWidth=r.width-24*u;
            if(MobileControls.Active&&InventoryPictogramAction(new Rect(r.x+12*u,r.yMax-52*u,buttonWidth,44*u),worn?"脱下":"穿戴",UIIconAtlas.Utility("confirm"),worn||item.level<=p.Profile.level,worn,true))
            {
                bool saved=worn?p.Unequip(item.slot):p.Equip(item.id);MobileInventoryResult(saved,worn?"装备已脱下":"装备已穿戴");
                inventoryPopupOpened=Time.frameCount;if(saved){inventoryComparisonOpen=false;CancelMobileScroll();}
            }
            GUI.enabled=prior;
        }

        private void DrawInventoryPopup(Rect bounds,float u)
        {
            if(!inventoryComparisonOpen)return;
            bool potion=inventoryPopupItem=="@potion",fashion=inventoryPopupItem!=null&&inventoryPopupItem.StartsWith("@fashion:");
            var appearance=fashion?session.Progression.Profile.fashions.Find(v=>v!=null&&v.id==inventoryPopupItem.Substring(9)):null;
            var item=potion||fashion?null:session.Progression.Profile.inventory.Find(v=>v!=null&&v.id==(inventoryPopupItem??selectedItem));
            if(!potion&&!fashion&&item==null||fashion&&appearance==null){inventoryComparisonOpen=false;return;}
            if(item!=null){DrawEquipmentSheet(item,u);return;}
            var area=InventoryGridGeometry.Popup(new MobilePanelLayout.Area(bounds.x/u,bounds.y/u,bounds.width/u,bounds.height/u),new MobilePanelLayout.Area(inventoryPopupAnchor.x/u,inventoryPopupAnchor.y/u,inventoryPopupAnchor.width/u,inventoryPopupAnchor.height/u),inventoryPopupCompare||item!=null);
            Rect r=inventoryPopupRect=new Rect(area.X*u,area.Y*u,area.Width*u,area.Height*u);
            if(fashion){float fw=Mathf.Min(420*u,width-24*u),fh=Mathf.Min(320*u,height-24*u);r=inventoryPopupRect=MobileControls.Active?new Rect((width-fw)*.5f,(height-fh)*.5f,fw,fh):DesktopInventorySheet(fw,fh);}
            Fill(r,new Color(.025f,.055f,.075f,.99f));Border(r,jade);
            bool prior=GUI.enabled;GUI.enabled=prior&&Time.frameCount!=inventoryPopupOpened;
            if(potion||fashion)
                Text(new Rect(r.x+8*u,r.y+4*u,r.width-56*u,36*u),potion?"生命药剂 × "+session.Progression.Profile.potions:ProgressionService.FashionName(appearance.slot,appearance.AppearanceRarity,session.Progression.Profile.heroClass),Mathf.RoundToInt(14*u),pale,true,true);
            else
            {
                Color rarity=GameBalance.RarityColor(item.rarity);
                float rowY=r.y+4*u,rowHeight=36*u;
                Text(new Rect(r.x+8*u,rowY,44*u,rowHeight),"Lv"+item.level,Mathf.RoundToInt(12*u),gold,true,false,TextAnchor.MiddleLeft);
                float tagWidth=34*u;
                Rect tag=new Rect(r.xMax-44*u-tagWidth,rowY+6*u,tagWidth,24*u);
                Fill(tag,new Color(rarity.r,rarity.g,rarity.b,.18f));Border(tag,rarity);
                Text(tag,GameBalance.RarityName(item.rarity),Mathf.RoundToInt(11*u),rarity,true,false,TextAnchor.MiddleCenter);
                Rect nameRect=new Rect(r.x+54*u,rowY,r.width-170*u,rowHeight);
                Text(nameRect,item.name,Mathf.RoundToInt(14*u),pale,true,false,TextAnchor.MiddleLeft);
                if(DrawInventoryLock(new Rect(nameRect.xMax,rowY,36*u,rowHeight),item.locked))
                {if(!session.Progression.SetItemLocked(item.id,!item.locked))MobileInventoryResult(false,"");inventoryPopupOpened=Time.frameCount;}
                Text(new Rect(r.x+8*u,r.y+38*u,48*u,28*u),"评分",Mathf.RoundToInt(14*u),pale,true,false,TextAnchor.MiddleLeft);
                Text(new Rect(r.x+60*u,r.y+38*u,r.width-70*u,28*u),EquipmentPreviewScore(item).ToString("0.#"),Mathf.RoundToInt(18*u),gold,true,false,TextAnchor.MiddleRight);
            }

            if(PopupCloseButton(new Rect(r.xMax-44*u,r.y,44*u,44*u))){inventoryComparisonOpen=false;BlockUITransition();}
            if(potion||fashion)Text(new Rect(r.x+8*u,r.y+42*u,r.width-16*u,22*u),potion?"恢复50%生命":"时装 · "+GameBalance.RarityName(appearance.rarity),Mathf.RoundToInt(12*u),muted);
            if(fashion)
            {
                float statsTop=MobileControls.Active?120:76;
                var detail=new EntryRewardPreview{Key="fashion:"+appearance.id,Name=appearance.name,Rarity=appearance.rarity,AppearanceSlot=appearance.slot,Description=ProgressionService.FashionBonus(appearance.slot,appearance.rarity)};
                inventoryDetailScroll=BeginTouchScroll("fashion-detail",new Rect(r.x+12*u,r.y+statsTop*u,r.width-24*u,r.height-(statsTop+8)*u),inventoryDetailScroll,new Rect(0,0,r.width-32*u,DrawRewardDetailRows(detail,r.width/u-32,u,false)*u));
                DrawRewardDetailRows(detail,r.width/u-32,u,true);
                EndTouchScroll();
            }
            float actionWidth=(r.width-16*u)*.5f;
            if(potion)
            {
                bool usable=session.Progression.Profile.potions>0&&session.Player!=null&&session.Player.Health<session.Player.MaxHealth-.5f;
                if(InventoryPictogramAction(new Rect(r.x+8*u,r.y+68*u,r.width-16*u,44*u),"使用",UIIconAtlas.Utility("potion"),usable,false,true)){session.DrinkPotion();inventoryPopupOpened=Time.frameCount;}
            }
            else if(fashion)
            {
                var current=session.Progression.EquippedFashion(appearance.slot);bool worn=current!=null&&current.id==appearance.id;
                if(MobileControls.Active&&InventoryPictogramAction(new Rect(r.x+8*u,r.y+68*u,r.width-16*u,44*u),worn?"卸下":"穿戴",UIIconAtlas.FashionCardIcon(appearance.slot,(int)appearance.AppearanceRarity,session.Progression.Profile.heroClass),true,worn,true))
                {MobileFashionResult(worn?session.Progression.UnequipFashion(appearance.slot):session.Progression.EquipFashion(appearance.id),worn?"已卸下外观":"外观已穿戴");inventoryPopupOpened=Time.frameCount;}
            }
            else
            {
                bool worn=IsEquipped(item);
                if(InventoryPictogramAction(new Rect(r.x+6*u,r.y+68*u,actionWidth,44*u),worn?"脱下":"穿戴",UIIconAtlas.EquipmentCardIcon(item.slot,item.level,item.rarity,session.Progression.Profile.heroClass),worn||item.level<=session.Progression.Profile.level,worn,true))
                {
                    bool saved=worn?session.Progression.Unequip(item.slot):session.Progression.Equip(item.id);
                    MobileInventoryResult(saved,worn?"装备已脱下":"装备已穿戴");
                    inventoryPopupOpened=Time.frameCount;
                    if(saved&&!worn){inventoryComparisonOpen=false;inventoryPopupCompare=false;CancelMobileScroll();}
                }
            }
            if(!inventoryPopupCompare&&!potion&&!fashion)
            {
                Rect detail=new Rect(r.x+8*u,r.y+116*u,r.width-16*u,Mathf.Max(24*u,r.height-120*u));
                var preview=session.Progression.PreviewEquippedItem(item);
                float textWidth=detail.width-18*u;
                float textHeight=DrawEquipmentAttributeDetails(preview,textWidth/u,u,false)*u;
                inventoryComparisonScroll=BeginTouchScroll("inventory-popup-detail",detail,inventoryComparisonScroll,new Rect(0,0,textWidth,Mathf.Max(detail.height,textHeight)));
                DrawEquipmentAttributeDetails(preview,textWidth/u,u,true);
                EndTouchScroll();
            }
            if(inventoryPopupCompare&&!potion&&!fashion)
            {
                var p=session.Progression;var current=p.Equipped(item.slot);var next=p.PreviewEquippedItem(item);
                Rect body=new Rect(r.x+6*u,r.y+116*u,r.width-12*u,Mathf.Max(24*u,r.height-120*u));
                float width=body.width-18*u,labelWidth=62*u,half=(width-labelWidth-6*u)*.5f;
                float h=222*u;
                for(int col=0;col<2;col++)
                {
                    var value=col==0?current:next;
                    string copy=EquipmentComparisonPresentation.Description(value,p);
                    h=Mathf.Max(h,206*u+Style(Mathf.RoundToInt(11*u),false,true).CalcHeight(new GUIContent(copy),half-12*u));
                }
                inventoryComparisonScroll=BeginTouchScroll("inventory-popup-comparison",body,inventoryComparisonScroll,new Rect(0,0,width,Mathf.Max(body.height,h)));
                string[] labels={"评分","攻击","防御","生命","暴击率","暴击伤害","攻击加成"};
                for(int row=0;row<labels.Length;row++)
                    Text(new Rect(0,(30+row*28)*u,labelWidth,24*u),labels[row],Mathf.RoundToInt(12*u),pale,true,false,TextAnchor.MiddleLeft);
                for(int col=0;col<2;col++)
                {
                    var value=col==0?current:next;Color accent=col==0?muted:jade;float x=labelWidth+col*(half+6*u);
                    Fill(new Rect(x,0,half,h),col==0?new Color(.045f,.075f,.095f):new Color(.035f,.105f,.12f));
                    Fill(new Rect(x,0,half,2*u),accent);
                    Text(new Rect(x+6*u,6*u,half-12*u,20*u),col==0?"当前装备":"所选装备",Mathf.RoundToInt(11*u),accent,true);
                    Text(new Rect(x+6*u,30*u,half-12*u,24*u),ProgressionService.EquipmentScore(value).ToString("0.#"),Mathf.RoundToInt(14*u),gold,true,false,TextAnchor.MiddleCenter);
                    float[] values={value==null?0:value.attack,value==null?0:value.defense,value==null?0:value.health,value==null?0:value.criticalChance,value==null?0:value.criticalDamageBonus};
                    float[] baseline={current==null?0:current.attack,current==null?0:current.defense,current==null?0:current.health,current==null?0:current.criticalChance,current==null?0:current.criticalDamageBonus};
                    for(int row=0;row<5;row++)
                    {
                        float y=(58+row*28)*u;float delta=values[row]-baseline[row];
                        Text(new Rect(x+6*u,y,half-12*u,24*u),(row>=3?(values[row]*100).ToString("0.##")+"%":values[row].ToString("0")),Mathf.RoundToInt(14*u),col==1&&delta!=0?(delta>0?jade:new Color(1,.48f,.42f)):pale,true,false,TextAnchor.MiddleCenter);
                    }
                    Text(new Rect(x+6*u,202*u,half-12*u,h-202*u),EquipmentComparisonPresentation.Description(value,p),Mathf.RoundToInt(11*u),accent,false,true);
                }
                EndTouchScroll();
            }
            GUI.enabled=prior;
        }
        private float DrawEquipmentAttributeDetails(ItemData item,float width,float u,bool draw)
        {
            string[] labels={"攻击","防御","生命","暴击率","暴击伤害加成"};
            string[] values={item.attack.ToString(),item.defense.ToString(),item.health.ToString(),(item.criticalChance*100).ToString("0.##")+"%",(item.criticalDamageBonus*100).ToString("0.##")+"%"};
            float y=4;
            for(int row=0;row<labels.Length;row++)
            {
                if(draw)
                {
                    if(row%2==0)Fill(new Rect(0,y*u,width*u,28*u),card);
                    Text(new Rect(6*u,y*u,(width-90)*u,28*u),labels[row],Mathf.RoundToInt(12*u),pale);
                    Text(new Rect((width-84)*u,y*u,78*u,28*u),values[row],Mathf.RoundToInt(14*u),row>=3?jade:pale,true,false,TextAnchor.MiddleRight);
                }
                y+=28;
            }
            y+=10;GoalParagraph(ref y,width,u,"镶嵌机制",14,gold,true,draw);
            GoalParagraph(ref y,width,u,EquipmentComparisonPresentation.Description(item,session.Progression),12,pale,false,draw);
            return y+8;
        }
        private bool DrawMobileEquipmentGrid(MobilePanelLayout.Area viewport,bool wide)
        {DrawEquipmentIconGrid(MobilePanelRect(viewport),ref mobileInventoryListScroll,TouchRatio);return false;}
    }
}
