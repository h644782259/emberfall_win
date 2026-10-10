"""Production wiring around the executable stage-ledger and kill-callback tests."""
from pathlib import Path
r=Path(__file__).resolve().parents[1]/'Assets/Scripts'
s=(r/'Core/GameSession.cs').read_text();p=(r/'Core/ProgressionService.cs').read_text()
kill=s[s.index('public void OnEnemyKilled('):s.index('private int checkpointRoom')]
assert kill.index('!Enemies.Remove(enemy)')<kill.index('Progression.RecordDungeonKill(')
assert 'Progression.GrantEnemyKillReward(gold,experience,deferSave:true);' in kill
assert 'pendingWildernessSave=Progression;' in kill
assert 'AfterCurrentAction(Progression.Save)' not in kill and 'if(!InDungeon)Progression.Save();' not in kill
assert 'SpawnGroundSupplies(' not in kill and 'beforeKillLevel' not in kill
assert 'if(!InDungeon)LogSystem(' in kill
assert 'runEnemyExperience+' in kill and 'runPickupGold+' in kill
loot=s[s.index('private void DeliverEnemyLoot('):s.index('public GroundLootPickup SpawnGroundLoot(')]
assert loot.index('Progression.RecordDungeonLoot(loot)')<loot.index('SpawnGroundLoot(')
assert 'if(dungeonStageActive)return RecordDungeonLoot(item);' in p
assert 'if(dungeonStageActive){TrackSkillStock(hero,count,remaining,period);return true;}' in p
assert 'public void Save()\n        {\n            if(dungeonStageActive)return;' in p
assert 'if(InDungeon)return;\n            autosaveTimer' in s
assert 'if(dungeon)Progression.BeginDungeonStage();' in s
assert 'if(!Progression.SaveDungeonCheckpoint())' in s
assert 'ChapterRun.DoorUnlocked' in s and 'RoomChainRun.DoorUnlocked' in s
leave=s[s.index('public bool SaveBeforeLeaving()'):s.index('public bool ExitApplication(')]
assert 'SaveDungeonCheckpoint()' in leave and 'FinishDungeonRewards()' not in leave
feedback=(r/'Core/GameSession.Feedback.cs').read_text()
assert 'if(!won&&!Progression.FinishDungeonRewards())' in feedback
assert 'if(InDungeon)return; // Final settlement' in s
assert 'session.Progression.GetStats().Damage' not in (r/'Combat/EnemyController.cs').read_text()
for file in ['Core/GameSession.Modes.cs','Core/GameSession.Expedition.cs']:
 assert 'SaveDungeonCheckpoint()' in (r/file).read_text()
print('PASS: stage checkpoints, deferred loot/XP/charges, failure settlement and cached hit-stat wiring')
