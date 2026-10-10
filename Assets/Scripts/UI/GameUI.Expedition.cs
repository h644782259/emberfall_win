using UnityEngine;

namespace Emberfall
{
    public sealed partial class GameUI
    {
        private int selectedBlessing = -1, campTab;
        private float lastBlessingClick=-10;
        private void ClickBlessing(int index)
        {
            if(!GUI.enabled||UITransitionBlocked)return;
            bool twice=selectedBlessing==index&&Time.unscaledTime-lastBlessingClick<.45f;
            selectedBlessing=index;lastBlessingClick=Time.unscaledTime;
            if(twice&&session.ConfirmBlessing(index))
            {selectedBlessing=-1;lastBlessingClick=-10;CancelMobileScroll();BlockUITransition();}
        }
        private bool systemHistory;
        public void CancelForegroundInput()
        {
            CancelHotbarPointer(); rebindingSlot = -1;
            // IMGUI buttons have their own hotControl, unrelated to our hotbar.
            GUIUtility.hotControl=0;GUIUtility.keyboardControl=0;
            lastBlessingClick=-10;
            BlockUITransition();
        }
        public void CancelBackgroundInput()
        {
            CancelForegroundInput();
            // Only focus/suspension and viewport invalidation require every
            // pointer to lift. Room transitions retain their initiating finger.
            lifecycleRelease.Block(Time.unscaledTime);
        }

        private void DrawDungeonSelection(){DrawArenaSelection();}

        private Vector2 branchScroll;
        private void DrawRoomBranchChoice()
        {
            float u=MobileControls.Active?TouchRatio:1;
            Rect w=Modal(Mathf.Min(width-24,900*u),Mathf.Min(height-24,520*u),"第三房 · 选择侧廊","只进入所选路线；第四房星泉汇合，第五房首领。无额外材料奖励。");
            Rect viewport=new Rect(w.x+20*u,w.y+105*u,w.width-40*u,Mathf.Max(60*u,w.height-175*u));
            float cw=(viewport.width-16*u)*.5f;
            branchScroll=BeginTouchScroll("room-branch",viewport,branchScroll,new Rect(0,0,viewport.width,280*u));
            for(int i=0;i<2;i++)
            {
                RoomBranch branch=i==0?RoomBranch.Seal:RoomBranch.Supply;
                Rect c=new Rect(i*(cw+16*u),0,cw,270*u);Fill(c,card);Border(c,jade);
                Text(new Rect(c.x+12*u,c.y+12*u,c.width-24*u,196*u),GameSession.RoomBranchDescription(branch),Mathf.RoundToInt(16*u),pale,false,true);
                if(PrimaryButton(new Rect(c.x+12*u,c.y+218*u,c.width-24*u,42*u), "进入这条侧廊", gold))
                {session.ConfirmRoomBranch(branch);BlockUITransition();}
            }
            EndTouchScroll();
            if(NavigationButton(new Rect(w.x+20*u,w.yMax-58*u,w.width-40*u,40*u), "返回第二房 · 暂不选择", jade))
            {session.CancelRoomBranchChoice();BlockUITransition();}
        }

        private void DrawBlessingChoice()
        {
            if(MobileControls.Active){DrawMobileBlessingChoice();return;}
            RunBlessing[] offer = session.RunChoices.Offer;
            ReconcileBlessingOffer(offer);
            float cardHeight=184;
            foreach(var blessing in offer)
                cardHeight=Mathf.Max(cardHeight,Style(23,true,true).CalcHeight(new GUIContent(RunChoices.Name(blessing)),260)+Style(12,false,true).CalcHeight(new GUIContent(RunChoices.Association(blessing,session.Progression.Profile,false)),260)+Style(16,false,true).CalcHeight(new GUIContent(RunChoices.Description(blessing)),260)+96);
            Rect w = Modal(1000, cardHeight+210, "星烬祝福", BlessingSubtitle(false));
            for (int i=0;i<offer.Length;i++)
            {
                Rect cardRect = new Rect(w.x+28+i*322,w.y+117,300,cardHeight);
                bool chosen = i == selectedBlessing;
                Fill(cardRect,chosen ? new Color(.12f,.2f,.21f):card); Border(cardRect,chosen?gold:jade*.5f);
                float nameHeight=Style(23,true,true).CalcHeight(new GUIContent(RunChoices.Name(offer[i])),260);
                float associationHeight=Style(12,false,true).CalcHeight(new GUIContent(RunChoices.Association(offer[i],session.Progression.Profile,false)),260);
                Text(new Rect(cardRect.x+20,cardRect.y+16,260,nameHeight),RunChoices.Name(offer[i]),23,chosen?gold:pale,true,true);
                bool compatible = RunChoices.IsCompatible(offer[i],session.Progression.Profile.heroClass,RunChoices.UsableRanks(session.Progression.Profile,false));
                Text(new Rect(cardRect.x+20,cardRect.y+24+nameHeight,260,associationHeight),RunChoices.Association(offer[i],session.Progression.Profile,false),12,compatible?jade:muted,false,true);
                Text(new Rect(cardRect.x+20,cardRect.y+36+nameHeight+associationHeight,260,cardHeight-88-nameHeight-associationHeight),RunChoices.Description(offer[i]),16,pale,false,true);
                if(GUI.Button(cardRect,GUIContent.none,invisibleButton))
                ClickBlessing(i);
                Text(new Rect(cardRect.x+20,cardRect.y+202,260,24),chosen?"再点击确认":"点击预览 · 双击确认",13,chosen?gold:muted,true,false,TextAnchor.MiddleCenter);
            }
            if(PrimaryButton(new Rect(w.x+310,w.y+cardHeight+137,380,46), session.RoomChainRun!=null?"确认祝福并继续":"确认并进入下一波", gold, selectedBlessing>=0&&selectedBlessing<offer.Length, null, true))
            { if(session.ConfirmBlessing(selectedBlessing))selectedBlessing=-1; }
        }

