using UnityEngine;

namespace Emberfall
{
    public sealed partial class GameUI
    {
        private enum BuildPlanAction { None, Reset, Save, Apply, Replace }
        private bool buildPlansOpen,buildPlanChoosing;
        private ItemSlot buildPlanPart;
        private ProgressionService.PresetEquipmentQuote buildPlanReplacement;
        private string buildPlanFingerprint;
        private BuildPlanAction buildPlanAction;
        private int buildPlanSlot;
        private ProgressionService buildPlanOwner;
        private PlayerController buildPlanHero;
        private string buildPlanCharacterId;
        private GameProfile buildPlanSource;
        private string buildPlanPreview, buildPlanError;
        private Vector2 buildPlanScroll;
        private int buildPlanDetails=-1;
        private static string BuildPlanName(int slot){return slot==0?"A":"B";}

        private void OpenBuildPlans()
        {
            buildPlansOpen=true;buildPlanAction=BuildPlanAction.None;buildPlanOwner=session.Progression;
            buildPlanHero=session.Player;buildPlanCharacterId=session.Progression.CurrentSlotId;
            buildPlanError=null;buildPlanScroll=Vector2.zero;buildPlanDetails=-1;practiceChoicesOpen=false;
            CancelMobileScroll();BlockUITransition();
        }
        private void RequestBuildPlanAction(BuildPlanAction action,int slot=0)
        {
            var p=session.Progression;
            // Shortcuts may open a confirmation directly from the workshop.
            // Only external entry resets disclosures; internal confirmations preserve them.
            if(!buildPlansOpen){buildPlanDetails=-1;practiceChoicesOpen=false;}
            buildPlansOpen=true;buildPlanOwner=p;buildPlanSource=p.Profile;
            buildPlanHero=session.Player;buildPlanCharacterId=p.CurrentSlotId;
            buildPlanAction=action;buildPlanSlot=slot;buildPlanFingerprint=p.BuildStateFingerprint();
            buildPlanPreview=action==BuildPlanAction.Apply?p.BuildPresetSummary(slot):p.CurrentBuildSummary();
            buildPlanError=action==BuildPlanAction.Apply?p.BuildPresetLockReason(slot,session.IsInCamp):null;
            buildPlanScroll=Vector2.zero;
            CancelMobileScroll();BlockUITransition();
        }
        private bool CloseBuildPlanSurface()
        {
            if(!buildPlansOpen||panel!=Panel.Camp)return false;
            if(allocationDraft!=null)CancelAllocationDraft();
            else if(buildPlanChoosing){buildPlanChoosing=false;}
            else if(buildPlanAction!=BuildPlanAction.None)buildPlanAction=BuildPlanAction.None;
            else {buildPlansOpen=false;buildPlanDetails=-1;practiceChoicesOpen=false;}
            buildPlanError=null;buildPlanScroll=Vector2.zero;
            CancelMobileScroll();BlockUITransition();return true;
        }
        private void ResetBuildPlanSurface()
        {
            CancelAllocationDraft();buildPlanChoosing=false;buildPlanReplacement=null;
            buildPlansOpen=false;buildPlanAction=BuildPlanAction.None;buildPlanDetails=-1;practiceChoicesOpen=false;
            buildPlanOwner=null;buildPlanSource=null;buildPlanHero=null;buildPlanCharacterId=null;
            buildPlanPreview=buildPlanError=null;buildPlanScroll=Vector2.zero;
        }
        private void ReconcileBuildPlanSurface()
        {
            if(session.PracticeActive)return;
            if(buildPlansOpen&&(panel!=Panel.Camp||buildPlanOwner!=session.Progression||
                buildPlanHero!=session.Player||buildPlanCharacterId!=session.Progression.CurrentSlotId))ResetBuildPlanSurface();
        }
        private bool DrawBuildPlanSurface()
        {
            if(!buildPlansOpen)return false;
            ReconcileBuildPlanSurface();
            if(!buildPlansOpen)return false;
            if(allocationDraft!=null)return DrawAllocationDraftSurface();
            float unit=MobileControls.Active?TouchRatio:1;
            var layout=new MobileDialogLayout(width/unit,height/unit);
            bool confirm=!buildPlanChoosing&&buildPlanAction!=BuildPlanAction.None;
            string title=buildPlanChoosing?"手动选择替换装备":buildPlanAction==BuildPlanAction.Replace?"确认单部位引用替换？":buildPlanAction==BuildPlanAction.Reset?"免费重置配点？":buildPlanAction==BuildPlanAction.Save?
                (session.Progression.HasBuildPreset(buildPlanSlot)?"覆盖配装方案 ":"记录配装方案 ")+BuildPlanName(buildPlanSlot)+"？":
                buildPlanAction==BuildPlanAction.Apply?"应用配装方案 "+BuildPlanName(buildPlanSlot)+"？":"配装方案 · A / B";
            Fill(new Rect(0,0,width,height),new Color(.008f,.018f,.03f,1));
            blockedRects.Add(new Rect(0,0,width,height));
            Box(BuildPlanRect(layout.Frame,unit),gold,false);
            Text(BuildPlanRect(layout.Header,unit),title,Mathf.RoundToInt(21*unit),pale,true);
            float contentWidth=layout.Body.Width-18;
            float contentHeight=DrawBuildPlanContent(contentWidth,unit,false);
            buildPlanScroll=BeginTouchScroll("build-plans",BuildPlanRect(layout.Body,unit),buildPlanScroll,
                new Rect(0,0,contentWidth*unit,Mathf.Max(layout.Body.Height,contentHeight)*unit));
            DrawBuildPlanContent(contentWidth,unit,true);
            EndTouchScroll();
            if(NavigationButton(BuildPlanRect(layout.FooterButton(0,2),unit), confirm?"取消":buildPlanChoosing?"返回方案":"返回营地工坊", jade))
            {CloseBuildPlanSurface();return true;}
            if(confirm)
            {
                bool fresh=buildPlanSource==session.Progression.Profile&&buildPlanFingerprint==session.Progression.BuildStateFingerprint();
                string reason=buildPlanAction==BuildPlanAction.Replace?(buildPlanReplacement==null?"候选失效":buildPlanReplacement.Error):buildPlanAction==BuildPlanAction.Apply?session.Progression.BuildPresetLockReason(buildPlanSlot,session.IsInCamp):null;
                string caption=!fresh?"重新核对":buildPlanAction==BuildPlanAction.Replace?"确认替换此部位":buildPlanAction==BuildPlanAction.Reset?"确认重置配点":buildPlanAction==BuildPlanAction.Save?"确认记录方案":"确认应用方案";
                if(DrawButton(BuildPlanRect(layout.FooterButton(1,2),unit), caption, buildPlanAction==BuildPlanAction.Reset?ButtonRole.Danger:ButtonRole.Primary, !fresh||session.IsInCamp&&string.IsNullOrEmpty(reason)))
                {if(fresh)ConfirmBuildPlanAction();else if(buildPlanAction==BuildPlanAction.Replace)BeginPresetReplacement(buildPlanSlot,buildPlanPart);else RequestBuildPlanAction(buildPlanAction,buildPlanSlot);}
            }
            else if(!buildPlanChoosing&&DangerButton(BuildPlanRect(layout.FooterButton(1,2),unit), "免费重置 · "+session.Progression.RefundableBuildPoints+"点", gold, session.IsInCamp&&(session.Progression.RefundableBuildPoints>0||session.Progression.Profile.masteryCore>=0)))
                RequestBuildPlanAction(BuildPlanAction.Reset);
            return true;
        }
        private static Rect BuildPlanRect(MobilePanelLayout.Area area,float unit)
        {return new Rect(area.X*unit,area.Y*unit,area.Width*unit,area.Height*unit);}
        private void BuildPlanParagraph(ref float y,float width,float unit,string value,Color color,bool draw,bool bold=false)
        {
            if(string.IsNullOrEmpty(value))return;
            int size=Mathf.RoundToInt(14*unit);
            float h=Mathf.Ceil(Style(size,bold,true).CalcHeight(new GUIContent(value),(width-16)*unit)/unit)+2;
            if(draw)Text(new Rect(8*unit,y*unit,(width-16)*unit,h*unit),value,size,color,bold,true);
            y+=h+10;
        }
        private float DrawBuildPlanContent(float width,float unit,bool draw)
        {
            var p=session.Progression;float y=4;
            BuildPlanParagraph(ref y,width,unit,buildPlanError,gold,draw,true);
            if(buildPlanChoosing)
            {
                BuildPlanParagraph(ref y,width,unit,"按同机制、所需变体、等级排序；点击候选只预览，确认后才更新引用。",muted,draw);
                foreach(var candidate in p.PresetReplacementCandidates(buildPlanSlot,buildPlanPart))
                {
                    var quote=p.QuotePresetReplacement(buildPlanSlot,buildPlanPart,candidate.id);
                    if(quote==null)continue;
                    BuildPlanParagraph(ref y,width,unit,quote.Summary+(string.IsNullOrEmpty(quote.Error)?"":"\n"+quote.Error),pale,draw);
                    string id=candidate.id;
                    DraftButton(ref y,width,unit,"预览替换 · 保留方案变体",string.IsNullOrEmpty(quote.Error),draw,()=>PreviewPresetReplacement(id));
                    if(!string.IsNullOrEmpty(quote.Error)&&candidate.level<=p.Profile.level)
                        DraftButton(ref y,width,unit,"明确改为 A / 无变体并预览",candidate.level<=p.Profile.level,draw,()=>PreviewPresetReplacement(id,0));
                }
                return y;
            }
            if(buildPlanAction!=BuildPlanAction.None)
            {
                if(buildPlanSource!=p.Profile)
                    BuildPlanParagraph(ref y,width,unit,"当前角色的配装或装备已变化。请点「重新核对」刷新预览，再确认；也可取消。",gold,draw,true);
                BuildPlanParagraph(ref y,width,unit,buildPlanPreview,pale,draw,true);
                if(buildPlanAction==BuildPlanAction.Reset)
                {
                    BuildPlanParagraph(ref y,width,unit,"返还技能进阶 "+p.RefundableSkillRanks+"点 + 精通 "+p.RefundableMasteryPoints+"点 = 共 "+p.RefundableBuildPoints+"点",gold,draw,true);
                    BuildPlanParagraph(ref y,width,unit,"保留已学1阶、技能前置、快捷栏、当前装备和配装方案；精通核心关闭。当前技能冷却不重置。",muted,draw);
                }
                else if(buildPlanAction==BuildPlanAction.Save)
                {
                    if(p.HasBuildPreset(buildPlanSlot))
                    {
                        BuildPlanParagraph(ref y,width,unit,"将覆盖下列原方案：",gold,draw,true);
                        BuildPlanParagraph(ref y,width,unit,p.BuildPresetSummary(buildPlanSlot),muted,draw);
                    }
                    BuildPlanParagraph(ref y,width,unit,"记录当前技能、精通 / 核心、职业路线、快捷栏和三件装备的编号。只记录配装，不创建新角色存档，也不复制装备。",muted,draw);
                }
                else if(buildPlanAction==BuildPlanAction.Replace)
                {BuildPlanParagraph(ref y,width,unit,buildPlanReplacement==null?"候选失效":buildPlanReplacement.Error,gold,draw);}
                else
                {
                    string reason=p.BuildPresetLockReason(buildPlanSlot,session.IsInCamp);
                    if(reason!=buildPlanError)BuildPlanParagraph(ref y,width,unit,reason,gold,draw,true);
                    BuildPlanParagraph(ref y,width,unit,"将切换为上面的配装。之后新学的1阶技能保留；点数、前置、等级和装备编号均会重新校验。当前技能冷却不重置。",muted,draw);
                }
                if(!session.IsInCamp)BuildPlanParagraph(ref y,width,unit,"请先安全返回营地再操作。",gold,draw);
                return y;
            }
            for(int slot=0;slot<ProgressionService.BuildPresetCount;slot++)
            {
                bool occupied=p.HasBuildPreset(slot);
                BuildPlanParagraph(ref y,width,unit,"配装方案 "+BuildPlanName(slot)+(occupied?"":" · 空位"),gold,draw,true);
                string reason=p.BuildPresetLockReason(slot,session.IsInCamp);
                if(occupied&&!string.IsNullOrEmpty(reason))BuildPlanParagraph(ref y,width,unit,reason,gold,draw);
                if(draw)
                {
                    float buttonWidth=(width-24)*.5f;
                    if(Button(new Rect(8*unit,y*unit,buttonWidth*unit,48*unit),occupied?"覆盖方案 "+BuildPlanName(slot):"记录方案 "+BuildPlanName(slot),jade,session.IsInCamp))
                        RequestBuildPlanAction(BuildPlanAction.Save,slot);
                    if(PrimaryButton(new Rect((16+buttonWidth)*unit,y*unit,buttonWidth*unit,48*unit), "应用方案 "+BuildPlanName(slot), gold, occupied&&string.IsNullOrEmpty(reason)))
                        RequestBuildPlanAction(BuildPlanAction.Apply,slot);
                }
                y+=64;
                if(occupied)
                {
                    int selectedPlan=slot;
                    DraftButton(ref y,width,unit,(buildPlanDetails==slot?"收起":"展开")+"方案 "+BuildPlanName(slot)+" · 详情与引用修复",true,draw,()=>
                    {buildPlanDetails=buildPlanDetails==selectedPlan?-1:selectedPlan;CancelMobileScroll();BlockUITransition();});
                    if(buildPlanDetails==slot)
                    {
                        BuildPlanParagraph(ref y,width,unit,p.BuildPresetSummary(slot),pale,draw);
                        for(int part=0;part<3;part++){var selectedPart=(ItemSlot)part;DraftButton(ref y,width,unit,"手动替换 "+GameBalance.SlotName(selectedPart)+" 引用",session.IsInCamp,draw,()=>BeginPresetReplacement(selectedPlan,selectedPart));}
                    }
                }
            }
            BuildPlanParagraph(ref y,width,unit,"方案属于当前角色。缺失或等级不足的装备会阻止应用；不会凭名称寻找替代装备或复制已出售物品。",muted,draw);
            DraftButton(ref y,width,unit,"局部调整配点 · 临时草稿",session.IsInCamp,draw,OpenAllocationDraft);
            DrawPracticeChoices(ref y,width,unit,draw,session.IsInCamp,null);
            BuildPlanParagraph(ref y,width,unit,"当前配装",gold,draw,true);
            BuildPlanParagraph(ref y,width,unit,p.CurrentBuildSummary(),pale,draw);
            BuildPlanParagraph(ref y,width,unit,"免费重置预览：技能进阶 "+p.RefundableSkillRanks+"点 + 精通 "+p.RefundableMasteryPoints+"点 = "+p.RefundableBuildPoints+"点。保留已学1阶，关闭精通核心。",muted,draw);
            return y;
        }
        private void BeginPresetReplacement(int plan,ItemSlot slot)
        {RequestBuildPlanAction(BuildPlanAction.None,plan);buildPlanChoosing=true;buildPlanPart=slot;buildPlanReplacement=null;}
        private void PreviewPresetReplacement(string id,int variant=-2)
        {
            var p=session.Progression;var quote=p.QuotePresetReplacement(buildPlanSlot,buildPlanPart,id,variant);if(quote==null)return;
            RequestBuildPlanAction(BuildPlanAction.Replace,buildPlanSlot);buildPlanChoosing=false;buildPlanReplacement=quote;buildPlanPreview=quote.Summary;buildPlanError=quote.Error;
        }
        private Vector2 presetSaleScroll;private bool presetSaleOpen,presetSaleBulk;private string presetSaleId,presetSaleState,presetSaleImpact,presetSaleError;private ProgressionService presetSaleOwner;
        private void RequestPresetSale(string id,bool bulk=false)
        {
            var p=session.Progression;string impact=bulk?p.BulkSalePresetImpact():p.PresetReferences(id);
            if(impact.Length==0){if(bulk){int sold=p.BulkSellLowQuality();Feedback(sold>0,"已出售 "+sold+" 件");}else SellInventoryItem(id,true);return;}
            presetSaleOwner=p;presetSaleState=p.BuildStateFingerprint();presetSaleId=id;presetSaleBulk=bulk;presetSaleImpact=impact;presetSaleError=null;presetSaleScroll=Vector2.zero;presetSaleOpen=true;BlockUITransition();
        }
        private void CancelPresetSale(){presetSaleOpen=false;BlockUITransition();}
        private void ConfirmPresetSale()
        {
            var p=session.Progression;
            if(!presetSaleOpen||presetSaleOwner!=p||presetSaleState!=p.BuildStateFingerprint()||session.IsDead||session.PracticeActive||panel!=Panel.Inventory&&panel!=Panel.Camp)return;
            bool ok;if(presetSaleBulk){int count=p.BulkSellLowQuality(true);ok=count>0;}else ok=p.Sell(presetSaleId,true);
            if(ok){presetSaleOpen=false;RebuildBagItems();ResolveSelectedItem();session.Notify("已确认出售；相关方案引用需手动修复");}else presetSaleError=p.LastError;
            BlockUITransition();
        }
        private bool DrawPresetSaleConfirmation()
        {
            if(!presetSaleOpen)return false;
            var p=session.Progression;if(presetSaleOwner!=p||session.IsDead||session.PracticeActive||panel!=Panel.Inventory&&panel!=Panel.Camp){presetSaleOpen=false;return false;}
            float unit=MobileControls.Active?TouchRatio:1;var layout=new MobileDialogLayout(width/unit,height/unit);
            Fill(new Rect(0,0,width,height),new Color(.008f,.018f,.03f,1));blockedRects.Add(new Rect(0,0,width,height));Box(BuildPlanRect(layout.Frame,unit),gold,false);
            Text(BuildPlanRect(layout.Header,unit),"出售会影响已存方案",Mathf.RoundToInt(21*unit),gold,true);
            bool fresh=presetSaleState==p.BuildStateFingerprint();
            string body=presetSaleImpact+"\n出售后相关方案将缺失装备引用，需手动替换才能应用；不会自动挑选替代物。\n"+(fresh?"确认出售？":"装备或方案已变化，请取消后重新选择。")+"\n"+presetSaleError;
            float bodyWidth=layout.Body.Width-18,bodyHeight=0;BuildPlanParagraph(ref bodyHeight,bodyWidth,unit,body,pale,false);
            presetSaleScroll=BeginTouchScroll("preset-sale",BuildPlanRect(layout.Body,unit),presetSaleScroll,new Rect(0,0,bodyWidth*unit,Mathf.Max(layout.Body.Height,bodyHeight)*unit));
            float y=0;BuildPlanParagraph(ref y,bodyWidth,unit,body,pale,true);EndTouchScroll();
            if(NavigationButton(BuildPlanRect(layout.FooterButton(0,2),unit), "取消", jade)){CancelPresetSale();}
            if(DangerButton(BuildPlanRect(layout.FooterButton(1,2),unit), "确认出售", gold, fresh))
            {
                ConfirmPresetSale();
            }
            return true;
        }

