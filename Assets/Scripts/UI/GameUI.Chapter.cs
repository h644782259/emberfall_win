using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private Vector2 chapterScroll,chapterResultScroll;
        private object chapterResultScrollOwner;
        private bool chapterStoryExpanded,chapterRulesExpanded;
        private GameProfile chapterSelectionOwner;
        private string chapterEntryError;
        private bool ChapterSelectionIsCurrent()
        {return panel==Panel.Chapter&&session.OpenChapterSelectionAllowed&&ReferenceEquals(chapterSelectionOwner,session.Progression.Profile);}
        private bool OpenChapterSelection()
        {
            if(UITransitionBlocked||!session.OpenChapterSelectionAllowed)return false;
            chapterSelectionOwner=session.Progression.Profile;chapterEntryError=null;chapterScroll=Vector2.zero;chapterStoryExpanded=false;chapterRulesExpanded=false;
            if(!ChapterProgression.IsUnlocked(chapterSelectionOwner,session.SelectedChapterNode))session.SelectedChapterNode=ChapterNode.ForestCourt;
            if(!ChapterProgression.CanEnter(chapterSelectionOwner,session.SelectedChapterNode,session.SelectedChapterDifficulty))session.SelectedChapterDifficulty=ChapterDifficulty.Normal;
            session.SelectedChapterTier=Mathf.Clamp(session.SelectedChapterTier,1,session.Progression.HighestUnlockedAdventureTier);
            CancelHotbarPointer();CancelMobileScroll();panel=Panel.Chapter;session.SetUIBlocking(true);BlockUITransition();return true;
        }
        private bool SelectChapterNode(ChapterNode node)
        {
            if(!ChapterSelectionIsCurrent()||!ChapterProgression.IsUnlocked(chapterSelectionOwner,node))return false;
            session.SelectedChapterNode=node;session.SelectedChapterDifficulty=ChapterDifficulty.Normal;session.SelectedChapterTactic=-1;
            chapterScroll=Vector2.zero;chapterStoryExpanded=false;chapterRulesExpanded=false;chapterEntryError=null;CancelMobileScroll();BlockUITransition();return true;
        }
        private bool SelectChapterDifficulty(ChapterDifficulty difficulty)
        {
            if(!ChapterSelectionIsCurrent()||!ChapterProgression.CanEnter(chapterSelectionOwner,session.SelectedChapterNode,difficulty))return false;
            session.SelectedChapterDifficulty=difficulty;chapterEntryError=null;BlockUITransition();return true;
        }
        private void ChangeChapterTier(int delta)
        {
            if(!ChapterSelectionIsCurrent())return;
            session.SelectedChapterTier=Mathf.Clamp(session.SelectedChapterTier+(delta<0?-1:delta>0?1:0),1,session.Progression.HighestUnlockedAdventureTier);
            // The stepper stays in place; only its values change, with no screen transition.
        }
        private void SetChapterLimitedHealing(bool limited)
        {if(!ChapterSelectionIsCurrent())return;session.SelectedChapterLimitedHealing=limited;BlockUITransition();}
        private bool ConfirmSelectedChapter()
        {
            if(!ChapterSelectionIsCurrent()||!ChapterProgression.CanEnter(chapterSelectionOwner,session.SelectedChapterNode,session.SelectedChapterDifficulty))return false;
            if(!session.ConfirmChapterEnter())
            {chapterEntryError=string.IsNullOrEmpty(session.Progression.LastError)?"暂时无法进入，请确认营地状态后重试。":session.Progression.LastError;BlockUITransition();return false;}
            panel=Panel.None;chapterSelectionOwner=null;chapterEntryError=null;CancelMobileScroll();session.SetUIBlocking(false);BlockUITransition();return true;
        }
        private bool CloseChapterSelection()
        {
            if(panel!=Panel.Chapter)return false;
            panel=Panel.None;chapterSelectionOwner=null;chapterEntryError=null;CancelMobileScroll();session.SetUIBlocking(false);BlockUITransition();return true;
        }
        private void OpenChapterExchange()
        {
            if(!ChapterSelectionIsCurrent())return;
            chapterSelectionOwner=null;chapterEntryError=null;campTab=1;panel=Panel.Camp;CancelMobileScroll();BlockUITransition();
        }
        private bool RetryChapterSettlement()
        {if(!session.ChapterFinished||!session.ChapterRewardPending)return false;bool saved=session.TrySettleChapterReward();BlockUITransition();return saved;}
        private void ReturnFromChapter()
        {if(!session.ChapterFinished)return;if(session.IsDead)session.Respawn();else session.ReturnToCamp();BlockUITransition();}
        private bool ReturnAndSelectNextChapter()
        {
            if(!session.ChapterFinished||session.ChapterRewardPending||session.ChapterRun.Failed)return false;
            int next=(int)session.ActiveChapterNode+1;
            if(next>=3||!ChapterProgression.IsUnlocked(session.Progression.Profile,(ChapterNode)next))return false;
            session.ReturnToCamp();
            if(!session.OpenChapterSelectionAllowed){BlockUITransition();return false;}
            session.SelectedChapterNode=(ChapterNode)next;session.SelectedChapterDifficulty=ChapterDifficulty.Normal;
            return OpenChapterSelection();
        }
        private void DrawChapterSelection()
        {
            if(!ChapterSelectionIsCurrent()){CloseChapterSelection();return;}
            bool mobile=MobileControls.Active;float u=mobile?TouchRatio:1;
            var layout=ChapterPanelGeometry();
            DrawChapterFrame(layout,u,"星路纪事",HubNpcServiceSubtitle("选择一段星路，整备后出发 · 已解锁节点可重复挑战"));
            var profile=session.Progression.Profile;var node=session.SelectedChapterNode;var difficulty=session.SelectedChapterDifficulty;
            // Node identity and completion remain visible while only details scroll.
            float cardWidth=(layout.Body.Width-16)/3;
            for(int i=0;i<3;i++)
            {
                var choice=(ChapterNode)i;bool unlocked=ChapterProgression.IsUnlocked(profile,choice);
                int highest=ChapterProgression.HighestCompletedDifficulty(profile,choice);
                Rect card=ChapterRect(new MobilePanelLayout.Area(layout.Body.X+i*(cardWidth+8),layout.Body.Y,cardWidth,48),u);
                if(ChapterChoice(card,ChapterDefinition.Get(choice).Name,choice==node,unlocked,u)){SelectChapterNode(choice);return;}
                DrawChapterSymbol(new Rect(card.x+8*u,card.yMax+8*u,12*u,12*u),choice,unlocked?jade:muted);
                string status=!unlocked?ChapterEntryPresentation.UnlockHint(choice):highest<0?"尚未通关 · 从普通开始":"已通关 · "+ChapterEntryPresentation.DifficultyName((ChapterDifficulty)highest);
                Text(new Rect(card.x+26*u,card.yMax+4*u,card.width-30*u,32*u),status,Mathf.RoundToInt(11*u),muted,false,true);
            }
            Rect body=ChapterRect(new MobilePanelLayout.Area(layout.Body.X,layout.Body.Y+88,layout.Body.Width,layout.Body.Height-88),u);
            float contentWidth=layout.Body.Width-18;
            bool columns=contentWidth>=720&&layout.Height>=440;
            float settingWidth=columns?320:contentWidth,infoX=columns?settingWidth+16:0,infoWidth=columns?contentWidth-infoX:contentWidth;
            string encounter=ChapterDefinition.DifficultyMechanic(node,difficulty);
            if(!string.IsNullOrEmpty(session.SelectedChapterLineupPreview))encounter+="\n"+session.SelectedChapterLineupPreview;
            string reward=ChapterEntryPresentation.RewardBreakdown(profile,node,difficulty,session.SelectedChapterTier);
            string next=node==ChapterNode.StarPlatform?"通关整章 → 挑战更高阶数":node==ChapterNode.ForestCourt?"首次通关 → 解锁赤岩断供":"首次通关 → 解锁星台封印";
            if(node==ChapterNode.StarPlatform&&!profile.firstClearRewardClaimed&&!profile.pendingFirstClearReward)next+="\n整章首通后可领取一次核心";
            string story=ChapterEntryPresentation.Story(node);
            string rules=ChapterEntryPresentation.Preview(profile,node,difficulty,session.SelectedChapterTier,session.SelectedChapterLimitedHealing);
            float errorH=string.IsNullOrEmpty(chapterEntryError)?0:ChapterCopyHeight(chapterEntryError,contentWidth-24,u,13)+20;
            float settingsH=348;
            float goalH=ChapterCopyHeight(ChapterDefinition.Get(node).Mechanic,infoWidth-32,u,15);
            float encounterH=ChapterCopyHeight(encounter,infoWidth-32,u,13);
            float rewardH=ChapterCopyHeight(reward,infoWidth-32,u,12);
            float nextH=ChapterCopyHeight(next,infoWidth-32,u,12);
            float infoH=152+goalH+encounterH+rewardH+nextH;
            float mainH=columns?Mathf.Max(settingsH,infoH):settingsH+16+infoH;
            bool tactics=RunChoices.ChapterTacticsAvailable(profile,node);
            float tacticHeight=0;
            if(tactics)
            {
                tacticHeight=96;
                for(int i=0;i<3;i++)tacticHeight+=60+ChapterCopyHeight(RunChoices.Description(RunChoices.ChapterTactic(profile,mobile,i)),contentWidth-32,u,12);
            }
            float optionalH=60+(chapterStoryExpanded?ChapterCopyHeight(story,contentWidth-32,u,14)+24:0)+
                (chapterRulesExpanded?ChapterCopyHeight(rules,contentWidth-32,u,13)+24:0);
            float total=errorH+mainH+16+tacticHeight+optionalH;
            chapterScroll=BeginTouchScroll("chapter-entry",body,chapterScroll,new Rect(0,0,contentWidth*u,Mathf.Max(body.height/u,total)*u),false,total>body.height/u);
            if(errorH>0)
            {
                Fill(new Rect(0,0,contentWidth*u,errorH*u),new Color(.22f,.10f,.08f));
                Text(new Rect(12*u,8*u,(contentWidth-24)*u,(errorH-16)*u),chapterEntryError,Mathf.RoundToInt(13*u),gold,false,true);
            }
            float y=errorH;
            ChapterSurface(new Rect(0,y*u,settingWidth*u,settingsH*u),u);
            Text(new Rect(16*u,(y+14)*u,(settingWidth-32)*u,24*u),"挑战设置",Mathf.RoundToInt(18*u),pale,true);
            float choiceW=(settingWidth-48)/3;
            for(int i=0;i<3;i++)
            {
                var choice=(ChapterDifficulty)i;bool allowed=ChapterProgression.CanEnter(profile,node,choice);
                if(ChapterChoice(new Rect((16+i*(choiceW+8))*u,(y+50)*u,choiceW*u,48*u),ChapterEntryPresentation.DifficultyName(choice),choice==difficulty,allowed,u))
                {SelectChapterDifficulty(choice);EndTouchScroll();return;}
            }
            string difficultyHint=ChapterEntryPresentation.DifficultyHint(difficulty,true);
            if(difficulty==ChapterDifficulty.Normal&&!ChapterProgression.CanEnter(profile,node,ChapterDifficulty.Hard))difficultyHint+="\n通关普通解锁困难，再通关困难解锁英雄";
            Text(new Rect(16*u,(y+108)*u,(settingWidth-32)*u,36*u),difficultyHint,Mathf.RoundToInt(12*u),muted,false,true);
            Text(new Rect(16*u,(y+150)*u,100*u,24*u),"挑战阶数",Mathf.RoundToInt(13*u),muted);
            Text(new Rect(16*u,(y+178)*u,100*u,36*u),"第 "+session.SelectedChapterTier+" 阶",Mathf.RoundToInt(22*u),pale,true);
            if(Button(new Rect((settingWidth-128)*u,(y+164)*u,48*u,48*u),"−",jade,session.SelectedChapterTier>1)){ChangeChapterTier(-1);EndTouchScroll();return;}
            if(Button(new Rect((settingWidth-72)*u,(y+164)*u,48*u,48*u),"+",jade,session.SelectedChapterTier<session.Progression.HighestUnlockedAdventureTier)){ChangeChapterTier(1);EndTouchScroll();return;}
            Text(new Rect(16*u,(y+218)*u,(settingWidth-32)*u,22*u),"当前最多第 "+session.Progression.HighestUnlockedAdventureTier+" 阶 · 与难度独立",Mathf.RoundToInt(11*u),muted);
            float healingW=(settingWidth-40)/2;
            if(ChapterChoice(new Rect(16*u,(y+250)*u,healingW*u,48*u),"携带药剂",!session.SelectedChapterLimitedHealing,true,u))
            {SetChapterLimitedHealing(false);EndTouchScroll();return;}
            if(ChapterChoice(new Rect((24+healingW)*u,(y+250)*u,healingW*u,48*u),"限疗挑战",session.SelectedChapterLimitedHealing,true,u))
            {SetChapterLimitedHealing(true);EndTouchScroll();return;}
            Text(new Rect(16*u,(y+307)*u,(settingWidth-32)*u,32*u),session.SelectedChapterLimitedHealing?"初始 3 次治疗充能，用完无法再治疗":"使用背包药剂 · 当前携带 "+profile.potions+" 瓶",Mathf.RoundToInt(12*u),muted,false,true);
            float infoY=columns?y:y+settingsH+16;
            ChapterSurface(new Rect(infoX*u,infoY*u,infoWidth*u,infoH*u),u);
            float at=infoY+14;
            Text(new Rect((infoX+16)*u,at*u,(infoWidth-32)*u,24*u),"本次挑战 · "+ChapterDefinition.Get(node).Name,Mathf.RoundToInt(18*u),pale,true);at+=36;
            Text(new Rect((infoX+16)*u,at*u,(infoWidth-32)*u,goalH*u),ChapterDefinition.Get(node).Mechanic,Mathf.RoundToInt(15*u),jade,true,true);at+=goalH+10;
            Text(new Rect((infoX+16)*u,at*u,(infoWidth-32)*u,encounterH*u),encounter,Mathf.RoundToInt(13*u),muted,false,true);at+=encounterH+18;
            Fill(new Rect((infoX+16)*u,at*u,(infoWidth-32)*u,1*u),new Color(.22f,.32f,.39f));at+=14;
            Text(new Rect((infoX+16)*u,at*u,(infoWidth-32)*u,30*u),"+ "+ChapterEntryPresentation.RewardMaterials(profile,node,difficulty,session.SelectedChapterTier)+" 碎片",Mathf.RoundToInt(23*u),gold,true);at+=38;
            Text(new Rect((infoX+16)*u,at*u,(infoWidth-32)*u,rewardH*u),reward,Mathf.RoundToInt(12*u),muted,false,true);at+=rewardH+12;
            Text(new Rect((infoX+16)*u,at*u,(infoWidth-32)*u,nextH*u),next,Mathf.RoundToInt(12*u),pale,false,true);
            y+=mainH+16;
            if(tactics)
            {
                Text(new Rect(16*u,y*u,(contentWidth-32)*u,26*u),"出发战术 · 可选一项",Mathf.RoundToInt(16*u),pale,true);y+=36;
                if(ChapterChoice(new Rect(0,y*u,contentWidth*u,48*u),"不携带战术",session.SelectedChapterTactic<0,true,u))
                {session.SelectedChapterTactic=-1;BlockUITransition();EndTouchScroll();return;}
                y+=60;
                for(int i=0;i<3;i++)
                {
                    var tactic=RunChoices.ChapterTactic(profile,mobile,i);
                    if(ChapterChoice(new Rect(0,y*u,contentWidth*u,48*u),RunChoices.Name(tactic),session.SelectedChapterTactic==i,true,u))
                    {session.SelectedChapterTactic=i;BlockUITransition();EndTouchScroll();return;}
                    y+=54;string description=RunChoices.Description(tactic);
                    float dh=ChapterCopyHeight(description,contentWidth-32,u,12);
                    Text(new Rect(16*u,y*u,(contentWidth-32)*u,dh*u),description,Mathf.RoundToInt(12*u),muted,false,true);y+=dh+6;
                }
            }
            float optionalW=(contentWidth-8)/2;
            if(NavigationButton(new Rect(0,y*u,optionalW*u,48*u), chapterStoryExpanded?"收起故事线索":"展开故事线索", muted)){chapterStoryExpanded=!chapterStoryExpanded;BlockUITransition();EndTouchScroll();return;}
            if(NavigationButton(new Rect((optionalW+8)*u,y*u,optionalW*u,48*u), chapterRulesExpanded?"收起详细规则":"奖励与解锁规则", muted)){chapterRulesExpanded=!chapterRulesExpanded;BlockUITransition();EndTouchScroll();return;}
            y+=60;
            if(chapterStoryExpanded)ChapterParagraph(ref y,contentWidth,story,u,14);
            if(chapterRulesExpanded)ChapterParagraph(ref y,contentWidth,rules,u,13);
            EndTouchScroll();
            if(NavigationButton(ChapterRect(layout.FooterButton(0,3),u), "返回营地", muted)){CloseChapterSelection();return;}
            if(NavigationButton(ChapterRect(layout.FooterButton(1,3),u), "机制兑换", jade)){OpenChapterExchange();return;}
            if(PrimaryButton(ChapterRect(layout.FooterButton(2,3),u), "进入 "+ChapterDefinition.Get(node).Name, gold, ChapterProgression.CanEnter(profile,node,difficulty), null, true))
            {ConfirmSelectedChapter();return;}
        }
        private float ChapterCopyHeight(string copy,float logicalWidth,float u,int size)
        {return Mathf.Max(18,Style(Mathf.RoundToInt(size*u),false,true).CalcHeight(new GUIContent(copy),logicalWidth*u)/u);}
        private void ChapterParagraph(ref float y,float w,string copy,float u,int size)
        {
            float h=ChapterCopyHeight(copy,w-32,u,size);
            ChapterSurface(new Rect(0,y*u,w*u,(h+24)*u),u);
            Text(new Rect(16*u,(y+12)*u,(w-32)*u,h*u),copy,Mathf.RoundToInt(size*u),muted,false,true);y+=h+24;
        }
        private void ChapterSurface(Rect r,float u)
        {
            Fill(r,new Color(.045f,.075f,.105f,1));
            Border(r,new Color(.23f,.34f,.43f,.5f));
            Fill(new Rect(r.x,r.y,3*u,r.height),new Color(.24f,.46f,.49f,.55f));
        }
        private bool ChapterChoice(Rect r,string label,bool selected,bool enabled,float u)
        {
            return DrawButton(r, enabled?label:label+" · 锁定", selected ? ButtonRole.SelectedTab : ButtonRole.Tab,
                enabled, null, Mathf.RoundToInt((enabled?15:12)*u));
        }
        private MobilePanelLayout ChapterPanelGeometry()
        {return MobileControls.Active?MobilePanelGeometry():new MobilePanelLayout(Mathf.Min(960,width),Mathf.Min(660,height));}
        private Rect ChapterRect(MobilePanelLayout.Area area,float u)
        {var layout=ChapterPanelGeometry();float x=MobileControls.Active?0:(width-layout.Width*u)*.5f,y=MobileControls.Active?0:(height-layout.Height*u)*.5f;
            return new Rect(x+area.X*u,y+area.Y*u,area.Width*u,area.Height*u);}
        private void DrawChapterFrame(MobilePanelLayout layout,float u,string title,string subtitle)
        {
            Fill(new Rect(0,0,width,height),new Color(.018f,.031f,.048f,1));
            Rect frame=ChapterRect(new MobilePanelLayout.Area(0,0,layout.Width,layout.Height),u);
            Fill(frame,new Color(.025f,.045f,.068f,1));
            Border(frame,new Color(.24f,.37f,.44f,.55f));
            DrawChapterSymbol(ChapterRect(new MobilePanelLayout.Area(16,16,14,14),u),session.ChapterFinished?session.ActiveChapterNode:session.SelectedChapterNode,gold);
            Text(ChapterRect(new MobilePanelLayout.Area(36,10,layout.Width-52,28),u),title,Mathf.RoundToInt(22*u),pale,true);
            Text(ChapterRect(new MobilePanelLayout.Area(16,40,layout.Width-32,18),u),subtitle,Mathf.RoundToInt(12*u),muted);
        }
        // Shared procedural marks avoid platform font dependencies: tree, stepped rock, star.
        private void DrawChapterSymbol(Rect r,ChapterNode node,Color color)
        {
            if(node==ChapterNode.ForestCourt)
            {Fill(new Rect(r.x+r.width*.45f,r.y,r.width*.1f,r.height),color);Fill(new Rect(r.x,r.y+r.height*.25f,r.width,r.height*.15f),color);Fill(new Rect(r.x+r.width*.15f,r.y+r.height*.55f,r.width*.7f,r.height*.15f),color);}
            else if(node==ChapterNode.Redrock)
            {Fill(new Rect(r.x,r.y+r.height*.4f,r.width,r.height*.6f),color);Fill(new Rect(r.x+r.width*.25f,r.y,r.width*.5f,r.height*.5f),color);}
            else
            {Fill(new Rect(r.x+r.width*.4f,r.y,r.width*.2f,r.height),color);Fill(new Rect(r.x,r.y+r.height*.4f,r.width,r.height*.2f),color);}
        }
        private void DrawChapterResult()
        {
            if(!ReferenceEquals(chapterResultScrollOwner,session.ChapterRun))
            {chapterResultScrollOwner=session.ChapterRun;chapterResultScroll=Vector2.zero;CancelMobileScroll();}
            float u=MobileControls.Active?TouchRatio:1;var layout=ChapterPanelGeometry();
            if(!session.ChapterResultReady)
            {
                // Preserve the battlefield while the already-dead boss's actual visual retires.
                Rect badge=new Rect((width-300*u)*.5f,14*u,300*u,42*u);
                Fill(badge,new Color(.025f,.06f,.08f,.85f));Text(badge,session.ChapterRewardPending?"节点完成 · 奖励待保存":"节点完成 · 奖励已保存",Mathf.RoundToInt(14*u),jade,true);
                if(NavigationButton(new Rect((width-180*u)*.5f,height-62*u,180*u,48*u), "继续 · 查看结果", jade)){session.ContinueChapterResult();BlockUITransition();}
                return;
            }
            bool failed=session.ChapterRun==null||session.ChapterRun.Failed,pending=session.ChapterRewardPending;
            DrawChapterFrame(layout,u,failed?"本次星路止步":pending?"结算待保存":"星路线索已记录",ChapterDefinition.Get(session.ActiveChapterNode).Name);
            string copy=ChapterEntryPresentation.Result(session.ChapterResult);
            if(!string.IsNullOrEmpty(session.Progression.LastError))copy=session.Progression.LastError+"\n\n"+copy;
            float h=Style(Mathf.RoundToInt(16*u),false,true).CalcHeight(new GUIContent(copy),(layout.Body.Width-26)*u)+16*u;
            chapterResultScroll=BeginTouchScroll("chapter-result",ChapterRect(layout.Body,u),chapterResultScroll,new Rect(0,0,(layout.Body.Width-16)*u,Mathf.Max(layout.Body.Height*u,h)));
            Text(new Rect(8*u,8*u,(layout.Body.Width-26)*u,h),copy,Mathf.RoundToInt(16*u),pale,false,true);EndTouchScroll();
            if(pending&&Button(ChapterRect(layout.FooterButton(0,2),u),"重试保存结算",gold)){RetryChapterSettlement();return;}
            if(failed&&Button(ChapterRect(layout.FooterButton(0,2),u),"原条件重试",gold,session.CanRetryChapter)){session.RetryFailedChapter();BlockUITransition();return;}
            bool next=!failed&&!pending&&(int)session.ActiveChapterNode<2;
            if(next&&Button(ChapterRect(layout.FooterButton(1,2),u),"下一节点 · 回营准备",gold)){ReturnAndSelectNextChapter();return;}
            if(NavigationButton(ChapterRect(layout.FooterButton(pending||failed?1:0,pending||next||failed?2:1),u), "返回营地", jade)){ReturnFromChapter();return;}
        }
    }
}
