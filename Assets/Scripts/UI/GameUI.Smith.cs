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
            Fill(new Rect(0,0,width,height),new Color(.018f,.031f,.048f,.985f));blockedRects.Add(new Rect(0,0,width,height));
            Text(BuildPlanRect(l.Header,u),"铁匠",Mathf.RoundToInt(22*u),gold,true);
            DrawServiceBalances(BuildPlanRect(l.Balance,u),u);
            if(PopupCloseButton(BuildPlanRect(l.Close,u))){smithPreviewMechanic=EquipmentMechanic.None;GUI.enabled=prior;ClosePanel();return;}
            smithCategory=Mathf.Clamp(smithCategory,0,2);
            string[] categories={"强化","镶嵌","洗练"};
            for(int i=0;i<categories.Length;i++)
                if(TabButton(new Rect((16+i*116)*u,58*u,108*u,40*u),categories[i],smithCategory==i)&&smithCategory!=i){smithCategory=i;smithDetailScroll=Vector2.zero;}
            Rect body=new Rect(16*u,110*u,width-32*u,height-122*u);
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
                labels=new[]{"阶数","升华",BuildCatalog.GemAttributeLabel(gem.mechanic),BuildCatalog.GemAscensionLabel(gem.mechanic)};
                before=new[]{gem.upgradeRank+" / 9",ascension+" / 3",(BuildCatalog.GemAttributeValue(gem.mechanic,gem.rarity,gem.upgradeRank)*100).ToString("0.#")+"%",(BuildCatalog.GemAscensionValue(gem.mechanic,ascension)*100).ToString("0.#")+"%"};
                after=new[]{nextRank+" / 9",nextAscension+" / 3",(BuildCatalog.GemAttributeValue(gem.mechanic,nextRarity,nextRank)*100).ToString("0.#")+"%",(BuildCatalog.GemAscensionValue(gem.mechanic,nextAscension)*100).ToString("0.#")+"%"};
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
            if(mechanic==EquipmentMechanic.CinderTrail)return variant==0?"燎原余烬":"凝焰火核";
            if(mechanic==EquipmentMechanic.FrostEcho)return variant==0?"凝霜回响":"扩散霜环";
            return variant==0?"形态 A":"形态 B";
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
            string description=BuildCatalog.MechanicDescription(mechanic);
            int split=description.IndexOf("变体B：",System.StringComparison.Ordinal);
            if(split>=0)description=variant==0?description.Substring(0,split):description.Substring(split);
            return description.Replace("变体A：","").Replace("变体B：","").Trim();
        }

        private void DrawSocketPicker(float u)
        {
            if(!smithSocketPicker)return;
            var p=session.Progression;var slot=(ItemSlot)smithSelectedSlot;
            Fill(new Rect(0,0,width,height),new Color(0,0,0,.6f));blockedRects.Add(new Rect(0,0,width,height));
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
            smithSocketScroll=BeginTouchScroll("smith-socket-picker",body,smithSocketScroll,new Rect(0,0,body.width,Mathf.Max(body.height,((candidates.Count+1)/2)*132*u)));
            for(int i=0;i<candidates.Count;i++)
            {
                var a=candidates[i];Rect tile=new Rect((i%2)*(cw+10)*u,(i/2)*132*u,cw*u,122*u);
                Fill(tile,new Color(.06f,.09f,.13f));Border(tile,a.mounted?jade:GameBalance.RarityColor(a.rarity));
                DrawIcon(new Rect(tile.center.x-24*u,tile.y+12*u,48*u,48*u),UIIconAtlas.Utility("gem"),GameBalance.RarityColor(a.rarity));
                Text(new Rect(tile.x+6*u,tile.y+65*u,tile.width-12*u,28*u),BuildCatalog.GemName(a.mechanic),Mathf.RoundToInt(13*u),pale,true,false,TextAnchor.MiddleCenter);
                Text(new Rect(tile.x,tile.y+96*u,tile.width,20*u),BuildCatalog.IsAttributeGem(a.mechanic)?BuildCatalog.GemAttributeSummary(a.mechanic,a.rarity,a.upgradeRank):a.mounted?"已镶嵌":"阶数 "+a.upgradeRank,Mathf.RoundToInt(11*u),a.mounted?jade:muted,false,false,TextAnchor.MiddleCenter);
                if(QuietAction(tile,"",SmithServiceActive&&!a.mounted))
                {if(p.SetAttachmentMounted(a.mechanic,true,SmithServiceActive)){Feedback(true,"宝石已镶嵌");smithSocketPicker=false;}else Feedback(false,p.LastError);}
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
                GoalParagraph(ref y,width,u,"装备洗练石 × "+p.Profile.refinementStones+" · 每次消耗1",13,gold,true,draw);
                GoalParagraph(ref y,width,u,"只提升已有属性，上限按装备等级和品质计算。主要产地：赤岩断供。",12,muted,false,draw);
                string[] labels={"攻击","防御","生命","暴击率","暴击伤害"};
                string[] current={item.attack.ToString(),item.defense.ToString(),item.health.ToString(),(item.criticalChance*100).ToString("0.##")+"%",(item.criticalDamageBonus*100).ToString("0.##")+"%"};
                string[] limits={cap.attack.ToString(),cap.defense.ToString(),cap.health.ToString(),(cap.criticalChance*100).ToString("0.##")+"%",(cap.criticalDamageBonus*100).ToString("0.##")+"%"};
                for(int stat=0;stat<5;stat++)
                {
                    bool present=stat==0?item.attack>0:stat==1?item.defense>0:stat==2?item.health>0:stat==3?item.criticalChance>0:item.criticalDamageBonus>0;if(!present)continue;
                    if(draw){Rect row=new Rect(8*u,y*u,(width-16)*u,34*u);Fill(row,card);Text(new Rect(row.x+6*u,row.y,row.width*.32f,row.height),labels[stat],Mathf.RoundToInt(12*u),muted);Text(new Rect(row.x+row.width*.34f,row.y,row.width*.66f,row.height),current[stat]+" / "+limits[stat],Mathf.RoundToInt(13*u),jade,true,false,TextAnchor.MiddleCenter);}y+=38;
                }
                GoalParagraph(ref y,width,u,reason.Length==0?"洗练结果立即保存，可连续操作":reason,12,muted,false,draw);
                if(draw&&Button(new Rect(8*u,y*u,Mathf.Min(180,width-16)*u,44*u),reason=="数值已满"?"数值已满":"洗练 · 1枚洗练石",gold,reason.Length==0))Feedback(p.RefineEquipment(item.id,SmithServiceActive),"洗练已保存");y+=54;
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
                    float size=64,socketX=mounted==null?(width-size)*.5f:8;
                    Rect socket=new Rect(socketX*u,(y)*u,size*u,size*u);
                    Fill(socket,card);Border(socket,mounted==null?jade:GameBalance.RarityColor(mounted.rarity),2);
                    if(mounted==null)Text(socket,"+",Mathf.RoundToInt(32*u),jade,false,false,TextAnchor.MiddleCenter);
                    else DrawIcon(new Rect(socket.x+8*u,socket.y+8*u,48*u,48*u),UIIconAtlas.Utility("gem"),GameBalance.RarityColor(mounted.rarity));
                    if(QuietAction(socket,"",SmithServiceActive)){smithSelectedSlot=(int)item.slot;smithSocketPicker=true;smithSocketScroll=Vector2.zero;}
                    if(mounted!=null)
                    {
                        float actionX=80,actionHeight=MobileControls.Active?44:30;
                        int columns=MobileControls.Active&&width<380?2:3;
                        float actionWidth=Mathf.Min(MobileControls.Active?112:96,(width-actionX-20)/columns);
                        for(int action=0;action<3;action++)
                        {
                            Rect button=new Rect((actionX+(action%columns)*(actionWidth+6))*u,(y+(action/columns)*(actionHeight+6))*u,actionWidth*u,actionHeight*u);
                            bool capped=action==0?mounted.upgradeRank>=ProgressionService.MaximumAttachmentRank:action==1&&mounted.ascensionRank>=3;
                            string caption=action==0?(capped?"已满阶":"升阶"):action==1?(capped?"已升华":"升华"):"卸下";
                            if(Button(button,caption,action==1?gold:action==2?muted:jade,SmithServiceActive&&!capped))
                            {
                                if(action<2)OpenGemPreview(mounted.mechanic,action==1);
                                else {bool saved=p.SetAttachmentMounted(mounted.mechanic,false,SmithServiceActive);Feedback(saved,saved?"宝石已卸下":p.LastError);if(saved)return y+110;}
                            }
                        }
                    }
                }
                y+=mounted==null?76:MobileControls.Active&&width<380?110:80;
                if(mounted!=null)
                {
                    if(BuildCatalog.HasMechanicVariant(mounted.mechanic))
                    {
                        GoalParagraph(ref y,width,u,"机制形态",13,muted,false,draw);
                        bool stackedVariants=width<420;
                        float variantWidth=stackedVariants?width-16:width/2-16;
                        float variantTextHeight=Mathf.Max(Style(Mathf.RoundToInt(12*u),false,true).CalcHeight(new GUIContent(SmithVariantDescription(mounted.mechanic,0)),(variantWidth-20)*u),Style(Mathf.RoundToInt(12*u),false,true).CalcHeight(new GUIContent(SmithVariantDescription(mounted.mechanic,1)),(variantWidth-20)*u))/u;
                        float variantHeight=92+variantTextHeight;
                        if(draw)for(int variant=0;variant<2;variant++)
                        {
                            Rect option=new Rect((stackedVariants?8:8+variant*(width/2))*u,(y+(stackedVariants?variant*(variantHeight+8):0))*u,variantWidth*u,variantHeight*u);
                            bool selected=mounted.variant==variant;
                            Fill(option,card);Border(option,selected?jade:muted,selected?2:1);
                            DrawIcon(new Rect(option.x+10*u,option.y+12*u,36*u,36*u),UIIconAtlas.Utility(variant==0?"core":"attack"),selected?jade:pale);
                            Text(new Rect(option.x+50*u,option.y,option.width-54*u,56*u),SmithVariantName(mounted.mechanic,variant),Mathf.RoundToInt(12*u),pale,true,false,TextAnchor.MiddleLeft);
                            DrawIcon(new Rect(option.xMax-18*u,option.y,18*u,18*u),UIIconAtlas.Utility(selected?"confirm":mounted.variantUnlocked?"help":"lock"),selected?jade:gold);
                            Text(new Rect(option.x+10*u,option.y+56*u,option.width-20*u,variantTextHeight*u),SmithVariantDescription(mounted.mechanic,variant),Mathf.RoundToInt(12*u),pale,false,true);
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
