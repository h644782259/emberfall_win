using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private ProgressionService.BuildDraft allocationDraft;
        private bool draftShowUnchanged=true;
        private Vector2 draftSummaryScroll;
        private void OpenAllocationDraft()
        {
            allocationDraft=session.Progression.BeginBuildDraft(session.IsInCamp);
            draftShowUnchanged=true;draftSummaryScroll=Vector2.zero;
            buildPlanScroll=Vector2.zero;CancelMobileScroll();BlockUITransition();
        }
        private void CancelAllocationDraft()
        {
            if(allocationDraft!=null)allocationDraft.Cancel();allocationDraft=null;
            buildPlanScroll=Vector2.zero;
        }
        private void ApplyAllocationDraft(int slot=-1)
        {
            if(allocationDraft==null)return;
            if(allocationDraft.Apply(session.IsInCamp,slot))
            {
                allocationDraft=null;buildPlanScroll=Vector2.zero;
                session.Notify(slot<0?"配点草稿已应用":"配点已应用并保存到方案 "+(slot==0?"A":"B"));
            }
            CancelMobileScroll();BlockUITransition();
        }
        private bool DrawAllocationDraftSurface()
        {
            float unit=MobileControls.Active?TouchRatio:1;
            var layout=new MobileDialogLayout(width/unit,height/unit);
            Fill(new Rect(0,0,width,height),new Color(.008f,.018f,.03f,1));blockedRects.Add(new Rect(0,0,width,height));
            Box(BuildPlanRect(layout.Frame,unit),gold,false);
            Text(BuildPlanRect(layout.Header,unit),"营地配点草稿 · 尚未应用",Mathf.RoundToInt(21*unit),pale,true);
            float contentWidth=layout.Body.Width-18;
            string summary=allocationDraft==null?"":allocationDraft.ChangeSummary;
            var summaryRect=new Rect(layout.Body.X*unit,layout.Body.Y*unit,layout.Body.Width*unit,86*unit);
            float summaryHeight=Mathf.Max(86,Style(14,true,true).CalcHeight(new GUIContent(summary),contentWidth));
            draftSummaryScroll=BeginTouchScroll("draft-fixed-summary",summaryRect,draftSummaryScroll,new Rect(0,0,contentWidth*unit,summaryHeight*unit));
            Text(new Rect(0,0,contentWidth*unit,summaryHeight*unit),summary,Mathf.RoundToInt(14*unit),gold,true,true);EndTouchScroll();
            var bodyRect=new Rect(layout.Body.X*unit,(layout.Body.Y+94)*unit,layout.Body.Width*unit,(layout.Body.Height-94)*unit);
            float contentHeight=DrawAllocationDraftContent(contentWidth,unit,false);
            buildPlanScroll=BeginTouchScroll("build-draft",bodyRect,buildPlanScroll,new Rect(0,0,contentWidth*unit,Mathf.Max(layout.Body.Height-94,contentHeight)*unit));
            DrawAllocationDraftContent(contentWidth,unit,true);EndTouchScroll();
            if(Button(BuildPlanRect(layout.FooterButton(0,2),unit),"取消草稿",jade))
            {CancelAllocationDraft();CancelMobileScroll();BlockUITransition();return true;}
            if(Button(BuildPlanRect(layout.FooterButton(1,2),unit),"应用配点",gold,allocationDraft!=null&&allocationDraft.IsCurrent&&session.IsInCamp))ApplyAllocationDraft();
            return true;
        }
        private void DraftButton(ref float y,float width,float unit,string text,bool enabled,bool draw,System.Action action)
        {
            int size=MobileControls.Active?TouchFont(15):15;
            float h=Mathf.Max(48*unit,Style(size,true,true).CalcHeight(new GUIContent(text),(width-16)*unit-16)+16);
            if(draw)
            {
                var rect=new Rect(8*unit,y*unit,(width-16)*unit,h);
                bool clicked=Button(rect,"",gold,enabled);
                Text(new Rect(rect.x+8,rect.y+8,rect.width-16,rect.height-16),text,size,enabled&&GUI.enabled?pale:muted*.7f,true,true,TextAnchor.MiddleCenter);
                if(clicked)action();
            }
            y+=h/unit+10;
        }
        private float DrawAllocationDraftContent(float width,float unit,bool draw)
        {
            var draft=allocationDraft;float y=4;if(draft==null)return y;bool fresh=draft.IsCurrent;
            BuildPlanParagraph(ref y,width,unit,draft.Error,gold,draw,true);
            if(!fresh)BuildPlanParagraph(ref y,width,unit,"角色资料已变化，请取消并重新打开草稿。",gold,draw,true);
            BuildPlanParagraph(ref y,width,unit,"共享剩余 "+draft.Points+" / "+GameBalance.SkillPointBudget(draft.Level)+"点 · 已学1阶不退还",gold,draw,true);
            BuildPlanParagraph(ref y,width,unit,"所有 +/- 仅修改临时草稿；取消不会改变角色、存档或方案。应用不会回复生命、能量或刷新冷却。",muted,draw);
            var before=session.Progression.GetStats();var after=draft.Stats;
            BuildPlanParagraph(ref y,width,unit,"属性预览（当前 → 草稿）\n伤害 "+before.Damage.ToString("0.0")+" → "+after.Damage.ToString("0.0")+" · 生命上限 "+before.MaxHealth.ToString("0")+" → "+after.MaxHealth.ToString("0")+"\n护甲 "+before.Armor.ToString("0.0")+" → "+after.Armor.ToString("0.0")+" · 暴击 "+before.CritChance.ToString("P0")+" → "+after.CritChance.ToString("P0")+" · 移速 "+before.MoveSpeed.ToString("0.0")+" → "+after.MoveSpeed.ToString("0.0"),pale,draw);
            DrawPracticeChoices(ref y,width,unit,draw,fresh,draft);
            DraftButton(ref y,width,unit,"撤销上一步",draft.CanUndo&&fresh,draw,()=>draft.Undo());
            DraftButton(ref y,width,unit,draftShowUnchanged?"折叠未改项目":"展开未改项目",true,draw,()=>draftShowUnchanged=!draftShowUnchanged);
            for(int i=0;i<GameBalance.SkillCount;i++)
            {
                if(!draftShowUnchanged&&!draft.SkillChanged(i))continue;
                int index=i;string name=GameBalance.SkillName(session.Progression.Profile.heroClass,i);
                BuildPlanParagraph(ref y,width,unit,(draft.SkillChanged(i)?"已改 · ":"")+name+" · "+draft.SkillRank(i)+"阶",draft.SkillChanged(i)?gold:pale,draw,true);
                DraftAdjustment(ref y,width,unit,draw,fresh?draft.SkillChangeReason(i,-1):"草稿已过期",fresh?draft.SkillChangeReason(i,1):"草稿已过期",()=>draft.ChangeSkill(index,-1),()=>draft.ChangeSkill(index,1));
                if(draft.SkillChanged(i))BuildPlanParagraph(ref y,width,unit,draft.SkillChangeEffects(i),gold,draw);
                if(draft.SkillRank(i)==0)BuildPlanParagraph(ref y,width,unit,"未学：请先在技能页解锁1阶。",muted,draw);
            }
            BuildPlanParagraph(ref y,width,unit,"精通 · 当前等级单项上限 "+ProgressionService.MasteryCap(draft.Level),gold,draw,true);
            for(int i=0;i<4;i++)
            {
                if(!draftShowUnchanged&&!draft.MasteryChanged(i))continue;
                int index=i;
                if(draft.MasteryChanged(i))BuildPlanParagraph(ref y,width,unit,"已改 · "+draft.OriginalMasteryRank(i)+" → "+draft.MasteryRank(i)+"点\n"+BuildCatalog.MasteryDescription((MasteryType)i),gold,draw);
                BuildPlanParagraph(ref y,width,unit,BuildCatalog.MasteryName((MasteryType)i)+" · "+draft.MasteryRank(i)+"点 · "+draft.CoreThreshold(i)+(draft.Core==i?" · 当前核心":""),pale,draw,true);
                DraftAdjustment(ref y,width,unit,draw,fresh?draft.MasteryChangeReason(i,-1):"草稿已过期",fresh?draft.MasteryChangeReason(i,1):"草稿已过期",()=>draft.ChangeMastery(index,-1),()=>draft.ChangeMastery(index,1));
                DraftButton(ref y,width,unit,draft.Core==i?"关闭此核心":"选择此核心",fresh&&draft.MasteryRank(i)>=MasteryCoreRules.InitialInvestment,draw,()=>draft.SelectCore(draft.Core==index?-1:index));
            }
            BuildPlanParagraph(ref y,width,unit,"退点使当前核心低于门槛时会在草稿中关闭它；撤销会还原核心。以下操作会同时应用草稿并覆盖所选方案，保存失败则两者均不改变。",muted,draw);
            for(int i=0;i<ProgressionService.BuildPresetCount;i++)
            {
                int slot=i;
                DraftButton(ref y,width,unit,"应用并"+(session.Progression.HasBuildPreset(i)?"覆盖":"保存")+"方案 "+(i==0?"A":"B"),fresh&&session.IsInCamp,draw,()=>ApplyAllocationDraft(slot));
            }
            return y;
        }
        private void DraftAdjustment(ref float y,float width,float unit,bool draw,string minusReason,string plusReason,System.Action minus,System.Action plus)
        {
            float half=(width-24)*.5f;
            if(draw)
            {
                if(Button(new Rect(8*unit,y*unit,half*unit,48*unit),"− 1点",jade,minusReason==null,minusReason))minus();
                if(Button(new Rect((16+half)*unit,y*unit,half*unit,48*unit),"+ 1点",gold,plusReason==null,plusReason))plus();
            }
            y+=58;
            if(plusReason!=null)BuildPlanParagraph(ref y,width,unit,plusReason,muted,draw);
        }
    }
}
