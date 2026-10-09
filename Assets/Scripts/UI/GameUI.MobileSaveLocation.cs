using System;
using System.IO;
using UnityEngine;

namespace Emberfall
{
    public sealed partial class GameUI
    {
        private Vector2 mobileSaveLocationScroll;
        private string mobileSaveLocationPath, mobileSaveLocationFile;
        private string mobileSaveLocationStatus;
        private bool mobileSaveLocationFailed;
        private float mobileSaveLocationWidth, mobileSaveLocationRatio;

        private void DrawMobileSaveLocation()
        {
            var layout = MobilePanelGeometry();
            string path = session.Progression.SaveDirectory;
            string file = Path.GetFileName(session.Progression.SaveFilePath);
            float contentWidth = layout.Body.Width - 16;
            bool identityChanged = mobileSaveLocationPath != path || mobileSaveLocationFile != file;
            if (identityChanged || mobileSaveLocationWidth != contentWidth || mobileSaveLocationRatio != TouchRatio)
            {
                mobileSaveLocationPath = path; mobileSaveLocationFile = file;
                mobileSaveLocationWidth = contentWidth; mobileSaveLocationRatio = TouchRatio;
                mobileSaveLocationScroll = Vector2.zero;
                if (identityChanged) mobileSaveLocationStatus = null;
            }
            if (DrawMobilePanelChrome(layout, "存档位置与迁移", "备份与迁移角色进度")) return;
            float contentHeight = DrawMobileSaveLocationContent(contentWidth, false);
            mobileSaveLocationScroll = BeginTouchScroll("mobile-save-location", MobilePanelRect(layout.Body), mobileSaveLocationScroll,
                new Rect(0, 0, contentWidth * TouchRatio, Mathf.Max(layout.Body.Height, contentHeight) * TouchRatio));
            DrawMobileSaveLocationContent(contentWidth, true);
            EndTouchScroll();
#if UNITY_IOS || UNITY_ANDROID
            const int buttons = 2;
#else
            const int buttons = 3;
#endif
            if (Button(MobilePanelRect(layout.FooterButton(0, buttons)), "复制存档位置", gold))
            {
                try { GUIUtility.systemCopyBuffer = path; MobileSaveLocationResult(true, "已复制存档位置"); }
                catch (Exception error) { MobileSaveLocationResult(false, "复制未完成：" + error.Message); }
                return;
            }
#if !UNITY_IOS && !UNITY_ANDROID
            if (NavigationButton(MobilePanelRect(layout.FooterButton(1, buttons)), "打开目录", jade))
            {
                try
                {
                    if (!Directory.Exists(path)) MobileSaveLocationResult(false, "存档目录尚不存在。请先成功保存角色，再打开目录。");
                    else
                    {
                        string absolute = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
                        Application.OpenURL(new Uri(absolute).AbsoluteUri);
                        MobileSaveLocationResult(true, "已请求系统打开存档目录");
                    }
                }
                catch (Exception error) { MobileSaveLocationResult(false, "无法打开目录：" + error.Message); }
                return;
            }
#endif
            if (saveReturnPause && NavigationButton(MobilePanelRect(layout.FooterButton(buttons - 1, buttons)), "返回设置", jade))
            { ClosePanel(); BlockUITransition(); }
        }

        private float DrawMobileSaveLocationContent(float width, bool draw)
        {
            float y = 8;
            if (!string.IsNullOrEmpty(mobileSaveLocationStatus))
                MobileSaveLocationParagraph(ref y, width, mobileSaveLocationStatus, 14, mobileSaveLocationFailed ? gold : jade, draw, true);
            MobileSaveLocationParagraph(ref y, width, "角色存档", 16, jade, draw, true);
            MobileSaveLocationParagraph(ref y, width, session.Progression.HasActiveSave ? "角色进度已保存在本机。" : "当前没有可用角色存档。", 14, pale, draw);
            MobileSaveLocationParagraph(ref y, width, "备份与迁移", 16, jade, draw, true);
            MobileSaveLocationParagraph(ref y, width, "迁移前，请退出两台设备上的游戏，并备份整个存档文件夹。复制时保留全部文件，再从角色列表读取进度。", 14, pale, draw);
#if UNITY_IOS || UNITY_ANDROID
            MobileSaveLocationParagraph(ref y, width, "复制存档位置不会导出存档；能否访问文件取决于设备系统。", 14, muted, draw);
#else
            MobileSaveLocationParagraph(ref y, width, "可通过下方按钮打开存档文件夹，或复制位置供迁移时使用。", 14, muted, draw);
#endif
            return y + 8;
        }

        private void MobileSaveLocationParagraph(ref float y, float width, string text, int size, Color color, bool draw, bool bold = false)
        {
            y += draw ? DrawMobileParagraph(8, y, width - 16, text, size, color, bold) : MeasureMobileParagraph(text, width - 16, size, bold);
            y += 12;
        }

        private void MobileSaveLocationResult(bool success, string message)
        {
            mobileSaveLocationStatus = message; mobileSaveLocationFailed = !success;
            CancelMobileScroll(); mobileSaveLocationScroll = Vector2.zero;
            session.Notify(success ? message : "操作未完成 · 详情见下方提示");
            BlockUITransition();
        }
    }
}
