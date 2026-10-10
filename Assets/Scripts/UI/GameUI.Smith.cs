using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private int smithCategory,smithSelectedSlot;
        private bool smithSocketPicker;
        private Vector2 smithSocketScroll;
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
        private void DrawSmithService()
        {
            var p=session.Progression;float u=MobileControls.Active?TouchRatio:1;
            bool prior=GUI.enabled;GUI.enabled=prior&&smithPreviewMechanic==EquipmentMechanic.None&&!smithSocketPicker;
            var l=new SmithServiceLayout(width/u,height/u,MobileControls.IsIPad);
            blockedRects.Add(new Rect(0,0,width,height));
            Box(new Rect(8*u,4*u,width-16*u,height-8*u),jade,false);
            Text(new Rect(16*u,10*u,width-360*u,36*u),"铁匠",Mathf.RoundToInt(22*u),gold,true);
            DrawServiceBalances(new Rect(width-296*u,12*u,224*u,28*u),u);
            if(PopupCloseButton(new Rect(width-56*u,10*u,42*u,36*u))){smithPreviewMechanic=EquipmentMechanic.None;GUI.enabled=prior;ClosePanel();return;}
            smithCategory=Mathf.Clamp(smithCategory,0,2);
            string[] categories={"强化","镶嵌","洗练"};
            for(int i=0;i<categories.Length;i++)
                if(TabButton(new Rect((16+i*116)*u,58*u,108*u,40*u),categories[i],smithCategory==i)&&smithCategory!=i){smithCategory=i;smithDetailScroll=Vector2.zero;}
            Rect body=new Rect(16*u,110*u,width-32*u,height-122*u);
            if(MobileControls.Active&&!MobileControls.IsIPad&&smithCategory==1)
            {
                DrawPhoneSocketService(body,u);
                GUI.enabled=prior;DrawGemPreview(u);DrawSocketPicker(u);return;
            }
            int columns=width/u>=540?3:1;
            float cardWidth=(body.width/u-(columns-1)*12)/columns;
            float[] cardHeights=new float[3];float rowHeight=0;
            for(int i=0;i<3;i++)
            {
                var gear=p.Equipped((ItemSlot)i);
                cardHeights[i]=gear==null?180:DrawSmithDetail(gear,cardWidth-16,u,false)+(smithCategory==0?62:0)+16;
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
                        string reason=p.RefinementLockReason(item.id,SmithServiceActive);
                        float aw=Mathf.Min(180,cardWidth-32);
                        Rect action=new Rect((cardWidth-16-aw)*.5f*u,tile.height-68*u,aw*u,44*u);
                        if(Button(action,"",gold,reason.Length==0))Feedback(p.RefineEquipment(item.id,SmithServiceActive),"洗练已保存");
                        Text(new Rect(action.x+8*u,action.y,action.width*.5f,action.height),reason=="数值已满"?"数值已满":"洗练",Mathf.RoundToInt(13*u),reason.Length==0?gold:muted,true,false,TextAnchor.MiddleLeft);
                        DrawIcon(new Rect(action.xMax-66*u,action.y+12*u,20*u,20*u),UIIconAtlas.Utility("gem"),p.Profile.refinementStones>0?gold:muted);
                        Text(new Rect(action.xMax-42*u,action.y,38*u,action.height),"×1",Mathf.RoundToInt(12*u),p.Profile.refinementStones>0?gold:new Color(1,.3f,.25f),true,false,TextAnchor.MiddleLeft);
                    }
                    if(smithCategory==0)
                    {
                        var quote=p.PrepareSmithUpgrade(slot,SmithServiceActive);
                        int cost=p.UpgradeCost(item);bool capped=p.SlotUpgradeRank(slot)>=p.CurrentUpgradeLimit;
                        float actionWidth=Mathf.Min(148,cardWidth-32);
                        Rect action=new Rect((cardWidth-16-actionWidth)*.5f*u,y*u,actionWidth*u,44*u);
                        Color accent=quote!=null?gold:muted;
                        Fill(action,new Color(accent.r,accent.g,accent.b,.13f));Border(action,new Color(accent.r,accent.g,accent.b,.55f));
                        if(QuietAction(action,"",quote!=null))Feedback(p.UpgradeAtSmith(quote,SmithServiceActive),"部位强化已保存，换装自动继承");
                        DrawIcon(new Rect(action.x+10*u,action.y+11*u,22*u,22*u),UIIconAtlas.Utility(capped?"confirm":"upgrade"),accent);
                        if(!capped)
                        {
                            DrawIcon(new Rect(action.x+42*u,action.y+13*u,18*u,18*u),UIIconAtlas.Utility("coin"),gold);
                            Text(new Rect(action.x+66*u,action.y,action.width-72*u,44*u),cost.ToString(),Mathf.RoundToInt(12*u),p.Profile.gold>=cost?accent:new Color(.98f,.28f,.24f),true,false,TextAnchor.MiddleLeft);
                        }
                    }
                }
                GUI.EndGroup();top+=cardHeights[i]+12;
            }
            EndTouchScroll();
            GUI.enabled=prior;DrawGemPreview(u);DrawSocketPicker(u);
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
            float w=detail.width/u,h=detail.height/u,left=Mathf.Min(166,w*.37f),right=w-left-16;
            Text(new Rect(8*u,2*u,(w-16)*u,22*u),selected.name,Mathf.RoundToInt(12*u),gold,true);
            MechanicAttachment gem=null;
            foreach(var mechanic in BuildCatalog.GemsFor(p.Profile.heroClass)){var value=p.Attachment(mechanic);if(value!=null&&value.mounted&&BuildCatalog.MechanicSlot(mechanic)==selected.slot){gem=value;break;}}
            float size=Mathf.Min(54,Mathf.Max(36,h*.25f));Rect socket=new Rect((left-size)*.5f*u,28*u,size*u,size*u);
            Fill(socket,card);Border(socket,gem==null?jade:GameBalance.RarityColor(gem.rarity),2);
            if(gem==null)Text(socket,"+",Mathf.RoundToInt(25*u),jade,false,false,TextAnchor.MiddleCenter);
            else DrawIcon(socket,UIIconAtlas.Utility("gem"),GameBalance.RarityColor(gem.rarity));
            if(QuietAction(socket,"",SmithServiceActive)){smithSocketPicker=true;smithSocketScroll=Vector2.zero;}
            if(gem!=null)
            {
                float actionY=30+size+20,buttonWidth=(left-18)*.5f;
                Text(new Rect(0,(28+size)*u,left*u,20*u),gem.upgradeRank+"阶 · 升华 "+gem.ascensionRank,Mathf.RoundToInt(11*u),gold,true,false,TextAnchor.MiddleCenter);
                for(int action=0;action<2;action++)
                {
                    bool ascend=action==0;string reason=ascend?p.AttachmentAscensionLock(gem.mechanic,SmithServiceActive):p.AttachmentUpgradeLock(gem.mechanic,SmithServiceActive);
                    if(Button(new Rect((6+action*(buttonWidth+6))*u,actionY*u,buttonWidth*u,36*u),ascend?(gem.ascensionRank>=3?"已满":"升华"):(gem.upgradeRank>=9?"已满阶":"升阶"),ascend?gold:jade,reason.Length==0))OpenGemPreview(gem.mechanic,ascend);
                }
                string lockHint=gem.ascensionRank>=3?"升华已满":gem.upgradeRank<(gem.ascensionRank+1)*3?"升至"+((gem.ascensionRank+1)*3)+"阶可升华":p.Profile.mechanicMaterials<ProgressionService.AscensionCost?"需24碎片":"";
                Text(new Rect(4*u,(actionY+38)*u,(left-8)*u,20*u),lockHint,Mathf.RoundToInt(10*u),muted,false,false,TextAnchor.MiddleCenter);
                string attribute=BuildCatalog.IsAttributeGem(gem.mechanic)?BuildCatalog.GemAttributeSummary(gem.mechanic,gem.rarity,gem.upgradeRank):BuildCatalog.AttributeLabel(BuildCatalog.MechanicAttribute(gem.mechanic))+" +"+(BuildCatalog.MechanicAttributeValue(gem.mechanic,gem.upgradeRank)*100).ToString("0.#")+"%";
                Text(new Rect(4*u,(h-24)*u,(left-8)*u,22*u),attribute,Mathf.RoundToInt(10*u),jade,true,false,TextAnchor.MiddleCenter);
                float cardHeight=(h-38)*.5f;
                for(int variant=0;variant<2;variant++)
                {
                    Rect option=new Rect((left+8)*u,(28+variant*(cardHeight+6))*u,right*u,cardHeight*u);
                    bool active=gem.variantUnlocked&&gem.variant==variant;Fill(option,active?new Color(.055f,.19f,.18f):card);Border(option,active?gold:muted);
                    Text(new Rect(option.x+7*u,option.y+3*u,option.width-14*u,20*u),SmithVariantName(gem.mechanic,variant)+(active?" · 当前":gem.variantUnlocked?"":" · 升华解锁"),Mathf.RoundToInt(11*u),active?gold:muted,true);
                    Text(new Rect(option.x+7*u,option.y+25*u,option.width-14*u,option.height-28*u),PhoneGemFormDescription(gem,variant),Mathf.RoundToInt(10*u),pale,false,true);
                    if(QuietAction(option,"",!active&&gem.variantUnlocked&&SmithServiceActive))SelectSmithVariant(gem.mechanic,variant);
                }
            }
            else Text(new Rect((left+8)*u,28*u,right*u,(h-34)*u),"点击宝石槽选择宝石",Mathf.RoundToInt(13*u),muted,false,false,TextAnchor.MiddleCenter);
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
                DrawIcon(new Rect(tile.center.x-24*u,tile.y+12*u,48*u,48*u),UIIconAtlas.Utility("gem"),GameBalance.RarityColor(a.rarity));
                Text(new Rect(tile.x+6*u,tile.y+65*u,tile.width-12*u,28*u),BuildCatalog.GemName(a.mechanic),Mathf.RoundToInt(13*u),pale,true,false,TextAnchor.MiddleCenter);
                Text(new Rect(tile.x,tile.y+94*u,tile.width,30*u),BuildCatalog.IsAttributeGem(a.mechanic)?BuildCatalog.GemAttributeSummary(a.mechanic,a.rarity,a.upgradeRank):a.mounted?"已镶嵌":"阶数 "+a.upgradeRank,Mathf.RoundToInt(10*u),a.mounted?jade:muted,false,true,TextAnchor.MiddleCenter);
                if(Button(new Rect(tile.x+8*u,tile.y+124*u,tile.width-16*u,36*u),a.mounted?"卸下":"镶嵌",jade,SmithServiceActive))
                {if(p.SetAttachmentMounted(a.mechanic,!a.mounted,SmithServiceActive)){Feedback(true,"宝石已更新");smithSocketPicker=false;}else Feedback(false,p.LastError);}
            }
            EndTouchScroll();
        }
        private float DrawSmithDetail(ItemData item,float width,float u,bool draw)
        {
            var p=session.Progression;float y=8;
            if(smithCategory==0){if(draw)DrawIcon(new Rect((width-64)*.5f*u,y*u,64*u,64*u),UIIconAtlas.EquipmentCardIcon(item.slot,item.level,item.rarity,session.Progression.Profile.heroClass),GameBalance.RarityColor(item.rarity));y+=72;}
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
                for(int stat=0;stat<6;stat++)
                {
                    bool present=stat==0?item.attack>0:stat==1?item.defense>0:stat==2?item.health>0:stat==3?item.criticalChance>0:stat==4?item.criticalDamageBonus>0:item.attackPercent>0;if(!present)continue;
                    if(draw){Rect row=new Rect(8*u,y*u,(width-16)*u,34*u);Fill(row,card);Text(new Rect(row.x+6*u,row.y,row.width*.32f,row.height),labels[stat],Mathf.RoundToInt(12*u),muted);Text(new Rect(row.x+row.width*.34f,row.y,row.width*.66f,row.height),current[stat]+" / "+limits[stat],Mathf.RoundToInt(13*u),jade,true,false,TextAnchor.MiddleCenter);}y+=38;
                }

                y+=54;
            }
            else if(smithCategory==0)
            {
                int rank=p.SlotUpgradeRank(item.slot);var next=p.PreviewUpgrade(item,Mathf.Max(rank,Mathf.Min(rank+1,p.CurrentUpgradeLimit)));
                if(draw)
                {
                    int[] before={item.attack,item.defense,item.health},after={next.attack,next.defense,next.health};
                    for(int stat=0;stat<3;stat++)
                    {
                        Rect row=new Rect(8*u,(y+stat*38)*u,(width-16)*u,32*u);
                        Fill(row,new Color(.025f,.045f,.065f,.8f));
                        Texture2D icon=stat==1?UIIconAtlas.EquipmentCardIcon(ItemSlot.Armor,item.level,item.rarity,p.Profile.heroClass):UIIconAtlas.Utility(stat==0?"attack":"potion");
                        DrawIcon(new Rect(row.x+4*u,row.y+4*u,24*u,24*u),icon,pale);
                        float col=(row.width-32*u)/3;
                        Text(new Rect(row.x+32*u,row.y,col,row.height),before[stat].ToString(),Mathf.RoundToInt(12*u),pale,true,false,TextAnchor.MiddleCenter);
                        Text(new Rect(row.x+32*u+col,row.y,col,row.height),"→",Mathf.RoundToInt(14*u),muted,false,false,TextAnchor.MiddleCenter);
                        Text(new Rect(row.x+32*u+col*2,row.y,col,row.height),after[stat].ToString(),Mathf.RoundToInt(12*u),after[stat]>before[stat]?jade:muted,true,false,TextAnchor.MiddleCenter);
                    }
                }
                y+=114;

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
                    else DrawIcon(new Rect(socket.x+8*u,socket.y+8*u,48*u,48*u),UIIconAtlas.Utility("gem"),GameBalance.RarityColor(mounted.rarity));
                    if(QuietAction(socket,"",SmithServiceActive)){smithSelectedSlot=(int)item.slot;smithSocketPicker=true;smithSocketScroll=Vector2.zero;}
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
                if(mounted!=null)GoalParagraph(ref y,width,u,BuildCatalog.IsAttributeGem(mounted.mechanic)?BuildCatalog.GemAttributeSummary(mounted.mechanic,mounted.rarity,mounted.upgradeRank):BuildCatalog.AttributeLabel(BuildCatalog.MechanicAttribute(mounted.mechanic))+" +"+(BuildCatalog.MechanicAttributeValue(mounted.mechanic,mounted.upgradeRank)*100).ToString("0.#")+"%",12,jade,true,draw);
                if(mounted!=null)
                {
                    if(BuildCatalog.HasMechanicVariant(mounted.mechanic))
                    {
                        GoalParagraph(ref y,width,u,"机制形态",13,muted,false,draw);
                        bool compact=MobileControls.Active&&!MobileControls.IsIPad;
                        bool stackedVariants=width<420&&!compact;
                        float variantWidth=stackedVariants?width-16:width/2-16;
                        float variantTextHeight=Mathf.Max(Style(Mathf.RoundToInt(12*u),false,true).CalcHeight(new GUIContent(SmithVariantDescription(mounted.mechanic,0)),(variantWidth-20)*u),Style(Mathf.RoundToInt(12*u),false,true).CalcHeight(new GUIContent(SmithVariantDescription(mounted.mechanic,1)),(variantWidth-20)*u))/u;
                        float variantHeight=(compact?72:92)+variantTextHeight;
                        if(draw)for(int variant=0;variant<2;variant++)
                        {
                            Rect option=new Rect((stackedVariants?8:8+variant*(width/2))*u,(y+(stackedVariants?variant*(variantHeight+8):0))*u,variantWidth*u,variantHeight*u);
                            bool selected=mounted.variantUnlocked&&mounted.variant==variant;
                            Fill(option,card);Border(option,selected?jade:muted,selected?2:1);
                            DrawIcon(new Rect(option.x+10*u,option.y+12*u,36*u,36*u),UIIconAtlas.Utility(variant==0?"core":"attack"),selected?jade:pale);
                            Text(new Rect(option.x+50*u,option.y,option.width-54*u,56*u),SmithVariantName(mounted.mechanic,variant),Mathf.RoundToInt(12*u),pale,true,false,TextAnchor.MiddleLeft);
                            DrawIcon(new Rect(option.xMax-18*u,option.y,18*u,18*u),UIIconAtlas.Utility(selected?"confirm":mounted.variantUnlocked?"help":"lock"),selected?jade:gold);
                            Text(new Rect(option.x+10*u,option.y+(compact?40:56)*u,option.width-20*u,variantTextHeight*u),SmithVariantDescription(mounted.mechanic,variant),Mathf.RoundToInt(12*u),pale,false,true);
                            Text(new Rect(option.x+10*u,option.yMax-28*u,option.width-20*u,24*u),selected?"当前形态":mounted.variantUnlocked?"点击切换":"3阶首次升华解锁",Mathf.RoundToInt(12*u),selected?jade:gold,true);
                            if(QuietAction(option,"",!selected&&mounted.variantUnlocked&&SmithServiceActive)){SelectSmithVariant(mounted.mechanic,variant);}
                        }
                        y+=(variantHeight+8)*(stackedVariants?2:1);
                    }
                }

            }
            return y+8;
        }
    }
}
