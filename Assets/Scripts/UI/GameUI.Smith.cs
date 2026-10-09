using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private int smithCategory,smithSelectedSlot;
        private bool smithSocketPicker;
        private EquipmentMechanic smithVariantMechanic;
        private int smithVariantChoice;
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
            string reason=smithPreviewAscend?gem.rarity==Rarity.Legendary?"已升华":gem.rarity!=Rarity.Epic?"需先升阶至史诗品质":p.HighestAdventureTier<5?"需通关 Lv50 副本":!SmithServiceActive?"请在铁匠处操作":p.Profile.mechanicMaterials<cost?"星烬碎片不足":"":p.AttachmentUpgradeLock(gem.mechanic,SmithServiceActive);
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
            bool prior=GUI.enabled;GUI.enabled=prior&&smithPreviewMechanic==EquipmentMechanic.None&&!smithSocketPicker&&smithVariantMechanic==EquipmentMechanic.None;
            var l=new SmithServiceLayout(width/u,height/u,MobileControls.IsIPad);
            Fill(new Rect(0,0,width,height),new Color(.018f,.031f,.048f,.985f));blockedRects.Add(new Rect(0,0,width,height));
            Text(BuildPlanRect(l.Header,u),"铁匠",Mathf.RoundToInt(22*u),gold,true);
            DrawServiceBalances(BuildPlanRect(l.Balance,u),u);
            if(PopupCloseButton(BuildPlanRect(l.Close,u))){smithPreviewMechanic=EquipmentMechanic.None;GUI.enabled=prior;ClosePanel();return;}
            smithCategory=Mathf.Clamp(smithCategory,0,1);
            string[] categories={"强化","镶嵌"};
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
            GUI.enabled=prior;DrawGemPreview(u);DrawSocketPicker(u);DrawVariantChoice(u);
        }
        private void DrawGemUpgradeComparison(ref float y,float width,float u,MechanicAttachment gem,bool ascend,bool draw)
        {
            int nextRank=ascend?gem.upgradeRank:Mathf.Min(ProgressionService.MaximumAttachmentRank,gem.upgradeRank+1);
            Rarity nextRarity=ascend?Rarity.Legendary:nextRank>=3&&gem.rarity<Rarity.Epic?Rarity.Epic:gem.rarity;
            bool capped=ascend?gem.rarity==Rarity.Legendary:gem.upgradeRank>=ProgressionService.MaximumAttachmentRank;
            float currentPower=(.7f+.15f*(int)gem.rarity)+.08f*gem.upgradeRank+(gem.upgradeRank>=5?.2f:0);
            float nextPower=(.7f+.15f*(int)nextRarity)+.08f*nextRank+(nextRank>=5?.2f:0);
            string[] labels={"阶数","品质","机制强度","作用范围","攻击加成","生命加成"};
            string[] before={"+"+gem.upgradeRank,GameBalance.RarityName(gem.rarity),currentPower.ToString("0.00")+"×",gem.upgradeRank>=3?"1.20×":"1.00×","+"+(gem.upgradeRank*1.5f).ToString("0.#")+"%","+"+(gem.upgradeRank*2)+"%"};
            string[] after={"+"+nextRank,GameBalance.RarityName(nextRarity),nextPower.ToString("0.00")+"×",nextRank>=3?"1.20×":"1.00×","+"+(nextRank*1.5f).ToString("0.#")+"%","+"+(nextRank*2)+"%"};
            if(BuildCatalog.IsAttributeGem(gem.mechanic))
            {
                labels=new[]{"阶数","品质",BuildCatalog.GemAttributeLabel(gem.mechanic)};
                before=new[]{"+"+gem.upgradeRank,GameBalance.RarityName(gem.rarity),(BuildCatalog.GemAttributeValue(gem.mechanic,gem.rarity,gem.upgradeRank)*100).ToString("0.#")+"%"};
                after=new[]{"+"+nextRank,GameBalance.RarityName(nextRarity),(BuildCatalog.GemAttributeValue(gem.mechanic,nextRarity,nextRank)*100).ToString("0.#")+"%"};
            }
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
        private string SmithVariantDescription(EquipmentMechanic mechanic,int variant)
        {
            string description=BuildCatalog.MechanicDescription(mechanic);
            int split=description.IndexOf("变体B：",System.StringComparison.Ordinal);
            if(split>=0)description=variant==0?description.Substring(0,split):description.Substring(split);
            return description.Replace("变体A：","").Replace("变体B：","").Trim();
        }
        private void DrawVariantChoice(float u)
        {
            if(smithVariantMechanic==EquipmentMechanic.None)return;
            var p=session.Progression;var gem=p.Attachment(smithVariantMechanic);
            if(gem==null){smithVariantMechanic=EquipmentMechanic.None;return;}
            Fill(new Rect(0,0,width,height),new Color(0,0,0,.6f));blockedRects.Add(new Rect(0,0,width,height));
            float w=Mathf.Min(680*u,width-24*u),column=(w-44*u)/2;
            string[] descriptions={SmithVariantDescription(gem.mechanic,0),SmithVariantDescription(gem.mechanic,1)};
            int font=Mathf.RoundToInt(13*u);
            float textHeight=Mathf.Max(Style(font,false,true).CalcHeight(new GUIContent(descriptions[0]),column-24*u),Style(font,false,true).CalcHeight(new GUIContent(descriptions[1]),column-24*u));
            float h=Mathf.Min(Mathf.Max(280*u,textHeight+174*u),height-24*u);
            while(textHeight>h-174*u&&font>Mathf.RoundToInt(11*u))
            {font--;textHeight=Mathf.Max(Style(font,false,true).CalcHeight(new GUIContent(descriptions[0]),column-24*u),Style(font,false,true).CalcHeight(new GUIContent(descriptions[1]),column-24*u));}
            Rect box=new Rect((width-w)/2,(height-h)/2,w,h);Fill(box,card);Border(box,jade);
            Text(new Rect(box.x+14*u,box.y+12*u,w-70*u,28*u),"机制形态对比",Mathf.RoundToInt(17*u),gold,true);
            if(PopupCloseButton(new Rect(box.xMax-48*u,box.y+6*u,44*u,36*u))){smithVariantMechanic=EquipmentMechanic.None;return;}
            for(int variant=0;variant<2;variant++)
            {
                Rect option=new Rect(box.x+16*u+variant*(column+12*u),box.y+52*u,column,h-120*u);
                Fill(option,new Color(.035f,.065f,.085f));Border(option,smithVariantChoice==variant?jade:muted,smithVariantChoice==variant?2*u:u);
                Text(new Rect(option.x+12*u,option.y+8*u,column-24*u,28*u),(variant==0?"形态 A":"形态 B")+(gem.variant==variant?" · 当前":""),Mathf.RoundToInt(14*u),gem.variant==variant?gold:pale,true);
                Text(new Rect(option.x+12*u,option.y+42*u,column-24*u,option.height-50*u),descriptions[variant],font,pale,false,true);
                if(QuietAction(option,"",true))smithVariantChoice=variant;
            }
            bool canSwitch=smithVariantChoice!=gem.variant&&SmithServiceActive&&(gem.variantUnlocked||p.Profile.mechanicMaterials>=ProgressionService.VariantCost);
            string action=smithVariantChoice==gem.variant?"当前形态":gem.variantUnlocked?"选择形态 "+(smithVariantChoice==0?"A":"B"):"解锁并选择 · "+ProgressionService.VariantCost+" 碎片";
            if(PrimaryButton(new Rect(box.x+16*u,box.yMax-56*u,w-32*u,40*u),action,jade,canSwitch))
            {if(p.ToggleAttachmentVariant(gem.mechanic,SmithServiceActive)){Feedback(true,"宝石形态已切换");smithVariantMechanic=EquipmentMechanic.None;}else Feedback(false,p.LastError);}
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
            if(smithCategory==0)
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
                    Rect socket=new Rect(socketX*u,(y+(mounted==null?0:30))*u,size*u,size*u);
                    Fill(socket,card);Border(socket,mounted==null?jade:GameBalance.RarityColor(mounted.rarity),2);
                    if(mounted==null)Text(socket,"+",Mathf.RoundToInt(32*u),jade,false,false,TextAnchor.MiddleCenter);
                    else DrawIcon(new Rect(socket.x+8*u,socket.y+8*u,48*u,48*u),UIIconAtlas.Utility("gem"),GameBalance.RarityColor(mounted.rarity));
                    if(QuietAction(socket,"",SmithServiceActive)){smithSelectedSlot=(int)item.slot;smithSocketPicker=true;smithSocketScroll=Vector2.zero;}
                    if(mounted!=null)
                    {
                        float actionX=80,actionWidth=width-actionX-8;
                        if(Button(new Rect(actionX*u,y*u,actionWidth*u,44*u),"升阶",jade))OpenGemPreview(mounted.mechanic,false);
                        if(Button(new Rect(actionX*u,(y+48)*u,actionWidth*u,44*u),"升华",gold))OpenGemPreview(mounted.mechanic,true);
                        if(Button(new Rect(actionX*u,(y+96)*u,actionWidth*u,44*u),"卸下",muted,SmithServiceActive))
                        {bool saved=p.SetAttachmentMounted(mounted.mechanic,false,SmithServiceActive);Feedback(saved,saved?"宝石已卸下":p.LastError);if(saved)return y+148;}
                    }
                }
                y+=mounted==null?76:148;
                if(mounted!=null)
                {
                    if(BuildCatalog.HasMechanicVariant(mounted.mechanic))
                    {
                        GoalParagraph(ref y,width,u,"机制形态",13,muted,false,draw);
                        bool stackedVariants=width<420;
                        float variantWidth=stackedVariants?width-16:width/2-16;
                        float variantTextHeight=Mathf.Max(Style(Mathf.RoundToInt(12*u),false,true).CalcHeight(new GUIContent(SmithVariantDescription(mounted.mechanic,0)),(variantWidth-20)*u),Style(Mathf.RoundToInt(12*u),false,true).CalcHeight(new GUIContent(SmithVariantDescription(mounted.mechanic,1)),(variantWidth-20)*u))/u;
                        float variantHeight=64+variantTextHeight;
                        if(draw)for(int variant=0;variant<2;variant++)
                        {
                            Rect option=new Rect((stackedVariants?8:8+variant*(width/2))*u,(y+(stackedVariants?variant*(variantHeight+8):0))*u,variantWidth*u,variantHeight*u);
                            bool selected=mounted.variant==variant;
                            Fill(option,card);Border(option,selected?jade:muted,selected?2:1);
                            DrawIcon(new Rect(option.x+10*u,option.y+12*u,36*u,36*u),UIIconAtlas.Utility(variant==0?"core":"attack"),selected?jade:pale);
                            Text(new Rect(option.x+50*u,option.y,option.width-54*u,56*u),variant==0?"形态 A":"形态 B",Mathf.RoundToInt(12*u),pale,true,false,TextAnchor.MiddleLeft);
                            DrawIcon(new Rect(option.xMax-18*u,option.y,18*u,18*u),UIIconAtlas.Utility(selected?"confirm":mounted.variantUnlocked?"help":"lock"),selected?jade:gold);
                            Text(new Rect(option.x+10*u,option.y+56*u,option.width-20*u,variantTextHeight*u),SmithVariantDescription(mounted.mechanic,variant),Mathf.RoundToInt(12*u),pale,false,true);
                            if(QuietAction(option,"",true)){smithVariantMechanic=mounted.mechanic;smithVariantChoice=variant;}
                        }
                        y+=(variantHeight+8)*(stackedVariants?2:1);
                    }
                }

            }
            return y+8;
        }
    }
}
