using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private Vector2 attachmentScroll;
        private void DrawAttachmentWorkshop()
        {
            if(session.ActiveHubNpc!=HubNpcKind.Merchant){merchantExchangeOpen=false;ClosePanel();return;}
            var p=session.Progression;
            Rect w=Modal(Mathf.Min(width-24,980),Mathf.Min(height-24,660),"商人 · 机制兑换","独立挂载 · 换装沿用 · 同机制仅一件 · 星烬碎片可升级");
            if(NavigationButton(new Rect(w.xMax-190,w.y+20,160,34),"返回商人",jade)){merchantExchangeOpen=false;panel=Panel.Inventory;return;}
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
                    Text(new Rect(16,y+168,cw-32,36),"已拥有 · 镶嵌、更换与升级请到铁匠",14,jade,true);
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
            else if(goal.Identity.Contains("practice")||session.Progression.Profile.progressionGoal==ProgressionGoalKind.ClassTutorial||session.Progression.Profile.progressionGoal==ProgressionGoalKind.CombatTrial)OpenProgressionGoals();
            else NavigateMerchantExchange();
            BlockUITransition();
        }
    }
}
