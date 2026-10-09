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
            Rect w = Modal(1000, 470, "星烬祝福", BlessingSubtitle(false));
            for (int i=0;i<offer.Length;i++)
            {
                Rect cardRect = new Rect(w.x+28+i*322,w.y+117,300,240);
                bool chosen = i == selectedBlessing;
                Fill(cardRect,chosen ? new Color(.12f,.2f,.21f):card); Border(cardRect,chosen?gold:jade*.5f);
                Text(new Rect(cardRect.x+20,cardRect.y+23,260,28),RunChoices.Name(offer[i]),23,chosen?gold:pale,true);
                bool compatible = RunChoices.IsCompatible(offer[i],session.Progression.Profile.heroClass,RunChoices.UsableRanks(session.Progression.Profile,false));
                Text(new Rect(cardRect.x+20,cardRect.y+63,260,21),RunChoices.Association(offer[i],session.Progression.Profile,false),12,compatible?jade:muted);
                Text(new Rect(cardRect.x+20,cardRect.y+102,260,93),RunChoices.Description(offer[i]),16,pale,false,true);
                if(GUI.Button(cardRect,GUIContent.none,invisibleButton))
                ClickBlessing(i);
                Text(new Rect(cardRect.x+20,cardRect.y+202,260,24),chosen?"再点击确认":"点击预览 · 双击确认",13,chosen?gold:muted,true,false,TextAnchor.MiddleCenter);
            }
            if(PrimaryButton(new Rect(w.x+310,w.y+392,380,46), session.RoomChainRun!=null?"确认祝福并继续":"确认并进入下一波", gold, selectedBlessing>=0&&selectedBlessing<offer.Length, null, true))
            { if(session.ConfirmBlessing(selectedBlessing))selectedBlessing=-1; }
        }

        private void DrawCampWorkshop()
        {
            if(merchantExchangeOpen){DrawAttachmentWorkshop();return;}
            if(DrawClassSwitchSurface()||DrawReforgeSurface()||DrawProgressionGoalSurface()||DrawBuildPlanSurface())return;
            if(campTab==4){DrawTownActivitySurface();return;}
            float u=MobileControls.Active?TouchRatio:1;
            Rect w=Modal(Mathf.Min(width-24,780*u),Mathf.Min(height-24,300*u),"营地工坊","职业能力与精通已并入技能；兑换请与商人对话，试炼请打开右上目标。");
            if(PopupCloseButton(new Rect(w.xMax-64*u,w.y+20*u,40*u,32*u)))ClosePanel();
            Text(new Rect(w.x+24*u,w.y+116*u,w.width-48*u,100*u),"技能：学习、职业路线、精通与配装方案。\n目标：实战试炼、进度、奖励和下一步指引。\n商人：机制兑换、药剂与交易。",Mathf.RoundToInt(16*u),pale,false,true);
        }

        private void DrawExpeditionHUD()
        {
            if(session.NearDungeonReturn){Rect exit=new Rect((width-240)*.5f,height-225,240,48);blockedRects.Add(exit);if(Button(exit,"返回营地 [E]",gold))session.ReturnToCamp();}
            else if(session.NearChapterExit){Rect next=new Rect((width-300)*.5f,height-225,300,48);blockedRects.Add(next);if(Button(next,"沿星路前进",gold))session.EnterNextChapterRoom();}
            else if(session.NearRoomExit){Rect next=new Rect((width-300)*.5f,height-225,300,48);blockedRects.Add(next);if(Button(next,"北门已开启 · 进入下一间",gold))session.EnterNextRoom();}
            if(session.SideEventAvailable)
            { Rect r=new Rect((width-410)*.5f,height-260,410,48);blockedRects.Add(r);
              if(Button(r,"开启晶核挑战 [E] · 2名守卫",gold,true,"额外一名遗迹守卫与一名魔灵；全部击败才获得1碎片和补给。未完成可放弃，不阻挡已开启的北门。"))session.StartSideEvent(); }

            DrawSystemLog();
        }
        private void DrawSystemLog()
        {
            if(panel!=Panel.None || session.InputBlocked)return;
            var messages=session.SystemMessages;
            if(messages.Count==0)return;
            int show=systemHistory?Mathf.Min(8,messages.Count):Mathf.Min(3,messages.Count);
            float panelHeight=AdventureSelectionLayout.LogHeight(messages.Count,systemHistory);
            float bottom=height-(MobileControls.Active?220:16);
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
            if(DrawStructuredRunRecap(false))ClosePanel();
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
