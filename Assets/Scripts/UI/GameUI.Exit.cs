using UnityEngine;

namespace Emberfall
{
    public sealed partial class GameUI
    {
        private readonly SafeExitRequest exitRequest = new SafeExitRequest();
        private string exitError;
        private readonly TouchReleaseLatch uiTransition = new TouchReleaseLatch();

        // OnGUI applies this before rendering any interactive controls. Use unscaled
        // time because menus pause the simulation, and consume an in-flight touch
        // until release so it cannot become a click on the newly exposed screen.
        private bool UITransitionBlocked
        {
            get
            {
                bool any=Input.GetMouseButton(0)||Input.GetMouseButtonUp(0)||Input.touchCount>0;
                bool trigger=uiTransition.Finger==-2&&(Input.GetMouseButton(0)||Input.GetMouseButtonUp(0));
                for(int i=0;!trigger&&i<Input.touchCount;i++)
                {Touch t=Input.GetTouch(i);if(t.fingerId==uiTransition.Finger)trigger=true;}
                return uiTransition.IsBlocked(Time.unscaledTime,any,trigger);
            }
        }

        private void BlockUITransition(){BlockUITransitionForFinger(TouchReleaseLatch.AnyPointer);}
        private void BlockUITransitionForFinger(int finger)
        {
            uiTransition.Block(Time.unscaledTime,finger);
            GUIUtility.keyboardControl=0;GUIUtility.hotControl=0;
        }

        private void RequestExit(bool toTitle)
        {
            if (exitRequest.Open || exitRequest.Busy) return;
            CancelHotbarPointer();
            exitError = null;
            exitRequest.Request(toTitle);
            BlockUITransition();
            if (session.HasStarted) session.SetPaused(true);
        }

        private void DrawExitConfirmation()
        {
            if (!exitRequest.Open) return;

            bool mobile = MobileControls.Active;
            Rect r = mobile ? TouchRect((MobileControls.Layout.Width - 480) / 2, (MobileControls.Layout.Height - 270) / 2, 480, 270)
                : new Rect((width - 580) / 2, (height - 290) / 2, 580, 290);
            Box(r, gold);
            float unit = mobile ? TouchRatio : 1;
            Text(new Rect(r.x + 24 * unit, r.y + 20 * unit, r.width - 48 * unit, 38 * unit),
                exitRequest.ToTitle ? "保存并返回主菜单？" : "保存并退出游戏？", mobile ? TouchFont(22) : 24, pale, true);
            string identity = session.HasStarted ? GameBalance.ClassName(session.Progression.Profile.heroClass) + " · " +
                session.Progression.Profile.level + "级\n保存成功后再离开，地面战利品会先收存。" : "当前没有进行中的冒险。";
            Text(new Rect(r.x + 24 * unit, r.y + 77 * unit, r.width - 48 * unit, 76 * unit), identity,
                mobile ? TouchFont(15) : 17, muted, false, true);
            if (!string.IsNullOrEmpty(exitError)) Text(new Rect(r.x + 24 * unit, r.y + 150 * unit, r.width - 48 * unit, 52 * unit),
                exitError, mobile ? TouchFont(13) : 14, gold, false, true);
            float bw = (r.width - 64 * unit) / 2;
            if (NavigationButton(new Rect(r.x + 24 * unit, r.yMax - 68 * unit, bw, 48 * unit), "取消", jade, !exitRequest.Busy))
            {
                exitRequest.Cancel();
                exitError = null;
                BlockUITransition();
                return;
            }
            if (DangerButton(new Rect(r.x + 40 * unit + bw, r.yMax - 68 * unit, bw, 48 * unit), exitRequest.ToTitle ? "保存并返回" : "保存并退出", gold, !exitRequest.Busy))
            {
                try
                {
                    exitRequest.Confirm(() =>
                    {
                        bool ok = session.SaveBeforeLeaving();
                        if (!ok) exitError = ExitFailureMessage();
                        return ok;
                    }, toTitle =>
                    {
                        // SafeExitRequest calls this only after durable preservation.
                        // Session APIs retain their own safety check for other callers.
                        bool accepted = toTitle ? session.QuitToTitle(true) : session.ExitApplication(true);
                        if (!accepted) { exitError = ExitFailureMessage(); return false; }
                        if (toTitle) { panel = Panel.None; saveSlotsDirty = true; }
                        BlockUITransition();
                        return true;
                    });
                }
                catch (System.Exception exception)
                {
                    // Retain confirmation/preparation state so the player can cancel
                    // or retry rather than disappearing behind the paused menu.
                    exitError = "离开未完成：" + exception.Message;
                }
            }
        }

        private string ExitFailureMessage()
        {
            return string.IsNullOrEmpty(session.Progression.LastError)
                ? "暂时无法离开，请重试或取消。" : session.Progression.LastError;
        }
    }
}
