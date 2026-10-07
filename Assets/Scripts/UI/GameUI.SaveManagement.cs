using UnityEngine;

namespace Emberfall
{
    public sealed partial class GameUI
    {
        private SaveDeletionRequest pendingSaveDeletion;
        private bool focusDeleteCancel;

        // Called by the selection page after its non-destructive controls. Corrupt
        // and backup-only saves can be selected and removed, but never auto-selected
        // for deletion. Selection is always a stable ID, not a sorted row index.
        private void DrawDeleteSaveButton(Rect rect)
        {
            if (session.HasStarted) return;
            SaveSlotInfo selected = saveSlots.Find(slot => slot.Id == selectedSaveId);
            if (DangerButton(rect, "删除角色…", new Color(1f, .48f, .42f), selected != null, "先核对角色、保存时间与存档编号，再确认永久删除。"))
            {
                SaveDeletionRequest request;
                if (session.Progression.PrepareSaveDeletion(selected.Id, out request))
                {
                    pendingSaveDeletion = request;
                    focusDeleteCancel = true;
                    BlockUITransition();
                    saveSelectionError = null;
                }
                else
                {
                    saveSelectionError = session.Progression.LastError;
                    RefreshSaveSlots();
                }
            }
        }

        // Render instead of the selection page while confirmation is open. The
        // underlying list cannot receive a click, scroll or accidental load.
        private bool DrawSaveDeletionConfirmation()
        {
            if (pendingSaveDeletion == null) return false;
            if (session.HasStarted) { CancelSaveDeletion(); return false; }
            if (MobileControls.Active) return DrawMobileSaveDeletionConfirmation();
            Fill(new Rect(0, 0, width, height), new Color(.018f, .029f, .048f, 1f));
            Rect w = Modal(780, 490, "删除角色存档？", "永久删除前，请核对下面的角色与编号");
            Color danger = new Color(1f, .48f, .42f);
            Text(new Rect(w.x + 28, w.y + 116, 724, 34), pendingSaveDeletion.DisplayName, 24, pale, true);
            string saved = pendingSaveDeletion.SavedAtUtc == System.DateTime.MinValue ? "未知" :
                pendingSaveDeletion.SavedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
            Text(new Rect(w.x + 28, w.y + 163, 724, 24), "保存时间：" + saved, 15, muted);
            Text(new Rect(w.x + 28, w.y + 197, 724, 24), "存档编号：" + pendingSaveDeletion.Id, 15, jade, true);
            string status = pendingSaveDeletion.WasCurrent ? "这是上次使用的角色。" : "其他角色的进度不受影响。";
            if (saveSlots.Count == 1) status += " 这也是最后一份存档。";
            Text(new Rect(w.x + 28, w.y + 235, 724, 46), status, 15, pale, false, true);
            Text(new Rect(w.x + 28, w.y + 294, 724, 68),
                "将删除此角色的主存档、备份及临时文件。此操作无法撤销，不能再从游戏内备份恢复。\n如果想保留角色，请选择「取消」。",
                16, danger, false, true);
            if (SaveConfirmationButton(new Rect(w.x + 28, w.y + 401, 338, 56), "取消 · 保留角色", jade, 15, true))
                CancelSaveDeletion();
            if (SaveConfirmationButton(new Rect(w.x + 398, w.y + 401, 354, 56), "永久删除此角色", danger, 15, false))
                SubmitSaveDeletion();
            FocusSaveCancel();
            return true;
        }

        private bool DrawMobileSaveDeletionConfirmation()
        {
            var layout = MobileControls.Layout;
            float x = (layout.Width - 490) * .5f, y = (layout.Height - 292) * .5f;
            Color danger = new Color(1f, .48f, .42f);
            Fill(new Rect(0, 0, width, height), new Color(.018f, .029f, .048f, 1f));
            Box(TouchRect(x, y, 490, 292), danger, false);
            Text(TouchRect(x + 16, y + 11, 458, 30), "永久删除角色？", TouchFont(22), pale, true);
            Text(TouchRect(x + 16, y + 47, 458, 26), pendingSaveDeletion.DisplayName, TouchFont(18), pale, true);
            string saved = pendingSaveDeletion.SavedAtUtc == System.DateTime.MinValue ? "未知" :
                pendingSaveDeletion.SavedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
            Text(TouchRect(x + 16, y + 80, 458, 20), "保存：" + saved, TouchFont(13), muted);
            Text(TouchRect(x + 16, y + 105, 458, 21), "编号：" + pendingSaveDeletion.Id, TouchFont(13), jade);
            string status = saveSlots.Count == 1 ? "这是最后一份存档。" : pendingSaveDeletion.WasCurrent ? "这是上次使用的角色。" : "其他角色不受影响。";
            Text(TouchRect(x + 16, y + 134, 458, 21), status, TouchFont(14), pale);
            Text(TouchRect(x + 16, y + 165, 458, 44), "将一并删除主存档、备份与临时文件。\n无法撤销，也无法再从备份恢复。", TouchFont(15), danger, false, true);
            // Both actions are 50 density-independent units high; the generous gap
            // keeps the destructive action separate from the preferred cancel action.
            if (SaveConfirmationButton(TouchRect(x + 16, y + 226, 214, 50), "取消 · 保留", jade, TouchFont(16), true)) CancelSaveDeletion();
            if (SaveConfirmationButton(TouchRect(x + 260, y + 226, 214, 50), "永久删除", danger, TouchFont(16), false)) SubmitSaveDeletion();
            FocusSaveCancel();
            return true;
        }

        private bool SaveConfirmationButton(Rect rect, string caption, Color accent, int size, bool cancel)
        {
            return DrawButton(rect, caption, cancel ? ButtonRole.Navigation : ButtonRole.Danger,
                true, null, size, cancel ? "save-delete-cancel" : null);
        }

        private void FocusSaveCancel()
        {
            if (focusDeleteCancel && Event.current.type == EventType.Repaint)
            {
                GUI.FocusControl("save-delete-cancel");
                focusDeleteCancel = false;
            }
        }

        private void SubmitSaveDeletion()
        {
            SaveDeletionRequest confirmed = pendingSaveDeletion;
            if (confirmed == null) return;
            pendingSaveDeletion = null; // One click submits once, including failure.
            focusDeleteCancel = false;
            bool deleted = session.Progression.DeleteSaveSlot(confirmed);
            string result = deleted ? "角色存档及备份已删除。" : session.Progression.LastError;
            RefreshSaveSlots();
            // Require a deliberate row selection before another load/delete. The
            // next character must not inherit rapid taps from this confirmation.
            if (deleted) selectedSaveId = null;
            BlockUITransition();
            saveSelectionError = result;
        }

        // ClosePanel calls this first, making Esc/back cancel only the confirmation.
        private bool CancelSaveDeletion()
        {
            if (pendingSaveDeletion == null) return false;
            pendingSaveDeletion = null;
            focusDeleteCancel = false;
            saveSelectionError = null;
            BlockUITransition();
            return true;
        }
    }
}
