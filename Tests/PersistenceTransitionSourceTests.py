#!/usr/bin/env python3
"""Adapter order contracts. These inspect source; they do not claim Unity execution."""
from pathlib import Path

root = Path(__file__).resolve().parent.parent
read = lambda path: (root / path).read_text(encoding="utf-8")
checks = []
def check(ok, why):
    if not ok:
        raise AssertionError(why)
    checks.append(why)

session = read("Assets/Scripts/Core/GameSession.cs")
zone = session[session.index("private bool ChangeZone("):session.index("private void SpawnWildernessEnemy(")]
preflight = zone.index("if (!loadingSaveSnapshot && !enteringChapter && !retryingRoomChain && !SaveBeforeLeaving()) return false;")
for mutation in ["changingZone = true;", "StopCoroutine(waveRoutine)", "Enemies.Clear();",
                 "Destroy(world)", "InDungeon = dungeon;", "ResetExpedition(dungeon);",
                 "WorldBuilder.Build(", "Player.Teleport(", "Player.RefreshStats(true);",
                 "Player.ResetCooldownsForDungeonEntry();", "BeginRoomChainScene()", "SpawnWildernessEnemy();"]:
    check(preflight < zone.index(mutation), "checked save precedes " + mutation)
chapter = read("Assets/Scripts/Core/GameSession.Chapter.cs")
chapterEntry=chapter[chapter.index("public bool ConfirmChapterEnter()"):chapter.index("private void ResetChapterRun()")]
check(chapterEntry.index("if(!SaveBeforeLeaving())return false;")<chapterEntry.index("Progression.TryBeginChapterNode(")<chapterEntry.index("enteringChapter=true;")<chapterEntry.index("ChangeZone(true)"),
      "chapter entry saves before adopting run identity and invoking its preflighted transition")
check("finally {enteringChapter=false;" in chapterEntry,"chapter preflight bypass is scoped and always restored")
check("Progression.Save(" not in zone, "no unchecked or duplicate tail save after world transition")
check(zone.count("ResetCooldownsForDungeonEntry()") == 1 and "if(dungeon)Player.ResetCooldownsForDungeonEntry();" in zone,
      "only successful dungeon entry resets cooldowns, once")

leave = session[session.index("public bool SaveBeforeLeaving()"):session.index("public bool ExitApplication(")]
check(leave.index("TrySettleDungeonReward()") < leave.index("TrySettleArenaReward()") < leave.index("PreserveWorldLoot()") < leave.index("Progression.Save();"),
      "leave checks rewards and loot before persisting the complete final live profile")
check("Notify(Progression.LastError); return false;" in leave, "a failed final save is visible and blocks leaving")

expedition = read("Assets/Scripts/Core/GameSession.Expedition.cs")
confirm = expedition[expedition.index("public void ConfirmDungeonSelection()"):expedition.index("private void ResetExpedition(")]
check(confirm.index("bool previousChallengeRun = ChallengeRun;") < confirm.index("ChallengeRun = SelectedChallengeMode;"), "entry remembers the old run flag before assigning the requested one")
failure = confirm[confirm.index("if(!ChangeZone(true))"):confirm.index("UpdateTimeScale();\n            Notify(")]
check("ChallengeRun=previousChallengeRun;DungeonSelectionOpen=true;UpdateTimeScale();Notify(Progression.LastError);return;" in failure,
      "failed entry restores run identity, reopens the selector, remains paused and reports failure")
check("ResetCooldowns" not in confirm, "portal selection itself never resets cooldowns")

load = session[session.index("private bool ContinueAdventure("):session.index("private void DiscardTransientAdventureForLoad()")]
check(load.index("DiscardTransientAdventureForLoad();") < load.index("Progression = candidate;") < load.index("loadingSaveSnapshot = true;") < load.index("try { BeginAdventure(); }"),
      "staged load clears old transients and publishes the candidate before its nonwriting scene build")
check("finally { loadingSaveSnapshot = false; }" in load, "staged loading guard is scoped and restored")
hubs = read("Assets/Scripts/Core/GameSession.Hubs.cs")
travel = hubs[hubs.index("public bool TravelToHub("):]
check(travel.index("!PreserveWorldLoot()||!Progression.TravelToHub(hub)") < travel.index("loadingSaveSnapshot=true;") < travel.index("ChangeZone(false)"),
      "hub bypass is allowed only after loot preservation and the destination transaction commit")
check("finally {loadingSaveSnapshot=false;}" in travel, "committed hub bypass cannot leak into later transitions")
respawn = session[session.index("public void Respawn()"):session.index("public void DrinkPotion()")]
check(respawn.index("if (!ChangeZone(false)) return;") < respawn.index("IsDead = false;"), "failed respawn transition retains the dead state")
new = session[session.index("public void StartNew("):session.index("public void ContinueGame()")]
check(new.index("if (!SaveBeforeLeaving()) return;") < new.index("Progression.CreateNewSlot(") < new.index("BeginAdventure();"),
      "new character settles the old run before selecting a new autosave target")
check(new.index("equipmentFingerprint = null;") < new.index("Progression.CreateNewSlot(") and "equipmentFingerprint = previousEquipment;" in new,
      "new-character publication cannot count old equipment as a new-character equipment tutorial action")
snapshot = session[session.index("public bool SaveAsNewSlot()"):session.index("private void BeginAdventure()")]
check(snapshot.index("if (!SaveBeforeLeaving()) return false;") < snapshot.index("Progression.SaveAsNewSlot();"),
      "explicit snapshot settles receipts to the original character before copying")
rooms = read("Assets/Scripts/Core/GameSession.RoomChain.cs")
roomTravel = rooms[rooms.index("public bool EnterNextRoom()"):rooms.index("private bool ConfirmRoomInterlude(")]
check(roomTravel.index("if(!SaveBeforeLeaving())return false;") < roomTravel.index("RoomChainRun.Next(") < roomTravel.index("Destroy(world)"),
      "room travel checks full persistence before advancing room identity or tearing down the scene")
check("ResetCooldownsForDungeonEntry" not in roomTravel, "room travel cannot reset skill cooldowns")

service = read("Assets/Scripts/Core/ProgressionService.cs")
write = service[service.index("private static bool TryWriteProfile("):service.index("private static void DeleteFailedSlotFile(")]
dedup = write.index("if (samePrimary && usableBackup) return true;")
for guard in ["File.Exists(primary + DeletionSuffix)", "if (createOnly &&", "Directory.Exists(temporary)",
              "发现可恢复的临时存档", "Encoding.UTF8.GetByteCount(json) > MaximumSaveBytes"]:
    check(write.index(guard) < dedup, "dedup cannot bypass " + guard)
check("string.Equals(existingDocument, json, StringComparison.Ordinal)" in write and "TryReadProfile(backup, out recovery" in write,
      "dedup requires exact document contents and a validated backup")
check(write.index("if (!usablePrimary && !usableBackup)") < write.index("new FileStream(temporary"), "both-damaged refusal happens before temporary writes")
check("GetLastWriteTime" not in write and "dirty" not in write.replace("// Compare the actual bounded document, never a dirty flag or file", ""),
      "save identity is not inferred from timestamps or a dirty marker")
reader = service[service.index("private static bool TryReadSaveDocument("):service.index("private static bool HasLegacyEnhancement(")]
check("length > MaximumSaveBytes" in reader and "new byte[(int)length]" in reader and "stream.ReadByte() != -1" in reader,
      "the opened stream has a capped allocation and detects growth after its bounded read")
print("PASS:", len(checks), "persistence adapter/source contracts (not Unity execution)")
