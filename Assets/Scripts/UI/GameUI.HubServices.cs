using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private Vector2 hubServiceScroll;
        private bool merchantExchangeOpen;
        private void DrawHubEquipmentService()
        {
            bool smith=session.ActiveHubNpc==HubNpcKind.Blacksmith;
            float u=MobileControls.Active?TouchRatio:1f;
            float w=Mathf.Min(820,width/u-24),h=Mathf.Min(600,height/u-24);
            float x=(width-w*u)*.5f,y=(height-h*u)*.5f;
            Rect frame=new Rect(x,y,w*u,h*u);
            Fill(new Rect(0,0,width,height),new Color(.012f,.025f,.04f,.9f));blockedRects.Add(frame);Box(frame,smith?gold:jade);
            Text(new Rect(x+20*u,y+16*u,(w-100)*u,32*u),smith?"铁匠 · 装备锻造":"商人 · 补给与交易",Mathf.RoundToInt(24*u),smith?gold:jade,true);
            if(NavigationButton(new Rect(frame.xMax-62*u,y+16*u,42*u,32*u),"×",muted)){ClosePanel();return;}
            if(!smith&&NavigationButton(new Rect(x+20*u,y+53*u,(w-40)*u,38*u),"机制兑换 · 星烬碎片",gold)){merchantExchangeOpen=true;panel=Panel.Camp;BlockUITransition();return;}
            Text(new Rect(x+20*u,y+54*u,(w-40)*u,44*u),smith?"强化永久提升装备部位，换上新装备会自动继承。":"",Mathf.RoundToInt(13*u),muted,false,true);
            var progression=session.Progression;
            Text(new Rect(x+20*u,y+99*u,(w-40)*u,24*u),"金币 "+progression.Profile.gold+"  ·  "+(smith?"部位强化上限 +"+ProgressionService.MaximumUpgrade:"生命药剂 ×"+progression.Profile.potions),Mathf.RoundToInt(15*u),gold,true);
            Rect viewport=new Rect(x+16*u,y+135*u,(w-32)*u,(h-153)*u);
            float bodyWidth=viewport.width-20*u;
            RebuildBagItems();
            float contentHeight=smith?3*164*u:(170+bagItems.Count*88)*u;
            hubServiceScroll=BeginTouchScroll("hub-equipment-service",viewport,hubServiceScroll,new Rect(0,0,bodyWidth,Mathf.Max(viewport.height,contentHeight)));
            if(smith)DrawSmithServiceCards(bodyWidth,u);else DrawMerchantServiceCards(bodyWidth,u);
            EndTouchScroll();
        }
        private void DrawSmithServiceCards(float width,float u)
        {
            var p=session.Progression;
            for(int i=0;i<3;i++)
            {
                var slot=(ItemSlot)i;var item=p.Equipped(slot);int rank=p.SlotUpgradeRank(slot);
                Rect r=new Rect(0,i*164*u,width,152*u);Fill(r,card);Fill(new Rect(0,r.y,3*u,r.height),gold);
                Text(new Rect(14*u,r.y+10*u,width-28*u,24*u),GameBalance.SlotName(slot)+"部位 · +"+rank,Mathf.RoundToInt(18*u),gold,true);
                if(item==null){Text(new Rect(14*u,r.y+46*u,width-28*u,60*u),"先在行囊中穿戴这个部位的装备，再进行强化。",Mathf.RoundToInt(14*u),muted,false,true);continue;}
                Text(new Rect(14*u,r.y+40*u,width-28*u,22*u),"当前装备："+item.name,Mathf.RoundToInt(14*u),GameBalance.RarityColor(item.rarity),true);
                bool max=rank>=ProgressionService.MaximumUpgrade;
                var next=p.PreviewUpgrade(item,Mathf.Min(rank+1,ProgressionService.MaximumUpgrade));
                string stats="攻击 "+item.attack+" → "+next.attack+"   防御 "+item.defense+" → "+next.defense+"   生命 "+item.health+" → "+next.health;
                Text(new Rect(14*u,r.y+68*u,width-28*u,32*u),max?"此部位已强化至上限，新装备仍会继承。":stats,Mathf.RoundToInt(13*u),pale,false,true);
                int cost=p.UpgradeCost(item);
                if(PrimaryButton(new Rect(14*u,r.y+108*u,width-28*u,34*u),max?"已达上限":"强化至 +"+(rank+1)+" · "+cost+" 金币",gold,!max&&p.Profile.gold>=cost,max?"部位强化已达上限。":"金币不足时无法强化；新装备会自动继承部位强化。"))
                {Feedback(p.Upgrade(item.id),GameBalance.SlotName(slot)+"部位已强化");}
            }
        }
        private void DrawMerchantServiceCards(float width,float u)
        {
            var p=session.Progression;
            Rect supply=new Rect(0,0,width,100*u);Fill(supply,card);Fill(new Rect(0,0,3*u,supply.height),jade);
            Text(new Rect(14*u,10*u,width-28*u,25*u),"生命药剂 · "+ProgressionService.PotionPrice+" 金币 / 瓶",Mathf.RoundToInt(18*u),jade,true);
            if(PrimaryButton(new Rect(14*u,49*u,width-28*u,38*u),"购买生命药剂",jade,p.Profile.gold>=ProgressionService.PotionPrice))
            {Feedback(p.BuyPotion(),"已购买生命药剂");BlockUITransition();}
            bool bulkSale=DangerButton(new Rect(14*u,112*u,width-28*u,48*u),"批量出售背包低品质装备",gold,true,"穿戴、锁定及机制装备受保护");
            string sell=null,unlock=null;
            for(int i=0;i<bagItems.Count;i++)
            {
                var item=bagItems[i];float y=(170+i*88)*u;
                Rect row=new Rect(0,y,width,80*u);Fill(row,card);Fill(new Rect(0,y,3*u,row.height),GameBalance.RarityColor(item.rarity));
                float actionWidth=Mathf.Min(160*u,width*.42f);
                Text(new Rect(12*u,y+8*u,width-actionWidth-30*u,26*u),item.name,Mathf.RoundToInt(16*u),GameBalance.RarityColor(item.rarity),true);
                Text(new Rect(12*u,y+37*u,width-actionWidth-30*u,33*u),"等级 "+item.level+" · "+GameBalance.RarityName(item.rarity)+(item.locked?"\n锁定保护 · 不可出售":" · "+GameBalance.SlotName(item.slot)),Mathf.RoundToInt(12*u),item.locked?gold:muted,false,true);
                Rect action=new Rect(width-actionWidth-12*u,y+22*u,actionWidth,36*u);
                if(IsEquipped(item)){Button(action,"穿戴中",muted,false,"请先卸下装备再出售");}
                else if(item.locked)
                {
                    if(NavigationButton(action,"解锁保护",gold,true,"解除锁定后才可出售；解锁不会出售装备。"))unlock=item.id;
                }
                else if(DangerButton(action,"出售 "+p.SellValue(item)+" 金",gold,true,"出售闲置装备；预设引用的装备会先要求确认。"))sell=item.id;
            }
            if(bagItems.Count==0)Text(new Rect(12*u,178*u,width-24*u,40*u),"没有可交易的闲置装备。",Mathf.RoundToInt(15*u),muted);
            if(unlock!=null){Feedback(p.SetItemLocked(unlock,false),"装备已解锁，可出售");BlockUITransition();}
            if(bulkSale)RequestPresetSale(null,true);
            else if(sell!=null){SellInventoryItem(sell);BlockUITransition();}
        }
    }
}
