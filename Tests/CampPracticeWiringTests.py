#!/usr/bin/env python3
"""Source-boundary audit, explicitly not runtime/Unity acceptance."""
from pathlib import Path
r=Path(__file__).resolve().parents[1]/'Assets/Scripts'
def text(path):return (r/path).read_text()
s=text('Core/GameSession.cs');p=text('Combat/PlayerController.cs');ui=text('UI/GameUI.cs');trial=text('Core/GameSession.Practice.cs')
assert 'if(PracticeActive){TickPractice();return;}' in s
assert 'if(PracticeActive){if(enemy!=null&&Enemies.Remove(enemy)){PracticeRecord.Defeat(' in s
assert 'if(PracticeActive){PracticeRecord.PlayerDefeated();return;}' in s
assert 'if(PracticeActive)return false;' in s[s.index('public bool TryCollectGroundLoot(string'):s.index('public void CollectRemainingDungeonLoot')]
leave=s[s.index('public bool SaveBeforeLeaving()'):s.index('public bool ExitApplication(')];assert 'EndPractice(' in leave and leave.index('EndPractice(')<leave.index('Progression.Save()')
assert 'if(PracticeActive){PracticeRecord.Mechanism(key);return;}' in text('Core/GameSession.Expedition.cs')
assert 'if(PracticeActive)return;' in text('Core/GameSession.Expedition.cs')
assert 'PracticeRecord.ConfirmedHealthLoss(previousHealth-Health,practiceCastId)' in text('Combat/EnemyController.cs')
assert 'session.RecordPracticeCast(castId,slot)' in p and 'session.RecordPracticeSkillHit(castId)' not in p
assert p.index('session = game;') < p.index('skillRuntime.EnergyChanged=session.RecordPracticeEnergy')
assert 'DrawPracticeCombatHUD();DrawPracticeOverlay();' in ui
assert 'panel=Panel.None' in text('UI/GameUI.Practice.cs') and 'panel=practiceReturnPanel' in text('UI/GameUI.Practice.cs')
assert '!session.PracticeActive&&' in text('UI/GameUI.Mobile.cs')
assert 'finally' in trial and 'Progression=practiceOwner;Player=practiceOriginalPlayer;Enemies=practiceOriginalEnemies;' in trial
print('PASS practice production wiring: reward/loot/save/tutorial guard, actual loss/cast/energy hooks, panel handoff, finally restoration (source audit only)')
