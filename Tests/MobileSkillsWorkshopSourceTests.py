"""Contracts for real touch-panel routing; does not execute Unity GUI/font rendering."""
from pathlib import Path

root = Path(__file__).resolve().parents[1]
skills = (root / 'Assets/Scripts/UI/GameUI.MobileSkills.cs').read_text()
camp = (root / 'Assets/Scripts/UI/GameUI.MobileWorkshop.cs').read_text()
shared = (root / 'Assets/Scripts/UI/GameUI.MobilePanels.cs').read_text()
route = (root / 'Assets/Scripts/UI/GameUI.RouteNextStep.cs').read_text()
checks = 0

def check(value, message):
    global checks
    checks += 1
    assert value, message

def method(source, signature):
    begin = source.index('{', source.index(signature))
    depth = 1
    end = begin + 1
    while depth:
        depth += (source[end] == '{') - (source[end] == '}')
        end += 1
    return source[begin:end]

check('MobilePanelGeometry()' in skills and 'MobilePanelGeometry()' in camp, 'both views use safe-area touch geometry')
check('layout.BodyLeft' in skills and 'layout.BodyRight' in skills, 'skills use full-height body panes without an unused tab row')
check('for (int skill = 0; skill < GameBalance.SkillCount; skill++)' in skills, 'all ten identities are iterated without a hotbar page')
check(all(word not in skills for word in ['hotbarPage', 'AssignSkill(', 'detailSlots', 'Modal(1160']), 'touch skills never inherit desktop paging or tiny detail layout')
check('MeasureMobileParagraph' in skills and 'MeasureMobileParagraph' in camp and 'CalcHeight' in shared, 'content measures real wrapped font height')
check('Mathf.Max(48, nameHeight + stateHeight + 24)' in skills, 'skill selection row measures text and retains a touch-size minimum')
check('SkillTooltip(p, skill, rank)' in skills and 'GameBalance.CategoryName' in skills, 'actual costs, cooldowns, charge and category descriptions stay available')
check('for (int stage = 1; stage <= 3; stage++)' in skills and 'GameBalance.SkillRankRequiredLevel' in skills and 'GameBalance.SkillEvolution' in skills,
      'all real rank requirements and evolutions remain readable')
check('SkillBudgetHint(p.heroClass, skill, stage)' in skills, 'corrected early field rank budgets are not replaced by generic scaling')
check('GameBalance.PrerequisiteDescription' in skills and 'progression.SkillLockReason(selectedSkill)' in skills, 'prerequisites and action eligibility come from shared rules')
check('progression.LearnSkill(selectedSkill)' in skills and 'string.IsNullOrEmpty(progression.LastError)' in skills, 'learn uses real service and failure feedback')
check('Attention.LearnableSkills.Contains' in skills and 'GameBalance.IsPassive' in skills, 'learnable badges and passive behavior survive the rewrite')
check(skills.count('BeginTouchScroll(') == 2 and skills.count('EndTouchScroll();') == 2, 'both skill panes use shared drag cancellation')
check('layout.FooterButton(1, 2)' in skills and skills.index('EndTouchScroll();', skills.index('DrawMobileSkillDescription(detailWidth, true)')) < skills.index('string caption'),
      'learn action remains fixed outside detail scrolling')
check('new Vector2[4]' in camp and 'layout.Tab(i, tabs.Length)' in camp and 'layout.TabbedBody' in camp, 'four camp tabs retain separate scroll state and fixed navigation')
check('TouchRect(8, y, width - 16, 48)' in camp, 'workshop actions use 48 touch units rather than desktop pixel rows')
check('SetSpecialization(index==0?ElementalistSpecialization.Shatter:ElementalistSpecialization.Burn,true)' in route and 'if(!session.IsInCamp)return;' in route and 'SetSpecialization(ElementalistSpecialization.None,session.IsInCamp)' in camp, 'all three valid element specializations route to the real service')
check('SetSummonerRoute((SummonerRoute)index,true)' in route and 'if(!session.IsInCamp)return;' in route, 'both summoner routes use the existing camp gate')
for call in ['MasteryLockReason(mastery)', 'LearnMastery(mastery)', 'SelectMasteryCore(mastery, session.IsInCamp)', 'RefundSkillRanks(session.IsInCamp)', 'ResetMastery(session.IsInCamp)']:
    check(call in camp, 'camp preserves ' + call)
check('MasteryCoreRules.InitialInvestment' in camp and 'MasteryCoreRules.EnhancedInvestment' in camp and 'MasteryCoreTier(mastery)' in camp, 'core actions use shared initial and enhanced thresholds')
for call in ['ClaimFirstClearReward(mechanic)', 'ExchangeMechanic(mechanic)', 'ToggleMechanicVariant(id, session.IsInCamp)', 'AscensionLockReason(id, session.IsInCamp)', 'AscendMechanic(id, session.IsInCamp)']:
    check(call in camp, 'mechanism panel preserves ' + call)
check('string id = item.id;' in camp and '编号：' in camp, 'equipment actions and labels use stable item identity')
check('ClaimRecoveryLoot(id)' in camp and 'ClaimPendingLoot(id)' in camp and 'InventoryCapacity' in camp, 'both mailboxes preserve stable-ID claims and capacity checks')
claims = method(camp, 'private void ClaimMobileWorkshopLoot()')
check(claims.index('claimed == 0 && !string.IsNullOrEmpty(p.LastError)') < claims.index('p.ClaimAllRecoveryLoot()'), 'first failed claim cannot have its error overwritten by a second service call')
check('sold > 0 || string.IsNullOrEmpty(p.LastError)' in camp, 'bulk sale failure cannot be announced as success')
check('tutorialMask & (1 << i)' in camp and 'actions.Length' in camp, 'tutorial completion comes from real progress bits')
result = method(camp, 'private void MobileWorkshopResult(')
check('!accepted || !string.IsNullOrEmpty(session.Progression.LastError)' in result and 'CancelMobileScroll();' in result and 'Vector2.zero' not in result,
      'failed actions retain real error and preserve the current scroll position')
check('BlockUITransition();' in result and 'BlockUITransition();' in method(skills, 'private void DrawMobileSkills()'), 'actions prevent repeated in-flight taps')
check('Profile.skillRanks[' not in camp and 'Profile.masteryRanks[i]++' not in camp and 'Profile.mechanicMaterials -=' not in camp,
      'panels do not duplicate progression mutation')
check('GUI.BeginScrollView' not in skills + camp and 'GUI.matrix' not in skills + camp, 'panels preserve the shared scroll/input and scale boundaries')

check('mobileWorkshopStatus' not in method(camp, 'private float DrawMobileWorkshopContent('), 'result feedback does not insert content above the current scroll anchor')

print(f'PASS: {checks} mobile skills/workshop source contracts (not Unity execution)')

reforge=(root / "Assets/Scripts/UI/GameUI.Reforge.cs").read_text()
check("()=>OpenReforgeSurface(id)" in camp and "reforgeOwner.ReforgeMechanic(reforgeSelected,session.IsInCamp)" in reforge,"mechanic operation retains mobile entry -> captured quote -> existing camp service")

check('if(!showDetail&&!RouteSkillReturnAvailable)listArea=new MobilePanelLayout.Area' in skills and '"返回冒险"' not in skills,'root skill tree returns footer space to tree and retains header close')
