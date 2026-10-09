using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private bool MobileSkillRowClicked(Rect row)
        { return GUI.enabled && touchScrollSuppressed != Time.frameCount && GUI.Button(row, GUIContent.none, invisibleButton); }
        // Details are a persistent column; closing the page no longer consumes a hidden popup step.
        private bool CloseMobileSkillDetail() { return false; }
        private void ResetMobileSkillNavigation() { routeSkillOwner=null;routeSkillSlot=null; CancelMobileScroll(); }
    }
}
