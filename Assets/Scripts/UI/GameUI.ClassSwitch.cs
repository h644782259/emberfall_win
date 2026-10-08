using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private bool classSwitchOpen;
        private ProgressionService classSwitchOwner;
        private PlayerController classSwitchHero;
        private ProgressionService.ClassSwitchTransaction classSwitchPreview;
        private string classSwitchFingerprint,classSwitchMessage;
        private Vector2 classSwitchScroll;
        internal bool ClassSwitchHasPendingEdit {get{return allocationDraft!=null||buildPlanAction!=BuildPlanAction.None||buildPlanChoosing||presetSaleOpen||reforgeOwner!=null;}}
        private void OpenClassSwitch()
        {
            if(ClassSwitchHasPendingEdit){session.Notify("请先完成或取消当前草稿与确认。");return;}
            classSwitchOpen=true;classSwitchOwner=session.Progression;classSwitchHero=session.Player;
            classSwitchPreview=null;classSwitchMessage=null;classSwitchScroll=Vector2.zero;
            CancelMobileScroll();BlockUITransition();
        }
        private bool CloseClassSwitchSurface()
        {
            if(!classSwitchOpen||(panel!=Panel.Camp&&panel!=Panel.Skills))return false;
            classSwitchOpen=false;classSwitchPreview=null;classSwitchScroll=Vector2.zero;
            CancelMobileScroll();BlockUITransition();return true;
        }
        private void ReconcileClassSwitchSurface()
        {
            if(classSwitchOpen&&((panel!=Panel.Camp&&panel!=Panel.Skills)||session.Progression!=classSwitchOwner||session.Player!=classSwitchHero))
            {classSwitchOpen=false;classSwitchPreview=null;classSwitchOwner=null;classSwitchHero=null;}
        }
        internal void OnClassSwitched()
        {
            classSwitchOpen=false;classSwitchPreview=null;classSwitchOwner=null;classSwitchHero=null;
            RebindProgressionNotifications(session.Progression,session.Progression);
            CancelForegroundInput();CancelMobileCast();CancelMobileScroll();ResetMobileSkillNavigation();
            skillDevelopmentScroll=Vector2.zero;saveSlotsDirty=true;BlockUITransition();
        }
        private void PreviewClassSwitch(HeroClass target)
        {
            classSwitchPreview=session.Progression.PrepareClassSwitch(target,session.IsInCamp);
            classSwitchFingerprint=session.Progression.BuildStateFingerprint();
            classSwitchMessage=classSwitchPreview==null?session.Progression.LastError:null;
            CancelMobileScroll();
        }
        private bool DrawClassSwitchSurface()
        {
            ReconcileClassSwitchSurface();
            if(!classSwitchOpen)return false;
            float unit=MobileControls.Active?TouchRatio:1;var layout=new MobileDialogLayout(width/unit,height/unit);
            Fill(new Rect(0,0,width,height),new Color(.008f,.018f,.03f,1));blockedRects.Add(new Rect(0,0,width,height));
            Box(BuildPlanRect(layout.Frame,unit),gold,false);Text(BuildPlanRect(layout.Header,unit),"营地 · 切换职业",Mathf.RoundToInt(21*unit),pale,true);
            float bodyWidth=layout.Body.Width-18,bodyHeight=DrawClassSwitchContent(bodyWidth,unit,false);
            classSwitchScroll=BeginTouchScroll("class-switch",BuildPlanRect(layout.Body,unit),classSwitchScroll,new Rect(0,0,bodyWidth*unit,Mathf.Max(layout.Body.Height,bodyHeight)*unit));
            DrawClassSwitchContent(bodyWidth,unit,true);EndTouchScroll();
            if(NavigationButton(BuildPlanRect(layout.FooterButton(0,2),unit), "取消 · 返回技能", jade)){CloseClassSwitchSurface();return true;}
            bool fresh=classSwitchPreview!=null&&classSwitchFingerprint==session.Progression.BuildStateFingerprint();
            string reason=session.ClassSwitchLockReason();
            string caption=classSwitchPreview==null?"选择另一职业":!fresh?"重新核对":"确认切换为"+GameBalance.ClassName(classSwitchPreview.Target);
            if(DrawButton(BuildPlanRect(layout.FooterButton(1,2),unit), caption, ButtonRole.Primary, classSwitchPreview!=null&&string.IsNullOrEmpty(reason)))
            {
                if(!fresh)PreviewClassSwitch(classSwitchPreview.Target);
                else if(!session.TrySwitchClass(classSwitchPreview.Target))classSwitchMessage=session.ClassSwitchError;
                CancelMobileScroll();BlockUITransition();
            }
            return true;
        }
        private float DrawClassSwitchContent(float bodyWidth,float unit,bool draw)
        {
            float y=4;var p=session.Progression;
            BuildPlanParagraph(ref y,bodyWidth,unit,"当前："+GameBalance.ClassName(p.Profile.heroClass),gold,draw,true);
            BuildPlanParagraph(ref y,bodyWidth,unit,"共享等级、装备与地图进度。每个职业独立保留配点、路线、快捷栏和方案 A / B。首次切换按技能位置保留合法配点，不复制教程或已存方案。",muted,draw);
            BuildPlanParagraph(ref y,bodyWidth,unit,"真实装备及其数值、品质、强化保持；专属机制不会转成其他职业收益。切换保持生命比例与能量，冷却继续计时，不重复发奖。",muted,draw);
            BuildPlanParagraph(ref y,bodyWidth,unit,classSwitchMessage,gold,draw,true);
            BuildPlanParagraph(ref y,bodyWidth,unit,session.ClassSwitchLockReason(),gold,draw);
            for(int i=0;i<4;i++)
            {
                HeroClass hero=(HeroClass)i;bool current=hero==p.Profile.heroClass;
                DraftButton(ref y,bodyWidth,unit,(current?"当前 · ":classSwitchPreview!=null&&classSwitchPreview.Target==hero?"已选择 · ":"预览 · ")+GameBalance.ClassName(hero),!current,draw,()=>PreviewClassSwitch(hero));
            }
            if(classSwitchPreview!=null)
            {
                BuildPlanParagraph(ref y,bodyWidth,unit,"切换后："+GameBalance.ClassName(classSwitchPreview.Target),gold,draw,true);
                BuildPlanParagraph(ref y,bodyWidth,unit,classSwitchPreview.PreviewSummary,pale,draw);
            }
            return y;
        }
    }
}