        // Dedicated utility surfaces remain reachable from their own entries.
        private void DrawHubUtility()
        {
            if(merchantExchangeOpen){DrawAttachmentWorkshop();return;}
            if(DrawClassSwitchSurface()||DrawReforgeSurface()||DrawProgressionGoalSurface()||DrawBuildPlanSurface())return;
            if(campTab==4){DrawTownActivitySurface();return;}
            ClosePanel();
        }

        private void DrawComboCounter()
        {
            int count=session.ComboHitCount;
            if(count<=0||panel!=Panel.None||session.Paused||session.IsDead)return;
            float u=MobileControls.Active?TouchRatio:1;
            Rect r=new Rect(width-244*u,Mathf.Clamp(height*.46f,30*u,height-90*u),208*u,54*u);
            if(MobileControls.Active)
            {
                var layout=MobileControls.Layout;
                // Keep combo feedback in the open field left of the upper skill column.
                // A viewport anchor stays below navigation even on compact phones.
                float centerX=Mathf.Min(layout.Width*.74f,layout.Skills[4].X-96);
                r=new Rect((centerX-80)*u,(layout.Height*.4f-27)*u,160*u,54*u);
            }
            var previous=GUI.matrix;
            GUIUtility.RotateAroundPivot(-7,r.center);
            var style=new GUIStyle(Style(Mathf.RoundToInt(30*u),true,false));
            style.fontStyle=FontStyle.BoldAndItalic;style.alignment=MobileControls.Active?TextAnchor.MiddleCenter:TextAnchor.MiddleRight;
            style.padding=new RectOffset(0,Mathf.RoundToInt(8*u),0,0);
            string combo="连击 x"+count;
            while(style.fontSize>Mathf.RoundToInt(12*u)&&style.CalcSize(new GUIContent(combo)).x>r.width-16*u)style.fontSize--;
            style.clipping=TextClipping.Clip;
            style.normal.textColor=new Color(.12f,.05f,.015f,.95f);
            GUI.Label(new Rect(r.x+2*u,r.y+2*u,r.width,r.height),combo,style);
            style.normal.textColor=gold;GUI.Label(r,combo,style);
            GUI.matrix=previous;
        }

        private void DrawDungeonExitButton()
        {
            if(!session.InDungeon||session.IsDead||session.Paused||panel!=Panel.None)return;
            float u=MobileControls.Active?TouchRatio:1f;
            float exitY=MobileControls.Active?MobileControls.Layout.EncounterText.Y+MobileControls.Layout.EncounterText.Height+14:72;
            Rect exit=new Rect(width-128*u,exitY*u,112*u,40*u);
            blockedRects.Add(exit);
            if(Button(exit,"退出副本",jade,!session.InCombat,"脱离战斗后可退出副本"))
            {
                session.ReturnToCamp();BlockUITransition();
            }
        }

