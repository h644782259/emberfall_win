#!/usr/bin/env python3
"""Camp build UI source contracts; not a Unity event-loop or rendering test."""
from pathlib import Path
root=Path(__file__).resolve().parents[1]
read=lambda name:(root/'Assets/Scripts/UI'/name).read_text()
s=read('GameUI.BuildPlans.cs');camp=read('GameUI.Expedition.cs');mobile=read('GameUI.MobileWorkshop.cs');ui=read('GameUI.cs')
checks=0
def check(value,label):
    global checks
    checks+=1
    assert value,label
def method(source,signature):
    begin=source.index('{',source.index(signature));end=begin+1;depth=1
    while depth:
        depth+=(source[end]=='{')-(source[end]=='}');end+=1
    return source[begin:end]
check('if(DrawBuildPlanSurface())return;' in camp,'build surface replaces camp controls instead of layering active buttons')
check('OpenBuildPlans();' in camp and 'true, draw, OpenBuildPlans' in mobile,'desktop and mobile camp expose two clearly named build slots')
check('RequestBuildPlanAction(BuildPlanAction.Reset)' in camp and 'RequestBuildPlanAction(BuildPlanAction.Reset)' in mobile,'both platforms expose combined respec preview')
check('if(CloseBuildPlanSurface())return;' in ui and 'buildPlanAction=BuildPlanAction.None' in method(s,'private bool CloseBuildPlanSurface()'),'Back/Escape cancels confirmation without performing a service action')
request=method(s,'private void RequestBuildPlanAction(')
check('buildPlanSource=p.Profile' in request and 'p.BuildPresetSummary(slot)' in request and 'p.CurrentBuildSummary()' in request,'confirmation snapshots current service/profile and actual selected build summary')
check('for(int slot=0;slot<ProgressionService.BuildPresetCount;slot++)' in s and 'p.HasBuildPreset(slot)' in s,'two bounded slots come from production service')
check('p.BuildPresetLockReason(slot,session.IsInCamp)' in s and 'occupied&&string.IsNullOrEmpty(reason)' in s,'missing/invalid/ineligible builds show service lock reason and cannot apply')
check('RequestBuildPlanAction(BuildPlanAction.Save,slot)' in s and 'RequestBuildPlanAction(BuildPlanAction.Apply,slot)' in s,'overwrite and apply route through explicit confirmation')
confirm=method(s,'private void ConfirmBuildPlanAction()')
check('buildPlanOwner!=p||buildPlanSource!=p.Profile' in confirm,'profile or character changes invalidate stale confirmation')
check('重新核对' in s and 'else RequestBuildPlanAction(buildPlanAction,buildPlanSlot)' in s,'stale confirmation offers an explicit refreshed preview instead of a dead button')
reconcile=method(s,'private void ReconcileBuildPlanSurface()')
check('panel!=Panel.Camp' in reconcile and 'buildPlanHero!=session.Player' in reconcile and 'buildPlanCharacterId!=session.Progression.CurrentSlotId' in reconcile,
      'closing camp, replacing hero or changing slot clears a prior build surface')
check('ResetBuildPlanSurface();' in read('GameUI.Attention.cs') and 'ReconcileBuildPlanSurface();' in ui,'load rebind and UI update both enforce build lifecycle isolation')
for action in ['p.ResetBuild(session.IsInCamp)','p.SaveBuildPreset(buildPlanSlot,session.IsInCamp)','p.ApplyBuildPreset(buildPlanSlot,session.IsInCamp)']:
    check(action in confirm,'confirmed action uses '+action)
check('RefundableSkillRanks' in s and 'RefundableMasteryPoints' in s and 'RefundableBuildPoints' in s,'refund preview shows skills, mastery and combined total')
check('保留已学1阶' in s and '当前技能冷却不重置' in s and '不创建新角色存档' in s,'build reset/save wording states retained unlocks, cooldowns and separate character identity')
check('if(!accepted)' in confirm and 'p.LastError' in confirm and 'buildPlanScroll=Vector2.zero' in confirm,'write failures stay retryable and visible')
check('p.Profile.' not in confirm and 'CommitCandidate' not in s,'UI performs no duplicate profile/economy mutation')
surface=method(s,'private bool DrawBuildPlanSurface()')
check('new MobileDialogLayout(width/unit,height/unit)' in surface and 'BeginTouchScroll(' in surface and surface.index('EndTouchScroll();')<surface.index('layout.FooterButton'),'both platform dialogs use tested responsive bounds and fixed confirmation actions')
content=method(s,'private float DrawBuildPlanContent(')
check(content.index('for(int slot=0;')<content.index('DrawPracticeChoices(')<content.index('"当前配装"'),'saved plan actions precede optional practice and current detail')
check('BuildPlanName(buildPlanSlot)' in s and 'BuildPlanName(slot)' in s and '(slot+1)' not in s and '(buildPlanSlot+1)' not in s,'visible slots consistently use A/B without changing service indices')
check('practiceChoicesOpen=false' in method(s,'private void OpenBuildPlans()'),'opening plans defaults optional practice to collapsed')
practice=read('GameUI.Practice.cs')
choices=method(practice,'private void DrawPracticeChoices(')
check(choices.index('if(!practiceChoicesOpen)return;')<choices.index('session.PracticeRecord'),'collapsed practice cannot mutate or replace result records')
check('practiceChoicesOpen' not in method(practice,'internal void LeavePracticePanel()'),'practice return preserves disclosure for results')
print(f'PASS: {checks} build-plan UI source contracts (not Unity execution)')
