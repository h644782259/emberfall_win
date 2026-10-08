using System;
using System.IO;
using UnityEngine;

namespace Emberfall
{
    public sealed partial class GameUI
    {
        private Vector2 mobileSaveLocationScroll;
        private string mobileSaveLocationPath, mobileSaveLocationFile, mobileSaveDisplayPath, mobileSaveDisplayFile;
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
                GUIStyle style = Style(TouchFont(14), false, false);
                Func<string, float> measure = value => style.CalcSize(new GUIContent(value)).x / TouchRatio;
                mobileSaveDisplayPath = MobileSavePathText.Wrap(path, contentWidth - 16, measure);
                mobileSaveDisplayFile = MobileSavePathText.Wrap(file, contentWidth - 16, measure);
                mobileSaveLocationScroll = Vector2.zero;
                if (identityChanged) mobileSaveLocationStatus = null;
            }
            if (DrawMobilePanelChrome(layout, "存档位置与迁移", "复制完整路径 · 迁移前保留备份")) return;
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
            if (Button(MobilePanelRect(layout.FooterButton(0, buttons)), "复制完整目录路径", gold))
            {
                try { GUIUtility.systemCopyBuffer = path; MobileSaveLocationResult(true, "已复制完整目录路径"); }
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
            MobileSaveLocationParagraph(ref y, width, "当前存档文件夹", 16, jade, draw, true);
            MobileSaveLocationParagraph(ref y, width, mobileSaveDisplayPath, 14, pale, draw);
            MobileSaveLocationParagraph(ref y, width, "当前角色文件", 16, jade, draw, true);
            MobileSaveLocationParagraph(ref y, width, session.Progression.HasActiveSave ? mobileSaveDisplayFile : "当前没有可用角色存档；原文件可能已移除。", 14, pale, draw);
            MobileSaveLocationParagraph(ref y, width, "长路径在界面中折行显示；复制按钮保留完整原始路径，不包含显示折行。", 14, muted, draw);
#if UNITY_IOS || UNITY_ANDROID
            MobileSaveLocationParagraph(ref y, width, "移动端使用应用私有存档目录。本页可复制路径；不会尝试用桌面文件夹链接打开它。复制路径不会导出存档，文件访问和导出取决于系统提供的方式。", 14, muted, draw);
#else
            MobileSaveLocationParagraph(ref y, width, "存档与游戏安装目录分开保存。可用下方按钮请求系统打开此目录，或复制完整路径后粘贴到文件管理器。", 14, muted, draw);
#endif
            MobileSaveLocationParagraph(ref y, width, "迁移前先退出两端游戏，并保留原目录备份。完整保留角色 .json、对应 .bak 备份、可恢复的 .tmp 文件以及 .delete-pending 删除标记；不要单独搬运旧备份或省略删除标记。", 14, pale, draw);
            MobileSaveLocationParagraph(ref y, width, "新设备的目标目录也以游戏显示的路径为准。迁移后从角色存档列表读取；本页只显示和复制路径，不会移动、覆盖或删除角色文件。", 14, muted, draw);
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
