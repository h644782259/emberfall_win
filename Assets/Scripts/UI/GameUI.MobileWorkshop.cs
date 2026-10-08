using System;
using System.Collections.Generic;
using UnityEngine;

namespace Emberfall
{
    public sealed partial class GameUI
    {
        private string mobileWorkshopStatus;
        private bool mobileWorkshopFailed;

        private void MobileWorkshopParagraph(ref float y, float width, string text, Color color, bool draw, bool bold = false, int size = 14)
        {
            y += draw ? DrawMobileParagraph(8, y, width - 16, text, size, color, bold) : MeasureMobileParagraph(text, width - 16, size, bold);
            y += 10;
        }

        private void MobileWorkshopAction(ref float y, float width, string caption, Color color, bool enabled, bool draw, Action action, ButtonRole role = ButtonRole.Action)
        {
            float actionWidth=Mathf.Min(width-16,Mathf.Max(88,Style(TouchFont(12),false).CalcSize(new GUIContent(caption)).x/TouchRatio+24));
            if (draw && QuietAction(TouchRect(8,y,actionWidth,48),caption,enabled)) action();
            y += 58;
        }

        private void MobileWorkshopResult(bool accepted, string message)
        {
            mobileWorkshopFailed = !accepted || !string.IsNullOrEmpty(session.Progression.LastError);
            mobileWorkshopStatus = mobileWorkshopFailed ? (string.IsNullOrEmpty(session.Progression.LastError) ? "当前操作未完成，请重试。" : session.Progression.LastError) : message;
            CancelMobileScroll();
            // The fixed header carries feedback; preserve the current reading/action position.
            Feedback(!mobileWorkshopFailed, mobileWorkshopStatus);

        }

        private void DrawMobileWorkshopAbilities(ref float y,float width,bool draw)
        {y=DrawSkillDevelopmentContent(width,TouchRatio,draw);}
    }
}
