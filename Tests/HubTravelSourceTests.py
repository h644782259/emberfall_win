#!/usr/bin/env python3
"""Read-only hub UI/atomic economy wiring checks; no player files or Unity runtime."""
from pathlib import Path
root = Path(__file__).resolve().parent.parent
read = lambda p: (root / p).read_text(encoding='utf-8')
checks = []
def check(ok, label):
    if not ok: raise AssertionError(label)
    checks.append(label)
def method(source, name):
    start = source.index(name)
    brace = source.index('{', start)
    depth = 1
    end = brace + 1
    while depth:
        if source[end] == '{': depth += 1
        elif source[end] == '}': depth -= 1
        end += 1
    return source[brace:end]
ui = read('Assets/Scripts/UI/GameUI.cs')
mobile = read('Assets/Scripts/UI/GameUI.Mobile.cs')
hubs = read('Assets/Scripts/UI/GameUI.Hubs.cs')
progression = read('Assets/Scripts/Core/ProgressionService.cs')
session = read('Assets/Scripts/Core/GameSession.Hubs.cs')
check('TravelMap' in method(ui, 'private enum Panel') and 'panel == Panel.TravelMap) DrawTravelMap();' in ui, 'travel map has distinct panel dispatch')
check('OpenTravelMap();' in method(ui, 'private void DrawMinimap()') and 'DrawHubActions(' in method(ui, 'private void DrawEdgeActions()'), 'desktop map and edge entry are actionable')
check('OpenTravelMap();' in method(mobile, 'private void DrawMobileHUD()') and 'OpenTravelMap();' in method(mobile, 'private void DrawMobilePause()'), 'mobile map and pause expose travel')
check('CloseTravelMap()' in method(ui, 'private void ClosePanel()') and 'session.SetPaused(travelReturnPause)' in hubs, 'back/Esc retains pause-origin navigation')
check('panel == Panel.TravelMap' in method(ui, 'private void Update()').split('if (Input.GetKeyDown(KeyCode.I))')[0] and
      'panel == Panel.Notice || panel == Panel.Chapter) return;' in ui, 'travel and message modals guard keyboard panel replacement')
check('HubInventoryTitle' in method(ui, 'private void DrawInventory()') and 'HubInventoryHint' in method(ui, 'private void DrawInventory()'), 'merchant and blacksmith preserve inventory service UI with explicit context')
npc = method(hubs, 'private void OpenNearbyHubNpc()')
check('session.NearbyHubNpc' in npc and 'kind == HubNpcKind.None' in npc and 'session.InputBlocked' in npc, 'NPC action rechecks actual safe proximity')
chapter = (root/'Assets/Scripts/UI/GameUI.Chapter.cs').read_text()
check('OpenChapterSelection();return;' in npc and 'campTab=1;panel=Panel.Camp;' in method(chapter, 'private void OpenChapterExchange()') and 'panel = Panel.Inventory;' in npc, 'exchange remains reachable from shared chapter page and merchants retain their service')
check('selectedItem = session.Progression.Profile.weaponId;' in npc, 'blacksmith selects worn item for existing upgrade controls')
check('OpenNearbyHubNpc();' in method(mobile, 'public void ActivateMobileInteraction(') and 'HubNpcMobileLabel(session.NearbyHubNpc)' in method(mobile, 'private void DrawMobileHUD()'), 'mobile nearby prompt and touch-owner action dispatch same NPC')
travel = method(hubs, 'private void DrawTravelMap()')
check('HubTravelRules.Count' in travel and 'HubTravelRules.IsUnlocked' in travel and 'HubTravelRules.UnlockHint' in travel, 'three town cards show actual unlock state and requirement')
check('unlocked && !current && session.CanOpenTravelMap && !UITransitionBlocked' in travel, 'locked/current/combat travel buttons are disabled with repeat-tap guard')
check('if (session.TravelToHub(hub))' in travel and travel.index('if (session.TravelToHub(hub))') < travel.index('CloseTravelMap();') and 'travelError =' in travel, 'failed travel retains modal and shows error')
check('BlockUITransition();' in method(hubs, 'private void OpenTravelMap()') and 'BlockUITransition();' in method(hubs, 'private bool CloseTravelMap()'), 'opening and closing consume repeat input')
check('HubTravelRules.CanTravel(HasStarted,InDungeon,IsDead,nearby,changingZone)' in session and 'Progression.TravelToHub(hub)' in session, 'runtime rechecks challenge/death/enemy guard and persists chosen hub')
for signature in ['public bool Sell(string id,bool confirmPresetReferences=false)', 'public bool BuyPotion()', 'public int BulkSellLowQuality(bool confirmPresetReferences=false)']:
    body = method(progression, signature)
    check('Snapshot()' in body and 'CommitCandidate(candidate)' in body and 'Commit();' not in body and 'Profile.gold =' not in body and 'Profile.inventory.Remove' not in body,
          signature + ' publishes a candidate only on successful persistence')
check('IsEquipped(candidate, item.id)' in method(progression, 'public int BulkSellLowQuality(bool confirmPresetReferences=false)') and 'IsProtectedLoot(item)' in method(progression, 'public int BulkSellLowQuality(bool confirmPresetReferences=false)'), 'bulk retains equip/lock/mechanic/upgrade/rarity protections')
check('HubTravelRules.UnlockedMask(profile.unlockedHubMask,profile.level,profile.clearedRuns)' in progression and 'HubTravelRules.SafeCurrent(profile.currentHub,profile.unlockedHubMask)' in progression, 'load migration bounds unlocks and restores valid hub')
print('PASS:', len(checks), 'hub travel and economy source contracts (not Unity execution)')
