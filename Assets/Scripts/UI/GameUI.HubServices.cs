using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private bool SmithServiceActive {get{return session.ActiveHubNpc==HubNpcKind.Blacksmith;}}
        private bool MerchantServiceActive {get{return session.ActiveHubNpc==HubNpcKind.Merchant;}}
        private Vector2 hubServiceScroll;
        private bool merchantExchangeOpen;
        private void DrawHubEquipmentService()
        {
            bool smith=session.ActiveHubNpc==HubNpcKind.Blacksmith;
            if(smith){DrawSmithService();return;}
            DrawMerchantService();
        }
    }
}
