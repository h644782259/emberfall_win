using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private Vector2 attachmentScroll;
        private void DrawAttachmentWorkshop()
        {
            var p=session.Progression;
            Rect w=Modal(Mathf.Min(width-24,980),Mathf.Min(height-24,660),"机制挂件","独立挂载 · 换装沿用 · 同机制仅一件 · 星烬碎片可升级");
            if(NavigationButton(new Rect(w.xMax-190,w.y+20,160,34),"返回工坊",jade)){campTab=0;return;}
            Rect view=new Rect(w.x+24,w.y+110,w.width-48,w.height-174);
            var all=BuildCatalog.MechanicsFor(p.Profile.heroClass);
            attachmentScroll=BeginTouchScroll("attachments",view,attachmentScroll,new Rect(0,0,view.width-16,all.Length*236));
            for(int i=0;i<all.Length;i++)
            {
                var mechanic=all[i];var a=p.Attachment(mechanic);float y=i*236,cw=view.width-16;
                Rect c=new Rect(0,y,cw,220);Fill(c,card);Border(c,a!=null&&a.mounted?jade:muted*.4f);
                DrawIcon(new Rect(16,y+16,48,48),UIIconAtlas.Utility("skills"),a!=null?gold:muted);
                Text(new Rect(78,y+12,cw-94,28),BuildCatalog.MechanicName(mechanic)+(a==null?" · 未获得":" · "+GameBalance.RarityName(a.rarity)+" · Lv."+a.level+" · "+a.upgradeRank+"/5阶"),20,pale,true);
                Text(new Rect(78,y+46,cw-94,22),a==null?"首通自选或12碎片兑换":(a.mounted?"已挂载":"已卸下")+" · 变体 "+(a.variant==0?"A":"B")+" · 每阶攻击+1.5%、生命+2% · 3阶拓展机制 · 5阶额外增强20%",12,a!=null&&a.mounted?jade:muted);
                Text(new Rect(16,y+79,cw-32,78),BuildCatalog.MechanicDescription(mechanic),14,muted,false,true);
                float bw=(cw-56)/4;
                if(a==null)
                {
                    bool first=p.Profile.pendingFirstClearReward&&!p.Profile.firstClearRewardClaimed;
                    if(PrimaryButton(new Rect(16,y+168,cw-32,36),first?"领取首通挂件":"兑换挂件 · 12碎片",gold,session.IsInCamp&&(first||p.Profile.mechanicMaterials>=12)))
                        Feedback(first?p.ClaimFirstClearReward(mechanic):p.ExchangeMechanic(mechanic),"挂件已获得并挂载");
                }
                else
                {
                    if(Button(new Rect(16,y+168,bw,36),a.mounted?"卸下":"挂载",jade,session.IsInCamp))Feedback(p.SetAttachmentMounted(mechanic,!a.mounted,session.IsInCamp),"挂载状态已保存");
                    string upgrade=p.AttachmentUpgradeLock(mechanic,session.IsInCamp);
                    if(Button(new Rect(24+bw,y+168,bw,36),"升级 · 6碎片",gold,upgrade.Length==0,upgrade))Feedback(p.UpgradeAttachment(mechanic,session.IsInCamp),"挂件等级与核心效果已提升");
                    bool variant=BuildCatalog.HasMechanicVariant(mechanic);
                    if(Button(new Rect(32+bw*2,y+168,bw,36),a.variantUnlocked?"切换变体":"变体 · 4碎片",jade,variant&&session.IsInCamp&&(a.variantUnlocked||p.Profile.mechanicMaterials>=4)))Feedback(p.ToggleAttachmentVariant(mechanic,session.IsInCamp),"挂件变体已切换");
                    if(Button(new Rect(40+bw*3,y+168,bw,36),a.rarity==Rarity.Legendary?"已升华":"升华 · 24碎片",gold,session.IsInCamp&&a.rarity==Rarity.Epic&&p.HighestAdventureTier>=5&&p.Profile.mechanicMaterials>=24))Feedback(p.AscendAttachment(mechanic,session.IsInCamp),"挂件已升华");
                }
            }
            EndTouchScroll();
            Text(new Rect(w.x+24,w.yMax-48,w.width-48,28),"星烬碎片 × "+p.Profile.mechanicMaterials+" · 传说机制额外+15% · 旧装备属性、强化与方案投入保留",14,gold,true);
        }
        private void DrawGrowthHudCard()
        {
            if(MobileControls.Active||session.PracticeActive||session.InDungeon)return;
            var goal=session.Progression.SelectedProgressionGoal(session.IsInCamp);
            Rect r=new Rect(18,height-156,282,98);blockedRects.Add(r);Box(r,jade,false);
            DrawIcon(new Rect(r.x+12,r.y+14,24,24),UIIconAtlas.Utility("confirm"),jade);
            Text(new Rect(r.x+46,r.y+10,r.width-58,25),goal.Title,14,pale,true);
            Text(new Rect(r.x+12,r.y+43,r.width-24,38),"达成自动奖励 · 点击查看与定位",12,muted,false,true);
            if(GUI.Button(r,GUIContent.none,invisibleButton))NavigateProgressionGoal(goal);
        }
        private void NavigateProgressionGoal(ProgressionGoalState goal)
        {
            if(!session.IsInCamp)
            {
                ClosePanel();session.ReturnToCamp();
                if(!session.IsInCamp)return;
            }
            if(panel!=Panel.Camp)TogglePanel(Panel.Camp);
            if(goal.Action==ProgressionGoalAction.OpenPresets)
            {progressionGoalsOpen=false;OpenBuildPlans();buildPlanDetails=1;}
            else if(goal.RequiredAdventureTier>0&&goal.MaterialCost==0||goal.MaterialCost>session.Progression.Profile.mechanicMaterials)
            {
                ClosePanel();session.Player.Teleport(WorldTraversal.NearestWalkable(new Vector3(0,0,11),.45f));
                session.EnterDungeon();session.SelectedDungeonTier=Mathf.Min(Mathf.Max(1,goal.RequiredAdventureTier),session.MaximumDungeonTier);
            }
            else if(goal.Identity.Contains("practice")||session.Progression.Profile.progressionGoal==ProgressionGoalKind.ClassTutorial)campTab=3;
            else campTab=1;
            BlockUITransition();
        }
        private bool DrawAutomaticGrowthSurface()
        {
            var p=session.Progression;var g=p.SelectedProgressionGoal(session.IsInCamp);
            Rect w=Modal(Mathf.Min(width-24,780),Mathf.Min(height-24,420),"自动成长", "每个目标只奖励一次 · 达成后自动推进 · 切职业保留各自回执");
            Text(new Rect(w.x+28,w.y+113,w.width-56,40),g.Title,24,gold,true);
            Text(new Rect(w.x+28,w.y+163,w.width-56,86),g.Step+"\n目标奖励：50金币 + 1星烬碎片",16,pale,false,true);
            if(g.Action!=ProgressionGoalAction.None&&Button(new Rect(w.x+28,w.y+267,(w.width-68)*.5f,42),g.ActionLabel,jade,g.CanAct,g.Step))
            {if(g.Action==ProgressionGoalAction.OpenPresets){NavigateProgressionGoal(g);return true;}Feedback(p.ExecuteProgressionGoal(g.ActionIdentity,session.IsInCamp),"成长操作已保存");}
            if(NavigationButton(new Rect(w.center.x+6,w.y+267,(w.width-68)*.5f,42),g.RequiredAdventureTier>0?"定位副本传送门":"定位营地服务",gold))
            {
                progressionGoalsOpen=false;NavigateProgressionGoal(g);return true;
            }
            if(NavigationButton(new Rect(w.x+28,w.yMax-64,w.width-56,40),"返回冒险",jade)){progressionGoalsOpen=false;ClosePanel();}
            return true;
        }
    }
}
