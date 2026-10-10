using System.Collections.Generic;
using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private bool MerchantRarityTab(Rect r,Rarity rarity,float u)
        {
            bool selected=merchantGemRarity==rarity;Color tint=GameBalance.RarityColor(rarity);
            Fill(r,selected?new Color(.10f,.18f,.22f):new Color(.04f,.075f,.10f));
            if(selected)Fill(new Rect(r.x+8*u,r.yMax-2*u,r.width-16*u,2*u),tint);
            Text(r,GameBalance.RarityName(rarity),Mathf.RoundToInt(12*u),selected?tint:muted,true,false,TextAnchor.MiddleCenter);
            return GUI.Button(r,GUIContent.none,invisibleButton);
        }
        private int merchantMode,merchantSelection=-1;
        private EquipmentMechanic merchantGemSaleConfirmation;
        private float merchantActionUntil=-1;
        private Vector2 merchantGridScroll;
        private void SelectMerchantMode(int mode,bool force=false)
        {if(!force&&merchantMode==mode)return;entryRewardSelection=null;merchantGemSaleConfirmation=EquipmentMechanic.None;merchantMode=mode;merchantSelection=mode==2?-1:0;merchantGridScroll=Vector2.zero;}
        private bool StartMerchantAction()
        {if(Time.unscaledTime<merchantActionUntil)return false;merchantActionUntil=Time.unscaledTime+.35f;return true;}
        private Rarity merchantGemRarity=Rarity.Common;
        private void DrawMerchantService()
        {
            var p=session.Progression;float u=MobileControls.Active?TouchRatio:1;
            var l=new MerchantServiceLayout(width/u,height/u,MobileControls.IsIPad);
            blockedRects.Add(new Rect(0,0,width,height));
            Box(BuildPlanRect(l.Frame,u),jade,false);
            Text(BuildPlanRect(l.Header,u),"商人",Mathf.RoundToInt(22*u),pale,true);
            DrawServiceBalances(BuildPlanRect(l.Balance,u),u);
            if(PopupCloseButton(BuildPlanRect(l.Close,u))){ClosePanel();return;}
            string[] tabs={"购买","兑换","出售"};
            for(int mode=0;mode<tabs.Length;mode++)if(TabButton(BuildPlanRect(l.Tab(mode),u),tabs[mode],merchantMode==mode))SelectMerchantMode(mode);
            if(!MerchantServiceActive){Text(BuildPlanRect(l.Body,u),"返回营地后可使用商店。",Mathf.RoundToInt(16*u),muted);return;}
            if(merchantMode==2)
            {
                var bulk=p.PrepareMerchantBulkSale(MerchantServiceActive);
                Text(BuildPlanRect(l.Info,u),bulk==null?"暂无可一键出售的装备":"未锁定且评分更低 · "+bulk.Count+"件 · +"+bulk.Gold+"金币",Mathf.RoundToInt(12*u),muted,false,true);
                if(PrimaryButton(BuildPlanRect(l.Action,u),"一键出售",gold,bulk!=null&&Time.unscaledTime>=merchantActionUntil)&&StartMerchantAction())
                {bool sold=p.SellAtMerchant(bulk,MerchantServiceActive);Feedback(sold,"已出售 "+bulk.Count+" 件 · +"+bulk.Gold+" 金币");if(sold){RebuildBagItems();ResolveSelectedItem();}}
            }
            var mechanics=BuildCatalog.GemsFor(p.Profile.heroClass);
            var saleItems=new List<ItemData>();
            if(merchantMode==2)foreach(var gear in p.Profile.inventory)if(gear!=null&&!gear.locked&&!IsEquipped(gear))saleItems.Add(gear);
            var saleGems=new List<MechanicAttachment>();
            if(merchantMode==2)foreach(var gem in p.Profile.attachments)if(gem!=null)saleGems.Add(gem);
            int count=merchantMode==0?1:merchantMode==1?mechanics.Length:saleItems.Count+saleGems.Count;
            if(merchantMode==2)merchantSelection=-1;
            else merchantSelection=Mathf.Clamp(merchantSelection,0,Mathf.Max(0,count-1));
            float contentHeight=l.GridHeight(count,true)+(merchantMode==1?((count+l.Columns-1)/l.Columns)*54:0);
            merchantGridScroll=BeginTouchScroll("merchant-"+merchantMode,BuildPlanRect(l.Body,u),merchantGridScroll,new Rect(0,0,l.Body.Width*u,Mathf.Max(l.Body.Height,contentHeight)*u));
            for(int index=0;index<count;index++)
            {
                var area=l.Tile(index,true);Rect tile=new Rect(area.X*u,area.Y*u,area.Width*u,area.Height*u);
                if(merchantMode==1){tile.y+=(index/l.Columns)*54*u;tile.height+=54*u;}
                bool selected=false;
                Fill(tile,selected?new Color(.12f,.22f,.24f):card);Border(tile,selected?gold:muted*.4f);
                if(selected){Border(new Rect(tile.x+2*u,tile.y+2*u,tile.width-4*u,tile.height-4*u),gold);DrawIcon(new Rect(tile.xMax-25*u,tile.y+4*u,20*u,20*u),UIIconAtlas.Utility("confirm"),gold);}
                if(merchantMode==2&&index>=saleItems.Count)
                {
                    var gem=saleGems[index-saleItems.Count];Color tint=GameBalance.RarityColor(gem.rarity);
                    Rect gemIcon=new Rect(tile.center.x-23*u,tile.y+6*u,46*u,46*u);
                    DrawIcon(gemIcon,UIIconAtlas.Utility("gem"),tint);
                    if(gem.mounted)DrawWornIconBadge(gemIcon,u,"已镶嵌");
                    Text(new Rect(tile.x+6*u,tile.y+54*u,tile.width-12*u,28*u),BuildCatalog.GemName(gem.mechanic),Mathf.RoundToInt(11*u),pale,false,true,TextAnchor.MiddleCenter);
                    DrawPrice(new Rect(tile.x+8*u,tile.y+80*u,tile.width-16*u,20*u),p.GemSellValue(gem.mechanic),true,u);
                    InspectRewardItem(new Rect(tile.x,tile.y,tile.width,100*u),new EntryRewardPreview{Key="merchant:gem:sale:"+gem.mechanic,Name=BuildCatalog.GemName(gem.mechanic),Rarity=gem.rarity,Tint=tint,Icon=UIIconAtlas.Utility("gem"),Description=GemRewardDescription(gem.mechanic,gem.rarity,gem.upgradeRank)},true);
                    string reason=p.GemSaleLock(gem.mechanic,MerchantServiceActive);
                    bool confirming=merchantGemSaleConfirmation==gem.mechanic;
                    if(PrimaryButton(new Rect(tile.x+6*u,tile.y+104*u,tile.width-12*u,40*u),reason.Length>0?reason:confirming?"确认出售":"出售",gold,reason.Length==0)&&StartMerchantAction())
                    {if(confirming){Feedback(p.SellGem(gem.mechanic,MerchantServiceActive),"宝石已出售");merchantGemSaleConfirmation=EquipmentMechanic.None;}else merchantGemSaleConfirmation=gem.mechanic;}
                    continue;
                }
                ItemData item=merchantMode==2?saleItems[index]:null;
                EquipmentMechanic mechanic=merchantMode==1?mechanics[index]:EquipmentMechanic.None;
                bool owned=mechanic!=EquipmentMechanic.None&&p.Attachment(mechanic)!=null&&p.Attachment(mechanic).rarity>=merchantGemRarity;
                var quote=item==null?p.PrepareMerchantPurchase(mechanic,MerchantServiceActive,merchantGemRarity):null;
                bool first=p.Profile.pendingFirstClearReward&&!p.Profile.firstClearRewardClaimed&&mechanic!=EquipmentMechanic.None&&merchantGemRarity==Rarity.Epic;
                int price=item!=null?p.SellValue(item):mechanic==EquipmentMechanic.None?ProgressionService.PotionPrice:first?0:BuildCatalog.GemPrice(merchantGemRarity);
                string caption=item!=null?item.name:mechanic==EquipmentMechanic.None?"生命药剂":BuildCatalog.GemName(mechanic);
                DrawIcon(new Rect(tile.center.x-23*u,tile.y+6*u,46*u,46*u),item!=null?UIIconAtlas.EquipmentCardIcon(item.slot,item.level,item.rarity,session.Progression.Profile.heroClass):UIIconAtlas.Utility(mechanic==EquipmentMechanic.None?"potion":"gem"),item!=null?GameBalance.RarityColor(item.rarity):merchantMode==1?GameBalance.RarityColor(merchantGemRarity):Color.white);
                Text(new Rect(tile.x+6*u,tile.y+52*u,tile.width-12*u,30*u),caption,Mathf.RoundToInt(11*u),pale,false,true,TextAnchor.MiddleCenter);
                if(merchantMode==1)
                {
                    string stats=BuildCatalog.IsAttributeGem(mechanic)?BuildCatalog.GemAttributeSummary(mechanic,merchantGemRarity,p.Attachment(mechanic)?.upgradeRank??0):BuildCatalog.AttributeLabel(BuildCatalog.MechanicAttribute(mechanic))+" +"+(BuildCatalog.MechanicAttributeValue(mechanic,p.Attachment(mechanic)?.upgradeRank??0)*100).ToString("0.#")+"%";
                    Text(new Rect(tile.x+6*u,tile.y+82*u,tile.width-12*u,18*u),GameBalance.SlotName(BuildCatalog.MechanicSlot(mechanic))+" · "+GameBalance.RarityName(merchantGemRarity),Mathf.RoundToInt(11*u),GameBalance.RarityColor(merchantGemRarity),true,false,TextAnchor.MiddleCenter);
                    Text(new Rect(tile.x+6*u,tile.y+100*u,tile.width-12*u,30*u),stats,Mathf.RoundToInt(11*u),jade,true,true,TextAnchor.MiddleCenter);
                }
                DrawPriceTint(new Rect(tile.x+8*u,tile.y+(merchantMode==1?131:77)*u,tile.width-16*u,20*u),price,merchantMode==1,u,item!=null||quote!=null?gold:new Color(.98f,.28f,.24f));
                EntryRewardPreview detail;
                if(item!=null){detail=ActualEquipmentPreview(item);detail.AllowActions=true;detail.Key="merchant:equipment:"+item.id;}
                else if(mechanic!=EquipmentMechanic.None)detail=new EntryRewardPreview{Key="merchant:gem:buy:"+mechanic+":"+merchantGemRarity,Name=caption,Rarity=merchantGemRarity,Tint=GameBalance.RarityColor(merchantGemRarity),Icon=UIIconAtlas.Utility("gem"),Description=GemRewardDescription(mechanic,merchantGemRarity)};
                else detail=new EntryRewardPreview{Key="merchant:potion",Name="生命药剂",Rarity=Rarity.Common,Tint=jade,Icon=UIIconAtlas.Utility("potion"),Description="生命药剂\n恢复50%生命"};
                InspectRewardItem(new Rect(tile.x,tile.y,tile.width,(merchantMode==1?150:96)*u),detail,true);
                if(item!=null)
                {
                    Rect sell=new Rect(tile.x+6*u,tile.y+100*u,tile.width-12*u,44*u);
                    if(PrimaryButton(sell,"出售",gold,Time.unscaledTime>=merchantActionUntil)&&StartMerchantAction())
                    {string id=item.id;RebuildBagItems();SellInventoryItem(id);}
                }
                else
                {
                    string captionAction=first?(owned?"领取 · +3碎片":"领取首通宝石"):owned?"已拥有":quote!=null?merchantMode==0?"购买":p.Attachment(mechanic)!=null?"兑换 · 提升品质":"兑换":merchantMode==0?p.Profile.potions>=99?"药剂已满":"金币不足":"碎片不足";
                    Rect action=new Rect(tile.x+6*u,tile.y+(merchantMode==1?154:100)*u,tile.width-12*u,44*u);
                    if(PrimaryButton(action,captionAction,gold,quote!=null&&Time.unscaledTime>=merchantActionUntil)&&StartMerchantAction())
                    {Feedback(p.BuyAtMerchant(quote,MerchantServiceActive),merchantMode==0?"购买成功 · 药剂已入行囊":"兑换成功 · 宝石已拥有，请到铁匠镶嵌");}

                }
            }
            EndTouchScroll();
            Rect info=BuildPlanRect(l.Info,u);
            if(merchantMode==1)
            {for(int tier=0;tier<4;tier++){Rect tab=new Rect(info.x+tier*(info.width/4),info.y,info.width/4-4*u,info.height);if(MerchantRarityTab(tab,(Rarity)tier,u)){merchantGemRarity=(Rarity)tier;}}}
            if(merchantMode==2&&count==0)Text(BuildPlanRect(l.Body,u),"没有可出售物品。",Mathf.RoundToInt(16*u),jade);
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
