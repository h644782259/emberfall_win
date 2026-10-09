using UnityEngine;

namespace Emberfall
{
    public sealed partial class GameUI
    {
        private readonly SafeSaveFlow saveFlow = new SafeSaveFlow();
        private bool saveSelectionFromPause;
        private string saveFlowSourceName, saveFlowTargetName, saveFlowError, saveFlowTargetRevision;
        private Vector2 mobileSaveFlowScroll;
        private string mobileSaveFlowProblem;

        private string CurrentSaveFlowId() { return session.Progression.CurrentSlotId ?? "（存档已移除）"; }

        private string ActiveCharacterName()
        {
            GameProfile profile = session.Progression.Profile;
            return GameBalance.ClassName(profile.heroClass) + " · " + profile.level + "级";
        }
        private static string SaveRevision(SaveSlotInfo slot)
        {
            return slot == null ? "missing" : slot.Id + "|" + slot.HeroClass + "|" + slot.Level + "|" +
                slot.SavedAtUtc.Ticks + "|" + slot.CanLoad + "|" + slot.RecoveredFromBackup;
        }
        private void RequestManualSave()
        {
            if (!session.HasStarted || saveFlow.Open) return;
            if (!session.Progression.HasActiveSave) { session.Notify("当前角色没有可覆盖的有效存档，请先读取其他角色。"); return; }
            saveFlow.RequestManual(session.Progression.CurrentSlotId);
            saveFlowSourceName = ActiveCharacterName();
            saveFlowError = null;
            mobileSaveFlowScroll=Vector2.zero;mobileSaveFlowProblem=null;
            saveSelectionFromPause = true;
            panel = Panel.SaveSelection;
            session.SetPaused(true);
            session.SetUIBlocking(true);
            BlockUITransition();
        }
        private void RequestLoadSelectedSave(SaveSlotInfo target)
        {
            if (target == null || !target.CanLoad || saveFlow.Open) return;
            saveFlow.RequestLoad(CurrentSaveFlowId(), target.Id);
            saveFlowSourceName = ActiveCharacterName();
            saveFlowTargetName = target.DisplayName;
            saveFlowTargetRevision = SaveRevision(target);
            saveFlowError = null;
            mobileSaveFlowScroll=Vector2.zero;mobileSaveFlowProblem=null;
            BlockUITransition();
        }
        private bool CancelActiveSaveFlow()
        {
            if (!saveFlow.Open || saveFlow.Busy) return false;
            bool manual = saveFlow.Kind == SaveFlowKind.ManualSave;
            saveFlow.Cancel();
            saveFlowError = null;
            if (manual)
            {
                panel = Panel.None;
                saveSelectionFromPause = false;
                session.SetUIBlocking(false);
            }
            else { panel = Panel.SaveSelection; RefreshSaveSlots(); }
            if (session.HasStarted) session.SetPaused(true);
            BlockUITransition();
            return true;
        }
        private bool DrawSaveFlowConfirmation()
        {
            if (!saveFlow.Open) return false;
            if (MobileControls.Active) return DrawMobileSaveFlowConfirmation();
            bool mobile = MobileControls.Active;
            bool manual = saveFlow.Kind == SaveFlowKind.ManualSave;
            float logicalWidth = mobile ? 520 : 680, logicalHeight = mobile ? 310 : 410;
            float x = mobile ? (MobileControls.Layout.Width - logicalWidth) * .5f : (width - logicalWidth) * .5f;
            float y = mobile ? (MobileControls.Layout.Height - logicalHeight) * .5f : (height - logicalHeight) * .5f;
            float unit = mobile ? TouchRatio : 1f;
            Rect w = mobile ? TouchRect(x, y, logicalWidth, logicalHeight) : new Rect(x, y, logicalWidth, logicalHeight);
            Fill(new Rect(0, 0, width, height), new Color(.008f, .018f, .03f, 1));
            Box(w, gold, false);
            float left = w.x + 16 * unit, available = w.width - 32 * unit;
            int normal = mobile ? TouchFont(13) : 16, small = mobile ? TouchFont(11) : 13;
            Text(new Rect(left, w.y + 12 * unit, available, 32 * unit), manual ? "覆盖保存当前角色？" : "读取另一份角色进度？",
                mobile ? TouchFont(21) : 25, pale, true);
            Text(new Rect(left, w.y + 49 * unit, available, 23 * unit), "当前：「" + saveFlowSourceName + "」", normal, pale, true);
            float warningY;
            string warning;
            if (manual)
            {
                warningY = 88;
                warning = "将覆盖这份角色存档，并收存当前地面战利品。\n自动保存仍持续写入当前角色。取消只取消这次手动保存。";
            }
            else
            {
                Text(new Rect(left, w.y + 78 * unit, available, 23 * unit), "读取：「" + saveFlowTargetName + "」", normal, gold, true);
                warningY = 112;
                warning = "保存并读取：先保存当前角色，再切换至目标角色。\n放弃未保存：丢弃尚未保存的变更、地面战利品" +
                    (session.ModeRewardPending ? "和待保存挑战奖励" : "（含本局临时状态）") + "。\n已自动保存的内容不会回滚；读取后返回营地。";
            }
            Text(new Rect(left, w.y + warningY * unit, available, (mobile ? 65 : 90) * unit), warning, small, muted, false, true);
            string problem = string.IsNullOrEmpty(saveFlow.Error) ? saveFlowError : saveFlow.Error;
            if (!string.IsNullOrEmpty(problem)) Text(new Rect(left, w.yMax - 105 * unit, available, 37 * unit), problem, small, gold, false, true);
            float gap = 12 * unit, buttonY = w.yMax - 62 * unit;
            float buttonWidth = (available - gap * (manual ? 1 : 2)) / (manual ? 2 : 3);
            if (NavigationButton(new Rect(left, buttonY, buttonWidth, 48 * unit), "取消", jade, !saveFlow.Busy, null, true))
            { CancelActiveSaveFlow(); return true; }
            if (manual)
            {
                if (PrimaryButton(new Rect(left + buttonWidth + gap, buttonY, buttonWidth, 48 * unit), "确认覆盖保存", gold, !saveFlow.Busy)) ConfirmManualSave();
            }
            else
            {
                if (PrimaryButton(new Rect(left + buttonWidth + gap, buttonY, buttonWidth, 48 * unit), "保存并读取", gold, !saveFlow.Busy)) ConfirmPauseLoad(SaveLoadChoice.SaveAndLoad);
                if (DangerButton(new Rect(left + 2 * (buttonWidth + gap), buttonY, buttonWidth, 48 * unit), "放弃未保存并读取", new Color(1f, .52f, .4f), !saveFlow.Busy)) ConfirmPauseLoad(SaveLoadChoice.DiscardAndLoad);
            }
            return true;
        }
        private bool DrawMobileSaveFlowConfirmation()
        {
            bool manual=saveFlow.Kind==SaveFlowKind.ManualSave;
            var layout=DrawMobileDialogChrome(manual?"覆盖保存当前角色？":"读取另一份角色进度？",gold);
            string problem=string.IsNullOrEmpty(saveFlow.Error)?saveFlowError:saveFlow.Error;
            if(problem!=mobileSaveFlowProblem)
            {mobileSaveFlowProblem=problem;if(!string.IsNullOrEmpty(problem)){CancelMobileScroll();mobileSaveFlowScroll=Vector2.zero;}}
            float contentWidth=layout.Body.Width-18;
            float contentHeight=DrawMobileSaveFlowContent(contentWidth,manual,problem,false);
            mobileSaveFlowScroll=BeginTouchScroll("mobile-save-confirm",MobilePanelRect(layout.Body),mobileSaveFlowScroll,
                new Rect(0,0,contentWidth*TouchRatio,Mathf.Max(layout.Body.Height,contentHeight)*TouchRatio));
            DrawMobileSaveFlowContent(contentWidth,manual,problem,true);
            EndTouchScroll();
            int count=manual?2:3;
            if(NavigationButton(MobilePanelRect(layout.FooterButton(0,count)), "取消", jade, !saveFlow.Busy, null, true))
            {CancelActiveSaveFlow();return true;}
            if(manual)
            {
                if(PrimaryButton(MobilePanelRect(layout.FooterButton(1,count)), "确认覆盖保存", gold, !saveFlow.Busy))ConfirmManualSave();
            }
            else
            {
                if(PrimaryButton(MobilePanelRect(layout.FooterButton(1,count)), "保存并读取", gold, !saveFlow.Busy))ConfirmPauseLoad(SaveLoadChoice.SaveAndLoad);
                if(DangerButton(MobilePanelRect(layout.FooterButton(2,count)), "放弃未保存并读取", new Color(1,.52f,.4f), !saveFlow.Busy))ConfirmPauseLoad(SaveLoadChoice.DiscardAndLoad);
            }
            return true;
        }
        private float DrawMobileSaveFlowContent(float width,bool manual,string problem,bool draw)
        {
            float y=4;
            MobileDialogParagraph(ref y,width,problem,14,gold,draw,true);
            MobileDialogParagraph(ref y,width,"当前：「"+saveFlowSourceName+"」",16,pale,draw,true);
            if(manual)
                MobileDialogParagraph(ref y,width,"将覆盖这份角色存档，并收存当前地面战利品。\n自动保存仍持续写入当前角色。取消只取消这次手动保存。",14,muted,draw);
            else
            {
                MobileDialogParagraph(ref y,width,"读取：「"+saveFlowTargetName+"」",16,gold,draw,true);
                MobileDialogParagraph(ref y,width,"保存并读取：先保存当前角色，再切换至目标角色。",14,muted,draw);
                MobileDialogParagraph(ref y,width,"放弃未保存：丢弃尚未保存的变更、地面战利品"+
                    (session.ModeRewardPending?"和待保存挑战奖励":"（含本局临时状态）")+"。",14,new Color(1,.64f,.49f),draw);
                MobileDialogParagraph(ref y,width,"已自动保存的内容不会回滚；读取后返回营地。",14,muted,draw);
            }
            return y;
        }
        private bool SaveCurrentForFlow()
        {
            bool saved = session.SaveBeforeLeaving();
            if (!saved) saveFlowError = session.Progression.LastError;
            return saved;
        }
        private void ConfirmManualSave()
        {
            try
            {
                if (!saveFlow.ConfirmManual(session.Progression.CurrentSlotId, SaveCurrentForFlow)) return;
                panel = Panel.None; saveSelectionFromPause = false;
                session.SetUIBlocking(false); session.SetPaused(true);
                saveSlotsDirty = true;
                session.Notify("已覆盖保存「" + saveFlowSourceName + "」。自动保存继续写入此角色。");
                BlockUITransition();
            }
            catch (System.Exception error) { saveFlowError = "保存未完成：" + error.Message; }
        }
        private void ConfirmPauseLoad(SaveLoadChoice choice)
        {
            SaveSlotInfo currentTarget = session.Progression.GetSaveSlots().Find(slot => slot.Id == saveFlow.TargetId);
            // A prior Save-and-Load attempt may itself have updated this same slot.
            bool ownSameSlotWrite = saveFlow.SavedCurrent && saveFlow.SourceId == saveFlow.TargetId;
            if (currentTarget == null || !currentTarget.CanLoad || (!ownSameSlotWrite && SaveRevision(currentTarget) != saveFlowTargetRevision))
            { saveFlowError = "目标存档已改变或无法读取。请取消并重新打开存档页确认。"; return; }
            try
            {
                bool loaded = saveFlow.ConfirmLoad(CurrentSaveFlowId(), selectedSaveId, choice, SaveCurrentForFlow, discard =>
                {
                    bool result = session.LoadSaveFromPause(saveFlow.TargetId, discard, !discard && saveFlow.SavedCurrent);
                    if (!result) saveFlowError = string.IsNullOrEmpty(session.SaveLoadError) ? session.Progression.LastError : session.SaveLoadError;
                    return result;
                });
                if (loaded) CompleteSaveLoadUI();
            }
            catch (System.Exception error) { saveFlowError = "读取未完成：" + error.Message; }
        }
        private void CompleteSaveLoadUI()
        {
            panel = Panel.None; saveSelectionFromPause = false;
            selectedItem = null; inventoryScroll = Vector2.zero;
            selectedSkill = 0; skillScroll = Vector2.zero; rebindingSlot = -1;
            saveSlotsDirty = true; saveSelectionError = saveFlowError = null;
            BlockUITransition();
        }
    }
}
