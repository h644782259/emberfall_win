#!/usr/bin/env python3
"""Execute production chapter host with real saves and managed scene boundary doubles."""
from CastReceiptFixtureSources import include_cast_receipt_source
import os,sys,tempfile,subprocess
from pathlib import Path
root=Path(__file__).resolve().parents[1]
dotnet=sys.argv[1] if len(sys.argv)>1 else os.environ.get('DOTNET','dotnet')
def method(file,signature):
    text=(root/file).read_text();start=text.index(signature);opening=text.index('{',start);depth=1;end=opening+1
    while depth:
        if text[end]=='{':depth+=1
        elif text[end]=='}':depth-=1
        end+=1
    return text[start:end]
core=['CombatImpactBatch','CampPracticeRecord','ThreatAdmissionPolicy','GameTypes', 'ProgressionService', 'ProgressionService.Chapter', 'ProgressionService.Attachments','ProgressionService.AutomaticGrowth','ProgressionService.Reforge', 'ReforgeQuote', 'ChapterProgression', 'ChapterResultSnapshot', 'ChapterCombatRun', 'RoomTactics', 'RoomChainState', 'ExpeditionModeState', 'RoomTacticalRegion', 'CombatBalance', 'HubTravelRules', 'MasteryCoreRuntime', 'TierRewardRules', 'TierRewardBand', 'ProgressionGoalState', 'AdventureResultPolicy', 'GameSession.Chapter', 'GameSession.ChapterSeals', 'EscapePostPolicy', 'RunChoices', 'RunChoices.Rooms', 'RunChoices.Chapter', 'RunMechanismEvidence']
with tempfile.TemporaryDirectory(prefix='chapter-host-production-') as temp:
    folder=Path(temp)
    for name in core:(folder/(name+'.cs')).write_text((root/'Assets/Scripts/Core'/(name+'.cs')).read_text())
    (folder/'ChapterSealPresentation.cs').write_text((root/'Assets/Scripts/UI/ChapterSealPresentation.cs').read_text())
    (folder/'EnemyControlPolicy.cs').write_text((root/'Assets/Scripts/Combat/EnemyControlPolicy.cs').read_text())
    (folder/'ChapterEntryPresentation.cs').write_text((root/'Assets/Scripts/UI/ChapterEntryPresentation.cs').read_text())
    (folder/'ChapterRoomGeometry.cs').write_text((root/'Assets/Scripts/World/ChapterRoomGeometry.cs').read_text())
    for name in ['ProgressionTests','ChapterHostFixture']:(folder/(name+'.cs')).write_text((root/'Tests'/(name+'.cs')).read_text())
    methods=[method('Assets/Scripts/Core/GameSession.cs',signature) for signature in ['private bool ChangeZone(bool dungeon)','public bool SaveBeforeLeaving()','private void SpawnEnemy(','public void OnEnemyKilled(','public void OnPlayerDied()','public void Respawn()']]
    methods.append(method('Assets/Scripts/Core/GameSession.Modes.cs','public bool ModeFinished'))
    methods.append(method('Assets/Scripts/Core/GameSession.RoomTactics.cs','private static bool LiveRoomEnemy('))
    methods.append(method('Assets/Scripts/Core/GameSession.Expedition.cs','private void ResetExpedition('))
    (folder/'Lifecycle.cs').write_text('using System.Collections.Generic;using UnityEngine;namespace Emberfall {public sealed partial class GameSession {'+'\n'.join(methods)+'}}')
    enemy=(root/'Assets/Scripts/Combat/EnemyController.cs').read_text()
    stat_start=enemy.index('            int challengeTier = game.InDungeon ? game.DungeonTier : 1;')
    stat_end=enemy.index('            Health = MaxHealth;',stat_start)+len('            Health = MaxHealth;')
    stats='using UnityEngine;namespace Emberfall {public sealed partial class EnemyController {public float MaxHealth,Health,damage;void ApplySpawnStats(GameSession game,int level,bool boss){var kind=Kind;'+enemy[stat_start:stat_end]+'}}}'
    (folder/'EnemyStats.cs').write_text(stats)
    (folder/'Program.cs').write_text('System.Console.WriteLine(ChapterHostProductionTests.Run(args[0]));System.Console.WriteLine(Emberfall.GameSession.VerifyChapterResult(args[0]));')
    project=folder/'Tests.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><NuGetAudit>false</NuGetAudit><NoWarn>0649;0414</NoWarn></PropertyGroup></Project>');include_cast_receipt_source(project)
    config=folder/'NuGet.Config';config.write_text('<configuration><packageSources><clear /></packageSources></configuration>')
    env=dict(os.environ,DOTNET_CLI_HOME=str(folder/'cli'),DOTNET_CLI_TELEMETRY_OPTOUT='1',DOTNET_NOLOGO='1')
    subprocess.run([dotnet,'restore',str(project),'--configfile',str(config),'-v:q'],env=env,check=True)
    command=[dotnet,'run','--project',str(project),'--no-restore','--',str(folder/'saves')]
    subprocess.run(command,env=env,check=True)
    # Compile mutants independently: an unrelated compile error is never a passing negative control.
    mutants=[('GameSession.Chapter.cs','if(ChapterRun!=null&&!ChapterRun.Finished&&!ChapterRun.Failed&&SelectedChapterTactic>=0)RunChoices.ChooseChapterTactic(Progression.Profile,receipt.Node,MobileControls.Active,SelectedChapterTactic);','if(SelectedChapterTactic>=0)RunChoices.ChooseChapterTactic(Progression.Profile,receipt.Node,MobileControls.Active,SelectedChapterTactic);','C failed spawn or unreachable entry never reapplies selected tactic after failure reset'),('GameSession.Chapter.cs','if(ChapterRun!=null&&!ChapterRun.Finished&&!ChapterRun.Failed&&SelectedChapterTactic>=0)RunChoices.ChooseChapterTactic(Progression.Profile,receipt.Node,MobileControls.Active,SelectedChapterTactic);','','C tactic applies after entry reset; first Hard lineup A'),('Lifecycle.cs','if(ChapterActive)FailChapter("角色倒下：本次章节挑战失败。");','if(ChapterActive){ChapterRun.Fail();Progression.CancelChapterRun();}', 'DEATH_RESULT_CAPTURE must exist before XP budget cancellation'),('GameSession.Chapter.cs','&&!LargeBossShutdownVisual.IsPresenting(this)','', 'ACTIVE_BOSS_EXIT must keep opaque result hidden'),('GameSession.Chapter.cs','index<2?EnemyKind.Wisp:index<4?EnemyKind.Guardian:index==4?EnemyKind.Goblin:EnemyKind.Slime',
              'index==0?EnemyKind.Wisp:index%3==1?EnemyKind.Guardian:index%3==2?EnemyKind.Goblin:EnemyKind.Slime',
              'crossfire roster uses two wisps and two guardians within six-enemy cap'),
             ('GameSession.Chapter.cs','if(!SaveBeforeLeaving())return false;','', 'entry save failure leaves old world and epoch intact'),
             ('Lifecycle.cs','InDungeon && !ChapterActive && ModeRun==null','InDungeon && ModeRun==null','chapter clear never schedules legacy NextWave or skips capture'),
             ('EnemyStats.cs','Health = MaxHealth;','MaxHealth*=ChapterDefinition.HealthMultiplier(game.ActiveChapterDifficulty);Health = MaxHealth;','chapter difficulty multiplies already tier-scaled stats exactly once')]
    mutants.append(('GameSession.Chapter.cs','if(redrockReplay&&ChapterRun!=null&&!ChapterRun.Finished&&!ChapterRun.Failed)','if(redrockReplay)','REPLAY_ADMISSION alternates only successfully admitted eligible runs'))
    mutants.append(('GameSession.Chapter.cs','redrockRouteOwner!=Progression.SaveFilePath||!previousRedrockSplit','true','REPLAY_ADMISSION alternates only successfully admitted eligible runs'))
    mutants += [('GameSession.Chapter.cs','if(!CanRetryChapter||!SaveBeforeLeaving())return false;','if(!CanRetryChapter)return false;','RETRY_SAVE atomic preflight preserves failed world and death state'),
                ('GameSession.Chapter.cs','receipt.Difficulty,chapterRetrySeed','receipt.Difficulty,chapterRetrySeed+1','RETRY_RESET rebuilds node start with same seed and new enemies')]
    for name,before,after,expected in mutants:
        path=folder/name;original=path.read_text()
        if before not in original:raise AssertionError('mutation anchor missing: '+name)
        path.write_text(original.replace(before,after,1))
        subprocess.run([dotnet,'build',str(project),'--no-restore','-v:q'],env=env,check=True,stdout=subprocess.DEVNULL)
        result=subprocess.run([dotnet,'run','--project',str(project),'--no-restore','--no-build','--',str(folder/('mutant-'+name))],env=env,capture_output=True,text=True)
        path.write_text(original)
        if result.returncode==0 or 'System.Exception: '+expected not in result.stdout+result.stderr:
            raise AssertionError('mutant failed to reach exact runtime oracle: '+name+'\n'+result.stdout+result.stderr)
    print(f'PASS: {len(mutants)} compiled chapter host mutations rejected by exact runtime assertions')
