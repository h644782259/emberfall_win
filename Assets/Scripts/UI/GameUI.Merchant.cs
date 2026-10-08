using System.Collections.Generic;
using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private int merchantMode,merchantSelection=-1;
        private float merchantActionUntil=-1;
        private Vector2 merchantGridScroll;
        private void SelectMerchantMode(int mode,bool force=false)
        {if(!force&&merchantMode==mode)return;merchantMode=mode;merchantSelection=mode==2?-1:0;merchantGridScroll=Vector2.zero;}
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
            if(merchantMode==2)merchantSelection=-1;
            else merchantSelection=Mathf.Clamp(merchantSelection,0,Mathf.Max(0,count-1));
            float contentHeight=l.GridHeight(count,true);
            merchantGridScroll=BeginTouchScroll("merchant-"+merchantMode,BuildPlanRect(l.Body,u),merchantGridScroll,new Rect(0,0,l.Body.Width*u,Mathf.Max(l.Body.Height,contentHeight)*u));
            for(int index=0;index<count;index++)
            {
                var area=l.Tile(index,true);Rect tile=new Rect(area.X*u,area.Y*u,area.Width*u,area.Height*u);
                bool selected=false;
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
                if(item!=null)
                {
                    Rect sell=new Rect(tile.x+6*u,tile.y+100*u,tile.width-12*u,44*u);
                    if(PrimaryButton(sell,"出售",gold,Time.unscaledTime>=merchantActionUntil)&&StartMerchantAction())
                    {string id=item.id;RebuildBagItems();SellInventoryItem(id);}
                }
                else
                {
                    string captionAction=owned?"已拥有":quote!=null?merchantMode==0?"购买":"兑换":merchantMode==0?p.Profile.potions>=99?"药剂已满":"金币不足":"碎片不足";
                    Rect action=new Rect(tile.x+6*u,tile.y+100*u,tile.width-12*u,44*u);
                    if(PrimaryButton(action,captionAction,gold,quote!=null&&Time.unscaledTime>=merchantActionUntil)&&StartMerchantAction())
                    {Feedback(p.BuyAtMerchant(quote,MerchantServiceActive),merchantMode==0?"购买成功 · 药剂已入行囊":"兑换成功 · 挂件已拥有，请到铁匠镶嵌");}

                }
            }
            EndTouchScroll();
            Rect info=BuildPlanRect(l.Info,u);
            Text(info,merchantMode==0?"生命药剂 × "+p.Profile.potions+" / 99":merchantMode==1?"兑换后可到铁匠镶嵌":"穿戴中与锁定物品受保护",Mathf.RoundToInt(13*u),muted,false,true);
            if(merchantMode==2&&count==0)Text(BuildPlanRect(l.Body,u),"没有可出售装备。",Mathf.RoundToInt(16*u),jade);
        }

        private bool ServiceCostAction(Rect r,string caption,int cost,bool material,float u,bool enabled,string reason=null)
        {
            bool clicked=Button(r,"",jade,enabled,reason);
            Text(new Rect(r.x+10*u,r.y,r.width-104*u,r.height),caption,Mathf.RoundToInt(13*u),enabled?pale:muted,true,false,TextAnchor.MiddleLeft);
            DrawPriceTint(new Rect(r.xMax-92*u,r.y+6*u,82*u,r.height-12*u),cost,material,u,enabled?gold:muted);
            return clicked;
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
            Fill(r,new Color(.14f,.115f,.055f,.55f));
            Border(r,new Color(tint.r,tint.g,tint.b,.25f));
            DrawIcon(new Rect(r.x+4*unit,r.center.y-9*unit,18*unit,18*unit),UIIconAtlas.Utility(material?"shard":"coin"),gold);
            Text(new Rect(r.x+27*unit,r.y,r.width-31*unit,r.height),amount.ToString(),Mathf.RoundToInt(13*unit),tint,true,false,TextAnchor.MiddleLeft);
        }
    }
}