        private void ConfirmBuildPlanAction()
        {
            var p=session.Progression;
            if(buildPlanAction==BuildPlanAction.None||buildPlanOwner!=p||buildPlanSource!=p.Profile||buildPlanFingerprint!=p.BuildStateFingerprint())return;
            bool accepted=buildPlanAction==BuildPlanAction.Replace?p.ReplacePresetEquipment(buildPlanReplacement,session.IsInCamp):buildPlanAction==BuildPlanAction.Reset?p.ResetBuild(session.IsInCamp):
                buildPlanAction==BuildPlanAction.Save?p.SaveBuildPreset(buildPlanSlot,session.IsInCamp):p.ApplyBuildPreset(buildPlanSlot,session.IsInCamp);
            if(!accepted)
                buildPlanError=string.IsNullOrEmpty(p.LastError)?"操作未保存，请重试或取消。":p.LastError;
            else
            {
                string result=buildPlanAction==BuildPlanAction.Replace?"仅此方案的单部位引用已更新":buildPlanAction==BuildPlanAction.Reset?"配点已重置；技能进阶与精通点已返还":
                    buildPlanAction==BuildPlanAction.Save?"配装方案 "+BuildPlanName(buildPlanSlot)+" 已记录":"已应用配装方案 "+BuildPlanName(buildPlanSlot);
                buildPlanAction=BuildPlanAction.None;buildPlanError=null;session.Notify(result);
            }
            buildPlanScroll=Vector2.zero;CancelMobileScroll();BlockUITransition();
        }
    }
}
