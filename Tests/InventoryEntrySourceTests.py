"""Source wiring contract only; does not prove Unity rendering or device usability."""
from pathlib import Path
r=Path(__file__).resolve().parents[1]/'Assets/Scripts/UI'
grid=(r/'GameUI.InventoryGrid.cs').read_text(); wear=(r/'GameUI.WearMap.cs').read_text(); scroll=(r/'GameUI.TouchScroll.cs').read_text(); bag=(r/'GameUI.MobileInventory.cs').read_text()
assert not (r/'GameUI.EquipmentAppearance.cs').exists()
assert 'GUI.enabled=false' not in scroll.replace(' ','')
assert '"穿戴"' in grid and '"对比"' in grid and 'SetItemLocked(lockId,target)' in grid
assert 'DrawCurrentWear' in bag and 'DrawBagSupplies' in bag and 'DrawBagFashion' in bag
assert 'wearModel.Render' in wear and 'collectionTrial' in wear
assert 'BuyPotion(' not in wear and '.Upgrade(' not in wear and 'SellInventoryItem(' not in wear
assert 'session.DrinkPotion(' in wear
assert 'EquipmentComparisonPresentation.Changes' in grid and 'EquipmentComparisonPresentation.Description' in grid
print('PASS inventory source wiring: current wear, inline actions, attributes/mechanics comparison, supplies and drag suppression (not visual QA)')

# Audit every UI entry, including desktop expedition and mobile reward mailbox.
for name in ['GameUI.MobileWorkshop.cs','GameUI.Expedition.cs','GameUI.InventoryGrid.cs','GameUI.MobileInventory.cs','GameUI.WearMap.cs','GameUI.CollectionPreview.cs']:
    source=(r/name).read_text()
    for call in ['RequestPresetSale(', 'SellInventoryItem(', '.Sell(', '.BulkSellLowQuality(', '.SetAutoSell(']:
        assert call not in source,(name,call)
hub=(r/'GameUI.HubServices.cs').read_text()
assert 'if(bulkSale)RequestPresetSale(null,true);' in hub
assert 'bool bulkSale=DangerButton' in hub
assert 'Rarity.Common,!p.Profile.autoSellCommon' in hub and 'Rarity.Rare,!p.Profile.autoSellRare' in hub
assert 'ClaimMobileWorkshopLoot' in (r/'GameUI.MobileWorkshop.cs').read_text()
assert 'ClaimPendingLoot(item.id)' in (r/'GameUI.Expedition.cs').read_text()
print('PASS merchant-only sale/settings entry audit, pending/recovery claims retained (source contract, not Unity execution)')