        public void OpenDungeonExit()
        {
            if(!session.NearDungeonReturn||session.InputBlocked)return;
            panel=Panel.DungeonExit;session.SetUIBlocking(true);BlockUITransition();
        }
        private void DrawDungeonExit()
        {
            if(!session.NearDungeonReturn){ClosePanel();return;}
            float u=MobileControls.Active?TouchRatio:1;
            Rect w=Modal(Mathf.Min(width-24,500*u),Mathf.Min(height-24,250*u),"传送点","");
            if(PopupCloseButton(new Rect(w.xMax-54*u,w.y+12*u,44*u,44*u))){ClosePanel();return;}
            float x=w.x+24*u,y=w.y+106*u,bw=(w.width-56*u)*.5f;

            if(Button(new Rect(x,y,w.width-48*u,44*u),"再次挑战当前副本",gold,session.CanRepeatCurrentDungeon))
            {
                session.SetUIBlocking(false);
                if(session.RepeatCurrentDungeon())panel=Panel.None;else session.SetUIBlocking(true);
                BlockUITransition();return;
            }
            y+=54*u;
            bool showNextTier=!session.ChapterActive||session.ActiveChapterDifficulty==ChapterDifficulty.Heroic;
            if(showNextTier&&Button(new Rect(x,y,bw,44*u),"挑战下一阶",gold,session.CanChallengeNextTier))
            {
                session.SetUIBlocking(false);
                if(session.ChallengeNextTier())panel=Panel.None;
                else session.SetUIBlocking(true);
                BlockUITransition();return;
            }
            if(Button(new Rect(showNextTier?x+bw+8*u:x,y,showNextTier?bw:w.width-48*u,44*u),"返回营地",jade))
            {
                session.SetUIBlocking(false);session.ReturnToCamp();
                if(!session.InDungeon)panel=Panel.None;else session.SetUIBlocking(true);
                BlockUITransition();
            }
        }

        private void DrawExpeditionHUD()
        {
            if(session.NearDungeonReturn){Rect exit=new Rect((width-240)*.5f,height-225,240,48);blockedRects.Add(exit);if(Button(exit,"传送点 [E]",gold))OpenDungeonExit();}
            else if(session.NearChapterExit){Rect next=new Rect((width-300)*.5f,height-225,300,48);blockedRects.Add(next);if(Button(next,"沿星路前进 [T]",gold))session.EnterNextChapterRoom();}
            else if(session.NearRoomExit){Rect next=new Rect((width-300)*.5f,height-225,300,48);blockedRects.Add(next);if(Button(next,"进入下一间 [T]",gold))session.EnterNextRoom();}
            if(session.SideEventAvailable)
            { Rect r=new Rect((width-410)*.5f,height-260,410,48);blockedRects.Add(r);
              if(Button(r,"开启晶核挑战 [E] · 2名守卫",gold,true,"额外一名遗迹守卫与一名魔灵；全部击败才获得1碎片和补给。"))session.StartSideEvent(); }

            DrawSystemLog();
        }
        private void DrawSystemLog()
        {
            if(panel!=Panel.None || session.InputBlocked)return;
            var messages=session.SystemMessages;
            if(messages.Count==0)return;
            int show=systemHistory?Mathf.Min(8,messages.Count):Mathf.Min(3,messages.Count);
            float panelHeight=AdventureSelectionLayout.LogHeight(messages.Count,systemHistory);
            float bottom=height-(MobileControls.Active?220:54);
            Rect r=new Rect(16,bottom-panelHeight,344,panelHeight);
            Fill(r,new Color(.025f,.045f,.065f,.88f));
            Text(new Rect(r.x+10,r.y+6,228,20),"系统信息",12,jade,true);
            if(Button(new Rect(r.xMax-68,r.y+3,59,25),systemHistory?"收起":"记录",muted))systemHistory=!systemHistory;
            for(int i=0;i<show;i++)
            {
                Rect row=new Rect(r.x+10,r.y+31+i*39,r.width-20,38);
                Text(row,messages[messages.Count-show+i].Text,13,pale,false,true);
                if(row.Contains(Mouse))tooltip=messages[messages.Count-show+i].Text;
            }
            blockedRects.Add(r);
        }

        private void DrawRunSummary()
        {
            if(DrawStructuredRunRecap(false))CloseSettlement();
        }

        private void DrawAccessibilityStrip(Rect r)
        {
            if(Button(new Rect(r.x,r.y,145,35), "字号 "+EffectPreferences.CombatTextScale.ToString("0.00")+"×", jade))
                EffectPreferences.CombatTextScale=EffectPreferences.CombatTextScale>=1.79f?1f:Mathf.Min(1.8f,EffectPreferences.CombatTextScale+.25f);
            if(ToggleButton(new Rect(r.x+154,r.y,145,35), EffectPreferences.CameraShake?"镜头震动：开":"镜头震动：关", EffectPreferences.CameraShake))EffectPreferences.CameraShake=!EffectPreferences.CameraShake;
            if(ToggleButton(new Rect(r.x+308,r.y,145,35), EffectPreferences.ReducedEffects?"低动态效果":"完整效果", !EffectPreferences.ReducedEffects))EffectPreferences.EffectsScale=EffectPreferences.ReducedEffects?1f:.3f;
        }
    }
}
