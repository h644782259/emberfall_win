using System.Collections.Generic;
using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private int merchantMode,merchantSelection=-1;
        private string merchantSaleId;
        private float merchantActionUntil=-1;
        private Vector2 merchantGridScroll;
        private void SelectMerchantMode(int mode,bool force=false)
        {if(!force&&merchantMode==mode)return;merchantMode=mode;merchantSelection=mode==2?-1:0;merchantSaleId=null;merchantGridScroll=Vector2.zero;BlockUITransition();}
        private bool StartMerchantAction()
        {if(Time.unscaledTime<merchantActionUntil)return false;merchantActionUntil=Time.unscaledTime+.35f;return true;}
        private void DrawMerchantService()
        {
            var p=session.Progression;float u=MobileControls.Active?TouchRatio:1;
            var l=new MerchantServiceLayout(width/u,height/u);
            Fill(new Rect(0,0,width,height),new Color(.018f,.031f,.048f,.985f));blockedRects.Add(new Rect(0,0,width,height));
            Text(BuildPlanRect(l.Header,u),"商人",Mathf.RoundToInt(22*u),pale,true);
            DrawServiceBalances(BuildPlanRect(l.Balance,u),u);
            if(NavigationButton(BuildPlanRect(l.Close,u),"×",jade)){ClosePanel();return;}
            string[] tabs={"购买","兑换","出售"};
            for(int mode=0;mode<tabs.Length;mode++)if(TabButton(BuildPlanRect(l.Tab(mode),u),tabs[mode],merchantMode==mode))SelectMerchantMode(mode);
            if(!MerchantServiceActive){Text(BuildPlanRect(l.Body,u),"靠近商人后才能交易。",Mathf.RoundToInt(16*u),muted);return;}
            var mechanics=BuildCatalog.MechanicsFor(p.Profile.heroClass);
            var saleItems=new List<ItemData>();
            if(merchantMode==2)foreach(var gear in p.Profile.inventory)if(gear!=null&&!gear.locked&&!IsEquipped(gear))saleItems.Add(gear);
            int count=merchantMode==0?1:merchantMode==1?mechanics.Length:saleItems.Count;
            if(merchantMode==2)merchantSelection=saleItems.FindIndex(item=>item.id==merchantSaleId);
            else merchantSelection=Mathf.Clamp(merchantSelection,0,Mathf.Max(0,count-1));
            float contentHeight=l.GridHeight(count);
            merchantGridScroll=BeginTouchScroll("merchant-"+merchantMode,BuildPlanRect(l.Body,u),merchantGridScroll,new Rect(0,0,l.Body.Width*u,Mathf.Max(l.Body.Height,contentHeight)*u));
            for(int index=0;index<count;index++)
            {
                var area=l.Tile(index);Rect tile=new Rect(area.X*u,area.Y*u,area.Width*u,area.Height*u);
                bool selected=index==merchantSelection;
                Fill(tile,selected?new Color(.12f,.22f,.24f):card);Border(tile,selected?gold:muted*.4f);
                if(selected){Border(new Rect(tile.x+2*u,tile.y+2*u,tile.width-4*u,tile.height-4*u),gold);DrawIcon(new Rect(tile.xMax-25*u,tile.y+4*u,20*u,20*u),UIIconAtlas.Utility("confirm"),gold);}
                ItemData item=merchantMode==2?saleItems[index]:null;
                EquipmentMechanic mechanic=merchantMode==1?mechanics[index]:EquipmentMechanic.None;
                bool owned=mechanic!=EquipmentMechanic.None&&p.Attachment(mechanic)!=null;
                var quote=item==null?p.PrepareMerchantPurchase(mechanic,MerchantServiceActive):null;
                bool first=p.Profile.pendingFirstClearReward&&!p.Profile.firstClearRewardClaimed;
                int price=item!=null?p.SellValue(item):mechanic==EquipmentMechanic.None?ProgressionService.PotionPrice:first?0:ProgressionService.MechanicExchangeCost;
                string caption=item!=null?item.name:mechanic==EquipmentMechanic.None?"生命药剂":BuildCatalog.MechanicName(mechanic);
                DrawIcon(new Rect(tile.center.x-23*u,tile.y+6*u,46*u,46*u),item!=null?UIIconAtlas.EquipmentCardIcon(item.slot):UIIconAtlas.Utility(mechanic==EquipmentMechanic.None?"potion":"skills"),item!=null?GameBalance.RarityColor(item.rarity):Color.white);
                Text(new Rect(tile.x+6*u,tile.y+54*u,tile.width-12*u,20*u),caption,Mathf.RoundToInt(12*u),pale,false,false,TextAnchor.MiddleCenter);
                DrawPriceTint(new Rect(tile.x+8*u,tile.y+77*u,tile.width-16*u,20*u),price,merchantMode==1,u,item!=null||quote!=null?gold:new Color(.98f,.28f,.24f));
                string state=item!=null?selected?"已选中":"点击选择":owned?"已拥有":quote!=null?merchantMode==0?"可购买":"可兑换":merchantMode==0?p.Profile.potions>=99?"药剂已满":"金币不足":"碎片不足";
                Text(new Rect(tile.x+6*u,tile.y+100*u,tile.width-12*u,18*u),state,Mathf.RoundToInt(11*u),quote!=null||selected?jade:muted,false,false,TextAnchor.MiddleCenter);
                if(GUI.Button(tile,GUIContent.none,invisibleButton)){merchantSelection=index;merchantSaleId=item==null?null:item.id;BlockUITransition();}
            }
            EndTouchScroll();
            Rect info=BuildPlanRect(l.Info,u);Rect action=BuildPlanRect(l.Action,u);
            if(merchantMode!=2)
            {
                EquipmentMechanic selected=merchantMode==1&&count>0?mechanics[merchantSelection]:EquipmentMechanic.None;
                var quote=count>0?p.PrepareMerchantPurchase(selected,MerchantServiceActive):null;
                string hint=merchantMode==0?"生命药剂 × "+p.Profile.potions+" / 99":"兑换后到铁匠镶嵌 · 已拥有的挂件不能重复兑换";
                Text(info,hint,Mathf.RoundToInt(13*u),muted,false,true);
                if(PrimaryButton(action,merchantMode==0?"购买":"兑换",gold,quote!=null&&Time.unscaledTime>=merchantActionUntil)&&StartMerchantAction())
                {Feedback(p.BuyAtMerchant(quote,MerchantServiceActive),merchantMode==0?"购买成功 · 药剂已入行囊":"兑换成功 · 挂件已拥有，请到铁匠镶嵌");BlockUITransition();}
            }
            else
            {
                Text(info,merchantSelection<0?"请选择装备 · 穿戴中与锁定物品受保护":"已选中："+saleItems[merchantSelection].name,Mathf.RoundToInt(13*u),pale,false,true);
                if(PrimaryButton(action,"出售选中",gold,merchantSelection>=0&&Time.unscaledTime>=merchantActionUntil)&&StartMerchantAction())
                {string id=merchantSaleId;merchantSaleId=null;merchantSelection=-1;RebuildBagItems();SellInventoryItem(id);BlockUITransition();}
                if(count==0)Text(BuildPlanRect(l.Body,u),"没有可出售装备。",Mathf.RoundToInt(16*u),jade);
            }
        }
        private void DrawServiceBalances(Rect r,float unit)
        {
            float half=r.width*.5f;
            DrawPrice(new Rect(r.x,r.y,half-4*unit,r.height),session.Progression.Profile.gold,false,unit);
            DrawPrice(new Rect(r.x+half,r.y,half,r.height),session.Progression.Profile.mechanicMaterials,true,unit);
        }
        private void DrawPrice(Rect r,int amount,bool material,float unit)
        {DrawPriceTint(r,amount,material,unit,gold);}
        private void DrawPriceTint(Rect r,int amount,bool material,float unit,Color tint)
        {
            DrawIcon(new Rect(r.x,r.center.y-10*unit,20*unit,20*unit),UIIconAtlas.Utility(material?"shard":"coin"),gold);
            Text(new Rect(r.x+23*unit,r.y,r.width-23*unit,r.height),amount.ToString(),Mathf.RoundToInt(13*unit),tint,true,false,TextAnchor.MiddleLeft);
        }
    }
}
