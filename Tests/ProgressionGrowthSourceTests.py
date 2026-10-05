#!/usr/bin/env python3
"""Production-source contracts for progression transactions and camp actions."""
from pathlib import Path
root=Path(__file__).resolve().parent.parent
read=lambda p:(root/p).read_text(encoding='utf-8')
checks=[]
def check(ok,label):
    if not ok:raise AssertionError(label)
    checks.append(label)
def method(s,name):
    start=s.index(name);begin=s.index('{',start);end=begin+1;depth=1
    while depth:
        if s[end]=='{':depth+=1
        elif s[end]=='}':depth-=1
        end+=1
    return s[begin:end]
p=read('Assets/Scripts/Core/ProgressionService.cs')
refund=method(p,'public bool RefundSkillRanks(')
check('!inCamp' in refund and 'Math.Min(1, candidate.skillRanks[i])' in refund,'refund is camp-only and retains all learned rank1 unlocks')
check('Snapshot()' in refund and 'CommitCandidate(candidate)' in refund and 'Profile.skillRanks[' not in refund,'refund publishes one isolated candidate')
check('MasteryCoreRules.InitialInvestment' in method(p,'public bool SelectMasteryCore(') and 'mastery[profile.masteryCore] < MasteryCoreRules.InitialInvestment' in p,'core selection and load migration share ten-point threshold')
ascend=method(p,'public bool AscendMechanic(')
check('AscensionLockReason' in ascend and 'CommitCandidate(candidate)' in ascend and 'random.' not in ascend and 'SetRolledStats' not in ascend,'ascension is gated, atomic and never rerolls')
check('item.id =' not in ascend and 'item.mechanic =' not in ascend and 'item.mechanicVariant =' not in ascend and 'item.level =' not in ascend,'ascension preserves durable equipment identity and variant fields')
check('EnsureUpgradeBasis(item)' in ascend and 'candidate.slotUpgradeRanks[(int)item.slot]' in ascend,'ascension scales bases then reapplies permanent slot rank')
reason=method(p,'public string AscensionLockReason(')
eligibility=method(p,'public string MechanicGoalEligibility(')
check('MechanicGoalEligibility(id,ProgressionGoalKind.Ascension)' in reason and 'HasDiscoveredMechanic' in eligibility and 'BuildCatalog.MechanicClass' in eligibility and 'Rarity.Epic' in eligibility and 'AscensionMilestone' in reason and 'AscensionCost' in reason,'known same-class epic, milestone and materials all required')
complete=method(p,'public bool TryCompleteDungeonRun(')
check('Profile.lastDungeonRewardId == rewardId' in complete and 'candidate.lastDungeonRewardId = rewardId' in complete,'ordinary clear has durable idempotent receipt')
check(complete.count('CommitCandidate(candidate)')==1 and 'candidate.clearedRuns' in complete and 'candidate.bestFloor' in complete and 'NewChestQualification(candidate,tier,rewardId)' in complete and 'candidate.pendingChestTier' in method(p,'private static void NewChestQualification(') and 'candidate.pendingFirstClearReward' in complete,'one candidate includes progress and every ordinary-clear entitlement')
check(complete.index('CommitCandidate(candidate)')<complete.index('LeveledUp(level)'),'level-up events occur only after durable completion')
check('Profile.pendingFashionChest || Profile.pendingChestReveal' in complete,'completion cannot overwrite an existing chest entitlement')
check('TierRewardRules.ChestGoldMinimum(profile.pendingChestTier)' in method(p,'internal static ChestReward BuildSingleChestRoll(') and 'pendingChestRollTier!=Profile.pendingChestTier' in method(p,'public string OpenDungeonChest()') and 'profile.pendingChestTier = TierRewardRules.ClampTier' in p,'chest uses earned tier and old saves get bounded migration')
check('TierRewardRules.DropRarity(boss, dungeonTier, roll)' in p,'loot rarity consumes actual dungeon tier')
ui=read('Assets/Scripts/UI/GameUI.Expedition.cs')
build_ui=read('Assets/Scripts/UI/GameUI.BuildPlans.cs')
mobile_ui=read('Assets/Scripts/UI/GameUI.MobileWorkshop.cs')
check('RequestBuildPlanAction(BuildPlanAction.Reset)' in ui and 'p.ResetBuild(session.IsInCamp)' in build_ui and
      'RefundableSkillRanks' in build_ui and 'RefundableMasteryPoints' in build_ui,
      'camp combined reset uses atomic service after showing both refund components')
check('p.RefundSkillRanks(session.IsInCamp)' in mobile_ui and 'p.ResetMastery(session.IsInCamp)' in mobile_ui,
      'mobile camp retains independent refunds alongside the combined reset')
check('p.AscendMechanic(equipped.id,session.IsInCamp)' in ui and 'AscensionLockReason' in ui,'camp ascension uses selected stable item and real lock reason')
core=read('Assets/Scripts/Core/MasteryCoreRuntime.cs')
check('cast.FirstCoreHit()' in core and 'ComboDuration=6f' in core and 'comboRemaining = ComboDuration' in core,'offense lifetime receipt deduplicates first contact and combo duration stays explicit')
check('healthFraction <= 0' in core and 'healthFraction >= .5f' in core,'vitality cannot revive lethal hits or proc outside low health')
check('Core == core && Tier == tier' in core and 'public void Reset()' in core,'routine profile refresh cannot reset proc budget; epoch reset remains explicit')
print('PASS:',len(checks),'progression-growth source contracts (not Unity execution)')
