"""Player-side Back ownership contracts; UI transition ownership is verified separately."""
from pathlib import Path
root=Path(__file__).resolve().parent.parent
read=lambda f:(root/'Assets/Scripts'/f).read_text()
p=read('Combat/PlayerController.cs');g=read('Core/GameSession.cs');c=read('Combat/SkillChargeController.cs')
u=p[p.index('private void Update()'):p.index('// Kept independent of Input')]
line=next(x.strip() for x in u.splitlines() if 'KeyCode.Escape' in x)
assert line.startswith('if (GameplayCancelAllowed && charge != null && charge.IsCharging && '), 'Back and camera cancel must both be gameplay gated'
assert '(Input.GetKeyDown(KeyCode.Escape) || AdventureCamera.CancelSkillRequested)) charge.Cancel();' in line
start=g.index('public bool InputBlocked');end=g.index('{',start)+1;depth=1
while depth:
    depth+=(g[end]=='{')-(g[end]=='}');end+=1
blocked=g[start:end]
for reason in ['Paused','uiBlocking','DungeonSelectionOpen','RunChoices.AwaitingChoice','pauseState.BackgroundPaused']:
    assert reason in blocked, reason+' must suppress player cancellation'
assert 'if (session.InputBlocked || deltaTime <= 0' in c, 'blocked charge retains its elapsed budget'
assert 'cancelledFrame = Time.frameCount;' in c and 'public bool ConsumedThisFrame' in c, 'real gameplay cancellation suppresses same-frame autoattack'
assert '!charge.IsCharging && !charge.ConsumedThisFrame' in u
assert 'session != null && !session.InputBlocked && (inputUI == null || inputUI.GameplayBackAllowed)' in p
assert 'inputUI = game == null ? null : game.GetComponent<GameUI>();' in p
t=read('Combat/SkillTargetingController.cs')
assert 'if (owner.GameplayCancelAllowed && (AdventureCamera.CancelSkillRequested || Input.GetKeyDown(KeyCode.Escape)))' in t
assert t.index('if (Invalid()) { Cancel(); return true; }', t.index('public bool TickInput()')) < t.index('if (owner.GameplayCancelAllowed')
ui=read('UI/GameUI.cs');lifecycle=read('UI/GameUI.Lifecycle.cs')
assert 'backConsumedFrame!=Time.frameCount' in lifecycle, 'UI-first close must retain Back ownership for the rest of the frame'
assert ui.index('bool gameplayBackAllowed=GameplayBackAllowed;') < ui.index('if(Input.GetKeyDown(KeyCode.Escape))backConsumedFrame=Time.frameCount;') < ui.index('if(exitRequest.Open)'), 'snapshot and claim precede modal dismissal'
assert 'if (gameplayBackAllowed && charge != null && (charge.IsCharging || charge.CancelledThisFrame))' in ui and 'if (gameplayBackAllowed && targeting != null && (targeting.IsTargeting || targeting.CancelledThisFrame))' in ui, 'Player-first normal cancel cannot fall through to pause'
print('PASS: 17 player pause/dialog/frame-ownership cancellation wiring contracts (no Android runtime claim)')
