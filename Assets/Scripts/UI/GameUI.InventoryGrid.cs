using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
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

        private bool DrawMobileEquipmentGrid(MobilePanelLayout.Area viewport,bool wide)
        {
            float width=viewport.Width-18,u=TouchRatio;
            var geometry=new InventoryGridGeometry(width);int columns=geometry.Columns;
            float cellWidth=geometry.CellWidth,cellHeight=InventoryGridGeometry.RowHeight;
            float header=wide?56:0;
            float height=header+((bagItems.Count+columns-1)/columns)*cellHeight+8;
            mobileInventoryListScroll=BeginTouchScroll("mobile-inventory-grid",MobilePanelRect(viewport),mobileInventoryListScroll,
                new Rect(0,0,width*u,Mathf.Max(viewport.Height,height)*u));
            string chosen=null,equip=null,lockId=null;int filter=0;
            if(wide)
            {
                if(QuietAction(TouchRect(0,0,64,48),MobileInventoryFilterLabel))filter=1;
                if(DrawInventorySortIcon(TouchRect(72,0,44,48)))filter=2;
            }
            for(int index=0;index<bagItems.Count;index++)
            {
                var item=bagItems[index];float x=(index%columns)*(cellWidth+6),y=header+(index/columns)*cellHeight;
                if((y+cellHeight)*u<mobileInventoryListScroll.y||y*u>mobileInventoryListScroll.y+viewport.Height*u)continue;
                Rect tile=TouchRect(x,y,cellWidth,cellHeight-6);
                Color rarity=GameBalance.RarityColor(item.rarity);
                Fill(tile,selectedItem==item.id?new Color(.065f,.12f,.14f):card);
                if(GUI.Button(TouchRect(x,y,cellWidth-44,44),GUIContent.none,invisibleButton))chosen=item.id;
                if(DrawInventoryLock(TouchRect(x+cellWidth-44,y,44,44),item.locked))lockId=item.id;
                DrawIcon(TouchRect(x+5,y+4,26,26),UIIconAtlas.EquipmentCardIcon(item.slot),rarity);
                Text(TouchRect(x+34,y+3,cellWidth-76,17),"Lv."+item.level,TouchFont(10),rarity,true);
                bool worn=IsEquipped(item),eligible=item.level<=session.Progression.Profile.level;
                Text(TouchRect(x+34,y+20,cellWidth-76,19),worn?"已穿戴":GameBalance.RarityName(item.rarity)+(item.locked?" · 锁":""),TouchFont(10),worn?jade:muted);
                if(QuietInventoryAction(TouchRect(x+4,y+44,(cellWidth-12)*.5f,44),"穿戴",eligible&&!worn,worn?"此装备已穿戴":eligible?item.name:"需要 "+item.level+" 级"))equip=item.id;
                if(QuietAction(TouchRect(x+cellWidth*.5f+2,y+44,(cellWidth-12)*.5f,44),"对比"))chosen=item.id;

            }
            if(bagItems.Count==0)Text(TouchRect(8,header+12,width-16,50),"这个分类暂无闲置装备",TouchFont(16),muted);
            EndTouchScroll();
            if(lockId!=null){var item=session.Progression.Profile.inventory.Find(v=>v!=null&&v.id==lockId);if(item!=null){bool target=!item.locked;MobileInventoryResult(session.Progression.SetItemLocked(lockId,target),target?"已锁定":"已解锁");}return true;}
            if(equip!=null){MobileInventoryResult(session.Progression.Equip(equip),"装备已穿戴");return true;}
            if(filter!=0){if(filter==1)CycleMobileInventoryFilter();else CycleMobileInventorySort();return true;}
            if(chosen==null)return false;
            selectedItem=chosen;inventoryComparisonOpen=true;inventoryComparisonScroll=Vector2.zero;
            mobileInventoryStatus=null;CancelMobileScroll();return true;
        }
    }
}
