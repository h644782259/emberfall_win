using UnityEngine;

namespace Emberfall
{
    public sealed partial class GameUI
    {
        // Roles describe the operation; item rarity and class colors do not change them.
        private enum ButtonRole { Action, Primary, Navigation, Danger, Tab, SelectedTab, Toggle, ActiveToggle, Row, SelectedRow }

        private bool NavigationButton(Rect rect, string caption, Color accent, bool enabled = true, string hint = null, bool primary = false)
        { return DrawButton(rect, caption, ButtonRole.Navigation, enabled, hint); }

        private bool DangerButton(Rect rect, string caption, Color accent, bool enabled = true, string hint = null, bool primary = false)
        { return DrawButton(rect, caption, ButtonRole.Danger, enabled, hint); }

        private bool PrimaryButton(Rect rect, string caption, Color accent, bool enabled = true, string hint = null, bool primary = false)
        { return DrawButton(rect, caption, ButtonRole.Primary, enabled, hint); }

        private bool TabButton(Rect rect, string caption, bool selected, bool enabled = true, string hint = null)
        { return DrawButton(rect, caption, selected ? ButtonRole.SelectedTab : ButtonRole.Tab, enabled, hint); }

        private bool ToggleButton(Rect rect, string caption, bool active, bool enabled = true, string hint = null)
        { return DrawButton(rect, caption, active ? ButtonRole.ActiveToggle : ButtonRole.Toggle, enabled, hint); }

        private bool DrawButton(Rect rect, string caption, ButtonRole role, bool enabled = true, string hint = null, int fontSize = 0, string controlName = null)
        {
            enabled = enabled && GUI.enabled;
            bool hover = rect.Contains(Mouse) && enabled;
            bool selected = role == ButtonRole.SelectedTab;
            bool primary = role == ButtonRole.Primary;
            bool navigation = role == ButtonRole.Navigation;
            bool danger = role == ButtonRole.Danger;
            bool tab = selected || role == ButtonRole.Tab;
            bool toggle = role == ButtonRole.Toggle || role == ButtonRole.ActiveToggle;
            bool active = role == ButtonRole.ActiveToggle;
            bool row = role == ButtonRole.Row || role == ButtonRole.SelectedRow;
            bool selectedRow = role == ButtonRole.SelectedRow;
            Color accent = danger ? new Color(1f, .43f, .4f) : primary || selected ? gold : jade;
            Color background = primary || selected ? new Color(.94f, .68f, .29f) :
                danger ? new Color(.27f, .075f, .085f) :
                navigation || tab || row && !selectedRow || toggle && !active ? new Color(.055f, .09f, .12f) : new Color(.055f, .255f, .225f);
            Color foreground = primary || selected ? new Color(.065f, .05f, .025f) :
                danger ? new Color(1f, .76f, .72f) : navigation ? new Color(.71f, .94f, .89f) : pale;
            Color outline = tab && !selected ? new Color(.22f, .32f, .38f) :
                navigation || toggle && !active ? new Color(.25f, .49f, .47f) : accent;
            if (hover) background = Color.Lerp(background, Color.white, primary || selected ? .1f : .09f);
            if (!enabled)
            {
                background = new Color(.075f, .095f, .115f);
                foreground = new Color(.68f, .72f, .76f);
                outline = new Color(.21f, .26f, .3f);
            }
            Fill(rect, background);
            Border(rect, outline, enabled && (primary || selected || danger || selectedRow) ? 2f : 1f);
            float unit = MobileControls.Active ? TouchRatio : 1f;
            if (selected)
                Fill(new Rect(rect.x + 2f, rect.yMax - 4f * unit, rect.width - 4f, 3f * unit),
                    enabled ? new Color(1f, .92f, .68f) : outline);
            else if (!navigation && !tab && !toggle && !primary && !row)
                Fill(new Rect(rect.x + 1f, rect.y + 1f, 3f * unit, rect.height - 2f), enabled ? accent : outline);
            float inset = 8f * unit;
            float trailing = navigation && rect.width >= 90f * unit && caption != "×" && !string.IsNullOrEmpty(caption) && !caption.Contains("›") && !caption.Contains("‹") ? 20f * unit : 0f;
            float leading = toggle ? 20f * unit : 0f;
            if (toggle)
            {
                Rect indicator = new Rect(rect.x + inset, rect.center.y - 4f * unit, 8f * unit, 8f * unit);
                if (active && enabled) Fill(indicator, jade);
                else Border(indicator, enabled ? outline : foreground);
            }
            int size = fontSize > 0 ? fontSize : MobileControls.Active && (rect.height >= 44f * unit || tab) ? TouchFont(15) : 15;
            Text(new Rect(rect.x + inset + leading, rect.y, Mathf.Max(0, rect.width - inset * 2f - trailing - leading), rect.height),
                caption, size, foreground, true, false, TextAnchor.MiddleCenter);
            if (trailing > 0)
                Text(new Rect(rect.xMax - trailing - inset, rect.y, trailing, rect.height), "›", size + 3,
                    foreground, true, false, TextAnchor.MiddleCenter);
            if (hover && !string.IsNullOrEmpty(hint)) tooltip = hint;
            // Disabled controls still expose the reason they cannot be used.
            if (!enabled && rect.Contains(Mouse) && GUI.enabled && !string.IsNullOrEmpty(hint)) tooltip = hint;
            bool prior = GUI.enabled;
            GUI.enabled = enabled && prior;
            if (!string.IsNullOrEmpty(controlName)) GUI.SetNextControlName(controlName);
            bool clicked = GUI.Button(rect, GUIContent.none, invisibleButton);
            GUI.enabled = prior;
            if (clicked) GameAudio.Play(SoundCue.UI);
            return clicked;
        }
    }
}
