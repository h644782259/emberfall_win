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
