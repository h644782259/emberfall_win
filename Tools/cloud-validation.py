#!/usr/bin/env python3
"""Run the existing standalone tests and an optional Unity API compile check.

No gameplay behavior is copied here. Generated projects and save fixtures live in
a temporary directory. This is not a Unity Editor, rendering, or player test.
"""
import argparse
import base64
from datetime import datetime, timezone
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
from xml.sax.saxutils import escape, quoteattr
import zipfile

ROOT = Path(__file__).resolve().parent.parent
PACKAGE_URL = ("https://api.nuget.org/v3-flatcontainer/unityengine.modules/"
               "2021.3.33/unityengine.modules.2021.3.33.nupkg")
PACKAGE_SHA512 = ("ad7eBwkG66RQ0ToAMD/ak8MZ6pfgrYXxcPGftN8H7ydkn5TdrwlU5qgZTkHpjOGYo"
                  "IJvIk3+77VWq53/UU2dqA==")


def unity_references(download):
    root = ROOT / "Tools/ReferenceAssemblies"
    references = root / "UnityEngine/lib/netstandard2.0"
    if references.is_dir() and list(references.glob("*.dll")):
        return references
    if not download:
        raise RuntimeError("Unity reference DLLs are missing; pass --download-references "
                           "to retrieve the repository's pinned compile-only NuGet package.")
    root.mkdir(parents=True, exist_ok=True)
    archive = root / "unityengine.modules.2021.3.33.nupkg"
    if not archive.exists():
        pending = archive.with_suffix(".download")
        subprocess.run(["curl", "--fail", "--location", "--retry", "1", "--connect-timeout",
                        "20", "--max-time", "180", PACKAGE_URL, "--output", str(pending)], check=True)
        pending.replace(archive)
    digest = base64.b64encode(hashlib.sha512(archive.read_bytes()).digest()).decode("ascii")
    if digest != PACKAGE_SHA512:
        raise RuntimeError("Unity reference package SHA-512 mismatch; refusing to extract it.")
    destination = (root / "UnityEngine").resolve()
    with zipfile.ZipFile(archive) as package:
        for entry in package.infolist():
            if not (destination / entry.filename).resolve().is_relative_to(destination):
                raise RuntimeError("Unsafe reference-package archive path.")
        package.extractall(destination)
    if not list(references.glob("*.dll")):
        raise RuntimeError("The pinned package contains no reference DLLs at the expected path.")
    return references


