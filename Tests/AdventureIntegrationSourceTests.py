from pathlib import Path
root=Path(__file__).resolve().parents[1]
def read(p):return(root/p).read_text()
s=read('Assets/Scripts/Core/GameSession.cs');m=read('Assets/Scripts/Core/GameSession.Modes.cs');r=read('Assets/Scripts/Core/GameSession.RoomChain.cs');ui=read('Assets/Scripts/UI/GameUI.Mobile.cs')
assert s.count('Player.ResetCooldownsForDungeonEntry();')==1
assert 'if(dungeon)Player.ResetCooldownsForDungeonEntry();' in s
assert 'ResetCooldownsForDungeonEntry' not in r and 'ResetCooldownsForDungeonEntry' not in m
assert 'ModeRun==null && RoomChainRun==null && !changingZone' in s
assert 'RecordRoomDefeat(enemy)' in s and 'RecordArenaDefeat(enemy)' in s
assert 'if(ModeRewardPending&&!TrySettleArenaReward())return false;' in s
assert 'ModeRun.Advance(Time.deltaTime,!InputBlocked' in m
assert 'TryGrantModeReward' in m and 'finally {rewardRun.CompleteReward(ticket,saved);}' in m
# Presentation comes from the durable receipt, including retries and callbacks
# that replace the active character; live before/after deltas cannot recover it.
service=read('Assets/Scripts/Core/ProgressionService.cs')
assert 'Progression.GetRewardPresentation(receipt)' in m
assert 'modeXpReward=detail==null?0:detail.Experience' in m
assert 'modeRewardDetailsUnavailable=detail==null' in m
assert 'if(saved){ApplyRewardPresentation(receipt);' in m
assert 'ModeRun!=rewardRun||Progression!=progression||progression.CurrentSlotId!=rewardSlot' in m
assert 'candidate.lastModeRewardDetails=CaptureRewardPresentation(receipt,Profile,candidate);' in service
assert 'RewardExperienceTotal(after)-RewardExperienceTotal(before)' in service
settlement=service[service.index('public bool TryGrantModeReward('):service.index('public void GrantEnemyKillReward(')]
assert settlement.index('candidate.lastModeRewardDetails=') < settlement.index('CommitCandidate(candidate)') < settlement.index('RaiseLeveledUp(level)')
assert m.index('ModeRun!=rewardRun||Progression!=progression||progression.CurrentSlotId!=rewardSlot') < m.index('if(saved){ApplyRewardPresentation(receipt);')
assert 'if(!SaveBeforeLeaving())return false;' in r and r.index('if(!SaveBeforeLeaving())return false;')<r.index('RoomChainRun.Next(true,false)')
assert 'TrySettleRoomReward' in r and 'roomEnemies.TryGetValue' in r
assert 'EnterNextRoom' in ui and 'IsNearDungeonEntrance' in ui
control=read('Assets/Scripts/UI/MobileControls.cs')
assert 'Area(ui.MobileInteractionArea).Contains(point)' in control and 'ui.ActivateMobileInteraction(finger)' in control
assert 'Role.Camera' in control and '!ui.IsScreenPointOverUI(screen)' in control
player=read('Assets/Scripts/Combat/PlayerController.cs')
assert player.count('session.CombatEnded')>=2
assert 'AdventureResultPolicy.AcceptsDamage(session.HasStarted,session.CombatEnded)' in read('Assets/Scripts/Combat/EnemyController.cs')
assert 'AdventureResultPolicy.AcceptsKill(HasStarted,CombatEnded,Enemies.Contains(enemy))' in s
scrolls=list((root/'Assets/Scripts/UI').glob('*.cs'))
assert all('GUI.BeginScrollView' not in p.read_text() for p in scrolls if p.name!='GameUI.TouchScroll.cs')
assert 'new Area[10]' in read('Assets/Scripts/UI/MobileControlLayout.cs')
uiroot=read('Assets/Scripts/UI/GameUI.cs')
assert uiroot.index('if (session.Paused) DrawPause();')<uiroot.index('else if(session.ModeFinished)')
assert '菜单 / 存档' in read('Assets/Scripts/UI/GameUI.RunRecap.cs')
assert 'NearbyHubNpc!=HubNpcKind.None' in read('Assets/Scripts/Core/GameSession.Expedition.cs')
print('PASS: adventure/room entry, cooldown, portal, reward, scroll and terminal-damage source contracts (not Unity execution)')
