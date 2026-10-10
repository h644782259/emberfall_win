using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private int achievementCategory=-1;
        private Vector2 achievementScroll;
        private void DrawAchievements(Rect area,float u)
        {
            var p=session.Progression;string[] filters={"全部","成长","战斗","探索","收集","打造"};
            float filterWidth=Mathf.Min(96*u,area.width/6);
            for(int i=0;i<filters.Length;i++)
            {
                Rect tab=new Rect(area.x+i*filterWidth,area.y,filterWidth-4*u,32*u);
                if(TabButton(tab,filters[i],achievementCategory==i-1)&&achievementCategory!=i-1){achievementCategory=i-1;achievementScroll=Vector2.zero;}
                bool ready=false;foreach(var entry in ProgressionService.Achievements)if((i==0||entry.Category==i-1)&&!p.AchievementClaimed(entry.Id)&&entry.Progress(p.Profile)>=entry.Target){ready=true;break;}
                if(ready)Text(new Rect(tab.xMax-13*u,tab.y,12*u,16*u),"●",Mathf.RoundToInt(10*u),new Color(1,.22f,.2f),true);
            }
            var entries=new System.Collections.Generic.List<AchievementDefinition>();
            foreach(var a in ProgressionService.Achievements)if(achievementCategory<0||a.Category==achievementCategory)entries.Add(a);
            entries.Sort((a,b)=>{int x=p.AchievementClaimed(a.Id)?2:a.Progress(p.Profile)>=a.Target?0:1;int y=p.AchievementClaimed(b.Id)?2:b.Progress(p.Profile)>=b.Target?0:1;return x!=y?x.CompareTo(y):System.Array.IndexOf(ProgressionService.Achievements,a).CompareTo(System.Array.IndexOf(ProgressionService.Achievements,b));});
            Rect viewport=new Rect(area.x,area.y+42*u,area.width,area.height-42*u);
            int columns=area.width/u>=720?2:1;float gap=10*u,cw=(viewport.width-18*u-(columns-1)*gap)/columns,rowHeight=110*u;
            achievementScroll=BeginTouchScroll("achievements",viewport,achievementScroll,new Rect(0,0,viewport.width-18*u,Mathf.Max(viewport.height,((entries.Count+columns-1)/columns)*rowHeight)));
            for(int i=0;i<entries.Count;i++)
            {
                var a=entries[i];bool claimed=p.AchievementClaimed(a.Id);int progress=Mathf.Min(a.Target,a.Progress(p.Profile));bool ready=progress>=a.Target&&!claimed;
                Rect tile=new Rect((i%columns)*(cw+gap),(i/columns)*rowHeight,cw,100*u);Fill(tile,card);Border(tile,ready?gold:muted*.3f);
                Text(new Rect(tile.x+10*u,tile.y+6*u,cw-20*u,26*u),a.Title,Mathf.RoundToInt(14*u),claimed?muted:pale,true);
                float barWidth=cw-136*u;
                Fill(new Rect(tile.x+10*u,tile.y+43*u,barWidth,5*u),muted*.3f);Fill(new Rect(tile.x+10*u,tile.y+43*u,barWidth*progress/a.Target,5*u),jade);
                Text(new Rect(tile.x+10*u,tile.y+49*u,barWidth,18*u),progress+" / "+a.Target,Mathf.RoundToInt(10*u),muted);
                DrawIcon(new Rect(tile.x+10*u,tile.y+72*u,18*u,18*u),UIIconAtlas.Utility("coin"),gold);
                Text(new Rect(tile.x+32*u,tile.y+70*u,68*u,22*u),"+"+a.Gold,Mathf.RoundToInt(12*u),gold,true);
                DrawIcon(new Rect(tile.x+100*u,tile.y+72*u,18*u,18*u),UIIconAtlas.Utility("shard"),jade);
                Text(new Rect(tile.x+122*u,tile.y+70*u,60*u,22*u),"+"+a.Shards,Mathf.RoundToInt(12*u),jade,true);
                if(PrimaryButton(new Rect(tile.xMax-110*u,tile.y+42*u,100*u,44*u),claimed?"已领取":ready?"领取奖励":"未完成",gold,ready))
                    Feedback(p.ClaimAchievement(a.Id),"成就奖励：+"+a.Gold+"金币 · +"+a.Shards+"碎片");
            }
            EndTouchScroll();
        }
        private bool progressionGoalsOpen;
        private Vector2 progressionGoalScroll,progressionGoalHeaderScroll;
        private ProgressionService progressionGoalOwner;
        private string progressionGoalCharacter;
        private void OpenProgressionGoals()
        {
            if(!CanSwitchFunction)return;PrepareFunctionSwitch();
            panel=Panel.Camp;session.SetUIBlocking(true);
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
            progressionGoalsOpen=false;panel=Panel.None;session.SetUIBlocking(false);CancelMobileScroll();BlockUITransition();return true;
        }
        private string CurrentProgressionGoalStatus(int runMaterials=0)
        {return session.Progression.ProgressionGoalStatus(runMaterials,session.IsInCamp);}
        private bool DrawProgressionGoalSurface()
        {
            ReconcileProgressionGoalSurface();if(!progressionGoalsOpen)return false;
            float u=MobileControls.Active?TouchRatio:1;var l=new MobileDialogLayout(width/u,height/u);
            blockedRects.Add(new Rect(0,0,width,height));
            Box(BuildPlanRect(l.Frame,u),jade,false);
            Text(BuildPlanRect(l.Header,u),"成就",Mathf.RoundToInt(21*u),pale,true);
            int available=session.Progression.ClaimableAchievements;
            if(DrawButton(new Rect((l.Frame.X+l.Frame.Width-222)*u,(l.Frame.Y+12)*u,154*u,32*u),"一键领取"+(available>0?" · "+available:""),ButtonRole.Primary,available>0,fontSize:Mathf.RoundToInt(15*u)))Feedback(session.Progression.ClaimAllAchievements(),"成就奖励已领取");
            DrawAchievements(BuildPlanRect(l.Body,u),u);
            if(PopupCloseButton(new Rect((l.Frame.X+l.Frame.Width-52)*u,(l.Frame.Y+12)*u,40*u,32*u)))CloseProgressionGoalSurface();
            return true;
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
        {return item.name+" · Lv"+item.level+" · "+GameBalance.RarityName(item.rarity);}
        private void GoalUnavailable(ref float y,float w,float u,string text,bool draw)
        {if(draw)Text(new Rect(8*u,y*u,(w-16)*u,40*u),text,Mathf.RoundToInt(13*u),muted,false,true);y+=48;}
    }
}
