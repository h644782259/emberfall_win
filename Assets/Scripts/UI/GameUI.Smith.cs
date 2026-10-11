using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private int smithCategory,smithSelectedSlot;
        private string smithUpgradeNotice,smithUpgradeNoticeItem,smithUpgradeNoticeSlot;
        private ProgressionService smithUpgradeNoticeOwner;
        private float smithUpgradeNoticeUntil;
        private bool smithUpgradeNoticeSuccess;
        private bool HasSmithUpgradeNotice(ItemData item,ProgressionService owner)
        {return item!=null&&smithUpgradeNoticeOwner==owner&&smithUpgradeNoticeSlot==owner.CurrentSlotId&&smithUpgradeNoticeItem==item.id&&Time.unscaledTime<smithUpgradeNoticeUntil;}

        private readonly System.Collections.Generic.Dictionary<string,Vector2> smithVariantScroll=new System.Collections.Generic.Dictionary<string,Vector2>();
        private bool smithSocketPicker;
        private EquipmentMechanic smithSocketPrevious;
        private Vector2 smithSocketScroll;
        private Vector2 smithDetailScroll,smithPreviewScroll;
        private ProgressionService.AffixReforgeQuote smithAffixQuote;
        private EquipmentMechanic smithPreviewMechanic;
        private bool smithPreviewAscend;
        private void OpenGemPreview(EquipmentMechanic mechanic,bool ascend)
        {smithPreviewMechanic=mechanic;smithPreviewAscend=ascend;smithPreviewScroll=Vector2.zero;}
        private void DrawGemPreview(float u)
        {
            if(smithPreviewMechanic==EquipmentMechanic.None)return;
            var p=session.Progression;var gem=p.Attachment(smithPreviewMechanic);
            if(gem==null){smithPreviewMechanic=EquipmentMechanic.None;return;}
            blockedRects.Add(new Rect(0,0,width,height));
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
            string reason=smithPreviewAscend?p.AttachmentAscensionLock(gem.mechanic,SmithServiceActive):p.AttachmentUpgradeLock(gem.mechanic,SmithServiceActive);
            bool enough=p.Profile.mechanicMaterials>=cost;
            Text(new Rect(box.x+14*u,box.yMax-88*u,w-28*u,30*u),reason,Mathf.RoundToInt(12*u),reason.Length==0?muted:new Color(1,.35f,.3f),false,true);
            Rect confirm=new Rect(box.x+14*u,box.yMax-52*u,w-28*u,40*u);
            bool accepted=PrimaryButton(confirm,"",gold,reason.Length==0);
            Text(new Rect(confirm.x+10*u,confirm.y,confirm.width-115*u,confirm.height),smithPreviewAscend?(gem.ascensionRank>=3?"升华已满":"确认升华"):(gem.upgradeRank>=ProgressionService.MaximumAttachmentRank?"已满阶":"继续升阶"),Mathf.RoundToInt(14*u),reason.Length==0?pale:muted,true,false,TextAnchor.MiddleLeft);
            DrawPriceTint(new Rect(confirm.xMax-100*u,confirm.y,90*u,confirm.height),cost,true,u,enough?gold:new Color(1,.25f,.2f));
            if(accepted){bool saved=smithPreviewAscend?p.AscendAttachment(gem.mechanic,SmithServiceActive):p.UpgradeAttachment(gem.mechanic,SmithServiceActive);Feedback(saved,smithPreviewAscend?"宝石已升华":"宝石已升阶");if(saved)smithPreviewScroll=Vector2.zero;}
        }
        private void DrawSmithResource(Rect rect,int amount,Texture2D icon,Color tint,float u,string resourceKey)
        {
            Fill(rect,new Color(.14f,.115f,.055f,.55f));Border(rect,new Color(tint.r,tint.g,tint.b,.25f));
            DrawIcon(new Rect(rect.x+4*u,rect.center.y-9*u,18*u,18*u),icon,tint);
            Text(new Rect(rect.x+27*u,rect.y,rect.width-31*u,rect.height),amount.ToString(),Mathf.RoundToInt(13*u),pale,true,false,TextAnchor.MiddleLeft);
            InspectRewardItem(rect,ResourceItemPreview(resourceKey,amount));
        }
        private void DrawSmithMaxBadge(Rect rect,string label,float u)
        {
            Fill(rect,new Color(.24f,.14f,.035f));Border(rect,gold,2*u);
            Text(rect,label,Mathf.RoundToInt(15*u),new Color(1,.9f,.55f),true,false,TextAnchor.MiddleCenter);
        }
        private void DrawSmithService()
        {
            var p=session.Progression;float u=MobileControls.Active?TouchRatio:1;
            bool prior=GUI.enabled;GUI.enabled=prior&&smithPreviewMechanic==EquipmentMechanic.None&&!smithSocketPicker&&smithAffixQuote==null;
            var l=new SmithServiceLayout(width/u,height/u,MobileControls.IsIPad);
            blockedRects.Add(new Rect(0,0,width,height));
            Box(new Rect(8*u,4*u,width-16*u,height-8*u),jade,false);
            float balanceWidth=Mathf.Min(100,(width/u-132)/4);
            float balanceStart=width-64*u-4*(balanceWidth+4)*u;
            Text(new Rect(16*u,10*u,Mathf.Max(48*u,balanceStart-24*u),36*u),"铁匠",Mathf.RoundToInt(22*u),gold,true);
            DrawPrice(new Rect(balanceStart,12*u,balanceWidth*u,28*u),p.Profile.gold,false,u);
            DrawPrice(new Rect(balanceStart+(balanceWidth+4)*u,12*u,balanceWidth*u,28*u),p.Profile.mechanicMaterials,true,u);
            InspectRewardItem(new Rect(balanceStart,12*u,balanceWidth*u,28*u),ResourceItemPreview("gold",p.Profile.gold));
            InspectRewardItem(new Rect(balanceStart+(balanceWidth+4)*u,12*u,balanceWidth*u,28*u),ResourceItemPreview("shard",p.Profile.mechanicMaterials));
            DrawSmithResource(new Rect(balanceStart+2*(balanceWidth+4)*u,12*u,balanceWidth*u,28*u),p.Profile.refinementStones,UIIconAtlas.Utility("gem"),GameBalance.RarityColor(Rarity.Epic),u,"refinement");
            DrawSmithResource(new Rect(balanceStart+3*(balanceWidth+4)*u,12*u,balanceWidth*u,28*u),p.Profile.fashionThreads,UIIconAtlas.Reward(2),GameBalance.RarityColor(Rarity.Rare),u,"thread");
            if(PopupCloseButton(new Rect(width-56*u,10*u,42*u,36*u))){smithPreviewMechanic=EquipmentMechanic.None;GUI.enabled=prior;ClosePanel();return;}
            smithCategory=Mathf.Clamp(smithCategory,0,3);
            string[] categories={"强化","镶嵌","洗练","时装"};
            for(int i=0;i<categories.Length;i++)
                if(TabButton(new Rect((16+i*116)*u,58*u,108*u,40*u),categories[i],smithCategory==i)&&smithCategory!=i){smithCategory=i;smithDetailScroll=Vector2.zero;}
            Rect body=new Rect(16*u,110*u,width-32*u,height-122*u);
            if(smithCategory==3){DrawFashionSmith(body,u);GUI.enabled=prior;return;}
            if(smithCategory==1)
            {
                DrawPhoneSocketService(body,u);
                GUI.enabled=prior;DrawGemPreview(u);DrawSocketPicker(u);DrawAffixConfirmation(u);return;
            }
            int columns=width/u>=540?3:1;
            float cardWidth=(body.width/u-(columns-1)*12)/columns;
            float[] cardHeights=new float[3];float rowHeight=0;
            for(int i=0;i<3;i++)
            {
                var gear=p.Equipped((ItemSlot)i);
                cardHeights[i]=gear==null?180:DrawSmithDetail(gear,cardWidth-16,u,false)+(smithCategory==0?(HasSmithUpgradeNotice(gear,p)?(MobileControls.Active?94:106):(MobileControls.Active?50:62)):0)+16;
                rowHeight=Mathf.Max(rowHeight,cardHeights[i]);
            }
            float contentHeight=columns==3?rowHeight:cardHeights[0]+cardHeights[1]+cardHeights[2]+24;
            smithDetailScroll=BeginTouchScroll("smith-all-equipment",body,smithDetailScroll,new Rect(0,0,body.width,Mathf.Max(body.height,contentHeight*u)));
            float top=0;
            for(int i=0;i<3;i++)
            {
                var slot=(ItemSlot)i;var item=p.Equipped(slot);
                Rect tile=new Rect((columns==3?i*(cardWidth+12):0)*u,(columns==3?0:top)*u,cardWidth*u,(columns==3?rowHeight:cardHeights[i])*u);
                Fill(tile,card);Border(tile,item==null?muted:GameBalance.RarityColor(item.rarity));
                GUI.BeginGroup(new Rect(tile.x+8*u,tile.y+8*u,tile.width-16*u,tile.height-16*u));
                if(item==null)
                {
                    DrawIcon(new Rect((cardWidth-64)*.5f*u,12*u,48*u,48*u),UIIconAtlas.EquipmentCardIcon(slot,1,Rarity.Common,p.Profile.heroClass),muted);
                    Text(new Rect(0,72*u,(cardWidth-16)*u,28*u),GameBalance.SlotName(slot),Mathf.RoundToInt(15*u),pale,true,false,TextAnchor.MiddleCenter);
                    Text(new Rect(0,110*u,(cardWidth-16)*u,40*u),"未穿戴",Mathf.RoundToInt(12*u),muted,false,false,TextAnchor.MiddleCenter);
                }
                else
                {
                    float y=DrawSmithDetail(item,cardWidth-16,u,true);
                    if(smithCategory==2)
                    {
                        string rerollReason=p.AffixReforgeLockReason(item.id,SmithServiceActive);
                        string reason=p.RefinementLockReason(item.id,SmithServiceActive);
                        float aw=(cardWidth-28)*.5f,buttonHeight=44;
                        Rect action=new Rect(4*u,tile.height-68*u,aw*u,buttonHeight*u);
                        Rect reroll=new Rect(action.xMax+4*u,action.y,aw*u,buttonHeight*u);
                        if(SmithMaterialAction(action,reason=="数值已满"?"已满":"洗练",GameBalance.RarityColor(Rarity.Epic),u,reason.Length==0,reason))
                            Feedback(p.RefineEquipment(item.id,SmithServiceActive),"洗练已保存");
                        if(SmithMaterialAction(reroll,"词条重铸",GameBalance.RarityColor(Rarity.Rare),u,rerollReason.Length==0,rerollReason))
                            smithAffixQuote=p.PrepareAffixReforge(item.id,SmithServiceActive);
                    }
                    if(smithCategory==0)
                    {
                        var quote=p.PrepareSmithUpgrade(slot,SmithServiceActive);
                        int cost=p.UpgradeCost(item);bool capped=p.SlotUpgradeRank(slot)>=p.CurrentUpgradeLimit;
                        float actionWidth=Mathf.Min(148,cardWidth-32);
                        float actionHeight=MobileControls.Active?36:44;
                        Rect action=new Rect((cardWidth-16-actionWidth)*.5f*u,tile.height-(28+actionHeight)*u,actionWidth*u,actionHeight*u);
                        Color accent=capped||quote!=null?gold:muted;
                        Fill(action,new Color(accent.r,accent.g,accent.b,.13f));Border(action,new Color(accent.r,accent.g,accent.b,.55f));
                        if(!capped&&QuietAction(action,"",quote!=null))
                        {
                            int attempts=p.Profile.equipmentUpgradeAttempts;
                            bool success=p.UpgradeAtSmith(quote,SmithServiceActive);
                            bool paid=p.Profile.equipmentUpgradeAttempts>attempts;
                            smithUpgradeNotice=success?"强化成功 · +"+p.SlotUpgradeRank(slot)+"\n已消耗 "+cost+" 金币":paid?"强化失败 · 等级保持 +"+p.SlotUpgradeRank(slot)+"\n已消耗 "+cost+" 金币":p.LastError;
                            smithUpgradeNoticeOwner=p;smithUpgradeNoticeSlot=p.CurrentSlotId;smithUpgradeNoticeItem=item.id;
                            smithUpgradeNoticeSuccess=success;smithUpgradeNoticeUntil=Time.unscaledTime+5;
                            Feedback(success,smithUpgradeNotice.Replace("\n"," · "));
                        }
                        if(HasSmithUpgradeNotice(item,p))Text(new Rect(8*u,action.y-44*u,(cardWidth-32)*u,40*u),smithUpgradeNotice,Mathf.RoundToInt(12*u),smithUpgradeNoticeSuccess?jade:new Color(1,.48f,.4f),true,true,TextAnchor.MiddleCenter);
                        if(capped)DrawSmithMaxBadge(action,"已满级",u);
                        else DrawIcon(new Rect(action.x+10*u,action.y+11*u,22*u,22*u),UIIconAtlas.Utility("upgrade"),accent);
                        if(!capped)
                        {
                            DrawIcon(new Rect(action.x+42*u,action.y+13*u,18*u,18*u),UIIconAtlas.Utility("coin"),gold);
                            Text(new Rect(action.x+66*u,action.y,action.width-72*u,action.height),cost.ToString(),Mathf.RoundToInt(12*u),p.Profile.gold>=cost?accent:new Color(.98f,.28f,.24f),true,false,TextAnchor.MiddleLeft);
                        }
                    }
                }
                GUI.EndGroup();top+=cardHeights[i]+12;
            }
            EndTouchScroll();
            GUI.enabled=prior;DrawGemPreview(u);DrawSocketPicker(u);DrawAffixConfirmation(u);
        }
        private string PhoneGemFormDescription(MechanicAttachment gem,int variant)
        {
            switch(gem.mechanic)
            {
                case EquipmentMechanic.CinderTrail:return variant==0?"陨星直伤−20%；完整火场，4跳共40%伤害。":"陨星直伤−20%；火场缩小30%，4跳共57%伤害。";
                case EquipmentMechanic.FrostEcho:return variant==0?"新星首击80%；延迟回响60%伤害，再次控制。":"范围+35%；首击65%、回响45%伤害，控制更多敌人。";
                case EquipmentMechanic.ReturningBlade:return variant==0?"普攻−8%；回刃弹射110%，回收强化下一刀。":"取消弹射；完美闪避后3秒内可突进反击175%。";
                case EquipmentMechanic.VenomSpread:return variant==0?"毒爆−20%；每2秒向附近2个目标传播毒素。":"扇形箭变单发毒矢，伤害240%/360%/480%；无传播。";
                default:return SmithVariantDescription(gem.mechanic,variant);
            }
        }
        private void DrawPhoneSocketService(Rect body,float u)
        {
            var p=session.Progression;smithSelectedSlot=Mathf.Clamp(smithSelectedSlot,0,2);
            float slotSize=Mathf.Min(64,(body.height/u-16)/3);
            for(int i=0;i<3;i++)
            {
                var item=p.Equipped((ItemSlot)i);Rect tab=new Rect(body.x,body.y+i*(slotSize+8)*u,slotSize*u,slotSize*u);
                Fill(tab,smithSelectedSlot==i?new Color(.08f,.25f,.24f):card);Border(tab,smithSelectedSlot==i?gold:muted,smithSelectedSlot==i?3:1);
                DrawIcon(new Rect(tab.x+7*u,tab.y+7*u,tab.width-14*u,tab.height-14*u),UIIconAtlas.EquipmentCardIcon((ItemSlot)i,item==null?1:item.level,item==null?Rarity.Common:item.rarity,p.Profile.heroClass),item==null?muted:GameBalance.RarityColor(item.rarity));
                if(QuietAction(tab,"")){smithSelectedSlot=i;smithDetailScroll=Vector2.zero;}
            }
            Rect detail=new Rect(body.x+(slotSize+12)*u,body.y,body.width-(slotSize+12)*u,body.height);Fill(detail,card);Border(detail,jade*.4f);
            var selected=p.Equipped((ItemSlot)smithSelectedSlot);
            if(selected==null){Text(detail,"未穿戴",Mathf.RoundToInt(14*u),muted,false,false,TextAnchor.MiddleCenter);return;}
            GUI.BeginGroup(detail);
            u=Mathf.Min(u,detail.height/320f);
            float w=detail.width/u,h=detail.height/u,left=Mathf.Min(190,w*.37f),right=w-left-16;
            Text(new Rect(8*u,2*u,(w-16)*u,22*u),selected.name,Mathf.RoundToInt(12*u),gold,true);
            var gems=new System.Collections.Generic.List<MechanicAttachment>();
            foreach(var mechanic in BuildCatalog.GemsFor(p.Profile.heroClass)){var value=p.Attachment(mechanic);if(value!=null&&value.mounted&&BuildCatalog.MechanicSlot(mechanic)==selected.slot)gems.Add(value);}
            float rowHeight=(h-32)/3,formWidth=(right-8)*.5f;
            for(int variant=0;variant<2;variant++)
            {
                Rect column=new Rect((left+8+variant*(formWidth+8))*u,28*u,formWidth*u,(h-36)*u);
                Fill(column,new Color(.035f,.075f,.10f));Border(column,jade*.4f);
            }
            for(int index=0;index<ProgressionService.MaximumMountedGemsPerSlot;index++)
            {
                var gem=index<gems.Count?gems[index]:null;float top=28+index*rowHeight;
                float size=Mathf.Clamp(rowHeight-64,28,44);Rect socket=new Rect(8*u,(top+4)*u,size*u,size*u);
                Fill(socket,card);Border(socket,gem==null?jade:GameBalance.RarityColor(gem.rarity),2);
                if(gem==null)Text(socket,"+",Mathf.RoundToInt(24*u),jade,false,false,TextAnchor.MiddleCenter);
                else {DrawIcon(socket,UIIconAtlas.Utility("gem"),GameBalance.RarityColor(gem.rarity));DrawWornIconBadge(socket,u,"已镶嵌");}
                if(QuietAction(socket,"",SmithServiceActive)){smithSocketPrevious=gem==null?EquipmentMechanic.None:gem.mechanic;smithSocketPicker=true;smithSocketScroll=Vector2.zero;}
                if(gem==null)
                {
                    Text(new Rect((size+14)*u,(top+4)*u,(left-size-18)*u,36*u),"宝石槽 "+(index+1),Mathf.RoundToInt(11*u),muted,false,true);
                    for(int variant=0;variant<2;variant++)Text(new Rect((left+16+variant*(formWidth+8))*u,(top+8)*u,(formWidth-16)*u,(rowHeight-16)*u),"未镶嵌",Mathf.RoundToInt(11*u),muted,false,false,TextAnchor.MiddleCenter);
                    continue;
                }
                Text(new Rect((size+14)*u,(top+2)*u,(left-size-18)*u,22*u),BuildCatalog.GemName(gem.mechanic),Mathf.RoundToInt(11*u),GameBalance.RarityColor(gem.rarity),true);
                Text(new Rect((size+14)*u,(top+24)*u,(left-size-18)*u,20*u),gem.upgradeRank+"阶 · 升华"+gem.ascensionRank,Mathf.RoundToInt(10*u),gold,true);
                float actionY=top+Mathf.Max(size+8,46),buttonWidth=(left-18)*.5f;
                for(int action=0;action<2;action++)
                {
                    bool ascend=action==0;string reason=ascend?p.AttachmentAscensionLock(gem.mechanic,SmithServiceActive):p.AttachmentUpgradeLock(gem.mechanic,SmithServiceActive);
                    if(Button(new Rect((6+action*(buttonWidth+6))*u,actionY*u,buttonWidth*u,28*u),ascend?(gem.ascensionRank>=3?"已满":"升华"):(gem.upgradeRank>=9?"已满阶":"升阶"),ascend?gold:jade,reason.Length==0,reason))OpenGemPreview(gem.mechanic,ascend);
                }
                string hint=gem.ascensionRank>=3?"升华已满":gem.upgradeRank<(gem.ascensionRank+1)*3?"升至"+((gem.ascensionRank+1)*3)+"阶可升华":p.Profile.mechanicMaterials<ProgressionService.AscensionCost?"升华需24碎片":"";
                if(rowHeight>=100)Text(new Rect(6*u,(actionY+30)*u,(left-12)*u,18*u),hint,Mathf.RoundToInt(9*u),muted,false,false,TextAnchor.MiddleCenter);
                if(rowHeight>=128)
                {
                    string attribute=BuildCatalog.IsAttributeGem(gem.mechanic)?BuildCatalog.GemAttributeSummary(gem.mechanic,gem.rarity,gem.upgradeRank):BuildCatalog.AttributeLabel(BuildCatalog.MechanicAttribute(gem.mechanic))+" +"+(BuildCatalog.MechanicAttributeValue(gem.mechanic,gem.upgradeRank)*100).ToString("0.#")+"%";
                    DrawGemEffectText(new Rect(6*u,(actionY+50)*u,(left-12)*u,20*u),attribute,Mathf.RoundToInt(10*u),jade,true,TextAnchor.MiddleCenter);
                }
                for(int variant=0;variant<2;variant++)
                {
                    Rect option=new Rect((left+12+variant*(formWidth+8))*u,(top+3)*u,(formWidth-8)*u,(rowHeight-8)*u);
                    bool active=gem.variantUnlocked&&gem.variant==variant;Fill(option,active?new Color(.055f,.19f,.18f):card);Border(option,active?gold:muted*.4f);
                    DrawIcon(new Rect(option.x+5*u,option.y+5*u,16*u,16*u),UIIconAtlas.Utility(active?"confirm":variant==0?"core":"attack"),active?gold:jade);
                    Text(new Rect(option.x+25*u,option.y+2*u,option.width-30*u,22*u),SmithVariantName(gem.mechanic,variant),Mathf.RoundToInt(11*u),active?gold:pale,true,false,TextAnchor.MiddleLeft);
                    string formText=gem.variantUnlocked?PhoneGemFormDescription(gem,variant):"升华后解锁 · "+PhoneGemFormDescription(gem,variant);
                    Rect copy=new Rect(option.x+6*u,option.y+27*u,option.width-12*u,option.height-31*u);
                    int font=Mathf.RoundToInt(12*u);while(font>Mathf.RoundToInt(9*u)&&Style(font,false,true).CalcHeight(new GUIContent(formText),copy.width)>copy.height)font--;
                    Text(copy,formText,font,gem.variantUnlocked?pale:muted,false,true);
                    if(QuietAction(option,"",!active&&gem.variantUnlocked&&SmithServiceActive))SelectSmithVariant(gem.mechanic,variant);
                }
            }
            GUI.EndGroup();
        }
        private void DrawGemUpgradeComparison(ref float y,float width,float u,MechanicAttachment gem,bool ascend,bool draw)
        {
            int nextRank=ascend?gem.upgradeRank:Mathf.Min(ProgressionService.MaximumAttachmentRank,gem.upgradeRank+1);
            Rarity nextRarity=!ascend&&nextRank>=3&&gem.rarity<Rarity.Epic?Rarity.Epic:gem.rarity;
            bool capped=ascend?gem.ascensionRank>=3:gem.upgradeRank>=ProgressionService.MaximumAttachmentRank;
            int ascension=Mathf.Max(0,gem.ascensionRank),nextAscension=ascend?Mathf.Min(3,ascension+1):ascension;
            string attribute=BuildCatalog.AttributeLabel(BuildCatalog.MechanicAttribute(gem.mechanic));
            string[] labels={"阶数","升华","机制强度",gem.mechanic==EquipmentMechanic.TwinSummonResonance?"伙伴生命":"作用范围",attribute,"机制形态"};
            string[] before={gem.upgradeRank+" / 9",ascension+" / 3",BuildCatalog.AscensionPower(ascension).ToString("0.00")+"×",BuildCatalog.AscensionRange(ascension).ToString("0.00")+"×","+"+(BuildCatalog.MechanicAttributeValue(gem.mechanic,gem.upgradeRank)*100).ToString("0.#")+"%",gem.variantUnlocked?"已解锁":"首次升华解锁"};
            string[] after={nextRank+" / 9",nextAscension+" / 3",BuildCatalog.AscensionPower(nextAscension).ToString("0.00")+"×",BuildCatalog.AscensionRange(nextAscension).ToString("0.00")+"×","+"+(BuildCatalog.MechanicAttributeValue(gem.mechanic,nextRank)*100).ToString("0.#")+"%",gem.variantUnlocked||nextAscension>0?"已解锁":"首次升华解锁"};
            if(BuildCatalog.IsAttributeGem(gem.mechanic))
            {
                labels=new[]{"阶数","升华",BuildCatalog.GemAttributeLabel(gem.mechanic),"机制形态","形态强度"};
                before=new[]{gem.upgradeRank+" / 9",ascension+" / 3",(BuildCatalog.GemAttributeValue(gem.mechanic,gem.rarity,gem.upgradeRank)*100).ToString("0.#")+"%",ascension>0?"已解锁":"未解锁",(BuildCatalog.GemAscensionValue(gem.mechanic,ascension)*(BuildCatalog.MechanicSlot(gem.mechanic)==ItemSlot.Weapon&&gem.variant==1?150:100)).ToString("0.#")+"%"};
                after=new[]{nextRank+" / 9",nextAscension+" / 3",(BuildCatalog.GemAttributeValue(gem.mechanic,nextRarity,nextRank)*100).ToString("0.#")+"%",nextAscension>0?"已解锁":"未解锁",(BuildCatalog.GemAscensionValue(gem.mechanic,nextAscension)*(BuildCatalog.MechanicSlot(gem.mechanic)==ItemSlot.Weapon&&gem.variant==1?150:100)).ToString("0.#")+"%"};
            }
            if(gem.mechanic==EquipmentMechanic.WeaponRuin)labels[4]="暴击伤害加成";
            if(BuildCatalog.IsAttributeGem(gem.mechanic)&&BuildCatalog.MechanicSlot(gem.mechanic)==ItemSlot.Relic){labels[4]="连携效果";before[4]=BuildCatalog.RelicFormStrength(gem.mechanic,gem.variant,ascension);after[4]=BuildCatalog.RelicFormStrength(gem.mechanic,gem.variant,nextAscension);}
            if(!BuildCatalog.IsAttributeGem(gem.mechanic)&&!BuildCatalog.HasMechanicVariant(gem.mechanic)){labels[5]="共鸣强化";before[5]=ascension+"次";after[5]=nextAscension+"次";}
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
        private string SmithVariantName(EquipmentMechanic mechanic,int variant)
        {
            return BuildCatalog.GemFormName(mechanic,variant);
        }
        private void SelectSmithVariant(EquipmentMechanic mechanic,int variant)
        {
            var p=session.Progression;var gem=p.Attachment(mechanic);
            if(gem==null||variant<0||variant>1||gem.variant==variant)return;
            if(p.ToggleAttachmentVariant(mechanic,SmithServiceActive))Feedback(true,"已切换为"+SmithVariantName(mechanic,variant));
            else Feedback(false,p.LastError);
        }
        private string SmithVariantDescription(EquipmentMechanic mechanic,int variant)
        {
            var gem=session.Progression.Attachment(mechanic);
            return BuildCatalog.GemFormDescription(mechanic,variant,gem==null?0:gem.ascensionRank);
        }

        private void DrawSocketPicker(float u)
        {
            if(!smithSocketPicker)return;
            var p=session.Progression;var slot=(ItemSlot)smithSelectedSlot;
            blockedRects.Add(new Rect(0,0,width,height));
            float w=Mathf.Min(460*u,width-24*u),h=Mathf.Min(420*u,height-24*u);
            Rect box=new Rect((width-w)/2,(height-h)/2,w,h);Fill(box,card);Border(box,jade);
            Text(new Rect(box.x+16*u,box.y+12*u,w-76*u,30*u),GameBalance.SlotName(slot)+" · 选择宝石",Mathf.RoundToInt(18*u),gold,true);
            if(PopupCloseButton(new Rect(box.xMax-48*u,box.y+6*u,44*u,36*u))){smithSocketPicker=false;return;}
            var candidates=new System.Collections.Generic.List<MechanicAttachment>();
            foreach(var mechanic in BuildCatalog.GemsFor(p.Profile.heroClass))
            {
                var a=p.Attachment(mechanic);
                if(a!=null&&BuildCatalog.MechanicSlot(mechanic)==slot)candidates.Add(a);
            }
            Rect body=new Rect(box.x+12*u,box.y+56*u,w-24*u,h-68*u);
            if(candidates.Count==0){Text(body,"暂无可镶嵌的宝石",Mathf.RoundToInt(14*u),muted,false,true,TextAnchor.MiddleCenter);return;}
            float cw=(body.width/u-10)/2;
            smithSocketScroll=BeginTouchScroll("smith-socket-picker",body,smithSocketScroll,new Rect(0,0,body.width,Mathf.Max(body.height,((candidates.Count+1)/2)*178*u)));
            for(int i=0;i<candidates.Count;i++)
            {
                var a=candidates[i];Rect tile=new Rect((i%2)*(cw+10)*u,(i/2)*178*u,cw*u,168*u);
                Fill(tile,new Color(.06f,.09f,.13f));Border(tile,a.mounted?jade:GameBalance.RarityColor(a.rarity));
                Rect gemIcon=new Rect(tile.center.x-24*u,tile.y+12*u,48*u,48*u);
                DrawIcon(gemIcon,UIIconAtlas.Utility("gem"),GameBalance.RarityColor(a.rarity));
                if(a.mounted)DrawWornIconBadge(gemIcon,u,"已镶嵌");
                Text(new Rect(tile.x+6*u,tile.y+65*u,tile.width-12*u,28*u),BuildCatalog.GemName(a.mechanic),Mathf.RoundToInt(13*u),pale,true,false,TextAnchor.MiddleCenter);
                DrawGemEffectText(new Rect(tile.x+6*u,tile.y+94*u,tile.width-12*u,30*u),BuildCatalog.IsAttributeGem(a.mechanic)?BuildCatalog.GemAttributeSummary(a.mechanic,a.rarity,a.upgradeRank):a.mounted?"已镶嵌":"阶数 "+a.upgradeRank,Mathf.RoundToInt(10*u),a.mounted?jade:muted,false,TextAnchor.MiddleCenter);
                string mountReason=p.AttachmentReplacementLock(a.mechanic,smithSocketPrevious,SmithServiceActive);
                if(Button(new Rect(tile.x+8*u,tile.y+124*u,tile.width-16*u,36*u),a.mechanic==smithSocketPrevious?"卸下":a.mounted?"已镶嵌":smithSocketPrevious!=EquipmentMechanic.None?"替换":"镶嵌",jade,SmithServiceActive&&mountReason.Length==0,mountReason))
                {if(a.mechanic==smithSocketPrevious?p.SetAttachmentMounted(a.mechanic,false,SmithServiceActive):p.ReplaceAttachment(a.mechanic,smithSocketPrevious,SmithServiceActive)){Feedback(true,"宝石已更新");smithSocketPicker=false;}else Feedback(false,p.LastError);}
            }
            EndTouchScroll();
        }
        private bool SmithMaterialAction(Rect r,string caption,Color materialTint,float u,bool enabled,string reason)
        {
            bool clicked=DrawButton(r,"",ButtonRole.Action,enabled,reason);
            Color textColor=enabled?pale:muted;
            float iconSize=14*u,costWidth=36*u;
            Text(new Rect(r.x+4*u,r.y,r.width-costWidth-8*u,r.height),caption,Mathf.RoundToInt(11*u),textColor,true,false,TextAnchor.MiddleCenter);
            DrawIcon(new Rect(r.xMax-costWidth,r.center.y-iconSize*.5f,iconSize,iconSize),UIIconAtlas.Utility("gem"),materialTint);
            Text(new Rect(r.xMax-costWidth+iconSize,r.y,costWidth-iconSize-3*u,r.height),"×1",Mathf.RoundToInt(10*u),textColor,true,false,TextAnchor.MiddleCenter);
            return clicked;
        }
        private float DrawSmithDetail(ItemData item,float width,float u,bool draw)
        {
            var p=session.Progression;float y=8;
            if(smithCategory==0){float iconSize=MobileControls.Active?32:64;if(draw)DrawIcon(new Rect((width-iconSize)*.5f*u,y*u,iconSize*u,iconSize*u),UIIconAtlas.EquipmentCardIcon(item.slot,item.level,item.rarity,session.Progression.Profile.heroClass),GameBalance.RarityColor(item.rarity));y+=iconSize+8;}
            GoalParagraph(ref y,width,u,item.name,15,gold,true,draw);
            if(draw)
            {
                float chip=(width-24)/3;
                string[] tags={"Lv."+item.level,GameBalance.RarityName(item.rarity),"+"+p.SlotUpgradeRank(item.slot)};
                for(int i=0;i<tags.Length;i++)
                {
                    Rect tag=new Rect((8+i*(chip+4))*u,y*u,chip*u,24*u);
                    Color tint=i==1?GameBalance.RarityColor(item.rarity):i==2?jade:muted;
                    Fill(tag,new Color(tint.r,tint.g,tint.b,.15f));
                    Text(tag,tags[i],Mathf.RoundToInt(10*u),tint,true,false,TextAnchor.MiddleCenter);
                }
            }
            y+=34;
            if(smithCategory==2)
            {
                var cap=p.PreviewRefinementLimit(item.id);string reason=p.RefinementLockReason(item.id,SmithServiceActive);
                if(cap==null)return y;

                string[] labels={"攻击","防御","生命","暴击率","暴击伤害","攻击加成"};
                string[] current={item.attack.ToString(),item.defense.ToString(),item.health.ToString(),(item.criticalChance*100).ToString("0.##")+"%",(item.criticalDamageBonus*100).ToString("0.##")+"%",(item.attackPercent*100).ToString("0.##")+"%"};
                string[] limits={cap.attack.ToString(),cap.defense.ToString(),cap.health.ToString(),(cap.criticalChance*100).ToString("0.##")+"%",(cap.criticalDamageBonus*100).ToString("0.##")+"%",(cap.attackPercent*100).ToString("0.##")+"%"};
                var basis=p.PreviewUpgrade(item,0);var upper=p.PreviewUpgrade(cap,0);
                float[] values={basis.attack,basis.defense,basis.health,item.criticalChance,item.criticalDamageBonus,item.attackPercent};
                float[] maxima={upper.attack,upper.defense,upper.health,cap.criticalChance,cap.criticalDamageBonus,cap.attackPercent};
                for(int stat=0;stat<6;stat++)
                {
                    bool present=stat==0?item.attack>0:stat==1?item.defense>0:stat==2?item.health>0:stat==3?item.criticalChance>0:stat==4?item.criticalDamageBonus>0:item.attackPercent>0;if(!present)continue;
                    if(draw){
                        float progress=maxima[stat]>0?Mathf.Clamp01(values[stat]/maxima[stat]):1;
                        bool full=values[stat]>=maxima[stat]-.000001f;
                        Color tint=Color.Lerp(new Color(.24f,.62f,.82f),new Color(.35f,.88f,.64f),Mathf.Clamp01(progress/.8f));
                        if(progress>.8f)tint=Color.Lerp(tint,gold,(progress-.8f)/.2f);
                        Rect row=new Rect(8*u,y*u,(width-16)*u,28*u);
                        float labelWidth=48*u;
                        Text(new Rect(row.x,row.y,labelWidth,row.height),labels[stat],Mathf.RoundToInt(11*u),pale);
                        Rect track=new Rect(row.x+labelWidth+4*u,row.y+2*u,row.width-labelWidth-4*u,24*u);
                        Fill(track,new Color(.025f,.045f,.06f));
                        Fill(new Rect(track.x,track.y,track.width*progress,track.height),tint);
                        Border(track,full?gold:jade*.4f,u);
                        // A dark inset keeps light values readable over every fill amount.
                        Rect value=new Rect(track.x+2*u,track.y+2*u,track.width-4*u,track.height-4*u);
                        Fill(value,new Color(.012f,.025f,.035f,.84f));
                        Text(value,current[stat]+" / "+limits[stat]+(full?" 满":""),Mathf.RoundToInt(11*u),pale,true,false,TextAnchor.MiddleCenter);
                    }y+=32;
                }
                y+=76;
            }
            else if(smithCategory==0)
            {
                int rank=p.SlotUpgradeRank(item.slot);var next=p.PreviewUpgrade(item,Mathf.Max(rank,Mathf.Min(rank+1,p.CurrentUpgradeLimit)));
                int[] before={item.attack,item.defense,item.health},after={next.attack,next.defense,next.health};
                int visibleStats=0;for(int stat=0;stat<3;stat++)if(before[stat]>0||after[stat]>0)visibleStats++;
                float statStep=MobileControls.Active?32:38,statHeight=MobileControls.Active?28:32;
                if(draw)
                {
                    int visible=0;for(int stat=0;stat<3;stat++)
                    {
                        if(before[stat]<=0&&after[stat]<=0)continue;
                        Rect row=new Rect(8*u,(y+visible++*statStep)*u,(width-16)*u,statHeight*u);
                        Fill(row,new Color(.025f,.045f,.065f,.8f));
                        Texture2D icon=stat==1?UIIconAtlas.EquipmentCardIcon(ItemSlot.Armor,item.level,item.rarity,p.Profile.heroClass):UIIconAtlas.Utility(stat==0?"attack":"potion");
                        DrawIcon(new Rect(row.x+4*u,row.y+4*u,24*u,24*u),icon,pale);
                        float col=(row.width-32*u)/3;
                        Text(new Rect(row.x+32*u,row.y,col,row.height),before[stat].ToString(),Mathf.RoundToInt(12*u),pale,true,false,TextAnchor.MiddleCenter);
                        Text(new Rect(row.x+32*u+col,row.y,col,row.height),"→",Mathf.RoundToInt(14*u),muted,false,false,TextAnchor.MiddleCenter);
                        Text(new Rect(row.x+32*u+col*2,row.y,col,row.height),after[stat].ToString(),Mathf.RoundToInt(12*u),after[stat]>before[stat]?jade:muted,true,false,TextAnchor.MiddleCenter);
                    }
                }
                y+=MobileControls.Active?visibleStats*statStep+6:114;

            }
            else
            {

                MechanicAttachment mounted=null;
                foreach(var mechanic in BuildCatalog.GemsFor(p.Profile.heroClass))
                {
                    var a=p.Attachment(mechanic);
                    if(BuildCatalog.MechanicSlot(mechanic)==item.slot&&a!=null&&a.mounted){mounted=a;break;}
                }
                if(draw)
                {
                    float size=64,socketX=(width-size)*.5f;
                    Rect socket=new Rect(socketX*u,(y)*u,size*u,size*u);
                    Fill(socket,card);Border(socket,mounted==null?jade:GameBalance.RarityColor(mounted.rarity),2);
                    if(mounted==null)Text(socket,"+",Mathf.RoundToInt(32*u),jade,false,false,TextAnchor.MiddleCenter);
                    else {Rect gemIcon=new Rect(socket.x+8*u,socket.y+8*u,48*u,48*u);DrawIcon(gemIcon,UIIconAtlas.Utility("gem"),GameBalance.RarityColor(mounted.rarity));DrawWornIconBadge(gemIcon,u,"已镶嵌");}
                    if(QuietAction(socket,"",SmithServiceActive)){smithSelectedSlot=(int)item.slot;smithSocketPrevious=mounted==null?EquipmentMechanic.None:mounted.mechanic;smithSocketPicker=true;smithSocketScroll=Vector2.zero;}
                    if(mounted!=null)
                    {
                        Text(new Rect(socket.xMax+8*u,socket.y,Mathf.Max(64*u,(width*u-socket.xMax-12*u)),64*u),mounted.upgradeRank+"阶\n升华 "+mounted.ascensionRank,Mathf.RoundToInt(12*u),gold,true,false,TextAnchor.MiddleLeft);
                        float actionHeight=MobileControls.Active?44:32,actionWidth=Mathf.Min(112,(width-24)/2);
                        float actionX=(width-actionWidth*2-8)*.5f;
                        for(int action=0;action<2;action++)
                        {
                            bool ascend=action==0;
                            string reason=ascend?p.AttachmentAscensionLock(mounted.mechanic,SmithServiceActive):p.AttachmentUpgradeLock(mounted.mechanic,SmithServiceActive);
                            Rect button=new Rect((actionX+action*(actionWidth+8))*u,(y+72)*u,actionWidth*u,actionHeight*u);
                            string caption=ascend?(mounted.ascensionRank>=3?"升华已满":"升华"):(mounted.upgradeRank>=9?"已满阶级":"升阶");
                            if(Button(button,caption,ascend?gold:jade,reason.Length==0))OpenGemPreview(mounted.mechanic,ascend);
                        }
                        string ascendReason=p.AttachmentAscensionLock(mounted.mechanic,SmithServiceActive);
                        if(ascendReason.Length>0)Text(new Rect(8*u,(y+120)*u,(width-16)*u,40*u),ascendReason,Mathf.RoundToInt(11*u),muted,false,true,TextAnchor.MiddleCenter);
                    }
                }
                y+=mounted==null?76:170;
                if(mounted!=null)
                {
                    string effect=BuildCatalog.IsAttributeGem(mounted.mechanic)?BuildCatalog.GemAttributeSummary(mounted.mechanic,mounted.rarity,mounted.upgradeRank):BuildCatalog.AttributeLabel(BuildCatalog.MechanicAttribute(mounted.mechanic))+" +"+(BuildCatalog.MechanicAttributeValue(mounted.mechanic,mounted.upgradeRank)*100).ToString("0.#")+"%";
                    if(MobileControls.Active){if(draw)DrawGemEffectText(new Rect(8*u,y*u,(width-16)*u,26*u),effect,Mathf.RoundToInt(12*u),jade,true,TextAnchor.MiddleLeft);y+=26;}
                    else GoalParagraph(ref y,width,u,effect,12,jade,true,draw);
                }
                if(mounted!=null)
                {
                    if(BuildCatalog.HasMechanicVariant(mounted.mechanic))
                    {
                        GoalParagraph(ref y,width,u,"机制形态",13,muted,false,draw);
                        bool compact=MobileControls.Active&&!MobileControls.IsIPad;
                        bool stackedVariants=width<420&&!compact;
                        float variantWidth=stackedVariants?width-16:width/2-16;
                        float variantHeight=220,variantTextHeight=variantHeight-92;
                        if(draw)for(int variant=0;variant<2;variant++)
                        {
                            Rect option=new Rect((stackedVariants?8:8+variant*(width/2))*u,(y+(stackedVariants?variant*(variantHeight+8):0))*u,variantWidth*u,variantHeight*u);
                            bool selected=mounted.variantUnlocked&&mounted.variant==variant;
                            Fill(option,card);Border(option,selected?jade:muted,selected?2:1);
                            DrawIcon(new Rect(option.x+10*u,option.y+12*u,36*u,36*u),UIIconAtlas.Utility(variant==0?"core":"attack"),selected?jade:pale);
                            Text(new Rect(option.x+50*u,option.y,option.width-54*u,56*u),SmithVariantName(mounted.mechanic,variant),Mathf.RoundToInt(12*u),pale,true,false,TextAnchor.MiddleLeft);
                            DrawIcon(new Rect(option.xMax-18*u,option.y,18*u,18*u),UIIconAtlas.Utility(selected?"confirm":mounted.variantUnlocked?"help":"lock"),selected?jade:gold);
                            string scrollKey="smith-form-"+mounted.mechanic+"-"+variant,description=SmithVariantDescription(mounted.mechanic,variant);
                            Vector2 offset;smithVariantScroll.TryGetValue(scrollKey,out offset);
                            Rect descriptionArea=new Rect(option.x+10*u,option.y+56*u,option.width-20*u,variantTextHeight*u);
                            float textWidth=descriptionArea.width-16*u,textHeight=Style(Mathf.RoundToInt(12*u),false,true).CalcHeight(new GUIContent(description),textWidth);
                            smithVariantScroll[scrollKey]=BeginTouchScroll(scrollKey,descriptionArea,offset,new Rect(0,0,textWidth,Mathf.Max(descriptionArea.height,textHeight)));
                            Text(new Rect(0,0,textWidth,textHeight),description,Mathf.RoundToInt(12*u),pale,false,true);
                            EndTouchScroll();
                            Text(new Rect(option.x+10*u,option.yMax-28*u,option.width-20*u,24*u),selected?"当前形态":mounted.variantUnlocked?"点击切换":"3阶首次升华解锁",Mathf.RoundToInt(12*u),selected?jade:gold,true);
                            if(QuietAction(option,"",!selected&&mounted.variantUnlocked&&SmithServiceActive)){SelectSmithVariant(mounted.mechanic,variant);}
                        }
                        y+=(variantHeight+8)*(stackedVariants?2:1);
                    }
                }

            }
            return y+8;
        }
        private void DrawAffixConfirmation(float u)
        {
            var quote=smithAffixQuote;if(quote==null)return;var p=session.Progression;
            float w=Mathf.Min(440*u,width-24*u),h=210*u;Rect box=new Rect((width-w)*.5f,(height-h)*.5f,w,h);
            blockedRects.Add(box);Fill(box,ink);Border(box,gold);
            Text(new Rect(box.x+16*u,box.y+12*u,w-76*u,32*u),"确认词条重铸",Mathf.RoundToInt(17*u),gold,true);
            if(PopupCloseButton(new Rect(box.xMax-48*u,box.y+8*u,44*u,36*u))){smithAffixQuote=null;return;}
            bool fresh=quote.Owner==p&&quote.Slot==p.CurrentSlotId&&quote.Fingerprint==p.BuildStateFingerprint();
            Text(new Rect(box.x+16*u,box.y+60*u,w-32*u,80*u),fresh?"消耗词条重铸石 ×1\n词条数值可能降低或者词条消失":"装备或材料已变化，请取消后重新确认。",Mathf.RoundToInt(15*u),pale,false,true);
            if(Button(new Rect(box.x+16*u,box.yMax-54*u,(w-40*u)*.5f,44*u),"取消",muted)){smithAffixQuote=null;return;}
            if(Button(new Rect(box.center.x+4*u,box.yMax-54*u,(w-40*u)*.5f,44*u),"确认重铸",gold,fresh&&p.AffixReforgeLockReason(quote.Id,SmithServiceActive).Length==0))
            {bool saved=p.ApplyAffixReforge(quote,SmithServiceActive);Feedback(saved,"词条已重铸");if(saved)smithAffixQuote=null;}
        }

    }
}
