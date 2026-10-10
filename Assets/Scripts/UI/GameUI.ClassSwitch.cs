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
        internal bool ClassSwitchHasPendingEdit {get{return allocationDraft!=null||reforgeOwner!=null;}}
        private void OpenClassSwitch()
        {
            panel=Panel.Skills;skillSection=2;classSwitchMessage=null;CancelMobileScroll();
        }
        private void DrawClassSelectionTab()
        {
            float u=MobileControls.Active?TouchRatio:1;Rect body;
            if(MobileControls.Active)
            {
                var l=MobilePanelGeometry();
                if(DrawMobilePanelChrome(l,"切换职业","",showNotice:false,headerRightReserve:276))return;
                DrawSkillTabs(TouchRect(l.Close.X-276,8,264,44));
                body=MobilePanelRect(new MobilePanelLayout.Area(l.Body.X,l.Body.Y,l.Body.Width,l.Height-l.Body.Y-12));
            }
            else
            {
                Rect w=Modal(1160,660,"切换职业","");
                if(PopupCloseButton(new Rect(w.xMax-69,w.y+20,44,32))){ClosePanel();return;}
                DrawSkillTabs(new Rect(w.x+330,w.y+20,264,36));
                body=new Rect(w.x+24,w.y+108,w.width-48,w.height-132);
            }
            if(skillSection!=2)return;
            string[] roles={"近战 · 斩击 · 耐久","法术 · 控制 · 爆发","远程 · 灵活 · 射击","召唤 · 协同 · 灵兽"};
            float gap=10*u,cardWidth=(body.width-3*gap)/4,cardHeight=Mathf.Min(280*u,body.height-48*u);
            float top=body.y+Mathf.Max(0,(body.height-cardHeight-40*u)*.5f);
            string reason=session.ClassSwitchLockReason();
            for(int i=0;i<4;i++)
            {
                var hero=(HeroClass)i;bool current=hero==session.Progression.Profile.heroClass;
                Rect choice=new Rect(body.x+i*(cardWidth+gap),top,cardWidth,cardHeight);
                Color accent=GameBalance.ClassColor(hero);
                Fill(choice,new Color(.055f,.09f,.13f));Border(choice,current?gold:accent*.45f,current?2:1);
                float crest=Mathf.Min(cardWidth*.58f,cardHeight*.42f);
                DrawCrest(new Rect(choice.center.x-crest*.5f,choice.y+cardHeight*.15f,crest,crest),hero,accent);
                Text(new Rect(choice.x+4*u,choice.y+5*u,choice.width-8*u,20*u),current?"当前职业":"",Mathf.RoundToInt(11*u),gold,true,false,TextAnchor.MiddleCenter);
                Text(new Rect(choice.x+4*u,choice.y+cardHeight*.64f,choice.width-8*u,30*u),GameBalance.ClassName(hero),Mathf.RoundToInt(18*u),pale,true,false,TextAnchor.MiddleCenter);
                if(cardHeight>180*u)Text(new Rect(choice.x+4*u,choice.yMax-34*u,choice.width-8*u,24*u),roles[i],Mathf.RoundToInt(11*u),muted,false,false,TextAnchor.MiddleCenter);
                if(GUI.Button(choice,GUIContent.none,invisibleButton)&&!current)
                {
                    if(!string.IsNullOrEmpty(reason))classSwitchMessage=reason;
                    else {bool saved=session.TrySwitchClass(hero);skillSection=2;classSwitchMessage=saved?"已切换为"+GameBalance.ClassName(hero):session.ClassSwitchError;}
                }
            }
            Text(new Rect(body.x,top+cardHeight+8*u,body.width,32*u),!string.IsNullOrEmpty(classSwitchMessage)?classSwitchMessage:!string.IsNullOrEmpty(reason)?reason:"选择职业即可切换",Mathf.RoundToInt(13*u),gold,false,true,TextAnchor.MiddleCenter);
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
            blockedRects.Add(new Rect(0,0,width,height));
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
            BuildPlanParagraph(ref y,bodyWidth,unit,"共享等级、装备与地图进度。每个职业独立保留配点、路线和快捷栏。首次切换按技能位置保留合法配点，不复制教程。",muted,draw);
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
