#!/usr/bin/env python3
"""Verified adapter ordering and pickup ownership contracts, not engine execution."""
from pathlib import Path
import re
root=Path(__file__).resolve().parent.parent
read=lambda path:(root/path).read_text()
session=read('Assets/Scripts/Core/GameSession.cs');service=read('Assets/Scripts/Core/ProgressionService.cs')
rooms=read('Assets/Scripts/Core/GameSession.RoomChain.cs');player=read('Assets/Scripts/Combat/PlayerController.cs');pickup=read('Assets/Scripts/World/GroundLootPickup.cs')
def body(source,signature):
 start=source.index('{',source.index(signature));depth=1
 for end in range(start+1,len(source)):
  depth+=(source[end]=='{')-(source[end]=='}')
  if depth==0:return source[start+1:end]
 raise AssertionError(signature)
checks=[]
def check(ok,why):
 if not ok:raise AssertionError(why)
 checks.append(why)
for src,signature in [(session,'private bool ChangeZone('),(rooms,'public bool EnterNextRoom()')]:
 transition=body(src,signature)
 if src is rooms:
  check(transition.index('if(!SaveBeforeLeaving())return false;')<transition.index('return EnterNextRoomAfterSave();'),'room adapter requires successful save before entering transition helper')
  transition += body(rooms,'private bool EnterNextRoomAfterSave()')
 sequence=['SaveBeforeLeaving()', 'changingZone', 'previousCombatEpoch', 'Enemies.Clear()', 'transientObjects.Clear()', 'world.SetActive(false)', 'RetireCombatForWorldTransition()', 'RetireWorldLootReceipts(previousCombatEpoch)', 'WorldBuilder.Build(', 'Player.Teleport(']
 positions=[re.search(r"(?<!\w)Enemies.Clear\(\)", transition).start() if token == "Enemies.Clear()" else transition.index(token) for token in sequence]
 check(positions==sorted(positions),signature+' retires old producers and epoch after checked persistence and before construction/spawn')
 check('obj.SetActive(false)' in transition,'transient producers deactivate synchronously before deferred destruction')
 check(transition.count('RetireWorldLootReceipts(')==1,'exactly one receipt boundary per transition')
retire=body(service,'internal bool TryRetireWorldLootReceipts')
check('!transitionCommitted || pendingGroundLoot != 0 || !oldProducersRetired || !combatEpochRetired' in retire,'receipt retirement requires all four safety conditions')
check(retire.index('return false')<retire.index('collectedLootIds.Clear()'),'failed guards cannot release any receipt')
check('Save(' not in retire and 'RaiseChanged' not in retire,'receipt release cannot save or emit a profile event')
adapter=body(session,'private void RetireWorldLootReceipts')
for condition in ['!world.activeInHierarchy','Enemies.Count == 0','transientObjects.Count == 0','pendingLoot.Count','Player.CombatEpoch != previousCombatEpoch']:
 check(condition in adapter,'adapter proves '+condition)
check(adapter.index('Progression.TryRetireWorldLootReceipts(')<adapter.index('collectedGroundLoot.Clear()'),'both receipt sets share the same accepted boundary')
for signature in ['public bool SaveBeforeLeaving()', 'private IEnumerator NextWave()', 'public void CollectRemainingDungeonLoot()']:
 check('RetireWorldLootReceipts(' not in body(session,signature),'no receipt release during '+signature)
check('collectedLootIds.Clear()' not in body(service,'public bool Sell('),'manual sale retains world receipt')
invalidate=body(player,'internal void RetireCombatForWorldTransition')
check('CombatEpoch++' in invalidate and 'ResetCooldowns' not in invalidate and 'Teleport(' not in invalidate,'early invalidation cancels old effects without relocation or skill reset')
kill=body(session,'public void OnEnemyKilled')
check('Progression.CreateLoot(' not in kill and 'Progression.RollLoot(' in kill and 'DeliverEnemyLoot(loot, position)' in kill,'enemy callback rolls exactly once then uses checked delivery')
delivery=body(session,'private void DeliverEnemyLoot')
check('SpawnGroundLoot(loot,position)' in delivery.replace(' ', '') and 'Progression.CollectLoot' not in delivery,'all enemy equipment lands before automatic collection')
check('LogSystem("获得' not in delivery,'ground spawn cannot announce an uncommitted pickup')
spawn=body(session,'public GroundLootPickup SpawnGroundLoot')
check('!InDungeon' not in spawn and 'changingZone' in spawn and '!world.activeInHierarchy' in spawn,'pending ground drops support wilderness but never a retired world')
backpressure=body(session,'private void SpawnWildernessEnemy')
check(backpressure.index('if (pendingLoot.Count > 0) return;')<backpressure.index('for('),'first retained drop stops replacement producers without deleting existing drops')
ownership=body(session,'internal bool IsCurrentGroundLoot')
check('object.ReferenceEquals(pending.Pickup, pickup)' in ownership and 'pickup.transform.IsChildOf(world.transform)' in ownership,'automatic collector must own the exact pending instance in this world')
update=body(pickup,'private void Update()')
check('!session.InDungeon' not in update and '!session.IsCurrentGroundLoot(this)' in update,'wilderness pickup update validates live ownership')
check('session.TryCollectGroundLoot(this)' in update and 'retryTime = RetryDelay' in update and 'retryTime <= 0' in update and 'ReadyToCollect' in update,'failed automatic retries back off and use identity-aware collection')
collect=body(session,'public bool TryCollectGroundLoot(string itemId')
check(collect.index('try { accepted = source.CollectLoot(pending.Item); }')<collect.index('finally')<collect.index('pending.Collecting = false;'),'collection always releases its reentrancy guard, including exceptions')
check(collect.index('if (accepted || source.HasCommittedWorldLoot(itemId))')<collect.index('pendingLoot.Remove(itemId)')<collect.index('pending.Pickup.Retire()'),'only accepted or committed receipt may retire exact pending item and body')
check('object.ReferenceEquals(current, pending)' in collect,'observer world replacement cannot retire a different pending identity')
fixture=read('Assets/Editor/GroundLootValidation.cs');blocked=read('Assets/Editor/PersistenceTransitionValidation.cs')
check('IEnumerator wilderness = ValidateWildernessRetention' in fixture and 'ValidateRoomReceiptBoundary(game, fixture, check)' in fixture,'prepared engine runner dispatches wilderness retention and successful room boundary checks')
check('automaticCollect.Invoke(game' in fixture and 'ReadPickupRetry(pending) > 0' in fixture,'prepared engine fixture covers stale collector ownership and real retry backoff')
check('sessionReceipts.Contains(receipt) && serviceReceipts.Contains(receipt)' in blocked,'prepared blocked-transition fixture checks both retained receipt sets')
print('PASS:',len(checks),'world loot receipt/retention source contracts (not Unity execution)')
