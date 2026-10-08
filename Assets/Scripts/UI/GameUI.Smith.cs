using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private int smithCategory,smithSelectedSlot;
        private Vector2 smithDetailScroll,smithPreviewScroll;
        private EquipmentMechanic smithPreviewMechanic;
        private bool smithPreviewAscend;
        private void OpenGemPreview(EquipmentMechanic mechanic,bool ascend)
        {smithPreviewMechanic=mechanic;smithPreviewAscend=ascend;smithPreviewScroll=Vector2.zero;}
        private void DrawGemPreview(float u)
        {
            if(smithPreviewMechanic==EquipmentMechanic.None)return;
            var p=session.Progression;var gem=p.Attachment(smithPreviewMechanic);
            if(gem==null){smithPreviewMechanic=EquipmentMechanic.None;return;}
            Fill(new Rect(0,0,width,height),new Color(0,0,0,.55f));blockedRects.Add(new Rect(0,0,width,height));
            float w=Mathf.Min(420*u,width-24*u),h=Mathf.Min(410*u,height-24*u);
            Rect box=new Rect((width-w)*.5f,(height-h)*.5f,w,h);Fill(box,card);Border(box,jade);
            Text(new Rect(box.x+14*u,box.y+10*u,w-70*u,28*u),BuildCatalog.GemName(gem.mechanic),Mathf.RoundToInt(17*u),gold,true);
            if(PopupCloseButton(new Rect(box.xMax-48*u,box.y+4*u,44*u,36*u))){smithPreviewMechanic=EquipmentMechanic.None;return;}
            Rect body=new Rect(box.x+8*u,box.y+48*u,w-16*u,h-144*u);float contentWidth=body.width/u-18,y=0;
            DrawGemUpgradeComparison(ref y,contentWidth,u,gem,smithPreviewAscend,false);
            GoalParagraph(ref y,contentWidth,u,BuildCatalog.MechanicDescription(gem.mechanic),12,muted,false,false);
            smithPreviewScroll=BeginTouchScroll("smith-gem-preview",body,smithPreviewScroll,new Rect(0,0,contentWidth*u,Mathf.Max(body.height,y*u)));
            y=0;DrawGemUpgradeComparison(ref y,contentWidth,u,gem,smithPreviewAscend,true);
            GoalParagraph(ref y,contentWidth,u,BuildCatalog.MechanicDescription(gem.mechanic),12,muted,false,true);EndTouchScroll();
            int cost=smithPreviewAscend?ProgressionService.AscensionCost:ProgressionService.AttachmentUpgradeCost;
            string reason=smithPreviewAscend?gem.rarity==Rarity.Legendary?"已升华":gem.rarity!=Rarity.Epic?"需先升阶至史诗品质":p.HighestAdventureTier<5?"需通关第5阶":!SmithServiceActive?"请在铁匠处操作":p.Profile.mechanicMaterials<cost?"星烬碎片不足":"":p.AttachmentUpgradeLock(gem.mechanic,SmithServiceActive);
            bool enough=p.Profile.mechanicMaterials>=cost;
            Text(new Rect(box.x+14*u,box.yMax-88*u,w-28*u,30*u),reason,Mathf.RoundToInt(12*u),reason.Length==0?muted:new Color(1,.35f,.3f),false,true);
            Rect confirm=new Rect(box.x+14*u,box.yMax-52*u,w-28*u,40*u);
            bool accepted=PrimaryButton(confirm,"",gold,reason.Length==0);
            Text(new Rect(confirm.x+10*u,confirm.y,confirm.width-115*u,confirm.height),smithPreviewAscend?"确认升华":"确认升阶",Mathf.RoundToInt(14*u),reason.Length==0?pale:muted,true,false,TextAnchor.MiddleLeft);
            DrawPriceTint(new Rect(confirm.xMax-100*u,confirm.y,90*u,confirm.height),cost,true,u,enough?gold:new Color(1,.25f,.2f));
            if(accepted){bool saved=smithPreviewAscend?p.AscendAttachment(gem.mechanic,SmithServiceActive):p.UpgradeAttachment(gem.mechanic,SmithServiceActive);Feedback(saved,smithPreviewAscend?"宝石已升华":"宝石已升阶");if(saved)smithPreviewMechanic=EquipmentMechanic.None;}
        }
        private void DrawSmithService()
        {
            var p=session.Progression;float u=MobileControls.Active?TouchRatio:1;
            bool prior=GUI.enabled;GUI.enabled=prior&&smithPreviewMechanic==EquipmentMechanic.None;
            var l=new SmithServiceLayout(width/u,height/u,MobileControls.IsIPad);
            Fill(new Rect(0,0,width,height),new Color(.018f,.031f,.048f,.985f));blockedRects.Add(new Rect(0,0,width,height));
            Text(BuildPlanRect(l.Header,u),"铁匠",Mathf.RoundToInt(22*u),gold,true);
            DrawServiceBalances(BuildPlanRect(l.Balance,u),u);
            if(PopupCloseButton(BuildPlanRect(l.Close,u))){smithPreviewMechanic=EquipmentMechanic.None;GUI.enabled=prior;ClosePanel();return;}
            smithCategory=Mathf.Clamp(smithCategory,0,1);
            string[] categories={"强化","镶嵌"};
            for(int i=0;i<categories.Length;i++)
                if(TabButton(BuildPlanRect(l.Category(i),u),categories[i],smithCategory==i)&&smithCategory!=i){smithCategory=i;smithDetailScroll=Vector2.zero;}
            smithSelectedSlot=Mathf.Clamp(smithSelectedSlot,0,2);
            for(int i=0;i<3;i++)
            {
                var slot=(ItemSlot)i;var gear=p.Equipped(slot);Rect row=BuildPlanRect(l.Equipment(i),u);
                if(QuietAction(row,"",gear!=null,null,smithSelectedSlot==i)&&smithSelectedSlot!=i){smithSelectedSlot=i;smithDetailScroll=Vector2.zero;}
                Text(new Rect(row.x+46*u,row.y+6*u,row.width-52*u,row.height-12*u),GameBalance.SlotName(slot)+"\n+"+p.SlotUpgradeRank(slot),Mathf.RoundToInt(12*u),pale,true,true);
                DrawIcon(new Rect(row.x+8*u,row.y+8*u,30*u,30*u),UIIconAtlas.EquipmentCardIcon(slot,gear==null?1:gear.level,gear==null?Rarity.Common:gear.rarity,session.Progression.Profile.heroClass),gear==null?muted:GameBalance.RarityColor(gear.rarity));
            }
            var item=p.Equipped((ItemSlot)smithSelectedSlot);
            if(item==null){Text(BuildPlanRect(l.Detail,u),"先在行囊中穿戴这个部位的装备。",Mathf.RoundToInt(16*u),muted,false,true);GUI.enabled=prior;DrawGemPreview(u);return;}
            float contentHeight=DrawSmithDetail(item,l.Detail.Width-18,u,false);
            Rect detailArea=BuildPlanRect(l.Detail,u);if(smithCategory==1)detailArea.height+=54*u;
            smithDetailScroll=BeginTouchScroll("smith-selected-detail",detailArea,smithDetailScroll,new Rect(0,0,(l.Detail.Width-18)*u,Mathf.Max(detailArea.height,contentHeight*u)));
            DrawSmithDetail(item,l.Detail.Width-18,u,true);EndTouchScroll();
            if(smithCategory==0)
            {
                var quote=p.PrepareSmithUpgrade(item.slot,SmithServiceActive);
                bool capped=p.SlotUpgradeRank(item.slot)>=p.CurrentUpgradeLimit;
                int cost=p.UpgradeCost(item);bool affordable=p.Profile.gold>=cost;
                Rect action=BuildPlanRect(l.Primary,u);
                if(PrimaryButton(action,"",gold,quote!=null))
                {Feedback(p.UpgradeAtSmith(quote,SmithServiceActive),"部位强化已保存，换装自动继承");}
                Text(new Rect(action.x+12*u,action.y,action.width-126*u,action.height),capped?"已达角色等级上限":affordable?"强化装备":"强化装备 · 金币不足",Mathf.RoundToInt(14*u),pale,true,false,TextAnchor.MiddleLeft);
                if(!capped)DrawPriceTint(new Rect(action.xMax-108*u,action.y,96*u,action.height),cost,false,u,affordable?gold:new Color(.98f,.28f,.24f));
            }
            GUI.enabled=prior;DrawGemPreview(u);
        }
        private void DrawGemUpgradeComparison(ref float y,float width,float u,MechanicAttachment gem,bool ascend,bool draw)
        {
            int nextRank=ascend?gem.upgradeRank:Mathf.Min(ProgressionService.MaximumAttachmentRank,gem.upgradeRank+1);
            Rarity nextRarity=ascend?Rarity.Legendary:nextRank>=3&&gem.rarity<Rarity.Epic?Rarity.Epic:gem.rarity;
            bool capped=ascend?gem.rarity==Rarity.Legendary:gem.upgradeRank>=ProgressionService.MaximumAttachmentRank;
            float currentPower=1f+.08f*gem.upgradeRank+(gem.upgradeRank>=5?.2f:0)+(gem.rarity==Rarity.Legendary?.15f:0);
            float nextPower=1f+.08f*nextRank+(nextRank>=5?.2f:0)+(nextRarity==Rarity.Legendary?.15f:0);
            string[] labels={"阶数","品质","机制强度","作用范围","攻击加成","生命加成"};
            string[] before={"+"+gem.upgradeRank,GameBalance.RarityName(gem.rarity),currentPower.ToString("0.00")+"×",gem.upgradeRank>=3?"1.20×":"1.00×","+"+(gem.upgradeRank*1.5f).ToString("0.#")+"%","+"+(gem.upgradeRank*2)+"%"};
            string[] after={"+"+nextRank,GameBalance.RarityName(nextRarity),nextPower.ToString("0.00")+"×",nextRank>=3?"1.20×":"1.00×","+"+(nextRank*1.5f).ToString("0.#")+"%","+"+(nextRank*2)+"%"};
            GoalParagraph(ref y,width,u,(ascend?"升华":"升阶")+(capped?" · 已达上限":!gem.mounted?" · 镶嵌后生效":""),15,gold,true,draw);
            if(draw)
            {
                float col=(width-16)/3;
                Fill(new Rect(8*u,y*u,(width-16)*u,200*u),card);
                Text(new Rect((8+col)*u,y*u,col*u,28*u),"当前",Mathf.RoundToInt(12*u),muted,true,false,TextAnchor.MiddleCenter);
                Text(new Rect((8+col*2)*u,y*u,col*u,28*u),capped?"保持":ascend?"升华后":"升阶后",Mathf.RoundToInt(12*u),jade,true,false,TextAnchor.MiddleCenter);
                for(int row=0;row<labels.Length;row++)
                {
                    float rowY=(y+28+row*28)*u;
                    Text(new Rect(12*u,rowY,(col-4)*u,28*u),labels[row],Mathf.RoundToInt(12*u),pale);
                    Text(new Rect((8+col)*u,rowY,col*u,28*u),before[row],Mathf.RoundToInt(13*u),pale,true,false,TextAnchor.MiddleCenter);
                    Text(new Rect((8+col*2)*u,rowY,col*u,28*u),after[row],Mathf.RoundToInt(13*u),before[row]==after[row]?muted:jade,true,false,TextAnchor.MiddleCenter);
                }
            }
            y+=208;
        }
        private float DrawSmithDetail(ItemData item,float width,float u,bool draw)
        {
            var p=session.Progression;float y=8;
            if(smithCategory==0){if(draw)DrawIcon(new Rect((width-64)*.5f*u,y*u,64*u,64*u),UIIconAtlas.EquipmentCardIcon(item.slot,item.level,item.rarity,session.Progression.Profile.heroClass),GameBalance.RarityColor(item.rarity));y+=72;}
            GoalParagraph(ref y,width,u,item.name+" · Lv"+item.level+" · "+GameBalance.RarityName(item.rarity),17,gold,true,draw);
            if(smithCategory==0)
            {
                int rank=p.SlotUpgradeRank(item.slot);var next=p.PreviewUpgrade(item,Mathf.Max(rank,Mathf.Min(rank+1,p.CurrentUpgradeLimit)));
                GoalParagraph(ref y,width,u,GameBalance.SlotName(item.slot)+"强化等级  +"+rank+" → +"+Mathf.Max(rank,Mathf.Min(rank+1,p.CurrentUpgradeLimit)),15,pale,true,draw);
                GoalParagraph(ref y,width,u,"强化上限 +"+p.CurrentUpgradeLimit+" · 随角色等级提升",12,muted,false,draw);
                GoalParagraph(ref y,width,u,"攻击  "+item.attack+" → "+next.attack+"\n防御  "+item.defense+" → "+next.defense+"\n生命  "+item.health+" → "+next.health,15,jade,false,draw);

            }
            else
            {

                foreach(var mechanic in BuildCatalog.MechanicsFor(p.Profile.heroClass))
                {
                    if(BuildCatalog.MechanicSlot(mechanic)!=item.slot)continue;
                    var a=p.Attachment(mechanic);
                    if(a==null)
                    {
                        if(draw){Rect empty=new Rect(8*u,y*u,64*u,64*u);Fill(empty,card);Border(empty,muted,2);DrawIcon(new Rect(empty.x+14*u,empty.y+14*u,36*u,36*u),UIIconAtlas.Utility("upgrade"),muted);}
                        y+=72;GoalParagraph(ref y,width,u,BuildCatalog.GemName(mechanic)+" · 空槽，获得宝石后可镶嵌",13,muted,false,draw);continue;
                    }
                    GoalParagraph(ref y,width,u,BuildCatalog.GemName(mechanic)+" · +"+a.upgradeRank,15,pale,true,draw);
                    if(draw)
                    {
                        Rect socket=new Rect(8*u,y*u,64*u,64*u),owned=new Rect(94*u,y*u,64*u,64*u);
                        if(QuietAction(socket,"",SmithServiceActive&&a.mounted))Feedback(p.SetAttachmentMounted(mechanic,false,SmithServiceActive),"宝石已卸下");
                        Border(socket,a.mounted?GameBalance.RarityColor(a.rarity):muted,2);
                        DrawIcon(new Rect(socket.x+10*u,socket.y+10*u,44*u,44*u),UIIconAtlas.Utility(a.mounted?"gem":"upgrade"),a.mounted?GameBalance.RarityColor(a.rarity):muted);
                        if(QuietAction(owned,"",SmithServiceActive&&!a.mounted))Feedback(p.SetAttachmentMounted(mechanic,true,SmithServiceActive),"宝石已镶嵌");
                        Border(owned,a.mounted?muted:GameBalance.RarityColor(a.rarity),2);
                        DrawIcon(new Rect(owned.x+10*u,owned.y+10*u,44*u,44*u),UIIconAtlas.Utility("gem"),GameBalance.RarityColor(a.rarity));
                        if(a.mounted)DrawIcon(new Rect(owned.xMax-20*u,owned.y,20*u,20*u),UIIconAtlas.Utility("confirm"),jade);
                        Text(new Rect(socket.x,socket.yMax+3*u,70*u,22*u),a.mounted?"已镶嵌":"空槽",Mathf.RoundToInt(12*u),muted,false,false,TextAnchor.MiddleCenter);
                        Text(new Rect(owned.x,owned.yMax+3*u,70*u,22*u),"拥有的宝石",Mathf.RoundToInt(12*u),muted,false,false,TextAnchor.MiddleCenter);
                    }
                    y+=94;
                    if(draw)
                    {
                        float actionWidth=(width-24)*.5f;
                        if(QuietAction(new Rect(8*u,y*u,actionWidth*u,40*u),"升阶",true))OpenGemPreview(mechanic,false);
                        if(QuietAction(new Rect((16+actionWidth)*u,y*u,actionWidth*u,40*u),"升华",true))OpenGemPreview(mechanic,true);
                    }
                    y+=48;
                    if(BuildCatalog.HasMechanicVariant(mechanic))
                    {
                        if(draw)for(int variant=0;variant<2;variant++)
                        {
                            Rect option=new Rect((width-100+variant*48)*u,(y-142)*u,44*u,44*u);
                            string optionDescription=BuildCatalog.MechanicDescription(mechanic);
                            int split=optionDescription.IndexOf("变体B：",System.StringComparison.Ordinal);
                            if(split>=0)optionDescription=variant==0?optionDescription.Substring(0,split).Replace("变体A：",""):optionDescription.Substring(split).Replace("变体B：","");
                            if(!MobileControls.Active&&option.Contains(Mouse))tooltip=optionDescription;
                            bool selected=a.variant==variant;
                            bool allowed=SmithServiceActive&&!selected&&(a.variantUnlocked||p.Profile.mechanicMaterials>=ProgressionService.VariantCost);
                            if(QuietAction(option,"",allowed))Feedback(p.ToggleAttachmentVariant(mechanic,SmithServiceActive),"宝石变体已保存");
                            Border(option,selected?jade:muted,selected?2:1);
                            DrawIcon(new Rect(option.x+6*u,option.y+6*u,32*u,32*u),UIIconAtlas.Utility(variant==0?"core":"attack"),selected?jade:pale);
                            if(selected)DrawIcon(new Rect(option.xMax-18*u,option.y,18*u,18*u),UIIconAtlas.Utility("confirm"),jade);
                            else if(!a.variantUnlocked)DrawIcon(new Rect(option.xMax-18*u,option.y,18*u,18*u),UIIconAtlas.Utility("lock"),gold);
                        }

                    }

                }


            }
            return y+8;
        }
    }
}
