namespace Emberfall
{
    public sealed partial class GameUI
    {
        private bool CloseMobileInventoryDetail()
        {
            // Comparison and fashion are inline bag surfaces on every device.
            if(panel!=Panel.Inventory||session.Paused)return false;
            if(inventoryFashionOpen){inventoryFashionOpen=false;collectionTrial=null;}
            else if(inventoryComparisonOpen){inventoryComparisonOpen=false;}
            else return false;
            mobileInventoryDetail=false;CancelMobileScroll();BlockUITransition();return true;
        }
    }
}
