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
        {return (panel==Panel.Chapter||session.DungeonSelectionOpen&&adventureChapterSelected)&&session.OpenChapterSelectionAllowed&&ReferenceEquals(chapterSelectionOwner,session.Progression.Profile);}
        private bool OpenChapterSelection()
        {
            if(UITransitionBlocked||!session.OpenChapterSelectionAllowed)return false;
            chapterSelectionOwner=session.Progression.Profile;chapterEntryError=null;chapterScroll=Vector2.zero;chapterStoryExpanded=false;chapterRulesExpanded=false;
            if(!ChapterProgression.IsUnlocked(chapterSelectionOwner,session.SelectedChapterNode))session.SelectedChapterNode=ChapterNode.ForestCourt;
            if(!ChapterProgression.CanEnter(chapterSelectionOwner,session.SelectedChapterNode,session.SelectedChapterDifficulty))session.SelectedChapterDifficulty=ChapterDifficulty.Heroic;
            session.SelectedChapterTier=Mathf.Clamp(session.SelectedChapterTier,1,session.Progression.UnlockedChapterTier(session.SelectedChapterNode,session.SelectedChapterDifficulty));
            CancelHotbarPointer();CancelMobileScroll();panel=Panel.Chapter;session.SetUIBlocking(true);BlockUITransition();return true;
        }
        private bool SelectChapterNode(ChapterNode node)
        {
            if(!ChapterSelectionIsCurrent()||!ChapterProgression.IsUnlocked(chapterSelectionOwner,node))return false;
            session.SelectedChapterNode=node;session.SelectedChapterDifficulty=ChapterDifficulty.Heroic;session.SelectedChapterTactic=-1;
            chapterScroll=Vector2.zero;chapterStoryExpanded=false;chapterRulesExpanded=false;chapterEntryError=null;CancelMobileScroll();return true;
        }
        private bool SelectChapterDifficulty(ChapterDifficulty difficulty)
        {
            if(!ChapterSelectionIsCurrent()||!ChapterProgression.CanEnter(chapterSelectionOwner,session.SelectedChapterNode,difficulty))return false;
            session.SelectedChapterDifficulty=difficulty;chapterEntryError=null;return true;
        }
        private void ChangeChapterTier(int delta)
        {
            if(!ChapterSelectionIsCurrent())return;
            session.SelectedChapterTier=Mathf.Clamp((int)System.Math.Max(1,System.Math.Min(int.MaxValue,(long)session.SelectedChapterTier+(delta<0?-1:delta>0?1:0))),1,session.Progression.UnlockedChapterTier(session.SelectedChapterNode,session.SelectedChapterDifficulty));
            // The stepper stays in place; only its values change, with no screen transition.
        }
        private void SetChapterLimitedHealing(bool limited)
        {if(!ChapterSelectionIsCurrent())return;session.SelectedChapterLimitedHealing=limited;}
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
            session.SelectedChapterNode=(ChapterNode)next;session.SelectedChapterDifficulty=ChapterDifficulty.Heroic;
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
                int highest=ChapterProgression.CompletedTier(profile,choice,ChapterDifficulty.Heroic);
                Rect card=ChapterRect(new MobilePanelLayout.Area(layout.Body.X+i*(cardWidth+8),layout.Body.Y,cardWidth,48),u);
                if(ChapterChoice(card,ChapterDefinition.Get(choice).Name,choice==node,unlocked,u)){SelectChapterNode(choice);return;}
                DrawChapterSymbol(new Rect(card.x+8*u,card.yMax+8*u,12*u,12*u),choice,unlocked?jade:muted);
                string status=!unlocked?ChapterEntryPresentation.UnlockHint(choice):highest<=0?"尚未通关":"最高通关 · 第 "+highest+" 阶";
                Text(new Rect(card.x+26*u,card.yMax+4*u,card.width-30*u,32*u),status,Mathf.RoundToInt(11*u),muted,false,true);
            }
            Rect body=ChapterRect(new MobilePanelLayout.Area(layout.Body.X,layout.Body.Y+88,layout.Body.Width,layout.Body.Height-144),u);
            DrawChapterEntryDetails(body,u);
            DrawChapterEntryControls(ChapterRect(new MobilePanelLayout.Area(layout.Footer.X,layout.Footer.Y-56,340,48),u),u);
            if(NavigationButton(ChapterRect(layout.FooterButton(0,2),u), "返回副本选择", muted)){CloseChapterSelection();session.EnterDungeon();return;}
            if(PrimaryButton(ChapterRect(layout.FooterButton(1,2),u), "进入 "+ChapterDefinition.Get(node).Name, gold, ChapterProgression.CanEnter(profile,node,difficulty), null, true))
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
        private void DrawChapterEntryDetails(Rect body,float u)
        {
            var profile=session.Progression.Profile;var node=session.SelectedChapterNode;
            int level=AdventureRewardRules.DungeonLevel(session.SelectedChapterTier),mode=(int)node;
            float w=body.width/u-18;
            string mechanic=ChapterDefinition.Get(node).Mechanic+"\n"+ChapterDefinition.DifficultyMechanic(node,session.SelectedChapterDifficulty);
            float descriptionHeight=Style(Mathf.RoundToInt(14*u),false,true).CalcHeight(new GUIContent(mechanic),(w-24)*u)/u+20;
            float rewardsHeight=DrawEntryRewardPreviews(w,u,mode,session.SelectedChapterTier,true,false);
            float imageHeight=Mathf.Clamp(w*.5f,96,MobileControls.Active?160:216),descriptionY=56+imageHeight;
            float total=descriptionY+descriptionHeight+rewardsHeight+48;
            chapterScroll=BeginTouchScroll("chapter-entry",body,chapterScroll,new Rect(0,0,w*u,Mathf.Max(body.height,total*u)));
            DrawChapterSymbol(new Rect(12*u,14*u,28*u,28*u),node,gold);
            Text(new Rect(50*u,8*u,(w-58)*u,30*u),ChapterDefinition.Get(node).Name,Mathf.RoundToInt(20*u),pale,true);
            DrawDungeonEntryArtwork(new Rect(12*u,44*u,(w-24)*u,imageHeight*u),5+(int)node);
            Text(new Rect(12*u,descriptionY*u,(w-24)*u,descriptionHeight*u),mechanic,Mathf.RoundToInt(14*u),muted,false,true);
            float y=descriptionY+descriptionHeight;
            entryRewardViewport=body;entryRewardContentOrigin=new Vector2(body.x-chapterScroll.x,body.y+y*u-chapterScroll.y);
            GUI.BeginGroup(new Rect(0,y*u,w*u,rewardsHeight*u));DrawEntryRewardPreviews(w,u,mode,session.SelectedChapterTier,true,true);GUI.EndGroup();
            if(!string.IsNullOrEmpty(chapterEntryError))Text(new Rect(8*u,(y+rewardsHeight)*u,(w-16)*u,48*u),"暂时无法进入，请稍后重试。",Mathf.RoundToInt(13*u),gold,false,true);
            EndTouchScroll();
        }
        private void DrawChapterEntryControls(Rect area,float u)
        {
            var node=session.SelectedChapterNode;
            int tier=session.SelectedChapterTier,maximum=session.Progression.UnlockedChapterTier(node,ChapterDifficulty.Heroic);
            float button=Mathf.Min(44*u,area.width*.24f),label=Mathf.Max(0,area.width-button*2);
            float x=area.x;
            if(Button(new Rect(x,area.y,button,area.height),"−",jade,tier>1))ChangeChapterTier(-1);
            Text(new Rect(x+button,area.y,label,area.height),"第 "+tier+" 阶",Mathf.RoundToInt(13*u),gold,true,false,TextAnchor.MiddleCenter);
            if(Button(new Rect(x+button+label,area.y,button,area.height),"+",jade,tier<maximum))ChangeChapterTier(1);
        }
        private void DrawInlineChapterEntry(Rect area,float u)
        {
            if(!ReferenceEquals(chapterSelectionOwner,session.Progression.Profile))
            {
                chapterSelectionOwner=session.Progression.Profile;chapterEntryError=null;chapterScroll=Vector2.zero;
                if(!ChapterProgression.IsUnlocked(chapterSelectionOwner,session.SelectedChapterNode))session.SelectedChapterNode=ChapterNode.ForestCourt;
                if(!ChapterProgression.CanEnter(chapterSelectionOwner,session.SelectedChapterNode,session.SelectedChapterDifficulty))session.SelectedChapterDifficulty=ChapterDifficulty.Heroic;
                session.SelectedChapterTier=Mathf.Clamp(session.SelectedChapterTier,1,session.Progression.UnlockedChapterTier(session.SelectedChapterNode,session.SelectedChapterDifficulty));
            }
            float cell=(area.width-16*u)/3;
            for(int i=0;i<3;i++)
            {
                var node=(ChapterNode)i;bool unlocked=ChapterProgression.IsUnlocked(chapterSelectionOwner,node);
                if(Button(new Rect(area.x+i*(cell+8*u),area.y,cell,58*u),ChapterDefinition.Get(node).Name+(unlocked?"":"\n"+ChapterProgression.UnlockLevel(node)+"级开启"),node==session.SelectedChapterNode?gold:jade,unlocked))SelectChapterNode(node);
            }
            DrawChapterEntryDetails(new Rect(area.x,area.y+70*u,area.width,Mathf.Max(48*u,area.height-70*u)),u);
        }
        private MobilePanelLayout ChapterPanelGeometry()
        {return MobileControls.Active?MobilePanelGeometry():new MobilePanelLayout(Mathf.Min(960,width),Mathf.Min(660,height));}
        private Rect ChapterRect(MobilePanelLayout.Area area,float u)
        {var layout=ChapterPanelGeometry();float x=MobileControls.Active?0:(width-layout.Width*u)*.5f,y=MobileControls.Active?0:(height-layout.Height*u)*.5f;
            return new Rect(x+area.X*u,y+area.Y*u,area.Width*u,area.Height*u);}
        private void DrawChapterFrame(MobilePanelLayout layout,float u,string title,string subtitle)
        {

            Rect frame=ChapterRect(new MobilePanelLayout.Area(0,0,layout.Width,layout.Height),u);
            Fill(frame,new Color(.025f,.045f,.068f,1));
            Border(frame,new Color(.24f,.37f,.44f,.55f));
            Box(ChapterRect(new MobilePanelLayout.Area(8,4,layout.Width-16,layout.Height-8),u),jade,false);
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
            bool rewardCards=!failed&&!pending&&session.ChapterResult!=null&&!session.ChapterResult.RewardDetailsUnavailable;
            float rewardHeight=rewardCards?92*u:0;
            float h=Style(Mathf.RoundToInt(16*u),false,true).CalcHeight(new GUIContent(copy),(layout.Body.Width-26)*u)+16*u+rewardHeight;
            chapterResultScroll=BeginTouchScroll("chapter-result",ChapterRect(layout.Body,u),chapterResultScroll,new Rect(0,0,(layout.Body.Width-16)*u,Mathf.Max(layout.Body.Height*u,h)));
            if(rewardCards)
            {
                float cw=(layout.Body.Width-42)*.5f;var result=session.ChapterResult;
                for(int i=0;i<2;i++)
                {
                    Rect r=new Rect((8+i*(cw+10))*u,8*u,cw*u,74*u);Fill(r,card);
                    DrawRewardToken(new Rect(r.x+10*u,r.y+6*u,r.width-20*u,36*u),i==0?1:3,i==0?result.Materials:result.KillExperience+result.CompletionExperience,u);
                    Text(new Rect(r.x+10*u,r.y+46*u,r.width-20*u,24*u),i==0?"星烬碎片":"经验",Mathf.RoundToInt(14*u),muted);
                }
            }
            Text(new Rect(8*u,8*u+rewardHeight,(layout.Body.Width-26)*u,h-rewardHeight),copy,Mathf.RoundToInt(16*u),pale,false,true);EndTouchScroll();
            if(pending&&Button(ChapterRect(layout.FooterButton(0,2),u),"重试保存结算",gold)){RetryChapterSettlement();return;}
            if(failed&&Button(ChapterRect(layout.FooterButton(0,2),u),"原条件重试",gold,session.CanRetryChapter)){session.RetryFailedChapter();BlockUITransition();return;}
            if(session.IsDead)
            {if(Button(ChapterRect(layout.FooterButton(1,2),u),"复活",jade)){ReturnFromChapter();return;}}
            else
            {
                if(PopupCloseButton(ChapterRect(layout.Close,u))||Button(ChapterRect(layout.FooterButton(pending||failed?1:0,pending||failed?2:1),u),"继续拾取",jade))
                {session.DismissFinishedResult();BlockUITransition();return;}
            }
        }
    }
}
