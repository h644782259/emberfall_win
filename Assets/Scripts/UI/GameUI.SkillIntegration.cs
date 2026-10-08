using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private int skillSection;
        private Vector2 skillDevelopmentScroll;
        private void DrawSkillTabs(Rect rect)
        {
            float gap=8*(MobileControls.Active?TouchRatio:1),w=(rect.width-gap)/2;
            if(TabButton(new Rect(rect.x,rect.y,w,rect.height),"学习与配置",skillSection==0)){skillSection=0;CancelMobileScroll();}
            if(TabButton(new Rect(rect.x+w+gap,rect.y,w,rect.height),"职业与精通",skillSection==1)){skillSection=1;CancelMobileScroll();}
        }
        private bool DrawSkillSubsurface()
        {return DrawClassSwitchSurface()||DrawBuildPlanSurface();}
        private void DrawSkillDevelopment()
        {
            var p=session.Progression;
            if(MobileControls.Active)
            {
                var l=MobilePanelGeometry();
                if(DrawMobilePanelChrome(l,"技能 · 职业与精通","技能点 "+p.Profile.skillPoints+" · 投入、重置、职业与方案共用现有存档"))return;
                DrawSkillTabs(MobilePanelRect(l.Tabs));
                if(skillSection==0)return;
                var body=new MobilePanelLayout.Area(l.TabbedBody.X,l.TabbedBody.Y,l.TabbedBody.Width,l.Height-l.TabbedBody.Y-12);
                float mobileWidth=body.Width-16,h=8;DrawMobileWorkshopAbilities(ref h,mobileWidth,false);
                skillDevelopmentScroll=BeginTouchScroll("skill-development",MobilePanelRect(body),skillDevelopmentScroll,new Rect(0,0,mobileWidth*TouchRatio,Mathf.Max(body.Height,h)*TouchRatio));
                h=8;DrawMobileWorkshopAbilities(ref h,mobileWidth,true);EndTouchScroll();return;
            }
            Rect w=Modal(980,660,"技能 · 职业与精通","");
            if(NavigationButton(new Rect(w.xMax-69,w.y+20,44,32),"×",jade))ClosePanel();
            DrawSkillTabs(new Rect(w.x+26,w.y+82,440,36));
            if(skillSection==0)return;
            if(NavigationButton(new Rect(w.xMax-255,w.y+20,170,36),"切换职业",jade))OpenClassSwitch();
                Text(new Rect(w.x+32,w.y+167,880,30),GameBalance.ClassName(p.Profile.heroClass)+" · 职业能力",23,gold,true);
                string[] signatures={"真正躲过攻击后，2秒内下一次普攻反击。","冰霜新星 → 陨星，消耗霜印碎冰。","普攻积累三层毒，以扇形箭引爆。","幼狼从开场协战；普攻让伙伴短时集火。"};
                Text(new Rect(w.x+32,w.y+208,880,38),signatures[(int)p.Profile.heroClass],18,pale,false,true);
                if(new Rect(w.x+32,w.y+167,880,81).Contains(Mouse))tooltip=BuildCatalog.ClassSignatureDescription(p.Profile.heroClass);
                for(int i=0;i<2;i++)
                {
                    int route=i;var info=CampRouteCards.Describe(p.Profile,false,i);
                    Rect c=new Rect(w.x+32+i*458,w.y+254,430,174);Fill(c,card);
                    Text(new Rect(c.x+14,c.y+10,402,25),info.Name,19,gold,true);
                    Text(new Rect(c.x+14,c.y+39,402,36),info.Loop,14,pale,false,true);
                    Text(new Rect(c.x+14,c.y+79,402,50),info.Requirements+(info.Ready?"\n"+info.Enhancement:""),12,info.Ready?jade:muted,false,true);
                    if(info.NextAction==CampRouteAction.None)Text(new Rect(c.x+14,c.y+137,402,25),info.NextStep,12,muted);
                    else if(DrawButton(new Rect(c.x+14,c.y+137,402,28),info.NextStep,CampRouteButtonRole(info),session.IsInCamp))FollowCampRouteStep(info,route);
                }
                for(int i=0;i<4;i++)
                {
                    MasteryType mastery=(MasteryType)i;Rect c=new Rect(w.x+32+i*229,w.y+435,214,134);
                    Text(new Rect(c.x,c.y,c.width,25),BuildCatalog.MasteryName(mastery)+"  "+p.Profile.masteryRanks[i]+"/"+ProgressionService.MasteryCap(p.Profile.level),17,pale,true);
                    string reason=p.MasteryLockReason(mastery);
                    if(Button(new Rect(c.x,c.y+39,c.width,35),"投入 1 点",jade,string.IsNullOrEmpty(reason),string.IsNullOrEmpty(reason)?BuildCatalog.MasteryDescription(mastery):reason))Feedback(p.LearnMastery(mastery),"精通已提高");
                    if(PrimaryButton(new Rect(c.x,c.y+82,c.width,32), p.HasMasteryCore(mastery)?(p.MasteryCoreTier(mastery)==2?"增强核心 ✓":"初阶核心 ✓"):"启用核心 · "+MasteryCoreRules.InitialInvestment+"点", gold, session.IsInCamp&&p.Profile.masteryRanks[i]>=MasteryCoreRules.InitialInvestment&&!p.HasMasteryCore(mastery), BuildCatalog.MasteryDescription(mastery)))Feedback(p.SelectMasteryCore(mastery,session.IsInCamp),"已切换唯一精通核心");
                }
                Text(new Rect(w.x+32,w.y+552,884,22),"可用点数 "+p.Profile.skillPoints+" · "+MasteryProgressionRules.TierSummary+" · "+MasteryProgressionRules.CoreSummary,12,muted);
                if(DangerButton(new Rect(w.x+32,w.y+580,435,30), "免费重置配点 · "+p.RefundableBuildPoints+"点", muted, session.IsInCamp&&(p.RefundableBuildPoints>0||p.Profile.masteryCore>=0), "先核对技能进阶与精通返还点数；保留已学1阶、快捷栏和装备。"))RequestBuildPlanAction(BuildPlanAction.Reset);
                if(NavigationButton(new Rect(w.x+485,w.y+580,461,30), "配装方案 A / B · 记录 / 应用", jade))OpenBuildPlans();
        }
    }
}
