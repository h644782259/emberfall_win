#!/usr/bin/env python3
"""Read-only save-flow wiring checks; no player files or Unity runtime invoked."""
from pathlib import Path
root=Path(__file__).resolve().parent.parent
read=lambda p:(root/p).read_text(encoding='utf-8')
checks=[]
def check(ok,message):
    if not ok:raise AssertionError(message)
    checks.append(message)
session=read('Assets/Scripts/Core/GameSession.cs')
flow=session[session.index('private bool ContinueAdventure('):session.index('public bool SaveAsNewSlot()')]
check(flow.index('SaveSlotTransition.TryStage')<flow.index('DiscardTransientAdventureForLoad();')<flow.index('Progression = candidate;'),'target stages before transient discard and publication')
check(flow.index('Progression.Changed -= OnProgressChanged;')<flow.index('Progression = candidate;'),'old live event subscriber detached before service publication')
check('ui.RebindProgressionNotifications(previous, candidate)' in flow,'badge subscriptions migrate with active progression service')
check('HasStarted && !discardUnsaved && !alreadySaved' in flow,'discard path never implicitly preserves current character')
check('targetId == Progression.CurrentSlotId' in flow,'same-slot Save+Load refreshes just-saved snapshot')
clear=flow[flow.index('private void DiscardTransientAdventureForLoad()'):]
check(clear.index('HasStarted = false;')<clear.index('pendingLoot.Clear();'),'lifecycle autosave disabled before dropping old transient loot')
check('SaveBeforeLeaving(' not in clear and 'PreserveWorldLoot(' not in clear and 'Progression.Save(' not in clear,'discard teardown contains no persistence action')
check('ResetExpedition(false);' in clear,'old expedition cleared before target service is published')
expedition=read('Assets/Scripts/Core/GameSession.Expedition.cs')
modes=read('Assets/Scripts/Core/GameSession.Modes.cs')
rooms=read('Assets/Scripts/Core/GameSession.RoomChain.cs')
check('ResetArenaMode(dungeon);' in expedition and 'ModeRun.Dispose();ModeRun=null' in modes and 'ResetRoomChain(dungeon);' in modes and 'RoomChainRun.Dispose();RoomChainRun=null' in rooms,'reset chain discards BOTH unclaimed arena and room-chain rewards')
check('if(!loadingSaveSnapshot&&!enteringChapter&&!retryingRoomChain&&!SaveBeforeLeaving())returnfalse;' in ''.join(session.split()),'camp construction for a staged load skips the checked transition preflight')
chapter=read('Assets/Scripts/Core/GameSession.Chapter.cs')
entry=chapter[chapter.index('public bool ConfirmChapterEnter()'):chapter.index('private void ResetChapterRun()')]
check(entry.index('if(!SaveBeforeLeaving())return false;')<entry.index('enteringChapter=true;') and 'finally {enteringChapter=false;' in entry,
      'chapter bypass has a successful save preflight and cannot leak into staged loads or later transitions')
ui=read('Assets/Scripts/UI/GameUI.cs');mobile=read('Assets/Scripts/UI/GameUI.Mobile.cs')
pause=ui[ui.index('private void DrawPause()'):ui.index('private void OpenControls()')]
mpause=mobile[mobile.index('private void DrawMobilePause()'):]
check('SaveAsNewSlot' not in pause and 'SaveAsNewSlot' not in mpause,'pause menus contain no duplicate Save As route')
check(pause.count('RequestManualSave();')==1 and mpause.count('RequestManualSave();')==1,'one manual Save entry on each platform')
check('OpenSaveSelection();' in pause and 'OpenSaveSelection();' in mpause,'both pause menus expose stable-slot load list')
check('if (DrawSaveFlowConfirmation()) return;' in pause and 'if(DrawSaveFlowConfirmation())return;' in ui,'confirmation replaces pause/list controls instead of layering active controls beneath')
check('if(CancelActiveSaveFlow())return;' in ui and 'session.SetPaused(returnPaused)' in ui,'Esc/back cancels confirmation and returns pause-origin lists to paused state')
confirm=read('Assets/Scripts/UI/GameUI.SaveFlow.cs')
check('自动保存仍持续写入当前角色' in confirm and '取消只取消这次手动保存' in confirm,'manual prompt separates requested manual save from continuing autosave')
check('地面战利品' in confirm and 'session.ModeRewardPending' in confirm and '待保存挑战奖励' in confirm,'discard prompt explicitly covers transient loot and unpaid mode rewards')
check('BlockUITransition();' in confirm and 'SaveRevision(currentTarget) != saveFlowTargetRevision' in confirm,'transitions suppress repeated taps and changed target needs fresh review')
print('PASS:',len(checks),'manual save/load source contracts (not Unity execution)')
