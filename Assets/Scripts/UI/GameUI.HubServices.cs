using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private bool SmithServiceActive {get{return inventoryHubNpc==HubNpcKind.Blacksmith&&HubServicesAvailable;}}
        private bool MerchantServiceActive {get{return inventoryHubNpc==HubNpcKind.Merchant&&HubServicesAvailable;}}
        private Vector2 hubServiceScroll;
        private bool merchantExchangeOpen;
        private void DrawHubEquipmentService()
        {
            bool smith=inventoryHubNpc==HubNpcKind.Blacksmith&&HubServicesAvailable;
            if(smith){DrawSmithService();return;}
            DrawMerchantService();
        }
    }
}
