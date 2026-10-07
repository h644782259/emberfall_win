using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private bool progressionGoalsOpen;
        private Vector2 progressionGoalScroll,progressionGoalHeaderScroll;
        private ProgressionService progressionGoalOwner;
        private string progressionGoalCharacter;
        private void OpenProgressionGoals()
        {
            progressionGoalsOpen=true;progressionGoalOwner=session.Progression;
            progressionGoalCharacter=session.Progression.CurrentSlotId;progressionGoalScroll=progressionGoalHeaderScroll=Vector2.zero;
            CancelMobileScroll();BlockUITransition();
        }
        private void ReconcileProgressionGoalSurface()
        {
            ReconcileReforgeSurface();
            if(progressionGoalsOpen&&(panel!=Panel.Camp||progressionGoalOwner!=session.Progression||progressionGoalCharacter!=session.Progression.CurrentSlotId))
                progressionGoalsOpen=false;
        }
        private bool CloseProgressionGoalSurface()
        {
            if(CloseReforgeSurface())return true;
            if(!progressionGoalsOpen)return false;
            progressionGoalsOpen=false;CancelMobileScroll();BlockUITransition();return true;
        }
        private string CurrentProgressionGoalStatus(int runMaterials=0)
        {return session.Progression.ProgressionGoalStatus(runMaterials,session.IsInCamp);}
        private bool DrawProgressionGoalSurface()
        {
            ReconcileProgressionGoalSurface();if(!progressionGoalsOpen)return false;
            float u=MobileControls.Active?TouchRatio:1;
            var l=new MobileDialogLayout(width/u,height/u);
            Fill(new Rect(0,0,width,height),new Color(.018f,.031f,.048f,1));blockedRects.Add(new Rect(0,0,width,height));
            Box(BuildPlanRect(l.Frame,u),jade,false);
            Text(BuildPlanRect(l.Header,u),"成长目标 · 一次追踪一个",Mathf.RoundToInt(21*u),pale,true);
            var p=session.Progression;var current=p.SelectedProgressionGoal(session.IsInCamp);
            string status=CurrentProgressionGoalStatus()+(string.IsNullOrEmpty(p.LastError)?"":"\n"+p.LastError);
            float statusHeight=Mathf.Ceil(Style(Mathf.RoundToInt(13*u),false,true).CalcHeight(new GUIContent(status),(l.Body.Width-26)*u)/u)+12;
            var sections=new ProgressionGoalLayout(l.Body,statusHeight,current.Action!=ProgressionGoalAction.None);
            progressionGoalHeaderScroll=BeginTouchScroll("progression-goal-current",BuildPlanRect(sections.Status,u),progressionGoalHeaderScroll,new Rect(0,0,(l.Body.Width-16)*u,statusHeight*u));
            Text(new Rect(4*u,4*u,(l.Body.Width-26)*u,(statusHeight-8)*u),status,Mathf.RoundToInt(13*u),jade,false,true);EndTouchScroll();
            if(current.Action!=ProgressionGoalAction.None&&PrimaryButton(BuildPlanRect(sections.Action,u), current.ActionLabel, gold, current.CanAct, current.Step))
            {if(current.Action==ProgressionGoalAction.OpenPresets){progressionGoalsOpen=false;OpenBuildPlans();}else Feedback(p.ExecuteProgressionGoal(current.ActionIdentity,session.IsInCamp),"目标操作已保存");progressionGoalHeaderScroll=Vector2.zero;BlockUITransition();return true;}
            float h=DrawProgressionGoalOptions(sections.Candidates.Width-18,u,false);
            progressionGoalScroll=BeginTouchScroll("progression-goals",BuildPlanRect(sections.Candidates,u),progressionGoalScroll,new Rect(0,0,(sections.Candidates.Width-18)*u,Mathf.Max(sections.Candidates.Height,h)*u));
            DrawProgressionGoalOptions(sections.Candidates.Width-18,u,true);EndTouchScroll();
            if(NavigationButton(BuildPlanRect(l.FooterButton(0,1),u), "返回工坊", jade))CloseProgressionGoalSurface();
            return true;
        }
        private float DrawProgressionGoalOptions(float w,float u,bool draw)
        {
            var p=session.Progression;float y=8;
            GoalNode(ref y,w,u,"10阶节点 · 首套方向（自主选择）","先选具体核心，集中材料建立一条路线；也可先做职业练习，不消耗机制材料。达到10阶不代表装备已成型。",draw);
            foreach(var mechanic in BuildCatalog.MechanicsFor(p.Profile.heroClass))GoalCoreOption(ref y,w,u,mechanic,Rarity.Common,draw);
            GoalOption(ref y,w,u,"职业练习 · "+p.ClassTutorialText,ProgressionGoalKind.ClassTutorial,null,0,draw);
            GoalNode(ref y,w,u,"20阶节点 · 第二套方向（自主选择）","保存方案 A / B 便于营地切换，保存本身不赠送装备；元素变体改变已有装备的收益与代价，需要对应装备和解锁材料。达到20阶不会代你保存方案。",draw);
            GoalOption(ref y,w,u,"保存第二套配装",ProgressionGoalKind.SecondPreset,null,0,draw);
            bool variants=false;
            foreach(ItemData item in p.Profile.inventory)
            {
                if(item==null||item.mechanic==EquipmentMechanic.None||BuildCatalog.MechanicClass(item.mechanic)!=p.Profile.heroClass)continue;
                if(!p.HasVariant(item)&&string.IsNullOrEmpty(p.MechanicGoalEligibility(item.id,ProgressionGoalKind.Variant)))
                {variants=true;GoalOption(ref y,w,u,"解锁变体 · "+GoalItemTitle(item),ProgressionGoalKind.Variant,item.id,0,draw);}
            }
            if(!variants)GoalUnavailable(ref y,w,u,"变体目标 · 需要尚未解锁变体的元素机制装备",draw);
            GoalNode(ref y,w,u,"40阶节点 · 自愿极限（自主选择）","升华把材料投入指定史诗装备；重铸追上角色等级。也可只追踪40阶挑战，逐阶解锁推进，不强制升华或换掉现有目标。",draw);
            bool ascensions=false;
            foreach(ItemData item in p.Profile.inventory)
            {
                if(item==null||item.mechanic==EquipmentMechanic.None||BuildCatalog.MechanicClass(item.mechanic)!=p.Profile.heroClass)continue;
                if(string.IsNullOrEmpty(p.MechanicGoalEligibility(item.id,ProgressionGoalKind.Ascension)))
                {ascensions=true;GoalOption(ref y,w,u,"传说升华 · "+GoalItemTitle(item),ProgressionGoalKind.Ascension,item.id,0,draw);}
                else if(item.rarity<Rarity.Epic){ascensions=true;GoalCoreOption(ref y,w,u,item.mechanic,Rarity.Epic,draw);}
                if(string.IsNullOrEmpty(p.MechanicGoalEligibility(item.id,ProgressionGoalKind.Reforge)))
                    GoalOption(ref y,w,u,"重铸至 "+p.Profile.level+" 级 · "+GoalItemTitle(item),ProgressionGoalKind.Reforge,item.id,0,draw);
            }
            if(!ascensions)GoalUnavailable(ref y,w,u,"升华目标 · 需要本职业史诗机制装备",draw);
            GoalOption(ref y,w,u,"自愿挑战 · 通关第40阶",ProgressionGoalKind.Tier,null,40,draw);
            if(p.HighestUnlockedAdventureTier!=40)GoalOption(ref y,w,u,"当前可进 · 第 "+p.HighestUnlockedAdventureTier+" 阶",ProgressionGoalKind.Tier,null,p.HighestUnlockedAdventureTier,draw);
            GoalOption(ref y,w,u,"取消追踪",ProgressionGoalKind.None,null,0,draw);return y;
        }
        private void GoalNode(ref float y,float w,float u,string title,string reason,bool draw)
        {
            GoalParagraph(ref y,w,u,title,16,gold,true,draw);
            GoalParagraph(ref y,w,u,reason,13,muted,false,draw);y+=8;
        }
        private void GoalParagraph(ref float y,float w,float u,string text,int size,Color color,bool bold,bool draw)
        {
            int font=Mathf.RoundToInt(size*u);
            float h=Mathf.Ceil(Style(font,bold,true).CalcHeight(new GUIContent(text),(w-16)*u)/u)+8;
            if(draw)Text(new Rect(8*u,y*u,(w-16)*u,h*u),text,font,color,bold,true);
            y+=h;
        }
        private static string GoalItemTitle(ItemData item)
        {return item.name+" · Lv."+item.level+" · "+GameBalance.RarityName(item.rarity);}
        private void GoalCoreOption(ref float y,float w,float u,EquipmentMechanic mechanic,Rarity rarity,bool draw)
        {
            var p=session.Progression;bool selected=p.Profile.progressionGoal==ProgressionGoalKind.Core&&p.Profile.progressionGoalMechanic==mechanic&&p.Profile.progressionGoalMinimumRarity==rarity;
            string text=(rarity==Rarity.Epic?"升华前置 · 获取史诗":"获取核心 · ")+BuildCatalog.MechanicName(mechanic);
            if(draw&&TabButton(new Rect(8*u,y*u,(w-16)*u,48*u), text+(selected?" ✓":""), selected))
            {Feedback(p.SelectCoreGoal(mechanic,rarity),"具体核心目标已保存");progressionGoalHeaderScroll=Vector2.zero;CancelMobileScroll();BlockUITransition();}
            y+=56;
            GoalParagraph(ref y,w,u,BuildCatalog.MechanicDescription(mechanic),13,muted,false,draw);
        }
        private void GoalOption(ref float y,float w,float u,string text,ProgressionGoalKind kind,string id,int tier,bool draw)
        {
            bool selected=session.Progression.Profile.progressionGoal==kind && (id==null||session.Progression.Profile.progressionGoalItemId==id) &&
                (kind!=ProgressionGoalKind.Tier||session.Progression.Profile.progressionGoalTier==tier) &&
                (kind!=ProgressionGoalKind.Reforge||session.Progression.Profile.progressionGoalLevel==session.Progression.Profile.level);
            if(draw&&TabButton(new Rect(8*u,y*u,(w-16)*u,48*u), text+(selected?" ✓":""), selected))
            {Feedback(session.Progression.SelectProgressionGoal(kind,id,tier,kind==ProgressionGoalKind.Reforge?session.Progression.Profile.level:0),"成长目标已保存");progressionGoalHeaderScroll=Vector2.zero;CancelMobileScroll();BlockUITransition();}
            y+=56;
        }
        private void GoalUnavailable(ref float y,float w,float u,string text,bool draw)
        {if(draw)Text(new Rect(8*u,y*u,(w-16)*u,40*u),text,Mathf.RoundToInt(13*u),muted,false,true);y+=48;}
    }
}
