"""Source wiring contract only; does not prove Unity rendering or device usability."""
from pathlib import Path
r=Path(__file__).resolve().parents[1]/'Assets/Scripts/UI'
grid=(r/'GameUI.InventoryGrid.cs').read_text(); wear=(r/'GameUI.WearMap.cs').read_text(); scroll=(r/'GameUI.TouchScroll.cs').read_text(); bag=(r/'GameUI.MobileInventory.cs').read_text()
assert not (r/'GameUI.EquipmentAppearance.cs').exists()
assert 'GUI.enabled=false' not in scroll.replace(' ','')
assert '"穿戴"' in grid and '"对比"' in grid and 'SetItemLocked(item.id,!item.locked)' in grid
assert 'DrawCurrentWear' in bag and 'DrawBagSupplies' in bag and 'DrawBagFashion' in bag
assert 'wearModel.Render' in wear and 'collectionTrial' not in wear
assert 'BuyPotion(' not in wear and '.Upgrade(' not in wear and 'SellInventoryItem(' not in wear
assert 'session.DrinkPotion(' in grid and 'OpenInventoryPopup("@potion",tile)' in wear
assert 'EquipmentComparisonPresentation.Changes' in grid and 'EquipmentComparisonPresentation.Description' in grid
print('PASS inventory source wiring: current wear, inline actions, attributes/mechanics comparison, supplies and drag suppression (not visual QA)')

# Audit every UI entry, including desktop expedition and mobile reward mailbox.
for name in ['GameUI.MobileWorkshop.cs','GameUI.Expedition.cs','GameUI.InventoryGrid.cs','GameUI.MobileInventory.cs','GameUI.WearMap.cs','GameUI.CollectionPreview.cs']:
    source=(r/name).read_text()
    for call in ['RequestPresetSale(', 'SellInventoryItem(', '.Sell(', '.BulkSellLowQuality(', '.SetAutoSell(']:
        assert call not in source,(name,call)
merchant=(r/'GameUI.Merchant.cs').read_text()
smith=(r/'GameUI.Smith.cs').read_text()
assert 'string[] tabs={"购买","兑换","出售"}' in merchant
assert '!gear.locked&&!IsEquipped(gear)' in merchant and 'SellInventoryItem(id)' in merchant and 'string id=merchantSaleId' in merchant
assert '.SetAutoSell(' not in merchant and '.BulkSellLowQuality(' not in merchant
assert 'ClaimMobileWorkshopLoot' not in (r/'GameUI.MobileWorkshop.cs').read_text()
assert 'ClaimPendingLoot(item.id)' not in (r/'GameUI.Expedition.cs').read_text()
assert 'ToggleAttachmentVariant' in smith and 'SetAttachmentMounted' in smith
print('PASS merchant explicit-sale/exchange and smith attachment entry audit; no auto-sale or pending-claim controls')
workshop=(r/'GameUI.MobileWorkshop.cs').read_text()
assert 'layout.FooterButton(' not in workshop
assert 'DrawEquipmentIconGrid(MobilePanelRect(viewport),ref mobileInventoryListScroll,TouchRatio)' in grid
assert 'DrawInventoryIcon(tile,item,u)' in grid and 'OpenInventoryPopup(chosen,anchor)' in grid
assert 'pip<=(int)item.rarity' in grid and '"L"+item.level' in grid
assert 'DrawInventorySortIcon(' not in bag.split('private string MobileInventoryFilterLabel')[0]
assert 'if(inventoryComparisonOpen&&picked!=null)' not in bag
print('PASS icon-only grid and same-page contextual popup wiring; no always-visible row actions or sorting entry')

assert 'mobileInventoryPicker' not in bag and 'DrawMobileInventoryPicker' not in bag and 'CycleMobileInventoryFilter' not in bag
assert 'MobileInventoryFilterLabel+" ▾"' not in bag
assert 'InventoryGridGeometry.FilterButton(full,index)' in grid and 'viewport.width-=InventoryGridGeometry.FilterRailWidth*u' in grid
assert 'inventoryFilter=index-1;scroll=Vector2.zero;RebuildBagItems();ResolveSelectedItem();' in grid
assert 'string[] filters={"全部","武器","护甲","饰品"}' in grid
hud=(r/'GameUI.Mobile.cs').read_text();controls=(r/'MobileControls.cs').read_text()
assert 'SkillPageArrow()' in hud and '↻' not in hud and 'Text(pageHit' not in hud
assert 'DrawMobileControlSurface(r,ready,pressed)' in hud and 'UIIconAtlas.SkillGlyph(p.heroClass,skill,48)' in hud
assert 'ControlRing(true)' in hud and 'ControlRing()' in hud and 'ControlRing()' in controls
assert 'for(int segment=0;segment<40' not in hud and 'for(int i=0;i<40' not in controls
print('PASS source wiring: permanent inline four-filter rail, no picker or sort entry, graphic-only page switching and continuous skill/action rims (not visual acceptance)')

smith=(r/'GameUI.Smith.cs').read_text()
assert 'DrawServiceBalances(BuildPlanRect(l.Balance,u),u)' in merchant and 'DrawServiceBalances(BuildPlanRect(l.Balance,u),u)' in smith
assert 'merchantMode==2' in merchant and 'merchantMode==1?mechanics[index]' in merchant and 'merchantMode==0?1' in merchant
assert 'merchantSaleId=null;merchantSelection=-1' in merchant and 'StartMerchantAction()' in merchant
assert 'UIIconAtlas.Utility("confirm")' in merchant and '已选中' in merchant
assert 'TextAnchor.MiddleLeft' in merchant and 'r.x+23*unit' in merchant
assert '费用：' not in smith and '永久提升此部位' not in smith and '兑换请找商人' not in smith and '此部位暂无已拥有' not in smith and '挂件与装备基础属性分开保留' not in smith
assert 'if(!capped)DrawPriceTint' in smith and 'affordable?gold:new Color(.98f,.28f,.24f)' in smith
assert smith.index('if(PrimaryButton(action,"",gold,quote!=null))')<smith.index('if(!capped)DrawPriceTint')
print('PASS merchant mode/resource/selection and smith inline cost/disabled-red wiring (not rendered UI acceptance)')

assert 'session.Progression.Profile.inventory.Find(entry=>entry!=null&&entry.id==id)' in (r/'GameUI.cs').read_text()
