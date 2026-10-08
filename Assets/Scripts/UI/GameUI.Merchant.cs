using System.Collections.Generic;
using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private int merchantMode,merchantSelection;
        private Vector2 merchantGridScroll;
        private void DrawMerchantService()
        {
            var p=session.Progression;float u=MobileControls.Active?TouchRatio:1;
            var l=new MerchantServiceLayout(width/u,height/u);
            Fill(new Rect(0,0,width,height),new Color(.018f,.031f,.048f,.985f));blockedRects.Add(new Rect(0,0,width,height));
            Text(BuildPlanRect(l.Header,u),"商人 · 购买与出售",Mathf.RoundToInt(22*u),pale,true);
            DrawPrice(BuildPlanRect(l.Balance,u),p.Profile.gold,false,u);
            if(NavigationButton(BuildPlanRect(l.Close,u),"×",jade)){ClosePanel();return;}
            for(int mode=0;mode<2;mode++)if(TabButton(BuildPlanRect(l.Tab(mode),u),mode==0?"购买 / 兑换":"出售",merchantMode==mode))
            {merchantMode=mode;merchantSelection=0;merchantGridScroll=Vector2.zero;BlockUITransition();}
            if(!MerchantServiceActive){Text(BuildPlanRect(l.Body,u),"靠近商人后才能交易。",Mathf.RoundToInt(16*u),muted);return;}
            var mechanics=BuildCatalog.MechanicsFor(p.Profile.heroClass);
            var saleItems=new List<ItemData>();
            if(merchantMode==1)foreach(var gear in p.Profile.inventory)if(gear!=null&&!gear.locked&&!IsEquipped(gear))saleItems.Add(gear);
            int count=merchantMode==0?mechanics.Length+1:saleItems.Count;
            merchantSelection=Mathf.Clamp(merchantSelection,0,Mathf.Max(0,count-1));
            float contentHeight=l.GridHeight(count);
            merchantGridScroll=BeginTouchScroll("merchant-"+merchantMode,BuildPlanRect(l.Body,u),merchantGridScroll,new Rect(0,0,l.Body.Width*u,Mathf.Max(l.Body.Height,contentHeight)*u));
            for(int index=0;index<count;index++)
            {
                var area=l.Tile(index);Rect tile=new Rect(area.X*u,area.Y*u,area.Width*u,area.Height*u);
                Fill(tile,index==merchantSelection?new Color(.10f,.18f,.20f):card);Border(tile,index==merchantSelection?gold:muted*.4f);
                ItemData item=merchantMode==1?saleItems[index]:null;
                EquipmentMechanic mechanic=merchantMode==0&&index>0?mechanics[index-1]:EquipmentMechanic.None;
                bool owned=mechanic!=EquipmentMechanic.None&&p.Attachment(mechanic)!=null;
                string caption=item!=null?item.name:mechanic==EquipmentMechanic.None?"生命药剂":BuildCatalog.MechanicName(mechanic);
                DrawIcon(new Rect(tile.center.x-23*u,tile.y+6*u,46*u,46*u),item!=null?UIIconAtlas.EquipmentCardIcon(item.slot):UIIconAtlas.Utility(mechanic==EquipmentMechanic.None?"potion":"skills"),owned?muted:item!=null?GameBalance.RarityColor(item.rarity):gold);
                Text(new Rect(tile.x+6*u,tile.y+54*u,tile.width-12*u,22*u),caption,Mathf.RoundToInt(12*u),pale,false,false,TextAnchor.MiddleCenter);
                int price=item!=null?p.SellValue(item):mechanic==EquipmentMechanic.None?ProgressionService.PotionPrice:p.Profile.pendingFirstClearReward?0:ProgressionService.MechanicExchangeCost;
                if(owned)Text(new Rect(tile.x+8*u,tile.y+80*u,tile.width-16*u,22*u),"已拥有",Mathf.RoundToInt(12*u),jade,false,false,TextAnchor.MiddleCenter);
                else DrawPrice(new Rect(tile.x+8*u,tile.y+80*u,tile.width-16*u,22*u),price,mechanic!=EquipmentMechanic.None,u);
                if(GUI.Button(tile,GUIContent.none,invisibleButton)){merchantSelection=index;BlockUITransition();}
            }
            EndTouchScroll();
            Rect info=BuildPlanRect(l.Info,u);Rect action=BuildPlanRect(l.Action,u);
            if(merchantMode==0)
            {
                EquipmentMechanic selected=merchantSelection>0?mechanics[merchantSelection-1]:EquipmentMechanic.None;
                var quote=p.PrepareMerchantPurchase(selected,MerchantServiceActive);
                string hint=selected==EquipmentMechanic.None?"生命药剂 × "+p.Profile.potions+" / 99":"星烬碎片 × "+p.Profile.mechanicMaterials+" · 兑换后到铁匠镶嵌";
                Text(info,hint,Mathf.RoundToInt(13*u),muted,false,true);
                if(PrimaryButton(action,selected==EquipmentMechanic.None?"购买":"兑换",gold,quote!=null))
                {Feedback(p.BuyAtMerchant(quote,MerchantServiceActive),selected==EquipmentMechanic.None?"购买成功 · 药剂已入行囊":"兑换成功 · 挂件已拥有，请到铁匠镶嵌");BlockUITransition();}
            }
            else
            {
                Text(info,"只列可售装备 · 穿戴中与锁定物品受保护",Mathf.RoundToInt(13*u),muted,false,true);
                if(saleItems.Count>0&&DangerButton(action,"出售选中",gold))
                {RebuildBagItems();SellInventoryItem(saleItems[merchantSelection].id);BlockUITransition();}
                if(count==0)Text(BuildPlanRect(l.Body,u),"没有可出售装备。",Mathf.RoundToInt(16*u),jade);
            }
        }
        private void DrawPrice(Rect r,int amount,bool material,float unit)
        {
            DrawIcon(new Rect(r.x,r.y,20*unit,20*unit),UIIconAtlas.Utility(material?"shard":"coin"),gold);
            Text(new Rect(r.x+25*unit,r.y,r.width-25*unit,r.height),amount.ToString(),Mathf.RoundToInt(14*unit),gold,true,false,TextAnchor.MiddleRight);
        }
    }
}