def write_project(directory, sources, program=None, references=None, framework="net8.0", defines=""):
    directory.mkdir()
    if program is not None:
        entry = directory / "Program.cs"
        entry.write_text(program, encoding="utf-8")
        sources = [*sources, entry]
    # Pure shared lifetime receipt types used by combat/core rules and actor fixtures.
    receipt = ROOT / "Assets/Scripts/Core/CastFirstHitReceipt.cs"
    if receipt not in sources and any("CastFirstHit" in Path(path).read_text(encoding="utf-8") for path in sources):
        sources = [*sources, receipt]
    source_items = "\n".join("    <Compile Include=" + quoteattr(str(path)) + " />" for path in sources)
    reference_items = ""
    if references is not None:
        reference_items = "\n".join(
            "    <Reference Include=" + quoteattr(path.stem) + "><HintPath>" + escape(str(path)) +
            "</HintPath><Private>false</Private></Reference>" for path in sorted(references))
    project = directory / "Validation.csproj"
    project.write_text('''<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>''' + framework + '''</TargetFramework>
    <OutputType>''' + ("Exe" if program is not None else "Library") + '''</OutputType>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
    <ImplicitUsings>disable</ImplicitUsings>
    <Nullable>disable</Nullable>
    <LangVersion>9.0</LangVersion>
    <NuGetAudit>false</NuGetAudit>
    <DefineConstants>''' + escape(defines) + '''</DefineConstants>
  </PropertyGroup>
  <ItemGroup>
''' + source_items + "\n" + reference_items + '''
  </ItemGroup>
</Project>
''', encoding="utf-8")
    return project


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--dotnet", default=os.environ.get("DOTNET", "dotnet"),
                        help=".NET 8 SDK executable (or set DOTNET)")
    parser.add_argument("--compile", action="store_true", help="also compile all runtime sources against Unity references")
    parser.add_argument("--compile-android", action="store_true", help="compile the UNITY_ANDROID runtime branch against pinned references; does not build an APK")
    parser.add_argument("--compile-ios", action="store_true", help="compile the UNITY_IOS runtime branch against pinned references; does not build an IPA")
    parser.add_argument("--download-references", action="store_true", help="download pinned Unity reference DLLs if missing; implies --compile")
    parser.add_argument("--unity-editor", type=Path, help="also compile Windows/iOS/Android runtime, Editor, and visual-validation source using installed Unity 6000.6 DLLs (does not launch Unity)")
    args = parser.parse_args()
    dotnet = shutil.which(args.dotnet)
    if not dotnet:
        parser.error(".NET 8 SDK is required. Install it from https://dotnet.microsoft.com/download/dotnet/8.0 or set --dotnet.")
    timestamp = datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%S%fZ")
    output = ROOT / "Tests/TestResults" / "Cloud-Latest"
    output.mkdir(parents=True, exist_ok=True)
    initial_sources = source_hashes()
    report = {"startedUtc": timestamp, "project": str(ROOT), "checks": [],
              "scope": "Standalone production-logic tests; optional Unity API/source compilation. "
                       "No Unity Editor execution, real JsonUtility, rendering, shaders, or platform build executed."}
    failed = False
    with tempfile.TemporaryDirectory(prefix="EmberfallCloudValidation-") as temporary:
        workspace = Path(temporary)
        config = workspace / "NuGet.Config"
        config.write_text('<configuration><packageSources><clear /></packageSources></configuration>', encoding="utf-8")
        env = os.environ.copy()
        env.update({"DOTNET_CLI_TELEMETRY_OPTOUT": "1", "DOTNET_SKIP_FIRST_TIME_EXPERIENCE": "1",
                    "DOTNET_GENERATE_ASPNET_CERTIFICATE": "false", "DOTNET_NOLOGO": "1",
                    "DOTNET_CLI_HOME": str(workspace / "dotnet-home"), "NUGET_PACKAGES": str(workspace / "nuget")})
        checks = [
            ("progression", [ROOT / "Assets/Scripts/Core/GameTypes.cs", ROOT / "Assets/Scripts/Core/ProgressionService.cs",
                             ROOT / "Tests/ProgressionTests.cs"],
             'using System; internal static class Program { static void Main(string[] args) { Console.WriteLine(ProgressionTests.Run(args[0])); } }'),
            ("skills", [ROOT / "Assets/Scripts/Core/GameTypes.cs", ROOT / "Assets/Scripts/Core/SkillRuntime.cs",
                        ROOT / "Tests/SkillRuntimeTests.cs"],
             'using System; internal static class Program { static void Main() { Console.WriteLine(SkillRuntimeTests.Run()); } }'),
        ]
        if (ROOT / "Tests/UpgradeProgressionTests.cs").exists():
            checks.append(("upgrade-progression", [ROOT / "Assets/Scripts/Core/GameTypes.cs",
                          ROOT / "Assets/Scripts/Core/ProgressionService.cs", ROOT / "Tests/ProgressionTests.cs",
                          ROOT / "Tests/UpgradeProgressionTests.cs"],
                          'using System; internal static class Program { static void Main(string[] args) { Console.WriteLine(UpgradeProgressionTests.Run(args[0])); } }'))
        if (ROOT / "Tests/BossAttackPolicyTests.cs").exists():
            checks.append(("boss-attack-policy", [ROOT / "Assets/Scripts/Combat/BossAttackPolicy.cs",
                          ROOT / "Tests/BossAttackPolicyTests.cs"],
                          'using System; internal static class Program { static void Main() { Console.WriteLine(BossAttackPolicyTests.Run()); } }'))
        if (ROOT / "Tests/PlayerUpgradeTests.cs").exists():
            checks.append(("player-upgrades", [ROOT / "Assets/Scripts/Core/GameTypes.cs",
                          ROOT / "Assets/Scripts/Core/SkillRuntime.cs", ROOT / "Tests/SkillRuntimeTests.cs",
                          ROOT / "Assets/Scripts/Combat/PlayerUpgradeRules.cs", ROOT / "Assets/Scripts/Combat/CombatDamage.cs",
                          ROOT / "Tests/PlayerUpgradeTests.cs"],
                          'using System; internal static class Program { static void Main() { Console.WriteLine(PlayerUpgradeTests.Run()); } }'))
        if (ROOT / "Tests/EncounterPlanTests.cs").exists():
            checks.append(("encounter-plans", [ROOT / "Assets/Scripts/Core/GameTypes.cs",
                          ROOT / "Assets/Scripts/Core/SkillRuntime.cs", ROOT / "Tests/SkillRuntimeTests.cs",
                          ROOT / "Assets/Scripts/Core/EncounterPlan.cs", ROOT / "Tests/EncounterPlanTests.cs"],
                          'using System; internal static class Program { static void Main() { Console.WriteLine(EncounterPlanTests.Run()); } }'))
        if (ROOT / "Tests/RunChoicesTests.cs").exists():
            checks.append(("run-choices", [ROOT / "Assets/Scripts/Core/GameTypes.cs",
                          ROOT / "Assets/Scripts/Core/SkillRuntime.cs", ROOT / "Tests/SkillRuntimeTests.cs",
                          ROOT / "Assets/Scripts/Core/RunChoices.cs", ROOT / "Tests/RunChoicesTests.cs"],
                          'using System; internal static class Program { static void Main() { Console.WriteLine(RunChoicesTests.Run()); } }'))
        if (ROOT / "Tests/ApplicationPauseStateTests.cs").exists():
            checks.append(("application-pause-state", [ROOT / "Assets/Scripts/Core/ApplicationPauseState.cs",
                          ROOT / "Tests/ApplicationPauseStateTests.cs"],
                          'using System; internal static class Program { static void Main() { Console.WriteLine(ApplicationPauseStateTests.Run()); } }'))
        for name, test_file in [("combat-balance", "CombatBalanceTests"), ("rebalance-progression", "RebalanceProgressionTests"), ("companion-rules", "CompanionRulesTests")]:
            if not (ROOT / ("Tests/" + test_file + ".cs")).exists():
                continue
            extra = [ROOT / "Assets/Scripts/Core/GameTypes.cs", ROOT / "Assets/Scripts/Core/ProgressionService.cs",
                     ROOT / "Tests/ProgressionTests.cs", ROOT / ("Tests/" + test_file + ".cs")]
            if name == "companion-rules": extra.append(ROOT / "Assets/Scripts/Combat/CompanionRules.cs")
            entry = ('using System; internal static class Program { static void Main(string[] args) { Console.WriteLine(' + test_file + '.Run(' + ('args[0]' if name == 'rebalance-progression' else '') + ')); } }')
            checks.append((name, extra, entry))
        for name, source, test in [("mobile-layout", "UI/MobileControlLayout", "MobileControlLayoutTests"), ("safe-exit", "Core/SafeExitRequest", "SafeExitRequestTests"), ("mobile-skills", "Combat/MobileSkillPolicy", "MobileSkillPolicyTests"), ("mobile-camera", "Core/MobileCameraGesture", "MobileCameraGestureTests"), ("touch-scroll", "UI/TouchScrollGesture", "TouchScrollGestureTests"), ("room-chain", "Core/RoomChainState", "RoomChainStateTests"), ("large-boss-phases", "Core/LargeBossPhaseState", "LargeBossPhaseTests"), ("adventure-results", "Core/AdventureResultPolicy", "AdventureResultPolicyTests"), ("combat-sight", "Core/CombatSightRules", "CombatSightRulesTests"), ("tier-reward-bands", "Core/TierRewardBand", "TierRewardBandTests"), ("touch-release-latch", "UI/TouchReleaseLatch", "TouchReleaseLatchTests")]:
            checks.append((name,[ROOT / ("Assets/Scripts/"+source+".cs"),ROOT / ("Tests/"+test+".cs")],
                'using System; internal static class Program { static void Main() { Console.WriteLine('+test+'.Run()); } }'))
        checks.append(("interface-safety",[ROOT/"Assets/Scripts/UI/ObjectiveCardLayout.cs",ROOT/"Assets/Scripts/Core/PortalInteractionPolicy.cs",ROOT/"Assets/Scripts/Core/SaveLifecycleGate.cs",ROOT/"Tests/InterfaceSafetyTests.cs"],
            'using System; internal static class Program { static void Main() { Console.WriteLine(InterfaceSafetyTests.Run()); } }'))
        checks.append(("destructible-props",[ROOT/"Assets/Scripts/Core/DestructiblePropRules.cs",ROOT/"Tests/DestructiblePropTests.cs"],
            'using System; internal static class Program { static void Main() { Console.WriteLine(DestructiblePropTests.Run()); } }'))
        checks.append(("destructible-traversal",[ROOT/"Assets/Scripts/World/WorldTraversal.cs",ROOT/"Tests/DestructibleTraversalTests.cs"],
            'using System; internal static class Program { static void Main() { Console.WriteLine(DestructibleTraversalTests.Run()); } }'))
        checks.append(("safe-save-flow",[ROOT/"Assets/Scripts/Core/GameTypes.cs",ROOT/"Assets/Scripts/Core/ProgressionService.cs",ROOT/"Assets/Scripts/Core/SafeSaveFlow.cs",ROOT/"Tests/ProgressionTests.cs",ROOT/"Tests/SafeSaveFlowTests.cs"],
            'using System; internal static class Program { static void Main(string[] args) { Console.WriteLine(SafeSaveFlowTests.Run(args[0])); } }'))
        checks.append(("hub-economy",[ROOT/"Assets/Scripts/Core/GameTypes.cs",ROOT/"Assets/Scripts/Core/ProgressionService.cs",ROOT/"Tests/ProgressionTests.cs",ROOT/"Tests/HubTravelEconomyTests.cs"],
            'using System; internal static class Program { static void Main(string[] args) { Console.WriteLine(HubTravelEconomyTests.Run(args[0])); } }'))
        checks.append(("mode-rewards",[ROOT/"Assets/Scripts/Core/GameTypes.cs",ROOT/"Assets/Scripts/Core/ProgressionService.cs",ROOT/"Tests/ProgressionTests.cs",ROOT/"Tests/ModeRewardTests.cs"],
            'using System; internal static class Program { static void Main(string[] args) { Console.WriteLine(ModeRewardTests.Run(args[0])); } }'))
        checks.append(("run-combat-rules",[ROOT/"Assets/Scripts/Core/GameTypes.cs",ROOT/"Assets/Scripts/Core/SkillRuntime.cs",ROOT/"Assets/Scripts/Core/RunChoices.cs",ROOT/"Assets/Scripts/Combat/CombatDamage.cs",ROOT/"Assets/Scripts/Combat/EnemyControlPolicy.cs",ROOT/"Assets/Scripts/Combat/BossAttackPolicy.cs",ROOT/"Assets/Scripts/Combat/ArenaBossPatternPolicy.cs",ROOT/"Tests/SkillRuntimeTests.cs",ROOT/"Tests/RunCombatRulesTests.cs"],
            'using System; internal static class Program { static void Main() { Console.WriteLine(RunCombatRulesTests.Run()); } }'))
        checks.append(("progression-attention",[ROOT/"Assets/Scripts/Core/GameTypes.cs",ROOT/"Assets/Scripts/Core/ProgressionService.cs",ROOT/"Assets/Scripts/Core/ProgressionAttention.cs",ROOT/"Tests/ProgressionTests.cs",ROOT/"Tests/ProgressionAttentionTests.cs"],
            'using System; internal static class Program { static void Main(string[] args) { Console.WriteLine(ProgressionAttentionTests.Run(args[0])); } }'))
        checks.append(("expedition-modes",[ROOT/"Assets/Scripts/Core/ExpeditionModeState.cs",ROOT/"Assets/Scripts/Core/TierRewardBand.cs",ROOT/"Tests/ExpeditionModeStateTests.cs"],
            'using System; internal static class Program { static void Main() { Console.WriteLine(ExpeditionModeStateTests.Run()); } }'))
        checks.append(("run-recap",[ROOT/"Assets/Scripts/UI/RunRecapPresentation.cs",ROOT/"Tests/RunRecapPresentationTests.cs"],
            'using System; internal static class Program { static void Main() { Console.WriteLine(RunRecapPresentationTests.Run()); } }'))
        checks.append(("filled-vfx",[ROOT/"Assets/Scripts/Core/FilledVfxRecipes.cs",ROOT/"Tests/FilledVfxRecipeTests.cs"],
            'using System; internal static class Program { static void Main() { Console.WriteLine(FilledVfxRecipeTests.Run()); } }'))
        checks.append(("procedural-visuals",[ROOT/"Assets/Scripts/Combat/VisualMeshRecipes.cs",ROOT/"Tests/ProceduralVisualTests.cs"],
            'using System; internal static class Program { static void Main() { Console.WriteLine(ProceduralVisualTests.Run()); } }'))
        checks.append(("save-deletion",[ROOT/"Assets/Scripts/Core/GameTypes.cs",ROOT/"Assets/Scripts/Core/ProgressionService.cs",ROOT/"Tests/ProgressionTests.cs",ROOT/"Tests/SaveDeletionTests.cs"],
            'using System; internal static class Program { static void Main(string[] args) { Console.WriteLine(SaveDeletionTests.Run(args[0])); } }'))
        checks.append(("scheduled-ticks",[ROOT/"Assets/Scripts/Core/ScheduledTickWindow.cs",ROOT/"Tests/ScheduledTickWindowTests.cs"],
            'using System; internal static class Program { static void Main() { Console.WriteLine(ScheduledTickWindowTests.Run()); } }'))
        checks.append(("combat-pacing-followup",[ROOT/"Assets/Scripts/Combat/BossAttackPolicy.cs",ROOT/"Assets/Scripts/Combat/CompanionRules.cs",ROOT/"Assets/Scripts/Combat/SummonerDamageRules.cs",ROOT/"Tests/CombatPacingFollowupTests.cs"],
            'using System; internal static class Program { static void Main() { Console.WriteLine(CombatPacingFollowupTests.Run()); } }'))
        checks.append(("combat-sight-traversal",[ROOT/"Assets/Scripts/World/WorldTraversal.cs",ROOT/"Assets/Scripts/Core/CombatSightRules.cs",ROOT/"Assets/Scripts/Combat/CombatSight.cs",ROOT/"Tests/DestructibleTraversalTests.cs",ROOT/"Tests/CombatSightTraversalTests.cs"],
            'using System; internal static class Program { static void Main() { Console.WriteLine(CombatSightTraversalTests.Run()); } }'))
        checks.append(("combo-resource-budgets",[ROOT/"Assets/Scripts/Core/GameTypes.cs",ROOT/"Assets/Scripts/Core/SkillDamageBudgets.cs",ROOT/"Assets/Scripts/Core/SkillRuntime.cs",ROOT/"Assets/Scripts/Core/ScheduledTickWindow.cs",ROOT/"Assets/Scripts/Combat/CombatDamage.cs",ROOT/"Assets/Scripts/Combat/ProjectileVolleyBudget.cs",ROOT/"Assets/Scripts/Combat/SummonerDamageRules.cs",ROOT/"Assets/Scripts/Combat/CompanionRules.cs",ROOT/"Assets/Scripts/Combat/PlayerUpgradeRules.cs",ROOT/"Tests/SkillRuntimeTests.cs",ROOT/"Tests/ComboBudgetSimulation.cs",ROOT/"Tests/ComboBudgetTests.cs"],
            'using System; internal static class Program { static void Main(string[] args) { Console.WriteLine(ComboBudgetTests.Run(args[0])); } }'))
        checks.append(("skill-damage-budgets",[ROOT/"Assets/Scripts/Core/GameTypes.cs",ROOT/"Assets/Scripts/Core/SkillRuntime.cs",ROOT/"Assets/Scripts/Core/SkillDamageBudgets.cs",ROOT/"Assets/Scripts/Combat/CombatDamage.cs",ROOT/"Assets/Scripts/Combat/ProjectileVolleyBudget.cs",ROOT/"Tests/SkillRuntimeTests.cs",ROOT/"Tests/SkillDamageBudgetTests.cs"],
            'using System; internal static class Program { static void Main() { Console.WriteLine(SkillDamageBudgetTests.Run()); } }'))
        for name, test, helpers in [("mobile-skills-workshop-layout", "MobileSkillsWorkshopLayoutTests", ["MobileControlLayout", "MobilePanelLayout"]), ("mobile-collection-layout", "MobileCollectionLayoutTests", ["MobileControlLayout", "MobilePanelLayout", "MobileCollectionLayout"])]:
            checks.append((name, [ROOT/("Assets/Scripts/UI/"+helper+".cs") for helper in helpers]+[ROOT/("Tests/"+test+".cs")],
                'using System; internal static class Program { static void Main() { Console.WriteLine('+test+'.Run()); } }'))
        checks.append(("mobile-save-location",[ROOT/"Assets/Scripts/UI/MobileControlLayout.cs",ROOT/"Assets/Scripts/UI/MobilePanelLayout.cs",ROOT/"Assets/Scripts/UI/MobileSavePathText.cs",ROOT/"Tests/MobileSaveLocationTests.cs"],
            'using System; internal static class Program { static void Main() { Console.WriteLine(MobileSaveLocationTests.Run()); } }'))
        checks.append(("save-idempotence",[ROOT/"Assets/Scripts/Core/GameTypes.cs",ROOT/"Assets/Scripts/Core/ProgressionService.cs",ROOT/"Tests/ProgressionTests.cs",ROOT/"Tests/SaveIdempotenceTests.cs"],
            'using System; internal static class Program { static void Main(string[] args) { Console.WriteLine(SaveIdempotenceTests.Run(args[0])); } }'))
        checks.append(("equipment-lookups",[ROOT/"Assets/Scripts/Core/GameTypes.cs",ROOT/"Assets/Scripts/Core/ProgressionService.cs",ROOT/"Tests/ProgressionTests.cs",ROOT/"Tests/EquipmentLookupTests.cs"],
            'using System; internal static class Program { static void Main(string[] args) { Console.WriteLine(EquipmentLookupTests.Run(args[0])); } }'))
        checks.append(("explicit-action-persistence",[ROOT/"Assets/Scripts/Core/GameTypes.cs",ROOT/"Assets/Scripts/Core/ProgressionService.cs",ROOT/"Tests/ProgressionTests.cs",ROOT/"Tests/ExplicitActionPersistenceTests.cs"],
            'using System; internal static class Program { static void Main(string[] args) { Console.WriteLine(ExplicitActionPersistenceTests.Run(args[0])); } }'))
        checks.append(("progression-growth",[ROOT/"Assets/Scripts/Core/GameTypes.cs",ROOT/"Assets/Scripts/Core/ProgressionService.cs",ROOT/"Tests/ProgressionTests.cs",ROOT/"Tests/ProgressionGrowthTests.cs"],
            'using System; internal static class Program { static void Main(string[] args) { Console.WriteLine(ProgressionGrowthTests.Run(args[0])); } }'))
        checks.append(("enemy-kill-rewards", [ROOT/"Assets/Scripts/Core/GameTypes.cs", ROOT/"Assets/Scripts/Core/ProgressionService.cs", ROOT/"Tests/ProgressionTests.cs", ROOT/"Tests/EnemyKillRewardTests.cs"],
            'using System; internal static class Program { static void Main(string[] args) { Console.WriteLine(EnemyKillRewardTests.Run(args[0])); } }'))
        checks.append(("world-loot-receipts", [ROOT/"Assets/Scripts/Core/GameTypes.cs", ROOT/"Assets/Scripts/Core/ProgressionService.cs", ROOT/"Tests/ProgressionTests.cs", ROOT/"Tests/WorldLootReceiptTests.cs"],
            'using System; internal static class Program { static void Main(string[] args) { Console.WriteLine(WorldLootReceiptTests.Run(args[0])); } }'))
        checks.append(("build-presets", [ROOT/"Assets/Scripts/Core/GameTypes.cs", ROOT/"Assets/Scripts/Core/ProgressionService.cs", ROOT/"Tests/ProgressionTests.cs", ROOT/"Tests/BuildPresetTests.cs"],
            'using System; internal static class Program { static void Main(string[] args) { Console.WriteLine(BuildPresetTests.Run(args[0])); } }'))
        checks.append(("adventure-progression", [ROOT/"Assets/Scripts/Core/GameTypes.cs", ROOT/"Assets/Scripts/Core/ProgressionService.cs", ROOT/"Tests/ProgressionTests.cs", ROOT/"Tests/AdventureProgressionTests.cs"],
            'using System; internal static class Program { static void Main(string[] args) { Console.WriteLine(AdventureProgressionTests.Run(args[0])); } }'))
        checks.append(("build-size-policy", [ROOT/"Assets/Editor/BuildSizePolicy.cs", ROOT/"Tests/BuildSizePolicyTests.cs"],
            'using System; internal static class Program { static void Main() { Console.WriteLine(BuildSizePolicyTests.Run()); } }'))
        for name,test in [("progression-goal-identity","ProgressionGoalIdentityTests"),("progression-route-layers","ProgressionRouteLayerTests")]:
            checks.append((name,[ROOT/("Assets/Scripts/Core/"+f+".cs") for f in ["GameTypes","ProgressionService","CampRouteCards","RunChoices","SkillRuntime","ProgressionHudHint","ProgressionAttention"]]+[ROOT/"Tests/ProgressionTests.cs",ROOT/("Tests/"+test+".cs")],
                'using System; internal static class Program { static void Main(string[] args) { Console.WriteLine('+test+'.Run(args[0])); } }'))
        checks.append(("chest-reveal-presentation",[ROOT/"Assets/Scripts/Core/GameTypes.cs",ROOT/"Assets/Scripts/Core/ProgressionService.cs",ROOT/"Assets/Scripts/UI/ChestRevealPresentation.cs",ROOT/"Tests/ProgressionTests.cs",ROOT/"Tests/ChestRevealPresentationTests.cs"],
            'using System; internal static class Program { static void Main(string[] args) { Console.WriteLine(ChestRevealPresentationTests.Run(args[0])); } }'))
        checks.append(("side-event-rewards",[ROOT/"Assets/Scripts/Core/GameTypes.cs",ROOT/"Assets/Scripts/Core/ProgressionService.cs",ROOT/"Tests/ProgressionTests.cs",ROOT/"Tests/SideEventRewardTests.cs"],
            'using System; internal static class Program { static void Main(string[] args) { Console.WriteLine(SideEventRewardTests.Run(args[0])); } }'))
        checks.append(("adventure-entry-truth",[ROOT/("Assets/Scripts/"+f+".cs") for f in ["UI/AdventureEntryPresentation","Core/ExpeditionModeState","Core/TierRewardBand","Core/TierRewardRules","Core/GameTypes","Core/SkillRuntime","Core/ProgressionGoalState"]]+[ROOT/"Tests/SkillRuntimeTests.cs",ROOT/"Tests/AdventureEntryPresentationTests.cs"],
            'using System; internal static class Program { static void Main() { Console.WriteLine(AdventureEntryPresentationTests.Run()); } }'))
        checks.append(("mobile-room-objective",[ROOT/"Assets/Scripts/Core/RoomTacticalRegion.cs",ROOT/"Assets/Scripts/Core/RoomChainState.cs",ROOT/"Assets/Scripts/UI/RoomObjectivePresentation.cs",ROOT/"Assets/Scripts/UI/MobileControlLayout.cs",ROOT/"Tests/RoomObjectivePresentationTests.cs"],
            'using System; internal static class Program { static void Main() { Console.WriteLine(RoomObjectivePresentationTests.Run()); } }'))
        checks.append(("environment-readability",[ROOT/("Assets/Scripts/"+f+".cs") for f in ["Core/ArenaPulseRules","Core/WorldLabelReadability","Core/CameraVisibilityRules","Core/CombatSightRules","Combat/CombatSight","World/WorldTraversal"]]+[ROOT/"Tests/DestructibleTraversalTests.cs",ROOT/"Tests/EnvironmentReadabilityTests.cs"],
            'using System; internal static class Program { static void Main() { Console.WriteLine(EnvironmentReadabilityTests.Run()); } }'))
        checks.append(("escape-room-formation",[ROOT/("Assets/Scripts/"+f+".cs") for f in ["Core/EscapePostPolicy","Core/RoomTacticalRegion","Core/RoomChainState","World/WorldTraversal","World/TacticalRoomGeometry","World/EscapeRoomFormation"]]+[ROOT/"Tests/DestructibleTraversalTests.cs",ROOT/"Tests/EscapeRoomFormationTests.cs"],
            'using System; internal static class Program { static void Main() { Console.WriteLine(EscapeRoomFormationTests.Run()); } }'))
        checks.append(("tactical-room-region",[ROOT/("Assets/Scripts/"+f+".cs") for f in ["Core/RoomTacticalRegion","Core/RoomChainState","Core/ExpeditionModeState","Core/TierRewardBand","UI/RoomObjectivePresentation","World/WorldTraversal"]]+[ROOT/"Tests/DestructibleTraversalTests.cs",ROOT/"Tests/RoomTacticalRegionTests.cs"],
            'using System; internal static class Program { static void Main() { Console.WriteLine(RoomTacticalRegionTests.Run()); } }'))
        checks.append(("tactical-room-geometry",[ROOT/"Assets/Scripts/World/WorldTraversal.cs",ROOT/"Assets/Scripts/World/TacticalRoomGeometry.cs",ROOT/"Tests/DestructibleTraversalTests.cs",ROOT/"Tests/TacticalRoomGeometryTests.cs"],
            'using System; internal static class Program { static void Main() { Console.WriteLine(TacticalRoomGeometryTests.Run()); } }'))
        for name, test, helpers in [
            ("basic-action-timeline", "BasicActionTimelineTests", ["Core/BasicActionTimeline"]),
            ("weapon-structure", "WeaponStructureTests", ["Core/WeaponStructure"]),
            ("equipment-attachment", "EquipmentAttachmentTests", ["Core/EquipmentAttachmentRecipe", "Core/WeaponStructure"]),
            ("progression-goal-layout", "ProgressionGoalLayoutTests", ["UI/MobilePanelLayout", "UI/ProgressionGoalLayout"]),
            ("visual-motion-envelope", "VisualMotionEnvelopeTests", ["Core/VisualMotionEnvelope"]),
            ("environment-light-profile", "EnvironmentLightProfileTests", ["Core/EnvironmentLightProfile"]),
            ("water-presentation", "WaterPresentationTests", ["Core/WaterPresentation"]),
            ("collection-preview-composition", "CollectionPreviewCompositionTests", ["UI/CollectionPreviewComposition"]),
            ("costume-layers", "CostumeLayersTests", ["Core/CostumeLayers"]),
            ("panel-readability", "PanelReadabilityLayoutTests", ["UI/AdventureSelectionLayout", "UI/MobilePanelLayout"]),
            ("decoration-budget", "DecorationBudgetTests", ["Core/DecorationBudget"]),
            ("mobile-combat-feedback", "MobileCombatFeedbackTests", ["UI/MobileCombatPresentation"]),
            ("combat-text-layout", "CombatTextLayoutTests", ["Combat/CombatTextLayout"]),
            ("combat-opportunity", "CombatOpportunityTests", ["UI/CombatOpportunityPresentation"]),
            ("large-boss-motion", "LargeBossMotionTests", ["Core/LargeBossMotion", "Core/LargeBossPhaseState"]),
            ("deferred-room-choice", "DeferredRoomChoiceTests", ["Core/DeferredRoomChoice"]),
            ("guardian-charge-pose", "GuardianChargePoseTests", ["Core/LocomotionPoseState", "Combat/BossAttackPolicy"]),
            ("locomotion-poses", "LocomotionPoseTests", ["Core/LocomotionPoseState"]),
            ("camera-visibility", "CameraVisibilityTests", ["Core/CameraVisibilityRules"]),
            ("companion-directive", "CompanionDirectiveTests", ["Combat/CompanionDirective"]),
        ]:
            checks.append((name,[ROOT/("Assets/Scripts/"+helper+".cs") for helper in helpers]+[ROOT/("Tests/"+test+".cs")],
                'using System; internal static class Program { static void Main() { Console.WriteLine('+test+'.Run()); } }'))
        checks.append(("equipment-comparison",[ROOT/"Assets/Scripts/Core/GameTypes.cs",ROOT/"Assets/Scripts/Core/SkillRuntime.cs",ROOT/"Assets/Scripts/UI/EquipmentComparisonPresentation.cs",ROOT/"Tests/SkillRuntimeTests.cs",ROOT/"Tests/EquipmentComparisonPresentationTests.cs"],
            'using System; internal static class Program { static void Main() { Console.WriteLine(EquipmentComparisonPresentationTests.Run()); } }'))
        checks.append(("hub-settlement-geometry",[ROOT/"Assets/Scripts/World/WorldTraversal.cs",ROOT/"Assets/Scripts/World/HubSettlementPlan.cs",ROOT/"Tests/DestructibleTraversalTests.cs",ROOT/"Tests/HubSettlementGeometryTests.cs"],
            'using System; internal static class Program { static void Main() { Console.WriteLine(HubSettlementGeometryTests.Run()); } }'))
        checks.append(("costume-recipes",[ROOT/"Assets/Scripts/Core/GameTypes.cs",ROOT/"Assets/Scripts/Core/SkillRuntime.cs",ROOT/"Assets/Scripts/Core/CostumeRecipes.cs",ROOT/"Tests/SkillRuntimeTests.cs",ROOT/"Tests/CostumeRecipeTests.cs"],
            'using System; internal static class Program { static void Main() { Console.WriteLine(CostumeRecipeTests.Run()); } }'))
        checks.append(("skill-visual-recipe",[ROOT/"Assets/Scripts/Core/GameTypes.cs",ROOT/"Assets/Scripts/Core/SkillRuntime.cs",ROOT/"Assets/Scripts/Core/FilledVfxRecipes.cs",ROOT/"Assets/Scripts/Core/SkillVisualRecipe.cs",ROOT/"Tests/SkillRuntimeTests.cs",ROOT/"Tests/SkillVisualRecipeTests.cs"],
            'using System; internal static class Program { static void Main() { Console.WriteLine(SkillVisualRecipeTests.Run()); } }'))
        checks.append(("hold-point-state",[ROOT/"Assets/Scripts/Core/ExpeditionModeState.cs",ROOT/"Assets/Scripts/Core/TierRewardBand.cs",ROOT/"Tests/HoldPointStateTests.cs"],
            'using System; internal static class Program { static void Main() { Console.WriteLine(HoldPointStateTests.Run()); } }'))
        checks.append(("room-blessing-usability",[ROOT/("Assets/Scripts/"+f+".cs") for f in ["Core/GameTypes","Core/RunChoices","Core/SkillRuntime","Core/SkillDamageBudgets","Combat/EnemyControlPolicy"]]+[ROOT/"Tests/SkillRuntimeTests.cs",ROOT/"Tests/RoomBlessingUsabilityTests.cs"],
            'using System; internal static class Program { static void Main() { Console.WriteLine(RoomBlessingUsabilityTests.Run()); } }'))
        checks.append(("room-blessing-preview",[ROOT/"Assets/Scripts/Core/RoomChainState.cs",ROOT/"Assets/Scripts/Core/RoomTactics.cs",ROOT/"Assets/Scripts/UI/RoomBlessingPreview.cs",ROOT/"Tests/RoomBlessingPreviewTests.cs"],
            'using System; internal static class Program { static void Main() { Console.WriteLine(RoomBlessingPreviewTests.Run()); } }'))
        checks.append(("room-blessing-routes",[ROOT/"Assets/Scripts/Core/GameTypes.cs",ROOT/"Assets/Scripts/Core/RunChoices.cs",ROOT/"Assets/Scripts/Core/CampRouteCards.cs",ROOT/"Assets/Scripts/Core/SkillRuntime.cs",ROOT/"Tests/SkillRuntimeTests.cs",ROOT/"Tests/RoomBlessingRouteTests.cs"],
            'using System; internal static class Program { static void Main() { Console.WriteLine(RoomBlessingRouteTests.Run()); } }'))
        checks.append(("mobile-pause-navigation",[ROOT/"Assets/Scripts/UI/GameUI.PauseNavigation.cs",ROOT/"Tests/MobilePauseNavigationTests.cs"],
            'using System; internal static class Program { static void Main() { Console.WriteLine(MobilePauseNavigationTests.Run()); } }'))
        checks.append(("collection-render-lifecycle",[ROOT/"Assets/Scripts/Core/RendererGroupCache.cs",ROOT/"Assets/Scripts/UI/CollectionModelPreview.cs",ROOT/"Assets/Scripts/UI/CollectionPreviewState.cs",ROOT/"Assets/Scripts/UI/CollectionPreviewComposition.cs",ROOT/"Tests/CollectionRenderLifecycleTests.cs"],
            'using System; internal static class Program { static void Main() { Console.WriteLine(CollectionRenderLifecycleTests.Run()); } }'))
        checks.append(("collection-render-lifecycle-modern",[ROOT/"Assets/Scripts/Core/RendererGroupCache.cs",ROOT/"Assets/Scripts/UI/CollectionModelPreview.cs",ROOT/"Assets/Scripts/UI/CollectionPreviewState.cs",ROOT/"Assets/Scripts/UI/CollectionPreviewComposition.cs",ROOT/"Tests/CollectionRenderLifecycleTests.cs"],
            'using System; internal static class Program { static void Main() { Console.WriteLine(CollectionRenderLifecycleTests.Run()); } }'))
        checks.append(("ui-render-cache",[ROOT/"Assets/Scripts/Core/GameTypes.cs",ROOT/"Assets/Scripts/Core/ProgressionService.cs",ROOT/"Tests/ProgressionTests.cs",ROOT/"Assets/Scripts/UI/CollectionPreviewState.cs",ROOT/"Assets/Scripts/Combat/CombatTextMetrics.cs",ROOT/"Assets/Scripts/Combat/CombatTextLayout.cs",ROOT/"Tests/UiRenderCacheTests.cs"],
            'using System; internal static class Program { static void Main() { Console.WriteLine(UiRenderCacheTests.Run()); } }'))
        checks.append(("ui-render-cache-lifecycle",[ROOT/"Assets/Scripts/Combat/FloatingNumber.cs",ROOT/"Assets/Scripts/Combat/CombatTextMetrics.cs",ROOT/"Assets/Scripts/Combat/CombatTextLayout.cs",ROOT/"Tests/UiRenderCacheLifecycleTests.cs"],
            'using System; internal static class Program { static void Main() { Console.WriteLine(UiRenderCacheLifecycleTests.Run()); } }'))
        for variant in ("desktop", "android", "ios"):
            checks.append(("game-font-"+variant,[ROOT/"Assets/Scripts/UI/GameFont.cs",ROOT/"Tests/GameFontTests.cs"],
                'using System; internal static class Program { static void Main() { Console.WriteLine(GameFontTests.Run()); } }'))
        checks.append(("android-lifecycle",[ROOT/"Assets/Scripts/UI/TouchViewportState.cs",ROOT/"Assets/Scripts/UI/TouchReleaseLatch.cs",ROOT/"Assets/Scripts/Core/AudioLifecycleGate.cs",ROOT/"Assets/Scripts/Core/ApplicationPauseState.cs",ROOT/"Assets/Scripts/Core/SaveLifecycleGate.cs",ROOT/"Tests/AndroidLifecycleTests.cs"],
            'using System; internal static class Program { static void Main() { Console.WriteLine(AndroidLifecycleTests.Run()); } }'))
        checks.append(("combat-review-object-id-modern-contract",[ROOT/"Assets/Scripts/Core/CombatReviewObjectId.cs",ROOT/"Tests/CombatReviewObjectIdTests.cs"],
            'using System; internal static class Program { static void Main() { Console.WriteLine(CombatReviewObjectIdTests.Run()); } }'))
        checks.append(("combat-review-object-id-legacy",[ROOT/"Assets/Scripts/Core/CombatReviewObjectId.cs",ROOT/"Tests/CombatReviewObjectIdTests.cs"],
            'using System; internal static class Program { static void Main() { Console.WriteLine(CombatReviewObjectIdTests.Run()); } }'))
        checks.append(("combat-review-events",[ROOT/"Assets/Scripts/Core/GameTypes.cs",ROOT/"Assets/Scripts/Core/CombatReviewEvents.cs",ROOT/"Assets/Scripts/Core/CombatReviewConfigurations.cs",ROOT/"Assets/Scripts/Core/SkillRuntime.cs",ROOT/"Tests/SkillRuntimeTests.cs",ROOT/"Tests/CombatReviewEventsTests.cs"],
            'using System; internal static class Program { static void Main() { Console.WriteLine(CombatReviewEventsTests.Run()); } }'))
        checks.append(("combat-review-rules",[ROOT/"Assets/Scripts/Core/GameTypes.cs",ROOT/"Assets/Scripts/Core/SkillRuntime.cs",ROOT/"Assets/Scripts/Core/SkillDamageBudgets.cs",ROOT/"Assets/Scripts/Core/AudioVoicePolicy.cs",ROOT/"Assets/Scripts/Core/LockedImpactMarkPolicy.cs",ROOT/"Assets/Scripts/Core/BasicActionTimeline.cs",ROOT/"Assets/Scripts/Combat/BossAttackPolicy.cs",ROOT/"Tests/SkillRuntimeTests.cs",ROOT/"Tests/CombatReviewRulesTests.cs"],
            'using System; internal static class Program { static void Main() { Console.WriteLine(CombatReviewRulesTests.Run()); } }'))
        checks.append(("hotbar-reset",[ROOT/"Assets/Scripts/Core/GameTypes.cs",ROOT/"Assets/Scripts/Core/ProgressionService.cs",ROOT/"Tests/ProgressionTests.cs",ROOT/"Tests/HotbarResetTests.cs"],
            'using System; internal static class Program { static void Main(string[] args) { Console.WriteLine(HotbarResetTests.Run(args[0])); } }'))
        checks.append(("chapter-room-geometry",[ROOT/"Assets/Scripts/World/WorldTraversal.cs",ROOT/"Assets/Scripts/World/ChapterRoomGeometry.cs",ROOT/"Assets/Scripts/Core/ChapterProgression.cs",ROOT/"Assets/Scripts/Core/RoomTactics.cs",ROOT/"Assets/Scripts/Core/ArenaPulseRules.cs",ROOT/"Assets/Scripts/World/ChapterHazardGeometry.cs",ROOT/"Assets/Scripts/World/TacticalRoomGeometry.cs",ROOT/"Tests/DestructibleTraversalTests.cs",ROOT/"Tests/ChapterGeometryFixture.cs",ROOT/"Tests/ChapterRoomGeometryTests.cs",ROOT/"Tests/ChapterFormationGeometryTests.cs"],
            'using System; internal static class Program { static void Main() { Console.WriteLine(ChapterRoomGeometryTests.Run()); Console.WriteLine(ChapterFormationGeometryTests.Run()); } }'))
        checks.append(("chapter-presentation",[ROOT/"Assets/Scripts/Core/ChapterResultSnapshot.cs",ROOT/"Assets/Scripts/Core/GameTypes.cs",ROOT/"Assets/Scripts/Core/ProgressionService.cs",ROOT/"Assets/Scripts/UI/ChapterEntryPresentation.cs",ROOT/"Tests/ProgressionTests.cs",ROOT/"Tests/ChapterPresentationTests.cs"],
            'using System; internal static class Program { static void Main() { Console.WriteLine(ChapterPresentationTests.Run()); } }'))
        checks.append(("chest-currency-deltas",[ROOT/"Assets/Scripts/Core/GameTypes.cs",ROOT/"Assets/Scripts/Core/ProgressionService.cs",ROOT/"Tests/ProgressionTests.cs",ROOT/"Tests/ChestCurrencyDeltaTests.cs"],
            'using System; internal static class Program { static void Main(string[] args) { Console.WriteLine(ChestCurrencyDeltaTests.Run(args[0])); } }'))
        checks.append(("reward-viewing-rules",[ROOT/"Assets/Scripts/Core/GameTypes.cs",ROOT/"Assets/Scripts/Core/ProgressionService.cs",ROOT/"Assets/Scripts/UI/ChestRevealPresentation.cs",ROOT/"Assets/Scripts/UI/ChestCompositeRules.cs",ROOT/"Assets/Scripts/UI/CollectionViewingState.cs",ROOT/"Assets/Scripts/UI/CollectionPreviewComposition.cs",ROOT/"Tests/ProgressionTests.cs",ROOT/"Tests/RewardViewingRulesTests.cs"],
            'using System; internal static class Program { static void Main() { Console.WriteLine(RewardViewingRulesTests.Run()); } }'))
        for _, sources, _ in checks:
            if ROOT / "Assets/Scripts/Core/RoomChainState.cs" in sources:
                sources.append(ROOT / "Assets/Scripts/Core/RoomTactics.cs")
        for _, sources, _ in checks:
            if ROOT / "Assets/Scripts/Core/RunChoices.cs" in sources:
                sources.append(ROOT / "Assets/Scripts/Core/RunChoices.Rooms.cs")
        for _, sources, _ in checks:
            if ROOT / "Assets/Scripts/Core/GameTypes.cs" in sources:
                sources.append(ROOT / "Assets/Scripts/Core/CombatBalance.cs")
        for _,sources,_ in checks:
            if ROOT/"Assets/Scripts/Core/ProgressionService.cs" in sources:
                for helper in ["HubTravelRules","MasteryCoreRuntime","TierRewardRules","TierRewardBand","ProgressionGoalState","ChapterProgression","ProgressionService.Chapter","ProgressionService.Reforge","ReforgeQuote","RoomTactics"]:sources.append(ROOT/("Assets/Scripts/Core/"+helper+".cs"))
            if ROOT/"Assets/Scripts/Core/ProgressionGoalState.cs" in sources:
                sources.append(ROOT/"Assets/Scripts/Core/ReforgeQuote.cs")
            if ROOT/"Tests/CombatBalanceTests.cs" in sources:sources.append(ROOT/"Assets/Scripts/Core/SkillDamageBudgets.cs")
        for _, sources, _ in checks:
            sources[:] = list(dict.fromkeys(sources))
        for name, script in [("asset-meta-guids", "Tools/validate-meta-guids.py"),
                             ("asset-meta-guid-controls", "Tests/MetaGuidAuditTests.py")]:
            passed = run_check(name, [[sys.executable, str(ROOT / script)]], env, output, report)
            failed = failed or not passed
        for name, sources, program in checks:
            project = write_project(workspace / name, sources, program, defines={"collection-render-lifecycle-modern":"UNITY_2023_1_OR_NEWER", "combat-review-object-id-modern-contract":"UNITY_6000_6_OR_NEWER", "game-font-android":"UNITY_ANDROID", "game-font-ios":"UNITY_IOS"}.get(name, ""))
            commands = [[dotnet, "restore", str(project), "--configfile", str(config), "--verbosity", "quiet"],
                        [dotnet, "run", "--project", str(project), "--no-restore", "--configuration", "Release", "--", str(workspace / "saves")]]
            passed = run_check(name, commands, env, output, report)
            failed = failed or not passed
        passed = run_check("combat-review-instrumentation", [[sys.executable, str(ROOT / "Tests/CombatReviewInstrumentationTests.py"), "--dotnet", dotnet]], env, output, report)
        failed = failed or not passed
        passed = run_check("fading-combat-effect-lifecycle", [[sys.executable, str(ROOT / "Tests/FadingCombatEffectLifecycleTests.py")]], dict(env, DOTNET=dotnet), output, report)
        failed = failed or not passed
        passed = run_check("production-touch-lifecycle", [[sys.executable, str(ROOT / "Tests/ProductionTouchLifecycleTests.py"), dotnet]], env, output, report)
        failed = failed or not passed
        passed = run_check("chest-pause-back-production", [[sys.executable, str(ROOT / "Tests/ChestPauseBackProductionTests.py"), dotnet]], env, output, report)
        failed = failed or not passed
        for name, script in [('tactical-live-visual', 'TacticalLiveVisualTests.py'), ('companion-appearance', 'CompanionAppearanceTests.py'), ('large-boss-shutdown', 'LargeBossShutdownTests.py'), ('burn-finale-production', 'BurnFinaleProductionTests.py'), ('economy-growth', 'EconomyGrowthTests.py'), ('companion-intent-production', 'CompanionIntentProductionTests.py'), ('contract-snapshot-production', 'ContractSnapshotProductionTests.py'), ('filled-vfx-allocation', 'FilledVfxAllocationTests.py'), ('side-event-production', 'SideEventProductionTests.py'), ('skill-readability-production', 'SkillReadabilityProductionTests.py'), ('milestone-goal-surface', 'MilestoneGoalSurfaceTests.py'), ('skill-panel-navigation', 'SkillPanelNavigationProductionTests.py'), ('room-blessing-usability-negative', 'RoomBlessingUsabilityNegativeTests.py'), ('collection-stage-recovery-negative', 'CollectionStageRecoveryNegativeTests.py'), ('mobile-inventory-back-production', 'MobileInventoryBackProductionTests.py'), ('elemental-field-continuity', 'ElementalFieldContinuityTests.py'), ('blocked-combat-update-production', 'BlockedCombatUpdateProductionTests.py'), ('mobile-blessing-preview-production', 'MobileBlessingPreviewProductionTests.py'), ('room-preview-ui-contract', 'RoomBlessingPreviewSourceTests.py'), ('room-tactical-los-production', 'RoomTacticalLosProductionTests.py'), ('chest-composite-production', 'ChestCompositeProductionTests.py'), ('chest-trial-production', 'ChestTrialProductionTests.py'), ('blessing-damage-production', 'BlessingDamageProductionTests.py'), ('room-failure-evidence', 'RoomFailureEvidenceTests.py'), ('shatter-availability', 'ShatterAvailabilityTests.py'), ('hero-pose-commit', 'HeroPoseCommitTests.py'), ('weapon-swing-identity', 'WeaponSwingIdentityTests.py'), ('guard-return', 'GuardReturnTests.py'), ('world-label-production', 'WorldLabelProductionTests.py'), ('combat-readability-production', 'CombatReadabilityProductionTests.py'), ('enemy-impact-contour', 'EnemyImpactContourTests.py'), ('chapter-progression', 'ChapterProgressionTests.py'), ('chapter-combat-production', 'ChapterCombatProductionTests.py'), ('chapter-entry-production', 'ChapterEntryProductionTests.py'), ('chapter-ui-wiring', 'ChapterUIWiringTests.py'), ('chapter-host-production', 'ChapterHostProductionTests.py'), ('water-flow-production', 'WaterFlowProductionTests.py'), ('occlusion-cause-production', 'OcclusionCauseProductionTests.py'), ('route-skill-navigation', 'RouteSkillNavigationProductionTests.py'), ('growth-navigation-source', 'GrowthNavigationSourceTests.py'), ('equipment-attachment-source', 'EquipmentAttachmentSourceTests.py'), ('equipment-composition-production', 'EquipmentCompositionProductionTests.py'), ('elemental-priority-production', 'ElementalPriorityProductionTests.py'), ('chapter-return-time-scale', 'ChapterReturnTimeScaleTests.py'), ('integrated-player-journey', 'IntegratedJourneyTests.py'), ('anchored-impact-coverage', 'AnchoredImpactCoverageTests.py'), ('shore-contact-production', 'ShoreContactProductionTests.py'), ('economy-goal-ui', 'EconomyGoalUiTests.py'), ('chapterexperience', 'ChapterExperienceTests.py'), ('chaptersealrouteproduction', 'ChapterSealRouteProductionTests.py'), ('mobilepinnedtargetproduction', 'MobilePinnedTargetProductionTests.py'), ('restricted-healing-production', 'RestrictedHealingProductionTests.py'), ('effectpriorityproduction', 'EffectPriorityProductionTests.py'), ('boss-sweep-capsule-production', 'BossSweepCapsuleProductionTests.py'), ('dense-finale-production', 'DenseFinaleProductionTests.py'), ('casterchargeproduction', 'CasterChargeProductionTests.py'), ('classtierstructureproduction', 'ClassTierStructureProductionTests.py'), ('collectionposeisolationproduction', 'CollectionPoseIsolationProductionTests.py'), ('collectionpreviewpresentationproduction', 'CollectionPreviewPresentationProductionTests.py'), ('previewclothproduction', 'PreviewClothProductionTests.py'), ('weaponfashionstructureproduction', 'WeaponFashionStructureProductionTests.py'), ('chapternodeworldproduction', 'ChapterNodeWorldProductionTests.py'), ('reforgeselection', 'ReforgeSelectionTests.py'), ('combat-opportunity-slot', 'CombatOpportunitySlotProductionTests.py'), ('desktop-opportunity-hotbar', 'DesktopOpportunityHotbarProductionTests.py'), ('collection-compact-geometry', 'CollectionCompactGeometryProductionTests.py'), ('equipment-appearance-production', 'EquipmentAppearanceProductionTests.py'), ('starting-skill-budget', 'StartingSkillBudgetTests.py'), ('level-up-point-feedback', 'LevelUpPointFeedbackTests.py'), ('pack-detour-production', 'PackDetourProductionTests.py'), ('chapter-mobile-support', 'ChapterMobileSupportTests.py'), ('mechanism-evidence-production', 'MechanismEvidenceProductionTests.py'), ('player-finale-tail-production', 'PlayerFinaleTailProductionTests.py'), ('returning-counter-production', 'ReturningCounterProductionTests.py'), ('returning-counter-persistence', 'ReturningCounterPersistenceTests.py')]:
            passed = run_check(name, [[sys.executable,str(ROOT/"Tests"/script),dotnet]], dict(env,DOTNET=dotnet), output, report)
            failed = failed or not passed
        # Round 3 packages: execute production paths and compiled negative controls.
        for name, script in [('practice-locomotion', 'PracticeLocomotionProductionTests.py'), ('practice-prop-isolation', 'PracticePropIsolationProductionTests.py'), ('enemy-status-anchor', 'EnemyStatusAnchorProductionTests.py'), ('mechanic-knowledge', 'MechanicKnowledgeProductionTests.py'), ('chest-choice-presentation', 'ChestChoicePresentationTests.py'), ('delayed-cast-first-hit', 'DelayedCastFirstHitTests.py'), ('delayed-cast-producer-lifetime', 'DelayedCastProducerLifetimeTests.py'), ('room-branch', 'RoomBranchProductionTests.py'), ('practice-pressure-combat', 'PracticePressureCombatProductionTests.py'), ('preset-replacement', 'PresetReplacementProductionTests.py'), ('venom-visual', 'VenomVisualProductionTests.py'), ('opportunity-duration', 'OpportunityDurationProductionTests.py'), ('mobile-opportunity-input', 'MobileOpportunityInputProductionTests.py'), ('practice-hud', 'PracticeHudProductionTests.py'), ('practice-action-settlement', 'PracticeActionSettlementProductionTests.py')]:
            passed = run_check(name, [[sys.executable,str(ROOT/"Tests"/script),dotnet]], dict(env,DOTNET=dotnet), output, report)
            failed = failed or not passed
        passed = run_check("room-free-seal-host", [[sys.executable,str(ROOT/"Tests/RoomFreeSealHostTests.py"),dotnet]], dict(env,DOTNET=dotnet), output, report)
        failed = failed or not passed
        for name, script in [("camp-build-draft-production", "CampBuildDraftProductionTests.py"),
                             ("scenery-presentation-production", "SceneryPresentationProductionTests.py"),
                             ("companion-path-allocation", "CompanionPathAllocationTests.py"),
                             ("combat-review-impact-production", "CombatReviewImpactProductionTests.py"),
                             ("death-loot-persistence-production", "DeathLootPersistenceProductionTests.py"),
                             ("room-first-choice-production", "RoomFirstChoiceProductionTests.py"),
                             ("redrock-replay-geometry", "RedrockReplayGeometryTests.py")]:
            passed = run_check(name, [[sys.executable,str(ROOT/"Tests"/script),dotnet]], dict(env,DOTNET=dotnet), output, report)
            failed = failed or not passed
        passed = run_check("room-seal-hud-production", [[sys.executable,str(ROOT/"Tests/RoomSealHudProductionTests.py"),dotnet]], dict(env,DOTNET=dotnet), output, report)
        failed = failed or not passed
        passed = run_check("authored-scenery-production", [[sys.executable,str(ROOT/"Tests/AuthoredSceneryProductionTests.py"),dotnet]], dict(env,DOTNET=dotnet), output, report)
        failed = failed or not passed
        passed = run_check("tree-occlusion-registry", [[sys.executable,str(ROOT/"Tests/TreeOcclusionRegistryTests.py"),dotnet]], dict(env,DOTNET=dotnet), output, report)
        failed = failed or not passed
        passed = run_check("blender-pilot-pose", [[sys.executable,str(ROOT/"Tools/test-blender-pilot.py")]], dict(env,DOTNET=dotnet), output, report)
        failed = failed or not passed
        for pilot_test in ["BlenderPilotAdapterProductionTests.py","PilotStarterCompatibilityTests.py","PilotFacingCommitProductionTests.py"]:
            passed = run_check(pilot_test[:-3], [[sys.executable,str(ROOT/"Tests"/pilot_test),dotnet]], dict(env,DOTNET=dotnet), output, report)
            failed = failed or not passed
        for name, script in [("authored-actor-modules", "ActorModulesProductionTests.py"),
                             ("actor-silhouette-f1-production", "ActorSilhouetteF1ProductionTests.py"),
                             ("integrated-actor-art-production", "IntegratedActorArtProductionTests.py"),
                             ("blender-skill-vfx-production", "BlenderSkillVfxProductionTests.py"),
                             ("blender-scenery-production", "BlenderSceneryProductionTests.py"),
                             ("authored-projectile-production", "AuthoredProjectileProductionTests.py"),
                             ("fixed-scenery-production", "FixedSceneryProductionTests.py"),
                             ("fixed-scenery-enabled-integration", "FixedSceneryEnabledIntegrationTests.py"),
                             ("authored-spell-bases", "AuthoredSpellBasesProductionTests.py"),
                             ("authored-spell-integration", "AuthoredSpellIntegrationTests.py"),
                             ("camera-occlusion-slots", "CameraOcclusionSlotsProductionTests.py"),
                             ("vanguard-actions-production", "VanguardActionsProductionTests.py"),
                             ("weapon-contact-production", "WeaponContactProductionTests.py"),
                             ("vanguard-recovery-integration", "VanguardRecoveryIntegrationTests.py"),
                             ("status-feedback-production", "StatusFeedbackProductionTests.py"),
                             ("defense-identity-production", "DefenseIdentityProductionTests.py"),
                             ("final-body-envelope-production", "FinalBodyEnvelopeProductionTests.py"),
                             ("skill-identity-callsite-production", "SkillIdentityCallsiteProductionTests.py"),
                             ("enemy-knockdown-production", "EnemyKnockdownProductionTests.py"),
                             ("enemy-knockdown-geometry", "EnemyKnockdownGeometryTests.py"),
                             ("tactical-attachments-production", "TacticalAttachmentsProductionTests.py"),
                             ("tactical-enemy-assembly", "TacticalEnemyAssemblyTests.py"),
                             ("filled-vfx-pool-production", "FilledVfxPoolProductionTests.py"),
                             ("arrow-batch-generation-production", "ArrowBatchGenerationProductionTests.py"),
                             ("enemy-silhouette-production", "EnemySilhouetteProductionTests.py"),
                             ("enemy-silhouette-animation", "EnemySilhouetteAnimationTests.py"),
                             ("wolf-silhouette-production", "WolfSilhouetteProductionTests.py")]:
            passed = run_check(name, [[sys.executable, str(ROOT/"Tests"/script), dotnet]], dict(env, DOTNET=dotnet), output, report)
            failed = failed or not passed
        # Planning round 2: real production paths, with explicit managed boundaries.
        for name, script in [('mastery-combo-round2', 'MasteryComboRound2Tests.py'), ('concentrated-venom-production', 'ConcentratedVenomProductionTests.py'), ('concentrated-venom-launch', 'ConcentratedVenomLaunchTests.py'), ('concentrated-venom-poison', 'ConcentratedVenomPoisonTests.py'), ('reward-revision', 'RewardRevisionTests.py'), ('threat-admission', 'ThreatAdmissionTests.py'), ('threat-admission-lifecycle', 'ThreatAdmissionLifecycleTests.py'), ('camp-practice-production', 'CampPracticeProductionTests.py'), ('camp-practice-session', 'CampPracticeSessionProductionTests.py'), ('camp-practice-wiring', 'CampPracticeWiringTests.py'), ('opportunity-channels-round2', 'OpportunityChannelsRound2Tests.py'), ('mobile-basic-window-draw', 'MobileBasicWindowDrawTests.py'), ('combat-result-pause-receipt', 'CombatResultPauseReceiptTests.py'), ('protection-presentation', 'ProtectionPresentationProductionTests.py'), ('enemy-status-visual', 'EnemyStatusVisualProductionTests.py'), ('hero-motion-style', 'HeroMotionStyleProductionTests.py'), ('hero-motion-factory', 'HeroMotionFactoryProductionTests.py'), ('g07-factory-inventory', 'G07FactoryInventoryTests.py'), ('build-draft-variant-effects', 'BuildDraftVariantEffectsProductionTests.py'), ('threat-admission-fairness', 'ThreatAdmissionFairnessProductionTests.py')]:
            passed = run_check(name, [[sys.executable, str(ROOT/"Tests"/script), dotnet]], dict(env, DOTNET=dotnet), output, report)
            failed = failed or not passed
        # Stability round 4: real transaction, input, and lifecycle regressions.
        for name, script in [
            ("enemy-kill-callback-fault", "EnemyKillCallbackFaultTests.py"),
            ("reward-presentation-exceptions", "RewardPresentationExceptionProductionTests.py"),
            ("chest-snapshot-retry", "ChestSnapshotRetryTests.py"),
            ("arena-reward-exception", "ArenaRewardExceptionTests.py"),
            ("ground-loot-callback-recovery", "GroundLootCallbackRecoveryTests.py"),
            ("mobile-pause-transition", "MobilePauseTransitionProductionTests.py"),
            ("practice-hotbar-navigation", "PracticeHotbarNavigationProductionTests.py"),
            ("practice-potion-input", "PracticePotionInputProductionTests.py"),
            ("companion-practice-timed-state", "CompanionPracticeTimedStateTests.py"),
            ("world-loot-receipt-source", "WorldLootReceiptSourceTests.py"),
        ]:
            passed = run_check(name, [[sys.executable, str(ROOT/"Tests"/script), dotnet]], dict(env, DOTNET=dotnet), output, report)
            failed = failed or not passed
        if args.compile or args.download_references or args.compile_android or args.compile_ios:
            try:
                refs = unity_references(args.download_references)
                sources = sorted((ROOT / "Assets/Scripts").rglob("*.cs"))
                variants = [("runtime-compile", "UNITY_STANDALONE;UNITY_STANDALONE_WIN")] if args.compile or args.download_references else []
                if args.compile_android:
                    variants.append(("android-runtime-compile", "UNITY_ANDROID"))
                if args.compile_ios:
                    variants.append(("ios-runtime-compile", "UNITY_IOS"))
                for name, defines in variants:
                    project = write_project(workspace / name, sources, references=list(refs.glob("*.dll")), defines=defines)
                    commands = [[dotnet, "restore", str(project), "--configfile", str(config), "--verbosity", "quiet"],
                                [dotnet, "build", str(project), "--no-restore", "--configuration", "Release", "--verbosity", "minimal"]]
                    passed = run_check(name, commands, env, output, report)
                    failed = failed or not passed
                report["runtimeSourceCount"] = len(sources)
            except (OSError, RuntimeError, subprocess.CalledProcessError) as error:
                print("FAIL reference-compile-setup: " + str(error), file=sys.stderr)
                report["checks"].append({"name": "reference-compile-setup", "passed": False, "error": str(error)})
                failed = True
        if args.unity_editor:
            editor = args.unity_editor.resolve()
            contents = editor.parent.parent if editor.parent.name == "MacOS" else editor.parent / "Data"
            host_define = "UNITY_EDITOR_OSX" if editor.parent.name == "MacOS" else "UNITY_EDITOR_WIN" if editor.suffix.lower() == ".exe" else "UNITY_EDITOR_LINUX"
            managed = contents / "Managed"
            modules = managed / "UnityEngine"
            if not (modules / "UnityEngine.CoreModule.dll").is_file():
                report["checks"].append({"name": "exact-unity-compile", "passed": False,
                                         "error": "Unity engine module DLLs not found beside the specified executable."})
                failed = True
            else:
                report["unityEditorReferences"] = str(args.unity_editor.resolve())
                for variant, extra_defines in [
                    ("runtime", "UNITY_STANDALONE;UNITY_STANDALONE_WIN"),
                    ("ios-runtime", "UNITY_IOS"),
                    ("android-runtime", "UNITY_ANDROID"),
                    ("editor", "UNITY_EDITOR;" + host_define),
                    ("android-editor", "UNITY_EDITOR;" + host_define + ";UNITY_ANDROID"),
                    ("ios-editor", "UNITY_EDITOR;" + host_define + ";UNITY_IOS"),
                    ("visual-validation", "EMBERFALL_VISUAL_VALIDATION;UNITY_STANDALONE;UNITY_STANDALONE_WIN"),
                ]:
                    name = "exact-unity-" + variant + "-compile"
                    refs = list(modules.glob("UnityEngine*.dll"))
                    sources = sorted((ROOT / "Assets/Scripts").rglob("*.cs"))
                    defines = "UNITY_2023_1_OR_NEWER;UNITY_6000_0_OR_NEWER;UNITY_6000_4_OR_NEWER;UNITY_6000_6_OR_NEWER;" + extra_defines
                    if variant in ("editor", "android-editor", "ios-editor"):
                        refs += list(modules.glob("UnityEditor*.dll"))
                        refs += list(managed.glob("UnityEditor*.dll"))
                        if variant == "android-editor":
                            refs += list((contents / "PlaybackEngines/AndroidPlayer").glob("**/UnityEditor.Android.Extensions.dll"))
                        if variant == "ios-editor":
                            refs += list((contents / "PlaybackEngines/iOSSupport").glob("**/UnityEditor.iOS.Extensions*.dll"))
                        refs = list({p.resolve():p for p in refs}.values())
                        sources += sorted((ROOT / "Assets/Editor").rglob("*.cs"))
                    elif variant == "visual-validation":
                        sources += sorted((ROOT / "Assets/Tests").rglob("*.cs"))
                    project = write_project(workspace / name, sources, references=refs,
                                            framework="netstandard2.1", defines=defines)
                    commands = [[dotnet, "restore", str(project), "--configfile", str(config), "--verbosity", "quiet"],
                                [dotnet, "build", str(project), "--no-restore", "--configuration", "Release", "--verbosity", "minimal"]]
                    passed = run_check(name, commands, env, output, report)
                    failed = failed or not passed
    final_sources = source_hashes()
    changed = sorted(path for path in initial_sources.keys() | final_sources.keys()
                     if initial_sources.get(path) != final_sources.get(path))
    report["sourceSha256"] = initial_sources
    report["sourceChangedDuringRun"] = changed
    if changed:
        print("Source changed during validation; rerun after edits finish: " + ", ".join(changed))
        failed = True
    report["completedUtc"] = datetime.now(timezone.utc).isoformat()
    report["passed"] = not failed
    (output / "report.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(report["scope"])
    print("Report: " + str(output / "report.json"))
    return 1 if failed else 0


def source_hashes():
    sources = sorted((ROOT / "Assets").rglob("*.cs")) + sorted((ROOT / "Tests").glob("*.cs"))
    sources += sorted((ROOT / "Tests").glob("*.py")) + sorted((ROOT / "Tools").glob("*.py")) + [ROOT / "Tests/Run-CloudValidation.sh"]
    return {str(path.relative_to(ROOT)): hashlib.sha256(path.read_bytes()).hexdigest() for path in sources}


def run_check(name, commands, env, output, report):
    chunks = []
    passed = True
    for command in commands:
        result = subprocess.run(command, env=env, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True)
        chunks.append(result.stdout)
        print(result.stdout, end="", flush=True)
        if result.returncode != 0:
            passed = False
            break
    log = output / (name + ".log")
    log.write_text("\n".join(chunks), encoding="utf-8")
    report["checks"].append({"name": name, "passed": passed, "log": log.name})
    print(("PASS " if passed else "FAIL ") + name, flush=True)
    return passed


if __name__ == "__main__":
    raise SystemExit(main())
